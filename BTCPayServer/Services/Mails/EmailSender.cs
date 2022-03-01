using BTCPayServer.Logging;
using MimeKit;

namespace BTCPayServer.Services.Mails;

public abstract class EmailSender : IEmailSender
{
    public Logs Logs { get; }

    private readonly IBackgroundJobClient _JobClient;

    public EmailSender(IBackgroundJobClient jobClient, Logs logs)
    {
        Logs = logs;
        _JobClient = jobClient ?? throw new ArgumentNullException(nameof(jobClient));
    }

    public void SendEmail(string email, string subject, string message)
    {
        _JobClient.Schedule(async (cancellationToken) =>
        {
            EmailSettings emailSettings = await GetEmailSettings();
            if (emailSettings?.IsComplete() != true)
            {
                Logs.Configuration.LogWarning("Should have sent email, but email settings are not configured");
                return;
            }
            using MailKit.Net.Smtp.SmtpClient smtp = await emailSettings.CreateSmtpClient();
            MimeMessage mail = emailSettings.CreateMailMessage(new MailboxAddress(email, email), subject, message, true);
            await smtp.SendAsync(mail, cancellationToken);
            await smtp.DisconnectAsync(true, cancellationToken);
        }, TimeSpan.Zero);
    }

    public abstract Task<EmailSettings> GetEmailSettings();
}
