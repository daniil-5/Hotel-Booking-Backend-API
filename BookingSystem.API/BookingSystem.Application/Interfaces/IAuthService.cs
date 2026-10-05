using BookingSystem.Application.DTOs;
using BookingSystem.Application.DTOs.User;
using BookingSystem.Domain.Entities;

namespace BookingSystem.Application.Interfaces;

public interface IAuthService
{
    Task<AuthResponse> Register(RegisterUserDto registerDto);
    Task<AuthResponse> Login(LoginDto loginDto);
    Task Logout(int userId);
    Task<UserDto> GetUserById(int userId);
}
