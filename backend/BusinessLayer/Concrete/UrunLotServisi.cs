using Microsoft.EntityFrameworkCore;

namespace BusinessLayer.Concrete;

public class UrunLotServisi(AppDbContext db, IGeriCagirmaServisi geri) : IUrunLotServisi
{
    public Task<List<UrunLotDto>> ListeAsync(int? urunId = null, CancellationToken ct = default) =>
        ListeAsync(urunId, null, ct);

    private async Task<List<UrunLotDto>> ListeAsync(int? urunId, string? lotNo, CancellationToken ct)
    {
        var q = db.Uretimler.AsNoTracking().Where(u => u.LotNo != "");
        if (urunId is { } id)
            q = q.Where(u => u.UrunId == id);
        if (lotNo is not null)
            q = q.Where(u => u.LotNo == lotNo);
        var satirlar = await q.Select(u => new
        {
            u.LotNo, u.UrunId, UrunAd = u.Urun!.Ad, u.Tarih,
            Satildi = u.SatisKalemi != null, Hurda = u.Hurda != null,
            Acik = u.Hurda == null && u.GeriCagirmalar.Any(g => g.KapanmaZamani == null),
        }).ToListAsync(ct);
        var lotNolar = satirlar.Select(s => s.LotNo).Distinct().ToList();
        var sorunlar = await db.UrunLotSorunlari.AsNoTracking().Where(s => lotNolar.Contains(s.LotNo))
            .ToDictionaryAsync(s => s.LotNo, StringComparer.OrdinalIgnoreCase, ct);

        return [.. satirlar.GroupBy(s => (s.LotNo, s.UrunId, s.UrunAd))
            .Select(g =>
            {
                var sorun = sorunlar.GetValueOrDefault(g.Key.LotNo);
                return new UrunLotDto(
                    g.Key.LotNo, g.Key.UrunId, g.Key.UrunAd, g.Min(s => s.Tarih), g.Count(),
                    g.Count(s => !s.Satildi && !s.Hurda), g.Count(s => s.Satildi), g.Count(s => s.Hurda), g.Count(s => s.Acik),
                    sorun is not null, sorun?.Aciklama ?? "", sorun?.HataliAdet ?? 0, sorun?.OlusturmaZamani);
            })
            .OrderByDescending(l => l.Tarih).ThenByDescending(l => l.LotNo)];
    }

    public Task<UrunLotSorunSonucuDto> SorunAsync(string lotNo, UrunLotSorunIstek istek, int kullaniciId,
                                                  CancellationToken ct = default)
    {
        var no = (lotNo ?? "").Trim();
        var aciklama = (istek.Aciklama ?? "").Trim();
        if (istek.Sorunlu && aciklama.Length == 0)
            throw new IsKuraliHatasi("Açıklama girin.");
        if (istek.HataliAdet < 0)
            throw new IsKuraliHatasi("Hatalı adet eksi olamaz.");

        return Islem.CalistirAsync(db, async () =>
        {
            var admin = await Kullanicilar.AdminGetirAsync(db, kullaniciId, "Hatalı ürün lotu işaretleme", ct);
            var uretimler = await db.Uretimler.Where(u => u.LotNo == no)
                .Select(u => new { u.SeriNo, u.LotNo, u.UrunId }).ToListAsync(ct);
            if (uretimler.Count == 0)
                throw new BulunamadiHatasi($"{no} ürün lotu bulunamadı.");
            var lotAdi = uretimler[0].LotNo;  // veritabanındaki yazımla
            if (istek.HataliAdet > uretimler.Count)
                throw new IsKuraliHatasi($"Hatalı adet {uretimler.Count} üründen fazla olamaz.");

            var kayit = await db.UrunLotSorunlari.SingleOrDefaultAsync(s => s.LotNo == lotAdi, ct);
            List<string> geriCagrilan = [];
            if (istek.Sorunlu)
            {
                if (kayit is null)
                {
                    db.UrunLotSorunlari.Add(new UrunLotSorunu
                    {
                        UrunId = uretimler[0].UrunId, LotNo = lotAdi, Aciklama = aciklama, HataliAdet = istek.HataliAdet,
                        OlusturanKullanici = admin, OlusturmaZamani = Zaman.Simdi(),
                    });
                }
                else
                {
                    (kayit.Aciklama, kayit.HataliAdet) = (aciklama, istek.HataliAdet);
                }
                await db.SaveChangesAsync(ct);

                var secilen = SeriListesi.Temizle(istek.GeriCagrilacaklar);
                var yabanci = secilen.Where(s => !uretimler.Any(u => string.Equals(u.SeriNo, s, StringComparison.OrdinalIgnoreCase))).ToList();
                if (yabanci.Count > 0)
                    throw new IsKuraliHatasi($"Bu lotta yok: {string.Join(", ", yabanci)}");
                if (secilen.Count > 0)
                    geriCagrilan = [.. (await geri.GeriCagirAsync(
                        new GeriCagirmaIstek(secilen, $"{lotAdi} hatalı: {aciklama}"), kullaniciId, ct)).Select(g => g.SeriNo)];
            }
            else if (kayit is not null)
            {
                db.UrunLotSorunlari.Remove(kayit);
                await db.SaveChangesAsync(ct);
            }
            return new UrunLotSorunSonucuDto((await ListeAsync(null, lotAdi, ct)).Single(), geriCagrilan);
        }, ct);
    }
}
