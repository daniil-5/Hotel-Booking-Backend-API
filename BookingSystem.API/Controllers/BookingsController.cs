using System.Security.Claims;
using BookingSystem.Application.DTOs.Booking;
using BookingSystem.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

[Route("api/bookings")]
[ApiController]
[Authorize]
public class BookingsController : ControllerBase
{
    private readonly IBookingService _bookingService;

    public BookingsController(IBookingService bookingService)
    {
        _bookingService = bookingService;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<BookingResponseDto>>> GetBookings()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized("User ID not found in token");
        }

        if (User.IsInRole("Manager") || User.IsInRole("Admin"))
        {
            var allBookings = await _bookingService.GetAllBookingsAsync();
            return Ok(allBookings);
        }
        else
        {
            var userBookings = await _bookingService.GetBookingsByUserIdAsync(int.Parse(userId));
            return Ok(userBookings);
        }
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<BookingResponseDto>> GetBooking(int id)
    {
        var booking = await _bookingService.GetBookingByIdAsync(id);

        if (booking == null)
            return NotFound();

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized("User ID not found in token");
        }

        if (User.IsInRole("Manager") || User.IsInRole("Admin"))
        {
            return Ok(booking);
        }

        if (booking.UserId != int.Parse(userId))
        {
            return Forbid();
        }

        return Ok(booking);
    }

    [HttpPost]
    public async Task<IActionResult> CreateBooking(CreateBookingDto bookingDto)
    {
        try
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized("User ID not found in token");
            }

            bookingDto.UserId = int.Parse(userId);

            var trackingId = await _bookingService.CreateBookingAsync(bookingDto);

            return Accepted(new
            {
                TrackingId = trackingId,
                Message = "Booking request submitted successfully. Please check status later.",
                StatusUrl = $"/api/bookings/status/{trackingId}"
            });
        }
        catch (Exception ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpGet("status/{trackingId}")]
    public async Task<IActionResult> GetBookingStatus(Guid trackingId)
    {
        var booking = await _bookingService.GetBookingByTrackingIdAsync(trackingId);
        Console.WriteLine(trackingId);

        if (booking == null)
        {
            return Ok(new { Status = "Processing", TrackingId = trackingId });
        }

        return Ok(new { Status = "Confirmed", Booking = booking });
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateBooking(int id, UpdateBookingDto bookingDto)
    {
        try
        {
            if (id != bookingDto.Id)
                return BadRequest("ID mismatch");

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized("User ID not found in token");
            }

            var existingBooking = await _bookingService.GetBookingByIdAsync(id);
            if (existingBooking == null)
                return NotFound();

            if (!User.IsInRole("Manager") && !User.IsInRole("Admin"))
            {
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
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized("User ID not found in token");
            }

            var existingBooking = await _bookingService.GetBookingByIdAsync(id);
            if (existingBooking == null)
                return NotFound();

            if (!User.IsInRole("Manager") && !User.IsInRole("Admin"))
            {
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
}
