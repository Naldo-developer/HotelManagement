using System.ComponentModel.DataAnnotations;
using HotelManagement.Application.Dtos.Payments;
using HotelManagement.Domain.Exceptions;
using HotelManagement.Domain.Repositories;
using MediatR;

namespace HotelManagement.Application.UseCases.Payments.Commands;

public sealed class CreatePaymentCommand(
    CreatePaymentDto input,
    long userId) : IRequest<PaymentDto>
{
    public CreatePaymentDto Input { get; } = input;
    public long UserId { get; } = userId;
}

internal sealed class CreatePaymentCommandHandler(
    IPaymentRepository paymentRepository)
    : IRequestHandler<CreatePaymentCommand, PaymentDto>
{
    public async Task<PaymentDto> Handle(
        CreatePaymentCommand request,
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
                errors[0].ErrorMessage ?? "Los datos del pago no son válidos.");
        }

        if (request.UserId <= 0)
        {
            throw new BusinessValidationException(
                "El usuario responsable no es válido.");
        }

        var payment = await paymentRepository.CreateAsync(
            request.Input.ReservationId,
            request.Input.Amount,
            request.Input.PaymentMethod,
            request.UserId,
            request.Input.Reference,
            request.Input.Notes,
            cancellationToken);

        return PaymentMapping.ToDto(payment);
    }
}