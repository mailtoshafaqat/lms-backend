using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Lms.Shared.Modules;

/// <summary>
/// A feature module in the modular monolith. Each module registers its own services,
/// DbContext, and (optionally) controllers via its assembly. The host discovers and
/// wires modules, enabling plug-and-play per tenant via feature flags.
/// </summary>
public interface IModule
{
    string Name { get; }
    void RegisterServices(IServiceCollection services, IConfiguration configuration);
}
