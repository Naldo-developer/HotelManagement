using HotelManagement.Application.Dtos.Employees;
using HotelManagement.Domain.Entities;

namespace HotelManagement.Application.UseCases.Employees;

internal static class EmployeeMapping
{
    public static EmployeeDto ToDto(Employee employee) => new()
    {
        EmployeeId = employee.EmployeeId,
        HotelId = employee.HotelId,
        DocumentType = employee.DocumentType,
        DocumentNumber = employee.DocumentNumber,
        FirstName = employee.FirstName,
        LastName = employee.LastName,
        Position = employee.Position,
        Email = employee.Email,
        Phone = employee.Phone,
        Active = employee.Active
    };
}