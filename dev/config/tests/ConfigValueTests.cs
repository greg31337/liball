namespace config.Tests;

public class ConfigValueTests
{
    [Test]
    public void ConfigValue_CreatesMissingNullAndScalarValues()
    {
        ConfigValue missing = ConfigValue.Missing();
        ConfigValue nullValue = ConfigValue.Null();
        ConfigValue text = ConfigValue.String("localhost");
        ConfigValue number = ConfigValue.Number(5432m);
        ConfigValue boolean = ConfigValue.Boolean(true);

        Assert.That(missing.Kind, Is.EqualTo(ConfigValueKind.Missing));
        Assert.That(missing.IsMissing(), Is.True);
        Assert.That(missing.ToDisplayString(), Is.EqualTo("(missing)"));
        Assert.That(nullValue.Kind, Is.EqualTo(ConfigValueKind.Null));
        Assert.That(nullValue.IsNull(), Is.True);
        Assert.That(nullValue.ToDisplayString(), Is.EqualTo("null"));
        Assert.That(text.Kind, Is.EqualTo(ConfigValueKind.String));
        Assert.That(text.StringValue, Is.EqualTo("localhost"));
        Assert.That(text.ToDisplayString(), Is.EqualTo("\"localhost\""));
        Assert.That(number.Kind, Is.EqualTo(ConfigValueKind.Number));
        Assert.That(number.NumberValue, Is.EqualTo(5432m));
        Assert.That(number.ToDisplayString(), Is.EqualTo("5432"));
        Assert.That(boolean.Kind, Is.EqualTo(ConfigValueKind.Boolean));
        Assert.That(boolean.BooleanValue, Is.True);
        Assert.That(boolean.ToDisplayString(), Is.EqualTo("true"));
    }

    [Test]
    public void ConfigArray_AddsValuesAndReturnsMissingForInvalidIndexes()
    {
        ConfigArray array = new ConfigArray();
        array.Add(ConfigValue.String("api"));
        array.Add(ConfigValue.Number(2m));

        ConfigValue first = array.Get(0);
        ConfigValue second = array.Get(1);
        ConfigValue beforeStart = array.Get(-1);
        ConfigValue afterEnd = array.Get(2);
        ConfigValue wrapper = ConfigValue.Array(array);

        Assert.That(array.Count(), Is.EqualTo(2));
        Assert.That(first.StringValue, Is.EqualTo("api"));
        Assert.That(second.NumberValue, Is.EqualTo(2m));
        Assert.That(beforeStart.Kind, Is.EqualTo(ConfigValueKind.Missing));
        Assert.That(afterEnd.Kind, Is.EqualTo(ConfigValueKind.Missing));
        Assert.That(wrapper.Kind, Is.EqualTo(ConfigValueKind.Array));
        Assert.That(wrapper.ArrayValue, Is.SameAs(array));
        Assert.That(wrapper.ToDisplayString(), Is.EqualTo("[\"api\", 2]"));
    }

    [Test]
    public void ConfigMap_SetsValuesAndReturnsMissingForAbsentKeys()
    {
        ConfigMap map = new ConfigMap();
        map.Set("host", ConfigValue.String("localhost"));
        map.Set("port", ConfigValue.Number(5432m));

        ConfigValue host = map.Get("host");
        ConfigValue port = map.Get("port");
        ConfigValue absent = map.Get("missing");
        ConfigValue wrapper = ConfigValue.Object(map);

        Assert.That(map.Count(), Is.EqualTo(2));
        Assert.That(map.Has("host"), Is.True);
        Assert.That(map.Has("missing"), Is.False);
        Assert.That(host.StringValue, Is.EqualTo("localhost"));
        Assert.That(port.NumberValue, Is.EqualTo(5432m));
        Assert.That(absent.Kind, Is.EqualTo(ConfigValueKind.Missing));
        Assert.That(map.Keys(), Is.EqualTo(new string[] { "host", "port" }));
        Assert.That(wrapper.Kind, Is.EqualTo(ConfigValueKind.Object));
        Assert.That(wrapper.ObjectValue, Is.SameAs(map));
        Assert.That(wrapper.ToDisplayString(), Is.EqualTo("{host: \"localhost\", port: 5432}"));
    }

    [Test]
    public void ConfigValue_StoresSourceMetadata()
    {
        ConfigValue plain = ConfigValue.String("before");
        ConfigValue sourced = ConfigValue.String("after");
        ConfigValue returned = sourced.SetSource("appsettings.json", 20, "database.host", 12, 5);

        Assert.That(returned, Is.SameAs(sourced));
        Assert.That(plain.HasSource(), Is.False);
        Assert.That(plain.HasLocation(), Is.False);
        Assert.That(sourced.HasSource(), Is.True);
        Assert.That(sourced.HasLocation(), Is.True);
        Assert.That(sourced.SourceName, Is.EqualTo("appsettings.json"));
        Assert.That(sourced.PrecedenceRank, Is.EqualTo(20));
        Assert.That(sourced.OriginalKey, Is.EqualTo("database.host"));
        Assert.That(sourced.LineNumber, Is.EqualTo(12));
        Assert.That(sourced.ColumnNumber, Is.EqualTo(5));
    }

    [Test]
    public void ConfigValue_CopyPreservesNestedValueSourceMetadata()
    {
        ConfigArray array = new ConfigArray();
        array.Add(ConfigValue.String("json").SetSource("base.json", 10, "sources.0", 3, 11));

        ConfigMap map = new ConfigMap();
        map.Set("sources", ConfigValue.Array(array).SetSource("base.json", 10, "sources", 2, 3));
        ConfigValue root = ConfigValue.Object(map).SetSource("base.json", 10, "", 1, 1);

        ConfigValue copy = root.Copy();
        ConfigValue copiedArrayValue = copy.ObjectValue!.Get("sources");
        ConfigValue copiedFirst = copiedArrayValue.ArrayValue!.Get(0);

        Assert.That(copy, Is.Not.SameAs(root));
        Assert.That(copy.ObjectValue, Is.Not.SameAs(map));
        Assert.That(copiedArrayValue.ArrayValue, Is.Not.SameAs(array));
        Assert.That(copy.SourceName, Is.EqualTo("base.json"));
        Assert.That(copiedArrayValue.SourceName, Is.EqualTo("base.json"));
        Assert.That(copiedArrayValue.OriginalKey, Is.EqualTo("sources"));
        Assert.That(copiedFirst.SourceName, Is.EqualTo("base.json"));
        Assert.That(copiedFirst.OriginalKey, Is.EqualTo("sources.0"));
        Assert.That(copiedFirst.LineNumber, Is.EqualTo(3));
        Assert.That(copiedFirst.ColumnNumber, Is.EqualTo(11));
    }
}
