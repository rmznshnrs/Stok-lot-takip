using LotTakip.DataAccess;
using Microsoft.EntityFrameworkCore;
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

    public AppDbContext YeniContext() =>
        new(new DbContextOptionsBuilder<AppDbContext>().UseSqlServer(Baglanti).Options);

    public async Task InitializeAsync()
    {
        await using var db = YeniContext();
        await db.Database.EnsureDeletedAsync();
        await db.Database.MigrateAsync();
    }

    public Task DisposeAsync() => Task.CompletedTask;
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
        if (_tx is not null)
            await _tx.RollbackAsync();
        await Db.DisposeAsync();
    }

    /// <summary>Örnek veriyi bu testin transaction'ı içinde yükler.</summary>
    protected Task<OrnekVeri.Sonuc> OrnekVeriYukle() =>
        OrnekVeri.YukleAsync(Db, TestVeritabani.AdminSifre, sifirla: false);
}
