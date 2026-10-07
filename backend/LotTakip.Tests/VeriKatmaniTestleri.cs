using LotTakip.Business;
using LotTakip.DataAccess;
using LotTakip.Entity;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace LotTakip.Tests;

public class SemaTestleri(TestVeritabani vt) : VeritabaniTesti(vt)
{
    [Fact]
    public async Task Veritabani_buyuk_kucuk_harf_duyarsiz_siralama_kuraliyla_olusur()
    {
        var kural = await Db.Database.SqlQueryRaw<string>(
            "SELECT CAST(DATABASEPROPERTYEX(DB_NAME(), 'Collation') AS nvarchar(128)) AS Value").SingleAsync();
        Assert.Equal(AppDbContext.Siralama, kural);
    }

    [Fact]
    public async Task Tum_tablolar_olusur()
    {
        var tablolar = await Db.Database.SqlQueryRaw<string>(
            "SELECT name AS Value FROM sys.tables WHERE name <> '__EFMigrationsHistory'").ToListAsync();
        Assert.Equal(
            ["Kullanici", "Musteri", "Parca", "Satis", "SatisKalemi", "StokDuzeltme", "StokLotu",
             "Tedarikci", "Uretim", "UretimTuketim", "Urun", "UrunAgaci"],
            tablolar.Order());
    }

    [Fact]
    public async Task Bekleyen_migration_yok_model_ile_sema_uyumlu()
    {
        Assert.Empty(await Db.Database.GetPendingMigrationsAsync());
        Assert.False(Db.Database.HasPendingModelChanges());
    }
}

public class OrnekVeriTestleri(TestVeritabani vt) : VeritabaniTesti(vt)
{
    [Fact]
    public async Task Ornek_veri_sayilari_dogru()
    {
        var s = await OrnekVeriYukle();
        Assert.Equal(new OrnekVeri.Sonuc(7, 3, 10, 2, 9, 2, 2), s);
        Assert.Equal(3, await Db.SatisKalemleri.CountAsync());
        Assert.Equal(45, await Db.UretimTuketimleri.CountAsync());
    }

    [Fact]
    public async Task Lotlarda_kalan_adetler_dogru()
    {
        await OrnekVeriYukle();
        // Örnek veri yüklendikten sonra (FIFO ile) beklenen kalanlar
        var beklenen = new Dictionary<string, int>
        {
            ["L-R-2501"] = 970, ["L-LED-2412"] = 0, ["L-LED-2602"] = 196, ["L-D-2601"] = 18,
            ["L-D-2603"] = 50, ["L-PCB-IP-01"] = 19, ["L-PCB-RK-01"] = 12, ["L-KON-2511"] = 50,
            ["L-KON-2602"] = 88, ["L-ROLE-2602"] = 24,
        };
        var kalanlar = await Db.StokLotlari.ToDictionaryAsync(l => l.LotNo, l => l.KalanAdet);
        Assert.Equal(beklenen, kalanlar);
    }

    [Fact]
    public async Task Geri_cagrilan_lot_hic_kullanilmamis_ve_led_lotu_degisimi_dogru()
    {
        await OrnekVeriYukle();
        var geri = await Db.StokLotlari.SingleAsync(l => l.GeriCagrildi);
        Assert.Equal("L-KON-2511", geri.LotNo);
        Assert.False(await Db.UretimTuketimleri.AnyAsync(t => t.StokLotuId == geri.Id));

        var ledLotlari = await Db.UretimTuketimleri
            .Where(t => t.StokLotu!.Parca!.Kod == "LED-KR")
            .Select(t => new { t.Uretim!.SeriNo, t.StokLotu!.LotNo })
            .ToDictionaryAsync(x => x.SeriNo, x => x.LotNo);
        Assert.Equal("L-LED-2412", ledLotlari["SN-IP-0005"]);
        Assert.Equal("L-LED-2602", ledLotlari["SN-IP-0006"]);
    }

    [Fact]
    public async Task Kayitlarda_islemi_yapan_kullanici_ve_zaman_tutulur()
    {
        await OrnekVeriYukle();
        var admin = await Db.Kullanicilar.SingleAsync();
        Assert.Equal((OrnekVeri.AdminKullaniciAdi, KullaniciRolu.Admin), (admin.KullaniciAdi, admin.Rol));
        Assert.True(BCrypt.Net.BCrypt.Verify(TestVeritabani.AdminSifre, admin.SifreHash));
        Assert.NotEqual(TestVeritabani.AdminSifre, admin.SifreHash);
        Assert.True(await Db.StokLotlari.AllAsync(l => l.OlusturanKullaniciId == admin.Id));
        Assert.True(await Db.Uretimler.AllAsync(u => u.OlusturanKullaniciId == admin.Id));
        Assert.True(await Db.Satislar.AllAsync(s => s.OlusturanKullaniciId == admin.Id));
    }

    [Fact]
    public async Task Veri_varken_durur_sifirla_ile_yeniden_yukler()
    {
        await OrnekVeriYukle();
        await Assert.ThrowsAsync<InvalidOperationException>(OrnekVeriYukle);
        Db.ChangeTracker.Clear();
        var s = await OrnekVeri.Olustur(Db).YukleAsync(adminSifre: null, sifirla: true);
        Assert.Equal(9, s.Uretim);
        Assert.Equal(10, await Db.StokLotlari.CountAsync());
        Assert.Equal(1, await Db.Kullanicilar.CountAsync());  // kullanıcılar silinmez
    }

    [Fact]
    public async Task Admin_yoksa_sifre_zorunlu()
    {
        var hata = await Assert.ThrowsAsync<InvalidOperationException>(
            () => OrnekVeri.Olustur(Db).YukleAsync(adminSifre: " ", sifirla: false));
        Assert.Contains("AdminSifre", hata.Message);
        Assert.False(await Db.Parcalar.AnyAsync());
    }
}

public class KisitTestleri(TestVeritabani vt) : VeritabaniTesti(vt)
{
    private async Task<Kullanici> Kullanici()
    {
        var k = new Kullanici { Ad = "Test", Soyad = "Kişi", KullaniciAdi = "test", SifreHash = "x" };
        Db.Kullanicilar.Add(k);
        await Db.SaveChangesAsync();
        return k;
    }

    private async Task<StokLotu> Lot(string lotNo = "L-1", int adet = 10)
    {
        var k = await Db.Kullanicilar.FirstOrDefaultAsync() ?? await Kullanici();
        var parca = await Db.Parcalar.FirstOrDefaultAsync() ?? new Parca { Kod = "P1", Ad = "Parça" };
        var ted = await Db.Tedarikciler.FirstOrDefaultAsync() ?? new Tedarikci { Ad = "Ted" };
        var lot = new StokLotu
        {
            LotNo = lotNo, Parca = parca, Tedarikci = ted, SiparisTarihi = new(2026, 1, 1),
            GirisAdet = adet, KalanAdet = adet, GirisZamani = DateTimeOffset.Now,
            OlusturanKullanici = k, OlusturmaZamani = DateTimeOffset.Now,
        };
        Db.StokLotlari.Add(lot);
        await Db.SaveChangesAsync();
        return lot;
    }

    /// <summary>Kayıt veritabanı tarafından reddedilmeli; context sonraki adımlar için temizlenir.</summary>
    private async Task Reddedilir(string beklenenKisit)
    {
        var hata = await Assert.ThrowsAsync<DbUpdateException>(() => Db.SaveChangesAsync());
        Assert.Contains(beklenenKisit, (hata.InnerException as SqlException)?.Message ?? hata.Message);
        Db.ChangeTracker.Clear();
    }

    [Fact]
    public async Task Kodlar_buyuk_kucuk_harf_fark_etmeden_benzersiz()
    {
        Db.Parcalar.Add(new Parca { Kod = "SN-IP", Ad = "A" });
        await Db.SaveChangesAsync();
        Db.Parcalar.Add(new Parca { Kod = "sn-ip", Ad = "B" });  // Türkçe kuralda "farklı" sayılırdı
        await Reddedilir("IX_Parca_Kod");
    }

    [Theory]
    [InlineData("LotNo")]
    [InlineData("SeriNo")]
    [InlineData("KullaniciAdi")]
    [InlineData("SeriOneki")]
    public async Task Benzersiz_alanlar_tekrarlanamaz(string alan)
    {
        var k = await Kullanici();
        var urun = new Urun { Kod = "U1", Ad = "Ürün", SeriOneki = "SN-U-" };
        Db.Urunler.Add(urun);
        await Db.SaveChangesAsync();
        switch (alan)
        {
            case "LotNo":
                await Lot("L-AYNI");
                Db.StokLotlari.Add(new StokLotu
                {
                    LotNo = "l-ayni", ParcaId = (await Db.Parcalar.FirstAsync()).Id,
                    TedarikciId = (await Db.Tedarikciler.FirstAsync()).Id, SiparisTarihi = new(2026, 1, 1),
                    GirisAdet = 1, KalanAdet = 1, OlusturanKullaniciId = k.Id,
                });
                await Reddedilir("IX_StokLotu_LotNo");
                break;
            case "SeriNo":
                Db.Uretimler.Add(new Uretim { SeriNo = "SN-U-0001", UrunId = urun.Id, OlusturanKullaniciId = k.Id });
                await Db.SaveChangesAsync();
                Db.Uretimler.Add(new Uretim { SeriNo = "sn-u-0001", UrunId = urun.Id, OlusturanKullaniciId = k.Id });
                await Reddedilir("IX_Uretim_SeriNo");
                break;
            case "KullaniciAdi":
                Db.Kullanicilar.Add(new Kullanici { Ad = "B", Soyad = "B", KullaniciAdi = "TEST", SifreHash = "y" });
                await Reddedilir("IX_Kullanici_KullaniciAdi");
                break;
            case "SeriOneki":
                Db.Urunler.Add(new Urun { Kod = "U2", Ad = "Başka", SeriOneki = "sn-u-" });
                await Reddedilir("IX_Urun_SeriOneki");
                break;
        }
    }

    [Fact]
    public async Task Kucuk_harfle_arama_buyuk_harfli_kaydi_bulur()
    {
        await OrnekVeriYukle();
        Assert.NotNull(await Db.Uretimler.SingleOrDefaultAsync(u => u.SeriNo == "sn-ip-0001"));
        Assert.NotNull(await Db.StokLotlari.SingleOrDefaultAsync(l => l.LotNo == "l-kon-2511"));
        Assert.NotNull(await Db.Parcalar.SingleOrDefaultAsync(p => p.Ad == "diyot 1n4007"));
    }

    [Fact]
    public async Task Kalan_adet_negatif_olamaz_ama_sayim_fazlasi_girisi_asabilir()
    {
        var lot = await Lot();
        lot.KalanAdet = 15;  // + düzeltme sonrası giriş adedini (10) aşabilir
        await Db.SaveChangesAsync();
        lot.KalanAdet = -1;
        await Reddedilir("CK_StokLotu_KalanAdet");
    }

    [Fact]
    public async Task Adet_ve_aciklama_kurallari()
    {
        var lot = await Lot();
        var k = await Db.Kullanicilar.FirstAsync();

        Db.StokDuzeltmeleri.Add(new StokDuzeltme { StokLotuId = lot.Id, Miktar = 0, Tur = StokDuzeltmeTuru.Fire,
                                                  Aciklama = "fire", OlusturanKullaniciId = k.Id });
        await Reddedilir("CK_StokDuzeltme_Miktar");

        Db.StokDuzeltmeleri.Add(new StokDuzeltme { StokLotuId = lot.Id, Miktar = -2, Tur = StokDuzeltmeTuru.Fire,
                                                  Aciklama = "", OlusturanKullaniciId = k.Id });
        await Reddedilir("CK_StokDuzeltme_Aciklama");

        var urun = new Urun { Kod = "U1", Ad = "Ürün", SeriOneki = "SN-U-" };
        urun.Agac.Add(new UrunAgaci { Urun = urun, ParcaId = lot.ParcaId, Adet = 0 });
        Db.Urunler.Add(urun);
        await Reddedilir("CK_UrunAgaci_Adet");
    }

    [Fact]
    public async Task Enumlar_ad_olarak_saklanir()
    {
        var lot = await Lot();
        Db.StokDuzeltmeleri.Add(new StokDuzeltme { StokLotuId = lot.Id, Miktar = -1, Tur = StokDuzeltmeTuru.SayimFarki,
                                                  Aciklama = "sayım", OlusturanKullaniciId = lot.OlusturanKullaniciId });
        await Db.SaveChangesAsync();
        var tur = await Db.Database.SqlQueryRaw<string>("SELECT Tur AS Value FROM StokDuzeltme").SingleAsync();
        Assert.Equal("SayimFarki", tur);
    }

    [Fact]
    public async Task Bagli_kayitlar_silinemez()
    {
        await OrnekVeriYukle();
        // API'deki gibi temiz bağlam: kısıtı EF değil veritabanı uygulamalı
        Db.ChangeTracker.Clear();
        Db.Parcalar.Remove(await Db.Parcalar.FirstAsync(p => p.Kod == "R-10K"));  // lotu var
        await Reddedilir("REFERENCE constraint");

        Db.StokLotlari.Remove(await Db.StokLotlari.FirstAsync(l => l.LotNo == "L-D-2601"));  // üretimde kullanıldı
        await Reddedilir("REFERENCE constraint");

        Db.Uretimler.Remove(await Db.Uretimler.FirstAsync(u => u.SeriNo == "SN-IP-0001"));  // satıldı, tüketimi var
        await Reddedilir("REFERENCE constraint");

        Db.Urunler.Remove(await Db.Urunler.FirstAsync(u => u.Kod == "IP"));  // üretimi var
        await Reddedilir("REFERENCE constraint");
    }

    [Fact]
    public async Task Uretimi_olmayan_urun_silinince_agaci_da_silinir()
    {
        var lot = await Lot();
        var urun = new Urun { Kod = "U1", Ad = "Ürün", SeriOneki = "SN-U-" };
        urun.Agac.Add(new UrunAgaci { Urun = urun, ParcaId = lot.ParcaId, Adet = 2 });
        Db.Urunler.Add(urun);
        await Db.SaveChangesAsync();

        await Db.Urunler.Where(u => u.Id == urun.Id).ExecuteDeleteAsync();
        Assert.False(await Db.UrunAgaclari.AnyAsync());
        Assert.True(await Db.Parcalar.AnyAsync());  // parça kalır
    }

    [Fact]
    public async Task Bir_seri_no_iki_kez_satilamaz()
    {
        await OrnekVeriYukle();
        // Temiz bağlam şart: eski kalem bellekte izlenirken EF bire bir ilişkide onu sessizce
        // silip yenisini yazar. Bu yüzden satış servisi "daha önce satıldı mı"yı açıkça kontrol eder.
        Db.ChangeTracker.Clear();
        var satilmis = await Db.SatisKalemleri.AsNoTracking().FirstAsync();
        var musteri = await Db.Musteriler.AsNoTracking().FirstAsync();
        var admin = await Db.Kullanicilar.AsNoTracking().FirstAsync();
        var satis = new Satis { MusteriId = musteri.Id, Tarih = new(2026, 6, 1), OlusturanKullaniciId = admin.Id };
        satis.Kalemler.Add(new SatisKalemi { Satis = satis, UretimId = satilmis.UretimId });
        Db.Satislar.Add(satis);
        await Reddedilir("IX_SatisKalemi_UretimId");
    }
}

/// <summary>
/// Eşzamanlılık: iki bağlantı gerçekten ayrı olmalı, bu yüzden bu test transaction
/// içinde değil, kendi kaydını oluşturup sonunda siler.
/// </summary>
[Collection(VeritabaniKoleksiyonu.Ad)]
public class EszamanlilikTestleri(TestVeritabani vt)
{
    [Fact]
    public async Task Ayni_lot_iki_islemde_ayni_anda_degistirilemez()
    {
        int lotId;
        await using (var kur = vt.YeniContext())
        {
            var k = new Kullanici { Ad = "E", Soyad = "Z", KullaniciAdi = "eszamanli", SifreHash = "x" };
            var lot = new StokLotu
            {
                LotNo = "L-ESZAMANLI", Parca = new Parca { Kod = "P-ESZ", Ad = "P" }, Tedarikci = new Tedarikci { Ad = "T-ESZ" },
                SiparisTarihi = new(2026, 1, 1), GirisAdet = 10, KalanAdet = 10, OlusturanKullanici = k,
            };
            kur.StokLotlari.Add(lot);
            await kur.SaveChangesAsync();
            lotId = lot.Id;
        }

        try
        {
            await using var birinci = vt.YeniContext();
            await using var ikinci = vt.YeniContext();
            var a = await birinci.StokLotlari.SingleAsync(l => l.Id == lotId);
            var b = await ikinci.StokLotlari.SingleAsync(l => l.Id == lotId);

            a.KalanAdet -= 4;
            await birinci.SaveChangesAsync();

            b.KalanAdet -= 4;  // eski RowVersion ile: üstüne yazmamalı
            await Assert.ThrowsAsync<DbUpdateConcurrencyException>(() => ikinci.SaveChangesAsync());

            await using var kontrol = vt.YeniContext();
            Assert.Equal(6, (await kontrol.StokLotlari.SingleAsync(l => l.Id == lotId)).KalanAdet);
        }
        finally
        {
            await using var temizle = vt.YeniContext();
            await temizle.StokLotlari.Where(l => l.Id == lotId).ExecuteDeleteAsync();
            await temizle.Parcalar.Where(p => p.Kod == "P-ESZ").ExecuteDeleteAsync();
            await temizle.Tedarikciler.Where(t => t.Ad == "T-ESZ").ExecuteDeleteAsync();
            await temizle.Kullanicilar.Where(k => k.KullaniciAdi == "eszamanli").ExecuteDeleteAsync();
        }
    }
}
