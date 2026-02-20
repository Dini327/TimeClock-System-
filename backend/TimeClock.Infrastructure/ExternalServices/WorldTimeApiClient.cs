using System.Globalization;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using TimeClock.Core.Interfaces.Services;

namespace TimeClock.Infrastructure.ExternalServices;

/// <summary>
/// Primary external time source. Calls worldtimeapi.org to retrieve the
/// current Europe/Zurich time. Contains no retry logic — that is handled
/// by TimeProviderService in the Services layer using Polly.
/// </summary>
public class WorldTimeApiClient : IExternalTimeClient
{
    private const string Endpoint = "api/timezone/Europe/Zurich";

    private readonly HttpClient _httpClient;

    public string SourceName => "worldtimeapi.org";

    public WorldTimeApiClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<DateTimeOffset> GetCurrentZurichTimeAsync()
    {
        var response = await _httpClient.GetFromJsonAsync<WorldTimeApiResponse>(Endpoint)
            ?? throw new InvalidOperationException("Received a null response from worldtimeapi.org.");

        // The "datetime" field is a full ISO 8601 string including the Zurich UTC offset,
        // e.g. "2024-11-15T14:23:45.123456+01:00". DateTimeOffset.Parse handles it directly.
        return DateTimeOffset.Parse(response.DateTime, CultureInfo.InvariantCulture);
    }

    // ── Private response model ────────────────────────────────────────────────

    private sealed record WorldTimeApiResponse(
        [property: JsonPropertyName("datetime")] string DateTime);
}
