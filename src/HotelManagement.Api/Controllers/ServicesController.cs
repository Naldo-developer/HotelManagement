using HotelManagement.Api.Authorization;
using HotelManagement.Application.Dtos.Services;
using HotelManagement.Application.UseCases.Services.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

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
}