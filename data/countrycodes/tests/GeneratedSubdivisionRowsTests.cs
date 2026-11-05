using System.Text;
using countrycodes;

namespace CountryCodes.Tests;

[TestFixture]
public sealed class GeneratedSubdivisionRowsTests
{
    [Test]
    public void CurrentRowsAreCanonicalCountryLinkedAndAcyclic()
    {
        GeneratedSubdivisionRow[] rows = SubdivisionTable.Rows;
        Assert.That(rows, Has.Length.EqualTo(5046));
        Assert.That(rows[0].Code, Is.EqualTo("AD-02"));
        Assert.That(rows[rows.Length - 1].Code, Is.EqualTo("ZW-MW"));

        HashSet<string> countries = new HashSet<string>(StringComparer.Ordinal);
        foreach (GeneratedCountryRow country in CountryTable.Rows)
        {
            countries.Add(country.Alpha2);
        }

        Dictionary<string, GeneratedSubdivisionRow> byCode =
            new Dictionary<string, GeneratedSubdivisionRow>(StringComparer.Ordinal);
        string previous = string.Empty;
        int parentCount = 0;
        foreach (GeneratedSubdivisionRow row in rows)
        {
            Assert.That(string.CompareOrdinal(previous, row.Code), Is.LessThan(0), row.Code);
            Assert.That(row.Code, Is.EqualTo(row.CountryCode + "-" + row.Suffix));
            Assert.That(countries.Contains(row.CountryCode), Is.True, row.Code);
            Assert.That(row.Type, Is.Not.Empty, row.Code);
            Assert.That(row.Status, Is.EqualTo(GeneratedSubdivisionStatus.Active), row.Code);
            Assert.That(row.SourceId, Is.EqualTo("debian-iso-codes"), row.Code);
            Assert.That(row.SourceRevision, Is.Not.Empty, row.Code);
            Assert.That(row.UpstreamKey, Is.EqualTo(row.Code));
            Assert.That(row.EffectiveDate, Is.Not.Empty, row.Code);
            Assert.That(byCode.TryAdd(row.Code, row), Is.True, row.Code);
            Assert.That(row.HasParent, Is.EqualTo(row.ParentCode.Length != 0), row.Code);
            if (row.HasParent)
            {
                parentCount += 1;
                Assert.That(row.ParentSourceId, Is.Not.Empty, row.Code);
                Assert.That(row.ParentSourceRevision, Is.Not.Empty, row.Code);
                Assert.That(row.ParentUpstreamKey, Is.Not.Empty, row.Code);
                Assert.That(row.ParentEffectiveDate, Is.Not.Empty, row.Code);
                Assert.That(row.ParentSourceField, Is.Not.Empty, row.Code);
            }
            else
            {
                Assert.That(row.ParentSourceId, Is.Empty, row.Code);
                Assert.That(row.ParentSourceRevision, Is.Empty, row.Code);
                Assert.That(row.ParentUpstreamKey, Is.Empty, row.Code);
                Assert.That(row.ParentEffectiveDate, Is.Empty, row.Code);
                Assert.That(row.ParentSourceField, Is.Empty, row.Code);
            }

            previous = row.Code;
        }

        Assert.That(parentCount, Is.EqualTo(1456));
        foreach (GeneratedSubdivisionRow row in rows)
        {
            if (!row.HasParent)
            {
                continue;
            }

            Assert.That(byCode.TryGetValue(row.ParentCode, out GeneratedSubdivisionRow? parent), Is.True);
            Assert.That(parent, Is.Not.Null);
            Assert.That(parent!.CountryCode, Is.EqualTo(row.CountryCode), row.Code);

            HashSet<string> visited = new HashSet<string>(StringComparer.Ordinal);
            GeneratedSubdivisionRow current = row;
            while (current.HasParent)
            {
                Assert.That(visited.Add(current.Code), Is.True, current.Code);
                current = byCode[current.ParentCode];
            }
        }

        GeneratedSubdivisionRow babek = byCode["AZ-BAB"];
        Assert.That(babek.ParentCode, Is.EqualTo("AZ-NX"));
        Assert.That(babek.Type, Is.EqualTo("Rayon"));
    }

    [Test]
    public void NameRowsCoverCanonicalAndLocalizedSourceValues()
    {
        GeneratedSubdivisionNameRow[] rows = SubdivisionNameTable.Rows;
        Assert.That(rows, Has.Length.EqualTo(5049));

        HashSet<string> subdivisionCodes = new HashSet<string>(StringComparer.Ordinal);
        foreach (GeneratedSubdivisionRow subdivision in SubdivisionTable.Rows)
        {
            subdivisionCodes.Add(subdivision.Code);
        }

        HashSet<string> canonical = new HashSet<string>(StringComparer.Ordinal);
        int localizedCount = 0;
        int aliasCount = 0;
        GeneratedSubdivisionNameRow? previous = null;
        foreach (GeneratedSubdivisionNameRow row in rows)
        {
            Assert.That(subdivisionCodes.Contains(row.SubdivisionCode), Is.True, row.SubdivisionCode);
            Assert.That(row.IsCurrent, Is.True, row.SubdivisionCode);
            Assert.That(row.DisplayName.IsNormalized(NormalizationForm.FormC), Is.True, row.DisplayName);
            Assert.That(row.SearchKey.IsNormalized(NormalizationForm.FormC), Is.True, row.SearchKey);
            Assert.That(row.DisplayName, Is.Not.Empty, row.SubdivisionCode);
            Assert.That(row.SearchKey, Is.Not.Empty, row.SubdivisionCode);
            Assert.That(row.Locale, Is.Not.Empty, row.SubdivisionCode);
            Assert.That(row.SourceId, Is.Not.Empty, row.SubdivisionCode);
            Assert.That(row.SourceRevision, Is.Not.Empty, row.SubdivisionCode);
            Assert.That(row.UpstreamKey, Is.Not.Empty, row.SubdivisionCode);
            Assert.That(row.EffectiveDate, Is.Not.Empty, row.SubdivisionCode);
            Assert.That(row.SourceField, Is.Not.Empty, row.SubdivisionCode);
            Assert.That(row.HasQualifier, Is.EqualTo(row.Qualifier.Length != 0));

            if (previous != null)
            {
                Assert.That(
                    CompareNames(previous, row),
                    Is.LessThan(0),
                    $"{DescribeName(previous)} before {DescribeName(row)}");
            }

            previous = row;

            if (row.NameKind == GeneratedSubdivisionNameKind.Canonical)
            {
                Assert.That(canonical.Add(row.SubdivisionCode), Is.True, row.SubdivisionCode);
            }
            else if (row.NameKind == GeneratedSubdivisionNameKind.Localized)
            {
                localizedCount += 1;
            }
            else
            {
                aliasCount += 1;
            }
        }

        Assert.That(canonical, Has.Count.EqualTo(5046));
        Assert.That(localizedCount, Is.EqualTo(3));
        Assert.That(aliasCount, Is.Zero);
        AssertName(
            "GB-WLS",
            GeneratedSubdivisionNameKind.Canonical,
            "Wales [Cymru GB-CYM]",
            "wales cymru gb cym",
            "debian-iso-codes");
        AssertName(
            "GB-WLS",
            GeneratedSubdivisionNameKind.Localized,
            "Wales",
            "wales",
            "cldr-48-2");
    }

    [Test]
    public void DeprecatedAliasesPreserveEveryReplacementAndResolutionState()
    {
        GeneratedSubdivisionAliasRow[] aliases = SubdivisionTable.Aliases;
        GeneratedSubdivisionReplacementRow[] replacements = SubdivisionTable.Replacements;
        Assert.That(aliases, Has.Length.EqualTo(147));
        Assert.That(replacements, Has.Length.EqualTo(174));

        HashSet<string> currentSubdivisions = new HashSet<string>(StringComparer.Ordinal);
        foreach (GeneratedSubdivisionRow subdivision in SubdivisionTable.Rows)
        {
            currentSubdivisions.Add(subdivision.Code);
        }

        HashSet<string> currentCountries = new HashSet<string>(StringComparer.Ordinal);
        foreach (GeneratedCountryRow country in CountryTable.Rows)
        {
            currentCountries.Add(country.Alpha2);
        }

        HashSet<string> aliasCodes = new HashSet<string>(StringComparer.Ordinal);
        int deprecatedCount = 0;
        int overlongCount = 0;
        int currentAliasCount = 0;
        string previousAlias = string.Empty;
        foreach (GeneratedSubdivisionAliasRow alias in aliases)
        {
            Assert.That(string.CompareOrdinal(previousAlias, alias.Code), Is.LessThan(0), alias.Code);
            Assert.That(alias.CountryCode, Is.EqualTo(alias.Code.Substring(0, 2)));
            Assert.That(alias.IsCurrentSubdivision, Is.EqualTo(currentSubdivisions.Contains(alias.Code)));
            Assert.That(alias.SourceId, Is.EqualTo("cldr-48-2"));
            Assert.That(alias.SourceRevision, Is.Not.Empty);
            Assert.That(alias.UpstreamKey, Is.Not.Empty);
            Assert.That(alias.EffectiveDate, Is.Not.Empty);
            Assert.That(alias.SourceField, Is.EqualTo("reason"));
            Assert.That(aliasCodes.Add(alias.Code), Is.True, alias.Code);
            if (alias.Reason == GeneratedSubdivisionAliasReason.Deprecated)
            {
                deprecatedCount += 1;
                Assert.That(alias.IsCurrentSubdivision, Is.False, alias.Code);
            }
            else
            {
                overlongCount += 1;
            }

            if (alias.IsCurrentSubdivision)
            {
                currentAliasCount += 1;
            }

            previousAlias = alias.Code;
        }

        Assert.That(deprecatedCount, Is.EqualTo(120));
        Assert.That(overlongCount, Is.EqualTo(27));
        Assert.That(currentAliasCount, Is.EqualTo(19));

        HashSet<string> aliasesWithReplacements = new HashSet<string>(StringComparer.Ordinal);
        string previousReplacement = string.Empty;
        foreach (GeneratedSubdivisionReplacementRow replacement in replacements)
        {
            string key = replacement.AliasCode + ":" + replacement.ReplacementCode;
            Assert.That(string.CompareOrdinal(previousReplacement, key), Is.LessThan(0), key);
            Assert.That(aliasCodes.Contains(replacement.AliasCode), Is.True, replacement.AliasCode);
            Assert.That(replacement.SourceId, Is.EqualTo("cldr-48-2"));
            Assert.That(replacement.SourceRevision, Is.Not.Empty);
            Assert.That(replacement.UpstreamKey, Is.Not.Empty);
            Assert.That(replacement.EffectiveDate, Is.Not.Empty);
            Assert.That(replacement.SourceField, Is.EqualTo("replacement"));
            if (replacement.TargetKind == GeneratedSubdivisionReplacementTargetKind.Subdivision)
            {
                Assert.That(replacement.ReplacementCode.Contains('-', StringComparison.Ordinal), Is.True);
                Assert.That(
                    replacement.IsCurrent,
                    Is.EqualTo(currentSubdivisions.Contains(replacement.ReplacementCode)));
            }
            else
            {
                Assert.That(replacement.ReplacementCode, Has.Length.EqualTo(2));
                Assert.That(
                    replacement.IsCurrent,
                    Is.EqualTo(currentCountries.Contains(replacement.ReplacementCode)));
            }

            aliasesWithReplacements.Add(replacement.AliasCode);
            previousReplacement = key;
        }

        Assert.That(aliasesWithReplacements.Count, Is.EqualTo(aliases.Length));
        AssertReplacement(
            "FI-01",
            "AX",
            GeneratedSubdivisionReplacementTargetKind.CountryOrArea,
            true);
        AssertReplacement(
            "CN-15",
            "CN-MN",
            GeneratedSubdivisionReplacementTargetKind.Subdivision,
            false);

        string[] newZealandNorthReplacements = GetReplacementCodes("NZ-N");
        Assert.That(
            newZealandNorthReplacements,
            Is.EqualTo(new[]
            {
                "NZ-AUK", "NZ-BOP", "NZ-GIS", "NZ-HKB", "NZ-MWT", "NZ-NTL", "NZ-TKI",
                "NZ-WGN", "NZ-WKO"
            }));
    }

    private static void AssertName(
        string code,
        GeneratedSubdivisionNameKind kind,
        string displayName,
        string searchKey,
        string sourceId)
    {
        foreach (GeneratedSubdivisionNameRow row in SubdivisionNameTable.Rows)
        {
            if (string.Equals(row.SubdivisionCode, code, StringComparison.Ordinal) &&
                row.NameKind == kind &&
                string.Equals(row.DisplayName, displayName, StringComparison.Ordinal) &&
                string.Equals(row.SourceId, sourceId, StringComparison.Ordinal))
            {
                Assert.That(row.SearchKey, Is.EqualTo(searchKey));
                return;
            }
        }

        Assert.Fail($"Missing subdivision name {code}:{kind}:{displayName}.");
    }

    private static void AssertReplacement(
        string aliasCode,
        string replacementCode,
        GeneratedSubdivisionReplacementTargetKind targetKind,
        bool isCurrent)
    {
        foreach (GeneratedSubdivisionReplacementRow row in SubdivisionTable.Replacements)
        {
            if (string.Equals(row.AliasCode, aliasCode, StringComparison.Ordinal) &&
                string.Equals(row.ReplacementCode, replacementCode, StringComparison.Ordinal))
            {
                Assert.That(row.TargetKind, Is.EqualTo(targetKind));
                Assert.That(row.IsCurrent, Is.EqualTo(isCurrent));
                return;
            }
        }

        Assert.Fail($"Missing subdivision replacement {aliasCode}:{replacementCode}.");
    }

    private static string[] GetReplacementCodes(string aliasCode)
    {
        List<string> result = new List<string>();
        foreach (GeneratedSubdivisionReplacementRow row in SubdivisionTable.Replacements)
        {
            if (string.Equals(row.AliasCode, aliasCode, StringComparison.Ordinal))
            {
                result.Add(row.ReplacementCode);
            }
        }

        return result.ToArray();
    }

    private static string DescribeName(GeneratedSubdivisionNameRow row)
    {
        return row.SubdivisionCode + ":" + row.Locale + ":" + row.SearchKey + ":" +
               ((int)row.NameKind).ToString() + ":" + row.DisplayName + ":" + row.SourceId + ":" +
               row.UpstreamKey + ":" + row.Qualifier;
    }

    private static int CompareNames(
        GeneratedSubdivisionNameRow left,
        GeneratedSubdivisionNameRow right)
    {
        int comparison = string.CompareOrdinal(left.SubdivisionCode, right.SubdivisionCode);
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

        return comparison == 0
            ? string.CompareOrdinal(left.Qualifier, right.Qualifier)
            : comparison;
    }
}
