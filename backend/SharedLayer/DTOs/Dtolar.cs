namespace SharedLayer.DTOs;

public record ParcaOzetDto(int Id, string Kod, string Ad);

public record LotDto(
    int Id, string LotNo, int ParcaId, string ParcaKod, string ParcaAd, string TedarikciAd,
    DateOnly SiparisTarihi, string SiparisNo, int GirisAdet, int KalanAdet, bool GeriCagrildi,
    DateTimeOffset GirisZamani, bool Sirada = false, bool Sorunlu = false, string SorunAciklama = "",
    string TedarikciLotNo = "")
{
    public bool UretimeKapali => GeriCagrildi;
}

public record SatisBilgisiDto(int SatisId, DateOnly Tarih, int MusteriId, string MusteriAd, string MusteriIletisim);

public record ParcaStokDto(
    int Id, string Kod, string Ad, int MinStok,
    int ToplamStok, int KullanilabilirStok, int LotSayisi, IReadOnlyList<string> KullanildigiUrunler, int HataliAdet = 0,
    int Ayrilan = 0)
{
    public int Serbest => Math.Max(0, KullanilabilirStok - Ayrilan);

    public bool MinAltinda => MinStok > 0 && Serbest <= MinStok;
}

public record StokGrubuDto(ParcaStokDto Parca, IReadOnlyList<LotDto> Lotlar, LotDto? Siradaki);

public record MalGirisiIstek(
    int ParcaId, string TedarikciAdi, string? LotNo, int Adet, DateOnly SiparisTarihi, string SiparisNo = "",
    string TedarikciLotNo = "");

public record MalGirisiSonucuDto(LotDto Lot, bool YeniTedarikci);

public record StokDuzeltmeIstek(int StokLotuId, int Miktar, string Tur, string Aciklama);

public record StokHataliIstek(int StokLotuId, int Adet, bool StoktanDus, string Aciklama);

public record StokDuzeltmeSonucuDto(int Id, string LotNo, int Miktar, string Tur, string Aciklama, int YeniKalanAdet);

public record LotDusumuDto(int LotId, string LotNo, int Adet, bool Sorunlu = false);

public record ParcaIhtiyaciDto(
    ParcaOzetDto Parca, int BirimAdet, int Gereken, int Kullanilabilir,
    IReadOnlyList<LotDusumuDto> Lotlar, IReadOnlyList<string> AtlananLotlar)
{
    public int Eksik => Math.Max(0, Gereken - Kullanilabilir);
}

public record UretimOnizlemeDto(
    int UrunId, string UrunKod, string UrunAd, int Adet,
    IReadOnlyList<ParcaIhtiyaciDto> Parcalar, IReadOnlyList<string> SeriNolar)
{
    public IReadOnlyList<ParcaIhtiyaciDto> Eksikler => [.. Parcalar.Where(p => p.Eksik > 0)];
    public bool Yeterli => Eksikler.Count == 0;
    public IReadOnlyList<string> AtlananLotlar => [.. Parcalar.SelectMany(p => p.AtlananLotlar).Distinct().Order()];
    public IReadOnlyList<string> SorunluKullanilanLotlar =>
        [.. Parcalar.SelectMany(p => p.Lotlar).Where(l => l.Sorunlu).Select(l => l.LotNo).Distinct().Order()];
}

public record EksikParcaDto(ParcaOzetDto Parca, int Gereken, int Kullanilabilir, int Eksik);

public record UretimSonucuDto(int UrunId, string UrunAd, IReadOnlyList<string> SeriNolar);

public record UretimGecmisiDto(
    int UretimId, string SeriNo, int UrunId, string UrunAd, DateTimeOffset Tarih,
    string? MusteriAd, DateOnly? SatisTarihi, IReadOnlyList<string> GeriCagrilanLotlar, HurdaBilgisiDto? Hurda,
    string? GeriCagirmaDurumu = null);

public record HurdaBilgisiDto(string Tur, string Aciklama, DateTimeOffset Zaman, string KullaniciAd);

public record UretimHurdaIstek(IReadOnlyList<string> SeriNolar, string Tur, string Aciklama);

public record UretimHurdaSonucuDto(IReadOnlyList<string> SeriNolar, string Tur, string Aciklama);

public record LotSorunIstek(bool Sorunlu, bool UretimdeKullan, string? Aciklama, IReadOnlyList<string>? GeriCagrilacaklar = null);

public record LotSorunSonucuDto(LotDto Lot, IReadOnlyList<string> GeriCagrilanSeriNolar);

public record GeriCagirmaIstek(IReadOnlyList<string> SeriNolar, string Aciklama, string? LotNo = null);

public record GeriCagirmaDto(
    int Id, string SeriNo, string UrunAd, string? LotNo, string Aciklama, DateTimeOffset Zaman, string KullaniciAd,
    string Durum, string? MusteriAd, string? MusteriIletisim, DateTimeOffset? GeriAlinmaZamani, DateTimeOffset? KapanmaZamani,
    DateTimeOffset? BilgiVerildiZamani = null);

public record GeriCagirmaHurdaIstek(string Aciklama);

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
    IReadOnlyList<IadeBilgisiDto>? Iadeler = null, GeriCagirmaDto? GeriCagirma = null)
{
    public IReadOnlyList<string> GeriCagrilanLotlar =>
        [.. Parcalar.Where(p => p.Lot.Sorunlu).Select(p => p.Lot.LotNo).Distinct().Order()];
}

public record NumaraAramaDto(string? Tur, string? Deger, IReadOnlyList<AdayDto> Adaylar);

public record AdayDto(string Tur, string Deger);

public record HareketDto(
    string Tur, DateOnly Tarih, string Aciklama, string Numara, string NumaraIlk, string NumaraTuru);

public record SatilabilirDto(int UretimId, string SeriNo, string UrunAd, IReadOnlyList<string> GeriCagrilanLotlar);

public record SatisUyarisiDto(string SeriNo, IReadOnlyList<string> Lotlar);

public record SatisIstek(
    string MusteriAdi, IReadOnlyList<string> SeriNolar, DateOnly? Tarih = null, bool GeriCagrilanOnayi = false,
    DateTimeOffset? Zaman = null);

public record SatisSonucuDto(
    int SatisId, int MusteriId, string MusteriAd, bool YeniMusteri, DateOnly Tarih,
    IReadOnlyList<string> SeriNolar, IReadOnlyList<SatisUyarisiDto> Uyarilar);

public record SatisGecmisiDto(int SatisId, DateOnly Tarih, string MusteriAd, string UrunAd, IReadOnlyList<string> SeriNolar,
                              IReadOnlyList<string> IadeEdilenler, IReadOnlyList<SatisKalemiDto>? Kalemler = null,
                              DateTimeOffset? Zaman = null);

public record SatisKalemiDto(string SeriNo);

public record SatisIadeIstek(IReadOnlyList<string> SeriNolar, string Aciklama);

public record SatisIadeSonucuDto(int SatisId, IReadOnlyList<string> SeriNolar);

public record IadeBilgisiDto(string MusteriAd, DateOnly SatisTarihi, DateTimeOffset IadeZamani, string Aciklama, string KullaniciAd);

public record IslemDto(DateTimeOffset Zaman, string Tur, string KullaniciAd, string Aciklama, string? Numara, string? NumaraTuru);

public record SiparisKalemiIstek(int UrunId, int Adet, IReadOnlyList<string>? SeriNolar = null);

public record SiparisIstek(int MusteriId, IReadOnlyList<SiparisKalemiIstek> Kalemler, DateOnly? Tarih = null,
                           DateOnly? IstenenTarih = null, string? Aciklama = null);

public record SiparisKalemiDto(
    int Id, int UrunId, string UrunKod, string UrunAd, int Adet,
    IReadOnlyList<string> Ayrilanlar, IReadOnlyList<string> Gonderilenler, int Uretimde)
{
    public int Eksik => Math.Max(0, Adet - Ayrilanlar.Count - Uretimde);
}

public record SiparisDto(
    int Id, string No, int MusteriId, string MusteriAd, DateOnly Tarih, DateOnly? IstenenTarih, string Aciklama,
    string Durum, IReadOnlyList<SiparisKalemiDto> Kalemler, DateTimeOffset OlusturmaZamani, string KullaniciAd,
    DateTimeOffset? IptalZamani);

public record SeriAyirIstek(IReadOnlyList<string> SeriNolar);

public record HazirSeriDto(string SeriNo, DateTimeOffset Tarih);

public record UretimEmriIstek(int UrunId, int? Adet, int? SiparisKalemiId = null, DateOnly? PlanlananTarih = null,
                              string? Aciklama = null);

public record EmirParcasiDto(ParcaOzetDto Parca, int BirimAdet, int Ayrilan);

public record UretimEmriDto(
    int Id, int Sira, int UrunId, string UrunKod, string UrunAd, int Adet, int Uretilen, string Durum,
    DateOnly PlanlananTarih, string Aciklama, int? SiparisId, string? SiparisNo, string? MusteriAd,
    DateTimeOffset OlusturmaZamani, string KullaniciAd, DateTimeOffset? BaslamaZamani, DateTimeOffset? BitisZamani,
    IReadOnlyList<EmirParcasiDto> Parcalar, IReadOnlyList<string> SeriNolar, int? PartiNo = null)
{
    public string? PartiAdi => SiparisNo is null ? null : $"{SiparisNo}/{PartiNo}";
}

public record EmirUretIstek(int? Adet = null);

public record EmirTasiIstek(int Yon);

public record EmirPlanIstek(DateOnly PlanlananTarih);

public record SevkiyatIstek(int SiparisId, IReadOnlyList<string> SeriNolar, string? KargoFirmasi = null, string? TakipNo = null,
                            string? Aciklama = null, bool GeriCagrilanOnayi = false);

public record KargoIstek(string KargoFirmasi, string TakipNo);

public record SevkiyatKalemiDto(string SeriNo, string UrunAd);

public record SevkiyatDto(
    int Id, string No, int MusteriId, string MusteriAd, int? SiparisId, string? SiparisNo, DateOnly Tarih, DateTimeOffset? Zaman,
    string Durum, string KargoFirmasi, string TakipNo, string Aciklama, DateTimeOffset? KargoZamani, DateTimeOffset? TeslimZamani,
    DateTimeOffset OlusturmaZamani, string KullaniciAd, IReadOnlyList<SevkiyatKalemiDto> Kalemler, IReadOnlyList<string> Iadeler);

public record UretilebilirParcaDto(ParcaOzetDto Parca, int BirimAdet, int Serbest);

public record UretilebilirUyariDto(int UrunId, string UrunKod, string UrunAd, int Uretilebilir,
                                   IReadOnlyList<UretilebilirParcaDto> Kisitlayanlar);

public record YaklasanTeslimDto(int SiparisId, string No, string MusteriAd, DateOnly IstenenTarih, int Kalan);

public record AnaSayfaOzetiDto(
    int AcikSiparis, int SiradakiIs, int SiradakiAdet, int BugunPlanlanan, int HazirlananSevkiyat, int KargodaSevkiyat,
    int DepodaUrun, int MinAltindaParca, int AcikGeriCagirma,
    IReadOnlyList<UretilebilirUyariDto> Uyarilar, IReadOnlyList<YaklasanTeslimDto> YaklasanTeslimler,
    IReadOnlyList<UrunKapasitesiDto> Kapasiteler, IReadOnlyList<ParcaStokDto> MinUyarilari);

public record UrunKapasitesiDto(int UrunId, string UrunAd, int Uretilebilir);

public record SiparisUrunuDto(
    string Durum, string UrunAd, int Adet, string? SeriNo, string? PartiAdi, DateOnly? PlanlananTarih, DateTimeOffset? UretimZamani,
    int? SevkiyatId, string? SevkiyatNo, string KargoFirmasi, string TakipNo, DateTimeOffset? KargoZamani, DateTimeOffset? TeslimZamani);

public record HaftalikUretimDto(DateOnly HaftaBasi, int Adet);

public record SiparisDurumSayisiDto(string Durum, int Sayi);

public record GecikenIsDto(string Tur, int Id, string No, string Ad, DateOnly Tarih, int Kalan, int? SiparisId);

public record UrunDagilimiDto(int UrunId, string UrunAd, int Gonderilen, int Depoda);

public record GrafikVerisiDto(
    IReadOnlyList<HaftalikUretimDto> HaftalikUretim, IReadOnlyList<SiparisDurumSayisiDto> SiparisDurumlari,
    IReadOnlyList<GecikenIsDto> Gecikenler, IReadOnlyList<UrunDagilimiDto> UrunDagilimi);

public record TakvimUrunuDto(int UrunId, string UrunAd, int Planlanan, int Uretilen, int Istenen);

public record TakvimPlaniDto(int Id, DateOnly Tarih, ParcaOzetDto? Parca, int? Adet, int? TedarikciId, string? TedarikciAd,
                             string Aciklama, bool Tamamlandi, string KullaniciAd);

public record TakvimPlaniIstek(DateOnly Tarih, int? ParcaId = null, int? Adet = null, int? TedarikciId = null, string? Aciklama = null);

public record TakvimGunuDto(DateOnly Tarih, IReadOnlyList<TakvimUrunuDto> Urunler, IReadOnlyList<TakvimPlaniDto> Planlar);
