using fake;

namespace fake.Tests;

[TestFixture]
public class DeviceProviderTests
{
    [Test]
    public void RootReturnsOneCachedDeviceProvider()
    {
        Fake fake = Fake.Create(9401);
        DeviceProvider first = fake.Device();
        DeviceProvider second = fake.Device();

        Assert.That(second, Is.SameAs(first));
    }

    [Test]
    public void ComponentProvidersAreDeterministicStableFixtures()
    {
        DeviceProvider first = Fake.Create(9402).Device();
        DeviceProvider second = Fake.Create(9402).Device();
        HashSet<string> manufacturers = new HashSet<string>();
        HashSet<string> models = new HashSet<string>();
        HashSet<string> types = new HashSet<string>();
        HashSet<string> operatingSystems = new HashSet<string>();

        int index = 0;
        while (index < 512)
        {
            string manufacturer = first.Manufacturer();
            string model = first.Model();
            string type = first.Type();
            string operatingSystem = first.OperatingSystem();
            string version = first.OperatingSystemVersion();
            string resolution = first.ScreenResolution();

            Assert.Multiple(() =>
            {
                Assert.That(manufacturer, Is.EqualTo(second.Manufacturer()));
                Assert.That(model, Is.EqualTo(second.Model()));
                Assert.That(type, Is.EqualTo(second.Type()));
                Assert.That(operatingSystem, Is.EqualTo(second.OperatingSystem()));
                Assert.That(version, Is.EqualTo(second.OperatingSystemVersion()));
                Assert.That(resolution, Is.EqualTo(second.ScreenResolution()));
                Assert.That(version, Does.Match("^[0-9]+(\\.[0-9]+){1,2}$"));
                Assert.That(resolution, Does.Match("^[1-9][0-9]{2,3}x[1-9][0-9]{2,3}$"));
            });
            manufacturers.Add(manufacturer);
            models.Add(model);
            types.Add(type);
            operatingSystems.Add(operatingSystem);
            index++;
        }

        Assert.Multiple(() =>
        {
            Assert.That(manufacturers, Is.EquivalentTo(new string[]
            {
                "Aster Mobile", "Bellwether Devices", "Calder Systems",
                "Meridian Works", "Pine Labs"
            }));
            Assert.That(models, Is.EquivalentTo(new string[]
            {
                "Aster One", "Aster Fold", "Canvas 11", "Northstar 14",
                "Beacon Mini", "Orbit Watch", "Harbor TV"
            }));
            Assert.That(types, Is.EquivalentTo(new string[]
            {
                "phone", "tablet", "laptop", "desktop", "wearable", "television"
            }));
            Assert.That(operatingSystems, Is.EquivalentTo(new string[]
            {
                "Android", "FixtureOS", "Example Wear", "Example TV OS"
            }));
        });
    }

    [Test]
    public void RecordKeepsModelSoftwareAndScreenMetadataCorrelated()
    {
        DeviceProvider first = Fake.Create(9403).Device();
        DeviceProvider second = Fake.Create(9403).Device();
        HashSet<string> observed = new HashSet<string>();
        HashSet<string> allowed = new HashSet<string>(new string[]
        {
            "Aster Mobile|Aster One|phone|Android|14.2|1080x2400",
            "Aster Mobile|Aster Fold|phone|Android|15.0|2208x1840",
            "Bellwether Devices|Canvas 11|tablet|Android|13.4|1600x2560",
            "Calder Systems|Northstar 14|laptop|FixtureOS|6.8.2|1920x1200",
            "Calder Systems|Beacon Mini|desktop|FixtureOS|6.8.2|2560x1440",
            "Meridian Works|Orbit Watch|wearable|Example Wear|5.1|454x454",
            "Pine Labs|Harbor TV|television|Example TV OS|12.0|3840x2160"
        });

        int index = 0;
        while (index < 512)
        {
            DeviceRecord record = first.Record();
            DeviceRecord matching = second.Record();
            string signature = Signature(record);

            Assert.Multiple(() =>
            {
                AssertRecordEqual(record, matching);
                Assert.That(allowed, Does.Contain(signature));
                Assert.That(record.ScreenResolution,
                    Is.EqualTo(record.ScreenWidth + "x" + record.ScreenHeight));
                Assert.That(record.HardwareToken, Does.Match("^[0-9a-f]{32}$"));
                Assert.That(record.PushToken, Does.Match("^[A-Za-z0-9_-]{64}$"));
                Assert.That(record.UsesAndroid,
                    Is.EqualTo(record.OperatingSystem == "Android"));
                Assert.That(record.AndroidId,
                    record.UsesAndroid
                        ? Does.Match("^[0-9a-f]{16}$")
                        : Is.Empty);
            });
            observed.Add(signature);
            index++;
        }

        Assert.That(observed, Is.EquivalentTo(allowed));
    }

    [Test]
    public void IndividualFixtureFieldsCorrelateWithARecordAtTheSameSeed()
    {
        const long seed = 9404;
        DeviceRecord record = Fake.Create(seed).Device().Record();

        Assert.Multiple(() =>
        {
            Assert.That(Fake.Create(seed).Device().Manufacturer(), Is.EqualTo(record.Manufacturer));
            Assert.That(Fake.Create(seed).Device().Model(), Is.EqualTo(record.Model));
            Assert.That(Fake.Create(seed).Device().Type(), Is.EqualTo(record.Type));
            Assert.That(Fake.Create(seed).Device().OperatingSystem(), Is.EqualTo(record.OperatingSystem));
            Assert.That(Fake.Create(seed).Device().OperatingSystemVersion(),
                Is.EqualTo(record.OperatingSystemVersion));
            Assert.That(Fake.Create(seed).Device().Os(), Is.EqualTo(record.OperatingSystem));
            Assert.That(Fake.Create(seed).Device().OsVersion(), Is.EqualTo(record.OperatingSystemVersion));
            Assert.That(Fake.Create(seed).Device().ScreenWidth(), Is.EqualTo(record.ScreenWidth));
            Assert.That(Fake.Create(seed).Device().ScreenHeight(), Is.EqualTo(record.ScreenHeight));
            Assert.That(Fake.Create(seed).Device().ScreenResolution(), Is.EqualTo(record.ScreenResolution));
        });
    }

    [Test]
    public void HardwareAndroidAndPushTokensHaveExactDeterministicShapes()
    {
        DeviceProvider first = Fake.Create(9405).Device();
        DeviceProvider second = Fake.Create(9405).Device();
        HashSet<string> hardwareTokens = new HashSet<string>();
        HashSet<string> androidIds = new HashSet<string>();
        HashSet<string> pushTokens = new HashSet<string>();

        int index = 0;
        while (index < 256)
        {
            string hardware = first.HardwareToken();
            string android = first.AndroidId();
            string push = first.PushToken();

            Assert.Multiple(() =>
            {
                Assert.That(hardware, Is.EqualTo(second.HardwareToken()));
                Assert.That(android, Is.EqualTo(second.AndroidId()));
                Assert.That(push, Is.EqualTo(second.PushToken()));
                Assert.That(hardware, Does.Match("^[0-9a-f]{32}$"));
                Assert.That(android, Does.Match("^[0-9a-f]{16}$"));
                Assert.That(push, Does.Match("^[A-Za-z0-9_-]{64}$"));
            });
            hardwareTokens.Add(hardware);
            androidIds.Add(android);
            pushTokens.Add(push);
            index++;
        }

        Assert.Multiple(() =>
        {
            Assert.That(hardwareTokens, Has.Count.EqualTo(256));
            Assert.That(androidIds, Has.Count.EqualTo(256));
            Assert.That(pushTokens, Has.Count.EqualTo(256));
        });
    }

    [Test]
    public void DeviceOutputsHonorConfiguredOutputLimits()
    {
        FakeOptions shortOptions = FakeOptions.Create(9406);
        shortOptions.MaximumOutputSize = 15;
        FakeOptions nameOptions = FakeOptions.Create(9406);
        nameOptions.MaximumOutputSize = 3;
        FakeOptions tokenOptions = FakeOptions.Create(9406);
        tokenOptions.MaximumOutputSize = 63;

        Assert.That(
            () => Fake.Create(shortOptions).Device().AndroidId(),
            Throws.TypeOf<ArgumentOutOfRangeException>().With.Property("ParamName").EqualTo("count"));
        Assert.That(
            () => Fake.Create(nameOptions).Device().Manufacturer(),
            Throws.TypeOf<ArgumentOutOfRangeException>());
        Assert.That(
            () => Fake.Create(tokenOptions).Device().PushToken(),
            Throws.TypeOf<ArgumentOutOfRangeException>().With.Property("ParamName").EqualTo("count"));
        Assert.That(
            () => Fake.Create(tokenOptions).Device().Record(),
            Throws.TypeOf<ArgumentOutOfRangeException>());
    }

    static string Signature(DeviceRecord record)
    {
        return record.Manufacturer + "|" +
            record.Model + "|" +
            record.Type + "|" +
            record.OperatingSystem + "|" +
            record.OperatingSystemVersion + "|" +
            record.ScreenResolution;
    }

    static void AssertRecordEqual(DeviceRecord first, DeviceRecord second)
    {
        Assert.That(first.Manufacturer, Is.EqualTo(second.Manufacturer));
        Assert.That(first.Model, Is.EqualTo(second.Model));
        Assert.That(first.Type, Is.EqualTo(second.Type));
        Assert.That(first.OperatingSystem, Is.EqualTo(second.OperatingSystem));
        Assert.That(first.OperatingSystemVersion, Is.EqualTo(second.OperatingSystemVersion));
        Assert.That(first.ScreenWidth, Is.EqualTo(second.ScreenWidth));
        Assert.That(first.ScreenHeight, Is.EqualTo(second.ScreenHeight));
        Assert.That(first.ScreenResolution, Is.EqualTo(second.ScreenResolution));
        Assert.That(first.HardwareToken, Is.EqualTo(second.HardwareToken));
        Assert.That(first.UsesAndroid, Is.EqualTo(second.UsesAndroid));
        Assert.That(first.AndroidId, Is.EqualTo(second.AndroidId));
        Assert.That(first.PushToken, Is.EqualTo(second.PushToken));
    }
}
