using LotTakip.Business;
using LotTakip.Entity;
using Microsoft.EntityFrameworkCore;

namespace LotTakip.Tests;

public class UretimOnizlemeTestleri(TestVeritabani vt) : IsKuraliTesti(vt)
{
    [Fact]
    public async Task Fifo_dagilimi_ve_geri_cagrilan_lot_atlanir()
    {
        var o = await Uretim.OnizleAsync(Urun.Id, 5);
        Assert.True(o.Yeterli);
        var diyot = o.Parcalar.Single(p => p.Parca.Kod == "DIY");
        Assert.Equal(5, diyot.Gereken);
        Assert.Equal(13, diyot.Kullanilabilir);  // geri çağrılan 50 sayılmaz
        Assert.Equal([("L-D-ESKI", 3), ("L-D-YENI", 2)], diyot.Lotlar.Select(l => (l.LotNo, l.Adet)));
        var led = o.Parcalar.Single(p => p.Parca.Kod == "LED");
        Assert.Equal([("L-LED", 20)], led.Lotlar.Select(l => (l.LotNo, l.Adet)));
    }

    [Fact]
    public async Task Eksikler()
    {
        var o = await Uretim.OnizleAsync(Urun.Id, 30);  // 120 LED, 30 diyot gerekir
        Assert.False(o.Yeterli);
        Assert.Equal(new Dictionary<string, int> { ["DIY"] = 17, ["LED"] = 20 },
                     o.Eksikler.ToDictionary(p => p.Parca.Kod, p => p.Eksik));
    }

    [Fact]
    public async Task Hicbir_sey_kaydetmez()
    {
        await Uretim.OnizleAsync(Urun.Id, 5);
        Assert.False(await Db.Uretimler.AnyAsync());
        Assert.Equal(3, await Kalan(DiyotEski));
    }

    [Fact]
    public async Task Gecersiz_adet_ve_bos_agac()
    {
        await Assert.ThrowsAsync<IsKuraliHatasi>(() => Uretim.OnizleAsync(Urun.Id, 0));
        await Assert.ThrowsAsync<IsKuraliHatasi>(() => Uretim.OnizleAsync(Urun.Id, 1000));
        await Assert.ThrowsAsync<BulunamadiHatasi>(() => Uretim.OnizleAsync(-1, 1));
        var bos = new Urun { Kod = "BOS", Ad = "Boş", SeriOneki = "SN-B-" };
        Db.Urunler.Add(bos);
        await Db.SaveChangesAsync();
        var hata = await Assert.ThrowsAsync<IsKuraliHatasi>(() => Uretim.OnizleAsync(bos.Id, 1));
        Assert.Contains("ürün ağacı boş", hata.Message);
    }

    [Fact]
    public async Task Seri_nolar_ve_atlanan_lotlar()
    {
        var o = await Uretim.OnizleAsync(Urun.Id, 3);
        Assert.Equal(["SN-IP-0001", "SN-IP-0002", "SN-IP-0003"], o.SeriNolar);
        Assert.Equal(["L-D-GERI"], o.Parcalar.Single(p => p.Parca.Kod == "DIY").AtlananLotlar);
        Assert.Equal(["L-D-GERI"], o.AtlananLotlar);
        await Uretim.UretAsync(Urun.Id, 1, Kullanici.Id);
        Assert.Equal(["SN-IP-0002"], await Uretim.SonrakiSeriNolarAsync(Urun.Id, 1));
    }
}

public class UretimYapTestleri(TestVeritabani vt) : IsKuraliTesti(vt)
{
    private async Task<string> DiyotLotu(string seriNo) =>
        await Db.UretimTuketimleri.Where(t => t.Uretim!.SeriNo == seriNo && t.StokLotu!.ParcaId == Diyot.Id)
                                  .Select(t => t.StokLotu!.LotNo).SingleAsync();

    [Fact]
    public async Task N_kayit_ve_ardisik_seri_no()
    {
        var s = await Uretim.UretAsync(Urun.Id, 3, Kullanici.Id);
        Assert.Equal(["SN-IP-0001", "SN-IP-0002", "SN-IP-0003"], s.SeriNolar);
        s = await Uretim.UretAsync(Urun.Id, 2, Kullanici.Id);
        Assert.Equal(["SN-IP-0004", "SN-IP-0005"], s.SeriNolar);
        Assert.Equal(5, await Db.Uretimler.CountAsync());
    }

    [Fact]
    public async Task Seri_no_en_buyuk_siradan_devam_eder()
    {
        Db.Uretimler.Add(new Uretim { UrunId = Urun.Id, SeriNo = "SN-IP-0041", OlusturanKullaniciId = Kullanici.Id });
        Db.Uretimler.Add(new Uretim { UrunId = Urun.Id, SeriNo = "SN-IP-HATALI", OlusturanKullaniciId = Kullanici.Id });
        await Db.SaveChangesAsync();
        Db.ChangeTracker.Clear();
        Assert.Equal(["SN-IP-0042"], (await Uretim.UretAsync(Urun.Id, 1, Kullanici.Id)).SeriNolar);
    }

    [Fact]
    public async Task Fifo_dusum_birden_fazla_lottan()
    {
        await Uretim.UretAsync(Urun.Id, 5, Kullanici.Id);
        Assert.Equal(0, await Kalan(DiyotEski));
        Assert.Equal(8, await Kalan(DiyotYeni));
        Assert.Equal(80, await Kalan(LedLot));
        // İlk 3 ürün eski lottan, sonraki 2 yeni lottan diyot alır
        var beklenen = new[] { "L-D-ESKI", "L-D-ESKI", "L-D-ESKI", "L-D-YENI", "L-D-YENI" };
        for (var i = 1; i <= 5; i++)
            Assert.Equal(beklenen[i - 1], await DiyotLotu($"SN-IP-{i:D4}"));
    }

    [Fact]
    public async Task Bir_urun_icin_lot_bolunur()
    {
        // LED: 6'lık eski lot + yeni lot; 2. ürünün 4 LED'i iki lottan gelmeli
        await Db.StokLotlari.Where(l => l.Id == LedLot.Id).ExecuteUpdateAsync(s => s.SetProperty(l => l.KalanAdet, 0));
        var eski = await LotEkle("L-LED-ESKI", Led, new(2025, 1, 1), 6);
        var yeni = await LotEkle("L-LED-YENI", Led, new(2025, 2, 1), 50);
        Db.ChangeTracker.Clear();

        await Uretim.UretAsync(Urun.Id, 2, Kullanici.Id);
        var ikinci = await Db.UretimTuketimleri.Where(t => t.Uretim!.SeriNo == "SN-IP-0002" && t.StokLotu!.ParcaId == Led.Id)
            .OrderBy(t => t.StokLotu!.LotNo).Select(t => new { t.StokLotu!.LotNo, t.Adet }).ToListAsync();
        Assert.Equal([("L-LED-ESKI", 2), ("L-LED-YENI", 2)], ikinci.Select(t => (t.LotNo, t.Adet)));
        Assert.Equal(0, await Kalan(eski));
        Assert.Equal(48, await Kalan(yeni));
    }

    [Fact]
    public async Task Geri_cagrilan_lot_kullanilmaz()
    {
        await Uretim.UretAsync(Urun.Id, 13, Kullanici.Id);
        Assert.Equal(50, await Kalan(DiyotGeri));
        Assert.False(await Db.UretimTuketimleri.AnyAsync(t => t.StokLotuId == DiyotGeri.Id));
    }

    [Fact]
    public async Task Yetersiz_stokta_hicbir_sey_kaydedilmez()
    {
        var hata = await Assert.ThrowsAsync<YetersizStokHatasi>(() => Uretim.UretAsync(Urun.Id, 14, Kullanici.Id));
        Assert.Equal([("DIY", 1)], hata.Eksikler.Select(e => (e.Parca.Kod, e.Eksik)));
        Assert.False(await Db.Uretimler.AnyAsync());
        Assert.False(await Db.UretimTuketimleri.AnyAsync());
        Assert.Equal(3, await Kalan(DiyotEski));
        Assert.Equal(100, await Kalan(LedLot));
    }

    [Fact]
    public async Task Hata_olursa_hicbir_sey_kaydedilmez()
    {
        // Tüketim satırları yazılırken veritabanı hatası (bağlantı kopması vb.)
        Vt.Kanca.Oncesi = k => k.CommandText.Contains("INSERT INTO [UretimTuketim]")
            ? throw new InvalidOperationException("beklenmeyen hata")
            : Task.CompletedTask;
        await Assert.ThrowsAnyAsync<Exception>(() => Uretim.UretAsync(Urun.Id, 3, Kullanici.Id));
        Vt.Kanca.Oncesi = null;

        Assert.False(await Db.Uretimler.AnyAsync());
        Assert.False(await Db.UretimTuketimleri.AnyAsync());
        Assert.Equal(3, await Kalan(DiyotEski));
        Assert.Equal(100, await Kalan(LedLot));
        // Bağlam temizlendi: sonraki bir işlem yarım kalan kayıtları yazmaz
        Assert.Equal(["SN-IP-0001"], (await Uretim.UretAsync(Urun.Id, 1, Kullanici.Id)).SeriNolar);
        Assert.Equal(1, await Db.Uretimler.CountAsync());
    }

    [Fact]
    public async Task Eski_uretim_urun_agaci_degisse_de_dogru_kalir()
    {
        var s = await Uretim.UretAsync(Urun.Id, 1, Kullanici.Id);
        await Db.UrunAgaclari.Where(a => a.ParcaId == Led.Id).ExecuteUpdateAsync(x => x.SetProperty(a => a.Adet, 10));
        var adet = await Db.UretimTuketimleri.Where(t => t.Uretim!.SeriNo == s.SeriNolar[0] && t.StokLotu!.ParcaId == Led.Id)
                                             .SumAsync(t => t.Adet);
        Assert.Equal(4, adet);
    }

    [Fact]
    public async Task Islemi_yapan_kullanici_ve_tarih_tutulur()
    {
        var tarih = new DateTimeOffset(2026, 4, 2, 10, 0, 0, TimeSpan.FromHours(3));
        await Uretim.UretAsync(Urun.Id, 2, Kullanici.Id, tarih);
        var kayitlar = await Db.Uretimler.AsNoTracking().ToListAsync();
        Assert.All(kayitlar, u => Assert.Equal((Kullanici.Id, tarih), (u.OlusturanKullaniciId, u.Tarih)));
        await Assert.ThrowsAsync<YetkiHatasi>(() => Uretim.UretAsync(Urun.Id, 1, kullaniciId: -5));
    }
}
