using LotTakip.DataAccess;
using LotTakip.Entity;
using LotTakip.Shared;
using Microsoft.EntityFrameworkCore;

namespace LotTakip.Business;

public interface IStokServisi
{
    /// <summary>Parçalar ve stok bilgileri (toplam, kullanılabilir, lot sayısı, kullanıldığı ürünler).</summary>
    Task<List<ParcaStokDto>> ParcaStoklariAsync(string? arama = null, CancellationToken ct = default);

    /// <summary>
    /// Parça bazında lotlar (FIFO sırasıyla) ve sıradaki lot. Arama parça kodu/adı veya lot no'da;
    /// parça eşleşirse tüm lotları, yalnız lot eşleşirse eşleşen lotları gösterir.
    /// bitenler=false ise kalanı 0 olan lotlar gizlenir.
    /// </summary>
    Task<List<StokGrubuDto>> StokDurumuAsync(string? arama = null, string? parcaKod = null, bool bitenler = false,
                                             CancellationToken ct = default);

    /// <summary>Yeni lot; tedarikçi adı yoksa tedarikçiyi de oluşturur.</summary>
    Task<MalGirisiSonucuDto> MalGirisiAsync(MalGirisiIstek istek, int kullaniciId, CancellationToken ct = default);

    /// <summary>Kod, ad veya "KOD – Ad" yazımından parçayı bulur; kısmi tek eşleşmeyi kabul eder.</summary>
    Task<ParcaOzetDto> ParcaBulAsync(string metin, CancellationToken ct = default);
}

public class StokServisi(AppDbContext db) : IStokServisi
{
    public async Task<List<ParcaStokDto>> ParcaStoklariAsync(string? arama = null, CancellationToken ct = default)
    {
        var q = db.Parcalar.AsQueryable();
        arama = arama?.Trim();
        if (!string.IsNullOrEmpty(arama))
            q = q.Where(p => p.Kod.Contains(arama) || p.Ad.Contains(arama));
        return await StokluListeAsync(q, ct);
    }

    private static async Task<List<ParcaStokDto>> StokluListeAsync(IQueryable<Parca> q, CancellationToken ct)
    {
        var satirlar = await q.OrderBy(p => p.Kod).Select(p => new
        {
            p.Id, p.Kod, p.Ad, p.Birim, p.MinStok,
            Toplam = p.Lotlar.Sum(l => (int?)l.KalanAdet) ?? 0,
            Kullanilabilir = p.Lotlar.Where(l => !l.GeriCagrildi).Sum(l => (int?)l.KalanAdet) ?? 0,
            LotSayisi = p.Lotlar.Count(l => l.KalanAdet > 0),
            Urunler = p.AgacSatirlari.OrderBy(a => a.Urun!.Ad).Select(a => a.Urun!.Ad).ToList(),
        }).ToListAsync(ct);
        return [.. satirlar.Select(s => new ParcaStokDto(
            s.Id, s.Kod, s.Ad, s.Birim, s.MinStok, s.Toplam, s.Kullanilabilir, s.LotSayisi, s.Urunler))];
    }

    public async Task<List<StokGrubuDto>> StokDurumuAsync(string? arama = null, string? parcaKod = null,
                                                          bool bitenler = false, CancellationToken ct = default)
    {
        arama = arama?.Trim();
        var parcalar = db.Parcalar.AsQueryable();
        if (!string.IsNullOrWhiteSpace(parcaKod))
        {
            var kod = parcaKod.Trim();
            parcalar = parcalar.Where(p => p.Kod == kod);
        }

        HashSet<int> parcaEslesen = [];
        if (!string.IsNullOrEmpty(arama))
        {
            parcaEslesen = [.. await db.Parcalar.Where(p => p.Kod.Contains(arama) || p.Ad.Contains(arama))
                                                .Select(p => p.Id).ToListAsync(ct)];
            var lotEslesen = await db.StokLotlari.Where(l => l.LotNo.Contains(arama))
                                                 .Select(l => l.ParcaId).Distinct().ToListAsync(ct);
            var idler = parcaEslesen.Union(lotEslesen).ToList();
            parcalar = parcalar.Where(p => idler.Contains(p.Id));
        }

        var stoklar = await StokluListeAsync(parcalar, ct);
        var ids = stoklar.Select(p => p.Id).ToList();
        var lotlar = await db.StokLotlari.Include(l => l.Parca).Include(l => l.Tedarikci)
            .Where(l => ids.Contains(l.ParcaId))
            .OrderBy(l => l.SiparisTarihi).ThenBy(l => l.Id)
            .ToListAsync(ct);

        var sonuc = new List<StokGrubuDto>();
        foreach (var parca in stoklar)
        {
            var tum = lotlar.Where(l => l.ParcaId == parca.Id).ToList();
            var siradaki = tum.FirstOrDefault(l => !l.GeriCagrildi && l.KalanAdet > 0);
            var gosterilen = tum.Where(l => bitenler || l.KalanAdet > 0);
            if (!string.IsNullOrEmpty(arama) && !parcaEslesen.Contains(parca.Id))
                gosterilen = gosterilen.Where(l => l.LotNo.Contains(arama, StringComparison.OrdinalIgnoreCase));
            sonuc.Add(new StokGrubuDto(
                parca,
                [.. gosterilen.Select(l => Donusum.Lot(l, sirada: l == siradaki))],
                siradaki is null ? null : Donusum.Lot(siradaki, sirada: true)));
        }
        return sonuc;
    }

    public Task<MalGirisiSonucuDto> MalGirisiAsync(MalGirisiIstek istek, int kullaniciId, CancellationToken ct = default)
    {
        var lotNo = (istek.LotNo ?? "").Trim();
        var tedarikciAdi = Donusum.Sadelestir(istek.TedarikciAdi);
        if (lotNo.Length == 0 || tedarikciAdi.Length == 0)
            throw new IsKuraliHatasi("Lot no ve tedarikçi boş olamaz.");
        if (istek.Adet < 1)
            throw new IsKuraliHatasi("Adet en az 1 olmalı.");

        return Islem.CalistirAsync(db, async () =>
        {
            var kullanici = await Kullanicilar.GetirAsync(db, kullaniciId, ct);
            var parca = await db.Parcalar.SingleOrDefaultAsync(p => p.Id == istek.ParcaId, ct)
                        ?? throw new BulunamadiHatasi("Parça bulunamadı.");
            if (await db.StokLotlari.AnyAsync(l => l.LotNo == lotNo, ct))
                throw new IsKuraliHatasi($"{lotNo} lot numarası zaten kayıtlı.");

            var tedarikci = await db.Tedarikciler.SingleOrDefaultAsync(t => t.Ad == tedarikciAdi, ct);
            var yeni = tedarikci is null;
            tedarikci ??= new Tedarikci { Ad = tedarikciAdi };

            var simdi = Zaman.Simdi();
            var lot = new StokLotu
            {
                Parca = parca, Tedarikci = tedarikci, LotNo = lotNo, SiparisTarihi = istek.SiparisTarihi,
                SiparisNo = (istek.SiparisNo ?? "").Trim(), GirisAdet = istek.Adet, KalanAdet = istek.Adet,
                GirisZamani = simdi, OlusturanKullanici = kullanici, OlusturmaZamani = simdi,
            };
            db.StokLotlari.Add(lot);
            await db.SaveChangesAsync(ct);
            return new MalGirisiSonucuDto(Donusum.Lot(lot), yeni);
        }, ct);
    }

    public async Task<ParcaOzetDto> ParcaBulAsync(string metin, CancellationToken ct = default)
    {
        var deger = (metin ?? "").Trim();
        if (deger.Length == 0)
            throw new IsKuraliHatasi("Parça kodu veya adı girin.");
        var kod = deger.Split(" – ")[0].Trim();  // öneri listesindeki "KOD – Ad" biçimi
        var parca = await db.Parcalar.FirstOrDefaultAsync(p => p.Kod == kod, ct)
                    ?? await db.Parcalar.FirstOrDefaultAsync(p => p.Ad == deger, ct);
        if (parca is not null)
            return Donusum.Ozet(parca);

        var adaylar = await db.Parcalar.Where(p => p.Kod.Contains(deger) || p.Ad.Contains(deger))
                                       .OrderBy(p => p.Kod).Take(6).ToListAsync(ct);
        return adaylar.Count switch
        {
            1 => Donusum.Ozet(adaylar[0]),
            0 => throw new BulunamadiHatasi($"\"{deger}\" adlı veya kodlu parça bulunamadı."),
            _ => throw new IsKuraliHatasi($"\"{deger}\" birden fazla parçayla eşleşti: " +
                                          $"{string.Join(", ", adaylar.Select(p => p.Kod))}. Kodu yazın."),
        };
    }
}

public interface IStokDuzeltmeServisi
{
    /// <summary>
    /// Admin'in açıklamalı stok düzeltmesi (sayım farkı, fire, diğer). Miktar + veya -;
    /// kalan adet negatife düşemez.
    /// </summary>
    Task<StokDuzeltmeSonucuDto> DuzeltAsync(StokDuzeltmeIstek istek, int kullaniciId, CancellationToken ct = default);
}

public class StokDuzeltmeServisi(AppDbContext db) : IStokDuzeltmeServisi
{
    public Task<StokDuzeltmeSonucuDto> DuzeltAsync(StokDuzeltmeIstek istek, int kullaniciId, CancellationToken ct = default)
    {
        var aciklama = (istek.Aciklama ?? "").Trim();
        if (istek.Miktar == 0)
            throw new IsKuraliHatasi("Düzeltme miktarı sıfır olamaz.");
        if (aciklama.Length == 0)
            throw new IsKuraliHatasi("Stok düzeltmede açıklama zorunludur.");
        if (!Enum.TryParse<StokDuzeltmeTuru>(istek.Tur, ignoreCase: true, out var tur) || !Enum.IsDefined(tur))
            throw new IsKuraliHatasi("Düzeltme türü SayimFarki, Fire veya Diger olmalı.");

        return Islem.CalistirAsync(db, async () =>
        {
            var admin = await Kullanicilar.AdminGetirAsync(db, kullaniciId, "Stok düzeltme", ct);
            var lot = await db.StokLotlari.SingleOrDefaultAsync(l => l.Id == istek.StokLotuId, ct)
                      ?? throw new BulunamadiHatasi("Lot bulunamadı.");
            var yeniKalan = lot.KalanAdet + istek.Miktar;
            if (yeniKalan < 0)
                throw new IsKuraliHatasi($"{lot.LotNo} lotunda {lot.KalanAdet} adet var; {-istek.Miktar} adet düşülemez.");

            lot.KalanAdet = yeniKalan;  // RowVersion: aynı anda değişirse yeniden denenir
            var duzeltme = new StokDuzeltme
            {
                StokLotu = lot, Miktar = istek.Miktar, Tur = tur, Aciklama = aciklama,
                OlusturanKullanici = admin, OlusturmaZamani = Zaman.Simdi(),
            };
            db.StokDuzeltmeleri.Add(duzeltme);
            await db.SaveChangesAsync(ct);
            return new StokDuzeltmeSonucuDto(duzeltme.Id, lot.LotNo, istek.Miktar, tur.ToString(), aciklama, yeniKalan);
        }, ct);
    }
}
