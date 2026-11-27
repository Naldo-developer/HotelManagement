namespace HotelManagement.Domain.Exceptions;

public sealed class BusinessValidationException(string message) : Exception(message);