using countrycodes;

namespace CountryCodes.Tests;

[TestFixture]
public sealed class PublicEntityDataTests
{
    [Test]
    public void LocalizedNameStoresDisplayData()
    {
        LocalizedName name = new LocalizedName(
            "fr",
            "Allemagne",
            CountryNameKind.NativeOrLocalized,
            "cldr");

        Assert.That(name.Locale, Is.EqualTo("fr"));
        Assert.That(name.Value, Is.EqualTo("Allemagne"));
        Assert.That(name.Kind, Is.EqualTo(CountryNameKind.NativeOrLocalized));
        Assert.That(name.Provenance, Is.EqualTo("cldr"));
    }

    [Test]
    public void CountryStoresCompleteDataAndDetachesCollections()
    {
        string[] aliases = { "Federal Republic of Germany" };
        LocalizedName localizedName = new LocalizedName(
            "de",
            "Deutschland",
            CountryNameKind.NativeOrLocalized,
            "cldr");
        LocalizedName[] localizedNames = { localizedName };
        string[] regions = { "001", "150", "155" };

        Country country = new Country(
            "DE",
            "DEU",
            "276",
            276,
            "DE",
            CodeAssignmentStatus.Assigned,
            "Germany",
            "Federal Republic of Germany",
            aliases,
            localizedNames,
            regions,
            "🇩🇪");

        aliases[0] = "changed";
        localizedName.Value = "changed";
        localizedNames[0] = new LocalizedName("en", "changed", CountryNameKind.Alias, "test");
        regions[0] = "999";

        Assert.That(country.Alpha2, Is.EqualTo("DE"));
        Assert.That(country.Alpha3, Is.EqualTo("DEU"));
        Assert.That(country.NumericText, Is.EqualTo("276"));
        Assert.That(country.NumericValue, Is.EqualTo(276));
        Assert.That(country.CanonicalCode, Is.EqualTo("DE"));
        Assert.That(country.AssignmentStatus, Is.EqualTo(CodeAssignmentStatus.Assigned));
        Assert.That(country.ShortName, Is.EqualTo("Germany"));
        Assert.That(country.OfficialName, Is.EqualTo("Federal Republic of Germany"));
        Assert.That(country.Aliases, Is.EqualTo(new[] { "Federal Republic of Germany" }));
        Assert.That(country.LocalizedNames[0].Value, Is.EqualTo("Deutschland"));
        Assert.That(country.RegionM49Codes, Is.EqualTo(new[] { "001", "150", "155" }));
        Assert.That(country.FlagEmoji, Is.EqualTo("🇩🇪"));
    }

    [Test]
    public void SubdivisionStoresCompleteDataAndDetachesCollections()
    {
        string[] aliases = { "Calif." };
        LocalizedName localizedName = new LocalizedName(
            "es",
            "California",
            CountryNameKind.NativeOrLocalized,
            "cldr");
        LocalizedName[] localizedNames = { localizedName };
        string[] replacements = { "US-CA" };

        Subdivision subdivision = new Subdivision(
            "US-CA",
            "US",
            "CA",
            "California",
            "State",
            null,
            SubdivisionStatus.Active,
            aliases,
            localizedNames,
            replacements);

        aliases[0] = "changed";
        localizedName.Value = "changed";
        replacements[0] = "US-NY";

        Assert.That(subdivision.Code, Is.EqualTo("US-CA"));
        Assert.That(subdivision.CountryCode, Is.EqualTo("US"));
        Assert.That(subdivision.Suffix, Is.EqualTo("CA"));
        Assert.That(subdivision.Name, Is.EqualTo("California"));
        Assert.That(subdivision.Type, Is.EqualTo("State"));
        Assert.That(subdivision.ParentCode, Is.Null);
        Assert.That(subdivision.Status, Is.EqualTo(SubdivisionStatus.Active));
        Assert.That(subdivision.Aliases, Is.EqualTo(new[] { "Calif." }));
        Assert.That(subdivision.LocalizedNames[0].Value, Is.EqualTo("California"));
        Assert.That(subdivision.ReplacementCodes, Is.EqualTo(new[] { "US-CA" }));
    }

    [Test]
    public void CountryChangeStoresTransitionDataAndDetachesCollections()
    {
        string[] predecessors = { "CSXX" };
        string[] successors = { "CZ", "SK" };

        CountryChange change = new CountryChange(
            "CSHH",
            CountryChangeKind.Split,
            predecessors,
            successors,
            "1993-01-01");

        predecessors[0] = "changed";
        successors[0] = "changed";

        Assert.That(change.Alpha4, Is.EqualTo("CSHH"));
        Assert.That(change.Kind, Is.EqualTo(CountryChangeKind.Split));
        Assert.That(change.PredecessorIdentifiers, Is.EqualTo(new[] { "CSXX" }));
        Assert.That(change.SuccessorIdentifiers, Is.EqualTo(new[] { "CZ", "SK" }));
        Assert.That(change.EffectiveDate, Is.EqualTo("1993-01-01"));
    }

    [Test]
    public void HistoricalCountryRepresentsMissingValuesAndDetachesTransitions()
    {
        string[] predecessors = Array.Empty<string>();
        string[] successors = { "AQ" };
        CountryChange linkedChange = new CountryChange(
            "BQAQ",
            CountryChangeKind.SignificantChange,
            new[] { "BQ" },
            new[] { "AQ" },
            "1979");
        CountryChange[] changes = { linkedChange };

        HistoricalCountry country = new HistoricalCountry(
            "BQ",
            "ATB",
            "BQAQ",
            false,
            null,
            0,
            "British Antarctic Territory",
            "1979",
            CodeAssignmentStatus.FormerlyAssigned,
            predecessors,
            successors,
            changes);

        successors[0] = "changed";
        linkedChange.SuccessorIdentifiers[0] = "changed";
        changes[0] = new CountryChange(
            "XXXX",
            CountryChangeKind.OtherOrUnknown,
            Array.Empty<string>(),
            Array.Empty<string>(),
            null);

        Assert.That(country.Alpha2, Is.EqualTo("BQ"));
        Assert.That(country.Alpha3, Is.EqualTo("ATB"));
        Assert.That(country.Alpha4, Is.EqualTo("BQAQ"));
        Assert.That(country.HasNumericValue, Is.False);
        Assert.That(country.NumericText, Is.Null);
        Assert.That(country.NumericValue, Is.Zero);
        Assert.That(country.Name, Is.EqualTo("British Antarctic Territory"));
        Assert.That(country.WithdrawalDate, Is.EqualTo("1979"));
        Assert.That(country.AssignmentStatus, Is.EqualTo(CodeAssignmentStatus.FormerlyAssigned));
        Assert.That(country.PredecessorIdentifiers, Is.Empty);
        Assert.That(country.SuccessorIdentifiers, Is.EqualTo(new[] { "AQ" }));
        Assert.That(country.Changes[0].Alpha4, Is.EqualTo("BQAQ"));
        Assert.That(country.Changes[0].SuccessorIdentifiers, Is.EqualTo(new[] { "AQ" }));
    }

    [Test]
    public void GeographicRegionStoresHierarchyDataAndDetachesCollections()
    {
        LocalizedName localizedName = new LocalizedName(
            "fr",
            "Europe",
            CountryNameKind.NativeOrLocalized,
            "un-m49");
        LocalizedName[] localizedNames = { localizedName };
        string[] children = { "151", "154", "155", "039" };
        string[] countries = { "DE", "FR" };

        GeographicRegion region = new GeographicRegion(
            "150",
            150,
            "Europe",
            localizedNames,
            GeographicRegionLevel.Region,
            "001",
            children,
            countries);

        localizedName.Value = "changed";
        children[0] = "999";
        countries[0] = "ZZ";

        Assert.That(region.M49Code, Is.EqualTo("150"));
        Assert.That(region.M49Value, Is.EqualTo(150));
        Assert.That(region.Name, Is.EqualTo("Europe"));
        Assert.That(region.LocalizedNames[0].Value, Is.EqualTo("Europe"));
        Assert.That(region.Level, Is.EqualTo(GeographicRegionLevel.Region));
        Assert.That(region.ParentM49Code, Is.EqualTo("001"));
        Assert.That(region.ChildM49Codes, Is.EqualTo(new[] { "151", "154", "155", "039" }));
        Assert.That(region.CountryCodes, Is.EqualTo(new[] { "DE", "FR" }));
    }
}
