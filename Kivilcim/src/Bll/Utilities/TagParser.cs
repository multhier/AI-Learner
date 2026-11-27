using Kivilcim.Bll.Rules;

namespace Kivilcim.Bll.Utilities;

public static class TagParser
{
    public const int MaxTagLength = 50;

    /// <summary>"#Kedi, Yaz Tatili ,#kedi" → ["kedi", "yaz tatili"]</summary>
    public static List<string> Parse(string? input)
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            return new List<string>();
        }

        return input
            .Split(new[] { ',', '\n' }, StringSplitOptions.RemoveEmptyEntries)
            .Select(Normalize)
            .Where(t => t.Length > 0)
            .Distinct()
            .Take(PostRules.MaxTagsPerPost)
            .ToList();
    }

    public static string Normalize(string tag)
    {
        var cleaned = System.Text.RegularExpressions.Regex.Replace(
            tag.Trim().TrimStart('#').ToLowerInvariant(), @"[^\p{L}\p{N} _-]", "");
        cleaned = System.Text.RegularExpressions.Regex.Replace(cleaned, @"\s+", " ").Trim();
        return cleaned.Length > MaxTagLength ? cleaned[..MaxTagLength].Trim() : cleaned;
    }
}
