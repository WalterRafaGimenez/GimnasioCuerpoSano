using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GimnasioCuerpoSano.Migrations
{
    /// <inheritdoc />
    public partial class AgregarHoraSalidaAsistencia : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
            name: "HoraSalida",
            table: "Asistencia",
            type: "datetime2",
            nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
            name: "HoraSalida",
            table: "Asistencia");
        }
    }
}
