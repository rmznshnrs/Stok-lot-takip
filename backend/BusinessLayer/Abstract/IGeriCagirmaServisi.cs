namespace BusinessLayer.Abstract;

public interface IGeriCagirmaServisi
{
    Task<List<GeriCagirmaDto>> GeriCagirAsync(GeriCagirmaIstek istek, int kullaniciId, CancellationToken ct = default);

    Task<GeriCagirmaDto> GeriAlAsync(int id, int kullaniciId, CancellationToken ct = default, KargoIstek? kargo = null);

    Task<GeriCagirmaDto> SatisaAcAsync(int id, int kullaniciId, CancellationToken ct = default);

    Task<GeriCagirmaDto> MusteriyeVerAsync(int id, int kullaniciId, CancellationToken ct = default, KargoIstek? kargo = null);

    Task<GeriCagirmaDto> BilgiVerildiAsync(int id, int kullaniciId, CancellationToken ct = default);
    Task<GeriCagirmaDto> HurdayaAyirAsync(int id, GeriCagirmaHurdaIstek istek, int kullaniciId, CancellationToken ct = default);
    Task<List<GeriCagirmaDto>> ListeAsync(bool acik = false, string? arama = null, CancellationToken ct = default);
}
