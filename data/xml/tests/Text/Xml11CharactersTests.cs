using xml;

namespace xml.Tests;

[TestFixture]
public class Xml11CharactersTests
{
    [Test]
    public void CharacterProductionIncludesXml11ControlsButExcludesNonScalars()
    {
        int[] legal = [0x1, 0x8, 0x9, 0xB, 0x1F, 0x20, 0x7F, 0x85, 0x9F, 0xD7FF, 0xE000, 0xFFFD, 0x10000, 0x10FFFF];
        int[] illegal = [-1, 0x0, 0xD800, 0xDFFF, 0xFFFE, 0xFFFF, 0x110000];

        foreach (int scalar in legal)
        {
            Assert.That(Xml11CharacterRules.IsLegalXml11Character(scalar), Is.True, "U+" + scalar.ToString("X"));
        }

        foreach (int scalar in illegal)
        {
            Assert.That(Xml11CharacterRules.IsLegalXml11Character(scalar), Is.False, "U+" + scalar.ToString("X"));
        }
    }

    [Test]
    public void RestrictedProductionUsesExactXml11ControlRanges()
    {
        int[] restricted = [0x1, 0x8, 0xB, 0xC, 0xE, 0x1F, 0x7F, 0x84, 0x86, 0x9F];
        int[] direct = [0x9, 0xA, 0xD, 0x20, 0x7E, 0x85, 0xA0, 0x10000];

        foreach (int scalar in restricted)
        {
            Assert.That(Xml11CharacterRules.IsRestrictedXml11Character(scalar), Is.True, "U+" + scalar.ToString("X"));
            Assert.That(Xml11CharacterRules.IsDirectXml11Character(scalar), Is.False, "U+" + scalar.ToString("X"));
        }

        foreach (int scalar in direct)
        {
            Assert.That(Xml11CharacterRules.IsRestrictedXml11Character(scalar), Is.False, "U+" + scalar.ToString("X"));
            Assert.That(Xml11CharacterRules.IsDirectXml11Character(scalar), Is.True, "U+" + scalar.ToString("X"));
        }
    }

    [Test]
    public void DirectTextValidationRequiresRestrictedControlsToBeReferenced()
    {
        string legal = "\t\n\r" + ((char)0x85) + ((char)0xA0) + char.ConvertFromUtf32(0x10000);
        Assert.That(() => Xml11CharacterRules.ValidateDirectXml11Characters(legal.AsMemory()), Throws.Nothing);

        Assert.That(() => Xml11CharacterRules.ValidateDirectXml11Characters("A\u0001".AsMemory()), Throws.TypeOf<FormatException>().With.Message.Contains("Restricted").And.Message.Contains("offset 1"));
        Assert.That(() => Xml11CharacterRules.ValidateDirectXml11Characters("\u007F".AsMemory()), Throws.TypeOf<FormatException>().With.Message.Contains("character reference"));
        Assert.That(() => Xml11CharacterRules.ValidateDirectXml11Characters("\0".AsMemory()), Throws.TypeOf<FormatException>().With.Message.Contains("Illegal"));
        Assert.That(() => Xml11CharacterRules.ValidateDirectXml11Characters("\uD800".AsMemory()), Throws.TypeOf<FormatException>().With.Message.Contains("surrogate"));
    }

    [Test]
    public void Xml11NameTablesMatchTheSecondEditionProductions()
    {
        int[] starts = [':', 'A', '_', 'z', 0xC0, 0x37F, 0x200C, 0x2C00, 0x3001, 0xF900, 0xFDF0, 0x10000, 0xEFFFF];
        int[] continuations = ['-', '.', '0', 0xB7, 0x300, 0x203F];
        int[] rejected = [' ', '/', 0x37E, 0xD800, 0xFFFE, 0xF0000];

        foreach (int scalar in starts)
        {
            Assert.That(Xml11CharacterRules.IsXml11NameStartCharacter(scalar), Is.True, "U+" + scalar.ToString("X"));
            Assert.That(Xml11CharacterRules.IsXml11NameCharacter(scalar), Is.True, "U+" + scalar.ToString("X"));
        }

        foreach (int scalar in continuations)
        {
            Assert.That(Xml11CharacterRules.IsXml11NameStartCharacter(scalar), Is.False, "U+" + scalar.ToString("X"));
            Assert.That(Xml11CharacterRules.IsXml11NameCharacter(scalar), Is.True, "U+" + scalar.ToString("X"));
        }

        foreach (int scalar in rejected)
        {
            Assert.That(Xml11CharacterRules.IsXml11NameStartCharacter(scalar), Is.False, "U+" + scalar.ToString("X"));
            Assert.That(Xml11CharacterRules.IsXml11NameCharacter(scalar), Is.False, "U+" + scalar.ToString("X"));
        }
    }

    [Test]
    public void CharacterReferencesPermitRestrictedXml11CharactersOnlyThroughReferences()
    {
        XmlCharacterReference one = Xml11CharacterReferenceParser.ParseXml11CharacterReference("&#1;".AsSpan(), 0);
        XmlCharacterReference delete = Xml11CharacterReferenceParser.ParseXml11CharacterReference("x&#x7F;y".AsSpan(), 1);
        XmlCharacterReference control = Xml11CharacterReferenceParser.ParseXml11CharacterReference("&#x9f;".AsSpan(), 0);
        XmlCharacterReference nextLine = Xml11CharacterReferenceParser.ParseXml11CharacterReference("&#x85;".AsSpan(), 0);
        XmlCharacterReference supplementary = Xml11CharacterReferenceParser.ParseXml11CharacterReference("&#x10000;".AsSpan(), 0);
        XmlCharacterReference xml10 = Xml10CharacterReferenceParser.ParseXml10CharacterReference("&#65;".AsSpan(), 0);

        AssertReference(one, 0x1, 4, true);
        AssertReference(delete, 0x7F, 6, true);
        AssertReference(control, 0x9F, 6, true);
        AssertReference(nextLine, 0x85, 6, false);
        AssertReference(supplementary, 0x10000, 9, false);
        Assert.That(xml10.RestrictedCharacter, Is.False);
        Assert.That(() => Xml10CharacterReferenceParser.ParseXml10CharacterReference("&#1;".AsSpan(), 0), Throws.TypeOf<FormatException>());
    }

    [Test]
    public void CharacterReferencesStillRejectNullSurrogatesAndMalformedSyntax()
    {
        string[] invalid = ["&#0;", "&#xD800;", "&#xDFFF;", "&#xFFFE;", "&#xFFFF;", "&#x110000;", "&#X41;", "&#x;", "&#12"];

        foreach (string reference in invalid)
        {
            Assert.That(() => Xml11CharacterReferenceParser.ParseXml11CharacterReference(reference.AsSpan(), 0), Throws.TypeOf<FormatException>(), reference);
        }
    }

    [Test]
    public void NormalizationCheckerReportsNonNfcTextWithoutChangingIt()
    {
        string text = "A e\u0301 Z";

        Xml11NormalizationResult result = Xml11NormalizationChecker.CheckXml11Normalization(text.AsMemory(), false);

        Assert.That(result.IsFullyNormalized, Is.False);
        Assert.That(result.IssueKind, Is.EqualTo(Xml11NormalizationIssueKind.NotNormalizationFormC));
        Assert.That(result.Utf16Offset, Is.EqualTo(3));
        Assert.That(result.Diagnostic, Is.Not.Null);
        Assert.That(result.Diagnostic!.Code, Is.EqualTo(XmlErrorCodes.GeneralFailure));
        Assert.That(result.Diagnostic.Category, Is.EqualTo(XmlErrorCategory.Xml));
        Assert.That(result.Diagnostic.Severity, Is.EqualTo(XmlDiagnosticSeverity.Warning));
        Assert.That(result.Diagnostic.Message, Does.Contain("Normalization Form C").And.Contain("offset 3"));
        Assert.That(text, Is.EqualTo("A e\u0301 Z"));
    }

    [Test]
    public void NormalizationCheckerRejectsComposingConstructStarts()
    {
        int[] composing = [0x301, 0x09BE, 0x1161, 0x11A8];
        foreach (int scalar in composing)
        {
            Assert.That(Xml11NormalizationChecker.IsXml11ComposingCharacter(scalar), Is.True, "U+" + scalar.ToString("X"));
            string text = char.ConvertFromUtf32(scalar);
            Xml11NormalizationResult result = Xml11NormalizationChecker.CheckXml11Normalization(text.AsMemory(), true);
            Assert.That(result.IsFullyNormalized, Is.False, "U+" + scalar.ToString("X"));
            Assert.That(result.IssueKind, Is.EqualTo(Xml11NormalizationIssueKind.ComposingCharacterAtConstructStart), "U+" + scalar.ToString("X"));
            Assert.That(result.Diagnostic!.Severity, Is.EqualTo(XmlDiagnosticSeverity.Warning));
        }

        Assert.That(Xml11NormalizationChecker.IsXml11ComposingCharacter('A'), Is.False);
        Assert.That(Xml11NormalizationChecker.IsXml11ComposingCharacter(0x1F600), Is.False);
        Assert.That(Xml11NormalizationChecker.CheckXml11Normalization("\u0301".AsMemory(), false).IsFullyNormalized, Is.True);
        Assert.That(Xml11NormalizationChecker.CheckXml11Normalization("\u00E9".AsMemory(), true).IsFullyNormalized, Is.True);
    }

    [Test]
    public void NormalizationCheckerRejectsIllegalScalarInput()
    {
        Assert.That(() => Xml11NormalizationChecker.CheckXml11Normalization("\0".AsMemory(), false), Throws.TypeOf<FormatException>());
        Assert.That(() => Xml11NormalizationChecker.CheckXml11Normalization("\uFFFE".AsMemory(), false), Throws.TypeOf<FormatException>());
        Assert.That(() => Xml11NormalizationChecker.CheckXml11Normalization("\uD800".AsMemory(), false), Throws.TypeOf<FormatException>());
    }

    private static void AssertReference(XmlCharacterReference reference, int scalar, int sourceCount, bool restricted)
    {
        Assert.That(reference.ScalarValue, Is.EqualTo(scalar));
        Assert.That(reference.SourceCharacterCount, Is.EqualTo(sourceCount));
        Assert.That(reference.ReplacementText, Is.EqualTo(char.ConvertFromUtf32(scalar)));
        Assert.That(reference.RestrictedCharacter, Is.EqualTo(restricted));
    }
}
