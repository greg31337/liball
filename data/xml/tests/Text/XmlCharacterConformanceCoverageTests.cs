using xml;

namespace xml.Tests;

[TestFixture]
public class XmlCharacterConformanceCoverageTests
{
    private static readonly int[] NameStartRanges =
    [
        0xC0, 0xD6,
        0xD8, 0xF6,
        0xF8, 0x2FF,
        0x370, 0x37D,
        0x37F, 0x1FFF,
        0x200C, 0x200D,
        0x2070, 0x218F,
        0x2C00, 0x2FEF,
        0x3001, 0xD7FF,
        0xF900, 0xFDCF,
        0xFDF0, 0xFFFD,
        0x10000, 0xEFFFF,
    ];

    private static readonly int[] NameContinuationRanges =
    [
        0x300, 0x36F,
        0x203F, 0x2040,
    ];

    [Test]
    public void CharacterClassifiersMatchEveryScalarInBothRecommendations()
    {
        List<string> mismatches = [];
        int scalar = -1;
        while (scalar <= 0x110000)
        {
            RecordMismatch(mismatches, "XML 1.0 Char", scalar, ExpectedXml10Character(scalar), Xml10CharacterRules.IsLegalXml10Character(scalar));
            RecordMismatch(mismatches, "XML 1.1 Char", scalar, ExpectedXml11Character(scalar), Xml11CharacterRules.IsLegalXml11Character(scalar));
            RecordMismatch(mismatches, "XML 1.1 RestrictedChar", scalar, ExpectedXml11RestrictedCharacter(scalar), Xml11CharacterRules.IsRestrictedXml11Character(scalar));
            RecordMismatch(mismatches, "XML 1.1 direct Char", scalar, ExpectedXml11Character(scalar) && !ExpectedXml11RestrictedCharacter(scalar), Xml11CharacterRules.IsDirectXml11Character(scalar));
            scalar = scalar + 1;
        }

        Assert.That(mismatches, Is.Empty, string.Join(Environment.NewLine, mismatches));
    }

    [Test]
    public void NameClassifiersMatchEveryScalarInBothRecommendations()
    {
        List<string> mismatches = [];
        int scalar = -1;
        while (scalar <= 0x110000)
        {
            bool expectedStart = ExpectedNameStartCharacter(scalar);
            bool expectedName = ExpectedNameCharacter(scalar);
            RecordMismatch(mismatches, "XML 1.0 NameStartChar", scalar, expectedStart, Xml10CharacterRules.IsXml10NameStartCharacter(scalar));
            RecordMismatch(mismatches, "XML 1.0 NameChar", scalar, expectedName, Xml10CharacterRules.IsXml10NameCharacter(scalar));
            RecordMismatch(mismatches, "XML 1.1 NameStartChar", scalar, expectedStart, Xml11CharacterRules.IsXml11NameStartCharacter(scalar));
            RecordMismatch(mismatches, "XML 1.1 NameChar", scalar, expectedName, Xml11CharacterRules.IsXml11NameCharacter(scalar));
            scalar = scalar + 1;
        }

        Assert.That(mismatches, Is.Empty, string.Join(Environment.NewLine, mismatches));
    }

    [Test]
    public void NameAndNcNameScannersHonorEveryProductionBoundary()
    {
        int[] boundaries = CreateNameBoundaryScalars();
        XmlVersion[] versions = [XmlVersion.Xml10, XmlVersion.Xml11];
        foreach (XmlVersion version in versions)
        {
            foreach (int scalar in boundaries)
            {
                if (!IsUnicodeScalar(scalar))
                {
                    continue;
                }

                string character = char.ConvertFromUtf32(scalar);
                XmlLexicalScanner nameScanner = new XmlLexicalScanner((character + "tail ").AsMemory(), version);
                if (ExpectedNameStartCharacter(scalar))
                {
                    Assert.That(nameScanner.ScanXmlName().Value, Is.EqualTo(character + "tail"), VersionScalarMessage(version, scalar));
                }
                else
                {
                    Assert.Throws<FormatException>(() => nameScanner.ScanXmlName(), VersionScalarMessage(version, scalar));
                }

                XmlLexicalScanner ncNameScanner = new XmlLexicalScanner((character + "tail ").AsMemory(), version);
                if (ExpectedNameStartCharacter(scalar) && scalar != ':')
                {
                    Assert.That(ncNameScanner.ScanXmlNcName().Value, Is.EqualTo(character + "tail"), VersionScalarMessage(version, scalar));
                }
                else
                {
                    Assert.Throws<FormatException>(() => ncNameScanner.ScanXmlNcName(), VersionScalarMessage(version, scalar));
                }
            }
        }
    }

    [Test]
    public void NameAndNmtokenContinuationScannersHonorEveryProductionBoundary()
    {
        int[] boundaries = CreateNameBoundaryScalars();
        XmlVersion[] versions = [XmlVersion.Xml10, XmlVersion.Xml11];
        foreach (XmlVersion version in versions)
        {
            foreach (int scalar in boundaries)
            {
                if (!IsUnicodeScalar(scalar))
                {
                    continue;
                }

                string character = char.ConvertFromUtf32(scalar);
                XmlLexicalScanner nameScanner = new XmlLexicalScanner(("A" + character + " ").AsMemory(), version);
                XmlLexicalToken name = nameScanner.ScanXmlName();
                if (ExpectedNameCharacter(scalar))
                {
                    Assert.That(name.Value, Is.EqualTo("A" + character), VersionScalarMessage(version, scalar));
                    XmlLexicalScanner tokenScanner = new XmlLexicalScanner((character + " ").AsMemory(), version);
                    Assert.That(tokenScanner.ScanXmlNmtoken().Value, Is.EqualTo(character), VersionScalarMessage(version, scalar));
                }
                else
                {
                    Assert.That(name.Value, Is.EqualTo("A"), VersionScalarMessage(version, scalar));
                    XmlLexicalScanner tokenScanner = new XmlLexicalScanner((character + " ").AsMemory(), version);
                    Assert.Throws<FormatException>(() => tokenScanner.ScanXmlNmtoken(), VersionScalarMessage(version, scalar));
                }
            }
        }
    }

    [Test]
    public void QNameScannerHonorsEveryNcNameStartBoundaryInBothParts()
    {
        int[] boundaries = CreateNameBoundaryScalars();
        XmlVersion[] versions = [XmlVersion.Xml10, XmlVersion.Xml11];
        foreach (XmlVersion version in versions)
        {
            foreach (int scalar in boundaries)
            {
                if (!IsUnicodeScalar(scalar))
                {
                    continue;
                }

                string character = char.ConvertFromUtf32(scalar);
                bool validNcStart = ExpectedNameStartCharacter(scalar) && scalar != ':';
                XmlLexicalScanner prefixScanner = new XmlLexicalScanner((character + "p:local ").AsMemory(), version);
                XmlLexicalScanner localScanner = new XmlLexicalScanner(("p:" + character + "local ").AsMemory(), version);
                if (validNcStart)
                {
                    XmlQualifiedNameToken prefixName = prefixScanner.ScanXmlQualifiedName();
                    XmlQualifiedNameToken localName = localScanner.ScanXmlQualifiedName();
                    Assert.That(prefixName.Prefix, Is.EqualTo(character + "p"), VersionScalarMessage(version, scalar));
                    Assert.That(prefixName.LocalName, Is.EqualTo("local"), VersionScalarMessage(version, scalar));
                    Assert.That(localName.Prefix, Is.EqualTo("p"), VersionScalarMessage(version, scalar));
                    Assert.That(localName.LocalName, Is.EqualTo(character + "local"), VersionScalarMessage(version, scalar));
                }
                else
                {
                    Assert.Throws<FormatException>(() => prefixScanner.ScanXmlQualifiedName(), VersionScalarMessage(version, scalar));
                    Assert.Throws<FormatException>(() => localScanner.ScanXmlQualifiedName(), VersionScalarMessage(version, scalar));
                }
            }
        }
    }

    [Test]
    public void DecimalAndHexReferencesHonorEveryCharacterRangeBoundary()
    {
        int[] boundaries =
        [
            0x0, 0x1, 0x8, 0x9, 0xA, 0xB, 0xC, 0xD, 0xE, 0x1F, 0x20,
            0x7E, 0x7F, 0x84, 0x85, 0x86, 0x9F, 0xA0,
            0xD7FF, 0xD800, 0xDFFF, 0xE000, 0xFFFD, 0xFFFE, 0xFFFF,
            0x10000, 0x10FFFF, 0x110000,
        ];
        XmlVersion[] versions = [XmlVersion.Xml10, XmlVersion.Xml11];
        foreach (XmlVersion version in versions)
        {
            foreach (int scalar in boundaries)
            {
                AssertCharacterReferenceBoundary(scalar, false, version);
                AssertCharacterReferenceBoundary(scalar, true, version);
            }
        }
    }

    [Test]
    public void Xml11RestrictedCharactersAreRejectedDirectlyAndAcceptedByReference()
    {
        int scalar = 0;
        while (scalar <= 0x9F)
        {
            if (ExpectedXml11RestrictedCharacter(scalar))
            {
                string character = char.ConvertFromUtf32(scalar);
                Assert.Throws<FormatException>(() => Xml11CharacterRules.ValidateDirectXml11Characters(character.AsMemory()), "direct U+" + scalar.ToString("X"));
                XmlCharacterReference reference = Xml11CharacterReferenceParser.ParseXml11CharacterReference(("&#x" + scalar.ToString("X") + ";").AsSpan(), 0);
                Assert.That(reference.ScalarValue, Is.EqualTo(scalar));
                Assert.That(reference.RestrictedCharacter, Is.True, "reference U+" + scalar.ToString("X"));
            }

            scalar = scalar + 1;
        }
    }

    [Test]
    public void NoncharactersFollowXmlCharacterAndNameProductions()
    {
        int scalar = 0xFDD0;
        while (scalar <= 0xFDEF)
        {
            Assert.That(Xml10CharacterRules.IsLegalXml10Character(scalar), Is.True, "XML 1.0 U+" + scalar.ToString("X"));
            Assert.That(Xml11CharacterRules.IsLegalXml11Character(scalar), Is.True, "XML 1.1 U+" + scalar.ToString("X"));
            Assert.That(Xml10CharacterRules.IsXml10NameCharacter(scalar), Is.False, "Name U+" + scalar.ToString("X"));
            scalar = scalar + 1;
        }

        int plane = 0;
        while (plane <= 16)
        {
            int penultimate = plane * 0x10000 + 0xFFFE;
            int ultimate = plane * 0x10000 + 0xFFFF;
            bool characterAllowed = plane > 0;
            bool nameAllowed = plane > 0 && plane <= 14;
            Assert.That(Xml10CharacterRules.IsLegalXml10Character(penultimate), Is.EqualTo(characterAllowed), "XML 1.0 U+" + penultimate.ToString("X"));
            Assert.That(Xml10CharacterRules.IsLegalXml10Character(ultimate), Is.EqualTo(characterAllowed), "XML 1.0 U+" + ultimate.ToString("X"));
            Assert.That(Xml11CharacterRules.IsLegalXml11Character(penultimate), Is.EqualTo(characterAllowed), "XML 1.1 U+" + penultimate.ToString("X"));
            Assert.That(Xml11CharacterRules.IsLegalXml11Character(ultimate), Is.EqualTo(characterAllowed), "XML 1.1 U+" + ultimate.ToString("X"));
            Assert.That(Xml10CharacterRules.IsXml10NameStartCharacter(penultimate), Is.EqualTo(nameAllowed), "Name U+" + penultimate.ToString("X"));
            Assert.That(Xml11CharacterRules.IsXml11NameStartCharacter(ultimate), Is.EqualTo(nameAllowed), "Name U+" + ultimate.ToString("X"));
            if (characterAllowed)
            {
                XmlCharacterReference reference = Xml10CharacterReferenceParser.ParseXml10CharacterReference(("&#x" + penultimate.ToString("X") + ";").AsSpan(), 0);
                Assert.That(reference.ScalarValue, Is.EqualTo(penultimate));
            }

            plane = plane + 1;
        }
    }

    [Test]
    public void ScalarScannerRejectsEveryIndividualSurrogateCodeUnit()
    {
        int codeUnit = 0xD800;
        while (codeUnit <= 0xDFFF)
        {
            string text = new string((char)codeUnit, 1);
            Assert.Throws<FormatException>(() => new UnicodeScalarScanner(text.AsMemory()).ReadUnicodeScalar(), "U+" + codeUnit.ToString("X"));
            codeUnit = codeUnit + 1;
        }
    }

    [TestCase(0xD800, 0xDC00, 0x10000)]
    [TestCase(0xD800, 0xDFFF, 0x103FF)]
    [TestCase(0xDBFF, 0xDC00, 0x10FC00)]
    [TestCase(0xDBFF, 0xDFFF, 0x10FFFF)]
    public void ScalarScannerDecodesEverySurrogatePairBoundary(int high, int low, int expected)
    {
        string text = new string([(char)high, (char)low]);

        UnicodeScalar scalar = new UnicodeScalarScanner(text.AsMemory()).ReadUnicodeScalar();

        Assert.That(scalar.Value, Is.EqualTo(expected));
        Assert.That(scalar.Utf16Offset, Is.Zero);
        Assert.That(scalar.Utf16CodeUnitCount, Is.EqualTo(2));
    }

    private static void AssertCharacterReferenceBoundary(int scalar, bool hexadecimal, XmlVersion version)
    {
        string number = hexadecimal ? scalar.ToString("X") : scalar.ToString();
        string text = hexadecimal ? "&#x" + number + ";" : "&#" + number + ";";
        bool expected = version == XmlVersion.Xml10 ? ExpectedXml10Character(scalar) : ExpectedXml11Character(scalar);
        if (expected)
        {
            XmlCharacterReference reference;
            if (version == XmlVersion.Xml10)
            {
                reference = Xml10CharacterReferenceParser.ParseXml10CharacterReference(text.AsSpan(), 0);
            }
            else
            {
                reference = Xml11CharacterReferenceParser.ParseXml11CharacterReference(text.AsSpan(), 0);
            }

            Assert.That(reference.ScalarValue, Is.EqualTo(scalar), VersionScalarMessage(version, scalar));
            Assert.That(reference.ReplacementText, Is.EqualTo(char.ConvertFromUtf32(scalar)), VersionScalarMessage(version, scalar));
            Assert.That(reference.SourceCharacterCount, Is.EqualTo(text.Length), VersionScalarMessage(version, scalar));
            Assert.That(reference.RestrictedCharacter, Is.EqualTo(version == XmlVersion.Xml11 && ExpectedXml11RestrictedCharacter(scalar)), VersionScalarMessage(version, scalar));
        }
        else if (version == XmlVersion.Xml10)
        {
            Assert.Throws<FormatException>(() => Xml10CharacterReferenceParser.ParseXml10CharacterReference(text.AsSpan(), 0), VersionScalarMessage(version, scalar));
        }
        else
        {
            Assert.Throws<FormatException>(() => Xml11CharacterReferenceParser.ParseXml11CharacterReference(text.AsSpan(), 0), VersionScalarMessage(version, scalar));
        }
    }

    private static bool ExpectedXml10Character(int scalar)
    {
        return scalar == 0x9 || scalar == 0xA || scalar == 0xD || scalar >= 0x20 && scalar <= 0xD7FF || scalar >= 0xE000 && scalar <= 0xFFFD || scalar >= 0x10000 && scalar <= 0x10FFFF;
    }

    private static bool ExpectedXml11Character(int scalar)
    {
        return scalar >= 0x1 && scalar <= 0xD7FF || scalar >= 0xE000 && scalar <= 0xFFFD || scalar >= 0x10000 && scalar <= 0x10FFFF;
    }

    private static bool ExpectedXml11RestrictedCharacter(int scalar)
    {
        return scalar >= 0x1 && scalar <= 0x8 || scalar >= 0xB && scalar <= 0xC || scalar >= 0xE && scalar <= 0x1F || scalar >= 0x7F && scalar <= 0x84 || scalar >= 0x86 && scalar <= 0x9F;
    }

    private static bool ExpectedNameStartCharacter(int scalar)
    {
        if (scalar == ':' || scalar == '_' || scalar >= 'A' && scalar <= 'Z' || scalar >= 'a' && scalar <= 'z')
        {
            return true;
        }

        return InRanges(scalar, NameStartRanges);
    }

    private static bool ExpectedNameCharacter(int scalar)
    {
        if (ExpectedNameStartCharacter(scalar) || scalar == '-' || scalar == '.' || scalar >= '0' && scalar <= '9' || scalar == 0xB7)
        {
            return true;
        }

        return InRanges(scalar, NameContinuationRanges);
    }

    private static bool InRanges(int scalar, int[] ranges)
    {
        int index = 0;
        while (index < ranges.Length)
        {
            if (scalar >= ranges[index] && scalar <= ranges[index + 1])
            {
                return true;
            }

            index = index + 2;
        }

        return false;
    }

    private static int[] CreateNameBoundaryScalars()
    {
        HashSet<int> boundaries = [-1, 0, ':', '_', 'A', 'Z', 'a', 'z', '-', '.', '0', '9', 0xB7, 0xD800, 0xDFFF, 0xFFFE, 0xFFFF, 0x110000];
        AddRangeBoundaries(boundaries, NameStartRanges);
        AddRangeBoundaries(boundaries, NameContinuationRanges);
        int[] result = [.. boundaries];
        Array.Sort(result);
        return result;
    }

    private static void AddRangeBoundaries(HashSet<int> boundaries, int[] ranges)
    {
        int index = 0;
        while (index < ranges.Length)
        {
            int start = ranges[index];
            int end = ranges[index + 1];
            boundaries.Add(start - 1);
            boundaries.Add(start);
            boundaries.Add(start + 1);
            boundaries.Add(end - 1);
            boundaries.Add(end);
            boundaries.Add(end + 1);
            index = index + 2;
        }
    }

    private static bool IsUnicodeScalar(int scalar)
    {
        return scalar >= 0 && scalar <= 0x10FFFF && (scalar < 0xD800 || scalar > 0xDFFF);
    }

    private static void RecordMismatch(List<string> mismatches, string production, int scalar, bool expected, bool actual)
    {
        if (expected != actual && mismatches.Count < 20)
        {
            mismatches.Add(production + " U+" + scalar.ToString("X") + ": expected " + expected + ", actual " + actual);
        }
    }

    private static string VersionScalarMessage(XmlVersion version, int scalar)
    {
        return version + " U+" + scalar.ToString("X");
    }
}
