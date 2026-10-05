using BookingSystem.Application.DTOs.User;
using BookingSystem.Application.Exceptions;
using BookingSystem.Application.Interfaces;
using BookingSystem.Domain.Entities;
using BookingSystem.Domain.Enums;
using BookingSystem.Domain.Interfaces;
using Microsoft.AspNetCore.Http;

namespace BookingSystem.Application.Services
{
    public class UserService : IUserService
    {
        private readonly IUserRepository _userRepository;
        private readonly ILoggingService _loggingService;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public UserService(IUserRepository userRepository, ILoggingService loggingService, IHttpContextAccessor httpContextAccessor) // Changed constructor
        {
            _userRepository = userRepository;
            _loggingService = loggingService;
            _httpContextAccessor = httpContextAccessor;
        }

        public async Task<UserDto> CreateUserAsync(CreateUserDto userDto)
        {
            // Check if user with same email or username already exists
            var existingEmail = await _userRepository.GetByEmailAsync(userDto.Email);
            if (existingEmail != null)
            {
                await _loggingService.LogErrorAsync(new ApplicationException("Email is already in use"), null,
                                                    _httpContextAccessor.HttpContext?.Connection.RemoteIpAddress?.ToString(),
                                                    _httpContextAccessor.HttpContext?.Request.Path,
                                                    _httpContextAccessor.HttpContext?.Request.Method);
                throw new DuplicateUserException("Email is already in use");
            }

            var existingUsername = await _userRepository.GetByEmailAsync(userDto.Username);
            if (existingUsername != null)
            {
                await _loggingService.LogErrorAsync(new ApplicationException("Username is already in use"), null,
                                                    _httpContextAccessor.HttpContext?.Connection.RemoteIpAddress?.ToString(),
                                                    _httpContextAccessor.HttpContext?.Request.Path,
                                                    _httpContextAccessor.HttpContext?.Request.Method);
                throw new DuplicateUserException("Username is already in use");
            }

            var user = new User
            {
                Username = userDto.Username,
                Email = userDto.Email,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(userDto.Password),
                FirstName = userDto.FirstName,
                LastName = userDto.LastName,
                PhoneNumber = userDto.PhoneNumber,
                Role = (int)UserRole.Guest
            };

            await _userRepository.AddAsync(user);
            await _loggingService.LogActionAsync(user.Id, UserActionType.UserCreated, $"User {user.Username} created with ID {user.Id}.",
                                                  _httpContextAccessor.HttpContext?.Connection.RemoteIpAddress?.ToString(),
                                                  _httpContextAccessor.HttpContext?.Request.Path,
                                                  _httpContextAccessor.HttpContext?.Request.Method);
            return MapToDto(user);
        }

        public async Task<UserDto> UpdateUserAsync(UpdateUserDto userDto)
        {
            var existingUser = await _userRepository.GetByIdAsync(userDto.Id);
            if (existingUser == null)
            {
                await _loggingService.LogErrorAsync(new ApplicationException($"User with ID {userDto.Id} not found"), userDto.Id,
                                                    _httpContextAccessor.HttpContext?.Connection.RemoteIpAddress?.ToString(),
                                                    _httpContextAccessor.HttpContext?.Request.Path,
                                                    _httpContextAccessor.HttpContext?.Request.Method);
                throw new ApplicationException($"User with ID {userDto.Id} not found");
            }

            // Check if the updated email or username is already in use by another user
            if (userDto.Email != existingUser.Email)
            {
                var existingEmail = await _userRepository.GetByEmailAsync(userDto.Email);
                if (existingEmail != null && existingEmail.Id != userDto.Id)
                {
                    await _loggingService.LogErrorAsync(new ApplicationException("Email is already in use"), userDto.Id,
                                                        _httpContextAccessor.HttpContext?.Connection.RemoteIpAddress?.ToString(),
                                                        _httpContextAccessor.HttpContext?.Request.Path,
                                                        _httpContextAccessor.HttpContext?.Request.Method);
                    throw new DuplicateUserException("Email is already in use");
                }
            }

            if (userDto.Username != existingUser.Username)
            {
                var existingUsername = await _userRepository.GetByEmailAsync(userDto.Username);
                if (existingUsername != null && existingUsername.Id != userDto.Id)
                {
                    await _loggingService.LogErrorAsync(new ApplicationException("Username is already in use"), userDto.Id,
                                                        _httpContextAccessor.HttpContext?.Connection.RemoteIpAddress?.ToString(),
                                                        _httpContextAccessor.HttpContext?.Request.Path,
                                                        _httpContextAccessor.HttpContext?.Request.Method);
                    throw new DuplicateUserException("Username is already in use");
                }
            }

            existingUser.Username = userDto.Username;
            existingUser.Email = userDto.Email;
            existingUser.FirstName = userDto.FirstName;
            existingUser.LastName = userDto.LastName;
            existingUser.PhoneNumber = userDto.PhoneNumber;
            // роль не передана (default 0) — сохраняем текущую, иначе нарушим FK на role
            if (userDto.Role != 0)
            {
                existingUser.Role = (int)userDto.Role;
            }
            existingUser.UpdatedAt = DateTime.UtcNow;

            await _userRepository.UpdateAsync(existingUser);
            await _loggingService.LogActionAsync(existingUser.Id, UserActionType.UserUpdated, $"User {existingUser.Username} with ID {existingUser.Id} updated.",
                                                  _httpContextAccessor.HttpContext?.Connection.RemoteIpAddress?.ToString(),
                                                  _httpContextAccessor.HttpContext?.Request.Path,
                                                  _httpContextAccessor.HttpContext?.Request.Method);
            return MapToDto(existingUser);
        }

        public async Task DeleteUserAsync(int id)
        {
            var existingUser = await _userRepository.GetByIdAsync(id);
            if (existingUser == null)
            {
                await _loggingService.LogErrorAsync(new ApplicationException($"User with ID {id} not found"), id,
                                                    _httpContextAccessor.HttpContext?.Connection.RemoteIpAddress?.ToString(),
                                                    _httpContextAccessor.HttpContext?.Request.Path,
                                                    _httpContextAccessor.HttpContext?.Request.Method);
                throw new ApplicationException($"User with ID {id} not found");
            }

            await _userRepository.DeleteAsync(id);
            await _loggingService.LogActionAsync(id, UserActionType.UserDeleted, $"User with ID {id} deleted.",
                                                  _httpContextAccessor.HttpContext?.Connection.RemoteIpAddress?.ToString(),
                                                  _httpContextAccessor.HttpContext?.Request.Path,
                                                  _httpContextAccessor.HttpContext?.Request.Method);
        }

        public async Task<UserDto> GetUserByIdAsync(int id)
        {
            var user = await _userRepository.GetByIdAsync(id);
            return user == null ? null : MapToDto(user);
        }

        public async Task<UserDto> GetUserByEmailAsync(string email)
        {
            var user = await _userRepository.GetByEmailAsync(email);
            return user == null ? null : MapToDto(user);
        }

        public async Task<UserDto> GetUserByUsernameAsync(string username)
        {
            var user = await _userRepository.GetByEmailAsync(username);
            return user == null ? null : MapToDto(user);
        }

        public async Task<IEnumerable<UserDto>> GetAllUsersAsync()
        {
            var users = await _userRepository.GetAllAsync();
            return users.Select(MapToDto).ToList();
        }


        public async Task<bool> ChangePasswordAsync(ChangePasswordDto changePasswordDto)
        {
            var user = await _userRepository.GetByIdAsync(changePasswordDto.UserId);
            if (user == null)
            {
                await _loggingService.LogErrorAsync(new ApplicationException($"User with ID {changePasswordDto.UserId} not found"), changePasswordDto.UserId,
                                                    _httpContextAccessor.HttpContext?.Connection.RemoteIpAddress?.ToString(),
                                                    _httpContextAccessor.HttpContext?.Request.Path,
                                                    _httpContextAccessor.HttpContext?.Request.Method);
                throw new ApplicationException($"User with ID {changePasswordDto.UserId} not found");
            }

            if (!BCrypt.Net.BCrypt.Verify(changePasswordDto.CurrentPassword, user.PasswordHash))
            {
                await _loggingService.LogActionAsync(user.Id, UserActionType.Unknown, $"Password change failed for user {user.Username}: incorrect current password.",
                                                      _httpContextAccessor.HttpContext?.Connection.RemoteIpAddress?.ToString(),
                                                      _httpContextAccessor.HttpContext?.Request.Path,
                                                      _httpContextAccessor.HttpContext?.Request.Method);
                return false;
            }

            if (changePasswordDto.NewPassword != changePasswordDto.ConfirmPassword)
            {
                await _loggingService.LogErrorAsync(new ApplicationException("New password and confirmation do not match"), user.Id,
                                                    _httpContextAccessor.HttpContext?.Connection.RemoteIpAddress?.ToString(),
                                                    _httpContextAccessor.HttpContext?.Request.Path,
                                                    _httpContextAccessor.HttpContext?.Request.Method);
                throw new ApplicationException("New password and confirmation do not match");
            }

            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(changePasswordDto.NewPassword);
            user.UpdatedAt = DateTime.UtcNow;

            await _userRepository.UpdateAsync(user);
            await _loggingService.LogActionAsync(user.Id, UserActionType.ChangePassword, $"User {user.Username} changed password successfully.",
                                                  _httpContextAccessor.HttpContext?.Connection.RemoteIpAddress?.ToString(),
                                                  _httpContextAccessor.HttpContext?.Request.Path,
                                                  _httpContextAccessor.HttpContext?.Request.Method);
            return true;
        }

        public async Task<bool> VerifyUserPasswordAsync(string email, string password)
        {
            var user = await _userRepository.GetByEmailAsync(email);
            if (user == null) return false;

            return BCrypt.Net.BCrypt.Verify(password, user.PasswordHash);
        }

        public async Task<IEnumerable<UserDto>> GetActiveUsersOrderedByRegistrationDateAsync()
        {
            var users = await _userRepository.GetActiveUsersOrderedByRegistrationDateAsync();
            return users.Select(MapToDto).ToList();
        }

        public async Task<IEnumerable<UserDto>> GetUsersWithNoBookingsAsync()
        {
            var users = await _userRepository.GetUsersWithNoBookingsAsync();
            return users.Select(MapToDto).ToList();
        }

        private static UserDto MapToDto(User user)
        {
            return new UserDto
            {
                Id = user.Id,
                Username = user.Username,
                Email = user.Email,
                FirstName = user.FirstName,
                LastName = user.LastName,
                PhoneNumber = user.PhoneNumber,
                Role = ((UserRole)user.Role).ToString(),
                CreatedAt = user.CreatedAt,
                UpdatedAt = user.UpdatedAt
            };
        }

    }
}
