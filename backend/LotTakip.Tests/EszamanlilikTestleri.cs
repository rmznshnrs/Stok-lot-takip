using LotTakip.Business;
using LotTakip.DataAccess;
using LotTakip.Entity;
using LotTakip.Shared;
using Microsoft.EntityFrameworkCore;

namespace LotTakip.Tests;

/// <summary>
/// Aynı anda yapılan işlemler. Gerçek hayattaki gibi iki ayrı
/// bağlantı ve iki ayrı bağlam kullanılır; bu yüzden veriler commit edilir ve her test
/// başında/sonunda tüm veri silinir (transaction ile geri alma burada kullanılamaz).
/// </summary>
[Collection(VeritabaniKoleksiyonu.Ad)]
public class EszamanliIsKuraliTestleri(TestVeritabani vt) : IAsyncLifetime
{
    private int _kullaniciId, _adminId, _urunId, _diyotLotId;

    public async Task InitializeAsync()
    {
        await vt.TumVeriyiSilAsync();
        await using var db = vt.YeniContext();
        var kullanici = new Kullanici { Ad = "D", Soyad = "G", KullaniciAdi = "depo", SifreHash = "x" };
        var admin = new Kullanici { Ad = "A", Soyad = "M", KullaniciAdi = "yonetici", SifreHash = "x", Rol = KullaniciRolu.Admin };
        var ted = new Tedarikci { Ad = "T" };
        var led = new Parca { Kod = "LED", Ad = "LED" };
        var diyot = new Parca { Kod = "DIY", Ad = "Diyot" };
        var urun = new Urun { Kod = "IP", Ad = "İndikatör Paneli", SeriOneki = "SN-IP-" };
        urun.Agac.Add(new UrunAgaci { Urun = urun, Parca = led, Adet = 4 });
        urun.Agac.Add(new UrunAgaci { Urun = urun, Parca = diyot, Adet = 1 });
        db.AddRange(kullanici, admin, urun);
        StokLotu Lot(string no, Parca p, int adet) => new()
        {
            LotNo = no, Parca = p, Tedarikci = ted, SiparisTarihi = new(2026, 1, 1), GirisAdet = adet, KalanAdet = adet,
            OlusturanKullanici = kullanici, GirisZamani = Zaman.Simdi(), OlusturmaZamani = Zaman.Simdi(),
        };
        db.StokLotlari.Add(Lot("L-LED", led, 400));
        var diyotLot = Lot("L-DIY", diyot, 1);  // diyot tek ürüne yeter (testler gerekirse artırır)
        db.StokLotlari.Add(diyotLot);
        db.Musteriler.Add(new Musteri { Ad = "Atlas" });
        await db.SaveChangesAsync();
        (_kullaniciId, _adminId, _urunId, _diyotLotId) = (kullanici.Id, admin.Id, urun.Id, diyotLot.Id);
    }

    public async Task DisposeAsync()
    {
        vt.Kanca.Oncesi = null;
        await vt.TumVeriyiSilAsync();
    }

    private static UretimServisi UretimServisi(AppDbContext db) =>
        new(db, new StokLotuRepository(db), new UretimRepository(db));

    private async Task DiyotKalaniniAyarla(int adet)
    {
        await using var db = vt.YeniContext();
        await db.StokLotlari.Where(l => l.Id == _diyotLotId)
            .ExecuteUpdateAsync(s => s.SetProperty(l => l.GirisAdet, adet).SetProperty(l => l.KalanAdet, adet));
    }

    private async Task<int> DiyotKalani()
    {
        await using var db = vt.YeniContext();
        return await db.StokLotlari.Where(l => l.Id == _diyotLotId).Select(l => l.KalanAdet).SingleAsync();
    }

    [Fact]
    public async Task Tek_urunluk_stokta_ayni_anda_iki_uretimden_biri_basarili_digeri_yetersiz()
    {
        await using var db1 = vt.YeniContext();
        await using var db2 = vt.YeniContext();
        var sonuclar = await Task.WhenAll(
            Dene(() => UretimServisi(db1).UretAsync(_urunId, 1, _kullaniciId)),
            Dene(() => UretimServisi(db2).UretAsync(_urunId, 1, _kullaniciId)));

        Assert.Equal(1, sonuclar.Count(s => s is null));                     // biri başarılı
        Assert.IsType<YetersizStokHatasi>(Assert.Single(sonuclar, s => s is not null));  // diğeri yetersiz stok
        Assert.Equal(0, await DiyotKalani());                                 // eksiye düşmedi
        await using var db = vt.YeniContext();
        Assert.Equal(["SN-IP-0001"], await db.Uretimler.Select(u => u.SeriNo).ToListAsync());
    }

    [Fact]
    public async Task Ayni_anda_iki_uretimde_seri_nolar_cakismaz()
    {
        await DiyotKalaniniAyarla(10);
        await using var db1 = vt.YeniContext();
        await using var db2 = vt.YeniContext();
        var sonuclar = await Task.WhenAll(
            UretimServisi(db1).UretAsync(_urunId, 2, _kullaniciId),
            UretimServisi(db2).UretAsync(_urunId, 2, _kullaniciId));

        Assert.Equal(["SN-IP-0001", "SN-IP-0002", "SN-IP-0003", "SN-IP-0004"],
                     sonuclar.SelectMany(s => s.SeriNolar).Order());
        Assert.Equal(6, await DiyotKalani());
        await using var db = vt.YeniContext();
        Assert.Equal(400 - 16, await db.StokLotlari.Where(l => l.LotNo == "L-LED").Select(l => l.KalanAdet).SingleAsync());
        Assert.Equal(16 + 4, await db.UretimTuketimleri.SumAsync(t => t.Adet));
    }

    [Fact]
    public async Task Seri_no_cakismasinda_yeniden_denenir()
    {
        await DiyotKalaniniAyarla(10);
        // Kayıt anında başka biri SN-IP-0001'i almış olsun (araya giren işlem, ayrı bağlantı)
        vt.Kanca.Oncesi = async komut =>
        {
            if (!komut.CommandText.Contains("INSERT INTO [Uretim]"))
                return;
            vt.Kanca.Oncesi = null;  // yalnız ilk denemede
            await using var araya = vt.YeniContext();
            araya.Uretimler.Add(new Uretim { UrunId = _urunId, SeriNo = "SN-IP-0001", OlusturanKullaniciId = _kullaniciId });
            await araya.SaveChangesAsync();
        };

        await using var db1 = vt.YeniContext();
        var s = await UretimServisi(db1).UretAsync(_urunId, 1, _kullaniciId);

        Assert.Equal(["SN-IP-0002"], s.SeriNolar);  // ilk deneme benzersiz indekse takıldı, ikincisi sonrakini aldı
        Assert.Equal(9, await DiyotKalani());       // stok yalnız bir kez düşüldü
        await using var db = vt.YeniContext();
        Assert.Equal(1, await db.UretimTuketimleri.Select(t => t.UretimId).Distinct().CountAsync());
    }

    [Fact]
    public async Task Ayni_lota_ayni_anda_duzeltmede_ikisi_de_yansir()
    {
        await DiyotKalaniniAyarla(10);
        // Birinci düzeltme lotu okuduktan sonra, kaydetmeden önce ikinci düzeltme araya girip kaydetsin
        vt.Kanca.Oncesi = async komut =>
        {
            if (!komut.CommandText.Contains("UPDATE [StokLotu]"))
                return;
            vt.Kanca.Oncesi = null;
            await using var araya = vt.YeniContext();
            await new StokDuzeltmeServisi(araya).DuzeltAsync(new StokDuzeltmeIstek(_diyotLotId, -3, "Fire", "araya giren"), _adminId);
        };

        await using var db1 = vt.YeniContext();
        var s = await new StokDuzeltmeServisi(db1).DuzeltAsync(new StokDuzeltmeIstek(_diyotLotId, 5, "SayimFarki", "sayım"), _adminId);

        Assert.Equal(12, s.YeniKalanAdet);  // 10 - 3 + 5: eski değerin üstüne yazılmadı (RowVersion + yeniden deneme)
        Assert.Equal(12, await DiyotKalani());
        await using var db = vt.YeniContext();
        Assert.Equal(2, await db.StokDuzeltmeleri.CountAsync());
    }

    [Fact]
    public async Task Ayni_seri_no_ayni_anda_iki_satista_yalniz_bir_kez_satilir()
    {
        await DiyotKalaniniAyarla(10);
        await using (var db = vt.YeniContext())
            await UretimServisi(db).UretAsync(_urunId, 1, _kullaniciId);

        await using var db1 = vt.YeniContext();
        await using var db2 = vt.YeniContext();
        var sonuclar = await Task.WhenAll(
            Dene(() => new SatisServisi(db1).SatAsync(new SatisIstek("Atlas", ["SN-IP-0001"]), _kullaniciId)),
            Dene(() => new SatisServisi(db2).SatAsync(new SatisIstek("Beta", ["SN-IP-0001"]), _kullaniciId)));

        Assert.Equal(1, sonuclar.Count(s => s is null));
        Assert.IsType<SatisHatasi>(Assert.Single(sonuclar, s => s is not null));
        await using var kontrol = vt.YeniContext();
        Assert.Equal(1, await kontrol.SatisKalemleri.CountAsync());
        Assert.Equal(1, await kontrol.Satislar.CountAsync());
    }

    /// <summary>İşi çalıştırır; başarılıysa null, hata olursa hatayı döndürür.</summary>
    private static async Task<Exception?> Dene<T>(Func<Task<T>> is_)
    {
        try
        {
            await is_();
            return null;
        }
        catch (Exception hata)
        {
            return hata;
        }
    }
}
