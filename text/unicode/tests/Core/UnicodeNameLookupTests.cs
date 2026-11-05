using NUnit.Framework;

namespace unicode.Tests;

[TestFixture]
public class UnicodeNameLookupTests
{
    [Test]
    public void GetName_ReturnsRepresentativeNames()
    {
        Assert.That(new UnicodeProperties().GetName(0x0030), Is.EqualTo("DIGIT ZERO"));
        Assert.That(new UnicodeProperties().GetName(0x0041), Is.EqualTo("LATIN CAPITAL LETTER A"));
        Assert.That(new UnicodeProperties().GetName(0x0061), Is.EqualTo("LATIN SMALL LETTER A"));
        Assert.That(new UnicodeProperties().GetName(0x00BD), Is.EqualTo("VULGAR FRACTION ONE HALF"));
        Assert.That(new UnicodeProperties().GetName(0x0301), Is.EqualTo("COMBINING ACUTE ACCENT"));
        Assert.That(new UnicodeProperties().GetName(0x1F600), Is.EqualTo("GRINNING FACE"));
    }

    [Test]
    public void GetName_ReturnsEmptyStringForUnknownCodePoint()
    {
        Assert.That(new UnicodeProperties().GetName(0x10FFFF), Is.EqualTo(""));
    }

    [Test]
    public void LookupName_ReturnsCodePointByUnicodeName()
    {
        Assert.That(new UnicodeProperties().LookupName("DIGIT ZERO"), Is.EqualTo(0x0030));
        Assert.That(new UnicodeProperties().LookupName("LATIN CAPITAL LETTER A"), Is.EqualTo(0x0041));
        Assert.That(new UnicodeProperties().LookupName("latin small letter a"), Is.EqualTo(0x0061));
        Assert.That(new UnicodeProperties().LookupName("vulgar fraction one half"), Is.EqualTo(0x00BD));
        Assert.That(new UnicodeProperties().LookupName("NO SUCH CHARACTER"), Is.EqualTo(-1));
    }

    [Test]
    public void ProductionNameTables_IncludeSupplementaryCharacters()
    {
        Assert.That(new UnicodeProperties().GetName(0x10400), Is.EqualTo("DESERET CAPITAL LETTER LONG I"));
        Assert.That(new UnicodeProperties().LookupName("deseret small letter long i"), Is.EqualTo(0x10428));
    }

    [TestCase(0xAC00, "HANGUL SYLLABLE GA")]
    [TestCase(0xAC01, "HANGUL SYLLABLE GAG")]
    [TestCase(0xD7A3, "HANGUL SYLLABLE HIH")]
    [TestCase(0x3400, "CJK UNIFIED IDEOGRAPH-3400")]
    [TestCase(0x20000, "CJK UNIFIED IDEOGRAPH-20000")]
    [TestCase(0x33479, "CJK UNIFIED IDEOGRAPH-33479")]
    [TestCase(0x17000, "TANGUT IDEOGRAPH-17000")]
    [TestCase(0x18D1E, "TANGUT IDEOGRAPH-18D1E")]
    public void AlgorithmicNames_RoundTrip(int codePoint, string name)
    {
        UnicodeProperties properties = new UnicodeProperties();

        Assert.That(properties.GetName(codePoint), Is.EqualTo(name));
        Assert.That(properties.LookupName(name.ToLowerInvariant()), Is.EqualTo(codePoint));
    }

    [Test]
    public void AlgorithmicNameLookup_RejectsMalformedAndOutOfRangeNames()
    {
        UnicodeProperties properties = new UnicodeProperties();

        Assert.That(properties.LookupName("HANGUL SYLLABLE"), Is.EqualTo(-1));
        Assert.That(properties.LookupName("HANGUL SYLLABLE GAX"), Is.EqualTo(-1));
        Assert.That(properties.LookupName("CJK UNIFIED IDEOGRAPH-4DC0"), Is.EqualTo(-1));
        Assert.That(properties.LookupName("CJK UNIFIED IDEOGRAPH-0004E00"), Is.EqualTo(-1));
        Assert.That(properties.LookupName("CJK UNIFIED IDEOGRAPH-110000"), Is.EqualTo(-1));
        Assert.That(properties.LookupName("TANGUT IDEOGRAPH-18800"), Is.EqualTo(-1));
        Assert.That(properties.GetName(-1), Is.EqualTo(""));
        Assert.That(properties.GetName(0xD800), Is.EqualTo(""));
        Assert.That(properties.GetName(0x110000), Is.EqualTo(""));
    }

    [Test]
    public void CharacterNameMatching_IsCaseInsensitiveButOtherwiseExact()
    {
        UnicodeProperties properties = new UnicodeProperties();

        Assert.That(properties.LookupName("latin capital letter a"), Is.EqualTo(0x0041));
        Assert.That(properties.LookupName("LATIN  CAPITAL LETTER A"), Is.EqualTo(-1));
        Assert.That(properties.LookupName("LATIN_CAPITAL_LETTER_A"), Is.EqualTo(-1));
        Assert.That(properties.LookupName(" LATIN CAPITAL LETTER A"), Is.EqualTo(-1));
    }

    [Test]
    public void LookupName_RejectsNullName()
    {
        Assert.Throws<ArgumentNullException>(() => new UnicodeProperties().LookupName(null!));
    }

    [Test]
    public void CharacterNameAliases_AreInspectableAndSearchable()
    {
        UnicodeProperties properties = new UnicodeProperties();
        UnicodeNameAlias[] aliases = properties.GetNameAliases(0x0000);

        Assert.That(aliases, Has.Length.EqualTo(2));
        Assert.That(aliases[0].CodePoint, Is.EqualTo(0x0000));
        Assert.That(aliases[0].Name, Is.EqualTo("NULL"));
        Assert.That(aliases[0].Kind, Is.EqualTo(UnicodeNameAliasKind.Control));
        Assert.That(aliases[1].Name, Is.EqualTo("NUL"));
        Assert.That(aliases[1].Kind, Is.EqualTo(UnicodeNameAliasKind.Abbreviation));
        Assert.That(properties.LookupName("null"), Is.EqualTo(0x0000));
        Assert.That(properties.LookupName("latin capital letter gha"), Is.EqualTo(0x01A2));
        Assert.That(properties.GetNameAliases(0x0041), Is.Empty);
    }

    [Test]
    public void CharacterNameAliases_ReturnCallerOwnedRecords()
    {
        UnicodeProperties properties = new UnicodeProperties();
        UnicodeNameAlias[] aliases = properties.GetNameAliases(0x0000);
        aliases[0].Name = "changed";
        aliases[0].CodePoint = 1;

        UnicodeNameAlias[] second = properties.GetNameAliases(0x0000);
        Assert.That(second[0].Name, Is.EqualTo("NULL"));
        Assert.That(second[0].CodePoint, Is.EqualTo(0x0000));
    }

    [Test]
    public void CharacterNameAliases_ExposeEveryAliasKind()
    {
        UnicodeProperties properties = new UnicodeProperties();

        Assert.That(properties.GetNameAliases(0x01A2)[0].Kind, Is.EqualTo(UnicodeNameAliasKind.Correction));
        Assert.That(properties.GetNameAliases(0x0000)[0].Kind, Is.EqualTo(UnicodeNameAliasKind.Control));
        Assert.That(properties.GetNameAliases(0xFEFF)[0].Kind, Is.EqualTo(UnicodeNameAliasKind.Alternate));
        Assert.That(properties.GetNameAliases(0x0080)[0].Kind, Is.EqualTo(UnicodeNameAliasKind.Figment));
        Assert.That(properties.GetNameAliases(0x0000)[1].Kind, Is.EqualTo(UnicodeNameAliasKind.Abbreviation));
        Assert.That(properties.LookupName("byte order mark"), Is.EqualTo(0xFEFF));
        Assert.That(properties.LookupName("padding character"), Is.EqualTo(0x0080));
        Assert.That(properties.GetNameAliases(-1), Is.Empty);
        Assert.That(properties.GetNameAliases(0x110000), Is.Empty);
    }

    [Test]
    public void LookupNamedSequence_ReturnsSequenceByName()
    {
        Assert.That(
            new UnicodeProperties().LookupNamedSequence("LATIN CAPITAL LETTER A WITH MACRON AND GRAVE"),
            Is.EqualTo(new int[] { 0x0100, 0x0300 }));
        Assert.That(
            new UnicodeProperties().LookupNamedSequence("keycap number sign"),
            Is.EqualTo(new int[] { 0x0023, 0xFE0F, 0x20E3 }));
    }

    [Test]
    public void LookupNamedSequence_ReturnsEmptyArrayForUnknownSequence()
    {
        Assert.That(new UnicodeProperties().LookupNamedSequence("NO SUCH SEQUENCE"), Is.Empty);
    }

    [Test]
    public void LookupNamedSequence_RejectsNullName()
    {
        Assert.Throws<ArgumentNullException>(() => new UnicodeProperties().LookupNamedSequence(null!));
    }

    [Test]
    public void LookupNamedSequence_ReturnsIndependentArrays()
    {
        UnicodeProperties properties = new UnicodeProperties();
        int[] first = properties.LookupNamedSequence("KEYCAP NUMBER SIGN");
        first[0] = 0;

        Assert.That(
            properties.LookupNamedSequence("KEYCAP NUMBER SIGN"),
            Is.EqualTo(new int[] { 0x0023, 0xFE0F, 0x20E3 }));
    }

    [Test]
    public void NamedSequences_AreInspectableAndCallerOwned()
    {
        UnicodeProperties properties = new UnicodeProperties();
        UnicodeNamedSequence[] sequences = properties.GetNamedSequences();
        UnicodeNamedSequence sequence = Array.Find(
            sequences,
            value => value.Name == "KEYCAP NUMBER SIGN")!;

        Assert.That(sequence, Is.Not.Null);
        Assert.That(sequence.CodePoints, Is.EqualTo(new int[] { 0x0023, 0xFE0F, 0x20E3 }));
        sequence.Name = "changed";
        sequence.CodePoints[0] = 0;

        UnicodeNamedSequence[] second = properties.GetNamedSequences();
        UnicodeNamedSequence secondSequence = Array.Find(
            second,
            value => value.Name == "KEYCAP NUMBER SIGN")!;
        Assert.That(secondSequence, Is.Not.Null);
        Assert.That(secondSequence.CodePoints, Is.EqualTo(new int[] { 0x0023, 0xFE0F, 0x20E3 }));
    }

    [Test]
    public void NamedSequenceConstructor_CopiesItsCodePoints()
    {
        int[] codePoints = new int[] { 0x0041, 0x0300 };
        UnicodeNamedSequence sequence = new UnicodeNamedSequence("TEST SEQUENCE", codePoints);
        codePoints[0] = 0;

        Assert.That(sequence.Name, Is.EqualTo("TEST SEQUENCE"));
        Assert.That(sequence.CodePoints, Is.EqualTo(new int[] { 0x0041, 0x0300 }));
        Assert.Throws<ArgumentNullException>(() => new UnicodeNamedSequence(null!, new int[] { }));
        Assert.Throws<ArgumentNullException>(() => new UnicodeNamedSequence("TEST", null!));
    }

    [Test]
    public void PropertyAliases_ResolveWithUnicodeLooseMatching()
    {
        UnicodeProperties properties = new UnicodeProperties();

        Assert.That(properties.ResolvePropertyAlias("gc"), Is.EqualTo("General_Category"));
        Assert.That(properties.ResolvePropertyAlias(" GENERAL-category "), Is.EqualTo("General_Category"));
        Assert.That(properties.ResolvePropertyAlias("canonical combining class"), Is.EqualTo("Canonical_Combining_Class"));
        Assert.That(properties.ResolvePropertyAlias("sfc"), Is.EqualTo("Simple_Case_Folding"));
        Assert.That(properties.ResolvePropertyAlias("space"), Is.EqualTo("White_Space"));
        Assert.That(properties.ResolvePropertyAlias("\twhite _ space\r\n"), Is.EqualTo("White_Space"));
        Assert.That(properties.ResolvePropertyAlias("not-a-property"), Is.EqualTo(""));
        Assert.That(properties.ResolvePropertyAlias(""), Is.EqualTo(""));
        Assert.Throws<ArgumentNullException>(() => properties.ResolvePropertyAlias(null!));
    }

    [Test]
    public void PropertyValueAliases_ResolveWithinTheirProperty()
    {
        UnicodeProperties properties = new UnicodeProperties();

        Assert.That(properties.ResolvePropertyValueAlias("gc", "lu"), Is.EqualTo("Uppercase_Letter"));
        Assert.That(properties.ResolvePropertyValueAlias("General Category", "uppercase-letter"), Is.EqualTo("Uppercase_Letter"));
        Assert.That(properties.ResolvePropertyValueAlias("sc", "latin"), Is.EqualTo("Latin"));
        Assert.That(properties.ResolvePropertyValueAlias("ccc", "000.0"), Is.EqualTo("Not_Reordered"));
        Assert.That(properties.ResolvePropertyValueAlias("Alphabetic", "t"), Is.EqualTo("Yes"));
        Assert.That(properties.ResolvePropertyValueAlias("age", "v1-1"), Is.EqualTo("V1_1"));
        Assert.That(properties.ResolvePropertyValueAlias("ccc", "230.00"), Is.EqualTo("Above"));
        Assert.That(properties.ResolvePropertyValueAlias("gc", "latin"), Is.EqualTo(""));
        Assert.That(properties.ResolvePropertyValueAlias("unknown", "lu"), Is.EqualTo(""));
        Assert.That(properties.ResolvePropertyValueAlias("gc", ""), Is.EqualTo(""));
        Assert.Throws<ArgumentNullException>(() => properties.ResolvePropertyValueAlias(null!, "lu"));
        Assert.Throws<ArgumentNullException>(() => properties.ResolvePropertyValueAlias("gc", null!));
    }
}
