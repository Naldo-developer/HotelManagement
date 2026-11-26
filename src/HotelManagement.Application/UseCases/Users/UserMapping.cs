using HotelManagement.Application.Dtos.Users;
using HotelManagement.Domain.Entities;

namespace HotelManagement.Application.UseCases.Users;

internal static class UserMapping
{
    public static UserDto ToDto(AppUser user) => new()
    {
        UserId = user.UserId,
        EmployeeId = user.EmployeeId,
        Username = user.Username,
        Active = user.Active,
        EmployeeName =
            $"{user.Employee.FirstName} {user.Employee.LastName}",
        Roles = user.Roles
            .Select(role => role.Code)
            .OrderBy(code => code)
            .ToList()
    };
}