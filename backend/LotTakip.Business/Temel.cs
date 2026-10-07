using LotTakip.DataAccess;
using LotTakip.Entity;
using LotTakip.Shared;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace LotTakip.Business;

// --- Hatalar -------------------------------------------------------------------
// İş kuralı ihlalleri. API bunları uygun HTTP yanıtına çevirir; mesajlar
// kullanıcıya gösterilecek Türkçe metinlerdir.

public class IsKuraliHatasi(string mesaj) : Exception(mesaj);

public class BulunamadiHatasi(string mesaj) : IsKuraliHatasi(mesaj);

public class YetkiHatasi(string mesaj) : IsKuraliHatasi(mesaj);

public class SatisHatasi(string mesaj) : IsKuraliHatasi(mesaj);

/// <summary>Üretim için stok yetmiyor; hiçbir şey kaydedilmedi.</summary>
public class YetersizStokHatasi(IReadOnlyList<EksikParcaDto> eksikler)
    : IsKuraliHatasi("Yetersiz stok – " + string.Join(", ", eksikler.Select(e => $"{e.Parca.Kod}: {e.Eksik} eksik")))
{
    public IReadOnlyList<EksikParcaDto> Eksikler { get; } = eksikler;
}

/// <summary>Listede geri çağrılan lot içeren ürün var; satış onay olmadan yapılmaz.</summary>
public class GeriCagrilanLotOnayiGerekliHatasi(IReadOnlyList<SatisUyarisiDto> uyarilar)
    : SatisHatasi("Geri çağrılan lot içeren ürün var: " +
                  string.Join(", ", uyarilar.Select(u => $"{u.SeriNo} ({string.Join(", ", u.Lotlar)})")) +
                  ". Yine de satmak için onay gerekir.")
{
    public IReadOnlyList<SatisUyarisiDto> Uyarilar { get; } = uyarilar;
}

// --- Transaction ve yeniden deneme -----------------------------------------------

internal static class Islem
{
    private const int DenemeSayisi = 3;

    /// <summary>
    /// İşi tek transaction içinde yapar. Lot sürümü çakışması (RowVersion), benzersiz indeks
    /// çakışması (ör. aynı anda verilen seri no) veya kilitlenme olursa baştan yeniden dener.
    /// Çağıran zaten bir transaction açmışsa (ör. örnek veri, testler) onun içinde bir kayıt
    /// noktası (savepoint) açar; hata olursa yalnız bu işin yaptıkları geri alınır.
    /// Hata olursa bağlamdaki bekleyen değişiklikler temizlenir ki sonradan kaydedilmesin.
    /// </summary>
    public static async Task<T> CalistirAsync<T>(AppDbContext db, Func<Task<T>> is_, CancellationToken ct)
    {
        if (db.Database.CurrentTransaction is { } dis)
        {
            var nokta = "islem_" + Guid.NewGuid().ToString("N")[..8];
            await dis.CreateSavepointAsync(nokta, ct);
            try
            {
                return await is_();
            }
            catch
            {
                db.ChangeTracker.Clear();
                await dis.RollbackToSavepointAsync(nokta, ct);
                throw;
            }
        }

        for (var deneme = 1; ; deneme++)
        {
            await using var tx = await db.Database.BeginTransactionAsync(ct);
            try
            {
                var sonuc = await is_();
                await tx.CommitAsync(ct);
                return sonuc;
            }
            catch (Exception hata)
            {
                db.ChangeTracker.Clear();
                await tx.RollbackAsync(CancellationToken.None);
                if (deneme >= DenemeSayisi || !YenidenDenenebilir(hata))
                    throw;
            }
        }
    }

    private static bool YenidenDenenebilir(Exception hata) => hata switch
    {
        DbUpdateConcurrencyException => true,
        // 2601/2627: benzersiz indeks çakışması, 1205: kilitlenme (deadlock) kurbanı
        DbUpdateException { InnerException: SqlException s } => s.Number is 2601 or 2627 or 1205,
        SqlException s => s.Number == 1205,
        _ => false,
    };
}

// --- Zaman -----------------------------------------------------------------------

public static class Zaman
{
    /// <summary>Firmanın saat dilimi; "bugün" ve hareket tarihleri buna göre.</summary>
    public static readonly TimeZoneInfo Istanbul = TimeZoneInfo.FindSystemTimeZoneById("Europe/Istanbul");

    public static DateTimeOffset Simdi() => TimeZoneInfo.ConvertTime(DateTimeOffset.UtcNow, Istanbul);

    public static DateOnly Bugun() => DateOnly.FromDateTime(Simdi().DateTime);

    public static DateOnly YerelTarih(DateTimeOffset an) => DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(an, Istanbul).DateTime);
}

// --- Yardımcılar --------------------------------------------------------------------

internal static class Donusum
{
    public static ParcaOzetDto Ozet(Parca p) => new(p.Id, p.Kod, p.Ad, p.Birim);

    /// <summary>Lot → DTO. Lotun Parca ve Tedarikci'si yüklenmiş olmalı.</summary>
    public static LotDto Lot(StokLotu l, bool sirada = false) => new(
        l.Id, l.LotNo, l.ParcaId, l.Parca!.Kod, l.Parca.Ad, l.Tedarikci!.Ad, l.SiparisTarihi, l.SiparisNo,
        l.GirisAdet, l.KalanAdet, l.GeriCagrildi, l.GirisZamani, sirada);

    /// <summary>Ad ve kodlarda baştaki/sondaki ve ardışık boşlukları temizler.</summary>
    public static string Sadelestir(string? metin) => string.Join(' ', (metin ?? "").Split(' ', StringSplitOptions.RemoveEmptyEntries)).Trim();
}

internal static class Kullanicilar
{
    /// <summary>İşlemi yapan kullanıcıyı getirir; yoksa hata.</summary>
    public static async Task<Kullanici> GetirAsync(AppDbContext db, int kullaniciId, CancellationToken ct) =>
        await db.Kullanicilar.SingleOrDefaultAsync(k => k.Id == kullaniciId, ct)
        ?? throw new YetkiHatasi("İşlemi yapan kullanıcı bulunamadı.");

    /// <summary>Kullanıcı Admin değilse YetkiHatasi.</summary>
    public static async Task<Kullanici> AdminGetirAsync(AppDbContext db, int kullaniciId, string islem, CancellationToken ct)
    {
        var k = await GetirAsync(db, kullaniciId, ct);
        if (k.Rol != KullaniciRolu.Admin)
            throw new YetkiHatasi($"{islem} yalnızca Admin yetkisiyle yapılabilir.");
        return k;
    }
}
