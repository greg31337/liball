using NUnit.Framework;

namespace uuid.Tests;

[TestFixture]
public class SnowflakeSettingsTests
{
    [Test]
    public void DefaultSettings_UseTwitterStyleLayout()
    {
        SnowflakeSettings settings = SnowflakeSettings.TwitterDefault();

        Assert.That(settings.EpochUnixMilliseconds, Is.EqualTo(1288834974657));
        Assert.That(settings.MachineId, Is.EqualTo(0));
        Assert.That(settings.MachineBitCount, Is.EqualTo(10));
        Assert.That(settings.SequenceBitCount, Is.EqualTo(12));
        Assert.That(settings.ClockRollbackBehavior, Is.EqualTo(SnowflakeClockRollbackBehavior.FailFast));
        Assert.That(settings.Validate().Ok, Is.True);
    }

    [Test]
    public void Constructor_StoresCustomValidSettings()
    {
        SnowflakeSettings settings = new SnowflakeSettings(1000, 31, 5, 17, SnowflakeClockRollbackBehavior.Wait);

        Assert.That(settings.EpochUnixMilliseconds, Is.EqualTo(1000));
        Assert.That(settings.MachineId, Is.EqualTo(31));
        Assert.That(settings.MachineBitCount, Is.EqualTo(5));
        Assert.That(settings.SequenceBitCount, Is.EqualTo(17));
        Assert.That(settings.ClockRollbackBehavior, Is.EqualTo(SnowflakeClockRollbackBehavior.Wait));
    }

    [Test]
    public void MaxHelpers_ReturnMaximumMachineAndSequenceValues()
    {
        SnowflakeSettings settings = new SnowflakeSettings(1000, 1023, 10, 12, SnowflakeClockRollbackBehavior.FailFast);

        Assert.That(settings.MaxMachineId(), Is.EqualTo(1023));
        Assert.That(settings.MaxSequence(), Is.EqualTo(4095));
        Assert.That(SnowflakeSettings.MaxValueForBitCount(0), Is.EqualTo(0));
        Assert.That(SnowflakeSettings.MaxValueForBitCount(22), Is.EqualTo(4194303));
    }

    [Test]
    public void Validate_RejectsInvalidBitCounts()
    {
        SnowflakeSettings negativeMachineBits = SnowflakeSettings.TwitterDefault();
        negativeMachineBits.MachineBitCount = -1;
        SnowflakeSettings highSequenceBits = SnowflakeSettings.TwitterDefault();
        highSequenceBits.SequenceBitCount = 23;
        SnowflakeSettings wrongTotal = SnowflakeSettings.TwitterDefault();
        wrongTotal.SequenceBitCount = 11;

        Assert.That(negativeMachineBits.Validate().Error, Is.EqualTo("machine bit count must be between 0 and 22"));
        Assert.That(highSequenceBits.Validate().Error, Is.EqualTo("sequence bit count must be between 0 and 22"));
        Assert.That(wrongTotal.Validate().Error, Is.EqualTo("machine and sequence bit counts must total 22"));
    }

    [Test]
    public void Validate_RejectsMachineIdOutsideConfiguredRange()
    {
        SnowflakeSettings negativeMachineId = SnowflakeSettings.TwitterDefault();
        negativeMachineId.MachineId = -1;
        SnowflakeSettings tooLargeMachineId = SnowflakeSettings.TwitterDefault();
        tooLargeMachineId.MachineId = 1024;

        Assert.That(negativeMachineId.Validate().Error, Is.EqualTo("machine ID is outside configured bit range"));
        Assert.That(tooLargeMachineId.Validate().Error, Is.EqualTo("machine ID is outside configured bit range"));
    }

    [Test]
    public void Validate_RejectsUnknownRollbackBehavior()
    {
        SnowflakeSettings settings = SnowflakeSettings.TwitterDefault();
        settings.ClockRollbackBehavior = (SnowflakeClockRollbackBehavior)99;

        Assert.That(settings.Validate().Error, Is.EqualTo("clock rollback behavior is not recognized"));
    }

    [Test]
    public void Constructor_ThrowsForInvalidSettings()
    {
        Assert.That(() => new SnowflakeSettings(1000, 32, 5, 17, SnowflakeClockRollbackBehavior.FailFast), Throws.ArgumentException);
        Assert.That(() => new SnowflakeSettings(1000, 0, 5, 16, SnowflakeClockRollbackBehavior.FailFast), Throws.ArgumentException);
    }
}
