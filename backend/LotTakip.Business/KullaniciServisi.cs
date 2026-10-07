using LotTakip.DataAccess;
using LotTakip.Entity;
using LotTakip.Shared;
using Microsoft.EntityFrameworkCore;

namespace LotTakip.Business;

public interface IKullaniciServisi
{
    /// <summary>Kullanıcı adı ve şifreyi doğrular (BCrypt). Hatalıysa null; hangisinin hatalı olduğu söylenmez.</summary>
    Task<KullaniciDto?> DogrulaAsync(string kullaniciAdi, string sifre, CancellationToken ct = default);

    Task<KullaniciDto?> GetirAsync(int id, CancellationToken ct = default);

    /// <summary>Kendi şifresini değiştirir; eski şifre doğru olmalı.</summary>
    Task SifreDegistirAsync(int kullaniciId, SifreDegistirIstek istek, CancellationToken ct = default);

    // Kullanıcı yönetimi: yalnız Admin
    Task<List<KullaniciDto>> ListeleAsync(int islemYapanId, CancellationToken ct = default);
    Task<KullaniciDto> OlusturAsync(KullaniciOlusturIstek istek, int islemYapanId, CancellationToken ct = default);
    /// <summary>Son Admin'in rolü Kullanici yapılamaz (sistem yöneticisiz kalmasın).</summary>
    Task<KullaniciDto> GuncelleAsync(int id, KullaniciGuncelleIstek istek, int islemYapanId, CancellationToken ct = default);
}

public class KullaniciServisi(AppDbContext db) : IKullaniciServisi
{
    public const int EnAzSifreUzunlugu = 8;

    // Kullanıcı adı yoksa da aynı sürede yanıt verilsin diye karşılaştırılan sabit bir özet
    private static readonly string SahteOzet = BCrypt.Net.BCrypt.HashPassword("kullanici-yok");

    public async Task<KullaniciDto?> DogrulaAsync(string kullaniciAdi, string sifre, CancellationToken ct = default)
    {
        var ad = (kullaniciAdi ?? "").Trim();
        var k = await db.Kullanicilar.AsNoTracking().SingleOrDefaultAsync(x => x.KullaniciAdi == ad, ct);
        var dogru = BCrypt.Net.BCrypt.Verify(sifre ?? "", k?.SifreHash ?? SahteOzet);
        return k is not null && dogru ? Dto(k) : null;
    }

    public async Task<KullaniciDto?> GetirAsync(int id, CancellationToken ct = default) =>
        await db.Kullanicilar.AsNoTracking().Where(k => k.Id == id).Select(k => Dto(k)).SingleOrDefaultAsync(ct);

    public Task SifreDegistirAsync(int kullaniciId, SifreDegistirIstek istek, CancellationToken ct = default)
    {
        SifreKontrol(istek.YeniSifre);
        return Islem.CalistirAsync(db, async () =>
        {
            var k = await Kullanicilar.GetirAsync(db, kullaniciId, ct);
            if (!BCrypt.Net.BCrypt.Verify(istek.EskiSifre ?? "", k.SifreHash))
                throw new IsKuraliHatasi("Mevcut şifre hatalı.");
            k.SifreHash = BCrypt.Net.BCrypt.HashPassword(istek.YeniSifre);
            await db.SaveChangesAsync(ct);
            return 0;
        }, ct);
    }

    public async Task<List<KullaniciDto>> ListeleAsync(int islemYapanId, CancellationToken ct = default)
    {
        await Kullanicilar.AdminGetirAsync(db, islemYapanId, "Kullanıcı yönetimi", ct);
        return await db.Kullanicilar.AsNoTracking().OrderBy(k => k.KullaniciAdi).Select(k => Dto(k)).ToListAsync(ct);
    }

    public Task<KullaniciDto> OlusturAsync(KullaniciOlusturIstek istek, int islemYapanId, CancellationToken ct = default)
    {
        var (ad, soyad) = (Donusum.Sadelestir(istek.Ad), Donusum.Sadelestir(istek.Soyad));
        var kullaniciAdi = (istek.KullaniciAdi ?? "").Trim();
        if (ad.Length == 0 || soyad.Length == 0 || kullaniciAdi.Length == 0)
            throw new IsKuraliHatasi("Ad, soyad ve kullanıcı adı boş olamaz.");
        if (kullaniciAdi.Contains(' '))
            throw new IsKuraliHatasi("Kullanıcı adında boşluk olamaz.");
        SifreKontrol(istek.Sifre);
        var rol = Rol(istek.Rol);

        return Islem.CalistirAsync(db, async () =>
        {
            await Kullanicilar.AdminGetirAsync(db, islemYapanId, "Kullanıcı yönetimi", ct);
            if (await db.Kullanicilar.AnyAsync(k => k.KullaniciAdi == kullaniciAdi, ct))
                throw new IsKuraliHatasi("Bu kullanıcı adı zaten kullanılıyor.");
            var k = new Kullanici
            {
                Ad = ad, Soyad = soyad, KullaniciAdi = kullaniciAdi, Rol = rol,
                SifreHash = BCrypt.Net.BCrypt.HashPassword(istek.Sifre),
            };
            db.Kullanicilar.Add(k);
            await db.SaveChangesAsync(ct);
            return Dto(k);
        }, ct);
    }

    public Task<KullaniciDto> GuncelleAsync(int id, KullaniciGuncelleIstek istek, int islemYapanId, CancellationToken ct = default)
    {
        var (ad, soyad) = (Donusum.Sadelestir(istek.Ad), Donusum.Sadelestir(istek.Soyad));
        if (ad.Length == 0 || soyad.Length == 0)
            throw new IsKuraliHatasi("Ad ve soyad boş olamaz.");
        var yeniSifre = string.IsNullOrEmpty(istek.YeniSifre) ? null : istek.YeniSifre;
        if (yeniSifre is not null)
            SifreKontrol(yeniSifre);
        var rol = Rol(istek.Rol);

        return Islem.CalistirAsync(db, async () =>
        {
            await Kullanicilar.AdminGetirAsync(db, islemYapanId, "Kullanıcı yönetimi", ct);
            var k = await db.Kullanicilar.SingleOrDefaultAsync(x => x.Id == id, ct)
                    ?? throw new BulunamadiHatasi("Kullanıcı bulunamadı.");
            if (k.Rol == KullaniciRolu.Admin && rol != KullaniciRolu.Admin
                && await db.Kullanicilar.CountAsync(x => x.Rol == KullaniciRolu.Admin, ct) == 1)
                throw new IsKuraliHatasi("Son Admin'in yetkisi alınamaz; önce başka bir kullanıcıyı Admin yapın.");
            (k.Ad, k.Soyad, k.Rol) = (ad, soyad, rol);
            if (yeniSifre is not null)
                k.SifreHash = BCrypt.Net.BCrypt.HashPassword(yeniSifre);
            await db.SaveChangesAsync(ct);
            return Dto(k);
        }, ct);
    }

    private static KullaniciDto Dto(Kullanici k) => new(k.Id, k.Ad, k.Soyad, k.KullaniciAdi, k.Rol.ToString());

    private static void SifreKontrol(string? sifre)
    {
        if (string.IsNullOrEmpty(sifre) || sifre.Length < EnAzSifreUzunlugu)
            throw new IsKuraliHatasi($"Şifre en az {EnAzSifreUzunlugu} karakter olmalı.");
    }

    private static KullaniciRolu Rol(string? rol) =>
        Enum.TryParse<KullaniciRolu>(rol, ignoreCase: true, out var r) && Enum.IsDefined(r)
            ? r
            : throw new IsKuraliHatasi("Rol Admin veya Kullanici olmalı.");
}
