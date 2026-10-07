using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace LotTakip.DataAccess;

public static class DataAccessKurulum
{
    /// <summary>AppDbContext'i SQL Server bağlantısıyla ve repository'leri kaydeder.</summary>
    public static IServiceCollection AddDataAccess(this IServiceCollection services, string baglanti) =>
        services.AddDataAccess(_ => baglanti);

    /// <summary>
    /// Bağlantı cümlesi ilk ihtiyaçta okunur (ör. ayarlardan); testler ayarı uygulama kurulduktan
    /// sonra da değiştirebilir.
    /// </summary>
    public static IServiceCollection AddDataAccess(this IServiceCollection services, Func<IServiceProvider, string> baglanti)
    {
        services.AddDbContext<AppDbContext>((sp, o) => o.UseSqlServer(baglanti(sp)));
        services.AddScoped<IStokLotuRepository, StokLotuRepository>();
        services.AddScoped<IUretimRepository, UretimRepository>();
        return services;
    }
}
