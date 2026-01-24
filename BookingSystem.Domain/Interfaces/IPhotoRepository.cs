using BookingSystem.Domain.Other;
using Microsoft.AspNetCore.Http;

namespace BookingSystem.Domain.Interfaces;

public interface IPhotoRepository
{
    Task<PhotoUploadResult> UploadPhotoAsync(IFormFile file, int hotelId, string description = null);
    Task<IEnumerable<PhotoUploadResult>> UploadPhotosAsync(IEnumerable<IFormFile> files, int hotelId);
    Task<bool> DeletePhotoAsync(string publicId);
    Task<string> GetImageUrlAsync(string publicId, string transformation = null);
    Task<IEnumerable<PhotoUploadResult>> GetHotelPhotosFromCloudinaryAsync(int hotelId, int maxResults = 100);
}