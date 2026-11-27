using HotelManagement.Application.Transactions;
using System.ComponentModel.DataAnnotations;
using HotelManagement.Application.Dtos.Services;
using HotelManagement.Application.Mappings;
using HotelManagement.Domain.Entities;
using HotelManagement.Domain.Exceptions;
using HotelManagement.Domain.Repositories;
using MediatR;

namespace HotelManagement.Application.UseCases.Services.Commands;

public sealed class CreateServiceCommand(
    CreateServiceDto input) : IRequest<ServiceDto>, ITransactionalRequest
{
    public CreateServiceDto Input { get; } = input;
}

internal sealed class CreateServiceCommandHandler(
    IServiceRepository serviceRepository)
    : IRequestHandler<CreateServiceCommand, ServiceDto>
{
    public async Task<ServiceDto> Handle(
        CreateServiceCommand request,
        CancellationToken cancellationToken)
    {
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

        var service = new Service
        {
            HotelId = request.Input.HotelId,
            Name = request.Input.Name.Trim(),
            Description = request.Input.Description?.Trim(),
            DefaultPrice = request.Input.DefaultPrice,
            Unit = request.Input.Unit.Trim(),
            Active = true
        };

        var created = await serviceRepository.CreateAsync(
            service,
            cancellationToken);

        return ServiceMapping.ToDto(created);
    }
}