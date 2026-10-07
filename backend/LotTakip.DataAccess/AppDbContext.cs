using LotTakip.Entity;
using Microsoft.EntityFrameworkCore;

namespace LotTakip.DataAccess;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    /// <summary>
    /// Büyük/küçük harf duyarsız, aksan duyarlı. Sunucu Türkçe (Turkish_CI_AS) kurulsa da
    /// "sn-ip-0001" = "SN-IP-0001" olsun diye (Türkçe kuralda I ≠ i). Türkçe karakterler korunur.
    /// </summary>
    public const string Siralama = "Latin1_General_100_CI_AS";

    public DbSet<Parca> Parcalar => Set<Parca>();
    public DbSet<Tedarikci> Tedarikciler => Set<Tedarikci>();
    public DbSet<StokLotu> StokLotlari => Set<StokLotu>();
    public DbSet<Urun> Urunler => Set<Urun>();
    public DbSet<UrunAgaci> UrunAgaclari => Set<UrunAgaci>();
    public DbSet<Uretim> Uretimler => Set<Uretim>();
    public DbSet<UretimTuketim> UretimTuketimleri => Set<UretimTuketim>();
    public DbSet<Musteri> Musteriler => Set<Musteri>();
    public DbSet<Satis> Satislar => Set<Satis>();
    public DbSet<SatisKalemi> SatisKalemleri => Set<SatisKalemi>();
    public DbSet<Kullanici> Kullanicilar => Set<Kullanici>();
    public DbSet<StokDuzeltme> StokDuzeltmeleri => Set<StokDuzeltme>();

    protected override void ConfigureConventions(ModelConfigurationBuilder c)
    {
        // Enum'lar sayı değil ad olarak saklanır (veritabanında okunabilir)
        c.Properties<KullaniciRolu>().HaveConversion<string>().HaveMaxLength(20);
        c.Properties<StokDuzeltmeTuru>().HaveConversion<string>().HaveMaxLength(20);
    }

    protected override void OnModelCreating(ModelBuilder m)
    {
        m.UseCollation(Siralama);

        m.Entity<Parca>(e =>
        {
            e.ToTable("Parca", t => t.HasCheckConstraint("CK_Parca_MinStok", "[MinStok] >= 0"));
            e.Property(x => x.Kod).HasMaxLength(50);
            e.Property(x => x.Ad).HasMaxLength(200);
            e.Property(x => x.Birim).HasMaxLength(20);
            e.HasIndex(x => x.Kod).IsUnique();
        });

        m.Entity<Tedarikci>(e =>
        {
            e.ToTable("Tedarikci");
            e.Property(x => x.Ad).HasMaxLength(200);
            e.Property(x => x.Iletisim).HasMaxLength(500);
            e.HasIndex(x => x.Ad).IsUnique();
        });

        m.Entity<StokLotu>(e =>
        {
            e.ToTable("StokLotu", t =>
            {
                t.HasCheckConstraint("CK_StokLotu_GirisAdet", "[GirisAdet] > 0");
                // Sayım fazlası (+ düzeltme) giriş adedini aşabilir; yalnız negatif olamaz
                t.HasCheckConstraint("CK_StokLotu_KalanAdet", "[KalanAdet] >= 0");
            });
            e.Property(x => x.LotNo).HasMaxLength(100);
            e.Property(x => x.SiparisNo).HasMaxLength(100);
            e.Property(x => x.RowVersion).IsRowVersion();
            e.HasIndex(x => x.LotNo).IsUnique();
            // FIFO sorgusu: bir parçanın kullanılabilir lotları, en eski sipariş önce
            e.HasIndex(x => new { x.ParcaId, x.GeriCagrildi, x.SiparisTarihi, x.Id });
            e.HasOne(x => x.Parca).WithMany(p => p.Lotlar).HasForeignKey(x => x.ParcaId);
            e.HasOne(x => x.Tedarikci).WithMany(t => t.Lotlar).HasForeignKey(x => x.TedarikciId);
            e.HasOne(x => x.OlusturanKullanici).WithMany().HasForeignKey(x => x.OlusturanKullaniciId);
        });

        m.Entity<Urun>(e =>
        {
            e.ToTable("Urun");
            e.Property(x => x.Kod).HasMaxLength(50);
            e.Property(x => x.Ad).HasMaxLength(200);
            e.Property(x => x.SeriOneki).HasMaxLength(20);
            e.HasIndex(x => x.Kod).IsUnique();
            e.HasIndex(x => x.SeriOneki).IsUnique();
        });

        m.Entity<UrunAgaci>(e =>
        {
            e.ToTable("UrunAgaci", t => t.HasCheckConstraint("CK_UrunAgaci_Adet", "[Adet] > 0"));
            e.HasKey(x => new { x.UrunId, x.ParcaId });
            // Ürün silinirse ağaç satırları da gider (üretimi olan ürün zaten silinemez)
            e.HasOne(x => x.Urun).WithMany(u => u.Agac).HasForeignKey(x => x.UrunId)
                .OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.Parca).WithMany(p => p.AgacSatirlari).HasForeignKey(x => x.ParcaId);
        });

        m.Entity<Uretim>(e =>
        {
            e.ToTable("Uretim");
            e.Property(x => x.SeriNo).HasMaxLength(50);
            e.HasIndex(x => x.SeriNo).IsUnique();
            e.HasOne(x => x.Urun).WithMany(u => u.Uretimler).HasForeignKey(x => x.UrunId);
            e.HasOne(x => x.OlusturanKullanici).WithMany().HasForeignKey(x => x.OlusturanKullaniciId);
        });

        m.Entity<UretimTuketim>(e =>
        {
            e.ToTable("UretimTuketim", t => t.HasCheckConstraint("CK_UretimTuketim_Adet", "[Adet] > 0"));
            e.HasKey(x => new { x.UretimId, x.StokLotuId });
            e.HasIndex(x => x.StokLotuId);  // lot → kullanıldığı üretimler (lot izleme)
            e.HasOne(x => x.Uretim).WithMany(u => u.Tuketimler).HasForeignKey(x => x.UretimId);
            e.HasOne(x => x.StokLotu).WithMany(l => l.Tuketimler).HasForeignKey(x => x.StokLotuId);
        });

        m.Entity<Musteri>(e =>
        {
            e.ToTable("Musteri");
            e.Property(x => x.Ad).HasMaxLength(200);
            e.Property(x => x.Iletisim).HasMaxLength(500);
            e.HasIndex(x => x.Ad).IsUnique();
        });

        m.Entity<Satis>(e =>
        {
            e.ToTable("Satis");
            e.HasOne(x => x.Musteri).WithMany(mu => mu.Satislar).HasForeignKey(x => x.MusteriId);
            e.HasOne(x => x.OlusturanKullanici).WithMany().HasForeignKey(x => x.OlusturanKullaniciId);
        });

        m.Entity<SatisKalemi>(e =>
        {
            e.ToTable("SatisKalemi");
            e.HasKey(x => new { x.SatisId, x.UretimId });
            e.HasIndex(x => x.UretimId).IsUnique();  // bir seri no yalnızca bir kez satılır
            e.HasOne(x => x.Satis).WithMany(s => s.Kalemler).HasForeignKey(x => x.SatisId);
            e.HasOne(x => x.Uretim).WithOne(u => u.SatisKalemi).HasForeignKey<SatisKalemi>(x => x.UretimId);
        });

        m.Entity<Kullanici>(e =>
        {
            e.ToTable("Kullanici");
            e.Property(x => x.Ad).HasMaxLength(100);
            e.Property(x => x.Soyad).HasMaxLength(100);
            e.Property(x => x.KullaniciAdi).HasMaxLength(50);
            e.Property(x => x.SifreHash).HasMaxLength(100);
            e.HasIndex(x => x.KullaniciAdi).IsUnique();
        });

        m.Entity<StokDuzeltme>(e =>
        {
            e.ToTable("StokDuzeltme", t =>
            {
                t.HasCheckConstraint("CK_StokDuzeltme_Miktar", "[Miktar] <> 0");
                t.HasCheckConstraint("CK_StokDuzeltme_Aciklama", "LEN([Aciklama]) > 0");
            });
            e.Property(x => x.Aciklama).HasMaxLength(500);
            e.HasOne(x => x.StokLotu).WithMany(l => l.Duzeltmeler).HasForeignKey(x => x.StokLotuId);
            e.HasOne(x => x.OlusturanKullanici).WithMany().HasForeignKey(x => x.OlusturanKullaniciId);
        });

        // Bağlı kayıt silinmez. Tek istisna yukarıdaki UrunAgaci → Urun.
        foreach (var fk in m.Model.GetEntityTypes().SelectMany(t => t.GetForeignKeys()))
        {
            if (!(fk.DeclaringEntityType.ClrType == typeof(UrunAgaci) && fk.PrincipalEntityType.ClrType == typeof(Urun)))
                fk.DeleteBehavior = DeleteBehavior.Restrict;
        }
    }
}
