using HotelManagement.Application.Transactions;
using System.ComponentModel.DataAnnotations;
using HotelManagement.Application.Dtos.Users;
using HotelManagement.Application.Security;
using HotelManagement.Domain.Entities;
using HotelManagement.Domain.Exceptions;
using HotelManagement.Domain.Repositories;
using MediatR;

namespace HotelManagement.Application.UseCases.Users.Commands;

public sealed class CreateUserCommand(
    CreateUserDto input) : IRequest<UserDto>, ITransactionalRequest
{
    public CreateUserDto Input { get; } = input;
}

internal sealed class CreateUserCommandHandler(
    IUserRepository userRepository,
    ICredentialHasher credentialHasher)
    : IRequestHandler<CreateUserCommand, UserDto>
{
    public async Task<UserDto> Handle(
        CreateUserCommand request,
        CancellationToken cancellationToken)
    {
        var errors = new List<ValidationResult>();

        if (!Validator.TryValidateObject(
                request.Input,
                new ValidationContext(request.Input),
                errors,
                validateAllProperties: true))
        {
            throw new BusinessValidationException(
                errors[0].ErrorMessage ??
                "Los datos del usuario no son válidos.");
        }

        var user = new AppUser
        {
            EmployeeId = request.Input.EmployeeId,
            Username = request.Input.Username.Trim(),
            PasswordHash = credentialHasher.Hash(request.Input.Password),
            Active = true
        };

        var created = await userRepository.CreateAsync(
            user,
            request.Input.RoleCodes,
            cancellationToken);

        return UserMapping.ToDto(created);
    }
}