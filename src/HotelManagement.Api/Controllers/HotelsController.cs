using HotelManagement.Application.UseCases.Hotels.Queries;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace HotelManagement.Api.Controllers;

[ApiController]
[Route("api/hotels")]
public class HotelsController(ISender sender) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var hotels = await sender.Send(
            new GetHotelsQuery(), HttpContext.RequestAborted);

        return Ok(hotels);
    }
}