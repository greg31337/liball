using countrycodes;

namespace CountryCodes.Tests;

[TestFixture]
public sealed class CountryFlagEmojiTests
{
    [Test]
    public void AssignedAlpha2CodesReturnCanonicalRegionalIndicatorPairs()
    {
        ICountryCatalog catalog = new CountryCodeService();
        CountryFlagEmojiResult germany = catalog.GetCountryFlagEmoji("de");

        Assert.Multiple(() =>
        {
            Assert.That(germany.IsAvailable, Is.True);
            Assert.That(germany.CanonicalAlpha2, Is.EqualTo("DE"));
            Assert.That(germany.HasAssignmentStatus, Is.True);
            Assert.That(
                germany.AssignmentStatus,
                Is.EqualTo(CodeAssignmentStatus.Assigned));
            Assert.That(germany.FlagEmoji, Is.EqualTo("🇩🇪"));
        });
    }

    [Test]
    public void EveryCurrentCountryHasThePairDerivedFromItsAlpha2Code()
    {
        ICountryCatalog catalog = new CountryCodeService();
        Country[] countries = catalog.GetCountries();

        foreach (Country country in countries)
        {
            string expected = CreateExpectedEmoji(country.Alpha2);
            CountryFlagEmojiResult result =
                catalog.GetCountryFlagEmoji(country.Alpha2);

            Assert.Multiple(() =>
            {
                Assert.That(result.IsAvailable, Is.True, country.Alpha2);
                Assert.That(result.FlagEmoji, Is.EqualTo(expected), country.Alpha2);
                Assert.That(country.FlagEmoji, Is.EqualTo(expected), country.Alpha2);
                Assert.That(result.FlagEmoji, Has.Length.EqualTo(4), country.Alpha2);
            });
        }
    }

    [TestCase("UK", CodeAssignmentStatus.ExceptionallyReserved)]
    [TestCase("YU", CodeAssignmentStatus.TransitionallyReserved)]
    [TestCase("AN", CodeAssignmentStatus.FormerlyAssigned)]
    [TestCase("AA", CodeAssignmentStatus.UserAssigned)]
    [TestCase("QM", CodeAssignmentStatus.UserAssigned)]
    [TestCase("XA", CodeAssignmentStatus.UserAssigned)]
    [TestCase("ZZ", CodeAssignmentStatus.UserAssigned)]
    [TestCase("AB", CodeAssignmentStatus.Unassigned)]
    public void NonCurrentAlpha2CodesReturnExplicitUnavailableResults(
        string value,
        CodeAssignmentStatus expectedStatus)
    {
        ICountryCatalog catalog = new CountryCodeService();
        CountryFlagEmojiResult result = catalog.GetCountryFlagEmoji(value);

        Assert.Multiple(() =>
        {
            Assert.That(result.IsAvailable, Is.False);
            Assert.That(result.CanonicalAlpha2, Is.EqualTo(value));
            Assert.That(result.HasAssignmentStatus, Is.True);
            Assert.That(result.AssignmentStatus, Is.EqualTo(expectedStatus));
            Assert.That(result.FlagEmoji, Is.Null);
        });
    }

    [TestCase(null)]
    [TestCase("")]
    [TestCase("U")]
    [TestCase("USA")]
    [TestCase("U1")]
    [TestCase(" U")]
    [TestCase("ÜS")]
    public void MalformedOrNonAlpha2InputsReturnUnavailableWithoutAStatus(
        string? value)
    {
        ICountryCatalog catalog = new CountryCodeService();
        CountryFlagEmojiResult result = catalog.GetCountryFlagEmoji(value!);

        Assert.Multiple(() =>
        {
            Assert.That(result.IsAvailable, Is.False);
            Assert.That(result.CanonicalAlpha2, Is.Null);
            Assert.That(result.HasAssignmentStatus, Is.False);
            Assert.That(
                result.AssignmentStatus,
                Is.EqualTo(CodeAssignmentStatus.Unassigned));
            Assert.That(result.FlagEmoji, Is.Null);
        });
    }

    [Test]
    public void MapperDoesNotDeriveEmojiForANonAssignedOrMalformedRow()
    {
        GeneratedCountryRow reserved = new GeneratedCountryRow(
            "UK",
            "GBR",
            "826",
            826,
            GeneratedCodeAssignmentState.ExceptionallyReserved,
            "test",
            "UK");
        GeneratedCountryRow malformed = new GeneratedCountryRow(
            "U1",
            "BAD",
            "999",
            999,
            GeneratedCodeAssignmentState.Assigned,
            "test",
            "U1");

        Assert.Multiple(() =>
        {
            Assert.That(CountryMapper.MapCountry(reserved).FlagEmoji, Is.Empty);
            Assert.That(CountryMapper.MapCountry(malformed).FlagEmoji, Is.Empty);
            Assert.That(
                CountryFlagEmojiUtility.CreateRegionalIndicatorPair("usa"),
                Is.Empty);
        });
    }

    private static string CreateExpectedEmoji(string alpha2)
    {
        int first = 0x1F1E6 + alpha2[0] - 'A';
        int second = 0x1F1E6 + alpha2[1] - 'A';
        return char.ConvertFromUtf32(first) + char.ConvertFromUtf32(second);
    }
}
