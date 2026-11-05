using countrycodes;

namespace CountryCodes.Tests;

[TestFixture]
public sealed class PublicResultDataTests
{
    [Test]
    public void CountryLookupResultSupportsFoundAndNotFoundOutcomes()
    {
        Country source = CreateCountry("DE", "Germany");
        Country[] candidates = { source };

        CountryLookupResult found = new CountryLookupResult(
            LookupStatus.Found,
            source,
            candidates);
        CountryLookupResult notFound = new CountryLookupResult(
            LookupStatus.NotFound,
            null,
            Array.Empty<Country>());

        source.ShortName = "changed";
        source.Aliases[0] = "changed";
        candidates[0] = CreateCountry("FR", "France");

        Assert.That(found.Status, Is.EqualTo(LookupStatus.Found));
        Assert.That(found.Country, Is.Not.Null);
        Assert.That(found.Country!.ShortName, Is.EqualTo("Germany"));
        Assert.That(found.Country.Aliases, Is.EqualTo(new[] { "Germany alias" }));
        Assert.That(found.Candidates[0].Alpha2, Is.EqualTo("DE"));
        Assert.That(found.Candidates[0], Is.Not.SameAs(found.Country));
        Assert.That(notFound.Status, Is.EqualTo(LookupStatus.NotFound));
        Assert.That(notFound.Country, Is.Null);
        Assert.That(notFound.Candidates, Is.Empty);
    }

    [Test]
    public void SubdivisionLookupResultDetachesItsEntityAndCandidates()
    {
        Subdivision source = CreateSubdivision();
        Subdivision[] candidates = { source };

        SubdivisionLookupResult result = new SubdivisionLookupResult(
            LookupStatus.Ambiguous,
            source,
            candidates);

        source.Name = "changed";
        source.Aliases[0] = "changed";
        candidates[0] = CreateSubdivision();

        Assert.That(result.Status, Is.EqualTo(LookupStatus.Ambiguous));
        Assert.That(result.Subdivision, Is.Not.Null);
        Assert.That(result.Subdivision!.Name, Is.EqualTo("California"));
        Assert.That(result.Subdivision.Aliases, Is.EqualTo(new[] { "Calif." }));
        Assert.That(result.Candidates[0].Code, Is.EqualTo("US-CA"));
        Assert.That(result.Candidates[0], Is.Not.SameAs(result.Subdivision));
    }

    [Test]
    public void HistoricalLookupResultDetachesRecordsChangesAndCandidates()
    {
        Country current = CreateCountry("AQ", "Antarctica");
        CountryChange change = CreateChange();
        HistoricalCountry historical = CreateHistoricalCountry(change);

        HistoricalLookupResult result = new HistoricalLookupResult(
            LookupStatus.Found,
            current,
            historical,
            change,
            new[] { current },
            new[] { historical },
            new[] { change });

        current.ShortName = "changed";
        historical.Name = "changed";
        historical.Changes[0].SuccessorIdentifiers[0] = "changed";
        change.SuccessorIdentifiers[0] = "changed";

        Assert.That(result.Status, Is.EqualTo(LookupStatus.Found));
        Assert.That(result.Country, Is.Not.Null);
        Assert.That(result.Country!.ShortName, Is.EqualTo("Antarctica"));
        Assert.That(result.CountryCandidates[0].Alpha2, Is.EqualTo("AQ"));
        Assert.That(result.HistoricalCountry, Is.Not.Null);
        Assert.That(result.HistoricalCountry!.Name, Is.EqualTo("British Antarctic Territory"));
        Assert.That(
            result.HistoricalCountry.Changes[0].SuccessorIdentifiers,
            Is.EqualTo(new[] { "AQ" }));
        Assert.That(result.CountryChange, Is.Not.Null);
        Assert.That(result.CountryChange!.SuccessorIdentifiers, Is.EqualTo(new[] { "AQ" }));
        Assert.That(result.HistoricalCountryCandidates[0].Alpha4, Is.EqualTo("BQAQ"));
        Assert.That(result.CountryChangeCandidates[0].Alpha4, Is.EqualTo("BQAQ"));
    }

    [Test]
    public void CodeNormalizationResultRepresentsAvailableAndMissingState()
    {
        string[] replacements = { "CZ", "SK" };
        CodeNormalizationResult replaced = new CodeNormalizationResult(
            NormalizationStatus.Ambiguous,
            CountryCodeKind.Alpha4,
            true,
            CodeAssignmentStatus.FormerlyAssigned,
            "CSHH",
            replacements);
        CodeNormalizationResult invalid = new CodeNormalizationResult(
            NormalizationStatus.Invalid,
            CountryCodeKind.Unknown,
            false,
            CodeAssignmentStatus.Unassigned,
            null,
            Array.Empty<string>());

        replacements[0] = "changed";

        Assert.That(replaced.Status, Is.EqualTo(NormalizationStatus.Ambiguous));
        Assert.That(replaced.Kind, Is.EqualTo(CountryCodeKind.Alpha4));
        Assert.That(replaced.HasAssignmentStatus, Is.True);
        Assert.That(replaced.AssignmentStatus, Is.EqualTo(CodeAssignmentStatus.FormerlyAssigned));
        Assert.That(replaced.CanonicalValue, Is.EqualTo("CSHH"));
        Assert.That(replaced.ReplacementCandidates, Is.EqualTo(new[] { "CZ", "SK" }));
        Assert.That(invalid.HasAssignmentStatus, Is.False);
        Assert.That(invalid.CanonicalValue, Is.Null);
        Assert.That(invalid.ReplacementCandidates, Is.Empty);
    }

    [Test]
    public void ValidationResultDetachesIssuesAndPreservesPositionAvailability()
    {
        ValidationIssue positioned = new ValidationIssue(
            ValidationFailureKind.InvalidCharacter,
            true,
            2,
            1,
            "Invalid character.");
        ValidationIssue unpositioned = new ValidationIssue(
            ValidationFailureKind.UnsupportedLocale,
            false,
            0,
            0,
            "Unsupported locale.");
        ValidationIssue[] issues = { positioned, unpositioned };

        ValidationResult result = new ValidationResult(false, "US-CA", issues);

        positioned.Message = "changed";
        issues[0] = unpositioned;

        Assert.That(result.IsValid, Is.False);
        Assert.That(result.NormalizedValue, Is.EqualTo("US-CA"));
        Assert.That(result.Issues, Has.Length.EqualTo(2));
        Assert.That(result.Issues[0].Kind, Is.EqualTo(ValidationFailureKind.InvalidCharacter));
        Assert.That(result.Issues[0].HasPosition, Is.True);
        Assert.That(result.Issues[0].Position, Is.EqualTo(2));
        Assert.That(result.Issues[0].Length, Is.EqualTo(1));
        Assert.That(result.Issues[0].Message, Is.EqualTo("Invalid character."));
        Assert.That(result.Issues[1].HasPosition, Is.False);
    }

    [Test]
    public void SearchOptionsStoreEverySearchControl()
    {
        SearchOptions options = new SearchOptions(
            SearchMode.Fuzzy,
            ValidationPolicy.AllowHistorical,
            "pt-BR",
            true,
            false,
            true,
            false,
            true,
            true,
            25);

        Assert.That(options.Mode, Is.EqualTo(SearchMode.Fuzzy));
        Assert.That(options.ValidationPolicy, Is.EqualTo(ValidationPolicy.AllowHistorical));
        Assert.That(options.Locale, Is.EqualTo("pt-BR"));
        Assert.That(options.UseLocaleFallback, Is.True);
        Assert.That(options.IncludeReserved, Is.False);
        Assert.That(options.IncludeHistorical, Is.True);
        Assert.That(options.IncludeUserAssigned, Is.False);
        Assert.That(options.FoldDiacritics, Is.True);
        Assert.That(options.FoldPunctuation, Is.True);
        Assert.That(options.MaximumResults, Is.EqualTo(25));
    }

    [Test]
    public void SearchMatchesDetachTheirEntitiesAndStoreRankingData()
    {
        Country country = CreateCountry("US", "United States");
        Subdivision subdivision = CreateSubdivision();

        CountrySearchMatch countryMatch = new CountrySearchMatch(
            country,
            SearchMatchKind.ExactName,
            "United States",
            "united states",
            1000);
        SubdivisionSearchMatch subdivisionMatch = new SubdivisionSearchMatch(
            subdivision,
            country,
            SearchMatchKind.Prefix,
            "California",
            "california",
            700);

        country.ShortName = "changed";
        subdivision.Name = "changed";

        Assert.That(countryMatch.Country.ShortName, Is.EqualTo("United States"));
        Assert.That(countryMatch.Kind, Is.EqualTo(SearchMatchKind.ExactName));
        Assert.That(countryMatch.SourceText, Is.EqualTo("United States"));
        Assert.That(countryMatch.NormalizedKey, Is.EqualTo("united states"));
        Assert.That(countryMatch.Score, Is.EqualTo(1000));
        Assert.That(subdivisionMatch.Subdivision.Name, Is.EqualTo("California"));
        Assert.That(subdivisionMatch.Country.ShortName, Is.EqualTo("United States"));
        Assert.That(subdivisionMatch.Kind, Is.EqualTo(SearchMatchKind.Prefix));
        Assert.That(subdivisionMatch.SourceText, Is.EqualTo("California"));
        Assert.That(subdivisionMatch.NormalizedKey, Is.EqualTo("california"));
        Assert.That(subdivisionMatch.Score, Is.EqualTo(700));
    }

    [Test]
    public void DataVersionStoresMetadataAndDetachesSourceRevisions()
    {
        string[] sourceRevisions =
        {
            "cldr-48-2=release-48-2",
            "debian-iso-codes=abb091ac"
        };

        DataVersion version = new DataVersion(
            "1.6.0",
            "1.0.8",
            "2026-08-27",
            "2026-08-27",
            "1.1.0",
            sourceRevisions,
            "922d6bcf");

        sourceRevisions[0] = "changed";

        Assert.That(version.SchemaVersion, Is.EqualTo("1.6.0"));
        Assert.That(version.DatasetVersion, Is.EqualTo("1.0.8"));
        Assert.That(version.EffectiveDate, Is.EqualTo("2026-08-27"));
        Assert.That(version.GeneratedDate, Is.EqualTo("2026-08-27"));
        Assert.That(version.GeneratorVersion, Is.EqualTo("1.1.0"));
        Assert.That(
            version.SourceRevisions,
            Is.EqualTo(new[]
            {
                "cldr-48-2=release-48-2",
                "debian-iso-codes=abb091ac"
            }));
        Assert.That(version.ContentChecksum, Is.EqualTo("922d6bcf"));
    }

    private static Country CreateCountry(string alpha2, string name)
    {
        return new Country(
            alpha2,
            alpha2 + "X",
            "001",
            1,
            alpha2,
            CodeAssignmentStatus.Assigned,
            name,
            null,
            new[] { name + " alias" },
            Array.Empty<LocalizedName>(),
            new[] { "001" },
            "flag");
    }

    private static Subdivision CreateSubdivision()
    {
        return new Subdivision(
            "US-CA",
            "US",
            "CA",
            "California",
            "State",
            null,
            SubdivisionStatus.Active,
            new[] { "Calif." },
            Array.Empty<LocalizedName>(),
            Array.Empty<string>());
    }

    private static CountryChange CreateChange()
    {
        return new CountryChange(
            "BQAQ",
            CountryChangeKind.SignificantChange,
            new[] { "BQ" },
            new[] { "AQ" },
            "1979");
    }

    private static HistoricalCountry CreateHistoricalCountry(CountryChange change)
    {
        return new HistoricalCountry(
            "BQ",
            "ATB",
            "BQAQ",
            false,
            null,
            0,
            "British Antarctic Territory",
            "1979",
            CodeAssignmentStatus.FormerlyAssigned,
            Array.Empty<string>(),
            new[] { "AQ" },
            new[] { change });
    }
}
