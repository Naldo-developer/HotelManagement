using HotelManagement.Api.Authorization;
using HotelManagement.Application.Dtos.RoomTypes;
using HotelManagement.Application.UseCases.RoomTypes.Commands;
using HotelManagement.Application.UseCases.RoomTypes.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HotelManagement.Api.Controllers;

[ApiController]
[Route("api/room-types")]
[Authorize]
public class RoomTypesController(ISender sender) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] long hotelId)
    {
        var roomTypes = await sender.Send(
            new GetRoomTypesQuery(hotelId),
            HttpContext.RequestAborted);

        return Ok(roomTypes);
    }

    [HttpPost]
    [Authorize(Policy = AppPolicies.Configuration)]
    public async Task<ActionResult<RoomTypeDto>> Create(
        [FromBody] CreateRoomTypeDto input,
        CancellationToken cancellationToken)
    {
        var roomType = await sender.Send(
            new CreateRoomTypeCommand(input),
            cancellationToken);

        return CreatedAtAction(
            nameof(GetAll),
            new { hotelId = roomType.HotelId },
            roomType);
    }

    [HttpPut("{roomTypeId:long}")]
    [Authorize(Policy = AppPolicies.Configuration)]
    public async Task<ActionResult<RoomTypeDto>> Update(
        long roomTypeId,
        [FromBody] UpdateRoomTypeDto input,
        CancellationToken cancellationToken)
    {
        var roomType = await sender.Send(
            new UpdateRoomTypeCommand(roomTypeId, input),
            cancellationToken);

        return Ok(roomType);
    }
}