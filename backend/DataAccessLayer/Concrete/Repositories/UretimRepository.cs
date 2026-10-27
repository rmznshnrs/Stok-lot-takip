using Microsoft.EntityFrameworkCore;

namespace DataAccessLayer.Concrete.Repositories;

public class UretimRepository(AppDbContext db) : IUretimRepository
{
    public Task<List<string>> OnekliSeriNolarAsync(string onek, CancellationToken ct = default) =>
        db.Uretimler.Where(u => u.SeriNo.StartsWith(onek)).Select(u => u.SeriNo).ToListAsync(ct);
}
