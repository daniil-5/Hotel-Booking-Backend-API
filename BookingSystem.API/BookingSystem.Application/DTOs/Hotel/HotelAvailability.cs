namespace BookingSystem.Application.DTOs.Hotel
{
    public class HotelAvailability
    {
        public required int HotelId { get; set; }
        public required string HotelName { get; set; }
        public required string Location { get; set; }
        public required double Rating { get; set; }
        public required string Description { get; set; }
        public required int RoomTypeId { get; set; }
        public required string RoomTypeName { get; set; }
        public required int Capacity { get; set; }
        public required decimal Price { get; set; }
        public required int Area { get; set; }
        public required int PhotoCount { get; set; }
        public required int BookedCount { get; set; }
    }
}
