using ClosedXML.Excel;
using Microsoft.EntityFrameworkCore;

namespace BusinessLayer.Concrete;

public class RaporServisi(AppDbContext db, IOzetServisi ozet, ISiparisServisi siparis, IUretimServisi uretim,
                          ISevkiyatServisi sevkiyat, IUretimSirasiServisi sira, IStokServisi stok,
                          IGeriCagirmaServisi geriCagirma) : IRaporServisi
{
    public static readonly int[] Aylar = [1, 3, 6, 12];

    private static readonly Dictionary<string, string> SiparisDurumu = new()
    {
        ["bekliyor"] = "Bekliyor", ["uretimde"] = "Üretimde", ["hazir"] = "Gönderime hazır",
        ["kismen_hazir"] = "Kısmen hazır", ["kargoya_verilecek"] = "Kargoya verilecek", ["kismen_gonderildi"] = "Kısmen gönderildi", ["tamamlandi"] = "Tamamlandı", ["iptal"] = "İptal edildi",
    };
    private static readonly Dictionary<string, string> SevkiyatDurumu = new()
    {
        ["Hazirlaniyor"] = "Kargoya verilecek", ["Kargoda"] = "Kargoda", ["TeslimEdildi"] = "Teslim edildi",
    };
    private static readonly Dictionary<string, string> GeriDurumu = new()
    {
        ["musteride"] = "Müşteriden geri istendi", ["geri_alindi"] = "Geri alındı", ["depoda"] = "Depoda, satışa kapalı",
        ["hurda"] = "Hurdaya ayrıldı", ["satisa_acildi"] = "Satışa açıldı", ["musteriye_verildi"] = "Müşteriye geri verildi",
    };

    public async Task<(byte[] Icerik, string DosyaAdi)> ExcelAsync(int ay, CancellationToken ct = default)
    {
        if (!Aylar.Contains(ay))
            throw new IsKuraliHatasi("Süre 1, 3, 6 veya 12 ay olmalı.");
        var bugun = Zaman.Bugun();
        var baslangic = bugun.AddMonths(-ay);
        var ilkAn = new DateTimeOffset(baslangic.ToDateTime(TimeOnly.MinValue), TimeSpan.FromHours(3));

        var o = await ozet.AnaSayfaAsync(ct);
        var siparisler = await siparis.ListeAsync(ct: ct, baslangic: baslangic);
        var uretimler = (await uretim.UretimGecmisiAsync(adet: int.MaxValue, ct: ct)).Where(u => u.Tarih >= ilkAn).ToList();
        var sevkiyatlar = await sevkiyat.ListeAsync(ct: ct, baslangic: baslangic);
        var bekleyen = await sira.ListeAsync(ct: ct);
        var parcalar = await stok.ParcaStoklariAsync(ct: ct);
        var geriCagirmalar = (await geriCagirma.ListeAsync(ct: ct)).Where(g => g.Zaman >= ilkAn).ToList();
        var girisler = await db.StokLotlari.AsNoTracking().Where(l => l.GirisZamani >= ilkAn)
            .OrderByDescending(l => l.GirisZamani)
            .Select(l => new { l.GirisZamani, l.LotNo, ParcaKod = l.Parca!.Kod, ParcaAd = l.Parca.Ad, TedarikciAd = l.Tedarikci!.Ad,
                               l.SiparisNo, l.TedarikciLotNo, l.GirisAdet, l.KalanAdet })
            .ToListAsync(ct);

        using var wb = new XLWorkbook();

        var ozetSayfa = wb.Worksheets.Add("Özet");
        ozetSayfa.Cell(1, 1).Value = $"Rapor: son {ay} ay ({baslangic:dd.MM.yyyy} – {bugun:dd.MM.yyyy})";
        ozetSayfa.Cell(1, 1).Style.Font.Bold = true;
        ozetSayfa.Cell(1, 1).Style.Font.FontSize = 14;
        Tablo(ozetSayfa, 3, ["Bilgi", "Değer"],
        [
            ["Açık sipariş", o.AcikSiparis],
            ["Üretim sırasındaki iş", o.SiradakiIs],
            ["Üretim sırasındaki ürün", o.SiradakiAdet],
            ["Kargoya verilecek sevkiyat", o.HazirlananSevkiyat],
            ["Kargodaki sevkiyat", o.KargodaSevkiyat],
            ["Depodaki ürün", o.DepodaUrun],
            ["Minimum stok altındaki parça", o.MinAltindaParca],
            ["Açık geri çağırma", o.AcikGeriCagirma],
            [$"Dönemde alınan sipariş", siparisler.Count],
            [$"Dönemde üretilen ürün", uretimler.Count],
            [$"Dönemde yapılan sevkiyat", sevkiyatlar.Count],
            [$"Dönemde mal girişi", girisler.Count],
        ]);
        var satir = 18;
        ozetSayfa.Cell(satir - 1, 1).Value = "Üretim uyarıları (parça stoğuyla 5 ve daha az üretilebilen ürünler)";
        ozetSayfa.Cell(satir - 1, 1).Style.Font.Bold = true;
        satir = Tablo(ozetSayfa, satir, ["Ürün", "Üretilebilir", "Yetmeyen parçalar"],
            [.. o.Uyarilar.Select(u => new object?[] { u.UrunAd, u.Uretilebilir,
                string.Join(", ", u.Kisitlayanlar.Select(k => $"{k.Parca.Ad}: {k.Serbest} serbest (1 üründe {k.BirimAdet})")) })]) + 2;
        ozetSayfa.Cell(satir - 1, 1).Value = "Yaklaşan teslimler";
        ozetSayfa.Cell(satir - 1, 1).Style.Font.Bold = true;
        Tablo(ozetSayfa, satir, ["Teslim", "Sipariş", "Müşteri", "Gönderilecek"],
            [.. o.YaklasanTeslimler.Select(t => new object?[] { t.IstenenTarih, t.No, t.MusteriAd, t.Kalan })]);

        Tablo(wb.Worksheets.Add("Talepler"), 1,
            ["Sipariş No", "Tarih", "Müşteri", "İstenen Teslim", "Ürün", "Adet", "Ayrılan", "Gönderilen", "Üretimde", "Eksik", "Durum", "Açıklama"],
            [.. siparisler.SelectMany(s => s.Kalemler.Select(k => new object?[] {
                s.SiparisNo, s.Tarih, s.MusteriAd, s.IstenenTarih, k.UrunAd, k.Adet, k.Ayrilanlar.Count, k.Gonderilenler.Count,
                k.Uretimde, k.Eksik, SiparisDurumu.GetValueOrDefault(s.Durum, s.Durum), s.Aciklama }))]);

        Tablo(wb.Worksheets.Add("Üretim Sırası"), 1,
            ["Sıra", "Ürün", "Adet", "Üretilen", "Planlanan Gün", "Müşteri", "Talep Tarihi", "Ayrılan Parçalar", "Açıklama"],
            [.. bekleyen.Select((e, i) => new object?[] {
                i + 1, e.UrunAd, e.Adet, e.Uretilen, e.PlanlananTarih, e.MusteriAd, e.TalepTarihi,
                string.Join(", ", e.Parcalar.Select(p => $"{p.Parca.Kod} {p.Ayrilan}")), e.Aciklama })]);

        Tablo(wb.Worksheets.Add("Üretim"), 1,
            ["Seri No", "Ürün", "Üretim Tarihi", "Durum", "Müşteri", "Gönderim Tarihi", "Hatalı Lot"],
            [.. uretimler.Select(u => new object?[] {
                u.SeriNo, u.UrunAd, Yerel(u.Tarih),
                u.Hurda is not null ? (u.Hurda.Tur == "Kayip" ? "Kayıp" : "Hurda") : u.KargoyaVerilecek ? "Kargoya verilecek" : u.MusteriAd is not null ? "Gönderildi" : "Depoda",
                u.MusteriAd, u.SatisTarihi, string.Join(", ", u.GeriCagrilanLotlar) })]);

        Tablo(wb.Worksheets.Add("Sevkiyat"), 1,
            ["Sevkiyat No", "Tarih", "Müşteri", "Sipariş No", "Ürünler", "Seri Numaraları", "Kargo Firması", "Takip No", "Durum",
             "Kargoya Verildi", "Teslim Edildi"],
            [.. sevkiyatlar.Select(s => new object?[] {
                s.No, s.Zaman is { } z ? Yerel(z) : s.Tarih, s.MusteriAd, s.SiparisNo,
                string.Join(", ", s.Kalemler.GroupBy(k => k.UrunAd).Select(g => $"{g.Key} × {g.Count()}")),
                string.Join(", ", s.Kalemler.Select(k => k.SeriNo)), s.KargoFirmasi, s.TakipNo,
                SevkiyatDurumu.GetValueOrDefault(s.Durum, s.Durum),
                s.KargoZamani is { } k ? Yerel(k) : null, s.TeslimZamani is { } t ? Yerel(t) : null })]);

        Tablo(wb.Worksheets.Add("Mal Girişi"), 1,
            ["Giriş Zamanı", "Lot No", "Parça Kodu", "Parça", "Tedarikçi", "Sipariş No", "Tedarikçi Lot No", "Gelen", "Elde Kalan"],
            [.. girisler.Select(l => new object?[] {
                Yerel(l.GirisZamani), l.LotNo, l.ParcaKod, l.ParcaAd, l.TedarikciAd, l.SiparisNo, l.TedarikciLotNo, l.GirisAdet, l.KalanAdet })]);

        Tablo(wb.Worksheets.Add("Parça Stoğu"), 1,
            ["Kod", "Parça", "Toplam", "Kullanılabilir", "Sıraya Ayrılan", "Serbest", "Minimum", "Hatalı", "Kullanıldığı Ürünler"],
            [.. parcalar.Select(p => new object?[] {
                p.Kod, p.Ad, p.ToplamStok, p.KullanilabilirStok, p.Ayrilan, p.Serbest, p.MinStok, p.HataliAdet,
                string.Join(", ", p.KullanildigiUrunler) })]);

        Tablo(wb.Worksheets.Add("Geri Çağırmalar"), 1,
            ["Seri No", "Ürün", "Tarih", "Durum", "Müşteri", "Neden", "Lot No"],
            [.. geriCagirmalar.Select(g => new object?[] {
                g.SeriNo, g.UrunAd, Yerel(g.Zaman), GeriDurumu.GetValueOrDefault(g.Durum, g.Durum), g.MusteriAd, g.Aciklama, g.LotNo })]);

        using var akis = new MemoryStream();
        wb.SaveAs(akis);
        return (akis.ToArray(), $"rapor-son-{ay}-ay-{bugun:yyyy-MM-dd}.xlsx");
    }

    private static DateTime Yerel(DateTimeOffset an) => TimeZoneInfo.ConvertTime(an, Zaman.Istanbul).DateTime;

    private static int Tablo(IXLWorksheet s, int ilkSatir, string[] basliklar, IReadOnlyList<object?[]> satirlar)
    {
        for (var i = 0; i < basliklar.Length; i++)
        {
            var c = s.Cell(ilkSatir, i + 1);
            c.Value = basliklar[i];
            c.Style.Font.Bold = true;
            c.Style.Font.FontColor = XLColor.White;
            c.Style.Fill.BackgroundColor = XLColor.FromHtml("#1D9E75");
        }
        for (var r = 0; r < satirlar.Count; r++)
            for (var i = 0; i < basliklar.Length && i < satirlar[r].Length; i++)
                Yaz(s.Cell(ilkSatir + 1 + r, i + 1), satirlar[r][i]);
        if (satirlar.Count == 0)
            s.Cell(ilkSatir + 1, 1).Value = "Kayıt yok";
        if (ilkSatir == 1)
        {
            s.SheetView.FreezeRows(1);
            s.Range(1, 1, Math.Max(2, satirlar.Count + 1), basliklar.Length).SetAutoFilter();
        }
        s.Columns(1, basliklar.Length).AdjustToContents(8.0, 60.0);
        return ilkSatir + Math.Max(1, satirlar.Count) + 1;
    }

    private static void Yaz(IXLCell c, object? deger)
    {
        switch (deger)
        {
            case null:
                return;
            case DateOnly d:
                c.Value = d.ToDateTime(TimeOnly.MinValue);
                c.Style.DateFormat.Format = "dd.MM.yyyy";
                break;
            case DateTime t:
                c.Value = t;
                c.Style.DateFormat.Format = "dd.MM.yyyy HH:mm";
                break;
            case int n:
                c.Value = n;
                break;
            default:
                c.Value = deger.ToString();
                break;
        }
    }
}
