using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using AIS.Core;
namespace AIS.Infrastructure;
public sealed class HttpTrackingClient(HttpClient http) : ITrackingClient
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web)
    { Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) } };
    public async Task<TrackingPage> GetAsync(string track, int limit, CancellationToken token)
    {
        var response = await http.GetAsync("api/v1/shipments/" +
            Uri.EscapeDataString(track) + "/events?limit=" + limit, token);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<TrackingPage>(Json, token)
            ?? throw new HttpRequestException("Empty response");
    }
}
