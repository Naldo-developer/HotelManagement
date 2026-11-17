using HotelManagement.Application.Dtos.Reservations;
using HotelManagement.Domain.Entities;

namespace HotelManagement.Application.UseCases.Reservations;

internal static class ReservationMapping
{
    public static ReservationDto ToDto(Reservation reservation)
    {
        return new ReservationDto
        {
            ReservationId = reservation.ReservationId,
            ConfirmationCode = reservation.ConfirmationCode,
            RoomId = reservation.RoomId,
            PrimaryGuestId = reservation.PrimaryGuestId,
            CheckInDate = reservation.CheckInDate,
            CheckOutDate = reservation.CheckOutDate,
            Adults = reservation.Adults,
            Children = reservation.Children,
            NightlyRate = reservation.NightlyRate,
            Status = reservation.Status,
            SpecialRequests = reservation.SpecialRequests,
            CancellationReason = reservation.CancellationReason
        };
    }
}