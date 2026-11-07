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
                ServiceUuids = new List<string>(x.ServiceUuids),
                ManufacturerData = x.ManufacturerData.Select(m =>
                    new ManufacturerRecord { CompanyId = m.CompanyId, DataHex = m.DataHex }).ToList()
            }).OrderByDescending(x => x.RssiDbm).ToList();
    }

    internal async Task<string> ScanAsync(int seconds)
    {
        if (seconds < 3 || seconds > 120)
            throw new ArgumentOutOfRangeException(nameof(seconds), "Escolha entre 3 e 120 segundos.");

        var adapter = await BluetoothAdapter.GetDefaultAsync();
        if (adapter is null) throw new InvalidOperationException(
            "Windows não encontrou adaptador Bluetooth. Verifique o Gerenciador de Dispositivos.");
        if (!adapter.IsLowEnergySupported) throw new InvalidOperationException(
            "O adaptador Bluetooth não informa suporte a BLE.");

        using var scan = new ScanSession(this);
        scan.Start();
        await Task.Delay(TimeSpan.FromSeconds(seconds));
        return scan.Stop();
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
                saved.Advertisements++;
                saved.ServiceUuids = saved.ServiceUuids.Union(uuids).Distinct().ToList();
                if (manufacturers.Count > 0) saved.ManufacturerData = manufacturers;
            }
        }
        catch (Exception)
        {
            // Erros de um anúncio isolado não devem interromper a varredura.
        }
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

        internal ScanSession(BleDiagnostics owner)
        {
            _watcher = new BluetoothLEAdvertisementWatcher
            {
                ScanningMode = BluetoothLEScanningMode.Active
            };
            _watcher.Received += (_, e) => owner.Observe(e);
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
