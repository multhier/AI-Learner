namespace Kivilcim.Bll.Media;

/// <summary>Web katmanından gelen dosya. Bll, IFormFile gibi web tiplerini bilmez; yalnızca bu soyutlamayı görür.</summary>
public sealed record MediaFile(string FileName, long Length, Func<Stream> OpenRead);
