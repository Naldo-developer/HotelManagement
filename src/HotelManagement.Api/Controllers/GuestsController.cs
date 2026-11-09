using HotelManagement.Application.DTOs.Guests; 
using HotelManagement.Application.UseCases.Guests.Queries;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace HotelManagement.Api.Controllers;

[ApiController]
[Route("api/guests")]
public class GuestsController : ControllerBase
{
    private readonly ISender _sender;

    public GuestsController(ISender sender)
    {
        _sender = sender;
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<GuestDto>>> GetGuests([FromQuery] GuestFilterDto filter)
    {
        var result = await _sender.Send(new GetGuestsQuery { Filter = filter });
        return Ok(result);
    }

    [HttpGet("{id:long}")]
    public async Task<ActionResult<GuestDto>> GetGuestById(long id)
    {
        var result = await _sender.Send(new GetGuestByIdQuery(id));
        return Ok(result);
    }
}