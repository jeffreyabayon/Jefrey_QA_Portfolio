using System.Text.Json.Serialization;
using Bogus;

namespace BookingApi.Tests.Builders;

public record BookingDates(
    [property: JsonPropertyName("checkin")] string CheckIn,
    [property: JsonPropertyName("checkout")] string CheckOut);

public record Booking(
    [property: JsonPropertyName("firstname")] string FirstName,
    [property: JsonPropertyName("lastname")] string LastName,
    [property: JsonPropertyName("totalprice")] int TotalPrice,
    [property: JsonPropertyName("depositpaid")] bool DepositPaid,
    [property: JsonPropertyName("bookingdates")] BookingDates BookingDates,
    [property: JsonPropertyName("additionalneeds")] string AdditionalNeeds);

public record CreatedBooking(
    [property: JsonPropertyName("bookingid")] int BookingId,
    [property: JsonPropertyName("booking")] Booking Booking);

/// <summary>
/// Builds realistic, randomized bookings so every test run uses fresh data.
/// Override only the fields a test actually cares about.
/// </summary>
public class BookingBuilder
{
    private static readonly Faker Faker = new();
    private Booking _booking;

    public BookingBuilder()
    {
        var checkIn = Faker.Date.SoonDateOnly(30);
        _booking = new Booking(
            FirstName: Faker.Name.FirstName(),
            LastName: Faker.Name.LastName(),
            TotalPrice: Faker.Random.Int(50, 2000),
            DepositPaid: Faker.Random.Bool(),
            BookingDates: new BookingDates(
                checkIn.ToString("yyyy-MM-dd"),
                checkIn.AddDays(Faker.Random.Int(1, 14)).ToString("yyyy-MM-dd")),
            AdditionalNeeds: Faker.PickRandom("Breakfast", "Late checkout", "Parking"));
    }

    public BookingBuilder WithGuest(string firstName, string lastName)
    {
        _booking = _booking with { FirstName = firstName, LastName = lastName };
        return this;
    }

    public BookingBuilder WithDeposit(bool paid)
    {
        _booking = _booking with { DepositPaid = paid };
        return this;
    }

    public BookingBuilder WithStay(DateOnly checkIn, int nights)
    {
        _booking = _booking with
        {
            BookingDates = new BookingDates(
                checkIn.ToString("yyyy-MM-dd"),
                checkIn.AddDays(nights).ToString("yyyy-MM-dd"))
        };
        return this;
    }

    public Booking Build() => _booking;
}
