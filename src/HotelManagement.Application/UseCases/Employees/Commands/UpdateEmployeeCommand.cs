using System.ComponentModel.DataAnnotations;
using HotelManagement.Application.Dtos.Employees;
using HotelManagement.Application.Exceptions;
using HotelManagement.Domain.Exceptions;
using HotelManagement.Domain.Repositories;
using MediatR;

namespace HotelManagement.Application.UseCases.Employees.Commands;

public sealed class UpdateEmployeeCommand(
    long employeeId,
    UpdateEmployeeDto input) : IRequest<EmployeeDto>
{
    public long EmployeeId { get; } = employeeId;
    public UpdateEmployeeDto Input { get; } = input;
}

internal sealed class UpdateEmployeeCommandHandler(
    IEmployeeRepository employeeRepository)
    : IRequestHandler<UpdateEmployeeCommand, EmployeeDto>
{
    public async Task<EmployeeDto> Handle(
        UpdateEmployeeCommand request,
        CancellationToken cancellationToken)
    {
        if (request.EmployeeId <= 0)
        {
            throw new BusinessValidationException(
                "El identificador del empleado no es válido.");
        }

        var errors = new List<ValidationResult>();

        if (!Validator.TryValidateObject(
                request.Input,
                new ValidationContext(request.Input),
                errors,
                validateAllProperties: true))
        {
            throw new BusinessValidationException(
                errors[0].ErrorMessage ??
                "Los datos del empleado no son válidos.");
        }

        var employee = await employeeRepository.GetByIdAsync(
            request.EmployeeId,
            cancellationToken);

        if (employee is null)
        {
            throw new ResourceNotFoundException(
                "El empleado indicado no existe.");
        }

        employee.HotelId = request.Input.HotelId;
        employee.DocumentType = request.Input.DocumentType?.Trim();
        employee.DocumentNumber = request.Input.DocumentNumber?.Trim();
        employee.FirstName = request.Input.FirstName.Trim();
        employee.LastName = request.Input.LastName.Trim();
        employee.Position = request.Input.Position?.Trim();
        employee.Email = request.Input.Email?.Trim();
        employee.Phone = request.Input.Phone?.Trim();
        employee.Active = request.Input.Active;

        var updated = await employeeRepository.UpdateAsync(
            employee,
            cancellationToken);

        return EmployeeMapping.ToDto(updated);
    }
}