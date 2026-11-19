using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DataAccessLayer.Migrations
{
    public partial class Sevkiyat : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Aciklama",
                table: "Satis",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Durum",
                table: "Satis",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "KargoFirmasi",
                table: "Satis",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "KargoZamani",
                table: "Satis",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "No",
                table: "Satis",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "SiparisId",
                table: "Satis",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TakipNo",
                table: "Satis",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "TeslimZamani",
                table: "Satis",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.Sql("""
                UPDATE Satis SET No = CONCAT('SV-', RIGHT(CONCAT('0000', Id), 4)),
                                 Durum = 'TeslimEdildi', TeslimZamani = COALESCE(Zaman, OlusturmaZamani)
                """);

            migrationBuilder.CreateIndex(
                name: "IX_Satis_Durum",
                table: "Satis",
                column: "Durum");

            migrationBuilder.CreateIndex(
                name: "IX_Satis_No",
                table: "Satis",
                column: "No",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Satis_SiparisId",
                table: "Satis",
                column: "SiparisId");

            migrationBuilder.AddForeignKey(
                name: "FK_Satis_Siparis_SiparisId",
                table: "Satis",
                column: "SiparisId",
                principalTable: "Siparis",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Satis_Siparis_SiparisId",
                table: "Satis");

            migrationBuilder.DropIndex(
                name: "IX_Satis_Durum",
                table: "Satis");

            migrationBuilder.DropIndex(
                name: "IX_Satis_No",
                table: "Satis");

            migrationBuilder.DropIndex(
                name: "IX_Satis_SiparisId",
                table: "Satis");

            migrationBuilder.DropColumn(
                name: "Aciklama",
                table: "Satis");

            migrationBuilder.DropColumn(
                name: "Durum",
                table: "Satis");

            migrationBuilder.DropColumn(
                name: "KargoFirmasi",
                table: "Satis");

            migrationBuilder.DropColumn(
                name: "KargoZamani",
                table: "Satis");

            migrationBuilder.DropColumn(
                name: "No",
                table: "Satis");

            migrationBuilder.DropColumn(
                name: "SiparisId",
                table: "Satis");

            migrationBuilder.DropColumn(
                name: "TakipNo",
                table: "Satis");

            migrationBuilder.DropColumn(
                name: "TeslimZamani",
                table: "Satis");
        }
    }
}
