namespace Kivilcim.Core.Realtime;

/// <summary>Canlı bağlantı gruplarının adları. Aynı gruba katılan sayfalar aynı olayları alır.</summary>
public static class RealtimeGroups
{
    public static string Post(int postId) => $"post-{postId}";
}
