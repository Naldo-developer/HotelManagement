using HotelManagement.Application.Dtos.Users;
using HotelManagement.Application.Mappings;
using HotelManagement.Domain.Repositories;
using MediatR;

namespace HotelManagement.Application.UseCases.Users.Queries;

public sealed record GetUsersQuery : IRequest<List<UserDto>>;

internal sealed class GetUsersQueryHandler(
    IUserRepository userRepository)
    : IRequestHandler<GetUsersQuery, List<UserDto>>
{
    public async Task<List<UserDto>> Handle(
        GetUsersQuery request,
        CancellationToken cancellationToken)
    {
        var users = await userRepository.GetAllAsync(
            cancellationToken);

        return users
            .Select(UserMapping.ToDto)
            .ToList();
    }
}