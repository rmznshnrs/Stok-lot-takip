namespace DataAccessLayer.Abstract;

public interface IUretimRepository
{
    /// <summary>Önekle başlayan tüm seri numaraları (büyük/küçük harf duyarsız).</summary>
    Task<List<string>> OnekliSeriNolarAsync(string onek, CancellationToken ct = default);
}
