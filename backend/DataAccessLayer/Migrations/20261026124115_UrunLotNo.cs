using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DataAccessLayer.Migrations
{
    public partial class UrunLotNo : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "LotNo",
                table: "Uretim",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "TedarikciLotNo",
                table: "StokLotu",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_Uretim_LotNo",
                table: "Uretim",
                column: "LotNo");

            migrationBuilder.CreateIndex(
                name: "IX_StokLotu_TedarikciLotNo",
                table: "StokLotu",
                column: "TedarikciLotNo");

            migrationBuilder.Sql("UPDATE StokLotu SET TedarikciLotNo = LotNo;");

            migrationBuilder.Sql("""
                WITH Parti AS (
                    SELECT UrunId, Tarih, ROW_NUMBER() OVER (PARTITION BY UrunId ORDER BY Tarih, MIN(Id)) AS Sira
                    FROM Uretim GROUP BY UrunId, Tarih)
                UPDATE u SET LotNo = ur.SeriOneki + 'L' + RIGHT('00' + CAST(p.Sira AS varchar(10)), 3)
                FROM Uretim u
                JOIN Parti p ON p.UrunId = u.UrunId AND p.Tarih = u.Tarih
                JOIN Urun ur ON ur.Id = u.UrunId;
                """);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Uretim_LotNo",
                table: "Uretim");

            migrationBuilder.DropIndex(
                name: "IX_StokLotu_TedarikciLotNo",
                table: "StokLotu");

            migrationBuilder.DropColumn(
                name: "LotNo",
                table: "Uretim");

            migrationBuilder.DropColumn(
                name: "TedarikciLotNo",
                table: "StokLotu");
        }
    }
}
