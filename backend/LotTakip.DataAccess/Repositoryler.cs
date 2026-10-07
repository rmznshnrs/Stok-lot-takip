using LotTakip.Entity;
using Microsoft.EntityFrameworkCore;

namespace LotTakip.DataAccess;

// İnce repository'ler: yalnızca LINQ ile ifade edilemeyen veya birden çok yerde kullanılan
// sorgular. Basit sorgular servislerde doğrudan AppDbContext ile yazılır.

public interface IStokLotuRepository
{
    /// <summary>
    /// Parçaların kullanılabilir lotlarını (geri çağrılmamış, kalanı olan) FIFO sırasıyla getirir
    /// ve transaction sonuna kadar kilitler (UPDLOCK). Aynı anda başka bir üretim aynı lotları
    /// okumak isterse bu transaction bitene kadar bekler. Açık bir transaction içinde çağrılmalı.
    /// </summary>
    Task<List<StokLotu>> KullanilabilirLotlariKilitleAsync(IReadOnlyCollection<int> parcaIdleri, CancellationToken ct = default);
}

public interface IUretimRepository
{
    /// <summary>Önekle başlayan tüm seri numaraları (büyük/küçük harf duyarsız).</summary>
    Task<List<string>> OnekliSeriNolarAsync(string onek, CancellationToken ct = default);
}

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

public class UretimRepository(AppDbContext db) : IUretimRepository
{
    public Task<List<string>> OnekliSeriNolarAsync(string onek, CancellationToken ct = default) =>
        db.Uretimler.Where(u => u.SeriNo.StartsWith(onek)).Select(u => u.SeriNo).ToListAsync(ct);
}
