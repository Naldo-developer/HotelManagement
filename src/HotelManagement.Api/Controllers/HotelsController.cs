using HotelManagement.Api.Authorization;
using HotelManagement.Application.Dtos.Hotels;
using HotelManagement.Application.UseCases.Hotels.Commands;
using HotelManagement.Application.UseCases.Hotels.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HotelManagement.Api.Controllers;

[ApiController]
[Route("api/hotels")]
[Authorize]
public class HotelsController(ISender sender) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var hotels = await sender.Send(
            new GetHotelsQuery(),
            HttpContext.RequestAborted);

        return Ok(hotels);
    }

    [HttpPost]
    [Authorize(Policy = AppPolicies.Configuration)]
    public async Task<ActionResult<HotelDto>> Create(
        [FromBody] CreateHotelDto input,
        CancellationToken cancellationToken)
    {
        var hotel = await sender.Send(
            new CreateHotelCommand(input),
            cancellationToken);

        return CreatedAtAction(nameof(GetAll), hotel);
    }

    [HttpPut("{hotelId:long}")]
    [Authorize(Policy = AppPolicies.Configuration)]
    public async Task<ActionResult<HotelDto>> Update(
        long hotelId,
        [FromBody] UpdateHotelDto input,
        CancellationToken cancellationToken)
    {
        var hotel = await sender.Send(
            new UpdateHotelCommand(hotelId, input),
            cancellationToken);

        return Ok(hotel);
    }
}