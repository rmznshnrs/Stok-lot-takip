using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LotTakip.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class HesapKayitSifre : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Eposta",
                table: "Kullanici",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "Onayli",
                table: "Kullanici",
                type: "bit",
                nullable: false,
                defaultValue: true);  // mevcut kullanıcılar onaylı sayılır

            migrationBuilder.CreateTable(
                name: "SifreSifirlama",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    KullaniciId = table.Column<int>(type: "int", nullable: false),
                    KodOzeti = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    OlusturmaZamani = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    SonGecerlilik = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    KullanilmaZamani = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SifreSifirlama", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SifreSifirlama_Kullanici_KullaniciId",
                        column: x => x.KullaniciId,
                        principalTable: "Kullanici",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Kullanici_Eposta",
                table: "Kullanici",
                column: "Eposta",
                unique: true,
                filter: "[Eposta] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_SifreSifirlama_KodOzeti",
                table: "SifreSifirlama",
                column: "KodOzeti",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SifreSifirlama_KullaniciId",
                table: "SifreSifirlama",
                column: "KullaniciId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SifreSifirlama");

            migrationBuilder.DropIndex(
                name: "IX_Kullanici_Eposta",
                table: "Kullanici");

            migrationBuilder.DropColumn(
                name: "Eposta",
                table: "Kullanici");

            migrationBuilder.DropColumn(
                name: "Onayli",
                table: "Kullanici");
        }
    }
}
