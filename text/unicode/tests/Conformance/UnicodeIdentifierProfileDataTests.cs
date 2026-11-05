using NUnit.Framework;
using unicode.Tools.UnicodeDataGenerator;

namespace unicode.Tests;

[TestFixture]
public class UnicodeIdentifierProfileDataTests
{
    const int UnicodeCodePointCount = 0x110000;

    [Test]
    public void BuiltInProfiles_MatchCompleteUnicode17IdentifierAndPatternData()
    {
        bool[] idStart = new bool[UnicodeCodePointCount];
        bool[] idContinue = new bool[UnicodeCodePointCount];
        bool[] xidStart = new bool[UnicodeCodePointCount];
        bool[] xidContinue = new bool[UnicodeCodePointCount];
        bool[] patternSyntax = new bool[UnicodeCodePointCount];
        bool[] patternWhiteSpace = new bool[UnicodeCodePointCount];
        bool[] immutableExclusion = new bool[UnicodeCodePointCount];

        UnicodePropertyRangeRecord[] derived = UnicodeDataParser.ReadDerivedCoreProperties(
            DataPath("DerivedCoreProperties.txt"));
        MarkProperty(derived, "ID_Start", idStart);
        MarkProperty(derived, "ID_Continue", idContinue);
        MarkProperty(derived, "XID_Start", xidStart);
        MarkProperty(derived, "XID_Continue", xidContinue);

        UnicodePropertyRangeRecord[] properties = UnicodeDataParser.ReadPropList(DataPath("PropList.txt"));
        MarkProperty(properties, "Pattern_Syntax", patternSyntax);
        MarkProperty(properties, "Pattern_White_Space", patternWhiteSpace);
        MarkProperty(properties, "Noncharacter_Code_Point", immutableExclusion);

        MarkImmutableGeneralCategories(
            UnicodeDataParser.ReadUnicodeData(DataPath("UnicodeData.txt")),
            immutableExclusion);

        UnicodeIdentifiers identifiers = new UnicodeIdentifiers();
        UnicodeIdentifierProfile defaultProfile = new UnicodeProfiles().CreateDefaultIdentifier();
        UnicodeIdentifierProfile immutableProfile = new UnicodeProfiles().CreateImmutableIdentifier();
        UnicodeIdentifierProfile normalizedProfile = new UnicodeProfiles().CreateNormalizedIdentifier();
        UnicodeIdentifierProfile caseInsensitiveProfile = new UnicodeProfiles().CreateCaseInsensitiveIdentifier();

        int codePoint = 0;
        while (codePoint < UnicodeCodePointCount)
        {
            AssertEqual(idStart[codePoint], identifiers.IsIdStart(codePoint), "ID_Start", codePoint);
            AssertEqual(idContinue[codePoint], identifiers.IsIdContinue(codePoint), "ID_Continue", codePoint);
            AssertEqual(xidStart[codePoint], identifiers.IsXidStart(codePoint), "XID_Start", codePoint);
            AssertEqual(xidContinue[codePoint], identifiers.IsXidContinue(codePoint), "XID_Continue", codePoint);
            AssertEqual(patternSyntax[codePoint], identifiers.IsPatternSyntax(codePoint), "Pattern_Syntax", codePoint);
            AssertEqual(patternWhiteSpace[codePoint], identifiers.IsPatternWhiteSpace(codePoint), "Pattern_White_Space", codePoint);

            AssertEqual(xidStart[codePoint], defaultProfile.AllowsStart(codePoint), "default Start", codePoint);
            AssertEqual(xidContinue[codePoint], defaultProfile.AllowsContinue(codePoint), "default Continue", codePoint);
            AssertEqual(xidStart[codePoint], normalizedProfile.AllowsStart(codePoint), "normalized Start", codePoint);
            AssertEqual(xidContinue[codePoint], normalizedProfile.AllowsContinue(codePoint), "normalized Continue", codePoint);
            AssertEqual(xidStart[codePoint], caseInsensitiveProfile.AllowsStart(codePoint), "case-insensitive Start", codePoint);
            AssertEqual(xidContinue[codePoint], caseInsensitiveProfile.AllowsContinue(codePoint), "case-insensitive Continue", codePoint);

            bool immutableAllowed = !immutableExclusion[codePoint]
                && !patternSyntax[codePoint]
                && !patternWhiteSpace[codePoint];
            AssertEqual(immutableAllowed, immutableProfile.AllowsStart(codePoint), "immutable Start", codePoint);
            AssertEqual(immutableAllowed, immutableProfile.AllowsContinue(codePoint), "immutable Continue", codePoint);

            codePoint = codePoint + 1;
        }

        Assert.That(defaultProfile.DisallowsPatternSyntax(), Is.True);
        Assert.That(defaultProfile.DisallowsPatternWhiteSpace(), Is.True);
        Assert.That(immutableProfile.DisallowsPatternSyntax(), Is.True);
        Assert.That(immutableProfile.DisallowsPatternWhiteSpace(), Is.True);
        Assert.That(normalizedProfile.IsNormalizationSatisfied("A\u0300"), Is.False);
        Assert.That(caseInsensitiveProfile.IsNormalizationSatisfied("\u00B2"), Is.False);
        Assert.That(caseInsensitiveProfile.GetCompareKey("A\u00DF"), Is.EqualTo("ass"));
    }

    static void MarkProperty(UnicodePropertyRangeRecord[] records, string propertyName, bool[] values)
    {
        int recordIndex = 0;
        while (recordIndex < records.Length)
        {
            UnicodePropertyRangeRecord record = records[recordIndex];
            if (record.PropertyName == propertyName)
            {
                int codePoint = record.StartCodePoint;
                while (codePoint <= record.EndCodePoint)
                {
                    values[codePoint] = true;
                    codePoint = codePoint + 1;
                }
            }

            recordIndex = recordIndex + 1;
        }
    }

    static void MarkImmutableGeneralCategories(UnicodeDataRecord[] records, bool[] values)
    {
        int recordIndex = 0;
        while (recordIndex < records.Length)
        {
            UnicodeDataRecord record = records[recordIndex];
            int endCodePoint = record.CodePoint;
            if (record.Name.EndsWith(", First>", StringComparison.Ordinal))
            {
                endCodePoint = records[recordIndex + 1].CodePoint;
                recordIndex = recordIndex + 1;
            }

            if (record.GeneralCategory == "Cc" || record.GeneralCategory == "Co" || record.GeneralCategory == "Cs")
            {
                int codePoint = record.CodePoint;
                while (codePoint <= endCodePoint)
                {
                    values[codePoint] = true;
                    codePoint = codePoint + 1;
                }
            }

            recordIndex = recordIndex + 1;
        }
    }

    static void AssertEqual(bool expected, bool actual, string rule, int codePoint)
    {
        if (expected == actual)
        {
            return;
        }

        Assert.Fail(rule + " mismatch at U+" + codePoint.ToString("X4", System.Globalization.CultureInfo.InvariantCulture));
    }

    static string DataPath(string fileName)
    {
        return Path.Combine(FindRepositoryRoot(), "data", "Unicode17.0.0", fileName);
    }

    static string DataPath(string directoryName, string fileName)
    {
        return Path.Combine(FindRepositoryRoot(), "data", "Unicode17.0.0", directoryName, fileName);
    }

    static string FindRepositoryRoot()
    {
        string directory = TestContext.CurrentContext.TestDirectory;

        while (!string.IsNullOrEmpty(directory))
        {
            if (File.Exists(Path.Combine(directory, "unicode.slnx")) && Directory.Exists(Path.Combine(directory, "data")))
            {
                return directory;
            }

            DirectoryInfo? parent = Directory.GetParent(directory);
            if (parent == null)
            {
                break;
            }

            directory = parent.FullName;
        }

        return Path.GetFullPath(Path.Combine(TestContext.CurrentContext.TestDirectory, "..", "..", "..", ".."));
    }
}
