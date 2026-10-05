using BookingSystem.Application.Services;
using BookingSystem.Domain.Entities;
using BookingSystem.Domain.Enums;
using Microsoft.Extensions.Configuration;
using System.IdentityModel.Tokens.Jwt;
using Xunit;

namespace BookingSystem.Tests;

/// <summary>
/// Юнит-тесты выпуска JWT-токенов: полезная нагрузка и роли.
/// </summary>
public class JwtServiceTests
{
    private static JwtService MakeService() =>
        new(new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["JWT:Secret"] = "test_secret_key_for_unit_tests_only_32chars",
                ["JWT:Issuer"] = "BookingSystem",
                ["JWT:Audience"] = "BookingSystemUsers",
            })
            .Build());

    private static User TestUser(int id = 4, UserRole role = UserRole.Guest) => new()
    {
        Id = id, Username = "user1", Email = "user1@booking.local", Role = (int)role,
    };

    [Fact]
    public void GenerateToken_ContainsUserIdAndRole()
    {
        var token = MakeService().GenerateToken(TestUser(4, UserRole.Admin));

        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(token);
        var idClaim = jwt.Claims.First(c => c.Type == JwtRegisteredClaimNames.Sub).Value;
        var roleClaim = jwt.Claims.First(c => c.Type.Contains("role")).Value;

        Assert.Equal("4", idClaim);
        Assert.Equal("Admin", roleClaim);
    }

    [Fact]
    public void GenerateToken_IsSignedAndValidFormat()
    {
        var token = MakeService().GenerateToken(TestUser());

        Assert.NotEmpty(token);
        Assert.Equal(3, token.Split('.').Length);
    }
}
