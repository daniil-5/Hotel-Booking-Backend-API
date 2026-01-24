using BookingSystem.Application.DTOs.RoomType;
using BookingSystem.Application.Services;
using BookingSystem.Domain.Entities;
using BookingSystem.Domain.Interfaces;
using Moq;
using System.Linq.Expressions;
using Xunit;

namespace BookingSystem.Tests.Services
{
    public class RoomTypeServiceTests
    {
        private readonly Mock<IRepository<RoomType>> _mockRoomTypeRepo;
        private readonly Mock<IHotelRepository> _mockHotelRepo;
        private readonly Mock<IRepository<Booking>> _mockBookingRepo;
        private readonly RoomTypeService _service;

        public RoomTypeServiceTests()
        {
            _mockRoomTypeRepo = new Mock<IRepository<RoomType>>();
            _mockHotelRepo = new Mock<IHotelRepository>();
            _mockBookingRepo = new Mock<IRepository<Booking>>();
            _service = new RoomTypeService(
                _mockRoomTypeRepo.Object, 
                _mockHotelRepo.Object, 
                _mockBookingRepo.Object
            );
        }

        #region CreateRoomTypeAsync Tests

        [Fact]
        public async Task CreateRoomTypeAsync_ShouldCreate()
        {
            // Arrange
            var dto = new CreateRoomTypeDto { HotelId = 1, Name = "Deluxe" };
            var hotel = new Hotel { Id = 1 };

            _mockHotelRepo.Setup(r => r.GetByIdAsync(1))
                .ReturnsAsync(hotel);

            // Act
            var result = await _service.CreateRoomTypeAsync(dto);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("Deluxe", result.Name);
            _mockRoomTypeRepo.Verify(r => r.AddAsync(It.IsAny<RoomType>()), Times.Once);
        }
        
        [Fact]
        public async Task CreateRoomTypeAsync_ShouldThrowKeyNotFoundException()
        {
            // Arrange
            var dto = new CreateRoomTypeDto { HotelId = 1 };
            
            _mockHotelRepo.Setup(r => r.GetByIdAsync(1))
                .ReturnsAsync((Hotel)null);

            // Act & Assert
            var ex = await Assert.ThrowsAsync<KeyNotFoundException>(() => _service.CreateRoomTypeAsync(dto));
            Assert.Contains("Hotel with ID 1 not found", ex.Message);
        }

        #endregion

        #region UpdateRoomTypeAsync Tests
        
        [Fact]
        public async Task UpdateRoomTypeAsync_ShouldThrowKeyNotFoundException()
        {
            // Arrange
            var dto = new UpdateRoomTypeDto { Id = 1, HotelId = 1 };
            
            _mockHotelRepo.Setup(r => r.GetByIdAsync(1))
                .ReturnsAsync(new Hotel { Id = 1 });
                
            _mockRoomTypeRepo.Setup(r => r.GetByIdAsync(1))
                .ReturnsAsync((RoomType)null);

            // Act & Assert
            var ex = await Assert.ThrowsAsync<KeyNotFoundException>(() => _service.UpdateRoomTypeAsync(dto));
            Assert.Contains("RoomType with ID 1 not found", ex.Message);
        }
        
        [Fact]
        public async Task UpdateRoomTypeAsync_ShouldThrowKeyNotFoundExceptionHotelNotFound()
        {
            // Arrange
            var dto = new UpdateRoomTypeDto { Id = 1, HotelId = 1 };
            
            _mockHotelRepo.Setup(r => r.GetByIdAsync(1))
                .ReturnsAsync((Hotel)null);

            // Act & Assert
            var ex = await Assert.ThrowsAsync<KeyNotFoundException>(() => _service.UpdateRoomTypeAsync(dto));
            Assert.Contains("Hotel with ID 1 not found", ex.Message);
        }

        [Fact]
        public async Task UpdateRoomTypeAsync_ShouldUpdate()
        {
            // Arrange
            var dto = new UpdateRoomTypeDto 
            { 
                Id = 1, 
                HotelId = 1, 
                Name = "Updated Name",
                Capacity = 4
            };
            
            var existingRoomType = new RoomType { Id = 1, Name = "Old Name", Capacity = 2, HotelId = 1 };

            _mockHotelRepo.Setup(r => r.GetByIdAsync(1))
                .ReturnsAsync(new Hotel { Id = 1 });
                
            _mockRoomTypeRepo.Setup(r => r.GetByIdAsync(1))
                .ReturnsAsync(existingRoomType);

            // Act
            var result = await _service.UpdateRoomTypeAsync(dto);

            // Assert
            Assert.Equal("Updated Name", result.Name);
            Assert.Equal(4, result.Capacity);
            
            _mockRoomTypeRepo.Verify(r => r.UpdateAsync(existingRoomType), Times.Once);
        }

        #endregion

        #region DeleteRoomTypeAsync Tests

        [Fact]
        public async Task DeleteRoomTypeAsync_ShouldThrowRoomTypeNotFound()
        {
            // Arrange
            _mockRoomTypeRepo.Setup(r => r.GetByIdAsync(1))
                .ReturnsAsync((RoomType)null);

            // Act & Assert
            var ex = await Assert.ThrowsAsync<KeyNotFoundException>(() => _service.DeleteRoomTypeAsync(1));
            Assert.Contains("RoomType with ID 1 not found", ex.Message);
        }

        [Fact]
        public async Task DeleteRoomTypeAsync_ShouldThrowInvalidOperationException_ActiveBookings()
        {
            // Arrange
            var roomType = new RoomType { Id = 1 };
            
            _mockRoomTypeRepo.Setup(r => r.GetByIdAsync(1))
                .ReturnsAsync(roomType);
            
            _mockBookingRepo.Setup(r => r.CountAsync(It.IsAny<Expression<Func<Booking, bool>>>()))
                .ReturnsAsync(5);

            // Act & Assert
            var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => _service.DeleteRoomTypeAsync(1));
            Assert.Contains("Cannot delete room type that has active bookings", ex.Message);
            
            _mockRoomTypeRepo.Verify(r => r.DeleteAsync(It.IsAny<int>()), Times.Never);
        }

        [Fact]
        public async Task DeleteRoomTypeAsync_ShouldDelete_NoActiveBookings()
        {
            // Arrange
            var roomType = new RoomType { Id = 1 };
            
            _mockRoomTypeRepo.Setup(r => r.GetByIdAsync(1))
                .ReturnsAsync(roomType);
            
            _mockBookingRepo.Setup(r => r.CountAsync(It.IsAny<Expression<Func<Booking, bool>>>()))
                .ReturnsAsync(0);

            // Act
            await _service.DeleteRoomTypeAsync(1);

            // Assert
            _mockRoomTypeRepo.Verify(r => r.DeleteAsync(1), Times.Once);
        }

        #endregion

        #region Get & GetAll Tests

        [Fact]
        public async Task GetRoomTypeByIdAsync_ShouldReturnDto()
        {
            // Arrange
            var roomType = new RoomType { Id = 1, Name = "Standard" };
            _mockRoomTypeRepo.Setup(r => r.GetByIdAsync(1))
                .ReturnsAsync(roomType);

            // Act
            var result = await _service.GetRoomTypeByIdAsync(1);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("Standard", result.Name);
        }

        [Fact]
        public async Task GetRoomTypeByIdAsync_NotFound()
        {
            // Arrange
            _mockRoomTypeRepo.Setup(r => r.GetByIdAsync(1))
                .ReturnsAsync((RoomType)null);

            // Act
            var result = await _service.GetRoomTypeByIdAsync(1);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task GetAllRoomTypesAsync_ShouldReturnList()
        {
            // Arrange
            var list = new List<RoomType> 
            { 
                new RoomType { Id = 1 }, 
                new RoomType { Id = 2 } 
            };
            
            _mockRoomTypeRepo.Setup(r => r.GetAllAsync())
                .ReturnsAsync(list);

            // Act
            var result = await _service.GetAllRoomTypesAsync();

            // Assert
            Assert.Equal(2, result.Count());
        }

        #endregion

        #region GetRoomTypesByHotelIdAsync Tests

        [Fact]
        public async Task GetRoomTypesByHotelIdAsync_ShouldThrowHotelNotFound()
        {
            // Arrange
            _mockHotelRepo.Setup(r => r.GetByIdAsync(1))
                .ReturnsAsync((Hotel)null);

            // Act & Assert
            var ex = await Assert.ThrowsAsync<KeyNotFoundException>(() => _service.GetRoomTypesByHotelIdAsync(1));
            Assert.Contains("Hotel with ID 1 not found", ex.Message);
        }

        [Fact]
        public async Task GetRoomTypesByHotelIdAsync_ShouldReturnList()
        {
            // Arrange
            var hotel = new Hotel { Id = 1 };
            var roomTypes = new List<RoomType>
            {
                new RoomType { Id = 10, HotelId = 1, Name = "A" },
                new RoomType { Id = 11, HotelId = 1, Name = "B" }
            };

            _mockHotelRepo.Setup(r => r.GetByIdAsync(1))
                .ReturnsAsync(hotel);
            
            _mockRoomTypeRepo.Setup(r => r.GetAllAsync(
                It.IsAny<Expression<Func<RoomType, bool>>>()))
                .ReturnsAsync(roomTypes);

            // Act
            var result = await _service.GetRoomTypesByHotelIdAsync(1);

            // Assert
            Assert.Equal(2, result.Count());
            Assert.Contains(result, rt => rt.Name == "A");
            Assert.Contains(result, rt => rt.Name == "B");
        }

        #endregion
    }
}