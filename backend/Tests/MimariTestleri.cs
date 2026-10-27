using System.Xml.Linq;

namespace Tests;

/// <summary>
/// Katman kurallarını proje referanslarından denetler.
/// .csproj okunur: derleyici kullanılmayan referansları DLL'den çıkardığı için
/// derlenmiş assembly'lere bakmak kural ihlalini gizleyebilir.
/// </summary>
public class MimariTestleri
{
    private static readonly Dictionary<string, string[]> IzinVerilen = new()
    {
        ["EntityLayer"] = [],
        ["SharedLayer"] = [],
        ["DataAccessLayer"] = ["EntityLayer"],
        ["BusinessLayer"] = ["EntityLayer", "DataAccessLayer", "SharedLayer"],
        ["PresentationLayer"] = ["BusinessLayer", "SharedLayer", "DataAccessLayer"],
    };

    private static string BackendKlasoru()
    {
        var klasor = new DirectoryInfo(AppContext.BaseDirectory);
        while (klasor is not null && !File.Exists(Path.Combine(klasor.FullName, "StokLotSistemi.sln")))
            klasor = klasor.Parent;
        Assert.NotNull(klasor);
        return Path.Combine(klasor!.FullName, "backend");
    }

    private static string[] Referanslar(string proje)
    {
        var yol = Path.Combine(BackendKlasoru(), proje, $"{proje}.csproj");
        return XDocument.Load(yol).Descendants("ProjectReference")
            .Select(r => Path.GetFileNameWithoutExtension(r.Attribute("Include")!.Value))
            .OrderBy(x => x)
            .ToArray();
    }

    [Theory]
    [InlineData("EntityLayer")]
    [InlineData("SharedLayer")]
    [InlineData("DataAccessLayer")]
    [InlineData("BusinessLayer")]
    [InlineData("PresentationLayer")]
    public void Katman_yalnizca_izin_verilen_katmanlara_baglidir(string proje)
    {
        var fazla = Referanslar(proje).Except(IzinVerilen[proje]).ToArray();
        Assert.True(fazla.Length == 0, $"{proje} izinsiz katmana bağlı: {string.Join(", ", fazla)}");
    }

    [Fact]
    public void Dto_katmani_entity_bilmez_ve_istemciler_is_kurali_tekrarlamaz()
    {
        Assert.DoesNotContain("EntityLayer", Referanslar("SharedLayer"));
        Assert.DoesNotContain("PresentationLayer", Referanslar("BusinessLayer"));
    }
}
