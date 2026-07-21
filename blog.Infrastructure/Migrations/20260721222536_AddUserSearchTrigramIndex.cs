using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace blog.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddUserSearchTrigramIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterDatabase()
                .Annotation("Npgsql:PostgresExtension:pg_trgm", ",,");

            migrationBuilder.AddColumn<string>(
                name: "FullNameSearch",
                table: "Users",
                type: "character varying(256)",
                maxLength: 256,
                nullable: true,
                computedColumnSql: "\"FirstName\" || ' ' || \"LastName\"",
                stored: true);

            migrationBuilder.CreateIndex(
                name: "IX_Users_Email_Trgm",
                table: "Users",
                column: "Email")
                .Annotation("Npgsql:IndexMethod", "gin")
                .Annotation("Npgsql:IndexOperators", new[] { "gin_trgm_ops" });

            migrationBuilder.CreateIndex(
                name: "IX_Users_FullNameSearch_Trgm",
                table: "Users",
                column: "FullNameSearch")
                .Annotation("Npgsql:IndexMethod", "gin")
                .Annotation("Npgsql:IndexOperators", new[] { "gin_trgm_ops" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Users_Email_Trgm",
                table: "Users");

            migrationBuilder.DropIndex(
                name: "IX_Users_FullNameSearch_Trgm",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "FullNameSearch",
                table: "Users");

            migrationBuilder.AlterDatabase()
                .OldAnnotation("Npgsql:PostgresExtension:pg_trgm", ",,");
        }
    }
}
