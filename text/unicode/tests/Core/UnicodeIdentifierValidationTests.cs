using NUnit.Framework;

namespace unicode.Tests;

[TestFixture]
public class UnicodeIdentifierValidationTests
{
    [Test]
    public void IsIdentifier_UsesDefaultProfile()
    {
        Assert.That(new UnicodeIdentifiers().IsIdentifier("A0_a\u0301"), Is.True);
        Assert.That(new UnicodeIdentifiers().IsIdentifier("0A"), Is.False);
    }

    [Test]
    public void ValidateIdentifier_ReturnsValidResultForDefaultIdentifier()
    {
        UnicodeIdentifierValidationResult result = new UnicodeIdentifiers().ValidateIdentifier("A0_a\u0301", new UnicodeProfiles().CreateDefaultIdentifier());

        Assert.That(result.IsValid, Is.True);
        Assert.That(result.Status, Is.EqualTo(UnicodeIdentifierStatus.Valid));
        Assert.That(result.Diagnostics, Is.Empty);
        Assert.That(result.Scalars, Has.Length.EqualTo(5));
    }

    [Test]
    public void ValidateIdentifier_ReportsEmptyIdentifierAtOffsetZero()
    {
        UnicodeIdentifierValidationResult result = new UnicodeIdentifiers().ValidateIdentifier("", new UnicodeProfiles().CreateDefaultIdentifier());

        Assert.That(result.IsValid, Is.False);
        Assert.That(result.Status, Is.EqualTo(UnicodeIdentifierStatus.Empty));
        Assert.That(result.Diagnostics, Has.Length.EqualTo(1));
        AssertDiagnostic(result.Diagnostics[0], 0, 0, -1, UnicodeGeneralCategory.OtherNotAssigned, UnicodeIdentifierStatus.Empty, UnicodeIdentifiers.EmptyRule);
    }

    [Test]
    public void ValidateIdentifier_ReportsInvalidStartAndContinueOffsets()
    {
        UnicodeIdentifierValidationResult result = new UnicodeIdentifiers().ValidateIdentifier("0A\U0001F600", new UnicodeProfiles().CreateDefaultIdentifier());

        Assert.That(result.IsValid, Is.False);
        Assert.That(result.Status, Is.EqualTo(UnicodeIdentifierStatus.InvalidStart));
        Assert.That(FindDiagnostic(result, UnicodeIdentifiers.InvalidStartRule).Utf16Index, Is.EqualTo(0));
        AssertDiagnostic(
            FindDiagnostic(result, UnicodeIdentifiers.InvalidContinueRule),
            2,
            2,
            0x1F600,
            UnicodeGeneralCategory.OtherSymbol,
            UnicodeIdentifierStatus.InvalidContinue,
            UnicodeIdentifiers.InvalidContinueRule);
    }

    [Test]
    public void ValidateIdentifier_ReportsPatternSyntaxAndPatternWhiteSpaceRuleCodes()
    {
        UnicodeIdentifierValidationResult result = new UnicodeIdentifiers().ValidateIdentifier("A !", new UnicodeProfiles().CreateDefaultIdentifier());

        Assert.That(result.IsValid, Is.False);
        AssertDiagnostic(
            FindDiagnostic(result, UnicodeIdentifiers.PatternWhiteSpaceRule),
            1,
            1,
            0x0020,
            UnicodeGeneralCategory.SpaceSeparator,
            UnicodeIdentifierStatus.Disallowed,
            UnicodeIdentifiers.PatternWhiteSpaceRule);
        AssertDiagnostic(
            FindDiagnostic(result, UnicodeIdentifiers.PatternSyntaxRule),
            2,
            1,
            0x0021,
            UnicodeGeneralCategory.OtherPunctuation,
            UnicodeIdentifierStatus.Disallowed,
            UnicodeIdentifiers.PatternSyntaxRule);
    }

    [Test]
    public void ValidateIdentifier_ReportsNormalizationRuleForNormalizedProfile()
    {
        UnicodeIdentifierValidationResult result = new UnicodeIdentifiers().ValidateIdentifier("A\u0300", new UnicodeProfiles().CreateNormalizedIdentifier());

        Assert.That(result.IsValid, Is.False);
        Assert.That(result.Status, Is.EqualTo(UnicodeIdentifierStatus.NeedsNormalization));
        AssertDiagnostic(
            FindDiagnostic(result, UnicodeIdentifiers.NeedsNormalizationRule),
            0,
            2,
            -1,
            UnicodeGeneralCategory.OtherNotAssigned,
            UnicodeIdentifierStatus.NeedsNormalization,
            UnicodeIdentifiers.NeedsNormalizationRule);
    }

    [Test]
    public void ValidateIdentifier_ReportsInvalidUtf16ScalarInput()
    {
        UnicodeIdentifierValidationResult result = new UnicodeIdentifiers().ValidateIdentifier("\uD800", new UnicodeProfiles().CreateDefaultIdentifier());

        Assert.That(result.IsValid, Is.False);
        Assert.That(result.Status, Is.EqualTo(UnicodeIdentifierStatus.InvalidScalar));
        AssertDiagnostic(
            result.Diagnostics[0],
            0,
            1,
            UnicodeScalar.ReplacementCharacter,
            UnicodeGeneralCategory.OtherSymbol,
            UnicodeIdentifierStatus.InvalidScalar,
            UnicodeIdentifiers.InvalidScalarRule);
    }

    [Test]
    public void ValidateIdentifier_ImmutableProfileAllowsUnassignedCodePoints()
    {
        UnicodeIdentifierValidationResult result = new UnicodeIdentifiers().ValidateIdentifier("\U00010000", new UnicodeProfiles().CreateImmutableIdentifier());

        Assert.That(result.IsValid, Is.True);
    }

    [Test]
    public void ValidateIdentifier_RejectsNullArguments()
    {
        Assert.Throws<ArgumentNullException>(() => new UnicodeIdentifiers().IsIdentifier(null!));
        Assert.Throws<ArgumentNullException>(() => new UnicodeIdentifiers().ValidateIdentifier(null!, new UnicodeProfiles().CreateDefaultIdentifier()));
        Assert.Throws<ArgumentNullException>(() => new UnicodeIdentifiers().ValidateIdentifier("A", null!));
    }

    static UnicodeIdentifierDiagnostic FindDiagnostic(UnicodeIdentifierValidationResult result, string rule)
    {
        int index = 0;

        while (index < result.Diagnostics.Length)
        {
            if (result.Diagnostics[index].Rule == rule)
            {
                return result.Diagnostics[index];
            }

            index = index + 1;
        }

        Assert.Fail("Missing diagnostic rule " + rule);
        return result.Diagnostics[0];
    }

    static void AssertDiagnostic(
        UnicodeIdentifierDiagnostic diagnostic,
        int utf16Index,
        int utf16Length,
        int codePoint,
        UnicodeGeneralCategory generalCategory,
        UnicodeIdentifierStatus status,
        string rule)
    {
        Assert.That(diagnostic.Utf16Index, Is.EqualTo(utf16Index));
        Assert.That(diagnostic.Utf16Length, Is.EqualTo(utf16Length));
        Assert.That(diagnostic.CodePoint, Is.EqualTo(codePoint));
        Assert.That(diagnostic.GeneralCategory, Is.EqualTo(generalCategory));
        Assert.That(diagnostic.Status, Is.EqualTo(status));
        Assert.That(diagnostic.Severity, Is.EqualTo(UnicodeDiagnosticSeverity.Error));
        Assert.That(diagnostic.Rule, Is.EqualTo(rule));
        Assert.That(diagnostic.Message, Is.Not.Empty);
    }
}
