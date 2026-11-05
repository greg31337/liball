using NUnit.Framework;

namespace uuid.Tests;

[TestFixture]
public class SnowflakeParseTests
{
    [Test]
    public void Decode_ReturnsValueUsingGeneratorSettings()
    {
        SnowflakeSettings settings = new SnowflakeSettings(1000, 42, 10, 12, SnowflakeClockRollbackBehavior.FailFast);
        SnowflakeGenerator generator = new SnowflakeGenerator(settings);
        ulong raw = SnowflakeGenerator.Pack(124456, 42, 7, settings);

        SnowflakeValue value = generator.Decode(raw);

        Assert.That(value.TimestampMilliseconds(), Is.EqualTo(123456));
        Assert.That(value.UnixMilliseconds(), Is.EqualTo(124456));
        Assert.That(value.MachineId(), Is.EqualTo(42));
        Assert.That(value.Sequence(), Is.EqualTo(7));
    }

    [Test]
    public void ParseDecimalString_ParsesWithoutLosingPrecision()
    {
        SnowflakeSettings settings = new SnowflakeSettings(1000, 42, 10, 12, SnowflakeClockRollbackBehavior.FailFast);
        ulong raw = SnowflakeGenerator.Pack(124456, 42, 7, settings);

        SnowflakeParseResult result = SnowflakeGenerator.ParseDecimalString("000" + raw.ToString(), settings);

        Assert.That(result.Ok, Is.True);
        Assert.That(result.Value, Is.Not.Null);
        Assert.That(result.Value.ToUInt64(), Is.EqualTo(raw));
        Assert.That(result.NormalizedText, Is.EqualTo(raw.ToString()));
        Assert.That(result.Value.MachineId(), Is.EqualTo(42));
        Assert.That(result.Value.Sequence(), Is.EqualTo(7));
    }

    [Test]
    public void GeneratorParseMethods_UseGeneratorSettings()
    {
        SnowflakeSettings settings = new SnowflakeSettings(1000, 42, 10, 12, SnowflakeClockRollbackBehavior.FailFast);
        SnowflakeGenerator generator = new SnowflakeGenerator(settings);
        SnowflakeValue generated = generator.NextAtUnixMillisecondsWithSequence(124456, 7);

        SnowflakeParseResult parsed = generator.Parse(generated.ToDecimalString());
        SnowflakeParseResult tryParsed = generator.TryParse(generated.ToDecimalString());
        SnowflakeValue parsedValue = generator.ParseValue(generated.ToDecimalString());

        Assert.That(parsed.Ok, Is.True);
        Assert.That(tryParsed.Ok, Is.True);
        Assert.That(parsed.Value, Is.Not.Null);
        Assert.That(parsed.Value.MachineId(), Is.EqualTo(42));
        Assert.That(parsedValue.Sequence(), Is.EqualTo(7));
    }

    [Test]
    public void ParseDecimalString_AcceptsMaximumSnowflakeValue()
    {
        SnowflakeSettings settings = SnowflakeSettings.TwitterDefault();

        SnowflakeParseResult result = SnowflakeGenerator.ParseDecimalString(SnowflakeLimits.MaxValue.ToString(), settings);

        Assert.That(result.Ok, Is.True);
        Assert.That(result.Value, Is.Not.Null);
        Assert.That(result.Value.ToUInt64(), Is.EqualTo(SnowflakeLimits.MaxValue));
    }

    [Test]
    public void ParseDecimalString_RejectsInvalidText()
    {
        SnowflakeSettings settings = SnowflakeSettings.TwitterDefault();

        AssertParseFailure(null, settings, "Snowflake text is null");
        AssertParseFailure("", settings, "Snowflake text is empty");
        AssertParseFailure("123x", settings, "invalid decimal character at index 3");
        AssertParseFailure("-1", settings, "invalid decimal character at index 0");
        AssertParseFailure("9223372036854775808", settings, "Snowflake decimal value exceeds 63-bit range");
    }

    [Test]
    public void ParseDecimalString_RejectsInvalidSettings()
    {
        SnowflakeSettings invalid = SnowflakeSettings.TwitterDefault();
        invalid.MachineId = 1024;

        AssertParseFailure("123", null, "Snowflake settings is null");
        AssertParseFailure("123", invalid, "machine ID is outside configured bit range");
    }

    [Test]
    public void ParseValue_ThrowsForInvalidDecimalText()
    {
        SnowflakeGenerator generator = new Snowflake().DefaultGenerator();

        Assert.That(() => generator.ParseValue("123x"), Throws.TypeOf<IdentifierException>());
    }

    static void AssertParseFailure(string? text, SnowflakeSettings? settings, string error)
    {
        SnowflakeParseResult result = SnowflakeGenerator.ParseDecimalString(text, settings);

        Assert.That(result.Ok, Is.False);
        Assert.That(result.Value, Is.Null);
        Assert.That(result.NormalizedText, Is.EqualTo(""));
        Assert.That(result.Error, Is.EqualTo(error));
    }
}
