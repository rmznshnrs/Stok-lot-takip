using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using System.Text;
using LotTakip.Shared;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace LotTakip.Api.Kimlik;

/// <summary>"Jwt" ayar bölümü. Anahtar appsettings.Development.json veya ortam değişkeninden (Jwt__Anahtar).</summary>
public class JwtAyarlari
{
    public const string Bolum = "Jwt";

    /// <summary>HMAC SHA-256 imza anahtarı; en az 32 karakter.</summary>
    [Required, MinLength(32, ErrorMessage = "Jwt:Anahtar en az 32 karakter olmalı.")]
    public string Anahtar { get; set; } = "";

    public string Yayinci { get; set; } = "LotTakip";
    public string Hedef { get; set; } = "LotTakip";
    public int GecerlilikSaat { get; set; } = 8;

    public SymmetricSecurityKey ImzaAnahtari() => new(Encoding.UTF8.GetBytes(Anahtar));
}

public static class KimlikKurulum
{
    /// <summary>
    /// JWT ile kimlik doğrulama ve yetki: login hariç tüm uçlar giriş ister (varsayılan politika).
    /// Ayarlar ilk ihtiyaçta okunur; eksik/kısa anahtar uygulama açılırken hata verir.
    /// </summary>
    public static IServiceCollection AddKimlik(this IServiceCollection services)
    {
        services.AddOptions<JwtAyarlari>().BindConfiguration(JwtAyarlari.Bolum).ValidateDataAnnotations().ValidateOnStart();
        services.AddSingleton<TokenUretici>();

        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer();
        services.AddOptions<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme)
            .Configure<IOptions<JwtAyarlari>>((o, ayar) =>
            {
                var j = ayar.Value;
                o.MapInboundClaims = false;  // "sub", "role" adları olduğu gibi kalsın
                o.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidIssuer = j.Yayinci,
                    ValidAudience = j.Hedef,
                    IssuerSigningKey = j.ImzaAnahtari(),
                    ValidAlgorithms = [SecurityAlgorithms.HmacSha256],
                    NameClaimType = JwtRegisteredClaimNames.UniqueName,
                    RoleClaimType = TokenUretici.RolClaim,
                    ClockSkew = TimeSpan.FromMinutes(1),
                };
            });

        services.AddAuthorizationBuilder()
            .SetFallbackPolicy(new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build())
            .AddPolicy(Politikalar.Admin, p => p.RequireRole(Roller.Admin));
        return services;
    }
}

public static class Politikalar
{
    public const string Admin = "Admin";
}

public class TokenUretici(IOptions<JwtAyarlari> ayarlar, TimeProvider zaman)
{
    public const string RolClaim = "role";

    public GirisSonucuDto Uret(KullaniciDto k)
    {
        var j = ayarlar.Value;
        var simdi = zaman.GetUtcNow();
        var son = simdi.AddHours(j.GecerlilikSaat);
        var token = new JsonWebTokenHandler().CreateToken(new SecurityTokenDescriptor
        {
            Issuer = j.Yayinci,
            Audience = j.Hedef,
            IssuedAt = simdi.UtcDateTime,
            NotBefore = simdi.UtcDateTime,
            Expires = son.UtcDateTime,
            SigningCredentials = new SigningCredentials(j.ImzaAnahtari(), SecurityAlgorithms.HmacSha256),
            Claims = new Dictionary<string, object>
            {
                [JwtRegisteredClaimNames.Sub] = k.Id.ToString(),
                [JwtRegisteredClaimNames.UniqueName] = k.KullaniciAdi,
                [JwtRegisteredClaimNames.Name] = $"{k.Ad} {k.Soyad}",
                [RolClaim] = k.Rol,
            },
        });
        return new GirisSonucuDto(token, son, k);
    }
}

public static class ClaimsUzantilari
{
    /// <summary>İşlemi yapan kullanıcının id'si (token'daki "sub"). Kayıtlara bu yazılır.</summary>
    public static int KullaniciId(this ClaimsPrincipal user) =>
        int.TryParse(user.FindFirstValue(JwtRegisteredClaimNames.Sub), out var id)
            ? id
            : throw new InvalidOperationException("Token'da kullanıcı bilgisi yok.");
}
