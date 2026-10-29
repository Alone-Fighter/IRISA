using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IRISA.Backend.Migrations
{
    /// <inheritdoc />
    public partial class AddPersonDemographics : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateOnly>(
                name: "BirthDate",
                table: "PersonSnapshots",
                type: "date",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Gender",
                table: "PersonSnapshots",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "BirthDate",
                table: "PersonSnapshots");

            migrationBuilder.DropColumn(
                name: "Gender",
                table: "PersonSnapshots");
        }
    }
}
