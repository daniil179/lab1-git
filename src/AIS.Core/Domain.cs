namespace AIS.Core;
public enum TrackingStatus { Received, InTransit, Delivered }
public sealed record SourceEvent(string Provider, string EventId, string TrackNumber,
    string State, string WarehouseCode, string EventTime, string Weight, string Unit);
public sealed record TrackingEvent(string Source, string EventId, string TrackingNumber,
    TrackingStatus Status, string WarehouseId, DateTimeOffset OccurredAt, decimal WeightKg);
public sealed record TrackingPage(string TrackingNumber, IReadOnlyList<TrackingEvent> Events, int Limit);
public enum StoreOutcome { Stored, Duplicate, Conflict }
public interface ITrackingRepository
{
    StoreOutcome Add(TrackingEvent item);
    TrackingPage? Get(string trackingNumber, int limit);
}
public interface ITrackingClient
{
    Task<TrackingPage> GetAsync(string trackingNumber, int limit, CancellationToken token);
}
public sealed record AccessRequest(string RequestId, string DeviceId, string CredentialType,
    string CredentialValue, string WarehouseId, string RequestedZone, string OccurredAt);
