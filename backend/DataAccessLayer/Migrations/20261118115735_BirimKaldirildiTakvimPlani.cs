using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DataAccessLayer.Migrations
{
    /// <inheritdoc />
    public partial class BirimKaldirildiTakvimPlani : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Birim",
                table: "Parca");

            migrationBuilder.CreateTable(
                name: "TakvimPlani",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Tarih = table.Column<DateOnly>(type: "date", nullable: false),
                    ParcaId = table.Column<int>(type: "int", nullable: true),
                    Adet = table.Column<int>(type: "int", nullable: true),
                    TedarikciId = table.Column<int>(type: "int", nullable: true),
                    Aciklama = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Tamamlandi = table.Column<bool>(type: "bit", nullable: false),
                    OlusturanKullaniciId = table.Column<int>(type: "int", nullable: false),
                    OlusturmaZamani = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TakvimPlani", x => x.Id);
                    table.CheckConstraint("CK_TakvimPlani_Adet", "[Adet] IS NULL OR [Adet] > 0");
                    table.ForeignKey(
                        name: "FK_TakvimPlani_Kullanici_OlusturanKullaniciId",
                        column: x => x.OlusturanKullaniciId,
                        principalTable: "Kullanici",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TakvimPlani_Parca_ParcaId",
                        column: x => x.ParcaId,
                        principalTable: "Parca",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TakvimPlani_Tedarikci_TedarikciId",
                        column: x => x.TedarikciId,
                        principalTable: "Tedarikci",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TakvimPlani_OlusturanKullaniciId",
                table: "TakvimPlani",
                column: "OlusturanKullaniciId");

            migrationBuilder.CreateIndex(
                name: "IX_TakvimPlani_ParcaId",
                table: "TakvimPlani",
                column: "ParcaId");

            migrationBuilder.CreateIndex(
                name: "IX_TakvimPlani_Tarih",
                table: "TakvimPlani",
                column: "Tarih");

            migrationBuilder.CreateIndex(
                name: "IX_TakvimPlani_TedarikciId",
                table: "TakvimPlani",
                column: "TedarikciId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TakvimPlani");

            migrationBuilder.AddColumn<string>(
                name: "Birim",
                table: "Parca",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "");
        }
    }
}
