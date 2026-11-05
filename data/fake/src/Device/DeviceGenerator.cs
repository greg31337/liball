using System.Globalization;

namespace fake;

internal class DeviceFixture
{
    internal string Manufacturer = string.Empty;
    internal string Model = string.Empty;
    internal string Type = string.Empty;
    internal string OperatingSystem = string.Empty;
    internal string OperatingSystemVersion = string.Empty;
    internal int ScreenWidth;
    internal int ScreenHeight;
    internal bool UsesAndroid;

    internal DeviceFixture(
        string manufacturer,
        string model,
        string type,
        string operatingSystem,
        string operatingSystemVersion,
        int screenWidth,
        int screenHeight,
        bool usesAndroid)
    {
        Manufacturer = manufacturer;
        Model = model;
        Type = type;
        OperatingSystem = operatingSystem;
        OperatingSystemVersion = operatingSystemVersion;
        ScreenWidth = screenWidth;
        ScreenHeight = screenHeight;
        UsesAndroid = usesAndroid;
    }
}

internal static class DeviceGenerator
{
    const int HardwareTokenLength = 32;
    const int AndroidIdLength = 16;
    const int PushTokenLength = 64;

    static readonly DeviceFixture[] Fixtures = new DeviceFixture[]
    {
        new DeviceFixture(
            "Aster Mobile",
            "Aster One",
            "phone",
            "Android",
            "14.2",
            1080,
            2400,
            true),
        new DeviceFixture(
            "Aster Mobile",
            "Aster Fold",
            "phone",
            "Android",
            "15.0",
            2208,
            1840,
            true),
        new DeviceFixture(
            "Bellwether Devices",
            "Canvas 11",
            "tablet",
            "Android",
            "13.4",
            1600,
            2560,
            true),
        new DeviceFixture(
            "Calder Systems",
            "Northstar 14",
            "laptop",
            "FixtureOS",
            "6.8.2",
            1920,
            1200,
            false),
        new DeviceFixture(
            "Calder Systems",
            "Beacon Mini",
            "desktop",
            "FixtureOS",
            "6.8.2",
            2560,
            1440,
            false),
        new DeviceFixture(
            "Meridian Works",
            "Orbit Watch",
            "wearable",
            "Example Wear",
            "5.1",
            454,
            454,
            false),
        new DeviceFixture(
            "Pine Labs",
            "Harbor TV",
            "television",
            "Example TV OS",
            "12.0",
            3840,
            2160,
            false)
    };

    internal static string Manufacturer(FakeRandom random, int maximumOutputSize)
    {
        string result = Fixture(random).Manufacturer;
        Guard.OutputSize(result.Length, maximumOutputSize, "manufacturer");
        return result;
    }

    internal static string Model(FakeRandom random, int maximumOutputSize)
    {
        string result = Fixture(random).Model;
        Guard.OutputSize(result.Length, maximumOutputSize, "model");
        return result;
    }

    internal static string Type(FakeRandom random, int maximumOutputSize)
    {
        string result = Fixture(random).Type;
        Guard.OutputSize(result.Length, maximumOutputSize, "type");
        return result;
    }

    internal static string OperatingSystem(FakeRandom random, int maximumOutputSize)
    {
        string result = Fixture(random).OperatingSystem;
        Guard.OutputSize(result.Length, maximumOutputSize, "operatingSystem");
        return result;
    }

    internal static string OperatingSystemVersion(
        FakeRandom random,
        int maximumOutputSize)
    {
        string result = Fixture(random).OperatingSystemVersion;
        Guard.OutputSize(result.Length, maximumOutputSize, "operatingSystemVersion");
        return result;
    }

    internal static int ScreenWidth(FakeRandom random)
    {
        return Fixture(random).ScreenWidth;
    }

    internal static int ScreenHeight(FakeRandom random)
    {
        return Fixture(random).ScreenHeight;
    }

    internal static string ScreenResolution(
        FakeRandom random,
        int maximumOutputSize)
    {
        DeviceFixture fixture = Fixture(random);
        return Resolution(fixture, maximumOutputSize);
    }

    internal static string HardwareToken(
        FakeRandom random,
        int maximumOutputSize)
    {
        return SecretGenerator.Hex(
            random,
            HardwareTokenLength,
            maximumOutputSize);
    }

    internal static string AndroidId(
        FakeRandom random,
        int maximumOutputSize)
    {
        return SecretGenerator.Hex(
            random,
            AndroidIdLength,
            maximumOutputSize);
    }

    internal static string PushToken(
        FakeRandom random,
        int maximumOutputSize)
    {
        return SecretGenerator.Token(
            random,
            PushTokenLength,
            maximumOutputSize);
    }

    internal static DeviceRecord Record(
        FakeRandom random,
        int maximumOutputSize)
    {
        DeviceFixture fixture = Fixture(random);
        DeviceRecord result = new DeviceRecord();
        result.Manufacturer = fixture.Manufacturer;
        result.Model = fixture.Model;
        result.Type = fixture.Type;
        result.OperatingSystem = fixture.OperatingSystem;
        result.OperatingSystemVersion = fixture.OperatingSystemVersion;
        result.ScreenWidth = fixture.ScreenWidth;
        result.ScreenHeight = fixture.ScreenHeight;
        result.ScreenResolution = Resolution(fixture, maximumOutputSize);
        result.HardwareToken = HardwareToken(random, maximumOutputSize);
        result.UsesAndroid = fixture.UsesAndroid;
        if (fixture.UsesAndroid)
        {
            result.AndroidId = AndroidId(random, maximumOutputSize);
        }
        else
        {
            result.AndroidId = string.Empty;
        }
        result.PushToken = PushToken(random, maximumOutputSize);
        ValidateOutput(result, maximumOutputSize);
        return result;
    }

    static DeviceFixture Fixture(FakeRandom random)
    {
        return Fixtures[random.IntFromMaximumExclusive(Fixtures.Length)];
    }

    static string Resolution(
        DeviceFixture fixture,
        int maximumOutputSize)
    {
        string result = fixture.ScreenWidth.ToString(CultureInfo.InvariantCulture) +
            "x" +
            fixture.ScreenHeight.ToString(CultureInfo.InvariantCulture);
        Guard.OutputSize(result.Length, maximumOutputSize, "screenResolution");
        return result;
    }

    static void ValidateOutput(DeviceRecord record, int maximumOutputSize)
    {
        Guard.OutputSize(record.Manufacturer.Length, maximumOutputSize, "deviceRecord");
        Guard.OutputSize(record.Model.Length, maximumOutputSize, "deviceRecord");
        Guard.OutputSize(record.Type.Length, maximumOutputSize, "deviceRecord");
        Guard.OutputSize(record.OperatingSystem.Length, maximumOutputSize, "deviceRecord");
        Guard.OutputSize(record.OperatingSystemVersion.Length, maximumOutputSize, "deviceRecord");
        Guard.OutputSize(record.ScreenResolution.Length, maximumOutputSize, "deviceRecord");
        Guard.OutputSize(record.HardwareToken.Length, maximumOutputSize, "deviceRecord");
        Guard.OutputSize(record.AndroidId.Length, maximumOutputSize, "deviceRecord");
        Guard.OutputSize(record.PushToken.Length, maximumOutputSize, "deviceRecord");
    }
}
