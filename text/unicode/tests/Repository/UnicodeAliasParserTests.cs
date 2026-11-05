using NUnit.Framework;
using unicode.Tools.UnicodeDataGenerator;

namespace unicode.Tests;

[TestFixture]
public class UnicodeAliasParserTests
{
    [Test]
    public void ParsePropertyAliasLine_ReadsShortLongAndExtraAliases()
    {
        UnicodePropertyAliasRecord record = UnicodeDataParser.ParsePropertyAliasLine("gc ; General_Category ; GeneralCategory # trailing comment");

        Assert.That(record.ShortName, Is.EqualTo("gc"));
        Assert.That(record.LongName, Is.EqualTo("General_Category"));
        Assert.That(record.Aliases, Is.EqualTo(new string[] { "GeneralCategory" }));
    }

    [Test]
    public void ParsePropertyAliasLine_AllowsRecordsWithoutExtraAliases()
    {
        UnicodePropertyAliasRecord record = UnicodeDataParser.ParsePropertyAliasLine("sc ; Script");

        Assert.That(record.ShortName, Is.EqualTo("sc"));
        Assert.That(record.LongName, Is.EqualTo("Script"));
        Assert.That(record.Aliases, Is.Empty);
    }

    [Test]
    public void ParsePropertyValueAliasLine_ReadsPropertyValueAndAliases()
    {
        UnicodePropertyValueAliasRecord record = UnicodeDataParser.ParsePropertyValueAliasLine("gc ; Lu ; Uppercase_Letter ; UppercaseLetter");

        Assert.That(record.PropertyName, Is.EqualTo("gc"));
        Assert.That(record.ValueName, Is.EqualTo("Lu"));
        Assert.That(record.Aliases, Is.EqualTo(new string[] { "Uppercase_Letter", "UppercaseLetter" }));
    }

    [Test]
    public void ParsePropertyValueAliasLine_PreservesNumericCanonicalCombiningClassValues()
    {
        UnicodePropertyValueAliasRecord record = UnicodeDataParser.ParsePropertyValueAliasLine("ccc ; 0 ; NR ; Not_Reordered");

        Assert.That(record.PropertyName, Is.EqualTo("ccc"));
        Assert.That(record.ValueName, Is.EqualTo("0"));
        Assert.That(record.Aliases, Is.EqualTo(new string[] { "NR", "Not_Reordered" }));
    }

    [Test]
    public void ReadAliasFiles_StripsCommentsAndBlankLines()
    {
        string propertyAliasesPath = Path.Combine(TestContext.CurrentContext.WorkDirectory, "PropertyAliases.sample.txt");
        string propertyValueAliasesPath = Path.Combine(TestContext.CurrentContext.WorkDirectory, "PropertyValueAliases.sample.txt");
        File.WriteAllLines(
            propertyAliasesPath,
            new string[]
            {
                "# comment",
                "",
                "gc ; General_Category",
                "sc ; Script # trailing comment",
            });
        File.WriteAllLines(
            propertyValueAliasesPath,
            new string[]
            {
                "# comment",
                "",
                "gc ; Lu ; Uppercase_Letter",
                "sc ; Latn ; Latin # trailing comment",
            });

        UnicodePropertyAliasRecord[] propertyAliases = UnicodeDataParser.ReadPropertyAliases(propertyAliasesPath);
        UnicodePropertyValueAliasRecord[] propertyValueAliases = UnicodeDataParser.ReadPropertyValueAliases(propertyValueAliasesPath);

        Assert.That(propertyAliases, Has.Length.EqualTo(2));
        Assert.That(propertyAliases[1].LongName, Is.EqualTo("Script"));
        Assert.That(propertyValueAliases, Has.Length.EqualTo(2));
        Assert.That(propertyValueAliases[1].Aliases, Is.EqualTo(new string[] { "Latin" }));
    }

    [Test]
    public void AliasParsers_RejectMalformedFieldCounts()
    {
        Assert.Throws<FormatException>(() => UnicodeDataParser.ParsePropertyAliasLine("gc"));
        Assert.Throws<FormatException>(() => UnicodeDataParser.ParsePropertyValueAliasLine("gc ; Lu"));
    }
}
