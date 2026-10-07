using LotTakip.DataAccess;
using LotTakip.Entity;
using LotTakip.Shared;
using Microsoft.EntityFrameworkCore;

namespace LotTakip.Business;

public interface ITanimServisi
{
    Task<ParcaOzetDto> ParcaOlusturAsync(ParcaIstek istek, CancellationToken ct = default);
    Task<ParcaOzetDto> ParcaGuncelleAsync(int id, ParcaIstek istek, CancellationToken ct = default);

    Task<List<UrunListeDto>> UrunleriListeleAsync(string? arama = null, CancellationToken ct = default);
    Task<UrunDetayDto?> UrunGetirAsync(int id, CancellationToken ct = default);
    Task<UrunDetayDto> UrunOlusturAsync(UrunIstek istek, CancellationToken ct = default);
    /// <summary>Üretimi olan ürünün seri öneki değiştirilemez.</summary>
    Task<UrunDetayDto> UrunGuncelleAsync(int id, UrunIstek istek, CancellationToken ct = default);
    /// <summary>
    /// Ürün ağacını verilen satırlarla değiştirir (eksik olan satırlar kaldırılır). Eski üretimler
    /// etkilenmez: hangi lottan kaç parça kullandıkları ayrıca saklanır.
    /// </summary>
    Task<UrunDetayDto> UrunAgaciKaydetAsync(int urunId, IReadOnlyList<UrunAgaciSatiriIstek> satirlar, CancellationToken ct = default);

    Task<List<IletisimDto>> TedarikcilerAsync(CancellationToken ct = default);
    Task<List<IletisimDto>> MusterilerAsync(CancellationToken ct = default);
}

public class TanimServisi(AppDbContext db, IStokServisi stok) : ITanimServisi
{
    // --- Parça ------------------------------------------------------------------

    public Task<ParcaOzetDto> ParcaOlusturAsync(ParcaIstek istek, CancellationToken ct = default) =>
        ParcaKaydetAsync(null, istek, ct);

    public Task<ParcaOzetDto> ParcaGuncelleAsync(int id, ParcaIstek istek, CancellationToken ct = default) =>
        ParcaKaydetAsync(id, istek, ct);

    private Task<ParcaOzetDto> ParcaKaydetAsync(int? id, ParcaIstek istek, CancellationToken ct)
    {
        var kod = (istek.Kod ?? "").Trim();
        var ad = Donusum.Sadelestir(istek.Ad);
        var birim = Donusum.Sadelestir(istek.Birim);
        if (kod.Length == 0 || ad.Length == 0)
            throw new IsKuraliHatasi("Parça kodu ve adı boş olamaz.");
        if (istek.MinStok < 0)
            throw new IsKuraliHatasi("Minimum stok negatif olamaz.");

        return Islem.CalistirAsync(db, async () =>
        {
            var parca = id is null
                ? db.Parcalar.Add(new Parca { Kod = kod, Ad = ad }).Entity
                : await db.Parcalar.SingleOrDefaultAsync(p => p.Id == id, ct) ?? throw new BulunamadiHatasi("Parça bulunamadı.");
            if (await db.Parcalar.AnyAsync(p => p.Kod == kod && p.Id != parca.Id, ct))
                throw new IsKuraliHatasi("Bu kodla bir parça zaten var.");
            (parca.Kod, parca.Ad, parca.Birim, parca.MinStok) = (kod, ad, birim.Length == 0 ? "adet" : birim, istek.MinStok);
            await db.SaveChangesAsync(ct);
            return Donusum.Ozet(parca);
        }, ct);
    }

    // --- Ürün -------------------------------------------------------------------

    public async Task<List<UrunListeDto>> UrunleriListeleAsync(string? arama = null, CancellationToken ct = default)
    {
        var q = db.Urunler.AsNoTracking();
        arama = arama?.Trim();
        if (!string.IsNullOrEmpty(arama))
            q = q.Where(u => u.Kod.Contains(arama) || u.Ad.Contains(arama));
        return await q.OrderBy(u => u.Kod)
            .Select(u => new UrunListeDto(u.Id, u.Kod, u.Ad, u.SeriOneki, u.Agac.Count, u.Uretimler.Count))
            .ToListAsync(ct);
    }

    public async Task<UrunDetayDto?> UrunGetirAsync(int id, CancellationToken ct = default)
    {
        var urun = await db.Urunler.AsNoTracking().Include(u => u.Agac).ThenInclude(a => a.Parca)
            .SingleOrDefaultAsync(u => u.Id == id, ct);
        if (urun is null)
            return null;
        var uretimVar = await db.Uretimler.AnyAsync(u => u.UrunId == id, ct);
        var stoklar = (await stok.ParcaStoklariAsync(ct: ct)).ToDictionary(p => p.Id);
        return new UrunDetayDto(urun.Id, urun.Kod, urun.Ad, urun.SeriOneki, !uretimVar,
            [.. urun.Agac.OrderBy(a => a.Parca!.Kod).Select(a => new UrunAgaciSatiriDto(
                a.ParcaId, a.Parca!.Kod, a.Parca.Ad, a.Parca.Birim, a.Adet,
                stoklar[a.ParcaId].KullanilabilirStok, stoklar[a.ParcaId].MinAltinda))]);
    }

    public Task<UrunDetayDto> UrunOlusturAsync(UrunIstek istek, CancellationToken ct = default) =>
        UrunKaydetAsync(null, istek, ct);

    public Task<UrunDetayDto> UrunGuncelleAsync(int id, UrunIstek istek, CancellationToken ct = default) =>
        UrunKaydetAsync(id, istek, ct);

    private async Task<UrunDetayDto> UrunKaydetAsync(int? id, UrunIstek istek, CancellationToken ct)
    {
        var kod = (istek.Kod ?? "").Trim();
        var ad = Donusum.Sadelestir(istek.Ad);
        var onek = (istek.SeriOneki ?? "").Trim();
        if (kod.Length == 0 || ad.Length == 0 || onek.Length == 0)
            throw new IsKuraliHatasi("Ürün kodu, adı ve seri no öneki boş olamaz.");

        var urunId = await Islem.CalistirAsync(db, async () =>
        {
            var urun = id is null
                ? db.Urunler.Add(new Urun { Kod = kod, Ad = ad, SeriOneki = onek }).Entity
                : await db.Urunler.SingleOrDefaultAsync(u => u.Id == id, ct) ?? throw new BulunamadiHatasi("Ürün bulunamadı.");
            if (await db.Urunler.AnyAsync(u => u.Kod == kod && u.Id != urun.Id, ct))
                throw new IsKuraliHatasi("Bu kodla bir ürün zaten var.");
            if (await db.Urunler.AnyAsync(u => u.SeriOneki == onek && u.Id != urun.Id, ct))
                throw new IsKuraliHatasi("Bu seri no öneki başka bir üründe kullanılıyor.");
            if (id is not null && !string.Equals(urun.SeriOneki, onek, StringComparison.OrdinalIgnoreCase)
                && await db.Uretimler.AnyAsync(u => u.UrunId == urun.Id, ct))
                throw new IsKuraliHatasi("Bu üründen üretim yapıldığı için seri no öneki değiştirilemez.");
            (urun.Kod, urun.Ad, urun.SeriOneki) = (kod, ad, onek);
            await db.SaveChangesAsync(ct);
            return urun.Id;
        }, ct);
        return (await UrunGetirAsync(urunId, ct))!;
    }

    public async Task<UrunDetayDto> UrunAgaciKaydetAsync(int urunId, IReadOnlyList<UrunAgaciSatiriIstek> satirlar,
                                                         CancellationToken ct = default)
    {
        if (satirlar.Any(s => s.Adet < 1))
            throw new IsKuraliHatasi("Ürün ağacında adet en az 1 olmalı.");
        var tekrar = satirlar.GroupBy(s => s.ParcaId).Where(g => g.Count() > 1).Select(g => g.Key).ToList();

        await Islem.CalistirAsync(db, async () =>
        {
            if (!await db.Urunler.AnyAsync(u => u.Id == urunId, ct))
                throw new BulunamadiHatasi("Ürün bulunamadı.");
            var idler = satirlar.Select(s => s.ParcaId).Distinct().ToList();
            var parcalar = await db.Parcalar.Where(p => idler.Contains(p.Id)).ToDictionaryAsync(p => p.Id, ct);
            if (parcalar.Count != idler.Count)
                throw new BulunamadiHatasi("Ürün ağacındaki parçalardan biri bulunamadı.");
            if (tekrar.Count > 0)
                throw new IsKuraliHatasi(
                    $"{string.Join(", ", tekrar.Select(i => parcalar[i].Kod))} ağaçta birden fazla kez var; adedini tek satırda girin.");

            var mevcut = await db.UrunAgaclari.Where(a => a.UrunId == urunId).ToListAsync(ct);
            db.UrunAgaclari.RemoveRange(mevcut.Where(a => !idler.Contains(a.ParcaId)));
            foreach (var s in satirlar)
            {
                var satir = mevcut.FirstOrDefault(a => a.ParcaId == s.ParcaId);
                if (satir is null)
                    db.UrunAgaclari.Add(new UrunAgaci { UrunId = urunId, ParcaId = s.ParcaId, Adet = s.Adet });
                else
                    satir.Adet = s.Adet;
            }
            await db.SaveChangesAsync(ct);
            return 0;
        }, ct);
        return (await UrunGetirAsync(urunId, ct))!;
    }

    // --- Listeler (seçim kutuları için) --------------------------------------------

    public Task<List<IletisimDto>> TedarikcilerAsync(CancellationToken ct = default) =>
        db.Tedarikciler.AsNoTracking().OrderBy(t => t.Ad).Select(t => new IletisimDto(t.Id, t.Ad, t.Iletisim)).ToListAsync(ct);

    public Task<List<IletisimDto>> MusterilerAsync(CancellationToken ct = default) =>
        db.Musteriler.AsNoTracking().OrderBy(m => m.Ad).Select(m => new IletisimDto(m.Id, m.Ad, m.Iletisim)).ToListAsync(ct);
}
