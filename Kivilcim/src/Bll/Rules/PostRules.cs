namespace Kivilcim.Bll.Rules;

/// <summary>Gönderi kuralları tek yerde: sınırlar buradan değiştirilir.</summary>
public static class PostRules
{
    public const int MaxImagesPerPost = 30;
    public const int MaxTagsPerPost = 10;
    public const int MaxImageBytes = 5 * 1024 * 1024;
    public const int MaxVideoBytes = 50 * 1024 * 1024;
    public const int MaxBodyLength = 10000;
    public const int MaxReplyLength = 2000;
    public const int MaxMessageLength = 4000;
}
