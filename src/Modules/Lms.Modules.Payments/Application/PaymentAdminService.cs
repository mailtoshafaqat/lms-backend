using System.Text.Json;
using Lms.Modules.Courses.Contracts;
using Lms.Modules.Payments.Domain;
using Lms.Modules.Payments.Infrastructure;
using Lms.Shared.Common;
using Lms.Shared.Enrollments;
using Lms.Shared.Payments;
using Lms.Shared.Tenancy;
using Lms.Shared.Users;
using Microsoft.EntityFrameworkCore;

namespace Lms.Modules.Payments.Application;

public sealed class PaymentAdminService : IPaymentAdminService
{
    private readonly PaymentsDbContext _db;
    private readonly IEnrollmentWriter _enrollments;
    private readonly IBundleCatalog _catalog;
    private readonly ITenantContext _tenant;
    private readonly ITenantPaymentSettingsProvider _settings;
    private readonly IUserDirectory _users;

    public PaymentAdminService(
        PaymentsDbContext db,
        IEnrollmentWriter enrollments,
        IBundleCatalog catalog,
        ITenantContext tenant,
        ITenantPaymentSettingsProvider settings,
        IUserDirectory users)
    {
        _db = db;
        _enrollments = enrollments;
        _catalog = catalog;
        _tenant = tenant;
        _settings = settings;
        _users = users;
    }

    public async Task<IReadOnlyList<AdminPaymentOrderDto>> ListOrdersAsync(
        PaymentStatus? status, CancellationToken ct = default)
    {
        var query = _db.PaymentOrders.AsNoTracking().AsQueryable();
        if (status is not null)
            query = query.Where(o => o.Status == status);

        var rows = await query
            .OrderByDescending(o => o.CreatedAt)
            .ToListAsync(ct);

        var contacts = await _users.GetContactsAsync(rows.Select(o => o.UserId), ct);

        return rows.Select(o =>
        {
            contacts.TryGetValue(o.UserId, out var contact);
            return new AdminPaymentOrderDto(
                o.Id,
                o.UserId,
                contact?.FullName,
                contact?.Email,
                o.BundleId,
                o.BundleTitle,
                o.Amount,
                o.Currency,
                o.Gateway,
                o.Status,
                o.ExternalPaymentId,
                ExtractNote(o.MetadataJson),
                o.MetadataJson,
                o.CreatedAt,
                o.PaidAt);
        }).ToList();
    }

    private static string? ExtractNote(string? metadataJson)
    {
        if (string.IsNullOrWhiteSpace(metadataJson)) return null;
        try
        {
            using var doc = JsonDocument.Parse(metadataJson);
            if (doc.RootElement.TryGetProperty("note", out var note)
                && note.ValueKind == JsonValueKind.String)
            {
                var value = note.GetString();
                return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
            }
        }
        catch (JsonException)
        {
            /* ignore malformed metadata */
        }

        return null;
    }

    public async Task<Result<PaymentOrderDto>> ApproveManualAsync(Guid orderId, CancellationToken ct = default)
    {
        var order = await _db.PaymentOrders.FirstOrDefaultAsync(o => o.Id == orderId, ct);
        if (order is null)
            return Result<PaymentOrderDto>.Failure("Payment order not found.");
        if (order.Gateway != PaymentGateway.Manual)
            return Result<PaymentOrderDto>.Failure("Only manual payments can be approved.");
        if (order.Status != PaymentStatus.AwaitingApproval)
            return Result<PaymentOrderDto>.Failure("Order is not awaiting approval.");

        order.Status = PaymentStatus.Paid;
        order.PaidAt = DateTime.UtcNow;

        var enrollment = await _enrollments.ProvisionEnrollAsync(order.UserId, order.BundleId, ct);
        if (enrollment is not null)
            order.EnrollmentId = enrollment.Id;

        await _db.SaveChangesAsync(ct);
        return Result<PaymentOrderDto>.Success(PaymentCheckoutService.MapOrder(order));
    }

    public async Task<Result<PaymentOrderDto>> RejectManualAsync(
        Guid orderId, string? reason, CancellationToken ct = default)
    {
        var order = await _db.PaymentOrders.FirstOrDefaultAsync(o => o.Id == orderId, ct);
        if (order is null)
            return Result<PaymentOrderDto>.Failure("Payment order not found.");
        if (order.Gateway != PaymentGateway.Manual)
            return Result<PaymentOrderDto>.Failure("Only manual payments can be rejected.");
        if (order.Status != PaymentStatus.AwaitingApproval)
            return Result<PaymentOrderDto>.Failure("Order is not awaiting approval.");

        order.Status = PaymentStatus.Failed;
        order.FailureReason = string.IsNullOrWhiteSpace(reason) ? "Rejected by admin" : reason.Trim();
        order.FailureCode = "admin_rejected";
        await _db.SaveChangesAsync(ct);
        return Result<PaymentOrderDto>.Success(PaymentCheckoutService.MapOrder(order));
    }

    public async Task<Result<PaymentOrderDto>> RecordManualForStudentAsync(
        AdminRecordManualPaymentRequest request, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(request.TransactionRef))
            return Result<PaymentOrderDto>.Failure("Transaction reference is required.");

        var bundle = await _catalog.GetBundleAsync(request.BundleId, ct);
        if (bundle is null || !bundle.IsPublished)
            return Result<PaymentOrderDto>.Failure("Bundle not found.");

        var settings = await _settings.GetAsync(ct);
        var now = DateTime.UtcNow;
        var txnRef = request.TransactionRef.Trim();

        var order = new PaymentOrder
        {
            TenantId = _tenant.TenantId,
            UserId = request.UserId,
            BundleId = bundle.Id,
            BundleTitle = bundle.Title,
            Amount = bundle.Price,
            Currency = settings?.Currency ?? "PKR",
            Gateway = PaymentGateway.Manual,
            Status = PaymentStatus.Paid,
            ExternalPaymentId = txnRef,
            PaidAt = now,
            MetadataJson = JsonSerializer.Serialize(new
            {
                source = "admin_record",
                note = string.IsNullOrWhiteSpace(request.Note) ? null : request.Note.Trim()
            })
        };

        _db.PaymentOrders.Add(order);

        var enrollment = await _enrollments.ProvisionEnrollAsync(request.UserId, bundle.Id, ct);
        if (enrollment is not null)
            order.EnrollmentId = enrollment.Id;

        await _db.SaveChangesAsync(ct);
        return Result<PaymentOrderDto>.Success(PaymentCheckoutService.MapOrder(order));
    }

    public Task<int> CountPendingManualAsync(CancellationToken ct = default) =>
        _db.PaymentOrders.CountAsync(
            o => o.Gateway == PaymentGateway.Manual && o.Status == PaymentStatus.AwaitingApproval,
            ct);
}
