using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GimnasioCuerpoSano.Migrations
{
    /// <inheritdoc />
    public partial class AgregarDNIenMiembro : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "DNI",
                table: "Miembros",
                type: "nvarchar(12)",
                maxLength: 12,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "TipoDocumento",
                table: "Miembros",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DNI",
                table: "Miembros");

            migrationBuilder.DropColumn(
                name: "TipoDocumento",
                table: "Miembros");
        }
    }
}
