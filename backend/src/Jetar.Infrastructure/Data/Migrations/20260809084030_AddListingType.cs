using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Jetar.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddListingType : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Type",
                table: "listings",
                type: "character varying(16)",
                maxLength: 16,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_listings_Type_Status",
                table: "listings",
                columns: new[] { "Type", "Status" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_listings_Type_Status",
                table: "listings");

            migrationBuilder.DropColumn(
                name: "Type",
                table: "listings");
        }
    }
}
