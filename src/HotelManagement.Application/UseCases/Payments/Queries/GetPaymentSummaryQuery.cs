using HotelManagement.Application.Dtos.Payments;
using HotelManagement.Application.Exceptions;
using HotelManagement.Domain.Repositories;
using MediatR;

namespace HotelManagement.Application.UseCases.Payments.Queries;

public sealed record GetPaymentSummaryQuery(
    long ReservationId) : IRequest<PaymentSummaryDto>;

internal sealed class GetPaymentSummaryQueryHandler(
    IReservationRepository reservationRepository,
    IStayRepository stayRepository,
    IConsumptionRepository consumptionRepository,
    IPaymentRepository paymentRepository)
    : IRequestHandler<GetPaymentSummaryQuery, PaymentSummaryDto>
{
    public async Task<PaymentSummaryDto> Handle(
        GetPaymentSummaryQuery request,
        CancellationToken cancellationToken)
    {
        if (request.ReservationId <= 0)
        {
            throw new BusinessValidationException(
                "El identificador de la reserva debe ser mayor que cero.");
        }

        var reservation = await reservationRepository.GetByIdAsync(
            request.ReservationId,
            cancellationToken);

        if (reservation is null)
        {
            throw new ResourceNotFoundException(
                "La reserva indicada no existe.");
        }

        var nights = reservation.CheckOutDate.DayNumber
            - reservation.CheckInDate.DayNumber;

        var accommodationTotal =
            nights * reservation.NightlyRate;

        var stay = await stayRepository.GetByReservationIdAsync(
            request.ReservationId,
            cancellationToken);

        var consumptionTotal = 0m;

        if (stay is not null)
        {
            var consumptions = await consumptionRepository.GetByStayIdAsync(
                stay.StayId,
                cancellationToken);

            consumptionTotal = consumptions.Sum(consumption =>
                consumption.Subtotal
                ?? consumption.Quantity * consumption.UnitPrice);
        }

        var payments = await paymentRepository.GetByReservationIdAsync(
            request.ReservationId,
            cancellationToken);

        var totalPaid = payments
            .Where(payment => payment.Status == "COMPLETED")
            .Sum(payment => payment.Amount);

        var totalDue = accommodationTotal + consumptionTotal;
        var balanceDue = Math.Max(0m, totalDue - totalPaid);

        return new PaymentSummaryDto
        {
            ReservationId = request.ReservationId,
            AccommodationTotal = accommodationTotal,
            ConsumptionTotal = consumptionTotal,
            TotalDue = totalDue,
            TotalPaid = totalPaid,
            BalanceDue = balanceDue
        };
    }
}