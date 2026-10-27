using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Options;
using MimeKit;

namespace PresentationLayer;

/// <summary>"Posta" ayarları. Sunucu boşsa e-posta gönderilmez, Klasor'e .eml dosyası olarak yazılır.</summary>
public class PostaAyarlari
{
    public string Sunucu { get; set; } = "";
    public int Port { get; set; } = 587;
    public string Kullanici { get; set; } = "";
    public string Sifre { get; set; } = "";
    public string Gonderen { get; set; } = "lot-takip@localhost";
    public string Klasor { get; set; } = "posta";
    /// <summary>Şifre sıfırlama bağlantısındaki web arayüzü adresi.</summary>
    public string WebAdresi { get; set; } = "http://localhost:3000";
}

/// <summary>
/// SMTP ile gönderir (MailKit). Sunucu ayarlı değilse postayı dosyaya yazar ki geliştirmede bağlantı görülebilsin.
/// Gönderilemezse hata kaydedilir ama isteği yapan kişiye ayrıntı verilmez.
/// </summary>
public class PostaGonderici(IOptions<PostaAyarlari> secenek, IHostEnvironment ortam, ILogger<PostaGonderici> log) : IPostaGonderici
{
    public async Task GonderAsync(string alici, string konu, string metin, CancellationToken ct = default)
    {
        var ayar = secenek.Value;
        var ileti = new MimeMessage();
        ileti.From.Add(new MailboxAddress("MKC Bilişim", ayar.Gonderen));
        ileti.To.Add(MailboxAddress.Parse(alici));
        ileti.Subject = konu;
        ileti.Body = new TextPart("plain") { Text = metin };

        try
        {
            if (string.IsNullOrWhiteSpace(ayar.Sunucu))
            {
                var klasor = Path.Combine(ortam.ContentRootPath, ayar.Klasor);
                Directory.CreateDirectory(klasor);
                var dosya = Path.Combine(klasor, $"{DateTime.Now:yyyyMMdd-HHmmss}-{Guid.NewGuid():N}.eml");
                await ileti.WriteToAsync(dosya, ct);
                log.LogWarning("SMTP ayarlı değil; e-posta dosyaya yazıldı: {Dosya}", dosya);
                return;
            }

            using var smtp = new SmtpClient();
            await smtp.ConnectAsync(ayar.Sunucu, ayar.Port, SecureSocketOptions.StartTlsWhenAvailable, ct);
            if (!string.IsNullOrEmpty(ayar.Kullanici))
                await smtp.AuthenticateAsync(ayar.Kullanici, ayar.Sifre, ct);
            await smtp.SendAsync(ileti, ct);
            await smtp.DisconnectAsync(true, ct);
        }
        catch (Exception hata) when (hata is not OperationCanceledException)
        {
            log.LogError(hata, "E-posta gönderilemedi: {Alici}", alici);
        }
    }
}
