using System;
using NUnit.Framework;

namespace uuid.Tests;

[TestFixture]
public class SnowflakePublicApiBehaviorTests
{
    [Test]
    public void SnowflakeInterface_ExposesSettingsFactoriesLimitsAndValidation()
    {
        ISnowflake snowflake = new Snowflake();
        SnowflakeSettings first = snowflake.DefaultSettings();
        SnowflakeSettings second = snowflake.DefaultSettings();
        SnowflakeGenerator defaultGenerator = snowflake.DefaultGenerator();
        SnowflakeGenerator customGenerator = snowflake.CreateGenerator(new SnowflakeSettings(1000, 42, 10, 12, SnowflakeClockRollbackBehavior.Wait));

        first.EpochUnixMilliseconds = 0;
        first.MachineId = 1;

        Assert.That(second.EpochUnixMilliseconds, Is.EqualTo(SnowflakeSettings.DefaultTwitterEpochUnixMilliseconds));
        Assert.That(second.MachineId, Is.EqualTo(0));
        Assert.That(second.MachineBitCount, Is.EqualTo(10));
        Assert.That(second.SequenceBitCount, Is.EqualTo(12));
        Assert.That(second.ClockRollbackBehavior, Is.EqualTo(SnowflakeClockRollbackBehavior.FailFast));
        Assert.That(defaultGenerator.ToSettings().EpochUnixMilliseconds, Is.EqualTo(SnowflakeSettings.DefaultTwitterEpochUnixMilliseconds));
        Assert.That(customGenerator.ToSettings().MachineId, Is.EqualTo(42));
        Assert.That(snowflake.ValidateSettings(second).Ok, Is.True);
        Assert.That(snowflake.MaxMachineId(0), Is.EqualTo(0));
        Assert.That(snowflake.MaxMachineId(10), Is.EqualTo(1023));
        Assert.That(snowflake.MaxMachineId(22), Is.EqualTo(4194303));
        Assert.That(snowflake.MaxSequence(0), Is.EqualTo(0));
        Assert.That(snowflake.MaxSequence(12), Is.EqualTo(4095));
        Assert.That(snowflake.MaxSequence(22), Is.EqualTo(4194303));
        Assert.That(snowflake.MaxTimestampOffsetMilliseconds(), Is.EqualTo(2199023255551));
    }

    [Test]
    public void GeneratorInterface_ExposesEveryGenerationRangeDecodeAndParseOperation()
    {
        SnowflakeSettings settings = new SnowflakeSettings(1000, 42, 10, 12, SnowflakeClockRollbackBehavior.FailFast);
        SnowflakeGenerator concreteGenerator = new Snowflake().CreateGenerator(settings);
        concreteGenerator.Clock = new FixedSnowflakeClock(124458);
        ISnowflakeGenerator generator = concreteGenerator;
        DateTimeOffset firstTime = DateTimeOffset.FromUnixTimeMilliseconds(124456);
        DateTimeOffset secondTime = DateTimeOffset.FromUnixTimeMilliseconds(124457);

        SnowflakeValue first = generator.NextAt(firstTime);
        SnowflakeValue second = generator.NextAtUnixMilliseconds(124456);
        SnowflakeValue explicitAt = generator.NextAtWithSequence(secondTime, 7);
        SnowflakeValue explicitMilliseconds = generator.NextAtUnixMillisecondsWithSequence(124457, 8);
        SnowflakeValue startAt = generator.RangeStartAt(secondTime);
        SnowflakeValue startMilliseconds = generator.RangeStartAtUnixMilliseconds(124457);
        SnowflakeValue endAt = generator.RangeEndAt(secondTime);
        SnowflakeValue endMilliseconds = generator.RangeEndAtUnixMilliseconds(124457);
        SnowflakeValue decoded = generator.Decode(explicitAt.ToUInt64());
        SnowflakeParseResult parsed = generator.Parse("000" + explicitAt.ToDecimalString());
        SnowflakeParseResult tried = generator.TryParse(explicitAt.ToDecimalString());
        SnowflakeValue parsedValue = generator.ParseValue(explicitAt.ToDecimalString());
        SnowflakeValue current = generator.Next();

        Assert.That(first.Sequence(), Is.EqualTo(0));
        Assert.That(second.Sequence(), Is.EqualTo(1));
        Assert.That(explicitAt.Sequence(), Is.EqualTo(7));
        Assert.That(explicitMilliseconds.Sequence(), Is.EqualTo(8));
        Assert.That(startAt.ToUInt64(), Is.EqualTo(startMilliseconds.ToUInt64()));
        Assert.That(startAt.MachineId(), Is.EqualTo(0));
        Assert.That(startAt.Sequence(), Is.EqualTo(0));
        Assert.That(endAt.ToUInt64(), Is.EqualTo(endMilliseconds.ToUInt64()));
        Assert.That(endAt.MachineId(), Is.EqualTo(1023));
        Assert.That(endAt.Sequence(), Is.EqualTo(4095));
        Assert.That(decoded.ToUInt64(), Is.EqualTo(explicitAt.ToUInt64()));
        Assert.That(parsed.Ok, Is.True);
        Assert.That(tried.Ok, Is.True);
        Assert.That(parsed.NormalizedText, Is.EqualTo(explicitAt.ToDecimalString()));
        Assert.That(parsedValue.ToUInt64(), Is.EqualTo(explicitAt.ToUInt64()));
        Assert.That(current.UnixMilliseconds(), Is.EqualTo(124458));
        Assert.That(generator.ToSettings().MachineId, Is.EqualTo(42));
    }

    [Test]
    public void ValueInterfaces_ExposeCanonicalNumericFieldTimestampAndSettingsOperations()
    {
        SnowflakeSettings settings = new SnowflakeSettings(1000, 42, 10, 12, SnowflakeClockRollbackBehavior.Wait);
        SnowflakeValue concrete = new SnowflakeGenerator(settings).NextAtUnixMillisecondsWithSequence(124456, 7);
        ISnowflakeValue value = concrete;
        IIdentifierValue identifier = concrete;
        ITimestampedIdentifierValue timestamped = concrete;

        Assert.That(value.ToUInt64(), Is.EqualTo((123456UL << 22) | (42UL << 12) | 7UL));
        Assert.That(value.ToDecimalString(), Is.EqualTo(value.ToUInt64().ToString()));
        Assert.That(identifier.ToCanonicalString(), Is.EqualTo(value.ToDecimalString()));
        Assert.That(identifier.Kind(), Is.EqualTo(IdentifierKind.Snowflake));
        Assert.That(value.TimestampMilliseconds(), Is.EqualTo(123456));
        Assert.That(value.UnixMilliseconds(), Is.EqualTo(124456));
        Assert.That(value.Time(), Is.EqualTo(DateTimeOffset.FromUnixTimeMilliseconds(124456)));
        Assert.That(value.MachineId(), Is.EqualTo(42));
        Assert.That(value.Sequence(), Is.EqualTo(7));
        Assert.That(value.ToSettings().ClockRollbackBehavior, Is.EqualTo(SnowflakeClockRollbackBehavior.Wait));
        Assert.That(timestamped.Timestamp().Ok, Is.True);
        Assert.That(timestamped.Timestamp().Kind, Is.EqualTo(IdentifierKind.Snowflake));
        Assert.That(timestamped.Timestamp().UnixMilliseconds, Is.EqualTo(124456));
        Assert.That(timestamped.Timestamp().UnixSeconds, Is.EqualTo(124));
        Assert.That(timestamped.Timestamp().Time, Is.EqualTo(DateTimeOffset.FromUnixTimeMilliseconds(124456)));
    }

    [Test]
    public void ConstantsAndEnums_HaveStableBoundaryValues()
    {
        Assert.That(SnowflakeSettings.DefaultTwitterEpochUnixMilliseconds, Is.EqualTo(1288834974657));
        Assert.That(SnowflakeSettings.TimestampBitCount, Is.EqualTo(41));
        Assert.That(SnowflakeSettings.AvailableWorkerAndSequenceBitCount, Is.EqualTo(22));
        Assert.That(SnowflakeLimits.MaxValue, Is.EqualTo(9223372036854775807UL));
        Assert.That((int)SnowflakeClockRollbackBehavior.FailFast, Is.EqualTo(0));
        Assert.That((int)SnowflakeClockRollbackBehavior.Wait, Is.EqualTo(1));
    }

    [Test]
    public void SettingsValidation_ReturnsCompleteFailuresForNullAndEveryMutableField()
    {
        ISnowflake snowflake = new Snowflake();
        SnowflakeSettings negativeMachineBits = snowflake.DefaultSettings();
        negativeMachineBits.MachineBitCount = -1;
        SnowflakeSettings highMachineBits = snowflake.DefaultSettings();
        highMachineBits.MachineBitCount = 23;
        SnowflakeSettings negativeSequenceBits = snowflake.DefaultSettings();
        negativeSequenceBits.SequenceBitCount = -1;
        SnowflakeSettings highSequenceBits = snowflake.DefaultSettings();
        highSequenceBits.SequenceBitCount = 23;
        SnowflakeSettings wrongTotal = snowflake.DefaultSettings();
        wrongTotal.SequenceBitCount = 11;
        SnowflakeSettings negativeMachine = snowflake.DefaultSettings();
        negativeMachine.MachineId = -1;
        SnowflakeSettings highMachine = snowflake.DefaultSettings();
        highMachine.MachineId = 1024;
        SnowflakeSettings rollback = snowflake.DefaultSettings();
        rollback.ClockRollbackBehavior = (SnowflakeClockRollbackBehavior)2;

        AssertValidationFailure(snowflake.ValidateSettings(null), IdentifierErrorCode.NullInput, "Snowflake settings is null");
        AssertValidationFailure(snowflake.ValidateSettings(negativeMachineBits), IdentifierErrorCode.InvalidValue, "machine bit count must be between 0 and 22");
        AssertValidationFailure(snowflake.ValidateSettings(highMachineBits), IdentifierErrorCode.InvalidValue, "machine bit count must be between 0 and 22");
        AssertValidationFailure(snowflake.ValidateSettings(negativeSequenceBits), IdentifierErrorCode.InvalidValue, "sequence bit count must be between 0 and 22");
        AssertValidationFailure(snowflake.ValidateSettings(highSequenceBits), IdentifierErrorCode.InvalidValue, "sequence bit count must be between 0 and 22");
        AssertValidationFailure(snowflake.ValidateSettings(wrongTotal), IdentifierErrorCode.InvalidValue, "machine and sequence bit counts must total 22");
        AssertValidationFailure(snowflake.ValidateSettings(negativeMachine), IdentifierErrorCode.InvalidValue, "machine ID is outside configured bit range");
        AssertValidationFailure(snowflake.ValidateSettings(highMachine), IdentifierErrorCode.InvalidValue, "machine ID is outside configured bit range");
        AssertValidationFailure(snowflake.ValidateSettings(rollback), IdentifierErrorCode.InvalidValue, "clock rollback behavior is not recognized");
    }

    [Test]
    public void SettingsConstructionAndLimitHelpers_ReportEveryDocumentedFailure()
    {
        ISnowflake snowflake = new Snowflake();
        SnowflakeSettings invalid = snowflake.DefaultSettings();
        invalid.MachineId = 1024;

        ArgumentNullException nullSettings = Assert.Throws<ArgumentNullException>(() => snowflake.CreateGenerator(null));
        ArgumentException invalidGenerator = Assert.Throws<ArgumentException>(() => snowflake.CreateGenerator(invalid));
        ArgumentException invalidMachine = Assert.Throws<ArgumentException>(() => new SnowflakeSettings(0, 32, 5, 17, SnowflakeClockRollbackBehavior.FailFast));
        ArgumentException invalidTotal = Assert.Throws<ArgumentException>(() => new SnowflakeSettings(0, 0, 5, 16, SnowflakeClockRollbackBehavior.FailFast));
        ArgumentException invalidRollback = Assert.Throws<ArgumentException>(() => new SnowflakeSettings(0, 0, 10, 12, (SnowflakeClockRollbackBehavior)2));
        ArgumentOutOfRangeException negativeMachineBits = Assert.Throws<ArgumentOutOfRangeException>(() => snowflake.MaxMachineId(-1));
        ArgumentOutOfRangeException highMachineBits = Assert.Throws<ArgumentOutOfRangeException>(() => snowflake.MaxMachineId(23));
        ArgumentOutOfRangeException negativeSequenceBits = Assert.Throws<ArgumentOutOfRangeException>(() => snowflake.MaxSequence(-1));
        ArgumentOutOfRangeException highSequenceBits = Assert.Throws<ArgumentOutOfRangeException>(() => snowflake.MaxSequence(23));

        Assert.That(nullSettings.ParamName, Is.EqualTo("settings"));
        Assert.That(invalidGenerator.ParamName, Is.EqualTo("SnowflakeSettings"));
        Assert.That(invalidMachine.ParamName, Is.EqualTo("SnowflakeSettings"));
        Assert.That(invalidTotal.ParamName, Is.EqualTo("SnowflakeSettings"));
        Assert.That(invalidRollback.ParamName, Is.EqualTo("SnowflakeSettings"));
        Assert.That(negativeMachineBits.ParamName, Is.EqualTo("bitCount"));
        Assert.That(highMachineBits.ParamName, Is.EqualTo("bitCount"));
        Assert.That(negativeSequenceBits.ParamName, Is.EqualTo("bitCount"));
        Assert.That(highSequenceBits.ParamName, Is.EqualTo("bitCount"));
    }

    [TestCase(null, IdentifierErrorCode.NullInput, "Snowflake text is null")]
    [TestCase("", IdentifierErrorCode.InvalidLength, "Snowflake text is empty")]
    [TestCase("-1", IdentifierErrorCode.InvalidCharacter, "invalid decimal character at index 0")]
    [TestCase("12x", IdentifierErrorCode.InvalidCharacter, "invalid decimal character at index 2")]
    [TestCase("9223372036854775808", IdentifierErrorCode.ValueOverflow, "Snowflake decimal value exceeds 63-bit range")]
    public void ParseTryParseAndParseValue_PreserveEveryFailure(string? text, IdentifierErrorCode code, string error)
    {
        ISnowflakeGenerator generator = new Snowflake().DefaultGenerator();

        SnowflakeParseResult parsed = generator.Parse(text);
        SnowflakeParseResult tried = generator.TryParse(text);

        AssertParseFailure(parsed, code, error);
        AssertParseFailure(tried, code, error);
        AssertIdentifierException(() => generator.ParseValue(text), code, error);
    }

    [Test]
    public void DecimalParsing_NormalizesZeroLeadingZerosAndMaximumWithoutPrecisionLoss()
    {
        ISnowflakeGenerator generator = new Snowflake().DefaultGenerator();

        SnowflakeParseResult zero = generator.Parse("000000000000000000000");
        SnowflakeParseResult ordinary = generator.Parse("0001234567890123456789");
        SnowflakeParseResult maximum = generator.Parse(SnowflakeLimits.MaxValue.ToString());

        AssertParseSuccess(zero, 0, "0");
        AssertParseSuccess(ordinary, 1234567890123456789UL, "1234567890123456789");
        AssertParseSuccess(maximum, SnowflakeLimits.MaxValue, "9223372036854775807");
        Assert.That(generator.ParseValue(maximum.NormalizedText).ToCanonicalString(), Is.EqualTo(maximum.NormalizedText));
    }

    [Test]
    public void ExplicitSequenceAndTimestampBoundaries_ReturnFieldsOrExactCodedFailures()
    {
        SnowflakeSettings settings = new SnowflakeSettings(1000, 1023, 10, 12, SnowflakeClockRollbackBehavior.FailFast);
        ISnowflakeGenerator generator = new SnowflakeGenerator(settings);
        long maximumTimestamp = 1000 + new Snowflake().MaxTimestampOffsetMilliseconds();

        SnowflakeValue minimum = generator.NextAtUnixMillisecondsWithSequence(1000, 0);
        SnowflakeValue maximum = generator.NextAtUnixMillisecondsWithSequence(maximumTimestamp, 4095);
        SnowflakeValue atTime = generator.NextAtWithSequence(DateTimeOffset.FromUnixTimeMilliseconds(124456), 7);

        Assert.That(minimum.ToUInt64(), Is.EqualTo(1023UL << 12));
        Assert.That(minimum.TimestampMilliseconds(), Is.EqualTo(0));
        Assert.That(maximum.ToUInt64(), Is.EqualTo(SnowflakeLimits.MaxValue));
        Assert.That(maximum.TimestampMilliseconds(), Is.EqualTo(2199023255551));
        Assert.That(maximum.MachineId(), Is.EqualTo(1023));
        Assert.That(maximum.Sequence(), Is.EqualTo(4095));
        Assert.That(atTime.UnixMilliseconds(), Is.EqualTo(124456));
        AssertIdentifierException(() => generator.NextAtUnixMillisecondsWithSequence(999, 0), IdentifierErrorCode.TimestampOverflow, "Snowflake timestamp is before the configured epoch");
        AssertIdentifierException(() => generator.NextAtUnixMillisecondsWithSequence(maximumTimestamp + 1, 0), IdentifierErrorCode.TimestampOverflow, "Snowflake timestamp exceeds the configured 41-bit range");
        AssertIdentifierException(() => generator.NextAtUnixMillisecondsWithSequence(1000, -1), IdentifierErrorCode.InvalidValue, "sequence is outside configured bit range");
        AssertIdentifierException(() => generator.NextAtUnixMillisecondsWithSequence(1000, 4096), IdentifierErrorCode.InvalidValue, "sequence is outside configured bit range");
        AssertIdentifierException(() => generator.NextAtWithSequence(DateTimeOffset.FromUnixTimeMilliseconds(1000), -1), IdentifierErrorCode.InvalidValue, "sequence is outside configured bit range");
    }

    [Test]
    public void StatefulSequence_ResetExhaustionFailFastAndExplicitWaitAreDeterministic()
    {
        SnowflakeGenerator failFast = new SnowflakeGenerator(new SnowflakeSettings(0, 7, 20, 2, SnowflakeClockRollbackBehavior.FailFast));
        SnowflakeValue first = failFast.NextAtUnixMilliseconds(1000);
        SnowflakeValue second = failFast.NextAtUnixMilliseconds(1000);
        SnowflakeValue third = failFast.NextAtUnixMilliseconds(1000);
        SnowflakeValue fourth = failFast.NextAtUnixMilliseconds(1000);
        IdentifierException exhausted = Assert.Throws<IdentifierException>(() => failFast.NextAtUnixMilliseconds(1000));
        SnowflakeValue reset = failFast.NextAtUnixMilliseconds(1001);
        IdentifierException rollback = Assert.Throws<IdentifierException>(() => failFast.NextAtUnixMilliseconds(1000));

        SnowflakeGenerator wait = new SnowflakeGenerator(new SnowflakeSettings(0, 7, 20, 2, SnowflakeClockRollbackBehavior.Wait));
        SnowflakeValue waitFirst = wait.NextAtUnixMilliseconds(1000);
        SnowflakeValue waitRollback = wait.NextAtUnixMilliseconds(999);

        Assert.That(first.Sequence(), Is.EqualTo(0));
        Assert.That(second.Sequence(), Is.EqualTo(1));
        Assert.That(third.Sequence(), Is.EqualTo(2));
        Assert.That(fourth.Sequence(), Is.EqualTo(3));
        Assert.That(exhausted.Code, Is.EqualTo(IdentifierErrorCode.SequenceExhaustion));
        Assert.That(exhausted.Message, Is.EqualTo("Snowflake sequence exhausted for millisecond"));
        Assert.That(reset.Sequence(), Is.EqualTo(0));
        Assert.That(rollback.Code, Is.EqualTo(IdentifierErrorCode.ClockRollback));
        Assert.That(rollback.Message, Is.EqualTo("Snowflake clock moved backwards"));
        Assert.That(waitFirst.Sequence(), Is.EqualTo(0));
        Assert.That(waitRollback.UnixMilliseconds(), Is.EqualTo(1000));
        Assert.That(waitRollback.Sequence(), Is.EqualTo(1));
    }

    [Test]
    public void RangeHelpers_BoundEveryLayoutAndReportTimestampFailures()
    {
        SnowflakeSettings noMachine = new SnowflakeSettings(1000, 0, 0, 22, SnowflakeClockRollbackBehavior.FailFast);
        SnowflakeGenerator generator = new SnowflakeGenerator(noMachine);
        long maximumTimestamp = 1000 + noMachine.MaxTimestampOffsetMilliseconds();

        SnowflakeValue minimumStart = generator.RangeStartAtUnixMilliseconds(1000);
        SnowflakeValue minimumEnd = generator.RangeEndAtUnixMilliseconds(1000);
        SnowflakeValue maximumStart = generator.RangeStartAt(DateTimeOffset.FromUnixTimeMilliseconds(maximumTimestamp));
        SnowflakeValue maximumEnd = generator.RangeEndAt(DateTimeOffset.FromUnixTimeMilliseconds(maximumTimestamp));

        Assert.That(minimumStart.ToUInt64(), Is.EqualTo(0));
        Assert.That(minimumEnd.ToUInt64(), Is.EqualTo(4194303));
        Assert.That(maximumStart.ToUInt64(), Is.EqualTo(SnowflakeLimits.MaxValue - 4194303));
        Assert.That(maximumEnd.ToUInt64(), Is.EqualTo(SnowflakeLimits.MaxValue));
        AssertIdentifierException(() => generator.RangeStartAtUnixMilliseconds(999), IdentifierErrorCode.TimestampOverflow, "Snowflake timestamp is before the configured epoch");
        AssertIdentifierException(() => generator.RangeEndAtUnixMilliseconds(maximumTimestamp + 1), IdentifierErrorCode.TimestampOverflow, "Snowflake timestamp exceeds the configured 41-bit range");
        AssertIdentifierException(() => generator.RangeStartAt(DateTimeOffset.FromUnixTimeMilliseconds(999)), IdentifierErrorCode.TimestampOverflow, "Snowflake timestamp is before the configured epoch");
        AssertIdentifierException(() => generator.RangeEndAt(DateTimeOffset.FromUnixTimeMilliseconds(maximumTimestamp + 1)), IdentifierErrorCode.TimestampOverflow, "Snowflake timestamp exceeds the configured 41-bit range");
    }

    [Test]
    public void DecodeAndConstruction_AcceptFull63BitRangeAndRejectReservedHighBit()
    {
        ISnowflakeGenerator generator = new Snowflake().DefaultGenerator();
        SnowflakeValue zero = generator.Decode(0);
        SnowflakeValue maximum = generator.Decode(SnowflakeLimits.MaxValue);
        SnowflakeValue directMaximum = new SnowflakeValue(SnowflakeLimits.MaxValue);

        Assert.That(zero.ToUInt64(), Is.EqualTo(0));
        Assert.That(maximum.ToUInt64(), Is.EqualTo(SnowflakeLimits.MaxValue));
        Assert.That(directMaximum.ToDecimalString(), Is.EqualTo("9223372036854775807"));

        ArgumentOutOfRangeException decoded = Assert.Throws<ArgumentOutOfRangeException>(() => generator.Decode(SnowflakeLimits.MaxValue + 1));
        ArgumentOutOfRangeException direct = Assert.Throws<ArgumentOutOfRangeException>(() => new SnowflakeValue(SnowflakeLimits.MaxValue + 1));

        Assert.That(decoded.ParamName, Is.EqualTo("value"));
        Assert.That(direct.ParamName, Is.EqualTo("value"));
    }

    [Test]
    public void ValueTimestamp_ReportsSignedAndDateTimeRangeOverflowsWithoutWrapping()
    {
        SnowflakeGenerator signedGenerator = new SnowflakeGenerator(new SnowflakeSettings(long.MaxValue, 0, 10, 12, SnowflakeClockRollbackBehavior.FailFast));
        SnowflakeValue signedOverflow = signedGenerator.Decode(1UL << 22);
        SnowflakeGenerator dateGenerator = new SnowflakeGenerator(new SnowflakeSettings(UnixTimeTools.MaxUnixMilliseconds + 1, 0, 10, 12, SnowflakeClockRollbackBehavior.FailFast));
        SnowflakeValue dateOverflow = dateGenerator.Decode(0);

        IdentifierTimestampResult signedTimestamp = signedOverflow.Timestamp();
        IdentifierTimestampResult dateTimestamp = dateOverflow.Timestamp();

        AssertTimestampFailure(signedTimestamp, IdentifierErrorCode.TimestampOverflow, "decoded Snowflake timestamp exceeds signed 64-bit range");
        AssertTimestampFailure(dateTimestamp, IdentifierErrorCode.TimestampOverflow, "unix milliseconds is outside DateTimeOffset range");
        AssertIdentifierException(() => signedOverflow.UnixMilliseconds(), IdentifierErrorCode.TimestampOverflow, "decoded Snowflake timestamp exceeds signed 64-bit range");
        AssertIdentifierException(() => signedOverflow.Time(), IdentifierErrorCode.TimestampOverflow, "decoded Snowflake timestamp exceeds signed 64-bit range");
        AssertIdentifierException(() => dateOverflow.Time(), IdentifierErrorCode.TimestampOverflow, "unix milliseconds is outside DateTimeOffset range");
    }

    [Test]
    public void GeneratorAndValueSettingsCopies_AreIndependentFromEverySourceAndReturnedCopy()
    {
        SnowflakeSettings source = new SnowflakeSettings(1000, 42, 10, 12, SnowflakeClockRollbackBehavior.Wait);
        SnowflakeGenerator generator = new SnowflakeGenerator(source);
        source.EpochUnixMilliseconds = 2000;
        source.MachineId = 1;
        source.ClockRollbackBehavior = SnowflakeClockRollbackBehavior.FailFast;

        SnowflakeSettings generatorCopy = generator.ToSettings();
        generatorCopy.EpochUnixMilliseconds = 3000;
        generatorCopy.MachineId = 2;
        SnowflakeValue value = generator.NextAtUnixMillisecondsWithSequence(124456, 7);
        SnowflakeSettings valueCopy = value.ToSettings();
        valueCopy.EpochUnixMilliseconds = 4000;
        valueCopy.MachineId = 3;

        Assert.That(generator.ToSettings().EpochUnixMilliseconds, Is.EqualTo(1000));
        Assert.That(generator.ToSettings().MachineId, Is.EqualTo(42));
        Assert.That(generator.ToSettings().ClockRollbackBehavior, Is.EqualTo(SnowflakeClockRollbackBehavior.Wait));
        Assert.That(value.UnixMilliseconds(), Is.EqualTo(124456));
        Assert.That(value.MachineId(), Is.EqualTo(42));
        Assert.That(value.ToSettings().EpochUnixMilliseconds, Is.EqualTo(1000));
        Assert.That(value.ToSettings().MachineId, Is.EqualTo(42));
    }

    [Test]
    public void ExtremeEpochs_PackValidEndpointsWithoutArithmeticOverflow()
    {
        SnowflakeGenerator minimum = new SnowflakeGenerator(new SnowflakeSettings(long.MinValue, 0, 10, 12, SnowflakeClockRollbackBehavior.FailFast));
        SnowflakeGenerator maximum = new SnowflakeGenerator(new SnowflakeSettings(long.MaxValue, 0, 10, 12, SnowflakeClockRollbackBehavior.FailFast));
        long maximumOffset = new Snowflake().MaxTimestampOffsetMilliseconds();

        SnowflakeValue minimumStart = minimum.NextAtUnixMillisecondsWithSequence(long.MinValue, 0);
        SnowflakeValue minimumEnd = minimum.NextAtUnixMillisecondsWithSequence(long.MinValue + maximumOffset, 4095);
        SnowflakeValue maximumStart = maximum.NextAtUnixMillisecondsWithSequence(long.MaxValue, 0);

        Assert.That(minimumStart.TimestampMilliseconds(), Is.EqualTo(0));
        Assert.That(minimumEnd.TimestampMilliseconds(), Is.EqualTo(maximumOffset));
        Assert.That(maximumStart.TimestampMilliseconds(), Is.EqualTo(0));
        AssertIdentifierException(() => minimum.NextAtUnixMillisecondsWithSequence(long.MaxValue, 0), IdentifierErrorCode.TimestampOverflow, "Snowflake timestamp exceeds the configured 41-bit range");
        AssertIdentifierException(() => maximum.NextAtUnixMillisecondsWithSequence(long.MaxValue - 1, 0), IdentifierErrorCode.TimestampOverflow, "Snowflake timestamp is before the configured epoch");
    }

    static void AssertParseSuccess(SnowflakeParseResult result, ulong value, string normalizedText)
    {
        Assert.That(result.Ok, Is.True);
        Assert.That(result.Value, Is.Not.Null);
        Assert.That(result.Value.ToUInt64(), Is.EqualTo(value));
        Assert.That(result.NormalizedText, Is.EqualTo(normalizedText));
        Assert.That(result.Error, Is.EqualTo(""));
        Assert.That(result.Code, Is.EqualTo(IdentifierErrorCode.None));
    }

    static void AssertParseFailure(SnowflakeParseResult result, IdentifierErrorCode code, string error)
    {
        Assert.That(result.Ok, Is.False);
        Assert.That(result.Value, Is.Null);
        Assert.That(result.NormalizedText, Is.EqualTo(""));
        Assert.That(result.Error, Is.EqualTo(error));
        Assert.That(result.Code, Is.EqualTo(code));
    }

    static void AssertValidationFailure(IdentifierValidationResult result, IdentifierErrorCode code, string error)
    {
        Assert.That(result.Ok, Is.False);
        Assert.That(result.Kind, Is.EqualTo(IdentifierKind.Unknown));
        Assert.That(result.NormalizedText, Is.EqualTo(""));
        Assert.That(result.Error, Is.EqualTo(error));
        Assert.That(result.Code, Is.EqualTo(code));
    }

    static void AssertTimestampFailure(IdentifierTimestampResult result, IdentifierErrorCode code, string error)
    {
        Assert.That(result.Ok, Is.False);
        Assert.That(result.Kind, Is.EqualTo(IdentifierKind.Unknown));
        Assert.That(result.UnixMilliseconds, Is.EqualTo(0));
        Assert.That(result.UnixSeconds, Is.EqualTo(0));
        Assert.That(result.Time, Is.EqualTo(DateTimeOffset.UnixEpoch));
        Assert.That(result.Error, Is.EqualTo(error));
        Assert.That(result.Code, Is.EqualTo(code));
    }

    static void AssertIdentifierException(TestDelegate action, IdentifierErrorCode code, string message)
    {
        IdentifierException exception = Assert.Throws<IdentifierException>(action);
        Assert.That(exception.Code, Is.EqualTo(code));
        Assert.That(exception.Message, Is.EqualTo(message));
    }

    class FixedSnowflakeClock : ISnowflakeClock
    {
        long Value;

        public FixedSnowflakeClock(long value)
        {
            Value = value;
        }

        public long UnixMilliseconds()
        {
            return Value;
        }

        public void WaitOneMillisecond()
        {
        }
    }
}
