namespace BusinessLayer.Abstract;

public interface IKullaniciServisi
{
    Task<KullaniciDto?> DogrulaAsync(string kullaniciAdi, string sifre, CancellationToken ct = default);

    Task<KullaniciDto?> GetirAsync(int id, CancellationToken ct = default);

    Task SifreDegistirAsync(int kullaniciId, SifreDegistirIstek istek, CancellationToken ct = default);

    Task<List<KullaniciDto>> ListeleAsync(int islemYapanId, CancellationToken ct = default);
    Task<KullaniciDto> OlusturAsync(KullaniciOlusturIstek istek, int islemYapanId, CancellationToken ct = default);
    Task<KullaniciDto> GuncelleAsync(int id, KullaniciGuncelleIstek istek, int islemYapanId, CancellationToken ct = default);
    Task<KullaniciSilSonucuDto> SilAsync(int id, int islemYapanId, CancellationToken ct = default);
    Task<KullaniciDto> EtkinlestirAsync(int id, int islemYapanId, CancellationToken ct = default);
}
