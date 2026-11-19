namespace BusinessLayer.Abstract;

public interface IUretimSirasiServisi
{
    Task<List<UretimEmriDto>> ListeAsync(bool bitenler = false, CancellationToken ct = default);

    Task<UretimEmriDto> GetirAsync(int id, CancellationToken ct = default);

    Task<UretimEmriDto> EkleAsync(UretimEmriIstek istek, int kullaniciId, CancellationToken ct = default);

    Task<UretimSonucuDto> UretAsync(int id, EmirUretIstek istek, int kullaniciId, CancellationToken ct = default);

    Task<UretimEmriDto> BaslaAsync(int id, CancellationToken ct = default);

    Task<UretimEmriDto> IptalAsync(int id, int kullaniciId, CancellationToken ct = default);

    Task<List<UretimEmriDto>> TasiAsync(int id, int yon, CancellationToken ct = default);

    Task<UretimEmriDto> PlanlaAsync(int id, DateOnly tarih, CancellationToken ct = default);
}
