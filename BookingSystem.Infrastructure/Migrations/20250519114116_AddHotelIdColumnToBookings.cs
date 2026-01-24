using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BookingSystem.Migrations
{
    /// <inheritdoc />
    public partial class AddHotelIdColumnToBookings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // 1. Добавляем колонку без внешнего ключа
            migrationBuilder.AddColumn<int>(
                name: "hotel_id",
                table: "bookings",
                nullable: false,
                defaultValue: 0);

            // 2. Обновляем существующие данные
            // Вариант 1: Привязываем к отелю с ID = 1 (убедитесь, что такой отель существует)
            migrationBuilder.Sql(@"
            UPDATE bookings 
            SET hotel_id = 1
            WHERE hotel_id = 0;
        ");

            // 3. Создаем индекс
            migrationBuilder.CreateIndex(
                name: "ix_bookings_hotel_id",
                table: "bookings",
                column: "hotel_id");

            // 4. Добавляем внешний ключ ПОСЛЕ обновления данных
            migrationBuilder.AddForeignKey(
                name: "fk_bookings_hotels_hotel_id",
                table: "bookings",
                column: "hotel_id",
                principalTable: "hotels",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_bookings_hotels_hotel_id",
                table: "bookings");

            migrationBuilder.DropIndex(
                name: "ix_bookings_hotel_id",
                table: "bookings");

            migrationBuilder.DropColumn(
                name: "hotel_id",
                table: "bookings");
        }
    }
}
