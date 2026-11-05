using System.Globalization;

namespace config.Tests;

public class ConfigPublicContractTests
{
    [Test]
    public void ConfigValues_InterfaceCreatesScalarsAndManagesMetadata()
    {
        CultureInfo previousCulture = CultureInfo.CurrentCulture;

        try
        {
            CultureInfo.CurrentCulture = new CultureInfo("fr-FR");
            IConfigValues values = new ConfigValues();
            ConfigValue missing = values.Missing();
            ConfigValue nullValue = values.Null();
            ConfigValue text = values.String("hello");
            ConfigValue number = values.Number(1234.5m);
            ConfigValue boolean = values.Boolean(false);
            ConfigValue returned = values.SetSource(text, "settings.flat", 3, "service.message", 4, 2);

            Assert.That(missing.Kind, Is.EqualTo(ConfigValueKind.Missing));
            Assert.That(values.IsMissing(missing), Is.True);
            Assert.That(values.IsNull(missing), Is.False);
            Assert.That(nullValue.Kind, Is.EqualTo(ConfigValueKind.Null));
            Assert.That(values.IsNull(nullValue), Is.True);
            Assert.That(text.Kind, Is.EqualTo(ConfigValueKind.String));
            Assert.That(text.StringValue, Is.EqualTo("hello"));
            Assert.That(number.Kind, Is.EqualTo(ConfigValueKind.Number));
            Assert.That(number.NumberValue, Is.EqualTo(1234.5m));
            Assert.That(boolean.Kind, Is.EqualTo(ConfigValueKind.Boolean));
            Assert.That(boolean.BooleanValue, Is.False);
            Assert.That(returned, Is.SameAs(text));
            Assert.That(values.HasSource(text), Is.True);
            Assert.That(values.HasLocation(text), Is.True);
            Assert.That(text.SourceName, Is.EqualTo("settings.flat"));
            Assert.That(text.PrecedenceRank, Is.EqualTo(3));
            Assert.That(text.OriginalKey, Is.EqualTo("service.message"));
            Assert.That(text.LineNumber, Is.EqualTo(4));
            Assert.That(text.ColumnNumber, Is.EqualTo(2));
            Assert.That(values.HasSource(number), Is.False);
            Assert.That(values.HasLocation(number), Is.False);
            Assert.That(values.ToDisplayString(missing), Is.EqualTo("(missing)"));
            Assert.That(values.ToDisplayString(nullValue), Is.EqualTo("null"));
            Assert.That(values.ToDisplayString(text), Is.EqualTo("\"hello\""));
            Assert.That(values.ToDisplayString(number), Is.EqualTo("1234.5"));
            Assert.That(values.ToDisplayString(boolean), Is.EqualTo("false"));
        }
        finally
        {
            CultureInfo.CurrentCulture = previousCulture;
        }
    }

    [Test]
    public void ConfigValues_InterfaceCreatesContainersAndDeepCopiesThem()
    {
        IConfigValues values = new ConfigValues();
        ConfigArray items = new ConfigArray();
        items.Add(values.String("first"));
        items.Add(values.Null());
        ConfigMap map = new ConfigMap();
        map.Set("zeta", values.Boolean(true));
        map.Set("alpha", values.Array(items));
        ConfigValue root = values.SetSource(values.Object(map), "memory", 1, "", 0, 0);

        ConfigValue copy = values.Copy(root);
        items.Get(0).StringValue = "changed";
        map.Set("zeta", values.Boolean(false));
        root.SourceName = "changed";

        Assert.That(root.ObjectValue, Is.SameAs(map));
        Assert.That(map.Get("alpha").ArrayValue, Is.SameAs(items));
        Assert.That(copy, Is.Not.SameAs(root));
        Assert.That(copy.ObjectValue, Is.Not.SameAs(map));
        Assert.That(copy.ObjectValue!.Get("alpha").ArrayValue, Is.Not.SameAs(items));
        Assert.That(copy.SourceName, Is.EqualTo("memory"));
        Assert.That(values.ToDisplayString(copy), Is.EqualTo("{alpha: [\"first\", null], zeta: true}"));
    }

    [Test]
    public void ConfigArray_InterfaceCoversAccessCopyAndDisplayBoundaries()
    {
        IConfigValues values = new ConfigValues();
        IConfigArray array = new ConfigArray();
        array.Add(values.String("first"));
        array.Add(values.Number(2m));

        ConfigValue[] exported = array.ToArray();
        ConfigArray deepCopy = array.Copy();
        exported[0] = values.String("replacement");
        array.Get(0).StringValue = "changed";

        Assert.That(array.Count(), Is.EqualTo(2));
        Assert.That(array.Get(0).StringValue, Is.EqualTo("changed"));
        Assert.That(array.Get(1).NumberValue, Is.EqualTo(2m));
        Assert.That(values.IsMissing(array.Get(-1)), Is.True);
        Assert.That(values.IsMissing(array.Get(2)), Is.True);
        Assert.That(exported[0].StringValue, Is.EqualTo("replacement"));
        Assert.That(deepCopy.Get(0).StringValue, Is.EqualTo("first"));
        Assert.That(deepCopy.Get(0), Is.Not.SameAs(array.Get(0)));
        Assert.That(array.ToDisplayString(), Is.EqualTo("[\"changed\", 2]"));
    }

    [Test]
    public void ConfigMap_InterfaceCoversDirectKeysCaseModeAndDisplay()
    {
        IConfigValues values = new ConfigValues();
        ConfigMap concrete = new ConfigMap();
        IConfigMap map = concrete;

        ConfigMap returned = map.SetCaseMode(ConfigCaseMode.Insensitive);
        map.Set("beta", values.String("second"));
        map.Set("Alpha", values.Number(1m));
        map.Set("ALPHA", values.Number(2m));

        Assert.That(returned, Is.SameAs(concrete));
        Assert.That(map.Count(), Is.EqualTo(2));
        Assert.That(map.Has("alpha"), Is.True);
        Assert.That(map.Get("aLpHa").NumberValue, Is.EqualTo(2m));
        Assert.That(values.IsMissing(map.Get("missing")), Is.True);
        Assert.That(map.Keys(), Is.EqualTo(new string[] { "Alpha", "beta" }));
        Assert.That(map.ToDisplayString(), Is.EqualTo("{Alpha: 2, beta: \"second\"}"));
        Assert.Throws<InvalidOperationException>(() => map.SetCaseMode(ConfigCaseMode.Sensitive));

        map.Remove("BETA");

        Assert.That(map.Count(), Is.EqualTo(1));
        Assert.That(map.Has("beta"), Is.False);
    }

    [Test]
    public void ConfigMap_InterfaceCoversDefaultAndCustomPathOperations()
    {
        IConfigValues values = new ConfigValues();
        IConfigMap map = new ConfigMap();
        map.SetPathDefault("service.host", values.String("localhost"));
        map.SetPath("service__port", values.Number(8080m), "__");
        map.SetPathDefault("literal\\.key.enabled", values.Boolean(true));
        map.Set("scalar", values.String("leaf"));

        ConfigValue root = map.GetPath("", ".");

        Assert.That(root.Kind, Is.EqualTo(ConfigValueKind.Object));
        Assert.That(root.ObjectValue, Is.SameAs(map));
        Assert.That(map.GetPathDefault("service.host").StringValue, Is.EqualTo("localhost"));
        Assert.That(map.GetPath("service__port", "__").NumberValue, Is.EqualTo(8080m));
        Assert.That(map.GetPathDefault("literal\\.key.enabled").BooleanValue, Is.True);
        Assert.That(map.HasPathDefault("service.host"), Is.True);
        Assert.That(map.HasPath("service__port", "__"), Is.True);
        Assert.That(map.HasPathDefault("service.missing"), Is.False);
        Assert.That(map.ChildKeysDefault("service"), Is.EqualTo(new string[] { "host", "port" }));
        Assert.That(map.ChildKeys("literal\\.key", "."), Is.EqualTo(new string[] { "enabled" }));
        Assert.That(map.ChildKeysDefault("scalar"), Is.Empty);
        Assert.That(map.ChildKeysDefault(""), Is.EqualTo(new string[] { "literal.key", "scalar", "service" }));
    }

    [Test]
    public void ConfigMap_CopyIsDeepAndPreservesCaseBehaviorAndReturnedArrays()
    {
        IConfigValues values = new ConfigValues();
        ConfigArray tags = new ConfigArray();
        tags.Add(values.String("api"));
        IConfigMap original = new ConfigMap();
        original.SetCaseMode(ConfigCaseMode.Insensitive);
        original.SetPathDefault("Service.Tags", values.Array(tags));
        original.SetPathDefault("Service.Enabled", values.Boolean(true));

        ConfigMap copy = original.Copy();
        string[] keys = original.Keys();
        string[] children = original.ChildKeysDefault("service");
        keys[0] = "changed";
        children[0] = "changed";
        tags.Get(0).StringValue = "worker";
        original.SetPathDefault("Service.Enabled", values.Boolean(false));

        Assert.That(original.Keys(), Is.EqualTo(new string[] { "Service" }));
        Assert.That(original.ChildKeysDefault("service"), Is.EqualTo(new string[] { "Enabled", "Tags" }));
        Assert.That(copy.HasPathDefault("service.tags"), Is.True);
        Assert.That(copy.GetPathDefault("SERVICE.TAGS").ArrayValue!.Get(0).StringValue, Is.EqualTo("api"));
        Assert.That(copy.GetPathDefault("service.enabled").BooleanValue, Is.True);
        Assert.That(copy.ToDisplayString(), Is.EqualTo("{Service: {Enabled: true, Tags: [\"api\"]}}"));
    }

    [Test]
    public void Snapshot_PublicInterfaceResolvesAliasChainsAndPreservesSourceMetadata()
    {
        IConfig snapshot = new ConfigBuilder()
            .SetCaseMode(ConfigCaseMode.Insensitive)
            .AddString("Service.Host=localhost\n", ConfigFormat.Flat, "settings.flat")
            .AddAlias("Legacy.Host", "Service.Host")
            .AddAlias("Older.Host", "Legacy.Host")
            .Build();

        ConfigValue value = snapshot.Value("OLDER.HOST");

        Assert.That(snapshot.Has("older.host"), Is.True);
        Assert.That(value.Kind, Is.EqualTo(ConfigValueKind.String));
        Assert.That(value.StringValue, Is.EqualTo("localhost"));
        Assert.That(value.SourceName, Is.EqualTo("settings.flat"));
        Assert.That(value.PrecedenceRank, Is.EqualTo(1));
        Assert.That(value.OriginalKey, Is.EqualTo("Service.Host"));
        Assert.That(value.LineNumber, Is.EqualTo(1));
        Assert.That(value.ColumnNumber, Is.EqualTo(1));
        Assert.That(snapshot.Keys(), Is.EqualTo(new string[] { "Service" }));
        Assert.That(snapshot.Children("Service"), Is.EqualTo(new string[] { "Host" }));
        Assert.That(snapshot.Section("Service").Get("Host").StringValue, Is.EqualTo("localhost"));
        Assert.That(snapshot.Diagnostics().HasErrors(), Is.False);
    }

    [Test]
    public void Snapshot_IsImmutableAcrossInputsBuilderAndReturnedCopies()
    {
        IConfigValues values = new ConfigValues();
        ConfigArray tags = new ConfigArray();
        tags.Add(values.String("api"));
        ConfigMap input = new ConfigMap();
        input.SetPathDefault("service.host", values.String("first"));
        input.SetPathDefault("service.tags", values.Array(tags));
        ConfigBuilder builder = new ConfigBuilder()
            .AddMemory(input, "memory")
            .AddAlias("legacy.host", "service.host");
        IConfig snapshot = builder.Build();

        input.SetPathDefault("service.host", values.String("input-change"));
        tags.Get(0).StringValue = "input-change";
        builder.AddOverride("service.host", "builder-change");
        builder.AddAlias("later.host", "service.host");
        ConfigValue returnedValue = snapshot.Value("service.tags");
        returnedValue.ArrayValue!.Get(0).StringValue = "returned-change";
        ConfigValue required = snapshot.Require("service.host");
        required.StringValue = "required-change";
        ConfigMap section = snapshot.Section("service");
        section.Set("host", values.String("section-change"));
        string[] keys = snapshot.Keys();
        string[] children = snapshot.Children("service");
        keys[0] = "keys-change";
        children[0] = "children-change";
        ConfigDiagnostics diagnostics = snapshot.Diagnostics();
        diagnostics.AddError("test", "service.host", "diagnostic-change", 0, 0);

        Assert.That(snapshot.GetString("service.host"), Is.EqualTo("first"));
        Assert.That(snapshot.GetStringArray("service.tags"), Is.EqualTo(new string[] { "api" }));
        Assert.That(snapshot.GetString("legacy.host"), Is.EqualTo("first"));
        Assert.That(snapshot.Has("later.host"), Is.False);
        Assert.That(snapshot.Keys(), Is.EqualTo(new string[] { "service" }));
        Assert.That(snapshot.Children("service"), Is.EqualTo(new string[] { "host", "tags" }));
        Assert.That(snapshot.Diagnostics().ErrorCount(), Is.Zero);
    }
}
