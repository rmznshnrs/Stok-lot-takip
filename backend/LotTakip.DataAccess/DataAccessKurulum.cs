using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace LotTakip.DataAccess;

public static class DataAccessKurulum
{
    /// <summary>AppDbContext'i SQL Server bağlantısıyla kaydeder.</summary>
    public static IServiceCollection AddDataAccess(this IServiceCollection services, string baglanti)
    {
        services.AddDbContext<AppDbContext>(o => o.UseSqlServer(baglanti));
        return services;
    }
}
