using System.Globalization;
using countrycodes;

namespace CountryCodes.Tests;

[TestFixture]
public sealed class GeneratedCountryRowsTests
{
    [Test]
    public void CurrentCountryRowsAreCanonicalUniqueAndOrdinal()
    {
        GeneratedCountryRow[] rows = CountryTable.Rows;
        Assert.That(rows, Has.Length.EqualTo(249));
        Assert.That(rows[0].Alpha2, Is.EqualTo("AD"));
        Assert.That(rows[rows.Length - 1].Alpha2, Is.EqualTo("ZW"));

        HashSet<string> alpha2 = new HashSet<string>(StringComparer.Ordinal);
        HashSet<string> alpha3 = new HashSet<string>(StringComparer.Ordinal);
        HashSet<string> numeric = new HashSet<string>(StringComparer.Ordinal);
        string previous = string.Empty;
        foreach (GeneratedCountryRow row in rows)
        {
            Assert.That(IsUpperAscii(row.Alpha2, 2), Is.True, row.Alpha2);
            Assert.That(IsUpperAscii(row.Alpha3, 3), Is.True, row.Alpha3);
            Assert.That(IsAsciiDigits(row.NumericText, 3), Is.True, row.NumericText);
            Assert.That(
                row.NumericText,
                Is.EqualTo(row.NumericValue.ToString("D3", CultureInfo.InvariantCulture)));
            Assert.That(row.AssignmentState, Is.EqualTo(GeneratedCodeAssignmentState.Assigned));
            Assert.That(row.SourceId, Is.EqualTo("debian-iso-codes"));
            Assert.That(row.UpstreamKey, Is.EqualTo(row.Alpha2));
            Assert.That(alpha2.Add(row.Alpha2), Is.True, row.Alpha2);
            Assert.That(alpha3.Add(row.Alpha3), Is.True, row.Alpha3);
            Assert.That(numeric.Add(row.NumericText), Is.True, row.NumericText);
            if (previous.Length != 0)
            {
                Assert.That(string.CompareOrdinal(previous, row.Alpha2), Is.LessThan(0));
            }

            previous = row.Alpha2;
        }
    }

    [Test]
    public void ReservedAndFormerRowsAreDistinctOrderedAndDoNotMaskAssignments()
    {
        GeneratedReservedCodeRow[] rows = ReservedCodeTable.Rows;
        Assert.That(rows, Has.Length.EqualTo(102));

        HashSet<string> assignedAlpha2 = new HashSet<string>(StringComparer.Ordinal);
        HashSet<string> assignedAlpha3 = new HashSet<string>(StringComparer.Ordinal);
        HashSet<string> assignedNumeric = new HashSet<string>(StringComparer.Ordinal);
        foreach (GeneratedCountryRow country in CountryTable.Rows)
        {
            assignedAlpha2.Add(country.Alpha2);
            assignedAlpha3.Add(country.Alpha3);
            assignedNumeric.Add(country.NumericText);
        }

        HashSet<string> keys = new HashSet<string>(StringComparer.Ordinal);
        GeneratedReservedCodeRow previous = rows[0];
        for (int index = 0; index < rows.Length; index += 1)
        {
            GeneratedReservedCodeRow row = rows[index];
            string key = ((int)row.Kind).ToString(CultureInfo.InvariantCulture) + ":" + row.Code;
            Assert.That(keys.Add(key), Is.True, key);
            Assert.That(row.SourceId, Is.Not.Empty);
            Assert.That(row.UpstreamKey, Is.Not.Empty);
            Assert.That(
                row.AssignmentState,
                Is.Not.EqualTo(GeneratedCodeAssignmentState.Assigned));
            Assert.That(
                row.AssignmentState,
                Is.Not.EqualTo(GeneratedCodeAssignmentState.UserAssigned));
            AssertCodeShape(row.Kind, row.Code);
            AssertDoesNotMaskCurrent(row, assignedAlpha2, assignedAlpha3, assignedNumeric);

            if (index != 0)
            {
                int kindOrder = ((int)previous.Kind).CompareTo((int)row.Kind);
                bool ordered = kindOrder < 0 ||
                               (kindOrder == 0 &&
                                string.CompareOrdinal(previous.Code, row.Code) < 0);
                Assert.That(ordered, Is.True, $"{previous.Code} before {row.Code}");
            }

            previous = row;
        }

        AssertReservation(
            GeneratedCodeKind.Alpha2,
            "UK",
            GeneratedCodeAssignmentState.ExceptionallyReserved);
        AssertReservation(
            GeneratedCodeKind.Alpha2,
            "YU",
            GeneratedCodeAssignmentState.TransitionallyReserved);
        AssertReservation(
            GeneratedCodeKind.Alpha4,
            "YUCS",
            GeneratedCodeAssignmentState.FormerlyAssigned);
    }

    [Test]
    public void UserAssignedRangesMatchReviewedAlgorithmicInputs()
    {
        GeneratedUserAssignedRange[] ranges = ReservedCodeTable.UserAssignedRanges;
        string[] expected =
        {
            "Alpha2:AA-AA",
            "Alpha2:QM-QZ",
            "Alpha2:XA-XZ",
            "Alpha2:ZZ-ZZ",
            "Alpha3:AAA-AAZ",
            "Alpha3:QMA-QZZ",
            "Alpha3:XAA-XZZ",
            "Alpha3:ZZA-ZZZ",
            "Numeric3:900-999"
        };

        Assert.That(ranges, Has.Length.EqualTo(expected.Length));
        for (int index = 0; index < ranges.Length; index += 1)
        {
            GeneratedUserAssignedRange range = ranges[index];
            string actual = range.Kind + ":" + range.Start + "-" + range.End;
            Assert.That(actual, Is.EqualTo(expected[index]));
            Assert.That(
                range.AssignmentState,
                Is.EqualTo(GeneratedCodeAssignmentState.UserAssigned));
            Assert.That(range.SourceId, Is.EqualTo("iso-3166-glossary-review"));
            Assert.That(range.UpstreamKey, Is.Not.Empty);
        }
    }

    private static void AssertReservation(
        GeneratedCodeKind kind,
        string code,
        GeneratedCodeAssignmentState expectedState)
    {
        foreach (GeneratedReservedCodeRow row in ReservedCodeTable.Rows)
        {
            if (row.Kind == kind && string.Equals(row.Code, code, StringComparison.Ordinal))
            {
                Assert.That(row.AssignmentState, Is.EqualTo(expectedState));
                return;
            }
        }

        Assert.Fail($"Missing generated reservation {kind}:{code}.");
    }

    private static void AssertDoesNotMaskCurrent(
        GeneratedReservedCodeRow row,
        IReadOnlySet<string> alpha2,
        IReadOnlySet<string> alpha3,
        IReadOnlySet<string> numeric)
    {
        if (row.Kind == GeneratedCodeKind.Alpha2)
        {
            Assert.That(alpha2.Contains(row.Code), Is.False, row.Code);
        }
        else if (row.Kind == GeneratedCodeKind.Alpha3)
        {
            Assert.That(alpha3.Contains(row.Code), Is.False, row.Code);
        }
        else if (row.Kind == GeneratedCodeKind.Numeric3)
        {
            Assert.That(numeric.Contains(row.Code), Is.False, row.Code);
        }
    }

    private static void AssertCodeShape(GeneratedCodeKind kind, string code)
    {
        if (kind == GeneratedCodeKind.Alpha2)
        {
            Assert.That(IsUpperAscii(code, 2), Is.True, code);
        }
        else if (kind == GeneratedCodeKind.Alpha3)
        {
            Assert.That(IsUpperAscii(code, 3), Is.True, code);
        }
        else if (kind == GeneratedCodeKind.Numeric3)
        {
            Assert.That(IsAsciiDigits(code, 3), Is.True, code);
        }
        else
        {
            Assert.That(IsUpperAscii(code, 4), Is.True, code);
        }
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
}
