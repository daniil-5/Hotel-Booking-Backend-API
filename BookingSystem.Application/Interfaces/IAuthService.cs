using BookingSystem.Application.DTOs.User;

namespace BookingSystem.Application.Interfaces;

public interface IAuthService
{
    Task<AuthResponse> Register(RegisterUserDto registerDto);
    Task<AuthResponse> Login(LoginDto loginDto);
    Task<UserDto> GetUserById(int userId);
}