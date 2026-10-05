using BookingSystem.Domain.Enums;

namespace BookingSystem.Application.DTOs.User;

public class UserSearchDto
{
    public string SearchTerm { get; set; } = string.Empty;
    public UserRole? Role { get; set; }
    public string SortBy { get; set; } = "CreatedAt";
    public bool SortDescending { get; set; } = true;
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 10;
}
