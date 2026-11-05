using NUnit.Framework;

namespace unicode.Tests;

[TestFixture]
public class UnicodeNormalizationTableTests
{
    [Test]
    public void DecompositionTables_ContainCanonicalAndCompatibilityMappings()
    {
        AssertDecomposition(0x00C0, "", new int[] { 0x0041, 0x0300 });
        AssertDecomposition(0x00BD, "fraction", new int[] { 0x0031, 0x2044, 0x0032 });
        Assert.That(UnicodeNormalizationTables.DecompositionCodePoints(), Has.Length.GreaterThan(5000));
    }

    [Test]
    public void CompositionExclusionTable_ContainsOfficialExclusions()
    {
        UnicodeRangeValue[] exclusions = UnicodeNormalizationTables.CompositionExclusions();

        Assert.That(exclusions, Has.Length.GreaterThan(50));
        Assert.That(exclusions.Any(range => range.Contains(0x0958)), Is.True);
        Assert.That(exclusions.Any(range => range.Contains(0x0F43)), Is.True);
        Assert.That(exclusions.All(range => range.Value == 1), Is.True);
    }

    [Test]
    public void QuickCheckTables_ContainNoAndMaybeRowsForAllForms()
    {
        Assert.That(UnicodeNormalizationTables.QuickCheckStarts(), Has.Length.GreaterThan(1000));
        Assert.That(UnicodeNormalizationTables.QuickCheckFormNames(), Is.EquivalentTo(new string[] { "NFC", "NFD", "NFKC", "NFKD" }));
        Assert.That(UnicodeNormalizationTables.QuickCheckValueNames(), Is.EquivalentTo(new string[] { "N", "M" }));
        AssertQuickCheck(0x0340, "NFC", "N");
        AssertQuickCheck(0x0300, "NFC", "M");
    }

    static void AssertDecomposition(int codePoint, string type, int[] expected)
    {
        int[] codePoints = UnicodeNormalizationTables.DecompositionCodePoints();
        int index = Array.IndexOf(codePoints, codePoint);
        Assert.That(index, Is.GreaterThanOrEqualTo(0));
        string[] typeNames = UnicodeNormalizationTables.DecompositionTypeNames();
        Assert.That(typeNames[UnicodeNormalizationTables.DecompositionTypeIndexes()[index]], Is.EqualTo(type));
        int offset = UnicodeNormalizationTables.DecompositionMappingOffsets()[index];
        int length = UnicodeNormalizationTables.DecompositionMappingLengths()[index];
        int[] mapping = new int[length];
        Array.Copy(UnicodeNormalizationTables.DecompositionMappingCodePoints(), offset, mapping, 0, length);
        Assert.That(mapping, Is.EqualTo(expected));
    }

    static void AssertQuickCheck(int codePoint, string form, string value)
    {
        int[] starts = UnicodeNormalizationTables.QuickCheckStarts();
        int[] ends = UnicodeNormalizationTables.QuickCheckEnds();
        int[] forms = UnicodeNormalizationTables.QuickCheckFormIndexes();
        int[] values = UnicodeNormalizationTables.QuickCheckValueIndexes();
        string[] formNames = UnicodeNormalizationTables.QuickCheckFormNames();
        string[] valueNames = UnicodeNormalizationTables.QuickCheckValueNames();
        Assert.That(
            Enumerable.Range(0, starts.Length).Any(index =>
                codePoint >= starts[index] && codePoint <= ends[index] &&
                formNames[forms[index]] == form && valueNames[values[index]] == value),
            Is.True);
    }
}
