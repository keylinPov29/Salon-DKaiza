using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DKaiza.Migrations
{
    /// <inheritdoc />
    public partial class RegistrarPagoMercadoPago : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CuponCodigo",
                table: "Citas",
                type: "character varying(30)",
                maxLength: 30,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "Descuento",
                table: "Citas",
                type: "numeric(10,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<DateTime>(
                name: "FechaPago",
                table: "Citas",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "MetodoPago",
                table: "Citas",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "NumeroOperacion",
                table: "Citas",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "PagoEnProceso",
                table: "Citas",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<decimal>(
                name: "PrecioBase",
                table: "Citas",
                type: "numeric(10,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.Sql("""
                UPDATE "Citas"
                SET "PrecioBase" = "PrecioTotal";
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CuponCodigo",
                table: "Citas");

            migrationBuilder.DropColumn(
                name: "Descuento",
                table: "Citas");

            migrationBuilder.DropColumn(
                name: "FechaPago",
                table: "Citas");

            migrationBuilder.DropColumn(
                name: "MetodoPago",
                table: "Citas");

            migrationBuilder.DropColumn(
                name: "NumeroOperacion",
                table: "Citas");

            migrationBuilder.DropColumn(
                name: "PagoEnProceso",
                table: "Citas");

            migrationBuilder.DropColumn(
                name: "PrecioBase",
                table: "Citas");
        }
    }
}
