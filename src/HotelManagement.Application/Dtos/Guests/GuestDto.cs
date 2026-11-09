namespace HotelManagement.Application.DTOs.Guests;

//Respuesta con los datos de un huésped.
public class GuestDto
{
    public long Id { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string? DocumentType { get; set; }
    public string? DocumentNumber { get; set; }
    public string? Email { get; set; }
    public string? Phone { get; set; }
}