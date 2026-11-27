using HotelManagement.Domain.Exceptions;

namespace HotelManagement.Domain.Rules;

public static class StayRules
{
    public static void EnsureReservationAllowsCheckIn(string status)
    {
        if (status is not ("PENDING" or "CONFIRMED"))
            throw new BusinessConflictException("Solo se puede hacer check-in de reservas pendientes o confirmadas.");
    }

    public static void EnsureNoExistingStay(bool hasStay)
    {
        if (hasStay)
            throw new BusinessConflictException("La reserva ya tiene una estadía registrada.");
    }

    public static void EnsureNoOpenStay(bool hasOpenStay)
    {
        if (hasOpenStay)
            throw new BusinessConflictException("La habitación ya tiene una estadía abierta.");
    }

    public static void EnsureActiveCatalog(bool hotelActive, bool roomTypeActive)
    {
        if (!hotelActive || !roomTypeActive)
            throw new BusinessConflictException("El hotel o el tipo de habitación no se encuentra activo.");
    }

    public static void EnsureCheckInCapacity(int adults, int children, int maxAdults, int maxChildren)
    {
        if (adults > maxAdults || children > maxChildren)
            throw new BusinessConflictException("La capacidad actual de la habitación no admite a los huéspedes de la reserva.");
    }

    public static void EnsureCheckInPeriod(DateTime checkInAtUtc, string timezone, DateOnly arrival, DateOnly departure)
    {
        var localDate = DateOnly.FromDateTime(TimeZoneInfo.ConvertTimeFromUtc(
            checkInAtUtc, TimeZoneInfo.FindSystemTimeZoneById(timezone)));
        if (localDate < arrival || localDate >= departure)
            throw new BusinessConflictException("El check-in debe realizarse desde la fecha de entrada y antes de la fecha de salida.");
    }

    public static void EnsureNoMaintenance(bool hasMaintenance)
    {
        if (hasMaintenance)
            throw new BusinessConflictException("La habitación tiene mantenimiento durante el periodo de la reserva.");
    }

    public static void EnsureOpenForCheckOut(string status, DateTime? checkOutAt)
    {
        if (status != "ACTIVE" || checkOutAt is not null)
            throw new BusinessConflictException("Solo se pueden cerrar estadías activas.");
    }

    public static void EnsureSettledBalance(DateOnly arrival, DateOnly departure,
        decimal nightlyRate, decimal consumptionTotal, decimal totalPaid)
    {
        var balance = (departure.DayNumber - arrival.DayNumber) * nightlyRate + consumptionTotal - totalPaid;
        if (balance > 0.01m)
            throw new BusinessConflictException($"No se puede cerrar la estadía. Saldo pendiente: {balance:0.00}.");
    }
}
