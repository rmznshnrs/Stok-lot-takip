using LotTakip.Business;
using LotTakip.Shared;

namespace LotTakip.Tests;

public class IletisimTestleri(TestVeritabani vt) : IsKuraliTesti(vt)
{
    private TanimServisi Tanim => new(Db, Stok);

    [Fact]
    public async Task Telefon_eposta_adres_ayri_tutulur_tek_satir_hesaplanir()
    {
        var m = await Tanim.MusteriKaydetAsync(null,
            new IletisimIstek("Gama", Telefon: " 0212  555 00 00 ", Eposta: " gama@ornek.com ", Adres: "Kayseri"));
        Assert.Equal(("0212 555 00 00", "gama@ornek.com", "Kayseri"), (m.Telefon, m.Eposta, m.Adres));
        Assert.Equal("0212 555 00 00 · gama@ornek.com · Kayseri", m.Iletisim);

        m = await Tanim.MusteriKaydetAsync(m.Id, new IletisimIstek("Gama", Telefon: "", Eposta: "gama@ornek.com", Adres: ""));
        Assert.Equal("gama@ornek.com", m.Iletisim);
        var t = await Tanim.TedarikciKaydetAsync(null, new IletisimIstek("Delta", Telefon: "0312 111 22 33"));
        Assert.Equal(("0312 111 22 33", "", ""), (t.Telefon, t.Eposta, t.Adres));
    }

    [Fact]
    public async Task Gecersiz_eposta_kabul_edilmez() =>
        await Assert.ThrowsAsync<IsKuraliHatasi>(() => Tanim.MusteriKaydetAsync(null, new IletisimIstek("Gama", Eposta: "gama")));

    [Fact]
    public async Task Satis_gecmisi_seri_numaralarini_urun_lotlariyla_verir()
    {
        await Uretim.UretAsync(Urun.Id, 2, Kullanici.Id);  // SN-IP-L001
        await Uretim.UretAsync(Urun.Id, 1, Kullanici.Id);  // SN-IP-L002
        await Satis.SatAsync(new SatisIstek("Atlas", ["SN-IP-0003", "SN-IP-0001"]), Kullanici.Id);
        var g = Assert.Single(await Satis.SatisGecmisiAsync());
        Assert.Equal([("SN-IP-0001", "SN-IP-L001"), ("SN-IP-0003", "SN-IP-L002")],
            g.Kalemler!.Select(k => (k.SeriNo, k.LotNo)));
    }
}
