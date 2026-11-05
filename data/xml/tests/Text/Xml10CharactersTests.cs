using xml;

namespace xml.Tests;

[TestFixture]
public class Xml10CharactersTests
{
    [Test]
    public void CharacterProductionAcceptsEveryRangeBoundary()
    {
        int[] legal = [0x9, 0xA, 0xD, 0x20, 0x21, 0xD7FF, 0xE000, 0xFFFD, 0x10000, 0x1FFFE, 0x10FFFF];
        int[] illegal = [-1, 0x0, 0x8, 0xB, 0xC, 0xE, 0x1F, 0xD800, 0xDFFF, 0xFFFE, 0xFFFF, 0x110000];

        foreach (int scalar in legal)
        {
            Assert.That(Xml10CharacterRules.IsLegalXml10Character(scalar), Is.True, "U+" + scalar.ToString("X"));
        }

        foreach (int scalar in illegal)
        {
            Assert.That(Xml10CharacterRules.IsLegalXml10Character(scalar), Is.False, "U+" + scalar.ToString("X"));
        }
    }

    [Test]
    public void NameStartProductionUsesXml10FifthEditionRanges()
    {
        int[] allowed = [':', 'A', 'Z', '_', 'a', 'z', 0xC0, 0xD6, 0xD8, 0xF6, 0xF8, 0x2FF, 0x370, 0x37D, 0x37F, 0x1FFF, 0x200C, 0x200D, 0x2070, 0x218F, 0x2C00, 0x2FEF, 0x3001, 0xD7FF, 0xF900, 0xFDCF, 0xFDF0, 0xFFFD, 0x10000, 0xEFFFF];
        int[] rejected = [-1, '-', '.', '0', 0xB7, 0xD7, 0xF7, 0x300, 0x36F, 0x37E, 0x2000, 0x200B, 0x200E, 0x206F, 0x2190, 0x2BFF, 0x2FF0, 0x3000, 0xD800, 0xFDD0, 0xFDEF, 0xFFFE, 0xF0000, 0x10FFFF];

        foreach (int scalar in allowed)
        {
            Assert.That(Xml10CharacterRules.IsXml10NameStartCharacter(scalar), Is.True, "U+" + scalar.ToString("X"));
            Assert.That(Xml10CharacterRules.IsXml10NameCharacter(scalar), Is.True, "U+" + scalar.ToString("X"));
        }

        foreach (int scalar in rejected)
        {
            Assert.That(Xml10CharacterRules.IsXml10NameStartCharacter(scalar), Is.False, "U+" + scalar.ToString("X"));
        }
    }

    [Test]
    public void NameCharacterProductionAddsDigitsPunctuationAndCombiningRanges()
    {
        int[] added = ['-', '.', '0', '9', 0xB7, 0x300, 0x36F, 0x203F, 0x2040];
        int[] rejected = [' ', '/', 0xB6, 0x203E, 0x2041, 0xD800, 0xF0000];

        foreach (int scalar in added)
        {
            Assert.That(Xml10CharacterRules.IsXml10NameCharacter(scalar), Is.True, "U+" + scalar.ToString("X"));
            Assert.That(Xml10CharacterRules.IsXml10NameStartCharacter(scalar), Is.False, "U+" + scalar.ToString("X"));
        }

        foreach (int scalar in rejected)
        {
            Assert.That(Xml10CharacterRules.IsXml10NameCharacter(scalar), Is.False, "U+" + scalar.ToString("X"));
        }
    }

    [Test]
    public void ScalarScannerPreservesSupplementaryCharactersAndUtf16Offsets()
    {
        string text = "A" + char.ConvertFromUtf32(0x1F600) + char.ConvertFromUtf32(0xEFFFF);
        UnicodeScalarScanner scanner = new UnicodeScalarScanner(text.AsMemory());

        UnicodeScalar first = scanner.ReadUnicodeScalar();
        UnicodeScalar second = scanner.ReadUnicodeScalar();
        UnicodeScalar third = scanner.ReadUnicodeScalar();
        UnicodeScalar end = scanner.ReadUnicodeScalar();

        AssertScalar(first, 'A', 0, 1, false);
        AssertScalar(second, 0x1F600, 1, 2, false);
        AssertScalar(third, 0xEFFFF, 3, 2, false);
        AssertScalar(end, 0, 5, 0, true);
        Assert.That(scanner.CurrentUtf16Offset(), Is.EqualTo(5));
        Assert.That(scanner.IsUnicodeScalarEnd(), Is.True);

        scanner.ResetUnicodeScalarScanner();
        Assert.That(scanner.CurrentUtf16Offset(), Is.Zero);
        Assert.That(scanner.ReadUnicodeScalar().Value, Is.EqualTo('A'));
    }

    [Test]
    public void ScalarScannerRejectsEveryUnpairedSurrogateShape()
    {
        Assert.That(() => ReadFirstScalar("\uD800"), Throws.TypeOf<FormatException>().With.Message.Contains("offset 0"));
        Assert.That(() => ReadFirstScalar("\uDBFFA"), Throws.TypeOf<FormatException>().With.Message.Contains("offset 0"));
        Assert.That(() => ReadFirstScalar("\uDC00"), Throws.TypeOf<FormatException>().With.Message.Contains("offset 0"));
        Assert.That(() => ReadSecondScalar("A\uDFFF"), Throws.TypeOf<FormatException>().With.Message.Contains("offset 1"));
    }

    [Test]
    public void Xml10TextValidationUsesScalarBoundariesAndReportsOffsets()
    {
        string legal = "\t\n\r text " + char.ConvertFromUtf32(0x1FFFE) + char.ConvertFromUtf32(0x10FFFF);
        Assert.That(() => Xml10CharacterRules.ValidateXml10Characters(legal.AsMemory()), Throws.Nothing);

        Assert.That(() => Xml10CharacterRules.ValidateXml10Characters("A\0B".AsMemory()), Throws.TypeOf<FormatException>().With.Message.Contains("U+0").And.Message.Contains("offset 1"));
        Assert.That(() => Xml10CharacterRules.ValidateXml10Characters("A\uFFFE".AsMemory()), Throws.TypeOf<FormatException>().With.Message.Contains("U+FFFE").And.Message.Contains("offset 1"));
        Assert.That(() => Xml10CharacterRules.ValidateXml10Characters("A\uD800".AsMemory()), Throws.TypeOf<FormatException>().With.Message.Contains("offset 1"));
    }

    [Test]
    public void CharacterReferencesDecodeDecimalHexAndSupplementaryValues()
    {
        XmlCharacterReference decimalReference = Xml10CharacterReferenceParser.ParseXml10CharacterReference("x&#65;y".AsSpan(), 1);
        XmlCharacterReference hexReference = Xml10CharacterReferenceParser.ParseXml10CharacterReference("&#x1f600;rest".AsSpan(), 0);
        XmlCharacterReference tabReference = Xml10CharacterReferenceParser.ParseXml10CharacterReference("&#9;".AsSpan(), 0);
        XmlCharacterReference maximumReference = Xml10CharacterReferenceParser.ParseXml10CharacterReference("&#x10FFFF;".AsSpan(), 0);
        XmlCharacterReference discouragedReference = Xml10CharacterReferenceParser.ParseXml10CharacterReference("&#x1FFFE;".AsSpan(), 0);

        AssertReference(decimalReference, 65, 5, "A");
        AssertReference(hexReference, 0x1F600, 9, char.ConvertFromUtf32(0x1F600));
        AssertReference(tabReference, 0x9, 4, "\t");
        AssertReference(maximumReference, 0x10FFFF, 10, char.ConvertFromUtf32(0x10FFFF));
        AssertReference(discouragedReference, 0x1FFFE, 9, char.ConvertFromUtf32(0x1FFFE));
    }

    [Test]
    public void CharacterReferencesRejectMalformedOverflowAndIllegalValues()
    {
        string[] malformed = ["&amp;", "&#;", "&#x;", "&#X41;", "&#12", "&#-1;", "&#xG;", "&# 9;", "&#1114112;", "&#999999999999999999999;"];
        string[] illegal = ["&#0;", "&#8;", "&#11;", "&#31;", "&#55296;", "&#57343;", "&#65534;", "&#65535;", "&#x110000;"];

        foreach (string reference in malformed)
        {
            Assert.That(() => Xml10CharacterReferenceParser.ParseXml10CharacterReference(reference.AsSpan(), 0), Throws.TypeOf<FormatException>(), reference);
        }

        foreach (string reference in illegal)
        {
            Assert.That(() => Xml10CharacterReferenceParser.ParseXml10CharacterReference(reference.AsSpan(), 0), Throws.TypeOf<FormatException>(), reference);
        }

        Assert.That(() => Xml10CharacterReferenceParser.ParseXml10CharacterReference("&#65;".AsSpan(), -1), Throws.TypeOf<ArgumentOutOfRangeException>());
        Assert.That(() => Xml10CharacterReferenceParser.ParseXml10CharacterReference("abc".AsSpan(), 3), Throws.TypeOf<FormatException>());
    }

    private static int ReadFirstScalar(string text)
    {
        return new UnicodeScalarScanner(text.AsMemory()).ReadUnicodeScalar().Value;
    }

    private static int ReadSecondScalar(string text)
    {
        UnicodeScalarScanner scanner = new UnicodeScalarScanner(text.AsMemory());
        scanner.ReadUnicodeScalar();
        return scanner.ReadUnicodeScalar().Value;
    }

    private static void AssertScalar(UnicodeScalar scalar, int value, int offset, int codeUnitCount, bool endOfInput)
    {
        Assert.That(scalar.Value, Is.EqualTo(value));
        Assert.That(scalar.Utf16Offset, Is.EqualTo(offset));
        Assert.That(scalar.Utf16CodeUnitCount, Is.EqualTo(codeUnitCount));
        Assert.That(scalar.EndOfInput, Is.EqualTo(endOfInput));
    }

    private static void AssertReference(XmlCharacterReference reference, int scalar, int sourceCount, string replacement)
    {
        Assert.That(reference.ScalarValue, Is.EqualTo(scalar));
        Assert.That(reference.SourceCharacterCount, Is.EqualTo(sourceCount));
        Assert.That(reference.ReplacementText, Is.EqualTo(replacement));
    }
}
