using Microsoft.EntityFrameworkCore;

namespace DataAccessLayer.Concrete.Contexts;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
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
    public DbSet<SifreSifirlama> SifreSifirlamalari => Set<SifreSifirlama>();
    public DbSet<StokDuzeltme> StokDuzeltmeleri => Set<StokDuzeltme>();
    public DbSet<UretimHurda> UretimHurdalari => Set<UretimHurda>();
    public DbSet<SatisIade> SatisIadeleri => Set<SatisIade>();
    public DbSet<UrunGeriCagirma> UrunGeriCagirmalari => Set<UrunGeriCagirma>();
    public DbSet<Siparis> Siparisler => Set<Siparis>();
    public DbSet<SiparisKalemi> SiparisKalemleri => Set<SiparisKalemi>();
    public DbSet<UretimEmri> UretimEmirleri => Set<UretimEmri>();
    public DbSet<UretimEmriParcasi> UretimEmriParcalari => Set<UretimEmriParcasi>();
    public DbSet<TakvimPlani> TakvimPlanlari => Set<TakvimPlani>();

    private const string IletisimSql = "CONCAT_WS(N' · ', NULLIF([Telefon], N''), NULLIF([Eposta], N''), NULLIF([Adres], N''))";

    protected override void ConfigureConventions(ModelConfigurationBuilder c)
    {
        c.Properties<KullaniciRolu>().HaveConversion<string>().HaveMaxLength(20);
        c.Properties<StokDuzeltmeTuru>().HaveConversion<string>().HaveMaxLength(20);
        c.Properties<UretimHurdaTuru>().HaveConversion<string>().HaveMaxLength(20);
        c.Properties<UretimEmriDurumu>().HaveConversion<string>().HaveMaxLength(20);
        c.Properties<SevkiyatDurumu>().HaveConversion<string>().HaveMaxLength(20);
    }

    protected override void OnModelCreating(ModelBuilder m)
    {
        m.UseCollation(Siralama);

        m.Entity<Parca>(e =>
        {
            e.ToTable("Parca", t => t.HasCheckConstraint("CK_Parca_MinStok", "[MinStok] >= 0"));
            e.Property(x => x.Kod).HasMaxLength(50);
            e.Property(x => x.Ad).HasMaxLength(200);
            e.Property(x => x.Aciklama).HasMaxLength(500);
            e.HasIndex(x => x.Kod).IsUnique();
        });

        m.Entity<Tedarikci>(e =>
        {
            e.ToTable("Tedarikci");
            e.Property(x => x.Ad).HasMaxLength(200);
            e.Property(x => x.Telefon).HasMaxLength(50);
            e.Property(x => x.Eposta).HasMaxLength(200);
            e.Property(x => x.Adres).HasMaxLength(500);
            e.Property(x => x.Iletisim).HasComputedColumnSql(IletisimSql);
            e.HasIndex(x => x.Ad).IsUnique();
        });

        m.Entity<StokLotu>(e =>
        {
            e.ToTable("StokLotu", t =>
            {
                t.HasCheckConstraint("CK_StokLotu_GirisAdet", "[GirisAdet] > 0");
                t.HasCheckConstraint("CK_StokLotu_KalanAdet", "[KalanAdet] >= 0");
            });
            e.Property(x => x.SorunAciklama).HasMaxLength(500);
            e.HasOne(x => x.SorunIsaretleyen).WithMany().HasForeignKey(x => x.SorunIsaretleyenId);
            e.Property(x => x.LotNo).HasMaxLength(100);
            e.Property(x => x.TedarikciLotNo).HasMaxLength(100);
            e.HasIndex(x => x.TedarikciLotNo);
            e.Property(x => x.SiparisNo).HasMaxLength(100);
            e.Property(x => x.RowVersion).IsRowVersion();
            e.HasIndex(x => x.LotNo).IsUnique();
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
            e.Property(x => x.Aciklama).HasMaxLength(500);
            e.HasIndex(x => x.Kod).IsUnique();
            e.HasIndex(x => x.SeriOneki).IsUnique();
        });

        m.Entity<UrunAgaci>(e =>
        {
            e.ToTable("UrunAgaci", t => t.HasCheckConstraint("CK_UrunAgaci_Adet", "[Adet] > 0"));
            e.HasKey(x => new { x.UrunId, x.ParcaId });
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
            e.HasOne(x => x.UretimEmri).WithMany(u => u.Uretimler).HasForeignKey(x => x.UretimEmriId);
            e.HasOne(x => x.SiparisKalemi).WithMany(k => k.Ayrilanlar).HasForeignKey(x => x.SiparisKalemiId);
        });

        m.Entity<Siparis>(e =>
        {
            e.ToTable("Siparis");
            e.Property(x => x.No).HasMaxLength(20);
            e.HasIndex(x => x.No).IsUnique();
            e.Property(x => x.Aciklama).HasMaxLength(500);
            e.Property(x => x.SiparisNo).HasMaxLength(50);
            e.HasIndex(x => x.SiparisNo);
            e.HasOne(x => x.Musteri).WithMany(mu => mu.Siparisler).HasForeignKey(x => x.MusteriId);
            e.HasOne(x => x.OlusturanKullanici).WithMany().HasForeignKey(x => x.OlusturanKullaniciId);
        });

        m.Entity<SiparisKalemi>(e =>
        {
            e.ToTable("SiparisKalemi", t => t.HasCheckConstraint("CK_SiparisKalemi_Adet", "[Adet] > 0"));
            e.HasOne(x => x.Siparis).WithMany(s => s.Kalemler).HasForeignKey(x => x.SiparisId);
            e.HasOne(x => x.Urun).WithMany().HasForeignKey(x => x.UrunId);
        });

        m.Entity<UretimEmri>(e =>
        {
            e.ToTable("UretimEmri", t =>
            {
                t.HasCheckConstraint("CK_UretimEmri_Adet", "[Adet] > 0");
                t.HasCheckConstraint("CK_UretimEmri_Uretilen", "[Uretilen] >= 0 AND [Uretilen] <= [Adet]");
            });
            e.Property(x => x.Aciklama).HasMaxLength(500);
            e.HasIndex(x => new { x.Durum, x.Sira });
            e.HasOne(x => x.Urun).WithMany().HasForeignKey(x => x.UrunId);
            e.HasOne(x => x.SiparisKalemi).WithMany(k => k.UretimEmirleri).HasForeignKey(x => x.SiparisKalemiId);
            e.HasOne(x => x.OlusturanKullanici).WithMany().HasForeignKey(x => x.OlusturanKullaniciId);
        });

        m.Entity<TakvimPlani>(e =>
        {
            e.ToTable("TakvimPlani", t => t.HasCheckConstraint("CK_TakvimPlani_Adet", "[Adet] IS NULL OR [Adet] > 0"));
            e.Property(x => x.Aciklama).HasMaxLength(500);
            e.HasIndex(x => x.Tarih);
            e.HasOne(x => x.Parca).WithMany().HasForeignKey(x => x.ParcaId);
            e.HasOne(x => x.Tedarikci).WithMany().HasForeignKey(x => x.TedarikciId);
            e.HasOne(x => x.OlusturanKullanici).WithMany().HasForeignKey(x => x.OlusturanKullaniciId);
        });

        m.Entity<UretimEmriParcasi>(e =>
        {
            e.ToTable("UretimEmriParcasi", t => t.HasCheckConstraint("CK_UretimEmriParcasi_BirimAdet", "[BirimAdet] > 0"));
            e.HasKey(x => new { x.UretimEmriId, x.ParcaId });
            e.HasOne(x => x.UretimEmri).WithMany(u => u.Parcalar).HasForeignKey(x => x.UretimEmriId);
            e.HasOne(x => x.Parca).WithMany().HasForeignKey(x => x.ParcaId);
        });

        m.Entity<UretimTuketim>(e =>
        {
            e.ToTable("UretimTuketim", t => t.HasCheckConstraint("CK_UretimTuketim_Adet", "[Adet] > 0"));
            e.HasKey(x => new { x.UretimId, x.StokLotuId });
            e.HasIndex(x => x.StokLotuId);
            e.HasOne(x => x.Uretim).WithMany(u => u.Tuketimler).HasForeignKey(x => x.UretimId);
            e.HasOne(x => x.StokLotu).WithMany(l => l.Tuketimler).HasForeignKey(x => x.StokLotuId);
        });

        m.Entity<Musteri>(e =>
        {
            e.ToTable("Musteri");
            e.Property(x => x.Ad).HasMaxLength(200);
            e.Property(x => x.Telefon).HasMaxLength(50);
            e.Property(x => x.Eposta).HasMaxLength(200);
            e.Property(x => x.Adres).HasMaxLength(500);
            e.Property(x => x.Iletisim).HasComputedColumnSql(IletisimSql);
            e.HasIndex(x => x.Ad).IsUnique();
        });

        m.Entity<Satis>(e =>
        {
            e.ToTable("Satis");
            e.Property(x => x.No).HasMaxLength(20);
            e.HasIndex(x => x.No).IsUnique();
            e.Property(x => x.KargoFirmasi).HasMaxLength(100);
            e.Property(x => x.TakipNo).HasMaxLength(100);
            e.Property(x => x.Aciklama).HasMaxLength(500);
            e.HasIndex(x => x.Durum);
            e.HasOne(x => x.Musteri).WithMany(mu => mu.Satislar).HasForeignKey(x => x.MusteriId);
            e.HasOne(x => x.Siparis).WithMany(s => s.Sevkiyatlar).HasForeignKey(x => x.SiparisId);
            e.HasOne(x => x.OlusturanKullanici).WithMany().HasForeignKey(x => x.OlusturanKullaniciId);
        });

        m.Entity<SatisKalemi>(e =>
        {
            e.ToTable("SatisKalemi");
            e.HasKey(x => new { x.SatisId, x.UretimId });
            e.HasIndex(x => x.UretimId).IsUnique();
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
            e.Property(x => x.Eposta).HasMaxLength(200);
            e.HasIndex(x => x.Eposta).IsUnique().HasFilter("[Eposta] IS NOT NULL");
        });

        m.Entity<SifreSifirlama>(e =>
        {
            e.ToTable("SifreSifirlama");
            e.Property(x => x.KodOzeti).HasMaxLength(64);
            e.HasIndex(x => x.KodOzeti).IsUnique();
            e.HasOne(x => x.Kullanici).WithMany().HasForeignKey(x => x.KullaniciId);
        });

        m.Entity<StokDuzeltme>(e =>
        {
            e.ToTable("StokDuzeltme", t =>
            {
                t.HasCheckConstraint("CK_StokDuzeltme_Miktar", "[Miktar] <> 0 OR [HataliAdet] > 0");
                t.HasCheckConstraint("CK_StokDuzeltme_HataliAdet", "[HataliAdet] >= 0");
                t.HasCheckConstraint("CK_StokDuzeltme_Aciklama", "LEN([Aciklama]) > 0");
            });
            e.Property(x => x.Aciklama).HasMaxLength(500);
            e.HasOne(x => x.StokLotu).WithMany(l => l.Duzeltmeler).HasForeignKey(x => x.StokLotuId);
            e.HasOne(x => x.OlusturanKullanici).WithMany().HasForeignKey(x => x.OlusturanKullaniciId);
        });

        m.Entity<UretimHurda>(e =>
        {
            e.ToTable("UretimHurda", t => t.HasCheckConstraint("CK_UretimHurda_Aciklama", "LEN([Aciklama]) > 0"));
            e.Property(x => x.Aciklama).HasMaxLength(500);
            e.HasIndex(x => x.UretimId).IsUnique();
            e.HasOne(x => x.Uretim).WithOne(u => u.Hurda).HasForeignKey<UretimHurda>(x => x.UretimId);
            e.HasOne(x => x.OlusturanKullanici).WithMany().HasForeignKey(x => x.OlusturanKullaniciId);
        });

        m.Entity<SatisIade>(e =>
        {
            e.ToTable("SatisIade", t => t.HasCheckConstraint("CK_SatisIade_Aciklama", "LEN([Aciklama]) > 0"));
            e.Property(x => x.Aciklama).HasMaxLength(500);
            e.HasOne(x => x.Satis).WithMany(s => s.Iadeler).HasForeignKey(x => x.SatisId);
            e.HasOne(x => x.Uretim).WithMany(u => u.Iadeler).HasForeignKey(x => x.UretimId);
            e.HasOne(x => x.OlusturanKullanici).WithMany().HasForeignKey(x => x.OlusturanKullaniciId);
        });

        m.Entity<UrunGeriCagirma>(e =>
        {
            e.ToTable("UrunGeriCagirma", t => t.HasCheckConstraint("CK_UrunGeriCagirma_Aciklama", "LEN([Aciklama]) > 0"));
            e.Property(x => x.Aciklama).HasMaxLength(500);
            e.Property(x => x.GeriAlmaKargoFirmasi).HasMaxLength(100);
            e.Property(x => x.GeriAlmaTakipNo).HasMaxLength(100);
            e.Property(x => x.GeriGondermeKargoFirmasi).HasMaxLength(100);
            e.Property(x => x.GeriGondermeTakipNo).HasMaxLength(100);
            e.HasIndex(x => x.UretimId).IsUnique().HasFilter("[KapanmaZamani] IS NULL");
            e.HasOne(x => x.Uretim).WithMany(u => u.GeriCagirmalar).HasForeignKey(x => x.UretimId);
            e.HasOne(x => x.StokLotu).WithMany().HasForeignKey(x => x.StokLotuId);
            e.HasOne(x => x.OlusturanKullanici).WithMany().HasForeignKey(x => x.OlusturanKullaniciId);
            e.HasOne(x => x.KapatanKullanici).WithMany().HasForeignKey(x => x.KapatanKullaniciId);
        });

        foreach (var fk in m.Model.GetEntityTypes().SelectMany(t => t.GetForeignKeys()))
        {
            if (!(fk.DeclaringEntityType.ClrType == typeof(UrunAgaci) && fk.PrincipalEntityType.ClrType == typeof(Urun)))
                fk.DeleteBehavior = DeleteBehavior.Restrict;
        }
    }
}
