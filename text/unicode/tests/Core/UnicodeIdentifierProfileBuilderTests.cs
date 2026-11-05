using NUnit.Framework;

namespace unicode.Tests;

[TestFixture]
public class UnicodeIdentifierProfileBuilderTests
{
    [Test]
    public void FactoryCreatesDefaultBehavioralBuilder()
    {
        IUnicodeIdentifierProfileBuilder builder = new UnicodeProfiles().CreateIdentifierProfileBuilder("Custom");
        UnicodeIdentifierProfile profile = builder.Build();

        Assert.That(profile.Name, Is.EqualTo("Custom"));
        Assert.That(profile, Is.AssignableTo<IUnicodeIdentifierProfile>());
        Assert.That(new UnicodeIdentifiers().ValidateIdentifier("A0_", profile).IsValid, Is.True);
        Assert.That(new UnicodeIdentifiers().ValidateIdentifier("0A", profile).IsValid, Is.False);
        Assert.That(profile.Normalize("A\u0300"), Is.EqualTo("A\u0300"));
        Assert.That(profile.GetCompareKey("A"), Is.EqualTo("A"));
    }

    [Test]
    public void AllowedAndDeniedRangesCustomizeBothIdentifierPositionsWithDenyPrecedence()
    {
        UnicodeIdentifierProfile allowed = new UnicodeIdentifierProfileBuilder("Allowed")
            .SetAllowedRanges(new UnicodeRange[] { new UnicodeRange(0x0024, 0x0024) })
            .SetPatternRestrictions(false, false)
            .Build();
        UnicodeIdentifierProfile denied = new UnicodeIdentifierProfileBuilder("Denied")
            .SetAllowedRanges(new UnicodeRange[] { new UnicodeRange(0x0041, 0x0042) })
            .SetDeniedRanges(new UnicodeRange[] { new UnicodeRange(0x0041, 0x0041) })
            .Build();

        Assert.That(new UnicodeIdentifiers().ValidateIdentifier("$A$", allowed).IsValid, Is.True);
        Assert.That(new UnicodeIdentifiers().ValidateIdentifier("A", denied).IsValid, Is.False);
        Assert.That(new UnicodeIdentifiers().ValidateIdentifier("B", denied).IsValid, Is.True);
    }

    [Test]
    public void NormalizationPoliciesPrepareAndRequireTheirSelectedForms()
    {
        UnicodeIdentifierProfile nfd = new UnicodeIdentifierProfileBuilder("NFD")
            .SetNormalizationPolicy(UnicodeIdentifierNormalizationPolicy.Nfd)
            .Build();
        UnicodeIdentifierProfile nfkc = new UnicodeIdentifierProfileBuilder("NFKC")
            .SetNormalizationPolicy(UnicodeIdentifierNormalizationPolicy.Nfkc)
            .Build();

        Assert.That(nfd.Normalize("\u00C0"), Is.EqualTo("A\u0300"));
        Assert.That(nfd.IsNormalizationSatisfied("\u00C0"), Is.False);
        Assert.That(new UnicodeIdentifiers().ValidateIdentifier("\u00C0", nfd).Status, Is.EqualTo(UnicodeIdentifierStatus.NeedsNormalization));
        Assert.That(nfkc.Normalize("\u00B2"), Is.EqualTo("2"));
    }

    [Test]
    public void CasePoliciesProduceTheSelectedCaselessKeys()
    {
        UnicodeIdentifierProfile ordinal = new UnicodeIdentifierProfileBuilder("Ordinal")
            .SetCasePolicy(UnicodeIdentifierCasePolicy.OrdinalFold)
            .Build();
        UnicodeIdentifierProfile nfc = new UnicodeIdentifierProfileBuilder("NFC Fold")
            .SetCasePolicy(UnicodeIdentifierCasePolicy.NfcFold)
            .Build();
        UnicodeIdentifierProfile nfkc = new UnicodeIdentifierProfileBuilder("NFKC Fold")
            .SetCasePolicy(UnicodeIdentifierCasePolicy.NfkcFold)
            .Build();

        Assert.That(new UnicodeIdentifiers().EqualsIdentifier("A\u00DF", "ass", ordinal), Is.True);
        Assert.That(new UnicodeIdentifiers().EqualsIdentifier("A\u0300", "\u00E0", nfc), Is.True);
        Assert.That(new UnicodeIdentifiers().GetIdentifierCompareKey("\u00B2A", nfkc), Is.EqualTo("2a"));
    }

    [Test]
    public void UnassignedPolicyRespectsDeniedRangesAndScriptRestrictions()
    {
        string unassigned = "\u0378";
        UnicodeIdentifierProfile allowed = new UnicodeIdentifierProfileBuilder("Unassigned")
            .SetAllowUnassignedCodePoints(true)
            .Build();
        UnicodeIdentifierProfile denied = new UnicodeIdentifierProfileBuilder("Denied unassigned")
            .SetAllowUnassignedCodePoints(true)
            .SetDeniedRanges(new UnicodeRange[] { new UnicodeRange(0x0378, 0x0378) })
            .Build();
        UnicodeIdentifierProfile scriptRestricted = new UnicodeIdentifierProfileBuilder("Script restricted")
            .SetAllowUnassignedCodePoints(true)
            .SetPermittedScripts(new UnicodeScript[] { UnicodeScript.Latin })
            .Build();

        Assert.That(new UnicodeIdentifiers().ValidateIdentifier(unassigned, allowed).IsValid, Is.True);
        Assert.That(new UnicodeIdentifiers().ValidateIdentifier(unassigned, denied).IsValid, Is.False);
        Assert.That(new UnicodeIdentifiers().ValidateIdentifier(unassigned, scriptRestricted).IsValid, Is.False);
    }

    [Test]
    public void PermittedScriptsRejectOtherSignificantScriptsButKeepNeutralCharacters()
    {
        UnicodeIdentifierProfile profile = new UnicodeIdentifierProfileBuilder("Latin only")
            .SetPermittedScripts(new UnicodeScript[] { UnicodeScript.Latin })
            .Build();

        Assert.That(new UnicodeIdentifiers().ValidateIdentifier("Latin_123", profile).IsValid, Is.True);
        Assert.That(new UnicodeIdentifiers().ValidateIdentifier("\u03B1", profile).IsValid, Is.False);
        Assert.That(profile.AllowsContinue(0x0301), Is.True);
    }

    [Test]
    public void PermittedScripts_AcceptRecommendedSecurityScripts()
    {
        UnicodeIdentifierProfile profile = new UnicodeIdentifierProfileBuilder("Armenian only")
            .SetPermittedScripts(new UnicodeScript[] { UnicodeScript.Armenian })
            .Build();

        Assert.That(new UnicodeIdentifiers().ValidateIdentifier("\u0531", profile).IsValid, Is.True);
        Assert.That(new UnicodeIdentifiers().ValidateIdentifier("A", profile).IsValid, Is.False);
    }

    [Test]
    public void BuilderDefensivelyCopiesRangesScriptsAndBuiltProfiles()
    {
        UnicodeRange allowedRange = new UnicodeRange(0x0024, 0x0024);
        UnicodeScript[] scripts = new UnicodeScript[] { UnicodeScript.Latin };
        UnicodeIdentifierProfileBuilder builder = new UnicodeIdentifierProfileBuilder("Copied")
            .SetAllowedRanges(new UnicodeRange[] { allowedRange })
            .SetPermittedScripts(scripts)
            .SetPatternRestrictions(false, false);

        allowedRange.Start = 0x0040;
        allowedRange.End = 0x0040;
        scripts[0] = UnicodeScript.Greek;
        UnicodeIdentifierProfile first = builder.Build();
        builder.SetDeniedRanges(new UnicodeRange[] { new UnicodeRange(0x0041, 0x005A) });
        UnicodeIdentifierProfile second = builder.Build();

        Assert.That(new UnicodeIdentifiers().ValidateIdentifier("$A", first).IsValid, Is.True);
        Assert.That(new UnicodeIdentifiers().ValidateIdentifier("$A", second).IsValid, Is.False);
        Assert.That(new UnicodeIdentifiers().ValidateIdentifier("\u03B1", first).IsValid, Is.False);
    }

    [Test]
    public void BuilderRejectsInvalidConfigurationValues()
    {
        Assert.Throws<ArgumentException>(() => new UnicodeIdentifierProfileBuilder(""));
        Assert.Throws<ArgumentNullException>(() => new UnicodeIdentifierProfileBuilder("Bad").SetAllowedRanges(null!));
        Assert.Throws<ArgumentException>(() => new UnicodeIdentifierProfileBuilder("Bad").SetDeniedRanges(new UnicodeRange[] { null! }));
        Assert.Throws<ArgumentNullException>(() => new UnicodeIdentifierProfileBuilder("Bad").SetPermittedScripts(null!));
        Assert.Throws<ArgumentOutOfRangeException>(() => new UnicodeIdentifierProfileBuilder("Bad").SetPermittedScripts(new UnicodeScript[] { (UnicodeScript)99 }));
        Assert.Throws<ArgumentOutOfRangeException>(() => new UnicodeIdentifierProfileBuilder("Bad").SetNormalizationPolicy((UnicodeIdentifierNormalizationPolicy)99));
        Assert.Throws<ArgumentOutOfRangeException>(() => new UnicodeIdentifierProfileBuilder("Bad").SetCasePolicy((UnicodeIdentifierCasePolicy)99));
    }
}
