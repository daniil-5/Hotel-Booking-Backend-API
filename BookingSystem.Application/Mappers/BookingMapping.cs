using BookingSystem.Application.DTOs.Booking;
using BookingSystem.Domain.Enums;

namespace BookingSystem.Application.Mappers
{
    public static class BookingMapping
    {
        public static BookingResponseDto ToDto(this Domain.Entities.Booking booking)
        {
            if (booking == null) return null;

            return new BookingResponseDto
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
                TrackingId = booking.TrackingId
            };
        }
        public static Domain.Entities.Booking ToEntity(this CreateBookingDto dto)
        {
            if (dto == null) return null;

            return new Domain.Entities.Booking
            {
                RoomTypeId = dto.RoomTypeId,
                UserId = dto.UserId,
                HotelId = dto.HotelId,
                CheckInDate = dto.CheckInDate,
                CheckOutDate = dto.CheckOutDate,
                GuestCount = dto.GuestCount,
                Status = (int)BookingStatus.Pending,
                CreatedAt = DateTime.UtcNow
            };
        }
    }
}