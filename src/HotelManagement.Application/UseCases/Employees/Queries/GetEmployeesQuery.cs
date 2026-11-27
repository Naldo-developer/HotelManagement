using HotelManagement.Application.Dtos.Employees;
using HotelManagement.Application.Mappings;
using HotelManagement.Domain.Exceptions;
using HotelManagement.Domain.Repositories;
using MediatR;

namespace HotelManagement.Application.UseCases.Employees.Queries;

public sealed record GetEmployeesQuery(
    long? HotelId) : IRequest<List<EmployeeDto>>;

internal sealed class GetEmployeesQueryHandler(
    IEmployeeRepository employeeRepository)
    : IRequestHandler<GetEmployeesQuery, List<EmployeeDto>>
{
    public async Task<List<EmployeeDto>> Handle(
        GetEmployeesQuery request,
        CancellationToken cancellationToken)
    {
        if (request.HotelId is <= 0)
        {
            throw new BusinessValidationException(
                "El identificador del hotel debe ser mayor que cero.");
        }

        var employees = await employeeRepository.GetAllAsync(
            request.HotelId,
            cancellationToken);

        return employees
            .Select(EmployeeMapping.ToDto)
            .ToList();
    }
}