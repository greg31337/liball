using System.Text;
using NUnit.Framework;
using unicode.Tools.UnicodeDataGenerator;

namespace unicode.Tests;

[TestFixture]
public class UnicodeSecurityConformanceTests
{
    [Test]
    public void ConfusableSkeleton_MatchesEveryUnicode17Mapping()
    {
        UnicodeConfusableRecord[] records = UnicodeDataParser.ReadConfusables(
            SecurityPath("confusables.txt"));
        Dictionary<int, int[]> mappings = ConfusableMappings(records);
        HashSet<int> defaultIgnorables = PropertyCodePoints(
            DataPath("DerivedCoreProperties.txt"),
            "Default_Ignorable_Code_Point");
        UnicodeSecurity security = new UnicodeSecurity();
        int index = 0;
        while (index < records.Length)
        {
            string source = CodePointsToString(records[index].Source);
            string expected = ExpectedSkeleton(source, mappings, defaultIgnorables);
            string actual = security.GetSkeleton(source);
            if (actual != expected)
            {
                Assert.Fail(
                    "Confusable skeleton mismatch for " +
                    CodePointsToLabel(records[index].Source) +
                    ": expected " +
                    CodePointsToLabel(records[index].Target) +
                    ", actual " +
                    CodePointsToLabel(ToCodePoints(actual)));
            }

            index = index + 1;
        }

        Assert.That(records.Length, Is.GreaterThan(6000));
    }

    [Test]
    public void SecurityDiagnostics_CoverEveryUnicode17SecurityPropertyScalar()
    {
        AssertDiagnosticProperty(
            DataPath("DerivedCoreProperties.txt"),
            "Default_Ignorable_Code_Point",
            UnicodeSecurityFlag.DefaultIgnorable,
            UnicodeSecurityRules.DefaultIgnorableRule,
            true);
        AssertDiagnosticProperty(
            DataPath("PropList.txt"),
            "Variation_Selector",
            UnicodeSecurityFlag.VariationSelector,
            UnicodeSecurityRules.VariationSelectorRule,
            true);
        AssertDiagnosticProperty(
            DataPath("PropList.txt"),
            "Join_Control",
            UnicodeSecurityFlag.JoinControl,
            UnicodeSecurityRules.JoinControlRule,
            true);
        AssertDiagnosticProperty(
            DataPath("PropList.txt"),
            "Bidi_Control",
            UnicodeSecurityFlag.BidiControl,
            UnicodeSecurityRules.BidiControlRule,
            true);
    }

    [Test]
    public void ScriptResolution_ProcessesEveryUnicode17ScriptExtensionScalar()
    {
        UnicodeScriptExtensionsRecord[] records = UnicodeDataParser.ReadScriptExtensions(
            DataPath("ScriptExtensions.txt"));
        UnicodeProperties properties = new UnicodeProperties();
        UnicodeSecurity security = new UnicodeSecurity();
        int recordIndex = 0;
        while (recordIndex < records.Length)
        {
            int codePoint = records[recordIndex].StartCodePoint;
            while (codePoint <= records[recordIndex].EndCodePoint)
            {
                UnicodeScript[] extensions = properties.GetScriptExtensions(codePoint);
                UnicodeScript[] expected = RemoveNeutralScripts(extensions);
                UnicodeScript[] actual = security.GetScriptSet(char.ConvertFromUtf32(codePoint));
                if (!expected.SequenceEqual(actual))
                {
                    Assert.Fail("Security script resolution mismatch at U+" + CodePointLabel(codePoint));
                }

                codePoint = codePoint + 1;
            }

            recordIndex = recordIndex + 1;
        }
    }

    static void AssertDiagnosticProperty(
        string path,
        string propertyName,
        UnicodeSecurityFlag flag,
        string rule,
        bool requireInvisible)
    {
        UnicodePropertyRangeRecord[] records = UnicodeDataParser.ReadPropertyRanges(path);
        UnicodeSecurity security = new UnicodeSecurity();
        int matched = 0;
        int recordIndex = 0;
        while (recordIndex < records.Length)
        {
            if (records[recordIndex].PropertyName == propertyName)
            {
                int codePoint = records[recordIndex].StartCodePoint;
                while (codePoint <= records[recordIndex].EndCodePoint)
                {
                    UnicodeSecurityDiagnostic[] diagnostics = security.GetSecurityDiagnostics(
                        char.ConvertFromUtf32(codePoint));
                    if (!ContainsDiagnostic(diagnostics, flag, rule))
                    {
                        Assert.Fail(propertyName + " diagnostic missing at U+" + CodePointLabel(codePoint));
                    }

                    if (requireInvisible && !ContainsDiagnostic(
                        diagnostics,
                        UnicodeSecurityFlag.Invisible,
                        UnicodeSecurityRules.InvisibleRule))
                    {
                        Assert.Fail("Invisible diagnostic missing at U+" + CodePointLabel(codePoint));
                    }

                    matched = matched + 1;
                    codePoint = codePoint + 1;
                }
            }

            recordIndex = recordIndex + 1;
        }

        Assert.That(matched, Is.GreaterThan(0), propertyName);
    }

    static bool ContainsDiagnostic(
        UnicodeSecurityDiagnostic[] diagnostics,
        UnicodeSecurityFlag flag,
        string rule)
    {
        int index = 0;
        while (index < diagnostics.Length)
        {
            if (diagnostics[index].Flag == flag && diagnostics[index].Rule == rule)
            {
                return true;
            }

            index = index + 1;
        }

        return false;
    }

    static UnicodeScript[] RemoveNeutralScripts(UnicodeScript[] scripts)
    {
        List<UnicodeScript> result = new List<UnicodeScript>();
        int index = 0;
        while (index < scripts.Length)
        {
            if (scripts[index] != UnicodeScript.Common &&
                scripts[index] != UnicodeScript.Inherited &&
                scripts[index] != UnicodeScript.Unknown)
            {
                result.Add(scripts[index]);
            }

            index = index + 1;
        }

        return result.ToArray();
    }

    static Dictionary<int, int[]> ConfusableMappings(UnicodeConfusableRecord[] records)
    {
        Dictionary<int, int[]> mappings = new Dictionary<int, int[]>();
        int index = 0;
        while (index < records.Length)
        {
            Assert.That(records[index].Source, Has.Length.EqualTo(1));
            mappings[records[index].Source[0]] = records[index].Target;
            index = index + 1;
        }

        return mappings;
    }

    static HashSet<int> PropertyCodePoints(string path, string propertyName)
    {
        UnicodePropertyRangeRecord[] records = UnicodeDataParser.ReadPropertyRanges(path);
        HashSet<int> result = new HashSet<int>();
        int recordIndex = 0;
        while (recordIndex < records.Length)
        {
            if (records[recordIndex].PropertyName == propertyName)
            {
                int codePoint = records[recordIndex].StartCodePoint;
                while (codePoint <= records[recordIndex].EndCodePoint)
                {
                    result.Add(codePoint);
                    codePoint = codePoint + 1;
                }
            }

            recordIndex = recordIndex + 1;
        }

        return result;
    }

    static string ExpectedSkeleton(
        string source,
        Dictionary<int, int[]> mappings,
        HashSet<int> defaultIgnorables)
    {
        string decomposed = source.Normalize(NormalizationForm.FormD);
        int[] codePoints = ToCodePoints(decomposed);
        StringBuilder builder = new StringBuilder();
        int index = 0;
        while (index < codePoints.Length)
        {
            if (!defaultIgnorables.Contains(codePoints[index]))
            {
                if (mappings.TryGetValue(codePoints[index], out int[]? target))
                {
                    builder.Append(CodePointsToString(target));
                }
                else
                {
                    builder.Append(char.ConvertFromUtf32(codePoints[index]));
                }
            }

            index = index + 1;
        }

        return builder.ToString().Normalize(NormalizationForm.FormD);
    }

    static string CodePointsToString(int[] codePoints)
    {
        StringBuilder builder = new StringBuilder();
        int index = 0;
        while (index < codePoints.Length)
        {
            builder.Append(char.ConvertFromUtf32(codePoints[index]));
            index = index + 1;
        }

        return builder.ToString();
    }

    static int[] ToCodePoints(string text)
    {
        UnicodeScalarRecord[] scalars = new UnicodeScalarReader().ReadAll(text);
        int[] result = new int[scalars.Length];
        int index = 0;
        while (index < scalars.Length)
        {
            result[index] = scalars[index].CodePoint;
            index = index + 1;
        }

        return result;
    }

    static string CodePointsToLabel(int[] codePoints)
    {
        StringBuilder builder = new StringBuilder();
        int index = 0;
        while (index < codePoints.Length)
        {
            if (index > 0)
            {
                builder.Append(' ');
            }

            builder.Append("U+");
            builder.Append(CodePointLabel(codePoints[index]));
            index = index + 1;
        }

        return builder.ToString();
    }

    static string CodePointLabel(int codePoint)
    {
        return codePoint.ToString("X4", System.Globalization.CultureInfo.InvariantCulture);
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
