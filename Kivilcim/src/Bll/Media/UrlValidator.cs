namespace Kivilcim.Bll.Media;

public static class UrlValidator
{
    public static bool IsSafeHttpUrl(string? url) =>
        Uri.TryCreate(url, UriKind.Absolute, out var uri) &&
        (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps);
}
