using BookingSystem.Application.DTOs.Amenity;
using BookingSystem.Application.DTOs.Hotel;
using BookingSystem.Application.Services;
using BookingSystem.Domain.Entities;
using BookingSystem.Domain.Interfaces;
using Microsoft.EntityFrameworkCore.Query;
using Moq;
using System.Linq.Expressions;
using Xunit;

namespace BookingSystem.Tests.Services
{
    public class HotelServiceTests
    {
        private readonly Mock<IHotelRepository> _mockHotelRepo;
        private readonly Mock<IRepository<Amenity>> _mockAmenityRepo;
        private readonly HotelService _service;

        public HotelServiceTests()
        {
            _mockHotelRepo = new Mock<IHotelRepository>();
            _mockAmenityRepo = new Mock<IRepository<Amenity>>();
            _service = new HotelService(_mockHotelRepo.Object, _mockAmenityRepo.Object);
        }

        #region CreateHotelAsync Tests

        [Fact]
        public async Task CreateHotelAsync_ShouldAddHotell()
        {
            // Arrange
            var dto = new CreateHotelDto { Name = "Hotel" };

            // Act
            var result = await _service.CreateHotelAsync(dto);

            // Assert
            Assert.NotNull(result);
            _mockHotelRepo.Verify(r => r.AddAsync(It.Is<Hotel>(h => h.Name == "Hotel")), Times.Once);
        }

        [Fact]
        public async Task CreateHotelAsync_ShouldAddHotel_WithAmenities()
        {
            // Arrange
            var dto = new CreateHotelDto
            {
                Name = "Luxury Hotel",
                Amenities = new List<AmenityDto> { new AmenityDto { Name = "Spa" } }
            };

            // Act
            var result = await _service.CreateHotelAsync(dto);

            // Assert
            _mockHotelRepo.Verify(r => r.AddAsync(It.IsAny<Hotel>()), Times.Once);
        }

        #endregion

        #region UpdateHotelAsync Tests

        [Fact]
        public async Task UpdateHotelAsync_ShouldThrow_HotelNotFound()
        {
            // Arrange
            var dto = new UpdateHotelDto { Id = 1 };
            _mockHotelRepo.Setup(r => r.GetByIdAsync(1))
                .ReturnsAsync((Hotel)null);

            // Act & Assert
            await Assert.ThrowsAsync<KeyNotFoundException>(() => _service.UpdateHotelAsync(dto));
        }

        [Fact]
        public async Task UpdateHotelAsync_ShouldUpdateProperties()
        {
            // Arrange
            var existingHotel = new Hotel { Id = 1, Name = "Old Name" };
            var dto = new UpdateHotelDto
            {
                Id = 1,
                Name = "New Name",
                Description = "Desc",
                Location = "Loc",
                Rating = 5,
                BasePrice = 200
            };

            _mockHotelRepo
                .Setup(r => r.GetByIdAsync(
                    1,
                    It.IsAny<Func<IQueryable<Hotel>, IQueryable<Hotel>>>()))
                .ReturnsAsync(existingHotel);

            // Act
            var result = await _service.UpdateHotelAsync(dto);

            // Assert
            Assert.Equal("New Name", result.Name);
            Assert.Equal(200, result.BasePrice);
            _mockHotelRepo.Verify(r => r.UpdateAsync(existingHotel), Times.Once);
        }

        [Fact]
        public async Task UpdateHotelAsync_ShouldHandleAmenities_NewAndExisting()
        {
            // Arrange
            var existingHotel = new Hotel
            {
                Id = 1,
                Amenities = new List<Amenity> { new Amenity { Name = "OldAmenity" } }
            };

            _mockHotelRepo.Setup(r => r.GetByIdAsync(1,
                    It.IsAny<Func<IQueryable<Hotel>, IQueryable<Hotel>>>()))
                .ReturnsAsync(existingHotel);

            var dbAmenity = new Amenity { Id = 10, Name = "Wifi" };
            _mockAmenityRepo.Setup(r => r.GetAllAsync())
                .ReturnsAsync(new List<Amenity> { dbAmenity });

            var dto = new UpdateHotelDto
            {
                Id = 1,
                Amenities = new List<AmenityDto>
                {
                    new AmenityDto { Name = "Wifi" },
                    new AmenityDto { Name = "Pool", Description = "New Pool" },
                    new AmenityDto { Name = "Wifi" }
                }
            };

            // Act
            await _service.UpdateHotelAsync(dto);

            // Assert
            Assert.Equal(2, existingHotel.Amenities.Count);
            Assert.Contains(existingHotel.Amenities, a => a.Name == "Wifi");
            Assert.Contains(existingHotel.Amenities, a => a.Name == "Pool");

            _mockHotelRepo.Verify(r => r.UpdateAsync(existingHotel), Times.Once);
        }

        #endregion

        #region DeleteHotelAsync Tests

        [Fact]
        public async Task DeleteHotelAsync_ShouldThrow_HotelNotFound()
        {
            // Arrange
            _mockHotelRepo.Setup(r => r.GetByIdAsync(1))
                .ReturnsAsync((Hotel)null);

            // Act & Assert
            await Assert.ThrowsAsync<KeyNotFoundException>(() => _service.DeleteHotelAsync(1));
        }

        [Fact]
        public async Task DeleteHotelAsync_ShouldCallDelete()
        {
            // Arrange
            _mockHotelRepo.Setup(r => r.GetByIdAsync(1))
                .ReturnsAsync(new Hotel { Id = 1 });

            // Act
            await _service.DeleteHotelAsync(1);

            // Assert
            _mockHotelRepo.Verify(r => r.DeleteAsync(1), Times.Once);
        }

        #endregion

        #region Get & GetAll Tests

        [Fact]
        public async Task GetHotelByIdAsync_ShouldReturnDto()
        {
            // Arrange
            var hotel = new Hotel { Id = 1, Name = "Test" };
            _mockHotelRepo.Setup(r => r.GetByIdAsync(1,
                    It.IsAny<Func<IQueryable<Hotel>, IQueryable<Hotel>>>()))
                .ReturnsAsync(hotel);

            // Act
            var result = await _service.GetHotelByIdAsync(1);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("Test", result.Name);
        }

        [Fact]
        public async Task GetHotelByIdAsync_NotFound()
        {
            // Arrange
            _mockHotelRepo.Setup(r => r.GetByIdAsync(1,
                    It.IsAny<Func<IQueryable<Hotel>, IQueryable<Hotel>>>()))
                .ReturnsAsync((Hotel)null);

            // Act
            var result = await _service.GetHotelByIdAsync(1);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task GetAllHotelsAsync_ShouldReturnList()
        {
            // Arrange
            var list = new List<Hotel> { new Hotel(), new Hotel() };
            _mockHotelRepo.Setup(r => r.GetAllAsync(
                It.IsAny<Expression<Func<Hotel, bool>>>(),
                It.IsAny<Func<IQueryable<Hotel>, IQueryable<Hotel>>>()))
                .ReturnsAsync(list);

            // Act
            var result = await _service.GetAllHotelsAsync();

            // Assert
            Assert.Equal(2, result.Count());
        }

        #endregion

        #region SearchHotelsAsync Tests

        [Fact]
        public async Task SearchHotelsAsync_ShouldConstructQueryAndReturnResults()
        {
            // Arrange
            var searchDto = new HotelSearchDto
            {
                Name = "Grand",
                Location = "Paris",
                MinRating = 4,
                MaxRating = 5,
                MinPrice = 100,
                MaxPrice = 500,
                RoomTypeId = 1,
                Amenities = new List<AmenityDto> { new AmenityDto { Name = "Wifi" } },
                SortBy = "price",
                SortDescending = true,
                PageNumber = 1,
                PageSize = 10
            };

            var hotels = new List<Hotel> { new Hotel { Name = "Grand Hotel Paris" } };

            _mockHotelRepo.Setup(r => r.SearchHotelsAsync(
                It.IsAny<Expression<Func<Hotel, bool>>>(),
                It.IsAny<Func<IQueryable<Hotel>, IOrderedQueryable<Hotel>>>(),
                1, 10, true, true, true
            )).ReturnsAsync((hotels, 1));

            // Act
            var result = await _service.SearchHotelsAsync(searchDto);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(1, result.TotalCount);
            Assert.Single(result.Hotels);
        }

        [Fact]
        public async Task SearchHotelsAsync_ShouldHandleSortingOptions()
        {
            // Arrange
            var searchDto = new HotelSearchDto { SortBy = "name" };

            _mockHotelRepo.Setup(r => r.SearchHotelsAsync(
                It.IsAny<Expression<Func<Hotel, bool>>>(),
                It.IsAny<Func<IQueryable<Hotel>, IOrderedQueryable<Hotel>>>(),
                It.IsAny<int>(), It.IsAny<int>(), It.IsAny<bool>(), It.IsAny<bool>(), It.IsAny<bool>()
            )).ReturnsAsync((new List<Hotel>(), 0));

            // Act
            await _service.SearchHotelsAsync(searchDto);

            // Assert
            _mockHotelRepo.Verify(r => r.SearchHotelsAsync(
                It.IsAny<Expression<Func<Hotel, bool>>>(),
                It.IsAny<Func<IQueryable<Hotel>, IOrderedQueryable<Hotel>>>(),
                It.IsAny<int>(), It.IsAny<int>(), It.IsAny<bool>(), It.IsAny<bool>(), It.IsAny<bool>()
            ), Times.Once);
        }

        #endregion
    }
}