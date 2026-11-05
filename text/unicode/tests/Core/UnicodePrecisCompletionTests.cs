using NUnit.Framework;

namespace unicode.Tests;

[TestFixture]
public class UnicodePrecisCompletionTests
{
    [Test]
    public void IdentifierProfiles_ApplyContextualJoinerRules()
    {
        UnicodePrecis precis = new UnicodePrecis();
        UnicodePrecisProfile profile = new UnicodeProfiles().CreateUsernameCaseMapped();

        Assert.That(precis.Validate("l\u00B7l", profile).IsValid, Is.True);
        Assert.That(precis.Validate("\u0915\u094D\u200D\u0937", profile).IsValid, Is.True);
        Assert.That(precis.Validate("\u0628\u200C\u0628", profile).IsValid, Is.True);

        UnicodePrecisValidationResult middleDot = precis.Validate("a\u00B7a", profile);
        UnicodePrecisValidationResult joiner = precis.Validate("a\u200Da", profile);

        Assert.That(middleDot.Status, Is.EqualTo(UnicodePrecisStatus.ContextualRule));
        Assert.That(ContainsRule(middleDot, UnicodePrecisRules.ContextualRule), Is.True);
        Assert.That(joiner.Status, Is.EqualTo(UnicodePrecisStatus.ContextualRule));
        Assert.That(ContainsRule(joiner, UnicodePrecisRules.ContextualRule), Is.True);
    }

    [Test]
    public void IdentifierProfiles_ApplyDerivedClassAndBidiRules()
    {
        UnicodePrecis precis = new UnicodePrecis();
        UnicodePrecisProfile profile = new UnicodeProfiles().CreateUsernameCaseMapped();

        Assert.That(precis.Validate("juliet@example.com", profile).IsValid, Is.True);
        Assert.That(precis.Validate("\u05D0\u05D11", profile).IsValid, Is.True);

        UnicodePrecisValidationResult compatibility = precis.Validate("\u00B2a", profile);
        UnicodePrecisValidationResult ignorable = precis.Validate("a\u00AD", profile);
        UnicodePrecisValidationResult bidi = precis.Validate("\u05D0a\u05D1", profile);

        Assert.That(compatibility.Status, Is.EqualTo(UnicodePrecisStatus.DisallowedCodePoint));
        Assert.That(ContainsRule(compatibility, UnicodePrecisRules.DisallowedCodePointRule), Is.True);
        Assert.That(ignorable.Status, Is.EqualTo(UnicodePrecisStatus.DisallowedCodePoint));
        Assert.That(bidi.Status, Is.EqualTo(UnicodePrecisStatus.BidiRule));
        Assert.That(ContainsRule(bidi, UnicodePrecisRules.BidiRule), Is.True);
    }

    [Test]
    public void FreeformProfiles_AllowSymbolsButRejectIgnorables()
    {
        UnicodePrecis precis = new UnicodePrecis();
        UnicodePrecisProfile nickname = new UnicodeProfiles().CreateNickname();
        UnicodePrecisProfile opaque = new UnicodeProfiles().CreateOpaqueString();

        Assert.That(precis.Validate("A + B", nickname).IsValid, Is.True);
        Assert.That(precis.Validate("A + B", opaque).IsValid, Is.True);
        Assert.That(precis.PrepareOpaqueString("A\u00A0B"), Is.EqualTo("A B"));
        Assert.That(precis.Validate("a\u2060", nickname).Status, Is.EqualTo(UnicodePrecisStatus.DisallowedCodePoint));
        Assert.That(precis.Validate("a\u2060", opaque).Status, Is.EqualTo(UnicodePrecisStatus.DisallowedCodePoint));
        Assert.That(precis.Validate("a\tb", nickname).Status, Is.EqualTo(UnicodePrecisStatus.DisallowedControl));
        Assert.That(precis.Validate("a\tb", opaque).Status, Is.EqualTo(UnicodePrecisStatus.DisallowedControl));
    }

    static bool ContainsRule(UnicodePrecisValidationResult result, string rule)
    {
        int index = 0;
        while (index < result.Diagnostics.Length)
        {
            if (result.Diagnostics[index].Rule == rule)
            {
                return true;
            }

            index = index + 1;
        }

        return false;
    }
}
