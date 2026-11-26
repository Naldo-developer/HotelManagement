using System.Security.Claims;
using HotelManagement.Api.Authorization;
using HotelManagement.Application.Dtos.Consumptions;
using HotelManagement.Application.UseCases.Consumptions.Commands;
using HotelManagement.Application.UseCases.Consumptions.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HotelManagement.Api.Controllers;

[ApiController]
[Route("api/consumptions")]
[Authorize(Policy = AppPolicies.Operations)]
public class ConsumptionsController(ISender sender) : ControllerBase
{
    [HttpPost]
    public async Task<ActionResult<ConsumptionDto>> Create(
        [FromBody] CreateConsumptionDto input,
        CancellationToken cancellationToken)
    {
        var claim = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (!long.TryParse(claim, out var userId))
        {
            return Unauthorized();
        }

        var consumption = await sender.Send(
            new CreateConsumptionCommand(input, userId),
            cancellationToken);

        return CreatedAtAction(
            nameof(GetByStay),
            new { stayId = input.StayId },
            consumption);
    }

    [HttpGet("by-stay/{stayId:long}")]
    public async Task<ActionResult<List<ConsumptionDto>>> GetByStay(
        long stayId,
        CancellationToken cancellationToken)
    {
        var consumptions = await sender.Send(
            new GetConsumptionsByStayQuery(stayId),
            cancellationToken);

        return Ok(consumptions);
    }
}