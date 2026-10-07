using LotTakip.Business;
using LotTakip.DataAccess;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

var baglanti = builder.Configuration.GetConnectionString("LotTakip")
    ?? throw new InvalidOperationException(
        "ConnectionStrings:LotTakip ayarı yok. appsettings.Development.example.json dosyasını " +
        "appsettings.Development.json olarak kopyalayın.");

builder.Services.AddDataAccess(baglanti);
builder.Services.AddBusiness();
builder.Services.AddControllers();

var app = builder.Build();

// Komut: dotnet run -- ornek-veri [--sifirla]  → veritabanını günceller, örnek veriyi yükler, çıkar
if (args.Contains("ornek-veri"))
{
    using var kapsam = app.Services.CreateScope();
    var db = kapsam.ServiceProvider.GetRequiredService<AppDbContext>();
    await db.Database.MigrateAsync();
    try
    {
        var sonuc = await kapsam.ServiceProvider.GetRequiredService<OrnekVeri>()
            .YukleAsync(app.Configuration["OrnekVeri:AdminSifre"], sifirla: args.Contains("--sifirla"));
        Console.WriteLine($"Örnek veri yüklendi: {sonuc.Parca} parça, {sonuc.Lot} lot, {sonuc.Urun} ürün, " +
                          $"{sonuc.Uretim} üretim, {sonuc.Satis} satış.");
    }
    catch (InvalidOperationException hata)
    {
        Console.Error.WriteLine(hata.Message);
        Environment.ExitCode = 1;
    }
    return;
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

app.Run();

// Testlerin WebApplicationFactory ile API'yi ayağa kaldırabilmesi için
public partial class Program;
