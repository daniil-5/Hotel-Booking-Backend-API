using BookingSystem.Application.DTOs.Booking;
using BookingSystem.Application.DTOs.Commands;
using BookingSystem.Application.Interfaces;
using BookingSystem.Application.Services;
using BookingSystem.Application.Settings;
using BookingSystem.Domain.Entities;
using BookingSystem.Domain.Enums;
using BookingSystem.Domain.Interfaces;
using Microsoft.EntityFrameworkCore.Query;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using System.Linq.Expressions;
using Xunit;

namespace BookingSystem.Tests.Services
{
    public class BookingServiceTests
    {
        private readonly Mock<IRepository<Booking>> _mockBookingRepo;
        private readonly Mock<IRepository<RoomType>> _mockRoomTypeRepo;
        private readonly Mock<IUserRepository> _mockUserRepo;
        private readonly Mock<IHotelRepository> _mockHotelRepo;
        private readonly Mock<IRepository<RoomPricing>> _mockPricingRepo;
        private readonly Mock<IKafkaProducer> _mockKafkaProducer;
        private readonly Mock<ILogger<BookingService>> _mockLogger;
        private readonly BookingService _service;

        public BookingServiceTests()
        {
            _mockBookingRepo = new Mock<IRepository<Booking>>();
            _mockRoomTypeRepo = new Mock<IRepository<RoomType>>();
            _mockUserRepo = new Mock<IUserRepository>();
            _mockHotelRepo = new Mock<IHotelRepository>();
            _mockPricingRepo = new Mock<IRepository<RoomPricing>>();
            _mockKafkaProducer = new Mock<IKafkaProducer>();
            _mockLogger = new Mock<ILogger<BookingService>>();

            var kafkaSettings = Options.Create(new KafkaSettings
            {
                Topics = new KafkaTopics { BookingRequests = "bookings-topic" }
            });

            _service = new BookingService(
                _mockBookingRepo.Object,
                _mockRoomTypeRepo.Object,
                _mockUserRepo.Object,
                _mockHotelRepo.Object,
                _mockPricingRepo.Object,
                _mockKafkaProducer.Object,
                kafkaSettings,
                _mockLogger.Object
            );
        }

        #region CreateBookingAsync Tests

        [Fact]
        public async Task CreateBookingAsync_ShouldThrow_DatesInvalid()
        {
            // Arrange
            var dto = new CreateBookingDto
            {
                CheckInDate = DateTime.Now.AddDays(2),
                CheckOutDate = DateTime.Now.AddDays(1)
            };

            // Act & Assert
            var ex = await Assert.ThrowsAsync<ArgumentException>(() => _service.CreateBookingAsync(dto));
            Assert.Equal("Check-out date must be after check-in date", ex.Message);
        }

        [Fact]
        public async Task CreateBookingAsync_ShouldSendKafkaMessage()
        {
            // Arrange
            var dto = new CreateBookingDto
            {
                UserId = 1,
                HotelId = 1,
                RoomTypeId = 10,
                CheckInDate = DateTime.UtcNow.AddDays(1),
                CheckOutDate = DateTime.UtcNow.AddDays(5),
                GuestCount = 2
            };

            // Act
            var result = await _service.CreateBookingAsync(dto);

            // Assert
            Assert.NotEqual(Guid.Empty, result);
            _mockKafkaProducer.Verify(k => k.SendMessageAsync(
                "bookings-topic",
                "10",
                It.Is<CreateBookingCommand>(c => c.UserId == 1 && c.HotelId == 1)
            ), Times.Once);
        }

        #endregion

        #region GetBookingByIdAsync Tests

        [Fact]
        public async Task GetBookingByIdAsync_ShouldReturnDto()
        {
            // Arrange
            var booking = new Booking { Id = 1, TrackingId = Guid.NewGuid() };

            _mockBookingRepo.Setup(r => r.GetByIdAsync(
                1,
                It.IsAny<Func<IQueryable<Booking>, IQueryable<Booking>>>()
            )).ReturnsAsync(booking);

            // Act
            var result = await _service.GetBookingByIdAsync(1);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(booking.Id, result.Id);
        }

        [Fact]
        public async Task GetBookingByIdAsync_ShouldReturnNull_NotFound()
        {
            // Arrange
            _mockBookingRepo.Setup(r => r.GetByIdAsync(
                1,
                It.IsAny<Func<IQueryable<Booking>, IQueryable<Booking>>>()
            )).ReturnsAsync((Booking)null);

            // Act
            var result = await _service.GetBookingByIdAsync(1);

            // Assert
            Assert.Null(result);
        }

        #endregion

        #region GetBookingByTrackingIdAsync Tests

        [Fact]
        public async Task GetBookingByTrackingIdAsync_ShouldReturnDto()
        {
            // Arrange
            var guid = Guid.NewGuid();
            var booking = new Booking { Id = 1, TrackingId = guid };

            _mockBookingRepo.Setup(r => r.FindAsync(It.IsAny<Expression<Func<Booking, bool>>>()))
                .ReturnsAsync(booking);

            // Act
            var result = await _service.GetBookingByTrackingIdAsync(guid);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(guid, result.TrackingId);
        }

        #endregion

        #region UpdateBookingAsync Tests

        [Fact]
        public async Task UpdateBookingAsync_ShouldThrow_BookingNotFound()
        {
            // Arrange
            _mockBookingRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync((Booking)null);

            // Act & Assert
            await Assert.ThrowsAsync<KeyNotFoundException>(() =>
                _service.UpdateBookingAsync(new UpdateBookingDto { Id = 1 }));
        }

        [Fact]
        public async Task UpdateBookingAsync_ShouldThrow_DatesInvalid()
        {
            // Arrange
            var booking = new Booking { Id = 1, CheckInDate = DateTime.Now, CheckOutDate = DateTime.Now.AddDays(1) };
            _mockBookingRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(booking);

            var dto = new UpdateBookingDto
            {
                Id = 1,
                CheckInDate = DateTime.Now.AddDays(2),
                CheckOutDate = DateTime.Now.AddDays(1)
            };

            // Act & Assert
            var ex = await Assert.ThrowsAsync<ArgumentException>(() => _service.UpdateBookingAsync(dto));
            Assert.Equal("Check-out date must be after check-in date", ex.Message);
        }

        [Fact]
        public async Task UpdateBookingAsync_ShouldThrow_GuestCountExceedsCapacity()
        {
            // Arrange
            var booking = new Booking { Id = 1, RoomTypeId = 10, GuestCount = 1 };
            _mockBookingRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(booking);

            var roomType = new RoomType { Id = 10, Capacity = 2 };
            _mockRoomTypeRepo.Setup(r => r.GetByIdAsync(10)).ReturnsAsync(roomType);

            var dto = new UpdateBookingDto
            {
                Id = 1,
                CheckInDate = booking.CheckInDate,
                CheckOutDate = booking.CheckOutDate,
                GuestCount = 5
            };

            // Act & Assert
            var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => _service.UpdateBookingAsync(dto));
            Assert.Contains("can only accommodate 2 guests", ex.Message);
        }

        [Fact]
        public async Task UpdateBookingAsync_Should_RoomUnavailable()
        {
            // Arrange
            var booking = new Booking { Id = 1, RoomTypeId = 10, HotelId = 1 };
            _mockBookingRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(booking);

            var roomType = new RoomType { Id = 10, HotelId = 1, Count = 1 };
            _mockRoomTypeRepo.Setup(r => r.GetByIdAsync(10)).ReturnsAsync(roomType);

            var overlappingBookings = new List<Booking>
            {
                new Booking { Id = 2, RoomTypeId = 10, CheckInDate = DateTime.Today, CheckOutDate = DateTime.Today.AddDays(5) }
            }.AsQueryable();

            var mockSet = MockAsyncQueryable(overlappingBookings);
            _mockBookingRepo.Setup(r => r.GetQueryable()).Returns(mockSet.Object);

            var dto = new UpdateBookingDto
            {
                Id = 1,
                CheckInDate = DateTime.Today.AddDays(1),
                CheckOutDate = DateTime.Today.AddDays(3)
            };

            // Act & Assert
            var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => _service.UpdateBookingAsync(dto));
            Assert.Contains("not available", ex.Message);
        }

        [Fact]
        public async Task UpdateBookingAsync_ShouldRecalculatePrice_WhenDatesChange()
        {
            // Arrange
            var booking = new Booking
            {
                Id = 1,
                RoomTypeId = 10,
                HotelId = 1,
                CheckInDate = DateTime.Today,
                CheckOutDate = DateTime.Today.AddDays(1),
                TotalPrice = 100
            };
            _mockBookingRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(booking);

            var roomType = new RoomType { Id = 10, HotelId = 1, Count = 10, BasePrice = 200 };
            _mockRoomTypeRepo.Setup(r => r.GetByIdAsync(10)).ReturnsAsync(roomType);

            var mockSet = MockAsyncQueryable(new List<Booking>().AsQueryable());
            _mockBookingRepo.Setup(r => r.GetQueryable()).Returns(mockSet.Object);

            _mockPricingRepo.Setup(r => r.GetAllAsync(It.IsAny<Expression<Func<RoomPricing, bool>>>(), null))
                .ReturnsAsync(new List<RoomPricing>());

            var dto = new UpdateBookingDto
            {
                Id = 1,
                CheckInDate = DateTime.Today.AddDays(10),
                CheckOutDate = DateTime.Today.AddDays(12),
                GuestCount = 1
            };

            // Act
            var result = await _service.UpdateBookingAsync(dto);

            // Assert
            Assert.Equal(400, booking.TotalPrice);
            _mockBookingRepo.Verify(r => r.UpdateAsync(booking), Times.Once);
        }

        #endregion

        #region CheckRoomTypeAvailabilityAsync Tests

        [Fact]
        public async Task CheckRoomTypeAvailabilityAsync_ShouldThrow_WhenRoomTypeMismatch()
        {
            // Arrange
            _mockRoomTypeRepo.Setup(r => r.GetByIdAsync(1))
                .ReturnsAsync(new RoomType { Id = 1, HotelId = 2 });

            // Act & Assert
            await Assert.ThrowsAsync<KeyNotFoundException>(() =>
                _service.CheckRoomTypeAvailabilityAsync(1, 1, DateTime.Now, DateTime.Now.AddDays(1)));
        }

        [Fact]
        public async Task CheckRoomTypeAvailabilityAsync_ShouldReturnFalse_NoRooms()
        {
            // Arrange
            _mockRoomTypeRepo.Setup(r => r.GetByIdAsync(1))
                .ReturnsAsync(new RoomType { Id = 1, HotelId = 1, Count = 0 });

            // Act
            var result = await _service.CheckRoomTypeAvailabilityAsync(1, 1, DateTime.Now, DateTime.Now.AddDays(1));

            // Assert
            Assert.False(result);
        }

        #endregion

        #region Other Method Tests

        [Fact]
        public async Task DeleteBookingAsync_ShouldDelete()
        {
            // Arrange
            _mockBookingRepo.Setup(r => r.GetByIdAsync(1))
                .ReturnsAsync(new Booking { Id = 1 });

            // Act
            await _service.DeleteBookingAsync(1);

            // Assert
            _mockBookingRepo.Verify(r => r.DeleteAsync(1), Times.Once);
        }

        [Fact]
        public async Task CancelBookingAsync_ShouldUpdateStatus()
        {
            // Arrange
            var booking = new Booking { Id = 1, Status = (int)BookingStatus.Confirmed };
            _mockBookingRepo.Setup(r => r.GetByIdAsync(1))
                .ReturnsAsync(booking);

            // Act
            await _service.CancelBookingAsync(1);

            // Assert
            Assert.Equal((int)BookingStatus.Cancelled, booking.Status);
            _mockBookingRepo.Verify(r => r.UpdateAsync(booking), Times.Once);
        }

        [Fact]
        public async Task UpdateBookingStatusAsync_ShouldThrow_StatusInvalid()
        {
            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(() =>
                _service.UpdateBookingStatusAsync(1, 999));
        }

        [Fact]
        public async Task UpdateBookingStatusAsync_ShouldUpdate()
        {
            // Arrange
            var booking = new Booking { Id = 1 };
            _mockBookingRepo.Setup(r => r.GetByIdAsync(1))
                .ReturnsAsync(booking);

            // Act
            await _service.UpdateBookingStatusAsync(1, (int)BookingStatus.Pending);

            // Assert
            Assert.Equal((int)BookingStatus.Pending, booking.Status);
        }

        [Fact]
        public async Task GetAllBookingsAsync_ShouldReturnList()
        {
            // Arrange
            _mockBookingRepo.Setup(r => r.GetAllAsync(null, It.IsAny<Func<IQueryable<Booking>, IQueryable<Booking>>>()))
                .ReturnsAsync(new List<Booking>());

            // Act
            await _service.GetAllBookingsAsync();

            // Assert
            _mockBookingRepo.Verify(r => r.GetAllAsync(null, It.IsAny<Func<IQueryable<Booking>, IQueryable<Booking>>>()), Times.Once);
        }

        [Fact]
        public async Task GetBookingsByDateRangeAsync_ShouldReturnList()
        {
            // Arrange
            _mockBookingRepo.Setup(r => r.GetAllAsync(It.IsAny<Expression<Func<Booking, bool>>>(), It.IsAny<Func<IQueryable<Booking>, IQueryable<Booking>>>()))
                .ReturnsAsync(new List<Booking>());

            // Act
            await _service.GetBookingsByDateRangeAsync(DateTime.Now, DateTime.Now.AddDays(1));

            // Assert
            _mockBookingRepo.Verify(r => r.GetAllAsync(It.IsAny<Expression<Func<Booking, bool>>>(), It.IsAny<Func<IQueryable<Booking>, IQueryable<Booking>>>()), Times.Once);
        }

        [Fact]
        public async Task GetBookingsByRoomTypeIdAsync_ShouldReturnList()
        {
            _mockBookingRepo.Setup(r => r.GetAllAsync(It.IsAny<Expression<Func<Booking, bool>>>(), It.IsAny<Func<IQueryable<Booking>, IQueryable<Booking>>>()))
               .ReturnsAsync(new List<Booking>());

            await _service.GetBookingsByRoomTypeIdAsync(1);

            _mockBookingRepo.Verify(r => r.GetAllAsync(It.IsAny<Expression<Func<Booking, bool>>>(), It.IsAny<Func<IQueryable<Booking>, IQueryable<Booking>>>()), Times.Once);
        }

        [Fact]
        public async Task GetBookingsByHotelIdAsync_ShouldReturnList()
        {
            _mockBookingRepo.Setup(r => r.GetAllAsync(It.IsAny<Expression<Func<Booking, bool>>>(), It.IsAny<Func<IQueryable<Booking>, IQueryable<Booking>>>()))
               .ReturnsAsync(new List<Booking>());

            await _service.GetBookingsByHotelIdAsync(1);

            _mockBookingRepo.Verify(r => r.GetAllAsync(It.IsAny<Expression<Func<Booking, bool>>>(), It.IsAny<Func<IQueryable<Booking>, IQueryable<Booking>>>()), Times.Once);
        }

        #endregion

        #region Helper for Async Queryable

        private Mock<IQueryable<T>> MockAsyncQueryable<T>(IQueryable<T> data) where T : class
        {
            var mockSet = new Mock<IQueryable<T>>();
            mockSet.As<IAsyncEnumerable<T>>()
                .Setup(m => m.GetAsyncEnumerator(It.IsAny<CancellationToken>()))
                .Returns(new TestAsyncEnumerator<T>(data.GetEnumerator()));

            mockSet.As<IQueryable<T>>()
                .Setup(m => m.Provider)
                .Returns(new TestAsyncQueryProvider<T>(data.Provider));

            mockSet.As<IQueryable<T>>().Setup(m => m.Expression).Returns(data.Expression);
            mockSet.As<IQueryable<T>>().Setup(m => m.ElementType).Returns(data.ElementType);
            mockSet.As<IQueryable<T>>().Setup(m => m.GetEnumerator()).Returns(data.GetEnumerator());

            return mockSet;
        }

        #endregion
    }


    internal class TestAsyncQueryProvider<TEntity> : IAsyncQueryProvider
    {
        private readonly IQueryProvider _inner;

        internal TestAsyncQueryProvider(IQueryProvider inner)
        {
            _inner = inner;
        }

        public IQueryable CreateQuery(Expression expression)
        {
            return new TestAsyncEnumerable<TEntity>(expression);
        }

        public IQueryable<TElement> CreateQuery<TElement>(Expression expression)
        {
            return new TestAsyncEnumerable<TElement>(expression);
        }

        public object Execute(Expression expression)
        {
            return _inner.Execute(expression);
        }

        public TResult Execute<TResult>(Expression expression)
        {
            return _inner.Execute<TResult>(expression);
        }

        public TResult ExecuteAsync<TResult>(Expression expression, CancellationToken cancellationToken = default)
        {
            var expectedResultType = typeof(TResult).GetGenericArguments()[0];
            var executionResult = typeof(IQueryProvider)
                                 .GetMethod(
                                     name: nameof(IQueryProvider.Execute),
                                     genericParameterCount: 1,
                                     types: new[] { typeof(Expression) }
                                 )
                                 .MakeGenericMethod(expectedResultType)
                                 .Invoke(this, new[] { expression });

            return (TResult)typeof(Task).GetMethod(nameof(Task.FromResult))
                .MakeGenericMethod(expectedResultType)
                .Invoke(null, new[] { executionResult });
        }
    }

    internal class TestAsyncEnumerable<T> : EnumerableQuery<T>, IAsyncEnumerable<T>, IQueryable<T>
    {
        public TestAsyncEnumerable(IEnumerable<T> enumerable) : base(enumerable) { }
        public TestAsyncEnumerable(Expression expression) : base(expression) { }

        public IAsyncEnumerator<T> GetAsyncEnumerator(CancellationToken cancellationToken = default)
        {
            return new TestAsyncEnumerator<T>(this.AsEnumerable().GetEnumerator());
        }

        IQueryProvider IQueryable.Provider => new TestAsyncQueryProvider<T>(this);
    }

    internal class TestAsyncEnumerator<T> : IAsyncEnumerator<T>
    {
        private readonly IEnumerator<T> _inner;

        public TestAsyncEnumerator(IEnumerator<T> inner)
        {
            _inner = inner;
        }

        public ValueTask DisposeAsync()
        {
            _inner.Dispose();
            return ValueTask.CompletedTask;
        }

        public ValueTask<bool> MoveNextAsync()
        {
            return ValueTask.FromResult(_inner.MoveNext());
        }

        public T Current => _inner.Current;
    }
}