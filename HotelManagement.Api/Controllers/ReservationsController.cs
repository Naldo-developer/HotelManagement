using HotelManagement.Application.Dtos.Reservations;
using HotelManagement.Application.UseCases.Reservations.Commands;
using HotelManagement.Application.UseCases.Reservations.Queries;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using HotelManagement.Api.Authorization;
using Microsoft.AspNetCore.Authorization;

namespace HotelManagement.Api.Controllers;

[ApiController]
[Route("api/reservations")]
[Authorize(Policy = AppPolicies.Operations)]
public class ReservationsController(ISender sender) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] ReservationFilterDto filter)
    {
        var reservations = await sender.Send(
            new GetReservationsQuery(filter),
            HttpContext.RequestAborted);

        return Ok(reservations);
    }

    [HttpGet("{id:long}")]
    public async Task<IActionResult> GetById(long id)
    {
        var reservation = await sender.Send(
            new GetReservationByIdQuery(id),
            HttpContext.RequestAborted);

        return Ok(reservation);
    }

    [HttpPost]
    public async Task<IActionResult> Create(
        [FromBody] CreateReservationDto input)
    {
        var reservation = await sender.Send(
            new CreateReservationCommand(input),
            HttpContext.RequestAborted);

        return CreatedAtAction(
            nameof(GetById),
            new { id = reservation.ReservationId },
            reservation);
    }

    [HttpPost("with-guest")]
    public async Task<IActionResult> CreateWithGuest([FromBody] CreateGuestReservationDto input)
    {
        var reservation = await sender.Send(new CreateGuestReservationCommand(input),
            HttpContext.RequestAborted);
        return CreatedAtAction(nameof(GetById), new { id = reservation.ReservationId }, reservation);
    }

    [HttpPatch("{id:long}/cancel")]
    public async Task<IActionResult> Cancel(
        long id,
        [FromBody] CancelReservationDto input)
    {
        var reservation = await sender.Send(
            new CancelReservationCommand(id, input),
            HttpContext.RequestAborted);

        return Ok(reservation);
    }
}
