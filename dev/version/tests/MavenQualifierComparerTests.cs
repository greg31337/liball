using NUnit.Framework;

namespace version.Tests;

[TestFixture]
public class MavenQualifierComparerTests
{
    [Test]
    public void NormalizeQualifier_MapsKnownAliases()
    {
        Assert.That(MavenQualifierComparer.NormalizeQualifier("a"), Is.EqualTo("alpha"));
        Assert.That(MavenQualifierComparer.NormalizeQualifier("b"), Is.EqualTo("beta"));
        Assert.That(MavenQualifierComparer.NormalizeQualifier("m"), Is.EqualTo("milestone"));
        Assert.That(MavenQualifierComparer.NormalizeQualifier("cr"), Is.EqualTo("rc"));
        Assert.That(MavenQualifierComparer.NormalizeQualifier("ga"), Is.EqualTo(""));
        Assert.That(MavenQualifierComparer.NormalizeQualifier("final"), Is.EqualTo(""));
        Assert.That(MavenQualifierComparer.NormalizeQualifier("release"), Is.EqualTo(""));
        Assert.That(MavenQualifierComparer.NormalizeQualifier("RC"), Is.EqualTo("rc"));
    }

    [Test]
    public void CompareQualifiers_OrdersKnownQualifiers()
    {
        AssertAscending(new string[]
        {
            "alpha",
            "beta",
            "milestone",
            "rc",
            "snapshot",
            "",
            "sp",
            "unknown"
        });
    }

    [Test]
    public void CompareQualifiers_TreatsAliasesAsEquivalent()
    {
        AssertSame("a", "alpha");
        AssertSame("b", "beta");
        AssertSame("m", "milestone");
        AssertSame("cr", "rc");
        AssertSame("ga", "");
        AssertSame("final", "");
        AssertSame("release", "");
    }

    [Test]
    public void CompareQualifiers_IsCaseInsensitive()
    {
        AssertSame("ALPHA", "alpha");
        AssertSame("Rc", "rc");
        AssertSame("SNAPSHOT", "snapshot");
        Assert.That(MavenQualifierComparer.CompareQualifiers("zzz", "ZZZ"), Is.EqualTo(0));
    }

    [Test]
    public void CompareQualifiers_OrdersUnknownQualifiersLexicallyAfterKnownOnes()
    {
        Assert.That(MavenQualifierComparer.IsKnownQualifier("sp"), Is.True);
        Assert.That(MavenQualifierComparer.IsKnownQualifier("custom"), Is.False);
        Assert.That(MavenQualifierComparer.CompareQualifiers("sp", "custom"), Is.EqualTo(-1));
        Assert.That(MavenQualifierComparer.CompareQualifiers("aaa", "zzz"), Is.EqualTo(-1));
        Assert.That(MavenQualifierComparer.CompareQualifiers("zzz", "aaa"), Is.EqualTo(1));
    }

    static void AssertAscending(string[] qualifiers)
    {
        int index = 0;

        while (index < qualifiers.Length - 1)
        {
            Assert.That(MavenQualifierComparer.CompareQualifiers(qualifiers[index], qualifiers[index + 1]), Is.EqualTo(-1));
            Assert.That(MavenQualifierComparer.CompareQualifiers(qualifiers[index + 1], qualifiers[index]), Is.EqualTo(1));
            index = index + 1;
        }
    }

    static void AssertSame(string left, string right)
    {
        Assert.That(MavenQualifierComparer.CompareQualifiers(left, right), Is.EqualTo(0));
        Assert.That(MavenQualifierComparer.CompareQualifiers(right, left), Is.EqualTo(0));
    }
}
