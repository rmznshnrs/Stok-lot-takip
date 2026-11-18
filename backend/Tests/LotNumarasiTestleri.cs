using Microsoft.EntityFrameworkCore;

namespace Tests;

public class LotNumarasiTestleri(TestVeritabani vt) : IsKuraliTesti(vt)
{
    private Task<MalGirisiSonucuDto> Giris(string? stokGirisNo = null, string tedarikciLotNo = "") =>
        Stok.MalGirisiAsync(new MalGirisiIstek(Led.Id, "Tedarikçi A", stokGirisNo, 10, new(2026, 9, 1), "", tedarikciLotNo),
                            Kullanici.Id);

    [Fact]
    public async Task Stok_giris_no_verilmezse_sirayla_verilir()
    {
        var a = await Giris(tedarikciLotNo: " T-LOT-7 ");
        var b = await Giris();
        Assert.Equal("SG-0001", a.Lot.LotNo);
        Assert.Equal("T-LOT-7", a.Lot.TedarikciLotNo);
        Assert.Equal("SG-0002", b.Lot.LotNo);
        Assert.Equal("", b.Lot.TedarikciLotNo);
    }

    [Fact]
    public async Task Stok_giris_no_en_buyugun_ustunden_devam_eder()
    {
        await Giris("SG-0041");
        await Giris("SG-ESKI");  // sıra biçiminde olmayan numara sayılmaz
        Assert.Equal("SG-0042", (await Giris()).Lot.LotNo);
    }

    [Fact]
    public async Task Numara_aramasi_tedarikci_lot_nosunu_tanir()
    {
        var tek = await Giris(tedarikciLotNo: "TD-55");
        Assert.Equal(("lot", tek.Lot.LotNo), Sonuc(await Izleme.NumaraAraAsync("TD-55")));

        var ikinci = await Giris(tedarikciLotNo: "TD-55");  // aynı tedarikçi lotu iki girişte
        var s = await Izleme.NumaraAraAsync("TD-55");
        Assert.Null(s.Tur);
        Assert.Equal([tek.Lot.LotNo, ikinci.Lot.LotNo], s.Adaylar.Select(a => a.Deger));
    }

    [Fact]
    public async Task Son_hareketlerde_uretim_seri_nosuyla_gorunur()
    {
        await Uretim.UretAsync(Urun.Id, 2, Kullanici.Id);
        var h = (await Izleme.SonHareketlerAsync()).Single(x => x.Tur == "uretim");
        Assert.Equal(("SN-IP-0001", "seri"), (h.NumaraIlk, h.NumaraTuru));
        Assert.Equal(("İndikatör Paneli · 2 adet", "SN-IP-0001 … SN-IP-0002"), (h.Aciklama, h.Numara));
    }

    private static (string?, string?) Sonuc(NumaraAramaDto d) => (d.Tur, d.Deger);
}
