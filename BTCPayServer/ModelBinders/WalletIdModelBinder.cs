using System.Reflection;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace BTCPayServer.ModelBinders;

public class WalletIdModelBinder : IModelBinder
{
    public Task BindModelAsync(ModelBindingContext bindingContext)
    {
        if (!typeof(WalletId).GetTypeInfo().IsAssignableFrom(bindingContext.ModelType))
        {
            return Task.CompletedTask;
        }

        ValueProviderResult val = bindingContext.ValueProvider.GetValue(
            bindingContext.ModelName);

        string key = val.FirstValue;
        if (key == null)
        {
            return Task.CompletedTask;
        }

        if (WalletId.TryParse(key, out WalletId walletId))
        {
            bindingContext.Result = ModelBindingResult.Success(walletId);
        }
        else
        {
            bindingContext.Result = ModelBindingResult.Failed();
            bindingContext.ModelState.AddModelError(bindingContext.ModelName, "Invalid wallet id");
        }
        return Task.CompletedTask;
    }
}
