using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;

namespace BusinessLayer.Concrete;

public class UretimSirasiServisi(AppDbContext db, IStokLotuRepository lotlar, IUretimRepository uretimler) : IUretimSirasiServisi
{
    private readonly UretimServisi uretim = new(db, lotlar, uretimler);

    private static readonly Expression<Func<UretimEmri, UretimEmriDto>> Donustur = e => new UretimEmriDto(
        e.Id, e.Sira, e.UrunId, e.Urun!.Kod, e.Urun.Ad, e.Adet, e.Uretilen, e.Durum.ToString(), e.PlanlananTarih, e.Aciklama,
        e.SiparisKalemi == null ? null : e.SiparisKalemi.SiparisId,
        e.SiparisKalemi == null ? null : e.SiparisKalemi.Siparis!.Tarih,
        e.SiparisKalemi == null ? null : e.SiparisKalemi.Siparis!.Musteri!.Ad,
        e.OlusturmaZamani, e.OlusturanKullanici!.Ad + " " + e.OlusturanKullanici.Soyad, e.BaslamaZamani, e.BitisZamani,
        e.Parcalar.OrderBy(p => p.Parca!.Kod).Select(p => new EmirParcasiDto(
            new ParcaOzetDto(p.Parca!.Id, p.Parca.Kod, p.Parca.Ad), p.BirimAdet,
            e.Durum == UretimEmriDurumu.Bekliyor ? p.BirimAdet * (e.Adet - e.Uretilen) : 0)).ToList(),
        e.Uretimler.OrderBy(u => u.SeriNo).Select(u => u.SeriNo).ToList());

    public async Task<List<UretimEmriDto>> ListeAsync(bool bitenler = false, CancellationToken ct = default)
    {
        var q = db.UretimEmirleri.AsNoTracking();
        q = bitenler
            ? q.Where(e => e.Durum != UretimEmriDurumu.Bekliyor).OrderByDescending(e => e.BitisZamani).ThenByDescending(e => e.Id).Take(200)
            : q.Where(e => e.Durum == UretimEmriDurumu.Bekliyor).OrderBy(e => e.Sira).ThenBy(e => e.Id);
        return await q.Select(Donustur).ToListAsync(ct);
    }

    public async Task<UretimEmriDto> GetirAsync(int id, CancellationToken ct = default) =>
        await db.UretimEmirleri.AsNoTracking().Where(e => e.Id == id).Select(Donustur).SingleOrDefaultAsync(ct)
        ?? throw new BulunamadiHatasi("Üretim işi bulunamadı.");

    public async Task<List<string>> EksikleriSirayaAlAsync(int kullaniciId, CancellationToken ct = default)
    {
        var bugun = Zaman.Bugun();
        var kalemler = await db.SiparisKalemleri.AsNoTracking()
            .Where(k => k.Siparis!.IptalZamani == null)
            .Select(k => new
            {
                k.Id, k.UrunId, UrunAd = k.Urun!.Ad, MusteriAd = k.Siparis!.Musteri!.Ad, k.Siparis.IstenenTarih, k.SiparisId,
                Eksik = k.Adet - k.Ayrilanlar.Count
                        - k.UretimEmirleri.Where(e => e.Durum == UretimEmriDurumu.Bekliyor).Sum(e => e.Adet - e.Uretilen),
            })
            .Where(k => k.Eksik > 0)
            .ToListAsync(ct);
        var girenler = new List<string>();
        foreach (var k in kalemler.OrderBy(k => k.IstenenTarih ?? DateOnly.MaxValue).ThenBy(k => k.SiparisId))
        {
            var gun = k.IstenenTarih is { } t && t > bugun ? t : bugun;
            for (var adet = k.Eksik; adet >= 1; adet--)
            {
                try
                {
                    await EkleAsync(new UretimEmriIstek(k.UrunId, adet, k.Id, gun), kullaniciId, ct);
                    girenler.Add($"{k.MusteriAd} · {k.UrunAd} × {adet}");
                    break;
                }
                catch (YetersizStokHatasi)
                {
                }
            }
        }
        return girenler;
    }

    public Task<UretimEmriDto> EkleAsync(UretimEmriIstek istek, int kullaniciId, CancellationToken ct = default) =>
        Islem.CalistirAsync(db, async () =>
        {
            var kullanici = await Kullanicilar.GetirAsync(db, kullaniciId, ct);
            SiparisKalemi? kalem = null;
            int adet;
            if (istek.SiparisKalemiId is { } kalemId)
            {
                kalem = await db.SiparisKalemleri.Include(k => k.Siparis).Include(k => k.Ayrilanlar).Include(k => k.UretimEmirleri)
                            .SingleOrDefaultAsync(k => k.Id == kalemId, ct)
                        ?? throw new BulunamadiHatasi("Talep kalemi bulunamadı.");
                if (kalem.Siparis!.IptalZamani is not null)
                    throw new IsKuraliHatasi("Talep iptal edilmiş.");
                if (kalem.UrunId != istek.UrunId)
                    throw new IsKuraliHatasi("Ürün talep kalemiyle uyuşmuyor.");
                var eksik = kalem.Adet - kalem.Ayrilanlar.Count
                            - kalem.UretimEmirleri.Where(e => e.Durum == UretimEmriDurumu.Bekliyor).Sum(e => e.Adet - e.Uretilen);
                if (eksik <= 0)
                    throw new IsKuraliHatasi("Bu talep kalemi için üretilecek ürün kalmadı.");
                adet = istek.Adet ?? eksik;
                if (adet > eksik)
                    throw new IsKuraliHatasi($"Bu talep kalemi için en fazla {eksik} adet sıraya eklenebilir.");
            }
            else
            {
                adet = istek.Adet ?? throw new IsKuraliHatasi("Adet girin.");
            }

            var urun = await uretim.UrunGetirAsync(istek.UrunId, adet, ct);
            var parcaIdleri = urun.Agac.Select(a => a.ParcaId).ToList();
            var kilitli = await lotlar.KullanilabilirLotlariKilitleAsync(parcaIdleri, ct);
            var ayrilan = await Ayrilan.ParcalarAsync(db, parcaIdleri, null, ct);
            var eksikler = urun.Agac.OrderBy(a => a.Parca!.Kod, DogalSira.Ornek)
                .Select(a =>
                {
                    var gereken = a.Adet * adet;
                    var kullanilabilir = Math.Max(0, kilitli.Where(l => l.ParcaId == a.ParcaId).Sum(l => l.KalanAdet)
                                                     - ayrilan.GetValueOrDefault(a.ParcaId));
                    return new EksikParcaDto(Donusum.Ozet(a.Parca!), gereken, kullanilabilir, Math.Max(0, gereken - kullanilabilir));
                })
                .Where(e => e.Eksik > 0).ToList();
            if (eksikler.Count > 0)
                throw new YetersizStokHatasi(eksikler);

            var sira = (await db.UretimEmirleri.Where(e => e.Durum == UretimEmriDurumu.Bekliyor)
                .MaxAsync(e => (int?)e.Sira, ct) ?? 0) + 1;
            var emir = new UretimEmri
            {
                Urun = urun, Adet = adet, Sira = sira, PlanlananTarih = istek.PlanlananTarih ?? Zaman.Bugun(),
                Durum = UretimEmriDurumu.Bekliyor, Aciklama = (istek.Aciklama ?? "").Trim(), SiparisKalemi = kalem,
                OlusturanKullanici = kullanici, OlusturmaZamani = Zaman.Simdi(),
                Parcalar = [.. urun.Agac.Select(a => new UretimEmriParcasi { ParcaId = a.ParcaId, BirimAdet = a.Adet })],
            };
            db.UretimEmirleri.Add(emir);
            await db.SaveChangesAsync(ct);
            return await GetirAsync(emir.Id, ct);
        }, ct);

    public Task<UretimSonucuDto> UretAsync(int id, EmirUretIstek istek, int kullaniciId, CancellationToken ct = default) =>
        Islem.CalistirAsync(db, async () =>
        {
            var kullanici = await Kullanicilar.GetirAsync(db, kullaniciId, ct);
            var emir = await BekleyenGetirAsync(id, ct);
            var kalan = emir.Adet - emir.Uretilen;
            var adet = istek.Adet ?? kalan;
            if (adet < 1 || adet > kalan)
                throw new IsKuraliHatasi($"1 ile {kalan} arasında adet girin.");
            var urun = await uretim.UrunGetirAsync(emir.UrunId, adet, ct);
            emir.BaslamaZamani ??= Zaman.Simdi();
            var sonuc = await uretim.UretIcAsync(urun, adet, kullanici, null, emir, ct, istek.SeriNolar);
            emir.Uretilen += adet;
            if (emir.Uretilen == emir.Adet)
            {
                emir.Durum = UretimEmriDurumu.Tamamlandi;
                emir.BitisZamani = Zaman.Simdi();
            }
            await db.SaveChangesAsync(ct);
            if (emir.SiparisKalemiId is not { } kalemId)
                return sonuc;
            var siparisId = await db.SiparisKalemleri.Where(k => k.Id == kalemId).Select(k => k.SiparisId).SingleAsync(ct);
            try
            {
                var sevkiyatlar = new SevkiyatServisi(db);
                var yeni = await sevkiyatlar.OlusturAsync(new SevkiyatIstek(siparisId, sonuc.SeriNolar), kullaniciId, ct);
                var sevkiyat = await sevkiyatlar.BekleyenleBirlestirAsync(yeni.Id, ct);
                return sonuc with { SevkiyatId = sevkiyat.Id, SevkiyatNo = sevkiyat.No };
            }
            catch (SatisHatasi)
            {
                return sonuc;
            }
        }, ct);

    public Task<UretimEmriDto> BaslaAsync(int id, CancellationToken ct = default, int? adet = null) =>
        Islem.CalistirAsync(db, async () =>
        {
            var emir = await BekleyenGetirAsync(id, ct);
            if (emir.BaslamaZamani is not null)
                throw new IsKuraliHatasi("Bu işin üretimine zaten başlandı.");
            var kalan = emir.Adet - emir.Uretilen;
            var baslayan = adet ?? kalan;
            if (baslayan < 1 || baslayan > kalan)
                throw new IsKuraliHatasi($"1 ile {kalan} arasında adet girin.");
            if (baslayan == kalan)
            {
                emir.BaslamaZamani = Zaman.Simdi();
                await db.SaveChangesAsync(ct);
                return await GetirAsync(id, ct);
            }
            var parcalar = await db.Set<UretimEmriParcasi>().Where(p => p.UretimEmriId == id).ToListAsync(ct);
            emir.Adet -= baslayan;
            var yeni = new UretimEmri
            {
                UrunId = emir.UrunId, Adet = baslayan, Sira = emir.Sira, PlanlananTarih = emir.PlanlananTarih,
                Durum = UretimEmriDurumu.Bekliyor, Aciklama = emir.Aciklama, SiparisKalemiId = emir.SiparisKalemiId,
                OlusturanKullaniciId = emir.OlusturanKullaniciId, OlusturmaZamani = emir.OlusturmaZamani, BaslamaZamani = Zaman.Simdi(),
                Parcalar = [.. parcalar.Select(p => new UretimEmriParcasi { ParcaId = p.ParcaId, BirimAdet = p.BirimAdet })],
            };
            db.UretimEmirleri.Add(yeni);
            await db.SaveChangesAsync(ct);
            return await GetirAsync(yeni.Id, ct);
        }, ct);

    public Task<UretimEmriDto> IptalAsync(int id, int kullaniciId, CancellationToken ct = default) =>
        Islem.CalistirAsync(db, async () =>
        {
            await Kullanicilar.GetirAsync(db, kullaniciId, ct);
            var emir = await BekleyenGetirAsync(id, ct);
            Kapat(emir);
            await db.SaveChangesAsync(ct);
            return await GetirAsync(id, ct);
        }, ct);

    public Task<List<UretimEmriDto>> TasiAsync(int id, int yon, CancellationToken ct = default) =>
        Islem.CalistirAsync(db, async () =>
        {
            if (yon is not (-1 or 1))
                throw new IsKuraliHatasi("Yön -1 ya da 1 olmalı.");
            var emir = await BekleyenGetirAsync(id, ct);
            var bekleyen = db.UretimEmirleri.Where(e => e.Durum == UretimEmriDurumu.Bekliyor);
            var komsu = await (yon < 0
                    ? bekleyen.Where(e => e.Sira < emir.Sira).OrderByDescending(e => e.Sira)
                    : bekleyen.Where(e => e.Sira > emir.Sira).OrderBy(e => e.Sira))
                .FirstOrDefaultAsync(ct);
            if (komsu is not null)
            {
                (emir.Sira, komsu.Sira) = (komsu.Sira, emir.Sira);
                await db.SaveChangesAsync(ct);
            }
            return await ListeAsync(ct: ct);
        }, ct);

    public Task<List<UretimEmriDto>> KonumaTasiAsync(int id, int hedefId, CancellationToken ct = default) =>
        Islem.CalistirAsync(db, async () =>
        {
            var emir = await BekleyenGetirAsync(id, ct);
            var hedef = await BekleyenGetirAsync(hedefId, ct);
            if (emir.Id != hedef.Id)
            {
                var sira = await db.UretimEmirleri.Where(e => e.Durum == UretimEmriDurumu.Bekliyor)
                    .OrderBy(e => e.Sira).ThenBy(e => e.Id).ToListAsync(ct);
                var asagi = sira.IndexOf(emir) < sira.IndexOf(hedef);
                sira.Remove(emir);
                sira.Insert(sira.IndexOf(hedef) + (asagi ? 1 : 0), emir);
                for (var i = 0; i < sira.Count; i++)
                    sira[i].Sira = i + 1;
                await db.SaveChangesAsync(ct);
            }
            return await ListeAsync(ct: ct);
        }, ct);

    public Task<UretimEmriDto> PlanlaAsync(int id, DateOnly tarih, CancellationToken ct = default) =>
        Islem.CalistirAsync(db, async () =>
        {
            var emir = await BekleyenGetirAsync(id, ct);
            emir.PlanlananTarih = tarih;
            await db.SaveChangesAsync(ct);
            return await GetirAsync(id, ct);
        }, ct);

    internal static void Kapat(UretimEmri emir)
    {
        if (emir.Uretilen > 0)
        {
            emir.Adet = emir.Uretilen;
            emir.Durum = UretimEmriDurumu.Tamamlandi;
        }
        else
        {
            emir.Durum = UretimEmriDurumu.Iptal;
        }
        emir.BitisZamani = Zaman.Simdi();
    }

    private async Task<UretimEmri> BekleyenGetirAsync(int id, CancellationToken ct)
    {
        var emir = await db.UretimEmirleri.SingleOrDefaultAsync(e => e.Id == id, ct)
                   ?? throw new BulunamadiHatasi("Üretim işi bulunamadı.");
        if (emir.Durum != UretimEmriDurumu.Bekliyor)
            throw new IsKuraliHatasi("Bu iş artık sırada değil.");
        return emir;
    }
}
