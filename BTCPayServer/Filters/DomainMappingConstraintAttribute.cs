using BTCPayServer.Abstractions.Contracts;
using BTCPayServer.Services;
using BTCPayServer.Services.Apps;
using Microsoft.AspNetCore.Mvc.ActionConstraints;

namespace BTCPayServer.Filters;

public class DomainMappingConstraintAttribute : Attribute, IActionConstraint
{
    public DomainMappingConstraintAttribute()
    {

    }
    public DomainMappingConstraintAttribute(AppType appType)
    {
        AppType = appType;
    }
    public int Order => 100;
    public AppType? AppType { get; set; }

    public bool Accept(ActionConstraintContext context)
    {
        if (context.RouteContext.RouteData.Values.ContainsKey("appId"))
        {
            return true;
        }

        ISettingsRepository settingsRepository = context.RouteContext.HttpContext.RequestServices.GetService<ISettingsRepository>();
        PoliciesSettings policies = settingsRepository.GetPolicies().GetAwaiter().GetResult();
        if (policies?.DomainToAppMapping is { } mapping)
        {
            PoliciesSettings.DomainToAppMappingItem matchedDomainMapping = mapping.FirstOrDefault(item =>
            item.Domain.Equals(context.RouteContext.HttpContext.Request.Host.Host, StringComparison.InvariantCultureIgnoreCase));
            if (matchedDomainMapping != null)
            {
                if (!(AppType is { } appType))
                {
                    return false;
                }

                if (appType != matchedDomainMapping.AppType)
                {
                    return false;
                }

                context.RouteContext.RouteData.Values.Add("appId", matchedDomainMapping.AppId);
                return true;
            }

            if (AppType == policies.RootAppType)
            {
                context.RouteContext.RouteData.Values.Add("appId", policies.RootAppId);

                return true;
            }

            return AppType is null;
        }

        return AppType is null;
    }
}
