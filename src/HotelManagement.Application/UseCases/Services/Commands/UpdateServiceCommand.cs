using System.ComponentModel.DataAnnotations;
using HotelManagement.Application.Dtos.Services;
using HotelManagement.Application.Exceptions;
using HotelManagement.Domain.Exceptions;
using HotelManagement.Domain.Repositories;
using MediatR;

namespace HotelManagement.Application.UseCases.Services.Commands;

public sealed class UpdateServiceCommand(
    long serviceId,
    UpdateServiceDto input) : IRequest<ServiceDto>
{
    public long ServiceId { get; } = serviceId;
    public UpdateServiceDto Input { get; } = input;
}

internal sealed class UpdateServiceCommandHandler(
    IServiceRepository serviceRepository)
    : IRequestHandler<UpdateServiceCommand, ServiceDto>
{
    public async Task<ServiceDto> Handle(
        UpdateServiceCommand request,
        CancellationToken cancellationToken)
    {
        if (request.ServiceId <= 0)
        {
            throw new BusinessValidationException(
                "El identificador del servicio no es válido.");
        }

        var errors = new List<ValidationResult>();

        if (!Validator.TryValidateObject(
                request.Input,
                new ValidationContext(request.Input),
                errors,
                validateAllProperties: true))
        {
            throw new BusinessValidationException(
                errors[0].ErrorMessage ??
                "Los datos del servicio no son válidos.");
        }

        var service = await serviceRepository.GetByIdAsync(
            request.ServiceId,
            cancellationToken);

        if (service is null)
        {
            throw new ResourceNotFoundException(
                "El servicio indicado no existe.");
        }

        service.Name = request.Input.Name.Trim();
        service.Description = request.Input.Description?.Trim();
        service.DefaultPrice = request.Input.DefaultPrice;
        service.Unit = request.Input.Unit.Trim();
        service.Active = request.Input.Active;

        var updated = await serviceRepository.UpdateAsync(
            service,
            cancellationToken);

        return ServiceMapping.ToDto(updated);
    }
}