using BookingSystem.Application.DTOs.RoomPricing;
using BookingSystem.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BookingSystem.Api.Controllers
{
    [ApiController]
    [Route("api/room_pricings")]
    public class RoomPricingsController : ControllerBase
    {
        private readonly IRoomPricingService _roomPricingService;

        public RoomPricingsController(IRoomPricingService roomPricingService)
        {
            _roomPricingService = roomPricingService;
        }
        
        [HttpGet]
        [ProducesResponseType(typeof(IEnumerable<RoomPricingDto>), StatusCodes.Status200OK)]
        public async Task<ActionResult<IEnumerable<RoomPricingDto>>> GetAll()
        {
            var pricings = await _roomPricingService.GetAllRoomPricingsAsync();
            return Ok(pricings);
        }
        
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
        
        [HttpPost]
        [Authorize(Roles = "Admin,Manager")]
        [ProducesResponseType(typeof(RoomPricingDto), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<RoomPricingDto>> Create([FromBody] CreateRoomPricingDto pricingDto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var createdPricing = await _roomPricingService.CreateRoomPricingAsync(pricingDto);
            
            return CreatedAtAction(
                nameof(GetById), 
                new { id = createdPricing.Id }, 
                createdPricing
            );
        }

        [HttpPut("{id}")]
        [Authorize(Roles = "Admin,Manager")]
        [ProducesResponseType(typeof(RoomPricingDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
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
        
        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin,Manager")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
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