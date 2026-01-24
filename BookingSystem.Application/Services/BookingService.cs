using BookingSystem.Application.Decorators;
using BookingSystem.Application.DTOs.Booking;
using BookingSystem.Application.DTOs.Commands;
using BookingSystem.Application.Interfaces;
using BookingSystem.Application.Mappers;
using BookingSystem.Application.Settings;
using BookingSystem.Domain.Entities;
using BookingSystem.Domain.Enums;
using BookingSystem.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace BookingSystem.Application.Services
{
    public class BookingService : IBookingService
    {
        private readonly IRepository<Booking> _bookingRepository;
        private readonly IRepository<RoomType> _roomTypeRepository;
        private readonly IHotelRepository _hotelRepository;
        private readonly IRepository<RoomPricing> _pricingRepository;
        private readonly IUserRepository _userRepository;

        private readonly IKafkaProducer _kafkaProducer;
        private readonly KafkaSettings _kafkaSettings;
        private readonly ILogger<BookingService> _logger;

        public BookingService(
            IRepository<Booking> bookingRepository,
            IRepository<RoomType> roomTypeRepository,
            IUserRepository userRepository,
            IHotelRepository hotelRepository,
            IRepository<RoomPricing> pricingRepository,
            IKafkaProducer kafkaProducer,
            IOptions<KafkaSettings> kafkaSettings,
            ILogger<BookingService> logger)
        {
            _bookingRepository = bookingRepository;
            _roomTypeRepository = roomTypeRepository;
            _userRepository = userRepository;
            _hotelRepository = hotelRepository;
            _pricingRepository = pricingRepository;
            _kafkaProducer = kafkaProducer;
            _kafkaSettings = kafkaSettings.Value;
            _logger = logger;
        }

        public async Task<BookingResponseDto> GetBookingByIdAsync(int id)
        {
            var booking = await _bookingRepository.GetByIdAsync(id,
                include: b => b.Include(x => x.Hotel)
                               .Include(x => x.RoomType)
                               .Include(x => x.User));

            return booking?.ToDto();
        }

        public async Task<IEnumerable<BookingResponseDto>> GetAllBookingsAsync()
        {
            var bookings = await _bookingRepository.GetAllAsync(
                include: query => query.Include(b => b.Hotel)
                                      .Include(b => b.RoomType)
                                      .Include(b => b.User));

            return bookings.Select(b => b.ToDto());
        }

        public async Task<Guid> CreateBookingAsync(CreateBookingDto dto)
        {
            if (dto.CheckInDate >= dto.CheckOutDate)
                throw new ArgumentException("Check-out date must be after check-in date");

            var command = new CreateBookingCommand
            {
                UserId = dto.UserId,
                HotelId = dto.HotelId,
                RoomTypeId = dto.RoomTypeId,
                CheckInDate = dto.CheckInDate,
                CheckOutDate = dto.CheckOutDate,
                GuestCount = dto.GuestCount,
                RequestTime = DateTime.UtcNow
            };

            await _kafkaProducer.SendMessageAsync(
                _kafkaSettings.Topics.BookingRequests,
                dto.RoomTypeId.ToString(),
                command
            );

            return command.TrackingId;
        }

        public async Task<BookingResponseDto?> GetBookingByTrackingIdAsync(Guid trackingId)
        {
            var booking = await _bookingRepository.FindAsync(b => b.TrackingId == trackingId);
            return booking?.ToDto();
        }

        public async Task<BookingResponseDto> UpdateBookingAsync(UpdateBookingDto dto)
        {
            var booking = await _bookingRepository.GetByIdAsync(dto.Id)
                ?? throw new KeyNotFoundException("Booking not found");

            if (booking.CheckInDate != dto.CheckInDate || booking.CheckOutDate != dto.CheckOutDate)
            {
                if (dto.CheckInDate >= dto.CheckOutDate)
                    throw new ArgumentException("Check-out date must be after check-in date");

                var isAvailable = await CheckRoomTypeAvailabilityAsync(
                    booking.RoomTypeId,
                    booking.HotelId,
                    dto.CheckInDate,
                    dto.CheckOutDate,
                    booking.Id);

                if (!isAvailable)
                    throw new InvalidOperationException("The room type is not available for the selected dates");

                booking.TotalPrice = await CalculateTotalPrice(new CreateBookingDto
                {
                    RoomTypeId = booking.RoomTypeId,
                    HotelId = booking.HotelId,
                    CheckInDate = dto.CheckInDate,
                    CheckOutDate = dto.CheckOutDate,
                    GuestCount = dto.GuestCount
                });
            }

            if (dto.GuestCount != booking.GuestCount)
            {
                var roomType = await _roomTypeRepository.GetByIdAsync(booking.RoomTypeId);
                if (dto.GuestCount > roomType.Capacity)
                    throw new InvalidOperationException($"This room type can only accommodate {roomType.Capacity} guests");
            }

            booking.CheckInDate = dto.CheckInDate;
            booking.CheckOutDate = dto.CheckOutDate;
            booking.GuestCount = dto.GuestCount;
            booking.Status = dto.Status;
            booking.UpdatedAt = DateTime.UtcNow;

            await _bookingRepository.UpdateAsync(booking);
            return booking.ToDto();
        }

        public async Task DeleteBookingAsync(int id)
        {
            var booking = await _bookingRepository.GetByIdAsync(id);
            if (booking == null)
                throw new KeyNotFoundException($"Booking with ID {id} not found");

            await _bookingRepository.DeleteAsync(id);
        }

        public async Task<IEnumerable<BookingResponseDto>> GetBookingsByUserIdAsync(int userId)
        {
            try
            {
                var bookings = await _bookingRepository.GetAllAsync(
                    b => b.UserId == userId && !b.IsDeleted,
                    include: query => query
                        .Include(b => b.Hotel)
                        .Include(b => b.RoomType)
                        .Include(b => b.User)
                );
                return bookings.Select(b => b.ToDto());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching bookings for user {UserId}", userId);
                var bookings = await _bookingRepository.GetAllAsync(b => b.UserId == userId && !b.IsDeleted);
                return bookings.Select(b => b.ToDto());
            }
        }

        public async Task<bool> CheckRoomTypeAvailabilityAsync(
            int roomTypeId,
            int hotelId,
            DateTime checkInDate,
            DateTime checkOutDate,
            int? excludeBookingId = null)
        {
            var roomType = await _roomTypeRepository.GetByIdAsync(roomTypeId);
            if (roomType == null || roomType.HotelId != hotelId)
                throw new KeyNotFoundException($"Room type with ID {roomTypeId} not found in hotel {hotelId}");

            var totalRooms = roomType.Count;

            if (totalRooms <= 0)
                return false;

            var query = _bookingRepository.GetQueryable()
                .Where(b => b.RoomTypeId == roomTypeId &&
                       b.Status != (int)BookingStatus.Cancelled &&
                       b.CheckInDate < checkOutDate &&
                       b.CheckOutDate > checkInDate);

            if (excludeBookingId.HasValue)
                query = query.Where(b => b.Id != excludeBookingId.Value);

            var overlappingBookingsCount = await query.CountAsync();

            return overlappingBookingsCount < totalRooms;
        }

        public async Task<BookingResponseDto> CancelBookingAsync(int id)
        {
            var booking = await _bookingRepository.GetByIdAsync(id)
                ?? throw new KeyNotFoundException("Booking not found");

            booking.Status = (int)BookingStatus.Cancelled;
            booking.UpdatedAt = DateTime.UtcNow;

            await _bookingRepository.UpdateAsync(booking);
            return booking.ToDto();
        }

        public async Task<IEnumerable<BookingResponseDto>> GetBookingsByDateRangeAsync(DateTime startDate, DateTime endDate)
        {
            var bookings = await _bookingRepository.GetAllAsync(
                b => b.CheckInDate >= startDate && b.CheckOutDate <= endDate,
                include: query => query.Include(b => b.Hotel)
                                      .Include(b => b.RoomType)
                                      .Include(b => b.User));

            return bookings.Select(b => b.ToDto());
        }

        public async Task<BookingResponseDto> UpdateBookingStatusAsync(int id, int statusCode)
        {
            if (!Enum.IsDefined(typeof(BookingStatus), statusCode))
                throw new ArgumentException("Invalid booking status code");

            var booking = await _bookingRepository.GetByIdAsync(id)
                ?? throw new KeyNotFoundException("Booking not found");

            booking.Status = statusCode;
            booking.UpdatedAt = DateTime.UtcNow;

            await _bookingRepository.UpdateAsync(booking);
            return booking.ToDto();
        }

        public async Task<IEnumerable<BookingResponseDto>> GetBookingsByRoomTypeIdAsync(int roomTypeId)
        {
            var bookings = await _bookingRepository.GetAllAsync(
                b => b.RoomTypeId == roomTypeId,
                include: query => query.Include(b => b.Hotel).Include(b => b.User));

            return bookings.Select(b => b.ToDto());
        }

        public async Task<IEnumerable<BookingResponseDto>> GetBookingsByHotelIdAsync(int hotelId)
        {
            var bookings = await _bookingRepository.GetAllAsync(
                b => b.HotelId == hotelId,
                include: query => query.Include(b => b.RoomType).Include(b => b.User));

            return bookings.Select(b => b.ToDto());
        }

        private async Task<decimal> CalculateTotalPrice(CreateBookingDto dto)
        {
            decimal totalPrice = 0;
            int nightCount = (int)(dto.CheckOutDate.Date - dto.CheckInDate.Date).TotalDays;

            var pricingRecords = await _pricingRepository.GetAllAsync(rp =>
                rp.RoomTypeId == dto.RoomTypeId &&
                rp.Date >= dto.CheckInDate.Date &&
                rp.Date < dto.CheckOutDate.Date);

            if (pricingRecords.Any())
            {
                var coveredDates = pricingRecords.Select(p => p.Date.Date).ToHashSet();
                totalPrice += pricingRecords.Sum(p => p.Price);

                if (coveredDates.Count < nightCount)
                {
                    var roomType = await _roomTypeRepository.GetByIdAsync(dto.RoomTypeId);
                    for (var date = dto.CheckInDate.Date; date < dto.CheckOutDate.Date; date = date.AddDays(1))
                    {
                        if (!coveredDates.Contains(date))
                            totalPrice += roomType.BasePrice;
                    }
                }
            }
            else
            {
                var roomType = await _roomTypeRepository.GetByIdAsync(dto.RoomTypeId);
                totalPrice = roomType.BasePrice * nightCount;
            }

            return totalPrice;
        }
    }
}
