using Microsoft.EntityFrameworkCore;

namespace BusinessLayer.Concrete;

public class SiparisServisi(AppDbContext db) : ISiparisServisi
{
    public const int EnFazlaAdet = 999;
    private const string NoOneki = "SP-";

    public async Task<List<SiparisDto>> ListeAsync(int? musteriId = null, bool acik = false, CancellationToken ct = default)
    {
        var q = Sorgu();
        if (musteriId is { } m)
            q = q.Where(s => s.MusteriId == m);
        if (acik)
            q = q.Where(s => s.IptalZamani == null
                             && s.Kalemler.Any(k => k.Ayrilanlar.Count(u => u.SatisKalemi != null) < k.Adet));
        var liste = await q.OrderByDescending(s => s.Tarih).ThenByDescending(s => s.Id).Take(500).ToListAsync(ct);
        return [.. liste.Select(Donustur)];
    }

    public async Task<SiparisDto> GetirAsync(int id, CancellationToken ct = default) =>
        Donustur(await Sorgu().SingleOrDefaultAsync(s => s.Id == id, ct) ?? throw new BulunamadiHatasi("Sipariş bulunamadı."));

    public Task<SiparisDto> OlusturAsync(SiparisIstek istek, int kullaniciId, CancellationToken ct = default)
    {
        var kalemler = istek.Kalemler ?? [];
        if (kalemler.Count == 0)
            throw new IsKuraliHatasi("En az bir ürün ekleyin.");
        if (kalemler.Any(k => k.Adet < 1 || k.Adet > EnFazlaAdet))
            throw new IsKuraliHatasi($"Adet 1 ile {EnFazlaAdet} arasında olmalı.");
        if (kalemler.GroupBy(k => k.UrunId).Any(g => g.Count() > 1))
            throw new IsKuraliHatasi("Aynı ürün siparişte iki kez yazılamaz.");

        return Islem.CalistirAsync(db, async () =>
        {
            var kullanici = await Kullanicilar.GetirAsync(db, kullaniciId, ct);
            var musteri = await db.Musteriler.SingleOrDefaultAsync(m => m.Id == istek.MusteriId, ct)
                          ?? throw new BulunamadiHatasi("Müşteri bulunamadı.");
            var urunIdleri = kalemler.Select(k => k.UrunId).ToList();
            var urunler = await db.Urunler.Where(u => urunIdleri.Contains(u.Id)).ToListAsync(ct);
            if (urunler.Count != urunIdleri.Count)
                throw new BulunamadiHatasi("Ürün bulunamadı.");

            var mevcut = await db.Siparisler.Where(s => s.No.StartsWith(NoOneki)).Select(s => s.No).ToListAsync(ct);
            var siparis = new Siparis
            {
                No = $"{NoOneki}{Sira.EnBuyuk(mevcut, NoOneki) + 1:D4}", Musteri = musteri,
                Tarih = istek.Tarih ?? Zaman.Bugun(), IstenenTarih = istek.IstenenTarih,
                Aciklama = (istek.Aciklama ?? "").Trim(), OlusturanKullanici = kullanici, OlusturmaZamani = Zaman.Simdi(),
            };
            foreach (var k in kalemler)
            {
                var kalem = new SiparisKalemi
                {
                    Siparis = siparis, UrunId = k.UrunId, Urun = urunler.First(u => u.Id == k.UrunId), Adet = k.Adet,
                };
                siparis.Kalemler.Add(kalem);
                await AyirAsync(kalem, k.SeriNolar, ct);
            }
            db.Siparisler.Add(siparis);
            await db.SaveChangesAsync(ct);
            return await GetirAsync(siparis.Id, ct);
        }, ct);
    }

    public Task<SiparisDto> SeriAyirAsync(int kalemId, SeriAyirIstek istek, CancellationToken ct = default) =>
        Islem.CalistirAsync(db, async () =>
        {
            var kalem = await KalemGetirAsync(kalemId, ct);
            if (SeriListesi.Temizle(istek.SeriNolar).Count == 0)
                throw new IsKuraliHatasi("Seri no seçin.");
            await AyirAsync(kalem, istek.SeriNolar, ct);
            await db.SaveChangesAsync(ct);
            return await GetirAsync(kalem.SiparisId, ct);
        }, ct);

    public Task<SiparisDto> SeriBirakAsync(int kalemId, string seriNo, CancellationToken ct = default) =>
        Islem.CalistirAsync(db, async () =>
        {
            var kalem = await KalemGetirAsync(kalemId, ct);
            var u = kalem.Ayrilanlar.SingleOrDefault(x => string.Equals(x.SeriNo, (seriNo ?? "").Trim(), StringComparison.OrdinalIgnoreCase))
                    ?? throw new BulunamadiHatasi($"{seriNo} bu kaleme ayrılmamış.");
            if (u.SatisKalemi is not null)
                throw new IsKuraliHatasi($"{u.SeriNo} gönderilmiş; ayrımı kaldırılamaz.");
            u.SiparisKalemiId = null;
            await db.SaveChangesAsync(ct);
            return await GetirAsync(kalem.SiparisId, ct);
        }, ct);

    public Task<SiparisDto> IptalAsync(int id, int kullaniciId, CancellationToken ct = default) =>
        Islem.CalistirAsync(db, async () =>
        {
            await Kullanicilar.GetirAsync(db, kullaniciId, ct);
            var siparis = await db.Siparisler
                              .Include(s => s.Kalemler).ThenInclude(k => k.Ayrilanlar).ThenInclude(u => u.SatisKalemi)
                              .Include(s => s.Kalemler).ThenInclude(k => k.UretimEmirleri)
                              .AsSplitQuery().SingleOrDefaultAsync(s => s.Id == id, ct)
                          ?? throw new BulunamadiHatasi("Sipariş bulunamadı.");
            if (siparis.IptalZamani is not null)
                throw new IsKuraliHatasi("Sipariş zaten iptal edilmiş.");
            foreach (var kalem in siparis.Kalemler)
            {
                foreach (var u in kalem.Ayrilanlar.Where(u => u.SatisKalemi is null))
                    u.SiparisKalemiId = null;
                foreach (var e in kalem.UretimEmirleri.Where(e => e.Durum == UretimEmriDurumu.Bekliyor))
                    UretimSirasiServisi.Kapat(e);
            }
            siparis.IptalZamani = Zaman.Simdi();
            await db.SaveChangesAsync(ct);
            return await GetirAsync(id, ct);
        }, ct);

    public Task<List<HazirSeriDto>> HazirSerilerAsync(int urunId, CancellationToken ct = default) =>
        Serbest(db.Uretimler.AsNoTracking().Where(u => u.UrunId == urunId))
            .OrderBy(u => u.SeriNo).Select(u => new HazirSeriDto(u.SeriNo, u.Tarih)).ToListAsync(ct);

    // --- iç ------------------------------------------------------------------

    /// <summary>Depoda serbest: satılmamış, hurda değil, açık geri çağırması yok, siparişe ayrılmamış.</summary>
    private static IQueryable<Uretim> Serbest(IQueryable<Uretim> q) =>
        q.Where(u => u.SatisKalemi == null && u.Hurda == null && u.SiparisKalemiId == null
                     && !u.GeriCagirmalar.Any(g => g.KapanmaZamani == null));

    private async Task<SiparisKalemi> KalemGetirAsync(int kalemId, CancellationToken ct)
    {
        var kalem = await db.SiparisKalemleri.Include(k => k.Siparis).Include(k => k.Ayrilanlar).ThenInclude(u => u.SatisKalemi)
                        .SingleOrDefaultAsync(k => k.Id == kalemId, ct)
                    ?? throw new BulunamadiHatasi("Sipariş kalemi bulunamadı.");
        if (kalem.Siparis!.IptalZamani is not null)
            throw new IsKuraliHatasi("Sipariş iptal edilmiş.");
        return kalem;
    }

    /// <summary>Seçilen serbest seri numaralarını kaleme ayırır; biri uygun değilse hiçbiri ayrılmaz.</summary>
    private async Task AyirAsync(SiparisKalemi kalem, IReadOnlyList<string>? seriNolar, CancellationToken ct)
    {
        var seriler = SeriListesi.Temizle(seriNolar);
        if (seriler.Count == 0)
            return;
        var kayitlar = await db.Uretimler.Where(u => seriler.Contains(u.SeriNo)).ToListAsync(ct);
        var bulunamayan = seriler.Where(s => !kayitlar.Any(u => string.Equals(u.SeriNo, s, StringComparison.OrdinalIgnoreCase))).ToList();
        if (bulunamayan.Count > 0)
            throw new IsKuraliHatasi($"Bulunamayan seri no: {string.Join(", ", bulunamayan)}");
        var baskaUrun = kayitlar.Where(u => u.UrunId != kalem.UrunId).Select(u => u.SeriNo).Order().ToList();
        if (baskaUrun.Count > 0)
            throw new IsKuraliHatasi($"Bu ürüne ait değil: {string.Join(", ", baskaUrun)}");
        var idler = kayitlar.Select(u => u.Id).ToList();
        var serbest = await Serbest(db.Uretimler.Where(u => idler.Contains(u.Id))).Select(u => u.Id).ToListAsync(ct);
        var uygunDegil = kayitlar.Where(u => !serbest.Contains(u.Id)).Select(u => u.SeriNo).Order().ToList();
        if (uygunDegil.Count > 0)
            throw new IsKuraliHatasi($"Depoda serbest değil (satılmış, ayrılmış, hurda ya da geri çağrılmış): {string.Join(", ", uygunDegil)}");
        if (kalem.Ayrilanlar.Count + kayitlar.Count > kalem.Adet)
            throw new IsKuraliHatasi($"Bu kaleme en fazla {kalem.Adet - kalem.Ayrilanlar.Count} ürün daha ayrılabilir.");
        foreach (var u in kayitlar)
            kalem.Ayrilanlar.Add(u);
    }

    private IQueryable<Siparis> Sorgu() => db.Siparisler.AsNoTracking()
        .Include(s => s.Musteri).Include(s => s.OlusturanKullanici)
        .Include(s => s.Kalemler).ThenInclude(k => k.Urun)
        .Include(s => s.Kalemler).ThenInclude(k => k.Ayrilanlar).ThenInclude(u => u.SatisKalemi)
        .Include(s => s.Kalemler).ThenInclude(k => k.UretimEmirleri)
        .AsSplitQuery();

    private static SiparisDto Donustur(Siparis s)
    {
        var kalemler = s.Kalemler.OrderBy(k => k.Urun!.Ad, StringComparer.CurrentCulture).Select(k => new SiparisKalemiDto(
            k.Id, k.UrunId, k.Urun!.Kod, k.Urun.Ad, k.Adet,
            [.. k.Ayrilanlar.Select(u => u.SeriNo).Order()],
            [.. k.Ayrilanlar.Where(u => u.SatisKalemi is not null).Select(u => u.SeriNo).Order()],
            k.UretimEmirleri.Where(e => e.Durum == UretimEmriDurumu.Bekliyor).Sum(e => e.Adet - e.Uretilen))).ToList();
        return new SiparisDto(s.Id, s.No, s.MusteriId, s.Musteri!.Ad, s.Tarih, s.IstenenTarih, s.Aciklama, Durum(s, kalemler),
            kalemler, s.OlusturmaZamani, $"{s.OlusturanKullanici!.Ad} {s.OlusturanKullanici.Soyad}", s.IptalZamani);
    }

    private static string Durum(Siparis s, List<SiparisKalemiDto> kalemler)
    {
        if (s.IptalZamani is not null)
            return "iptal";
        var toplam = kalemler.Sum(k => k.Adet);
        var gonderilen = kalemler.Sum(k => k.Gonderilenler.Count);
        if (gonderilen >= toplam)
            return "tamamlandi";
        if (gonderilen > 0)
            return "kismen_gonderildi";
        if (kalemler.Sum(k => k.Ayrilanlar.Count) >= toplam)
            return "hazir";
        return kalemler.Any(k => k.Uretimde > 0) ? "uretimde" : "bekliyor";
    }
}
