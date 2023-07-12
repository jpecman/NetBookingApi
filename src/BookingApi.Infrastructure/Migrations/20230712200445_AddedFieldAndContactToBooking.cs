using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BookingApi.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddedFieldAndContactToBooking : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "ContactId",
                table: "Bookings",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "FieldId",
                table: "Bookings",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateTable(
                name: "FieldDb",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FieldDb", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Bookings_ContactId",
                table: "Bookings",
                column: "ContactId");

            migrationBuilder.CreateIndex(
                name: "IX_Bookings_FieldId",
                table: "Bookings",
                column: "FieldId");

            migrationBuilder.AddForeignKey(
                name: "FK_Bookings_Contacts_ContactId",
                table: "Bookings",
                column: "ContactId",
                principalTable: "Contacts",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Bookings_FieldDb_FieldId",
                table: "Bookings",
                column: "FieldId",
                principalTable: "FieldDb",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Bookings_Contacts_ContactId",
                table: "Bookings");

            migrationBuilder.DropForeignKey(
                name: "FK_Bookings_FieldDb_FieldId",
                table: "Bookings");

            migrationBuilder.DropTable(
                name: "FieldDb");

            migrationBuilder.DropIndex(
                name: "IX_Bookings_ContactId",
                table: "Bookings");

            migrationBuilder.DropIndex(
                name: "IX_Bookings_FieldId",
                table: "Bookings");

            migrationBuilder.DropColumn(
                name: "ContactId",
                table: "Bookings");

            migrationBuilder.DropColumn(
                name: "FieldId",
                table: "Bookings");
        }
    }
}
