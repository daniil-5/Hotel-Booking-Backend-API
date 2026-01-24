using BookingSystem.Application.DTOs.RoomType;
using BookingSystem.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BookingSystem.API.Controllers;

[Route("api/room_types")]
[ApiController]
public class RoomTypesController : ControllerBase
{
    private readonly IRoomTypeService _roomTypeService;

    public RoomTypesController(IRoomTypeService roomTypeService)
    {
        _roomTypeService = roomTypeService;
    }

    [HttpGet]
    [AllowAnonymous]
    public async Task<ActionResult<IEnumerable<RoomTypeDto>>> GetAllRoomTypes()
    {
        try
        {
            var roomTypes = await _roomTypeService.GetAllRoomTypesAsync();
            return Ok(roomTypes);
        }
        catch (Exception ex)
        {
            return StatusCode(500, ex.Message);
        }
    }

    [HttpGet("{id:int}")]
    [AllowAnonymous]
    public async Task<ActionResult<RoomTypeDto>> GetRoomType(int id)
    {
        try
        {
            var roomType = await _roomTypeService.GetRoomTypeByIdAsync(id);
            return roomType != null ? Ok(roomType) : NotFound($"Room type with ID {id} not found");
        }
        catch (Exception ex)
        {
            return StatusCode(500, ex.Message);
        }
    }

    [HttpGet("by_hotel/{hotelId}")]
    [AllowAnonymous]
    public async Task<ActionResult<IEnumerable<RoomTypeDto>>> GetRoomTypesByHotelId(int hotelId)
    {
        try
        {
            var roomTypes = await _roomTypeService.GetRoomTypesByHotelIdAsync(hotelId);
            return Ok(roomTypes);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ex.Message);
        }
        catch (Exception ex)
        {
            return StatusCode(500, ex.Message);
        }
    }

    [HttpPost]
    [Authorize(Roles = "Manager,Admin")]
    public async Task<ActionResult<RoomTypeDto>> CreateRoomType(CreateRoomTypeDto roomTypeDto)
    {
        try
        {
            var createdRoomType = await _roomTypeService.CreateRoomTypeAsync(roomTypeDto);
            return CreatedAtAction(nameof(GetRoomType), new { id = createdRoomType.Id }, createdRoomType);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ex.Message);
        }
        catch (Exception ex)
        {
            return StatusCode(500, ex.Message);
        }
    }

    [HttpPut("{id}")]
    [Authorize(Roles = "Manager,Admin")]
    public async Task<IActionResult> UpdateRoomType(int id, UpdateRoomTypeDto roomTypeDto)
    {
        if (id != roomTypeDto.Id)
        {
            return BadRequest("ID mismatch");
        }
        
        try
        {
            var updatedRoomType = await _roomTypeService.UpdateRoomTypeAsync(roomTypeDto);
            return Ok(updatedRoomType);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ex.Message);
        }
        catch (Exception ex)
        {
            return StatusCode(500, ex.Message);
        }
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = "Manager,Admin")]
    public async Task<IActionResult> DeleteRoomType(int id)
    {
        try
        {
            await _roomTypeService.DeleteRoomTypeAsync(id);
            return NoContent();
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ex.Message);
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(ex.Message);
        }
        catch (Exception ex)
        {
            return StatusCode(500, ex.Message);
        }
    }
}