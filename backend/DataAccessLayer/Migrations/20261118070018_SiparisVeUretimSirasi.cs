using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DataAccessLayer.Migrations
{
    /// <inheritdoc />
    public partial class SiparisVeUretimSirasi : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "SiparisKalemiId",
                table: "Uretim",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "UretimEmriId",
                table: "Uretim",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Siparis",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    No = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    MusteriId = table.Column<int>(type: "int", nullable: false),
                    Tarih = table.Column<DateOnly>(type: "date", nullable: false),
                    IstenenTarih = table.Column<DateOnly>(type: "date", nullable: true),
                    Aciklama = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    IptalZamani = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    OlusturanKullaniciId = table.Column<int>(type: "int", nullable: false),
                    OlusturmaZamani = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Siparis", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Siparis_Kullanici_OlusturanKullaniciId",
                        column: x => x.OlusturanKullaniciId,
                        principalTable: "Kullanici",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Siparis_Musteri_MusteriId",
                        column: x => x.MusteriId,
                        principalTable: "Musteri",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "SiparisKalemi",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SiparisId = table.Column<int>(type: "int", nullable: false),
                    UrunId = table.Column<int>(type: "int", nullable: false),
                    Adet = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SiparisKalemi", x => x.Id);
                    table.CheckConstraint("CK_SiparisKalemi_Adet", "[Adet] > 0");
                    table.ForeignKey(
                        name: "FK_SiparisKalemi_Siparis_SiparisId",
                        column: x => x.SiparisId,
                        principalTable: "Siparis",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SiparisKalemi_Urun_UrunId",
                        column: x => x.UrunId,
                        principalTable: "Urun",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "UretimEmri",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UrunId = table.Column<int>(type: "int", nullable: false),
                    Adet = table.Column<int>(type: "int", nullable: false),
                    Uretilen = table.Column<int>(type: "int", nullable: false),
                    Sira = table.Column<int>(type: "int", nullable: false),
                    PlanlananTarih = table.Column<DateOnly>(type: "date", nullable: false),
                    Durum = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Aciklama = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    SiparisKalemiId = table.Column<int>(type: "int", nullable: true),
                    BitisZamani = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    OlusturanKullaniciId = table.Column<int>(type: "int", nullable: false),
                    OlusturmaZamani = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UretimEmri", x => x.Id);
                    table.CheckConstraint("CK_UretimEmri_Adet", "[Adet] > 0");
                    table.CheckConstraint("CK_UretimEmri_Uretilen", "[Uretilen] >= 0 AND [Uretilen] <= [Adet]");
                    table.ForeignKey(
                        name: "FK_UretimEmri_Kullanici_OlusturanKullaniciId",
                        column: x => x.OlusturanKullaniciId,
                        principalTable: "Kullanici",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_UretimEmri_SiparisKalemi_SiparisKalemiId",
                        column: x => x.SiparisKalemiId,
                        principalTable: "SiparisKalemi",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_UretimEmri_Urun_UrunId",
                        column: x => x.UrunId,
                        principalTable: "Urun",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "UretimEmriParcasi",
                columns: table => new
                {
                    UretimEmriId = table.Column<int>(type: "int", nullable: false),
                    ParcaId = table.Column<int>(type: "int", nullable: false),
                    BirimAdet = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UretimEmriParcasi", x => new { x.UretimEmriId, x.ParcaId });
                    table.CheckConstraint("CK_UretimEmriParcasi_BirimAdet", "[BirimAdet] > 0");
                    table.ForeignKey(
                        name: "FK_UretimEmriParcasi_Parca_ParcaId",
                        column: x => x.ParcaId,
                        principalTable: "Parca",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_UretimEmriParcasi_UretimEmri_UretimEmriId",
                        column: x => x.UretimEmriId,
                        principalTable: "UretimEmri",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Uretim_SiparisKalemiId",
                table: "Uretim",
                column: "SiparisKalemiId");

            migrationBuilder.CreateIndex(
                name: "IX_Uretim_UretimEmriId",
                table: "Uretim",
                column: "UretimEmriId");

            migrationBuilder.CreateIndex(
                name: "IX_Siparis_MusteriId",
                table: "Siparis",
                column: "MusteriId");

            migrationBuilder.CreateIndex(
                name: "IX_Siparis_No",
                table: "Siparis",
                column: "No",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Siparis_OlusturanKullaniciId",
                table: "Siparis",
                column: "OlusturanKullaniciId");

            migrationBuilder.CreateIndex(
                name: "IX_SiparisKalemi_SiparisId",
                table: "SiparisKalemi",
                column: "SiparisId");

            migrationBuilder.CreateIndex(
                name: "IX_SiparisKalemi_UrunId",
                table: "SiparisKalemi",
                column: "UrunId");

            migrationBuilder.CreateIndex(
                name: "IX_UretimEmri_Durum_Sira",
                table: "UretimEmri",
                columns: new[] { "Durum", "Sira" });

            migrationBuilder.CreateIndex(
                name: "IX_UretimEmri_OlusturanKullaniciId",
                table: "UretimEmri",
                column: "OlusturanKullaniciId");

            migrationBuilder.CreateIndex(
                name: "IX_UretimEmri_SiparisKalemiId",
                table: "UretimEmri",
                column: "SiparisKalemiId");

            migrationBuilder.CreateIndex(
                name: "IX_UretimEmri_UrunId",
                table: "UretimEmri",
                column: "UrunId");

            migrationBuilder.CreateIndex(
                name: "IX_UretimEmriParcasi_ParcaId",
                table: "UretimEmriParcasi",
                column: "ParcaId");

            migrationBuilder.AddForeignKey(
                name: "FK_Uretim_SiparisKalemi_SiparisKalemiId",
                table: "Uretim",
                column: "SiparisKalemiId",
                principalTable: "SiparisKalemi",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Uretim_UretimEmri_UretimEmriId",
                table: "Uretim",
                column: "UretimEmriId",
                principalTable: "UretimEmri",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Uretim_SiparisKalemi_SiparisKalemiId",
                table: "Uretim");

            migrationBuilder.DropForeignKey(
                name: "FK_Uretim_UretimEmri_UretimEmriId",
                table: "Uretim");

            migrationBuilder.DropTable(
                name: "UretimEmriParcasi");

            migrationBuilder.DropTable(
                name: "UretimEmri");

            migrationBuilder.DropTable(
                name: "SiparisKalemi");

            migrationBuilder.DropTable(
                name: "Siparis");

            migrationBuilder.DropIndex(
                name: "IX_Uretim_SiparisKalemiId",
                table: "Uretim");

            migrationBuilder.DropIndex(
                name: "IX_Uretim_UretimEmriId",
                table: "Uretim");

            migrationBuilder.DropColumn(
                name: "SiparisKalemiId",
                table: "Uretim");

            migrationBuilder.DropColumn(
                name: "UretimEmriId",
                table: "Uretim");
        }
    }
}
