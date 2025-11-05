using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GimnasioCuerpoSano.Migrations
{
    /// <inheritdoc />
    public partial class AgregarCupoMaximoEnClase : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "CupoMaximo",
                table: "Clase",
                type: "int",
                nullable: false,
                defaultValue: 1);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CupoMaximo",
                table: "Clase");
        }
    }
}

