using NUnit.Framework;

namespace version.Tests;

[TestFixture]
public class RubyGemsVersionOrderingTests
{
    [Test]
    public void Compare_OrdersDocumentedPrereleasesBetweenReleases()
    {
        AssertAscending(new string[]
        {
            "0.9",
            "1.0.a.2",
            "1.0.b1",
            "1.0"
        });
    }

    [Test]
    public void Compare_OrdersRationalVersionLifecycleExamples()
    {
        AssertAscending(new string[]
        {
            "0.0.1",
            "0.0.2",
            "0.1.0",
            "1.0.0",
            "1.1.0",
            "1.1.1",
            "1.1.2"
        });
    }

    [Test]
    public void Compare_OrdersMixedPrereleaseIdentifiers()
    {
        AssertAscending(new string[]
        {
            "1.0.a",
            "1.0.a.1",
            "1.0.a.2",
            "1.0.a.10",
            "1.0.b",
            "1.0.b1",
            "1.0"
        });
    }

    [Test]
    public void Compare_TreatsCanonicalEquivalentSpellingsAsEqual()
    {
        AssertEqual("1", "1.0");
        AssertEqual("1.0", "1.0.0.0");
        AssertEqual("1.0.a", "1.a");
        AssertEqual("1.0.0.a", "1.a");
        AssertEqual("1.0.a.0", "1.0.a");
        AssertEqual("1.0.a10", "1.0.a.10");
    }

    static void AssertAscending(string[] versions)
    {
        int leftIndex = 0;

        while (leftIndex < versions.Length)
        {
            int rightIndex = leftIndex + 1;

            while (rightIndex < versions.Length)
            {
                Assert.That(RubyGemsVersionComparer.Compare(versions[leftIndex], versions[rightIndex]), Is.EqualTo(-1));
                Assert.That(RubyGemsVersionComparer.Compare(versions[rightIndex], versions[leftIndex]), Is.EqualTo(1));
                rightIndex = rightIndex + 1;
            }

            leftIndex = leftIndex + 1;
        }
    }

    static void AssertEqual(string leftText, string rightText)
    {
        Assert.That(RubyGemsVersionComparer.Compare(leftText, rightText), Is.EqualTo(0));
        Assert.That(RubyGemsVersionComparer.Compare(rightText, leftText), Is.EqualTo(0));
    }
}
