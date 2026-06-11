using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.DependencyInjection;

namespace Lms.Shared.Tenancy;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public sealed class RequireProductModuleAttribute : Attribute, IFilterFactory
{
    public RequireProductModuleAttribute(ProductModule module) => Module = module;

    public ProductModule Module { get; }

    public bool IsReusable => false;

    public IFilterMetadata CreateInstance(IServiceProvider serviceProvider)
    {
        var access = serviceProvider.GetRequiredService<ITenantModuleAccess>();
        return new RequireProductModuleFilter(Module, access);
    }
}

public sealed class RequireProductModuleFilter : IAsyncActionFilter
{
    private readonly ProductModule _module;
    private readonly ITenantModuleAccess _access;

    public RequireProductModuleFilter(ProductModule module, ITenantModuleAccess access)
    {
        _module = module;
        _access = access;
    }

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        if (!await _access.IsEnabledAsync(_module, context.HttpContext.RequestAborted))
        {
            context.Result = new Microsoft.AspNetCore.Mvc.NotFoundResult();
            return;
        }

        await next();
    }
}
