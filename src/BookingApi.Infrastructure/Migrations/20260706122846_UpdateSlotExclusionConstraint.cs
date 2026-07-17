using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BookingApi.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class UpdateSlotExclusionConstraint : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                "ALTER TABLE \"Slots\" DROP CONSTRAINT IF EXISTS EnsureUniqueSlot;"
            );
            migrationBuilder.Sql(
                // Status = 3 is SlotStatus.Cancelled — see SlotStatus.cs for why that value must not change
                "ALTER TABLE \"Slots\" ADD CONSTRAINT EnsureUniqueSlot EXCLUDE USING gist (\"PitchId\" WITH =, \"Duration\" WITH &&) WHERE (\"Status\" != 3);"
            );
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                "ALTER TABLE \"Slots\" DROP CONSTRAINT IF EXISTS EnsureUniqueSlot;"
            );
            migrationBuilder.Sql(
                "ALTER TABLE \"Slots\" ADD CONSTRAINT EnsureUniqueSlot EXCLUDE USING gist (\"PitchId\" WITH =, \"Duration\" WITH &&);"
            );
        }
    }
}
