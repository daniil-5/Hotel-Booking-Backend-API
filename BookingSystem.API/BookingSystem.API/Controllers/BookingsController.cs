using BookingSystem.Application.Exceptions;
using Npgsql;
using BookingSystem.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using BookingSystem.Application.DTOs.Booking;
using BookingSystem.Application.DTOs.User;
using BookingSystem.Domain.DTOs.Booking;


[Route("api/[controller]")]
[ApiController]
[Authorize]
public class BookingsController : ControllerBase
{
    private readonly IBookingService _bookingService;

    public BookingsController(IBookingService bookingService)
    {
        _bookingService = bookingService;
    }



    [HttpGet("{id}")]
    public async Task<ActionResult<BookingResponseDto>> GetBooking(int id)
    {
        var booking = await _bookingService.GetBookingByIdAsync(id);

        if (booking == null)
            return NotFound();

        // Check if user has access to this booking
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized("User ID not found in token");
        }

        // Allow managers/admins to access any booking
        if (User.IsInRole("Manager") || User.IsInRole("Admin"))
        {
            return Ok(booking);
        }

        // Regular users can only access their own bookings
        if (booking.UserId != int.Parse(userId))
        {
            return Forbid();
        }

        return Ok(booking);
    }

    [HttpPost]
    public async Task<ActionResult<BookingResponseDto>> CreateBooking(CreateBookingDto bookingDto)
    {
        try
        {
            // Get current user ID from JWT claims and assign it to the booking
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized("User ID not found in token");
            }

            // Assign the current user ID to the booking
            bookingDto.UserId = int.Parse(userId);

            var booking = await _bookingService.CreateBookingAsync(bookingDto);
            return CreatedAtAction(nameof(GetBooking), new { id = booking.Id }, booking);
        }
        catch (BookingConflictException ex)
        {
            return Conflict(ex.Message);
        }
        catch (PostgresException ex) when (ex.SqlState is "23P01" or "23514")
        {
            // исключающее ограничение no_overlap_active_bookings
            return Conflict("Номер занят на выбранные даты");
        }
        catch (Exception ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateBooking(int id, UpdateBookingDto bookingDto)
    {
        try
        {
            if (id != bookingDto.Id)
                return BadRequest("ID mismatch");

            // Check if user has permission to update this booking
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized("User ID not found in token");
            }

            // Get the booking to check ownership
            var existingBooking = await _bookingService.GetBookingByIdAsync(id);
            if (existingBooking == null)
                return NotFound();

            // Allow managers/admins to update any booking
            if (!User.IsInRole("Manager") && !User.IsInRole("Admin"))
            {
                // Regular users can only update their own bookings
                if (existingBooking.UserId != int.Parse(userId))
                {
                    return Forbid();
                }
            }

            await _bookingService.UpdateBookingAsync(bookingDto);
            return NoContent();
        }
        catch (Exception ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteBooking(int id)
    {
        try
        {
            // Check if user has permission to delete this booking
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized("User ID not found in token");
            }

            // Get the booking to check ownership
            var existingBooking = await _bookingService.GetBookingByIdAsync(id);
            if (existingBooking == null)
                return NotFound();

            // Allow managers/admins to delete any booking
            if (!User.IsInRole("Manager") && !User.IsInRole("Admin"))
            {
                // Regular users can only delete their own bookings
                if (existingBooking.UserId != int.Parse(userId))
                {
                    return Forbid();
                }
            }

            await _bookingService.DeleteBookingAsync(id);
            return NoContent();
        }
        catch (Exception ex)
        {
            return BadRequest(ex.Message);
        }
    }
    /// <summary>
    /// Бронирования текущего пользователя с названиями отеля и типа номера.
    /// </summary>
    [HttpGet("my")]
    public async Task<ActionResult<IEnumerable<BookingDetails>>> GetMyBookings()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized("User ID not found in token");
        }

        var bookings = await _bookingService.GetBookingsWithDetailsAsync(int.Parse(userId));
        return Ok(bookings);
    }

    [HttpGet("all")]
    [Authorize(Roles = "Manager,Admin")]
    public async Task<ActionResult<IEnumerable<BookingResponseDto>>> GetAllBookings()
    {
        var bookings = await _bookingService.GetAllBookingsAsync();
        return Ok(bookings);
    }

    [HttpGet("details")]
    [Authorize(Roles = "Manager,Admin")]
    public async Task<ActionResult<IEnumerable<BookingDetails>>> GetBookingsWithDetails([FromQuery] int? userId, [FromQuery] int? hotelId, [FromQuery] int? status)
    {
        var bookings = await _bookingService.GetBookingsWithDetailsAsync(userId, hotelId, status);
        return Ok(bookings);
    }

    [HttpGet("user/{userId}/history")]
    public async Task<ActionResult<UserBookingHistory>> GetUserBookingHistory(int userId)
    {
        var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(currentUserId))
        {
            return Unauthorized("User ID not found in token");
        }

        if (!User.IsInRole("Manager") && !User.IsInRole("Admin") && userId != int.Parse(currentUserId))
        {
            return Forbid();
        }

        var history = await _bookingService.GetUserBookingHistoryAsync(userId);
        if (history == null)
        {
            return NotFound();
        }

        return Ok(history);
    }

    [HttpGet("active-details")]
    [Authorize(Roles = "Manager,Admin")]
    public async Task<ActionResult<IEnumerable<BookingSystem.Domain.DTOs.Booking.ActiveBookingDetailsDto>>> GetActiveBookingsWithDetails()
    {
        var bookings = await _bookingService.GetActiveBookingsWithDetailsAsync();
        return Ok(bookings);
    }
}
