using fake;

namespace fake.Tests;

[TestFixture]
public class CalendarDateTests
{
    [Test]
    public void AgeUsesCompletedYearsAtTheCapturedCalendarDate()
    {
        FakeOptions options = FakeOptions.Create(5901);
        options.ReferenceTime = new DateTimeOffset(2024, 6, 15, 23, 30, 0, TimeSpan.FromHours(-7));
        DateProvider date = Fake.Create(options).Date();

        Assert.Multiple(() =>
        {
            Assert.That(date.Age(new DateOnly(2000, 6, 15)), Is.EqualTo(24));
            Assert.That(date.Age(new DateOnly(2000, 6, 16)), Is.EqualTo(23));
            Assert.That(date.Age(new DateOnly(2024, 6, 15)), Is.Zero);
        });

        FakeOptions leapOptions = FakeOptions.Create(5902);
        leapOptions.ReferenceTime = new DateTimeOffset(2023, 2, 28, 12, 0, 0, TimeSpan.Zero);
        Assert.That(Fake.Create(leapOptions).Date().Age(new DateOnly(2000, 2, 29)), Is.EqualTo(22));
    }

    [Test]
    public void BirthDatesAndReportedAgesRemainCorrelated()
    {
        FakeOptions options = FakeOptions.Create(5903);
        DateTimeOffset capturedReference = new DateTimeOffset(2035, 3, 4, 5, 6, 7, TimeSpan.FromHours(9));
        options.ReferenceTime = capturedReference;
        DateProvider date = Fake.Create(options).Date();
        options.ReferenceTime = capturedReference.AddYears(30);

        int index = 0;
        while (index < 1024)
        {
            DateOnly exact = date.BirthDateFromAge(25);
            DateOnly ranged = date.BirthDateFromMinimumAgeAndMaximumAge(18, 80);
            DateOnly defaultValue = date.BirthDate();
            BirthDateAge correlated = date.BirthDateAndAge(30, 40);

            Assert.That(date.Age(exact), Is.EqualTo(25));
            Assert.That(date.Age(ranged), Is.InRange(18, 80));
            Assert.That(date.Age(defaultValue), Is.InRange(18, 80));
            Assert.That(correlated.Age, Is.InRange(30, 40));
            Assert.That(correlated.Age, Is.EqualTo(date.Age(correlated.BirthDate)));
            index++;
        }
    }

    [Test]
    public void BirthDateGenerationIncludesLeapDayInItsCorrectAgeBand()
    {
        FakeOptions options = FakeOptions.Create(5904);
        options.ReferenceTime = new DateTimeOffset(2023, 2, 28, 0, 0, 0, TimeSpan.Zero);
        DateProvider date = Fake.Create(options).Date();

        int index = 0;
        while (index < 2048)
        {
            DateOnly value = date.BirthDateFromAge(22);
            Assert.That(date.Age(value), Is.EqualTo(22));
            index++;
        }

        Assert.That(date.Age(new DateOnly(2000, 2, 29)), Is.EqualTo(22));
    }

    [Test]
    public void AnniversaryApisHandleReferencePositionRangesAndLeapDays()
    {
        FakeOptions options = FakeOptions.Create(5905);
        options.ReferenceTime = new DateTimeOffset(2030, 6, 15, 12, 0, 0, TimeSpan.Zero);
        DateProvider date = Fake.Create(options).Date();

        Assert.Multiple(() =>
        {
            Assert.That(date.Anniversary(new DateOnly(2000, 8, 20)), Is.EqualTo(new DateOnly(2030, 8, 20)));
            Assert.That(date.Anniversary(new DateOnly(2000, 1, 20)), Is.EqualTo(new DateOnly(2031, 1, 20)));
            Assert.That(date.Anniversary(new DateOnly(2000, 6, 15)), Is.EqualTo(new DateOnly(2030, 6, 15)));
            Assert.That(date.AnniversaryFromOriginalDateAndYears(new DateOnly(2020, 2, 29), 3), Is.EqualTo(new DateOnly(2023, 2, 28)));
        });

        int index = 0;
        while (index < 256)
        {
            DateOnly value = date.AnniversaryFromOriginalDateAndMinimumYearsAndMaximumYears(new DateOnly(2010, 9, 10), 10, 20);
            Assert.That(value.Year, Is.InRange(2020, 2030));
            Assert.That(value.Month, Is.EqualTo(9));
            Assert.That(value.Day, Is.EqualTo(10));
            index++;
        }
    }

    [Test]
    public void ExpiryDatesUseCapturedLeadRangesAndClampAtTheRuntimeMaximum()
    {
        FakeOptions options = FakeOptions.Create(5906);
        options.ReferenceTime = new DateTimeOffset(2030, 1, 2, 3, 4, 5, TimeSpan.FromHours(5.5));
        DateProvider date = Fake.Create(options).Date();

        int index = 0;
        while (index < 256)
        {
            DateTimeOffset defaultValue = date.ExpiryDate();
            DateTimeOffset custom = date.ExpiryDateFromMinimumLeadAndMaximumLead(TimeSpan.FromHours(2), TimeSpan.FromHours(6));
            Assert.That(defaultValue, Is.InRange(options.ReferenceTime.AddDays(1), options.ReferenceTime.AddDays(365)));
            Assert.That(custom, Is.InRange(options.ReferenceTime.AddHours(2), options.ReferenceTime.AddHours(6)));
            Assert.That(custom.Offset, Is.EqualTo(TimeSpan.Zero));
            index++;
        }

        FakeOptions maximumOptions = FakeOptions.Create(5907);
        maximumOptions.ReferenceTime = DateTimeOffset.MaxValue.AddTicks(-5L);
        DateTimeOffset clamped = Fake.Create(maximumOptions).Date().ExpiryDateFromMinimumLeadAndMaximumLead(
            TimeSpan.FromTicks(1L),
            TimeSpan.MaxValue);
        Assert.That(clamped, Is.InRange(maximumOptions.ReferenceTime.AddTicks(1L), DateTimeOffset.MaxValue));
    }

    [Test]
    public void BusinessAndWeekendGenerationSelectOnlyEligibleDays()
    {
        DateProvider date = Fake.Create(5908).Date();
        DateOnly monday = new DateOnly(2024, 1, 1);
        DateOnly nextMonday = monday.AddDays(7);
        HashSet<DayOfWeek> businessKinds = new HashSet<DayOfWeek>();
        HashSet<DayOfWeek> weekendKinds = new HashSet<DayOfWeek>();

        int index = 0;
        while (index < 512)
        {
            DateOnly business = date.BusinessDayFromMinimumAndMaximum(monday, nextMonday);
            DateOnly weekend = date.WeekendFromMinimumAndMaximum(monday, nextMonday);
            Assert.That(business.DayOfWeek, Is.Not.EqualTo(DayOfWeek.Saturday));
            Assert.That(business.DayOfWeek, Is.Not.EqualTo(DayOfWeek.Sunday));
            Assert.That(weekend.DayOfWeek, Is.EqualTo(DayOfWeek.Saturday).Or.EqualTo(DayOfWeek.Sunday));
            businessKinds.Add(business.DayOfWeek);
            weekendKinds.Add(weekend.DayOfWeek);
            index++;
        }

        Assert.That(businessKinds.Count, Is.EqualTo(5));
        Assert.That(weekendKinds, Is.EquivalentTo(new DayOfWeek[] { DayOfWeek.Saturday, DayOfWeek.Sunday }));
        Assert.That(date.BusinessDayFromMinimumAndMaximumAndBoundary(monday, monday, DateBoundary.Inclusive), Is.EqualTo(monday));
        Assert.That(date.WeekendFromMinimumAndMaximumAndBoundary(monday.AddDays(5), monday.AddDays(5), DateBoundary.Inclusive), Is.EqualTo(monday.AddDays(5)));
    }

    [Test]
    public void DefaultBusinessAndWeekendWindowsUseTheCapturedReferenceDate()
    {
        FakeOptions options = FakeOptions.Create(5909);
        DateOnly referenceDate = new DateOnly(2032, 5, 6);
        options.ReferenceTime = new DateTimeOffset(2032, 5, 6, 23, 0, 0, TimeSpan.FromHours(-8));
        DateProvider date = Fake.Create(options).Date();

        int index = 0;
        while (index < 128)
        {
            DateOnly business = date.BusinessDay();
            DateOnly weekend = date.Weekend();
            Assert.That(business, Is.InRange(referenceDate, referenceDate.AddDays(365)));
            Assert.That(weekend, Is.InRange(referenceDate, referenceDate.AddDays(365)));
            index++;
        }
    }

    [Test]
    public void FiscalPeriodsUseStartingYearLabelsAndInclusiveQuarterBounds()
    {
        FakeOptions options = FakeOptions.Create(5910);
        options.ReferenceTime = new DateTimeOffset(2030, 1, 15, 8, 0, 0, TimeSpan.Zero);
        DateProvider date = Fake.Create(options).Date();

        FiscalPeriod calendar = date.FiscalPeriod();
        FiscalPeriod aprilBased = date.FiscalPeriodFromFiscalYearStartMonth(4);
        FiscalPeriod requested = date.FiscalPeriodFromFiscalYearAndQuarterAndFiscalYearStartMonth(2024, 4, 10);
        FiscalPeriod runtimeLast = date.FiscalPeriodFromFiscalYearAndQuarterAndFiscalYearStartMonth(9999, 4, 1);

        Assert.Multiple(() =>
        {
            Assert.That(calendar.FiscalYear, Is.EqualTo(2030));
            Assert.That(calendar.Quarter, Is.EqualTo(1));
            Assert.That(calendar.Start, Is.EqualTo(new DateOnly(2030, 1, 1)));
            Assert.That(calendar.End, Is.EqualTo(new DateOnly(2030, 3, 31)));
            Assert.That(aprilBased.FiscalYear, Is.EqualTo(2029));
            Assert.That(aprilBased.Quarter, Is.EqualTo(4));
            Assert.That(aprilBased.Start, Is.EqualTo(new DateOnly(2030, 1, 1)));
            Assert.That(aprilBased.End, Is.EqualTo(new DateOnly(2030, 3, 31)));
            Assert.That(requested.Start, Is.EqualTo(new DateOnly(2025, 7, 1)));
            Assert.That(requested.End, Is.EqualTo(new DateOnly(2025, 9, 30)));
            Assert.That(runtimeLast.Start, Is.EqualTo(new DateOnly(9999, 10, 1)));
            Assert.That(runtimeLast.End, Is.EqualTo(DateOnly.MaxValue));
        });
    }

    [Test]
    public void OrderedIntervalsStayWithinBoundsAndHonorDurations()
    {
        DateProvider date = Fake.Create(5911).Date();
        DateTimeOffset minimum = new DateTimeOffset(2020, 1, 1, 0, 0, 0, TimeSpan.FromHours(10));
        DateTimeOffset maximum = new DateTimeOffset(2021, 1, 1, 0, 0, 0, TimeSpan.FromHours(-8));

        int index = 0;
        while (index < 512)
        {
            DateInterval unconstrained = date.OrderedInterval(minimum, maximum);
            DateInterval constrained = date.OrderedIntervalFromMinimumAndMaximumAndMinimumDurationAndMaximumDuration(
                minimum,
                maximum,
                TimeSpan.FromDays(2),
                TimeSpan.FromDays(5));
            Assert.That(unconstrained.Start, Is.GreaterThanOrEqualTo(minimum));
            Assert.That(unconstrained.End, Is.LessThanOrEqualTo(maximum));
            Assert.That(unconstrained.Start, Is.LessThanOrEqualTo(unconstrained.End));
            Assert.That(constrained.Start, Is.GreaterThanOrEqualTo(minimum));
            Assert.That(constrained.End, Is.LessThanOrEqualTo(maximum));
            Assert.That(constrained.End - constrained.Start, Is.InRange(TimeSpan.FromDays(2), TimeSpan.FromDays(5)));
            index++;
        }
    }

    [Test]
    public void OrderedSingletonPreservesItsOriginalOffsetWithoutEntropy()
    {
        DateProvider actual = Fake.Create(5912).Date();
        DateProvider expected = Fake.Create(5912).Date();
        DateTimeOffset value = new DateTimeOffset(2024, 5, 6, 7, 8, 9, TimeSpan.FromHours(5.5));
        DateTimeOffset sameInstant = value.ToOffset(TimeSpan.FromHours(-7));

        DateInterval interval = actual.OrderedInterval(value, sameInstant);
        DateInterval durationInterval = actual.OrderedIntervalFromMinimumAndMaximumAndMinimumDurationAndMaximumDuration(
            value,
            sameInstant,
            TimeSpan.Zero,
            TimeSpan.Zero);

        Assert.Multiple(() =>
        {
            Assert.That(interval.Start.Offset, Is.EqualTo(value.Offset));
            Assert.That(interval.End.Offset, Is.EqualTo(value.Offset));
            Assert.That(durationInterval.Start.Offset, Is.EqualTo(value.Offset));
            Assert.That(durationInterval.End.Offset, Is.EqualTo(value.Offset));
        });
        Assert.That(actual.Anytime(), Is.EqualTo(expected.Anytime()));
    }

    [Test]
    public void EqualSeedsReplayEveryRandomCalendarApi()
    {
        FakeOptions options = FakeOptions.Create(5913);
        options.ReferenceTime = new DateTimeOffset(2033, 4, 5, 6, 7, 8, TimeSpan.FromHours(-4));
        DateProvider first = Fake.Create(options).Date();
        DateProvider second = Fake.Create(options).Date();
        DateOnly dayMinimum = new DateOnly(2020, 1, 1);
        DateOnly dayMaximum = new DateOnly(2040, 1, 1);
        DateTimeOffset instantMinimum = options.ReferenceTime.AddYears(-2);
        DateTimeOffset instantMaximum = options.ReferenceTime.AddYears(2);

        int index = 0;
        while (index < 128)
        {
            Assert.That(first.BirthDateFromMinimumAgeAndMaximumAge(20, 70), Is.EqualTo(second.BirthDateFromMinimumAgeAndMaximumAge(20, 70)));
            Assert.That(first.AnniversaryFromOriginalDateAndMinimumYearsAndMaximumYears(dayMinimum, 2, 15), Is.EqualTo(second.AnniversaryFromOriginalDateAndMinimumYearsAndMaximumYears(dayMinimum, 2, 15)));
            Assert.That(first.ExpiryDateFromMinimumLeadAndMaximumLead(TimeSpan.FromDays(2), TimeSpan.FromDays(30)), Is.EqualTo(second.ExpiryDateFromMinimumLeadAndMaximumLead(TimeSpan.FromDays(2), TimeSpan.FromDays(30))));
            Assert.That(first.BusinessDayFromMinimumAndMaximum(dayMinimum, dayMaximum), Is.EqualTo(second.BusinessDayFromMinimumAndMaximum(dayMinimum, dayMaximum)));
            Assert.That(first.WeekendFromMinimumAndMaximum(dayMinimum, dayMaximum), Is.EqualTo(second.WeekendFromMinimumAndMaximum(dayMinimum, dayMaximum)));
            DateInterval firstInterval = first.OrderedInterval(instantMinimum, instantMaximum);
            DateInterval secondInterval = second.OrderedInterval(instantMinimum, instantMaximum);
            Assert.That(firstInterval.Start, Is.EqualTo(secondInterval.Start));
            Assert.That(firstInterval.End, Is.EqualTo(secondInterval.End));
            index++;
        }
    }

    [Test]
    public void InvalidCalendarArgumentsFailBeforeDateEntropy()
    {
        FakeOptions options = FakeOptions.Create(5914);
        options.ReferenceTime = new DateTimeOffset(2030, 1, 1, 0, 0, 0, TimeSpan.Zero);
        DateProvider actual = Fake.Create(options).Date();
        DateProvider expected = Fake.Create(options).Date();
        DateOnly monday = new DateOnly(2024, 1, 1);
        DateTimeOffset instant = options.ReferenceTime;

        Assert.Multiple(() =>
        {
            Assert.That(Assert.Throws<ArgumentOutOfRangeException>(() => actual.BirthDateFromAge(-1)).ParamName, Is.EqualTo("minimumAge"));
            Assert.That(Assert.Throws<ArgumentOutOfRangeException>(() => actual.BirthDateFromMinimumAgeAndMaximumAge(20, 19)).ParamName, Is.EqualTo("maximumAge"));
            Assert.That(Assert.Throws<ArgumentOutOfRangeException>(() => actual.Age(new DateOnly(2030, 1, 2))).ParamName, Is.EqualTo("birthDate"));
            Assert.That(Assert.Throws<ArgumentOutOfRangeException>(() => actual.Anniversary(new DateOnly(2031, 1, 1))).ParamName, Is.EqualTo("originalDate"));
            Assert.That(Assert.Throws<ArgumentOutOfRangeException>(() => actual.AnniversaryFromOriginalDateAndYears(monday, 0)).ParamName, Is.EqualTo("years"));
            Assert.That(Assert.Throws<ArgumentOutOfRangeException>(() => actual.ExpiryDateFromMinimumLeadAndMaximumLead(TimeSpan.Zero, TimeSpan.FromDays(1))).ParamName, Is.EqualTo("minimumLead"));
            Assert.That(Assert.Throws<ArgumentOutOfRangeException>(() => actual.ExpiryDateFromMinimumLeadAndMaximumLead(TimeSpan.FromDays(2), TimeSpan.FromDays(1))).ParamName, Is.EqualTo("maximumLead"));
            Assert.That(() => actual.BusinessDayFromMinimumAndMaximum(monday.AddDays(5), monday.AddDays(7)), Throws.TypeOf<FakeException>());
            Assert.That(() => actual.WeekendFromMinimumAndMaximum(monday, monday.AddDays(5)), Throws.TypeOf<FakeException>());
            Assert.That(Assert.Throws<ArgumentOutOfRangeException>(() => actual.BusinessDayFromMinimumAndMaximumAndBoundary(monday, monday, (DateBoundary)99)).ParamName, Is.EqualTo("boundary"));
            Assert.That(Assert.Throws<ArgumentOutOfRangeException>(() => actual.FiscalPeriodFromFiscalYearStartMonth(0)).ParamName, Is.EqualTo("fiscalYearStartMonth"));
            Assert.That(Assert.Throws<ArgumentOutOfRangeException>(() => actual.FiscalPeriodFromFiscalYearAndQuarterAndFiscalYearStartMonth(2024, 5, 1)).ParamName, Is.EqualTo("quarter"));
            Assert.That(Assert.Throws<ArgumentOutOfRangeException>(() => actual.OrderedInterval(instant, instant.AddTicks(-1L))).ParamName, Is.EqualTo("maximum"));
            Assert.That(Assert.Throws<ArgumentOutOfRangeException>(() => actual.OrderedIntervalFromMinimumAndMaximumAndMinimumDurationAndMaximumDuration(instant, instant.AddDays(1), TimeSpan.FromDays(2), TimeSpan.FromDays(3))).ParamName, Is.EqualTo("minimumDuration"));
        });

        Assert.That(actual.Anytime(), Is.EqualTo(expected.Anytime()));
    }

    [Test]
    public void RuntimeCalendarEdgesAreExplicitAndOverflowSafe()
    {
        FakeOptions minimumOptions = FakeOptions.Create(5915);
        minimumOptions.ReferenceTime = DateTimeOffset.MinValue;
        DateProvider minimumDate = Fake.Create(minimumOptions).Date();
        FakeOptions maximumOptions = FakeOptions.Create(5916);
        maximumOptions.ReferenceTime = DateTimeOffset.MaxValue;
        DateProvider maximumDate = Fake.Create(maximumOptions).Date();

        Assert.That(minimumDate.BirthDateFromAge(0), Is.EqualTo(DateOnly.MinValue));
        Assert.That(() => minimumDate.BirthDate(), Throws.TypeOf<ArgumentOutOfRangeException>());
        Assert.That(() => minimumDate.FiscalPeriodFromFiscalYearStartMonth(4), Throws.TypeOf<FakeException>());
        Assert.That(() => maximumDate.ExpiryDate(), Throws.TypeOf<FakeException>());
        Assert.That(() => maximumDate.Anniversary(new DateOnly(9000, 12, 30)), Throws.TypeOf<FakeException>());

        DateInterval full = maximumDate.OrderedIntervalFromMinimumAndMaximumAndMinimumDurationAndMaximumDuration(
            DateTimeOffset.MinValue,
            DateTimeOffset.MaxValue,
            TimeSpan.Zero,
            TimeSpan.MaxValue);
        Assert.That(full.Start, Is.GreaterThanOrEqualTo(DateTimeOffset.MinValue));
        Assert.That(full.End, Is.LessThanOrEqualTo(DateTimeOffset.MaxValue));
        Assert.That(full.Start, Is.LessThanOrEqualTo(full.End));
    }

    [Test]
    public void CalendarGenerationSupportsStreamIsolationAndCryptographicMode()
    {
        Fake actual = Fake.Create(5917);
        Fake expected = Fake.Create(5917);
        actual.Identifier().Uuid4();
        actual.Random().Long();
        Assert.That(actual.Date().BirthDateFromMinimumAgeAndMaximumAge(18, 80), Is.EqualTo(expected.Date().BirthDateFromMinimumAgeAndMaximumAge(18, 80)));

        FakeOptions options = new FakeOptions();
        options.RandomMode = RandomMode.Cryptographic;
        options.ReferenceTime = new DateTimeOffset(2030, 1, 1, 0, 0, 0, TimeSpan.Zero);
        DateProvider date = Fake.Create(options).Date();
        DateOnly business = date.BusinessDayFromMinimumAndMaximum(new DateOnly(2024, 1, 1), new DateOnly(2025, 1, 1));
        DateOnly weekend = date.WeekendFromMinimumAndMaximum(new DateOnly(2024, 1, 1), new DateOnly(2025, 1, 1));
        DateInterval interval = date.OrderedInterval(options.ReferenceTime, options.ReferenceTime.AddYears(1));

        Assert.That(date.Age(date.BirthDateFromAge(40)), Is.EqualTo(40));
        Assert.That(business.DayOfWeek, Is.Not.EqualTo(DayOfWeek.Saturday));
        Assert.That(business.DayOfWeek, Is.Not.EqualTo(DayOfWeek.Sunday));
        Assert.That(weekend.DayOfWeek, Is.EqualTo(DayOfWeek.Saturday).Or.EqualTo(DayOfWeek.Sunday));
        Assert.That(interval.Start, Is.LessThanOrEqualTo(interval.End));
        Assert.That(date.ExpiryDate(), Is.GreaterThan(options.ReferenceTime));
    }
}
