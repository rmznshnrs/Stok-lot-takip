namespace LotTakip.Shared;

// İş servislerinin döndürdüğü ve API'nin istemcilere ilettiği veri sınıfları.
// Entity'lere bağlı değildir (katman kuralı).

// --- Ortak --------------------------------------------------------------------

public record ParcaOzetDto(int Id, string Kod, string Ad, string Birim);

public record LotDto(
    int Id, string LotNo, int ParcaId, string ParcaKod, string ParcaAd, string TedarikciAd,
    DateOnly SiparisTarihi, string SiparisNo, int GirisAdet, int KalanAdet, bool GeriCagrildi,
    DateTimeOffset GirisZamani, bool Sirada = false);

public record SatisBilgisiDto(int SatisId, DateOnly Tarih, int MusteriId, string MusteriAd, string MusteriIletisim);

// --- Stok ---------------------------------------------------------------------

public record ParcaStokDto(
    int Id, string Kod, string Ad, string Birim, int MinStok,
    int ToplamStok, int KullanilabilirStok, int LotSayisi, IReadOnlyList<string> KullanildigiUrunler)
{
    /// <summary>Geri çağrılanlar hariç kullanılabilir stok minimumun altında.</summary>
    public bool MinAltinda => KullanilabilirStok < MinStok;
}

/// <summary>Bir parça ve lotları (FIFO sırasıyla); Siradaki üretimde ilk kullanılacak lot.</summary>
public record StokGrubuDto(ParcaStokDto Parca, IReadOnlyList<LotDto> Lotlar, LotDto? Siradaki);

public record MalGirisiIstek(
    int ParcaId, string TedarikciAdi, string LotNo, int Adet, DateOnly SiparisTarihi, string SiparisNo = "");

public record MalGirisiSonucuDto(LotDto Lot, bool YeniTedarikci);

public record StokDuzeltmeIstek(int StokLotuId, int Miktar, string Tur, string Aciklama);

public record StokDuzeltmeSonucuDto(int Id, string LotNo, int Miktar, string Tur, string Aciklama, int YeniKalanAdet);

// --- Üretim -------------------------------------------------------------------

public record LotDusumuDto(int LotId, string LotNo, int Adet);

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
    /// <summary>Atlanacak (kalanı olan) geri çağrılmış lotlar.</summary>
    public IReadOnlyList<string> AtlananLotlar => [.. Parcalar.SelectMany(p => p.AtlananLotlar).Distinct().Order()];
}

public record EksikParcaDto(ParcaOzetDto Parca, int Gereken, int Kullanilabilir, int Eksik);

public record UretimSonucuDto(int UrunId, string UrunAd, IReadOnlyList<string> SeriNolar);

// --- İzlenebilirlik -----------------------------------------------------------

public record LotIzUretimDto(
    int UretimId, string SeriNo, DateTimeOffset Tarih, string UrunKod, string UrunAd, int Adet,
    SatisBilgisiDto? Satis);

public record MusteriSatisiDto(string SeriNo, DateOnly Tarih);

public record LotIzMusteriDto(int Id, string Ad, string Iletisim, IReadOnlyList<MusteriSatisiDto> Satislar)
{
    public IReadOnlyList<string> SeriNolar => [.. Satislar.Select(s => s.SeriNo)];
}

public record LotIzDto(
    LotDto Lot, int KullanilanAdet, IReadOnlyList<LotIzUretimDto> Uretimler,
    IReadOnlyList<LotIzMusteriDto> Musteriler)
{
    public IReadOnlyList<LotIzUretimDto> StoktakiUretimler => [.. Uretimler.Where(u => u.Satis is null)];
}

public record SeriIzParcaDto(ParcaOzetDto Parca, LotDto Lot, int Adet);

public record SeriIzDto(
    int UretimId, string SeriNo, DateTimeOffset Tarih, string UrunKod, string UrunAd,
    IReadOnlyList<SeriIzParcaDto> Parcalar, SatisBilgisiDto? Satis)
{
    public IReadOnlyList<string> GeriCagrilanLotlar =>
        [.. Parcalar.Where(p => p.Lot.GeriCagrildi).Select(p => p.Lot.LotNo).Distinct().Order()];
}

/// <summary>Tur: "lot", "seri", "parca" veya null (bulunamadı / adaylar).</summary>
public record NumaraAramaDto(string? Tur, string? Deger, IReadOnlyList<AdayDto> Adaylar);

public record AdayDto(string Tur, string Deger);

/// <summary>Tur: "giris", "uretim", "satis". NumaraTuru: "lot" veya "seri".</summary>
public record HareketDto(
    string Tur, DateOnly Tarih, string Aciklama, string Numara, string NumaraIlk, string NumaraTuru);

// --- Satış --------------------------------------------------------------------

public record SatilabilirDto(int UretimId, string SeriNo, string UrunAd, IReadOnlyList<string> GeriCagrilanLotlar);

public record SatisUyarisiDto(string SeriNo, IReadOnlyList<string> Lotlar);

/// <summary>GeriCagrilanOnayi: geri çağrılan lot içeren ürünler varsa satış ancak true iken yapılır.</summary>
public record SatisIstek(
    string MusteriAdi, IReadOnlyList<string> SeriNolar, DateOnly? Tarih = null, bool GeriCagrilanOnayi = false);

public record SatisSonucuDto(
    int SatisId, int MusteriId, string MusteriAd, bool YeniMusteri, DateOnly Tarih,
    IReadOnlyList<string> SeriNolar, IReadOnlyList<SatisUyarisiDto> Uyarilar);

public record SatisGecmisiDto(int SatisId, DateOnly Tarih, string MusteriAd, string UrunAd, IReadOnlyList<string> SeriNolar);
