namespace HotelManagement.Application.Exceptions;

public sealed class BusinessConflictException(string message) : Exception(message);
