using BTCPayServer.Abstractions.Contracts;

namespace BTCPayServer.Components.Notifications;

public class NotificationsViewModel
{
    public int UnseenCount { get; set; }
    public List<NotificationViewModel> Last5 { get; set; }
}
