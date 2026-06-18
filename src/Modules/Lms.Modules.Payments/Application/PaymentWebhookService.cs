using System.Text;
using System.Text.Json;
using Lms.Modules.Payments.Domain;
using Lms.Modules.Payments.Infrastructure;
using Lms.Shared.Enrollments;
using Lms.Shared.Payments;
using Microsoft.EntityFrameworkCore;
using Stripe;

namespace Lms.Modules.Payments.Application;

public sealed class PaymentWebhookService : IPaymentWebhookService
{
    private const int MaxPayloadLength = 8000;
    private readonly PaymentsDbContext _db;
    private readonly ITenantPaymentSettingsProvider _settings;
    private readonly IEnrollmentWriter _enrollments;

    public PaymentWebhookService(
        PaymentsDbContext db,
        ITenantPaymentSettingsProvider settings,
        IEnrollmentWriter enrollments)
    {
        _db = db;
        _settings = settings;
        _enrollments = enrollments;
    }

    public async Task ProcessStripeWebhookAsync(string json, string signatureHeader, CancellationToken ct = default)
    {
        string? eventId = null;
        PaymentGateway gateway = PaymentGateway.Stripe;
        try
        {
            var stub = JsonDocument.Parse(json);
            eventId = stub.RootElement.TryGetProperty("id", out var idEl)
                ? idEl.GetString()
                : Guid.NewGuid().ToString();

            if (await _db.PaymentWebhookEvents.AnyAsync(
                    e => e.Gateway == gateway && e.ExternalEventId == eventId, ct))
                return;

            var tenantId = ExtractTenantId(stub);
            if (tenantId is null)
            {
                await RecordEventAsync(gateway, eventId, null, tenantId, json, false,
                    "Missing tenantId in Stripe event metadata.", ct);
                return;
            }

            var settings = await _settings.GetForTenantAsync(tenantId.Value, ct);
            if (settings?.StripeWebhookSecret is null or { Length: 0 })
            {
                await RecordEventAsync(gateway, eventId, null, tenantId, json, false,
                    "Stripe webhook secret not configured.", ct);
                return;
            }

            Event stripeEvent;
            try
            {
                stripeEvent = EventUtility.ConstructEvent(json, signatureHeader, settings.StripeWebhookSecret);
            }
            catch (Exception ex)
            {
                await RecordEventAsync(gateway, eventId, null, tenantId, json, false,
                    $"Signature verification failed: {ex.Message}", ct);
                return;
            }

            eventId = stripeEvent.Id;
            if (await _db.PaymentWebhookEvents.AnyAsync(
                    e => e.Gateway == gateway && e.ExternalEventId == eventId, ct))
                return;

            switch (stripeEvent.Type)
            {
                case "checkout.session.completed":
                    await HandleStripeSessionCompletedAsync(stripeEvent, tenantId.Value, json, ct);
                    break;
                case "checkout.session.expired":
                    await HandleStripeSessionFailedAsync(stripeEvent, tenantId.Value, json,
                        "Session expired", "session_expired", ct);
                    break;
                case "payment_intent.payment_failed":
                    await HandleStripePaymentFailedAsync(stripeEvent, tenantId.Value, json, ct);
                    break;
                default:
                    await RecordEventAsync(gateway, eventId, null, tenantId, json, true,
                        $"Ignored event type {stripeEvent.Type}.", ct);
                    break;
            }
        }
        catch (Exception ex)
        {
            await RecordEventAsync(gateway, eventId ?? Guid.NewGuid().ToString(), null, null, json, false,
                ex.Message, ct);
        }
    }

    public async Task ProcessJazzCashWebhookAsync(
        IReadOnlyDictionary<string, string> form, CancellationToken ct = default)
    {
        var payload = JsonSerializer.Serialize(form);
        var eventId = form.TryGetValue("pp_TxnRefNo", out var refNo) ? refNo : Guid.NewGuid().ToString();
        try
        {
            if (await _db.PaymentWebhookEvents.AnyAsync(
                    e => e.Gateway == PaymentGateway.JazzCash && e.ExternalEventId == eventId, ct))
                return;

            var order = await _db.PaymentOrders.IgnoreQueryFilters()
                .FirstOrDefaultAsync(o => o.ExternalPaymentId == eventId, ct);

            var responseCode = form.TryGetValue("pp_ResponseCode", out var code) ? code : null;
            var success = responseCode == "000" || responseCode == "121";

            if (order is null)
            {
                await RecordEventAsync(PaymentGateway.JazzCash, eventId, null, null, payload, false,
                    "Payment order not found for JazzCash reference.", ct);
                return;
            }

            if (success && order.Status != PaymentStatus.Paid)
                await MarkPaidAndEnrollAsync(order, form.TryGetValue("pp_TxnRefNo", out var ext) ? ext : eventId, ct);
            else if (!success)
            {
                order.Status = PaymentStatus.Failed;
                order.FailureCode = responseCode;
                order.FailureReason = form.TryGetValue("pp_ResponseMessage", out var msg) ? msg : "Payment failed";
                await _db.SaveChangesAsync(ct);
            }

            await RecordEventAsync(PaymentGateway.JazzCash, eventId, order.Id, order.TenantId, payload, success,
                success ? null : order.FailureReason, ct);
        }
        catch (Exception ex)
        {
            await RecordEventAsync(PaymentGateway.JazzCash, eventId, null, null, payload, false, ex.Message, ct);
        }
    }

    public async Task<PaymentWebhookResult> ProcessEasypaisaWebhookAsync(
        IReadOnlyDictionary<string, string> form, CancellationToken ct = default)
    {
        var payload = JsonSerializer.Serialize(form);
        var eventId = form.TryGetValue("orderRefNum", out var refNo) && !string.IsNullOrWhiteSpace(refNo)
            ? refNo
            : Guid.NewGuid().ToString();

        try
        {
            var duplicate = await _db.PaymentWebhookEvents.AnyAsync(
                e => e.Gateway == PaymentGateway.Easypaisa && e.ExternalEventId == eventId, ct);

            var order = await _db.PaymentOrders.IgnoreQueryFilters()
                .FirstOrDefaultAsync(o => o.ExternalPaymentId == eventId, ct);

            if (duplicate)
            {
                if (order is null)
                    return new PaymentWebhookResult(false, null, "Duplicate Easypaisa callback.");

                if (order.Status is not PaymentStatus.Processing and not PaymentStatus.Pending)
                    return new PaymentWebhookResult(
                        order.Status == PaymentStatus.Paid,
                        order.Id,
                        "Duplicate Easypaisa callback.");
            }

            if (order is null)
            {
                await RecordEventAsync(PaymentGateway.Easypaisa, eventId, null, null, payload, false,
                    "Payment order not found for Easypaisa reference.", ct);
                return new PaymentWebhookResult(false, null, "Order not found.");
            }

            var success = EasypaisaCheckoutHelper.IsSuccessResponse(form);

            if (success && order.Status != PaymentStatus.Paid)
            {
                var extId = form.TryGetValue("transactionId", out var txnId) ? txnId
                    : form.TryGetValue("authToken", out var auth) ? auth
                    : eventId;
                await MarkPaidAndEnrollAsync(order, extId, ct);
            }
            else if (!success && order.Status is PaymentStatus.Processing or PaymentStatus.Pending)
            {
                order.Status = PaymentStatus.Failed;
                order.FailureCode = form.TryGetValue("responseCode", out var code) ? code : null;
                order.FailureReason = form.TryGetValue("responseDesc", out var desc) ? desc
                    : form.TryGetValue("responseMessage", out var msg) ? msg
                    : "Easypaisa payment failed or was cancelled.";
                await _db.SaveChangesAsync(ct);
            }

            if (!duplicate)
            {
                await RecordEventAsync(
                    PaymentGateway.Easypaisa,
                    eventId,
                    order.Id,
                    order.TenantId,
                    payload,
                    success,
                    success ? null : order.FailureReason,
                    ct);
            }

            return new PaymentWebhookResult(success, order.Id, success ? null : order.FailureReason);
        }
        catch (Exception ex)
        {
            await RecordEventAsync(PaymentGateway.Easypaisa, eventId, null, null, payload, false, ex.Message, ct);
            return new PaymentWebhookResult(false, null, ex.Message);
        }
    }

    private async Task HandleStripeSessionCompletedAsync(
        Event stripeEvent, Guid tenantId, string json, CancellationToken ct)
    {
        var session = stripeEvent.Data.Object as Stripe.Checkout.Session;
        if (session is null)
        {
            await RecordEventAsync(PaymentGateway.Stripe, stripeEvent.Id, null, tenantId, json, false,
                "Invalid checkout session payload.", ct);
            return;
        }

        var orderId = ParseOrderId(session.Metadata);
        var order = await FindOrderAsync(orderId, session.Id, tenantId, ct);
        if (order is null)
        {
            await RecordEventAsync(PaymentGateway.Stripe, stripeEvent.Id, orderId, tenantId, json, false,
                "Payment order not found.", ct);
            return;
        }

        order.ExternalSessionId = session.Id;
        order.ExternalPaymentId = session.PaymentIntentId ?? session.Id;
        await MarkPaidAndEnrollAsync(order, order.ExternalPaymentId, ct);

        await RecordEventAsync(PaymentGateway.Stripe, stripeEvent.Id, order.Id, tenantId, json, true, null, ct);
    }

    private async Task HandleStripeSessionFailedAsync(
        Event stripeEvent, Guid tenantId, string json, string reason, string code, CancellationToken ct)
    {
        var session = stripeEvent.Data.Object as Stripe.Checkout.Session;
        var orderId = session is not null ? ParseOrderId(session.Metadata) : null;
        var order = await FindOrderAsync(orderId, session?.Id, tenantId, ct);
        if (order is not null)
        {
            order.Status = PaymentStatus.Failed;
            order.FailureReason = reason;
            order.FailureCode = code;
            if (session?.Id is not null) order.ExternalSessionId = session.Id;
            await _db.SaveChangesAsync(ct);
        }

        await RecordEventAsync(PaymentGateway.Stripe, stripeEvent.Id, order?.Id, tenantId, json,
            order is not null, order is null ? "Order not found." : null, ct);
    }

    private async Task HandleStripePaymentFailedAsync(
        Event stripeEvent, Guid tenantId, string json, CancellationToken ct)
    {
        var intent = stripeEvent.Data.Object as PaymentIntent;
        var order = intent is null
            ? null
            : await _db.PaymentOrders.IgnoreQueryFilters()
                .FirstOrDefaultAsync(o => o.TenantId == tenantId && o.ExternalPaymentId == intent.Id, ct);

        if (order is not null)
        {
            order.Status = PaymentStatus.Failed;
            order.ExternalPaymentId = intent.Id;
            order.FailureCode = intent.LastPaymentError?.Code;
            order.FailureReason = intent.LastPaymentError?.Message ?? "Payment failed";
            await _db.SaveChangesAsync(ct);
        }

        await RecordEventAsync(PaymentGateway.Stripe, stripeEvent.Id, order?.Id, tenantId, json,
            order is not null, order is null ? "Order not found for payment intent." : null, ct);
    }

    private async Task MarkPaidAndEnrollAsync(PaymentOrder order, string? externalId, CancellationToken ct)
    {
        if (order.Status == PaymentStatus.Paid) return;

        order.Status = PaymentStatus.Paid;
        order.PaidAt = DateTime.UtcNow;
        if (!string.IsNullOrWhiteSpace(externalId))
            order.ExternalPaymentId = externalId;

        var enrollment = await _enrollments.EnrollAsync(order.UserId, order.BundleId, ct);
        if (enrollment is not null)
            order.EnrollmentId = enrollment.Id;

        await _db.SaveChangesAsync(ct);
    }

    private async Task<PaymentOrder?> FindOrderAsync(
        Guid? orderId, string? sessionId, Guid tenantId, CancellationToken ct)
    {
        if (orderId is not null)
        {
            return await _db.PaymentOrders.IgnoreQueryFilters()
                .FirstOrDefaultAsync(o => o.Id == orderId && o.TenantId == tenantId, ct);
        }

        if (!string.IsNullOrEmpty(sessionId))
        {
            return await _db.PaymentOrders.IgnoreQueryFilters()
                .FirstOrDefaultAsync(o => o.ExternalSessionId == sessionId && o.TenantId == tenantId, ct);
        }

        return null;
    }

    private static Guid? ParseOrderId(IReadOnlyDictionary<string, string>? metadata)
    {
        if (metadata is null || !metadata.TryGetValue("orderId", out var raw)) return null;
        return Guid.TryParse(raw, out var id) ? id : null;
    }

    private static Guid? ExtractTenantId(JsonDocument doc)
    {
        if (!doc.RootElement.TryGetProperty("data", out var data)) return null;
        if (!data.TryGetProperty("object", out var obj)) return null;
        if (!obj.TryGetProperty("metadata", out var meta)) return null;
        if (!meta.TryGetProperty("tenantId", out var tenantEl)) return null;
        var raw = tenantEl.GetString();
        return Guid.TryParse(raw, out var id) ? id : null;
    }

    private async Task RecordEventAsync(
        PaymentGateway gateway,
        string eventId,
        Guid? orderId,
        Guid? tenantId,
        string rawPayload,
        bool success,
        string? error,
        CancellationToken ct)
    {
        var payload = rawPayload.Length <= MaxPayloadLength
            ? rawPayload
            : rawPayload[..MaxPayloadLength];

        _db.PaymentWebhookEvents.Add(new PaymentWebhookEvent
        {
            Gateway = gateway,
            EventType = gateway.ToString(),
            ExternalEventId = eventId,
            PaymentOrderId = orderId,
            TenantId = tenantId,
            RawPayload = payload,
            ProcessedAt = DateTime.UtcNow,
            Success = success,
            ErrorMessage = error
        });
        await _db.SaveChangesAsync(ct);
    }
}
