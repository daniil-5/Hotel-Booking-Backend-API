using BookingSystem.Domain.Other;
using Microsoft.AspNetCore.Http;

namespace BookingSystem.Application.Interfaces;

public interface IPhotoService
{
    Task<PhotoUploadResult> AddPhotoAsync(IFormFile file);
    Task<bool> DeletePhotoAsync(string publicId);
}
