using NUnit.Framework;

namespace unicode.Tests;

[TestFixture]
public class UnicodeCaseTableTests
{
    [Test]
    public void SimpleCaseTables_ContainBmpAndSupplementaryMappings()
    {
        AssertParallelMapping(0x0061, 0x0041, UnicodeCaseTables.SimpleUppercaseCodePoints(), UnicodeCaseTables.SimpleUppercaseMappings());
        AssertParallelMapping(0x0041, 0x0061, UnicodeCaseTables.SimpleLowercaseCodePoints(), UnicodeCaseTables.SimpleLowercaseMappings());
        AssertParallelMapping(0x10428, 0x10400, UnicodeCaseTables.SimpleUppercaseCodePoints(), UnicodeCaseTables.SimpleUppercaseMappings());
    }

    [Test]
    public void SpecialCaseTables_ContainExpansionLocaleAndContextMappings()
    {
        Assert.That(UnicodeCaseTables.SpecialCaseCodePoints(), Does.Contain(0x00DF));
        Assert.That(UnicodeCaseTables.SpecialConditionNames(), Does.Contain("Final_Sigma"));
        Assert.That(UnicodeCaseTables.SpecialConditionNames(), Does.Contain("tr"));
        Assert.That(UnicodeCaseTables.SpecialConditionNames(), Does.Contain("lt"));
        Assert.That(UnicodeCaseTables.SpecialUppercaseMappings(), Does.Contain(0x0053));
    }

    [Test]
    public void CaseFoldTables_ContainCommonFullAndTurkicMappings()
    {
        int sharpS = FindFold(0x00DF, "F");
        Assert.That(FoldMapping(sharpS), Is.EqualTo(new int[] { 0x0073, 0x0073 }));

        int turkicI = FindFold(0x0049, "T");
        Assert.That(FoldMapping(turkicI), Is.EqualTo(new int[] { 0x0131 }));
        Assert.That(UnicodeCaseTables.CaseFoldCodePoints(), Has.Length.GreaterThan(1500));
    }

    static void AssertParallelMapping(int source, int target, int[] sources, int[] targets)
    {
        int index = Array.IndexOf(sources, source);
        Assert.That(index, Is.GreaterThanOrEqualTo(0));
        Assert.That(targets[index], Is.EqualTo(target));
    }

    static int FindFold(int codePoint, string status)
    {
        int[] codePoints = UnicodeCaseTables.CaseFoldCodePoints();
        int[] statusIndexes = UnicodeCaseTables.CaseFoldStatusIndexes();
        string[] statusNames = UnicodeCaseTables.CaseFoldStatusNames();
        int index = 0;
        while (index < codePoints.Length)
        {
            if (codePoints[index] == codePoint && statusNames[statusIndexes[index]] == status)
            {
                return index;
            }

            index = index + 1;
        }

        Assert.Fail("Missing fold mapping");
        return -1;
    }

    static int[] FoldMapping(int index)
    {
        int[] offsets = UnicodeCaseTables.CaseFoldMappingOffsets();
        int[] lengths = UnicodeCaseTables.CaseFoldMappingLengths();
        int[] mappings = UnicodeCaseTables.CaseFoldMappings();
        int[] result = new int[lengths[index]];
        Array.Copy(mappings, offsets[index], result, 0, result.Length);
        return result;
    }
}
