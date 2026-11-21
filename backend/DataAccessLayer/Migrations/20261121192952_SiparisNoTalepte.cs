using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DataAccessLayer.Migrations
{
    public partial class SiparisNoTalepte : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "SiparisNo",
                table: "Siparis",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.Sql("UPDATE s SET SiparisNo = v.SiparisNo FROM Siparis s CROSS APPLY (SELECT TOP 1 SiparisNo FROM Satis WHERE SiparisId = s.Id AND SiparisNo <> N'' ORDER BY Tarih, Id) v");

            migrationBuilder.CreateIndex(
                name: "IX_Siparis_SiparisNo",
                table: "Siparis",
                column: "SiparisNo");

            migrationBuilder.DropIndex(
                name: "IX_Satis_SiparisNo",
                table: "Satis");

            migrationBuilder.DropColumn(
                name: "SiparisNo",
                table: "Satis");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Siparis_SiparisNo",
                table: "Siparis");

            migrationBuilder.DropColumn(
                name: "SiparisNo",
                table: "Siparis");

            migrationBuilder.AddColumn<string>(
                name: "SiparisNo",
                table: "Satis",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_Satis_SiparisNo",
                table: "Satis",
                column: "SiparisNo");
        }
    }
}
