using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace DKaiza.Migrations
{
    /// <inheritdoc />
    public partial class Estilistas : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "EstilistaId",
                table: "Usuarios",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Estilistas",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Nombres = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Apellidos = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Dni = table.Column<string>(type: "character varying(12)", maxLength: 12, nullable: false),
                    Telefono = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Especialidad = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    InicioJornada = table.Column<TimeOnly>(type: "time without time zone", nullable: false),
                    FinJornada = table.Column<TimeOnly>(type: "time without time zone", nullable: false),
                    InicioDescanso = table.Column<TimeOnly>(type: "time without time zone", nullable: true),
                    FinDescanso = table.Column<TimeOnly>(type: "time without time zone", nullable: true),
                    Foto = table.Column<byte[]>(type: "bytea", nullable: true),
                    FotoTipo = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    Activo = table.Column<bool>(type: "boolean", nullable: false),
                    UsuarioRegistroId = table.Column<int>(type: "integer", nullable: true),
                    FechaRegistro = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UsuarioModificaId = table.Column<int>(type: "integer", nullable: true),
                    FechaModifica = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Estilistas", x => x.Id);
                });

            migrationBuilder.InsertData(
                table: "Estilistas",
                columns: new[] { "Id", "Activo", "Apellidos", "Dni", "Especialidad", "FechaModifica", "FechaRegistro", "FinDescanso", "FinJornada", "Foto", "FotoTipo", "InicioDescanso", "InicioJornada", "Nombres", "Telefono", "UsuarioModificaId", "UsuarioRegistroId" },
                values: new object[,]
                {
                    { 1, true, "González", "70000001", "Coloración", null, new DateTime(2026, 10, 4, 0, 0, 0, 0, DateTimeKind.Utc), new TimeOnly(15, 0, 0), new TimeOnly(20, 0, 0), null, null, new TimeOnly(14, 0, 0), new TimeOnly(10, 0, 0), "María", "999000001", null, null },
                    { 2, true, "Fernández", "70000002", "Cabello", null, new DateTime(2026, 10, 4, 0, 0, 0, 0, DateTimeKind.Utc), new TimeOnly(15, 0, 0), new TimeOnly(20, 0, 0), null, null, new TimeOnly(14, 0, 0), new TimeOnly(10, 0, 0), "Laura", "999000002", null, null },
                    { 3, true, "Rojas", "70000003", "Tratamientos", null, new DateTime(2026, 10, 4, 0, 0, 0, 0, DateTimeKind.Utc), new TimeOnly(15, 0, 0), new TimeOnly(20, 0, 0), null, null, new TimeOnly(14, 0, 0), new TimeOnly(10, 0, 0), "Carolina", "999000003", null, null }
                });

            migrationBuilder.CreateIndex(
                name: "IX_Usuarios_EstilistaId",
                table: "Usuarios",
                column: "EstilistaId");

            migrationBuilder.CreateIndex(
                name: "IX_Estilistas_Dni",
                table: "Estilistas",
                column: "Dni",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Usuarios_Estilistas_EstilistaId",
                table: "Usuarios",
                column: "EstilistaId",
                principalTable: "Estilistas",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Usuarios_Estilistas_EstilistaId",
                table: "Usuarios");

            migrationBuilder.DropTable(
                name: "Estilistas");

            migrationBuilder.DropIndex(
                name: "IX_Usuarios_EstilistaId",
                table: "Usuarios");

            migrationBuilder.DropColumn(
                name: "EstilistaId",
                table: "Usuarios");
        }
    }
}
