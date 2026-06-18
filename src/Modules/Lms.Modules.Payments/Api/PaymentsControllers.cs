using Lms.Modules.Payments.Application;
using Lms.Shared.Auth;
using Lms.Shared.Common;
using Lms.Shared.Configuration;
using Lms.Shared.Payments;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Lms.Modules.Payments.Api;

[ApiController]
[Route("api/v1/payments")]
[Authorize]
public sealed class PaymentsController : ControllerBase
{
    private readonly IPaymentCheckoutService _checkout;
    private readonly IPaymentGatewayResolver _resolver;
    private readonly ICurrentUser _currentUser;

    public PaymentsController(
        IPaymentCheckoutService checkout,
        IPaymentGatewayResolver resolver,
        ICurrentUser currentUser)
    {
        _checkout = checkout;
        _resolver = resolver;
        _currentUser = currentUser;
    }

    [HttpGet("available-gateways")]
    public async Task<IActionResult> AvailableGateways(
        [FromQuery] Guid bundleId,
        [FromQuery] string? studentCountry,
        CancellationToken ct)
    {
        if (_currentUser.Role != Roles.Student)
            return BadRequest(new { error = "Only students can view payment options." });
        return Ok(await _resolver.GetAvailableAsync(bundleId, studentCountry, ct));
    }

    [HttpPost("checkout")]
    public async Task<IActionResult> Checkout([FromBody] CheckoutRequest request, CancellationToken ct)
    {
        var userId = _currentUser.UserId;
        if (userId is null) return Unauthorized();
        if (_currentUser.Role != Roles.Student)
            return BadRequest(new { error = "Only students can checkout." });

        var result = await _checkout.StartCheckoutAsync(userId.Value, request, ct);
        return result.Succeeded ? Ok(result.Value) : BadRequest(new { error = result.Error });
    }

    [HttpPost("manual")]
    public async Task<IActionResult> Manual([FromBody] ManualPaymentRequest request, CancellationToken ct)
    {
        var userId = _currentUser.UserId;
        if (userId is null) return Unauthorized();
        if (_currentUser.Role != Roles.Student)
            return BadRequest(new { error = "Only students can submit manual payments." });

        var result = await _checkout.SubmitManualAsync(userId.Value, request, ct);
        return result.Succeeded ? Ok(result.Value) : BadRequest(new { error = result.Error });
    }
}

[ApiController]
[Route("api/v1/me/payments")]
[Authorize]
public sealed class MyPaymentsController : ControllerBase
{
    private readonly IPaymentCheckoutService _checkout;
    private readonly ICurrentUser _currentUser;

    public MyPaymentsController(IPaymentCheckoutService checkout, ICurrentUser currentUser)
    {
        _checkout = checkout;
        _currentUser = currentUser;
    }

    [HttpGet]
    public async Task<IActionResult> List(CancellationToken ct)
    {
        var userId = _currentUser.UserId;
        if (userId is null) return Unauthorized();
        return Ok(await _checkout.GetMyOrdersAsync(userId.Value, ct));
    }
}

[ApiController]
[Route("api/v1/payments/webhooks")]
public sealed class PaymentWebhooksController : ControllerBase
{
    private readonly IPaymentWebhookService _webhooks;
    private readonly IAppUrls _urls;

    public PaymentWebhooksController(IPaymentWebhookService webhooks, IAppUrls urls)
    {
        _webhooks = webhooks;
        _urls = urls;
    }

    [HttpPost("stripe")]
    [AllowAnonymous]
    public async Task<IActionResult> Stripe(CancellationToken ct)
    {
        using var reader = new StreamReader(Request.Body);
        var json = await reader.ReadToEndAsync(ct);
        var signature = Request.Headers["Stripe-Signature"].ToString();
        await _webhooks.ProcessStripeWebhookAsync(json, signature, ct);
        return Ok();
    }

    [HttpPost("jazzcash")]
    [AllowAnonymous]
    public async Task<IActionResult> JazzCash(CancellationToken ct)
    {
        var form = Request.Form.ToDictionary(k => k.Key, v => v.Value.ToString());
        await _webhooks.ProcessJazzCashWebhookAsync(form, ct);
        return Ok();
    }

    [HttpPost("easypaisa")]
    [HttpGet("easypaisa")]
    [AllowAnonymous]
    public async Task<IActionResult> Easypaisa(CancellationToken ct)
    {
        var form = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var kv in Request.Query)
            form[kv.Key] = kv.Value.ToString();
        if (Request.HasFormContentType)
        {
            foreach (var kv in Request.Form)
                form[kv.Key] = kv.Value.ToString();
        }

        var result = await _webhooks.ProcessEasypaisaWebhookAsync(form, ct);

        var frontend = _urls.FrontendBaseUrl;

        if (result.OrderId is Guid orderId)
        {
            return result.Success
                ? Redirect($"{frontend}/checkout/success?orderId={orderId}")
                : Redirect($"{frontend}/checkout/cancel?orderId={orderId}");
        }

        return result.Success ? Ok() : BadRequest(new { error = result.Message ?? "Easypaisa callback failed." });
    }
}

[ApiController]
[Route("api/v1/admin/payments")]
[Authorize(Policy = "InstituteAdmin")]
public sealed class AdminPaymentsController : ControllerBase
{
    private readonly IPaymentAdminService _admin;

    public AdminPaymentsController(IPaymentAdminService admin) => _admin = admin;

    [HttpGet]
    public async Task<IActionResult> List([FromQuery] PaymentStatus? status, CancellationToken ct) =>
        Ok(await _admin.ListOrdersAsync(status, ct));

    [HttpGet("pending-count")]
    public async Task<IActionResult> PendingCount(CancellationToken ct) =>
        Ok(new { count = await _admin.CountPendingManualAsync(ct) });

    [HttpPost("{id:guid}/approve")]
    public async Task<IActionResult> Approve(Guid id, CancellationToken ct)
    {
        var result = await _admin.ApproveManualAsync(id, ct);
        return result.Succeeded ? Ok(result.Value) : BadRequest(new { error = result.Error });
    }

    [HttpPost("{id:guid}/reject")]
    public async Task<IActionResult> Reject(Guid id, [FromBody] RejectPaymentRequest? body, CancellationToken ct)
    {
        var result = await _admin.RejectManualAsync(id, body?.Reason, ct);
        return result.Succeeded ? Ok(result.Value) : BadRequest(new { error = result.Error });
    }

    [HttpPost("record-manual")]
    public async Task<IActionResult> RecordManual(
        [FromBody] AdminRecordManualPaymentRequest req, CancellationToken ct)
    {
        var result = await _admin.RecordManualForStudentAsync(req, ct);
        return result.Succeeded ? Ok(result.Value) : BadRequest(new { error = result.Error });
    }
}

public sealed record RejectPaymentRequest(string? Reason);
