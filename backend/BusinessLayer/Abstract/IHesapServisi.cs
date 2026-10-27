namespace BusinessLayer.Abstract;

public interface IHesapServisi
{
    /// <summary>Herkese açık kayıt: Çalışan rolüyle, onaysız açılır; yönetici onaylayana kadar giriş yapılamaz.</summary>
    Task<OnayBekleyenDto> KayitAsync(KayitIstek istek, CancellationToken ct = default);

    Task<bool> OnayliMiAsync(int kullaniciId, CancellationToken ct = default);

    /// <summary>Yalnız Admin: onay bekleyen kayıtlar (eskiden yeniye).</summary>
    Task<List<OnayBekleyenDto>> OnayBekleyenlerAsync(int adminId, CancellationToken ct = default);

    Task OnaylaAsync(int id, int adminId, CancellationToken ct = default);

    /// <summary>Yalnız Admin: onaylanmamış kaydı siler (hiç giriş yapmadığı için bağlı kaydı yoktur).</summary>
    Task ReddetAsync(int id, int adminId, CancellationToken ct = default);

    Task<EpostaDto> EpostaGetirAsync(int kullaniciId, CancellationToken ct = default);

    /// <summary>Kendi e-postasını ayarlar (boş: kaldırır). Şifre sıfırlama bağlantısı buraya gider.</summary>
    Task<EpostaDto> EpostaAyarlaAsync(int kullaniciId, string? eposta, CancellationToken ct = default);

    /// <summary>
    /// Kullanıcı adı ya da e-postayla eşleşen onaylı hesabın e-postasına sıfırlama bağlantısı gönderir.
    /// Hesap yoksa da hata vermez (kimin kayıtlı olduğu bu yolla öğrenilemesin).
    /// </summary>
    Task SifreUnuttumAsync(string kullaniciAdiVeyaEposta, string webAdresi, CancellationToken ct = default);

    /// <summary>E-postadaki kodla yeni şifre belirlenir; kod tek kullanımlıktır ve süresi sınırlıdır.</summary>
    Task SifreSifirlaAsync(SifreSifirlaIstek istek, CancellationToken ct = default);
}
