using System.Globalization;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using TimeClock.Core.Interfaces.Services;

namespace TimeClock.Infrastructure.ExternalServices;

/// <summary>
/// Fallback external time source. Calls timeapi.io when worldtimeapi.org is unavailable.
/// Contains no retry logic — that is handled by TimeProviderService in the Services layer.
/// </summary>
public class TimeApiIoClient : IExternalTimeClient
{
    private const string Endpoint = "api/time/current/zone?timeZone=Europe%2FZurich";

    private readonly HttpClient _httpClient;

    public string SourceName => "timeapi.io";

    public TimeApiIoClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<DateTimeOffset> GetCurrentZurichTimeAsync()
    {
        var response = await _httpClient.GetFromJsonAsync<TimeApiIoResponse>(Endpoint)
            ?? throw new InvalidOperationException("Received a null response from timeapi.io.");

        // timeapi.io returns the local Zurich time without a UTC offset, e.g.
        // "2024-11-15T14:23:45.1234567". We reconstruct the DateTimeOffset by
        // looking up the Zurich offset from the system timezone database (IANA IDs
        // are supported on all platforms in .NET 6+).
        var localTime = DateTime.Parse(
            response.DateTime,
            CultureInfo.InvariantCulture,
            DateTimeStyles.RoundtripKind);

        var zurichTz = TimeZoneInfo.FindSystemTimeZoneById("Europe/Zurich");
        var utcOffset = zurichTz.GetUtcOffset(localTime);

        return new DateTimeOffset(localTime, utcOffset);
    }

    // ── Private response model ────────────────────────────────────────────────

    private sealed record TimeApiIoResponse(
        [property: JsonPropertyName("dateTime")] string DateTime);
}
