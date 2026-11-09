using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace DKaiza.Migrations
{
    /// <inheritdoc />
    public partial class CalificacionesCitas : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Calificaciones",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CitaId = table.Column<int>(type: "integer", nullable: false),
                    EstilistaId = table.Column<int>(type: "integer", nullable: false),
                    ClienteId = table.Column<int>(type: "integer", nullable: false),
                    Estrellas = table.Column<int>(type: "integer", nullable: false),
                    Comentario = table.Column<string>(type: "character varying(250)", maxLength: 250, nullable: true),
                    FechaCreacion = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Calificaciones", x => x.Id);
                    table.CheckConstraint("CK_Calificaciones_Estrellas", "\"Estrellas\" BETWEEN 1 AND 5");
                    table.ForeignKey(
                        name: "FK_Calificaciones_Citas_CitaId",
                        column: x => x.CitaId,
                        principalTable: "Citas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Calificaciones_Estilistas_EstilistaId",
                        column: x => x.EstilistaId,
                        principalTable: "Estilistas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Calificaciones_Usuarios_ClienteId",
                        column: x => x.ClienteId,
                        principalTable: "Usuarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Calificaciones_CitaId",
                table: "Calificaciones",
                column: "CitaId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Calificaciones_ClienteId",
                table: "Calificaciones",
                column: "ClienteId");

            migrationBuilder.CreateIndex(
                name: "IX_Calificaciones_EstilistaId_FechaCreacion",
                table: "Calificaciones",
                columns: new[] { "EstilistaId", "FechaCreacion" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Calificaciones");
        }
    }
}
