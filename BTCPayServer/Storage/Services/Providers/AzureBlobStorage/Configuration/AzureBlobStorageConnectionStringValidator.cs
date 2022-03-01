namespace BTCPayServer.Storage.Services.Providers.AzureBlobStorage.Configuration;

using System;
using System.ComponentModel.DataAnnotations;
using Microsoft.WindowsAzure.Storage;

public class AzureBlobStorageConnectionStringValidator : ValidationAttribute
{
    protected override ValidationResult IsValid(object value, ValidationContext validationContext)
    {
        try
        {
            _ = CloudStorageAccount.Parse(value as string);
            return ValidationResult.Success;
        }
        catch (Exception e)
        {
            return new ValidationResult(e.Message);
        }
    }
}
