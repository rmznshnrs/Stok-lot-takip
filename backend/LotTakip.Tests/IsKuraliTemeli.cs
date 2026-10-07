using LotTakip.Business;
using LotTakip.DataAccess;
using LotTakip.Entity;
using Microsoft.EntityFrameworkCore;

namespace LotTakip.Tests;

/// <summary>
/// Ortak başlangıç verisi: İndikatör Paneli = 4 LED + 1 diyot.
/// LED tek lot (100); diyot üç lot: YENI (2026-03-01, 10), ESKI (2026-01-01, 3),
/// GERI (2025-06-01, 50, geri çağrılmış). Sıra kasıtlı karışık: FIFO id'ye değil sipariş tarihine bakmalı.
/// </summary>
public abstract class IsKuraliTesti(TestVeritabani vt) : VeritabaniTesti(vt)
{
    protected Kullanici Kullanici = null!;
    protected Kullanici Admin = null!;
    protected Tedarikci Ted = null!;
    protected Parca Led = null!, Diyot = null!;
    protected Urun Urun = null!;
    protected StokLotu LedLot = null!, DiyotYeni = null!, DiyotEski = null!, DiyotGeri = null!;
    protected Musteri Musteri = null!;

    protected IUretimServisi Uretim = null!;
    protected ISatisServisi Satis = null!;
    protected IStokServisi Stok = null!;
    protected IStokDuzeltmeServisi Duzeltme = null!;
    protected IIzlemeServisi Izleme = null!;

    public override async Task InitializeAsync()
    {
        await base.InitializeAsync();
        Uretim = new UretimServisi(Db, new StokLotuRepository(Db), new UretimRepository(Db));
        Satis = new SatisServisi(Db);
        Stok = new StokServisi(Db);
        Duzeltme = new StokDuzeltmeServisi(Db);
        Izleme = new IzlemeServisi(Db);

        Kullanici = new Kullanici { Ad = "Depo", Soyad = "Görevlisi", KullaniciAdi = "depo", SifreHash = "x" };
        Admin = new Kullanici { Ad = "Ad", Soyad = "Min", KullaniciAdi = "yonetici", SifreHash = "x", Rol = KullaniciRolu.Admin };
        Ted = new Tedarikci { Ad = "Tedarikçi A" };
        Led = new Parca { Kod = "LED", Ad = "LED" };
        Diyot = new Parca { Kod = "DIY", Ad = "Diyot" };
        Urun = new Urun { Kod = "IP", Ad = "İndikatör Paneli", SeriOneki = "SN-IP-" };
        Urun.Agac.Add(new UrunAgaci { Urun = Urun, Parca = Led, Adet = 4 });
        Urun.Agac.Add(new UrunAgaci { Urun = Urun, Parca = Diyot, Adet = 1 });
        Musteri = new Musteri { Ad = "Atlas" };
        Db.AddRange(Kullanici, Admin, Ted, Urun, Musteri);
        await Db.SaveChangesAsync();

        LedLot = await LotEkle("L-LED", Led, new(2026, 1, 1), 100);
        DiyotYeni = await LotEkle("L-D-YENI", Diyot, new(2026, 3, 1), 10);
        DiyotEski = await LotEkle("L-D-ESKI", Diyot, new(2026, 1, 1), 3);
        DiyotGeri = await LotEkle("L-D-GERI", Diyot, new(2025, 6, 1), 50, geri: true);
        // API'deki gibi temiz bağlamla başla: servisler bellekteki eski değerleri görmesin
        Db.ChangeTracker.Clear();
    }

    protected async Task<StokLotu> LotEkle(string lotNo, Parca parca, DateOnly tarih, int adet, bool geri = false)
    {
        // Kimliklerle bağla: bağlam temizlendikten sonra da tedarikçi/kullanıcı yeniden eklenmesin
        var lot = new StokLotu
        {
            LotNo = lotNo, ParcaId = parca.Id, TedarikciId = Ted.Id, SiparisTarihi = tarih, GirisAdet = adet,
            KalanAdet = adet, GeriCagrildi = geri, GirisZamani = Zaman.Simdi(),
            OlusturanKullaniciId = Kullanici.Id, OlusturmaZamani = Zaman.Simdi(),
        };
        Db.StokLotlari.Add(lot);
        await Db.SaveChangesAsync();
        return lot;
    }

    /// <summary>Lotun veritabanındaki güncel kalanı (bağlamdaki eski değere değil).</summary>
    protected Task<int> Kalan(StokLotu lot) =>
        Db.StokLotlari.AsNoTracking().Where(l => l.Id == lot.Id).Select(l => l.KalanAdet).SingleAsync();

    protected async Task GeriCagir(StokLotu lot)
    {
        await Db.StokLotlari.Where(l => l.Id == lot.Id).ExecuteUpdateAsync(s => s.SetProperty(l => l.GeriCagrildi, true));
        Db.ChangeTracker.Clear();
    }
}
