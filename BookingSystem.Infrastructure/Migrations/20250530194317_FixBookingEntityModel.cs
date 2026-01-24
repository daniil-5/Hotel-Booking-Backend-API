using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BookingSystem.Migrations
{
    /// <inheritdoc />
    public partial class FixBookingEntityModel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "room_id",
                table: "bookings",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "ix_bookings_room_id",
                table: "bookings",
                column: "room_id");

            migrationBuilder.AddForeignKey(
                name: "f_k_bookings_rooms_room_id",
                table: "bookings",
                column: "room_id",
                principalTable: "rooms",
                principalColumn: "id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "f_k_bookings_rooms_room_id",
                table: "bookings");

            migrationBuilder.DropIndex(
                name: "ix_bookings_room_id",
                table: "bookings");

            migrationBuilder.DropColumn(
                name: "room_id",
                table: "bookings");
        }
    }
}
