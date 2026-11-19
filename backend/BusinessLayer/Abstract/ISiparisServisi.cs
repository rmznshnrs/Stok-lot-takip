namespace BusinessLayer.Abstract;

public interface ISiparisServisi
{
    Task<List<SiparisDto>> ListeAsync(int? musteriId = null, bool acik = false, CancellationToken ct = default,
                                      DateOnly? baslangic = null);

    Task<SiparisDto> GetirAsync(int id, CancellationToken ct = default);

    Task<List<SiparisUrunuDto>> UrunlerAsync(int id, CancellationToken ct = default);

    Task<SiparisDto> OlusturAsync(SiparisIstek istek, int kullaniciId, CancellationToken ct = default);

    Task<SiparisDto> SeriAyirAsync(int kalemId, SeriAyirIstek istek, CancellationToken ct = default);

    Task<SiparisDto> SeriBirakAsync(int kalemId, string seriNo, CancellationToken ct = default);

    Task<SiparisDto> IptalAsync(int id, int kullaniciId, CancellationToken ct = default);

    Task<List<HazirSeriDto>> HazirSerilerAsync(int urunId, CancellationToken ct = default);
}
