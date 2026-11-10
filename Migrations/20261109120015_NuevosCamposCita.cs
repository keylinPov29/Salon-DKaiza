using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DKaiza.Migrations
{
    public partial class NuevosCamposCita : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Todas las columnas ya existen en la base de datos de Neon.
            // Comentamos estas líneas para evitar el error de "columna duplicada".
            
            // migrationBuilder.AddColumn<bool>(
            //     name: "AsistenciaConfirmada",
            //     table: "Citas",
            //     type: "boolean",
            //     nullable: false,
            //     defaultValue: false);

            // migrationBuilder.AddColumn<string>(
            //     name: "CuponCodigo",
            //     table: "Citas",
            //     type: "character varying(30)",
            //     maxLength: 30,
            //     nullable: true);

            // migrationBuilder.AddColumn<decimal>(
            //     name: "Descuento",
            //     table: "Citas",
            //     type: "numeric",
            //     nullable: false,
            //     defaultValue: 0m);

            // migrationBuilder.AddColumn<DateTime>(
            //     name: "FechaPago",
            //     table: "Citas",
            //     type: "timestamp with time zone",
            //     nullable: true);

            // migrationBuilder.AddColumn<int>(
            //     name: "MetodoPago",
            //     table: "Citas",
            //     type: "integer",
            //     nullable: true);

            // migrationBuilder.AddColumn<string>(
            //     name: "NumeroOperacion",
            //     table: "Citas",
            //     type: "character varying(100)",
            //     maxLength: 100,
            //     nullable: true);

            // migrationBuilder.AddColumn<bool>(
            //     name: "PagoEnProceso",
            //     table: "Citas",
            //     type: "boolean",
            //     nullable: false,
            //     defaultValue: false);

            // migrationBuilder.AddColumn<decimal>(
            //     name: "PrecioBase",
            //     table: "Citas",
            //     type: "numeric",
            //     nullable: false,
            //     defaultValue: 0m);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Al revertir, no eliminamos nada para no borrar datos existentes.
        }
    }
}