using BookingSystem.Application.DTOs.User;
using BookingSystem.Domain.Entities;
using BookingSystem.Domain.Enums;

namespace BookingSystem.Application.Mappers
{
    public static class UserMapping
    {
        public static UserDto ToDto(this User user)
        {
            if (user == null) return null;

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
        public static User ToEntity(this CreateUserDto dto)
        {
            if (dto == null) return null;

            return new User
            {
                Username = dto.Username,
                Email = dto.Email,
                FirstName = dto.FirstName,
                LastName = dto.LastName,
                PhoneNumber = dto.PhoneNumber,
                Role = (int)UserRole.Guest,
                PasswordHash =  BCrypt.Net.BCrypt.HashPassword(dto.Password),
            };
        }
        
        public static User ToEntity(this UserDto dto)
        {
            if (dto == null) return null;

            return new User
            {
                Id = dto.Id,
                Email = dto.Email,
                Username = dto.Username,
                FirstName = dto.FirstName,
                LastName = dto.LastName,
                Role = Enum.Parse<UserRole>(dto.Role).GetHashCode()
            };
        }

        public static CreateUserDto ToCreateDto(this RegisterUserDto registerUserDto)
        {
            if (registerUserDto == null) return null;

            return new CreateUserDto
            {
                Username = registerUserDto.Username,
                Email = registerUserDto.Email,
                FirstName = registerUserDto.FirstName,
                LastName = registerUserDto.LastName,
                PhoneNumber = registerUserDto.PhoneNumber,
                Password = registerUserDto.Password,
                Role = UserRole.Guest
            };
        }
    }
}