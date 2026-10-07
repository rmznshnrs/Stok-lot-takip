using LotTakip.Business;
using LotTakip.Shared;
using Microsoft.EntityFrameworkCore;

namespace LotTakip.Tests;

public class LotIzlemeTestleri(TestVeritabani vt) : IsKuraliTesti(vt)
{
    [Fact]
    public async Task Uretimler_ve_musteriler()
    {
        await Db.Musteriler.Where(m => m.Id == Musteri.Id).ExecuteUpdateAsync(s => s.SetProperty(m => m.Iletisim, "atlas@ornek.example"));
        Db.ChangeTracker.Clear();
        await Uretim.UretAsync(Urun.Id, 5, Kullanici.Id);  // 0001-0003 ESKI, 0004-0005 YENI
        await Satis.SatAsync(new SatisIstek("Atlas", ["SN-IP-0001", "SN-IP-0002"], new(2026, 5, 12)), Kullanici.Id);

        var iz = (await Izleme.LotIzleAsync("l-d-eski"))!;  // büyük/küçük harf duyarsız
        Assert.Equal("L-D-ESKI", iz.Lot.LotNo);
        Assert.Equal(3, iz.KullanilanAdet);
        Assert.Equal(["SN-IP-0001", "SN-IP-0002", "SN-IP-0003"], iz.Uretimler.Select(u => u.SeriNo));
        Assert.Equal(1, iz.Uretimler[0].Adet);
        Assert.Equal("Atlas", iz.Uretimler[0].Satis!.MusteriAd);
        var m = Assert.Single(iz.Musteriler);
        Assert.Equal(("Atlas", "atlas@ornek.example"), (m.Ad, m.Iletisim));
        Assert.Equal(["SN-IP-0001", "SN-IP-0002"], m.SeriNolar);
        Assert.Equal([new MusteriSatisiDto("SN-IP-0001", new(2026, 5, 12)), new MusteriSatisiDto("SN-IP-0002", new(2026, 5, 12))], m.Satislar);
        Assert.Equal(["SN-IP-0003"], iz.StoktakiUretimler.Select(u => u.SeriNo));
        Assert.DoesNotContain("SN-IP-0004", iz.Uretimler.Select(u => u.SeriNo));
    }

    [Fact]
    public async Task Kullanilmamis_lot()
    {
        var iz = (await Izleme.LotIzleAsync("L-D-GERI"))!;
        Assert.Empty(iz.Uretimler);
        Assert.Empty(iz.Musteriler);
        Assert.True(iz.Lot.GeriCagrildi);
    }

    [Fact]
    public async Task Bilinmeyen_lot() => Assert.Null(await Izleme.LotIzleAsync("YOK"));
}

public class SeriIzlemeTestleri(TestVeritabani vt) : IsKuraliTesti(vt)
{
    [Fact]
    public async Task Parcalar_lotlar_ve_satis()
    {
        await Uretim.UretAsync(Urun.Id, 1, Kullanici.Id);
        var satis = await Satis.SatAsync(new SatisIstek("Atlas", ["SN-IP-0001"]), Kullanici.Id);
        var iz = (await Izleme.SeriIzleAsync("sn-ip-0001"))!;
        Assert.Equal(("SN-IP-0001", "IP"), (iz.SeriNo, iz.UrunKod));
        Assert.Equal([("DIY", "L-D-ESKI", 1), ("LED", "L-LED", 4)], iz.Parcalar.Select(p => (p.Parca.Kod, p.Lot.LotNo, p.Adet)));
        Assert.Equal("Tedarikçi A", iz.Parcalar[0].Lot.TedarikciAd);
        Assert.Equal((satis.SatisId, "Atlas"), (iz.Satis!.SatisId, iz.Satis.MusteriAd));
        Assert.Empty(iz.GeriCagrilanLotlar);
    }

    [Fact]
    public async Task Satilmamis_ve_geri_cagrilan()
    {
        await Uretim.UretAsync(Urun.Id, 1, Kullanici.Id);
        await GeriCagir(DiyotEski);
        var iz = (await Izleme.SeriIzleAsync("SN-IP-0001"))!;
        Assert.Null(iz.Satis);
        Assert.Equal(["L-D-ESKI"], iz.GeriCagrilanLotlar);
    }

    [Fact]
    public async Task Bilinmeyen_seri() => Assert.Null(await Izleme.SeriIzleAsync("SN-YOK-0001"));
}

public class SatisTestleri(TestVeritabani vt) : IsKuraliTesti(vt)
{
    public override async Task InitializeAsync()
    {
        await base.InitializeAsync();
        await Uretim.UretAsync(Urun.Id, 5, Kullanici.Id);  // 0001-0003 L-D-ESKI, 0004-0005 L-D-YENI
    }

    private Task<int> SatisSayisi() => Db.Satislar.CountAsync();

    [Fact]
    public async Task Satis_kaydedilir()
    {
        var s = await Satis.SatAsync(new SatisIstek(" atlas ", ["SN-IP-0004", " sn-ip-0001 "], new(2026, 5, 1)), Kullanici.Id);
        Assert.Equal((Musteri.Id, "Atlas", false), (s.MusteriId, s.MusteriAd, s.YeniMusteri));  // var olan müşteri
        Assert.Equal(new DateOnly(2026, 5, 1), s.Tarih);
        Assert.Equal(["SN-IP-0004", "SN-IP-0001"], s.SeriNolar);  // verilen sırayla, kayıttaki yazımla
        Assert.Empty(s.Uyarilar);
        var kayit = await Db.Satislar.AsNoTracking().Include(x => x.Kalemler).SingleAsync();
        Assert.Equal((2, Kullanici.Id), (kayit.Kalemler.Count, kayit.OlusturanKullaniciId));
    }

    [Fact]
    public async Task Yeni_musteri_olusur_ve_tarih_bugun()
    {
        var s = await Satis.SatAsync(new SatisIstek("Gama  Ltd", ["SN-IP-0001"]), Kullanici.Id);
        Assert.True(s.YeniMusteri);
        Assert.Equal("Gama Ltd", s.MusteriAd);
        Assert.Equal(Zaman.Bugun(), s.Tarih);
    }

    [Fact]
    public async Task Geri_cagrilan_lot_onay_olmadan_satilmaz()
    {
        await GeriCagir(DiyotEski);
        var hata = await Assert.ThrowsAsync<GeriCagrilanLotOnayiGerekliHatasi>(
            () => Satis.SatAsync(new SatisIstek("Atlas", ["SN-IP-0001", "SN-IP-0004"]), Kullanici.Id));
        Assert.Equal([("SN-IP-0001", "L-D-ESKI")], hata.Uyarilar.Select(u => (u.SeriNo, string.Join(",", u.Lotlar))));
        Assert.Equal(0, await SatisSayisi());

        var s = await Satis.SatAsync(new SatisIstek("Atlas", ["SN-IP-0001", "SN-IP-0004"], GeriCagrilanOnayi: true), Kullanici.Id);
        Assert.Equal(2, s.SeriNolar.Count);
        Assert.Equal("SN-IP-0001", Assert.Single(s.Uyarilar).SeriNo);

        // Kaydetmeden kontrol de aynı uyarıyı verir
        Assert.Equal([("SN-IP-0002", "L-D-ESKI")],
                     (await Satis.SatisUyarilariAsync(["sn-ip-0002", "SN-IP-0005"])).Select(u => (u.SeriNo, string.Join(",", u.Lotlar))));
    }

    [Fact]
    public async Task Daha_once_satilmis_seri_no_satilamaz()
    {
        await Satis.SatAsync(new SatisIstek("Atlas", ["SN-IP-0001"], new(2026, 5, 12)), Kullanici.Id);
        var hata = await Assert.ThrowsAsync<SatisHatasi>(
            () => Satis.SatAsync(new SatisIstek("Beta", ["SN-IP-0002", "SN-IP-0001"]), Kullanici.Id));
        Assert.Contains("SN-IP-0001 daha önce satılmış: Atlas, 12.05.2026", hata.Message);
        Assert.Equal(1, await SatisSayisi());
        Assert.False(await Db.SatisKalemleri.AnyAsync(k => k.Uretim!.SeriNo == "SN-IP-0002"));
        Assert.False(await Db.Musteriler.AnyAsync(m => m.Ad == "Beta"));  // hata olunca müşteri de oluşmaz
    }

    [Fact]
    public async Task Ayni_baglamda_bile_satilmis_kontrolu_acik_yapilir()
    {
        // EF tuzağı: eski kalem bellekteyken bire bir ilişkide eskisi sessizce silinirdi
        await Satis.SatAsync(new SatisIstek("Atlas", ["SN-IP-0003"]), Kullanici.Id);
        var eskiKalem = await Db.SatisKalemleri.Include(k => k.Uretim).SingleAsync();  // izlenen kayıt
        await Assert.ThrowsAsync<SatisHatasi>(() => Satis.SatAsync(new SatisIstek("Beta", ["SN-IP-0003"]), Kullanici.Id));
        Assert.Equal(1, await Db.SatisKalemleri.AsNoTracking().CountAsync());
        Assert.Equal(eskiKalem.SatisId, await Db.SatisKalemleri.AsNoTracking().Select(k => k.SatisId).SingleAsync());
    }

    [Theory]
    [InlineData(new[] { "SN-IP-9999" }, "Bulunamayan")]
    [InlineData(new[] { "SN-IP-0001", "sn-ip-0001" }, "tekrarlanan")]
    [InlineData(new string[0], "En az bir")]
    [InlineData(new[] { "  " }, "En az bir")]
    public async Task Bilinmeyen_tekrarlanan_ve_bos_liste(string[] seriler, string mesaj)
    {
        var hata = await Assert.ThrowsAsync<SatisHatasi>(() => Satis.SatAsync(new SatisIstek("Atlas", seriler), Kullanici.Id));
        Assert.Contains(mesaj, hata.Message);
        Assert.Equal(0, await SatisSayisi());
    }

    [Fact]
    public async Task Musteri_adi_bos_olamaz() =>
        await Assert.ThrowsAsync<SatisHatasi>(() => Satis.SatAsync(new SatisIstek("  ", ["SN-IP-0001"]), Kullanici.Id));

    [Fact]
    public async Task Satilabilir_kontrolu()
    {
        var s = await Satis.SatilabilirAsync("sn-ip-0001");
        Assert.Equal(("SN-IP-0001", "İndikatör Paneli"), (s.SeriNo, s.UrunAd));
        Assert.Empty(s.GeriCagrilanLotlar);

        await Satis.SatAsync(new SatisIstek("Atlas", ["SN-IP-0001"], new(2026, 5, 12)), Kullanici.Id);
        var hata = await Assert.ThrowsAsync<SatisHatasi>(() => Satis.SatilabilirAsync("SN-IP-0001"));
        Assert.Contains("daha önce satılmış: Atlas", hata.Message);
        hata = await Assert.ThrowsAsync<SatisHatasi>(() => Satis.SatilabilirAsync("SN-YOK"));
        Assert.Contains("bulunamadı", hata.Message);

        await GeriCagir(DiyotYeni);
        Assert.Equal(["L-D-YENI"], (await Satis.SatilabilirAsync("SN-IP-0004")).GeriCagrilanLotlar);
    }

    [Fact]
    public async Task Satis_gecmisi()
    {
        await Satis.SatAsync(new SatisIstek("Atlas", ["SN-IP-0002", "SN-IP-0001"], new(2026, 5, 1)), Kullanici.Id);
        await Satis.SatAsync(new SatisIstek("Beta", ["SN-IP-0003"], new(2026, 6, 1)), Kullanici.Id);
        var g = await Satis.SatisGecmisiAsync();
        Assert.Equal([("Beta", new[] { "SN-IP-0003" }), ("Atlas", new[] { "SN-IP-0001", "SN-IP-0002" })],
                     g.Select(x => (x.MusteriAd, x.SeriNolar.ToArray())));
        Assert.Equal(["Atlas"], (await Satis.SatisGecmisiAsync("0001")).Select(x => x.MusteriAd));
        Assert.Equal(["Beta"], (await Satis.SatisGecmisiAsync("bet")).Select(x => x.MusteriAd));
    }
}
