using BookingSystem.Domain.Entities;

namespace BookingSystem.Application.Interfaces;

public interface IJwtService
{
    string GenerateToken(User user);
}
