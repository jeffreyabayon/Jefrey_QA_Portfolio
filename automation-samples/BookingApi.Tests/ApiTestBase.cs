using System.Net;
using System.Text.Json.Serialization;
using BookingApi.Tests.Builders;
using NUnit.Framework;
using RestSharp;

namespace BookingApi.Tests;

/// <summary>
/// Shared setup for every API test: one HTTP client, a cached auth token,
/// and automatic cleanup of any booking a test creates.
/// </summary>
public abstract class ApiTestBase
{
    protected static readonly RestClient Client = new(new RestClientOptions(
        TestContext.Parameters.Get("BaseUrl", "https://restful-booker.herokuapp.com")));

    private static string? _token;
    private readonly List<int> _createdIds = new();

    // The API replies 418 "I'm a Teapot" unless Accept is exactly application/json
    static ApiTestBase() => Client.AddDefaultHeader("Accept", "application/json");

    protected static async Task<string> GetTokenAsync()
    {
        if (_token is not null) return _token;

        // Public demo credentials from the Restful-Booker docs; real suites read these from RunSettings/CI secrets
        var request = new RestRequest("/auth", Method.Post).AddJsonBody(new
        {
            username = TestContext.Parameters.Get("ApiUser", "admin"),
            password = TestContext.Parameters.Get("ApiPassword", "password123"),
        });
        var response = await Client.ExecuteAsync<AuthResponse>(request);

        Assert.That(response.Data?.Token, Is.Not.Null.And.Not.Empty, "Auth failed: check RunSettings");
        return _token = response.Data!.Token;
    }

    protected async Task<RestResponse<CreatedBooking>> CreateBookingAsync(Booking payload)
    {
        var request = new RestRequest("/booking", Method.Post).AddJsonBody(payload);
        var response = await Client.ExecuteAsync<CreatedBooking>(request);

        if (response.Data is { BookingId: > 0 } created) _createdIds.Add(created.BookingId);
        return response;
    }

    [TearDown]
    public async Task CleanUpCreatedBookings()
    {
        if (_createdIds.Count == 0) return;
        var token = await GetTokenAsync();

        foreach (var id in _createdIds)
        {
            var request = new RestRequest($"/booking/{id}", Method.Delete)
                .AddHeader("Cookie", $"token={token}");
            var response = await Client.ExecuteAsync(request);

            // Restful-Booker answers a successful delete with 201 Created
            if (response.StatusCode != HttpStatusCode.Created)
                TestContext.Progress.WriteLine($"Cleanup failed for booking {id}: {(int)response.StatusCode}");
        }
        _createdIds.Clear();
    }
}

public record AuthResponse([property: JsonPropertyName("token")] string Token);
