using BookingSystem.Application.DTOs.Booking;
using BookingSystem.Application.Exceptions;
using BookingSystem.Application.Interfaces;
using BookingSystem.Domain.Entities;
using BookingSystem.Domain.Enums;
using BookingSystem.Domain.Interfaces;
using BookingSystem.Application.DTOs.User;
using BookingSystem.Domain.DTOs.Booking;
using Microsoft.AspNetCore.Http; // Added for HttpContextAccessor

namespace BookingSystem.Application.Services
{
    public class BookingService : IBookingService
    {
        private readonly IBookingRepository _bookingRepository;
        private readonly IRepository<RoomType> _roomTypeRepository;
        private readonly IHotelRepository _hotelRepository;
        private readonly IRepository<RoomPricing> _pricingRepository;
        private readonly ILoggingService _loggingService;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public BookingService(
            IBookingRepository bookingRepository,
            IRepository<RoomType> roomTypeRepository,
            IHotelRepository hotelRepository,
            IRepository<RoomPricing> pricingRepository,
            ILoggingService loggingService,
            IHttpContextAccessor httpContextAccessor)
        {
            _bookingRepository = bookingRepository;
            _roomTypeRepository = roomTypeRepository;
            _hotelRepository = hotelRepository;
            _pricingRepository = pricingRepository;
            _loggingService = loggingService;
            _httpContextAccessor = httpContextAccessor;
        }

        public async Task<BookingResponseDto> GetBookingByIdAsync(int id)
        {
            var booking = await _bookingRepository.GetByIdAsync(id);

            return booking != null ? MapToDto(booking) : null;
        }

        public async Task<IEnumerable<BookingResponseDto>> GetAllBookingsAsync()
        {
            var bookings = await _bookingRepository.GetAllAsync();

            return bookings.Select(MapToDto);
        }

        public async Task<BookingResponseDto> CreateBookingAsync(CreateBookingDto bookingDto)
        {
            // Validate dates
            if (bookingDto.CheckInDate >= bookingDto.CheckOutDate)
            {
                await _loggingService.LogErrorAsync(new ArgumentException("Check-out date must be after check-in date"), bookingDto.UserId,
                                                    _httpContextAccessor.HttpContext?.Connection.RemoteIpAddress?.ToString(),
                                                    _httpContextAccessor.HttpContext?.Request.Path,
                                                    _httpContextAccessor.HttpContext?.Request.Method);
                throw new ArgumentException("Check-out date must be after check-in date");
            }

            // Check if hotel exists
            var hotel = await _hotelRepository.GetByIdAsync(bookingDto.HotelId);
            if (hotel == null)
            {
                await _loggingService.LogErrorAsync(new KeyNotFoundException($"Hotel with ID {bookingDto.HotelId} not found"), bookingDto.UserId,
                                                    _httpContextAccessor.HttpContext?.Connection.RemoteIpAddress?.ToString(),
                                                    _httpContextAccessor.HttpContext?.Request.Path,
                                                    _httpContextAccessor.HttpContext?.Request.Method);
                throw new KeyNotFoundException($"Hotel with ID {bookingDto.HotelId} not found");
            }

            // Check if room type exists and belongs to the hotel
            var roomType = await _roomTypeRepository.GetByIdAsync(bookingDto.RoomTypeId);
            if (roomType == null || roomType.HotelId != bookingDto.HotelId)
            {
                await _loggingService.LogErrorAsync(new KeyNotFoundException($"Room type with ID {bookingDto.RoomTypeId} not found in hotel {bookingDto.HotelId}"), bookingDto.UserId,
                                                    _httpContextAccessor.HttpContext?.Connection.RemoteIpAddress?.ToString(),
                                                    _httpContextAccessor.HttpContext?.Request.Path,
                                                    _httpContextAccessor.HttpContext?.Request.Method);
                throw new KeyNotFoundException($"Room type with ID {bookingDto.RoomTypeId} not found in hotel {bookingDto.HotelId}");
            }

            // Check if the number of guests is within the room type capacity
            if (bookingDto.GuestCount > roomType.Capacity)
            {
                await _loggingService.LogErrorAsync(new InvalidOperationException($"This room type can only accommodate {roomType.Capacity} guests"), bookingDto.UserId,
                                                    _httpContextAccessor.HttpContext?.Connection.RemoteIpAddress?.ToString(),
                                                    _httpContextAccessor.HttpContext?.Request.Path,
                                                    _httpContextAccessor.HttpContext?.Request.Method);
                throw new InvalidOperationException($"This room type can only accommodate {roomType.Capacity} guests");
            }

            // Check availability of the room type for the requested dates
            var isAvailable = await CheckRoomTypeAvailabilityAsync(
                bookingDto.RoomTypeId,
                bookingDto.HotelId,
                bookingDto.CheckInDate,
                bookingDto.CheckOutDate);

            if (!isAvailable)
            {
                await _loggingService.LogErrorAsync(new InvalidOperationException("The selected room type is not available for the requested dates"), bookingDto.UserId,
                                                    _httpContextAccessor.HttpContext?.Connection.RemoteIpAddress?.ToString(),
                                                    _httpContextAccessor.HttpContext?.Request.Path,
                                                    _httpContextAccessor.HttpContext?.Request.Method);
                throw new BookingConflictException("Номер занят на выбранные даты");
            }

            // Calculate total price
            var totalPrice = await CalculateTotalPrice(bookingDto);

            // Create booking
            var booking = new Domain.Entities.Booking
            {
                RoomTypeId = bookingDto.RoomTypeId,
                UserId = bookingDto.UserId,
                HotelId = bookingDto.HotelId,
                CheckInDate = bookingDto.CheckInDate,
                CheckOutDate = bookingDto.CheckOutDate,
                GuestCount = bookingDto.GuestCount,
                TotalPrice = totalPrice,
                Status = (int)BookingStatus.Pending,
                CreatedAt = DateTime.UtcNow
            };

            // при гонке запросов пересечение дат отклонит исключающее
            // ограничение СУБД; контроллер переводит его в код 409
            await _bookingRepository.AddAsync(booking);

            await _loggingService.LogActionAsync(booking.UserId, UserActionType.BookingCreated, $"Booking {booking.Id} created successfully.",
                                                  _httpContextAccessor.HttpContext?.Connection.RemoteIpAddress?.ToString(),
                                                  _httpContextAccessor.HttpContext?.Request.Path,
                                                  _httpContextAccessor.HttpContext?.Request.Method);

            return MapToDto(booking);
        }

        public async Task<BookingResponseDto> UpdateBookingAsync(UpdateBookingDto dto)
        {
            var booking = await _bookingRepository.GetByIdAsync(dto.Id);
            if (booking == null)
            {
                await _loggingService.LogErrorAsync(new KeyNotFoundException("Booking not found"), null, // UserId might not be available here
                                                    _httpContextAccessor.HttpContext?.Connection.RemoteIpAddress?.ToString(),
                                                    _httpContextAccessor.HttpContext?.Request.Path,
                                                    _httpContextAccessor.HttpContext?.Request.Method);
                throw new KeyNotFoundException("Booking not found");
            }

            // If dates changed, recalculate total price and check availability
            if (booking.CheckInDate != dto.CheckInDate ||
                booking.CheckOutDate != dto.CheckOutDate)
            {
                // Validate new dates
                if (dto.CheckInDate >= dto.CheckOutDate)
                {
                    await _loggingService.LogErrorAsync(new ArgumentException("Check-out date must be after check-in date"), booking.UserId,
                                                        _httpContextAccessor.HttpContext?.Connection.RemoteIpAddress?.ToString(),
                                                        _httpContextAccessor.HttpContext?.Request.Path,
                                                        _httpContextAccessor.HttpContext?.Request.Method);
                    throw new ArgumentException("Check-out date must be after check-in date");
                }

                // Check availability with new dates
                var isAvailable = await CheckRoomTypeAvailabilityAsync(
                    booking.RoomTypeId,
                    booking.HotelId,
                    dto.CheckInDate,
                    dto.CheckOutDate,
                    booking.Id); // Exclude current booking

                if (!isAvailable)
                {
                    await _loggingService.LogErrorAsync(new InvalidOperationException("The room type is not available for the selected dates"), booking.UserId,
                                                        _httpContextAccessor.HttpContext?.Connection.RemoteIpAddress?.ToString(),
                                                        _httpContextAccessor.HttpContext?.Request.Path,
                                                        _httpContextAccessor.HttpContext?.Request.Method);
                    throw new BookingConflictException("Номер занят на выбранные даты");
                }

                // Recalculate price
                booking.TotalPrice = await CalculateTotalPrice(new CreateBookingDto
                {
                    RoomTypeId = booking.RoomTypeId,
                    HotelId = booking.HotelId,
                    CheckInDate = dto.CheckInDate,
                    CheckOutDate = dto.CheckOutDate,
                    GuestCount = dto.GuestCount
                });
            }

            // Check if the number of guests is within the room type capacity
            if (dto.GuestCount != booking.GuestCount)
            {
                var roomType = await _roomTypeRepository.GetByIdAsync(booking.RoomTypeId);
                if (dto.GuestCount > roomType.Capacity)
                {
                    await _loggingService.LogErrorAsync(new InvalidOperationException($"This room type can only accommodate {roomType.Capacity} guests"), booking.UserId,
                                                        _httpContextAccessor.HttpContext?.Connection.RemoteIpAddress?.ToString(),
                                                        _httpContextAccessor.HttpContext?.Request.Path,
                                                        _httpContextAccessor.HttpContext?.Request.Method);
                    throw new InvalidOperationException($"This room type can only accommodate {roomType.Capacity} guests");
                }
            }

            booking.CheckInDate = dto.CheckInDate;
            booking.CheckOutDate = dto.CheckOutDate;
            booking.GuestCount = dto.GuestCount;
            booking.Status = dto.Status;
            await _bookingRepository.UpdateAsync(booking);
            await _loggingService.LogActionAsync(booking.UserId, UserActionType.BookingUpdated, $"Booking {booking.Id} updated successfully.",
                                                  _httpContextAccessor.HttpContext?.Connection.RemoteIpAddress?.ToString(),
                                                  _httpContextAccessor.HttpContext?.Request.Path,
                                                  _httpContextAccessor.HttpContext?.Request.Method);

            return MapToDto(booking);
        }

        public async Task DeleteBookingAsync(int id)
        {
            var booking = await _bookingRepository.GetByIdAsync(id);
            if (booking == null)
            {
                await _loggingService.LogErrorAsync(new KeyNotFoundException($"Booking with ID {id} not found"), null,
                                                    _httpContextAccessor.HttpContext?.Connection.RemoteIpAddress?.ToString(),
                                                    _httpContextAccessor.HttpContext?.Request.Path,
                                                    _httpContextAccessor.HttpContext?.Request.Method);
                throw new KeyNotFoundException($"Booking with ID {id} not found");
            }

            await _bookingRepository.DeleteAsync(id);
            await _loggingService.LogActionAsync(booking.UserId, UserActionType.BookingDeleted, $"Booking {booking.Id} deleted successfully.",
                                                  _httpContextAccessor.HttpContext?.Connection.RemoteIpAddress?.ToString(),
                                                  _httpContextAccessor.HttpContext?.Request.Path,
                                                  _httpContextAccessor.HttpContext?.Request.Method);
        }

        // public async Task<IEnumerable<BookingResponseDto>> GetBookingsByUserIdAsync(int userId)
        // {
        //     var bookings = await _bookingRepository.GetAllAsync(
        //         b => b.UserId == userId,
        //         include: query => query.Include(b => b.Hotel)
        //                               .Include(b => b.RoomType));
        //                               
        //     return bookings.Select(MapToDto);
        // }
        public async Task<IEnumerable<BookingResponseDto>> GetBookingsByUserIdAsync(int userId)
        {
            try
            {

                var bookings = await _bookingRepository.GetAllAsync(
                    b => b.UserId == userId && !b.IsDeleted
                );

                return bookings.Select(MapToDto);
            }
            catch (Exception ex)
            {
                await _loggingService.LogErrorAsync(ex, userId,
                                                    _httpContextAccessor.HttpContext?.Connection.RemoteIpAddress?.ToString(),
                                                    _httpContextAccessor.HttpContext?.Request.Path,
                                                    _httpContextAccessor.HttpContext?.Request.Method);
                // Fallback with no includes if error occurs
                Console.WriteLine($"Error in GetBookingsByUserIdAsync: {ex.Message}");
                var bookings = await _bookingRepository.GetAllAsync(
                    b => b.UserId == userId && !b.IsDeleted
                );

                return bookings.Select(b => new BookingResponseDto
                {
                    Id = b.Id,
                    RoomTypeId = b.RoomTypeId,
                    UserId = b.UserId,
                    HotelId = b.HotelId,
                    CheckInDate = b.CheckInDate,
                    CheckOutDate = b.CheckOutDate,
                    GuestCount = b.GuestCount,
                    TotalPrice = b.TotalPrice,
                    Status = b.Status,
                });
            }
        }

        public async Task<bool> CheckRoomTypeAvailabilityAsync(
            int roomTypeId,
            int hotelId,
            DateTime checkInDate,
            DateTime checkOutDate,
            int? excludeBookingId = null)
        {
            // Get the room type
            var roomType = await _roomTypeRepository.GetByIdAsync(roomTypeId);
            if (roomType == null || roomType.HotelId != hotelId)
            {
                await _loggingService.LogErrorAsync(new KeyNotFoundException($"Room type with ID {roomTypeId} not found in hotel {hotelId}"), null,
                                                    _httpContextAccessor.HttpContext?.Connection.RemoteIpAddress?.ToString(),
                                                    _httpContextAccessor.HttpContext?.Request.Path,
                                                    _httpContextAccessor.HttpContext?.Request.Method);
                throw new KeyNotFoundException($"Room type with ID {roomTypeId} not found in hotel {hotelId}");
            }

            // Проверка пересечения с активными бронированиями (статусы 1–3)
            return !await _bookingRepository.HasOverlappingBookingAsync(
                roomTypeId, checkInDate, checkOutDate, excludeBookingId);
        }

        public async Task<BookingResponseDto> CancelBookingAsync(int id)
        {
            var booking = await _bookingRepository.GetByIdAsync(id);
            if (booking == null)
            {
                await _loggingService.LogErrorAsync(new KeyNotFoundException("Booking not found"), null,
                                                    _httpContextAccessor.HttpContext?.Connection.RemoteIpAddress?.ToString(),
                                                    _httpContextAccessor.HttpContext?.Request.Path,
                                                    _httpContextAccessor.HttpContext?.Request.Method);
                throw new KeyNotFoundException("Booking not found");
            }

            booking.Status = (int)BookingStatus.Cancelled;
            booking.UpdatedAt = DateTime.UtcNow;

            await _bookingRepository.UpdateAsync(booking);

            await _loggingService.LogActionAsync(booking.UserId, UserActionType.BookingUpdated, $"Booking {booking.Id} cancelled successfully.",
                                                  _httpContextAccessor.HttpContext?.Connection.RemoteIpAddress?.ToString(),
                                                  _httpContextAccessor.HttpContext?.Request.Path,
                                                  _httpContextAccessor.HttpContext?.Request.Method);

            return MapToDto(booking);
        }

        public async Task<IEnumerable<BookingResponseDto>> GetBookingsByDateRangeAsync(DateTime startDate, DateTime endDate)
        {
            var bookings = await _bookingRepository.GetAllAsync(
                b => b.CheckInDate >= startDate && b.CheckOutDate <= endDate);

            return bookings.Select(MapToDto);
        }

        public async Task<BookingResponseDto> UpdateBookingStatusAsync(int id, int statusCode)
        {
            if (!Enum.IsDefined(typeof(BookingStatus), statusCode))
            {
                await _loggingService.LogErrorAsync(new ArgumentException("Invalid booking status code"), null,
                                                    _httpContextAccessor.HttpContext?.Connection.RemoteIpAddress?.ToString(),
                                                    _httpContextAccessor.HttpContext?.Request.Path,
                                                    _httpContextAccessor.HttpContext?.Request.Method);
                throw new ArgumentException("Invalid booking status code");
            }

            var booking = await _bookingRepository.GetByIdAsync(id);
            if (booking == null)
            {
                await _loggingService.LogErrorAsync(new KeyNotFoundException("Booking not found"), null,
                                                    _httpContextAccessor.HttpContext?.Connection.RemoteIpAddress?.ToString(),
                                                    _httpContextAccessor.HttpContext?.Request.Path,
                                                    _httpContextAccessor.HttpContext?.Request.Method);
                throw new KeyNotFoundException("Booking not found");
            }

            booking.Status = statusCode;
            booking.UpdatedAt = DateTime.UtcNow;

            await _bookingRepository.UpdateAsync(booking);
            await _loggingService.LogActionAsync(booking.UserId, UserActionType.BookingUpdated, $"Booking {booking.Id} status updated to {((BookingStatus)statusCode).ToString()}.",
                                                  _httpContextAccessor.HttpContext?.Connection.RemoteIpAddress?.ToString(),
                                                  _httpContextAccessor.HttpContext?.Request.Path,
                                                  _httpContextAccessor.HttpContext?.Request.Method);
            return MapToDto(booking);
        }

        public async Task<IEnumerable<BookingResponseDto>> GetBookingsByRoomTypeIdAsync(int roomTypeId)
        {
            var bookings = await _bookingRepository.GetAllAsync(
                b => b.RoomTypeId == roomTypeId);

            return bookings.Select(MapToDto);
        }

        public async Task<IEnumerable<BookingResponseDto>> GetBookingsByHotelIdAsync(int hotelId)
        {
            var bookings = await _bookingRepository.GetAllAsync(
                b => b.HotelId == hotelId);

            return bookings.Select(MapToDto);
        }

        public async Task<IEnumerable<BookingDetails>> GetBookingsWithDetailsAsync(int? userId = null, int? hotelId = null, int? status = null)
        {
            return await _bookingRepository.GetBookingsWithDetailsAsync(userId, hotelId, status);
        }

        public async Task<UserBookingHistory> GetUserBookingHistoryAsync(int userId)
        {
            try
            {
                var result = await _bookingRepository.GetUserBookingHistoryAsync(userId);
                if (result == null)
                    return null;

                var dictionary = (IDictionary<string, object>)result;

                var recentBookingsRaw = dictionary.ContainsKey("recentbookings") ? dictionary["recentbookings"] : null;
                var recentBookings = (recentBookingsRaw as IEnumerable<dynamic>)
                    ?.Where(b => b != null) // Filter out null bookings
                    .Select(b =>
                    {
                        var bDict = (IDictionary<string, object>)b;
                        return new UserRecentBooking
                        {
                            BookingId = (int)bDict["bookingid"],
                            CheckInDate = (DateTime)bDict["checkindate"],
                            CheckOutDate = (DateTime)bDict["checkoutdate"],
                            Status = (int)bDict["status"],
                            TotalPrice = (decimal)bDict["totalprice"],
                            HotelName = (string)bDict["hotelname"],
                            Location = (string)bDict["location"],
                            RoomTypeName = (string)bDict["roomtypename"]
                        };
                    }).ToList() ?? new List<UserRecentBooking>();

                var favoriteLocationsRaw = dictionary.ContainsKey("favoritelocations") ? dictionary["favoritelocations"] : null;
                var favoriteLocations = (favoriteLocationsRaw as IEnumerable<dynamic>)
                    ?.Where(l => l != null) // Filter out null locations
                    .Select(l =>
                    {
                        var lDict = (IDictionary<string, object>)l;
                        return new LocationStatistic
                        {
                            Location = (string)lDict["location"],
                            BookingCount = Convert.ToInt32(lDict["bookingcount"]),
                            TotalSpent = (decimal)lDict["totalspent"]
                        };
                    }).ToList() ?? new List<LocationStatistic>();

                var history = new UserBookingHistory
                {
                    UserId = (int)dictionary["userid"],
                    Username = (string)dictionary["username"],
                    Email = (string)dictionary["email"],
                    FullName = (string)dictionary["fullname"],
                    TotalBookings = Convert.ToInt32(dictionary["totalbookings"]),
                    ConfirmedBookings = Convert.ToInt32(dictionary["completedbookings"]),
                    CancelledBookings = Convert.ToInt32(dictionary["cancelledbookings"]),
                    TotalSpent = (decimal)dictionary["totalspent"],
                    MemberSince = (DateTime)dictionary["firstbookingdate"],
                    AverageBookingValue = (decimal)dictionary["averagebookingvalue"],
                    FirstBookingDate = (DateTime?)dictionary["firstbookingdate"],
                    LastBookingDate = (DateTime?)dictionary["lastbookingdate"],
                    UniqueHotelsVisited = Convert.ToInt32(dictionary["uniquehotelsvisited"]),
                    RecentBookings = recentBookings,
                    FavoriteLocations = favoriteLocations,
                    CompletedBookings = Convert.ToInt32(dictionary["completedbookings"])
                };

                return history;
            }
            catch (Exception ex)
            {
                await _loggingService.LogErrorAsync(ex, userId,
                                                    _httpContextAccessor.HttpContext?.Connection.RemoteIpAddress?.ToString(),
                                                    _httpContextAccessor.HttpContext?.Request.Path,
                                                    _httpContextAccessor.HttpContext?.Request.Method);
                throw;
            }
        }

        public async Task<IEnumerable<BookingSystem.Domain.DTOs.Booking.ActiveBookingDetailsDto>> GetActiveBookingsWithDetailsAsync()
        {
            return await _bookingRepository.GetActiveBookingsWithDetailsAsync();
        }

        private async Task<decimal> CalculateTotalPrice(CreateBookingDto dto)
        {
            decimal totalPrice = 0;
            int nightCount = (int)(dto.CheckOutDate.Date - dto.CheckInDate.Date).TotalDays;

            // Try to get seasonal pricing
            var pricingRecords = await _pricingRepository.GetAllAsync(rp =>
                rp.RoomTypeId == dto.RoomTypeId &&
                rp.Date >= dto.CheckInDate.Date &&
                rp.Date < dto.CheckOutDate.Date);

            if (pricingRecords.Any())
            {
                // If we have pricing records for some or all days
                var coveredDates = pricingRecords.Select(p => p.Date.Date).ToHashSet();

                // Add up prices for days with specific pricing
                totalPrice += pricingRecords.Sum(p => p.Price);

                // If some days don't have specific pricing, use the room type's base price
                if (coveredDates.Count < nightCount)
                {
                    var roomType = await _roomTypeRepository.GetByIdAsync(dto.RoomTypeId);

                    // For each date in the range, check if it has specific pricing
                    for (var date = dto.CheckInDate.Date; date < dto.CheckOutDate.Date; date = date.AddDays(1))
                    {
                        if (!coveredDates.Contains(date))
                        {
                            totalPrice += roomType.BasePrice;
                        }
                    }
                }
            }
            else
            {
                // If no pricing records, use the room type's base price
                var roomType = await _roomTypeRepository.GetByIdAsync(dto.RoomTypeId);
                totalPrice = roomType.BasePrice * nightCount;
            }

            return totalPrice;
        }

        private static BookingResponseDto MapToDto(Domain.Entities.Booking booking) => new()
        {
            Id = booking.Id,
            RoomTypeId = booking.RoomTypeId,
            UserId = booking.UserId,
            HotelId = booking.HotelId,
            CheckInDate = booking.CheckInDate,
            CheckOutDate = booking.CheckOutDate,
            GuestCount = booking.GuestCount,
            TotalPrice = booking.TotalPrice,
            Status = booking.Status,
        };
    }
}
