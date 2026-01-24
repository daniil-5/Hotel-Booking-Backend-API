namespace BookingSystem.Application.DTOs.HotelPhoto;

public class HotelPhotoDto
{
    public int Id { get; set; }
    public int HotelId { get; set; }
    public string Url { get; set; }

    public string PublicId { get; set; }
    public string Description { get; set; }
    public bool IsMain { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}
