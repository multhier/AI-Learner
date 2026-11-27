using Kivilcim.Model;

namespace Kivilcim.Bll.Utilities;

public static class NotificationTexts
{
    public static string Describe(NotificationType type) => type switch
    {
        NotificationType.Like => "gönderini beğendi",
        NotificationType.Reblog => "gönderini yeniden paylaştı",
        NotificationType.Reply => "gönderine yanıt yazdı",
        NotificationType.Follow => "seni takip etmeye başladı",
        _ => string.Empty
    };
}
