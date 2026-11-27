namespace Kivilcim.Core.Abstractions;

/// <summary>Yüklenen dosyaların saklandığı yer (disk, bulut vb.). Uygulama bunun nasıl yapıldığını bilmez.</summary>
public interface IFileStorage
{
    /// <summary>Dosyayı kaydeder ve genel yolunu döner, örn. "/uploads/posts/abc.png".</summary>
    Task<string> SaveAsync(Stream content, string folder, string extension, CancellationToken ct = default);

    /// <summary>Genel yoldaki dosyayı siler. Yol boşsa ya da uygulama dışıysa hiçbir şey yapmaz.</summary>
    void Delete(string? publicPath);
}
