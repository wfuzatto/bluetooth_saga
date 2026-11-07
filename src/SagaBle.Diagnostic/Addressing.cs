using System.Globalization;

namespace SagaBle.Diagnostic;

internal static class Addressing
{
    internal static string Format(ulong address) =>
        string.Join(":", Enumerable.Range(0, 6)
            .Select(i => ((address >> (8 * (5 - i))) & 0xff).ToString("X2")));

    internal static bool TryParse(string input, out ulong address)
    {
        address = 0;
        if (string.IsNullOrWhiteSpace(input)) return false;
        var clean = input.Trim().Replace(":", "").Replace("-", "");
        return clean.Length == 12 &&
            ulong.TryParse(clean, NumberStyles.AllowHexSpecifier, CultureInfo.InvariantCulture, out address);
    }
}

internal static class SelfTests
{
    internal static bool Run()
    {
        const ulong value = 0xAABBCCDDEEFF;
        bool valid = Addressing.Format(value) == "AA:BB:CC:DD:EE:FF"
            && Addressing.TryParse("aa:bb:cc:dd:ee:ff", out ulong parsed) && parsed == value
            && Addressing.TryParse("AA-BB-CC-DD-EE-FF", out parsed) && parsed == value
            && !Addressing.TryParse("invalid", out _)
            && !Addressing.TryParse("AA:BB:CC:DD:EE", out _);
        Console.WriteLine(valid ? "PASS: endereços BLE / validações" : "FAIL: endereços BLE / validações");

        var advertisement = new DeviceRecord();
        BleDiagnostics.RecordAdvertisement(advertisement, "ConnectableUndirected", "Random", true, true, false, false);
        BleDiagnostics.RecordAdvertisement(advertisement, "ScanResponse", "Random", false, false, false, true);
        bool advertisementValid = advertisement.Advertisements == 2
            && advertisement.ConnectableAdvertisements == 1
            && advertisement.ScannableAdvertisements == 1
            && advertisement.ScanResponseAdvertisements == 1
            && advertisement.AdvertisementTypes.Count == 2
            && advertisement.AddressTypes.Count == 1;
        Console.WriteLine(advertisementValid
            ? "PASS: metadados de anúncio / conectabilidade"
            : "FAIL: metadados de anúncio / conectabilidade");

        bool targetMatchValid = BleDiagnostics.MatchesConnectableName("TestLock_Unit01", "TestLock_Unit01", true)
            && !BleDiagnostics.MatchesConnectableName("TestLock_Unit01", "TestLock_Unit01", false)
            && !BleDiagnostics.MatchesConnectableName("OtherDevice", "TestLock_Unit01", true)
            && !BleDiagnostics.MatchesConnectableName(null, "TestLock_Unit01", true);
        Console.WriteLine(targetMatchValid
            ? "PASS: seleção por nome exato e anúncio conectável"
            : "FAIL: seleção por nome exato e anúncio conectável");
        return valid && advertisementValid && targetMatchValid;
    }
}
