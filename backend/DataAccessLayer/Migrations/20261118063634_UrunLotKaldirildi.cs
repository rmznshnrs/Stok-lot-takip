using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DataAccessLayer.Migrations
{
    /// <inheritdoc />
    public partial class UrunLotKaldirildi : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "UrunLotSorunu");

            migrationBuilder.DropIndex(
                name: "IX_Uretim_LotNo",
                table: "Uretim");

            migrationBuilder.DropColumn(
                name: "LotNo",
                table: "Uretim");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "LotNo",
                table: "Uretim",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateTable(
                name: "UrunLotSorunu",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    OlusturanKullaniciId = table.Column<int>(type: "int", nullable: false),
                    UrunId = table.Column<int>(type: "int", nullable: false),
                    Aciklama = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    HataliAdet = table.Column<int>(type: "int", nullable: false),
                    LotNo = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
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

            migrationBuilder.CreateIndex(
                name: "IX_Uretim_LotNo",
                table: "Uretim",
                column: "LotNo");

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
    }
}
