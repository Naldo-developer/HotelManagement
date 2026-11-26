using HotelManagement.Application.Dtos.RoomTypes;
using HotelManagement.Domain.Entities;

namespace HotelManagement.Application.UseCases.RoomTypes;

internal static class RoomTypeMapping
{
    public static RoomTypeDto ToDto(RoomType roomType)
    {
        return new RoomTypeDto
        {
            RoomTypeId = roomType.RoomTypeId,
            HotelId = roomType.HotelId,
            Name = roomType.Name,
            Description = roomType.Description,
            MaxAdults = roomType.MaxAdults,
            MaxChildren = roomType.MaxChildren,
            BaseRate = roomType.BaseRate,
            Active = roomType.Active,
            CreatedAt = roomType.CreatedAt
        };
    }
}