using AIS.Core;
using AIS.Infrastructure;
using AIS.UI;
using System.Text.Json;
using System.Text.Json.Serialization;
var results = new List<object>();
int total = 0, failed = 0;
void Check(string name, Action action)
{
    total++;
    try { action(); Console.WriteLine("PASS " + name); results.Add(new { name, passed = true, error = (string?)null }); }
    catch (Exception ex) { failed++; Console.WriteLine("FAIL " + name + ": " + ex.Message); results.Add(new { name, passed = false, error = ex.Message }); }
}
void Require(bool value) { if (!value) throw new Exception("Assertion failed"); }
void Reject(SourceEvent x)
{
    try { CarrierMapper.Map(x); throw new Exception("Mapping accepted invalid input"); }
    catch (ArgumentException) { }
}
var src = new SourceEvent("carrier-a", "evt-0003", " lh-2026-000003 ",
    "ARRIVAL", "W01", "2026-11-04T09:00:00+03:00", "1250", "g");
var mapped = CarrierMapper.Map(src);
Check("M01_TrackNormalization", () => Require(mapped.TrackingNumber == "LH-2026-000003"));
Check("M02_WeightGramsToKg", () => Require(mapped.WeightKg == 1.250m));
Check("M03_TimeToUtc", () => Require(mapped.OccurredAt.Hour == 6 && mapped.OccurredAt.Offset == TimeSpan.Zero));
Check("M04_StateMapping", () => Require(mapped.Status == TrackingStatus.Received));
Check("M05_UnknownStatusRejected", () => Reject(src with { State = "LOST" }));
Check("M06_CommaWeightRejected", () => Reject(src with { Weight = "1,25", Unit = "kg" }));
Check("M07_MissingOffsetRejected", () => Reject(src with { EventTime = "2026-11-04T09:00:00" }));
Check("M08_UnknownUnitRejected", () => Reject(src with { Unit = "lb" }));
Check("M09_NegativeWeightRejected", () => Reject(src with { Weight = "-1" }));
Check("M10_PrecisionRejected", () => Reject(src with { Weight = "1.2345", Unit = "kg" }));
Check("M11_InvalidTrackRejected", () => Reject(src with { TrackNumber = "abc" }));
Check("M12_InvalidDateRejected", () => Reject(src with { EventTime = "2026-02-30T09:00:00Z" }));
var repo = new InMemoryTrackingRepository();
Check("R01_Insert", () => Require(repo.Add(mapped) == StoreOutcome.Stored));
Check("R02_Duplicate", () => Require(repo.Add(mapped) == StoreOutcome.Duplicate && repo.Get(mapped.TrackingNumber, 50)!.Events.Count == 1));
Check("R03_Conflict", () => Require(repo.Add(mapped with { WeightKg = 2 }) == StoreOutcome.Conflict));
Check("R04_LateEventDoesNotReplaceLatest", () =>
{
    repo.Add(mapped with { EventId = "evt-0002", OccurredAt = mapped.OccurredAt.AddHours(-1), Status = TrackingStatus.InTransit });
    Require(repo.Get(mapped.TrackingNumber, 1)!.Events[0] == mapped);
});
var now = DateTimeOffset.Parse("2026-11-04T06:00:00Z");
var request = new AccessRequest("11111111-1111-4111-8111-111111111111", "SCN-01", "rfid", "E2000017221101441890ABCD", "W01", "PICKING", "2026-11-04T09:00:00+03:00");
void Valid(AccessRequest? r) => Require(AccessValidator.Validate(r, now).Count == 0);
void Invalid(AccessRequest? r) => Require(AccessValidator.Validate(r, now).Count > 0);
Check("A01_RfidValid", () => Valid(request));
Check("A02_QrValid", () => Valid(request with { CredentialType = "qr", CredentialValue = "PASS-22222222-2222-4222-8222-222222222222" }));
Check("A03_NullBody", () => Invalid(null));
Check("A04_EmptyRequestId", () => Invalid(request with { RequestId = "" }));
Check("A05_BadDevice", () => Invalid(request with { DeviceId = "SCN-100" }));
Check("A06_BadRfid", () => Invalid(request with { CredentialValue = "E200" }));
Check("A07_BadWarehouse", () => Invalid(request with { WarehouseId = "W1" }));
Check("A08_UnknownZone", () => Invalid(request with { RequestedZone = "ADMIN" }));
Check("A09_Expired", () => Invalid(request with { OccurredAt = "2026-11-04T05:57:59Z" }));
Check("A10_Future", () => Invalid(request with { OccurredAt = "2026-11-04T06:02:01Z" }));
Check("A11_MissingOffset", () => Invalid(request with { OccurredAt = "2026-11-04T06:00:00" }));
Check("A12_Boundary120Seconds", () => Valid(request with { OccurredAt = "2026-11-04T05:58:00Z" }));
Check("V01_LoadHistory", () =>
{
    var vm = new TrackingViewModel(new FakeClient(new TrackingPage(mapped.TrackingNumber, new[] { mapped }, 50), false));
    vm.LoadAsync().GetAwaiter().GetResult(); Require(vm.Events.Count == 1 && !vm.IsBusy && vm.Error is null);
});
Check("V02_NetworkError", () =>
{
    var vm = new TrackingViewModel(new FakeClient(null, true));
    vm.LoadAsync().GetAwaiter().GetResult(); Require(vm.Events.Count == 0 && !vm.IsBusy && vm.Error is not null);
});
var opt = new JsonSerializerOptions { WriteIndented = true, PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) } };
string output = Path.GetFullPath(args.Length > 0 ? args[0] : "artifacts"); Directory.CreateDirectory(output);
File.WriteAllText(Path.Combine(output, "checks.json"), JsonSerializer.Serialize(new { total, passed = total - failed, failed, results }, opt));
File.WriteAllText(Path.Combine(output, "mapped-event.json"), JsonSerializer.Serialize(mapped, opt));
Console.WriteLine($"TOTAL {total}; PASSED {total - failed}; FAILED {failed}");
return failed == 0 ? 0 : 1;
sealed class FakeClient(TrackingPage? page, bool fail) : ITrackingClient
{
    public Task<TrackingPage> GetAsync(string trackingNumber, int limit, CancellationToken token) => fail
        ? Task.FromException<TrackingPage>(new HttpRequestException("Simulated network failure"))
        : Task.FromResult(page!);
}
