using NUnit.Framework;

namespace version.Tests;

[TestFixture]
public class SemVerSpecificationPrecedenceTests
{
    [Test]
    public void ComparePrecedence_OrdersSpecificationCoreVersionExample()
    {
        AssertAscendingPrecedence(new string[]
        {
            "1.0.0",
            "2.0.0",
            "2.1.0",
            "2.1.1"
        });
    }

    [Test]
    public void ComparePrecedence_OrdersSpecificationPrereleaseExample()
    {
        AssertAscendingPrecedence(new string[]
        {
            "1.0.0-alpha",
            "1.0.0-alpha.1",
            "1.0.0-alpha.beta",
            "1.0.0-beta",
            "1.0.0-beta.2",
            "1.0.0-beta.11",
            "1.0.0-rc.1",
            "1.0.0"
        });
    }

    [Test]
    public void ComparePrecedence_IgnoresSpecificationBuildMetadataExamples()
    {
        AssertSamePrecedence("1.0.0-alpha+001", "1.0.0-alpha+20130313144700");
        AssertSamePrecedence("1.0.0+20130313144700", "1.0.0+21AF26D3----117B344092BD");
        AssertSamePrecedence("1.0.0-beta+exp.sha.5114f85", "1.0.0-beta+build.7");
    }

    static void AssertAscendingPrecedence(string[] versions)
    {
        int leftIndex = 0;

        while (leftIndex < versions.Length)
        {
            int rightIndex = leftIndex + 1;

            while (rightIndex < versions.Length)
            {
                UniversalVersion left = ParseVersion(versions[leftIndex]);
                UniversalVersion right = ParseVersion(versions[rightIndex]);

                Assert.That(SemVerVersionComparer.ComparePrecedence(left, right), Is.EqualTo(-1));
                Assert.That(SemVerVersionComparer.ComparePrecedence(right, left), Is.EqualTo(1));

                rightIndex = rightIndex + 1;
            }

            leftIndex = leftIndex + 1;
        }
    }

    static void AssertSamePrecedence(string leftText, string rightText)
    {
        UniversalVersion left = ParseVersion(leftText);
        UniversalVersion right = ParseVersion(rightText);

        Assert.That(SemVerVersionComparer.ComparePrecedence(left, right), Is.EqualTo(0));
        Assert.That(SemVerVersionComparer.ComparePrecedence(right, left), Is.EqualTo(0));
        Assert.That(left.EqualsByPrecedence(right), Is.True);
    }

    static UniversalVersion ParseVersion(string text)
    {
        VersionParseResult result = SemVerParser.ParseStrict(text);

        Assert.That(result.IsSuccess(), Is.True);
        return result.GetVersion();
    }
}
