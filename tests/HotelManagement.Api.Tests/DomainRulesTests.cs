using HotelManagement.Domain.Exceptions;
using HotelManagement.Domain.Rules;

namespace HotelManagement.Api.Tests;

public sealed class DomainRulesTests
{
    [Theory]
    [InlineData(0, 0, 2, 1)]
    [InlineData(3, 0, 2, 1)]
    [InlineData(1, -1, 2, 1)]
    [InlineData(1, 2, 2, 1)]
    public void Capacity_rejects_values_outside_the_room_type(int adults, int children, int maxAdults, int maxChildren) =>
        Assert.Throws<BusinessValidationException>(() => ReservationRules.EnsureCapacity(adults, children, maxAdults, maxChildren));

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Departure_must_be_after_arrival(int days) => Assert.Throws<BusinessValidationException>(() =>
        ReservationRules.EnsureValidPeriod(new DateOnly(2030, 1, 2), new DateOnly(2030, 1, 2).AddDays(days)));

    [Fact]
    public void Check_in_uses_the_hotel_date_instead_of_the_UTC_date()
    {
        // 27/11 UTC todavía es 26/11 en el hotel de Lima.
        var instant = new DateTime(2026, 11, 27, 0, 5, 0, DateTimeKind.Utc);
        StayRules.EnsureCheckInPeriod(instant, "America/Lima", new DateOnly(2026, 11, 26), new DateOnly(2026, 11, 27));
        Assert.Throws<BusinessConflictException>(() => StayRules.EnsureCheckInPeriod(
            instant, "America/Lima", new DateOnly(2026, 11, 27), new DateOnly(2026, 11, 28)));
    }

    [Fact]
    public void Departure_day_is_excluded_from_the_check_in_period() => Assert.Throws<BusinessConflictException>(() =>
        StayRules.EnsureCheckInPeriod(new DateTime(2030, 1, 2, 12, 0, 0, DateTimeKind.Utc),
            "UTC", new DateOnly(2030, 1, 1), new DateOnly(2030, 1, 2)));

    [Theory]
    [InlineData("CHECKED_IN")]
    [InlineData("CANCELLED")]
    [InlineData("COMPLETED")]
    public void Check_in_rejects_reservations_in_a_terminal_or_occupied_state(string status) =>
        Assert.Throws<BusinessConflictException>(() => StayRules.EnsureReservationAllowsCheckIn(status));

    [Theory]
    [InlineData(229.98, false)]
    [InlineData(229.99, true)]
    [InlineData(230.00, true)]
    [InlineData(231.00, true)]
    public void Check_out_respects_the_existing_one_cent_tolerance(double paid, bool allowed)
    {
        void Validate() => StayRules.EnsureSettledBalance(new DateOnly(2030, 1, 1),
            new DateOnly(2030, 1, 3), 100m, 30m, (decimal)paid);
        if (allowed) Validate();
        else Assert.Throws<BusinessConflictException>(Validate);
    }
}
