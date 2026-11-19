using System.Globalization;
using System.Text;

namespace BusinessLayer;

public static class Arama
{
    private static readonly CultureInfo Turkce = CultureInfo.GetCultureInfo("tr-TR");

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

    public static bool Eslesir(string metin, string aranan) => Katla(metin).Contains(Katla(aranan), StringComparison.Ordinal);

    private static string Katla(string s) => s.ToLower(Turkce).Replace('ı', 'i');
}
