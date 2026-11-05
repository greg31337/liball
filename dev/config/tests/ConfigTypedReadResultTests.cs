using System;

namespace config.Tests;

public class ConfigTypedReadResultTests
{
    [Test]
    public void TypedReads_DistinguishSuccessMissingNullAndMalformedWithDiagnostics()
    {
        ConfigMap nullValues = new ConfigMap();
        nullValues.Set("nullValue", ConfigValue.Null());
        IConfig config = new ConfigBuilder()
            .AddString("valid=42\nbad=not-a-number\n", ConfigFormat.Flat, "typed.flat")
            .AddMemory(nullValues, "memory")
            .Build();

        ConfigReadResult success = config.ReadInt("valid");
        ConfigReadResult missing = config.ReadInt("missing");
        ConfigReadResult nullResult = config.ReadInt("nullValue");
        ConfigReadResult malformed = config.ReadInt("bad");

        Assert.Multiple(() =>
        {
            Assert.That(success.Status, Is.EqualTo(ConfigReadStatus.Success));
            Assert.That(success.IntValue, Is.EqualTo(42));
            Assert.That(success.Diagnostic.Message, Is.Empty);
            Assert.That(missing.Status, Is.EqualTo(ConfigReadStatus.Missing));
            Assert.That(missing.Diagnostic.Path, Is.EqualTo("missing"));
            Assert.That(missing.Diagnostic.SourceName, Is.Empty);
            Assert.That(nullResult.Status, Is.EqualTo(ConfigReadStatus.Null));
            Assert.That(nullResult.Diagnostic.SourceName, Is.EqualTo("memory"));
            Assert.That(nullResult.Diagnostic.Message, Is.EqualTo("config value is null"));
            Assert.That(malformed.Status, Is.EqualTo(ConfigReadStatus.Malformed));
            Assert.That(malformed.Diagnostic.SourceName, Is.EqualTo("typed.flat"));
            Assert.That(malformed.Diagnostic.Path, Is.EqualTo("bad"));
            Assert.That(malformed.Diagnostic.LineNumber, Is.EqualTo(2));
            Assert.That(malformed.Diagnostic.ColumnNumber, Is.EqualTo(1));
            Assert.That(malformed.Diagnostic.Message, Is.EqualTo("value cannot be converted to 32-bit integer"));
        });
    }

    [Test]
    public void TypedReads_ReturnEverySupportedConvertedValue()
    {
        ConfigArray strings = new ConfigArray();
        strings.Add(ConfigValue.String("one"));
        strings.Add(ConfigValue.Number(2m));
        ConfigMap values = new ConfigMap();
        values.Set("text", ConfigValue.Number(12.5m));
        values.Set("boolean", ConfigValue.String("yes"));
        values.Set("integer", ConfigValue.String("42"));
        values.Set("long", ConfigValue.String("1234567890123"));
        values.Set("decimal", ConfigValue.String("12.75"));
        values.Set("double", ConfigValue.String("1.5"));
        values.Set("duration", ConfigValue.String("5m"));
        values.Set("size", ConfigValue.String("2KB"));
        values.Set("strings", ConfigValue.Array(strings));
        values.Set("integers", ConfigValue.String("1,2"));
        values.Set("longs", ConfigValue.String("3,4"));
        values.Set("decimals", ConfigValue.String("5.5,6.5"));
        values.Set("doubles", ConfigValue.String("7.5,8.5"));
        values.Set("mode", ConfigValue.String("fAsT"));
        IConfig config = new ConfigBuilder().AddMemory(values, "memory").Build();

        Assert.Multiple(() =>
        {
            Assert.That(config.ReadString("text").StringValue, Is.EqualTo("12.5"));
            Assert.That(config.ReadBool("boolean").BooleanValue, Is.True);
            Assert.That(config.ReadInt("integer").IntValue, Is.EqualTo(42));
            Assert.That(config.ReadLong("long").LongValue, Is.EqualTo(1234567890123L));
            Assert.That(config.ReadDecimal("decimal").DecimalValue, Is.EqualTo(12.75m));
            Assert.That(config.ReadDouble("double").DoubleValue, Is.EqualTo(1.5d));
            Assert.That(config.ReadTimeSpan("duration").TimeSpanValue, Is.EqualTo(TimeSpan.FromMinutes(5)));
            Assert.That(config.ReadByteSize("size").ByteSizeValue, Is.EqualTo(2L * 1024L));
            Assert.That(config.ReadStringArray("strings").StringArrayValue, Is.EqualTo(new string[] { "one", "2" }));
            Assert.That(config.ReadIntArray("integers").IntArrayValue, Is.EqualTo(new int[] { 1, 2 }));
            Assert.That(config.ReadLongArray("longs").LongArrayValue, Is.EqualTo(new long[] { 3L, 4L }));
            Assert.That(config.ReadDecimalArray("decimals").DecimalArrayValue, Is.EqualTo(new decimal[] { 5.5m, 6.5m }));
            Assert.That(config.ReadDoubleArray("doubles").DoubleArrayValue, Is.EqualTo(new double[] { 7.5d, 8.5d }));
            Assert.That(config.ReadEnum("mode", typeof(ReadMode)).EnumValue, Is.EqualTo(ReadMode.Fast));
        });

        ConfigReadResult first = config.ReadIntArray("integers");
        first.IntArrayValue[0] = 99;
        Assert.That(config.ReadIntArray("integers").IntArrayValue, Is.EqualTo(new int[] { 1, 2 }));
    }

    [Test]
    public void TypedReads_ReportMalformedForEveryFailedConversionWithoutThrowing()
    {
        ConfigMap values = new ConfigMap();
        values.Set("badBool", ConfigValue.String("perhaps"));
        values.Set("badInt", ConfigValue.String("1.5"));
        values.Set("badLong", ConfigValue.String("1.5"));
        values.Set("badDecimal", ConfigValue.String("many"));
        values.Set("badDouble", ConfigValue.String("many"));
        values.Set("badDuration", ConfigValue.String("later"));
        values.Set("badSize", ConfigValue.String("huge"));
        values.Set("badStrings", ConfigValue.Object(new ConfigMap()));
        values.Set("badIntegers", ConfigValue.String("1,bad"));
        values.Set("badLongs", ConfigValue.String("1,bad"));
        values.Set("badDecimals", ConfigValue.String("1,bad"));
        values.Set("badDoubles", ConfigValue.String("1,bad"));
        values.Set("badMode", ConfigValue.String("Turbo"));
        IConfig config = new ConfigBuilder().AddMemory(values, "memory").Build();

        Assert.Multiple(() =>
        {
            Assert.That(config.ReadBool("badBool").Status, Is.EqualTo(ConfigReadStatus.Malformed));
            Assert.That(config.ReadInt("badInt").Status, Is.EqualTo(ConfigReadStatus.Malformed));
            Assert.That(config.ReadLong("badLong").Status, Is.EqualTo(ConfigReadStatus.Malformed));
            Assert.That(config.ReadDecimal("badDecimal").Status, Is.EqualTo(ConfigReadStatus.Malformed));
            Assert.That(config.ReadDouble("badDouble").Status, Is.EqualTo(ConfigReadStatus.Malformed));
            Assert.That(config.ReadTimeSpan("badDuration").Status, Is.EqualTo(ConfigReadStatus.Malformed));
            Assert.That(config.ReadByteSize("badSize").Status, Is.EqualTo(ConfigReadStatus.Malformed));
            Assert.That(config.ReadStringArray("badStrings").Status, Is.EqualTo(ConfigReadStatus.Malformed));
            Assert.That(config.ReadIntArray("badIntegers").Status, Is.EqualTo(ConfigReadStatus.Malformed));
            Assert.That(config.ReadLongArray("badLongs").Status, Is.EqualTo(ConfigReadStatus.Malformed));
            Assert.That(config.ReadDecimalArray("badDecimals").Status, Is.EqualTo(ConfigReadStatus.Malformed));
            Assert.That(config.ReadDoubleArray("badDoubles").Status, Is.EqualTo(ConfigReadStatus.Malformed));
            Assert.That(config.ReadEnum("badMode", typeof(ReadMode)).Status, Is.EqualTo(ConfigReadStatus.Malformed));
            Assert.That(config.ReadEnum("badMode", typeof(string)).Status, Is.EqualTo(ConfigReadStatus.Malformed));
            Assert.That(config.ReadEnum("badMode", null!).Status, Is.EqualTo(ConfigReadStatus.Malformed));
        });
    }

    [Test]
    public void RequiredTypedReads_RejectExplicitNullForEverySupportedType()
    {
        ConfigMap values = new ConfigMap();
        values.Set("value", ConfigValue.Null());
        IConfig config = new ConfigBuilder().AddMemory(values, "memory").Build();

        Assert.Multiple(() =>
        {
            Assert.Throws<ConfigConversionException>(() => config.RequireString("value"));
            Assert.Throws<ConfigConversionException>(() => config.RequireBool("value"));
            Assert.Throws<ConfigConversionException>(() => config.RequireInt("value"));
            Assert.Throws<ConfigConversionException>(() => config.RequireLong("value"));
            Assert.Throws<ConfigConversionException>(() => config.RequireDecimal("value"));
            Assert.Throws<ConfigConversionException>(() => config.RequireDouble("value"));
            Assert.Throws<ConfigConversionException>(() => config.RequireTimeSpan("value"));
            Assert.Throws<ConfigConversionException>(() => config.RequireByteSize("value"));
            Assert.Throws<ConfigConversionException>(() => config.RequireStringArray("value"));
            Assert.Throws<ConfigConversionException>(() => config.RequireIntArray("value"));
            Assert.Throws<ConfigConversionException>(() => config.RequireLongArray("value"));
            Assert.Throws<ConfigConversionException>(() => config.RequireDecimalArray("value"));
            Assert.Throws<ConfigConversionException>(() => config.RequireDoubleArray("value"));
            Assert.Throws<ConfigConversionException>(() => config.RequireEnum("value", typeof(ReadMode)));
        });
    }

    [Test]
    public void RequiredTypedReads_RejectEveryFailedConversion()
    {
        ConfigMap values = new ConfigMap();
        values.Set("badBool", ConfigValue.String("perhaps"));
        values.Set("badInt", ConfigValue.String("1.5"));
        values.Set("badLong", ConfigValue.String("1.5"));
        values.Set("badDecimal", ConfigValue.String("many"));
        values.Set("badDouble", ConfigValue.String("many"));
        values.Set("badDuration", ConfigValue.String("later"));
        values.Set("badSize", ConfigValue.String("huge"));
        values.Set("badStrings", ConfigValue.Object(new ConfigMap()));
        values.Set("badIntegers", ConfigValue.String("1,bad"));
        values.Set("badLongs", ConfigValue.String("1,bad"));
        values.Set("badDecimals", ConfigValue.String("1,bad"));
        values.Set("badDoubles", ConfigValue.String("1,bad"));
        values.Set("badMode", ConfigValue.String("Turbo"));
        IConfig config = new ConfigBuilder().AddMemory(values, "memory").Build();

        Assert.Multiple(() =>
        {
            Assert.Throws<ConfigConversionException>(() => config.RequireBool("badBool"));
            Assert.Throws<ConfigConversionException>(() => config.RequireInt("badInt"));
            Assert.Throws<ConfigConversionException>(() => config.RequireLong("badLong"));
            Assert.Throws<ConfigConversionException>(() => config.RequireDecimal("badDecimal"));
            Assert.Throws<ConfigConversionException>(() => config.RequireDouble("badDouble"));
            Assert.Throws<ConfigConversionException>(() => config.RequireTimeSpan("badDuration"));
            Assert.Throws<ConfigConversionException>(() => config.RequireByteSize("badSize"));
            Assert.Throws<ConfigConversionException>(() => config.RequireStringArray("badStrings"));
            Assert.Throws<ConfigConversionException>(() => config.RequireIntArray("badIntegers"));
            Assert.Throws<ConfigConversionException>(() => config.RequireLongArray("badLongs"));
            Assert.Throws<ConfigConversionException>(() => config.RequireDecimalArray("badDecimals"));
            Assert.Throws<ConfigConversionException>(() => config.RequireDoubleArray("badDoubles"));
            Assert.Throws<ConfigConversionException>(() => config.RequireEnum("badMode", typeof(ReadMode)));
        });

        ConfigConversionException? error = Assert.Throws<ConfigConversionException>(() => config.RequireInt("badInt"));
        Assert.That(error!.Message, Does.Contain("value cannot be converted to 32-bit integer"));
    }

    private enum ReadMode
    {
        Unknown = 0,
        Fast = 1
    }
}
