using HotelManagement.Application.Dtos.Reservations;
using HotelManagement.Application.UseCases.Reservations.Commands;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace HotelManagement.Api.Controllers;

[ApiController]
[Route("api/reservations")]
public class ReservationsController(ISender sender) : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateReservationDto input)
    {
        var reservation = await sender.Send(
            new CreateReservationCommand(input),
            HttpContext.RequestAborted);

        return StatusCode(StatusCodes.Status201Created, reservation);
    }
}