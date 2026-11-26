using HotelManagement.Application.UseCases.Guests.Queries;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using HotelManagement.Application.UseCases.Guests.Commands;
using HotelManagement.Api.Authorization;
using HotelManagement.Application.Dtos.Guests;
using Microsoft.AspNetCore.Authorization;

namespace HotelManagement.Api.Controllers;

[ApiController]
[Route("api/guests")]
[Authorize(Policy = AppPolicies.Operations)]
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
    [HttpPost]
    public async Task<ActionResult<GuestDto>> CreateGuest([FromBody] CreateGuestDto input)
    {
        var result = await _sender.Send(
            new CreateGuestCommand(input), HttpContext.RequestAborted);

        return CreatedAtAction(nameof(GetGuestById), new { id = result.Id }, result);
    }
    [HttpPut("{id:long}")]
    public async Task<ActionResult<GuestDto>> UpdateGuest(
        long id, [FromBody] UpdateGuestDto input)
    {
        var result = await _sender.Send(
            new UpdateGuestCommand(id, input), HttpContext.RequestAborted);

        return Ok(result);
    }
}