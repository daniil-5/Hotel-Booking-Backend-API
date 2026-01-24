using BookingSystem.Application.DTOs.HotelPhoto;
using BookingSystem.Application.Interfaces;
using BookingSystem.Application.Mappers;
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
        var hotelPhoto = photoDto.ToEntity();
        await _hotelPhotoRepository.AddAsync(hotelPhoto);
        return hotelPhoto.ToDto();
    }

    public async Task<HotelPhotoDto> UploadHotelPhotoAsync(IFormFile file, int hotelId, string description = null, bool isMain = false)
    {
        if (file == null || file.Length == 0)
            throw new ArgumentException("File is required and must not be empty", nameof(file));

        if (hotelId <= 0)
            throw new ArgumentException("Hotel ID must be a positive number", nameof(hotelId));

        var uploadResult = await _cloudinaryRepository.UploadPhotoAsync(file, hotelId, description);

        var hotelPhoto = new HotelPhoto
        {
            HotelId = hotelId,
            Url = uploadResult.Url,
            PublicId = uploadResult.PublicId,
            Description = description ?? "No description",
            IsMain = isMain
        };

        await _hotelPhotoRepository.AddAsync(hotelPhoto);
        return hotelPhoto.ToDto();
    }

    public async Task<IEnumerable<HotelPhotoDto>> UploadMultipleHotelPhotosAsync(IEnumerable<IFormFile> files, int hotelId)
    {
        if (files == null || !files.Any())
            throw new ArgumentException("Files are required", nameof(files));

        if (hotelId <= 0)
            throw new ArgumentException("Hotel ID must be a positive number", nameof(hotelId));

        var uploadResults = await _cloudinaryRepository.UploadPhotosAsync(files, hotelId);

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

        return hotelPhotos.Select(p => p.ToDto()).ToList();
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
        return existingPhoto.ToDto();
    }

    public async Task DeleteHotelPhotoAsync(int id)
    {
        var photo = await _hotelPhotoRepository.GetByIdAsync(id);
        if (photo == null)
            throw new KeyNotFoundException($"Photo with ID {id} not found");

        if (!string.IsNullOrEmpty(photo.PublicId))
        {
            var deleteResult = await _cloudinaryRepository.DeletePhotoAsync(photo.PublicId);
            if (!deleteResult)
            {
                throw new Exception($"Failed to delete photo with public ID {photo.PublicId} from Cloudinary");
            }
        }

        await _hotelPhotoRepository.DeleteAsync(id);
    }

    public async Task<HotelPhotoDto> GetHotelPhotoByIdAsync(int id)
    {
        var photo = await _hotelPhotoRepository.GetByIdAsync(id);
        return photo?.ToDto();
    }

    public async Task<IEnumerable<HotelPhotoDto>> GetAllHotelPhotosAsync()
    {
        var photos = await _hotelPhotoRepository.GetAllAsync();
        return photos.Select(p => p.ToDto()).ToList();
    }

    public async Task<IEnumerable<HotelPhotoDto>> GetPhotosByHotelIdAsync(int hotelId)
    {
        var photos = await _hotelPhotoRepository.GetAllAsync();
        return photos.Where(p => p.HotelId == hotelId && p.IsDeleted == false).Select(p => p.ToDto()).ToList();
    }

    public async Task<HotelPhotoDto> SetMainPhotoAsync(int photoId, int hotelId)
    {
        var photos = await _hotelPhotoRepository.GetAllAsync();
        var hotelPhotos = photos.Where(p => p.HotelId == hotelId).ToList();

        var mainPhoto = hotelPhotos.FirstOrDefault(p => p.Id == photoId);
        if (mainPhoto == null)
            throw new KeyNotFoundException($"Photo with ID {photoId} not found for hotel {hotelId}");

        foreach (var photo in hotelPhotos)
        {
            if (photo.IsMain)
            {
                photo.IsMain = false;
                await _hotelPhotoRepository.UpdateAsync(photo);
            }
        }

        mainPhoto.IsMain = true;
        await _hotelPhotoRepository.UpdateAsync(mainPhoto);

        return mainPhoto.ToDto();
    }

    public async Task<string> GetTransformedImageUrlAsync(int photoId, string transformation)
    {
        var photo = await _hotelPhotoRepository.GetByIdAsync(photoId);
        if (photo == null)
            throw new KeyNotFoundException($"Photo with ID {photoId} not found");

        return await _cloudinaryRepository.GetImageUrlAsync(photo.PublicId, transformation);
    }

    public async Task SyncCloudinaryPhotosAsync(int hotelId)
    {
        var cloudinaryPhotos = await _cloudinaryRepository.GetHotelPhotosFromCloudinaryAsync(hotelId);

        var dbPhotos = (await _hotelPhotoRepository.GetAllAsync()).Where(p => p.HotelId == hotelId).ToList();

        var cloudinaryPublicIds = cloudinaryPhotos.Select(p => p.PublicId).ToHashSet();
        var dbPublicIds = dbPhotos.Where(p => !string.IsNullOrEmpty(p.PublicId))
                                   .Select(p => p.PublicId)
                                   .ToHashSet();

        var photosToUpload = dbPhotos.Where(p => string.IsNullOrEmpty(p.PublicId) ||
                                             !cloudinaryPublicIds.Contains(p.PublicId))
                                     .ToList();

        foreach (var photoToUpload in photosToUpload)
        {
            try
            {
                if (!string.IsNullOrEmpty(photoToUpload.Url))
                {
                    using (var httpClient = new HttpClient())
                    {
                        var imageBytes = await httpClient.GetByteArrayAsync(photoToUpload.Url);

                        using (var stream = new MemoryStream(imageBytes))
                        {
                            var fileName = $"hotel_{hotelId}_photo_{photoToUpload.Id}.jpg";
                            var formFile = new FormFile(
                                baseStream: stream,
                                baseStreamOffset: 0,
                                length: stream.Length,
                                name: "file",
                                fileName: fileName
                            );

                            var uploadResult = await _cloudinaryRepository.UploadPhotoAsync(
                                formFile,
                                hotelId,
                                photoToUpload.Description
                            );

                            photoToUpload.PublicId = uploadResult.PublicId;
                            photoToUpload.Url = uploadResult.Url;

                            await _hotelPhotoRepository.UpdateAsync(photoToUpload);
                        }
                    }
                }
            }
            catch (Exception)
            {
            }
        }

        cloudinaryPhotos = await _cloudinaryRepository.GetHotelPhotosFromCloudinaryAsync(hotelId);
        cloudinaryPublicIds = cloudinaryPhotos.Select(p => p.PublicId).ToHashSet();

        foreach (var cloudinaryPhoto in cloudinaryPhotos)
        {
            if (!dbPublicIds.Contains(cloudinaryPhoto.PublicId))
            {
                try
                {
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
                }
            }
        }

        var photosToRemove = dbPhotos.Where(p => !string.IsNullOrEmpty(p.PublicId) &&
                                            !cloudinaryPublicIds.Contains(p.PublicId))
                                    .ToList();

        foreach (var photoToRemove in photosToRemove)
        {
            try
            {
                photoToRemove.PublicId = null;
                photoToRemove.Url = photoToRemove.Url + "?status=missing_from_cloudinary";
                await _hotelPhotoRepository.UpdateAsync(photoToRemove);
            }
            catch (Exception)
            {
            }
        }

        var photosToUpdate = dbPhotos.Where(p =>
            !string.IsNullOrEmpty(p.PublicId) &&
            cloudinaryPublicIds.Contains(p.PublicId) &&
            cloudinaryPhotos.First(c => c.PublicId == p.PublicId).Url != p.Url
        ).ToList();

        foreach (var photoToUpdate in photosToUpdate)
        {
            try
            {
                var cloudinaryPhoto = cloudinaryPhotos.First(c => c.PublicId == photoToUpdate.PublicId);
                photoToUpdate.Url = cloudinaryPhoto.Url;
                await _hotelPhotoRepository.UpdateAsync(photoToUpdate);
            }
            catch (Exception)
            {
            }
        }
    }
}
