namespace BusinessLayer.Abstract;

public interface ITanimServisi
{
    Task<ParcaOzetDto> ParcaOlusturAsync(ParcaIstek istek, CancellationToken ct = default);
    Task<ParcaOzetDto> ParcaGuncelleAsync(int id, ParcaIstek istek, CancellationToken ct = default);

    Task<List<UrunListeDto>> UrunleriListeleAsync(string? arama = null, CancellationToken ct = default);
    Task<UrunDetayDto?> UrunGetirAsync(int id, CancellationToken ct = default);
    Task<UrunDetayDto> UrunOlusturAsync(UrunIstek istek, CancellationToken ct = default);
    /// <summary>Üretimi olan ürünün seri öneki değiştirilemez.</summary>
    Task<UrunDetayDto> UrunGuncelleAsync(int id, UrunIstek istek, CancellationToken ct = default);
    /// <summary>
    /// Ürün ağacını verilen satırlarla değiştirir (eksik olan satırlar kaldırılır). Eski üretimler
    /// etkilenmez: hangi lottan kaç parça kullandıkları ayrıca saklanır.
    /// </summary>
    Task<UrunDetayDto> UrunAgaciKaydetAsync(int urunId, IReadOnlyList<UrunAgaciSatiriIstek> satirlar, CancellationToken ct = default);

    Task<List<IletisimDto>> TedarikcilerAsync(CancellationToken ct = default);
    Task<List<IletisimDto>> MusterilerAsync(CancellationToken ct = default);

    /// <summary>id null ise yeni müşteri. Ad boş olamaz ve tekildir (büyük/küçük harf duyarsız).</summary>
    Task<IletisimDto> MusteriKaydetAsync(int? id, IletisimIstek istek, CancellationToken ct = default);
    /// <summary>id null ise yeni tedarikçi. Ad boş olamaz ve tekildir.</summary>
    Task<IletisimDto> TedarikciKaydetAsync(int? id, IletisimIstek istek, CancellationToken ct = default);
}
