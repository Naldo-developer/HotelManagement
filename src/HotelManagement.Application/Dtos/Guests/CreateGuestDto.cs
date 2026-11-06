using System.ComponentModel.DataAnnotations;

namespace HotelManagement.Application.DTOs.Guests;

/// Datos para registrar un huésped.
/// Regla del documento: si se informa DocumentType o DocumentNumber,
/// deben informarse ambos. Si no se informa ninguno, el huésped se registra sin documento.
public class CreateGuestDto : IValidatableObject
{
    [Required(ErrorMessage = "El nombre es obligatorio.")]
    [StringLength(80, ErrorMessage = "El nombre admite hasta 80 caracteres.")]
    public string FirstName { get; set; } = string.Empty;

    [Required(ErrorMessage = "El apellido es obligatorio.")]
    [StringLength(100, ErrorMessage = "El apellido admite hasta 100 caracteres.")]
    public string LastName { get; set; } = string.Empty;

    [StringLength(20, ErrorMessage = "El tipo de documento admite hasta 20 caracteres.")]
    public string? DocumentType { get; set; }

    [StringLength(30, ErrorMessage = "El número de documento admite hasta 30 caracteres.")]
    public string? DocumentNumber { get; set; }

    [StringLength(120, ErrorMessage = "El correo admite hasta 120 caracteres.")]
    [EmailAddress(ErrorMessage = "El correo no tiene un formato válido.")]
    public string? Email { get; set; }

    [StringLength(30, ErrorMessage = "El teléfono admite hasta 30 caracteres.")]
    public string? Phone { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        var hasType = !string.IsNullOrWhiteSpace(DocumentType);
        var hasNumber = !string.IsNullOrWhiteSpace(DocumentNumber);

        if (hasType && !hasNumber)
        {
            yield return new ValidationResult(
                "Si se informa el tipo de documento, también debe informarse el número.",
                new[] { nameof(DocumentNumber) });
        }
        else if (!hasType && hasNumber)
        {
            yield return new ValidationResult(
                "Si se informa el número de documento, también debe informarse el tipo.",
                new[] { nameof(DocumentType) });
        }
    }
}