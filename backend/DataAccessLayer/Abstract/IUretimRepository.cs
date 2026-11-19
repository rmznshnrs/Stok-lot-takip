namespace DataAccessLayer.Abstract;

public interface IUretimRepository
{
    Task<List<string>> OnekliSeriNolarAsync(string onek, CancellationToken ct = default);
}
