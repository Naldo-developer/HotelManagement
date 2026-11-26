using HotelManagement.Application.Configuration;
using HotelManagement.Application.Dtos.Guests;
using HotelManagement.Application.Exceptions;
using HotelManagement.Application.UseCases.Guests.Commands;
using HotelManagement.Domain.Entities;
using HotelManagement.Domain.Repositories;
using MediatR;
using Microsoft.Extensions.DependencyInjection;

namespace HotelManagement.Api.Tests;

public class UpdateGuestTests
{
    [Fact]
    public async Task Update_keeps_id_and_changes_data()
    {
        var (sender, repo) = Build();

        var result = await sender.Send(new UpdateGuestCommand(1, new UpdateGuestDto
        {
            FirstName = "Ana",
            LastName = "Gómez",
            DocumentType = "DNI",
            DocumentNumber = "111"
        }));

        Assert.Equal(1, result.Id);
        Assert.Equal("Gómez", result.LastName);
        Assert.Equal("Gómez", repo.Guests[0].LastName);
    }

    [Fact]
    public async Task Unknown_id_throws_not_found()
    {
        var (sender, _) = Build();

        await Assert.ThrowsAsync<ResourceNotFoundException>(
            () => sender.Send(new UpdateGuestCommand(99, ValidDto())));
    }

    [Fact]
    public async Task Document_of_another_guest_throws_conflict()
    {
        var (sender, _) = Build();

        var dto = ValidDto();
        dto.DocumentNumber = "222";

        await Assert.ThrowsAsync<BusinessConflictException>(
            () => sender.Send(new UpdateGuestCommand(1, dto)));
    }

    [Fact]
    public async Task Document_type_without_number_is_rejected()
    {
        var (sender, _) = Build();

        var dto = ValidDto();
        dto.DocumentNumber = null;

        await Assert.ThrowsAsync<BusinessValidationException>(
            () => sender.Send(new UpdateGuestCommand(1, dto)));
    }

    private static UpdateGuestDto ValidDto() => new()
    {
        FirstName = "Ana",
        LastName = "Pérez",
        DocumentType = "DNI",
        DocumentNumber = "111"
    };

    private static (ISender Sender, FakeGuestRepository Repo) Build()
    {
        var repo = new FakeGuestRepository();
        repo.Guests.Add(new Guest
        {
            GuestId = 1, FirstName = "Ana", LastName = "Pérez",
            DocumentType = "DNI", DocumentNumber = "111"
        });
        repo.Guests.Add(new Guest
        {
            GuestId = 2, FirstName = "Luis", LastName = "Rojas",
            DocumentType = "DNI", DocumentNumber = "222"
        });

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddApplication();
        services.AddSingleton<IGuestRepository>(repo);
        return (services.BuildServiceProvider().GetRequiredService<ISender>(), repo);
    }
}