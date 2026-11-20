namespace BusinessLayer.Abstract;

public interface ISevkiyatServisi
{
    Task<List<SevkiyatDto>> ListeAsync(string? arama = null, string? durum = null, int? musteriId = null, CancellationToken ct = default,
                                       DateOnly? baslangic = null);

    Task<SevkiyatDto> GetirAsync(int id, CancellationToken ct = default);

    Task<SevkiyatDto> OlusturAsync(SevkiyatIstek istek, int kullaniciId, CancellationToken ct = default);

    Task<SevkiyatDto> KargoAsync(int id, KargoIstek istek, CancellationToken ct = default);

    Task<SevkiyatDto> SiparisNoAsync(int id, SiparisNoIstek istek, CancellationToken ct = default);

    Task<SevkiyatDto> TeslimAsync(int id, CancellationToken ct = default);
}
