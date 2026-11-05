using fake;

namespace fake.Tests;

[TestFixture]
public class TimeZoneTests
{
    [Test]
    public void BundledIdentifiersExposeTheCompleteZone1970SourceOrder()
    {
        DateProvider date = Fake.Create(6101).Date();

        string[] first = date.TimeZoneIds();
        string[] second = date.TimeZoneIds();

        Assert.That(first.Length, Is.EqualTo(312));
        Assert.That(first[0], Is.EqualTo("Europe/Andorra"));
        Assert.That(first[first.Length - 1], Is.EqualTo("Africa/Johannesburg"));
        Assert.That(new HashSet<string>(first).Count, Is.EqualTo(first.Length));
        Assert.That(second, Is.EqualTo(first));
        Assert.That(second, Is.Not.SameAs(first));
    }

    [Test]
    public void CountryFilteringIsCaseInsensitiveAndIncludesSharedZones()
    {
        DateProvider date = Fake.Create(6102).Date();

        string[] japan = date.TimeZoneIdsFromCountryCode("jp");
        string[] unitedStates = date.TimeZoneIdsFromCountryCode("US");
        string[] canada = date.TimeZoneIdsFromCountryCode("ca");

        Assert.That(japan, Is.EqualTo(new string[] { "Asia/Tokyo" }));
        Assert.That(unitedStates, Does.Contain("America/New_York"));
        Assert.That(unitedStates, Does.Contain("America/Los_Angeles"));
        Assert.That(unitedStates, Does.Contain("Pacific/Honolulu"));
        Assert.That(canada, Does.Contain("America/Toronto"));
        Assert.That(canada, Does.Contain("America/Whitehorse"));
    }

    [Test]
    public void SeededIdentifierSelectionReplaysGloballyAndByCountry()
    {
        DateProvider first = Fake.Create(6103).Date();
        DateProvider second = Fake.Create(6103).Date();
        HashSet<string> global = new HashSet<string>();
        HashSet<string> unitedStates = new HashSet<string>();

        int index = 0;
        while (index < 1024)
        {
            string firstGlobal = first.TimeZoneId();
            string firstCountry = first.TimeZoneIdFromCountryCode("us");
            Assert.That(firstGlobal, Is.EqualTo(second.TimeZoneId()));
            Assert.That(firstCountry, Is.EqualTo(second.TimeZoneIdFromCountryCode("US")));
            global.Add(firstGlobal);
            unitedStates.Add(firstCountry);
            index++;
        }

        Assert.That(global.Count, Is.GreaterThan(250));
        Assert.That(unitedStates.Count, Is.GreaterThan(10));
    }

    [Test]
    public void RuntimeMappingResolvesRepresentativeBundledIanaZones()
    {
        DateProvider date = Fake.Create(6104).Date();
        string[] identifiers = new string[]
        {
            "Africa/Abidjan",
            "America/New_York",
            "Asia/Kathmandu",
            "Australia/Lord_Howe",
            "Europe/London",
            "Pacific/Chatham"
        };

        int index = 0;
        while (index < identifiers.Length)
        {
            TimeZoneInfo runtime = date.TimeZoneFromIanaId(identifiers[index]);
            Assert.That(runtime, Is.Not.Null);
            Assert.That(runtime.Id, Is.Not.Empty);
            Assert.That(date.RuntimeTimeZoneId(identifiers[index]), Is.EqualTo(runtime.Id));
            index++;
        }
    }

    [Test]
    public void RuntimeOffsetsApplyDstAndFractionalHourRulesAtTheInstant()
    {
        DateProvider date = Fake.Create(6105).Date();
        DateTimeOffset winter = new DateTimeOffset(2030, 1, 15, 12, 0, 0, TimeSpan.Zero);
        DateTimeOffset summer = new DateTimeOffset(2030, 7, 15, 12, 0, 0, TimeSpan.Zero);

        Assert.Multiple(() =>
        {
            Assert.That(date.TimeZoneOffsetFromIanaIdAndInstant("Africa/Abidjan", winter), Is.EqualTo(TimeSpan.Zero));
            Assert.That(date.TimeZoneOffsetFromIanaIdAndInstant("America/New_York", winter), Is.EqualTo(TimeSpan.FromHours(-5)));
            Assert.That(date.TimeZoneOffsetFromIanaIdAndInstant("America/New_York", summer), Is.EqualTo(TimeSpan.FromHours(-4)));
            Assert.That(date.TimeZoneOffsetFromIanaIdAndInstant("Asia/Kathmandu", winter), Is.EqualTo(TimeSpan.FromMinutes(345)));
            Assert.That(date.TimeZoneOffsetFromIanaIdAndInstant("Australia/Lord_Howe", winter), Is.EqualTo(TimeSpan.FromHours(11)));
            Assert.That(date.TimeZoneOffsetFromIanaIdAndInstant("Australia/Lord_Howe", summer), Is.EqualTo(TimeSpan.FromMinutes(630)));
            Assert.That(date.TimeZoneOffsetFromIanaIdAndInstant("Pacific/Chatham", winter), Is.EqualTo(TimeSpan.FromMinutes(825)));
            Assert.That(date.TimeZoneOffsetFromIanaIdAndInstant("Pacific/Chatham", summer), Is.EqualTo(TimeSpan.FromMinutes(765)));
        });
    }

    [Test]
    public void InTimeZonePreservesTheInstantAndUsesTheRuntimeOffset()
    {
        DateProvider date = Fake.Create(6106).Date();
        DateTimeOffset instant = new DateTimeOffset(2032, 3, 4, 5, 6, 7, TimeSpan.FromHours(9));

        DateTimeOffset local = date.InTimeZone("America/New_York", instant);

        Assert.That(local.UtcTicks, Is.EqualTo(instant.UtcTicks));
        Assert.That(local.Offset, Is.EqualTo(date.TimeZoneOffsetFromIanaIdAndInstant("America/New_York", instant)));
    }

    [Test]
    public void DetailsCorrelateBundledMetadataWithRuntimeResults()
    {
        DateProvider date = Fake.Create(6107).Date();
        DateTimeOffset instant = new DateTimeOffset(2030, 7, 15, 12, 0, 0, TimeSpan.Zero);

        TimeZoneDetails first = date.TimeZoneDetailsFromIanaIdAndInstant("America/New_York", instant);
        first.CountryCodes[0] = "changed";
        TimeZoneDetails second = date.TimeZoneDetailsFromIanaIdAndInstant("America/New_York", instant);

        Assert.Multiple(() =>
        {
            Assert.That(second.IanaId, Is.EqualTo("America/New_York"));
            Assert.That(second.RuntimeId, Is.EqualTo(date.RuntimeTimeZoneId("America/New_York")));
            Assert.That(second.CountryCodes, Does.Contain("US"));
            Assert.That(second.Coordinates, Is.EqualTo("+404251-0740023"));
            Assert.That(second.Comment, Does.Contain("Eastern"));
            Assert.That(second.Instant, Is.EqualTo(instant));
            Assert.That(second.Offset, Is.EqualTo(TimeSpan.FromHours(-4)));
            Assert.That(second.LocalTime.UtcTicks, Is.EqualTo(instant.UtcTicks));
            Assert.That(second.LocalTime.Offset, Is.EqualTo(second.Offset));
            Assert.That(second.SupportsDaylightSavingTime, Is.True);
        });
    }

    [Test]
    public void CapturedReferenceControlsDefaultOffsetsAndDetails()
    {
        FakeOptions options = FakeOptions.Create(6108);
        DateTimeOffset reference = new DateTimeOffset(2030, 1, 15, 12, 0, 0, TimeSpan.FromHours(8));
        options.ReferenceTime = reference;
        DateProvider date = Fake.Create(options).Date();
        options.ReferenceTime = reference.AddMonths(6);

        Assert.That(date.TimeZoneOffsetFromIanaId("America/New_York"), Is.EqualTo(TimeSpan.FromHours(-5)));
        TimeZoneDetails details = date.TimeZoneDetailsFromIanaId("America/New_York");
        Assert.That(details.Instant, Is.EqualTo(reference));
        Assert.That(details.Offset, Is.EqualTo(TimeSpan.FromHours(-5)));
    }

    [Test]
    public void RandomRuntimeOperationsReplayWithEqualSeeds()
    {
        FakeOptions options = FakeOptions.Create(6109);
        options.ReferenceTime = new DateTimeOffset(2030, 5, 6, 7, 8, 9, TimeSpan.Zero);
        DateProvider first = Fake.Create(options).Date();
        DateProvider second = Fake.Create(options).Date();

        int index = 0;
        while (index < 256)
        {
            Assert.That(first.TimeZone().Id, Is.EqualTo(second.TimeZone().Id));
            Assert.That(first.TimeZoneOffset(), Is.EqualTo(second.TimeZoneOffset()));
            TimeZoneDetails firstDetails = first.TimeZoneDetails();
            TimeZoneDetails secondDetails = second.TimeZoneDetails();
            Assert.That(firstDetails.IanaId, Is.EqualTo(secondDetails.IanaId));
            Assert.That(firstDetails.Offset, Is.EqualTo(secondDetails.Offset));
            index++;
        }
    }

    [Test]
    public void InvalidIdentifiersCountriesAndLimitsFailBeforeDateEntropy()
    {
        FakeOptions options = FakeOptions.Create(6110);
        options.MaximumCount = 10;
        options.MaximumOutputSize = 10;
        DateProvider actual = Fake.Create(options).Date();
        DateProvider expected = Fake.Create(options).Date();

        Assert.Multiple(() =>
        {
            Assert.That(Assert.Throws<ArgumentNullException>(() => actual.TimeZoneFromIanaId(null)).ParamName, Is.EqualTo("ianaId"));
            Assert.That(Assert.Throws<ArgumentException>(() => actual.TimeZoneFromIanaId("america/new_york")).ParamName, Is.EqualTo("ianaId"));
            Assert.That(Assert.Throws<ArgumentException>(() => actual.TimeZoneIdFromCountryCode("USA")).ParamName, Is.EqualTo("countryCode"));
            Assert.That(Assert.Throws<ArgumentException>(() => actual.TimeZoneIdsFromCountryCode("ZZ")).ParamName, Is.EqualTo("countryCode"));
            Assert.That(Assert.Throws<ArgumentOutOfRangeException>(() => actual.TimeZoneIds()).ParamName, Is.EqualTo("timeZoneIds"));
        });

        Assert.That(actual.TimeZoneId(), Is.EqualTo(expected.TimeZoneId()));
    }

    [Test]
    public void TimeZoneGenerationIsIndependentAndSupportsCryptographicMode()
    {
        Fake actual = Fake.Create(6111);
        Fake expected = Fake.Create(6111);
        actual.Identifier().Uuid4();
        actual.Random().Long();
        Assert.That(actual.Date().TimeZoneId(), Is.EqualTo(expected.Date().TimeZoneId()));

        FakeOptions options = new FakeOptions();
        options.RandomMode = RandomMode.Cryptographic;
        options.ReferenceTime = new DateTimeOffset(2030, 1, 1, 0, 0, 0, TimeSpan.Zero);
        DateProvider date = Fake.Create(options).Date();
        HashSet<string> bundled = new HashSet<string>(date.TimeZoneIds());

        int index = 0;
        while (index < 128)
        {
            string identifier = date.TimeZoneId();
            TimeZoneDetails details = date.TimeZoneDetails();
            Assert.That(bundled, Does.Contain(identifier));
            Assert.That(bundled, Does.Contain(details.IanaId));
            Assert.That(details.Offset, Is.InRange(TimeSpan.FromHours(-14), TimeSpan.FromHours(14)));
            index++;
        }
    }
}
