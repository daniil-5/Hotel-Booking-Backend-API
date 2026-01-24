using BookingSystem.Application.DTOs.HotelPhoto;
using Microsoft.AspNetCore.Http;

namespace BookingSystem.Application.Interfaces;

public interface IHotelPhotoService
{
    Task<HotelPhotoDto> CreateHotelPhotoAsync(CreateHotelPhotoDto photoDto);
    Task<HotelPhotoDto> UpdateHotelPhotoAsync(UpdateHotelPhotoDto photoDto);
    Task DeleteHotelPhotoAsync(int id);
    Task<HotelPhotoDto> UploadHotelPhotoAsync(IFormFile file, int hotelId, string description = null, bool isMain = false);
    Task<IEnumerable<HotelPhotoDto>> UploadMultipleHotelPhotosAsync(IEnumerable<IFormFile> files, int hotelId);
    Task<HotelPhotoDto> GetHotelPhotoByIdAsync(int id);
    Task<IEnumerable<HotelPhotoDto>> GetAllHotelPhotosAsync();
    Task SyncCloudinaryPhotosAsync(int hotelId);
    Task<string> GetTransformedImageUrlAsync(int photoId, string transformation);
    Task<HotelPhotoDto> SetMainPhotoAsync(int photoId, int hotelId);
    Task<IEnumerable<HotelPhotoDto>> GetPhotosByHotelIdAsync(int hotelId);
}
