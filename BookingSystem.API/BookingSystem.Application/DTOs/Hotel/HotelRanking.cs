namespace BookingSystem.Application.DTOs.Hotel
{
    public class HotelRanking
    {
        public required int HotelId { get; set; }
        public required string HotelName { get; set; }
        public required string Location { get; set; }
        public required double Rating { get; set; }
        public required decimal BasePrice { get; set; }
        public required int BookingCount { get; set; }
        public required decimal TotalRevenue { get; set; }
        public required int RankInLocation { get; set; }
        public required int OverallRevenueRank { get; set; }
        public required decimal MarketShareInLocation { get; set; }
    }
}
