using BookingSystem.Application.DTOs.Amenity;
using BookingSystem.Application.Services;
using BookingSystem.Domain.Entities;
using BookingSystem.Domain.Interfaces;
using Moq;
using System.Linq.Expressions;
using Xunit;

namespace BookingSystem.Tests.Services
{
    public class AmenityServiceTests
    {
        private readonly Mock<IRepository<Amenity>> _mockAmenityRepo;
        private readonly AmenityService _service;

        public AmenityServiceTests()
        {
            _mockAmenityRepo = new Mock<IRepository<Amenity>>();
            _service = new AmenityService(_mockAmenityRepo.Object);
        }

        #region CreateAmenityAsync Tests

        
        [Fact]
        public async Task CreateAmenityAsync_ShouldAddAmenity()
        {
            // Arrange
            var createDto = new CreateAmenityDto { Name = "Pool", Description = "Outdoor pool" };
            
            _mockAmenityRepo.Setup(repo => repo.GetAllAsync(
                    It.IsAny<Expression<Func<Amenity, bool>>>(), 
                    null))
                .ReturnsAsync(new List<Amenity>());

            // Act
            var result = await _service.CreateAmenityAsync(createDto);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(createDto.Name, result.Name);
            Assert.Equal(createDto.Description, result.Description);
        }
        
        [Fact]
        public async Task CreateAmenityAsync_ShouldThrowInvalidOperationException()
        {
            // Arrange
            var createDto = new CreateAmenityDto { Name = "WiFi", Description = "Free internet" };
            var existingAmenities = new List<Amenity> { new Amenity { Id = 1, Name = "WiFi" } };
            
            _mockAmenityRepo.Setup(repo => repo.GetAllAsync(
                It.IsAny<Expression<Func<Amenity, bool>>>()))
                .ReturnsAsync(existingAmenities);

            // Act & Assert
            var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => 
                _service.CreateAmenityAsync(createDto));
            
            Assert.Equal("Amenity 'WiFi' already exists.", exception.Message);
            _mockAmenityRepo.Verify(repo => repo.AddAsync(It.IsAny<Amenity>()), Times.Never);
        }

        #endregion

        #region UpdateAmenityAsync Tests
        
        [Fact]
        public async Task UpdateAmenityAsync_ShouldUpdate()
        {
            // Arrange
            var updateDto = new UpdateAmenityDto { Id = 1, Name = "Updated Gym", Description = "New Machines" };
            var existingAmenity = new Amenity { Id = 1, Name = "Old Gym", Description = "Old Machines" };

            _mockAmenityRepo.Setup(repo => repo.GetByIdAsync(1))
                .ReturnsAsync(existingAmenity);

            // Act
            var result = await _service.UpdateAmenityAsync(updateDto);

            // Assert
            Assert.Equal("Updated Gym", result.Name);
            Assert.Equal("New Machines", result.Description);
            Assert.Equal(1, result.Id);
            
            Assert.Equal("Updated Gym", existingAmenity.Name);
            
            _mockAmenityRepo.Verify(repo => repo.UpdateAsync(existingAmenity), Times.Once);
        }
        
        [Fact]
        public async Task UpdateAmenityAsync_ShouldThrowKeyNotFoundException()
        {
            // Arrange
            var updateDto = new UpdateAmenityDto { Id = 1, Name = "Gym", Description = "24/7" };

            _mockAmenityRepo.Setup(repo => repo.GetByIdAsync(1))
                .ReturnsAsync((Amenity)null);

            // Act & Assert
            await Assert.ThrowsAsync<KeyNotFoundException>(() => _service.UpdateAmenityAsync(updateDto));
            
            _mockAmenityRepo.Verify(repo => repo.UpdateAsync(It.IsAny<Amenity>()), Times.Never);
        }

        #endregion

        #region DeleteAmenityAsync Tests

        [Fact]
        public async Task DeleteAmenityAsync_ShouldThrowKeyNotFoundException()
        {
            // Arrange
            _mockAmenityRepo.Setup(repo => repo.GetByIdAsync(1))
                .ReturnsAsync((Amenity)null);

            // Act & Assert
            await Assert.ThrowsAsync<KeyNotFoundException>(() => _service.DeleteAmenityAsync(1));
            
            _mockAmenityRepo.Verify(repo => repo.DeleteAsync(It.IsAny<int>()), Times.Never);
        }

        [Fact]
        public async Task DeleteAmenityAsync_ShouldDelete()
        {
            // Arrange
            var amenity = new Amenity { Id = 1 };
            _mockAmenityRepo.Setup(repo => repo.GetByIdAsync(1))
                .ReturnsAsync(amenity);

            // Act
            await _service.DeleteAmenityAsync(1);

            // Assert
            _mockAmenityRepo.Verify(repo => repo.DeleteAsync(1), Times.Once);
        }

        #endregion

        #region GetAmenityByIdAsync Tests

        [Fact]
        public async Task GetAmenityByIdAsync_ShouldReturnNull()
        {
            // Arrange
            _mockAmenityRepo.Setup(repo => repo.GetByIdAsync(1))
                .ReturnsAsync((Amenity)null);

            // Act
            var result = await _service.GetAmenityByIdAsync(1);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task GetAmenityByIdAsync_ShouldReturnDto()
        {
            // Arrange
            var amenity = new Amenity { Id = 1, Name = "Spa" };
            _mockAmenityRepo.Setup(repo => repo.GetByIdAsync(1))
                .ReturnsAsync(amenity);

            // Act
            var result = await _service.GetAmenityByIdAsync(1);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(1, result.Id);
            Assert.Equal("Spa", result.Name);
        }

        #endregion

        #region GetAllAmenitiesAsync Tests

        [Fact]
        public async Task GetAllAmenitiesAsync_ShouldReturnListOfDtos()
        {
            // Arrange
            var amenities = new List<Amenity>
            {
                new Amenity { Id = 1, Name = "A1" },
                new Amenity { Id = 2, Name = "A2" }
            };

            _mockAmenityRepo.Setup(repo => repo.GetAllAsync())
                .ReturnsAsync(amenities);

            // Act
            var result = await _service.GetAllAmenitiesAsync();

            // Assert
            Assert.NotNull(result);
            Assert.Equal(2, result.Count());
            Assert.Contains(result, r => r.Name == "A1");
            Assert.Contains(result, r => r.Name == "A2");
        }

        [Fact]
        public async Task GetAllAmenitiesAsync_ShouldReturnEmptyList()
        {
            // Arrange
            _mockAmenityRepo.Setup(repo => repo.GetAllAsync())
                .ReturnsAsync(new List<Amenity>());

            // Act
            var result = await _service.GetAllAmenitiesAsync();

            // Assert
            Assert.NotNull(result);
            Assert.Empty(result);
        }

        #endregion
    }
}