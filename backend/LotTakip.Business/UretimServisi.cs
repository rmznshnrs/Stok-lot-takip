using LotTakip.DataAccess;
using LotTakip.Entity;
using LotTakip.Shared;
using Microsoft.EntityFrameworkCore;

namespace LotTakip.Business;

public interface IUretimServisi
{
    /// <summary>
    /// Kaydetmeden: her parça için hangi lottan kaç adet düşüleceği (FIFO), eksikler, atlanacak
    /// geri çağrılmış lotlar ve verilecek seri numaraları.
    /// </summary>
    Task<UretimOnizlemeDto> OnizleAsync(int urunId, int adet, CancellationToken ct = default);

    /// <summary>
    /// N adet üretir: N Uretim kaydı, ardışık seri no, FIFO lot düşümü, tek transaction.
    /// Geri çağrılan lotlar kullanılmaz. Stok yetmezse hiçbir şey kaydedilmez (YetersizStokHatasi).
    /// Kullanılan lotlar transaction boyunca kilitlenir; aynı anda yapılan üretimler sırayla işler.
    /// </summary>
    Task<UretimSonucuDto> UretAsync(int urunId, int adet, int kullaniciId, DateTimeOffset? tarih = null,
                                    CancellationToken ct = default);

    /// <summary>Şimdi üretilirse verilecek ardışık seri numaraları.</summary>
    Task<List<string>> SonrakiSeriNolarAsync(int urunId, int adet, CancellationToken ct = default);

    /// <summary>
    /// Üretim geçmişi: her seri no bir satır (yeniden eskiye), satış durumu ve geri çağrılan lotlarıyla.
    /// Arama seri no, ürün adı veya müşteri adında.
    /// </summary>
    Task<List<UretimGecmisiDto>> UretimGecmisiAsync(string? arama = null, int? urunId = null, int adet = 500,
                                                    CancellationToken ct = default);

    /// <summary>
    /// Admin: depodaki seri numaralarını hurdaya / kayba ayırır (açıklama zorunlu). Bulunamayan, satılmış
    /// veya zaten ayrılmış seri no varsa hiçbiri ayrılmaz. Ayrılan ürün satılamaz.
    /// </summary>
    Task<UretimHurdaSonucuDto> HurdayaAyirAsync(UretimHurdaIstek istek, int kullaniciId, CancellationToken ct = default);
}

public class UretimServisi(AppDbContext db, IStokLotuRepository lotlar, IUretimRepository uretimler) : IUretimServisi
{
    public const int EnFazlaAdet = 999;

    private sealed record Plan(Urun Urun, int Adet, List<(UrunAgaci Satir, List<StokLotu> Lotlar, ParcaIhtiyaciDto Ihtiyac)> Parcalar);

    public async Task<List<UretimGecmisiDto>> UretimGecmisiAsync(string? arama = null, int? urunId = null,
                                                                 int adet = 500, CancellationToken ct = default)
    {
        var q = db.Uretimler.AsNoTracking();
        if (urunId is { } id)
            q = q.Where(u => u.UrunId == id);
        arama = arama?.Trim();
        if (!string.IsNullOrEmpty(arama))
            q = q.Where(u => EF.Functions.Like(u.SeriNo, Arama.Desen(arama)) || EF.Functions.Like(u.Urun!.Ad, Arama.Desen(arama))
                             || (u.SatisKalemi != null && EF.Functions.Like(u.SatisKalemi.Satis!.Musteri!.Ad, Arama.Desen(arama))));
        return await q.OrderByDescending(u => u.Tarih).ThenByDescending(u => u.Id).Take(adet)
            .Select(u => new UretimGecmisiDto(
                u.Id, u.SeriNo, u.UrunId, u.Urun!.Ad, u.Tarih,
                u.SatisKalemi == null ? null : u.SatisKalemi.Satis!.Musteri!.Ad,
                u.SatisKalemi == null ? null : u.SatisKalemi.Satis!.Tarih,
                u.Tuketimler.Where(t => t.StokLotu!.Sorunlu).Select(t => t.StokLotu!.LotNo).Distinct().OrderBy(l => l).ToList(),
                u.Hurda == null ? null : new HurdaBilgisiDto(u.Hurda.Tur.ToString(), u.Hurda.Aciklama, u.Hurda.OlusturmaZamani,
                    u.Hurda.OlusturanKullanici!.Ad + " " + u.Hurda.OlusturanKullanici.Soyad),
                u.Hurda == null && u.GeriCagirmalar.Any(g => g.KapanmaZamani == null) ? "acik" : null))
            .ToListAsync(ct);
    }

    public Task<UretimHurdaSonucuDto> HurdayaAyirAsync(UretimHurdaIstek istek, int kullaniciId, CancellationToken ct = default)
    {
        var aciklama = (istek.Aciklama ?? "").Trim();
        if (aciklama.Length == 0)
            throw new IsKuraliHatasi("Hurdaya ayırmada açıklama zorunludur.");
        if (!Enum.TryParse<UretimHurdaTuru>(istek.Tur, ignoreCase: true, out var tur) || !Enum.IsDefined(tur))
            throw new IsKuraliHatasi("Tür Hurda veya Kayip olmalı.");
        var seriler = (istek.SeriNolar ?? []).Select(s => (s ?? "").Trim()).Where(s => s.Length > 0)
            .Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        if (seriler.Count == 0)
            throw new IsKuraliHatasi("En az bir seri no seçilmeli.");

        return Islem.CalistirAsync(db, async () =>
        {
            var admin = await Kullanicilar.AdminGetirAsync(db, kullaniciId, "Hurdaya ayırma", ct);
            var kayitlar = await db.Uretimler.Include(u => u.SatisKalemi).Include(u => u.Hurda)
                .Where(u => seriler.Contains(u.SeriNo)).ToListAsync(ct);
            var bulunamayan = seriler
                .Where(s => !kayitlar.Any(u => string.Equals(u.SeriNo, s, StringComparison.OrdinalIgnoreCase))).ToList();
            if (bulunamayan.Count > 0)
                throw new IsKuraliHatasi($"Bulunamayan seri no: {string.Join(", ", bulunamayan)}");
            var satilmis = kayitlar.Where(u => u.SatisKalemi is not null).Select(u => u.SeriNo).Order().ToList();
            if (satilmis.Count > 0)
                throw new IsKuraliHatasi($"Satılmış ürün hurdaya ayrılamaz: {string.Join(", ", satilmis)}");
            var ayrilmis = kayitlar.Where(u => u.Hurda is not null).Select(u => u.SeriNo).Order().ToList();
            if (ayrilmis.Count > 0)
                throw new IsKuraliHatasi($"Zaten ayrılmış: {string.Join(", ", ayrilmis)}");

            var zaman = Zaman.Simdi();
            foreach (var u in kayitlar)
                db.UretimHurdalari.Add(new UretimHurda
                {
                    Uretim = u, Tur = tur, Aciklama = aciklama, OlusturanKullanici = admin, OlusturmaZamani = zaman,
                });
            await db.SaveChangesAsync(ct);
            return new UretimHurdaSonucuDto([.. kayitlar.Select(u => u.SeriNo).Order()], tur.ToString(), aciklama);
        }, ct);
    }

    public async Task<UretimOnizlemeDto> OnizleAsync(int urunId, int adet, CancellationToken ct = default)
    {
        var urun = await UrunGetirAsync(urunId, adet, ct);
        var parcaIdleri = urun.Agac.Select(a => a.ParcaId).ToList();
        var kullanilabilir = await db.StokLotlari.AsNoTracking()
            .Where(l => parcaIdleri.Contains(l.ParcaId) && !l.GeriCagrildi && l.KalanAdet > 0)
            .OrderBy(l => l.SiparisTarihi).ThenBy(l => l.Id).ToListAsync(ct);
        var plan = await PlanlaAsync(urun, adet, kullanilabilir, ct);
        return new UretimOnizlemeDto(urun.Id, urun.Kod, urun.Ad, adet,
            [.. plan.Parcalar.Select(p => p.Ihtiyac)], await SonrakiSeriNolarAsync(urun, adet, ct));
    }

    public Task<UretimSonucuDto> UretAsync(int urunId, int adet, int kullaniciId, DateTimeOffset? tarih = null,
                                           CancellationToken ct = default) =>
        Islem.CalistirAsync(db, async () =>
        {
            var kullanici = await Kullanicilar.GetirAsync(db, kullaniciId, ct);
            var urun = await UrunGetirAsync(urunId, adet, ct);
            // Lotlar kilitlenerek okunur: aynı anda başka bir üretim bu transaction bitene kadar bekler
            var kilitli = await lotlar.KullanilabilirLotlariKilitleAsync([.. urun.Agac.Select(a => a.ParcaId)], ct);
            var plan = await PlanlaAsync(urun, adet, kilitli, ct, atlananlariHesapla: false);

            var eksikler = plan.Parcalar.Where(p => p.Ihtiyac.Eksik > 0)
                .Select(p => new EksikParcaDto(p.Ihtiyac.Parca, p.Ihtiyac.Gereken, p.Ihtiyac.Kullanilabilir, p.Ihtiyac.Eksik))
                .ToList();
            if (eksikler.Count > 0)
                throw new YetersizStokHatasi(eksikler);

            var an = tarih ?? Zaman.Simdi();
            var seriNolar = await SonrakiSeriNolarAsync(urun, adet, ct);
            // Her parça için FIFO kuyruğu; ürünler sırayla bu kuyruktan tüketir
            var kuyruklar = plan.Parcalar.ToDictionary(p => p.Satir.ParcaId, p => new Queue<StokLotu>(p.Lotlar));

            foreach (var seriNo in seriNolar)
            {
                var uretim = new Uretim
                {
                    Urun = urun, SeriNo = seriNo, Tarih = an, OlusturanKullanici = kullanici, OlusturmaZamani = Zaman.Simdi(),
                };
                foreach (var (satir, _, _) in plan.Parcalar)
                {
                    var kuyruk = kuyruklar[satir.ParcaId];
                    var gereken = satir.Adet;
                    while (gereken > 0)
                    {
                        var lot = kuyruk.Peek();
                        var dusulen = Math.Min(gereken, lot.KalanAdet);
                        lot.KalanAdet -= dusulen;
                        gereken -= dusulen;
                        uretim.Tuketimler.Add(new UretimTuketim { Uretim = uretim, StokLotu = lot, Adet = dusulen });
                        if (lot.KalanAdet == 0)
                            kuyruk.Dequeue();
                    }
                }
                db.Uretimler.Add(uretim);
            }

            await db.SaveChangesAsync(ct);
            return new UretimSonucuDto(urun.Id, urun.Ad, seriNolar);
        }, ct);

    public async Task<List<string>> SonrakiSeriNolarAsync(int urunId, int adet, CancellationToken ct = default)
    {
        var urun = await db.Urunler.AsNoTracking().SingleOrDefaultAsync(u => u.Id == urunId, ct)
                   ?? throw new BulunamadiHatasi("Ürün bulunamadı.");
        return await SonrakiSeriNolarAsync(urun, adet, ct);
    }

    // --- iç ------------------------------------------------------------------

    private async Task<Urun> UrunGetirAsync(int urunId, int adet, CancellationToken ct)
    {
        if (adet < 1)
            throw new IsKuraliHatasi("Üretim adedi en az 1 olmalı.");
        if (adet > EnFazlaAdet)
            throw new IsKuraliHatasi($"Tek seferde en fazla {EnFazlaAdet} adet üretilebilir.");
        var urun = await db.Urunler.Include(u => u.Agac).ThenInclude(a => a.Parca)
                       .SingleOrDefaultAsync(u => u.Id == urunId, ct)
                   ?? throw new BulunamadiHatasi("Ürün bulunamadı.");
        if (urun.Agac.Count == 0)
            throw new IsKuraliHatasi($"{urun.Kod} ürününün ürün ağacı boş.");
        return urun;
    }

    /// <summary>FIFO dağılımı: verilen lotlar (geri çağrılmamış, kalanı olan, FIFO sıralı) üzerinden.</summary>
    private async Task<Plan> PlanlaAsync(Urun urun, int adet, List<StokLotu> kullanilabilir, CancellationToken ct,
                                         bool atlananlariHesapla = true)
    {
        var satirlar = urun.Agac.OrderBy(a => a.Parca!.Kod).ToList();
        Dictionary<int, List<string>> atlanan = [];
        if (atlananlariHesapla)
        {
            var parcaIdleri = satirlar.Select(a => a.ParcaId).ToList();
            atlanan = (await db.StokLotlari.AsNoTracking()
                    .Where(l => parcaIdleri.Contains(l.ParcaId) && l.GeriCagrildi && l.KalanAdet > 0)
                    .OrderBy(l => l.SiparisTarihi).ThenBy(l => l.Id)
                    .Select(l => new { l.ParcaId, l.LotNo }).ToListAsync(ct))
                .GroupBy(l => l.ParcaId).ToDictionary(g => g.Key, g => g.Select(l => l.LotNo).ToList());
        }

        var parcalar = new List<(UrunAgaci, List<StokLotu>, ParcaIhtiyaciDto)>();
        foreach (var satir in satirlar)
        {
            var lotlarim = kullanilabilir.Where(l => l.ParcaId == satir.ParcaId).ToList();
            var gereken = satir.Adet * adet;
            var kalan = gereken;
            var dusumler = new List<LotDusumuDto>();
            foreach (var lot in lotlarim)
            {
                if (kalan == 0)
                    break;
                var dusulen = Math.Min(kalan, lot.KalanAdet);
                dusumler.Add(new LotDusumuDto(lot.Id, lot.LotNo, dusulen, lot.Sorunlu));
                kalan -= dusulen;
            }
            parcalar.Add((satir, lotlarim, new ParcaIhtiyaciDto(
                Donusum.Ozet(satir.Parca!), satir.Adet, gereken, lotlarim.Sum(l => l.KalanAdet), dusumler,
                atlanan.GetValueOrDefault(satir.ParcaId) ?? [])));
        }
        return new Plan(urun, adet, parcalar);
    }

    private async Task<List<string>> SonrakiSeriNolarAsync(Urun urun, int adet, CancellationToken ct)
    {
        // Önekten sonrası yalnız rakam olan seri no'ların en büyüğü + 1 (silinen/atlanan numara tekrar verilmez)
        var mevcut = await uretimler.OnekliSeriNolarAsync(urun.SeriOneki, ct);
        var enBuyuk = mevcut
            .Select(s => s[urun.SeriOneki.Length..])
            .Where(son => son.Length > 0 && son.All(char.IsAsciiDigit))
            .Select(son => int.TryParse(son, out var n) ? n : 0)
            .DefaultIfEmpty(0).Max();
        return [.. Enumerable.Range(enBuyuk + 1, adet).Select(n => $"{urun.SeriOneki}{n:D4}")];
    }
}
