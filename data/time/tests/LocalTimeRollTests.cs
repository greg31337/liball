using NUnit.Framework;

namespace time.Tests;

[TestFixture]
public class LocalTimeRollTests
{
    [TestCase(0L, 0)]
    [TestCase(1L, -1)]
    [TestCase(863999999999L, 1)]
    public void Create_StoresTicksSinceMidnightAndDayDelta(long ticksSinceMidnight, int dayDelta)
    {
        LocalTimeRoll roll = LocalTimeRoll.Create(ticksSinceMidnight, dayDelta);

        Assert.That(roll.GetTicksSinceMidnight(), Is.EqualTo(ticksSinceMidnight));
        Assert.That(roll.GetDayDelta(), Is.EqualTo(dayDelta));
    }

    [TestCase(-1L)]
    [TestCase(864000000000L)]
    public void Create_RejectsTicksOutsideSingleDay(long ticksSinceMidnight)
    {
        ArgumentOutOfRangeException exception = Assert.Throws<ArgumentOutOfRangeException>(() => LocalTimeRoll.Create(ticksSinceMidnight, 0))!;

        Assert.That(exception.ParamName, Is.EqualTo("ticksSinceMidnight"));
    }

    [Test]
    public void GetTime_ReturnsLocalTimeForTicksSinceMidnight()
    {
        LocalTimeRoll roll = LocalTimeRoll.Create(452967891234L, 1);
        LocalTime time = roll.GetTime();

        Assert.That(time.GetTicksSinceMidnight(), Is.EqualTo(roll.GetTicksSinceMidnight()));
        Assert.That(time.GetHour(), Is.EqualTo(12));
        Assert.That(time.GetMinute(), Is.EqualTo(34));
        Assert.That(time.GetSecond(), Is.EqualTo(56));
        Assert.That(time.GetMillisecond(), Is.EqualTo(789));
        Assert.That(time.GetMicrosecond(), Is.EqualTo(123));
        Assert.That(time.GetTickRemainder(), Is.EqualTo(4));
    }
}
