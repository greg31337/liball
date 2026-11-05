using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using countrycodes;

namespace CountryCodes.Tests;

[TestFixture]
public sealed class GeneratedIndexesAndMetadataTests
{
    [Test]
    public void ExactIndexesAreCompleteOrderedAndPointToCanonicalRows()
    {
        GeneratedExactIndexRow[] rows = GeneratedIndexTable.ExactRows;
        int historicalNumericCount = 0;
        foreach (GeneratedHistoricalCountryRow historical in HistoricalCountryTable.Rows)
        {
            if (historical.HasNumeric)
            {
                historicalNumericCount += 1;
            }
        }

        int expectedCount =
            CountryTable.Rows.Length * 3 +
            ReservedCodeTable.Rows.Length +
            SubdivisionTable.Rows.Length +
            SubdivisionTable.Aliases.Length +
            HistoricalCountryTable.Rows.Length * 3 +
            historicalNumericCount +
            CountryChangeTable.Rows.Length +
            GeographicRegionTable.Rows.Length +
            GeographicRegionTable.CountryMappings.Length;
        Assert.That(rows, Has.Length.EqualTo(expectedCount));
        Assert.That(rows, Has.Length.EqualTo(6470));

        HashSet<GeneratedExactIndexKind> kinds = new HashSet<GeneratedExactIndexKind>();
        GeneratedExactIndexRow? previous = null;
        foreach (GeneratedExactIndexRow row in rows)
        {
            Assert.That(row.Key, Is.Not.Empty);
            Assert.That(row.RowIndex, Is.GreaterThanOrEqualTo(0));
            Assert.That(ResolveExactKey(row), Is.EqualTo(row.Key));
            if (previous != null)
            {
                Assert.That(CompareExact(previous, row), Is.LessThan(0));
            }

            previous = row;
            kinds.Add(row.Kind);
        }

        Assert.That(kinds.Count, Is.EqualTo(Enum.GetValues<GeneratedExactIndexKind>().Length));
        Assert.That(
            CountryTable.Rows[FindExact(GeneratedExactIndexKind.CountryAlpha3, "FRA")].Alpha2,
            Is.EqualTo("FR"));
        Assert.That(
            SubdivisionTable.Rows[FindExact(GeneratedExactIndexKind.SubdivisionCode, "GB-WLS")].Code,
            Is.EqualTo("GB-WLS"));
        Assert.That(
            HistoricalCountryTable.Rows[
                FindExact(GeneratedExactIndexKind.HistoricalAlpha4, "YUCS")].Alpha4,
            Is.EqualTo("YUCS"));
        Assert.That(
            GeographicRegionTable.Rows[
                FindExact(GeneratedExactIndexKind.GeographicRegionM49, "419")].M49Code,
            Is.EqualTo("419"));
    }

    [Test]
    public void SearchIndexesAreCompleteOrderedAndResolveEveryNameRow()
    {
        GeneratedSearchIndexRow[] rows = GeneratedIndexTable.SearchRows;
        int expectedCount = CountryNameTable.Rows.Length +
                            SubdivisionNameTable.Rows.Length +
                            GeographicRegionTable.Names.Length;
        Assert.That(rows, Has.Length.EqualTo(expectedCount));
        Assert.That(rows, Has.Length.EqualTo(7475));

        HashSet<GeneratedSearchIndexKind> kinds = new HashSet<GeneratedSearchIndexKind>();
        GeneratedSearchIndexRow? previous = null;
        foreach (GeneratedSearchIndexRow row in rows)
        {
            Assert.That(row.SearchKey, Is.Not.Empty);
            Assert.That(row.RowIndex, Is.GreaterThanOrEqualTo(0));
            Assert.That(ResolveSearchKey(row), Is.EqualTo(row.SearchKey));
            if (previous != null)
            {
                Assert.That(CompareSearch(previous, row), Is.LessThan(0));
            }

            previous = row;
            kinds.Add(row.Kind);
        }

        Assert.That(kinds.Count, Is.EqualTo(Enum.GetValues<GeneratedSearchIndexKind>().Length));
        int countryNameIndex = FindSearch(
            GeneratedSearchIndexKind.CountryName,
            "cote d ivoire");
        Assert.That(CountryNameTable.Rows[countryNameIndex].TargetCode, Is.EqualTo("CI"));
        int regionNameIndex = FindSearch(
            GeneratedSearchIndexKind.GeographicRegionName,
            "latin america and the caribbean");
        Assert.That(
            GeographicRegionTable.Names[regionNameIndex].RegionM49Code,
            Is.EqualTo("419"));
    }

    [Test]
    public void MetadataContainsVersionedSourceAndContentIdentity()
    {
        Assert.That(GeneratedDataMetadata.SchemaVersion, Is.EqualTo("1.6.0"));
        Assert.That(GeneratedDataMetadata.DataVersion, Is.EqualTo("1.0.8"));
        Assert.That(GeneratedDataMetadata.EffectiveDate, Is.EqualTo("2026-08-27"));
        Assert.That(GeneratedDataMetadata.GeneratorVersion, Is.EqualTo("1.1.0"));
        Assert.That(GeneratedDataMetadata.ContentChecksumAlgorithm, Is.EqualTo("SHA-256"));
        Assert.That(
            GeneratedDataMetadata.ContentChecksumFormat,
            Is.EqualTo("file-name NUL lowercase-sha256 LF; ordinal file-name order"));
        Assert.That(IsLowerSha256(GeneratedDataMetadata.AggregateContentChecksum), Is.True);

        GeneratedSourceRevisionRow[] sources = GeneratedDataMetadata.SourceRevisions;
        Assert.That(sources, Has.Length.EqualTo(6));
        string previousSource = string.Empty;
        foreach (GeneratedSourceRevisionRow source in sources)
        {
            Assert.That(string.CompareOrdinal(previousSource, source.SourceId), Is.LessThan(0));
            Assert.That(source.Revision, Is.Not.Empty);
            Assert.That(IsCanonicalDate(source.RetrievedOn), Is.True, source.SourceId);
            previousSource = source.SourceId;
        }

        Assert.That(FindSource("debian-iso-codes").Revision, Does.StartWith("abb091ac"));
        Assert.That(
            FindSource("iana-language-subtag-registry").Revision,
            Is.EqualTo("File-Date:2026-08-08"));
    }

    [Test]
    public void AggregateChecksumMatchesTheOrderedContentHashManifest()
    {
        GeneratedContentHashRow[] rows = GeneratedDataMetadata.ContentHashes;
        Assert.That(rows, Has.Length.EqualTo(9));
        StringBuilder payload = new StringBuilder();
        string previous = string.Empty;
        foreach (GeneratedContentHashRow row in rows)
        {
            Assert.That(string.CompareOrdinal(previous, row.FileName), Is.LessThan(0));
            Assert.That(row.FileName, Does.EndWith(".g.cs"));
            Assert.That(row.FileName, Is.Not.EqualTo("GeneratedDataMetadata.g.cs"));
            Assert.That(IsLowerSha256(row.Sha256), Is.True, row.FileName);
            payload.Append(row.FileName);
            payload.Append('\0');
            payload.Append(row.Sha256);
            payload.Append('\n');
            previous = row.FileName;
        }

        string actual = Convert.ToHexString(
                SHA256.HashData(Encoding.UTF8.GetBytes(payload.ToString())))
            .ToLowerInvariant();
        Assert.That(actual, Is.EqualTo(GeneratedDataMetadata.AggregateContentChecksum));
    }

    private static string ResolveExactKey(GeneratedExactIndexRow index)
    {
        if (index.Kind == GeneratedExactIndexKind.CountryAlpha2)
        {
            return CountryTable.Rows[index.RowIndex].Alpha2;
        }

        if (index.Kind == GeneratedExactIndexKind.CountryAlpha3)
        {
            return CountryTable.Rows[index.RowIndex].Alpha3;
        }

        if (index.Kind == GeneratedExactIndexKind.CountryNumeric)
        {
            return CountryTable.Rows[index.RowIndex].NumericText;
        }

        if (index.Kind == GeneratedExactIndexKind.ReservedAlpha2 ||
            index.Kind == GeneratedExactIndexKind.ReservedAlpha3 ||
            index.Kind == GeneratedExactIndexKind.ReservedNumeric ||
            index.Kind == GeneratedExactIndexKind.ReservedAlpha4)
        {
            AssertReservedKind(index);
            return ReservedCodeTable.Rows[index.RowIndex].Code;
        }

        if (index.Kind == GeneratedExactIndexKind.SubdivisionCode)
        {
            return SubdivisionTable.Rows[index.RowIndex].Code;
        }

        if (index.Kind == GeneratedExactIndexKind.SubdivisionAlias)
        {
            return SubdivisionTable.Aliases[index.RowIndex].Code;
        }

        if (index.Kind == GeneratedExactIndexKind.HistoricalAlpha2)
        {
            return HistoricalCountryTable.Rows[index.RowIndex].Alpha2;
        }

        if (index.Kind == GeneratedExactIndexKind.HistoricalAlpha3)
        {
            return HistoricalCountryTable.Rows[index.RowIndex].Alpha3;
        }

        if (index.Kind == GeneratedExactIndexKind.HistoricalAlpha4)
        {
            return HistoricalCountryTable.Rows[index.RowIndex].Alpha4;
        }

        if (index.Kind == GeneratedExactIndexKind.HistoricalNumeric)
        {
            Assert.That(HistoricalCountryTable.Rows[index.RowIndex].HasNumeric, Is.True);
            return HistoricalCountryTable.Rows[index.RowIndex].NumericText;
        }

        if (index.Kind == GeneratedExactIndexKind.CountryChangeAlpha4)
        {
            return CountryChangeTable.Rows[index.RowIndex].Alpha4;
        }

        if (index.Kind == GeneratedExactIndexKind.GeographicRegionM49)
        {
            return GeographicRegionTable.Rows[index.RowIndex].M49Code;
        }

        return GeographicRegionTable.CountryMappings[index.RowIndex].CountryAlpha2;
    }

    private static void AssertReservedKind(GeneratedExactIndexRow index)
    {
        GeneratedCodeKind expected;
        if (index.Kind == GeneratedExactIndexKind.ReservedAlpha2)
        {
            expected = GeneratedCodeKind.Alpha2;
        }
        else if (index.Kind == GeneratedExactIndexKind.ReservedAlpha3)
        {
            expected = GeneratedCodeKind.Alpha3;
        }
        else if (index.Kind == GeneratedExactIndexKind.ReservedNumeric)
        {
            expected = GeneratedCodeKind.Numeric3;
        }
        else
        {
            expected = GeneratedCodeKind.Alpha4;
        }

        Assert.That(ReservedCodeTable.Rows[index.RowIndex].Kind, Is.EqualTo(expected));
    }

    private static string ResolveSearchKey(GeneratedSearchIndexRow index)
    {
        if (index.Kind == GeneratedSearchIndexKind.CountryName ||
            index.Kind == GeneratedSearchIndexKind.HistoricalCountryName)
        {
            GeneratedCountryNameRow row = CountryNameTable.Rows[index.RowIndex];
            GeneratedCountryNameTargetKind expected =
                index.Kind == GeneratedSearchIndexKind.CountryName
                    ? GeneratedCountryNameTargetKind.Country
                    : GeneratedCountryNameTargetKind.HistoricalCountry;
            Assert.That(row.TargetKind, Is.EqualTo(expected));
            return row.SearchKey;
        }

        if (index.Kind == GeneratedSearchIndexKind.SubdivisionName)
        {
            return SubdivisionNameTable.Rows[index.RowIndex].SearchKey;
        }

        return GeographicRegionTable.Names[index.RowIndex].SearchKey;
    }

    private static int FindExact(GeneratedExactIndexKind kind, string key)
    {
        foreach (GeneratedExactIndexRow row in GeneratedIndexTable.ExactRows)
        {
            if (row.Kind == kind && string.Equals(row.Key, key, StringComparison.Ordinal))
            {
                return row.RowIndex;
            }
        }

        Assert.Fail($"Missing exact index {kind}:{key}.");
        return 0;
    }

    private static int FindSearch(GeneratedSearchIndexKind kind, string key)
    {
        foreach (GeneratedSearchIndexRow row in GeneratedIndexTable.SearchRows)
        {
            if (row.Kind == kind && string.Equals(row.SearchKey, key, StringComparison.Ordinal))
            {
                return row.RowIndex;
            }
        }

        Assert.Fail($"Missing search index {kind}:{key}.");
        return 0;
    }

    private static GeneratedSourceRevisionRow FindSource(string sourceId)
    {
        foreach (GeneratedSourceRevisionRow row in GeneratedDataMetadata.SourceRevisions)
        {
            if (string.Equals(row.SourceId, sourceId, StringComparison.Ordinal))
            {
                return row;
            }
        }

        Assert.Fail($"Missing source revision {sourceId}.");
        return GeneratedDataMetadata.SourceRevisions[0];
    }

    private static int CompareExact(GeneratedExactIndexRow left, GeneratedExactIndexRow right)
    {
        int comparison = ((int)left.Kind).CompareTo((int)right.Kind);
        if (comparison == 0)
        {
            comparison = string.CompareOrdinal(left.Key, right.Key);
        }

        return comparison == 0 ? left.RowIndex.CompareTo(right.RowIndex) : comparison;
    }

    private static int CompareSearch(GeneratedSearchIndexRow left, GeneratedSearchIndexRow right)
    {
        int comparison = string.CompareOrdinal(left.SearchKey, right.SearchKey);
        if (comparison == 0)
        {
            comparison = ((int)left.Kind).CompareTo((int)right.Kind);
        }

        return comparison == 0 ? left.RowIndex.CompareTo(right.RowIndex) : comparison;
    }

    private static bool IsCanonicalDate(string value)
    {
        return DateOnly.TryParseExact(
            value,
            "yyyy-MM-dd",
            CultureInfo.InvariantCulture,
            DateTimeStyles.None,
            out DateOnly _);
    }

    private static bool IsLowerSha256(string value)
    {
        if (value.Length != 64)
        {
            return false;
        }

        foreach (char character in value)
        {
            bool digit = character >= '0' && character <= '9';
            bool lowerHex = character >= 'a' && character <= 'f';
            if (!digit && !lowerHex)
            {
                return false;
            }
        }

        return true;
    }
}
