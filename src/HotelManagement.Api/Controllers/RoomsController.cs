using HotelManagement.Application.Dtos.Rooms;
using HotelManagement.Application.UseCases.Rooms.Queries;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using HotelManagement.Application.UseCases.Rooms.Commands;

namespace HotelManagement.Api.Controllers;

[ApiController]
[Route("api/rooms")]
public class RoomsController(ISender sender) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] RoomFilterDto filter)
    {
        var rooms = await sender.Send(
            new GetRoomsQuery(filter), HttpContext.RequestAborted);

        return Ok(rooms);
    }

    [HttpGet("{id:long}")]
    public async Task<IActionResult> GetById(long id)
    {
        var room = await sender.Send(
            new GetRoomByIdQuery(id), HttpContext.RequestAborted);

        return Ok(room);
    }
    
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateRoomDto input)
    {
        var room = await sender.Send(
            new CreateRoomCommand(input), HttpContext.RequestAborted);

        return CreatedAtAction(nameof(GetById), new { id = room.RoomId }, room);
    }
}