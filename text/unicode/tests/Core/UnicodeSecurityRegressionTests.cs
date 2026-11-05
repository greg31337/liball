using NUnit.Framework;

namespace unicode.Tests;

[TestFixture]
public class UnicodeSecurityRegressionTests
{
    [Test]
    public void ConfusableSkeletons_CatchSingleAndMultiScalarMappings()
    {
        Assert.That(new UnicodeSecurity().GetSkeleton("\u0391"), Is.EqualTo("A"));
        Assert.That(new UnicodeSecurity().GetSkeleton("m"), Is.EqualTo("rn"));
        Assert.That(new UnicodeSecurity().GetSkeleton("xm y"), Is.EqualTo("xrn y"));

        Assert.That(new UnicodeSecurity().AreConfusable("\u0391", "A"), Is.True);
        Assert.That(new UnicodeSecurity().AreConfusable("\u0391", "a"), Is.False);
        Assert.That(new UnicodeSecurity().AreConfusable("m", "rn"), Is.True);
        Assert.That(new UnicodeSecurity().AreConfusable("rn", "rn"), Is.False);
    }

    [Test]
    public void CheckIdentifier_FlagsMixedScriptLookalikeCandidate()
    {
        UnicodeSecurityCheckResult result = new UnicodeSecurity().CheckIdentifier("A\u0391", new UnicodeProfiles().CreateImmutableIdentifier());

        Assert.That(result.IsSafe, Is.False);
        Assert.That(result.Scripts, Is.EqualTo(new UnicodeScript[] { UnicodeScript.Latin, UnicodeScript.Greek }));
        Assert.That(result.RestrictionLevel, Is.EqualTo(UnicodeRestrictionLevel.MinimallyRestrictive));
        Assert.That(UnicodeSecurity.HasSecurityFlag(result.Flags, UnicodeSecurityFlag.MixedScript), Is.True);
        Assert.That(UnicodeSecurity.HasSecurityFlag(result.Flags, UnicodeSecurityFlag.Restricted), Is.False);
        Assert.That(result.IdentifierValidation.IsValid, Is.True);
        Assert.That(result.IdentifierValidation.Status, Is.EqualTo(UnicodeIdentifierStatus.Valid));
        Assert.That(result.SecurityDiagnostics, Is.Empty);
    }

    [Test]
    public void CheckIdentifier_FlagsInvisibleJoinControlWithUtf16Offset()
    {
        UnicodeSecurityCheckResult result = new UnicodeSecurity().CheckIdentifier("A\u200D", new UnicodeProfiles().CreateImmutableIdentifier());

        Assert.That(result.IsSafe, Is.False);
        Assert.That(UnicodeSecurity.HasSecurityFlag(result.Flags, UnicodeSecurityFlag.Invisible), Is.True);
        Assert.That(UnicodeSecurity.HasSecurityFlag(result.Flags, UnicodeSecurityFlag.JoinControl), Is.True);
        Assert.That(UnicodeSecurity.HasSecurityFlag(result.Flags, UnicodeSecurityFlag.Restricted), Is.True);
        AssertSecurityDiagnostic(
            FindSecurityDiagnostic(result, UnicodeSecurity.JoinControlRule),
            1,
            1,
            0x200D,
            UnicodeSecurityFlag.JoinControl);
    }

    [Test]
    public void CheckIdentifier_FlagsBidiControlAsUnsafeSpoofingInput()
    {
        UnicodeSecurityCheckResult result = new UnicodeSecurity().CheckIdentifier("A\u202E", new UnicodeProfiles().CreateImmutableIdentifier());

        Assert.That(result.IsSafe, Is.False);
        Assert.That(UnicodeSecurity.HasSecurityFlag(result.Flags, UnicodeSecurityFlag.Invisible), Is.True);
        Assert.That(UnicodeSecurity.HasSecurityFlag(result.Flags, UnicodeSecurityFlag.BidiControl), Is.True);
        Assert.That(UnicodeSecurity.HasSecurityFlag(result.Flags, UnicodeSecurityFlag.Restricted), Is.True);
        AssertSecurityDiagnostic(
            FindSecurityDiagnostic(result, UnicodeSecurity.BidiControlRule),
            1,
            1,
            0x202E,
            UnicodeSecurityFlag.BidiControl);
    }

    [Test]
    public void CheckIdentifier_InvalidUtf16CannotBeSafe()
    {
        UnicodeSecurityCheckResult result = new UnicodeSecurity().CheckIdentifier("\uD800", new UnicodeProfiles().CreateDefaultIdentifier());

        Assert.That(result.IsSafe, Is.False);
        Assert.That(result.IdentifierValidation.Status, Is.EqualTo(UnicodeIdentifierStatus.InvalidScalar));
        Assert.That(result.RestrictionLevel, Is.EqualTo(UnicodeRestrictionLevel.Unrestricted));
        Assert.That(UnicodeSecurity.HasSecurityFlag(result.Flags, UnicodeSecurityFlag.Restricted), Is.True);
    }

    [Test]
    public void SafeAsciiIdentifier_RemainsNonConfusableBaseline()
    {
        UnicodeSecurityCheckResult result = new UnicodeSecurity().CheckIdentifier("AZ09", new UnicodeProfiles().CreateDefaultIdentifier());

        Assert.That(result.IsSafe, Is.True);
        Assert.That(result.Flags, Is.EqualTo(UnicodeSecurityFlag.None));
        Assert.That(new UnicodeSecurity().AreConfusable("AZ09", "BZ09"), Is.False);
    }

    static UnicodeSecurityDiagnostic FindSecurityDiagnostic(UnicodeSecurityCheckResult result, string rule)
    {
        int index = 0;

        while (index < result.SecurityDiagnostics.Length)
        {
            if (result.SecurityDiagnostics[index].Rule == rule)
            {
                return result.SecurityDiagnostics[index];
            }

            index = index + 1;
        }

        Assert.Fail("Missing security diagnostic rule " + rule);
        return result.SecurityDiagnostics[0];
    }

    static void AssertSecurityDiagnostic(
        UnicodeSecurityDiagnostic diagnostic,
        int utf16Index,
        int utf16Length,
        int codePoint,
        UnicodeSecurityFlag flag)
    {
        Assert.That(diagnostic.Utf16Index, Is.EqualTo(utf16Index));
        Assert.That(diagnostic.Utf16Length, Is.EqualTo(utf16Length));
        Assert.That(diagnostic.CodePoint, Is.EqualTo(codePoint));
        Assert.That(diagnostic.Flag, Is.EqualTo(flag));
        Assert.That(diagnostic.Rule, Is.Not.Empty);
        Assert.That(diagnostic.Message, Is.Not.Empty);
    }
}
