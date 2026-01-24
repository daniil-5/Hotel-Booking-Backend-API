using BookingSystem.Application.DTOs.User;
using BookingSystem.Application.Services;
using BookingSystem.Domain.Entities;
using BookingSystem.Domain.Enums;
using BookingSystem.Domain.Interfaces;
using Moq;
using System.Linq.Expressions;
using Xunit;

namespace BookingSystem.Tests.Services
{
    public class UserServiceTests
    {
        private readonly Mock<IUserRepository> _mockUserRepo;
        private readonly UserService _service;

        public UserServiceTests()
        {
            _mockUserRepo = new Mock<IUserRepository>();
            _service = new UserService(_mockUserRepo.Object);
        }

        #region CreateUserAsync Tests

        [Fact]
        public async Task CreateUserAsync_ShouldThrowEmail()
        {
            // Arrange
            var dto = new CreateUserDto { Email = "test@test.com" };
            _mockUserRepo.Setup(r => r.GetByEmailAsync(dto.Email)).ReturnsAsync(new User());

            // Act & Assert
            var ex = await Assert.ThrowsAsync<ApplicationException>(() => _service.CreateUserAsync(dto));
            Assert.Equal("Email is already in use", ex.Message);
        }

        [Fact]
        public async Task CreateUserAsync_ShouldThrowUsername()
        {
            // Arrange
            var dto = new CreateUserDto { Email = "new@test.com", Username = "existingUser" };
            _mockUserRepo.Setup(r => r.GetByEmailAsync(dto.Email)).ReturnsAsync((User)null);
            _mockUserRepo.Setup(r => r.FindAsync(It.IsAny<Expression<Func<User, bool>>>()))
                .ReturnsAsync(new User());

            // Act & Assert
            var ex = await Assert.ThrowsAsync<ApplicationException>(() => _service.CreateUserAsync(dto));
            Assert.Equal("Username is already in use", ex.Message);
        }

        [Fact]
        public async Task CreateUserAsync_ShouldCreate()
        {
            // Arrange
            var dto = new CreateUserDto { Email = "new@test.com", Username = "newuser", Password = "Password123" };
            _mockUserRepo.Setup(r => r.GetByEmailAsync(It.IsAny<string>())).ReturnsAsync((User)null);
            _mockUserRepo.Setup(r => r.FindAsync(It.IsAny<Expression<Func<User, bool>>>())).ReturnsAsync((User)null);

            // Act
            var result = await _service.CreateUserAsync(dto);

            // Assert
            _mockUserRepo.Verify(r => r.AddAsync(It.IsAny<User>()), Times.Once);
            Assert.Equal(dto.Email, result.Email);
        }

        #endregion

        #region UpdateUserAsync Tests

        [Fact]
        public async Task UpdateUserAsync_ShouldThrowNotFoud()
        {
            // Arrange
            _mockUserRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync((User)null);

            // Act & Assert
            var ex = await Assert.ThrowsAsync<ApplicationException>(() => _service.UpdateUserAsync(new UpdateUserDto { Id = 1 }));
            Assert.Equal("User with ID 1 not found", ex.Message);
        }

        [Fact]
        public async Task UpdateUserAsync_ShouldThrowEmailTaken()
        {
            // Arrange
            var user = new User { Id = 1, Email = "old@test.com" };
            var otherUser = new User { Id = 2, Email = "taken@test.com" };
            
            _mockUserRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(user);
            _mockUserRepo.Setup(r => r.GetByEmailAsync("taken@test.com")).ReturnsAsync(otherUser);

            var dto = new UpdateUserDto { Id = 1, Email = "taken@test.com" };

            // Act & Assert
            var ex = await Assert.ThrowsAsync<ApplicationException>(() => _service.UpdateUserAsync(dto));
            Assert.Equal("Email is already in use", ex.Message);
        }
        
        [Fact]
        public async Task UpdateUserAsync_ShouldThrowUsernameTaken()
        {
            // Arrange
            var user = new User { Id = 1, Username = "oldname" };
            var otherUser = new User { Id = 2, Username = "takenname" };
            
            _mockUserRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(user);
            _mockUserRepo.Setup(r => r.GetByEmailAsync("takenname")).ReturnsAsync(otherUser);

            var dto = new UpdateUserDto { Id = 1, Username = "takenname", Email = "valid@test.com" };

            // Act & Assert
            var ex = await Assert.ThrowsAsync<ApplicationException>(() => _service.UpdateUserAsync(dto));
            Assert.Equal("Username is already in use", ex.Message);
        }

        [Fact]
        public async Task UpdateUserAsync_ShouldUpdate()
        {
            // Arrange
            var user = new User { Id = 1, Email = "old@test.com", Username = "old" };
            _mockUserRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(user);
            _mockUserRepo.Setup(r => r.GetByEmailAsync(It.IsAny<string>())).ReturnsAsync((User)null);

            var dto = new UpdateUserDto 
            { 
                Id = 1, 
                Email = "new@test.com", 
                Username = "new", 
                Role = UserRole.Manager 
            };

            // Act
            var result = await _service.UpdateUserAsync(dto);

            // Assert
            Assert.Equal("new@test.com", result.Email);
            Assert.Equal("new", result.Username);
            Assert.Equal("Manager", result.Role);
            _mockUserRepo.Verify(r => r.UpdateAsync(user), Times.Once);
        }

        #endregion

        #region ChangePasswordAsync Tests

        [Fact]
        public async Task ChangePasswordAsync_ShouldThrowNotFound()
        {
            // Arrange
            _mockUserRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync((User)null);

            // Act & Assert
            var ex = await Assert.ThrowsAsync<ApplicationException>(() => 
                _service.ChangePasswordAsync(new ChangePasswordDto { UserId = 1 }));
            Assert.Equal("User with ID 1 not found", ex.Message);
        }

        [Fact]
        public async Task ChangePasswordAsync_ShouldReturnInvalid()
        {
            // Arrange
            var user = new User { Id = 1, PasswordHash = BCrypt.Net.BCrypt.HashPassword("Correct") };
            _mockUserRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(user);

            // Act
            var result = await _service.ChangePasswordAsync(new ChangePasswordDto 
            { 
                UserId = 1, 
                CurrentPassword = "Wrong", 
                NewPassword = "New", 
                ConfirmPassword = "New" 
            });

            // Assert
            Assert.False(result);
        }

        [Fact]
        public async Task ChangePasswordAsync_ShouldThrowDoNotMatch()
        {
            // Arrange
            var user = new User { Id = 1, PasswordHash = BCrypt.Net.BCrypt.HashPassword("Correct") };
            _mockUserRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(user);

            // Act & Assert
            var ex = await Assert.ThrowsAsync<ApplicationException>(() => _service.ChangePasswordAsync(new ChangePasswordDto 
            { 
                UserId = 1, 
                CurrentPassword = "Correct", 
                NewPassword = "New", 
                ConfirmPassword = "Mismatch" 
            }));
            Assert.Equal("New password and confirmation do not match", ex.Message);
        }

        [Fact]
        public async Task ChangePasswordAsync_ShouldUpdate()
        {
            // Arrange
            var user = new User { Id = 1, PasswordHash = BCrypt.Net.BCrypt.HashPassword("Correct") };
            _mockUserRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(user);

            // Act
            var result = await _service.ChangePasswordAsync(new ChangePasswordDto 
            { 
                UserId = 1, 
                CurrentPassword = "Correct", 
                NewPassword = "NewPass", 
                ConfirmPassword = "NewPass" 
            });

            // Assert
            Assert.True(result);
            _mockUserRepo.Verify(r => r.UpdateAsync(user), Times.Once);
        }

        #endregion

        #region SearchUsersAsync Tests

        [Fact]
        public async Task SearchUsersAsync_ShouldReturnResults()
        {
            // Arrange
            var users = new List<User> 
            { 
                new User { Username = "Alice", Email = "alice@test.com" },
                new User { Username = "Bob", Email = "bob@test.com" }
            };

            _mockUserRepo.Setup(r => r.SearchUsersAsync(
                It.IsAny<Expression<Func<User, bool>>>(),
                It.IsAny<Func<IQueryable<User>, IOrderedQueryable<User>>>(),
                It.IsAny<int>(),
                It.IsAny<int>()
            )).ReturnsAsync((users, 2));

            var dto = new UserSearchDto 
            { 
                SearchTerm = "test", 
                SortBy = "username", 
                SortDescending = false,
                PageNumber = 1, 
                PageSize = 10 
            };

            // Act
            var result = await _service.SearchUsersAsync(dto);

            // Assert
            Assert.Equal(2, result.TotalCount);
            Assert.Equal(2, result.Users.Count());
            Assert.Equal(1, result.TotalPages);
        }
        
        [Fact]
        public async Task SearchUsersAsync_ShouldHandleRoleFilter()
        {
            // Arrange
            var dto = new UserSearchDto { Role = UserRole.Admin, PageNumber = 1, PageSize = 10 };
            
            _mockUserRepo.Setup(r => r.SearchUsersAsync(
                It.IsAny<Expression<Func<User, bool>>>(),
                It.IsAny<Func<IQueryable<User>, IOrderedQueryable<User>>>(),
                It.IsAny<int>(),
                It.IsAny<int>()
            )).ReturnsAsync((new List<User>(), 0));

            // Act
            await _service.SearchUsersAsync(dto);
            
            // Assert
            _mockUserRepo.Verify(r => r.SearchUsersAsync(
                It.IsAny<Expression<Func<User, bool>>>(),
                It.IsAny<Func<IQueryable<User>, IOrderedQueryable<User>>>(),
                1, 10), Times.Once);
        }

        #endregion

        #region Get & Delete Tests

        [Fact]
        public async Task GetUserByIdAsync_ShouldReturnDto()
        {
            // Arrange
            var user = new User { Id = 1, Username = "test" };
            _mockUserRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(user);
            
            // Act
            var result = await _service.GetUserByIdAsync(1);
            
            // Assert
            Assert.Equal("test", result.Username);
        }

        [Fact]
        public async Task GetUserByEmailAsync_ShouldReturnDto()
        {
            // Arrange
            var user = new User { Email = "test@test.com" };
            _mockUserRepo.Setup(r => r.GetByEmailAsync("test@test.com")).ReturnsAsync(user);
            
            // Act
            var result = await _service.GetUserByEmailAsync("test@test.com");
            
            // Assert
            Assert.Equal("test@test.com", result.Email);
        }
        
        [Fact]
        public async Task GetAllUsersAsync_ShouldReturnList()
        {
             // Arrange
             _mockUserRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<User> { new User() });
             
             // Act
             var result = await _service.GetAllUsersAsync();
             
             // Assert
             Assert.Single(result);
        }

        [Fact]
        public async Task DeleteUserAsync_ShouldThrowNotFound()
        {
            // Arrange
            _mockUserRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync((User)null);
            
            // Act & Assert
            await Assert.ThrowsAsync<ApplicationException>(() => _service.DeleteUserAsync(1));
        }

        [Fact]
        public async Task DeleteUserAsync_ShouldDelete()
        {
            // Arrange
            _mockUserRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(new User());
            
            // Act
            await _service.DeleteUserAsync(1);
            
            // Assert
            _mockUserRepo.Verify(r => r.DeleteAsync(1), Times.Once);
        }
        
        [Fact]
        public async Task VerifyUserPasswordAsync_ShouldCorrect()
        {
            // Arrange
            var user = new User { PasswordHash = BCrypt.Net.BCrypt.HashPassword("Pass") };
            _mockUserRepo.Setup(r => r.GetByEmailAsync("test")).ReturnsAsync(user);
            
            // Act
            var result = await _service.VerifyUserPasswordAsync("test", "Pass");
            
            // Assert
            Assert.True(result);
        }

        #endregion
    }
}