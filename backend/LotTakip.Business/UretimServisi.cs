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
}

public class UretimServisi(AppDbContext db, IStokLotuRepository lotlar, IUretimRepository uretimler) : IUretimServisi
{
    public const int EnFazlaAdet = 999;

    private sealed record Plan(Urun Urun, int Adet, List<(UrunAgaci Satir, List<StokLotu> Lotlar, ParcaIhtiyaciDto Ihtiyac)> Parcalar);

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
                dusumler.Add(new LotDusumuDto(lot.Id, lot.LotNo, dusulen));
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
