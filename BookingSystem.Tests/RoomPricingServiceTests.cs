using BookingSystem.Application.DTOs.RoomPricing;
using BookingSystem.Application.Services;
using BookingSystem.Domain.Entities;
using BookingSystem.Domain.Interfaces;
using Moq;
using Xunit;

namespace BookingSystem.Tests.Services
{
    public class RoomPricingServiceTests
    {
        private readonly Mock<IRepository<RoomPricing>> _mockRepo;
        private readonly RoomPricingService _service;

        public RoomPricingServiceTests()
        {
            _mockRepo = new Mock<IRepository<RoomPricing>>();
            _service = new RoomPricingService(_mockRepo.Object);
        }

        #region CreateRoomPricingAsync Tests

        [Fact]
        public async Task CreateRoomPricingAsync_ShouldAddAndReturnDto()
        {
            // Arrange
            var dto = new CreateRoomPricingDto
            {
                RoomTypeId = 1,
                Price = 100.50m,
                Date = DateTime.Today
            };

            // Act
            var result = await _service.CreateRoomPricingAsync(dto);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(100.50m, result.Price);
            Assert.Equal(1, result.RoomTypeId);
        }

        #endregion

        #region UpdateRoomPricingAsync Tests

        [Fact]
        public async Task UpdateRoomPricingAsync_ShouldUpdateEntity()
        {
            // Arrange
            var existingEntity = new RoomPricing
            {
                Id = 1,
                RoomTypeId = 1,
                Price = 50,
                Date = DateTime.Today.AddDays(-1)
            };

            var updateDto = new UpdateRoomPricingDto
            {
                Id = 1,
                RoomTypeId = 2,
                Price = 75.00m,
                Date = DateTime.Today
            };

            _mockRepo.Setup(r => r.GetByIdAsync(1))
                .ReturnsAsync(existingEntity);

            // Act
            var result = await _service.UpdateRoomPricingAsync(updateDto);

            // Assert
            Assert.Equal(75.00m, result.Price);
            Assert.Equal(2, result.RoomTypeId);
            Assert.Equal(75.00m, existingEntity.Price);
            Assert.Equal(2, existingEntity.RoomTypeId);

            _mockRepo.Verify(r => r.UpdateAsync(existingEntity), Times.Once);
        }

        #endregion

        #region DeleteRoomPricingAsync Tests

        [Fact]
        public async Task DeleteRoomPricingAsync_ShouldCallDeleteOnRepository()
        {
            // Arrange
            int idToDelete = 5;

            // Act
            await _service.DeleteRoomPricingAsync(idToDelete);

            // Assert
            _mockRepo.Verify(r => r.DeleteAsync(idToDelete), Times.Once);
        }

        #endregion

        #region GetRoomPricingByIdAsync Tests

        [Fact]
        public async Task GetRoomPricingByIdAsync_ShouldReturnDto_WhenFound()
        {
            // Arrange
            var pricing = new RoomPricing { Id = 1, Price = 120 };
            _mockRepo.Setup(r => r.GetByIdAsync(1))
                .ReturnsAsync(pricing);

            // Act
            var result = await _service.GetRoomPricingByIdAsync(1);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(1, result.Id);
            Assert.Equal(120, result.Price);
        }

        [Fact]
        public async Task GetRoomPricingByIdAsync_ShouldReturnNull_WhenNotFound()
        {
            // Arrange
            _mockRepo.Setup(r => r.GetByIdAsync(1))
                .ReturnsAsync((RoomPricing)null);

            // Act
            var result = await _service.GetRoomPricingByIdAsync(1);

            // Assert
            Assert.Null(result);
        }

        #endregion

        #region GetAllRoomPricingsAsync Tests

        [Fact]
        public async Task GetAllRoomPricingsAsync_ShouldReturnList()
        {
            // Arrange
            var list = new List<RoomPricing>
            {
                new RoomPricing { Id = 1, Price = 50 },
                new RoomPricing { Id = 2, Price = 60 }
            };

            _mockRepo.Setup(r => r.GetAllAsync())
                .ReturnsAsync(list);

            // Act
            var result = await _service.GetAllRoomPricingsAsync();

            // Assert
            Assert.NotNull(result);
            Assert.Equal(2, result.Count());
            Assert.Contains(result, p => p.Price == 50);
            Assert.Contains(result, p => p.Price == 60);
        }

        [Fact]
        public async Task GetAllRoomPricingsAsync_ShouldReturnEmptyList()
        {
            // Arrange
            _mockRepo.Setup(r => r.GetAllAsync())
                .ReturnsAsync(new List<RoomPricing>());

            // Act
            var result = await _service.GetAllRoomPricingsAsync();

            // Assert
            Assert.NotNull(result);
            Assert.Empty(result);
        }

        #endregion
    }
}
