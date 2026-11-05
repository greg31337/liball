using fake;

namespace fake.Tests;

[TestFixture]
public class RelativeDateTests
{
    [Test]
    public void RelativeDefaultsUseTheCapturedReferenceTime()
    {
        FakeOptions options = FakeOptions.Create(5801);
        DateTimeOffset reference = new DateTimeOffset(2034, 6, 7, 8, 9, 10, TimeSpan.FromHours(-7));
        options.ReferenceTime = reference;
        DateProvider date = Fake.Create(options).Date();
        options.ReferenceTime = reference.AddYears(20);

        int index = 0;
        while (index < 256)
        {
            DateTimeOffset past = date.Past();
            DateTimeOffset future = date.Future();
            DateTimeOffset recent = date.Recent();
            DateTimeOffset soon = date.Soon();

            Assert.That(past, Is.GreaterThanOrEqualTo(reference.AddDays(-365.0)));
            Assert.That(past, Is.LessThan(reference));
            Assert.That(future, Is.GreaterThan(reference));
            Assert.That(future, Is.LessThanOrEqualTo(reference.AddDays(365.0)));
            Assert.That(recent, Is.GreaterThanOrEqualTo(reference.AddDays(-1.0)));
            Assert.That(recent, Is.LessThanOrEqualTo(reference));
            Assert.That(soon, Is.GreaterThan(reference));
            Assert.That(soon, Is.LessThanOrEqualTo(reference.AddDays(1.0)));
            index++;
        }
    }

    [Test]
    public void OneTickWindowsHonorStrictAndInclusiveEndpoints()
    {
        FakeOptions options = FakeOptions.Create(5802);
        DateTimeOffset reference = new DateTimeOffset(2040, 1, 2, 3, 4, 5, TimeSpan.FromHours(5.5));
        options.ReferenceTime = reference;
        DateProvider date = Fake.Create(options).Date();
        TimeSpan tick = TimeSpan.FromTicks(1L);

        Assert.Multiple(() =>
        {
            Assert.That(date.PastFromMaximumAge(tick), Is.EqualTo(reference.AddTicks(-1L)));
            Assert.That(date.FutureFromMaximumLead(tick), Is.EqualTo(reference.AddTicks(1L)));
            Assert.That(date.SoonFromWindow(tick), Is.EqualTo(reference.AddTicks(1L)));
            Assert.That(date.RecentFromWindow(tick), Is.InRange(reference.AddTicks(-1L), reference));
        });
    }

    [Test]
    public void EqualSeedsReplayEveryRelativeDateApi()
    {
        FakeOptions firstOptions = FakeOptions.Create(5803);
        firstOptions.ReferenceTime = new DateTimeOffset(2035, 5, 6, 7, 8, 9, TimeSpan.FromHours(-4));
        FakeOptions secondOptions = firstOptions.Copy();
        DateProvider first = Fake.Create(firstOptions).Date();
        DateProvider second = Fake.Create(secondOptions).Date();
        DateTimeOffset minimum = firstOptions.ReferenceTime.AddYears(-10);
        DateTimeOffset maximum = firstOptions.ReferenceTime.AddYears(10);

        int index = 0;
        while (index < 128)
        {
            Assert.That(first.Anytime(), Is.EqualTo(second.Anytime()));
            Assert.That(first.Between(minimum, maximum), Is.EqualTo(second.Between(minimum, maximum)));
            Assert.That(first.Betweens(minimum, maximum, 7), Is.EqualTo(second.Betweens(minimum, maximum, 7)));
            Assert.That(first.PastFromMaximumAge(TimeSpan.FromHours(6.0)), Is.EqualTo(second.PastFromMaximumAge(TimeSpan.FromHours(6.0))));
            Assert.That(first.FutureFromMaximumLead(TimeSpan.FromHours(6.0)), Is.EqualTo(second.FutureFromMaximumLead(TimeSpan.FromHours(6.0))));
            Assert.That(first.RecentFromWindow(TimeSpan.FromMinutes(30.0)), Is.EqualTo(second.RecentFromWindow(TimeSpan.FromMinutes(30.0))));
            Assert.That(first.SoonFromWindow(TimeSpan.FromMinutes(30.0)), Is.EqualTo(second.SoonFromWindow(TimeSpan.FromMinutes(30.0))));
            index++;
        }
    }

    [Test]
    public void BetweenSupportsExclusiveAndInclusiveInstantRanges()
    {
        DateProvider date = Fake.Create(5804).Date();
        DateTimeOffset minimum = new DateTimeOffset(2024, 2, 29, 12, 34, 56, TimeSpan.FromHours(8.0));
        DateTimeOffset adjacent = minimum.AddTicks(1L);
        DateTimeOffset sameInstant = minimum.ToOffset(TimeSpan.FromHours(-7.0));

        DateTimeOffset exclusive = date.Between(minimum, adjacent);
        DateTimeOffset singleton = date.BetweenFromMinimumAndMaximumAndBoundary(minimum, sameInstant, DateBoundary.Inclusive);

        Assert.Multiple(() =>
        {
            Assert.That(exclusive, Is.EqualTo(minimum));
            Assert.That(exclusive.Offset, Is.EqualTo(TimeSpan.Zero));
            Assert.That(singleton, Is.EqualTo(minimum));
            Assert.That(singleton.Offset, Is.EqualTo(minimum.Offset));
        });
    }

    [Test]
    public void BetweensReturnsSortedBoundedValuesAndFreshArrays()
    {
        DateProvider date = Fake.Create(5805).Date();
        DateTimeOffset minimum = new DateTimeOffset(1990, 1, 1, 0, 0, 0, TimeSpan.FromHours(9.0));
        DateTimeOffset maximum = new DateTimeOffset(2050, 1, 1, 0, 0, 0, TimeSpan.FromHours(-8.0));

        DateTimeOffset[] values = date.Betweens(minimum, maximum, 512);
        DateTimeOffset[] emptyFirst = date.Betweens(minimum, maximum, 0);
        DateTimeOffset[] emptySecond = date.Betweens(minimum, maximum, 0);

        Assert.That(values.Length, Is.EqualTo(512));
        int index = 0;
        while (index < values.Length)
        {
            Assert.That(values[index], Is.GreaterThanOrEqualTo(minimum));
            Assert.That(values[index], Is.LessThan(maximum));
            if (index > 0)
            {
                Assert.That(values[index], Is.GreaterThanOrEqualTo(values[index - 1]));
            }
            index++;
        }
        Assert.That(emptyFirst, Is.Empty);
        Assert.That(emptySecond, Is.Empty);
        Assert.That(emptyFirst, Is.Not.SameAs(emptySecond));
    }

    [Test]
    public void InclusiveSingletonBetweensPreservesRepresentationWithoutEntropy()
    {
        DateProvider actual = Fake.Create(5806).Date();
        DateProvider expected = Fake.Create(5806).Date();
        DateTimeOffset value = new DateTimeOffset(2028, 3, 4, 5, 6, 7, TimeSpan.FromHours(5.5));
        DateTimeOffset sameInstant = value.ToOffset(TimeSpan.FromHours(-6.0));

        DateTimeOffset[] values = actual.BetweensFromMinimumAndMaximumAndCountAndBoundary(value, sameInstant, 8, DateBoundary.Inclusive);

        int index = 0;
        while (index < values.Length)
        {
            Assert.That(values[index], Is.EqualTo(value));
            Assert.That(values[index].Offset, Is.EqualTo(value.Offset));
            index++;
        }
        Assert.That(actual.Anytime(), Is.EqualTo(expected.Anytime()));
    }

    [Test]
    public void InvalidRelativeArgumentsFailBeforeEntropy()
    {
        FakeOptions options = FakeOptions.Create(5807);
        options.MaximumCount = 3;
        options.MaximumOutputSize = 2;
        DateProvider actual = Fake.Create(options).Date();
        DateProvider expected = Fake.Create(options).Date();
        DateTimeOffset minimum = new DateTimeOffset(2020, 1, 1, 0, 0, 0, TimeSpan.Zero);
        DateTimeOffset maximum = minimum.AddDays(1.0);

        Assert.Multiple(() =>
        {
            Assert.That(Assert.Throws<ArgumentOutOfRangeException>(() => actual.PastFromMaximumAge(TimeSpan.Zero)).ParamName, Is.EqualTo("maximumAge"));
            Assert.That(Assert.Throws<ArgumentOutOfRangeException>(() => actual.FutureFromMaximumLead(TimeSpan.FromTicks(-1L))).ParamName, Is.EqualTo("maximumLead"));
            Assert.That(Assert.Throws<ArgumentOutOfRangeException>(() => actual.RecentFromWindow(TimeSpan.Zero)).ParamName, Is.EqualTo("window"));
            Assert.That(Assert.Throws<ArgumentOutOfRangeException>(() => actual.SoonFromWindow(TimeSpan.FromDays(-1.0))).ParamName, Is.EqualTo("window"));
            Assert.That(Assert.Throws<ArgumentOutOfRangeException>(() => actual.Betweens(minimum, maximum, -1)).ParamName, Is.EqualTo("count"));
            Assert.That(Assert.Throws<ArgumentOutOfRangeException>(() => actual.Betweens(minimum, maximum, 4)).ParamName, Is.EqualTo("count"));
            Assert.That(Assert.Throws<ArgumentOutOfRangeException>(() => actual.Betweens(minimum, maximum, 3)).ParamName, Is.EqualTo("count"));
            Assert.That(Assert.Throws<ArgumentOutOfRangeException>(() => actual.Betweens(maximum, minimum, 0)).ParamName, Is.EqualTo("maximum"));
            Assert.That(Assert.Throws<ArgumentOutOfRangeException>(() => actual.BetweensFromMinimumAndMaximumAndCountAndBoundary(minimum, maximum, 0, (DateBoundary)99)).ParamName, Is.EqualTo("boundary"));
        });

        Assert.That(actual.Anytime(), Is.EqualTo(expected.Anytime()));
    }

    [Test]
    public void RuntimeExtremesClampWindowsWithoutTickOverflow()
    {
        FakeOptions pastOptions = FakeOptions.Create(5808);
        pastOptions.ReferenceTime = DateTimeOffset.MinValue.AddTicks(5L);
        DateProvider pastDate = Fake.Create(pastOptions).Date();
        FakeOptions futureOptions = FakeOptions.Create(5809);
        futureOptions.ReferenceTime = DateTimeOffset.MaxValue.AddTicks(-5L);
        DateProvider futureDate = Fake.Create(futureOptions).Date();

        int index = 0;
        while (index < 128)
        {
            Assert.That(pastDate.PastFromMaximumAge(TimeSpan.MaxValue), Is.InRange(DateTimeOffset.MinValue, pastOptions.ReferenceTime.AddTicks(-1L)));
            Assert.That(pastDate.RecentFromWindow(TimeSpan.MaxValue), Is.InRange(DateTimeOffset.MinValue, pastOptions.ReferenceTime));
            Assert.That(futureDate.FutureFromMaximumLead(TimeSpan.MaxValue), Is.InRange(futureOptions.ReferenceTime.AddTicks(1L), DateTimeOffset.MaxValue));
            Assert.That(futureDate.SoonFromWindow(TimeSpan.MaxValue), Is.InRange(futureOptions.ReferenceTime.AddTicks(1L), DateTimeOffset.MaxValue));
            index++;
        }

        FakeOptions minimumOptions = FakeOptions.Create(5810);
        minimumOptions.ReferenceTime = DateTimeOffset.MinValue;
        FakeOptions maximumOptions = FakeOptions.Create(5811);
        maximumOptions.ReferenceTime = DateTimeOffset.MaxValue;

        Assert.That(() => Fake.Create(minimumOptions).Date().Past(), Throws.TypeOf<FakeException>());
        Assert.That(() => Fake.Create(maximumOptions).Date().Future(), Throws.TypeOf<FakeException>());
        Assert.That(() => Fake.Create(maximumOptions).Date().Soon(), Throws.TypeOf<FakeException>());
    }

    [Test]
    public void DateProviderStreamRemainsIndependentFromOtherProviders()
    {
        Fake actual = Fake.Create(5812);
        Fake expected = Fake.Create(5812);

        actual.Identifier().Uuid4();
        actual.Identifier().Ulid();
        actual.Random().Long();

        Assert.That(actual.Date().Betweens(DateTimeOffset.MinValue, DateTimeOffset.MaxValue, 16),
            Is.EqualTo(expected.Date().Betweens(DateTimeOffset.MinValue, DateTimeOffset.MaxValue, 16)));
    }

    [Test]
    public void CryptographicModeSupportsEveryRelativeDateApi()
    {
        FakeOptions options = new FakeOptions();
        options.RandomMode = RandomMode.Cryptographic;
        options.ReferenceTime = new DateTimeOffset(2030, 1, 1, 0, 0, 0, TimeSpan.Zero);
        DateProvider date = Fake.Create(options).Date();
        DateTimeOffset minimum = options.ReferenceTime.AddYears(-1);
        DateTimeOffset maximum = options.ReferenceTime.AddYears(1);

        int index = 0;
        while (index < 128)
        {
            Assert.That(date.Anytime(), Is.InRange(DateTimeOffset.MinValue, DateTimeOffset.MaxValue));
            Assert.That(date.Between(minimum, maximum), Is.InRange(minimum, maximum.AddTicks(-1L)));
            Assert.That(date.Past(), Is.InRange(options.ReferenceTime.AddDays(-365.0), options.ReferenceTime.AddTicks(-1L)));
            Assert.That(date.Future(), Is.InRange(options.ReferenceTime.AddTicks(1L), options.ReferenceTime.AddDays(365.0)));
            Assert.That(date.Recent(), Is.InRange(options.ReferenceTime.AddDays(-1.0), options.ReferenceTime));
            Assert.That(date.Soon(), Is.InRange(options.ReferenceTime.AddTicks(1L), options.ReferenceTime.AddDays(1.0)));
            index++;
        }

        Assert.That(date.Betweens(minimum, maximum, 12).Length, Is.EqualTo(12));
    }
}
