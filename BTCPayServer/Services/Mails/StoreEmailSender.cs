using BTCPayServer.Data;
using BTCPayServer.Logging;
using BTCPayServer.Services.Stores;

namespace BTCPayServer.Services.Mails;

internal class StoreEmailSender : EmailSender
{
    public StoreEmailSender(StoreRepository storeRepository,
                            EmailSender fallback,
                            IBackgroundJobClient backgroundJobClient,
                            string storeId,
                            Logs logs) : base(backgroundJobClient, logs)
    {
        StoreId = storeId ?? throw new ArgumentNullException(nameof(storeId));
        StoreRepository = storeRepository;
        FallbackSender = fallback;
    }

    public StoreRepository StoreRepository { get; }
    public EmailSender FallbackSender { get; }
    public string StoreId { get; }

    public override async Task<EmailSettings> GetEmailSettings()
    {
        StoreData store = await StoreRepository.FindStore(StoreId);
        EmailSettings emailSettings = store.GetStoreBlob().EmailSettings;
        if (emailSettings?.IsComplete() == true)
        {
            return emailSettings;
        }

        if (FallbackSender != null)
        {
            return await FallbackSender?.GetEmailSettings();
        }

        return null;
    }
}
