using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Model.Migrations
{
    /// <inheritdoc />
    public partial class ReplaceBookmarkOfferWithConfiguration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Bookmark_Offer_OfferId",
                table: "Bookmark");

            migrationBuilder.RenameColumn(
                name: "OfferId",
                table: "Bookmark",
                newName: "ConfigurationId");

            migrationBuilder.RenameIndex(
                name: "IX_Bookmark_OfferId",
                table: "Bookmark",
                newName: "IX_Bookmark_ConfigurationId");

            migrationBuilder.AddForeignKey(
                name: "FK_Bookmark_Configuration_ConfigurationId",
                table: "Bookmark",
                column: "ConfigurationId",
                principalTable: "Configuration",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Bookmark_Configuration_ConfigurationId",
                table: "Bookmark");

            migrationBuilder.RenameColumn(
                name: "ConfigurationId",
                table: "Bookmark",
                newName: "OfferId");

            migrationBuilder.RenameIndex(
                name: "IX_Bookmark_ConfigurationId",
                table: "Bookmark",
                newName: "IX_Bookmark_OfferId");

            migrationBuilder.AddForeignKey(
                name: "FK_Bookmark_Offer_OfferId",
                table: "Bookmark",
                column: "OfferId",
                principalTable: "Offer",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
