using countrycodes;

namespace CountryCodes.Tests;

[TestFixture]
public sealed class CountryCatalogEnumerationTests
{
    [Test]
    public void GetCountriesReturnsCompleteStableAlpha2OrderedSnapshots()
    {
        ICountryCatalog catalog = new CountryCodeService();
        Country[] countries = catalog.GetCountries();

        Assert.That(countries, Has.Length.EqualTo(249));
        Assert.That(countries[0].Alpha2, Is.EqualTo("AD"));
        Assert.That(countries[countries.Length - 1].Alpha2, Is.EqualTo("ZW"));
        int index = 0;
        string previous = string.Empty;
        while (index < countries.Length)
        {
            Country country = countries[index];
            Assert.That(string.CompareOrdinal(previous, country.Alpha2), Is.LessThan(0));
            Assert.That(country.CanonicalCode, Is.EqualTo(country.Alpha2));
            Assert.That(country.Alpha3, Has.Length.EqualTo(3));
            Assert.That(country.NumericText, Has.Length.EqualTo(3));
            Assert.That(country.AssignmentStatus, Is.EqualTo(CodeAssignmentStatus.Assigned));
            Assert.That(country.ShortName, Is.Not.Empty);
            Assert.That(country.FlagEmoji, Is.Not.Empty);
            previous = country.Alpha2;
            index += 1;
        }

        Country germany = FindCountry(countries, "DE");
        Assert.That(germany.ShortName, Is.EqualTo("Germany"));
        Assert.That(germany.OfficialName, Is.EqualTo("Federal Republic of Germany"));
        Assert.That(germany.RegionM49Codes, Is.EqualTo(new[] { "001", "150", "155" }));
    }

    [Test]
    public void AssignmentFilteringReturnsOnlyMatchingCurrentCountries()
    {
        ICountryCatalog catalog = new CountryCodeService();
        Country[] assigned =
            catalog.GetCountriesByAssignmentStatus(CodeAssignmentStatus.Assigned);

        Assert.That(assigned, Has.Length.EqualTo(249));
        Assert.That(assigned[0].Alpha2, Is.EqualTo("AD"));
        Assert.That(assigned[assigned.Length - 1].Alpha2, Is.EqualTo("ZW"));
        int index = 0;
        while (index < assigned.Length)
        {
            Assert.That(
                assigned[index].AssignmentStatus,
                Is.EqualTo(CodeAssignmentStatus.Assigned));
            index += 1;
        }

        Assert.That(
            catalog.GetCountriesByAssignmentStatus(CodeAssignmentStatus.ExceptionallyReserved),
            Is.Empty);
        Assert.That(
            catalog.GetCountriesByAssignmentStatus(CodeAssignmentStatus.TransitionallyReserved),
            Is.Empty);
        Assert.That(
            catalog.GetCountriesByAssignmentStatus(CodeAssignmentStatus.IndeterminatelyReserved),
            Is.Empty);
        Assert.That(
            catalog.GetCountriesByAssignmentStatus(CodeAssignmentStatus.FormerlyAssigned),
            Is.Empty);
        Assert.That(
            catalog.GetCountriesByAssignmentStatus(CodeAssignmentStatus.UserAssigned),
            Is.Empty);
        Assert.That(
            catalog.GetCountriesByAssignmentStatus(CodeAssignmentStatus.Unassigned),
            Is.Empty);
        Assert.That(
            catalog.GetCountriesByAssignmentStatus((CodeAssignmentStatus)999),
            Is.Empty);
    }

    [Test]
    public void EnumerationResultsAreDeeplyDetachedAcrossCallsAndFilters()
    {
        ICountryCatalog catalog = new CountryCodeService();
        Country[] first = catalog.GetCountries();
        Country ivoryCoast = FindCountry(first, "CI");
        ivoryCoast.Alpha2 = "XX";
        ivoryCoast.Aliases[0] = "changed";
        ivoryCoast.LocalizedNames[0].Value = "changed";
        ivoryCoast.RegionM49Codes[0] = "999";
        first[0] = first[1];

        Country[] second = catalog.GetCountries();
        Country repeatedIvoryCoast = FindCountry(second, "CI");
        Assert.That(second[0].Alpha2, Is.EqualTo("AD"));
        Assert.That(repeatedIvoryCoast.Alpha2, Is.EqualTo("CI"));
        Assert.That(repeatedIvoryCoast.Aliases, Is.EqualTo(new[] { "Ivory Coast" }));
        Assert.That(repeatedIvoryCoast.LocalizedNames[0].Value, Is.Not.EqualTo("changed"));
        Assert.That(repeatedIvoryCoast.RegionM49Codes[0], Is.EqualTo("001"));

        Country[] filtered =
            catalog.GetCountriesByAssignmentStatus(CodeAssignmentStatus.Assigned);
        Country filteredIvoryCoast = FindCountry(filtered, "CI");
        Assert.That(filtered, Is.Not.SameAs(second));
        Assert.That(filteredIvoryCoast, Is.Not.SameAs(repeatedIvoryCoast));
        Assert.That(filteredIvoryCoast.Aliases, Is.Not.SameAs(repeatedIvoryCoast.Aliases));
        Assert.That(
            filteredIvoryCoast.LocalizedNames,
            Is.Not.SameAs(repeatedIvoryCoast.LocalizedNames));
    }

    private static Country FindCountry(Country[] countries, string alpha2)
    {
        int index = 0;
        while (index < countries.Length)
        {
            if (string.Equals(countries[index].Alpha2, alpha2, StringComparison.Ordinal))
            {
                return countries[index];
            }

            index += 1;
        }

        Assert.Fail($"Missing country {alpha2}.");
        return countries[0];
    }
}
