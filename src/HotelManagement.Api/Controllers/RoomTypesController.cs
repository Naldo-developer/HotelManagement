using HotelManagement.Application.UseCases.RoomTypes.Queries;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;

namespace HotelManagement.Api.Controllers;

[ApiController]
[Route("api/room-types")]
[Authorize]
public class RoomTypesController(ISender sender) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] long hotelId)
    {
        var roomTypes = await sender.Send(
            new GetRoomTypesQuery(hotelId), HttpContext.RequestAborted);

        return Ok(roomTypes);
    }
}