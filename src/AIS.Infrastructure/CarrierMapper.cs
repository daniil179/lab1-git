using System.Globalization;
using System.Text.RegularExpressions;
using AIS.Core;
namespace AIS.Infrastructure;
public static class CarrierMapper
{
    public static TrackingEvent Map(SourceEvent s)
    {
        string track = (s.TrackNumber ?? "").Trim().ToUpperInvariant();
        string provider = (s.Provider ?? "").Trim().ToLowerInvariant();
        if (provider != "carrier-a") throw new ArgumentException("Unknown provider");
        if (!Regex.IsMatch(s.EventId ?? "", @"\Aevt-[0-9]{4}\z"))
            throw new ArgumentException("Invalid eventId");
        if (!Regex.IsMatch(track, @"\ALH-[0-9]{4}-[0-9]{6}\z"))
            throw new ArgumentException("Invalid tracking number");
        TrackingStatus status = s.State switch
        {
            "ARRIVAL" => TrackingStatus.Received,
            "TRANSIT" => TrackingStatus.InTransit,
            "DELIVERED" => TrackingStatus.Delivered,
            _ => throw new ArgumentException("Unknown status")
        };
        if (!Regex.IsMatch(s.WarehouseCode ?? "", @"\AW[0-9]{2}\z"))
            throw new ArgumentException("Invalid warehouse");
        if (!Regex.IsMatch(s.EventTime ?? "", @"\A[0-9]{4}-[0-9]{2}-[0-9]{2}T[0-9]{2}:[0-9]{2}:[0-9]{2}(\.[0-9]{1,7})?(Z|[+-][0-9]{2}:[0-9]{2})\z") ||
            !DateTimeOffset.TryParse(s.EventTime, CultureInfo.InvariantCulture,
                DateTimeStyles.None, out var at)) throw new ArgumentException("Missing offset");
        if (!decimal.TryParse(s.Weight, NumberStyles.AllowDecimalPoint,
                CultureInfo.InvariantCulture, out decimal weight) || weight <= 0)
            throw new ArgumentException("Invalid weight");
        decimal kg = s.Unit switch
        {
            "g" => weight / 1000m,
            "kg" => weight,
            _ => throw new ArgumentException("Unknown unit")
        };
        if (kg > 10000m || kg != decimal.Round(kg, 3))
            throw new ArgumentException("Weight range or precision");
        return new TrackingEvent(provider, s.EventId!, track, status,
            s.WarehouseCode!, at.ToUniversalTime(), kg);
    }
}
