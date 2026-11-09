namespace HotelManagement.Application.Exceptions;

public sealed class BusinessValidationException(string message) : Exception(message);