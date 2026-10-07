using LotTakip.Entity;
using Microsoft.EntityFrameworkCore;

namespace LotTakip.DataAccess;

/// <summary>
/// Deneme için örnek veri.
/// 7 parça, 3 tedarikçi, 10 lot (biri geri çağrılmış), 2 ürün, 9 üretim, 2 müşteri, 2 satış.
///
/// Üretimlerin hangi lottan kaç parça kullandığı FIFO sırasına göre hesaplanıp
/// sabit veri olarak yazılır; KalanAdet bu tüketimlerden hesaplanır.
/// TODO: üretimleri üretim servisiyle (FIFO) oluştur; bu tablo kalkar.
/// </summary>
public static class OrnekVeri
{
    public const string AdminKullaniciAdi = "admin";

    public sealed record Sonuc(int Parca, int Tedarikci, int Lot, int Urun, int Uretim, int Musteri, int Satis);

    private static readonly TimeSpan Istanbul = TimeSpan.FromHours(3);
    private static DateTimeOffset Zaman(int y, int a, int g, int s, int d) => new(y, a, g, s, d, 0, Istanbul);

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

    private static readonly (string Lot, int Adet)[] IpEskiLed =
        [("L-D-2601", 1), ("L-KON-2602", 1), ("L-LED-2412", 4), ("L-PCB-IP-01", 1), ("L-R-2501", 4)];
    private static readonly (string Lot, int Adet)[] IpYeniLed =
        [("L-D-2601", 1), ("L-KON-2602", 1), ("L-LED-2602", 4), ("L-PCB-IP-01", 1), ("L-R-2501", 4)];
    private static readonly (string Lot, int Adet)[] Rk =
        [("L-D-2601", 2), ("L-KON-2602", 2), ("L-PCB-RK-01", 1), ("L-R-2501", 2), ("L-ROLE-2602", 2)];

    // Eski LED lotu (20 adet) SN-IP-0005'te biter, SN-IP-0006 yeni lottan alır
    private static readonly (string SeriNo, string Urun, DateTimeOffset Tarih, (string Lot, int Adet)[] Tuketim)[] Uretimler =
    [
        ("SN-IP-0001", "IP", Zaman(2026, 4, 2, 10, 0), IpEskiLed),
        ("SN-IP-0002", "IP", Zaman(2026, 4, 2, 10, 0), IpEskiLed),
        ("SN-IP-0003", "IP", Zaman(2026, 4, 2, 10, 0), IpEskiLed),
        ("SN-IP-0004", "IP", Zaman(2026, 4, 2, 10, 0), IpEskiLed),
        ("SN-RK-0001", "RK", Zaman(2026, 4, 9, 14, 30), Rk),
        ("SN-RK-0002", "RK", Zaman(2026, 4, 9, 14, 30), Rk),
        ("SN-RK-0003", "RK", Zaman(2026, 4, 9, 14, 30), Rk),
        ("SN-IP-0005", "IP", Zaman(2026, 5, 6, 9, 15), IpEskiLed),
        ("SN-IP-0006", "IP", Zaman(2026, 5, 6, 9, 15), IpYeniLed),
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
    public static async Task<Sonuc> YukleAsync(AppDbContext db, string? adminSifre, bool sifirla,
                                               CancellationToken ct = default)
    {
        // Çağıran zaten bir transaction açtıysa (ör. testler) onun içinde çalış
        await using var tx = db.Database.CurrentTransaction is null
            ? await db.Database.BeginTransactionAsync(ct)
            : null;

        if (sifirla)
            await SifirlaAsync(db, ct);
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

        var parcalar = Parcalar.ToDictionary(p => p.Kod,
            p => new Parca { Kod = p.Kod, Ad = p.Ad, Birim = p.Birim, MinStok = p.MinStok });
        var tedarikciler = Tedarikciler.ToDictionary(t => t.Ad,
            t => new Tedarikci { Ad = t.Ad, Iletisim = t.Iletisim });

        var harcanan = Uretimler.SelectMany(u => u.Tuketim)
            .GroupBy(t => t.Lot).ToDictionary(g => g.Key, g => g.Sum(t => t.Adet));
        var lotlar = Lotlar.ToDictionary(l => l.LotNo, l =>
        {
            var giris = new DateTimeOffset(l.Siparis.AddDays(7).ToDateTime(new TimeOnly(9, 30)), Istanbul);
            return new StokLotu
            {
                LotNo = l.LotNo, Parca = parcalar[l.Parca], Tedarikci = tedarikciler[l.Tedarikci],
                SiparisTarihi = l.Siparis, SiparisNo = l.SiparisNo, GirisAdet = l.Adet,
                KalanAdet = l.Adet - harcanan.GetValueOrDefault(l.LotNo), GeriCagrildi = l.Geri,
                GirisZamani = giris, OlusturanKullanici = admin, OlusturmaZamani = giris,
            };
        });

        var urunler = Urunler.ToDictionary(u => u.Kod, u =>
        {
            var urun = new Urun { Kod = u.Kod, Ad = u.Ad, SeriOneki = u.Onek };
            urun.Agac = [.. u.Agac.Select(a => new UrunAgaci { Urun = urun, Parca = parcalar[a.Parca], Adet = a.Adet })];
            return urun;
        });

        var uretimler = Uretimler.ToDictionary(u => u.SeriNo, u =>
        {
            var uretim = new Uretim
            {
                SeriNo = u.SeriNo, Urun = urunler[u.Urun], Tarih = u.Tarih,
                OlusturanKullanici = admin, OlusturmaZamani = u.Tarih,
            };
            uretim.Tuketimler = [.. u.Tuketim.Select(t => new UretimTuketim { Uretim = uretim, StokLotu = lotlar[t.Lot], Adet = t.Adet })];
            return uretim;
        });

        var musteriler = Musteriler.ToDictionary(m => m.Ad, m => new Musteri { Ad = m.Ad, Iletisim = m.Iletisim });
        var satislar = Satislar.Select(s =>
        {
            var satis = new Satis
            {
                Musteri = musteriler[s.Musteri], Tarih = s.Tarih, OlusturanKullanici = admin,
                OlusturmaZamani = new DateTimeOffset(s.Tarih.ToDateTime(new TimeOnly(17, 0)), Istanbul),
            };
            satis.Kalemler = [.. s.Seriler.Select(seri => new SatisKalemi { Satis = satis, Uretim = uretimler[seri] })];
            return satis;
        }).ToList();

        db.AddRange(lotlar.Values);
        db.AddRange(urunler.Values);
        db.AddRange(uretimler.Values);
        db.AddRange(satislar);
        await db.SaveChangesAsync(ct);
        if (tx is not null)
            await tx.CommitAsync(ct);

        return new Sonuc(parcalar.Count, tedarikciler.Count, lotlar.Count, urunler.Count,
                         uretimler.Count, musteriler.Count, satislar.Count);
    }

    private static async Task SifirlaAsync(AppDbContext db, CancellationToken ct)
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
    }
}
