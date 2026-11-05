using NUnit.Framework;

namespace uuid.Tests;

[TestFixture]
public class SnowflakeClockRollbackTests
{
    [Test]
    public void Next_WaitsUntilRealTimeClockCatchesUp()
    {
        SnowflakeGenerator generator = WaitGenerator(12);
        ScriptedSnowflakeClock clock = new ScriptedSnowflakeClock(new long[] { 1000, 999, 999, 1000 });
        generator.Clock = clock;

        SnowflakeValue first = generator.Next();
        SnowflakeValue second = generator.Next();

        Assert.That(clock.WaitCount, Is.EqualTo(2));
        Assert.That(clock.ReadCount, Is.EqualTo(4));
        Assert.That(first.UnixMilliseconds(), Is.EqualTo(1000));
        Assert.That(first.Sequence(), Is.EqualTo(0));
        Assert.That(second.UnixMilliseconds(), Is.EqualTo(1000));
        Assert.That(second.Sequence(), Is.EqualTo(1));
        Assert.That(second.ToUInt64(), Is.GreaterThan(first.ToUInt64()));
    }

    [Test]
    public void Next_ResetsSequenceWhenClockCatchesUpPastLastTimestamp()
    {
        SnowflakeGenerator generator = WaitGenerator(12);
        ScriptedSnowflakeClock clock = new ScriptedSnowflakeClock(new long[] { 1000, 998, 1001 });
        generator.Clock = clock;

        SnowflakeValue first = generator.Next();
        SnowflakeValue second = generator.Next();

        Assert.That(clock.WaitCount, Is.EqualTo(1));
        Assert.That(first.UnixMilliseconds(), Is.EqualTo(1000));
        Assert.That(second.UnixMilliseconds(), Is.EqualTo(1001));
        Assert.That(second.Sequence(), Is.EqualTo(0));
        Assert.That(second.ToUInt64(), Is.GreaterThan(first.ToUInt64()));
    }

    [Test]
    public void Next_FailFastRollbackDoesNotWait()
    {
        SnowflakeSettings settings = new SnowflakeSettings(0, 7, 10, 12, SnowflakeClockRollbackBehavior.FailFast);
        SnowflakeGenerator generator = new SnowflakeGenerator(settings);
        ScriptedSnowflakeClock clock = new ScriptedSnowflakeClock(new long[] { 1000, 999 });
        generator.Clock = clock;
        generator.Next();

        IdentifierException exception = Assert.Throws<IdentifierException>(() => generator.Next());

        Assert.That(exception.Code, Is.EqualTo(IdentifierErrorCode.ClockRollback));
        Assert.That(clock.WaitCount, Is.EqualTo(0));
        Assert.That(clock.ReadCount, Is.EqualTo(2));
    }

    [Test]
    public void ExplicitTimestampGeneration_RemainsDeterministicAndDoesNotReadClock()
    {
        SnowflakeGenerator generator = WaitGenerator(12);
        ScriptedSnowflakeClock clock = new ScriptedSnowflakeClock(new long[] { 5000 });
        generator.Clock = clock;

        SnowflakeValue first = generator.NextAtUnixMilliseconds(1000);
        SnowflakeValue rollback = generator.NextAtUnixMilliseconds(999);

        Assert.That(clock.ReadCount, Is.EqualTo(0));
        Assert.That(clock.WaitCount, Is.EqualTo(0));
        Assert.That(first.UnixMilliseconds(), Is.EqualTo(1000));
        Assert.That(first.Sequence(), Is.EqualTo(0));
        Assert.That(rollback.UnixMilliseconds(), Is.EqualTo(1000));
        Assert.That(rollback.Sequence(), Is.EqualTo(1));
        Assert.That(rollback.ToUInt64(), Is.GreaterThan(first.ToUInt64()));
    }

    [Test]
    public void Next_DoesNotEmitDuplicateWhenCatchUpExhaustsSequence()
    {
        SnowflakeGenerator generator = WaitGenerator(0);
        ScriptedSnowflakeClock clock = new ScriptedSnowflakeClock(new long[] { 1000, 999, 1000 });
        generator.Clock = clock;
        SnowflakeValue first = generator.Next();

        IdentifierException exception = Assert.Throws<IdentifierException>(() => generator.Next());

        Assert.That(first.Sequence(), Is.EqualTo(0));
        Assert.That(exception.Code, Is.EqualTo(IdentifierErrorCode.SequenceExhaustion));
        Assert.That(clock.WaitCount, Is.EqualTo(1));
    }

    static SnowflakeGenerator WaitGenerator(int sequenceBitCount)
    {
        int machineBitCount = SnowflakeSettings.AvailableWorkerAndSequenceBitCount - sequenceBitCount;
        SnowflakeSettings settings = new SnowflakeSettings(0, 0, machineBitCount, sequenceBitCount, SnowflakeClockRollbackBehavior.Wait);
        return new SnowflakeGenerator(settings);
    }

    class ScriptedSnowflakeClock : ISnowflakeClock
    {
        long[] Values;
        int Index;
        public int ReadCount;
        public int WaitCount;

        public ScriptedSnowflakeClock(long[] values)
        {
            Values = values;
        }

        public long UnixMilliseconds()
        {
            long value = Values[Index];
            Index = Index + 1;
            ReadCount = ReadCount + 1;
            return value;
        }

        public void WaitOneMillisecond()
        {
            WaitCount = WaitCount + 1;
        }
    }
}
