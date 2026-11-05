using NUnit.Framework;

namespace version.Tests;

[TestFixture]
public class MavenVersionOfficialExampleTests
{
    [Test]
    public void Compare_HandlesMavenDocumentationSnapshotExample()
    {
        AssertCompare("1.2.7", "1.2-SNAPSHOT", 1);
        AssertCompare("1.2-SNAPSHOT", "1.2-snapshot", 0);
    }

    [Test]
    public void Compare_HandlesMavenDocumentationHyphenExamples()
    {
        AssertCompare("1.0.RC2", "1.0-RC3", -1);
        AssertCompare("1.0-RC3", "1.0.1", -1);
        AssertCompare("1.0.X2", "1.0-X3", -1);
        AssertCompare("1.0-X3", "1.0.1", -1);
    }

    [Test]
    public void Compare_PrefersHyphenQualifierWhenAllItemsMatch()
    {
        AssertCompare("1.0.0-RC1", "1.0.0.RC1", 1);
        AssertCompare("1.0.0-X1", "1.0.0.X1", 1);
    }

    [Test]
    public void Compare_CoversAdditionalMavenEdgeCases()
    {
        AssertCompare("1.0", "1", 0);
        AssertCompare("1.0-sp", "1.0", 1);
        AssertCompare("1.0-foo", "1.0-bar", 1);
        AssertCompare("1.0-alpha-1", "1.0-alpha", 1);
    }

    static void AssertCompare(string leftText, string rightText, int expected)
    {
        Assert.That(MavenVersionComparer.Compare(leftText, rightText), Is.EqualTo(expected));
        Assert.That(MavenVersionComparer.Compare(rightText, leftText), Is.EqualTo(0 - expected));
    }
}
