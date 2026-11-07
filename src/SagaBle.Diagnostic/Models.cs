namespace SagaBle.Diagnostic;

public sealed class ManufacturerRecord
{
    public ushort CompanyId { get; set; }
    public string DataHex { get; set; } = "";
}

public sealed class DeviceRecord
{
    public ulong AddressValue { get; set; }
    public string Address { get; set; } = "";
    public string Name { get; set; } = "";
    public short RssiDbm { get; set; }
    public DateTimeOffset FirstSeenUtc { get; set; }
    public DateTimeOffset LastSeenUtc { get; set; }
    public int Advertisements { get; set; }
    public int ConnectableAdvertisements { get; set; }
    public int ScannableAdvertisements { get; set; }
    public int DirectedAdvertisements { get; set; }
    public int ScanResponseAdvertisements { get; set; }
    public List<string> AdvertisementTypes { get; set; } = new();
    public List<string> AddressTypes { get; set; } = new();
    public List<string> ServiceUuids { get; set; } = new();
    public List<ManufacturerRecord> ManufacturerData { get; set; } = new();
}

public sealed class CharacteristicRecord
{
    public string Uuid { get; set; } = "";
    public string Properties { get; set; } = "";
    public ushort Handle { get; set; }
}

public sealed class ServiceRecord
{
    public string Uuid { get; set; } = "";
    public string Status { get; set; } = "";
    public List<CharacteristicRecord> Characteristics { get; set; } = new();
}

public sealed class GattInspection
{
    public string Address { get; set; } = "";
    public string Name { get; set; } = "";
    public string Status { get; set; } = "";
    public DateTimeOffset InspectedUtc { get; set; } = DateTimeOffset.UtcNow;
    public List<ServiceRecord> Services { get; set; } = new();
}

public sealed class DiagnosticReport
{
    public string Project { get; set; } = "bluetooth_saga";
    public string Mode { get; set; } = "somente leitura / metadados BLE e GATT";
    public DateTimeOffset GeneratedUtc { get; set; } = DateTimeOffset.UtcNow;
    public List<DeviceRecord> Devices { get; set; } = new();
    public List<GattInspection> Inspections { get; set; } = new();
}
