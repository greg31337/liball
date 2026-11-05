using NUnit.Framework;

namespace unicode.Tests;

[TestFixture]
public class UnicodeSecurityConfusableTests
{
    [Test]
    public void AreConfusable_ReturnsTrueWhenSkeletonsMatch()
    {
        Assert.That(new UnicodeSecurity().AreConfusable("\u0391", "A"), Is.True);
        Assert.That(new UnicodeSecurity().AreConfusable("m", "rn"), Is.True);
        Assert.That(new UnicodeSecurity().AreConfusable("1", "l"), Is.True);
    }

    [Test]
    public void AreConfusable_ReturnsFalseWhenSkeletonsDiffer()
    {
        Assert.That(new UnicodeSecurity().AreConfusable("A", "B"), Is.False);
        Assert.That(new UnicodeSecurity().AreConfusable("A", "a"), Is.False);
        Assert.That(new UnicodeSecurity().AreConfusable("", "A"), Is.False);
    }

    [Test]
    public void AreConfusable_ReturnsFalseForIdenticalText()
    {
        Assert.That(new UnicodeSecurity().AreConfusable("A", "A"), Is.False);
        Assert.That(new UnicodeSecurity().AreConfusable("", ""), Is.False);
    }

    [Test]
    public void AreConfusable_RejectsNullArguments()
    {
        Assert.Throws<ArgumentNullException>(() => new UnicodeSecurity().AreConfusable(null!, "A"));
        Assert.Throws<ArgumentNullException>(() => new UnicodeSecurity().AreConfusable("A", null!));
    }

    [Test]
    public void AreMixedScriptConfusable_ClassifiesDisjointResolvedScripts()
    {
        UnicodeSecurity security = new UnicodeSecurity();

        Assert.That(security.AreMixedScriptConfusable("paypal", "p\u0430yp\u0430l"), Is.True);
        Assert.That(security.AreMixedScriptConfusable("\u01C9eto", "ljeto"), Is.False);
        Assert.That(security.AreMixedScriptConfusable("scope", "unrelated"), Is.False);
    }

    [Test]
    public void AreWholeScriptConfusable_RequiresTwoSingleScriptStrings()
    {
        UnicodeSecurity security = new UnicodeSecurity();

        Assert.That(security.AreWholeScriptConfusable("scope", "\u0455\u0441\u043E\u0440\u0435"), Is.True);
        Assert.That(security.AreWholeScriptConfusable("paypal", "p\u0430yp\u0430l"), Is.False);
        Assert.That(security.AreWholeScriptConfusable("\u01C9eto", "ljeto"), Is.False);
    }

    [Test]
    public void ConfusableClassifiers_HandleInheritedCommonAndMixedInputs()
    {
        UnicodeSecurity security = new UnicodeSecurity();

        Assert.That(security.AreWholeScriptConfusable("A\u0301", "\u0391\u0301"), Is.True);
        Assert.That(security.AreMixedScriptConfusable("AA", "A\u0391"), Is.True);
        Assert.That(security.AreWholeScriptConfusable("AA", "A\u0391"), Is.False);
        Assert.That(security.AreConfusable("0", "O"), Is.True);
        Assert.That(security.AreMixedScriptConfusable("0", "O"), Is.False);
    }

    [Test]
    public void GetConfusableFlags_ReportsOnlyChecksThatSucceeded()
    {
        UnicodeSecurity security = new UnicodeSecurity();

        UnicodeSecurityFlag wholeScript = security.GetConfusableFlags("scope", "\u0455\u0441\u043E\u0440\u0435");
        UnicodeSecurityFlag mixedScript = security.GetConfusableFlags("paypal", "p\u0430yp\u0430l");

        Assert.That(UnicodeSecurity.HasSecurityFlag(wholeScript, UnicodeSecurityFlag.Confusable), Is.True);
        Assert.That(UnicodeSecurity.HasSecurityFlag(wholeScript, UnicodeSecurityFlag.WholeScriptConfusable), Is.True);
        Assert.That(UnicodeSecurity.HasSecurityFlag(mixedScript, UnicodeSecurityFlag.Confusable), Is.True);
        Assert.That(UnicodeSecurity.HasSecurityFlag(mixedScript, UnicodeSecurityFlag.WholeScriptConfusable), Is.False);
        Assert.That(security.GetConfusableFlags("scope", "unrelated"), Is.EqualTo(UnicodeSecurityFlag.None));
        Assert.That(security.GetConfusableFlags("scope", "scope"), Is.EqualTo(UnicodeSecurityFlag.None));
    }

    [Test]
    public void CheckIdentifierAgainst_AddsPairAwareConfusableFlags()
    {
        UnicodeSecurityCheckResult result = new UnicodeSecurity().CheckIdentifierAgainst(
            "\u0455\u0441\u043E\u0440\u0435",
            "scope",
            new UnicodeProfiles().CreateImmutableIdentifier());

        Assert.That(result.IsSafe, Is.False);
        Assert.That(result.IdentifierValidation.IsValid, Is.True);
        Assert.That(UnicodeSecurity.HasSecurityFlag(result.Flags, UnicodeSecurityFlag.Confusable), Is.True);
        Assert.That(UnicodeSecurity.HasSecurityFlag(result.Flags, UnicodeSecurityFlag.WholeScriptConfusable), Is.True);
    }

    [Test]
    public void ConfusableClassifiers_RejectNullArguments()
    {
        UnicodeSecurity security = new UnicodeSecurity();
        IUnicodeIdentifierProfile profile = new UnicodeProfiles().CreateDefaultIdentifier();

        Assert.Throws<ArgumentNullException>(() => security.AreMixedScriptConfusable(null!, "A"));
        Assert.Throws<ArgumentNullException>(() => security.AreMixedScriptConfusable("A", null!));
        Assert.Throws<ArgumentNullException>(() => security.AreWholeScriptConfusable(null!, "A"));
        Assert.Throws<ArgumentNullException>(() => security.AreWholeScriptConfusable("A", null!));
        Assert.Throws<ArgumentNullException>(() => security.GetConfusableFlags(null!, "A"));
        Assert.Throws<ArgumentNullException>(() => security.GetConfusableFlags("A", null!));
        Assert.Throws<ArgumentNullException>(() => security.CheckIdentifierAgainst(null!, "A", profile));
        Assert.Throws<ArgumentNullException>(() => security.CheckIdentifierAgainst("A", null!, profile));
        Assert.Throws<ArgumentNullException>(() => security.CheckIdentifierAgainst("A", "B", null!));
    }
}
