using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DataAccessLayer.Migrations
{
    /// <inheritdoc />
    public partial class IletisimAlanlari : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Adres",
                table: "Tedarikci",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Eposta",
                table: "Tedarikci",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Telefon",
                table: "Tedarikci",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Adres",
                table: "Musteri",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Eposta",
                table: "Musteri",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Telefon",
                table: "Musteri",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            // Eski tek alanlı iletişim " · " ile bölünür: @ içeren e-posta, yalnız rakam/boşluk/+()- olan telefon,
            // kalanı adres. Sonra Iletisim bu üç alandan hesaplanan sütun olur.
            foreach (var tablo in new[] { "Tedarikci", "Musteri" })
            {
                migrationBuilder.Sql($$"""
                    UPDATE t SET
                        Eposta = LEFT(ISNULL((SELECT TOP 1 TRIM(p.value) FROM STRING_SPLIT(REPLACE(t.Iletisim, N' · ', N'|'), N'|') p
                                              WHERE p.value LIKE N'%@%'), N''), 200),
                        Telefon = LEFT(ISNULL((SELECT TOP 1 TRIM(p.value) FROM STRING_SPLIT(REPLACE(t.Iletisim, N' · ', N'|'), N'|') p
                                               WHERE p.value NOT LIKE N'%@%' AND TRIM(p.value) NOT LIKE N'%[^0-9 +()-]%'
                                                     AND LEN(TRIM(p.value)) >= 7), N''), 50),
                        Adres = LEFT(ISNULL((SELECT STRING_AGG(TRIM(p.value), N' · ') FROM STRING_SPLIT(REPLACE(t.Iletisim, N' · ', N'|'), N'|') p
                                             WHERE TRIM(p.value) <> N'' AND p.value NOT LIKE N'%@%'
                                                   AND NOT (TRIM(p.value) NOT LIKE N'%[^0-9 +()-]%' AND LEN(TRIM(p.value)) >= 7)), N''), 500)
                    FROM {{tablo}} t;
                    """);
            }

            migrationBuilder.AlterColumn<string>(
                name: "Iletisim",
                table: "Tedarikci",
                type: "nvarchar(max)",
                nullable: false,
                computedColumnSql: "CONCAT_WS(N' · ', NULLIF([Telefon], N''), NULLIF([Eposta], N''), NULLIF([Adres], N''))",
                oldClrType: typeof(string),
                oldType: "nvarchar(500)",
                oldMaxLength: 500);

            migrationBuilder.AlterColumn<string>(
                name: "Iletisim",
                table: "Musteri",
                type: "nvarchar(max)",
                nullable: false,
                computedColumnSql: "CONCAT_WS(N' · ', NULLIF([Telefon], N''), NULLIF([Eposta], N''), NULLIF([Adres], N''))",
                oldClrType: typeof(string),
                oldType: "nvarchar(500)",
                oldMaxLength: 500);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Adres",
                table: "Tedarikci");

            migrationBuilder.DropColumn(
                name: "Eposta",
                table: "Tedarikci");

            migrationBuilder.DropColumn(
                name: "Telefon",
                table: "Tedarikci");

            migrationBuilder.DropColumn(
                name: "Adres",
                table: "Musteri");

            migrationBuilder.DropColumn(
                name: "Eposta",
                table: "Musteri");

            migrationBuilder.DropColumn(
                name: "Telefon",
                table: "Musteri");

            migrationBuilder.AlterColumn<string>(
                name: "Iletisim",
                table: "Tedarikci",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldComputedColumnSql: "CONCAT_WS(N' · ', NULLIF([Telefon], N''), NULLIF([Eposta], N''), NULLIF([Adres], N''))");

            migrationBuilder.AlterColumn<string>(
                name: "Iletisim",
                table: "Musteri",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldComputedColumnSql: "CONCAT_WS(N' · ', NULLIF([Telefon], N''), NULLIF([Eposta], N''), NULLIF([Adres], N''))");
        }
    }
}
