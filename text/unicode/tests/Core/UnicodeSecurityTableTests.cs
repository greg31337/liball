using NUnit.Framework;

namespace unicode.Tests;

[TestFixture]
public class UnicodeSecurityTableTests
{
    [Test]
    public void IdentifierStatusTables_ContainProductionAllowedRanges()
    {
        Assert.That(UnicodeSecurityTables.IdentifierStatusNames(), Does.Contain("Allowed"));
        Assert.That(UnicodeSecurityTables.AllowedIdentifierStatus(), Has.Length.GreaterThan(1000));
        Assert.That(UnicodeRangeSet.ContainsRanges(UnicodeSecurityTables.AllowedIdentifierStatus(), 0x0041), Is.True);
        Assert.That(UnicodeRangeSet.ContainsRanges(UnicodeSecurityTables.AllowedIdentifierStatus(), 0x03B1), Is.True);
        Assert.That(UnicodeRangeSet.ContainsRanges(UnicodeSecurityTables.AllowedIdentifierStatus(), 0x1F600), Is.False);
    }

    [Test]
    public void ConfusableTables_ContainProductionMappings()
    {
        Assert.That(UnicodeSecurityTables.ConfusableTypeNames(), Does.Contain("MA"));
        Assert.That(UnicodeSecurityTables.ConfusableSourceOffsets(), Has.Length.GreaterThan(6000));
        AssertConfusableMapping(0x0391, new int[] { 0x0041 });
        AssertConfusableMapping(0x006D, new int[] { 0x0072, 0x006E });
    }

    [Test]
    public void IdentifierTypeTables_ContainProductionRestrictionInputs()
    {
        Assert.That(UnicodeSecurityTables.IdentifierTypeNames(), Does.Contain("Recommended"));
        Assert.That(UnicodeSecurityTables.IdentifierTypeNames(), Does.Contain("Not_NFKC"));
        Assert.That(UnicodeSecurityTables.IdentifierTypeNames(), Does.Contain("Default_Ignorable"));
        Assert.That(UnicodeSecurityTables.IdentifierTypeStarts(), Has.Length.GreaterThan(1000));
    }

    static void AssertConfusableMapping(int source, int[] expected)
    {
        int[] offsets = UnicodeSecurityTables.ConfusableSourceOffsets();
        int[] lengths = UnicodeSecurityTables.ConfusableSourceLengths();
        int[] sources = UnicodeSecurityTables.ConfusableSourceCodePoints();
        int record = Enumerable.Range(0, offsets.Length).Single(index => lengths[index] == 1 && sources[offsets[index]] == source);
        int targetOffset = UnicodeSecurityTables.ConfusableTargetOffsets()[record];
        int targetLength = UnicodeSecurityTables.ConfusableTargetLengths()[record];
        int[] target = new int[targetLength];
        Array.Copy(UnicodeSecurityTables.ConfusableTargetCodePoints(), targetOffset, target, 0, targetLength);
        Assert.That(target, Is.EqualTo(expected));
    }
}
