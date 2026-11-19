namespace BusinessLayer.Abstract;

public interface ITanimServisi
{
    Task<ParcaOzetDto> ParcaOlusturAsync(ParcaIstek istek, CancellationToken ct = default);
    Task<ParcaOzetDto> ParcaGuncelleAsync(int id, ParcaIstek istek, CancellationToken ct = default);

    Task<List<UrunListeDto>> UrunleriListeleAsync(string? arama = null, CancellationToken ct = default);
    Task<UrunDetayDto?> UrunGetirAsync(int id, CancellationToken ct = default);
    Task<UrunDetayDto> UrunOlusturAsync(UrunIstek istek, CancellationToken ct = default);
    Task<UrunDetayDto> UrunGuncelleAsync(int id, UrunIstek istek, CancellationToken ct = default);
    Task<UrunDetayDto> UrunAgaciKaydetAsync(int urunId, IReadOnlyList<UrunAgaciSatiriIstek> satirlar, CancellationToken ct = default);

    Task<List<IletisimDto>> TedarikcilerAsync(CancellationToken ct = default);
    Task<List<IletisimDto>> MusterilerAsync(CancellationToken ct = default);

    Task<IletisimDto> MusteriKaydetAsync(int? id, IletisimIstek istek, CancellationToken ct = default);
    Task<IletisimDto> TedarikciKaydetAsync(int? id, IletisimIstek istek, CancellationToken ct = default);
}
