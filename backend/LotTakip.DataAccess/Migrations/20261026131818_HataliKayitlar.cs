using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LotTakip.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class HataliKayitlar : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_StokDuzeltme_Miktar",
                table: "StokDuzeltme");

            migrationBuilder.AddColumn<bool>(
                name: "MusteriyeVerildi",
                table: "UrunGeriCagirma",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "HataliAdet",
                table: "StokDuzeltme",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "UrunLotSorunu",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UrunId = table.Column<int>(type: "int", nullable: false),
                    LotNo = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Aciklama = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    HataliAdet = table.Column<int>(type: "int", nullable: false),
                    OlusturanKullaniciId = table.Column<int>(type: "int", nullable: false),
                    OlusturmaZamani = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UrunLotSorunu", x => x.Id);
                    table.CheckConstraint("CK_UrunLotSorunu_Aciklama", "LEN([Aciklama]) > 0");
                    table.CheckConstraint("CK_UrunLotSorunu_HataliAdet", "[HataliAdet] >= 0");
                    table.ForeignKey(
                        name: "FK_UrunLotSorunu_Kullanici_OlusturanKullaniciId",
                        column: x => x.OlusturanKullaniciId,
                        principalTable: "Kullanici",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_UrunLotSorunu_Urun_UrunId",
                        column: x => x.UrunId,
                        principalTable: "Urun",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.AddCheckConstraint(
                name: "CK_StokDuzeltme_HataliAdet",
                table: "StokDuzeltme",
                sql: "[HataliAdet] >= 0");

            migrationBuilder.AddCheckConstraint(
                name: "CK_StokDuzeltme_Miktar",
                table: "StokDuzeltme",
                sql: "[Miktar] <> 0 OR [HataliAdet] > 0");

            migrationBuilder.CreateIndex(
                name: "IX_UrunLotSorunu_LotNo",
                table: "UrunLotSorunu",
                column: "LotNo",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UrunLotSorunu_OlusturanKullaniciId",
                table: "UrunLotSorunu",
                column: "OlusturanKullaniciId");

            migrationBuilder.CreateIndex(
                name: "IX_UrunLotSorunu_UrunId",
                table: "UrunLotSorunu",
                column: "UrunId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "UrunLotSorunu");

            migrationBuilder.DropCheckConstraint(
                name: "CK_StokDuzeltme_HataliAdet",
                table: "StokDuzeltme");

            migrationBuilder.DropCheckConstraint(
                name: "CK_StokDuzeltme_Miktar",
                table: "StokDuzeltme");

            migrationBuilder.DropColumn(
                name: "MusteriyeVerildi",
                table: "UrunGeriCagirma");

            migrationBuilder.DropColumn(
                name: "HataliAdet",
                table: "StokDuzeltme");

            migrationBuilder.AddCheckConstraint(
                name: "CK_StokDuzeltme_Miktar",
                table: "StokDuzeltme",
                sql: "[Miktar] <> 0");
        }
    }
}
