using NUnit.Framework;

namespace unicode.Tests;

[TestFixture]
public class UnicodeSecurityDiagnosticTests
{
    [Test]
    public void GetSecurityDiagnostics_ReportsRuleCodesFlagsAndOffsets()
    {
        UnicodeSecurityDiagnostic[] diagnostics = new UnicodeSecurity().GetSecurityDiagnostics("A\u00AD\uFE0F\u200D\u202E");

        AssertDiagnostic(FindDiagnostic(diagnostics, UnicodeSecurity.DefaultIgnorableRule), 1, 1, 0x00AD, UnicodeSecurityFlag.DefaultIgnorable);
        AssertDiagnostic(FindDiagnostic(diagnostics, UnicodeSecurity.VariationSelectorRule), 2, 1, 0xFE0F, UnicodeSecurityFlag.VariationSelector);
        AssertDiagnostic(FindDiagnostic(diagnostics, UnicodeSecurity.JoinControlRule), 3, 1, 0x200D, UnicodeSecurityFlag.JoinControl);
        AssertDiagnostic(FindDiagnostic(diagnostics, UnicodeSecurity.BidiControlRule), 4, 1, 0x202E, UnicodeSecurityFlag.BidiControl);
        Assert.That(CountRule(diagnostics, UnicodeSecurity.InvisibleRule), Is.EqualTo(4));
    }

    [Test]
    public void GetSecurityDiagnostics_ReportsSupplementaryVariationSelectorUtf16Length()
    {
        UnicodeSecurityDiagnostic[] diagnostics = new UnicodeSecurity().GetSecurityDiagnostics("A\U000E0100");
        UnicodeSecurityDiagnostic diagnostic = FindDiagnostic(diagnostics, UnicodeSecurity.VariationSelectorRule);

        AssertDiagnostic(diagnostic, 1, 2, 0xE0100, UnicodeSecurityFlag.VariationSelector);
    }

    [Test]
    public void GetSecurityDiagnostics_ReturnsEmptyForPlainText()
    {
        Assert.That(new UnicodeSecurity().GetSecurityDiagnostics("PlainText09"), Is.Empty);
    }

    [Test]
    public void GetSecurityDiagnostics_ReportsRepeatedMarksWithinOneMarkSequence()
    {
        UnicodeSecurity security = new UnicodeSecurity();

        UnicodeSecurityDiagnostic[] repeated = security.GetSecurityDiagnostics("a\u0301\u0300\u0301");
        UnicodeSecurityDiagnostic diagnostic = FindDiagnosticAt(repeated, UnicodeSecurity.InvisibleRule, 3);

        AssertDiagnostic(diagnostic, 3, 1, 0x0301, UnicodeSecurityFlag.Invisible);
        Assert.That(CountRule(security.GetSecurityDiagnostics("a\u0301b\u0301"), UnicodeSecurity.InvisibleRule), Is.EqualTo(0));
        Assert.That(CountRule(security.GetSecurityDiagnostics("a\u0301\u0300"), UnicodeSecurity.InvisibleRule), Is.EqualTo(0));
        Assert.That(FindDiagnosticAt(
            security.GetSecurityDiagnostics("a\u0301\u0341"),
            UnicodeSecurity.InvisibleRule,
            2).CodePoint, Is.EqualTo(0x0341));
        Assert.That(FindDiagnosticAt(
            security.GetSecurityDiagnostics("\u00E9\u0301"),
            UnicodeSecurity.InvisibleRule,
            1).CodePoint, Is.EqualTo(0x0301));
    }

    [Test]
    public void SecurityCharacterClassifiers_ReturnRepresentativeResults()
    {
        Assert.That(UnicodeSecurity.IsDefaultIgnorable(0x00AD), Is.True);
        Assert.That(UnicodeSecurity.IsVariationSelector(0xFE0F), Is.True);
        Assert.That(UnicodeSecurity.IsVariationSelector(0xE0100), Is.True);
        Assert.That(UnicodeSecurity.IsJoinControl(0x200C), Is.True);
        Assert.That(UnicodeSecurity.IsJoinControl(0x200D), Is.True);
        Assert.That(UnicodeSecurity.IsBidiControl(0x202E), Is.True);
        Assert.That(UnicodeSecurity.IsBidiControl(0x2067), Is.True);
        Assert.That(UnicodeSecurity.IsInvisible(0x200B), Is.True);
        Assert.That(UnicodeSecurity.IsInvisible(0x0041), Is.False);
    }

    [Test]
    public void GetSecurityDiagnostics_RejectsNullText()
    {
        Assert.Throws<ArgumentNullException>(() => new UnicodeSecurity().GetSecurityDiagnostics(null!));
    }

    static UnicodeSecurityDiagnostic FindDiagnostic(UnicodeSecurityDiagnostic[] diagnostics, string rule)
    {
        int index = 0;

        while (index < diagnostics.Length)
        {
            if (diagnostics[index].Rule == rule)
            {
                return diagnostics[index];
            }

            index = index + 1;
        }

        Assert.Fail("Missing diagnostic rule " + rule);
        return diagnostics[0];
    }

    static int CountRule(UnicodeSecurityDiagnostic[] diagnostics, string rule)
    {
        int count = 0;
        int index = 0;

        while (index < diagnostics.Length)
        {
            if (diagnostics[index].Rule == rule)
            {
                count = count + 1;
            }

            index = index + 1;
        }

        return count;
    }

    static UnicodeSecurityDiagnostic FindDiagnosticAt(
        UnicodeSecurityDiagnostic[] diagnostics,
        string rule,
        int utf16Index)
    {
        int index = 0;
        while (index < diagnostics.Length)
        {
            if (diagnostics[index].Rule == rule && diagnostics[index].Utf16Index == utf16Index)
            {
                return diagnostics[index];
            }

            index = index + 1;
        }

        Assert.Fail("Missing diagnostic rule " + rule + " at index " + utf16Index.ToString());
        return diagnostics[0];
    }

    static void AssertDiagnostic(
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
        Assert.That(diagnostic.Message, Is.Not.Empty);
    }
}
