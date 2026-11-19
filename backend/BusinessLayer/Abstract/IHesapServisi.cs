namespace BusinessLayer.Abstract;

public interface IHesapServisi
{
    Task<OnayBekleyenDto> KayitAsync(KayitIstek istek, CancellationToken ct = default);

    Task<bool> OnayliMiAsync(int kullaniciId, CancellationToken ct = default);

    Task<List<OnayBekleyenDto>> OnayBekleyenlerAsync(int adminId, CancellationToken ct = default);

    Task OnaylaAsync(int id, int adminId, CancellationToken ct = default);

    Task ReddetAsync(int id, int adminId, CancellationToken ct = default);

    Task<EpostaDto> EpostaGetirAsync(int kullaniciId, CancellationToken ct = default);

    Task<EpostaDto> EpostaAyarlaAsync(int kullaniciId, string? eposta, CancellationToken ct = default);

    Task SifreUnuttumAsync(string kullaniciAdiVeyaEposta, string webAdresi, CancellationToken ct = default);

    Task SifreSifirlaAsync(SifreSifirlaIstek istek, CancellationToken ct = default);
}
