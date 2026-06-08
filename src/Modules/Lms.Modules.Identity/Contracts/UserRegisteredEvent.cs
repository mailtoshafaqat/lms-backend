using Lms.Shared.Events;

namespace Lms.Modules.Identity.Contracts;

/// <summary>Published when a new user registers. Other modules (e.g. enrollment, notifications)
/// can subscribe without the Identity module knowing about them.</summary>
public sealed record UserRegisteredEvent(Guid UserId, Guid TenantId, string Email, string Role) : IEvent;
