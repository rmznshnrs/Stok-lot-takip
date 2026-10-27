using System.Net.Mail;
using System.Security.Cryptography;
using System.Text;
using LotTakip.DataAccess;
using LotTakip.Entity;
using LotTakip.Shared;
using Microsoft.EntityFrameworkCore;

namespace LotTakip.Business;

/// <summary>E-posta gönderimi (API katmanında SMTP ile ya da ayarlı değilse dosyaya yazılarak).</summary>
public interface IPostaGonderici
{
    Task GonderAsync(string alici, string konu, string metin, CancellationToken ct = default);
}

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

public class HesapServisi(AppDbContext db, IPostaGonderici posta) : IHesapServisi
{
    public static readonly TimeSpan KodSuresi = TimeSpan.FromMinutes(30);

    public Task<OnayBekleyenDto> KayitAsync(KayitIstek istek, CancellationToken ct = default)
    {
        var ad = Donusum.Sadelestir(istek.Ad);
        var soyad = Donusum.Sadelestir(istek.Soyad);
        var kullaniciAdi = (istek.KullaniciAdi ?? "").Trim();
        if (ad.Length == 0 || soyad.Length == 0 || kullaniciAdi.Length == 0)
            throw new IsKuraliHatasi("Ad, soyad ve kullanıcı adı boş olamaz.");
        var eposta = Eposta(istek.Eposta) ?? throw new IsKuraliHatasi("E-posta boş olamaz.");
        SifreKontrol(istek.Sifre);

        return Islem.CalistirAsync(db, async () =>
        {
            if (await db.Kullanicilar.AnyAsync(k => k.KullaniciAdi == kullaniciAdi, ct))
                throw new IsKuraliHatasi($"{kullaniciAdi} kullanıcı adı alınmış.");
            if (await db.Kullanicilar.AnyAsync(k => k.Eposta == eposta, ct))
                throw new IsKuraliHatasi("Bu e-posta kayıtlı.");
            var k = new Kullanici
            {
                Ad = ad, Soyad = soyad, KullaniciAdi = kullaniciAdi, Eposta = eposta, Onayli = false,
                SifreHash = BCrypt.Net.BCrypt.HashPassword(istek.Sifre), Rol = KullaniciRolu.Kullanici,
            };
            db.Kullanicilar.Add(k);
            await db.SaveChangesAsync(ct);
            return new OnayBekleyenDto(k.Id, k.Ad, k.Soyad, k.KullaniciAdi, k.Eposta);
        }, ct);
    }

    public Task<bool> OnayliMiAsync(int kullaniciId, CancellationToken ct = default) =>
        db.Kullanicilar.AnyAsync(k => k.Id == kullaniciId && k.Onayli, ct);

    public async Task<List<OnayBekleyenDto>> OnayBekleyenlerAsync(int adminId, CancellationToken ct = default)
    {
        await Kullanicilar.AdminGetirAsync(db, adminId, "Kayıt onayı", ct);
        return await db.Kullanicilar.AsNoTracking().Where(k => !k.Onayli).OrderBy(k => k.Id)
            .Select(k => new OnayBekleyenDto(k.Id, k.Ad, k.Soyad, k.KullaniciAdi, k.Eposta)).ToListAsync(ct);
    }

    public Task OnaylaAsync(int id, int adminId, CancellationToken ct = default) =>
        Islem.CalistirAsync(db, async () =>
        {
            await Kullanicilar.AdminGetirAsync(db, adminId, "Kayıt onayı", ct);
            var k = await BekleyenGetirAsync(id, ct);
            k.Onayli = true;
            await db.SaveChangesAsync(ct);
            return 0;
        }, ct);

    public Task ReddetAsync(int id, int adminId, CancellationToken ct = default) =>
        Islem.CalistirAsync(db, async () =>
        {
            await Kullanicilar.AdminGetirAsync(db, adminId, "Kayıt onayı", ct);
            db.Kullanicilar.Remove(await BekleyenGetirAsync(id, ct));
            await db.SaveChangesAsync(ct);
            return 0;
        }, ct);

    public async Task<EpostaDto> EpostaGetirAsync(int kullaniciId, CancellationToken ct = default) =>
        new((await Kullanicilar.GetirAsync(db, kullaniciId, ct)).Eposta);

    public Task<EpostaDto> EpostaAyarlaAsync(int kullaniciId, string? eposta, CancellationToken ct = default)
    {
        var yeni = Eposta(eposta);
        return Islem.CalistirAsync(db, async () =>
        {
            var k = await Kullanicilar.GetirAsync(db, kullaniciId, ct);
            if (yeni is not null && await db.Kullanicilar.AnyAsync(x => x.Id != k.Id && x.Eposta == yeni, ct))
                throw new IsKuraliHatasi("Bu e-posta kayıtlı.");
            k.Eposta = yeni;
            await db.SaveChangesAsync(ct);
            return new EpostaDto(k.Eposta);
        }, ct);
    }

    public async Task SifreUnuttumAsync(string kullaniciAdiVeyaEposta, string webAdresi, CancellationToken ct = default)
    {
        var deger = (kullaniciAdiVeyaEposta ?? "").Trim();
        if (deger.Length == 0)
            throw new IsKuraliHatasi("Kullanıcı adı ya da e-posta girin.");
        var k = await db.Kullanicilar
            .SingleOrDefaultAsync(x => x.Onayli && x.Eposta != null && (x.KullaniciAdi == deger || x.Eposta == deger), ct);
        if (k is null)
            return;

        var kod = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32)).TrimEnd('=').Replace('+', '-').Replace('/', '_');
        var simdi = Zaman.Simdi();
        db.SifreSifirlamalari.Add(new SifreSifirlama
        {
            KullaniciId = k.Id, KodOzeti = Ozet(kod), OlusturmaZamani = simdi, SonGecerlilik = simdi + KodSuresi,
        });
        await db.SaveChangesAsync(ct);

        var baglanti = $"{webAdresi.TrimEnd('/')}/sifre-sifirla?kod={kod}";
        await posta.GonderAsync(k.Eposta!, "Şifre sıfırlama",
            $"Merhaba {k.Ad},\n\n{k.KullaniciAdi} hesabı için şifre sıfırlama istendi. Yeni şifre belirlemek için " +
            $"{KodSuresi.TotalMinutes:0} dakika içinde şu bağlantıyı açın:\n\n{baglanti}\n\n" +
            "Bu isteği siz yapmadıysanız bu e-postayı dikkate almayın; şifreniz değişmez.\n\nMKC Bilişim", ct);
    }

    public Task SifreSifirlaAsync(SifreSifirlaIstek istek, CancellationToken ct = default)
    {
        SifreKontrol(istek.YeniSifre);
        var ozet = Ozet((istek.Kod ?? "").Trim());
        return Islem.CalistirAsync(db, async () =>
        {
            var simdi = Zaman.Simdi();
            var s = await db.SifreSifirlamalari.Include(x => x.Kullanici).SingleOrDefaultAsync(x => x.KodOzeti == ozet, ct);
            if (s is null || s.KullanilmaZamani is not null || s.SonGecerlilik < simdi)
                throw new IsKuraliHatasi("Bağlantı geçersiz veya süresi dolmuş.");
            s.Kullanici!.SifreHash = BCrypt.Net.BCrypt.HashPassword(istek.YeniSifre);
            // Bu kullanıcının açıkta kalan bütün kodları da kapanır
            foreach (var acik in await db.SifreSifirlamalari
                         .Where(x => x.KullaniciId == s.KullaniciId && x.KullanilmaZamani == null).ToListAsync(ct))
                acik.KullanilmaZamani = simdi;
            await db.SaveChangesAsync(ct);
            return 0;
        }, ct);
    }

    // --- iç ------------------------------------------------------------------

    private async Task<Kullanici> BekleyenGetirAsync(int id, CancellationToken ct) =>
        await db.Kullanicilar.SingleOrDefaultAsync(k => k.Id == id && !k.Onayli, ct)
        ?? throw new BulunamadiHatasi("Kayıt bulunamadı.");

    private static string? Eposta(string? metin)
    {
        var e = (metin ?? "").Trim();
        if (e.Length == 0)
            return null;
        if (e.Length > 200 || !MailAddress.TryCreate(e, out var adres) || adres.Address != e || !adres.Host.Contains('.'))
            throw new IsKuraliHatasi("Geçerli bir e-posta girin.");
        return e;
    }

    private static void SifreKontrol(string? sifre)
    {
        if (string.IsNullOrEmpty(sifre) || sifre.Length < KullaniciServisi.EnAzSifreUzunlugu)
            throw new IsKuraliHatasi($"Şifre en az {KullaniciServisi.EnAzSifreUzunlugu} karakter olmalı.");
    }

    private static string Ozet(string kod) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(kod)));
}
