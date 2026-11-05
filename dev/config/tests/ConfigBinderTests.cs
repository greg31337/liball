namespace config.Tests;

public class ConfigBinderTests
{
    [Test]
    public void Bind_PopulatesPublicScalarFieldsFromRoot()
    {
        ConfigMap values = new ConfigMap();
        values.SetPathDefault("Host", ConfigValue.String("localhost"));
        values.SetPathDefault("Port", ConfigValue.String("5432"));
        values.SetPathDefault("Enabled", ConfigValue.String("yes"));
        values.SetPathDefault("MaxBytes", ConfigValue.String("67108864"));
        values.SetPathDefault("Ratio", ConfigValue.String("1.25"));
        values.SetPathDefault("Load", ConfigValue.Number(2.5m));
        values.SetPathDefault("Timeout", ConfigValue.String("5s"));
        Config config = Config.CreateBuilder()
            .AddMemory(values, "memory")
            .Build();
        FlatSettings target = new FlatSettings();

        config.BindInstance(target);

        Assert.That(target.Host, Is.EqualTo("localhost"));
        Assert.That(target.Port, Is.EqualTo(5432));
        Assert.That(target.Enabled, Is.True);
        Assert.That(target.MaxBytes, Is.EqualTo(64L * 1024L * 1024L));
        Assert.That(target.Ratio, Is.EqualTo(1.25m));
        Assert.That(target.Load, Is.EqualTo(2.5d));
        Assert.That(target.Timeout, Is.EqualTo(TimeSpan.FromSeconds(5)));
    }

    [Test]
    public void Bind_CreatesTargetTypeAndReadsFromSection()
    {
        ConfigMap values = new ConfigMap();
        values.SetPathDefault("service.Host", ConfigValue.String("localhost"));
        values.SetPathDefault("service.Port", ConfigValue.Number(8080m));
        values.SetPathDefault("other.Host", ConfigValue.String("ignored"));
        Config config = Config.CreateBuilder()
            .AddMemory(values, "memory")
            .Build();

        FlatSettings target = (FlatSettings)config.BindSection("service", typeof(FlatSettings));

        Assert.That(target.Host, Is.EqualTo("localhost"));
        Assert.That(target.Port, Is.EqualTo(8080));
    }

    [Test]
    public void Bind_UsesConfiguredDelimiterForFieldPaths()
    {
        ConfigMap values = new ConfigMap();
        values.SetPath("service__Host", ConfigValue.String("localhost"), "__");
        values.SetPath("service__Port", ConfigValue.String("9000"), "__");
        Config config = Config.CreateBuilder()
            .SetDelimiter("__")
            .AddMemory(values, "memory")
            .Build();
        FlatSettings target = new FlatSettings();

        config.BindSectionInstance("service", target);

        Assert.That(target.Host, Is.EqualTo("localhost"));
        Assert.That(target.Port, Is.EqualTo(9000));
    }

    [Test]
    public void Bind_ReportsMalformedScalarFields()
    {
        ConfigMap values = new ConfigMap();
        values.SetPathDefault("Host", ConfigValue.String("localhost"));
        values.SetPathDefault("Port", ConfigValue.String("not-an-int"));
        values.SetPathDefault("Enabled", ConfigValue.String("not-a-bool"));
        values.SetPathDefault("Timeout", ConfigValue.String("not-a-duration"));
        Config config = Config.CreateBuilder()
            .AddMemory(values, "memory")
            .Build();
        DefaultsSettings target = new DefaultsSettings();

        ConfigConversionException? error = Assert.Throws<ConfigConversionException>(() => config.BindInstance(target));

        Assert.That(error!.Message, Does.Contain("field Port"));
        Assert.That(error.Message, Does.Contain("System.Int32"));
    }

    [Test]
    public void Bind_PreservesDefaultsForMissingAndNullValues()
    {
        ConfigMap values = new ConfigMap();
        values.SetPathDefault("Host", ConfigValue.Null());
        values.SetPathDefault("Nested.Name", ConfigValue.Null());
        Config config = Config.CreateBuilder()
            .AddMemory(values, "memory")
            .Build();
        DefaultsSettings target = new DefaultsSettings();

        config.BindInstance(target);

        Assert.That(target.Host, Is.EqualTo("default"));
        Assert.That(target.Port, Is.EqualTo(1234));
        Assert.That(target.Enabled, Is.True);
        Assert.That(target.Nested.Name, Is.EqualTo("default-nested"));
        Assert.That(target.Nested.RetryCount, Is.EqualTo(1));
        Assert.That(target.Created, Is.Null);
    }

    [Test]
    public void Bind_BindsNestedPublicFieldObjects()
    {
        ConfigMap values = new ConfigMap();
        values.SetPathDefault("Nested.Name", ConfigValue.String("configured-nested"));
        values.SetPathDefault("Nested.RetryCount", ConfigValue.String("4"));
        values.SetPathDefault("Created.Name", ConfigValue.String("created-nested"));
        values.SetPathDefault("Created.RetryCount", ConfigValue.Number(9m));
        Config config = Config.CreateBuilder()
            .AddMemory(values, "memory")
            .Build();
        DefaultsSettings target = new DefaultsSettings();
        NestedSettings nested = target.Nested;

        config.BindInstance(target);

        Assert.That(target.Nested, Is.SameAs(nested));
        Assert.That(target.Nested.Name, Is.EqualTo("configured-nested"));
        Assert.That(target.Nested.RetryCount, Is.EqualTo(4));
        Assert.That(target.Created, Is.Not.Null);
        Assert.That(target.Created!.Name, Is.EqualTo("created-nested"));
        Assert.That(target.Created.RetryCount, Is.EqualTo(9));
    }

    [Test]
    public void Bind_BindsArraysAndListsOfScalars()
    {
        Config config = Config.CreateBuilder()
            .AddString(
                "{ \"Hosts\": [\"one\", \"two\"], \"Ports\": [\"80\", 443], \"Flags\": [\"true\", \"off\"], \"Delays\": [\"1s\", \"00:00:02\"] }",
                ConfigFormat.Json,
                "json")
            .Build();
        CollectionSettings target = new CollectionSettings();

        config.BindInstance(target);

        Assert.That(target.Hosts, Is.EqualTo(new string[] { "one", "two" }));
        Assert.That(target.Ports, Is.EqualTo(new int[] { 80, 443 }));
        Assert.That(target.Flags, Is.EqualTo(new List<bool> { true, false }));
        Assert.That(target.Delays, Is.EqualTo(new List<TimeSpan> { TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(2) }));
    }

    [Test]
    public void Bind_ReportsCollectionElementConversionFailures()
    {
        Config config = Config.CreateBuilder()
            .AddString(
                "{ \"Ports\": [\"80\", \"bad\", 443], \"Flags\": [\"true\", \"maybe\", false], \"Delays\": [\"1s\", \"bad\"] }",
                ConfigFormat.Json,
                "json")
            .Build();
        CollectionSettings target = new CollectionSettings();

        ConfigConversionException? error = Assert.Throws<ConfigConversionException>(() => config.BindInstance(target));

        Assert.That(error!.Message, Does.Contain("field Ports"));
        Assert.That(error.Message, Does.Contain("System.Int32"));
    }

    [Test]
    public void Bind_BindsArraysAndListsOfObjects()
    {
        Config config = Config.CreateBuilder()
            .AddString(
                "{ \"Nodes\": [{ \"Name\": \"one\", \"RetryCount\": 1 }, { \"Name\": \"two\", \"RetryCount\": \"2\" }], \"Services\": [{ \"Name\": \"api\", \"RetryCount\": 3 }] }",
                ConfigFormat.Json,
                "json")
            .Build();
        ObjectCollectionSettings target = new ObjectCollectionSettings();

        config.BindInstance(target);

        Assert.That(target.Nodes.Length, Is.EqualTo(2));
        Assert.That(target.Nodes[0].Name, Is.EqualTo("one"));
        Assert.That(target.Nodes[0].RetryCount, Is.EqualTo(1));
        Assert.That(target.Nodes[1].Name, Is.EqualTo("two"));
        Assert.That(target.Nodes[1].RetryCount, Is.EqualTo(2));
        Assert.That(target.Services.Count, Is.EqualTo(1));
        Assert.That(target.Services[0].Name, Is.EqualTo("api"));
        Assert.That(target.Services[0].RetryCount, Is.EqualTo(3));
    }

    [Test]
    public void Bind_BindsNullableReferenceFields()
    {
        Config config = Config.CreateBuilder()
            .AddString(
                "{ \"Name\": \"api\", \"Nested\": { \"Name\": \"worker\" }, \"Services\": [{ \"Name\": \"first\" }] }",
                ConfigFormat.Json,
                "json")
            .Build();
        NullableReferenceSettings target = new NullableReferenceSettings();

        config.BindInstance(target);

        Assert.That(target.Name, Is.EqualTo("api"));
        Assert.That(target.Nested!.Name, Is.EqualTo("worker"));
        Assert.That(target.Services!.Count, Is.EqualTo(1));
        Assert.That(target.Services[0].Name, Is.EqualTo("first"));
    }

    [Test]
    public void Bind_UsesFieldMapForRootFields()
    {
        ConfigMap values = new ConfigMap();
        values.SetPathDefault("database.host", ConfigValue.String("db.local"));
        values.SetPathDefault("database.port", ConfigValue.String("5432"));
        Config config = Config.CreateBuilder()
            .AddMemory(values, "memory")
            .Build();
        ConfigFieldMap fieldMap = new ConfigFieldMap();
        fieldMap
            .Add("DatabaseHost", "database.host")
            .Add("DatabasePort", "database.port");

        MappedDatabaseSettings target = (MappedDatabaseSettings)config.BindMapped(typeof(MappedDatabaseSettings), fieldMap);

        Assert.That(target.DatabaseHost, Is.EqualTo("db.local"));
        Assert.That(target.DatabasePort, Is.EqualTo(5432));
    }

    [Test]
    public void Bind_UsesFieldMapRelativeToBindRootForNestedFields()
    {
        Config config = Config.CreateBuilder()
            .AddString(
                "{ \"service\": { \"server-name\": \"api\", \"credentials\": { \"api-key\": \"secret\" }, \"ports\": [80, \"443\"] } }",
                ConfigFormat.Json,
                "json")
            .Build();
        ConfigFieldMap fieldMap = new ConfigFieldMap();
        fieldMap
            .Add("ServerName", "server-name")
            .Add("Credentials", "credentials")
            .Add("Credentials.ApiKey", "credentials.api-key")
            .Add("PublicPorts", "ports");
        MappedServiceSettings target = new MappedServiceSettings();

        config.BindMappedSectionInstance("service", target, fieldMap);

        Assert.That(target.ServerName, Is.EqualTo("api"));
        Assert.That(target.Credentials.ApiKey, Is.EqualTo("secret"));
        Assert.That(target.PublicPorts, Is.EqualTo(new int[] { 80, 443 }));
    }

    [Test]
    public void Bind_BindsEnumFieldsArraysAndListsByName()
    {
        Config config = Config.CreateBuilder()
            .AddString(
                "{ \"Mode\": \"fAsT\", \"Modes\": [\"Slow\", \"FAST\"], \"ModeList\": [\"Fast\", \"Slow\"] }",
                ConfigFormat.Json,
                "json")
            .Build();
        EnumBindingSettings target = new EnumBindingSettings();

        config.BindInstance(target);

        Assert.That(target.Mode, Is.EqualTo(BindingMode.Fast));
        Assert.That(target.Modes, Is.EqualTo(new BindingMode[] { BindingMode.Slow, BindingMode.Fast }));
        Assert.That(target.ModeList, Is.EqualTo(new List<BindingMode> { BindingMode.Fast, BindingMode.Slow }));
    }

    [Test]
    public void Bind_HonorsConfiguredCaseModeThroughoutNestedCollections()
    {
        string json = "{ \"service\": { \"name\": \"api\", \"nested\": { \"name\": \"worker\" }, \"items\": [{ \"name\": \"first\", \"retrycount\": 3 }] } }";
        Config sensitive = Config.CreateBuilder()
            .AddString(json, ConfigFormat.Json, "json")
            .Build();
        Config insensitive = Config.CreateBuilder()
            .SetCaseMode(ConfigCaseMode.Insensitive)
            .AddString(json, ConfigFormat.Json, "json")
            .Build();
        CaseBindingSettings sensitiveTarget = new CaseBindingSettings();
        CaseBindingSettings insensitiveTarget = new CaseBindingSettings();

        sensitive.BindSectionInstance("service", sensitiveTarget);
        insensitive.BindSectionInstance("SERVICE", insensitiveTarget);

        Assert.That(sensitiveTarget.Name, Is.EqualTo("default"));
        Assert.That(sensitiveTarget.Items, Is.Empty);
        Assert.That(insensitiveTarget.Name, Is.EqualTo("api"));
        Assert.That(insensitiveTarget.Nested.Name, Is.EqualTo("worker"));
        Assert.That(insensitiveTarget.Items.Count, Is.EqualTo(1));
        Assert.That(insensitiveTarget.Items[0].Name, Is.EqualTo("first"));
        Assert.That(insensitiveTarget.Items[0].RetryCount, Is.EqualTo(3));
    }

    [Test]
    public void Bind_AppliesFullFieldMappingsInsideNestedCollectionObjects()
    {
        Config config = Config.CreateBuilder()
            .AddString(
                "{ \"cluster\": { \"nodes\": [{ \"display-name\": \"one\", \"mode\": \"Fast\", \"auth\": { \"api-key\": \"secret\" } }] } }",
                ConfigFormat.Json,
                "json")
            .Build();
        ConfigFieldMap fieldMap = new ConfigFieldMap();
        fieldMap
            .Add("Nodes", "cluster.nodes")
            .Add("Nodes.Name", "cluster.nodes.display-name")
            .Add("Nodes.Mode", "cluster.nodes.mode")
            .Add("Nodes.Credentials", "cluster.nodes.auth")
            .Add("Nodes.Credentials.ApiKey", "cluster.nodes.auth.api-key");

        MappedCollectionSettings target = (MappedCollectionSettings)config.BindMapped(typeof(MappedCollectionSettings), fieldMap);

        Assert.That(target.Nodes.Count, Is.EqualTo(1));
        Assert.That(target.Nodes[0].Name, Is.EqualTo("one"));
        Assert.That(target.Nodes[0].Mode, Is.EqualTo(BindingMode.Fast));
        Assert.That(target.Nodes[0].Credentials.ApiKey, Is.EqualTo("secret"));
    }

    [Test]
    public void Bind_ReportsEverySupportedScalarAndEnumConversionFailure()
    {
        AssertBindingFailure("Boolean", "perhaps", "System.Boolean");
        AssertBindingFailure("Integer", "1.5", "System.Int32");
        AssertBindingFailure("Long", "1.5", "System.Int64");
        AssertBindingFailure("Decimal", "many", "System.Decimal");
        AssertBindingFailure("Double", "many", "System.Double");
        AssertBindingFailure("Duration", "later", "System.TimeSpan");
        AssertBindingFailure("Mode", "Turbo", "BindingMode");
        AssertBindingFailure("Nested", "not-an-object", "NestedSettings");
    }

    [Test]
    public void Bind_ReportsUnsupportedRootFieldAndCollectionElementTypes()
    {
        Config empty = Config.CreateBuilder().Build();
        Config unsupportedFields = Config.CreateBuilder()
            .AddString("{ \"Timestamp\": \"2026-01-01\", \"Values\": [] }", ConfigFormat.Json, "json")
            .Build();

        ConfigConversionException? rootError = Assert.Throws<ConfigConversionException>(() => empty.Bind(typeof(string)));
        ConfigConversionException? fieldError = Assert.Throws<ConfigConversionException>(() => unsupportedFields.Bind(typeof(UnsupportedFieldSettings)));
        ConfigConversionException? collectionError = Assert.Throws<ConfigConversionException>(() => unsupportedFields.Bind(typeof(UnsupportedCollectionSettings)));

        Assert.That(rootError!.Message, Does.Contain("unsupported config binding target type"));
        Assert.That(fieldError!.Message, Does.Contain("field Timestamp"));
        Assert.That(fieldError.Message, Does.Contain("System.DateTime"));
        Assert.That(collectionError!.Message, Does.Contain("field Values"));
        Assert.That(collectionError.Message, Does.Contain("System.DateTime"));
    }

    private static void AssertBindingFailure(string fieldName, string value, string typeName)
    {
        ConfigMap values = new ConfigMap();
        values.Set(fieldName, ConfigValue.String(value));
        Config config = Config.CreateBuilder().AddMemory(values, "memory").Build();
        BindingFailureSettings target = new BindingFailureSettings();

        ConfigConversionException? error = Assert.Throws<ConfigConversionException>(() => config.BindInstance(target));

        Assert.That(error!.Message, Does.Contain("field " + fieldName));
        Assert.That(error.Message, Does.Contain(typeName));
    }
}

public class FlatSettings
{
    public string Host = "";
    public int Port;
    public bool Enabled;
    public long MaxBytes;
    public decimal Ratio;
    public double Load;
    public TimeSpan Timeout;
}

public class DefaultsSettings
{
    public string Host = "default";
    public int Port = 1234;
    public bool Enabled = true;
    public TimeSpan Timeout = TimeSpan.FromSeconds(2);
    public NestedSettings Nested = new NestedSettings();
    public NestedSettings? Created;
}

public class NestedSettings
{
    public string Name = "default-nested";
    public int RetryCount = 1;
}

public class CollectionSettings
{
    public string[] Hosts = new string[0];
    public int[] Ports = new int[0];
    public List<bool> Flags = new List<bool>();
    public List<TimeSpan> Delays = new List<TimeSpan>();
}

public class ObjectCollectionSettings
{
    public NestedSettings[] Nodes = new NestedSettings[0];
    public List<NestedSettings> Services = new List<NestedSettings>();
}

public class NullableReferenceSettings
{
    public string? Name;
    public NestedSettings? Nested;
    public List<NestedSettings>? Services;
}

public class MappedDatabaseSettings
{
    public string DatabaseHost = "";
    public int DatabasePort;
}

public class MappedServiceSettings
{
    public string ServerName = "";
    public MappedCredentials Credentials = new MappedCredentials();
    public int[] PublicPorts = new int[0];
}

public class MappedCredentials
{
    public string ApiKey = "";
}

public enum BindingMode
{
    Unknown = 0,
    Fast = 1,
    Slow = 2
}

public class EnumBindingSettings
{
    public BindingMode Mode;
    public BindingMode[] Modes = new BindingMode[0];
    public List<BindingMode> ModeList = new List<BindingMode>();
}

public class CaseBindingSettings
{
    public string Name = "default";
    public NestedSettings Nested = new NestedSettings();
    public List<NestedSettings> Items = new List<NestedSettings>();
}

public class MappedCollectionSettings
{
    public List<MappedNodeSettings> Nodes = new List<MappedNodeSettings>();
}

public class MappedNodeSettings
{
    public string Name = "";
    public BindingMode Mode;
    public MappedCredentials Credentials = new MappedCredentials();
}

public class BindingFailureSettings
{
    public bool Boolean;
    public int Integer;
    public long Long;
    public decimal Decimal;
    public double Double;
    public TimeSpan Duration;
    public BindingMode Mode;
    public NestedSettings Nested = new NestedSettings();
}

public class UnsupportedFieldSettings
{
    public DateTime Timestamp;
}

public class UnsupportedCollectionSettings
{
    public List<DateTime> Values = new List<DateTime>();
}
