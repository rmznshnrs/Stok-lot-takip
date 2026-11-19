using Microsoft.EntityFrameworkCore;

namespace BusinessLayer.Concrete;

public class OzetServisi(AppDbContext db, IStokServisi stok) : IOzetServisi
{
    public const int UyariEsigi = 3;
    public const int EnFazlaGun = 62;

    public async Task<AnaSayfaOzetiDto> AnaSayfaAsync(CancellationToken ct = default)
    {
        var bugun = Zaman.Bugun();
        var acikSiparisler = await db.Siparisler.AsNoTracking()
            .Where(s => s.IptalZamani == null)
            .Select(s => new
            {
                s.Id, s.No, MusteriAd = s.Musteri!.Ad, s.IstenenTarih,
                Adet = s.Kalemler.Sum(k => k.Adet),
                Gonderilen = s.Kalemler.Sum(k => k.Ayrilanlar.Count(u => u.SatisKalemi != null)),
            })
            .Where(s => s.Gonderilen < s.Adet).ToListAsync(ct);
        var bekleyen = await db.UretimEmirleri.AsNoTracking().Where(e => e.Durum == UretimEmriDurumu.Bekliyor)
            .Select(e => new { e.PlanlananTarih, Kalan = e.Adet - e.Uretilen }).ToListAsync(ct);
        var sevkiyat = await db.Satislar.AsNoTracking().Where(s => s.Durum != SevkiyatDurumu.TeslimEdildi)
            .GroupBy(s => s.Durum).Select(g => new { g.Key, Sayi = g.Count() }).ToListAsync(ct);
        var depoda = await db.Uretimler.CountAsync(u => u.SatisKalemi == null && u.Hurda == null, ct);
        var acikGeri = await db.UrunGeriCagirmalari.CountAsync(g => g.KapanmaZamani == null && g.Uretim!.Hurda == null, ct);
        var parcalar = await stok.ParcaStoklariAsync(ct: ct);

        var yaklasan = acikSiparisler.Where(s => s.IstenenTarih is { } t && t <= bugun.AddDays(7))
            .OrderBy(s => s.IstenenTarih).ThenBy(s => s.No)
            .Select(s => new YaklasanTeslimDto(s.Id, s.No, s.MusteriAd, s.IstenenTarih!.Value, s.Adet - s.Gonderilen)).ToList();

        var kapasiteler = await KapasitelerAsync(parcalar, ct);
        return new AnaSayfaOzetiDto(
            acikSiparisler.Count, bekleyen.Count, bekleyen.Sum(e => e.Kalan),
            bekleyen.Where(e => e.PlanlananTarih <= bugun).Sum(e => e.Kalan),
            sevkiyat.FirstOrDefault(s => s.Key == SevkiyatDurumu.Hazirlaniyor)?.Sayi ?? 0,
            sevkiyat.FirstOrDefault(s => s.Key == SevkiyatDurumu.Kargoda)?.Sayi ?? 0,
            depoda, parcalar.Count(p => p.MinAltinda), acikGeri,
            [.. kapasiteler.Where(u => u.Uretilebilir <= UyariEsigi)], yaklasan,
            [.. kapasiteler.Select(u => new UrunKapasitesiDto(u.UrunId, u.UrunAd, u.Uretilebilir))
                .OrderBy(u => u.UrunAd, StringComparer.CurrentCulture)]);
    }

    public async Task<List<TakvimGunuDto>> TakvimAsync(DateOnly baslangic, DateOnly bitis, CancellationToken ct = default)
    {
        if (bitis < baslangic)
            throw new IsKuraliHatasi("Bitiş başlangıçtan önce olamaz.");
        if (bitis.DayNumber - baslangic.DayNumber >= EnFazlaGun)
            throw new IsKuraliHatasi($"Takvim en çok {EnFazlaGun} gün gösterir.");

        var planlanan = await db.UretimEmirleri.AsNoTracking()
            .Where(e => e.Durum == UretimEmriDurumu.Bekliyor && e.PlanlananTarih >= baslangic && e.PlanlananTarih <= bitis)
            .Select(e => new { Tarih = e.PlanlananTarih, e.UrunId, Adet = e.Adet - e.Uretilen }).ToListAsync(ct);
        var ilk = new DateTimeOffset(baslangic.AddDays(-1).ToDateTime(TimeOnly.MinValue), TimeSpan.Zero);
        var son = new DateTimeOffset(bitis.AddDays(2).ToDateTime(TimeOnly.MinValue), TimeSpan.Zero);
        var uretilen = (await db.Uretimler.AsNoTracking().Where(u => u.Tarih >= ilk && u.Tarih < son)
                .Select(u => new { u.Tarih, u.UrunId }).ToListAsync(ct))
            .Select(u => new { Tarih = Zaman.YerelTarih(u.Tarih), u.UrunId })
            .Where(u => u.Tarih >= baslangic && u.Tarih <= bitis).ToList();
        var istenen = await db.SiparisKalemleri.AsNoTracking()
            .Where(k => k.Siparis!.IptalZamani == null)
            .Select(k => new { Tarih = k.Siparis!.IstenenTarih ?? k.Siparis.Tarih, k.UrunId, k.Adet })
            .Where(k => k.Tarih >= baslangic && k.Tarih <= bitis).ToListAsync(ct);

        var planlar = await PlanSorgu().Where(p => p.Tarih >= baslangic && p.Tarih <= bitis)
            .OrderBy(p => p.Tamamlandi).ThenBy(p => p.Id).ToListAsync(ct);

        var urunAdlari = await db.Urunler.AsNoTracking().ToDictionaryAsync(u => u.Id, u => u.Ad, ct);
        var satirlar = planlanan.Select(p => (p.Tarih, p.UrunId, Plan: p.Adet, Uret: 0, Ist: 0))
            .Concat(uretilen.Select(u => (u.Tarih, u.UrunId, Plan: 0, Uret: 1, Ist: 0)))
            .Concat(istenen.Select(i => (i.Tarih, i.UrunId, Plan: 0, Uret: 0, Ist: i.Adet)))
            .ToList();
        var gunler = satirlar.Select(s => s.Tarih).Concat(planlar.Select(p => p.Tarih)).Distinct().Order();
        return [.. gunler.Select(gun => new TakvimGunuDto(gun,
            [.. satirlar.Where(s => s.Tarih == gun).GroupBy(s => s.UrunId)
                .Select(u => new TakvimUrunuDto(u.Key, urunAdlari.GetValueOrDefault(u.Key, "?"),
                    u.Sum(x => x.Plan), u.Sum(x => x.Uret), u.Sum(x => x.Ist)))
                .OrderBy(u => u.UrunAd, StringComparer.CurrentCulture)],
            [.. planlar.Where(p => p.Tarih == gun).Select(Donustur)]))];
    }

    public Task<TakvimPlaniDto> PlanEkleAsync(TakvimPlaniIstek istek, int kullaniciId, CancellationToken ct = default) =>
        Islem.CalistirAsync(db, async () =>
        {
            var kullanici = await Kullanicilar.GetirAsync(db, kullaniciId, ct);
            var aciklama = (istek.Aciklama ?? "").Trim();
            if (istek.ParcaId is null && aciklama.Length == 0)
                throw new IsKuraliHatasi("Parça seçin ya da not yazın.");
            if (aciklama.Length > 500)
                throw new IsKuraliHatasi("Not en fazla 500 karakter olabilir.");
            if (istek.Adet is < 1 or > 1_000_000)
                throw new IsKuraliHatasi("Adet en az 1 olmalı.");
            if (istek.ParcaId is { } p && !await db.Parcalar.AnyAsync(x => x.Id == p, ct))
                throw new BulunamadiHatasi("Parça bulunamadı.");
            if (istek.TedarikciId is { } t && !await db.Tedarikciler.AnyAsync(x => x.Id == t, ct))
                throw new BulunamadiHatasi("Tedarikçi bulunamadı.");
            var plan = new TakvimPlani
            {
                Tarih = istek.Tarih, ParcaId = istek.ParcaId, Adet = istek.ParcaId is null ? null : istek.Adet,
                TedarikciId = istek.TedarikciId, Aciklama = aciklama, OlusturanKullanici = kullanici, OlusturmaZamani = Zaman.Simdi(),
            };
            db.TakvimPlanlari.Add(plan);
            await db.SaveChangesAsync(ct);
            return Donustur(await PlanSorgu().SingleAsync(x => x.Id == plan.Id, ct));
        }, ct);

    public Task<TakvimPlaniDto> PlanTamamAsync(int id, bool tamamlandi, CancellationToken ct = default) =>
        Islem.CalistirAsync(db, async () =>
        {
            var plan = await db.TakvimPlanlari.SingleOrDefaultAsync(p => p.Id == id, ct) ?? throw new BulunamadiHatasi("Plan bulunamadı.");
            plan.Tamamlandi = tamamlandi;
            await db.SaveChangesAsync(ct);
            return Donustur(await PlanSorgu().SingleAsync(x => x.Id == id, ct));
        }, ct);

    public Task PlanSilAsync(int id, CancellationToken ct = default) =>
        Islem.CalistirAsync(db, async () =>
        {
            var plan = await db.TakvimPlanlari.SingleOrDefaultAsync(p => p.Id == id, ct) ?? throw new BulunamadiHatasi("Plan bulunamadı.");
            db.TakvimPlanlari.Remove(plan);
            await db.SaveChangesAsync(ct);
            return true;
        }, ct);

    private IQueryable<TakvimPlani> PlanSorgu() => db.TakvimPlanlari.AsNoTracking()
        .Include(p => p.Parca).Include(p => p.Tedarikci).Include(p => p.OlusturanKullanici);

    private static TakvimPlaniDto Donustur(TakvimPlani p) => new(
        p.Id, p.Tarih, p.Parca is null ? null : Donusum.Ozet(p.Parca), p.Adet, p.TedarikciId, p.Tedarikci?.Ad,
        p.Aciklama, p.Tamamlandi, $"{p.OlusturanKullanici!.Ad} {p.OlusturanKullanici.Soyad}");

    private async Task<List<UretilebilirUyariDto>> KapasitelerAsync(List<ParcaStokDto> parcalar, CancellationToken ct)
    {
        var serbest = parcalar.ToDictionary(p => p.Id);
        var urunler = await db.Urunler.AsNoTracking().Include(u => u.Agac).ThenInclude(a => a.Parca)
            .Where(u => u.Agac.Any()).ToListAsync(ct);
        return [.. urunler
            .Select(u =>
            {
                var satirlar = u.Agac.Select(a => (Agac: a, Serbest: serbest.TryGetValue(a.ParcaId, out var p) ? p.Serbest : 0))
                    .Select(x => (x.Agac, x.Serbest, Adet: x.Serbest / x.Agac.Adet)).ToList();
                var enAz = satirlar.Min(x => x.Adet);
                return new UretilebilirUyariDto(u.Id, u.Kod, u.Ad, enAz,
                    [.. satirlar.Where(x => x.Adet <= UyariEsigi).OrderBy(x => x.Adet).ThenBy(x => x.Agac.Parca!.Kod)
                        .Select(x => new UretilebilirParcaDto(Donusum.Ozet(x.Agac.Parca!), x.Agac.Adet, x.Serbest))]);
            })
            .OrderBy(u => u.Uretilebilir).ThenBy(u => u.UrunAd, StringComparer.CurrentCulture)];
    }
}
