using BookingSystem.Application.DTOs.Amenity;
using BookingSystem.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BookingSystem.API.Controllers;

[Route("api/amenities")]
[ApiController]
public class AmenitiesController : ControllerBase
{
    private readonly IAmenityService _amenityService;

    public AmenitiesController(IAmenityService amenityService)
    {
        _amenityService = amenityService;
    }

    [HttpGet]
    [AllowAnonymous]
    public async Task<ActionResult<IEnumerable<AmenityDto>>> GetAmenities()
    {
        var amenities = await _amenityService.GetAllAmenitiesAsync();
        return Ok(amenities);
    }

    [HttpGet("{id}")]
    [AllowAnonymous]
    public async Task<ActionResult<AmenityDto>> GetAmenity(int id)
    {
        var amenity = await _amenityService.GetAmenityByIdAsync(id);
        if (amenity == null)
        {
            return NotFound($"Amenity with ID {id} not found");
        }
        return Ok(amenity);
    }

    [HttpPost]
    [Authorize(Roles = "Manager,Admin")]
    public async Task<ActionResult<AmenityDto>> CreateAmenity(CreateAmenityDto amenityDto)
    {
        try
        {
            var createdAmenity = await _amenityService.CreateAmenityAsync(amenityDto);
            return CreatedAtAction(nameof(GetAmenity), new { id = createdAmenity.Id }, createdAmenity);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpPut("{id}")]
    [Authorize(Roles = "Manager,Admin")]
    public async Task<ActionResult<AmenityDto>> UpdateAmenity(int id, UpdateAmenityDto amenityDto)
    {
        if (id != amenityDto.Id)
        {
            return BadRequest("ID mismatch");
        }

        try
        {
            var updatedAmenity = await _amenityService.UpdateAmenityAsync(amenityDto);
            return Ok(updatedAmenity);
        }
        catch (KeyNotFoundException)
        {
            return NotFound($"Amenity with ID {id} not found");
        }
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = "Manager,Admin")]
    public async Task<IActionResult> DeleteAmenity(int id)
    {
        try
        {
            await _amenityService.DeleteAmenityAsync(id);
            return NoContent();
        }
        catch (KeyNotFoundException)
        {
            return NotFound($"Amenity with ID {id} not found");
        }
    }
}
