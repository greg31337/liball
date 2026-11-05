using System.Globalization;
using countrycodes;

namespace CountryCodes.Tests;

[TestFixture]
public sealed class GeneratedHistoricalRowsTests
{
    [Test]
    public void HistoricalRowsPreserveIdentifiersDatesAndOptionalFields()
    {
        GeneratedHistoricalCountryRow[] rows = HistoricalCountryTable.Rows;
        Assert.That(rows, Has.Length.EqualTo(31));
        Assert.That(rows[0].Alpha4, Is.EqualTo("AIDJ"));
        Assert.That(rows[rows.Length - 1].Alpha4, Is.EqualTo("ZRCD"));

        HashSet<string> reservedAlpha4 = new HashSet<string>(StringComparer.Ordinal);
        foreach (GeneratedReservedCodeRow reserved in ReservedCodeTable.Rows)
        {
            if (reserved.Kind == GeneratedCodeKind.Alpha4)
            {
                reservedAlpha4.Add(reserved.Code);
            }
        }

        HashSet<string> historicalNameTargets = new HashSet<string>(StringComparer.Ordinal);
        foreach (GeneratedCountryNameRow name in CountryNameTable.Rows)
        {
            if (name.TargetKind == GeneratedCountryNameTargetKind.HistoricalCountry)
            {
                historicalNameTargets.Add(name.TargetCode);
            }
        }

        string previous = string.Empty;
        int numericCount = 0;
        int commentCount = 0;
        foreach (GeneratedHistoricalCountryRow row in rows)
        {
            Assert.That(string.CompareOrdinal(previous, row.Alpha4), Is.LessThan(0), row.Alpha4);
            Assert.That(IsUpperAscii(row.Alpha2, 2), Is.True, row.Alpha2);
            Assert.That(IsUpperAscii(row.Alpha3, 3), Is.True, row.Alpha3);
            Assert.That(IsUpperAscii(row.Alpha4, 4), Is.True, row.Alpha4);
            Assert.That(row.Alpha4.StartsWith(row.Alpha2, StringComparison.Ordinal), Is.True);
            Assert.That(row.AssignmentState, Is.EqualTo(GeneratedCodeAssignmentState.FormerlyAssigned));
            Assert.That(reservedAlpha4.Contains(row.Alpha4), Is.True, row.Alpha4);
            Assert.That(historicalNameTargets.Contains(row.Alpha4), Is.True, row.Alpha4);
            Assert.That(row.HasNumeric, Is.EqualTo(row.NumericText.Length != 0));
            if (row.HasNumeric)
            {
                numericCount += 1;
                Assert.That(IsAsciiDigits(row.NumericText, 3), Is.True, row.NumericText);
                Assert.That(
                    row.NumericValue,
                    Is.EqualTo(int.Parse(row.NumericText, CultureInfo.InvariantCulture)));
            }
            else
            {
                Assert.That(row.NumericValue, Is.Zero);
            }

            Assert.That(row.HasWithdrawalDate, Is.EqualTo(row.WithdrawalDate.Length != 0));
            Assert.That(row.HasWithdrawalDate, Is.True, row.Alpha4);
            Assert.That(IsCanonicalDate(row.WithdrawalDate), Is.True, row.WithdrawalDate);
            Assert.That(row.HasComment, Is.EqualTo(row.Comment.Length != 0));
            if (row.HasComment)
            {
                commentCount += 1;
            }

            Assert.That(row.SourceId, Is.EqualTo("debian-iso-codes"));
            Assert.That(row.SourceRevision, Is.Not.Empty);
            Assert.That(row.UpstreamKey, Is.EqualTo(row.Alpha4));
            Assert.That(row.EffectiveDate, Is.EqualTo(row.WithdrawalDate));
            previous = row.Alpha4;
        }

        Assert.That(numericCount, Is.EqualTo(26));
        Assert.That(commentCount, Is.EqualTo(7));
        GeneratedHistoricalCountryRow britishAntarcticTerritory = FindHistorical("BQAQ");
        Assert.That(britishAntarcticTerritory.HasNumeric, Is.False);
        GeneratedHistoricalCountryRow netherlandsAntilles = FindHistorical("ANHH");
        Assert.That(netherlandsAntilles.WithdrawalDate, Is.EqualTo("2010-12-15"));
        Assert.That(netherlandsAntilles.Comment, Does.Contain("Aruba"));
    }

    [Test]
    public void CountryChangesHaveCompleteOrderedAndReferentialLinks()
    {
        GeneratedCountryChangeRow[] changes = CountryChangeTable.Rows;
        GeneratedCountryChangeLinkRow[] predecessors = CountryChangeTable.Predecessors;
        GeneratedCountryChangeLinkRow[] successors = CountryChangeTable.Successors;
        Assert.That(changes, Has.Length.EqualTo(31));
        Assert.That(predecessors, Has.Length.EqualTo(31));
        Assert.That(successors, Has.Length.EqualTo(55));

        HashSet<string> currentCountries = new HashSet<string>(StringComparer.Ordinal);
        foreach (GeneratedCountryRow country in CountryTable.Rows)
        {
            currentCountries.Add(country.Alpha2);
        }

        HashSet<string> historicalCountries = new HashSet<string>(StringComparer.Ordinal);
        foreach (GeneratedHistoricalCountryRow historical in HistoricalCountryTable.Rows)
        {
            historicalCountries.Add(historical.Alpha4);
        }

        Dictionary<string, GeneratedCountryChangeKind> kinds =
            new Dictionary<string, GeneratedCountryChangeKind>(StringComparer.Ordinal);
        int significantCount = 0;
        int mergerCount = 0;
        int splitCount = 0;
        int otherCount = 0;
        string previousChange = string.Empty;
        foreach (GeneratedCountryChangeRow change in changes)
        {
            Assert.That(string.CompareOrdinal(previousChange, change.Alpha4), Is.LessThan(0));
            Assert.That(historicalCountries.Contains(change.Alpha4), Is.True, change.Alpha4);
            Assert.That(change.HasEffectiveDate, Is.EqualTo(change.EffectiveDate.Length != 0));
            Assert.That(change.HasEffectiveDate, Is.True, change.Alpha4);
            Assert.That(IsCanonicalDate(change.EffectiveDate), Is.True, change.EffectiveDate);
            Assert.That(change.SourceId, Is.EqualTo("debian-iso-codes"));
            Assert.That(change.SourceRevision, Is.Not.Empty);
            Assert.That(change.UpstreamKey, Is.EqualTo(change.Alpha4));
            Assert.That(change.SourceEffectiveDate, Is.EqualTo(change.EffectiveDate));
            Assert.That(kinds.TryAdd(change.Alpha4, change.Kind), Is.True);
            if (change.Kind == GeneratedCountryChangeKind.SignificantChange)
            {
                significantCount += 1;
            }
            else if (change.Kind == GeneratedCountryChangeKind.Merger)
            {
                mergerCount += 1;
            }
            else if (change.Kind == GeneratedCountryChangeKind.Split)
            {
                splitCount += 1;
            }
            else
            {
                otherCount += 1;
            }

            previousChange = change.Alpha4;
        }

        Assert.That(significantCount, Is.EqualTo(10));
        Assert.That(mergerCount, Is.EqualTo(13));
        Assert.That(splitCount, Is.EqualTo(8));
        Assert.That(otherCount, Is.Zero);

        Dictionary<string, int> predecessorCounts = ValidateLinks(
            predecessors,
            kinds,
            currentCountries,
            historicalCountries);
        Dictionary<string, int> successorCounts = ValidateLinks(
            successors,
            kinds,
            currentCountries,
            historicalCountries);
        foreach (GeneratedCountryChangeRow change in changes)
        {
            Assert.That(predecessorCounts[change.Alpha4], Is.EqualTo(1), change.Alpha4);
            Assert.That(successorCounts[change.Alpha4], Is.GreaterThanOrEqualTo(1), change.Alpha4);
            if (change.Kind == GeneratedCountryChangeKind.Split)
            {
                Assert.That(successorCounts[change.Alpha4], Is.GreaterThan(1), change.Alpha4);
            }
            else
            {
                Assert.That(successorCounts[change.Alpha4], Is.EqualTo(1), change.Alpha4);
            }
        }

        foreach (GeneratedCountryChangeLinkRow predecessor in predecessors)
        {
            Assert.That(
                predecessor.TargetKind,
                Is.EqualTo(GeneratedCountryChangeTargetKind.HistoricalCountry));
            Assert.That(predecessor.TargetCode, Is.EqualTo(predecessor.ChangeAlpha4));
        }
    }

    [Test]
    public void SplitsAndMultiStepChangesPreserveAllSuccessors()
    {
        Assert.That(GetSuccessorCodes("ANHH"), Is.EqualTo(new[] { "BQ", "CW", "SX" }));
        Assert.That(GetSuccessorCodes("CSHH"), Is.EqualTo(new[] { "CZ", "SK" }));
        Assert.That(GetSuccessorCodes("CSXX"), Is.EqualTo(new[] { "ME", "RS" }));
        Assert.That(GetSuccessorCodes("GEHH"), Is.EqualTo(new[] { "KI", "TV" }));
        Assert.That(
            GetSuccessorCodes("SUHH"),
            Is.EqualTo(new[]
            {
                "AM", "AZ", "BY", "EE", "GE", "KG", "KZ", "LT", "LV", "MD", "RU", "TJ",
                "TM", "UA", "UZ"
            }));

        GeneratedCountryChangeLinkRow czechoslovakia = FindSuccessor("CSHH", "CZ");
        Assert.That(czechoslovakia.SourceId, Is.EqualTo("un-m49"));
        Assert.That(czechoslovakia.SourceField, Is.EqualTo("note"));
        GeneratedCountryChangeLinkRow gilbertAndEllice = FindSuccessor("GEHH", "KI");
        Assert.That(gilbertAndEllice.SourceId, Is.EqualTo("debian-iso-codes"));
        Assert.That(gilbertAndEllice.SourceField, Is.EqualTo("comment"));
        GeneratedCountryChangeLinkRow sovietUnion = FindSuccessor("SUHH", "RU");
        Assert.That(sovietUnion.SourceId, Is.EqualTo("cldr-48-2"));
        Assert.That(sovietUnion.SourceField, Is.EqualTo("replacement"));

        GeneratedCountryChangeLinkRow yugoslavia = GetOnlySuccessor("YUCS");
        Assert.That(
            yugoslavia.TargetKind,
            Is.EqualTo(GeneratedCountryChangeTargetKind.HistoricalCountry));
        Assert.That(yugoslavia.TargetCode, Is.EqualTo("CSXX"));
        Assert.That(FindChange("YUCS").Kind, Is.EqualTo(GeneratedCountryChangeKind.SignificantChange));
        Assert.That(FindChange("CSXX").Kind, Is.EqualTo(GeneratedCountryChangeKind.Split));

        GeneratedCountryChangeLinkRow eastGermany = GetOnlySuccessor("DDDE");
        Assert.That(eastGermany.TargetCode, Is.EqualTo("DE"));
        Assert.That(FindChange("DDDE").Kind, Is.EqualTo(GeneratedCountryChangeKind.Merger));

        GeneratedCountryChangeLinkRow byelorussia = GetOnlySuccessor("BYAA");
        Assert.That(byelorussia.TargetCode, Is.EqualTo("BY"));
        Assert.That(
            FindChange("BYAA").Kind,
            Is.EqualTo(GeneratedCountryChangeKind.SignificantChange));

        AssertHistoricalTransitionGraphIsAcyclic();
    }

    private static Dictionary<string, int> ValidateLinks(
        GeneratedCountryChangeLinkRow[] links,
        IReadOnlyDictionary<string, GeneratedCountryChangeKind> changes,
        IReadOnlySet<string> currentCountries,
        IReadOnlySet<string> historicalCountries)
    {
        Dictionary<string, int> counts = new Dictionary<string, int>(StringComparer.Ordinal);
        GeneratedCountryChangeLinkRow? previous = null;
        foreach (GeneratedCountryChangeLinkRow link in links)
        {
            Assert.That(changes.ContainsKey(link.ChangeAlpha4), Is.True, link.ChangeAlpha4);
            if (link.TargetKind == GeneratedCountryChangeTargetKind.CurrentCountry)
            {
                Assert.That(currentCountries.Contains(link.TargetCode), Is.True, link.TargetCode);
            }
            else
            {
                Assert.That(historicalCountries.Contains(link.TargetCode), Is.True, link.TargetCode);
            }

            Assert.That(link.SourceId, Is.Not.Empty);
            Assert.That(link.SourceRevision, Is.Not.Empty);
            Assert.That(link.UpstreamKey, Is.Not.Empty);
            Assert.That(link.EffectiveDate, Is.Not.Empty);
            Assert.That(link.SourceField, Is.Not.Empty);
            if (previous != null)
            {
                Assert.That(CompareLinks(previous, link), Is.LessThan(0));
            }

            previous = link;
            counts[link.ChangeAlpha4] = counts.GetValueOrDefault(link.ChangeAlpha4) + 1;
        }

        return counts;
    }

    private static void AssertHistoricalTransitionGraphIsAcyclic()
    {
        Dictionary<string, string> edges = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (GeneratedCountryChangeLinkRow successor in CountryChangeTable.Successors)
        {
            if (successor.TargetKind == GeneratedCountryChangeTargetKind.HistoricalCountry)
            {
                edges.Add(successor.ChangeAlpha4, successor.TargetCode);
            }
        }

        foreach (string start in edges.Keys)
        {
            HashSet<string> visited = new HashSet<string>(StringComparer.Ordinal);
            string current = start;
            while (edges.TryGetValue(current, out string? next))
            {
                Assert.That(visited.Add(current), Is.True, current);
                current = next;
            }
        }
    }

    private static GeneratedHistoricalCountryRow FindHistorical(string alpha4)
    {
        foreach (GeneratedHistoricalCountryRow row in HistoricalCountryTable.Rows)
        {
            if (string.Equals(row.Alpha4, alpha4, StringComparison.Ordinal))
            {
                return row;
            }
        }

        Assert.Fail($"Missing historical country {alpha4}.");
        return HistoricalCountryTable.Rows[0];
    }

    private static GeneratedCountryChangeRow FindChange(string alpha4)
    {
        foreach (GeneratedCountryChangeRow row in CountryChangeTable.Rows)
        {
            if (string.Equals(row.Alpha4, alpha4, StringComparison.Ordinal))
            {
                return row;
            }
        }

        Assert.Fail($"Missing country change {alpha4}.");
        return CountryChangeTable.Rows[0];
    }

    private static GeneratedCountryChangeLinkRow GetOnlySuccessor(string alpha4)
    {
        GeneratedCountryChangeLinkRow? result = null;
        foreach (GeneratedCountryChangeLinkRow row in CountryChangeTable.Successors)
        {
            if (!string.Equals(row.ChangeAlpha4, alpha4, StringComparison.Ordinal))
            {
                continue;
            }

            Assert.That(result, Is.Null, alpha4);
            result = row;
        }

        Assert.That(result, Is.Not.Null, alpha4);
        return result!;
    }

    private static GeneratedCountryChangeLinkRow FindSuccessor(
        string alpha4,
        string targetCode)
    {
        foreach (GeneratedCountryChangeLinkRow row in CountryChangeTable.Successors)
        {
            if (string.Equals(row.ChangeAlpha4, alpha4, StringComparison.Ordinal) &&
                string.Equals(row.TargetCode, targetCode, StringComparison.Ordinal))
            {
                return row;
            }
        }

        Assert.Fail($"Missing successor {alpha4}:{targetCode}.");
        return CountryChangeTable.Successors[0];
    }

    private static string[] GetSuccessorCodes(string alpha4)
    {
        List<string> result = new List<string>();
        foreach (GeneratedCountryChangeLinkRow row in CountryChangeTable.Successors)
        {
            if (string.Equals(row.ChangeAlpha4, alpha4, StringComparison.Ordinal))
            {
                result.Add(row.TargetCode);
            }
        }

        return result.ToArray();
    }

    private static int CompareLinks(
        GeneratedCountryChangeLinkRow left,
        GeneratedCountryChangeLinkRow right)
    {
        int comparison = string.CompareOrdinal(left.ChangeAlpha4, right.ChangeAlpha4);
        if (comparison == 0)
        {
            comparison = ((int)left.TargetKind).CompareTo((int)right.TargetKind);
        }

        return comparison == 0
            ? string.CompareOrdinal(left.TargetCode, right.TargetCode)
            : comparison;
    }

    private static bool IsUpperAscii(string value, int length)
    {
        if (value.Length != length)
        {
            return false;
        }

        foreach (char character in value)
        {
            if (character < 'A' || character > 'Z')
            {
                return false;
            }
        }

        return true;
    }

    private static bool IsAsciiDigits(string value, int length)
    {
        if (value.Length != length)
        {
            return false;
        }

        foreach (char character in value)
        {
            if (character < '0' || character > '9')
            {
                return false;
            }
        }

        return true;
    }

    private static bool IsCanonicalDate(string value)
    {
        if (value.Length == 4)
        {
            return IsAsciiDigits(value, 4);
        }

        return value.Length == 10 &&
               IsAsciiDigits(value.Substring(0, 4), 4) &&
               value[4] == '-' &&
               IsAsciiDigits(value.Substring(5, 2), 2) &&
               value[7] == '-' &&
               IsAsciiDigits(value.Substring(8, 2), 2);
    }
}
