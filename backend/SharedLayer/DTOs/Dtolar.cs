namespace SharedLayer.DTOs;

// İş servislerinin döndürdüğü ve API'nin istemcilere ilettiği veri sınıfları.
// Entity'lere bağlı değildir (katman kuralı).

// --- Ortak --------------------------------------------------------------------

public record ParcaOzetDto(int Id, string Kod, string Ad, string Birim);

public record LotDto(
    int Id, string LotNo, int ParcaId, string ParcaKod, string ParcaAd, string TedarikciAd,
    DateOnly SiparisTarihi, string SiparisNo, int GirisAdet, int KalanAdet, bool GeriCagrildi,
    DateTimeOffset GirisZamani, bool Sirada = false, bool Sorunlu = false, string SorunAciklama = "",
    string TedarikciLotNo = "")
{
    /// <summary>Üretime kapalı (sorunlu lotta kalan parçalar kullanılmasın seçildi). GeriCagrildi ile aynı.</summary>
    public bool UretimeKapali => GeriCagrildi;
}

public record SatisBilgisiDto(int SatisId, DateOnly Tarih, int MusteriId, string MusteriAd, string MusteriIletisim);

// --- Stok ---------------------------------------------------------------------

public record ParcaStokDto(
    int Id, string Kod, string Ad, string Birim, int MinStok,
    int ToplamStok, int KullanilabilirStok, int LotSayisi, IReadOnlyList<string> KullanildigiUrunler, int HataliAdet = 0)
{
    /// <summary>Geri çağrılanlar hariç kullanılabilir stok minimumun altında.</summary>
    public bool MinAltinda => KullanilabilirStok < MinStok;
}

/// <summary>Bir parça ve lotları (FIFO sırasıyla); Siradaki üretimde ilk kullanılacak lot.</summary>
public record StokGrubuDto(ParcaStokDto Parca, IReadOnlyList<LotDto> Lotlar, LotDto? Siradaki);

public record MalGirisiIstek(
    int ParcaId, string TedarikciAdi, string? LotNo, int Adet, DateOnly SiparisTarihi, string SiparisNo = "",
    string TedarikciLotNo = "");

public record MalGirisiSonucuDto(LotDto Lot, bool YeniTedarikci);

public record StokDuzeltmeIstek(int StokLotuId, int Miktar, string Tur, string Aciklama);

/// <summary>Bir stok girişinden hatalı çıkan parça adedi; StoktanDus=false ise yalnız sayı kaydedilir.</summary>
public record StokHataliIstek(int StokLotuId, int Adet, bool StoktanDus, string Aciklama);

public record StokDuzeltmeSonucuDto(int Id, string LotNo, int Miktar, string Tur, string Aciklama, int YeniKalanAdet);

// --- Üretim -------------------------------------------------------------------

public record LotDusumuDto(int LotId, string LotNo, int Adet, bool Sorunlu = false);

public record ParcaIhtiyaciDto(
    ParcaOzetDto Parca, int BirimAdet, int Gereken, int Kullanilabilir,
    IReadOnlyList<LotDusumuDto> Lotlar, IReadOnlyList<string> AtlananLotlar)
{
    public int Eksik => Math.Max(0, Gereken - Kullanilabilir);
}

public record UretimOnizlemeDto(
    int UrunId, string UrunKod, string UrunAd, int Adet,
    IReadOnlyList<ParcaIhtiyaciDto> Parcalar, IReadOnlyList<string> SeriNolar, string LotNo = "")
{
    public IReadOnlyList<ParcaIhtiyaciDto> Eksikler => [.. Parcalar.Where(p => p.Eksik > 0)];
    public bool Yeterli => Eksikler.Count == 0;
    /// <summary>Atlanacak (kalanı olan) üretime kapalı sorunlu lotlar.</summary>
    public IReadOnlyList<string> AtlananLotlar => [.. Parcalar.SelectMany(p => p.AtlananLotlar).Distinct().Order()];
    /// <summary>Sorunlu olduğu hâlde üretimde kullanılmasına izin verilmiş, bu üretimde kullanılacak lotlar.</summary>
    public IReadOnlyList<string> SorunluKullanilanLotlar =>
        [.. Parcalar.SelectMany(p => p.Lotlar).Where(l => l.Sorunlu).Select(l => l.LotNo).Distinct().Order()];
}

public record EksikParcaDto(ParcaOzetDto Parca, int Gereken, int Kullanilabilir, int Eksik);

public record UretimSonucuDto(int UrunId, string UrunAd, IReadOnlyList<string> SeriNolar, string LotNo = "");

/// <summary>Üretim geçmişinde bir seri no. MusteriAd/SatisTarihi satılmadıysa, Hurda ayrılmadıysa null.</summary>
public record UretimGecmisiDto(
    int UretimId, string SeriNo, int UrunId, string UrunAd, DateTimeOffset Tarih,
    string? MusteriAd, DateOnly? SatisTarihi, IReadOnlyList<string> GeriCagrilanLotlar, HurdaBilgisiDto? Hurda,
    string? GeriCagirmaDurumu = null, string LotNo = "");

/// <summary>Tur: "Hurda" veya "Kayip".</summary>
public record HurdaBilgisiDto(string Tur, string Aciklama, DateTimeOffset Zaman, string KullaniciAd);

/// <summary>Depodaki seri numaralarını hurdaya / kayba ayırma. Tur: "Hurda" veya "Kayip"; açıklama zorunlu.</summary>
public record UretimHurdaIstek(IReadOnlyList<string> SeriNolar, string Tur, string Aciklama);

public record UretimHurdaSonucuDto(IReadOnlyList<string> SeriNolar, string Tur, string Aciklama);

// --- Sorunlu lot ve ürün geri çağırma -------------------------------------------

/// <summary>Sorunlu=false sorunu kaldırır. UretimdeKullan: kalan parçalar üretimde kullanılsın mı.</summary>
/// <summary>GeriCagrilacaklar: geri çağrılacak seri numaraları (bu lottan parça taşıyanlardan); boşsa hiçbiri.</summary>
public record LotSorunIstek(bool Sorunlu, bool UretimdeKullan, string? Aciklama, IReadOnlyList<string>? GeriCagrilacaklar = null);

/// <summary>GeriCagrilanSeriNolar: bu işlemde seçilip geri çağrılan ürünler.</summary>
public record LotSorunSonucuDto(LotDto Lot, IReadOnlyList<string> GeriCagrilanSeriNolar);

/// <summary>Seri numaralarını geri çağırma; LotNo: sebep olan sorunlu lot (isteğe bağlı).</summary>
public record GeriCagirmaIstek(IReadOnlyList<string> SeriNolar, string Aciklama, string? LotNo = null);

/// <summary>Durum: musteride, geri_alindi, depoda, hurda, satisa_acildi.</summary>
public record GeriCagirmaDto(
    int Id, string SeriNo, string UrunAd, string? LotNo, string Aciklama, DateTimeOffset Zaman, string KullaniciAd,
    string Durum, string? MusteriAd, string? MusteriIletisim, DateTimeOffset? GeriAlinmaZamani, DateTimeOffset? KapanmaZamani,
    DateTimeOffset? BilgiVerildiZamani = null);

public record GeriCagirmaHurdaIstek(string Aciklama);

// --- Ürün lotu ----------------------------------------------------------------

/// <summary>Bir ürün lotu (aynı anda üretilenler). Depoda: satılmamış ve hurdaya ayrılmamış.</summary>
public record UrunLotDto(
    string LotNo, int UrunId, string UrunAd, DateTimeOffset Tarih, int Toplam, int Depoda, int Satilan, int HurdaKayip,
    int AcikGeriCagirma, bool Sorunlu, string SorunAciklama, int HataliAdet, DateTimeOffset? SorunZamani);

public record UrunLotSorunIstek(bool Sorunlu, string? Aciklama, int HataliAdet = 0, IReadOnlyList<string>? GeriCagrilacaklar = null);

public record UrunLotSorunSonucuDto(UrunLotDto Lot, IReadOnlyList<string> GeriCagrilanSeriNolar);

// --- İzlenebilirlik -----------------------------------------------------------

/// <summary>HurdaTuru: "Hurda", "Kayip" veya null (ayrılmadı).</summary>
public record LotIzUretimDto(
    int UretimId, string SeriNo, DateTimeOffset Tarih, string UrunKod, string UrunAd, int Adet,
    SatisBilgisiDto? Satis, string? HurdaTuru = null, string? GeriCagirmaDurumu = null);

public record MusteriSatisiDto(string SeriNo, DateOnly Tarih);

public record LotIzMusteriDto(int Id, string Ad, string Iletisim, IReadOnlyList<MusteriSatisiDto> Satislar)
{
    public IReadOnlyList<string> SeriNolar => [.. Satislar.Select(s => s.SeriNo)];
}

public record LotIzDto(
    LotDto Lot, int KullanilanAdet, IReadOnlyList<LotIzUretimDto> Uretimler,
    IReadOnlyList<LotIzMusteriDto> Musteriler)
{
    public IReadOnlyList<LotIzUretimDto> StoktakiUretimler => [.. Uretimler.Where(u => u.Satis is null && u.HurdaTuru is null)];
}

public record SeriIzParcaDto(ParcaOzetDto Parca, LotDto Lot, int Adet);

public record SeriIzDto(
    int UretimId, string SeriNo, DateTimeOffset Tarih, string UrunKod, string UrunAd,
    IReadOnlyList<SeriIzParcaDto> Parcalar, SatisBilgisiDto? Satis, HurdaBilgisiDto? Hurda = null,
    IReadOnlyList<IadeBilgisiDto>? Iadeler = null, GeriCagirmaDto? GeriCagirma = null, string LotNo = "")
{
    /// <summary>İçindeki sorunlu lotlar (alan adı eski).</summary>
    public IReadOnlyList<string> GeriCagrilanLotlar =>
        [.. Parcalar.Where(p => p.Lot.Sorunlu).Select(p => p.Lot.LotNo).Distinct().Order()];
}

/// <summary>Tur: "lot", "seri", "parca" veya null (bulunamadı / adaylar).</summary>
public record NumaraAramaDto(string? Tur, string? Deger, IReadOnlyList<AdayDto> Adaylar);

public record AdayDto(string Tur, string Deger);

/// <summary>Tur: "giris", "uretim", "satis". NumaraTuru: "lot" (stok giriş no), "seri" veya "urunlot".</summary>
public record HareketDto(
    string Tur, DateOnly Tarih, string Aciklama, string Numara, string NumaraIlk, string NumaraTuru);

// --- Satış --------------------------------------------------------------------

public record SatilabilirDto(int UretimId, string SeriNo, string UrunAd, IReadOnlyList<string> GeriCagrilanLotlar);

public record SatisUyarisiDto(string SeriNo, IReadOnlyList<string> Lotlar);

/// <summary>GeriCagrilanOnayi: geri çağrılan lot içeren ürünler varsa satış ancak true iken yapılır.</summary>
public record SatisIstek(
    string MusteriAdi, IReadOnlyList<string> SeriNolar, DateOnly? Tarih = null, bool GeriCagrilanOnayi = false,
    DateTimeOffset? Zaman = null);

public record SatisSonucuDto(
    int SatisId, int MusteriId, string MusteriAd, bool YeniMusteri, DateOnly Tarih,
    IReadOnlyList<string> SeriNolar, IReadOnlyList<SatisUyarisiDto> Uyarilar);

/// <summary>SeriNolar: hâlâ satılmış olanlar; IadeEdilenler: bu satıştan iade alınanlar.</summary>
/// <summary>Bir satışta bir ürün. Kalemler: satılan seri numaraları ürün lotlarıyla.</summary>
public record SatisGecmisiDto(int SatisId, DateOnly Tarih, string MusteriAd, string UrunAd, IReadOnlyList<string> SeriNolar,
                              IReadOnlyList<string> IadeEdilenler, IReadOnlyList<SatisKalemiDto>? Kalemler = null,
                              DateTimeOffset? Zaman = null);

public record SatisKalemiDto(string SeriNo, string LotNo);

/// <summary>Satıştan iade: seri numaraları ve zorunlu açıklama.</summary>
public record SatisIadeIstek(IReadOnlyList<string> SeriNolar, string Aciklama);

public record SatisIadeSonucuDto(int SatisId, IReadOnlyList<string> SeriNolar);

/// <summary>Seri no'nun geçmişteki bir satışından iade.</summary>
public record IadeBilgisiDto(string MusteriAd, DateOnly SatisTarihi, DateTimeOffset IadeZamani, string Aciklama, string KullaniciAd);

// --- İşlem geçmişi ------------------------------------------------------------

/// <summary>Tur: giris, uretim, satis, iade, duzeltme, hurda, sorun, geri. NumaraTuru: "lot", "seri" veya null.</summary>
public record IslemDto(DateTimeOffset Zaman, string Tur, string KullaniciAd, string Aciklama, string? Numara, string? NumaraTuru);
