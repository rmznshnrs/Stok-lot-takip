using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace BusinessLayer;

public class IsKuraliHatasi(string mesaj) : Exception(mesaj);

public class BulunamadiHatasi(string mesaj) : IsKuraliHatasi(mesaj);

public class YetkiHatasi(string mesaj) : IsKuraliHatasi(mesaj);

public class SatisHatasi(string mesaj) : IsKuraliHatasi(mesaj);

public class YetersizStokHatasi(IReadOnlyList<EksikParcaDto> eksikler)
    : IsKuraliHatasi("Yetersiz stok – " + string.Join(", ", eksikler.Select(e => $"{e.Parca.Kod}: {e.Eksik} eksik")))
{
    public IReadOnlyList<EksikParcaDto> Eksikler { get; } = eksikler;
}

public class GeriCagrilanLotOnayiGerekliHatasi(IReadOnlyList<SatisUyarisiDto> uyarilar)
    : SatisHatasi("Geri çağrılan lot içeren ürün var: " +
                  string.Join(", ", uyarilar.Select(u => $"{u.SeriNo} ({string.Join(", ", u.Lotlar)})")) +
                  ". Yine de satmak için onay gerekir.")
{
    public IReadOnlyList<SatisUyarisiDto> Uyarilar { get; } = uyarilar;
}

internal static class Sira
{
    public static int EnBuyuk(IEnumerable<string> numaralar, string onek) => numaralar
        .Where(s => s.StartsWith(onek, StringComparison.OrdinalIgnoreCase))
        .Select(s => s[onek.Length..])
        .Where(son => son.Length > 0 && son.All(char.IsAsciiDigit))
        .Select(son => int.TryParse(son, out var n) ? n : 0)
        .DefaultIfEmpty(0).Max();
}

internal sealed class DogalSira : IComparer<string>
{
    public static readonly DogalSira Ornek = new();

    public int Compare(string? x, string? y)
    {
        if (ReferenceEquals(x, y)) return 0;
        if (x is null) return -1;
        if (y is null) return 1;
        int i = 0, j = 0;
        while (i < x.Length && j < y.Length)
        {
            if (char.IsAsciiDigit(x[i]) && char.IsAsciiDigit(y[j]))
            {
                int bi = i, bj = j;
                while (i < x.Length && char.IsAsciiDigit(x[i])) i++;
                while (j < y.Length && char.IsAsciiDigit(y[j])) j++;
                var a = x[bi..i].TrimStart('0');
                var b = y[bj..j].TrimStart('0');
                var fark = a.Length != b.Length ? a.Length.CompareTo(b.Length) : string.CompareOrdinal(a, b);
                if (fark != 0) return fark;
            }
            else
            {
                var fark = string.Compare(x[i].ToString(), y[j].ToString(), StringComparison.CurrentCultureIgnoreCase);
                if (fark != 0) return fark;
                i++;
                j++;
            }
        }
        return (x.Length - i).CompareTo(y.Length - j);
    }
}

internal static class SeriListesi
{
    public static List<string> Temizle(IEnumerable<string?>? seriler) =>
        [.. (seriler ?? []).Select(s => (s ?? "").Trim()).Where(s => s.Length > 0).Distinct(StringComparer.OrdinalIgnoreCase)];
}

internal static class Islem
{
    private const int DenemeSayisi = 3;

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
        DbUpdateException { InnerException: SqlException s } => s.Number is 2601 or 2627 or 1205,
        SqlException s => s.Number == 1205,
        _ => false,
    };
}

public static class Zaman
{
    public static readonly TimeZoneInfo Istanbul = TimeZoneInfo.FindSystemTimeZoneById("Europe/Istanbul");

    public static DateTimeOffset Simdi() => TimeZoneInfo.ConvertTime(DateTimeOffset.UtcNow, Istanbul);

    public static DateOnly Bugun() => DateOnly.FromDateTime(Simdi().DateTime);

    public static DateOnly YerelTarih(DateTimeOffset an) => DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(an, Istanbul).DateTime);
}

internal static class Donusum
{
    public static ParcaOzetDto Ozet(Parca p) => new(p.Id, p.Kod, p.Ad);

    public static LotDto Lot(StokLotu l, bool sirada = false) => new(
        l.Id, l.LotNo, l.ParcaId, l.Parca!.Kod, l.Parca.Ad, l.Tedarikci!.Ad, l.SiparisTarihi, l.SiparisNo,
        l.GirisAdet, l.KalanAdet, l.GeriCagrildi, l.GirisZamani, sirada, l.Sorunlu, l.SorunAciklama, l.TedarikciLotNo);

    public static string Sadelestir(string? metin) => string.Join(' ', (metin ?? "").Split(' ', StringSplitOptions.RemoveEmptyEntries)).Trim();
}

internal static class Kullanicilar
{
    public static async Task<Kullanici> GetirAsync(AppDbContext db, int kullaniciId, CancellationToken ct) =>
        await db.Kullanicilar.SingleOrDefaultAsync(k => k.Id == kullaniciId && k.Aktif, ct)
        ?? throw new YetkiHatasi("İşlemi yapan kullanıcı bulunamadı.");

    public static async Task<Kullanici> AdminGetirAsync(AppDbContext db, int kullaniciId, string islem, CancellationToken ct)
    {
        var k = await GetirAsync(db, kullaniciId, ct);
        if (k.Rol != KullaniciRolu.Admin)
            throw new YetkiHatasi($"{islem} için yönetici yetkisi gerekir.");
        return k;
    }
}
