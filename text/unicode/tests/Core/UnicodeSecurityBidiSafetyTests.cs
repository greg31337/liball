using NUnit.Framework;

namespace unicode.Tests;

[TestFixture]
public class UnicodeSecurityBidiSafetyTests
{
    [Test]
    public void GetIdentifierBidiDiagnostics_AcceptsSafeRtlAndOrdinaryLtrIdentifiers()
    {
        UnicodeSecurity security = new UnicodeSecurity();

        Assert.That(security.GetIdentifierBidiDiagnostics("Plain-09"), Is.Empty);
        Assert.That(security.GetIdentifierBidiDiagnostics("\u05D0\u05D1\u0301"), Is.Empty);
        Assert.That(security.GetIdentifierBidiDiagnostics("\u0627\u0661"), Is.Empty);
    }

    [Test]
    public void GetIdentifierBidiDiagnostics_ReportsDirectionAndDigitViolations()
    {
        UnicodeSecurity security = new UnicodeSecurity();

        UnicodeSecurityDiagnostic[] mixedDirection = security.GetIdentifierBidiDiagnostics("\u05D0A");
        UnicodeSecurityDiagnostic[] badEnd = security.GetIdentifierBidiDiagnostics("\u05D0-");
        UnicodeSecurityDiagnostic[] mixedDigits = security.GetIdentifierBidiDiagnostics("\u0627\u0661" + "1");

        Assert.That(ContainsRule(mixedDirection, UnicodeSecurityRules.IdentifierBidiRule), Is.True);
        Assert.That(ContainsCodePoint(mixedDirection, 0x0041), Is.True);
        Assert.That(ContainsCodePoint(badEnd, 0x002D), Is.True);
        Assert.That(ContainsCodePoint(mixedDigits, 0x0031), Is.True);
        Assert.That(AllHaveFlag(mixedDigits, UnicodeSecurityFlag.BidiUnsafe), Is.True);
    }

    [Test]
    public void GetLabelBidiDiagnostics_EnforcesLtrAndRtlLabelRules()
    {
        UnicodeSecurity security = new UnicodeSecurity();

        Assert.That(security.GetLabelBidiDiagnostics("abc-1"), Is.Empty);
        Assert.That(security.GetLabelBidiDiagnostics("\u05D0\u05D1" + "1"), Is.Empty);
        Assert.That(ContainsCodePoint(security.GetLabelBidiDiagnostics("1abc"), 0x0031), Is.True);
        Assert.That(ContainsCodePoint(security.GetLabelBidiDiagnostics("abc-"), 0x002D), Is.True);
        Assert.That(ContainsRule(security.GetLabelBidiDiagnostics(""), UnicodeSecurityRules.LabelBidiRule), Is.True);
    }

    [Test]
    public void GetLabelBidiDiagnostics_AcceptsEveryPermittedWeakAndNeutralClass()
    {
        UnicodeSecurity security = new UnicodeSecurity();

        Assert.That(security.GetLabelBidiDiagnostics("a1-,#_\u200D\u0301b"), Is.Empty);
        Assert.That(security.GetLabelBidiDiagnostics("\u05D0+,$(_\u200D\u0301\u05D1"), Is.Empty);
        Assert.That(security.GetLabelBidiDiagnostics("\u0627\u0661\u0628"), Is.Empty);
    }

    [Test]
    public void GetLabelBidiDiagnostics_RejectsEveryGlobalRuleShape()
    {
        UnicodeSecurity security = new UnicodeSecurity();

        Assert.That(security.GetLabelBidiDiagnostics("1abc"), Is.Not.Empty);
        Assert.That(security.GetLabelBidiDiagnostics("\u05D0A\u05D1"), Is.Not.Empty);
        Assert.That(security.GetLabelBidiDiagnostics("\u05D0-"), Is.Not.Empty);
        Assert.That(security.GetLabelBidiDiagnostics("\u0627\u0661" + "1"), Is.Not.Empty);
        Assert.That(security.GetLabelBidiDiagnostics("a\u05D0b"), Is.Not.Empty);
        Assert.That(security.GetLabelBidiDiagnostics("a-"), Is.Not.Empty);
    }

    [Test]
    public void CheckIdentifier_AggregatesBidiSafetySeparatelyFromBidiControls()
    {
        UnicodeSecurityCheckResult result = new UnicodeSecurity().CheckIdentifier(
            "\u05D0A",
            new UnicodeProfiles().CreateImmutableIdentifier());

        Assert.That(result.IsSafe, Is.False);
        Assert.That(UnicodeSecurity.HasSecurityFlag(result.Flags, UnicodeSecurityFlag.BidiUnsafe), Is.True);
        Assert.That(UnicodeSecurity.HasSecurityFlag(result.Flags, UnicodeSecurityFlag.BidiControl), Is.False);
        Assert.That(ContainsRule(result.SecurityDiagnostics, UnicodeSecurityRules.IdentifierBidiRule), Is.True);
    }

    [Test]
    public void GetSecurityDiagnostics_KeepsControlAndBidiDiagnosticsInInputOrder()
    {
        UnicodeSecurityDiagnostic[] diagnostics = new UnicodeSecurity().GetSecurityDiagnostics("\u05D0A\u202E");

        Assert.That(IndexesAreNondecreasing(diagnostics), Is.True);
        Assert.That(ContainsRule(diagnostics, UnicodeSecurityRules.IdentifierBidiRule), Is.True);
        Assert.That(ContainsRule(diagnostics, UnicodeSecurityRules.BidiControlRule), Is.True);
    }

    [Test]
    public void BidiSafetyChecks_RejectNullArguments()
    {
        UnicodeSecurity security = new UnicodeSecurity();

        Assert.Throws<ArgumentNullException>(() => security.GetIdentifierBidiDiagnostics(null!));
        Assert.Throws<ArgumentNullException>(() => security.GetLabelBidiDiagnostics(null!));
    }

    static bool ContainsRule(UnicodeSecurityDiagnostic[] diagnostics, string rule)
    {
        int index = 0;
        while (index < diagnostics.Length)
        {
            if (diagnostics[index].Rule == rule)
            {
                return true;
            }

            index = index + 1;
        }

        return false;
    }

    static bool ContainsCodePoint(UnicodeSecurityDiagnostic[] diagnostics, int codePoint)
    {
        int index = 0;
        while (index < diagnostics.Length)
        {
            if (diagnostics[index].CodePoint == codePoint)
            {
                return true;
            }

            index = index + 1;
        }

        return false;
    }

    static bool AllHaveFlag(UnicodeSecurityDiagnostic[] diagnostics, UnicodeSecurityFlag flag)
    {
        if (diagnostics.Length == 0)
        {
            return false;
        }

        int index = 0;
        while (index < diagnostics.Length)
        {
            if (diagnostics[index].Flag != flag)
            {
                return false;
            }

            index = index + 1;
        }

        return true;
    }

    static bool IndexesAreNondecreasing(UnicodeSecurityDiagnostic[] diagnostics)
    {
        int index = 1;
        while (index < diagnostics.Length)
        {
            if (diagnostics[index].Utf16Index < diagnostics[index - 1].Utf16Index)
            {
                return false;
            }

            index = index + 1;
        }

        return true;
    }
}
