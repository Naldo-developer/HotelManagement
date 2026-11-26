using System.Globalization;
using System.Security.Claims;
using HotelManagement.Api.Authorization;
using HotelManagement.Application.Dtos.Maintenances;
using HotelManagement.Application.UseCases.Maintenances.Commands;
using HotelManagement.Application.UseCases.Maintenances.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HotelManagement.Api.Controllers;

[ApiController]
[Route("api/maintenance")]
[Authorize(Policy = AppPolicies.Maintenance)]
public class MaintenanceController(ISender sender) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<List<MaintenanceDto>>> GetAll(
        [FromQuery] long? roomId,
        [FromQuery] string? status,
        CancellationToken cancellationToken)
    {
        var maintenances = await sender.Send(
            new GetMaintenancesQuery(roomId, status),
            cancellationToken);

        return Ok(maintenances);
    }

    [HttpPost]
    public async Task<ActionResult<MaintenanceDto>> Create(
        [FromBody] CreateMaintenanceDto input,
        CancellationToken cancellationToken)
    {
        var userId = long.Parse(
            User.FindFirstValue(ClaimTypes.NameIdentifier)!,
            CultureInfo.InvariantCulture);

        var maintenance = await sender.Send(
            new CreateMaintenanceCommand(input, userId),
            cancellationToken);

        return CreatedAtAction(
            nameof(GetAll),
            new { roomId = maintenance.RoomId },
            maintenance);
    }

    [HttpPatch("{id:long}/close")]
    public async Task<ActionResult<MaintenanceDto>> Close(
        long id,
        [FromBody] CloseMaintenanceDto input,
        CancellationToken cancellationToken)
    {
        var maintenance = await sender.Send(
            new CloseMaintenanceCommand(id, input),
            cancellationToken);

        return Ok(maintenance);
    }
}