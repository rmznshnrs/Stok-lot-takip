namespace BusinessLayer.Abstract;

public interface IIzlemeServisi
{
    Task<LotIzDto?> LotIzleAsync(string lotNo, CancellationToken ct = default);

    Task<SeriIzDto?> SeriIzleAsync(string seriNo, CancellationToken ct = default);

    Task<NumaraAramaDto> NumaraAraAsync(string metin, CancellationToken ct = default);

    Task<List<HareketDto>> SonHareketlerAsync(int adet = 8, CancellationToken ct = default);

    Task<LotSorunSonucuDto> LotSorunAsync(string lotNo, LotSorunIstek istek, int kullaniciId, CancellationToken ct = default);
}
