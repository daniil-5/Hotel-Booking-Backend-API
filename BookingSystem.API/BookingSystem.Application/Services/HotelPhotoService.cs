using BookingSystem.Application.DTOs.HotelPhoto;
using System.Diagnostics.CodeAnalysis;
using BookingSystem.Application.Interfaces;
using BookingSystem.Domain.Entities;
using BookingSystem.Domain.Interfaces;
using Microsoft.AspNetCore.Http;

namespace BookingSystem.Application.Services;

public class HotelPhotoService : IHotelPhotoService
{
    private readonly IRepository<HotelPhoto> _hotelPhotoRepository;
    private readonly IPhotoRepository _cloudinaryRepository;

    public HotelPhotoService(
        IRepository<HotelPhoto> hotelPhotoRepository,
        IPhotoRepository cloudinaryRepository)
    {
        _hotelPhotoRepository = hotelPhotoRepository;
        _cloudinaryRepository = cloudinaryRepository;
    }

    public async Task<HotelPhotoDto> CreateHotelPhotoAsync(CreateHotelPhotoDto photoDto)
    {
        var hotelPhoto = new HotelPhoto
        {
            HotelId = photoDto.HotelId,
            Url = photoDto.Url,
            PublicId = photoDto.PublicId,
            Description = photoDto.Description,
            IsMain = photoDto.IsMain
        };

        await _hotelPhotoRepository.AddAsync(hotelPhoto);
        return MapToDto(hotelPhoto);
    }

    public async Task<HotelPhotoDto> UploadHotelPhotoAsync(IFormFile file, int hotelId, string? description = null, bool isMain = false)
    {
        if (file == null || file.Length == 0)
            throw new ArgumentException("File is required and must not be empty", nameof(file));

        if (hotelId <= 0)
            throw new ArgumentException("Hotel ID must be a positive number", nameof(hotelId));

        // Upload the photo to Cloudinary
        var uploadResult = await _cloudinaryRepository.UploadPhotoAsync(file, hotelId, description);

        // Create a new HotelPhoto entity and store it in the database
        var hotelPhoto = new HotelPhoto
        {
            HotelId = hotelId,
            Url = uploadResult.Url,
            PublicId = uploadResult.PublicId,
            Description = description ?? "No description",
            IsMain = isMain
        };

        await _hotelPhotoRepository.AddAsync(hotelPhoto);
        return MapToDto(hotelPhoto);
    }

    public async Task<IEnumerable<HotelPhotoDto>> UploadMultipleHotelPhotosAsync(IEnumerable<IFormFile> files, int hotelId)
    {
        if (files == null || !files.Any())
            throw new ArgumentException("Files are required", nameof(files));

        if (hotelId <= 0)
            throw new ArgumentException("Hotel ID must be a positive number", nameof(hotelId));

        var uploadResults = await _cloudinaryRepository.UploadPhotosAsync(files, hotelId);

        // Create HotelPhoto entities and store them in the database
        var hotelPhotos = new List<HotelPhoto>();

        foreach (var result in uploadResults)
        {
            var hotelPhoto = new HotelPhoto
            {
                HotelId = hotelId,
                Url = result.Url,
                PublicId = result.PublicId,
                Description = "No description",
                IsMain = false
            };

            await _hotelPhotoRepository.AddAsync(hotelPhoto);
            hotelPhotos.Add(hotelPhoto);
        }

        return hotelPhotos.Select(MapToDto).ToList();
    }

    public async Task<HotelPhotoDto> UpdateHotelPhotoAsync(UpdateHotelPhotoDto photoDto)
    {
        var existingPhoto = await _hotelPhotoRepository.GetByIdAsync(photoDto.Id);
        if (existingPhoto == null)
            throw new KeyNotFoundException($"Photo with ID {photoDto.Id} not found");

        existingPhoto.HotelId = photoDto.HotelId;
        existingPhoto.Url = photoDto.Url;
        existingPhoto.PublicId = photoDto.PublicId;
        existingPhoto.Description = photoDto.Description;
        existingPhoto.IsMain = photoDto.IsMain;

        await _hotelPhotoRepository.UpdateAsync(existingPhoto);
        return MapToDto(existingPhoto);
    }

    public async Task DeleteHotelPhotoAsync(int id)
    {
        var photo = await _hotelPhotoRepository.GetByIdAsync(id);
        if (photo == null)
            throw new KeyNotFoundException($"Photo with ID {id} not found");

        // Delete from Cloudinary first
        if (!string.IsNullOrEmpty(photo.PublicId))
        {
            var deleteResult = await _cloudinaryRepository.DeletePhotoAsync(photo.PublicId);
            if (!deleteResult)
            {
                throw new Exception($"Failed to delete photo with public ID {photo.PublicId} from Cloudinary");
            }
        }

        // Then delete from database
        await _hotelPhotoRepository.DeleteAsync(id);
    }

    public async Task<HotelPhotoDto> GetHotelPhotoByIdAsync(int id)
    {
        var photo = await _hotelPhotoRepository.GetByIdAsync(id);
        return photo != null ? MapToDto(photo) : null;
    }

    public async Task<IEnumerable<HotelPhotoDto>> GetAllHotelPhotosAsync()
    {
        var photos = await _hotelPhotoRepository.GetAllAsync();
        return photos.Select(MapToDto).ToList();
    }

    public async Task<IEnumerable<HotelPhotoDto>> GetPhotosByHotelIdAsync(int hotelId)
    {
        var photos = await _hotelPhotoRepository.GetAllAsync();
        return photos.Where(p => p.HotelId == hotelId && p.IsDeleted == false).Select(MapToDto).ToList();
    }

    public async Task<HotelPhotoDto> SetMainPhotoAsync(int photoId, int hotelId)
    {
        // Get all photos for this hotel
        var photos = await _hotelPhotoRepository.GetAllAsync();
        var hotelPhotos = hotelId > 0
            ? photos.Where(p => p.HotelId == hotelId).ToList()
            : photos.Where(p => p.Id == photoId).ToList();

        // Ensure the photo exists and belongs to the specified hotel
        var mainPhoto = hotelPhotos.FirstOrDefault(p => p.Id == photoId);
        if (mainPhoto == null)
            throw new KeyNotFoundException($"Photo with ID {photoId} not found for hotel {hotelId}");

        // Reset IsMain flag for all photos of this hotel
        foreach (var photo in hotelPhotos)
        {
            if (photo.IsMain)
            {
                photo.IsMain = false;
                await _hotelPhotoRepository.UpdateAsync(photo);
            }
        }

        // Set the new main photo
        mainPhoto.IsMain = true;
        await _hotelPhotoRepository.UpdateAsync(mainPhoto);

        return MapToDto(mainPhoto);
    }

    public async Task<string> GetTransformedImageUrlAsync(int photoId, string transformation)
    {
        var photo = await _hotelPhotoRepository.GetByIdAsync(photoId);
        if (photo == null)
            throw new KeyNotFoundException($"Photo with ID {photoId} not found");

        return await _cloudinaryRepository.GetImageUrlAsync(photo.PublicId, transformation);
    }

    [ExcludeFromCodeCoverage]
    public async Task SyncCloudinaryPhotosAsync(int hotelId)
    {
        // Get photos from Cloudinary
        var cloudinaryPhotos = await _cloudinaryRepository.GetHotelPhotosFromCloudinaryAsync(hotelId);

        // Get photos from database
        var dbPhotos = (await _hotelPhotoRepository.GetAllAsync()).Where(p => p.HotelId == hotelId).ToList();

        // Create lookup collections for faster comparison
        var cloudinaryPublicIds = cloudinaryPhotos.Select(p => p.PublicId).ToHashSet();
        var dbPublicIds = dbPhotos.Where(p => !string.IsNullOrEmpty(p.PublicId))
                                   .Select(p => p.PublicId)
                                   .ToHashSet();

        // PART 1: Upload photos that exist in DB but not in Cloudinary
        var photosToUpload = dbPhotos.Where(p => string.IsNullOrEmpty(p.PublicId) ||
                                             !cloudinaryPublicIds.Contains(p.PublicId))
                                     .ToList();

        foreach (var photoToUpload in photosToUpload)
        {
            try
            {
                // If we have a URL but no PublicId, the photo exists somewhere but not in Cloudinary
                if (!string.IsNullOrEmpty(photoToUpload.Url))
                {
                    // Download the image from its current URL
                    using (var httpClient = new HttpClient())
                    {
                        var imageBytes = await httpClient.GetByteArrayAsync(photoToUpload.Url);

                        // Create a memory stream from the downloaded image
                        using (var stream = new MemoryStream(imageBytes))
                        {
                            // Create a form file from the stream
                            var fileName = $"hotel_{hotelId}_photo_{photoToUpload.Id}.jpg";
                            var formFile = new FormFile(
                                baseStream: stream,
                                baseStreamOffset: 0,
                                length: stream.Length,
                                name: "file",
                                fileName: fileName
                            );

                            // Upload to Cloudinary
                            var uploadResult = await _cloudinaryRepository.UploadPhotoAsync(
                                formFile,
                                hotelId,
                                photoToUpload.Description
                            );

                            // Update the database record with Cloudinary information
                            photoToUpload.PublicId = uploadResult.PublicId;
                            photoToUpload.Url = uploadResult.Url;

                            await _hotelPhotoRepository.UpdateAsync(photoToUpload);
                        }
                    }
                }
            }
            catch (Exception)
            {
                // Continue with other photos
            }
        }

        // After uploading, refresh the Cloudinary photos list
        cloudinaryPhotos = await _cloudinaryRepository.GetHotelPhotosFromCloudinaryAsync(hotelId);
        cloudinaryPublicIds = cloudinaryPhotos.Select(p => p.PublicId).ToHashSet();

        // PART 2: Add photos that exist in Cloudinary but not in the database
        foreach (var cloudinaryPhoto in cloudinaryPhotos)
        {
            if (!dbPublicIds.Contains(cloudinaryPhoto.PublicId))
            {
                try
                {
                    // Add the missing photo to the database
                    var newPhoto = new HotelPhoto
                    {
                        HotelId = hotelId,
                        Url = cloudinaryPhoto.Url,
                        PublicId = cloudinaryPhoto.PublicId,
                        Description = null,
                        IsMain = false
                    };


                    await _hotelPhotoRepository.AddAsync(newPhoto);
                }
                catch (Exception)
                {
                    // Continue with other photos
                }
            }
        }

        // PART 3: Handle photos that are in DB but no longer exist in Cloudinary
        var photosToRemove = dbPhotos.Where(p => !string.IsNullOrEmpty(p.PublicId) &&
                                            !cloudinaryPublicIds.Contains(p.PublicId))
                                    .ToList();

        foreach (var photoToRemove in photosToRemove)
        {
            try
            {
                // Option 1: Remove the photo from the database
                // await _hotelPhotoRepository.DeleteAsync(photoToRemove.Id);

                // Option 2: Update the record to indicate it's missing from Cloudinary
                photoToRemove.PublicId = null;
                photoToRemove.Url = photoToRemove.Url + "?status=missing_from_cloudinary";
                await _hotelPhotoRepository.UpdateAsync(photoToRemove);
            }
            catch (Exception)
            {
                // Continue with other photos
            }
        }

        // PART 4: Verify all Cloudinary URLs match database URLs (to catch URL changes)
        var photosToUpdate = dbPhotos.Where(p =>
            !string.IsNullOrEmpty(p.PublicId) &&
            cloudinaryPublicIds.Contains(p.PublicId) &&
            cloudinaryPhotos.First(c => c.PublicId == p.PublicId).Url != p.Url
        ).ToList();

        foreach (var photoToUpdate in photosToUpdate)
        {
            try
            {
                // Update the URL to match Cloudinary
                var cloudinaryPhoto = cloudinaryPhotos.First(c => c.PublicId == photoToUpdate.PublicId);
                photoToUpdate.Url = cloudinaryPhoto.Url;
                await _hotelPhotoRepository.UpdateAsync(photoToUpdate);
            }
            catch (Exception)
            {
                // Continue with other photos
            }
        }
    }

    // Helper method to map from entity to DTO
    private static HotelPhotoDto MapToDto(HotelPhoto photo)
    {
        return new HotelPhotoDto
        {
            Id = photo.Id,
            HotelId = photo.HotelId,
            Url = photo.Url,
            PublicId = photo.PublicId,
            Description = photo.Description ?? "No description",
            IsMain = photo.IsMain,
            CreatedAt = photo.CreatedAt
        };
    }
}
