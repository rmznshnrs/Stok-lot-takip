using System.Data.Common;
using LotTakip.Business;
using LotTakip.DataAccess;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Storage;

// Testler tek bir gerçek veritabanını paylaşır; aynı anda koşarlarsa birbirini kilitler.
[assembly: CollectionBehavior(DisableTestParallelization = true)]

namespace LotTakip.Tests;

/// <summary>
/// Gerçek SQL Server'da ayrı test veritabanı; InMemory kullanılmaz.
/// Test çalıştırması başında silinip migration'larla yeniden oluşturulur.
/// Bağlantı LOTTAKIP_TEST_DB ortam değişkeninden, yoksa yerel SQLEXPRESS / LotTakip_Test.
/// </summary>
public sealed class TestVeritabani : IAsyncLifetime
{
    public static string Baglanti { get; } =
        Environment.GetEnvironmentVariable("LOTTAKIP_TEST_DB")
        ?? @"Server=localhost\SQLEXPRESS;Database=LotTakip_Test;Trusted_Connection=True;TrustServerCertificate=True";

    public const string AdminSifre = "test-admin-sifresi";

    /// <summary>Komutlar veritabanına gitmeden hemen önce çalışan test kancası (hata, çakışma üretmek için).</summary>
    public KomutKancasi Kanca { get; } = new();

    public AppDbContext YeniContext() =>
        new(new DbContextOptionsBuilder<AppDbContext>().UseSqlServer(Baglanti).AddInterceptors(Kanca).Options);

    public async Task InitializeAsync()
    {
        await using var db = YeniContext();
        await db.Database.EnsureDeletedAsync();
        await db.Database.MigrateAsync();
    }

    public Task DisposeAsync() => Task.CompletedTask;

    /// <summary>Kalıcı (commit edilmiş) tüm veriyi siler. Transaction dışında çalışan testler için.</summary>
    public async Task TumVeriyiSilAsync()
    {
        await using var db = YeniContext();
        await db.Database.ExecuteSqlRawAsync("""
            DELETE FROM SatisKalemi; DELETE FROM Satis; DELETE FROM Musteri; DELETE FROM StokDuzeltme;
            DELETE FROM UretimTuketim; DELETE FROM Uretim; DELETE FROM UrunAgaci; DELETE FROM Urun;
            DELETE FROM StokLotu; DELETE FROM Tedarikci; DELETE FROM Parca; DELETE FROM Kullanici;
            """);
    }
}

/// <summary>
/// Her komuttan önce <see cref="Oncesi"/> çağrılır (null değilse). Test, belirli bir komutta hata
/// fırlatabilir ya da başka bir bağlantıdan araya girip çakışma oluşturabilir.
/// </summary>
public sealed class KomutKancasi : DbCommandInterceptor
{
    public Func<DbCommand, Task>? Oncesi { get; set; }

    public override async ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(
        DbCommand command, CommandEventData eventData, InterceptionResult<DbDataReader> result, CancellationToken ct = default)
    {
        if (Oncesi is { } k)
            await k(command);
        return result;
    }

    public override async ValueTask<InterceptionResult<int>> NonQueryExecutingAsync(
        DbCommand command, CommandEventData eventData, InterceptionResult<int> result, CancellationToken ct = default)
    {
        if (Oncesi is { } k)
            await k(command);
        return result;
    }
}

[CollectionDefinition(Ad)]
public sealed class VeritabaniKoleksiyonu : ICollectionFixture<TestVeritabani>
{
    public const string Ad = "Veritabanı";
}

/// <summary>
/// Her test kendi transaction'ında koşar ve sonunda geri alınır: testler birbirini etkilemez,
/// veritabanı her testte boş başlar.
/// </summary>
[Collection(VeritabaniKoleksiyonu.Ad)]
public abstract class VeritabaniTesti(TestVeritabani vt) : IAsyncLifetime
{
    protected TestVeritabani Vt { get; } = vt;
    protected AppDbContext Db { get; private set; } = null!;
    private IDbContextTransaction? _tx;

    public virtual async Task InitializeAsync()
    {
        Db = Vt.YeniContext();
        _tx = await Db.Database.BeginTransactionAsync();
    }

    public virtual async Task DisposeAsync()
    {
        Vt.Kanca.Oncesi = null;
        if (_tx is not null)
            await _tx.RollbackAsync();
        await Db.DisposeAsync();
    }

    /// <summary>Örnek veriyi bu testin transaction'ı içinde yükler.</summary>
    protected Task<OrnekVeri.Sonuc> OrnekVeriYukle() =>
        OrnekVeri.Olustur(Db).YukleAsync(TestVeritabani.AdminSifre, sifirla: false);
}
