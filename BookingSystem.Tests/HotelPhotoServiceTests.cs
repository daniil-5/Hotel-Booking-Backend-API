using BookingSystem.Application.DTOs.HotelPhoto;
using BookingSystem.Application.Services;
using BookingSystem.Domain.Entities;
using BookingSystem.Domain.Interfaces;
using BookingSystem.Domain.Other;
using Microsoft.AspNetCore.Http;
using Moq;
using Xunit;

namespace BookingSystem.Tests.Services
{
    public class HotelPhotoServiceTests
    {
        private readonly Mock<IRepository<HotelPhoto>> _mockRepo;
        private readonly Mock<IPhotoRepository> _mockCloudinaryRepo;
        private readonly HotelPhotoService _service;

        public HotelPhotoServiceTests()
        {
            _mockRepo = new Mock<IRepository<HotelPhoto>>();
            _mockCloudinaryRepo = new Mock<IPhotoRepository>();
            _service = new HotelPhotoService(_mockRepo.Object, _mockCloudinaryRepo.Object);
        }

        #region CreateHotelPhotoAsync Tests

        [Fact]
        public async Task CreateHotelPhotoAsync_ShouldAddEntityAndReturnDto()
        {
            // Arrange
            var dto = new CreateHotelPhotoDto { HotelId = 1, Url = "test.jpg", Description = "Test" };

            // Act
            var result = await _service.CreateHotelPhotoAsync(dto);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("test.jpg", result.Url);
            _mockRepo.Verify(r => r.AddAsync(It.IsAny<HotelPhoto>()), Times.Once);
        }

        #endregion

        #region UploadHotelPhotoAsync Tests

        [Fact]
        public async Task UploadHotelPhotoAsync_ShouldThrow_FileIsNull()
        {
            // Arrange & Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(() =>
                _service.UploadHotelPhotoAsync(null, 1));
        }

        [Fact]
        public async Task UploadHotelPhotoAsync_ShouldThrow_FileIsEmpty()
        {
            // Arrange
            var file = new Mock<IFormFile>();
            file.Setup(f => f.Length).Returns(0);

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(() =>
                _service.UploadHotelPhotoAsync(file.Object, 1));
        }

        [Fact]
        public async Task UploadHotelPhotoAsync_ShouldThrow_HotelIdInvalid()
        {
            // Arrange
            var file = new Mock<IFormFile>();
            file.Setup(f => f.Length).Returns(100);

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(() =>
                _service.UploadHotelPhotoAsync(file.Object, 0));
        }

        [Fact]
        public async Task UploadHotelPhotoAsync_ShouldUploadAndSave()
        {
            // Arrange
            var file = new Mock<IFormFile>();
            file.Setup(f => f.Length).Returns(100);

            var uploadResult = new PhotoUploadResult { Url = "http://cloudinary.com/img.jpg", PublicId = "123" };

            _mockCloudinaryRepo.Setup(c => c.UploadPhotoAsync(file.Object, 1, "desc"))
                .ReturnsAsync(uploadResult);

            // Act
            var result = await _service.UploadHotelPhotoAsync(file.Object, 1, "desc", true);

            // Assert
            Assert.Equal("http://cloudinary.com/img.jpg", result.Url);
            Assert.Equal("123", result.PublicId);
            Assert.True(result.IsMain);

            _mockRepo.Verify(r => r.AddAsync(It.Is<HotelPhoto>(p =>
                p.PublicId == "123" && p.IsMain == true
            )), Times.Once);
        }

        #endregion

        #region UploadMultipleHotelPhotosAsync Tests

        [Fact]
        public async Task UploadMultipleHotelPhotosAsync_ShouldThrow_FilesNullOrEmpty()
        {
            // Arrange & Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(() => _service.UploadMultipleHotelPhotosAsync(null, 1));
            await Assert.ThrowsAsync<ArgumentException>(() => _service.UploadMultipleHotelPhotosAsync(new List<IFormFile>(), 1));
        }

        [Fact]
        public async Task UploadMultipleHotelPhotosAsync_ShouldThrow_HotelIdInvalid()
        {
            // Arrange
            var files = new List<IFormFile> { new Mock<IFormFile>().Object };

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(() => _service.UploadMultipleHotelPhotosAsync(files, -1));
        }

        [Fact]
        public async Task UploadMultipleHotelPhotosAsync_ShouldUploadAndSaveAll()
        {
            // Arrange
            var files = new List<IFormFile> { new Mock<IFormFile>().Object, new Mock<IFormFile>().Object };
            var uploadResults = new List<PhotoUploadResult>
            {
                new PhotoUploadResult { Url = "url1", PublicId = "p1" },
                new PhotoUploadResult { Url = "url2", PublicId = "p2" }
            };

            _mockCloudinaryRepo.Setup(c => c.UploadPhotosAsync(files, 1))
                .ReturnsAsync(uploadResults);

            // Act
            var result = await _service.UploadMultipleHotelPhotosAsync(files, 1);

            // Assert
            Assert.Equal(2, result.Count());
            _mockRepo.Verify(r => r.AddAsync(It.IsAny<HotelPhoto>()), Times.Exactly(2));
        }

        #endregion

        #region UpdateHotelPhotoAsync Tests

        [Fact]
        public async Task UpdateHotelPhotoAsync_ShouldThrowKeyNotFound()
        {
            // Arrange
            _mockRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync((HotelPhoto)null);

            // Act & Assert
            await Assert.ThrowsAsync<KeyNotFoundException>(() =>
                _service.UpdateHotelPhotoAsync(new UpdateHotelPhotoDto { Id = 1 }));
        }

        [Fact]
        public async Task UpdateHotelPhotoAsync_ShouldUpdate()
        {
            // Arrange
            var existing = new HotelPhoto { Id = 1, Url = "old" };
            _mockRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(existing);

            var dto = new UpdateHotelPhotoDto { Id = 1, Url = "new", Description = "Updated" };

            // Act
            var result = await _service.UpdateHotelPhotoAsync(dto);

            // Assert
            Assert.Equal("new", result.Url);
            Assert.Equal("Updated", result.Description);
            _mockRepo.Verify(r => r.UpdateAsync(existing), Times.Once);
        }

        #endregion

        #region DeleteHotelPhotoAsync Tests

        [Fact]
        public async Task DeleteHotelPhotoAsync_ShouldThrow_PhotoDoesNotExist()
        {
            // Arrange
            _mockRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync((HotelPhoto)null);

            // Act & Assert
            await Assert.ThrowsAsync<KeyNotFoundException>(() => _service.DeleteHotelPhotoAsync(1));
        }

        [Fact]
        public async Task DeleteHotelPhotoAsync_ShouldThrow_DeleteFails()
        {
            // Arrange
            var photo = new HotelPhoto { Id = 1, PublicId = "123" };
            _mockRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(photo);

            _mockCloudinaryRepo.Setup(c => c.DeletePhotoAsync("123")).ReturnsAsync(false);

            // Act & Assert
            var ex = await Assert.ThrowsAsync<Exception>(() => _service.DeleteHotelPhotoAsync(1));
            Assert.Contains("Failed to delete photo", ex.Message);
        }

        [Fact]
        public async Task DeleteHotelPhotoAsync_ShouldDelete()
        {
            // Arrange
            var photo = new HotelPhoto { Id = 1, PublicId = "123" };
            _mockRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(photo);
            _mockCloudinaryRepo.Setup(c => c.DeletePhotoAsync("123")).ReturnsAsync(true); // Success

            // Act
            await _service.DeleteHotelPhotoAsync(1);

            // Assert
            _mockCloudinaryRepo.Verify(c => c.DeletePhotoAsync("123"), Times.Once);
            _mockRepo.Verify(r => r.DeleteAsync(1), Times.Once);
        }

        [Fact]
        public async Task DeleteHotelPhotoAsync_ShouldSkipCloudinary_NoPublicId()
        {
            // Arrange
            var photo = new HotelPhoto { Id = 1, PublicId = null };
            _mockRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(photo);

            // Act
            await _service.DeleteHotelPhotoAsync(1);

            // Assert
            _mockCloudinaryRepo.Verify(c => c.DeletePhotoAsync(It.IsAny<string>()), Times.Never);
            _mockRepo.Verify(r => r.DeleteAsync(1), Times.Once);
        }

        #endregion

        #region SetMainPhotoAsync Tests

        [Fact]
        public async Task SetMainPhotoAsync_ShouldThrow_PhotoNotFoundInHotel()
        {
            // Arrange
            var photos = new List<HotelPhoto>
            {
                new HotelPhoto { Id = 2, HotelId = 1 }
            };
            _mockRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(photos);

            // Act & Assert
            await Assert.ThrowsAsync<KeyNotFoundException>(() => _service.SetMainPhotoAsync(1, 1));
        }

        [Fact]
        public async Task SetMainPhotoAsync_ShouldUpdateFlags()
        {
            // Arrange
            var photo1 = new HotelPhoto { Id = 1, HotelId = 1, IsMain = true };
            var photo2 = new HotelPhoto { Id = 2, HotelId = 1, IsMain = false };
            var photos = new List<HotelPhoto> { photo1, photo2 };

            _mockRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(photos);

            // Act
            var result = await _service.SetMainPhotoAsync(2, 1);

            // Assert
            Assert.True(result.IsMain);
            Assert.False(photo1.IsMain);

            _mockRepo.Verify(r => r.UpdateAsync(It.IsAny<HotelPhoto>()), Times.Exactly(2));
        }

        #endregion

        #region Get & GetAll Tests

        [Fact]
        public async Task GetHotelPhotoByIdAsync_ShouldReturnDto()
        {
            // Arrange
            _mockRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(new HotelPhoto { Id = 1 });

            // Act
            var result = await _service.GetHotelPhotoByIdAsync(1);

            // Assert
            Assert.NotNull(result);
        }

        [Fact]
        public async Task GetAllHotelPhotosAsync_ShouldReturnList()
        {
            // Arrange
            _mockRepo.Setup(r => r.GetAllAsync())
                .ReturnsAsync(new List<HotelPhoto> { new HotelPhoto() });

            // Act
            var result = await _service.GetAllHotelPhotosAsync();

            // Assert
            Assert.Single(result);
        }

        [Fact]
        public async Task GetPhotosByHotelIdAsync_ShouldFilterByHotelId()
        {
            // Arrange
            var photos = new List<HotelPhoto>
            {
                new HotelPhoto { HotelId = 1 },
                new HotelPhoto { HotelId = 2 },
                new HotelPhoto { HotelId = 1, IsDeleted = true }
            };
            _mockRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(photos);

            // Act
            var result = await _service.GetPhotosByHotelIdAsync(1);

            // Assert
            Assert.Single(result);
        }

        [Fact]
        public async Task GetTransformedImageAsync_ShouldThrow_WhenNotFound()
        {
            // Arrange
            _mockRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync((HotelPhoto)null);

            // Act & Assert
            await Assert.ThrowsAsync<KeyNotFoundException>(() =>
                _service.GetTransformedImageUrlAsync(1, "trans"));
        }

        #endregion
    }
}
