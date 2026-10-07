using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace LotTakip.DataAccess;

public static class DataAccessKurulum
{
    /// <summary>AppDbContext'i SQL Server bağlantısıyla ve repository'leri kaydeder.</summary>
    public static IServiceCollection AddDataAccess(this IServiceCollection services, string baglanti)
    {
        services.AddDbContext<AppDbContext>(o => o.UseSqlServer(baglanti));
        services.AddScoped<IStokLotuRepository, StokLotuRepository>();
        services.AddScoped<IUretimRepository, UretimRepository>();
        return services;
    }
}
