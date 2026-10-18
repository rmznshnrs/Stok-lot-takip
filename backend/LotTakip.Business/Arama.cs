using System.Globalization;
using System.Text;

namespace LotTakip.Business;

/// <summary>
/// Metin araması. Veritabanı büyük/küçük harf duyarsız ama Türkçe olmayan sıralama kuralıyla çalışır;
/// orada "i" ile "İ", "ı" ile "I" eşleşmez. Aramada bu dört harf birbirinin yerine geçer:
/// "indikatör" → "İndikatör", "kirmizi" → "Kırmızı".
/// </summary>
public static class Arama
{
    private static readonly CultureInfo Turkce = CultureInfo.GetCultureInfo("tr-TR");

    /// <summary>EF.Functions.Like için "içinde geçer" kalıbı: LIKE özel karakterleri kaçırılır, i/ı/I/İ tek sınıf olur.</summary>
    public static string Desen(string metin)
    {
        var d = new StringBuilder("%");
        foreach (var c in metin)
        {
            d.Append(c switch
            {
                'i' or 'I' or 'İ' or 'ı' => "[iİı]",
                '%' or '_' or '[' => $"[{c}]",
                _ => c.ToString(),
            });
        }
        return d.Append('%').ToString();
    }

    /// <summary>Bellekteki karşılaştırma için aynı kural.</summary>
    public static bool Eslesir(string metin, string aranan) => Katla(metin).Contains(Katla(aranan), StringComparison.Ordinal);

    private static string Katla(string s) => s.ToLower(Turkce).Replace('ı', 'i');
}
