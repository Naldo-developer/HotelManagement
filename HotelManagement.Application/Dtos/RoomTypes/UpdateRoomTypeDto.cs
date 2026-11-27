using System.ComponentModel.DataAnnotations;

namespace HotelManagement.Application.Dtos.RoomTypes;

public class UpdateRoomTypeDto
{
    [Required, StringLength(80)]
    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    [Range(1, short.MaxValue)]
    public short MaxAdults { get; set; } = 1;

    [Range(0, short.MaxValue)]
    public short MaxChildren { get; set; }

    [Range(typeof(decimal), "0", "9999999999.99")]
    public decimal BaseRate { get; set; }
}