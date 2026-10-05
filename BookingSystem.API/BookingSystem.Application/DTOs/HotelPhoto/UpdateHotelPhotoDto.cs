namespace BookingSystem.Application.DTOs.HotelPhoto;

public class UpdateHotelPhotoDto
{
    public int Id { get; set; }
    public int HotelId { get; set; }
    public string Url { get; set; } = string.Empty;

    public string PublicId { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public bool IsMain { get; set; }
}
