using NUnit.Framework;

namespace unicode.Tests;

[TestFixture]
public class UnicodePrecisProfilePreparationTests
{
    [Test]
    public void UsernameCaseMapped_ProfileAllowsDigitLedPreparedNames()
    {
        UnicodePrecisProfile profile = new UnicodeProfiles().CreateUsernameCaseMapped();

        UnicodePrecisValidationResult result = new UnicodePrecis().Validate("2a_", profile);

        Assert.That(result.IsValid, Is.True);
        Assert.That(result.Status, Is.EqualTo(UnicodePrecisStatus.Valid));
        Assert.That(result.PreparedText, Is.EqualTo("2a_"));
        Assert.That(new UnicodePrecis().IsPrepared("2a_", profile), Is.True);
    }

    [Test]
    public void UsernameCaseMapped_ProfileReportsPreparationBeforePreparedValidationPasses()
    {
        UnicodePrecisProfile profile = new UnicodeProfiles().CreateUsernameCaseMapped();

        UnicodePrecisValidationResult raw = new UnicodePrecis().Validate("\uFF21", profile);
        UnicodePrecisValidationResult prepared = new UnicodePrecis().Validate(raw.PreparedText, profile);

        Assert.That(raw.IsValid, Is.False);
        Assert.That(raw.Status, Is.EqualTo(UnicodePrecisStatus.NeedsPreparation));
        Assert.That(raw.PreparedText, Is.EqualTo("a"));
        Assert.That(prepared.IsValid, Is.True);
        Assert.That(new UnicodePrecis().IsPrepared("\uFF21", profile), Is.False);
    }

    [Test]
    public void UsernameCaseMapped_ProfileAllowsPrintableAsciiAndRejectsWhitespace()
    {
        UnicodePrecisProfile profile = new UnicodeProfiles().CreateUsernameCaseMapped();

        UnicodePrecisValidationResult syntax = new UnicodePrecis().Validate("a!", profile);
        UnicodePrecisValidationResult whiteSpace = new UnicodePrecis().Validate("a\tb", profile);

        Assert.That(syntax.IsValid, Is.True);

        Assert.That(whiteSpace.Status, Is.EqualTo(UnicodePrecisStatus.DisallowedControl));
        AssertDiagnostic(
            FindDiagnostic(whiteSpace, UnicodePrecis.DisallowedControlRule),
            1,
            1,
            0x0009,
            UnicodePrecisStatus.DisallowedControl);
    }

    [Test]
    public void UsernameCasePreserved_ProfilePreservesStorageButFoldsComparison()
    {
        UnicodePrecisProfile profile = new UnicodeProfiles().CreateUsernameCasePreserved();

        Assert.That(new UnicodePrecis().Prepare("A0", profile), Is.EqualTo("A0"));
        Assert.That(new UnicodePrecis().GetCompareKey("A0", profile), Is.EqualTo("a0"));
        Assert.That(new UnicodePrecis().EqualsPrepared("A0", "a0", profile), Is.True);
        Assert.That(new UnicodePrecis().Validate("A0", profile).IsValid, Is.True);
    }

    [Test]
    public void Nickname_ProfileNormalizesSpaceRunsToSingleFoldedDisplayKey()
    {
        UnicodePrecisProfile profile = new UnicodeProfiles().CreateNickname();

        Assert.That(UnicodePrecis.MapSpacesToAscii("A\u00A0\u2003B"), Is.EqualTo("A  B"));
        Assert.That(UnicodePrecis.CollapseAsciiSpaces(" A   B ", true), Is.EqualTo("A B"));
        Assert.That(new UnicodePrecis().Prepare(" \u00A0A  \u00DF\u2003 ", profile), Is.EqualTo("A \u00DF"));
        Assert.That(new UnicodePrecis().GetCompareKey("A \u00DF", profile), Is.EqualTo("a \u00DF"));
        Assert.That(new UnicodePrecis().Validate("A \u00DF", profile).IsValid, Is.True);
    }

    [Test]
    public void Nickname_ProfileRejectsEmptyAfterTrimming()
    {
        UnicodePrecisValidationResult result = new UnicodePrecis().Validate(" \u00A0 ", new UnicodeProfiles().CreateNickname());

        Assert.That(result.IsValid, Is.False);
        Assert.That(result.Status, Is.EqualTo(UnicodePrecisStatus.Empty));
        Assert.That(result.PreparedText, Is.EqualTo(""));
        AssertDiagnostic(result.Diagnostics[0], 0, 0, -1, UnicodePrecisStatus.Empty);
    }

    [Test]
    public void OpaqueString_ProfilePreservesCaseAndCompatibilityCharacters()
    {
        UnicodePrecisProfile profile = new UnicodeProfiles().CreateOpaqueString();

        UnicodePrecisValidationResult raw = new UnicodePrecis().Validate(" \u00B2A ", profile);
        UnicodePrecisValidationResult prepared = new UnicodePrecis().Validate(" \u00B2A ", profile);

        Assert.That(new UnicodePrecis().PrepareOpaqueString(" \u00B2A "), Is.EqualTo(" \u00B2A "));
        Assert.That(raw.IsValid, Is.True);
        Assert.That(raw.PreparedText, Is.EqualTo(" \u00B2A "));
        Assert.That(prepared.IsValid, Is.True);
        Assert.That(new UnicodePrecis().EqualsPrepared(" A ", " a ", profile), Is.False);
    }

    [Test]
    public void OpaqueString_ProfileAllowsSpacesButRejectsEmptyAndControls()
    {
        UnicodePrecisProfile profile = new UnicodeProfiles().CreateOpaqueString();

        UnicodePrecisValidationResult spaces = new UnicodePrecis().Validate("   ", profile);
        UnicodePrecisValidationResult empty = new UnicodePrecis().Validate("", profile);
        UnicodePrecisValidationResult control = new UnicodePrecis().Validate("a\u007F", profile);

        Assert.That(spaces.IsValid, Is.True);
        Assert.That(empty.Status, Is.EqualTo(UnicodePrecisStatus.Empty));
        Assert.That(control.Status, Is.EqualTo(UnicodePrecisStatus.DisallowedControl));
        AssertDiagnostic(
            FindDiagnostic(control, UnicodePrecis.DisallowedControlRule),
            1,
            1,
            0x007F,
            UnicodePrecisStatus.DisallowedControl);
    }

    static UnicodePrecisDiagnostic FindDiagnostic(UnicodePrecisValidationResult result, string rule)
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

        Assert.Fail("Missing PRECIS diagnostic rule " + rule);
        return result.Diagnostics[0];
    }

    static void AssertDiagnostic(
        UnicodePrecisDiagnostic diagnostic,
        int utf16Index,
        int utf16Length,
        int codePoint,
        UnicodePrecisStatus status)
    {
        Assert.That(diagnostic.Utf16Index, Is.EqualTo(utf16Index));
        Assert.That(diagnostic.Utf16Length, Is.EqualTo(utf16Length));
        Assert.That(diagnostic.CodePoint, Is.EqualTo(codePoint));
        Assert.That(diagnostic.Status, Is.EqualTo(status));
        Assert.That(diagnostic.Rule, Is.Not.Empty);
        Assert.That(diagnostic.Message, Is.Not.Empty);
    }
}
