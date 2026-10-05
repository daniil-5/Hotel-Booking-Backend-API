using System.ComponentModel.DataAnnotations.Schema;
using BookingSystem.Domain.Enums;
namespace BookingSystem.Domain.Entities;

[Table("users")]
public class User : BaseEntity
{
    [Column("username")]
    public string Username { get; set; }
    [Column("email")]
    public string Email { get; set; }
    [Column("password_hash")]
    public string PasswordHash { get; set; }
    [Column("first_name")]
    public string FirstName { get; set; }
    [Column("last_name")]
    public string LastName { get; set; }
    [Column("phone_number")]
    public string PhoneNumber { get; set; }
    [Column("role")]
    public int Role { get; set; } = (int)UserRole.Guest;
    public ICollection<Booking> Bookings { get; set; } = new List<Booking>();
}
