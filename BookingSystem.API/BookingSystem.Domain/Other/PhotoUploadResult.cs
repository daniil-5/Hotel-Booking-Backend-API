namespace BookingSystem.Domain.Other;

public class PhotoUploadResult
{
    public required string PublicId { get; set; }
    public required string Url { get; set; }
    public required string Format { get; set; }
    public required long Bytes { get; set; }
}
