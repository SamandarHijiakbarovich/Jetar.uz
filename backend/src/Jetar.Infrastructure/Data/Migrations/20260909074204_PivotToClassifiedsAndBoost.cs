using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Jetar.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class PivotToClassifiedsAndBoost : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ratings_FromUserId",
                table: "ratings");

            migrationBuilder.AlterColumn<Guid>(
                name: "TransactionId",
                table: "ratings",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.CreateTable(
                name: "boost_requests",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ListingId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Days = table.Column<int>(type: "integer", nullable: false),
                    Amount = table.Column<decimal>(type: "numeric(12,0)", precision: 12, scale: 0, nullable: false),
                    ScreenshotUrl = table.Column<string>(type: "character varying(600)", maxLength: 600, nullable: true),
                    Status = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    ReviewNote = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    ReviewedById = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ReviewedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_boost_requests", x => x.Id);
                    table.ForeignKey(
                        name: "FK_boost_requests_listings_ListingId",
                        column: x => x.ListingId,
                        principalTable: "listings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_boost_requests_users_ReviewedById",
                        column: x => x.ReviewedById,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_boost_requests_users_UserId",
                        column: x => x.UserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ratings_FromUserId_ToUserId",
                table: "ratings",
                columns: new[] { "FromUserId", "ToUserId" },
                unique: true,
                filter: "\"TransactionId\" IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_boost_requests_ListingId_Status",
                table: "boost_requests",
                columns: new[] { "ListingId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_boost_requests_ReviewedById",
                table: "boost_requests",
                column: "ReviewedById");

            migrationBuilder.CreateIndex(
                name: "IX_boost_requests_Status",
                table: "boost_requests",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_boost_requests_UserId",
                table: "boost_requests",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "boost_requests");

            migrationBuilder.DropIndex(
                name: "IX_ratings_FromUserId_ToUserId",
                table: "ratings");

            migrationBuilder.AlterColumn<Guid>(
                name: "TransactionId",
                table: "ratings",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_ratings_FromUserId",
                table: "ratings",
                column: "FromUserId");
        }
    }
}
