using System.Xml.Linq;

namespace LotTakip.Tests;

/// <summary>
/// Katman kurallarını proje referanslarından denetler.
/// .csproj okunur: derleyici kullanılmayan referansları DLL'den çıkardığı için
/// derlenmiş assembly'lere bakmak kural ihlalini gizleyebilir.
/// </summary>
public class MimariTestleri
{
    private static readonly Dictionary<string, string[]> IzinVerilen = new()
    {
        ["LotTakip.Entity"] = [],
        ["LotTakip.Shared"] = [],
        ["LotTakip.DataAccess"] = ["LotTakip.Entity"],
        ["LotTakip.Business"] = ["LotTakip.Entity", "LotTakip.DataAccess", "LotTakip.Shared"],
        ["LotTakip.Api"] = ["LotTakip.Business", "LotTakip.Shared", "LotTakip.DataAccess"],
    };

    private static string BackendKlasoru()
    {
        var klasor = new DirectoryInfo(AppContext.BaseDirectory);
        while (klasor is not null && !File.Exists(Path.Combine(klasor.FullName, "LotTakip.sln")))
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
    [InlineData("LotTakip.Entity")]
    [InlineData("LotTakip.Shared")]
    [InlineData("LotTakip.DataAccess")]
    [InlineData("LotTakip.Business")]
    [InlineData("LotTakip.Api")]
    public void Katman_yalnizca_izin_verilen_katmanlara_baglidir(string proje)
    {
        var fazla = Referanslar(proje).Except(IzinVerilen[proje]).ToArray();
        Assert.True(fazla.Length == 0, $"{proje} izinsiz katmana bağlı: {string.Join(", ", fazla)}");
    }

    [Fact]
    public void Dto_katmani_entity_bilmez_ve_istemciler_is_kurali_tekrarlamaz()
    {
        Assert.DoesNotContain("LotTakip.Entity", Referanslar("LotTakip.Shared"));
        Assert.DoesNotContain("LotTakip.Api", Referanslar("LotTakip.Business"));
    }
}
