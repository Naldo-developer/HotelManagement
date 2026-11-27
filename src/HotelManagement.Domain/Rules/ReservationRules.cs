using HotelManagement.Domain.Exceptions;

namespace HotelManagement.Domain.Rules;

public static class ReservationRules
{
    public static void EnsureValidPeriod(DateOnly checkIn, DateOnly checkOut)
    {
        if (checkIn == default || checkOut <= checkIn)
            throw new BusinessValidationException("La fecha de salida debe ser posterior a la fecha de entrada.");
    }

    public static void EnsureOperationalRoom(string operationalStatus)
    {
        if (operationalStatus != "ACTIVE")
            throw new BusinessConflictException("La habitación no se encuentra operativa.");
    }

    public static void EnsureCapacity(int adults, int children, int maxAdults, int maxChildren)
    {
        if (adults < 1 || adults > maxAdults)
            throw new BusinessValidationException("La cantidad de adultos debe estar entre uno y la capacidad de la habitación.");
        if (children < 0 || children > maxChildren)
            throw new BusinessValidationException("La cantidad de niños supera el rango permitido para la habitación.");
    }

    public static void EnsureAvailable(bool hasReservation, bool hasMaintenance)
    {
        if (hasReservation || hasMaintenance)
            throw new BusinessConflictException("La habitación no está disponible para las fechas indicadas.");
    }
}
