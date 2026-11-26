using HotelManagement.Application.Configuration;
using HotelManagement.Application.DTOs.Auth;
using HotelManagement.Application.Exceptions;
using HotelManagement.Application.Security;
using HotelManagement.Application.UseCases.Auth.Commands;
using HotelManagement.Domain.Entities;
using HotelManagement.Domain.Repositories;
using MediatR;
using Microsoft.Extensions.DependencyInjection;

namespace HotelManagement.Api.Tests;

public class LoginTests
{
    private const string Password = "Clave-de-prueba-1";

    [Fact]
    public async Task Valid_credentials_return_user_with_roles_and_record_login()
    {
        var (sender, repo) = Build();

        var result = await sender.Send(Login("recepcion.demo", Password));

        Assert.NotNull(result);
        Assert.Equal(1, result.UserId);
        Assert.Equal("Rosa Demo", result.FullName);
        Assert.Equal(["RECEPTION"], result.Roles);
        Assert.Equal([1L], repo.LoginsRecorded);
    }

    [Fact]
    public async Task Wrong_password_returns_null()
    {
        var (sender, repo) = Build();

        Assert.Null(await sender.Send(Login("recepcion.demo", "otra-clave")));
        Assert.Empty(repo.LoginsRecorded);
    }

    [Fact]
    public async Task Unknown_user_returns_null()
    {
        var (sender, _) = Build();

        Assert.Null(await sender.Send(Login("nadie", Password)));
    }

    [Fact]
    public async Task Inactive_user_returns_null()
    {
        var (sender, repo) = Build();
        repo.Users[0].Active = false;

        Assert.Null(await sender.Send(Login("recepcion.demo", Password)));
    }

    [Fact]
    public async Task Missing_password_is_rejected()
    {
        var (sender, _) = Build();

        await Assert.ThrowsAsync<BusinessValidationException>(
            () => sender.Send(Login("recepcion.demo", "")));
    }

    private static LoginCommand Login(string username, string password) =>
        new(new LoginDto { Username = username, Password = password });

    private static (ISender Sender, FakeUserRepository Repo) Build()
    {
        var repo = new FakeUserRepository();
        var user = new AppUser
        {
            UserId = 1,
            Username = "recepcion.demo",
            PasswordHash = "hash:" + Password,
            Active = true,
            Employee = new Employee { FirstName = "Rosa", LastName = "Demo", Active = true }
        };
        user.Roles.Add(new Role { Code = "RECEPTION", Name = "Recepción" });
        repo.Users.Add(user);

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddApplication();
        services.AddSingleton<IUserRepository>(repo);
        services.AddSingleton<ICredentialHasher>(new FakeCredentialHasher());
        return (services.BuildServiceProvider().GetRequiredService<ISender>(), repo);
    }

    private sealed class FakeCredentialHasher : ICredentialHasher
    {
        public string Hash(string password) => "hash:" + password;

        public bool Verify(string hash, string password) => hash == "hash:" + password;
    }

    private sealed class FakeUserRepository : IUserRepository
    {
        public List<AppUser> Users { get; } = [];

        public List<long> LoginsRecorded { get; } = [];

        public Task<AppUser?> GetByUsernameAsync(string username, CancellationToken cancellationToken) =>
            Task.FromResult(Users.FirstOrDefault(u => u.Username == username));

        public Task RegisterLoginAsync(
            long userId, DateTime loggedInAtUtc, CancellationToken cancellationToken)
        {
            LoginsRecorded.Add(userId);
            return Task.CompletedTask;
        }
        public Task<List<AppUser>> GetAllAsync(
            CancellationToken cancellationToken)
        {
            return Task.FromResult(Users.ToList());
        }

        public Task<AppUser?> GetByIdAsync(
            long userId,
            CancellationToken cancellationToken)
        {
            return Task.FromResult(
                Users.FirstOrDefault(user => user.UserId == userId));
        }

        public Task<AppUser> CreateAsync(
            AppUser user,
            IEnumerable<string> roleCodes,
            CancellationToken cancellationToken)
        {
            Users.Add(user);
            return Task.FromResult(user);
        }

        public Task<AppUser> UpdateAsync(
            AppUser user,
            IEnumerable<string> roleCodes,
            CancellationToken cancellationToken)
        {
            var index = Users.FindIndex(
                item => item.UserId == user.UserId);

            if (index >= 0)
            {
                Users[index] = user;
            }

            return Task.FromResult(user);
        }
    }
}