namespace BusinessLayer.Abstract;

public interface IPostaGonderici
{
    Task GonderAsync(string alici, string konu, string metin, CancellationToken ct = default);
}
