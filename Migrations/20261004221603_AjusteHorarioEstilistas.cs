using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DKaiza.Migrations
{
    /// <inheritdoc />
    public partial class AjusteHorarioEstilistas : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "Estilistas",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "FinDescanso", "FinJornada", "InicioDescanso" },
                values: new object[] { new TimeOnly(14, 0, 0), new TimeOnly(22, 0, 0), new TimeOnly(13, 0, 0) });

            migrationBuilder.UpdateData(
                table: "Estilistas",
                keyColumn: "Id",
                keyValue: 2,
                column: "FinJornada",
                value: new TimeOnly(22, 0, 0));

            migrationBuilder.UpdateData(
                table: "Estilistas",
                keyColumn: "Id",
                keyValue: 3,
                columns: new[] { "FinDescanso", "FinJornada", "InicioDescanso" },
                values: new object[] { new TimeOnly(16, 0, 0), new TimeOnly(22, 0, 0), new TimeOnly(15, 0, 0) });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "Estilistas",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "FinDescanso", "FinJornada", "InicioDescanso" },
                values: new object[] { new TimeOnly(15, 0, 0), new TimeOnly(20, 0, 0), new TimeOnly(14, 0, 0) });

            migrationBuilder.UpdateData(
                table: "Estilistas",
                keyColumn: "Id",
                keyValue: 2,
                column: "FinJornada",
                value: new TimeOnly(20, 0, 0));

            migrationBuilder.UpdateData(
                table: "Estilistas",
                keyColumn: "Id",
                keyValue: 3,
                columns: new[] { "FinDescanso", "FinJornada", "InicioDescanso" },
                values: new object[] { new TimeOnly(15, 0, 0), new TimeOnly(20, 0, 0), new TimeOnly(14, 0, 0) });
        }
    }
}
