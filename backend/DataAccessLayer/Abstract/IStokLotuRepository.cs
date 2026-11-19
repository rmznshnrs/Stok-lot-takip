namespace DataAccessLayer.Abstract;

public interface IStokLotuRepository
{
    Task<List<StokLotu>> KullanilabilirLotlariKilitleAsync(IReadOnlyCollection<int> parcaIdleri, CancellationToken ct = default);
}
