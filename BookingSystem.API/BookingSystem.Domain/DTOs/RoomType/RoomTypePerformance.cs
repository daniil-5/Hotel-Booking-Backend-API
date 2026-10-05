namespace BookingSystem.Domain.DTOs.RoomType
{
    public class RoomTypePerformance
    {
        public required int RoomTypeId { get; set; }
        public required string RoomTypeName { get; set; }
        public required int Capacity { get; set; }
        public required decimal BasePrice { get; set; }
        public required int BookingCount { get; set; }
        public required decimal Revenue { get; set; }
        public required decimal AveragePrice { get; set; }
        public required int ConfirmedCount { get; set; }
        public required int CancelledCount { get; set; }
        public required decimal CancellationRate { get; set; }
    }
}
