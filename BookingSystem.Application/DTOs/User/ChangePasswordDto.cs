namespace BookingSystem.Application.DTOs.User;

public class ChangePasswordDto
{
    public int UserId { get; set; }
    public string CurrentPassword { get; set; }
    public string NewPassword { get; set; }
    public string ConfirmPassword { get; set; }
}