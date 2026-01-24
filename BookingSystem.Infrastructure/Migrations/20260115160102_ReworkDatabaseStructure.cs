using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace BookingSystem.Migrations
{
    /// <inheritdoc />
    public partial class ReworkDatabaseStructure : Migration
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

            migrationBuilder.DropTable(
                name: "rooms");

            migrationBuilder.DropIndex(
                name: "IX_room_pricings_room_id",
                table: "room_pricings");

            migrationBuilder.DropIndex(
                name: "ix_bookings_room_id",
                table: "bookings");

            migrationBuilder.DropColumn(
                name: "room_id",
                table: "room_pricings");

            migrationBuilder.DropColumn(
                name: "amenities",
                table: "hotels");

            migrationBuilder.DropColumn(
                name: "room_id",
                table: "bookings");

            migrationBuilder.AlterColumn<decimal>(
                name: "area",
                table: "room_types",
                type: "numeric(18,2)",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric");

            migrationBuilder.AddColumn<int>(
                name: "count",
                table: "room_types",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AlterColumn<decimal>(
                name: "rating",
                table: "hotels",
                type: "numeric(2,1)",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric");

            migrationBuilder.CreateTable(
                name: "amenities",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    name = table.Column<string>(type: "text", nullable: false),
                    description = table.Column<string>(type: "text", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("p_k_amenities", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "hotel_amenities",
                columns: table => new
                {
                    amenities_id = table.Column<int>(type: "integer", nullable: false),
                    hotels_id = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("p_k_hotel_amenities", x => new { x.amenities_id, x.hotels_id });
                    table.ForeignKey(
                        name: "f_k_hotel_amenities_amenities_amenities_id",
                        column: x => x.amenities_id,
                        principalTable: "amenities",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "f_k_hotel_amenities_hotels_hotels_id",
                        column: x => x.hotels_id,
                        principalTable: "hotels",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_hotel_amenities_hotels_id",
                table: "hotel_amenities",
                column: "hotels_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "hotel_amenities");

            migrationBuilder.DropTable(
                name: "amenities");

            migrationBuilder.DropColumn(
                name: "count",
                table: "room_types");

            migrationBuilder.AlterColumn<decimal>(
                name: "area",
                table: "room_types",
                type: "numeric",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(18,2)");

            migrationBuilder.AddColumn<int>(
                name: "room_id",
                table: "room_pricings",
                type: "integer",
                nullable: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "rating",
                table: "hotels",
                type: "numeric",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(2,1)");

            migrationBuilder.AddColumn<string>(
                name: "amenities",
                table: "hotels",
                type: "jsonb",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "room_id",
                table: "bookings",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "rooms",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    room_type_id = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    is_available = table.Column<bool>(type: "boolean", nullable: false),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false),
                    room_number = table.Column<string>(type: "text", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("p_k_rooms", x => x.id);
                    table.ForeignKey(
                        name: "f_k_rooms_room_types_room_type_id",
                        column: x => x.room_type_id,
                        principalTable: "room_types",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_room_pricings_room_id",
                table: "room_pricings",
                column: "room_id");

            migrationBuilder.CreateIndex(
                name: "ix_bookings_room_id",
                table: "bookings",
                column: "room_id");

            migrationBuilder.CreateIndex(
                name: "IX_rooms_room_type_id",
                table: "rooms",
                column: "room_type_id");

            migrationBuilder.AddForeignKey(
                name: "f_k_bookings_rooms_room_id",
                table: "bookings",
                column: "room_id",
                principalTable: "rooms",
                principalColumn: "id");

            migrationBuilder.AddForeignKey(
                name: "f_k_room_pricings_rooms_room_id",
                table: "room_pricings",
                column: "room_id",
                principalTable: "rooms",
                principalColumn: "id");
        }
    }
}
