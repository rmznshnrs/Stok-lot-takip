using System.Data.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Storage;

[assembly: CollectionBehavior(DisableTestParallelization = true)]

namespace Tests;

public sealed class TestVeritabani : IAsyncLifetime
{
    public static string Baglanti { get; } =
        Environment.GetEnvironmentVariable("LOTTAKIP_TEST_DB")
        ?? @"Server=localhost\SQLEXPRESS;Database=LotTakip_Test;Trusted_Connection=True;TrustServerCertificate=True";

    public const string AdminSifre = "test-admin-sifresi";

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

    public async Task TumVeriyiSilAsync()
    {
        await using var db = YeniContext();
        await db.Database.ExecuteSqlRawAsync("""
            UPDATE Uretim SET SiparisKalemiId = NULL, UretimEmriId = NULL; UPDATE Satis SET SiparisId = NULL;
            DELETE FROM TakvimPlani; DELETE FROM UretimEmriParcasi; DELETE FROM UretimEmri; DELETE FROM SiparisKalemi; DELETE FROM Siparis;
            DELETE FROM SifreSifirlama; DELETE FROM UrunGeriCagirma; DELETE FROM SatisIade; DELETE FROM UretimHurda;
            DELETE FROM SatisKalemi; DELETE FROM Satis; DELETE FROM Musteri; DELETE FROM StokDuzeltme;
            DELETE FROM UretimTuketim; DELETE FROM Uretim; DELETE FROM UrunAgaci; DELETE FROM Urun;
            DELETE FROM StokLotu; DELETE FROM Tedarikci; DELETE FROM Parca; DELETE FROM Kullanici;
            """);
    }
}

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

    protected Task<OrnekVeri.Sonuc> OrnekVeriYukle() =>
        OrnekVeri.Olustur(Db).YukleAsync(TestVeritabani.AdminSifre, sifirla: false);
}
