using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GimnasioCuerpoSano.Migrations
{
    /// <inheritdoc />
    public partial class MapeoDiaSemanaLimpio : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // ----------------------------------------------------------------------
            // PASO 1: Mantenemos la eliminación de FKs en 'Clase'
            //         (Esto es lo que EF Core necesita para re-establecerlas correctamente)
            // ----------------------------------------------------------------------
            migrationBuilder.DropForeignKey(
                name: "FK_Clase_Entrenadores_EntrenadorId",
                table: "Clase");

            migrationBuilder.DropForeignKey(
                name: "FK_Clase_Salas_SalaId",
                table: "Clase");

            // ----------------------------------------------------------------------
            // PASO 2: Mantenemos la ALTERACIÓN de 'DiaSemana'
            //         (Esto registra el mapeo de Enum a String y soluciona el error InvalidCastException)
            // ----------------------------------------------------------------------
            migrationBuilder.AlterColumn<string>(
                name: "DiaSemana",
                table: "HorarioClase",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(20)",
                oldMaxLength: 20);

            // ----------------------------------------------------------------------
            // ❌ LÍNEAS ELIMINADAS: Se eliminan las siguientes líneas que duplicaban la columna SalaId:
            //
            // migrationBuilder.AddColumn<int>(name: "SalaId", table: "HorarioClase", ...);
            // migrationBuilder.CreateIndex(name: "IX_HorarioClase_SalaId", ...);
            // migrationBuilder.AddForeignKey(name: "FK_HorarioClase_Salas_SalaId", ...);
            // ----------------------------------------------------------------------


            // ----------------------------------------------------------------------
            // PASO 3: Mantenemos la RECREACIÓN de las FKs en 'Clase'
            // ----------------------------------------------------------------------
            migrationBuilder.AddForeignKey(
                name: "FK_Clase_Entrenadores_EntrenadorId",
                table: "Clase",
                column: "EntrenadorId",
                principalTable: "Entrenadores",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade); // Asumiendo que quieres CASCADE aquí tras la limpieza de la FK

            migrationBuilder.AddForeignKey(
                name: "FK_Clase_Salas_SalaId",
                table: "Clase",
                column: "SalaId",
                principalTable: "Salas",
                principalColumn: "ID_Sala",
                onDelete: ReferentialAction.Cascade); // Asumiendo que quieres CASCADE aquí tras la limpieza de la FK
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // ----------------------------------------------------------------------
            // PASO 1: Drop de las FKs en 'Clase'
            // ----------------------------------------------------------------------
            migrationBuilder.DropForeignKey(
                name: "FK_Clase_Entrenadores_EntrenadorId",
                table: "Clase");

            migrationBuilder.DropForeignKey(
                name: "FK_Clase_Salas_SalaId",
                table: "Clase");

            // ----------------------------------------------------------------------
            // ❌ LÍNEAS ELIMINADAS: Se eliminan las siguientes líneas que deshacen la adición de SalaId:
            //
            // migrationBuilder.DropForeignKey(name: "FK_HorarioClase_Salas_SalaId", table: "HorarioClase");
            // migrationBuilder.DropIndex(name: "IX_HorarioClase_SalaId", table: "HorarioClase");
            // migrationBuilder.DropColumn(name: "SalaId", table: "HorarioClase");
            // ----------------------------------------------------------------------


            // ----------------------------------------------------------------------
            // PASO 2: Revertir la alteración de 'DiaSemana'
            // ----------------------------------------------------------------------
            migrationBuilder.AlterColumn<string>(
                name: "DiaSemana",
                table: "HorarioClase",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            // ----------------------------------------------------------------------
            // PASO 3: Revertir las FKs en 'Clase' a su estado original (Restrict)
            // ----------------------------------------------------------------------
            migrationBuilder.AddForeignKey(
                name: "FK_Clase_Entrenadores_EntrenadorId",
                table: "Clase",
                column: "EntrenadorId",
                principalTable: "Entrenadores",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Clase_Salas_SalaId",
                table: "Clase",
                column: "SalaId",
                principalTable: "Salas",
                principalColumn: "ID_Sala",
                onDelete: ReferentialAction.Restrict);
        }
    }
}