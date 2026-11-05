using NUnit.Framework;

namespace uuid.Tests;

[TestFixture]
public class SnowflakeGeneratorTests
{
    [Test]
    public void Pack_CombinesTimestampMachineAndSequenceFields()
    {
        SnowflakeSettings settings = new SnowflakeSettings(1000, 42, 10, 12, SnowflakeClockRollbackBehavior.FailFast);

        ulong raw = SnowflakeGenerator.Pack(124456, 42, 7, settings);

        Assert.That(raw, Is.EqualTo((123456UL << 22) | (42UL << 12) | 7UL));
    }

    [Test]
    public void Pack_UsesFull41BitTimestampWithoutSettingSignBit()
    {
        SnowflakeSettings settings = new SnowflakeSettings(0, 1023, 10, 12, SnowflakeClockRollbackBehavior.FailFast);

        ulong raw = SnowflakeGenerator.Pack(settings.MaxTimestampOffsetMilliseconds(), 1023, 4095, settings);
        SnowflakeValue value = SnowflakeValue.Create(raw, settings);

        Assert.That(raw, Is.EqualTo(SnowflakeLimits.MaxValue));
        Assert.That(value.TimestampMilliseconds(), Is.EqualTo(settings.MaxTimestampOffsetMilliseconds()));
        Assert.That(value.MachineId(), Is.EqualTo(1023));
        Assert.That(value.Sequence(), Is.EqualTo(4095));
    }

    [Test]
    public void NextAtUnixMillisecondsWithSequence_UsesConfiguredMachineIdAndSequence()
    {
        SnowflakeSettings settings = new SnowflakeSettings(1000, 42, 10, 12, SnowflakeClockRollbackBehavior.FailFast);
        SnowflakeGenerator generator = new SnowflakeGenerator(settings);

        SnowflakeValue value = generator.NextAtUnixMillisecondsWithSequence(124456, 7);

        Assert.That(value.TimestampMilliseconds(), Is.EqualTo(123456));
        Assert.That(value.UnixMilliseconds(), Is.EqualTo(124456));
        Assert.That(value.MachineId(), Is.EqualTo(42));
        Assert.That(value.Sequence(), Is.EqualTo(7));
    }

    [Test]
    public void NextAtWithSequence_UsesDateTimeOffsetTimestamp()
    {
        SnowflakeSettings settings = new SnowflakeSettings(1000, 42, 10, 12, SnowflakeClockRollbackBehavior.FailFast);
        SnowflakeGenerator generator = new SnowflakeGenerator(settings);
        DateTimeOffset time = DateTimeOffset.FromUnixTimeMilliseconds(124456);

        SnowflakeValue value = generator.NextAtWithSequence(time, 7);

        Assert.That(value.UnixMilliseconds(), Is.EqualTo(124456));
        Assert.That(value.Time(), Is.EqualTo(time));
    }

    [Test]
    public void Generator_CopiesSettings()
    {
        SnowflakeSettings settings = new SnowflakeSettings(1000, 42, 10, 12, SnowflakeClockRollbackBehavior.FailFast);
        SnowflakeGenerator generator = new SnowflakeGenerator(settings);

        settings.EpochUnixMilliseconds = 5000;
        settings.MachineId = 1;
        SnowflakeValue value = generator.NextAtUnixMillisecondsWithSequence(124456, 7);

        Assert.That(value.UnixMilliseconds(), Is.EqualTo(124456));
        Assert.That(value.MachineId(), Is.EqualTo(42));
    }

    [Test]
    public void Pack_RejectsInvalidTimestampMachineAndSequence()
    {
        SnowflakeSettings settings = new SnowflakeSettings(1000, 42, 10, 12, SnowflakeClockRollbackBehavior.FailFast);

        Assert.That(() => SnowflakeGenerator.Pack(999, 42, 0, settings), Throws.TypeOf<ArgumentOutOfRangeException>());
        Assert.That(() => SnowflakeGenerator.Pack(1000 + settings.MaxTimestampOffsetMilliseconds() + 1, 42, 0, settings), Throws.TypeOf<ArgumentOutOfRangeException>());
        Assert.That(() => SnowflakeGenerator.Pack(1000, -1, 0, settings), Throws.TypeOf<ArgumentOutOfRangeException>());
        Assert.That(() => SnowflakeGenerator.Pack(1000, 1024, 0, settings), Throws.TypeOf<ArgumentOutOfRangeException>());
        Assert.That(() => SnowflakeGenerator.Pack(1000, 42, -1, settings), Throws.TypeOf<ArgumentOutOfRangeException>());
        Assert.That(() => SnowflakeGenerator.Pack(1000, 42, 4096, settings), Throws.TypeOf<ArgumentOutOfRangeException>());
        Assert.That(() => SnowflakeGenerator.Pack(1000, 42, 0, null), Throws.TypeOf<ArgumentNullException>());
    }

    [Test]
    public void Pack_HandlesExtremeCustomEpochsWithoutArithmeticOverflow()
    {
        SnowflakeSettings minimumEpoch = new SnowflakeSettings(long.MinValue, 0, 10, 12, SnowflakeClockRollbackBehavior.FailFast);
        long maximumOffset = minimumEpoch.MaxTimestampOffsetMilliseconds();

        Assert.That(SnowflakeGenerator.Pack(long.MinValue, 0, 0, minimumEpoch), Is.EqualTo(0));
        Assert.That(SnowflakeGenerator.Pack(long.MinValue + maximumOffset, 0, 0, minimumEpoch), Is.EqualTo((ulong)maximumOffset << 22));
        Assert.That(() => SnowflakeGenerator.Pack(long.MaxValue, 0, 0, minimumEpoch), Throws.TypeOf<ArgumentOutOfRangeException>());

        SnowflakeSettings maximumEpoch = new SnowflakeSettings(long.MaxValue, 0, 10, 12, SnowflakeClockRollbackBehavior.FailFast);
        Assert.That(SnowflakeGenerator.Pack(long.MaxValue, 0, 0, maximumEpoch), Is.EqualTo(0));
    }

    [Test]
    public void Constructor_RejectsInvalidSettings()
    {
        SnowflakeSettings invalid = SnowflakeSettings.TwitterDefault();
        invalid.MachineId = 1024;

        Assert.That(() => new SnowflakeGenerator(null), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => new SnowflakeGenerator(invalid), Throws.ArgumentException);
    }

    [Test]
    public void NextAtUnixMilliseconds_IncrementsSequenceWithinSameMillisecond()
    {
        SnowflakeSettings settings = new SnowflakeSettings(1000, 42, 10, 12, SnowflakeClockRollbackBehavior.FailFast);
        SnowflakeGenerator generator = new SnowflakeGenerator(settings);

        SnowflakeValue first = generator.NextAtUnixMilliseconds(124456);
        SnowflakeValue second = generator.NextAtUnixMilliseconds(124456);
        SnowflakeValue third = generator.NextAtUnixMilliseconds(124456);

        Assert.That(first.Sequence(), Is.EqualTo(0));
        Assert.That(second.Sequence(), Is.EqualTo(1));
        Assert.That(third.Sequence(), Is.EqualTo(2));
        Assert.That(first.MachineId(), Is.EqualTo(42));
        Assert.That(second.UnixMilliseconds(), Is.EqualTo(124456));
    }

    [Test]
    public void NextAtUnixMilliseconds_ResetsSequenceWhenTimestampAdvances()
    {
        SnowflakeSettings settings = new SnowflakeSettings(1000, 42, 10, 12, SnowflakeClockRollbackBehavior.FailFast);
        SnowflakeGenerator generator = new SnowflakeGenerator(settings);

        SnowflakeValue first = generator.NextAtUnixMilliseconds(124456);
        SnowflakeValue second = generator.NextAtUnixMilliseconds(124457);

        Assert.That(first.Sequence(), Is.EqualTo(0));
        Assert.That(second.Sequence(), Is.EqualTo(0));
        Assert.That(second.UnixMilliseconds(), Is.EqualTo(124457));
        Assert.That(second.ToUInt64(), Is.GreaterThan(first.ToUInt64()));
    }

    [Test]
    public void NextAtUnixMilliseconds_DetectsSequenceExhaustion()
    {
        SnowflakeSettings settings = new SnowflakeSettings(1000, 42, 22, 0, SnowflakeClockRollbackBehavior.FailFast);
        SnowflakeGenerator generator = new SnowflakeGenerator(settings);

        SnowflakeValue first = generator.NextAtUnixMilliseconds(124456);

        Assert.That(first.Sequence(), Is.EqualTo(0));
        Assert.That(() => generator.NextAtUnixMilliseconds(124456), Throws.TypeOf<IdentifierException>());
    }

    [Test]
    public void NextAtUnixMilliseconds_AllowsMaxSequenceThenResetsOnNextMillisecond()
    {
        SnowflakeSettings settings = new SnowflakeSettings(1000, 7, 20, 2, SnowflakeClockRollbackBehavior.FailFast);
        SnowflakeGenerator generator = new SnowflakeGenerator(settings);

        SnowflakeValue first = generator.NextAtUnixMilliseconds(124456);
        SnowflakeValue second = generator.NextAtUnixMilliseconds(124456);
        SnowflakeValue third = generator.NextAtUnixMilliseconds(124456);
        SnowflakeValue fourth = generator.NextAtUnixMilliseconds(124456);
        SnowflakeValue nextMillisecond = generator.NextAtUnixMilliseconds(124457);

        Assert.That(first.Sequence(), Is.EqualTo(0));
        Assert.That(second.Sequence(), Is.EqualTo(1));
        Assert.That(third.Sequence(), Is.EqualTo(2));
        Assert.That(fourth.Sequence(), Is.EqualTo(3));
        Assert.That(nextMillisecond.Sequence(), Is.EqualTo(0));
        Assert.That(nextMillisecond.ToUInt64(), Is.GreaterThan(fourth.ToUInt64()));
    }

    [Test]
    public void NextAtUnixMilliseconds_RejectsPastMaxSequenceBeforeTimestampAdvances()
    {
        SnowflakeSettings settings = new SnowflakeSettings(1000, 7, 20, 2, SnowflakeClockRollbackBehavior.FailFast);
        SnowflakeGenerator generator = new SnowflakeGenerator(settings);

        generator.NextAtUnixMilliseconds(124456);
        generator.NextAtUnixMilliseconds(124456);
        generator.NextAtUnixMilliseconds(124456);
        generator.NextAtUnixMilliseconds(124456);

        Assert.That(() => generator.NextAtUnixMilliseconds(124456), Throws.TypeOf<IdentifierException>());
    }

    [Test]
    public void NextAtUnixMilliseconds_FailsFastOnClockRollback()
    {
        SnowflakeSettings settings = new SnowflakeSettings(1000, 42, 10, 12, SnowflakeClockRollbackBehavior.FailFast);
        SnowflakeGenerator generator = new SnowflakeGenerator(settings);

        generator.NextAtUnixMilliseconds(124456);

        Assert.That(() => generator.NextAtUnixMilliseconds(124455), Throws.TypeOf<IdentifierException>());
    }

    [Test]
    public void NextAtUnixMilliseconds_WaitBehaviorContinuesAtLastTimestampForSuppliedRollback()
    {
        SnowflakeSettings settings = new SnowflakeSettings(1000, 42, 10, 12, SnowflakeClockRollbackBehavior.Wait);
        SnowflakeGenerator generator = new SnowflakeGenerator(settings);

        SnowflakeValue first = generator.NextAtUnixMilliseconds(124456);
        SnowflakeValue rollback = generator.NextAtUnixMilliseconds(124455);

        Assert.That(first.Sequence(), Is.EqualTo(0));
        Assert.That(rollback.UnixMilliseconds(), Is.EqualTo(124456));
        Assert.That(rollback.Sequence(), Is.EqualTo(1));
    }

    [Test]
    public void NextAtUnixMilliseconds_WaitRollbackCanStillExhaustLastTimestampSequence()
    {
        SnowflakeSettings settings = new SnowflakeSettings(1000, 7, 20, 2, SnowflakeClockRollbackBehavior.Wait);
        SnowflakeGenerator generator = new SnowflakeGenerator(settings);

        generator.NextAtUnixMilliseconds(124456);
        generator.NextAtUnixMilliseconds(124456);
        generator.NextAtUnixMilliseconds(124456);
        generator.NextAtUnixMilliseconds(124456);

        Assert.That(() => generator.NextAtUnixMilliseconds(124455), Throws.TypeOf<IdentifierException>());
    }

    [Test]
    public void NextAt_AndNext_GenerateStatefulValues()
    {
        SnowflakeSettings settings = new SnowflakeSettings(1000, 42, 10, 12, SnowflakeClockRollbackBehavior.FailFast);
        SnowflakeGenerator generator = new SnowflakeGenerator(settings);
        DateTimeOffset time = DateTimeOffset.FromUnixTimeMilliseconds(124456);

        SnowflakeValue atTime = generator.NextAt(time);
        SnowflakeValue now = new Snowflake().DefaultGenerator().Next();

        Assert.That(atTime.UnixMilliseconds(), Is.EqualTo(124456));
        Assert.That(atTime.Sequence(), Is.EqualTo(0));
        Assert.That(now.Time(), Is.LessThanOrEqualTo(DateTimeOffset.UtcNow));
    }

    [Test]
    public void RangeStartAndEndAtUnixMilliseconds_BoundAllIdsForMillisecond()
    {
        SnowflakeSettings settings = new SnowflakeSettings(1000, 42, 10, 12, SnowflakeClockRollbackBehavior.FailFast);
        SnowflakeGenerator generator = new SnowflakeGenerator(settings);

        SnowflakeValue start = generator.RangeStartAtUnixMilliseconds(124456);
        SnowflakeValue middle = generator.NextAtUnixMillisecondsWithSequence(124456, 7);
        SnowflakeValue end = generator.RangeEndAtUnixMilliseconds(124456);

        Assert.That(start.UnixMilliseconds(), Is.EqualTo(124456));
        Assert.That(start.MachineId(), Is.EqualTo(0));
        Assert.That(start.Sequence(), Is.EqualTo(0));
        Assert.That(end.UnixMilliseconds(), Is.EqualTo(124456));
        Assert.That(end.MachineId(), Is.EqualTo(1023));
        Assert.That(end.Sequence(), Is.EqualTo(4095));
        Assert.That(middle.ToUInt64(), Is.GreaterThanOrEqualTo(start.ToUInt64()));
        Assert.That(middle.ToUInt64(), Is.LessThanOrEqualTo(end.ToUInt64()));
    }

    [Test]
    public void RangeStartAndEndAt_UseDateTimeOffsetTimestamp()
    {
        SnowflakeSettings settings = new SnowflakeSettings(1000, 42, 10, 12, SnowflakeClockRollbackBehavior.FailFast);
        SnowflakeGenerator generator = new SnowflakeGenerator(settings);
        DateTimeOffset time = DateTimeOffset.FromUnixTimeMilliseconds(124456);

        SnowflakeValue start = generator.RangeStartAt(time);
        SnowflakeValue end = generator.RangeEndAt(time);

        Assert.That(start.UnixMilliseconds(), Is.EqualTo(124456));
        Assert.That(end.UnixMilliseconds(), Is.EqualTo(124456));
        Assert.That(start.ToUInt64(), Is.EqualTo(SnowflakeGenerator.RangeStartAtUnixMilliseconds(124456, settings)));
        Assert.That(end.ToUInt64(), Is.EqualTo(SnowflakeGenerator.RangeEndAtUnixMilliseconds(124456, settings)));
    }

    [Test]
    public void RangeEndAtUnixMilliseconds_UsesConfiguredBitWidths()
    {
        SnowflakeSettings settings = new SnowflakeSettings(1000, 31, 5, 17, SnowflakeClockRollbackBehavior.Wait);
        SnowflakeGenerator generator = new SnowflakeGenerator(settings);

        SnowflakeValue end = generator.RangeEndAtUnixMilliseconds(124456);

        Assert.That(end.MachineId(), Is.EqualTo(31));
        Assert.That(end.Sequence(), Is.EqualTo(131071));
        Assert.That(end.ToUInt64(), Is.EqualTo((123456UL << 22) | (31UL << 17) | 131071UL));
    }

    [Test]
    public void RangeHelpers_RejectInvalidTimestamp()
    {
        SnowflakeSettings settings = new SnowflakeSettings(1000, 42, 10, 12, SnowflakeClockRollbackBehavior.FailFast);
        SnowflakeGenerator generator = new SnowflakeGenerator(settings);

        Assert.That(() => generator.RangeStartAtUnixMilliseconds(999), Throws.TypeOf<IdentifierException>());
        Assert.That(() => generator.RangeEndAtUnixMilliseconds(1000 + settings.MaxTimestampOffsetMilliseconds() + 1), Throws.TypeOf<IdentifierException>());
        Assert.That(() => SnowflakeGenerator.RangeStartAtUnixMilliseconds(999, settings), Throws.TypeOf<ArgumentOutOfRangeException>());
        Assert.That(() => SnowflakeGenerator.RangeEndAtUnixMilliseconds(1000 + settings.MaxTimestampOffsetMilliseconds() + 1, settings), Throws.TypeOf<ArgumentOutOfRangeException>());
    }

    [Test]
    public void Pack_SupportsLayoutsWithoutMachineOrSequenceBits()
    {
        SnowflakeSettings noMachineBits = new SnowflakeSettings(1000, 0, 0, 22, SnowflakeClockRollbackBehavior.FailFast);
        SnowflakeSettings noSequenceBits = new SnowflakeSettings(1000, 4194303, 22, 0, SnowflakeClockRollbackBehavior.FailFast);

        ulong noMachineRaw = SnowflakeGenerator.Pack(124456, 0, 12345, noMachineBits);
        ulong noSequenceRaw = SnowflakeGenerator.Pack(124456, 4194303, 0, noSequenceBits);

        Assert.That(SnowflakeValue.Create(noMachineRaw, noMachineBits).MachineId(), Is.EqualTo(0));
        Assert.That(SnowflakeValue.Create(noMachineRaw, noMachineBits).Sequence(), Is.EqualTo(12345));
        Assert.That(SnowflakeValue.Create(noSequenceRaw, noSequenceBits).MachineId(), Is.EqualTo(4194303));
        Assert.That(SnowflakeValue.Create(noSequenceRaw, noSequenceBits).Sequence(), Is.EqualTo(0));
        Assert.That(() => SnowflakeGenerator.Pack(124456, 1, 0, noMachineBits), Throws.TypeOf<ArgumentOutOfRangeException>());
        Assert.That(() => SnowflakeGenerator.Pack(124456, 0, 1, noSequenceBits), Throws.TypeOf<ArgumentOutOfRangeException>());
    }

    [Test]
    public void DecimalString_RoundTripsLargeSnowflakeValues()
    {
        SnowflakeSettings settings = new SnowflakeSettings(0, 1023, 10, 12, SnowflakeClockRollbackBehavior.FailFast);
        SnowflakeGenerator generator = new SnowflakeGenerator(settings);
        SnowflakeValue value = SnowflakeValue.Create(SnowflakeLimits.MaxValue, settings);

        string text = value.ToDecimalString();
        SnowflakeParseResult result = generator.Parse(text);

        Assert.That(text, Is.EqualTo("9223372036854775807"));
        Assert.That(result.Ok, Is.True);
        Assert.That(result.Value, Is.Not.Null);
        Assert.That(result.Value.ToUInt64(), Is.EqualTo(SnowflakeLimits.MaxValue));
        Assert.That(result.NormalizedText, Is.EqualTo(text));
    }
}
