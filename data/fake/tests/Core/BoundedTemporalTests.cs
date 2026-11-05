using fake;

namespace fake.Tests;

[TestFixture]
public class BoundedTemporalTests
{
    [Test]
    public void EqualSeedsReplayEveryBoundedTemporalType()
    {
        DateProvider first = Fake.Create(5701).Date();
        DateProvider second = Fake.Create(5701).Date();
        DateTime dateMinimum = new DateTime(2000, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        DateTime dateMaximum = new DateTime(2030, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        DateTimeOffset offsetMinimum = new DateTimeOffset(2000, 1, 1, 0, 0, 0, TimeSpan.FromHours(-7));
        DateTimeOffset offsetMaximum = new DateTimeOffset(2030, 1, 1, 0, 0, 0, TimeSpan.FromHours(9));

        int index = 0;
        while (index < 256)
        {
            Assert.That(first.DateTime(dateMinimum, dateMaximum), Is.EqualTo(second.DateTime(dateMinimum, dateMaximum)));
            Assert.That(first.DateTimeOffset(offsetMinimum, offsetMaximum), Is.EqualTo(second.DateTimeOffset(offsetMinimum, offsetMaximum)));
            Assert.That(first.DateOnly(new DateOnly(2000, 1, 1), new DateOnly(2030, 1, 1)), Is.EqualTo(second.DateOnly(new DateOnly(2000, 1, 1), new DateOnly(2030, 1, 1))));
            Assert.That(first.TimeOnly(new TimeOnly(1, 2, 3), new TimeOnly(22, 0, 0)), Is.EqualTo(second.TimeOnly(new TimeOnly(1, 2, 3), new TimeOnly(22, 0, 0))));
            Assert.That(first.TimeSpan(System.TimeSpan.FromDays(-30), System.TimeSpan.FromDays(30)), Is.EqualTo(second.TimeSpan(System.TimeSpan.FromDays(-30), System.TimeSpan.FromDays(30))));
            index++;
        }
    }

    [Test]
    public void DefaultIntervalsExcludeTheMaximum()
    {
        DateProvider date = Fake.Create(5702).Date();
        DateTime dateMinimum = new DateTime(100L, DateTimeKind.Utc);
        DateTime dateMaximum = new DateTime(101L, DateTimeKind.Utc);
        DateTimeOffset offsetMinimum = new DateTimeOffset(100L, TimeSpan.Zero);
        DateTimeOffset offsetMaximum = new DateTimeOffset(101L, TimeSpan.Zero);
        DateOnly dayMinimum = new DateOnly(2020, 1, 1);
        DateOnly dayMaximum = dayMinimum.AddDays(1);
        TimeOnly timeMinimum = new TimeOnly(100L);
        TimeOnly timeMaximum = new TimeOnly(101L);
        System.TimeSpan spanMinimum = new System.TimeSpan(-1L);
        System.TimeSpan spanMaximum = System.TimeSpan.Zero;

        Assert.Multiple(() =>
        {
            Assert.That(date.DateTime(dateMinimum, dateMaximum), Is.EqualTo(dateMinimum));
            Assert.That(date.DateTimeOffset(offsetMinimum, offsetMaximum), Is.EqualTo(offsetMinimum));
            Assert.That(date.DateOnly(dayMinimum, dayMaximum), Is.EqualTo(dayMinimum));
            Assert.That(date.TimeOnly(timeMinimum, timeMaximum), Is.EqualTo(timeMinimum));
            Assert.That(date.TimeSpan(spanMinimum, spanMaximum), Is.EqualTo(spanMinimum));
        });
    }

    [Test]
    public void InclusiveSingletonsReturnTheExactValueWithoutEntropy()
    {
        DateProvider actual = Fake.Create(5703).Date();
        DateProvider expected = Fake.Create(5703).Date();
        DateTime dateValue = new DateTime(2024, 2, 29, 12, 34, 56, DateTimeKind.Local);
        DateTimeOffset offsetValue = new DateTimeOffset(2024, 2, 29, 12, 34, 56, TimeSpan.FromHours(5.5));
        DateTimeOffset sameInstant = offsetValue.ToOffset(TimeSpan.FromHours(-7));
        DateOnly dayValue = new DateOnly(2024, 2, 29);
        TimeOnly timeValue = new TimeOnly(23, 59, 59).Add(TimeSpan.FromTicks(9999999L));
        System.TimeSpan spanValue = System.TimeSpan.MinValue;

        Assert.Multiple(() =>
        {
            Assert.That(actual.DateTimeFromMinimumAndMaximumAndBoundary(dateValue, dateValue, DateBoundary.Inclusive), Is.EqualTo(dateValue));
            Assert.That(actual.DateTimeOffsetFromMinimumAndMaximumAndBoundary(offsetValue, sameInstant, DateBoundary.Inclusive), Is.EqualTo(offsetValue));
            Assert.That(actual.DateTimeOffsetFromMinimumAndMaximumAndBoundary(offsetValue, sameInstant, DateBoundary.Inclusive).Offset, Is.EqualTo(offsetValue.Offset));
            Assert.That(actual.DateOnlyFromMinimumAndMaximumAndBoundary(dayValue, dayValue, DateBoundary.Inclusive), Is.EqualTo(dayValue));
            Assert.That(actual.TimeOnlyFromMinimumAndMaximumAndBoundary(timeValue, timeValue, DateBoundary.Inclusive), Is.EqualTo(timeValue));
            Assert.That(actual.TimeSpanFromMinimumAndMaximumAndBoundary(spanValue, spanValue, DateBoundary.Inclusive), Is.EqualTo(spanValue));
        });

        Assert.That(actual.RandomValue.Long(), Is.EqualTo(expected.RandomValue.Long()));
    }

    [Test]
    public void FullRuntimeRangesGenerateWithoutTickOverflow()
    {
        DateProvider date = Fake.Create(5704).Date();
        DateTime utcMinimum = DateTime.SpecifyKind(DateTime.MinValue, DateTimeKind.Utc);
        DateTime utcMaximum = DateTime.SpecifyKind(DateTime.MaxValue, DateTimeKind.Utc);

        int index = 0;
        while (index < 1024)
        {
            DateTime dateTime = date.DateTimeFromMinimumAndMaximumAndBoundary(utcMinimum, utcMaximum, DateBoundary.Inclusive);
            DateTimeOffset dateTimeOffset = date.DateTimeOffsetFromMinimumAndMaximumAndBoundary(DateTimeOffset.MinValue, DateTimeOffset.MaxValue, DateBoundary.Inclusive);
            DateOnly dateOnly = date.DateOnlyFromMinimumAndMaximumAndBoundary(DateOnly.MinValue, DateOnly.MaxValue, DateBoundary.Inclusive);
            TimeOnly timeOnly = date.TimeOnlyFromMinimumAndMaximumAndBoundary(TimeOnly.MinValue, TimeOnly.MaxValue, DateBoundary.Inclusive);
            System.TimeSpan timeSpan = date.TimeSpanFromMinimumAndMaximumAndBoundary(System.TimeSpan.MinValue, System.TimeSpan.MaxValue, DateBoundary.Inclusive);

            Assert.That(dateTime, Is.InRange(utcMinimum, utcMaximum));
            Assert.That(dateTime.Kind, Is.EqualTo(DateTimeKind.Utc));
            Assert.That(dateTimeOffset, Is.InRange(DateTimeOffset.MinValue, DateTimeOffset.MaxValue));
            Assert.That(dateOnly, Is.InRange(DateOnly.MinValue, DateOnly.MaxValue));
            Assert.That(timeOnly, Is.InRange(TimeOnly.MinValue, TimeOnly.MaxValue));
            Assert.That(timeSpan, Is.InRange(System.TimeSpan.MinValue, System.TimeSpan.MaxValue));
            index++;
        }
    }

    [Test]
    public void DateTimePreservesTheSharedKind()
    {
        DateProvider date = Fake.Create(5705).Date();
        DateTime localMinimum = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Local);
        DateTime localMaximum = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Local);
        DateTime unspecifiedMinimum = DateTime.SpecifyKind(localMinimum, DateTimeKind.Unspecified);
        DateTime unspecifiedMaximum = DateTime.SpecifyKind(localMaximum, DateTimeKind.Unspecified);

        Assert.Multiple(() =>
        {
            Assert.That(date.DateTime(localMinimum, localMaximum).Kind, Is.EqualTo(DateTimeKind.Local));
            Assert.That(date.DateTime(unspecifiedMinimum, unspecifiedMaximum).Kind, Is.EqualTo(DateTimeKind.Unspecified));
        });
    }

    [Test]
    public void DateTimeOffsetUsesInstantBoundsAndSafeUtcResults()
    {
        DateProvider date = Fake.Create(5706).Date();
        DateTimeOffset minimum = new DateTimeOffset(2024, 1, 1, 0, 0, 0, TimeSpan.FromHours(12));
        DateTimeOffset maximum = new DateTimeOffset(2024, 1, 2, 0, 0, 0, TimeSpan.FromHours(-10));

        int index = 0;
        while (index < 256)
        {
            DateTimeOffset value = date.DateTimeOffset(minimum, maximum);
            Assert.That(value, Is.GreaterThanOrEqualTo(minimum));
            Assert.That(value, Is.LessThan(maximum));
            Assert.That(value.Offset, Is.EqualTo(TimeSpan.Zero));
            index++;
        }
    }

    [Test]
    public void InclusiveAdjacentRangesCanReturnEitherBoundary()
    {
        DateProvider date = Fake.Create(5707).Date();
        DateOnly minimum = new DateOnly(2024, 1, 1);
        DateOnly maximum = minimum.AddDays(1);
        HashSet<DateOnly> values = new HashSet<DateOnly>();

        int index = 0;
        while (index < 128)
        {
            values.Add(date.DateOnlyFromMinimumAndMaximumAndBoundary(minimum, maximum, DateBoundary.Inclusive));
            index++;
        }

        Assert.That(values, Is.EquivalentTo(new DateOnly[] { minimum, maximum }));
    }

    [Test]
    public void EmptyReversedAndMismatchedRangesFailBeforeEntropy()
    {
        DateProvider actual = Fake.Create(5708).Date();
        DateProvider expected = Fake.Create(5708).Date();
        DateTime utc = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        DateTime local = DateTime.SpecifyKind(utc, DateTimeKind.Local);
        DateTimeOffset offset = new DateTimeOffset(utc);
        DateOnly day = new DateOnly(2024, 1, 1);
        TimeOnly time = new TimeOnly(12, 0);
        System.TimeSpan span = System.TimeSpan.Zero;

        Assert.Multiple(() =>
        {
            Assert.That(Assert.Throws<ArgumentOutOfRangeException>(() => actual.DateTime(utc, utc)).ParamName, Is.EqualTo("maximum"));
            Assert.That(Assert.Throws<ArgumentOutOfRangeException>(() => actual.DateTimeOffset(offset, offset)).ParamName, Is.EqualTo("maximum"));
            Assert.That(Assert.Throws<ArgumentOutOfRangeException>(() => actual.DateOnly(day, day)).ParamName, Is.EqualTo("maximum"));
            Assert.That(Assert.Throws<ArgumentOutOfRangeException>(() => actual.TimeOnly(time, time)).ParamName, Is.EqualTo("maximum"));
            Assert.That(Assert.Throws<ArgumentOutOfRangeException>(() => actual.TimeSpan(span, span)).ParamName, Is.EqualTo("maximum"));
            Assert.That(Assert.Throws<ArgumentOutOfRangeException>(() => actual.DateOnlyFromMinimumAndMaximumAndBoundary(day, day.AddDays(-1), DateBoundary.Inclusive)).ParamName, Is.EqualTo("maximum"));
            Assert.That(Assert.Throws<ArgumentException>(() => actual.DateTimeFromMinimumAndMaximumAndBoundary(utc, local, DateBoundary.Inclusive)).ParamName, Is.EqualTo("maximum"));
            Assert.That(Assert.Throws<ArgumentOutOfRangeException>(() => actual.TimeSpanFromMinimumAndMaximumAndBoundary(span, span, (DateBoundary)99)).ParamName, Is.EqualTo("boundary"));
        });

        Assert.That(actual.RandomValue.Long(), Is.EqualTo(expected.RandomValue.Long()));
    }

    [Test]
    public void DateProviderStreamIsIndependentFromOtherProviders()
    {
        Fake actual = Fake.Create(5709);
        Fake expected = Fake.Create(5709);

        actual.Identifier().Uuid4();
        actual.Identifier().Ulid();
        actual.Random().Long();

        Assert.That(
            actual.Date().TimeSpanFromMinimumAndMaximumAndBoundary(System.TimeSpan.MinValue, System.TimeSpan.MaxValue, DateBoundary.Inclusive),
            Is.EqualTo(expected.Date().TimeSpanFromMinimumAndMaximumAndBoundary(System.TimeSpan.MinValue, System.TimeSpan.MaxValue, DateBoundary.Inclusive)));
    }

    [Test]
    public void CryptographicModeSupportsEveryBoundedTemporalType()
    {
        FakeOptions options = new FakeOptions();
        options.RandomMode = RandomMode.Cryptographic;
        DateProvider date = Fake.Create(options).Date();

        int index = 0;
        while (index < 128)
        {
            Assert.That(date.DateTimeFromMinimumAndMaximumAndBoundary(DateTime.MinValue, DateTime.MaxValue, DateBoundary.Inclusive), Is.InRange(DateTime.MinValue, DateTime.MaxValue));
            Assert.That(date.DateTimeOffsetFromMinimumAndMaximumAndBoundary(DateTimeOffset.MinValue, DateTimeOffset.MaxValue, DateBoundary.Inclusive), Is.InRange(DateTimeOffset.MinValue, DateTimeOffset.MaxValue));
            Assert.That(date.DateOnlyFromMinimumAndMaximumAndBoundary(DateOnly.MinValue, DateOnly.MaxValue, DateBoundary.Inclusive), Is.InRange(DateOnly.MinValue, DateOnly.MaxValue));
            Assert.That(date.TimeOnlyFromMinimumAndMaximumAndBoundary(TimeOnly.MinValue, TimeOnly.MaxValue, DateBoundary.Inclusive), Is.InRange(TimeOnly.MinValue, TimeOnly.MaxValue));
            Assert.That(date.TimeSpanFromMinimumAndMaximumAndBoundary(System.TimeSpan.MinValue, System.TimeSpan.MaxValue, DateBoundary.Inclusive), Is.InRange(System.TimeSpan.MinValue, System.TimeSpan.MaxValue));
            index++;
        }
    }
}
