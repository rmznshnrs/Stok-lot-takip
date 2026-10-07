using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace LotTakip.DataAccess;

/// <summary>
/// "dotnet ef migrations add" için: API'yi çalıştırmadan context oluşturur.
/// Bağlantı LOTTAKIP_DB ortam değişkeninden, yoksa yerel SQLEXPRESS.
/// </summary>
public class TasarimZamaniFabrikasi : IDesignTimeDbContextFactory<AppDbContext>
{
    public const string VarsayilanBaglanti =
        @"Server=localhost\SQLEXPRESS;Database=LotTakip;Trusted_Connection=True;TrustServerCertificate=True";

    public AppDbContext CreateDbContext(string[] args)
    {
        var baglanti = Environment.GetEnvironmentVariable("LOTTAKIP_DB") ?? VarsayilanBaglanti;
        return new AppDbContext(new DbContextOptionsBuilder<AppDbContext>().UseSqlServer(baglanti).Options);
    }
}
