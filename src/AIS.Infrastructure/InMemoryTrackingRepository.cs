using AIS.Core;
namespace AIS.Infrastructure;
public sealed class InMemoryTrackingRepository : ITrackingRepository
{
    private readonly object _gate = new();
    private readonly Dictionary<(string, string), TrackingEvent> _events = new();
    public StoreOutcome Add(TrackingEvent item)
    {
        lock (_gate)
        {
            var key = (item.Source, item.EventId);
            if (_events.TryGetValue(key, out var old))
                return old == item ? StoreOutcome.Duplicate : StoreOutcome.Conflict;
            _events.Add(key, item);
            return StoreOutcome.Stored;
        }
    }
    public TrackingPage? Get(string trackingNumber, int limit)
    {
        if (limit is < 1 or > 100) throw new ArgumentOutOfRangeException(nameof(limit));
        lock (_gate)
        {
            var events = _events.Values.Where(e => e.TrackingNumber == trackingNumber)
                .OrderByDescending(e => e.OccurredAt).ThenBy(e => e.EventId).ToArray();
            return events.Length == 0 ? null : new TrackingPage(trackingNumber,
                events.Take(limit).ToArray(), limit);
        }
    }
}
