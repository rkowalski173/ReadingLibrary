using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ReadingLibrary.Migrations
{
    /// <inheritdoc />
    public partial class FixCollation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(name: "IX_Books_Title_Id",   table: "Books");
            migrationBuilder.DropIndex(name: "IX_Authors_Name_Id",  table: "Authors");

            migrationBuilder.AlterColumn<string>(
                name: "Title",
                table: "Books",
                type: "text",
                nullable: false,
                collation: "und-x-icu",
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "Authors",
                type: "text",
                nullable: false,
                collation: "und-x-icu",
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.CreateIndex(name: "IX_Books_Title_Id",  table: "Books",   columns: ["Title", "Id"]);
            migrationBuilder.CreateIndex(name: "IX_Authors_Name_Id", table: "Authors", columns: ["Name", "Id"]);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "Title",
                table: "Books",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text",
                oldCollation: "und-x-icu");

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "Authors",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text",
                oldCollation: "und-x-icu");
        }
    }
}
