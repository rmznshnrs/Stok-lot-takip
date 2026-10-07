using LotTakip.Api;
using LotTakip.Api.Kimlik;
using LotTakip.Business;
using LotTakip.DataAccess;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDataAccess(sp =>
    sp.GetRequiredService<IConfiguration>().GetConnectionString("LotTakip")
    ?? throw new InvalidOperationException(
        "ConnectionStrings:LotTakip ayarı yok. appsettings.Development.example.json dosyasını " +
        "appsettings.Development.json olarak kopyalayın."));
builder.Services.AddBusiness();
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddKimlik();
builder.Services.AddControllers();
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<HataIsleyici>();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(o =>
{
    o.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Lot Takip API",
        Version = "v1",
    });
    o.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
    });
    o.AddSecurityRequirement(belge => new OpenApiSecurityRequirement
    {
        [new OpenApiSecuritySchemeReference("Bearer", belge)] = [],
    });
});

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

app.UseExceptionHandler();
app.UseStatusCodePages();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(o =>
    {
        o.DocumentTitle = "Lot Takip API";
        o.EnablePersistAuthorization();  // sayfa yenilense de token hatırlansın
    });
}
else
{
    app.UseHttpsRedirection();
}

app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();

// Testlerin WebApplicationFactory ile API'yi ayağa kaldırabilmesi için
public partial class Program;
