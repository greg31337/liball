using NUnit.Framework;

namespace unicode.Tests;

[TestFixture]
public class UnicodeIdentifierTableTests
{
    [Test]
    public void IdentifierTables_ContainProductionUax31IdentifierProperties()
    {
        Assert.That(UnicodeIdentifierTables.IdentifierPropertyNames(), Does.Contain("ID_Start"));
        Assert.That(UnicodeIdentifierTables.IdStart(), Has.Length.GreaterThan(700));
        Assert.That(UnicodeRangeSet.ContainsRanges(UnicodeIdentifierTables.IdStart(), 0x0041), Is.True);
        Assert.That(UnicodeRangeSet.ContainsRanges(UnicodeIdentifierTables.IdStart(), 0x10400), Is.True);
        Assert.That(UnicodeRangeSet.ContainsRanges(UnicodeIdentifierTables.IdContinue(), 0x0301), Is.True);
        Assert.That(UnicodeRangeSet.ContainsRanges(UnicodeIdentifierTables.IdStart(), 0x0030), Is.False);
    }

    [Test]
    public void IdentifierTables_ContainXidAndPatternProperties()
    {
        Assert.That(UnicodeRangeSet.ContainsRanges(UnicodeIdentifierTables.XidStart(), 0x0061), Is.True);
        Assert.That(UnicodeRangeSet.ContainsRanges(UnicodeIdentifierTables.XidContinue(), 0x0301), Is.True);
        Assert.That(UnicodeRangeSet.ContainsRanges(UnicodeIdentifierTables.PatternSyntax(), 0x0021), Is.True);
        Assert.That(UnicodeRangeSet.ContainsRanges(UnicodeIdentifierTables.PatternWhiteSpace(), 0x0020), Is.True);
        Assert.That(UnicodeRangeSet.ContainsRanges(UnicodeIdentifierTables.PatternSyntax(), 0x0041), Is.False);
    }
}
