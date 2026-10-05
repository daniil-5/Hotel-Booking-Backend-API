using BookingSystem.Application.DTOs;
using BookingSystem.Application.DTOs.User;
using BookingSystem.Application.Exceptions;
using BookingSystem.Application.Interfaces;
using BookingSystem.Domain.Entities;
using BookingSystem.Domain.Enums;
using Microsoft.AspNetCore.Http; // Added for HttpContextAccessor

namespace BookingSystem.Application.Services;

public class AuthService : IAuthService
{
    private readonly IJwtService _jwtService;
    private readonly IUserService _userService;
    private readonly ILoggingService _loggingService;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public AuthService(IJwtService jwtService, IUserService userService, ILoggingService loggingService, IHttpContextAccessor httpContextAccessor) // Changed constructor
    {
        _jwtService = jwtService;
        _userService = userService;
        _loggingService = loggingService;
        _httpContextAccessor = httpContextAccessor;
    }

    public async Task<AuthResponse> Register(RegisterUserDto registerDto)
    {
        var existingUser = await _userService.GetUserByEmailAsync(registerDto.Email);
        if (existingUser != null)
        {
            await _loggingService.LogActionAsync(null, UserActionType.Unknown, $"Registration failed: User with email {registerDto.Email} already exists.",
                                                  _httpContextAccessor.HttpContext?.Connection.RemoteIpAddress?.ToString(),
                                                  _httpContextAccessor.HttpContext?.Request.Path,
                                                  _httpContextAccessor.HttpContext?.Request.Method);
            throw new DuplicateUserException("User with this email already exists");
        }

        var createUserDto = new CreateUserDto
        {
            Username = registerDto.Username,
            Email = registerDto.Email,
            Password = registerDto.Password,
            FirstName = registerDto.FirstName,
            LastName = registerDto.LastName,
            PhoneNumber = registerDto.PhoneNumber,
            Role = registerDto.Role
        };
        var userDto = await _userService.CreateUserAsync(createUserDto);

        await _loggingService.LogActionAsync(userDto.Id, UserActionType.UserCreated, $"User {userDto.Username} registered successfully.",
                                              _httpContextAccessor.HttpContext?.Connection.RemoteIpAddress?.ToString(),
                                              _httpContextAccessor.HttpContext?.Request.Path,
                                              _httpContextAccessor.HttpContext?.Request.Method);

        var userForToken = new User
        {
            Id = userDto.Id,
            Email = userDto.Email,
            Username = userDto.Username,
            FirstName = userDto.FirstName,
            LastName = userDto.LastName,
            Role = (int)registerDto.Role
        };

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
        {
            await _loggingService.LogActionAsync(null, UserActionType.Unknown, $"Login failed: Invalid credentials for email {loginDto.Email}.",
                                                  _httpContextAccessor.HttpContext?.Connection.RemoteIpAddress?.ToString(),
                                                  _httpContextAccessor.HttpContext?.Request.Path,
                                                  _httpContextAccessor.HttpContext?.Request.Method);
            throw new Exception("Invalid credentials");
        }

        var userDto = await _userService.GetUserByEmailAsync(loginDto.Email);
        if (userDto == null)
        {
            await _loggingService.LogActionAsync(null, UserActionType.Unknown, $"Login failed: User not found after successful password verification for email {loginDto.Email}.",
                                                  _httpContextAccessor.HttpContext?.Connection.RemoteIpAddress?.ToString(),
                                                  _httpContextAccessor.HttpContext?.Request.Path,
                                                  _httpContextAccessor.HttpContext?.Request.Method);
            throw new Exception("Invalid credentials");
        }

        await _loggingService.LogActionAsync(userDto.Id, UserActionType.UserLogin, $"User {userDto.Username} logged in successfully.",
                                              _httpContextAccessor.HttpContext?.Connection.RemoteIpAddress?.ToString(),
                                              _httpContextAccessor.HttpContext?.Request.Path,
                                              _httpContextAccessor.HttpContext?.Request.Method);

        // Create JWT token
        var userForToken = new User
        {
            Id = userDto.Id,
            Email = userDto.Email,
            Username = userDto.Username,
            FirstName = userDto.FirstName,
            LastName = userDto.LastName,
            Role = Enum.Parse<UserRole>(userDto.Role).GetHashCode()
        };

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

    public async Task Logout(int userId)
    {
        await _loggingService.LogActionAsync(userId, UserActionType.UserLogout, $"User {userId} logged out.",
                                              _httpContextAccessor.HttpContext?.Connection.RemoteIpAddress?.ToString(),
                                              _httpContextAccessor.HttpContext?.Request.Path,
                                              _httpContextAccessor.HttpContext?.Request.Method);
    }
}
