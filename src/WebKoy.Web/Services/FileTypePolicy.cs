namespace WebKoy.Web.Services;

/// <summary>Yüklenebilecek dosya türleri. Çalıştırılabilir ve sunucu tarafı betikler engellenir.</summary>
public static class FileTypePolicy
{
    private static readonly HashSet<string> Text = new(StringComparer.OrdinalIgnoreCase)
    {
        "html", "htm", "css", "js", "mjs", "json", "txt", "md", "xml", "svg", "csv", "tsv",
        "rss", "atom", "webmanifest", "map", "yml", "yaml", "toml", "glsl", "frag", "vert"
    };

    private static readonly HashSet<string> Binary = new(StringComparer.OrdinalIgnoreCase)
    {
        "png", "jpg", "jpeg", "gif", "webp", "avif", "ico", "bmp", "apng",
        "woff", "woff2", "ttf", "otf", "eot",
        "mp3", "ogg", "oga", "wav", "flac", "m4a", "opus", "mid", "midi",
        "mp4", "webm", "ogv",
        "pdf", "wasm", "glb", "gltf", "obj"
    };

    public static bool IsAllowed(string path)
    {
        var ext = SitePath.Extension(path);
        return Text.Contains(ext) || Binary.Contains(ext);
    }

    public static bool IsEditableText(string path) => Text.Contains(SitePath.Extension(path));

    public static string AllowedListForDisplay =>
        string.Join(", ", Text.Concat(Binary).OrderBy(x => x));
}
