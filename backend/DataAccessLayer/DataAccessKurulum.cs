using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace DataAccessLayer;

public static class DataAccessKurulum
{
    public static IServiceCollection AddDataAccess(this IServiceCollection services, string baglanti) =>
        services.AddDataAccess(_ => baglanti);

    public static IServiceCollection AddDataAccess(this IServiceCollection services, Func<IServiceProvider, string> baglanti)
    {
        services.AddDbContext<AppDbContext>((sp, o) => o.UseSqlServer(baglanti(sp)));
        services.AddScoped<IStokLotuRepository, StokLotuRepository>();
        services.AddScoped<IUretimRepository, UretimRepository>();
        return services;
    }
}
