using NUnit.Framework;
using unicode.Tools.UnicodeDataGenerator;

namespace unicode.Tests;

[TestFixture]
public class UnicodeSecurityIdentifierDataTests
{
    const int UnicodeCodePointCount = 0x110000;

    [Test]
    public void IdentifierStatus_MatchesEveryUnicode17CodePoint()
    {
        bool[] allowed = new bool[UnicodeCodePointCount];
        UnicodeIdentifierStatusRecord[] records = UnicodeDataParser.ReadIdentifierStatus(
            SecurityPath("IdentifierStatus.txt"));
        int recordIndex = 0;
        while (recordIndex < records.Length)
        {
            UnicodeIdentifierStatusRecord record = records[recordIndex];
            int codePoint = record.StartCodePoint;
            while (codePoint <= record.EndCodePoint)
            {
                allowed[codePoint] = record.Status == "Allowed";
                codePoint = codePoint + 1;
            }

            recordIndex = recordIndex + 1;
        }

        UnicodeSecurity security = new UnicodeSecurity();
        int candidate = 0;
        while (candidate < UnicodeCodePointCount)
        {
            UnicodeIdentifierSecurityStatus expected = allowed[candidate]
                ? UnicodeIdentifierSecurityStatus.Allowed
                : UnicodeIdentifierSecurityStatus.Restricted;
            if (security.GetIdentifierStatus(candidate) != expected)
            {
                Assert.Fail("Identifier_Status mismatch at U+" + candidate.ToString("X4", System.Globalization.CultureInfo.InvariantCulture));
            }

            candidate = candidate + 1;
        }

        Assert.That(security.GetIdentifierStatus(-1), Is.EqualTo(UnicodeIdentifierSecurityStatus.Restricted));
        Assert.That(security.GetIdentifierStatus(0x110000), Is.EqualTo(UnicodeIdentifierSecurityStatus.Restricted));
    }

    [Test]
    public void IdentifierTypes_MatchEveryUnicode17RangeAndDefault()
    {
        UnicodeIdentifierTypeRecord[] records = UnicodeDataParser.ReadIdentifierType(
            SecurityPath("IdentifierType.txt"));
        UnicodeSecurity security = new UnicodeSecurity();
        int recordIndex = 0;
        while (recordIndex < records.Length)
        {
            UnicodeIdentifierTypeRecord record = records[recordIndex];
            UnicodeIdentifierType[] expected = ToIdentifierTypes(record.Types);
            AssertTypes(expected, security.GetIdentifierTypes(record.StartCodePoint), record.StartCodePoint);
            AssertTypes(expected, security.GetIdentifierTypes(record.EndCodePoint), record.EndCodePoint);
            int middle = record.StartCodePoint + ((record.EndCodePoint - record.StartCodePoint) / 2);
            AssertTypes(expected, security.GetIdentifierTypes(middle), middle);
            recordIndex = recordIndex + 1;
        }

        Assert.That(security.GetIdentifierTypes(0x0378), Is.EqualTo(new UnicodeIdentifierType[] { UnicodeIdentifierType.NotCharacter }));
        Assert.That(security.GetIdentifierTypes(-1), Is.EqualTo(new UnicodeIdentifierType[] { UnicodeIdentifierType.NotCharacter }));
        Assert.That(security.GetIdentifierTypes(0x110000), Is.EqualTo(new UnicodeIdentifierType[] { UnicodeIdentifierType.NotCharacter }));

        UnicodeIdentifierType[] returned = security.GetIdentifierTypes(0x0653);
        returned[0] = UnicodeIdentifierType.NotCharacter;
        Assert.That(
            security.GetIdentifierTypes(0x0653),
            Is.EqualTo(new UnicodeIdentifierType[] { UnicodeIdentifierType.UncommonUse, UnicodeIdentifierType.Technical }));
    }

    [Test]
    public void GeneralSecurityProfile_AppliesCanonicalEquivalence()
    {
        UnicodeSecurity security = new UnicodeSecurity();

        Assert.That(security.IsAllowedByGeneralSecurityProfile("Allowed_1"), Is.True);
        Assert.That(security.IsAllowedByGeneralSecurityProfile("\u0627\u0653"), Is.True);
        Assert.That(security.IsAllowedByGeneralSecurityProfile("\u0114"), Is.True);
        Assert.That(security.IsAllowedByGeneralSecurityProfile("\u0627\u0653\u0114"), Is.True);
        Assert.That(security.IsAllowedByGeneralSecurityProfile("\U0001F600"), Is.False);
        Assert.That(security.IsAllowedByGeneralSecurityProfile(""), Is.False);
        Assert.That(security.IsAllowedByGeneralSecurityProfile("\uD800"), Is.False);
    }

    [Test]
    public void RestrictionLevels_CoverEveryUnicode17AllowedScript()
    {
        bool[] allowed = new bool[UnicodeCodePointCount];
        UnicodeIdentifierStatusRecord[] statusRecords = UnicodeDataParser.ReadIdentifierStatus(
            SecurityPath("IdentifierStatus.txt"));
        int statusIndex = 0;
        while (statusIndex < statusRecords.Length)
        {
            UnicodeIdentifierStatusRecord status = statusRecords[statusIndex];
            int codePoint = status.StartCodePoint;
            while (codePoint <= status.EndCodePoint)
            {
                allowed[codePoint] = status.Status == "Allowed";
                codePoint = codePoint + 1;
            }

            statusIndex = statusIndex + 1;
        }

        Dictionary<UnicodeScript, int> representatives = new Dictionary<UnicodeScript, int>();
        UnicodePropertyRangeRecord[] scripts = UnicodeDataParser.ReadScripts(DataPath("Scripts.txt"));
        UnicodeProperties properties = new UnicodeProperties();
        int rangeIndex = 0;
        while (rangeIndex < scripts.Length)
        {
            UnicodePropertyRangeRecord range = scripts[rangeIndex];
            UnicodeScript expected;
            if (Enum.TryParse(range.PropertyName, out expected))
            {
                int codePoint = range.StartCodePoint;
                while (codePoint <= range.EndCodePoint)
                {
                    if (allowed[codePoint])
                    {
                        Assert.That(properties.GetScript(codePoint), Is.EqualTo(expected), "U+" + codePoint.ToString("X4", System.Globalization.CultureInfo.InvariantCulture));
                        if (!representatives.ContainsKey(expected))
                        {
                            representatives.Add(expected, codePoint);
                        }
                    }

                    codePoint = codePoint + 1;
                }
            }

            rangeIndex = rangeIndex + 1;
        }

        UnicodeSecurity security = new UnicodeSecurity();
        foreach (UnicodeScript script in Enum.GetValues<UnicodeScript>())
        {
            if (script == UnicodeScript.Unknown)
            {
                continue;
            }

            Assert.That(representatives.ContainsKey(script), Is.True, script.ToString());
            if (script == UnicodeScript.Common || script == UnicodeScript.Latin)
            {
                continue;
            }

            string candidate = "A" + new UnicodeScalar().ToUtf16String(representatives[script]);
            Assert.That(security.GetRestrictionLevel(candidate), Is.EqualTo(ExpectedLevelWithLatin(script)), script.ToString());
        }
    }

    static UnicodeIdentifierType[] ToIdentifierTypes(string[] names)
    {
        UnicodeIdentifierType[] types = new UnicodeIdentifierType[names.Length];
        int index = 0;
        while (index < names.Length)
        {
            types[index] = TypeFromName(names[index]);
            index = index + 1;
        }

        return types;
    }

    static UnicodeIdentifierType TypeFromName(string name)
    {
        return name switch
        {
            "Deprecated" => UnicodeIdentifierType.Deprecated,
            "Default_Ignorable" => UnicodeIdentifierType.DefaultIgnorable,
            "Not_NFKC" => UnicodeIdentifierType.NotNfkc,
            "Not_XID" => UnicodeIdentifierType.NotXid,
            "Exclusion" => UnicodeIdentifierType.Exclusion,
            "Obsolete" => UnicodeIdentifierType.Obsolete,
            "Technical" => UnicodeIdentifierType.Technical,
            "Uncommon_Use" => UnicodeIdentifierType.UncommonUse,
            "Limited_Use" => UnicodeIdentifierType.LimitedUse,
            "Inclusion" => UnicodeIdentifierType.Inclusion,
            "Recommended" => UnicodeIdentifierType.Recommended,
            _ => UnicodeIdentifierType.NotCharacter,
        };
    }

    static void AssertTypes(UnicodeIdentifierType[] expected, UnicodeIdentifierType[] actual, int codePoint)
    {
        if (expected.SequenceEqual(actual))
        {
            return;
        }

        Assert.Fail("Identifier_Type mismatch at U+" + codePoint.ToString("X4", System.Globalization.CultureInfo.InvariantCulture));
    }

    static UnicodeRestrictionLevel ExpectedLevelWithLatin(UnicodeScript script)
    {
        if (script == UnicodeScript.Common || script == UnicodeScript.Inherited || script == UnicodeScript.Latin)
        {
            return UnicodeRestrictionLevel.SingleScriptRestrictive;
        }

        if (script == UnicodeScript.Greek || script == UnicodeScript.Cyrillic)
        {
            return UnicodeRestrictionLevel.MinimallyRestrictive;
        }

        if (script == UnicodeScript.Han || script == UnicodeScript.Hangul || script == UnicodeScript.Hiragana || script == UnicodeScript.Katakana)
        {
            return UnicodeRestrictionLevel.HighlyRestrictive;
        }

        return UnicodeRestrictionLevel.ModeratelyRestrictive;
    }

    static string DataPath(string fileName)
    {
        return Path.Combine(FindRepositoryRoot(), "data", "Unicode17.0.0", fileName);
    }

    static string SecurityPath(string fileName)
    {
        return Path.Combine(FindRepositoryRoot(), "data", "Unicode17.0.0", "security", fileName);
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
