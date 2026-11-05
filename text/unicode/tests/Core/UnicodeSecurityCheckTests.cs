using NUnit.Framework;

namespace unicode.Tests;

[TestFixture]
public class UnicodeSecurityCheckTests
{
    [Test]
    public void CheckIdentifier_ReturnsSafeAggregateForAsciiIdentifier()
    {
        UnicodeSecurityCheckResult result = new UnicodeSecurity().CheckIdentifier("AZ09", new UnicodeProfiles().CreateDefaultIdentifier());

        Assert.That(result.IsSafe, Is.True);
        Assert.That(result.Flags, Is.EqualTo(UnicodeSecurityFlag.None));
        Assert.That(result.RestrictionLevel, Is.EqualTo(UnicodeRestrictionLevel.Ascii));
        Assert.That(result.Skeleton, Is.EqualTo("AZO9"));
        Assert.That(result.Scripts, Is.EqualTo(new UnicodeScript[] { UnicodeScript.Latin }));
        Assert.That(result.IdentifierValidation.IsValid, Is.True);
        Assert.That(result.SecurityDiagnostics, Is.Empty);
    }

    [Test]
    public void CheckIdentifier_AggregatesMixedScriptRestrictionAndSecurityFlags()
    {
        UnicodeSecurityCheckResult result = new UnicodeSecurity().CheckIdentifier("A\u0391\u200D", new UnicodeProfiles().CreateImmutableIdentifier());

        Assert.That(result.IsSafe, Is.False);
        Assert.That(UnicodeSecurity.HasSecurityFlag(result.Flags, UnicodeSecurityFlag.MixedScript), Is.True);
        Assert.That(UnicodeSecurity.HasSecurityFlag(result.Flags, UnicodeSecurityFlag.Restricted), Is.True);
        Assert.That(UnicodeSecurity.HasSecurityFlag(result.Flags, UnicodeSecurityFlag.Invisible), Is.True);
        Assert.That(UnicodeSecurity.HasSecurityFlag(result.Flags, UnicodeSecurityFlag.JoinControl), Is.True);
        Assert.That(result.RestrictionLevel, Is.EqualTo(UnicodeRestrictionLevel.Unrestricted));
        Assert.That(result.Scripts, Is.EqualTo(new UnicodeScript[] { UnicodeScript.Latin, UnicodeScript.Greek }));
        Assert.That(result.SecurityDiagnostics, Has.Length.GreaterThanOrEqualTo(2));
    }

    [Test]
    public void CheckIdentifier_PreservesIdentifierValidationResult()
    {
        UnicodeSecurityCheckResult result = new UnicodeSecurity().CheckIdentifier("0A", new UnicodeProfiles().CreateDefaultIdentifier());

        Assert.That(result.IsSafe, Is.False);
        Assert.That(result.Flags, Is.EqualTo(UnicodeSecurityFlag.None));
        Assert.That(result.IdentifierValidation.IsValid, Is.False);
        Assert.That(result.IdentifierValidation.Status, Is.EqualTo(UnicodeIdentifierStatus.InvalidStart));
    }

    [Test]
    public void AggregateSecurityFlags_CombinesDiagnosticFlagsWithContextFlags()
    {
        UnicodeSecurityDiagnostic[] diagnostics = new UnicodeSecurityDiagnostic[]
        {
            new UnicodeSecurityDiagnostic(0, 1, 0x200D, UnicodeSecurityFlag.JoinControl, UnicodeSecurity.JoinControlRule, "join"),
            new UnicodeSecurityDiagnostic(0, 1, 0x200D, UnicodeSecurityFlag.Invisible, UnicodeSecurity.InvisibleRule, "invisible"),
        };

        UnicodeSecurityFlag flags = UnicodeSecurity.AggregateSecurityFlags(diagnostics, true, UnicodeRestrictionLevel.Unrestricted);

        Assert.That(UnicodeSecurity.HasSecurityFlag(flags, UnicodeSecurityFlag.MixedScript), Is.True);
        Assert.That(UnicodeSecurity.HasSecurityFlag(flags, UnicodeSecurityFlag.Restricted), Is.True);
        Assert.That(UnicodeSecurity.HasSecurityFlag(flags, UnicodeSecurityFlag.JoinControl), Is.True);
        Assert.That(UnicodeSecurity.HasSecurityFlag(flags, UnicodeSecurityFlag.Invisible), Is.True);
    }

    [Test]
    public void CheckIdentifier_RejectsNullArguments()
    {
        Assert.Throws<ArgumentNullException>(() => new UnicodeSecurity().CheckIdentifier(null!, new UnicodeProfiles().CreateDefaultIdentifier()));
        Assert.Throws<ArgumentNullException>(() => new UnicodeSecurity().CheckIdentifier("A", null!));
        Assert.Throws<ArgumentNullException>(() => UnicodeSecurity.AggregateSecurityFlags(null!, false, UnicodeRestrictionLevel.Ascii));
    }
}
