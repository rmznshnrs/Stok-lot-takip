using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LotTakip.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class SatisIade : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "SatisIade",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SatisId = table.Column<int>(type: "int", nullable: false),
                    UretimId = table.Column<int>(type: "int", nullable: false),
                    Aciklama = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    OlusturanKullaniciId = table.Column<int>(type: "int", nullable: false),
                    OlusturmaZamani = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SatisIade", x => x.Id);
                    table.CheckConstraint("CK_SatisIade_Aciklama", "LEN([Aciklama]) > 0");
                    table.ForeignKey(
                        name: "FK_SatisIade_Kullanici_OlusturanKullaniciId",
                        column: x => x.OlusturanKullaniciId,
                        principalTable: "Kullanici",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SatisIade_Satis_SatisId",
                        column: x => x.SatisId,
                        principalTable: "Satis",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SatisIade_Uretim_UretimId",
                        column: x => x.UretimId,
                        principalTable: "Uretim",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SatisIade_OlusturanKullaniciId",
                table: "SatisIade",
                column: "OlusturanKullaniciId");

            migrationBuilder.CreateIndex(
                name: "IX_SatisIade_SatisId",
                table: "SatisIade",
                column: "SatisId");

            migrationBuilder.CreateIndex(
                name: "IX_SatisIade_UretimId",
                table: "SatisIade",
                column: "UretimId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SatisIade");
        }
    }
}
