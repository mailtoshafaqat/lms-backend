using System.Reflection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Lms.Shared.Modules;

/// <summary>Host-side helper that registers a set of modules and exposes their assemblies
/// (so the API host can add their controllers as application parts).</summary>
public static class ModuleRegistry
{
    public static IReadOnlyList<Assembly> RegisterModules(
        this IServiceCollection services,
        IConfiguration configuration,
        params IModule[] modules)
    {
        var assemblies = new List<Assembly>();
        foreach (var module in modules)
        {
            module.RegisterServices(services, configuration);
            assemblies.Add(module.GetType().Assembly);
        }
        return assemblies;
    }
}
