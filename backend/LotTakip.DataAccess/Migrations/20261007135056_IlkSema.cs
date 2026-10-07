using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LotTakip.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class IlkSema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Kullanici",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Ad = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Soyad = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    KullaniciAdi = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    SifreHash = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Rol = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Kullanici", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Musteri",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Ad = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Iletisim = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Musteri", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Parca",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Kod = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Ad = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Birim = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    MinStok = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Parca", x => x.Id);
                    table.CheckConstraint("CK_Parca_MinStok", "[MinStok] >= 0");
                });

            migrationBuilder.CreateTable(
                name: "Tedarikci",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Ad = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Iletisim = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Tedarikci", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Urun",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Kod = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Ad = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    SeriOneki = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Urun", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Satis",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MusteriId = table.Column<int>(type: "int", nullable: false),
                    Tarih = table.Column<DateOnly>(type: "date", nullable: false),
                    OlusturanKullaniciId = table.Column<int>(type: "int", nullable: false),
                    OlusturmaZamani = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Satis", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Satis_Kullanici_OlusturanKullaniciId",
                        column: x => x.OlusturanKullaniciId,
                        principalTable: "Kullanici",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Satis_Musteri_MusteriId",
                        column: x => x.MusteriId,
                        principalTable: "Musteri",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "StokLotu",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ParcaId = table.Column<int>(type: "int", nullable: false),
                    TedarikciId = table.Column<int>(type: "int", nullable: false),
                    LotNo = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    SiparisTarihi = table.Column<DateOnly>(type: "date", nullable: false),
                    SiparisNo = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    GirisAdet = table.Column<int>(type: "int", nullable: false),
                    KalanAdet = table.Column<int>(type: "int", nullable: false),
                    GeriCagrildi = table.Column<bool>(type: "bit", nullable: false),
                    GirisZamani = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false),
                    OlusturanKullaniciId = table.Column<int>(type: "int", nullable: false),
                    OlusturmaZamani = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StokLotu", x => x.Id);
                    table.CheckConstraint("CK_StokLotu_GirisAdet", "[GirisAdet] > 0");
                    table.CheckConstraint("CK_StokLotu_KalanAdet", "[KalanAdet] >= 0");
                    table.ForeignKey(
                        name: "FK_StokLotu_Kullanici_OlusturanKullaniciId",
                        column: x => x.OlusturanKullaniciId,
                        principalTable: "Kullanici",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_StokLotu_Parca_ParcaId",
                        column: x => x.ParcaId,
                        principalTable: "Parca",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_StokLotu_Tedarikci_TedarikciId",
                        column: x => x.TedarikciId,
                        principalTable: "Tedarikci",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Uretim",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UrunId = table.Column<int>(type: "int", nullable: false),
                    SeriNo = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Tarih = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    OlusturanKullaniciId = table.Column<int>(type: "int", nullable: false),
                    OlusturmaZamani = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Uretim", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Uretim_Kullanici_OlusturanKullaniciId",
                        column: x => x.OlusturanKullaniciId,
                        principalTable: "Kullanici",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Uretim_Urun_UrunId",
                        column: x => x.UrunId,
                        principalTable: "Urun",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "UrunAgaci",
                columns: table => new
                {
                    UrunId = table.Column<int>(type: "int", nullable: false),
                    ParcaId = table.Column<int>(type: "int", nullable: false),
                    Adet = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UrunAgaci", x => new { x.UrunId, x.ParcaId });
                    table.CheckConstraint("CK_UrunAgaci_Adet", "[Adet] > 0");
                    table.ForeignKey(
                        name: "FK_UrunAgaci_Parca_ParcaId",
                        column: x => x.ParcaId,
                        principalTable: "Parca",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_UrunAgaci_Urun_UrunId",
                        column: x => x.UrunId,
                        principalTable: "Urun",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "StokDuzeltme",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    StokLotuId = table.Column<int>(type: "int", nullable: false),
                    Miktar = table.Column<int>(type: "int", nullable: false),
                    Tur = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Aciklama = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    OlusturanKullaniciId = table.Column<int>(type: "int", nullable: false),
                    OlusturmaZamani = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StokDuzeltme", x => x.Id);
                    table.CheckConstraint("CK_StokDuzeltme_Aciklama", "LEN([Aciklama]) > 0");
                    table.CheckConstraint("CK_StokDuzeltme_Miktar", "[Miktar] <> 0");
                    table.ForeignKey(
                        name: "FK_StokDuzeltme_Kullanici_OlusturanKullaniciId",
                        column: x => x.OlusturanKullaniciId,
                        principalTable: "Kullanici",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_StokDuzeltme_StokLotu_StokLotuId",
                        column: x => x.StokLotuId,
                        principalTable: "StokLotu",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "SatisKalemi",
                columns: table => new
                {
                    SatisId = table.Column<int>(type: "int", nullable: false),
                    UretimId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SatisKalemi", x => new { x.SatisId, x.UretimId });
                    table.ForeignKey(
                        name: "FK_SatisKalemi_Satis_SatisId",
                        column: x => x.SatisId,
                        principalTable: "Satis",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SatisKalemi_Uretim_UretimId",
                        column: x => x.UretimId,
                        principalTable: "Uretim",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "UretimTuketim",
                columns: table => new
                {
                    UretimId = table.Column<int>(type: "int", nullable: false),
                    StokLotuId = table.Column<int>(type: "int", nullable: false),
                    Adet = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UretimTuketim", x => new { x.UretimId, x.StokLotuId });
                    table.CheckConstraint("CK_UretimTuketim_Adet", "[Adet] > 0");
                    table.ForeignKey(
                        name: "FK_UretimTuketim_StokLotu_StokLotuId",
                        column: x => x.StokLotuId,
                        principalTable: "StokLotu",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_UretimTuketim_Uretim_UretimId",
                        column: x => x.UretimId,
                        principalTable: "Uretim",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Kullanici_KullaniciAdi",
                table: "Kullanici",
                column: "KullaniciAdi",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Musteri_Ad",
                table: "Musteri",
                column: "Ad",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Parca_Kod",
                table: "Parca",
                column: "Kod",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Satis_MusteriId",
                table: "Satis",
                column: "MusteriId");

            migrationBuilder.CreateIndex(
                name: "IX_Satis_OlusturanKullaniciId",
                table: "Satis",
                column: "OlusturanKullaniciId");

            migrationBuilder.CreateIndex(
                name: "IX_SatisKalemi_UretimId",
                table: "SatisKalemi",
                column: "UretimId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_StokDuzeltme_OlusturanKullaniciId",
                table: "StokDuzeltme",
                column: "OlusturanKullaniciId");

            migrationBuilder.CreateIndex(
                name: "IX_StokDuzeltme_StokLotuId",
                table: "StokDuzeltme",
                column: "StokLotuId");

            migrationBuilder.CreateIndex(
                name: "IX_StokLotu_LotNo",
                table: "StokLotu",
                column: "LotNo",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_StokLotu_OlusturanKullaniciId",
                table: "StokLotu",
                column: "OlusturanKullaniciId");

            migrationBuilder.CreateIndex(
                name: "IX_StokLotu_ParcaId_GeriCagrildi_SiparisTarihi_Id",
                table: "StokLotu",
                columns: new[] { "ParcaId", "GeriCagrildi", "SiparisTarihi", "Id" });

            migrationBuilder.CreateIndex(
                name: "IX_StokLotu_TedarikciId",
                table: "StokLotu",
                column: "TedarikciId");

            migrationBuilder.CreateIndex(
                name: "IX_Tedarikci_Ad",
                table: "Tedarikci",
                column: "Ad",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Uretim_OlusturanKullaniciId",
                table: "Uretim",
                column: "OlusturanKullaniciId");

            migrationBuilder.CreateIndex(
                name: "IX_Uretim_SeriNo",
                table: "Uretim",
                column: "SeriNo",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Uretim_UrunId",
                table: "Uretim",
                column: "UrunId");

            migrationBuilder.CreateIndex(
                name: "IX_UretimTuketim_StokLotuId",
                table: "UretimTuketim",
                column: "StokLotuId");

            migrationBuilder.CreateIndex(
                name: "IX_Urun_Kod",
                table: "Urun",
                column: "Kod",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Urun_SeriOneki",
                table: "Urun",
                column: "SeriOneki",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UrunAgaci_ParcaId",
                table: "UrunAgaci",
                column: "ParcaId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SatisKalemi");

            migrationBuilder.DropTable(
                name: "StokDuzeltme");

            migrationBuilder.DropTable(
                name: "UretimTuketim");

            migrationBuilder.DropTable(
                name: "UrunAgaci");

            migrationBuilder.DropTable(
                name: "Satis");

            migrationBuilder.DropTable(
                name: "StokLotu");

            migrationBuilder.DropTable(
                name: "Uretim");

            migrationBuilder.DropTable(
                name: "Musteri");

            migrationBuilder.DropTable(
                name: "Parca");

            migrationBuilder.DropTable(
                name: "Tedarikci");

            migrationBuilder.DropTable(
                name: "Kullanici");

            migrationBuilder.DropTable(
                name: "Urun");
        }
    }
}
