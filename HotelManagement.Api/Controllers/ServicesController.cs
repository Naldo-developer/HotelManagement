using HotelManagement.Api.Authorization;
using HotelManagement.Application.Dtos.Services;
using HotelManagement.Application.UseCases.Services.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using HotelManagement.Application.UseCases.Services.Commands;

namespace HotelManagement.Api.Controllers;

[ApiController]
[Route("api/services")]
[Authorize(Policy = AppPolicies.Operations)]
public class ServicesController(ISender sender) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<List<ServiceDto>>> GetAll(
        [FromQuery] long hotelId,
        CancellationToken cancellationToken)
    {
        var services = await sender.Send(
            new GetServicesQuery(hotelId),
            cancellationToken);

        return Ok(services);
    }
    [HttpPost]
    [Authorize(Policy = AppPolicies.Configuration)]
    public async Task<ActionResult<ServiceDto>> Create(
        [FromBody] CreateServiceDto input,
        CancellationToken cancellationToken)
    {
        var service = await sender.Send(
            new CreateServiceCommand(input),
            cancellationToken);

        return CreatedAtAction(
            nameof(GetAll),
            new { hotelId = input.HotelId },
            service);
    }

    [HttpPut("{serviceId:long}")]
    [Authorize(Policy = AppPolicies.Configuration)]
    public async Task<ActionResult<ServiceDto>> Update(
        long serviceId,
        [FromBody] UpdateServiceDto input,
        CancellationToken cancellationToken)
    {
        var service = await sender.Send(
            new UpdateServiceCommand(serviceId, input),
            cancellationToken);

        return Ok(service);
    }
}