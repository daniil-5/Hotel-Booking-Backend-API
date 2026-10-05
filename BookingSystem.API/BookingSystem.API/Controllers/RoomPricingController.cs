using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using System.Collections.Generic;
using System.Threading.Tasks;
using BookingSystem.Application.DTOs.RoomPricing;
using BookingSystem.Application.Interfaces;
using BookingSystem.Application.Services;

namespace BookingSystem.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class RoomPricingsController : ControllerBase
    {
        private readonly IRoomPricingService _roomPricingService;

        public RoomPricingsController(IRoomPricingService roomPricingService)
        {
            _roomPricingService = roomPricingService;
        }

        /// <summary>
        /// Get all room pricing records
        /// </summary>
        /// <returns>A collection of room pricing records</returns>
        [HttpGet]
        [ProducesResponseType(typeof(IEnumerable<RoomPricingDto>), StatusCodes.Status200OK)]
        public async Task<ActionResult<IEnumerable<RoomPricingDto>>> GetAll()
        {
            var pricings = await _roomPricingService.GetAllRoomPricingsAsync();
            return Ok(pricings);
        }

        /// <summary>
        /// Get a room pricing record by ID
        /// </summary>
        /// <param name="id">The ID of the room pricing record</param>
        /// <returns>The room pricing record</returns>
        [HttpGet("{id}")]
        [ProducesResponseType(typeof(RoomPricingDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<RoomPricingDto>> GetById(int id)
        {
            var pricing = await _roomPricingService.GetRoomPricingByIdAsync(id);
            if (pricing == null)
            {
                return NotFound();
            }
            return Ok(pricing);
        }

        /// <summary>
        /// Create a new room pricing record
        /// </summary>
        /// <param name="pricingDto">The room pricing data</param>
        /// <returns>The newly created room pricing record</returns>
        [HttpPost]
        [Authorize(Roles = "Admin,Manager")]
        public async Task<ActionResult<RoomPricingDto>> Create([FromBody] CreateRoomPricingDto pricingDto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            try
            {
                var createdPricing = await _roomPricingService.CreateRoomPricingAsync(pricingDto);

                return CreatedAtAction(
                    nameof(GetById),
                    new { id = createdPricing.Id },
                    createdPricing
                );
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(ex.Message);
            }
        }

        /// <summary>
        /// Update an existing room pricing record
        /// </summary>
        /// <param name="id">The ID of the room pricing record to update</param>
        /// <param name="pricingDto">The updated room pricing data</param>
        /// <returns>The updated room pricing record</returns>
        [HttpPut("{id}")]
        [Authorize(Roles = "Admin,Manager")]
        public async Task<ActionResult<RoomPricingDto>> Update(int id, [FromBody] UpdateRoomPricingDto pricingDto)
        {
            if (id != pricingDto.Id)
            {
                return BadRequest("ID in URL does not match ID in request body");
            }

            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            try
            {
                var updatedPricing = await _roomPricingService.UpdateRoomPricingAsync(pricingDto);
                return Ok(updatedPricing);
            }
            catch (KeyNotFoundException)
            {
                return NotFound($"Room pricing with ID {id} not found");
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"An error occurred while updating room pricing: {ex.Message}");
            }
        }

        /// <summary>
        /// Delete a room pricing record
        /// </summary>
        /// <param name="id">The ID of the room pricing record to delete</param>
        /// <returns>No content</returns>
        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin,Manager")]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                await _roomPricingService.DeleteRoomPricingAsync(id);
                return NoContent();
            }
            catch (KeyNotFoundException)
            {
                return NotFound($"Room pricing with ID {id} not found");
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"An error occurred while deleting room pricing: {ex.Message}");
            }
        }
    }
}
