using HotelManagement.Application.Dtos.Hotels;
using HotelManagement.Domain.Entities;

namespace HotelManagement.Application.UseCases.Hotels;

internal static class HotelMapping
{
    public static HotelDto ToDto(Hotel hotel)
    {
        return new HotelDto
        {
            HotelId = hotel.HotelId,
            Name = hotel.Name,
            TaxId = hotel.TaxId,
            Address = hotel.Address,
            Phone = hotel.Phone,
            Email = hotel.Email,
            CurrencyCode = hotel.CurrencyCode,
            Timezone = hotel.Timezone,
            Active = hotel.Active,
            CreatedAt = hotel.CreatedAt
        };
    }
}