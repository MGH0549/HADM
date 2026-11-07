using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Hadm.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddAiPreferences : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "AiConsentAt",
                table: "AspNetUsers",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "AiEnabled",
                table: "AspNetUsers",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AiConsentAt",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "AiEnabled",
                table: "AspNetUsers");
        }
    }
}
