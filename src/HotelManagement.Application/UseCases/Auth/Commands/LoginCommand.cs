using System.ComponentModel.DataAnnotations;
using HotelManagement.Application.Dtos.Auth;
using HotelManagement.Application.Interfaces.Services;
using HotelManagement.Application.Security;
using HotelManagement.Domain.Exceptions;
using HotelManagement.Domain.Repositories;
using MediatR;

namespace HotelManagement.Application.UseCases.Auth.Commands;

public class LoginCommand(LoginDto input) : IRequest<AuthenticatedUserDto?>
{
    public LoginDto Input { get; } = input;
}

internal class LoginCommandHandler(IUserRepository userRepository, ICredentialHasher hasher, IAccessTokenIssuer tokens)
    : IRequestHandler<LoginCommand, AuthenticatedUserDto?>
{
    public async Task<AuthenticatedUserDto?> Handle(
        LoginCommand request,
        CancellationToken cancellationToken)
    {
        var input = request.Input;
        var errors = new List<ValidationResult>();

        if (!Validator.TryValidateObject(input, new ValidationContext(input),
                errors, validateAllProperties: true))
        {
            throw new BusinessValidationException(
                errors[0].ErrorMessage ?? "Los datos de acceso son inválidos.");
        }

        var user = await userRepository.GetByUsernameAsync(
            input.Username.Trim(), cancellationToken);

        if (user is null
            || !user.Active
            || !user.Employee.Active
            || !hasher.Verify(user.PasswordHash, input.Password))
        {
            return null;
        }

        await userRepository.RegisterLoginAsync(user.UserId, DateTime.UtcNow, cancellationToken);

        var identity = new AuthenticatedUserDto
        {
            UserId = user.UserId,
            Username = user.Username,
            FullName = $"{user.Employee.FirstName} {user.Employee.LastName}",
            Roles = user.Roles.Select(r => r.Code).OrderBy(code => code).ToList()
        };
        var issued = tokens.Issue(identity);
        identity.AccessToken = issued.AccessToken;
        identity.TokenType = "Bearer";
        identity.ExpiresAtUtc = issued.ExpiresAtUtc;
        return identity;
    }
}
