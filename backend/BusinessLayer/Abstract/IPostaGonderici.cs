namespace BusinessLayer.Abstract;

/// <summary>E-posta gönderimi (API katmanında SMTP ile ya da ayarlı değilse dosyaya yazılarak).</summary>
public interface IPostaGonderici
{
    Task GonderAsync(string alici, string konu, string metin, CancellationToken ct = default);
}
