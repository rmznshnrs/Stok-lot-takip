using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DataAccessLayer.Migrations
{
    /// <inheritdoc />
    public partial class UretimHurda : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "UretimHurda",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UretimId = table.Column<int>(type: "int", nullable: false),
                    Tur = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Aciklama = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    OlusturanKullaniciId = table.Column<int>(type: "int", nullable: false),
                    OlusturmaZamani = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UretimHurda", x => x.Id);
                    table.CheckConstraint("CK_UretimHurda_Aciklama", "LEN([Aciklama]) > 0");
                    table.ForeignKey(
                        name: "FK_UretimHurda_Kullanici_OlusturanKullaniciId",
                        column: x => x.OlusturanKullaniciId,
                        principalTable: "Kullanici",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_UretimHurda_Uretim_UretimId",
                        column: x => x.UretimId,
                        principalTable: "Uretim",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_UretimHurda_OlusturanKullaniciId",
                table: "UretimHurda",
                column: "OlusturanKullaniciId");

            migrationBuilder.CreateIndex(
                name: "IX_UretimHurda_UretimId",
                table: "UretimHurda",
                column: "UretimId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "UretimHurda");
        }
    }
}
