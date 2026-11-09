using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using AIS.Core;
using AIS.Infrastructure;
var builder = WebApplication.CreateBuilder(args);
builder.Services.ConfigureHttpJsonOptions(o => o.SerializerOptions.Converters.Add(
    new JsonStringEnumConverter(JsonNamingPolicy.CamelCase)));
builder.Services.AddSingleton<ITrackingRepository>(_ =>
{
    var repo = new InMemoryTrackingRepository();
    repo.Add(CarrierMapper.Map(new SourceEvent("carrier-a", "evt-0003",
        "LH-2026-000003", "ARRIVAL", "W01", "2026-11-04T09:00:00+03:00", "1250", "g")));
    return repo;
});
var app = builder.Build();
string expected = Environment.GetEnvironmentVariable("LOGISTICS_DEMO_TOKEN") ?? "";
app.Use(async (ctx, next) =>
{
    if (ctx.Request.Path.StartsWithSegments("/api"))
    {
        string header = ctx.Request.Headers.Authorization.ToString();
        string token = header.StartsWith("Bearer ", StringComparison.Ordinal) ? header[7..] : "";
        if (expected.Length == 0 || !CryptographicOperations.FixedTimeEquals(
            Encoding.UTF8.GetBytes(token), Encoding.UTF8.GetBytes(expected)))
        {
            ctx.Response.StatusCode = 401;
            ctx.Response.Headers.WWWAuthenticate = "Bearer";
            await ctx.Response.WriteAsJsonAsync(new { code = "UNAUTHORIZED", message = "Bearer token required" });
            return;
        }
    }
    await next(ctx);
});
app.MapGet("/api/v1/shipments/{trackingNumber}/events",
    (string trackingNumber, int? limit, ITrackingRepository repo) =>
{
    if (!Regex.IsMatch(trackingNumber, @"\ALH-[0-9]{4}-[0-9]{6}\z") || limit is < 1 or > 100)
        return Results.BadRequest(new { code = "VALIDATION_ERROR", message = "Invalid track or limit" });
    var page = repo.Get(trackingNumber, limit ?? 50);
    return page is null
        ? Results.NotFound(new { code = "SHIPMENT_NOT_FOUND", message = "Shipment not found" })
        : Results.Ok(page);
});
app.MapGet("/health", () => Results.Ok(new { status = "ok" }));
app.Run();
