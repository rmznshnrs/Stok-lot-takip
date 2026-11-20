using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DataAccessLayer.Migrations
{
    public partial class SevkiyatSiparisNo : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
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

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Satis_SiparisNo",
                table: "Satis");

            migrationBuilder.DropColumn(
                name: "SiparisNo",
                table: "Satis");
        }
    }
}
