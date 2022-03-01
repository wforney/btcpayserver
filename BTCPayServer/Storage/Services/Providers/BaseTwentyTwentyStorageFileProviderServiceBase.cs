using BTCPayServer.Data;
using BTCPayServer.Storage.Models;
using BTCPayServer.Storage.Services.Providers.Models;
using TwentyTwenty.Storage;

namespace BTCPayServer.Storage.Services.Providers;

public abstract class
    BaseTwentyTwentyStorageFileProviderServiceBase<TStorageConfiguration> : IStorageProviderService
    where TStorageConfiguration : IBaseStorageConfiguration
{
    public abstract StorageProvider StorageProvider();

    public async Task<StoredFile> AddFile(IFormFile file, StorageSettings configuration)
    {
        //respect https://www.microsoftpressstore.com/articles/article.aspx?p=2224058&seqNum=8 in naming
        var storageFileName = $"{Guid.NewGuid()}-{file.FileName.ToLowerInvariant()}";
        TStorageConfiguration providerConfiguration = GetProviderConfiguration(configuration);
        IStorageProvider provider = await GetStorageProvider(providerConfiguration);
        using (System.IO.Stream fileStream = file.OpenReadStream())
        {
            await provider.SaveBlobStreamAsync(providerConfiguration.ContainerName, storageFileName, fileStream,
                new BlobProperties()
                {
                    ContentType = file.ContentType,
                    ContentDisposition = file.ContentDisposition,
                    Security = BlobSecurity.Public,
                });
        }

        return new StoredFile()
        {
            Timestamp = DateTime.UtcNow,
            FileName = file.FileName,
            StorageFileName = storageFileName
        };
    }

    public virtual async Task<string> GetFileUrl(Uri baseUri, StoredFile storedFile, StorageSettings configuration)
    {
        TStorageConfiguration providerConfiguration = GetProviderConfiguration(configuration);
        IStorageProvider provider = await GetStorageProvider(providerConfiguration);
        return provider.GetBlobUrl(providerConfiguration.ContainerName, storedFile.StorageFileName);
    }

    public virtual async Task<string> GetTemporaryFileUrl(Uri baseUri, StoredFile storedFile,
        StorageSettings configuration,
        DateTimeOffset expiry, bool isDownload, BlobUrlAccess access = BlobUrlAccess.Read)
    {
        TStorageConfiguration providerConfiguration = GetProviderConfiguration(configuration);
        IStorageProvider provider = await GetStorageProvider(providerConfiguration);
        if (isDownload)
        {
            BlobDescriptor descriptor =
                await provider.GetBlobDescriptorAsync(providerConfiguration.ContainerName,
                    storedFile.StorageFileName);
            return provider.GetBlobSasUrl(providerConfiguration.ContainerName, storedFile.StorageFileName, expiry,
                true, storedFile.FileName, descriptor.ContentType, access);
        }

        return provider.GetBlobSasUrl(providerConfiguration.ContainerName, storedFile.StorageFileName, expiry,
            false, null, null, access);
    }

    public async Task RemoveFile(StoredFile storedFile, StorageSettings configuration)
    {
        TStorageConfiguration providerConfiguration = GetProviderConfiguration(configuration);
        IStorageProvider provider = await GetStorageProvider(providerConfiguration);
        await provider.DeleteBlobAsync(providerConfiguration.ContainerName, storedFile.StorageFileName);
    }

    public TStorageConfiguration GetProviderConfiguration(StorageSettings configuration)
    {
        return configuration.Configuration.ToObject<TStorageConfiguration>();
    }


    protected abstract Task<IStorageProvider> GetStorageProvider(TStorageConfiguration configuration);
}
