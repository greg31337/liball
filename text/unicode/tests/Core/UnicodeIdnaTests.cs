using NUnit.Framework;

namespace unicode.Tests;

[TestFixture]
public class UnicodeIdnaTests
{
    [Test]
    public void PrepareLabel_AppliesNontransitionalMappingAndNfc()
    {
        UnicodeIdna idna = new UnicodeIdna();

        Assert.That(idna.PrepareLabel("B\u00DCCHER"), Is.EqualTo("b\u00FCcher"));
        Assert.That(idna.PrepareLabel("\uFF21\uFF22\uFF23"), Is.EqualTo("abc"));
        Assert.That(idna.PrepareLabel("\u1E9E\u00DF\u03C2"), Is.EqualTo("\u00DF\u00DF\u03C2"));
        Assert.That(idna.PrepareLabel("e\u0301"), Is.EqualTo("\u00E9"));
        Assert.That(idna.PrepareLabel("a\u00ADb"), Is.EqualTo("ab"));
    }

    [Test]
    public void ValidateLabel_ReturnsPreparedLabelAndSecurityObservations()
    {
        UnicodeIdnaValidationResult result = new UnicodeIdna().ValidateLabel("B\u00DCCHER");

        Assert.That(result.IsValid, Is.True);
        Assert.That(result.Status, Is.EqualTo(UnicodeIdnaStatus.Valid));
        Assert.That(result.PreparedLabel, Is.EqualTo("b\u00FCcher"));
        Assert.That(result.Diagnostics, Is.Empty);
        Assert.That(result.SecurityDiagnostics, Is.Not.Null);
        Assert.That(result.RestrictionLevel, Is.EqualTo(UnicodeRestrictionLevel.SingleScriptRestrictive));
    }

    [Test]
    public void ValidateLabel_RejectsStructuralFailures()
    {
        UnicodeIdna idna = new UnicodeIdna();

        AssertStatus(idna.ValidateLabel(""), UnicodeIdnaStatus.Empty, UnicodeIdnaRules.EmptyRule);
        AssertStatus(idna.ValidateLabel("-abc"), UnicodeIdnaStatus.Hyphen, UnicodeIdnaRules.HyphenRule);
        AssertStatus(idna.ValidateLabel("abc-"), UnicodeIdnaStatus.Hyphen, UnicodeIdnaRules.HyphenRule);
        AssertStatus(idna.ValidateLabel("ab--cd"), UnicodeIdnaStatus.Hyphen, UnicodeIdnaRules.HyphenRule);
        AssertStatus(idna.ValidateLabel("\u0301abc"), UnicodeIdnaStatus.LeadingMark, UnicodeIdnaRules.LeadingMarkRule);
        AssertStatus(idna.ValidateLabel("a\u3002b"), UnicodeIdnaStatus.LabelSeparator, UnicodeIdnaRules.LabelSeparatorRule);
        AssertStatus(idna.ValidateLabel("a_b"), UnicodeIdnaStatus.DisallowedCodePoint, UnicodeIdnaRules.DisallowedCodePointRule);
    }

    [Test]
    public void ValidateLabel_EnforcesEveryContextualRuleShape()
    {
        UnicodeIdna idna = new UnicodeIdna();

        Assert.That(idna.IsLabelValid("l\u00B7l"), Is.True);
        Assert.That(idna.IsLabelValid("\u0375\u03B1"), Is.True);
        Assert.That(idna.IsLabelValid("\u05D0\u05F3"), Is.True);
        Assert.That(idna.IsLabelValid("\u30AB\u30FB\u30CA"), Is.True);
        Assert.That(idna.IsLabelValid("\u0627\u0661\u0662"), Is.True);
        Assert.That(idna.IsLabelValid("\u0915\u094D\u200D\u0937"), Is.True);
        Assert.That(idna.IsLabelValid("\u0628\u200C\u0628"), Is.True);

        AssertStatus(idna.ValidateLabel("a\u00B7a"), UnicodeIdnaStatus.ContextualRule, UnicodeIdnaRules.ContextualRule);
        AssertStatus(idna.ValidateLabel("\u0375a"), UnicodeIdnaStatus.ContextualRule, UnicodeIdnaRules.ContextualRule);
        AssertStatus(idna.ValidateLabel("a\u05F3"), UnicodeIdnaStatus.ContextualRule, UnicodeIdnaRules.ContextualRule);
        AssertStatus(idna.ValidateLabel("a\u30FBa"), UnicodeIdnaStatus.ContextualRule, UnicodeIdnaRules.ContextualRule);
        AssertStatus(idna.ValidateLabel("\u0661\u06F1"), UnicodeIdnaStatus.ContextualRule, UnicodeIdnaRules.ContextualRule);
        AssertStatus(idna.ValidateLabel("a\u200Da"), UnicodeIdnaStatus.ContextualRule, UnicodeIdnaRules.ContextualRule);
    }

    [Test]
    public void ValidateLabel_EnforcesBidiAndScalarRules()
    {
        UnicodeIdna idna = new UnicodeIdna();

        Assert.That(idna.IsLabelValid("\u05D0\u05D11"), Is.True);
        AssertStatus(idna.ValidateLabel("\u05D0a\u05D1"), UnicodeIdnaStatus.BidiRule, UnicodeIdnaRules.BidiRule);
        AssertStatus(idna.ValidateLabel("\uD800"), UnicodeIdnaStatus.InvalidScalar, UnicodeIdnaRules.InvalidScalarRule);
        Assert.That(idna.IsLabelValid("abc"), Is.True);
    }

    [Test]
    public void LabelApi_RejectsNullArguments()
    {
        UnicodeIdna idna = new UnicodeIdna();

        Assert.Throws<ArgumentNullException>(() => idna.PrepareLabel(null!));
        Assert.Throws<ArgumentNullException>(() => idna.ValidateLabel(null!));
        Assert.Throws<ArgumentNullException>(() => idna.IsLabelValid(null!));
    }

    static void AssertStatus(
        UnicodeIdnaValidationResult result,
        UnicodeIdnaStatus status,
        string rule)
    {
        Assert.That(result.IsValid, Is.False);
        Assert.That(result.Status, Is.EqualTo(status));
        Assert.That(ContainsRule(result, rule), Is.True);
    }

    static bool ContainsRule(UnicodeIdnaValidationResult result, string rule)
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
