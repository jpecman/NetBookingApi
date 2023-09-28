using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BookingApi.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddTimestampToDbEntities : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<uint>(
                name: "xmin",
                table: "Fields",
                type: "xid",
                rowVersion: true,
                nullable: false,
                defaultValue: 0u);

            migrationBuilder.AddColumn<uint>(
                name: "xmin",
                table: "Contacts",
                type: "xid",
                rowVersion: true,
                nullable: false,
                defaultValue: 0u);

            migrationBuilder.AddColumn<uint>(
                name: "xmin",
                table: "Bookings",
                type: "xid",
                rowVersion: true,
                nullable: false,
                defaultValue: 0u);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "xmin",
                table: "Fields");

            migrationBuilder.DropColumn(
                name: "xmin",
                table: "Contacts");

            migrationBuilder.DropColumn(
                name: "xmin",
                table: "Bookings");
        }
    }
}
