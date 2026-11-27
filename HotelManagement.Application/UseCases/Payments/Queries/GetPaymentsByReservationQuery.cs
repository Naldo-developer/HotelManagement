using HotelManagement.Application.Dtos.Payments;
using HotelManagement.Application.Mappings;
using HotelManagement.Domain.Exceptions;
using HotelManagement.Domain.Repositories;
using MediatR;

namespace HotelManagement.Application.UseCases.Payments.Queries;

public sealed record GetPaymentsByReservationQuery(
    long ReservationId) : IRequest<List<PaymentDto>>;

internal sealed class GetPaymentsByReservationQueryHandler(
    IPaymentRepository paymentRepository)
    : IRequestHandler<
        GetPaymentsByReservationQuery,
        List<PaymentDto>>
{
    public async Task<List<PaymentDto>> Handle(
        GetPaymentsByReservationQuery request,
        CancellationToken cancellationToken)
    {
        if (request.ReservationId <= 0)
        {
            throw new BusinessValidationException(
                "El identificador de la reserva debe ser mayor que cero.");
        }

        var payments = await paymentRepository.GetByReservationIdAsync(
            request.ReservationId,
            cancellationToken);

        return payments
            .Select(PaymentMapping.ToDto)
            .ToList();
    }
}