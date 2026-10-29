using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DataAccessLayer.Migrations
{
    /// <inheritdoc />
    public partial class Duzenlemeler : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "BilgiVerildiZamani",
                table: "UrunGeriCagirma",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "Zaman",
                table: "Satis",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "Aktif",
                table: "Kullanici",
                type: "bit",
                nullable: false,
                defaultValue: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "BilgiVerildiZamani",
                table: "UrunGeriCagirma");

            migrationBuilder.DropColumn(
                name: "Zaman",
                table: "Satis");

            migrationBuilder.DropColumn(
                name: "Aktif",
                table: "Kullanici");
        }
    }
}
