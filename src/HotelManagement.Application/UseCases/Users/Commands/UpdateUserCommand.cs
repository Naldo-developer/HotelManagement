using System.ComponentModel.DataAnnotations;
using HotelManagement.Application.Dtos.Users;
using HotelManagement.Application.Exceptions;
using HotelManagement.Application.Security;
using HotelManagement.Domain.Repositories;
using MediatR;

namespace HotelManagement.Application.UseCases.Users.Commands;

public sealed class UpdateUserCommand(
    long userId,
    UpdateUserDto input) : IRequest<UserDto>
{
    public long UserId { get; } = userId;
    public UpdateUserDto Input { get; } = input;
}

internal sealed class UpdateUserCommandHandler(
    IUserRepository userRepository,
    ICredentialHasher credentialHasher)
    : IRequestHandler<UpdateUserCommand, UserDto>
{
    public async Task<UserDto> Handle(
        UpdateUserCommand request,
        CancellationToken cancellationToken)
    {
        if (request.UserId <= 0)
        {
            throw new BusinessValidationException(
                "El identificador del usuario no es válido.");
        }

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

        var user = await userRepository.GetByIdAsync(
            request.UserId,
            cancellationToken);

        if (user is null)
        {
            throw new ResourceNotFoundException(
                "El usuario indicado no existe.");
        }

        user.Username = request.Input.Username.Trim();
        user.Active = request.Input.Active;

        if (!string.IsNullOrWhiteSpace(request.Input.Password))
        {
            user.PasswordHash = credentialHasher.Hash(
                request.Input.Password);
        }

        var updated = await userRepository.UpdateAsync(
            user,
            request.Input.RoleCodes,
            cancellationToken);

        return UserMapping.ToDto(updated);
    }
}