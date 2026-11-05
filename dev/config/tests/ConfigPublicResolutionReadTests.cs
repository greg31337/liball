using System;

namespace config.Tests;

public class ConfigPublicResolutionReadTests
{
    [Test]
    public void PublicReads_ResolveEscapedPathsSectionsChildrenAndAliases()
    {
        IConfig config = new ConfigBuilder()
            .AddString("{\"service.primary\":{\"host.name\":\"localhost\",\"nested\":{\"child.one\":\"one\",\"child.two\":\"two\"}}}", ConfigFormat.Json, "escaped.json")
            .AddAlias("legacy.host", "service\\.primary.host\\.name")
            .Build();

        ConfigValue host = config.Value("service\\.primary.host\\.name");
        ConfigMap section = config.Section("service\\.primary");

        Assert.Multiple(() =>
        {
            Assert.That(config.Has("service\\.primary.host\\.name"), Is.True);
            Assert.That(config.Has("legacy.host"), Is.True);
            Assert.That(config.GetString("legacy.host"), Is.EqualTo("localhost"));
            Assert.That(host.SourceName, Is.EqualTo("escaped.json"));
            Assert.That(host.OriginalKey, Is.EqualTo("service\\.primary.host\\.name"));
            Assert.That(config.Keys(), Is.EqualTo(new string[] { "service.primary" }));
            Assert.That(config.Children("service\\.primary"), Is.EqualTo(new string[] { "host.name", "nested" }));
            Assert.That(config.Children("service\\.primary.nested"), Is.EqualTo(new string[] { "child.one", "child.two" }));
            Assert.That(section.Get("host.name").StringValue, Is.EqualTo("localhost"));
            Assert.That(section.GetPathDefault("nested.child\\.two").StringValue, Is.EqualTo("two"));
        });
    }

    [Test]
    public void PublicResolution_UsesConfigPathsBeforeEnvironmentFallbackAndResolvesContainers()
    {
        string configuredName = "CONFIG_PUBLIC_CONFIGURED_" + Guid.NewGuid().ToString("N");
        string fallbackName = "CONFIG_PUBLIC_FALLBACK_" + Guid.NewGuid().ToString("N");
        string? previousConfigured = Environment.GetEnvironmentVariable(configuredName);
        string? previousFallback = Environment.GetEnvironmentVariable(fallbackName);
        Environment.SetEnvironmentVariable(configuredName, "environment-loses");
        Environment.SetEnvironmentVariable(fallbackName, "environment-wins");

        try
        {
            string json = "{\"" + configuredName + "\":\"configuration-wins\",\"selected\":\"${" + configuredName + "}\",\"fallback\":\"${" + fallbackName + "}\",\"nested\":{\"selected\":\"value-${selected}\"},\"items\":[\"${selected}\",\"${" + fallbackName + "}\"]}";
            IConfig config = new ConfigBuilder().AddString(json, ConfigFormat.Json, "substitutions.json").Build();

            Assert.Multiple(() =>
            {
                Assert.That(config.GetString("selected"), Is.EqualTo("configuration-wins"));
                Assert.That(config.GetString("fallback"), Is.EqualTo("environment-wins"));
                Assert.That(config.Section("nested").Get("selected").StringValue, Is.EqualTo("value-configuration-wins"));
                Assert.That(config.GetStringArray("items"), Is.EqualTo(new string[] { "configuration-wins", "environment-wins" }));
                Assert.That(config.Diagnostics().HasErrors(), Is.False);
            });
        }
        finally
        {
            Environment.SetEnvironmentVariable(configuredName, previousConfigured);
            Environment.SetEnvironmentVariable(fallbackName, previousFallback);
        }
    }

    [Test]
    public void PublicResolution_ReportsRequiredMissingReferencesAndBuildOrThrowPreservesContext()
    {
        string missingName = "CONFIG_PUBLIC_MISSING_" + Guid.NewGuid().ToString("N");
        ConfigResolutionOptions options = new ConfigResolutionOptions(ConfigSubstitutionLookupMode.Enabled, ConfigSubstitutionLookupMode.Disabled, ConfigSubstitutionMissingMode.Error, 8, ConfigOptional.Required, 8);
        ConfigBuilder builder = new ConfigBuilder()
            .SetResolutionOptions(options)
            .AddString("required=before-${" + missingName + "}-after\noptional=before-${?" + missingName + "}-after", ConfigFormat.Flat, "missing.flat");

        IConfig config = builder.Build();
        ConfigDiagnostics diagnostics = config.Diagnostics();
        ConfigError error = diagnostics.Error(0);

        Assert.Multiple(() =>
        {
            Assert.That(config.GetString("required"), Is.EqualTo("before--after"));
            Assert.That(config.GetString("optional"), Is.EqualTo("before--after"));
            Assert.That(diagnostics.ErrorCount(), Is.EqualTo(1));
            Assert.That(error.SourceName, Is.EqualTo("missing.flat"));
            Assert.That(error.Path, Is.EqualTo("required"));
            Assert.That(error.LineNumber, Is.EqualTo(1));
            Assert.That(error.ColumnNumber, Is.EqualTo(1));
            Assert.That(error.Message, Is.EqualTo("missing substitution reference: " + missingName));
        });

        ConfigLoadException? exception = Assert.Throws<ConfigLoadException>(() => builder.BuildOrThrow());
        Assert.Multiple(() =>
        {
            Assert.That(exception, Is.Not.Null);
            Assert.That(exception!.SourceName, Is.EqualTo("missing.flat"));
            Assert.That(exception.Path, Is.EqualTo("required"));
            Assert.That(exception.LineNumber, Is.EqualTo(1));
            Assert.That(exception.ColumnNumber, Is.EqualTo(1));
        });
    }

    [Test]
    public void PublicResolution_ReportsCyclesAndDepthLimitsWithoutRecursingForever()
    {
        ConfigResolutionOptions options = new ConfigResolutionOptions(ConfigSubstitutionLookupMode.Enabled, ConfigSubstitutionLookupMode.Disabled, ConfigSubstitutionMissingMode.Error, 1, ConfigOptional.Required, 8);
        IConfig config = new ConfigBuilder()
            .SetResolutionOptions(options)
            .AddString("cycle.a=${cycle.b}\ncycle.b=${cycle.a}\nchain.a=${chain.b}\nchain.b=${chain.c}\nchain.c=end", ConfigFormat.Flat, "resolution.flat")
            .Build();
        ConfigDiagnostics diagnostics = config.Diagnostics();

        Assert.Multiple(() =>
        {
            Assert.That(config.GetString("cycle.a"), Is.Empty);
            Assert.That(config.GetString("chain.a"), Is.Empty);
            Assert.That(config.GetString("chain.b"), Is.EqualTo("end"));
            Assert.That(HasError(diagnostics, "cycle.a", "substitution cycle:"), Is.True);
            Assert.That(HasError(diagnostics, "chain.b", "substitution depth limit exceeded"), Is.True);
        });
    }

    [Test]
    public void PublicReads_ConvertEveryScalarArrayAndEnumType()
    {
        IConfig config = BuildTypedConfig();

        Assert.Multiple(() =>
        {
            Assert.That(config.GetString("text"), Is.EqualTo("12.5"));
            Assert.That(config.GetBool("boolean"), Is.True);
            Assert.That(config.GetInt("integer"), Is.EqualTo(42));
            Assert.That(config.GetLong("long"), Is.EqualTo(1234567890123L));
            Assert.That(config.GetDecimal("decimal"), Is.EqualTo(12.75m));
            Assert.That(config.GetDouble("double"), Is.EqualTo(1.5d));
            Assert.That(config.GetTimeSpan("duration"), Is.EqualTo(TimeSpan.FromMilliseconds(1500)));
            Assert.That(config.GetByteSize("size"), Is.EqualTo(2L * 1024L));
            Assert.That(config.GetStringArray("strings"), Is.EqualTo(new string[] { "one", "2", "true" }));
            Assert.That(config.GetIntArray("integers"), Is.EqualTo(new int[] { 1, 2 }));
            Assert.That(config.GetLongArray("longs"), Is.EqualTo(new long[] { 3L, 4L }));
            Assert.That(config.GetDecimalArray("decimals"), Is.EqualTo(new decimal[] { 5.5m, 6.5m }));
            Assert.That(config.GetDoubleArray("doubles"), Is.EqualTo(new double[] { 7.5d, 8.5d }));
            Assert.That(config.GetEnum("mode", typeof(PublicReadMode)), Is.EqualTo(PublicReadMode.Fast));
        });
    }

    [Test]
    public void PublicReads_ApplyEveryDefaultAndRequiredReadContract()
    {
        IConfig defaults = new ConfigBuilder()
            .AddString("{\"bad\":\"not-a-value\",\"object\":{},\"mode\":\"invalid\"}", ConfigFormat.Json, "defaults.json")
            .Build();
        string[] stringFallback = new string[] { "fallback" };
        int[] intFallback = new int[] { 10 };
        long[] longFallback = new long[] { 11L };
        decimal[] decimalFallback = new decimal[] { 12.5m };
        double[] doubleFallback = new double[] { 13.5d };

        Assert.Multiple(() =>
        {
            Assert.That(defaults.GetStringOrDefault("missing", "fallback"), Is.EqualTo("fallback"));
            Assert.That(defaults.GetBoolOrDefault("bad", true), Is.True);
            Assert.That(defaults.GetIntOrDefault("bad", 2), Is.EqualTo(2));
            Assert.That(defaults.GetLongOrDefault("bad", 3L), Is.EqualTo(3L));
            Assert.That(defaults.GetDecimalOrDefault("bad", 4.5m), Is.EqualTo(4.5m));
            Assert.That(defaults.GetDoubleOrDefault("bad", 5.5d), Is.EqualTo(5.5d));
            Assert.That(defaults.GetTimeSpanOrDefault("bad", TimeSpan.FromSeconds(6)), Is.EqualTo(TimeSpan.FromSeconds(6)));
            Assert.That(defaults.GetByteSizeOrDefault("bad", 7L), Is.EqualTo(7L));
            Assert.That(defaults.GetStringArrayOrDefault("object", stringFallback), Is.EqualTo(stringFallback));
            Assert.That(defaults.GetIntArrayOrDefault("object", intFallback), Is.EqualTo(intFallback));
            Assert.That(defaults.GetLongArrayOrDefault("object", longFallback), Is.EqualTo(longFallback));
            Assert.That(defaults.GetDecimalArrayOrDefault("object", decimalFallback), Is.EqualTo(decimalFallback));
            Assert.That(defaults.GetDoubleArrayOrDefault("object", doubleFallback), Is.EqualTo(doubleFallback));
            Assert.That(defaults.GetEnumOrDefault("mode", typeof(PublicReadMode), PublicReadMode.Slow), Is.EqualTo(PublicReadMode.Slow));
            Assert.That(defaults.GetEnum("missing", typeof(PublicReadMode)), Is.EqualTo(PublicReadMode.Unknown));
        });

        IConfig required = BuildTypedConfig();
        Assert.Multiple(() =>
        {
            Assert.That(required.Require("text").NumberValue, Is.EqualTo(12.5m));
            Assert.That(required.RequireString("text"), Is.EqualTo("12.5"));
            Assert.That(required.RequireBool("boolean"), Is.True);
            Assert.That(required.RequireInt("integer"), Is.EqualTo(42));
            Assert.That(required.RequireLong("long"), Is.EqualTo(1234567890123L));
            Assert.That(required.RequireDecimal("decimal"), Is.EqualTo(12.75m));
            Assert.That(required.RequireDouble("double"), Is.EqualTo(1.5d));
            Assert.That(required.RequireTimeSpan("duration"), Is.EqualTo(TimeSpan.FromMilliseconds(1500)));
            Assert.That(required.RequireByteSize("size"), Is.EqualTo(2L * 1024L));
            Assert.That(required.RequireStringArray("strings"), Is.EqualTo(new string[] { "one", "2", "true" }));
            Assert.That(required.RequireIntArray("integers"), Is.EqualTo(new int[] { 1, 2 }));
            Assert.That(required.RequireLongArray("longs"), Is.EqualTo(new long[] { 3L, 4L }));
            Assert.That(required.RequireDecimalArray("decimals"), Is.EqualTo(new decimal[] { 5.5m, 6.5m }));
            Assert.That(required.RequireDoubleArray("doubles"), Is.EqualTo(new double[] { 7.5d, 8.5d }));
            Assert.That(required.RequireEnum("mode", typeof(PublicReadMode)), Is.EqualTo(PublicReadMode.Fast));
            Assert.Throws<ConfigConversionException>(() => required.Require("missing"));
            Assert.Throws<ConfigConversionException>(() => required.RequireInt("missing"));
            Assert.Throws<ConfigConversionException>(() => required.RequireEnum("missing", typeof(PublicReadMode)));
        });
    }

    [Test]
    public void PublicReads_ReturnEveryExceptionlessResultAndConversionDiagnostics()
    {
        IConfig config = BuildTypedConfig();

        Assert.Multiple(() =>
        {
            AssertSuccess(config.ReadString("text"), "12.5");
            Assert.That(config.ReadBool("boolean").BooleanValue, Is.True);
            Assert.That(config.ReadInt("integer").IntValue, Is.EqualTo(42));
            Assert.That(config.ReadLong("long").LongValue, Is.EqualTo(1234567890123L));
            Assert.That(config.ReadDecimal("decimal").DecimalValue, Is.EqualTo(12.75m));
            Assert.That(config.ReadDouble("double").DoubleValue, Is.EqualTo(1.5d));
            Assert.That(config.ReadTimeSpan("duration").TimeSpanValue, Is.EqualTo(TimeSpan.FromMilliseconds(1500)));
            Assert.That(config.ReadByteSize("size").ByteSizeValue, Is.EqualTo(2L * 1024L));
            Assert.That(config.ReadStringArray("strings").StringArrayValue, Is.EqualTo(new string[] { "one", "2", "true" }));
            Assert.That(config.ReadIntArray("integers").IntArrayValue, Is.EqualTo(new int[] { 1, 2 }));
            Assert.That(config.ReadLongArray("longs").LongArrayValue, Is.EqualTo(new long[] { 3L, 4L }));
            Assert.That(config.ReadDecimalArray("decimals").DecimalArrayValue, Is.EqualTo(new decimal[] { 5.5m, 6.5m }));
            Assert.That(config.ReadDoubleArray("doubles").DoubleArrayValue, Is.EqualTo(new double[] { 7.5d, 8.5d }));
            Assert.That(config.ReadEnum("mode", typeof(PublicReadMode)).EnumValue, Is.EqualTo(PublicReadMode.Fast));
        });

        IConfig failures = new ConfigBuilder()
            .AddString("bad=not-an-integer", ConfigFormat.Flat, "reads.flat")
            .AddString("{\"null\":null}", ConfigFormat.Json, "null.json")
            .Build();
        ConfigReadResult missing = failures.ReadInt("missing");
        ConfigReadResult nullResult = failures.ReadInt("null");
        ConfigReadResult malformed = failures.ReadInt("bad");
        ConfigReadResult invalidEnumRequest = failures.ReadEnum("bad", typeof(string));

        Assert.Multiple(() =>
        {
            Assert.That(missing.Status, Is.EqualTo(ConfigReadStatus.Missing));
            Assert.That(missing.Diagnostic.Path, Is.EqualTo("missing"));
            Assert.That(nullResult.Status, Is.EqualTo(ConfigReadStatus.Null));
            Assert.That(nullResult.Diagnostic.SourceName, Is.EqualTo("null.json"));
            Assert.That(malformed.Status, Is.EqualTo(ConfigReadStatus.Malformed));
            Assert.That(malformed.Diagnostic.SourceName, Is.EqualTo("reads.flat"));
            Assert.That(malformed.Diagnostic.Path, Is.EqualTo("bad"));
            Assert.That(malformed.Diagnostic.LineNumber, Is.EqualTo(1));
            Assert.That(malformed.Diagnostic.ColumnNumber, Is.EqualTo(1));
            Assert.That(malformed.Diagnostic.Message, Is.EqualTo("value cannot be converted to 32-bit integer"));
            Assert.That(invalidEnumRequest.Status, Is.EqualTo(ConfigReadStatus.Malformed));
            Assert.That(invalidEnumRequest.Diagnostic.Message, Is.EqualTo("type is not an enum"));
        });

        ConfigConversionException? exception = Assert.Throws<ConfigConversionException>(() => failures.RequireInt("bad"));
        Assert.Multiple(() =>
        {
            Assert.That(exception, Is.Not.Null);
            Assert.That(exception!.SourceName, Is.EqualTo("reads.flat"));
            Assert.That(exception.Path, Is.EqualTo("bad"));
            Assert.That(exception.LineNumber, Is.EqualTo(1));
            Assert.That(exception.ColumnNumber, Is.EqualTo(1));
            Assert.That(exception.Message, Does.Contain("value cannot be converted to 32-bit integer"));
        });
    }

    private static IConfig BuildTypedConfig()
    {
        return new ConfigBuilder()
            .AddString("{\"text\":12.5,\"boolean\":\"yes\",\"integer\":\"42\",\"long\":\"1234567890123\",\"decimal\":\"12.75\",\"double\":\"1.5\",\"duration\":\"1500ms\",\"size\":\"2kb\",\"strings\":[\"one\",2,true],\"integers\":[\"1\",2],\"longs\":\"3,4\",\"decimals\":[5.5,\"6.5\"],\"doubles\":\"7.5,8.5\",\"mode\":\"fAsT\"}", ConfigFormat.Json, "typed.json")
            .Build();
    }

    private static bool HasError(ConfigDiagnostics diagnostics, string path, string messagePrefix)
    {
        int index = 0;
        while (index < diagnostics.ErrorCount())
        {
            ConfigError error = diagnostics.Error(index);
            if (error.Path == path && error.Message.StartsWith(messagePrefix, StringComparison.Ordinal))
            {
                return true;
            }

            index = index + 1;
        }

        return false;
    }

    private static void AssertSuccess(ConfigReadResult result, string expected)
    {
        Assert.That(result.Status, Is.EqualTo(ConfigReadStatus.Success));
        Assert.That(result.StringValue, Is.EqualTo(expected));
        Assert.That(result.Diagnostic.Message, Is.Empty);
    }

    private enum PublicReadMode
    {
        Unknown = 0,
        Fast = 1,
        Slow = 2
    }
}
