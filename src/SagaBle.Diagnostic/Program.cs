using System.Text;
using System.Text.Json;

namespace SagaBle.Diagnostic;

internal static class Program
{
    private static async Task<int> Main(string[] args)
    {
        Console.OutputEncoding = Encoding.UTF8;
        if (args.Contains("--self-test", StringComparer.OrdinalIgnoreCase))
            return SelfTests.Run() ? 0 : 1;

        if (args.Contains("--help", StringComparer.OrdinalIgnoreCase) || args.Contains("-h"))
        {
            Help();
            return 0;
        }

        if (!OperatingSystem.IsWindowsVersionAtLeast(10, 0, 19041))
        {
            Console.Error.WriteLine("Este executável precisa de Windows 10 2004+ ou Windows 11.");
            return 2;
        }

        Console.Title = "Saga BLE - Diagnóstico (somente leitura)";
        Console.WriteLine("=================================================");
        Console.WriteLine(" Saga BLE | Diagnóstico de fechadura - Windows");
        Console.WriteLine("=================================================");
        Console.WriteLine("MODO SEGURO: varredura BLE e metadados GATT.");
        Console.WriteLine("NÃO realiza pareamento, leitura de valores, escrita, abertura ou testes de senhas.");
        Console.WriteLine("Atenção: o relatório inclui endereços Bluetooth e anúncios; mantenha-o privado.\n");

        var ble = new BleDiagnostics();
        var inspections = new List<GattInspection>();

        try
        {
            if (args.Length > 0)
            {
                if (args[0] != "--scan")
                {
                    Help();
                    return 2;
                }

                int seconds = (args.Length > 1 && int.TryParse(args[1], out var parsed)) ? parsed : 20;
                await ScanAndShowAsync(ble, seconds);

                int inspectIndex = Array.IndexOf(args, "--inspect");
                if (inspectIndex >= 0)
                {
                    if (inspectIndex + 1 >= args.Length ||
                        !Addressing.TryParse(args[inspectIndex + 1], out ulong address))
                    {
                        Console.Error.WriteLine("Use --inspect AA:BB:CC:DD:EE:FF com endereço válido.");
                        return 2;
                    }

                    if (ble.Snapshot().All(x => x.AddressValue != address))
                    {
                        Console.Error.WriteLine("Endereço não observado nesta varredura. Nenhuma conexão GATT foi tentada.");
                        return 3;
                    }

                    Console.WriteLine("Inspeção GATT de metadados (sem leitura de valores ou escrita)...");
                    var inspection = await ble.InspectAsync(address);
                    inspections.Add(inspection);
                    Console.WriteLine($"Dispositivo: {inspection.Address}; nome: {inspection.Name}; status: {inspection.Status}");
                    foreach (var service in inspection.Services)
                    {
                        Console.WriteLine($"Serviço {service.Uuid}: {service.Status}");
                        foreach (var characteristic in service.Characteristics)
                            Console.WriteLine($"  {characteristic.Uuid} | {characteristic.Properties} | handle {characteristic.Handle}");
                    }
                }

                int exportIndex = Array.IndexOf(args, "--export");
                if (exportIndex >= 0)
                {
                    var path = exportIndex + 1 < args.Length ? args[exportIndex + 1] : null;
                    Save(ble, inspections, path);
                }

                return inspections.Count > 0 && inspections[0].Status != "Success" ? 4 : 0;
            }

            await ScanAndShowAsync(ble, 20);
            while (true)
            {
                Console.WriteLine("\n[1] Repetir varredura  [2] Inspecionar GATT  [3] Exportar JSON  [4] Sair");
                Console.Write("Opção: ");
                var command = Console.ReadLine()?.Trim();
                if (command is null || command == "4" || command.Equals("sair", StringComparison.OrdinalIgnoreCase))
                    return 0;

                switch (command)
                {
                    case "1":
                        Console.Write("Duração em segundos (3-120; padrão 20): ");
                        var duration = Console.ReadLine();
                        int scanSeconds = int.TryParse(duration, out int selected) ? selected : 20;
                        await ScanAndShowAsync(ble, scanSeconds);
                        break;
                    case "2":
                        await InspectInteractiveAsync(ble, inspections);
                        break;
                    case "3":
                        Save(ble, inspections, null);
                        break;
                    default:
                        Console.WriteLine("Opção inválida.");
                        break;
                }
            }
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine("\nFalha: " + ex.Message);
            Console.Error.WriteLine("Verifique Bluetooth habilitado, driver, distância e permissões do Windows.");
            if (args.Length == 0)
            {
                Console.WriteLine("Pressione ENTER para finalizar.");
                Console.ReadLine();
            }
            return 1;
        }
    }

    private static async Task ScanAndShowAsync(BleDiagnostics ble, int seconds)
    {
        Console.Write($"Procurando sinais BLE por {seconds} segundos");
        var task = ble.ScanAsync(seconds);
        while (!task.IsCompleted)
        {
            await Task.WhenAny(task, Task.Delay(1000));
            if (!task.IsCompleted) Console.Write(".");
        }
        string status = await task;
        Console.WriteLine($"\nVarredura encerrada (Windows: {status}).");
        PrintDevices(ble.Snapshot());
    }

    private static void PrintDevices(List<DeviceRecord> devices)
    {
        Console.WriteLine($"\nDispositivos únicos observados: {devices.Count}");
        Console.WriteLine("  Nº  RSSI   Endereço Bluetooth      Nome             Serviços anunciados");
        for (int i = 0; i < devices.Count; i++)
        {
            var d = devices[i];
            string name = string.IsNullOrWhiteSpace(d.Name) ? "(sem nome)" : d.Name;
            if (name.Length > 18) name = name[..18];
            Console.WriteLine($" {i + 1,3} {d.RssiDbm,5}  {d.Address,-23} {name,-18} {d.ServiceUuids.Count}");
        }
        if (devices.Count == 0)
            Console.WriteLine("Nenhum anúncio BLE. Aproxime o PC, confira alimentação e se o módulo anuncia BLE.");
        Console.WriteLine("RSSI mais próximo de zero costuma indicar sinal mais forte; não identifica a fechadura sozinho.");
    }

    private static async Task InspectInteractiveAsync(BleDiagnostics ble, List<GattInspection> inspections)
    {
        var devices = ble.Snapshot();
        if (devices.Count == 0)
        {
            Console.WriteLine("Faça uma varredura antes de inspecionar.");
            return;
        }

        PrintDevices(devices);
        Console.Write("Número do dispositivo ou endereço Bluetooth: ");
        string input = Console.ReadLine()?.Trim() ?? "";
        ulong address;
        if (int.TryParse(input, out int index) && index >= 1 && index <= devices.Count)
            address = devices[index - 1].AddressValue;
        else if (!Addressing.TryParse(input, out address) || devices.All(x => x.AddressValue != address))
        {
            Console.WriteLine("Escolha um índice da lista ou endereço encontrado nesta varredura.");
            return;
        }

        Console.WriteLine("Iniciando descoberta GATT. Pode levar vários segundos, sem escrita ou destravamento.");
        var inspection = await ble.InspectAsync(address);
        inspections.Add(inspection);
        Console.WriteLine($"Dispositivo: {inspection.Address}  Nome: {inspection.Name}");
        Console.WriteLine($"Status: {inspection.Status}");
        foreach (var service in inspection.Services)
        {
            Console.WriteLine($"  Serviço {service.Uuid}: {service.Status}");
            foreach (var ch in service.Characteristics)
                Console.WriteLine($"    {ch.Uuid} | {ch.Properties} | handle {ch.Handle}");
        }
        if (inspection.Services.Count == 0)
            Console.WriteLine("Sem serviços acessíveis. Pode exigir provisionamento, pareamento ou estar fora de alcance.");
    }

    private static void Save(BleDiagnostics ble, List<GattInspection> inspections, string? destination)
    {
        string defaultDirectory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "BluetoothSaga", "Relatorios");
        var path = string.IsNullOrWhiteSpace(destination)
            ? Path.Combine(defaultDirectory, "saga-ble-" + DateTime.Now.ToString("yyyyMMdd-HHmmss") + ".json")
            : Path.GetFullPath(destination);

        var directory = Path.GetDirectoryName(path);
        if (!string.IsNullOrWhiteSpace(directory)) Directory.CreateDirectory(directory);

        var report = new DiagnosticReport
        {
            Devices = ble.Snapshot(),
            Inspections = inspections.ToList()
        };
        File.WriteAllText(path, JsonSerializer.Serialize(report, new JsonSerializerOptions
        {
            WriteIndented = true
        }), Encoding.UTF8);
        Console.WriteLine("Relatório salvo localmente: " + path);
        Console.WriteLine("Não publique o JSON integral em repositório público: pode conter identificadores BLE.");
    }

    private static void Help()
    {
        Console.WriteLine("SagaBleDiagnostico.exe             abre menu interativo");
        Console.WriteLine("SagaBleDiagnostico.exe --scan 30   varredura por 30 segundos e sai");
        Console.WriteLine("SagaBleDiagnostico.exe --scan 30 --export C:\\Temp\\saga.json");
        Console.WriteLine("SagaBleDiagnostico.exe --scan 30 --inspect AA:BB:CC:DD:EE:FF --export C:\\Temp\\saga.json");
        Console.WriteLine("SagaBleDiagnostico.exe --self-test valida utilitários sem hardware");
    }
}
