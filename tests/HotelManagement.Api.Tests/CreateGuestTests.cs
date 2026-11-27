using HotelManagement.Application.Configuration;
using HotelManagement.Application.Dtos.Guests;
using HotelManagement.Application.Exceptions;
using HotelManagement.Application.UseCases.Guests.Commands;
using HotelManagement.Domain.Entities;
using HotelManagement.Domain.Exceptions;
using HotelManagement.Domain.Repositories;
using MediatR;
using Microsoft.Extensions.DependencyInjection;

namespace HotelManagement.Api.Tests;

public class CreateGuestTests
{
    [Fact]
    public async Task Valid_guest_is_created_with_generated_id()
    {
        var (sender, repo) = Build();

        var result = await sender.Send(new CreateGuestCommand(new CreateGuestDto
        {
            FirstName = "  Ana ",
            LastName = "Pérez",
            DocumentType = "DNI",
            DocumentNumber = "12345678"
        }));

        Assert.Equal(1, result.Id);
        Assert.Equal("Ana", result.FirstName);
        Assert.Single(repo.Guests);
    }

    [Fact]
    public async Task Duplicate_document_throws_conflict()
    {
        var (sender, _) = Build();
        var dto = new CreateGuestDto
        {
            FirstName = "Ana", LastName = "Pérez",
            DocumentType = "DNI", DocumentNumber = "12345678"
        };

        await sender.Send(new CreateGuestCommand(dto));

        await Assert.ThrowsAsync<BusinessConflictException>(
            () => sender.Send(new CreateGuestCommand(dto)));
    }

    [Fact]
    public async Task Document_type_without_number_is_rejected()
    {
        var (sender, repo) = Build();

        await Assert.ThrowsAsync<BusinessValidationException>(
            () => sender.Send(new CreateGuestCommand(new CreateGuestDto
            {
                FirstName = "Ana", LastName = "Pérez", DocumentType = "DNI"
            })));

        Assert.Empty(repo.Guests);
    }

    [Fact]
    public async Task Guests_without_document_can_be_registered_twice()
    {
        var (sender, repo) = Build();
        var dto = new CreateGuestDto { FirstName = "Ana", LastName = "Pérez" };

        await sender.Send(new CreateGuestCommand(dto));
        await sender.Send(new CreateGuestCommand(dto));

        Assert.Equal(2, repo.Guests.Count);
    }

    private static (ISender Sender, FakeGuestRepository Repo) Build()
    {
        var repo = new FakeGuestRepository();
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddApplication();
        services.AddSingleton<HotelManagement.Application.Transactions.IUnitOfWork, FakeUnitOfWork>();
        services.AddSingleton<IGuestRepository>(repo);
        return (services.BuildServiceProvider().GetRequiredService<ISender>(), repo);
    }
}
