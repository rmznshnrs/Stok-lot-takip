using Microsoft.Extensions.DependencyInjection;

namespace LotTakip.Business;

public static class BusinessKurulum
{
    /// <summary>İş servislerini kaydeder (istek başına bir örnek, AppDbContext ile aynı ömür).</summary>
    public static IServiceCollection AddBusiness(this IServiceCollection services)
    {
        services.AddScoped<IStokServisi, StokServisi>();
        services.AddScoped<IStokDuzeltmeServisi, StokDuzeltmeServisi>();
        services.AddScoped<IUretimServisi, UretimServisi>();
        services.AddScoped<IIzlemeServisi, IzlemeServisi>();
        services.AddScoped<ISatisServisi, SatisServisi>();
        services.AddScoped<ITanimServisi, TanimServisi>();
        services.AddScoped<IKullaniciServisi, KullaniciServisi>();
        services.AddScoped<OrnekVeri>();
        return services;
    }
}
