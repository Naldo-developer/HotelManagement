using System.ComponentModel.DataAnnotations;
using HotelManagement.Application.Dtos.Employees;
using HotelManagement.Domain.Entities;
using HotelManagement.Domain.Exceptions;
using HotelManagement.Domain.Repositories;
using MediatR;

namespace HotelManagement.Application.UseCases.Employees.Commands;

public sealed class CreateEmployeeCommand(
    CreateEmployeeDto input) : IRequest<EmployeeDto>
{
    public CreateEmployeeDto Input { get; } = input;
}

internal sealed class CreateEmployeeCommandHandler(
    IEmployeeRepository employeeRepository)
    : IRequestHandler<CreateEmployeeCommand, EmployeeDto>
{
    public async Task<EmployeeDto> Handle(
        CreateEmployeeCommand request,
        CancellationToken cancellationToken)
    {
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

        var employee = new Employee
        {
            HotelId = request.Input.HotelId,
            DocumentType = request.Input.DocumentType?.Trim(),
            DocumentNumber = request.Input.DocumentNumber?.Trim(),
            FirstName = request.Input.FirstName.Trim(),
            LastName = request.Input.LastName.Trim(),
            Position = request.Input.Position?.Trim(),
            Email = request.Input.Email?.Trim(),
            Phone = request.Input.Phone?.Trim(),
            Active = true
        };

        var created = await employeeRepository.CreateAsync(
            employee,
            cancellationToken);

        return EmployeeMapping.ToDto(created);
    }
}