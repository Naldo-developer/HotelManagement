using HotelManagement.Api.Authorization;
using HotelManagement.Application.Dtos.Users;
using HotelManagement.Application.UseCases.Users.Commands;
using HotelManagement.Application.UseCases.Users.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HotelManagement.Api.Controllers;

[ApiController]
[Route("api/users")]
[Authorize(Policy = AppPolicies.Configuration)]
public class UsersController(ISender sender) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<List<UserDto>>> GetAll(
        CancellationToken cancellationToken)
    {
        var users = await sender.Send(
            new GetUsersQuery(),
            cancellationToken);

        return Ok(users);
    }

    [HttpPost]
    public async Task<ActionResult<UserDto>> Create(
        [FromBody] CreateUserDto input,
        CancellationToken cancellationToken)
    {
        var user = await sender.Send(
            new CreateUserCommand(input),
            cancellationToken);

        return CreatedAtAction(
            nameof(GetAll),
            user);
    }

    [HttpPut("{userId:long}")]
    public async Task<ActionResult<UserDto>> Update(
        long userId,
        [FromBody] UpdateUserDto input,
        CancellationToken cancellationToken)
    {
        var user = await sender.Send(
            new UpdateUserCommand(userId, input),
            cancellationToken);

        return Ok(user);
    }
}