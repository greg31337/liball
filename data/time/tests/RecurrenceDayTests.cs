using NUnit.Framework;

namespace time.Tests;

[TestFixture]
public class RecurrenceDayTests
{
    [Test]
    public void Create_StoresWeekdayWithoutOrdinal()
    {
        RecurrenceDay day = RecurrenceDay.Create(TimeConstants.IsoMonday);

        Assert.That(day.GetDayOfWeek(), Is.EqualTo(TimeConstants.IsoMonday));
        Assert.That(day.GetOrdinal(), Is.EqualTo(RecurrenceDay.NoOrdinal));
        Assert.That(day.HasOrdinal(), Is.False);
    }

    [TestCase(TimeConstants.IsoMonday, 1)]
    [TestCase(TimeConstants.IsoWednesday, 2)]
    [TestCase(TimeConstants.IsoFriday, -1)]
    [TestCase(TimeConstants.IsoSunday, -53)]
    [TestCase(TimeConstants.IsoSunday, 53)]
    public void Create_StoresWeekdayWithOrdinal(int dayOfWeek, int ordinal)
    {
        RecurrenceDay day = RecurrenceDay.Create(dayOfWeek, ordinal);

        Assert.That(day.GetDayOfWeek(), Is.EqualTo(dayOfWeek));
        Assert.That(day.GetOrdinal(), Is.EqualTo(ordinal));
        Assert.That(day.HasOrdinal(), Is.EqualTo(ordinal != RecurrenceDay.NoOrdinal));
    }

    [TestCase(0)]
    [TestCase(8)]
    public void Create_RejectsInvalidWeekday(int dayOfWeek)
    {
        ArgumentOutOfRangeException exception = Assert.Throws<ArgumentOutOfRangeException>(() => RecurrenceDay.Create(dayOfWeek))!;

        Assert.That(exception.ParamName, Is.EqualTo("dayOfWeek"));
    }

    [TestCase(-54)]
    [TestCase(54)]
    public void Create_RejectsInvalidOrdinal(int ordinal)
    {
        ArgumentOutOfRangeException exception = Assert.Throws<ArgumentOutOfRangeException>(() => RecurrenceDay.Create(TimeConstants.IsoMonday, ordinal))!;

        Assert.That(exception.ParamName, Is.EqualTo("ordinal"));
    }

    [Test]
    public void Constructor_StoresValues()
    {
        RecurrenceDay day = new RecurrenceDay(TimeConstants.IsoTuesday, -2);

        Assert.That(day.GetDayOfWeek(), Is.EqualTo(TimeConstants.IsoTuesday));
        Assert.That(day.GetOrdinal(), Is.EqualTo(-2));
    }

    [Test]
    public void EqualsValue_ComparesWeekdayAndOrdinal()
    {
        RecurrenceDay day = RecurrenceDay.Create(TimeConstants.IsoMonday, 1);

        Assert.That(day.EqualsValue(RecurrenceDay.Create(TimeConstants.IsoMonday, 1)), Is.True);
        Assert.That(day.EqualsValue(RecurrenceDay.Create(TimeConstants.IsoMonday, 2)), Is.False);
        Assert.That(day.EqualsValue(RecurrenceDay.Create(TimeConstants.IsoTuesday, 1)), Is.False);
        Assert.That(day.EqualsValue(null), Is.False);
    }
}
