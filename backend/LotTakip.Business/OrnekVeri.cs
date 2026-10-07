using LotTakip.DataAccess;
using LotTakip.Entity;
using LotTakip.Shared;
using Microsoft.EntityFrameworkCore;

namespace LotTakip.Business;

/// <summary>
/// Deneme için örnek veri.
/// 7 parça, 3 tedarikçi, 10 lot (biri geri çağrılmış), 2 ürün, 9 üretim, 2 müşteri, 2 satış.
/// Üretimler gerçek üretim servisiyle (FIFO), satışlar satış servisiyle yapılır; böylece
/// lotlarda kalan adetler kuralların kendisinden çıkar.
/// </summary>
public class OrnekVeri(AppDbContext db, IUretimServisi uretimServisi, ISatisServisi satisServisi)
{
    public const string AdminKullaniciAdi = "admin";

    public sealed record Sonuc(int Parca, int Tedarikci, int Lot, int Urun, int Uretim, int Musteri, int Satis);

    /// <summary>DI olmadan (komut satırı, testler) aynı bağlamla kurar.</summary>
    public static OrnekVeri Olustur(AppDbContext db) =>
        new(db, new UretimServisi(db, new StokLotuRepository(db), new UretimRepository(db)), new SatisServisi(db));

    private static readonly TimeSpan Istanbul = TimeSpan.FromHours(3);
    private static DateTimeOffset Zaman_(int y, int a, int g, int s, int d) => new(y, a, g, s, d, 0, Istanbul);

    private static readonly (string Kod, string Ad, string Birim, int MinStok)[] Parcalar =
    [
        ("R-10K", "Direnç 10 kΩ 0805", "adet", 200),
        ("LED-KR", "LED kırmızı 3 mm", "adet", 50),
        ("D-1N4007", "Diyot 1N4007", "adet", 30),
        ("PCB-IP", "İndikatör paneli PCB", "adet", 10),
        ("PCB-RK", "Röle kartı PCB", "adet", 10),
        ("KON-2P", "Klemens konnektör 2 pin", "adet", 40),
        ("ROLE-12", "Röle 12 V", "adet", 20),
    ];

    private static readonly (string Ad, string Iletisim)[] Tedarikciler =
    [
        ("Elektro Komponent A.Ş.", "satis@elektrokomponent.example · 0212 555 10 10"),
        ("Mikro Devre Ltd.", "info@mikrodevre.example · 0216 555 20 20"),
        ("Anadolu PCB", "siparis@anadolupcb.example · 0232 555 30 30"),
    ];

    // Parça siparişten bir hafta sonra gelmiş gibi (giriş saati 09:30)
    private static readonly (string LotNo, string Parca, string Tedarikci, DateOnly Siparis, string SiparisNo, int Adet, bool Geri)[] Lotlar =
    [
        ("L-R-2501", "R-10K", "Elektro Komponent A.Ş.", new(2026, 1, 8), "SP-1001", 1000, false),
        ("L-LED-2412", "LED-KR", "Mikro Devre Ltd.", new(2025, 12, 15), "SP-0987", 20, false),
        ("L-LED-2602", "LED-KR", "Mikro Devre Ltd.", new(2026, 2, 20), "SP-1023", 200, false),
        ("L-D-2601", "D-1N4007", "Elektro Komponent A.Ş.", new(2026, 1, 10), "SP-1002", 30, false),
        ("L-D-2603", "D-1N4007", "Mikro Devre Ltd.", new(2026, 3, 5), "SP-1040", 50, false),
        ("L-PCB-IP-01", "PCB-IP", "Anadolu PCB", new(2026, 1, 20), "SP-1010", 25, false),
        ("L-PCB-RK-01", "PCB-RK", "Anadolu PCB", new(2026, 1, 20), "SP-1011", 15, false),
        // En eski konnektör lotu geri çağrılmış: FIFO bu lotu atlar
        ("L-KON-2511", "KON-2P", "Elektro Komponent A.Ş.", new(2025, 11, 3), "SP-0950", 50, true),
        ("L-KON-2602", "KON-2P", "Elektro Komponent A.Ş.", new(2026, 2, 1), "SP-1015", 100, false),
        ("L-ROLE-2602", "ROLE-12", "Mikro Devre Ltd.", new(2026, 2, 10), "SP-1020", 30, false),
    ];

    private static readonly (string Kod, string Ad, string Onek, (string Parca, int Adet)[] Agac)[] Urunler =
    [
        ("IP", "İndikatör Paneli", "SN-IP-",
            [("LED-KR", 4), ("R-10K", 4), ("D-1N4007", 1), ("PCB-IP", 1), ("KON-2P", 1)]),
        ("RK", "Röle Kartı", "SN-RK-",
            [("ROLE-12", 2), ("D-1N4007", 2), ("R-10K", 2), ("PCB-RK", 1), ("KON-2P", 2)]),
    ];

    // Ürün, adet, tarih. Son grupta eski LED lotu biter, yeni lota geçilir (FIFO).
    private static readonly (string Urun, int Adet, DateTimeOffset Tarih)[] Uretimler =
    [
        ("IP", 4, Zaman_(2026, 4, 2, 10, 0)),
        ("RK", 3, Zaman_(2026, 4, 9, 14, 30)),
        ("IP", 2, Zaman_(2026, 5, 6, 9, 15)),
    ];

    private static readonly (string Ad, string Iletisim)[] Musteriler =
    [
        ("Atlas Otomasyon", "satinalma@atlasotomasyon.example · 0224 555 40 40"),
        ("Beta Enerji", "tedarik@betaenerji.example · 0312 555 50 50"),
    ];

    private static readonly (string Musteri, DateOnly Tarih, string[] Seriler)[] Satislar =
    [
        ("Atlas Otomasyon", new(2026, 5, 12), ["SN-IP-0001", "SN-IP-0002"]),
        ("Beta Enerji", new(2026, 5, 20), ["SN-RK-0001"]),
    ];

    /// <summary>
    /// Örnek veriyi yükler. Veri varsa ve <paramref name="sifirla"/> false ise durur.
    /// <paramref name="sifirla"/> tüm takip verisini siler (kullanıcılar kalır).
    /// "admin" kullanıcısı yoksa <paramref name="adminSifre"/> ile oluşturulur.
    /// </summary>
    public async Task<Sonuc> YukleAsync(string? adminSifre, bool sifirla, CancellationToken ct = default)
    {
        // Çağıran zaten bir transaction açtıysa (ör. testler) onun içinde çalış
        await using var tx = db.Database.CurrentTransaction is null
            ? await db.Database.BeginTransactionAsync(ct)
            : null;

        if (sifirla)
            await SifirlaAsync(ct);
        else if (await db.Parcalar.AnyAsync(ct))
            throw new InvalidOperationException(
                "Veritabanında zaten veri var. Silip yeniden yüklemek için --sifirla kullanın.");

        var admin = await db.Kullanicilar.SingleOrDefaultAsync(k => k.KullaniciAdi == AdminKullaniciAdi, ct);
        if (admin is null)
        {
            if (string.IsNullOrWhiteSpace(adminSifre))
                throw new InvalidOperationException(
                    "\"admin\" kullanıcısı için şifre gerekli: OrnekVeri:AdminSifre ayarını girin.");
            admin = new Kullanici
            {
                Ad = "Sistem", Soyad = "Yöneticisi", KullaniciAdi = AdminKullaniciAdi,
                SifreHash = BCrypt.Net.BCrypt.HashPassword(adminSifre), Rol = KullaniciRolu.Admin,
            };
            db.Kullanicilar.Add(admin);
        }

        // Tanımlar ve mal girişleri (henüz hiç üretim yok: kalan = giriş)
        var parcalar = Parcalar.ToDictionary(p => p.Kod,
            p => new Parca { Kod = p.Kod, Ad = p.Ad, Birim = p.Birim, MinStok = p.MinStok });
        var tedarikciler = Tedarikciler.ToDictionary(t => t.Ad, t => new Tedarikci { Ad = t.Ad, Iletisim = t.Iletisim });
        foreach (var l in Lotlar)
        {
            var giris = new DateTimeOffset(l.Siparis.AddDays(7).ToDateTime(new TimeOnly(9, 30)), Istanbul);
            db.StokLotlari.Add(new StokLotu
            {
                LotNo = l.LotNo, Parca = parcalar[l.Parca], Tedarikci = tedarikciler[l.Tedarikci],
                SiparisTarihi = l.Siparis, SiparisNo = l.SiparisNo, GirisAdet = l.Adet, KalanAdet = l.Adet,
                GeriCagrildi = l.Geri, GirisZamani = giris, OlusturanKullanici = admin, OlusturmaZamani = giris,
            });
        }
        var urunler = Urunler.ToDictionary(u => u.Kod, u =>
        {
            var urun = new Urun { Kod = u.Kod, Ad = u.Ad, SeriOneki = u.Onek };
            urun.Agac = [.. u.Agac.Select(a => new UrunAgaci { Urun = urun, Parca = parcalar[a.Parca], Adet = a.Adet })];
            return urun;
        });
        db.Urunler.AddRange(urunler.Values);
        db.Musteriler.AddRange(Musteriler.Select(m => new Musteri { Ad = m.Ad, Iletisim = m.Iletisim }));
        await db.SaveChangesAsync(ct);

        // Üretim ve satış gerçek iş kurallarıyla
        var uretimSayisi = 0;
        foreach (var (urunKod, adet, tarih) in Uretimler)
            uretimSayisi += (await uretimServisi.UretAsync(urunler[urunKod].Id, adet, admin.Id, tarih, ct)).SeriNolar.Count;
        foreach (var (musteri, tarih, seriler) in Satislar)
            await satisServisi.SatAsync(new SatisIstek(musteri, seriler, tarih), admin.Id, ct);

        if (tx is not null)
            await tx.CommitAsync(ct);

        return new Sonuc(parcalar.Count, tedarikciler.Count, Lotlar.Length, urunler.Count,
                         uretimSayisi, Musteriler.Length, Satislar.Length);
    }

    private async Task SifirlaAsync(CancellationToken ct)
    {
        // Restrict ilişkileri nedeniyle bağımlı kayıtlardan başlayarak sil
        await db.SatisKalemleri.ExecuteDeleteAsync(ct);
        await db.Satislar.ExecuteDeleteAsync(ct);
        await db.Musteriler.ExecuteDeleteAsync(ct);
        await db.StokDuzeltmeleri.ExecuteDeleteAsync(ct);
        await db.UretimTuketimleri.ExecuteDeleteAsync(ct);
        await db.Uretimler.ExecuteDeleteAsync(ct);
        await db.UrunAgaclari.ExecuteDeleteAsync(ct);
        await db.Urunler.ExecuteDeleteAsync(ct);
        await db.StokLotlari.ExecuteDeleteAsync(ct);
        await db.Tedarikciler.ExecuteDeleteAsync(ct);
        await db.Parcalar.ExecuteDeleteAsync(ct);
        db.ChangeTracker.Clear();
    }
}
