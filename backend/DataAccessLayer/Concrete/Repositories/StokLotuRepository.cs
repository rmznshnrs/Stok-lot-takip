using Microsoft.EntityFrameworkCore;

namespace DataAccessLayer.Concrete.Repositories;

public class StokLotuRepository(AppDbContext db) : IStokLotuRepository
{
    public async Task<List<StokLotu>> KullanilabilirLotlariKilitleAsync(
        IReadOnlyCollection<int> parcaIdleri, CancellationToken ct = default)
    {
        if (parcaIdleri.Count == 0)
            return [];
        if (db.Database.CurrentTransaction is null)
            throw new InvalidOperationException("Lot kilidi yalnızca bir transaction içinde alınabilir.");

        // SQL metninde yalnız kilit ipucu var; koşullar LINQ ile eklenir ve EF onları
        // parametreli olarak alt sorguya yazar (SQL enjeksiyonuna kapalı).
        var idler = parcaIdleri.Distinct().ToList();
        return await db.StokLotlari
            .FromSql($"SELECT * FROM StokLotu WITH (UPDLOCK, ROWLOCK)")
            .Where(l => idler.Contains(l.ParcaId) && !l.GeriCagrildi && l.KalanAdet > 0)
            .OrderBy(l => l.SiparisTarihi).ThenBy(l => l.Id)
            .ToListAsync(ct);
    }
}
