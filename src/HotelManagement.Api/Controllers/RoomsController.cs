using HotelManagement.Application.Dtos.Rooms;
using HotelManagement.Application.UseCases.Rooms.Queries;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using HotelManagement.Application.UseCases.Rooms.Commands;
using HotelManagement.Api.Authorization;
using Microsoft.AspNetCore.Authorization;

namespace HotelManagement.Api.Controllers;

[ApiController]
[Route("api/rooms")]
[Authorize]
public class RoomsController(ISender sender) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] RoomFilterDto filter)
    {
        var rooms = await sender.Send(
            new GetRoomsQuery(filter), HttpContext.RequestAborted);

        return Ok(rooms);
    }
    
    [HttpGet("availability")]
    public async Task<IActionResult> GetAvailability(
        [FromQuery] RoomAvailabilityFilterDto filter)
    {
        var rooms = await sender.Send(
            new GetAvailableRoomsQuery(filter), HttpContext.RequestAborted);

        return Ok(rooms);
    }

    [HttpGet("{id:long}")]
    public async Task<IActionResult> GetById(long id)
    {
        var room = await sender.Send(
            new GetRoomByIdQuery(id), HttpContext.RequestAborted);

        return Ok(room);
    }
    [Authorize(Policy = AppPolicies.Configuration)]
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateRoomDto input)
    {
        var room = await sender.Send(
            new CreateRoomCommand(input), HttpContext.RequestAborted);

        return CreatedAtAction(nameof(GetById), new { id = room.RoomId }, room);
    }
    [Authorize(Policy = AppPolicies.Configuration)]
    [HttpPut("{id:long}")]
    public async Task<IActionResult> Update(long id, [FromBody] UpdateRoomDto input)
    {
        var room = await sender.Send(
            new UpdateRoomCommand(id, input), HttpContext.RequestAborted);

        return Ok(room);
    }
}