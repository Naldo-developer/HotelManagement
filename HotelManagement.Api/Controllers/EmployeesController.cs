using HotelManagement.Api.Authorization;
using HotelManagement.Application.Dtos.Employees;
using HotelManagement.Application.UseCases.Employees.Commands;
using HotelManagement.Application.UseCases.Employees.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HotelManagement.Api.Controllers;

[ApiController]
[Route("api/employees")]
[Authorize(Policy = AppPolicies.Configuration)]
public class EmployeesController(ISender sender) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<List<EmployeeDto>>> GetAll(
        [FromQuery] long? hotelId,
        CancellationToken cancellationToken)
    {
        var employees = await sender.Send(
            new GetEmployeesQuery(hotelId),
            cancellationToken);

        return Ok(employees);
    }

    [HttpPost]
    public async Task<ActionResult<EmployeeDto>> Create(
        [FromBody] CreateEmployeeDto input,
        CancellationToken cancellationToken)
    {
        var employee = await sender.Send(
            new CreateEmployeeCommand(input),
            cancellationToken);

        return CreatedAtAction(
            nameof(GetAll),
            new { hotelId = input.HotelId },
            employee);
    }

    [HttpPut("{employeeId:long}")]
    public async Task<ActionResult<EmployeeDto>> Update(
        long employeeId,
        [FromBody] UpdateEmployeeDto input,
        CancellationToken cancellationToken)
    {
        var employee = await sender.Send(
            new UpdateEmployeeCommand(employeeId, input),
            cancellationToken);

        return Ok(employee);
    }
}