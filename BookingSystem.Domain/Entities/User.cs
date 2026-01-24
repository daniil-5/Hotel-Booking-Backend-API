using BookingSystem.Domain.Enums;
namespace BookingSystem.Domain.Entities;

public class User : BaseEntity
{
    public string Username { get; set; }
    public string Email { get; set; }
    public string PasswordHash { get; set; }
    public string FirstName { get; set; }
    public string LastName { get; set; }
    public string PhoneNumber { get; set; }
    public int Role { get; set; } = (int)UserRole.Guest;
    public ICollection<Booking> Bookings { get; set; } = new List<Booking>();
}
