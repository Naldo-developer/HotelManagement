using HotelManagement.Application.Dtos.Stays;
using HotelManagement.Domain.Entities;

namespace HotelManagement.Application.Mappings;

internal static class StayMapping
{
    public static StayDto ToDto(Stay stay)
    {
        return new StayDto
        {
            StayId = stay.StayId,
            ReservationId = stay.ReservationId,
            RoomId = stay.Reservation.RoomId,
            CheckInAt = stay.CheckInAt,
            CheckOutAt = stay.CheckOutAt,
            Status = stay.Status,
            CheckInByUserId = stay.CheckInByUserId,
            CheckOutByUserId = stay.CheckOutByUserId,
            Notes = stay.Notes
        };
    }
}