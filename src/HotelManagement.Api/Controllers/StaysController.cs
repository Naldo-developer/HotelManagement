using System.Globalization;
using System.Security.Claims;
using HotelManagement.Api.Authorization;
using HotelManagement.Application.Dtos.Stays;
using HotelManagement.Application.UseCases.Stays.Commands;
using HotelManagement.Application.UseCases.Stays.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HotelManagement.Api.Controllers;

[ApiController]
[Route("api/stays")]
[Authorize(Policy = AppPolicies.Operations)]
public class StaysController(ISender sender) : ControllerBase
{
    [HttpPost("check-in")]
    public async Task<ActionResult<StayDto>> CheckIn(
        [FromBody] CheckInDto input)
    {
        var userIdClaim = User.FindFirstValue(
            ClaimTypes.NameIdentifier);

        if (!long.TryParse(
                userIdClaim,
                NumberStyles.None,
                CultureInfo.InvariantCulture,
                out var userId) ||
            userId <= 0)
        {
            return Problem(
                statusCode: StatusCodes.Status401Unauthorized,
                title: "Sesión inválida.",
                detail: "Inicia sesión nuevamente.");
        }

        var result = await sender.Send(
            new CheckInCommand(input, userId),
            HttpContext.RequestAborted);

        return CreatedAtAction(
            nameof(GetById),
            new { id = result.StayId },
            result);
    }

    [HttpGet("{id:long}")]
    public async Task<ActionResult<StayDto>> GetById(long id)
    {
        var result = await sender.Send(
            new GetStayByIdQuery(id),
            HttpContext.RequestAborted);

        return Ok(result);
    }

    [HttpGet("by-reservation/{reservationId:long}")]
    public async Task<ActionResult<StayDto>> GetByReservationId(
        long reservationId)
    {
        var result = await sender.Send(
            new GetStayByReservationIdQuery(reservationId),
            HttpContext.RequestAborted);

        return Ok(result);
    }
}