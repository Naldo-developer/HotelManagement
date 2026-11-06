using System.ComponentModel.DataAnnotations;

namespace HotelManagement.Application.DTOs.Guests;

//Filtros de búsqueda de huéspedes con paginación.
public class GuestFilterDto
{
    //Texto a buscar en nombre o apellido.
    [StringLength(100, ErrorMessage = "El nombre a buscar admite hasta 100 caracteres.")]
    public string? Name { get; set; }

    [StringLength(20, ErrorMessage = "El tipo de documento admite hasta 20 caracteres.")]
    public string? DocumentType { get; set; }

    [StringLength(30, ErrorMessage = "El número de documento admite hasta 30 caracteres.")]
    public string? DocumentNumber { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "La página mínima es 1.")]
    public int Page { get; set; } = 1;

    [Range(1, 100, ErrorMessage = "El tamaño de página debe estar entre 1 y 100.")]
    public int PageSize { get; set; } = 20;
}