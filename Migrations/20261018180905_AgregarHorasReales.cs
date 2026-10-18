using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DKaiza.Migrations
{
    /// <inheritdoc />
    public partial class AgregarHorasReales : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "HoraFinReal",
                table: "Citas",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "HoraInicioReal",
                table: "Citas",
                type: "timestamp with time zone",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "HoraFinReal",
                table: "Citas");

            migrationBuilder.DropColumn(
                name: "HoraInicioReal",
                table: "Citas");
        }
    }
}
