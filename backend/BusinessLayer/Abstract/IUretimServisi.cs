namespace BusinessLayer.Abstract;

public interface IUretimServisi
{
    Task<UretimOnizlemeDto> OnizleAsync(int urunId, int adet, CancellationToken ct = default);

    Task<UretimSonucuDto> UretAsync(int urunId, int adet, int kullaniciId, DateTimeOffset? tarih = null,
                                    CancellationToken ct = default);

    Task<List<string>> SonrakiSeriNolarAsync(int urunId, int adet, CancellationToken ct = default);

    Task<List<UretimGecmisiDto>> UretimGecmisiAsync(string? arama = null, int? urunId = null, int adet = 500,
                                                    CancellationToken ct = default);

    Task<UretimHurdaSonucuDto> HurdayaAyirAsync(UretimHurdaIstek istek, int kullaniciId, CancellationToken ct = default);
}
