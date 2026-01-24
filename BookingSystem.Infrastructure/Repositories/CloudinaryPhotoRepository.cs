using BookingSystem.Domain.Interfaces;
using BookingSystem.Domain.Other;
using CloudinaryDotNet.Actions;
using CloudinaryDotNet;
using Microsoft.AspNetCore.Http;

namespace BookingSystem.Infrastructure.Repositories;

public class CloudinaryPhotoRepository : IPhotoRepository
{
    private readonly Cloudinary _cloudinary;
    private const string HotelFolderPrefix = "hotels/";
    
    public CloudinaryPhotoRepository(CloudinarySettings settings)
    {
        if (settings == null)
            throw new ArgumentNullException(nameof(settings));
            
        if (string.IsNullOrEmpty(settings.CloudName))
            throw new ArgumentException("Cloud name is required", nameof(settings.CloudName));
        if (string.IsNullOrEmpty(settings.ApiKey))
            throw new ArgumentException("API key is required", nameof(settings.ApiKey));
        if (string.IsNullOrEmpty(settings.ApiSecret))
            throw new ArgumentException("API secret is required", nameof(settings.ApiSecret));

        var account = new Account(settings.CloudName, settings.ApiKey, settings.ApiSecret);
        _cloudinary = new Cloudinary(account);
    }

    public CloudinaryPhotoRepository(Account account)
    {
        _cloudinary = new Cloudinary(account) ?? throw new ArgumentNullException(nameof(account));
    }

    public async Task<PhotoUploadResult> UploadPhotoAsync(IFormFile file, int hotelId, string description = null)
    {
        if (file == null || file.Length == 0)
            throw new ArgumentException("File is required and must not be empty", nameof(file));

        if (hotelId <= 0)
            throw new ArgumentException("Hotel ID must be a positive number", nameof(hotelId));
            
        try
        {
            var uploadParams = new ImageUploadParams
            {
                Folder = $"{HotelFolderPrefix}{hotelId}",
                File = new FileDescription(file.FileName, file.OpenReadStream()),
                PublicId = Path.GetFileNameWithoutExtension(file.FileName),
                Overwrite = true
            };

            if (!string.IsNullOrEmpty(description))
            {
                uploadParams.Context = new StringDictionary
                {
                    { "description", description }
                };
            }

            var uploadResult = await _cloudinary.UploadAsync(uploadParams);

            if (uploadResult.Error != null)
            {
                throw new Exception($"Failed to upload image: {uploadResult.Error.Message}");
            }

            return new PhotoUploadResult
            {
                PublicId = uploadResult.PublicId,
                Url = uploadResult.SecureUrl.AbsoluteUri,
                Format = uploadResult.Format
            };
        }
        catch (Exception ex)
        {
            throw new Exception($"Error uploading photo: {ex.Message}", ex);
        }
    }

    public async Task<IEnumerable<PhotoUploadResult>> UploadPhotosAsync(IEnumerable<IFormFile> files, int hotelId)
    {
        if (files == null || !files.Any())
            throw new ArgumentException("Files are required", nameof(files));

        if (hotelId <= 0)
            throw new ArgumentException("Hotel ID must be a positive number", nameof(hotelId));

        var results = new List<PhotoUploadResult>();

        foreach (var file in files)
        {
            var result = await UploadPhotoAsync(file, hotelId);
            results.Add(result);
        }

        return results;
    }

    public async Task<bool> DeletePhotoAsync(string publicId)
    {
        if (string.IsNullOrEmpty(publicId))
            throw new ArgumentException("Public ID is required", nameof(publicId));

        try
        {
            var deleteParams = new DeletionParams(publicId);
            var result = await _cloudinary.DestroyAsync(deleteParams);

            if (result.Error != null)
            {
                return false;
            }

            return result.Result == "ok";
        }
        catch (Exception ex)
        {
            throw new Exception($"Error deleting photo: {ex.Message}", ex);
        }
    }

    public async Task<string> GetImageUrlAsync(string publicId, string transformation = null)
    {
        if (string.IsNullOrEmpty(publicId))
            throw new ArgumentException("Public ID is required", nameof(publicId));

        try
        {
            Transformation transformationObj = null;
            if (!string.IsNullOrEmpty(transformation))
            {
                transformationObj = new Transformation().RawTransformation(transformation);
            }
            
            var urlBuilder = _cloudinary.Api.UrlImgUp;
            
            string url;
            if (transformationObj != null)
            {
                url = urlBuilder.Transform(transformationObj).BuildUrl(publicId);
            }
            else
            {
                url = urlBuilder.BuildUrl(publicId);
            }

            return await Task.FromResult(url);
        }
        catch (Exception ex)
        {
            throw new Exception($"Error generating image URL: {ex.Message}", ex);
        }
    }

    public async Task<IEnumerable<PhotoUploadResult>> GetHotelPhotosFromCloudinaryAsync(int hotelId, int maxResults = 100)
    {
        if (hotelId <= 0)
            throw new ArgumentException("Hotel ID must be a positive number", nameof(hotelId));
    
        if (maxResults <= 0)
            throw new ArgumentException("Max results must be a positive number", nameof(maxResults));
    
        try
        {
            string folderPath = $"{HotelFolderPrefix}{hotelId}/";
            var result = _cloudinary.ListResourcesByPrefix(
                prefix: folderPath,
                type: "upload"
            );
        
            if (result.Error != null)
            {
                throw new Exception($"Failed to retrieve images: {result.Error.Message}");
            }
            
            return result.Resources
                .Take(maxResults)
                .Select(resource => new PhotoUploadResult
                {
                    PublicId = resource.PublicId,
                    Url = resource.SecureUrl.AbsoluteUri
                })
                .ToList();
     
        }
        catch (Exception ex)
        {
            throw new Exception($"Error retrieving hotel photos: {ex.Message}", ex);
        }
    }

}