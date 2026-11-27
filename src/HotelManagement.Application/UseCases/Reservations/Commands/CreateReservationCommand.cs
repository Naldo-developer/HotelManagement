using System.ComponentModel.DataAnnotations;
using HotelManagement.Application.Dtos.Reservations;
using HotelManagement.Domain.Entities;
using HotelManagement.Domain.Exceptions;
using HotelManagement.Domain.Repositories;
using MediatR;

namespace HotelManagement.Application.UseCases.Reservations.Commands;

public class CreateReservationCommand(CreateReservationDto input)
    : IRequest<ReservationDto>
{
    public CreateReservationDto Input { get; } = input;
}

internal class CreateReservationCommandHandler(
    IReservationRepository reservationRepository)
    : IRequestHandler<CreateReservationCommand, ReservationDto>
{
    public async Task<ReservationDto> Handle(
        CreateReservationCommand request,
        CancellationToken cancellationToken)
    {
        var input = request.Input;
        var errors = new List<ValidationResult>();

        if (!Validator.TryValidateObject(
                input,
                new ValidationContext(input),
                errors,
                validateAllProperties: true))
        {
            throw new BusinessValidationException(
                errors[0].ErrorMessage ?? "Los datos de la reserva son inválidos.");
        }

        var checkInDate = input.CheckInDate!.Value;
        var checkOutDate = input.CheckOutDate!.Value;

        HotelManagement.Domain.Rules.ReservationRules.EnsureValidPeriod(checkInDate, checkOutDate);

        var reservation = new Reservation
        {
            ConfirmationCode = Guid.NewGuid().ToString("N")[..30],
            RoomId = input.RoomId,
            PrimaryGuestId = input.PrimaryGuestId,
            CheckInDate = checkInDate,
            CheckOutDate = checkOutDate,
            Adults = input.Adults,
            Children = input.Children,
            SpecialRequests = string.IsNullOrWhiteSpace(input.SpecialRequests)
                ? null
                : input.SpecialRequests.Trim()
        };

        var created = await reservationRepository.CreateAsync(
            reservation, cancellationToken);

        return new ReservationDto
        {
            ReservationId = created.ReservationId,
            ConfirmationCode = created.ConfirmationCode,
            RoomId = created.RoomId,
            PrimaryGuestId = created.PrimaryGuestId,
            CheckInDate = created.CheckInDate,
            CheckOutDate = created.CheckOutDate,
            Adults = created.Adults,
            Children = created.Children,
            NightlyRate = created.NightlyRate,
            Status = created.Status,
            SpecialRequests = created.SpecialRequests
        };
    }
}