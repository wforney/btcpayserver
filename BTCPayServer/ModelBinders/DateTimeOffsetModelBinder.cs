using System.Globalization;
using System.Reflection;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace BTCPayServer.ModelBinders;

public class DateTimeOffsetModelBinder : IModelBinder
{
    public Task BindModelAsync(ModelBindingContext bindingContext)
    {
        if (!typeof(DateTimeOffset).GetTypeInfo().IsAssignableFrom(bindingContext.ModelType) &&
            !typeof(DateTimeOffset?).GetTypeInfo().IsAssignableFrom(bindingContext.ModelType))
        {
            return Task.CompletedTask;
        }
        ValueProviderResult val = bindingContext.ValueProvider.GetValue(bindingContext.ModelName);
        string v = val.FirstValue;
        if (v == null)
        {
            return Task.CompletedTask;
        }

        try
        {
            var sec = long.Parse(v, CultureInfo.InvariantCulture);
            bindingContext.Result = ModelBindingResult.Success(NBitcoin.Utils.UnixTimeToDateTime(sec));
        }
        catch
        {
            bindingContext.Result = ModelBindingResult.Failed();
            bindingContext.ModelState.AddModelError(bindingContext.ModelName, "Invalid unix timestamp");
        }
        return Task.CompletedTask;
    }
}
