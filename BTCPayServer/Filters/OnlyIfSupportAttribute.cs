using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace BTCPayServer.Filters;

public class OnlyIfSupportAttribute : Attribute, IAsyncActionFilter
{
    private readonly string _cryptoCode;

    public OnlyIfSupportAttribute(string cryptoCode)
    {
        _cryptoCode = cryptoCode;
    }

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        BTCPayNetworkProvider options = context.HttpContext.RequestServices.GetService<BTCPayNetworkProvider>();
        if (options.GetNetwork(_cryptoCode) == null)
        {
            context.Result = new NotFoundResult();
            return;
        }

        await next();
    }
}
