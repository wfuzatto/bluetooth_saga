using Windows.Devices.Bluetooth;
using Windows.Devices.Bluetooth.Advertisement;
using Windows.Devices.Bluetooth.GenericAttributeProfile;
using Windows.Storage.Streams;

namespace SagaBle.Diagnostic;

// Somente descoberta, inspeção de serviços e leitura de metadados.
// Não executa pairing, ReadValue, WriteValue, Subscribe ou comandos de destravamento.
internal sealed class BleDiagnostics
{
    private readonly object _gate = new();
    private readonly Dictionary<ulong, DeviceRecord> _devices = new();

    internal List<DeviceRecord> Snapshot()
    {
        lock (_gate)
            return _devices.Values.Select(x => new DeviceRecord
            {
                AddressValue = x.AddressValue,
                Address = x.Address,
                Name = x.Name,
                RssiDbm = x.RssiDbm,
                FirstSeenUtc = x.FirstSeenUtc,
                LastSeenUtc = x.LastSeenUtc,
                Advertisements = x.Advertisements,
                ConnectableAdvertisements = x.ConnectableAdvertisements,
                ScannableAdvertisements = x.ScannableAdvertisements,
                DirectedAdvertisements = x.DirectedAdvertisements,
                ScanResponseAdvertisements = x.ScanResponseAdvertisements,
                AdvertisementTypes = new List<string>(x.AdvertisementTypes),
                AddressTypes = new List<string>(x.AddressTypes),
                ServiceUuids = new List<string>(x.ServiceUuids),
                ManufacturerData = x.ManufacturerData.Select(m =>
                    new ManufacturerRecord { CompanyId = m.CompanyId, DataHex = m.DataHex }).ToList()
            }).OrderByDescending(x => x.RssiDbm).ToList();
    }

    internal async Task<string> ScanAsync(int seconds)
    {
        ValidateScanDuration(seconds);
        await RequireBleAdapterAsync();

        using var scan = new ScanSession(this);
        scan.Start();
        await Task.Delay(TimeSpan.FromSeconds(seconds));
        return scan.Stop();
    }

    internal async Task<ulong?> ScanForConnectableNameAsync(int seconds, string targetName)
    {
        ValidateScanDuration(seconds);
        if (string.IsNullOrWhiteSpace(targetName))
            throw new ArgumentException("Informe o nome exato anunciado pelo dispositivo.", nameof(targetName));
        await RequireBleAdapterAsync();

        var match = new TaskCompletionSource<ulong>(TaskCreationOptions.RunContinuationsAsynchronously);
        using var scan = new ScanSession(this, e =>
        {
            if (MatchesConnectableName(
                e.Advertisement.LocalName,
                targetName,
                HasConnectableAdvertisement(e.BluetoothAddress)))
                match.TrySetResult(e.BluetoothAddress);
        });
        scan.Start();
        var timeout = Task.Delay(TimeSpan.FromSeconds(seconds));
        var completed = await Task.WhenAny(match.Task, timeout);
        scan.Stop();
        return completed == match.Task ? await match.Task : null;
    }

    internal static bool MatchesConnectableName(string? receivedName, string targetName, bool isConnectable) =>
        isConnectable && !string.IsNullOrWhiteSpace(receivedName) &&
        string.Equals(receivedName, targetName, StringComparison.OrdinalIgnoreCase);

    private static void ValidateScanDuration(int seconds)
    {
        if (seconds < 3 || seconds > 120)
            throw new ArgumentOutOfRangeException(nameof(seconds), "Escolha entre 3 e 120 segundos.");
    }

    private static async Task RequireBleAdapterAsync()
    {
        var adapter = await BluetoothAdapter.GetDefaultAsync();
        if (adapter is null) throw new InvalidOperationException(
            "Windows não encontrou adaptador Bluetooth. Verifique o Gerenciador de Dispositivos.");
        if (!adapter.IsLowEnergySupported) throw new InvalidOperationException(
            "O adaptador Bluetooth não informa suporte a BLE.");
    }

    private bool HasConnectableAdvertisement(ulong address)
    {
        lock (_gate)
            return _devices.TryGetValue(address, out var device) && device.ConnectableAdvertisements > 0;
    }

    private void Observe(BluetoothLEAdvertisementReceivedEventArgs e)
    {
        try
        {
            var name = e.Advertisement.LocalName ?? "";
            var uuids = e.Advertisement.ServiceUuids
                .Select(g => g.ToString("D")).Distinct().ToList();
            var manufacturers = e.Advertisement.ManufacturerData.Select(m =>
                new ManufacturerRecord { CompanyId = m.CompanyId, DataHex = Hex(m.Data) }).ToList();

            lock (_gate)
            {
                if (!_devices.TryGetValue(e.BluetoothAddress, out var saved))
                {
                    saved = new DeviceRecord
                    {
                        AddressValue = e.BluetoothAddress,
                        Address = Addressing.Format(e.BluetoothAddress),
                        FirstSeenUtc = e.Timestamp,
                    };
                    _devices.Add(e.BluetoothAddress, saved);
                }

                if (!string.IsNullOrWhiteSpace(name)) saved.Name = name;
                saved.LastSeenUtc = e.Timestamp;
                saved.RssiDbm = e.RawSignalStrengthInDBm;
                RecordAdvertisement(saved,
                    e.AdvertisementType.ToString(),
                    e.BluetoothAddressType.ToString(),
                    e.IsConnectable,
                    e.IsScannable,
                    e.IsDirected,
                    e.IsScanResponse);
                saved.ServiceUuids = saved.ServiceUuids.Union(uuids).Distinct().ToList();
                if (manufacturers.Count > 0) saved.ManufacturerData = manufacturers;
            }
        }
        catch (Exception)
        {
            // Erros de um anúncio isolado não devem interromper a varredura.
        }
    }

    internal static void RecordAdvertisement(
        DeviceRecord saved,
        string advertisementType,
        string addressType,
        bool isConnectable,
        bool isScannable,
        bool isDirected,
        bool isScanResponse)
    {
        saved.Advertisements++;
        if (isConnectable) saved.ConnectableAdvertisements++;
        if (isScannable) saved.ScannableAdvertisements++;
        if (isDirected) saved.DirectedAdvertisements++;
        if (isScanResponse) saved.ScanResponseAdvertisements++;

        if (!string.IsNullOrWhiteSpace(advertisementType) && !saved.AdvertisementTypes.Contains(advertisementType))
            saved.AdvertisementTypes.Add(advertisementType);
        if (!string.IsNullOrWhiteSpace(addressType) && !saved.AddressTypes.Contains(addressType))
            saved.AddressTypes.Add(addressType);
    }

    private static string Hex(IBuffer buffer)
    {
        using var reader = DataReader.FromBuffer(buffer);
        byte[] data = new byte[(int)buffer.Length];
        reader.ReadBytes(data);
        return Convert.ToHexString(data);
    }

    internal async Task<GattInspection> InspectAsync(ulong address)
    {
        var report = new GattInspection { Address = Addressing.Format(address) };

        try
        {
            using var device = await BluetoothLEDevice.FromBluetoothAddressAsync(address);
            if (device is null)
            {
                report.Status = "Dispositivo não encontrado pelo Windows. Refaça a varredura próximo à fechadura.";
                return report;
            }

            report.Name = device.Name;
            // Uncached realiza descoberta no dispositivo; não escreve em nenhuma característica.
            var servicesResult = await device.GetGattServicesAsync(BluetoothCacheMode.Uncached);
            report.Status = servicesResult.Status.ToString();
            if (servicesResult.Status != GattCommunicationStatus.Success)
                return report;

            foreach (var service in servicesResult.Services)
            {
                using (service)
                {
                    var entry = new ServiceRecord { Uuid = service.Uuid.ToString("D") };
                    report.Services.Add(entry);
                    try
                    {
                        var characteristics = await service.GetCharacteristicsAsync(BluetoothCacheMode.Uncached);
                        entry.Status = characteristics.Status.ToString();
                        if (characteristics.Status != GattCommunicationStatus.Success) continue;

                        foreach (var characteristic in characteristics.Characteristics)
                        {
                            entry.Characteristics.Add(new CharacteristicRecord
                            {
                                Uuid = characteristic.Uuid.ToString("D"),
                                Properties = characteristic.CharacteristicProperties.ToString(),
                                Handle = characteristic.AttributeHandle
                            });
                        }
                    }
                    catch (Exception ex)
                    {
                        entry.Status = "Acesso negado ou falha GATT: " + ex.Message;
                    }
                }
            }
        }
        catch (Exception ex)
        {
            report.Status = "Falha de comunicação: " + ex.Message;
        }
        return report;
    }

    private sealed class ScanSession : IDisposable
    {
        private readonly BluetoothLEAdvertisementWatcher _watcher;
        private bool _stopped;

        internal ScanSession(BleDiagnostics owner, Action<BluetoothLEAdvertisementReceivedEventArgs>? onReceived = null)
        {
            _watcher = new BluetoothLEAdvertisementWatcher
            {
                ScanningMode = BluetoothLEScanningMode.Active
            };
            _watcher.Received += (_, e) =>
            {
                owner.Observe(e);
                onReceived?.Invoke(e);
            };
        }

        internal void Start() => _watcher.Start();

        internal string Stop()
        {
            if (!_stopped)
            {
                _stopped = true;
                _watcher.Stop();
            }
            return _watcher.Status.ToString();
        }

        public void Dispose() => Stop();
    }
}
