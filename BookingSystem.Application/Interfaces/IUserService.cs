using BookingSystem.Application.DTOs.User;

namespace BookingSystem.Application.Interfaces;

public interface IUserService
{
    Task<UserDto> CreateUserAsync(CreateUserDto userDto);
    Task<UserDto> UpdateUserAsync(UpdateUserDto userDto);
    Task DeleteUserAsync(int id);
    Task<UserDto> GetUserByIdAsync(int id);
    Task<UserDto> GetUserByEmailAsync(string email);
    Task<UserDto> GetUserByUsernameAsync(string username);
    Task<IEnumerable<UserDto>> GetAllUsersAsync();
    Task<UserSearchResultDto> SearchUsersAsync(UserSearchDto searchDto);
    Task<bool> ChangePasswordAsync(ChangePasswordDto changePasswordDto);

    Task<bool> VerifyUserPasswordAsync(string email, string password);
}