using System;
using System.Globalization;

namespace config.Tests;

public class ConfigNumericArrayAndEnumTests
{
    [Test]
    public void NumericArrayReads_ConvertNativeCsvScalarAndSubstitutedValuesInvariantly()
    {
        CultureInfo previousCulture = CultureInfo.CurrentCulture;
        CultureInfo.CurrentCulture = new CultureInfo("fr-FR");

        try
        {
            ConfigArray integers = new ConfigArray();
            integers.Add(ConfigValue.Number(1m));
            integers.Add(ConfigValue.String("2"));
            integers.Add(ConfigValue.Number(-3m));
            ConfigArray decimals = new ConfigArray();
            decimals.Add(ConfigValue.Number(1.25m));
            decimals.Add(ConfigValue.String("2.5"));
            decimals.Add(ConfigValue.Boolean(true));
            ConfigMap values = new ConfigMap();
            values.Set("integers", ConfigValue.Array(integers));
            values.Set("longs", ConfigValue.String("1, 9223372036854775807"));
            values.Set("decimals", ConfigValue.Array(decimals));
            values.Set("doubles", ConfigValue.String("1.5, 2.75"));
            values.Set("single", ConfigValue.Number(7m));
            values.Set("source", ConfigValue.String("8, 9"));
            values.Set("substituted", ConfigValue.String("${source}"));
            IConfig config = new ConfigBuilder().AddMemory(values, "memory").Build();

            Assert.That(config.GetIntArray("integers"), Is.EqualTo(new int[] { 1, 2, -3 }));
            Assert.That(config.GetLongArray("longs"), Is.EqualTo(new long[] { 1L, long.MaxValue }));
            Assert.That(config.GetDecimalArray("decimals"), Is.EqualTo(new decimal[] { 1.25m, 2.5m, 1m }));
            Assert.That(config.GetDoubleArray("doubles"), Is.EqualTo(new double[] { 1.5d, 2.75d }));
            Assert.That(config.GetIntArray("single"), Is.EqualTo(new int[] { 7 }));
            Assert.That(config.GetIntArray("substituted"), Is.EqualTo(new int[] { 8, 9 }));
        }
        finally
        {
            CultureInfo.CurrentCulture = previousCulture;
        }
    }

    [Test]
    public void NumericArrayFallbacks_AreAllOrNothingAndIndependentCopies()
    {
        ConfigArray badIntegers = new ConfigArray();
        badIntegers.Add(ConfigValue.Number(1m));
        badIntegers.Add(ConfigValue.String("bad"));
        ConfigArray badLongs = new ConfigArray();
        badLongs.Add(ConfigValue.Number(1.5m));
        ConfigMap values = new ConfigMap();
        values.Set("integers", ConfigValue.Array(badIntegers));
        values.Set("longs", ConfigValue.Array(badLongs));
        values.Set("decimals", ConfigValue.Object(new ConfigMap()));
        values.Set("doubles", ConfigValue.Null());
        IConfig config = new ConfigBuilder().AddMemory(values, "memory").Build();
        int[] intFallback = new int[] { 10, 11 };
        long[] longFallback = new long[] { 12L };
        decimal[] decimalFallback = new decimal[] { 13.5m };
        double[] doubleFallback = new double[] { 14.5d };

        int[] intResult = config.GetIntArrayOrDefault("integers", intFallback);
        long[] longResult = config.GetLongArrayOrDefault("longs", longFallback);
        decimal[] decimalResult = config.GetDecimalArrayOrDefault("decimals", decimalFallback);
        double[] doubleResult = config.GetDoubleArrayOrDefault("doubles", doubleFallback);
        intResult[0] = 99;
        longResult[0] = 99L;
        decimalResult[0] = 99m;
        doubleResult[0] = 99d;

        Assert.That(config.GetIntArray("integers"), Is.Empty);
        Assert.That(config.GetLongArray("longs"), Is.Empty);
        Assert.That(intFallback, Is.EqualTo(new int[] { 10, 11 }));
        Assert.That(longFallback, Is.EqualTo(new long[] { 12L }));
        Assert.That(decimalFallback, Is.EqualTo(new decimal[] { 13.5m }));
        Assert.That(doubleFallback, Is.EqualTo(new double[] { 14.5d }));
    }

    [Test]
    public void RequiredNumericArrays_ConvertPresentValuesAndRejectMissingPaths()
    {
        ConfigMap values = new ConfigMap();
        values.Set("integers", ConfigValue.String("1,2"));
        values.Set("longs", ConfigValue.String("3,4"));
        values.Set("decimals", ConfigValue.String("5.5,6.5"));
        values.Set("doubles", ConfigValue.String("7.5,8.5"));
        IConfig config = new ConfigBuilder().AddMemory(values, "memory").Build();

        Assert.That(config.RequireIntArray("integers"), Is.EqualTo(new int[] { 1, 2 }));
        Assert.That(config.RequireLongArray("longs"), Is.EqualTo(new long[] { 3L, 4L }));
        Assert.That(config.RequireDecimalArray("decimals"), Is.EqualTo(new decimal[] { 5.5m, 6.5m }));
        Assert.That(config.RequireDoubleArray("doubles"), Is.EqualTo(new double[] { 7.5d, 8.5d }));
        Assert.Throws<ConfigConversionException>(() => config.RequireIntArray("missing"));
        Assert.Throws<ConfigConversionException>(() => config.RequireLongArray("missing"));
        Assert.Throws<ConfigConversionException>(() => config.RequireDecimalArray("missing"));
        Assert.Throws<ConfigConversionException>(() => config.RequireDoubleArray("missing"));
    }

    [Test]
    public void EnumReads_UseCaseInsensitiveNamesFallbacksAndRequiredPaths()
    {
        ConfigMap values = new ConfigMap();
        values.Set("mode", ConfigValue.String("fAsT"));
        values.Set("bad", ConfigValue.String("unknown-name"));
        values.Set("source", ConfigValue.String("Slow"));
        values.Set("substituted", ConfigValue.String("${source}"));
        IConfig config = new ConfigBuilder().AddMemory(values, "memory").Build();

        Assert.That(config.GetEnum("mode", typeof(ReadMode)), Is.EqualTo(ReadMode.Fast));
        Assert.That(config.GetEnum("bad", typeof(ReadMode)), Is.EqualTo(ReadMode.Unknown));
        Assert.That(config.GetEnumOrDefault("bad", typeof(ReadMode), ReadMode.Slow), Is.EqualTo(ReadMode.Slow));
        Assert.That(config.GetEnumOrDefault("missing", typeof(ReadMode), ReadMode.Fast), Is.EqualTo(ReadMode.Fast));
        Assert.That(config.RequireEnum("substituted", typeof(ReadMode)), Is.EqualTo(ReadMode.Slow));
        Assert.Throws<ConfigConversionException>(() => config.RequireEnum("missing", typeof(ReadMode)));
    }

    [Test]
    public void EnumReads_ValidateRuntimeTypeAndFallback()
    {
        ConfigMap values = new ConfigMap();
        values.Set("mode", ConfigValue.String("Fast"));
        IConfig config = new ConfigBuilder().AddMemory(values, "memory").Build();

        Assert.Throws<ArgumentException>(() => config.GetEnum("mode", typeof(string)));
        Assert.Throws<ArgumentException>(() => config.GetEnumOrDefault("mode", typeof(ReadMode), OtherMode.First));
        Assert.Throws<ArgumentNullException>(() => config.GetEnum("mode", null!));
    }

    private enum ReadMode
    {
        Unknown = 0,
        Fast = 1,
        Slow = 2
    }

    private enum OtherMode
    {
        First = 0
    }
}
