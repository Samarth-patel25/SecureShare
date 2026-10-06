using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SecureShare.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddSharePassword : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsPasswordProtected",
                table: "FileShares",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "PasswordHash",
                table: "FileShares",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_FileShares_FileId",
                table: "FileShares",
                column: "FileId");

            migrationBuilder.AddForeignKey(
                name: "FK_FileShares_Files_FileId",
                table: "FileShares",
                column: "FileId",
                principalTable: "Files",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_FileShares_Files_FileId",
                table: "FileShares");

            migrationBuilder.DropIndex(
                name: "IX_FileShares_FileId",
                table: "FileShares");

            migrationBuilder.DropColumn(
                name: "IsPasswordProtected",
                table: "FileShares");

            migrationBuilder.DropColumn(
                name: "PasswordHash",
                table: "FileShares");
        }
    }
}
