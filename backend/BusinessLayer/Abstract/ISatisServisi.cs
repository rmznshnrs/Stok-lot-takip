namespace BusinessLayer.Abstract;

public interface ISatisServisi
{
    Task<SatilabilirDto> SatilabilirAsync(string seriNo, CancellationToken ct = default);

    Task<List<SatisUyarisiDto>> SatisUyarilariAsync(IReadOnlyCollection<string> seriNolar, CancellationToken ct = default);

    Task<SatisSonucuDto> SatAsync(SatisIstek istek, int kullaniciId, CancellationToken ct = default);

    Task<List<SatisGecmisiDto>> SatisGecmisiAsync(string? arama = null, int adet = 500, CancellationToken ct = default,
                                                    int? musteriId = null);

    Task<SatisIadeSonucuDto> IadeAlAsync(int satisId, SatisIadeIstek istek, int kullaniciId, CancellationToken ct = default);
}
