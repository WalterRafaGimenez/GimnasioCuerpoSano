using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GimnasioCuerpoSano.Migrations
{
    /// <inheritdoc />
    public partial class VincularClaseHorarioInscripcion : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "HorarioClase",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ClaseId = table.Column<int>(type: "int", nullable: false),
                    DiaSemana = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    HoraInicio = table.Column<TimeSpan>(type: "time", nullable: false),
                    HoraFin = table.Column<TimeSpan>(type: "time", nullable: false),
                    CapacidadMaxima = table.Column<int>(type: "int", nullable: false),
                    SePuedeInscribir = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HorarioClase", x => x.Id);
                    table.ForeignKey(
                        name: "FK_HorarioClase_Clase_ClaseId",
                        column: x => x.ClaseId,
                        principalTable: "Clase",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "InscripcionClase",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    HorarioClaseId = table.Column<int>(type: "int", nullable: false),
                    MiembroId = table.Column<int>(type: "int", nullable: false),
                    Estado = table.Column<bool>(type: "bit", nullable: false),
                    FechaInscripcion = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InscripcionClase", x => x.Id);
                    table.ForeignKey(
                        name: "FK_InscripcionClase_HorarioClase_HorarioClaseId",
                        column: x => x.HorarioClaseId,
                        principalTable: "HorarioClase",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_InscripcionClase_Miembros_MiembroId",
                        column: x => x.MiembroId,
                        principalTable: "Miembros",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_HorarioClase_ClaseId",
                table: "HorarioClase",
                column: "ClaseId");

            migrationBuilder.CreateIndex(
                name: "IX_InscripcionClase_HorarioClaseId",
                table: "InscripcionClase",
                column: "HorarioClaseId");

            migrationBuilder.CreateIndex(
                name: "IX_InscripcionClase_MiembroId",
                table: "InscripcionClase",
                column: "MiembroId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "InscripcionClase");

            migrationBuilder.DropTable(
                name: "HorarioClase");
        }
    }
}
