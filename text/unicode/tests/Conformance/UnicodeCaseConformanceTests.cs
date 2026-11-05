using System.Text;
using NUnit.Framework;
using unicode.Tools.UnicodeDataGenerator;

namespace unicode.Tests;

[TestFixture]
public class UnicodeCaseConformanceTests
{
    [Test]
    public void Unicode17CaseFolding_MatchesEveryDefaultFullAndSimpleRow()
    {
        UnicodeCaseFoldingRecord[] records = UnicodeDataParser.ReadCaseFolding(UnicodeDataPath("CaseFolding.txt"));
        UnicodeCase unicodeCase = new UnicodeCase();
        int commonCount = 0;
        int fullCount = 0;
        int simpleCount = 0;

        Assert.That(records, Has.Length.EqualTo(1618));

        int index = 0;
        while (index < records.Length)
        {
            UnicodeCaseFoldingRecord record = records[index];
            string source = new UnicodeScalar().ToUtf16String(record.CodePoint);
            string expected = ToText(record.Mapping);

            if (record.Status == "C")
            {
                commonCount = commonCount + 1;
                Assert.That(unicodeCase.Fold(source), Is.EqualTo(expected), Message(record, "full common fold"));
                Assert.That(unicodeCase.FoldScalar(record.CodePoint), Is.EqualTo(record.Mapping[0]), Message(record, "simple common fold"));
                AssertDefaultCaselessInvariants(unicodeCase, source, expected, record);
            }

            if (record.Status == "F")
            {
                fullCount = fullCount + 1;
                Assert.That(unicodeCase.Fold(source), Is.EqualTo(expected), Message(record, "full fold"));
                AssertDefaultCaselessInvariants(unicodeCase, source, expected, record);
            }

            if (record.Status == "S")
            {
                simpleCount = simpleCount + 1;
                Assert.That(record.Mapping, Has.Length.EqualTo(1), Message(record, "simple row length"));
                Assert.That(unicodeCase.FoldScalar(record.CodePoint), Is.EqualTo(record.Mapping[0]), Message(record, "simple fold"));
            }

            index = index + 1;
        }

        Assert.That(commonCount, Is.EqualTo(1481));
        Assert.That(fullCount, Is.EqualTo(104));
        Assert.That(simpleCount, Is.EqualTo(31));
    }

    [Test]
    public void Unicode17CaseFolding_MatchesEveryTurkicRow()
    {
        UnicodeCaseFoldingRecord[] records = UnicodeDataParser.ReadCaseFolding(UnicodeDataPath("CaseFolding.txt"));
        UnicodeCase unicodeCase = new UnicodeCase();
        int turkicCount = 0;
        int index = 0;

        while (index < records.Length)
        {
            UnicodeCaseFoldingRecord record = records[index];
            if (record.Status == "T")
            {
                turkicCount = turkicCount + 1;
                string source = new UnicodeScalar().ToUtf16String(record.CodePoint);
                string expected = ToText(record.Mapping);
                Assert.That(unicodeCase.FoldTurkic(source), Is.EqualTo(expected), Message(record, "full Turkic fold"));
                Assert.That(unicodeCase.FoldScalarTurkic(record.CodePoint), Is.EqualTo(record.Mapping[0]), Message(record, "simple Turkic fold"));
                Assert.That(unicodeCase.FoldTurkic(expected), Is.EqualTo(expected), Message(record, "Turkic idempotence"));
            }

            index = index + 1;
        }

        Assert.That(turkicCount, Is.EqualTo(2));
    }

    [Test]
    public void Unicode17SpecialCasing_MatchesEveryUnconditionalMapping()
    {
        UnicodeSpecialCasingRecord[] records = UnicodeDataParser.ReadSpecialCasing(UnicodeDataPath("SpecialCasing.txt"));
        UnicodeCase unicodeCase = new UnicodeCase();
        int unconditionalCount = 0;
        int index = 0;

        Assert.That(records, Has.Length.EqualTo(119));

        while (index < records.Length)
        {
            UnicodeSpecialCasingRecord record = records[index];
            if (record.Conditions.Length == 0)
            {
                unconditionalCount = unconditionalCount + 1;
                string source = new UnicodeScalar().ToUtf16String(record.CodePoint);
                Assert.That(unicodeCase.ToLower(source), Is.EqualTo(ToText(record.LowercaseMapping)), Message(record, "lowercase"));
                Assert.That(unicodeCase.ToTitle(source), Is.EqualTo(ToText(record.TitlecaseMapping)), Message(record, "titlecase"));
                Assert.That(unicodeCase.ToUpper(source), Is.EqualTo(ToText(record.UppercaseMapping)), Message(record, "uppercase"));
            }

            index = index + 1;
        }

        Assert.That(unconditionalCount, Is.EqualTo(103));
    }

    [Test]
    public void Unicode17SpecialCasing_MatchesEveryConditionalMapping()
    {
        UnicodeSpecialCasingRecord[] records = UnicodeDataParser.ReadSpecialCasing(UnicodeDataPath("SpecialCasing.txt"));
        UnicodeCase unicodeCase = new UnicodeCase();
        int conditionalCount = 0;
        int index = 0;

        while (index < records.Length)
        {
            UnicodeSpecialCasingRecord record = records[index];
            if (record.Conditions.Length > 0)
            {
                conditionalCount = conditionalCount + 1;
                AssertConditionalMapping(unicodeCase, record);
            }

            index = index + 1;
        }

        Assert.That(conditionalCount, Is.EqualTo(16));
    }

    [Test]
    public void LocaleTags_SelectEverySupportedPrimaryLanguageForm()
    {
        UnicodeCase unicodeCase = new UnicodeCase();
        string[] turkishTags = new string[] { "tr", "TR", "tr-TR", "TR_tr", " tr-Latn-TR " };
        string[] azeriTags = new string[] { "az", "AZ", "az-Latn", "AZ_AZ", " az-Latn-AZ " };
        string[] lithuanianTags = new string[] { "lt", "LT", "lt-LT", "LT_Latn", " lt-Latn-LT " };

        int index = 0;
        while (index < turkishTags.Length)
        {
            Assert.That(unicodeCase.ToUpperLocale("i", turkishTags[index]), Is.EqualTo("\u0130"), turkishTags[index]);
            Assert.That(unicodeCase.ToLowerLocale("I", turkishTags[index]), Is.EqualTo("\u0131"), turkishTags[index]);
            Assert.That(unicodeCase.ToTitleLocale("i", turkishTags[index]), Is.EqualTo("\u0130"), turkishTags[index]);
            index = index + 1;
        }

        index = 0;
        while (index < azeriTags.Length)
        {
            Assert.That(unicodeCase.ToUpperLocale("i", azeriTags[index]), Is.EqualTo("\u0130"), azeriTags[index]);
            Assert.That(unicodeCase.ToLowerLocale("I", azeriTags[index]), Is.EqualTo("\u0131"), azeriTags[index]);
            Assert.That(unicodeCase.ToTitleLocale("i", azeriTags[index]), Is.EqualTo("\u0130"), azeriTags[index]);
            index = index + 1;
        }

        index = 0;
        while (index < lithuanianTags.Length)
        {
            Assert.That(unicodeCase.ToLowerLocale("I\u0301", lithuanianTags[index]), Is.EqualTo("i\u0307\u0301"), lithuanianTags[index]);
            Assert.That(unicodeCase.ToUpperLocale("i\u0307", lithuanianTags[index]), Is.EqualTo("I"), lithuanianTags[index]);
            Assert.That(unicodeCase.ToTitleLocale("i\u0307", lithuanianTags[index]), Is.EqualTo("I"), lithuanianTags[index]);
            index = index + 1;
        }

        Assert.That(unicodeCase.ToLowerLocale("I", "en-US"), Is.EqualTo("i"));
        Assert.That(unicodeCase.ToUpperLocale("i", ""), Is.EqualTo("I"));
    }

    [Test]
    public void ExpansionSupplementaryAndTitleModes_UseTheCompleteMappings()
    {
        UnicodeCase unicodeCase = new UnicodeCase();
        string supplementaryUpper = "\U00010400";
        string supplementaryLower = "\U00010428";
        string titleInput = "aA a\u0345 \u00DFX";

        Assert.That(unicodeCase.ToUpper("\u00DF\uFB03"), Is.EqualTo("SSFFI"));
        Assert.That(unicodeCase.ToTitle("\u00DF\uFB03"), Is.EqualTo("SsFfi"));
        Assert.That(unicodeCase.Fold("\u1E9E\uFB03"), Is.EqualTo("ssffi"));

        Assert.That(unicodeCase.ToLower(supplementaryUpper), Is.EqualTo(supplementaryLower));
        Assert.That(unicodeCase.ToUpper(supplementaryLower), Is.EqualTo(supplementaryUpper));
        Assert.That(unicodeCase.ToTitle(supplementaryLower), Is.EqualTo(supplementaryUpper));
        Assert.That(unicodeCase.Fold(supplementaryUpper), Is.EqualTo(supplementaryLower));

        Assert.That(unicodeCase.ToTitle(titleInput), Is.EqualTo("AA A\u0399 SsX"));
        Assert.That(unicodeCase.ToTitleWords(titleInput), Is.EqualTo("Aa A\u0345 Ssx"));
        Assert.That(unicodeCase.ToTitleGraphemes(titleInput), Is.EqualTo("AA A\u0345 SsX"));
        Assert.That(unicodeCase.ToTitleLocale("iI", "tr"), Is.EqualTo("\u0130I"));
        Assert.That(unicodeCase.ToTitleWordsLocale("iI", "tr"), Is.EqualTo("\u0130\u0131"));
        Assert.That(unicodeCase.ToTitleGraphemesLocale("iI", "tr"), Is.EqualTo("\u0130I"));
    }

    static void AssertDefaultCaselessInvariants(
        UnicodeCase unicodeCase,
        string source,
        string expected,
        UnicodeCaseFoldingRecord record)
    {
        Assert.That(unicodeCase.Fold(expected), Is.EqualTo(expected), Message(record, "fold idempotence"));
        Assert.That(unicodeCase.EqualsFolded(source, expected), Is.True, Message(record, "folded equality"));
        string key = unicodeCase.CompareKey(source, UnicodeCompareKeyMode.NfcFold);
        Assert.That(unicodeCase.CompareKey(key, UnicodeCompareKeyMode.NfcFold), Is.EqualTo(key), Message(record, "NFC fold-key idempotence"));
    }

    static void AssertConditionalMapping(UnicodeCase unicodeCase, UnicodeSpecialCasingRecord record)
    {
        string source = new UnicodeScalar().ToUtf16String(record.CodePoint);
        string lower = ToText(record.LowercaseMapping);
        string title = ToText(record.TitlecaseMapping);
        string upper = ToText(record.UppercaseMapping);

        if (ContainsCondition(record, "Final_Sigma"))
        {
            Assert.That(unicodeCase.ToLower("A" + source), Is.EqualTo("a" + lower), Message(record, "final sigma lower"));
            Assert.That(unicodeCase.ToTitle("A" + source), Is.EqualTo("A" + title), Message(record, "final sigma title"));
            Assert.That(unicodeCase.ToUpper("A" + source), Is.EqualTo("A" + upper), Message(record, "final sigma upper"));
            return;
        }

        string locale = LocaleCondition(record);
        if (ContainsCondition(record, "After_Soft_Dotted"))
        {
            Assert.That(unicodeCase.ToLowerLocale("i\u0327" + source, locale), Is.EqualTo("i\u0327" + lower), Message(record, "after soft dotted lower"));
            Assert.That(unicodeCase.ToTitleLocale("i\u0327" + source, locale), Is.EqualTo("I\u0327" + title), Message(record, "after soft dotted title"));
            Assert.That(unicodeCase.ToUpperLocale("i\u0327" + source, locale), Is.EqualTo("I\u0327" + upper), Message(record, "after soft dotted upper"));
            return;
        }

        if (ContainsCondition(record, "More_Above"))
        {
            Assert.That(unicodeCase.ToLowerLocale(source + "\u0301", locale), Is.EqualTo(lower + "\u0301"), Message(record, "more above lower"));
            Assert.That(unicodeCase.ToTitleLocale(source + "\u0301", locale), Is.EqualTo(title + "\u0301"), Message(record, "more above title"));
            Assert.That(unicodeCase.ToUpperLocale(source + "\u0301", locale), Is.EqualTo(upper + "\u0301"), Message(record, "more above upper"));
            return;
        }

        if (ContainsCondition(record, "After_I"))
        {
            Assert.That(unicodeCase.ToLowerLocale("I" + source, locale), Is.EqualTo("i" + lower), Message(record, "after I lower"));
            Assert.That(unicodeCase.ToTitleLocale("I" + source, locale), Is.EqualTo("I" + title), Message(record, "after I title"));
            Assert.That(unicodeCase.ToUpperLocale("I" + source, locale), Is.EqualTo("I" + upper), Message(record, "after I upper"));
            return;
        }

        Assert.That(unicodeCase.ToLowerLocale(source, locale), Is.EqualTo(lower), Message(record, "locale lower"));
        Assert.That(unicodeCase.ToTitleLocale(source, locale), Is.EqualTo(title), Message(record, "locale title"));
        Assert.That(unicodeCase.ToUpperLocale(source, locale), Is.EqualTo(upper), Message(record, "locale upper"));
    }

    static bool ContainsCondition(UnicodeSpecialCasingRecord record, string condition)
    {
        int index = 0;
        while (index < record.Conditions.Length)
        {
            if (record.Conditions[index] == condition)
            {
                return true;
            }

            index = index + 1;
        }

        return false;
    }

    static string LocaleCondition(UnicodeSpecialCasingRecord record)
    {
        if (ContainsCondition(record, "tr"))
        {
            return "tr";
        }

        if (ContainsCondition(record, "az"))
        {
            return "az";
        }

        if (ContainsCondition(record, "lt"))
        {
            return "lt";
        }

        return "";
    }

    static string Message(UnicodeCaseFoldingRecord record, string operation)
    {
        return "U+" + record.CodePoint.ToString("X4") + " " + operation + " " + record.Comment;
    }

    static string Message(UnicodeSpecialCasingRecord record, string operation)
    {
        return "U+" + record.CodePoint.ToString("X4") + " " + operation + " " + record.Comment;
    }

    static string ToText(int[] codePoints)
    {
        StringBuilder builder = new StringBuilder();
        int index = 0;

        while (index < codePoints.Length)
        {
            builder.Append(new UnicodeScalar().ToUtf16String(codePoints[index]));
            index = index + 1;
        }

        return builder.ToString();
    }

    static string UnicodeDataPath(string fileName)
    {
        return Path.Combine(FindRepositoryRoot(), "data", "Unicode17.0.0", fileName);
    }

    static string FindRepositoryRoot()
    {
        string directory = TestContext.CurrentContext.TestDirectory;

        while (!string.IsNullOrEmpty(directory))
        {
            if (File.Exists(Path.Combine(directory, "unicode.slnx")) && Directory.Exists(Path.Combine(directory, "src")))
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
