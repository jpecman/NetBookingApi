using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BookingApi.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddPitchesDbSet : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(name: "FK_PitchDb_Fields_FieldId", table: "PitchDb");

            migrationBuilder.DropForeignKey(name: "FK_Slots_PitchDb_PitchId", table: "Slots");

            migrationBuilder.DropPrimaryKey(name: "PK_PitchDb", table: "PitchDb");

            migrationBuilder.RenameTable(name: "PitchDb", newName: "Pitches");

            migrationBuilder.RenameIndex(
                name: "IX_PitchDb_FieldId",
                table: "Pitches",
                newName: "IX_Pitches_FieldId"
            );

            migrationBuilder.AddPrimaryKey(name: "PK_Pitches", table: "Pitches", column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Pitches_Fields_FieldId",
                table: "Pitches",
                column: "FieldId",
                principalTable: "Fields",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade
            );

            migrationBuilder.AddForeignKey(
                name: "FK_Slots_Pitches_PitchId",
                table: "Slots",
                column: "PitchId",
                principalTable: "Pitches",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade
            );
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(name: "FK_Pitches_Fields_FieldId", table: "Pitches");

            migrationBuilder.DropForeignKey(name: "FK_Slots_Pitches_PitchId", table: "Slots");

            migrationBuilder.DropPrimaryKey(name: "PK_Pitches", table: "Pitches");

            migrationBuilder.RenameTable(name: "Pitches", newName: "PitchDb");

            migrationBuilder.RenameIndex(
                name: "IX_Pitches_FieldId",
                table: "PitchDb",
                newName: "IX_PitchDb_FieldId"
            );

            migrationBuilder.AddPrimaryKey(name: "PK_PitchDb", table: "PitchDb", column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_PitchDb_Fields_FieldId",
                table: "PitchDb",
                column: "FieldId",
                principalTable: "Fields",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade
            );

            migrationBuilder.AddForeignKey(
                name: "FK_Slots_PitchDb_PitchId",
                table: "Slots",
                column: "PitchId",
                principalTable: "PitchDb",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade
            );
        }
    }
}
