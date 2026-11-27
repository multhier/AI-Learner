using Kivilcim.Bll.Rules;

namespace Kivilcim.Bll.Media;

/// <summary>Dosya türü, boyutu ve gerçek içeriği (dosya imzası) kontrolü. Uzantı tek başına güvenilmez.</summary>
public static class MediaValidator
{
    private static readonly string[] ImageExtensions = { ".jpg", ".jpeg", ".png", ".gif", ".webp" };
    private static readonly string[] VideoExtensions = { ".mp4", ".webm" };

    public static string Extension(string fileName) => Path.GetExtension(fileName).ToLowerInvariant();

    /// <summary>Geçersizse Türkçe hata mesajı, geçerliyse null döner.</summary>
    public static string? ValidateImage(MediaFile file)
    {
        if (!ImageExtensions.Contains(Extension(file.FileName)))
        {
            return "Sadece JPG, PNG, GIF veya WEBP yükleyebilirsiniz.";
        }
        if (file.Length == 0 || file.Length > PostRules.MaxImageBytes)
        {
            return "Görsel en fazla 5 MB olabilir.";
        }
        return HasImageSignature(file) ? null : "Dosya geçerli bir görsel değil.";
    }

    public static string? ValidateVideo(MediaFile file)
    {
        var ext = Extension(file.FileName);
        if (!VideoExtensions.Contains(ext))
        {
            return "Sadece MP4 veya WEBM video yükleyebilirsiniz.";
        }
        if (file.Length == 0 || file.Length > PostRules.MaxVideoBytes)
        {
            return "Video en fazla 50 MB olabilir.";
        }
        return HasVideoSignature(file, ext) ? null : "Dosya geçerli bir video değil.";
    }

    private static bool HasImageSignature(MediaFile file)
    {
        var header = ReadHeader(file, 12);
        if (header.Length < 4) return false;
        bool Starts(params byte[] sig) => header.Take(sig.Length).SequenceEqual(sig);
        return Starts(0xFF, 0xD8, 0xFF)
            || Starts(0x89, 0x50, 0x4E, 0x47)
            || Starts(0x47, 0x49, 0x46, 0x38)
            || (header.Length >= 12 && Starts(0x52, 0x49, 0x46, 0x46) && header[8] == 0x57 && header[9] == 0x45 && header[10] == 0x42 && header[11] == 0x50);
    }

    private static bool HasVideoSignature(MediaFile file, string ext)
    {
        var header = ReadHeader(file, 12);
        if (header.Length < 8) return false;
        return ext == ".webm"
            ? header[0] == 0x1A && header[1] == 0x45 && header[2] == 0xDF && header[3] == 0xA3
            : header[4] == (byte)'f' && header[5] == (byte)'t' && header[6] == (byte)'y' && header[7] == (byte)'p';
    }

    private static byte[] ReadHeader(MediaFile file, int count)
    {
        using var stream = file.OpenRead();
        var buffer = new byte[count];
        var read = stream.Read(buffer, 0, count);
        return buffer.Take(read).ToArray();
    }
}
