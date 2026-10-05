using BookingSystem.Application.DTOs.Booking;

namespace BookingSystem.Application.DTOs.User;

public class UserBookingHistory
{
    public required int UserId { get; set; }
    public required string Username { get; set; }
    public required string Email { get; set; }
    public required string FullName { get; set; }
    public required int TotalBookings { get; set; }
    public required int ConfirmedBookings { get; set; }
    public required int CancelledBookings { get; set; }
    public required decimal TotalSpent { get; set; }
    public required DateTime MemberSince { get; set; }
    public required IEnumerable<UserRecentBooking> RecentBookings { get; set; }
    public required IEnumerable<LocationStatistic> FavoriteLocations { get; set; }
    public required int CompletedBookings { get; set; }
    public required decimal AverageBookingValue { get; set; }
    public required DateTime? FirstBookingDate { get; set; }
    public required DateTime? LastBookingDate { get; set; }
    public required int UniqueHotelsVisited { get; set; }
}
