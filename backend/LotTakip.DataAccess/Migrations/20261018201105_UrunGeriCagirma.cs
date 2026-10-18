using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LotTakip.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class UrunGeriCagirma : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "SorunAciklama",
                table: "StokLotu",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "SorunIsaretleyenId",
                table: "StokLotu",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "SorunZamani",
                table: "StokLotu",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "Sorunlu",
                table: "StokLotu",
                type: "bit",
                nullable: false,
                defaultValue: false);

            // Eskiden "geri çağrılmış" lotlar artık sorunlu ve üretime kapalı
            migrationBuilder.Sql(
                "UPDATE StokLotu SET Sorunlu = 1, SorunAciklama = N'Önceki geri çağırmadan aktarıldı', SorunZamani = SYSDATETIMEOFFSET() WHERE GeriCagrildi = 1");

            migrationBuilder.CreateTable(
                name: "UrunGeriCagirma",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UretimId = table.Column<int>(type: "int", nullable: false),
                    StokLotuId = table.Column<int>(type: "int", nullable: true),
                    Aciklama = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    OlusturanKullaniciId = table.Column<int>(type: "int", nullable: false),
                    OlusturmaZamani = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    GeriAlinmaZamani = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    KapanmaZamani = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    KapatanKullaniciId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UrunGeriCagirma", x => x.Id);
                    table.CheckConstraint("CK_UrunGeriCagirma_Aciklama", "LEN([Aciklama]) > 0");
                    table.ForeignKey(
                        name: "FK_UrunGeriCagirma_Kullanici_KapatanKullaniciId",
                        column: x => x.KapatanKullaniciId,
                        principalTable: "Kullanici",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_UrunGeriCagirma_Kullanici_OlusturanKullaniciId",
                        column: x => x.OlusturanKullaniciId,
                        principalTable: "Kullanici",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_UrunGeriCagirma_StokLotu_StokLotuId",
                        column: x => x.StokLotuId,
                        principalTable: "StokLotu",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_UrunGeriCagirma_Uretim_UretimId",
                        column: x => x.UretimId,
                        principalTable: "Uretim",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_StokLotu_SorunIsaretleyenId",
                table: "StokLotu",
                column: "SorunIsaretleyenId");

            migrationBuilder.CreateIndex(
                name: "IX_UrunGeriCagirma_KapatanKullaniciId",
                table: "UrunGeriCagirma",
                column: "KapatanKullaniciId");

            migrationBuilder.CreateIndex(
                name: "IX_UrunGeriCagirma_OlusturanKullaniciId",
                table: "UrunGeriCagirma",
                column: "OlusturanKullaniciId");

            migrationBuilder.CreateIndex(
                name: "IX_UrunGeriCagirma_StokLotuId",
                table: "UrunGeriCagirma",
                column: "StokLotuId");

            migrationBuilder.CreateIndex(
                name: "IX_UrunGeriCagirma_UretimId",
                table: "UrunGeriCagirma",
                column: "UretimId",
                unique: true,
                filter: "[KapanmaZamani] IS NULL");

            migrationBuilder.AddForeignKey(
                name: "FK_StokLotu_Kullanici_SorunIsaretleyenId",
                table: "StokLotu",
                column: "SorunIsaretleyenId",
                principalTable: "Kullanici",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_StokLotu_Kullanici_SorunIsaretleyenId",
                table: "StokLotu");

            migrationBuilder.DropTable(
                name: "UrunGeriCagirma");

            migrationBuilder.DropIndex(
                name: "IX_StokLotu_SorunIsaretleyenId",
                table: "StokLotu");

            migrationBuilder.DropColumn(
                name: "SorunAciklama",
                table: "StokLotu");

            migrationBuilder.DropColumn(
                name: "SorunIsaretleyenId",
                table: "StokLotu");

            migrationBuilder.DropColumn(
                name: "SorunZamani",
                table: "StokLotu");

            migrationBuilder.DropColumn(
                name: "Sorunlu",
                table: "StokLotu");
        }
    }
}
