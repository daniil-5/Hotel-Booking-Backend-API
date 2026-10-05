using BookingSystem.Domain.Entities;
using BookingSystem.Domain.Other;
using Microsoft.AspNetCore.Http;

namespace BookingSystem.Domain.Interfaces;

public interface IPhotoRepository
{
    // Upload operations
    Task<PhotoUploadResult> UploadPhotoAsync(IFormFile file, int hotelId, string description = null);
    Task<IEnumerable<PhotoUploadResult>> UploadPhotosAsync(IEnumerable<IFormFile> files, int hotelId);
    Task<bool> CreateHotelDirectoryAsync(int hotelId, string hotelName);

    // Management operations
    Task<bool> DeletePhotoAsync(string publicId);

    // Transformation/URL operations
    Task<string> GetImageUrlAsync(string publicId, string transformation = null);
    Task<IEnumerable<PhotoUploadResult>> GetHotelPhotosFromCloudinaryAsync(int hotelId, int maxResults = 100);
}
