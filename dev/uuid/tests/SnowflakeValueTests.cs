using NUnit.Framework;

namespace uuid.Tests;

[TestFixture]
public class SnowflakeValueTests
{
    [Test]
    public void ToUInt64AndDecimalString_ReturnStoredValue()
    {
        SnowflakeValue value = new SnowflakeValue(123456789UL);

        Assert.That(value.ToUInt64(), Is.EqualTo(123456789UL));
        Assert.That(value.ToDecimalString(), Is.EqualTo("123456789"));
    }

    [Test]
    public void DecodedHelpers_UseConfiguredBitLayout()
    {
        SnowflakeSettings settings = new SnowflakeSettings(1000, 42, 10, 12, SnowflakeClockRollbackBehavior.FailFast);
        ulong raw = (123456UL << 22) | (42UL << 12) | 7UL;

        SnowflakeValue value = SnowflakeValue.Create(raw, settings);

        Assert.That(value.TimestampMilliseconds(), Is.EqualTo(123456));
        Assert.That(value.UnixMilliseconds(), Is.EqualTo(124456));
        Assert.That(value.Time(), Is.EqualTo(DateTimeOffset.FromUnixTimeMilliseconds(124456)));
        Assert.That(value.MachineId(), Is.EqualTo(42));
        Assert.That(value.Sequence(), Is.EqualTo(7));
    }

    [Test]
    public void DecodedHelpers_SupportCustomMachineAndSequenceBitCounts()
    {
        SnowflakeSettings settings = new SnowflakeSettings(5000, 31, 5, 17, SnowflakeClockRollbackBehavior.Wait);
        ulong raw = (99UL << 22) | (31UL << 17) | 12345UL;

        SnowflakeValue value = SnowflakeValue.Create(raw, settings);

        Assert.That(value.TimestampMilliseconds(), Is.EqualTo(99));
        Assert.That(value.UnixMilliseconds(), Is.EqualTo(5099));
        Assert.That(value.MachineId(), Is.EqualTo(31));
        Assert.That(value.Sequence(), Is.EqualTo(12345));
    }

    [Test]
    public void Constructor_CopiesSettings()
    {
        SnowflakeSettings settings = new SnowflakeSettings(1000, 42, 10, 12, SnowflakeClockRollbackBehavior.FailFast);
        ulong raw = (123456UL << 22) | (42UL << 12) | 7UL;

        SnowflakeValue value = SnowflakeValue.Create(raw, settings);
        settings.EpochUnixMilliseconds = 5000;
        settings.MachineBitCount = 5;
        settings.SequenceBitCount = 17;

        Assert.That(value.UnixMilliseconds(), Is.EqualTo(124456));
        Assert.That(value.MachineId(), Is.EqualTo(42));
        Assert.That(value.Sequence(), Is.EqualTo(7));
    }

    [Test]
    public void ToSettings_ReturnsIndependentSettingsCopy()
    {
        SnowflakeSettings settings = new SnowflakeSettings(1000, 42, 10, 12, SnowflakeClockRollbackBehavior.FailFast);
        SnowflakeValue value = SnowflakeValue.Create(0, settings);

        SnowflakeSettings copy = value.ToSettings();
        copy.EpochUnixMilliseconds = 9999;

        Assert.That(value.ToSettings().EpochUnixMilliseconds, Is.EqualTo(1000));
    }

    [Test]
    public void Constructor_RejectsInvalidInput()
    {
        SnowflakeSettings invalid = SnowflakeSettings.TwitterDefault();
        invalid.MachineId = 1024;

        Assert.That(() => new SnowflakeValue(SnowflakeLimits.MaxValue + 1), Throws.TypeOf<ArgumentOutOfRangeException>());
        Assert.That(() => SnowflakeValue.Create(0, null), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => SnowflakeValue.Create(0, invalid), Throws.ArgumentException);
    }

    [Test]
    public void UnixMilliseconds_RejectsDecodedValuesBeyondSignedRange()
    {
        SnowflakeSettings settings = new SnowflakeSettings(long.MaxValue, 0, 10, 12, SnowflakeClockRollbackBehavior.FailFast);
        SnowflakeValue value = SnowflakeValue.Create(1UL << 22, settings);

        Assert.That(() => value.UnixMilliseconds(), Throws.TypeOf<IdentifierException>());
    }
}
