using BTCPayServer.Services;
using Microsoft.AspNetCore.Mvc.ActionConstraints;

namespace BTCPayServer.Filters;

public class CheatModeRouteAttribute : Attribute, IActionConstraint
{
    public int Order => 100;

    public bool Accept(ActionConstraintContext context)
    {
        return context.RouteContext.HttpContext.RequestServices.GetRequiredService<BTCPayServerEnvironment>().CheatMode;
    }
}
