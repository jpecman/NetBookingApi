using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BookingApi.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddBookingType : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Type",
                table: "Bookings",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            // Backfill from distinct start times of active slots (all slots if none are active):
            // 1 start → Single (1), all starts within 7 days → Camp (3), otherwise Repeated (2).
            // Bookings without slots stay Unknown (0). Slot status 3 = Cancelled.
            migrationBuilder.Sql(
                """
                UPDATE "Bookings" b
                SET "Type" = CASE
                    WHEN s.starts = 1 THEN 1
                    WHEN s.span < interval '7 days' THEN 3
                    ELSE 2
                END
                FROM (
                    SELECT sl."BookingId",
                           count(DISTINCT lower(sl."Duration")) AS starts,
                           max(lower(sl."Duration")) - min(lower(sl."Duration")) AS span
                    FROM "Slots" sl
                    WHERE sl."Status" <> 3
                       OR NOT EXISTS (
                           SELECT 1 FROM "Slots" a
                           WHERE a."BookingId" = sl."BookingId" AND a."Status" <> 3
                       )
                    GROUP BY sl."BookingId"
                ) s
                WHERE b."Id" = s."BookingId";
                """
            );
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Type",
                table: "Bookings");
        }
    }
}
