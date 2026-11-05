namespace config.Tests;

public class ConfigConversionTests
{
    [Test]
    public void TypedReads_ConvertNativeAndTextScalarValues()
    {
        ConfigMap values = new ConfigMap();
        values.SetPathDefault("text", ConfigValue.String("hello"));
        values.SetPathDefault("flag.native", ConfigValue.Boolean(true));
        values.SetPathDefault("flag.text", ConfigValue.String("yes"));
        values.SetPathDefault("count.native", ConfigValue.Number(42m));
        values.SetPathDefault("count.text", ConfigValue.String("43"));
        values.SetPathDefault("long.text", ConfigValue.String("1234567890123"));
        values.SetPathDefault("decimal.text", ConfigValue.String("12.75"));
        values.SetPathDefault("double.text", ConfigValue.String("1.5"));
        values.SetPathDefault("duration.invariant", ConfigValue.String("01:02:03"));
        values.SetPathDefault("duration.friendly", ConfigValue.String("5m"));
        values.SetPathDefault("size.friendly", ConfigValue.String("2mb"));
        ConfigBuilder builder = new ConfigBuilder();
        builder.AddMemory(values, "memory");

        Config config = builder.Build();

        Assert.That(config.GetString("text"), Is.EqualTo("hello"));
        Assert.That(config.GetString("count.native"), Is.EqualTo("42"));
        Assert.That(config.GetBool("flag.native"), Is.True);
        Assert.That(config.GetBool("flag.text"), Is.True);
        Assert.That(config.GetInt("count.native"), Is.EqualTo(42));
        Assert.That(config.GetInt("count.text"), Is.EqualTo(43));
        Assert.That(config.GetLong("long.text"), Is.EqualTo(1234567890123L));
        Assert.That(config.GetDecimal("decimal.text"), Is.EqualTo(12.75m));
        Assert.That(config.GetDouble("double.text"), Is.EqualTo(1.5d));
        Assert.That(config.GetTimeSpan("duration.invariant"), Is.EqualTo(new TimeSpan(1, 2, 3)));
        Assert.That(config.GetTimeSpan("duration.friendly"), Is.EqualTo(TimeSpan.FromMinutes(5)));
        Assert.That(config.GetByteSize("size.friendly"), Is.EqualTo(2L * 1024L * 1024L));
    }

    [Test]
    public void TypedReads_UseAliasesWhenResolvingValues()
    {
        ConfigMap values = new ConfigMap();
        values.SetPathDefault("service.port", ConfigValue.String("5432"));
        values.SetPathDefault("service.enabled", ConfigValue.String("on"));
        ConfigBuilder builder = new ConfigBuilder();
        builder
            .AddMemory(values, "memory")
            .AddAlias("old.port", "service.port")
            .AddAlias("old.enabled", "service.enabled");

        Config config = builder.Build();

        Assert.That(config.GetInt("old.port"), Is.EqualTo(5432));
        Assert.That(config.GetBool("old.enabled"), Is.True);
    }

    [Test]
    public void TypedReads_ReturnFallbacksAndDefaultsForMissingMalformedAndFractionalValues()
    {
        ConfigMap values = new ConfigMap();
        values.SetPathDefault("bad.bool", ConfigValue.String("maybe"));
        values.SetPathDefault("bad.int", ConfigValue.String("12.5"));
        values.SetPathDefault("bad.whole", ConfigValue.Number(12.5m));
        values.SetPathDefault("bad.duration", ConfigValue.String("later"));
        values.SetPathDefault("bad.size", ConfigValue.String("large"));
        ConfigBuilder builder = new ConfigBuilder();
        builder.AddMemory(values, "memory");

        Config config = builder.Build();

        Assert.That(config.GetString("missing"), Is.EqualTo(""));
        Assert.That(config.GetStringOrDefault("missing", "fallback"), Is.EqualTo("fallback"));
        Assert.That(config.GetBool("bad.bool"), Is.False);
        Assert.That(config.GetBoolOrDefault("bad.bool", true), Is.True);
        Assert.That(config.GetInt("bad.int"), Is.EqualTo(0));
        Assert.That(config.GetIntOrDefault("bad.int", 7), Is.EqualTo(7));
        Assert.That(config.GetIntOrDefault("bad.whole", 8), Is.EqualTo(8));
        Assert.That(config.GetLong("missing"), Is.EqualTo(0L));
        Assert.That(config.GetDecimal("missing"), Is.EqualTo(0m));
        Assert.That(config.GetDouble("missing"), Is.EqualTo(0d));
        Assert.That(config.GetTimeSpan("bad.duration"), Is.EqualTo(TimeSpan.Zero));
        Assert.That(config.GetByteSize("bad.size"), Is.EqualTo(0L));
        Assert.That(config.GetStringArray("missing"), Is.EqualTo(new string[0]));
    }

    [Test]
    public void GetStringArray_ConvertsArraysCommaDelimitedStringsAndScalars()
    {
        ConfigArray array = new ConfigArray();
        array.Add(ConfigValue.String("one"));
        array.Add(ConfigValue.Number(2m));
        array.Add(ConfigValue.Boolean(true));
        ConfigMap values = new ConfigMap();
        values.SetPathDefault("array", ConfigValue.Array(array));
        values.SetPathDefault("csv", ConfigValue.String("alpha, beta,,gamma"));
        values.SetPathDefault("scalar", ConfigValue.Number(9m));
        ConfigBuilder builder = new ConfigBuilder();
        builder.AddMemory(values, "memory");

        Config config = builder.Build();

        Assert.That(config.GetStringArray("array"), Is.EqualTo(new string[] { "one", "2", "true" }));
        Assert.That(config.GetStringArray("csv"), Is.EqualTo(new string[] { "alpha", "beta", "gamma" }));
        Assert.That(config.GetStringArray("scalar"), Is.EqualTo(new string[] { "9" }));
    }

    [Test]
    public void FallbackOverloads_ReturnProvidedDefaultsForMissingMalformedAndUnsupportedValues()
    {
        ConfigMap values = new ConfigMap();
        values.SetPathDefault("bad.long", ConfigValue.String("12.5"));
        values.SetPathDefault("bad.decimal", ConfigValue.String("not-decimal"));
        values.SetPathDefault("bad.double", ConfigValue.String("not-double"));
        values.SetPathDefault("bad.duration", ConfigValue.String("not-duration"));
        values.SetPathDefault("bad.size", ConfigValue.String("not-size"));
        values.SetPathDefault("bad.array", ConfigValue.Object(new ConfigMap()));
        ConfigBuilder builder = new ConfigBuilder();
        builder.AddMemory(values, "memory");
        Config config = builder.Build();
        string[] fallbackArray = new string[] { "fallback" };

        string[] result = config.GetStringArrayOrDefault("missing.array", fallbackArray);
        result[0] = "changed";

        Assert.That(config.GetLongOrDefault("bad.long", 99L), Is.EqualTo(99L));
        Assert.That(config.GetLongOrDefault("missing.long", 98L), Is.EqualTo(98L));
        Assert.That(config.GetDecimalOrDefault("bad.decimal", 12.5m), Is.EqualTo(12.5m));
        Assert.That(config.GetDoubleOrDefault("bad.double", 14.5d), Is.EqualTo(14.5d));
        Assert.That(config.GetTimeSpanOrDefault("bad.duration", TimeSpan.FromSeconds(3)), Is.EqualTo(TimeSpan.FromSeconds(3)));
        Assert.That(config.GetByteSizeOrDefault("bad.size", 2048L), Is.EqualTo(2048L));
        Assert.That(config.GetStringArrayOrDefault("bad.array", fallbackArray), Is.EqualTo(new string[] { "fallback" }));
        Assert.That(fallbackArray, Is.EqualTo(new string[] { "fallback" }));
    }

    [Test]
    public void Require_ReturnsValuesAndUsesAliases()
    {
        ConfigMap values = new ConfigMap();
        values.SetPathDefault("service.host", ConfigValue.String("localhost"));
        values.SetPathDefault("service.port", ConfigValue.String("5432"));
        ConfigBuilder builder = new ConfigBuilder();
        builder
            .AddMemory(values, "memory")
            .AddAlias("old.host", "service.host");
        Config config = builder.Build();

        ConfigValue value = config.Require("old.host");

        Assert.That(value.StringValue, Is.EqualTo("localhost"));
        Assert.That(value.SourceName, Is.EqualTo("memory"));
        Assert.That(config.RequireInt("service.port"), Is.EqualTo(5432));
    }

    [Test]
    public void Require_ThrowsForMissingValues()
    {
        ConfigMap values = new ConfigMap();
        values.SetPathDefault("service.host", ConfigValue.String("localhost"));
        ConfigBuilder builder = new ConfigBuilder();
        builder.AddMemory(values, "memory");
        Config config = builder.Build();

        ConfigConversionException exception = Assert.Throws<ConfigConversionException>(() => config.Require("service.port"));

        Assert.That(exception!.Message, Is.EqualTo("missing required config value: service.port"));
    }

    [Test]
    public void TypedRequireMethods_ConvertPresentValues()
    {
        ConfigArray names = new ConfigArray();
        names.Add(ConfigValue.String("api"));
        names.Add(ConfigValue.String("worker"));
        ConfigMap values = new ConfigMap();
        values.SetPathDefault("text", ConfigValue.String("hello"));
        values.SetPathDefault("flag", ConfigValue.String("on"));
        values.SetPathDefault("int", ConfigValue.String("10"));
        values.SetPathDefault("long", ConfigValue.String("1234567890123"));
        values.SetPathDefault("decimal", ConfigValue.String("5.25"));
        values.SetPathDefault("double", ConfigValue.String("2.5"));
        values.SetPathDefault("duration", ConfigValue.String("750ms"));
        values.SetPathDefault("size", ConfigValue.String("3kb"));
        values.SetPathDefault("names", ConfigValue.Array(names));
        ConfigBuilder builder = new ConfigBuilder();
        builder.AddMemory(values, "memory");

        Config config = builder.Build();

        Assert.That(config.RequireString("text"), Is.EqualTo("hello"));
        Assert.That(config.RequireBool("flag"), Is.True);
        Assert.That(config.RequireInt("int"), Is.EqualTo(10));
        Assert.That(config.RequireLong("long"), Is.EqualTo(1234567890123L));
        Assert.That(config.RequireDecimal("decimal"), Is.EqualTo(5.25m));
        Assert.That(config.RequireDouble("double"), Is.EqualTo(2.5d));
        Assert.That(config.RequireTimeSpan("duration"), Is.EqualTo(TimeSpan.FromMilliseconds(750)));
        Assert.That(config.RequireByteSize("size"), Is.EqualTo(3L * 1024L));
        Assert.That(config.RequireStringArray("names"), Is.EqualTo(new string[] { "api", "worker" }));
    }

    [Test]
    public void BoolReads_AcceptCommonTextualForms()
    {
        ConfigMap values = new ConfigMap();
        values.SetPathDefault("true.word", ConfigValue.String(" true "));
        values.SetPathDefault("true.one", ConfigValue.String("1"));
        values.SetPathDefault("true.yes", ConfigValue.String("YES"));
        values.SetPathDefault("true.short", ConfigValue.String("y"));
        values.SetPathDefault("true.on", ConfigValue.String("On"));
        values.SetPathDefault("false.word", ConfigValue.String(" false "));
        values.SetPathDefault("false.zero", ConfigValue.String("0"));
        values.SetPathDefault("false.no", ConfigValue.String("NO"));
        values.SetPathDefault("false.short", ConfigValue.String("n"));
        values.SetPathDefault("false.off", ConfigValue.String("Off"));
        ConfigBuilder builder = new ConfigBuilder();
        builder.AddMemory(values, "memory");

        Config config = builder.Build();

        Assert.That(config.GetBool("true.word"), Is.True);
        Assert.That(config.GetBool("true.one"), Is.True);
        Assert.That(config.GetBool("true.yes"), Is.True);
        Assert.That(config.GetBool("true.short"), Is.True);
        Assert.That(config.GetBool("true.on"), Is.True);
        Assert.That(config.GetBoolOrDefault("false.word", true), Is.False);
        Assert.That(config.GetBoolOrDefault("false.zero", true), Is.False);
        Assert.That(config.GetBoolOrDefault("false.no", true), Is.False);
        Assert.That(config.GetBoolOrDefault("false.short", true), Is.False);
        Assert.That(config.GetBoolOrDefault("false.off", true), Is.False);
    }

    [Test]
    public void TimeSpanReads_AcceptInvariantNumericAndFriendlyDurationForms()
    {
        ConfigMap values = new ConfigMap();
        values.SetPathDefault("duration.invariant", ConfigValue.String("00:00:01.500"));
        values.SetPathDefault("duration.number", ConfigValue.Number(1500m));
        values.SetPathDefault("duration.ms", ConfigValue.String("250ms"));
        values.SetPathDefault("duration.seconds", ConfigValue.String("2s"));
        values.SetPathDefault("duration.minutes", ConfigValue.String("3m"));
        values.SetPathDefault("duration.hours", ConfigValue.String("4h"));
        values.SetPathDefault("duration.days", ConfigValue.String("1.5d"));
        ConfigBuilder builder = new ConfigBuilder();
        builder.AddMemory(values, "memory");

        Config config = builder.Build();

        Assert.That(config.GetTimeSpan("duration.invariant"), Is.EqualTo(TimeSpan.FromMilliseconds(1500)));
        Assert.That(config.GetTimeSpan("duration.number"), Is.EqualTo(TimeSpan.FromMilliseconds(1500)));
        Assert.That(config.GetTimeSpan("duration.ms"), Is.EqualTo(TimeSpan.FromMilliseconds(250)));
        Assert.That(config.GetTimeSpan("duration.seconds"), Is.EqualTo(TimeSpan.FromSeconds(2)));
        Assert.That(config.GetTimeSpan("duration.minutes"), Is.EqualTo(TimeSpan.FromMinutes(3)));
        Assert.That(config.GetTimeSpan("duration.hours"), Is.EqualTo(TimeSpan.FromHours(4)));
        Assert.That(config.GetTimeSpan("duration.days"), Is.EqualTo(TimeSpan.FromDays(1.5d)));
    }

    [Test]
    public void TimeSpanReads_ReturnFallbackForValuesOutsideTimeSpanRange()
    {
        ConfigMap values = new ConfigMap();
        values.SetPathDefault("duration.numericOverflow", ConfigValue.Number(decimal.MaxValue));
        values.SetPathDefault("duration.numericUnderflow", ConfigValue.Number(decimal.MinValue));
        values.SetPathDefault("duration.suffixOverflow", ConfigValue.String("1e300d"));
        values.SetPathDefault("duration.suffixUnderflow", ConfigValue.String("-1e300d"));
        values.SetPathDefault("duration.notNumber", ConfigValue.String("NaNms"));
        ConfigBuilder builder = new ConfigBuilder();
        builder.AddMemory(values, "memory");
        Config config = builder.Build();
        TimeSpan fallback = TimeSpan.FromSeconds(17);

        Assert.That(config.GetTimeSpanOrDefault("duration.numericOverflow", fallback), Is.EqualTo(fallback));
        Assert.That(config.GetTimeSpanOrDefault("duration.numericUnderflow", fallback), Is.EqualTo(fallback));
        Assert.That(config.GetTimeSpanOrDefault("duration.suffixOverflow", fallback), Is.EqualTo(fallback));
        Assert.That(config.GetTimeSpanOrDefault("duration.suffixUnderflow", fallback), Is.EqualTo(fallback));
        Assert.That(config.GetTimeSpanOrDefault("duration.notNumber", fallback), Is.EqualTo(fallback));
    }

    [Test]
    public void ByteSizeReads_AcceptPlainNumbersAndUnitSuffixedForms()
    {
        ConfigMap values = new ConfigMap();
        values.SetPathDefault("size.number", ConfigValue.Number(128m));
        values.SetPathDefault("size.plain", ConfigValue.String("512"));
        values.SetPathDefault("size.bytes", ConfigValue.String("7b"));
        values.SetPathDefault("size.k", ConfigValue.String("2k"));
        values.SetPathDefault("size.kb", ConfigValue.String("2kb"));
        values.SetPathDefault("size.m", ConfigValue.String("3m"));
        values.SetPathDefault("size.mb", ConfigValue.String("4mb"));
        values.SetPathDefault("size.gb", ConfigValue.String("1.5gb"));
        values.SetPathDefault("size.t", ConfigValue.String("1t"));
        ConfigBuilder builder = new ConfigBuilder();
        builder.AddMemory(values, "memory");

        Config config = builder.Build();

        Assert.That(config.GetByteSize("size.number"), Is.EqualTo(128L));
        Assert.That(config.GetByteSize("size.plain"), Is.EqualTo(512L));
        Assert.That(config.GetByteSize("size.bytes"), Is.EqualTo(7L));
        Assert.That(config.GetByteSize("size.k"), Is.EqualTo(2L * 1024L));
        Assert.That(config.GetByteSize("size.kb"), Is.EqualTo(2L * 1024L));
        Assert.That(config.GetByteSize("size.m"), Is.EqualTo(3L * 1024L * 1024L));
        Assert.That(config.GetByteSize("size.mb"), Is.EqualTo(4L * 1024L * 1024L));
        Assert.That(config.GetByteSize("size.gb"), Is.EqualTo(1610612736L));
        Assert.That(config.GetByteSize("size.t"), Is.EqualTo(1L * 1024L * 1024L * 1024L * 1024L));
    }

    [Test]
    public void ByteSizeReads_ReturnFallbackForValuesThatOverflowLong()
    {
        ConfigMap values = new ConfigMap();
        values.SetPathDefault("size.numericMaximum", ConfigValue.Number(long.MaxValue));
        values.SetPathDefault("size.numericMinimum", ConfigValue.Number(long.MinValue));
        values.SetPathDefault("size.maximum", ConfigValue.String("9223372036854775807"));
        values.SetPathDefault("size.minimum", ConfigValue.String("-9223372036854775808"));
        values.SetPathDefault("size.overflow", ConfigValue.String("9223372036854775808"));
        values.SetPathDefault("size.underflow", ConfigValue.String("-9223372036854775809"));
        values.SetPathDefault("size.suffixOverflow", ConfigValue.String("9007199254740992kb"));
        ConfigBuilder builder = new ConfigBuilder();
        builder.AddMemory(values, "memory");
        Config config = builder.Build();

        Assert.That(config.GetByteSizeOrDefault("size.numericMaximum", 16L), Is.EqualTo(long.MaxValue));
        Assert.That(config.GetByteSizeOrDefault("size.numericMinimum", 15L), Is.EqualTo(long.MinValue));
        Assert.That(config.GetByteSizeOrDefault("size.maximum", 17L), Is.EqualTo(long.MaxValue));
        Assert.That(config.GetByteSizeOrDefault("size.minimum", 14L), Is.EqualTo(long.MinValue));
        Assert.That(config.GetByteSizeOrDefault("size.overflow", 18L), Is.EqualTo(18L));
        Assert.That(config.GetByteSizeOrDefault("size.underflow", 20L), Is.EqualTo(20L));
        Assert.That(config.GetByteSizeOrDefault("size.suffixOverflow", 19L), Is.EqualTo(19L));
    }

    [Test]
    public void MalformedAndMissingConversionReads_ReturnDefaultsOrFallbacks()
    {
        ConfigMap values = new ConfigMap();
        values.SetPathDefault("null.value", ConfigValue.Null());
        values.SetPathDefault("object.value", ConfigValue.Object(new ConfigMap()));
        values.SetPathDefault("bad.int", ConfigValue.String("999999999999999999999"));
        values.SetPathDefault("bad.long", ConfigValue.String("12.5"));
        values.SetPathDefault("bad.decimal", ConfigValue.String("abc"));
        values.SetPathDefault("bad.double", ConfigValue.String("abc"));
        values.SetPathDefault("bad.duration", ConfigValue.String("10w"));
        values.SetPathDefault("bad.size", ConfigValue.String("10xb"));
        ConfigBuilder builder = new ConfigBuilder();
        builder.AddMemory(values, "memory");
        Config config = builder.Build();
        string[] fallbackArray = new string[] { "one", "two" };

        Assert.That(config.GetString("null.value"), Is.EqualTo(""));
        Assert.That(config.GetStringOrDefault("null.value", "fallback"), Is.EqualTo("fallback"));
        Assert.That(config.GetIntOrDefault("bad.int", 9), Is.EqualTo(9));
        Assert.That(config.GetLongOrDefault("bad.long", 10L), Is.EqualTo(10L));
        Assert.That(config.GetDecimalOrDefault("bad.decimal", 11m), Is.EqualTo(11m));
        Assert.That(config.GetDoubleOrDefault("bad.double", 12d), Is.EqualTo(12d));
        Assert.That(config.GetTimeSpanOrDefault("bad.duration", TimeSpan.FromMinutes(13)), Is.EqualTo(TimeSpan.FromMinutes(13)));
        Assert.That(config.GetByteSizeOrDefault("bad.size", 14L), Is.EqualTo(14L));
        Assert.That(config.GetStringArrayOrDefault("null.value", fallbackArray), Is.EqualTo(new string[] { "one", "two" }));
        Assert.That(config.GetStringArrayOrDefault("object.value", fallbackArray), Is.EqualTo(new string[] { "one", "two" }));
        Assert.That(config.GetBoolOrDefault("missing.bool", true), Is.True);
        Assert.That(config.GetIntOrDefault("missing.int", 15), Is.EqualTo(15));
    }
}
