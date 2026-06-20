using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Lms.Modules.Courses.Contracts;
using Lms.Modules.Payments.Domain;
using Lms.Modules.Payments.Infrastructure;
using Lms.Shared.Common;
using Lms.Shared.Configuration;
using Lms.Shared.Enrollments;
using Lms.Shared.Events;
using Lms.Shared.Payments;
using Lms.Shared.Tenancy;
using Lms.Shared.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Stripe;
using Stripe.Checkout;

namespace Lms.Modules.Payments.Application;

public sealed class PaymentCheckoutService : IPaymentCheckoutService
{
    private readonly PaymentsDbContext _db;
    private readonly IBundleCatalog _catalog;
    private readonly ITenantContext _tenant;
    private readonly ITenantPaymentSettingsProvider _settings;
    private readonly IPaymentGatewayResolver _resolver;
    private readonly IAppUrls _urls;
    private readonly PaymentsOptions _paymentsOptions;
    private readonly IEventBus _events;
    private readonly IUserDirectory _users;
    private readonly IBundleEnrollmentPolicy _batchPolicy;

    public PaymentCheckoutService(
        PaymentsDbContext db,
        IBundleCatalog catalog,
        ITenantContext tenant,
        ITenantPaymentSettingsProvider settings,
        IPaymentGatewayResolver resolver,
        IAppUrls urls,
        IOptions<PaymentsOptions> paymentsOptions,
        IEventBus events,
        IUserDirectory users,
        IBundleEnrollmentPolicy batchPolicy)
    {
        _db = db;
        _catalog = catalog;
        _tenant = tenant;
        _settings = settings;
        _resolver = resolver;
        _urls = urls;
        _paymentsOptions = paymentsOptions.Value;
        _events = events;
        _users = users;
        _batchPolicy = batchPolicy;
    }

    public async Task<Result<CheckoutResponse>> StartCheckoutAsync(
        Guid userId, CheckoutRequest request, CancellationToken ct = default)
    {
        var available = await _resolver.GetAvailableAsync(request.BundleId, request.StudentCountry, ct);
        if (!available.Any(g => g.Gateway == request.Gateway))
            return Result<CheckoutResponse>.Failure("Payment gateway is not available.");

        var bundle = await _catalog.GetBundleAsync(request.BundleId, ct);
        if (bundle is null || !bundle.IsPublished)
            return Result<CheckoutResponse>.Failure("Bundle not found.");

        var batchCheck = await _batchPolicy.CheckCanEnrollAsync(
            request.BundleId, BundleEnrollmentCheckMode.Student, ct);
        if (!batchCheck.Allowed)
            return Result<CheckoutResponse>.Failure(batchCheck.ErrorMessage ?? "Cannot enroll in this batch.");

        var settings = await _settings.GetAsync(ct);
        if (settings is null)
            return Result<CheckoutResponse>.Failure("Payment settings not configured.");

        if (await _db.PaymentOrders.AnyAsync(o => o.UserId == userId && o.BundleId == request.BundleId
                && o.Status == PaymentStatus.AwaitingApproval, ct))
        {
            return Result<CheckoutResponse>.Failure(
                "You already have a manual payment awaiting approval for this course.");
        }

        var pending = await _db.PaymentOrders
            .Where(o => o.UserId == userId && o.BundleId == request.BundleId
                && (o.Status == PaymentStatus.Pending || o.Status == PaymentStatus.Processing))
            .ToListAsync(ct);
        if (pending.Count > 0)
        {
            foreach (var abandoned in pending)
            {
                abandoned.Status = PaymentStatus.Failed;
                abandoned.FailureCode = "superseded";
                abandoned.FailureReason = "Replaced by a new checkout attempt.";
            }

            await _db.SaveChangesAsync(ct);
        }

        var order = new PaymentOrder
        {
            TenantId = _tenant.TenantId,
            UserId = userId,
            BundleId = bundle.Id,
            BundleTitle = bundle.Title,
            Amount = bundle.Price,
            Currency = settings.Currency,
            Gateway = request.Gateway,
            Status = PaymentStatus.Pending,
            StudentCountry = request.StudentCountry
        };

        _db.PaymentOrders.Add(order);
        await _db.SaveChangesAsync(ct);

        return request.Gateway switch
        {
            PaymentGateway.Stripe => await StartStripeAsync(order, settings, ct),
            PaymentGateway.JazzCash => await StartJazzCashAsync(order, settings, ct),
            PaymentGateway.Easypaisa => await StartEasypaisaAsync(order, settings, ct),
            _ => Result<CheckoutResponse>.Failure("Use manual payment for this gateway.")
        };
    }

    public async Task<Result<PaymentOrderDto>> SubmitManualAsync(
        Guid userId, ManualPaymentRequest request, CancellationToken ct = default)
    {
        var available = await _resolver.GetAvailableAsync(request.BundleId, request.StudentCountry, ct);
        if (!available.Any(g => g.Gateway == PaymentGateway.Manual))
            return Result<PaymentOrderDto>.Failure("Manual payment is not available.");

        var bundle = await _catalog.GetBundleAsync(request.BundleId, ct);
        if (bundle is null || !bundle.IsPublished)
            return Result<PaymentOrderDto>.Failure("Bundle not found.");

        if (string.IsNullOrWhiteSpace(request.TransactionRef))
            return Result<PaymentOrderDto>.Failure("Transaction reference is required.");

        var settings = await _settings.GetAsync(ct);
        var order = new PaymentOrder
        {
            TenantId = _tenant.TenantId,
            UserId = userId,
            BundleId = bundle.Id,
            BundleTitle = bundle.Title,
            Amount = bundle.Price,
            Currency = settings?.Currency ?? "PKR",
            Gateway = PaymentGateway.Manual,
            Status = PaymentStatus.AwaitingApproval,
            ExternalPaymentId = request.TransactionRef.Trim(),
            StudentCountry = request.StudentCountry,
            MetadataJson = string.IsNullOrWhiteSpace(request.Note)
                ? null
                : JsonSerializer.Serialize(new { note = request.Note.Trim() })
        };

        _db.PaymentOrders.Add(order);
        await _db.SaveChangesAsync(ct);

        var contacts = await _users.GetContactsAsync([userId], ct);
        contacts.TryGetValue(userId, out var contact);
        await _events.PublishAsync(
            new ManualPaymentSubmittedEvent(
                order.Id,
                order.TenantId,
                userId,
                contact?.FullName ?? "Student",
                contact?.Email ?? "",
                bundle.Id,
                bundle.Title,
                order.Amount,
                order.Currency,
                order.ExternalPaymentId ?? request.TransactionRef.Trim(),
                string.IsNullOrWhiteSpace(request.Note) ? null : request.Note.Trim()),
            ct);

        return Result<PaymentOrderDto>.Success(MapOrder(order));
    }

    public async Task<IReadOnlyList<PaymentOrderDto>> GetMyOrdersAsync(
        Guid userId, CancellationToken ct = default)
    {
        var rows = await _db.PaymentOrders
            .Where(o => o.UserId == userId)
            .OrderByDescending(o => o.CreatedAt)
            .ToListAsync(ct);
        return rows.Select(MapOrder).ToList();
    }

    private async Task<Result<CheckoutResponse>> StartStripeAsync(
        PaymentOrder order, TenantPaymentSettings settings, CancellationToken ct)
    {
        if (string.IsNullOrEmpty(settings.StripeSecretKey))
            return Result<CheckoutResponse>.Failure("Stripe is not configured.");

        var frontend = _urls.FrontendBaseUrl;

        StripeConfiguration.ApiKey = settings.StripeSecretKey;

        var amountMinor = (long)Math.Round(order.Amount * 100m, MidpointRounding.AwayFromZero);
        var options = new SessionCreateOptions
        {
            Mode = "payment",
            SuccessUrl = $"{frontend.TrimEnd('/')}/checkout/success?orderId={order.Id}",
            CancelUrl = $"{frontend.TrimEnd('/')}/checkout/cancel?orderId={order.Id}",
            LineItems =
            [
                new SessionLineItemOptions
                {
                    Quantity = 1,
                    PriceData = new SessionLineItemPriceDataOptions
                    {
                        Currency = order.Currency.ToLowerInvariant(),
                        UnitAmount = amountMinor,
                        ProductData = new SessionLineItemPriceDataProductDataOptions
                        {
                            Name = order.BundleTitle
                        }
                    }
                }
            ],
            Metadata = new Dictionary<string, string>
            {
                ["tenantId"] = order.TenantId.ToString(),
                ["orderId"] = order.Id.ToString(),
                ["userId"] = order.UserId.ToString(),
                ["bundleId"] = order.BundleId.ToString()
            }
        };

        var service = new SessionService();
        var session = await service.CreateAsync(options, cancellationToken: ct);

        order.ExternalSessionId = session.Id;
        order.Status = PaymentStatus.Processing;
        await _db.SaveChangesAsync(ct);

        return Result<CheckoutResponse>.Success(new CheckoutResponse(
            order.Id,
            PaymentGateway.Stripe,
            order.Status,
            session.Url,
            session.Id,
            null));
    }

    private async Task<Result<CheckoutResponse>> StartJazzCashAsync(
        PaymentOrder order, TenantPaymentSettings settings, CancellationToken ct)
    {
        if (string.IsNullOrEmpty(settings.JazzCashMerchantId)
            || string.IsNullOrEmpty(settings.JazzCashPassword)
            || string.IsNullOrEmpty(settings.JazzCashHashKey))
        {
            return Result<CheckoutResponse>.Failure("JazzCash is not configured.");
        }

        var sandbox = _paymentsOptions.JazzCashSandbox;
        var baseUrl = sandbox
            ? "https://sandbox.jazzcash.com.pk/CustomerPortal/transactionmanagement/merchantform/"
            : "https://payments.jazzcash.com.pk/CustomerPortal/transactionmanagement/merchantform/";

        var frontend = _urls.FrontendBaseUrl;

        var returnUrl = settings.JazzCashReturnUrl
            ?? $"{frontend.TrimEnd('/')}/checkout/success?orderId={order.Id}";

        var txnRef = order.Id.ToString("N")[..20];
        var amountPaisa = ((long)Math.Round(order.Amount * 100m, MidpointRounding.AwayFromZero)).ToString();
        var txnDateTime = DateTime.UtcNow.ToString("yyyyMMddHHmmss");
        var expiry = DateTime.UtcNow.AddHours(1).ToString("yyyyMMddHHmmss");

        var fields = new SortedDictionary<string, string>(StringComparer.Ordinal)
        {
            ["pp_Amount"] = amountPaisa,
            ["pp_BillReference"] = txnRef,
            ["pp_Description"] = order.BundleTitle.Length > 200 ? order.BundleTitle[..200] : order.BundleTitle,
            ["pp_Language"] = "EN",
            ["pp_MerchantID"] = settings.JazzCashMerchantId,
            ["pp_Password"] = settings.JazzCashPassword,
            ["pp_ReturnURL"] = returnUrl,
            ["pp_TxnCurrency"] = order.Currency,
            ["pp_TxnDateTime"] = txnDateTime,
            ["pp_TxnExpiryDateTime"] = expiry,
            ["pp_TxnRefNo"] = txnRef,
            ["pp_TxnType"] = "MWALLET",
            ["pp_Version"] = "1.1"
        };

        var hashInput = settings.JazzCashHashKey + string.Concat(fields.Values);
        var hash = ComputeSha256(hashInput);
        fields["pp_SecureHash"] = hash;

        order.ExternalPaymentId = txnRef;
        order.Status = PaymentStatus.Processing;
        order.MetadataJson = JsonSerializer.Serialize(fields);
        await _db.SaveChangesAsync(ct);

        var query = string.Join("&", fields.Select(kv =>
            $"{Uri.EscapeDataString(kv.Key)}={Uri.EscapeDataString(kv.Value)}"));
        var checkoutUrl = $"{baseUrl}?{query}";

        return Result<CheckoutResponse>.Success(new CheckoutResponse(
            order.Id,
            PaymentGateway.JazzCash,
            order.Status,
            checkoutUrl,
            txnRef,
            null,
            null));
    }

    private async Task<Result<CheckoutResponse>> StartEasypaisaAsync(
        PaymentOrder order, TenantPaymentSettings settings, CancellationToken ct)
    {
        if (string.IsNullOrEmpty(settings.EasypaisaStoreId)
            || string.IsNullOrEmpty(settings.EasypaisaHashKey))
        {
            return Result<CheckoutResponse>.Failure("Easypaisa is not configured.");
        }

        var sandbox = _paymentsOptions.EasypaisaSandbox;
        var actionUrl = sandbox ? EasypaisaCheckoutHelper.SandboxUrl : EasypaisaCheckoutHelper.ProductionUrl;

        var postBackUrl = $"{_urls.ApiBaseUrl}/api/v1/payments/webhooks/easypaisa";
        var orderRef = EasypaisaCheckoutHelper.OrderReference(order.Id);

        var fields = EasypaisaCheckoutHelper.BuildHostedFormFields(
            settings.EasypaisaStoreId,
            settings.EasypaisaHashKey,
            postBackUrl,
            orderRef,
            order.Amount);

        order.ExternalPaymentId = orderRef;
        order.Status = PaymentStatus.Processing;
        order.MetadataJson = JsonSerializer.Serialize(fields);
        await _db.SaveChangesAsync(ct);

        return Result<CheckoutResponse>.Success(new CheckoutResponse(
            order.Id,
            PaymentGateway.Easypaisa,
            order.Status,
            null,
            orderRef,
            null,
            new CheckoutFormPost(actionUrl, fields)));
    }

    internal static PaymentOrderDto MapOrder(PaymentOrder o) => new(
        o.Id, o.BundleId, o.BundleTitle, o.Amount, o.Currency, o.Gateway, o.Status,
        o.ExternalPaymentId, o.PaidAt, o.EnrollmentId, o.CreatedAt, o.FailureReason);

    private static string ComputeSha256(string input)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(input));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }
}
