using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace DKaiza.Migrations
{
    /// <inheritdoc />
    public partial class AgregarComprobantePago : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ComprobantesPago",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    NumeroComprobante = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    CitaId = table.Column<int>(type: "integer", nullable: false),
                    ClienteId = table.Column<int>(type: "integer", nullable: false),
                    ClienteNombre = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    ClienteEmail = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    ClienteTelefono = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true),
                    ServicioNombre = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    EstilistaId = table.Column<int>(type: "integer", nullable: true),
                    EstilistaNombre = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    MontoBase = table.Column<decimal>(type: "numeric(10,2)", nullable: false),
                    Descuento = table.Column<decimal>(type: "numeric(10,2)", nullable: false),
                    CuponCodigo = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true),
                    MontoTotal = table.Column<decimal>(type: "numeric(10,2)", nullable: false),
                    MetodoPago = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    NumeroOperacion = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    FechaEmision = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    FechaServicio = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ComprobantesPago", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ComprobantesPago_Citas_CitaId",
                        column: x => x.CitaId,
                        principalTable: "Citas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ComprobantesPago_Usuarios_ClienteId",
                        column: x => x.ClienteId,
                        principalTable: "Usuarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ComprobantesPago_CitaId",
                table: "ComprobantesPago",
                column: "CitaId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ComprobantesPago_ClienteId",
                table: "ComprobantesPago",
                column: "ClienteId");

            migrationBuilder.CreateIndex(
                name: "IX_ComprobantesPago_NumeroComprobante",
                table: "ComprobantesPago",
                column: "NumeroComprobante",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ComprobantesPago");
        }
    }
}
