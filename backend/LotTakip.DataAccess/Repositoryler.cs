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

        // Id'ler tamsayı olduğu için listeyi metne gömmek güvenli
        var idler = string.Join(",", parcaIdleri.Distinct());
        return await db.StokLotlari
            .FromSqlRaw($"SELECT * FROM StokLotu WITH (UPDLOCK, ROWLOCK) " +
                        $"WHERE ParcaId IN ({idler}) AND GeriCagrildi = 0 AND KalanAdet > 0")
            .OrderBy(l => l.SiparisTarihi).ThenBy(l => l.Id)
            .ToListAsync(ct);
    }
}

public class UretimRepository(AppDbContext db) : IUretimRepository
{
    public Task<List<string>> OnekliSeriNolarAsync(string onek, CancellationToken ct = default) =>
        db.Uretimler.Where(u => u.SeriNo.StartsWith(onek)).Select(u => u.SeriNo).ToListAsync(ct);
}
