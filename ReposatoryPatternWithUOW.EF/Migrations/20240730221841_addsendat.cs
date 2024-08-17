using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ReposatoryPatternWithUOW.EF.Migrations
{
    /// <inheritdoc />
    public partial class addsendat : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CourseId",
                table: "ChatMessage");

            migrationBuilder.AddColumn<DateTime>(
                name: "SendAt",
                table: "Messages",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SendAt",
                table: "Messages");

            migrationBuilder.AddColumn<int>(
                name: "CourseId",
                table: "ChatMessage",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }
    }
}
