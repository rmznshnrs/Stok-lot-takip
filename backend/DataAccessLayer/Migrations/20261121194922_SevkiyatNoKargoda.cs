using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DataAccessLayer.Migrations
{
    public partial class SevkiyatNoKargoda : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Satis_No",
                table: "Satis");

            migrationBuilder.AlterColumn<string>(
                name: "No",
                table: "Satis",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(20)",
                oldMaxLength: 20);

            migrationBuilder.CreateIndex(
                name: "IX_Satis_No",
                table: "Satis",
                column: "No",
                unique: true,
                filter: "[No] IS NOT NULL");

            migrationBuilder.Sql("UPDATE Satis SET No = NULL WHERE Durum = N'Hazirlaniyor' AND SiparisId IS NOT NULL");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Satis_No",
                table: "Satis");

            migrationBuilder.AlterColumn<string>(
                name: "No",
                table: "Satis",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(20)",
                oldMaxLength: 20,
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Satis_No",
                table: "Satis",
                column: "No",
                unique: true);
        }
    }
}
