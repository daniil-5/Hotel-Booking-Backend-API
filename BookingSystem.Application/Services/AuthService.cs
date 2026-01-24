using BookingSystem.Application.DTOs;
using BookingSystem.Application.DTOs.User;
using BookingSystem.Application.Interfaces;
using BookingSystem.Application.Mappers;

namespace BookingSystem.Application.Services;

public class AuthService : IAuthService
{
    private readonly IJwtService _jwtService;
    private readonly IUserService _userService;

    public AuthService(IJwtService jwtService, IUserService userService)
    {
        _jwtService = jwtService;
        _userService = userService;
    }

    public async Task<AuthResponse> Register(RegisterUserDto registerDto)
    {
        var existingUser = await _userService.GetUserByEmailAsync(registerDto.Email);
        if (existingUser != null)
            throw new Exception("User already exists");

        var createUserDto = registerDto.ToCreateDto();
        var userDto = await _userService.CreateUserAsync(createUserDto);

        var userForToken = userDto.ToEntity();

        return new AuthResponse
        {
            Id = userDto.Id,
            Email = userDto.Email,
            Username = userDto.Username,
            Token = _jwtService.GenerateToken(userForToken)
        };
    }

    public async Task<AuthResponse> Login(LoginDto loginDto)
    {
        var isPasswordValid = await _userService.VerifyUserPasswordAsync(loginDto.Email, loginDto.Password);
        if (!isPasswordValid)
            throw new Exception("Invalid credentials");

        var userDto = await _userService.GetUserByEmailAsync(loginDto.Email);
        if (userDto == null)
            throw new Exception("Invalid credentials");

        var userForToken = userDto.ToEntity();

        return new AuthResponse
        {
            Id = userDto.Id,
            Email = userDto.Email,
            Username = userDto.Username,
            Token = _jwtService.GenerateToken(userForToken)
        };
    }

    public async Task<UserDto> GetUserById(int userId)
    {
        return await _userService.GetUserByIdAsync(userId);
    }
}
