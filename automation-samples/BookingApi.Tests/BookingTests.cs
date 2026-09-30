using System.Net;
using BookingApi.Tests.Builders;
using NUnit.Framework;
using RestSharp;

namespace BookingApi.Tests;

[TestFixture]
[Category("Regression")]
public class BookingTests : ApiTestBase
{
    [Test]
    [Category("Smoke")]
    public async Task CreateBooking_WithValidPayload_ReturnsSavedBooking()
    {
        var payload = new BookingBuilder().Build();

        var response = await CreateBookingAsync(payload);

        Assert.Multiple(() =>
        {
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            Assert.That(response.Data!.BookingId, Is.GreaterThan(0));
            Assert.That(response.Data.Booking, Is.EqualTo(payload));
        });
    }

    [Test]
    public async Task GetBooking_AfterCreate_ReturnsSameData()
    {
        var payload = new BookingBuilder().WithDeposit(false).Build();
        var created = await CreateBookingAsync(payload);

        var request = new RestRequest($"/booking/{created.Data!.BookingId}");
        var response = await Client.ExecuteAsync<Booking>(request);

        Assert.Multiple(() =>
        {
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            Assert.That(response.Data, Is.EqualTo(payload));
        });
    }

    [TestCase("Breakfast")]
    [TestCase("Airport transfer")]
    public async Task PartialUpdate_AdditionalNeeds_ChangesOnlyThatField(string needs)
    {
        var original = new BookingBuilder().Build();
        var created = await CreateBookingAsync(original);

        var request = new RestRequest($"/booking/{created.Data!.BookingId}", Method.Patch)
            .AddHeader("Cookie", $"token={await GetTokenAsync()}")
            .AddJsonBody(new { additionalneeds = needs });
        var response = await Client.ExecuteAsync<Booking>(request);

        Assert.Multiple(() =>
        {
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            Assert.That(response.Data, Is.EqualTo(original with { AdditionalNeeds = needs }));
        });
    }

    [Test]
    [Category("Security")]
    public async Task UpdateBooking_WithoutToken_IsForbidden()
    {
        var created = await CreateBookingAsync(new BookingBuilder().Build());

        var request = new RestRequest($"/booking/{created.Data!.BookingId}", Method.Put)
            .AddJsonBody(new BookingBuilder().Build());
        var response = await Client.ExecuteAsync(request);

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Forbidden));
    }

    [Test]
    public async Task GetBooking_ThatDoesNotExist_ReturnsNotFound()
    {
        var response = await Client.ExecuteAsync(new RestRequest("/booking/999999999"));

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
    }
}
