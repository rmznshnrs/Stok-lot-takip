using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DataAccessLayer.Migrations
{
    public partial class GeriCagirmaKargo : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "GeriAlmaKargoFirmasi",
                table: "UrunGeriCagirma",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "GeriAlmaTakipNo",
                table: "UrunGeriCagirma",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "GeriGondermeKargoFirmasi",
                table: "UrunGeriCagirma",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "GeriGondermeTakipNo",
                table: "UrunGeriCagirma",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "GeriAlmaKargoFirmasi",
                table: "UrunGeriCagirma");

            migrationBuilder.DropColumn(
                name: "GeriAlmaTakipNo",
                table: "UrunGeriCagirma");

            migrationBuilder.DropColumn(
                name: "GeriGondermeKargoFirmasi",
                table: "UrunGeriCagirma");

            migrationBuilder.DropColumn(
                name: "GeriGondermeTakipNo",
                table: "UrunGeriCagirma");
        }
    }
}
