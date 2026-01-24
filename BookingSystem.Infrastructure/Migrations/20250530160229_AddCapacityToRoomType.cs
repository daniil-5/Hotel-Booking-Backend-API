using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BookingSystem.Migrations
{
    /// <inheritdoc />
    public partial class AddCapacityToRoomType : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "f_k_bookings_rooms_room_id",
                table: "bookings");

            migrationBuilder.DropForeignKey(
                name: "f_k_room_pricings_rooms_room_id",
                table: "room_pricings");

            migrationBuilder.AlterColumn<decimal>(
                name: "base_price",
                table: "room_types",
                type: "numeric(18,2)",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric");

            migrationBuilder.AddColumn<int>(
                name: "capacity",
                table: "room_types",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AlterColumn<int>(
                name: "room_id",
                table: "room_pricings",
                type: "integer",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AlterColumn<decimal>(
                name: "price",
                table: "room_pricings",
                type: "numeric(18,2)",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric");

            migrationBuilder.AddColumn<int>(
                name: "room_type_id",
                table: "room_pricings",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AlterColumn<decimal>(
                name: "total_price",
                table: "bookings",
                type: "numeric(18,2)",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric");

            migrationBuilder.AlterColumn<int>(
                name: "room_id",
                table: "bookings",
                type: "integer",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AddColumn<int>(
                name: "room_type_id",
                table: "bookings",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_room_pricings_room_type_id",
                table: "room_pricings",
                column: "room_type_id");

            migrationBuilder.CreateIndex(
                name: "IX_bookings_room_type_id",
                table: "bookings",
                column: "room_type_id");

            migrationBuilder.AddForeignKey(
                name: "f_k_bookings_room_types_room_type_id",
                table: "bookings",
                column: "room_type_id",
                principalTable: "room_types",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "f_k_bookings_rooms_room_id",
                table: "bookings",
                column: "room_id",
                principalTable: "rooms",
                principalColumn: "id");

            migrationBuilder.AddForeignKey(
                name: "f_k_room_pricings_room_types_room_type_id",
                table: "room_pricings",
                column: "room_type_id",
                principalTable: "room_types",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "f_k_room_pricings_rooms_room_id",
                table: "room_pricings",
                column: "room_id",
                principalTable: "rooms",
                principalColumn: "id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "f_k_bookings_room_types_room_type_id",
                table: "bookings");

            migrationBuilder.DropForeignKey(
                name: "f_k_bookings_rooms_room_id",
                table: "bookings");

            migrationBuilder.DropForeignKey(
                name: "f_k_room_pricings_room_types_room_type_id",
                table: "room_pricings");

            migrationBuilder.DropForeignKey(
                name: "f_k_room_pricings_rooms_room_id",
                table: "room_pricings");

            migrationBuilder.DropIndex(
                name: "IX_room_pricings_room_type_id",
                table: "room_pricings");

            migrationBuilder.DropIndex(
                name: "IX_bookings_room_type_id",
                table: "bookings");

            migrationBuilder.DropColumn(
                name: "capacity",
                table: "room_types");

            migrationBuilder.DropColumn(
                name: "room_type_id",
                table: "room_pricings");

            migrationBuilder.DropColumn(
                name: "room_type_id",
                table: "bookings");

            migrationBuilder.AlterColumn<decimal>(
                name: "base_price",
                table: "room_types",
                type: "numeric",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(18,2)");

            migrationBuilder.AlterColumn<int>(
                name: "room_id",
                table: "room_pricings",
                type: "integer",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "price",
                table: "room_pricings",
                type: "numeric",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(18,2)");

            migrationBuilder.AlterColumn<decimal>(
                name: "total_price",
                table: "bookings",
                type: "numeric",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(18,2)");

            migrationBuilder.AlterColumn<int>(
                name: "room_id",
                table: "bookings",
                type: "integer",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "f_k_bookings_rooms_room_id",
                table: "bookings",
                column: "room_id",
                principalTable: "rooms",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "f_k_room_pricings_rooms_room_id",
                table: "room_pricings",
                column: "room_id",
                principalTable: "rooms",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
