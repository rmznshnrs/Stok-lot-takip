namespace BusinessLayer.Abstract;

/// <summary>Müşteri siparişleri: ürün ve adet; depodaki hazır ürünler ayrılır, eksikler üretim sırasına eklenir.</summary>
public interface ISiparisServisi
{
    /// <summary>Yeniden eskiye. acik: yalnız bitmemiş (iptal edilmemiş, hepsi gönderilmemiş) siparişler.</summary>
    /// <summary>baslangic verilirse o tarihten sonraki siparişlerin hepsi (sınırsız; rapor için).</summary>
    Task<List<SiparisDto>> ListeAsync(int? musteriId = null, bool acik = false, CancellationToken ct = default,
                                      DateOnly? baslangic = null);

    Task<SiparisDto> GetirAsync(int id, CancellationToken ct = default);

    Task<SiparisDto> OlusturAsync(SiparisIstek istek, int kullaniciId, CancellationToken ct = default);

    /// <summary>Depodaki serbest ürünleri kaleme ayırır (en fazla kalemin adedi kadar).</summary>
    Task<SiparisDto> SeriAyirAsync(int kalemId, SeriAyirIstek istek, CancellationToken ct = default);

    /// <summary>Gönderilmemiş bir ürünün ayrımını kaldırır; ürün serbest stoğa döner.</summary>
    Task<SiparisDto> SeriBirakAsync(int kalemId, string seriNo, CancellationToken ct = default);

    /// <summary>Gönderilmeyen ayrılmış ürünler serbest kalır, bekleyen üretimler kapatılır.</summary>
    Task<SiparisDto> IptalAsync(int id, int kullaniciId, CancellationToken ct = default);

    /// <summary>Ürünün depoda serbest (satılmamış, ayrılmamış, hurda ve geri çağrılmamış) seri numaraları.</summary>
    Task<List<HazirSeriDto>> HazirSerilerAsync(int urunId, CancellationToken ct = default);
}
