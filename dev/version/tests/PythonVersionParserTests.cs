using NUnit.Framework;

namespace version.Tests;

[TestFixture]
public class PythonVersionParserTests
{
    [Test]
    public void Parse_NormalizesDocumentedAlternateSpellings()
    {
        PythonVersion rc = ParseVersion("v1.1RC1");
        Assert.That(rc.GetEpoch(), Is.EqualTo("0"));
        Assert.That(rc.GetReleaseSegments(), Is.EqualTo(new string[] { "1", "1" }));
        Assert.That(rc.HasPreRelease(), Is.True);
        Assert.That(rc.GetPreReleaseLabel(), Is.EqualTo("rc"));
        Assert.That(rc.GetPreReleaseNumber(), Is.EqualTo("1"));

        PythonVersion post = ParseVersion("1.2-post2");
        Assert.That(post.HasPostRelease(), Is.True);
        Assert.That(post.GetPostReleaseNumber(), Is.EqualTo("2"));

        PythonVersion local = ParseVersion("1.0+ubuntu-1");
        Assert.That(local.HasLocalVersion(), Is.True);
        Assert.That(local.GetLocalSegmentCount(), Is.EqualTo(2));
        Assert.That(local.GetLocalSegment(0).GetText(), Is.EqualTo("ubuntu"));
        Assert.That(local.GetLocalSegment(1).GetText(), Is.EqualTo("1"));
    }

    [Test]
    public void Compare_UsesPep440ReleaseAndSuffixOrdering()
    {
        AssertCompare("1.0.dev1", "1.0a1", -1);
        AssertCompare("1.0a1", "1.0b1", -1);
        AssertCompare("1.0b1", "1.0rc1", -1);
        AssertCompare("1.0rc1", "1.0", -1);
        AssertCompare("1.0", "1.0.post1", -1);
        AssertCompare("1.0", "1.0.0", 0);
        AssertCompare("1!1.0", "2.0", 1);
    }

    [Test]
    public void Compare_UsesPep440LocalVersionOrdering()
    {
        AssertCompare("1.0+abc", "1.0+abc.1", -1);
        AssertCompare("1.0+abc", "1.0+abd", -1);
        AssertCompare("1.0+abc.1", "1.0+abc.a", 1);
    }

    [Test]
    public void Parse_RejectsInvalidVersions()
    {
        AssertInvalid("");
        AssertInvalid("bad");
        AssertInvalid("1..0");
        AssertInvalid("1.0+");
        AssertInvalid("1.0+bad!");
    }

    static PythonVersion ParseVersion(string text)
    {
        VersionParseResult result = PythonVersionParser.Parse(text);

        Assert.That(result.IsSuccess(), Is.True, text);
        Assert.That(result.HasVersion(), Is.True, text);
        return PythonVersionParser.ParseVersion(text);
    }

    static void AssertCompare(string leftText, string rightText, int expected)
    {
        Assert.That(PythonVersionComparer.Compare(leftText, rightText), Is.EqualTo(expected));
        Assert.That(PythonVersionComparer.Compare(rightText, leftText), Is.EqualTo(0 - expected));
    }

    static void AssertInvalid(string text)
    {
        VersionParseResult result = PythonVersionParser.Parse(text);

        Assert.That(result.IsFailure(), Is.True, text);
        Assert.That(result.HasVersion(), Is.False, text);
        Assert.That(result.GetDiagnosticCount(), Is.EqualTo(1), text);
        Assert.That(result.GetDiagnostic(0).GetCode(), Is.EqualTo(PythonDiagnosticCodes.InvalidVersion), text);
    }
}
