using BTCPayServer.Logging;

namespace BTCPayServer.Services.Mails;

internal class ServerEmailSender : EmailSender
{
    public ServerEmailSender(SettingsRepository settingsRepository,
                            IBackgroundJobClient backgroundJobClient,
                            Logs logs) : base(backgroundJobClient, logs)
    {
        ArgumentNullException.ThrowIfNull(settingsRepository);
        SettingsRepository = settingsRepository;
    }

    public SettingsRepository SettingsRepository { get; }

    public override Task<EmailSettings> GetEmailSettings()
    {
        return SettingsRepository.GetSettingAsync<EmailSettings>();
    }
}
