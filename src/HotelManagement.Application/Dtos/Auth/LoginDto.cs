using System.ComponentModel.DataAnnotations;

namespace HotelManagement.Application.DTOs.Auth;

public class LoginDto
{
    [Required(ErrorMessage = "El usuario es obligatorio.")]
    [StringLength(60, ErrorMessage = "El usuario no puede superar 60 caracteres.")]
    public string Username { get; set; } = string.Empty;

    [Required(ErrorMessage = "La contraseña es obligatoria.")]
    [StringLength(128, ErrorMessage = "La contraseña no puede superar 128 caracteres.")]
    public string Password { get; set; } = string.Empty;
}