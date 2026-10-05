using BookingSystem.Application.DTOs.HotelPhoto;
using System.Diagnostics.CodeAnalysis;
using BookingSystem.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BookingSystem.API.Controllers;


[Route("api/[controller]")]
[ApiController]
public class HotelPhotosController : ControllerBase
{
    private readonly IHotelPhotoService _photoService;
    private readonly ILogger<HotelPhotosController> _logger;

    public HotelPhotosController(
        IHotelPhotoService photoService,
        ILogger<HotelPhotosController> logger)
    {
        _photoService = photoService;
        _logger = logger;
    }

    /// <summary>
    /// Gets all photos in the system
    /// </summary>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [Authorize(Roles = "Manager,Admin")]
    public async Task<ActionResult<IEnumerable<HotelPhotoDto>>> GetAll()
    {
        try
        {
            var photos = await _photoService.GetAllHotelPhotosAsync();
            return Ok(photos);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving all photos");
            return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while retrieving all photos");
        }
    }
    /// <summary>
    /// Gets a photo by ID
    /// </summary>
    [HttpGet("{id}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<HotelPhotoDto>> GetById(int id)
    {
        try
        {
            var photo = await _photoService.GetHotelPhotoByIdAsync(id);
            if (photo == null)
                return NotFound($"Photo with ID {id} not found");

            return Ok(photo);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving photo with ID {PhotoId}", id);
            return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while retrieving the photo");
        }
    }

    /// <summary>
    /// Gets all photos for a specific hotel
    /// </summary>
    [HttpGet("hotel/{hotelId}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<HotelPhotoDto>>> GetByHotelId(int hotelId)
    {
        try
        {
            var photos = await _photoService.GetPhotosByHotelIdAsync(hotelId);
            return Ok(photos);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving photos for hotel with ID {HotelId}", hotelId);
            return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while retrieving the photos");
        }
    }

    /// <summary>
    /// Creates a new photo record manually
    /// </summary>
    [HttpPost]
    [Authorize(Roles = "Manager,Admin")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<HotelPhotoDto>> Create([FromBody] CreateHotelPhotoDto photoDto)
    {
        try
        {
            var createdPhoto = await _photoService.CreateHotelPhotoAsync(photoDto);
            return CreatedAtAction(nameof(GetById), new { id = createdPhoto.Id }, createdPhoto);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating photo for hotel with ID {HotelId}", photoDto.HotelId);
            return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while creating the photo");
        }
    }

    /// <summary>
    /// Uploads a single photo file to Cloudinary and creates a record
    /// </summary>
    [HttpPost("upload")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ExcludeFromCodeCoverage]
    public async Task<ActionResult<HotelPhotoDto>> UploadPhoto(
        IFormFile file,
        [FromQuery] int hotelId,
        [FromQuery] string? description = null,
        [FromQuery] bool isMain = false)
    {
        try
        {
            if (file == null || file.Length == 0)
                return BadRequest("No file was uploaded");

            var uploadedPhoto = await _photoService.UploadHotelPhotoAsync(file, hotelId, description, isMain);
            return CreatedAtAction(nameof(GetById), new { id = uploadedPhoto.Id }, uploadedPhoto);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error uploading photo for hotel with ID {HotelId}", hotelId);
            return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while uploading the photo");
        }
    }

    /// <summary>
    /// Uploads multiple photos to Cloudinary for a hotel
    /// </summary>
    [HttpPost("upload/multiple")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ExcludeFromCodeCoverage]
    public async Task<ActionResult<IEnumerable<HotelPhotoDto>>> UploadMultiplePhotos(
        [FromForm] List<IFormFile> files,
        [FromQuery] int hotelId)
    {
        try
        {
            if (files == null || files.Count == 0)
                return BadRequest("No files were uploaded");

            var uploadedPhotos = await _photoService.UploadMultipleHotelPhotosAsync(files, hotelId);
            return Ok(uploadedPhotos);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error uploading multiple photos for hotel with ID {HotelId}", hotelId);
            return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while uploading the photos");
        }
    }

    /// <summary>
    /// Updates an existing photo's metadata
    /// </summary>
    [HttpPut("{id}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<HotelPhotoDto>> Update(int id, [FromBody] UpdateHotelPhotoDto photoDto)
    {
        try
        {
            if (id != photoDto.Id)
                return BadRequest("ID in the URL must match the ID in the request body");

            var updatedPhoto = await _photoService.UpdateHotelPhotoAsync(photoDto);
            return Ok(updatedPhoto);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating photo with ID {PhotoId}", id);
            return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while updating the photo");
        }
    }

    /// <summary>
    /// Deletes a photo from both Cloudinary and the database
    /// </summary>
    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> Delete(int id)
    {
        try
        {
            await _photoService.DeleteHotelPhotoAsync(id);
            return NoContent();
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting photo with ID {PhotoId}", id);
            return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while deleting the photo");
        }
    }

    /// <summary>
    /// Sets a photo as the main photo for a hotel
    /// </summary>
    [HttpPut("{id}/set-main")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<HotelPhotoDto>> SetMainPhoto(int id, [FromQuery] int hotelId)
    {
        try
        {
            var mainPhoto = await _photoService.SetMainPhotoAsync(id, hotelId);
            return Ok(mainPhoto);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error setting main photo with ID {PhotoId} for hotel {HotelId}", id, hotelId);
            return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while setting the main photo");
        }
    }

    /// <summary>
    /// Gets a URL for a transformed image
    /// </summary>
    [HttpGet("{id}/transform")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<string>> GetTransformedImageUrl(int id, [FromQuery] string transformation)
    {
        try
        {
            var url = await _photoService.GetTransformedImageUrlAsync(id, transformation);
            return Ok(url);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting transformed URL for photo with ID {PhotoId}", id);
            return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while getting the transformed image URL");
        }
    }

    /// <summary>
    /// Syncs photos between Cloudinary and the database for a hotel
    /// </summary>
    [HttpPost("sync/{hotelId}")]
    [Authorize(Roles = "Manager,Admin")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult> SyncPhotos(int hotelId)
    {
        try
        {
            await _photoService.SyncCloudinaryPhotosAsync(hotelId);
            return Ok($"Successfully synchronized photos for hotel with ID {hotelId}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error syncing photos for hotel with ID {HotelId}", hotelId);
            return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while synchronizing photos");
        }
    }
}
