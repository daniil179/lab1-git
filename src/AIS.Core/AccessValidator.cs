using System.Globalization;
using System.Text.RegularExpressions;
namespace AIS.Core;
public static class AccessValidator
{
    public static IReadOnlyList<string> Validate(AccessRequest? r, DateTimeOffset now)
    {
        if (r is null) return new[] { "body" };
        var errors = new List<string>();
        if (!Guid.TryParseExact(r.RequestId, "D", out var id) || id == Guid.Empty)
            errors.Add("requestId");
        if (!Regex.IsMatch(r.DeviceId ?? "", @"\ASCN-[0-9]{2}\z")) errors.Add("deviceId");
        if (!Regex.IsMatch(r.WarehouseId ?? "", @"\AW[0-9]{2}\z")) errors.Add("warehouseId");
        if (r.RequestedZone is not ("RECEIVING" or "PICKING" or "SHIPPING"))
            errors.Add("requestedZone");
        string value = r.CredentialValue ?? "";
        bool validCredential = r.CredentialType switch
        {
            "rfid" => Regex.IsMatch(value, @"\A[0-9A-F]{24}\z"),
            "qr" => value.StartsWith("PASS-", StringComparison.Ordinal) &&
                Guid.TryParseExact(value[5..], "D", out var ticket) && ticket != Guid.Empty,
            _ => false
        };
        if (!validCredential) errors.Add("credential");
        bool offset = Regex.IsMatch(r.OccurredAt ?? "", @"\A[0-9]{4}-[0-9]{2}-[0-9]{2}T[0-9]{2}:[0-9]{2}:[0-9]{2}(\.[0-9]{1,7})?(Z|[+-][0-9]{2}:[0-9]{2})\z");
        if (!offset || !DateTimeOffset.TryParse(r.OccurredAt,
            CultureInfo.InvariantCulture, DateTimeStyles.None, out var at) ||
            (now - at).Duration() > TimeSpan.FromMinutes(2)) errors.Add("occurredAt");
        return errors;
    }
}
