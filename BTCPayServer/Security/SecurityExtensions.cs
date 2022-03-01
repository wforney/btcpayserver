using Microsoft.AspNetCore.Authorization;

namespace BTCPayServer.Security;

public static class SecurityExtensions
{
    public static bool HasScopes(this AuthorizationHandlerContext context, params string[] scopes)
    {
        return scopes.All(s => context.User.HasClaim(c => c.Type.Equals("scope", StringComparison.InvariantCultureIgnoreCase) && c.Value.Split(' ').Contains(s)));
    }

    public static string GetImplicitStoreId(this HttpContext httpContext)
    {
        // 1. Check in the routeData
        RouteData routeData = httpContext.GetRouteData();
        string storeId = null;
        if (routeData != null)
        {
            if (routeData.Values.TryGetValue("storeId", out var v))
            {
                storeId = v as string;
            }
        }

        if (storeId == null)
        {
            if (httpContext.Request.Query.TryGetValue("storeId", out Microsoft.Extensions.Primitives.StringValues sv))
            {
                storeId = sv.FirstOrDefault();
            }
        }

        // 2. Check in forms
        if (storeId == null)
        {
            if (httpContext.Request.HasFormContentType &&
                httpContext.Request.Form != null &&
                httpContext.Request.Form.TryGetValue("storeId", out Microsoft.Extensions.Primitives.StringValues sv))
            {
                storeId = sv.FirstOrDefault();
            }
        }

        // 3. Checks in walletId
        if (storeId == null && routeData != null)
        {
            if (routeData.Values.TryGetValue("walletId", out var walletId) &&
                WalletId.TryParse((string)walletId, out WalletId w))
            {
                storeId = w.StoreId;
            }
        }

        return storeId;
    }
}
