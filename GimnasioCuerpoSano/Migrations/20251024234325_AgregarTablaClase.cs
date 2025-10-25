using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GimnasioCuerpoSano.Migrations
{
    /// <inheritdoc />
    public partial class AgregarTablaClase : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
           // migrationBuilder.AddColumn<DateTime>(
           //     name: "FechaVencimiento",
           //     table: "Miembros",
           //     nullable: false,
            //    defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

           // migrationBuilder.AddColumn<int>(
           //     name: "DuracionEnMeses",
           //     table: "Membresias",
           //     type: "int",
           //     nullable: false,
           //     defaultValue: 0);

          //  migrationBuilder.AlterColumn<string>(
           //     name: "Telefono",
           //     table: "Entrenadores",
           //     type: "nvarchar(max)",
           //     nullable: false,
           //     oldClrType: typeof(string),
           //     oldType: "nvarchar(10)",
           //     oldMaxLength: 10);

            migrationBuilder.CreateTable(
                name: "Clase",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Nombre = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Precio = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    DuracionMinutos = table.Column<int>(type: "int", nullable: false),
                    EntrenadorId = table.Column<int>(type: "int", nullable: false),
                    SalaId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Clase", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Clase_Entrenadores_EntrenadorId",
                        column: x => x.EntrenadorId,
                        principalTable: "Entrenadores",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Clase_Salas_SalaId",
                        column: x => x.SalaId,
                        principalTable: "Salas",
                        principalColumn: "ID_Sala",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Clase_EntrenadorId",
                table: "Clase",
                column: "EntrenadorId");

            migrationBuilder.CreateIndex(
                name: "IX_Clase_SalaId",
                table: "Clase",
                column: "SalaId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Clase");
        }
    }
}
