using NUnit.Framework;

namespace unicode.Tests;

[TestFixture]
public class UnicodePrecisTests
{
    [Test]
    public void ProfileFactories_ReturnExpectedPrecisInspiredProfiles()
    {
        UnicodePrecisProfile usernameMapped = new UnicodeProfiles().CreateUsernameCaseMapped();
        UnicodePrecisProfile usernamePreserved = new UnicodeProfiles().CreateUsernameCasePreserved();
        UnicodePrecisProfile nickname = new UnicodeProfiles().CreateNickname();
        UnicodePrecisProfile opaque = new UnicodeProfiles().CreateOpaqueString();

        Assert.That(usernameMapped.Name, Is.EqualTo("UsernameCaseMapped"));
        Assert.That(usernameMapped.IdentifierClass, Is.True);
        Assert.That(usernameMapped.CaseMapped, Is.True);
        Assert.That(usernameMapped.AllowSpaces, Is.False);

        Assert.That(usernamePreserved.Name, Is.EqualTo("UsernameCasePreserved"));
        Assert.That(usernamePreserved.IdentifierClass, Is.True);
        Assert.That(usernamePreserved.CaseMapped, Is.False);
        Assert.That(usernamePreserved.CaseInsensitive, Is.True);

        Assert.That(nickname.Name, Is.EqualTo("Nickname"));
        Assert.That(nickname.MapSpaces, Is.True);
        Assert.That(nickname.TrimSpaces, Is.True);
        Assert.That(nickname.CollapseSpaces, Is.True);
        Assert.That(nickname.AllowSpaces, Is.True);

        Assert.That(opaque.Name, Is.EqualTo("OpaqueString"));
        Assert.That(opaque.IdentifierClass, Is.False);
        Assert.That(opaque.CaseMapped, Is.False);
        Assert.That(opaque.CaseInsensitive, Is.False);
        Assert.That(opaque.AllowSpaces, Is.True);
    }

    [Test]
    public void PrepareUsername_AppliesWidthLowercaseAndNfcRules()
    {
        Assert.That(new UnicodePrecis().PrepareUsername("A\u00DF0"), Is.EqualTo("a\u00DF0"));
        Assert.That(new UnicodePrecis().Prepare("A\u00DF0", new UnicodeProfiles().CreateUsernameCasePreserved()), Is.EqualTo("A\u00DF0"));
        Assert.That(new UnicodePrecis().GetCompareKey("A\u00DF0", new UnicodeProfiles().CreateUsernameCasePreserved()), Is.EqualTo("a\u00DF0"));
        Assert.That(new UnicodePrecis().EqualsPrepared("A\u00DF0", "a\u00DF0", new UnicodeProfiles().CreateUsernameCasePreserved()), Is.True);
        Assert.That(new UnicodePrecis().PrepareUsername("\uFF21bc"), Is.EqualTo("abc"));
    }

    [Test]
    public void ValidateUsername_RequiresPreparedIdentifierClassText()
    {
        UnicodePrecisProfile profile = new UnicodeProfiles().CreateUsernameCaseMapped();

        Assert.That(new UnicodePrecis().Validate("a0_", profile).IsValid, Is.True);

        UnicodePrecisValidationResult needsPreparation = new UnicodePrecis().Validate("A0", profile);
        Assert.That(needsPreparation.IsValid, Is.False);
        Assert.That(needsPreparation.Status, Is.EqualTo(UnicodePrecisStatus.NeedsPreparation));
        Assert.That(needsPreparation.PreparedText, Is.EqualTo("a0"));

        UnicodePrecisValidationResult disallowedSpace = new UnicodePrecis().Validate("a 0", profile);
        Assert.That(disallowedSpace.IsValid, Is.False);
        Assert.That(disallowedSpace.Status, Is.EqualTo(UnicodePrecisStatus.DisallowedSpace));
        AssertDiagnostic(
            disallowedSpace.Diagnostics[0],
            1,
            1,
            0x0020,
            UnicodePrecisStatus.DisallowedSpace,
            UnicodePrecis.DisallowedSpaceRule);
    }

    [Test]
    public void PrepareNickname_MapsTrimsCollapsesAndPreservesDisplayCase()
    {
        UnicodePrecisProfile profile = new UnicodeProfiles().CreateNickname();

        Assert.That(new UnicodePrecis().PrepareNickname("  A\u00A0\u2003\u00DF  "), Is.EqualTo("A \u00DF"));
        Assert.That(new UnicodePrecis().Validate("A \u00DF", profile).IsValid, Is.True);
        Assert.That(new UnicodePrecis().GetCompareKey("A \u00DF", profile), Is.EqualTo("a \u00DF"));

        UnicodePrecisValidationResult result = new UnicodePrecis().Validate("  A\u00A0\u2003\u00DF  ", profile);
        Assert.That(result.IsValid, Is.False);
        Assert.That(result.Status, Is.EqualTo(UnicodePrecisStatus.NeedsPreparation));
        Assert.That(result.PreparedText, Is.EqualTo("A \u00DF"));
    }

    [Test]
    public void OpaqueString_PreservesCaseAndSpaces()
    {
        UnicodePrecisProfile profile = new UnicodeProfiles().CreateOpaqueString();

        Assert.That(new UnicodePrecis().PrepareOpaqueString(" A\u00DF  "), Is.EqualTo(" A\u00DF  "));
        Assert.That(new UnicodePrecis().Validate(" A\u00DF  ", profile).IsValid, Is.True);
        Assert.That(new UnicodePrecis().EqualsPrepared("A", "a", profile), Is.False);
    }

    [Test]
    public void Validate_RejectsInvalidUtf16AndControls()
    {
        UnicodePrecisValidationResult invalid = new UnicodePrecis().Validate("\uD800", new UnicodeProfiles().CreateOpaqueString());
        Assert.That(invalid.IsValid, Is.False);
        Assert.That(invalid.Status, Is.EqualTo(UnicodePrecisStatus.InvalidScalar));
        AssertDiagnostic(
            invalid.Diagnostics[0],
            0,
            1,
            UnicodeScalar.ReplacementCharacter,
            UnicodePrecisStatus.InvalidScalar,
            UnicodePrecis.InvalidScalarRule);

        UnicodePrecisValidationResult control = new UnicodePrecis().Validate("a\u0000", new UnicodeProfiles().CreateNickname());
        Assert.That(control.IsValid, Is.False);
        Assert.That(control.Status, Is.EqualTo(UnicodePrecisStatus.DisallowedControl));
        AssertDiagnostic(
            control.Diagnostics[0],
            1,
            1,
            0x0000,
            UnicodePrecisStatus.DisallowedControl,
            UnicodePrecis.DisallowedControlRule);
    }

    [Test]
    public void PrecisHelpers_RejectNullArguments()
    {
        UnicodePrecisProfile profile = new UnicodeProfiles().CreateNickname();

        Assert.Throws<ArgumentNullException>(() => new UnicodePrecis().Prepare(null!, profile));
        Assert.Throws<ArgumentNullException>(() => new UnicodePrecis().Prepare("a", null!));
        Assert.Throws<ArgumentNullException>(() => new UnicodePrecis().GetCompareKey(null!, profile));
        Assert.Throws<ArgumentNullException>(() => new UnicodePrecis().GetCompareKey("a", null!));
        Assert.Throws<ArgumentNullException>(() => new UnicodePrecis().EqualsPrepared(null!, "a", profile));
        Assert.Throws<ArgumentNullException>(() => new UnicodePrecis().EqualsPrepared("a", null!, profile));
        Assert.Throws<ArgumentNullException>(() => new UnicodePrecis().EqualsPrepared("a", "a", null!));
        Assert.Throws<ArgumentNullException>(() => new UnicodePrecis().Validate(null!, profile));
        Assert.Throws<ArgumentNullException>(() => new UnicodePrecis().Validate("a", null!));
        Assert.Throws<ArgumentNullException>(() => UnicodePrecis.MapSpacesToAscii(null!));
        Assert.Throws<ArgumentNullException>(() => UnicodePrecis.CollapseAsciiSpaces(null!, true));
        Assert.Throws<ArgumentNullException>(() => UnicodePrecis.TrimAsciiSpaces(null!));
        Assert.Throws<ArgumentException>(() => new UnicodePrecisProfile("", true, false, false, true, UnicodeNormalizationForm.Nfkc, false, false, false, false, false));
        Assert.Throws<ArgumentOutOfRangeException>(() => new UnicodePrecisProfile("Bad", true, false, false, true, (UnicodeNormalizationForm)99, false, false, false, false, false));
    }

    static void AssertDiagnostic(
        UnicodePrecisDiagnostic diagnostic,
        int utf16Index,
        int utf16Length,
        int codePoint,
        UnicodePrecisStatus status,
        string rule)
    {
        Assert.That(diagnostic.Utf16Index, Is.EqualTo(utf16Index));
        Assert.That(diagnostic.Utf16Length, Is.EqualTo(utf16Length));
        Assert.That(diagnostic.CodePoint, Is.EqualTo(codePoint));
        Assert.That(diagnostic.Status, Is.EqualTo(status));
        Assert.That(diagnostic.Rule, Is.EqualTo(rule));
        Assert.That(diagnostic.Message, Is.Not.Empty);
    }
}
