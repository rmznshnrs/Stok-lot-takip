using Microsoft.EntityFrameworkCore;

namespace BusinessLayer;

/// <summary>Sıradaki (bekleyen) üretim işlerine ayrılmış parçalar.</summary>
internal static class Ayrilan
{
    /// <summary>Parça başına ayrılan adet: BirimAdet × kalan üretim. haricEmirId: bu işin ayırdığı sayılmaz.</summary>
    public static async Task<Dictionary<int, int>> ParcalarAsync(AppDbContext db, IReadOnlyCollection<int>? parcaIdleri,
                                                                 int? haricEmirId, CancellationToken ct)
    {
        var q = db.UretimEmriParcalari.AsNoTracking()
            .Where(p => p.UretimEmri!.Durum == UretimEmriDurumu.Bekliyor && p.UretimEmriId != haricEmirId);
        if (parcaIdleri is not null)
            q = q.Where(p => parcaIdleri.Contains(p.ParcaId));
        return await q.GroupBy(p => p.ParcaId)
            .Select(g => new { g.Key, Adet = g.Sum(p => p.BirimAdet * (p.UretimEmri!.Adet - p.UretimEmri.Uretilen)) })
            .ToDictionaryAsync(x => x.Key, x => x.Adet, ct);
    }
}
