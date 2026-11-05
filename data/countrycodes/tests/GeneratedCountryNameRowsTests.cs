using System.Globalization;
using System.Text;
using countrycodes;

namespace CountryCodes.Tests;

[TestFixture]
public sealed class GeneratedCountryNameRowsTests
{
    [Test]
    public void NameRowsCoverEveryPinnedNameCategoryAndEntity()
    {
        GeneratedCountryNameRow[] rows = CountryNameTable.Rows;
        Assert.That(rows, Has.Length.EqualTo(2246));

        Dictionary<GeneratedCountryNameKind, int> counts = new Dictionary<GeneratedCountryNameKind, int>();
        HashSet<string> assignedCountries = new HashSet<string>(StringComparer.Ordinal);
        foreach (GeneratedCountryRow country in CountryTable.Rows)
        {
            assignedCountries.Add(country.Alpha2);
        }

        HashSet<string> historicalAlpha4 = new HashSet<string>(StringComparer.Ordinal);
        foreach (GeneratedReservedCodeRow reservation in ReservedCodeTable.Rows)
        {
            if (reservation.Kind == GeneratedCodeKind.Alpha4)
            {
                historicalAlpha4.Add(reservation.Code);
            }
        }

        HashSet<string> canonicalCountries = new HashSet<string>(StringComparer.Ordinal);
        HashSet<string> namedHistoricalCountries = new HashSet<string>(StringComparer.Ordinal);
        foreach (GeneratedCountryNameRow row in rows)
        {
            counts[row.NameKind] = counts.GetValueOrDefault(row.NameKind) + 1;
            if (row.TargetKind == GeneratedCountryNameTargetKind.Country)
            {
                Assert.That(assignedCountries.Contains(row.TargetCode), Is.True, row.TargetCode);
                if (row.NameKind == GeneratedCountryNameKind.IsoShort)
                {
                    Assert.That(canonicalCountries.Add(row.TargetCode), Is.True, row.TargetCode);
                }
            }
            else
            {
                Assert.That(row.NameKind, Is.EqualTo(GeneratedCountryNameKind.Historical));
                Assert.That(historicalAlpha4.Contains(row.TargetCode), Is.True, row.TargetCode);
                Assert.That(namedHistoricalCountries.Add(row.TargetCode), Is.True, row.TargetCode);
            }
        }

        Assert.That(canonicalCountries, Has.Count.EqualTo(249));
        Assert.That(namedHistoricalCountries, Has.Count.EqualTo(31));
        Assert.That(counts[GeneratedCountryNameKind.IsoShort], Is.EqualTo(249));
        Assert.That(counts[GeneratedCountryNameKind.IsoOfficial], Is.EqualTo(173));
        Assert.That(counts[GeneratedCountryNameKind.Common], Is.EqualTo(11));
        Assert.That(counts[GeneratedCountryNameKind.Localized], Is.EqualTo(1737));
        Assert.That(counts[GeneratedCountryNameKind.Alias], Is.EqualTo(21));
        Assert.That(counts[GeneratedCountryNameKind.Deprecated], Is.EqualTo(24));
        Assert.That(counts[GeneratedCountryNameKind.Historical], Is.EqualTo(31));
    }

    [Test]
    public void DisplayNamesAndSearchKeysAreNormalizedAndTraceable()
    {
        foreach (GeneratedCountryNameRow row in CountryNameTable.Rows)
        {
            Assert.That(row.DisplayName, Is.Not.Empty);
            Assert.That(row.DisplayName.IsNormalized(NormalizationForm.FormC), Is.True, row.DisplayName);
            Assert.That(row.SearchKey, Is.Not.Empty);
            Assert.That(row.SearchKey.IsNormalized(NormalizationForm.FormC), Is.True, row.SearchKey);
            Assert.That(row.SearchKey, Is.EqualTo(row.SearchKey.ToLowerInvariant()), row.SearchKey);
            Assert.That(row.SearchKey[0], Is.Not.EqualTo(' '), row.SearchKey);
            Assert.That(row.SearchKey[row.SearchKey.Length - 1], Is.Not.EqualTo(' '), row.SearchKey);
            Assert.That(row.SearchKey.Contains("  ", StringComparison.Ordinal), Is.False, row.SearchKey);
            Assert.That(row.Locale, Is.Not.Empty);
            Assert.That(row.SourceId, Is.Not.Empty);
            Assert.That(row.SourceRevision, Is.Not.Empty);
            Assert.That(row.UpstreamKey, Is.Not.Empty);
            Assert.That(row.EffectiveDate, Is.Not.Empty);
            Assert.That(row.SourceField, Is.Not.Empty);
            Assert.That(row.HasQualifier, Is.EqualTo(row.Qualifier.Length != 0));
        }

        AssertName(
            "CI",
            GeneratedCountryNameKind.IsoShort,
            "en",
            "Côte d'Ivoire",
            "cote d ivoire",
            null,
            "debian-iso-codes");
        AssertName(
            "CI",
            GeneratedCountryNameKind.Alias,
            "en",
            "Ivory Coast",
            "ivory coast",
            "variant",
            "cldr-48-2");
        AssertName(
            "CZ",
            GeneratedCountryNameKind.Deprecated,
            "en",
            "Czech Republic",
            "czech republic",
            "previously-listed",
            "un-m49");
        AssertName(
            "ANHH",
            GeneratedCountryNameKind.Historical,
            "en",
            "Netherlands Antilles",
            "netherlands antilles",
            null,
            "debian-iso-codes");
        AssertName(
            "DE",
            GeneratedCountryNameKind.Localized,
            "ar",
            "ألمانيا",
            "المانيا",
            null,
            "un-m49");
    }

    [Test]
    public void NameRowsUseStableOrdinalOrder()
    {
        GeneratedCountryNameRow[] rows = CountryNameTable.Rows;
        for (int index = 1; index < rows.Length; index += 1)
        {
            Assert.That(
                CompareRows(rows[index - 1], rows[index]),
                Is.LessThan(0),
                $"{Describe(rows[index - 1])} before {Describe(rows[index])}");
        }
    }

    private static void AssertName(
        string targetCode,
        GeneratedCountryNameKind kind,
        string locale,
        string displayName,
        string searchKey,
        string? qualifier,
        string sourceId)
    {
        foreach (GeneratedCountryNameRow row in CountryNameTable.Rows)
        {
            if (string.Equals(row.TargetCode, targetCode, StringComparison.Ordinal) &&
                row.NameKind == kind &&
                string.Equals(row.Locale, locale, StringComparison.Ordinal) &&
                string.Equals(row.DisplayName, displayName, StringComparison.Ordinal) &&
                string.Equals(row.SourceId, sourceId, StringComparison.Ordinal))
            {
                Assert.That(row.SearchKey, Is.EqualTo(searchKey));
                Assert.That(row.HasQualifier, Is.EqualTo(qualifier != null));
                Assert.That(row.Qualifier, Is.EqualTo(qualifier ?? string.Empty));
                return;
            }
        }

        Assert.Fail($"Missing generated name {targetCode}:{kind}:{locale}:{displayName}.");
    }

    private static int CompareRows(GeneratedCountryNameRow left, GeneratedCountryNameRow right)
    {
        int comparison = ((int)left.TargetKind).CompareTo((int)right.TargetKind);
        if (comparison == 0)
        {
            comparison = string.CompareOrdinal(left.TargetCode, right.TargetCode);
        }

        if (comparison == 0)
        {
            comparison = string.CompareOrdinal(left.Locale, right.Locale);
        }

        if (comparison == 0)
        {
            comparison = string.CompareOrdinal(left.SearchKey, right.SearchKey);
        }

        if (comparison == 0)
        {
            comparison = ((int)left.NameKind).CompareTo((int)right.NameKind);
        }

        if (comparison == 0)
        {
            comparison = string.CompareOrdinal(left.DisplayName, right.DisplayName);
        }

        if (comparison == 0)
        {
            comparison = string.CompareOrdinal(left.SourceId, right.SourceId);
        }

        if (comparison == 0)
        {
            comparison = string.CompareOrdinal(left.UpstreamKey, right.UpstreamKey);
        }

        if (comparison == 0)
        {
            comparison = string.CompareOrdinal(left.SourceField, right.SourceField);
        }

        return comparison == 0
            ? string.CompareOrdinal(left.Qualifier ?? string.Empty, right.Qualifier ?? string.Empty)
            : comparison;
    }

    private static string Describe(GeneratedCountryNameRow row)
    {
        return row.TargetKind + ":" + row.TargetCode + ":" + row.Locale + ":" + row.SearchKey + ":" +
               row.NameKind + ":" + row.SourceId;
    }
}
