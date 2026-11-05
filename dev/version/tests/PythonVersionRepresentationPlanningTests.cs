using NUnit.Framework;

namespace version.Tests;

[TestFixture]
public class PythonVersionRepresentationPlanningTests
{
    [Test]
    public void PythonVersion_CanRepresentFinalReleaseWithImplicitEpoch()
    {
        PythonVersion version = new PythonVersion(
            "1.2.3",
            "0",
            new string[] { "1", "2", "3" },
            false,
            "",
            "",
            false,
            "",
            false,
            "",
            new PythonLocalVersionSegment[0]);

        Assert.That(version.GetOriginalText(), Is.EqualTo("1.2.3"));
        Assert.That(version.GetEpoch(), Is.EqualTo("0"));
        Assert.That(version.GetReleaseSegmentCount(), Is.EqualTo(3));
        Assert.That(version.GetReleaseSegment(0), Is.EqualTo("1"));
        Assert.That(version.GetReleaseSegment(1), Is.EqualTo("2"));
        Assert.That(version.GetReleaseSegment(2), Is.EqualTo("3"));
        Assert.That(version.HasPreRelease(), Is.False);
        Assert.That(version.HasPostRelease(), Is.False);
        Assert.That(version.HasDevelopmentRelease(), Is.False);
        Assert.That(version.HasLocalVersion(), Is.False);
        Assert.That(version.IsFinalRelease(), Is.True);
        Assert.That(version.IsPrerelease(), Is.False);
    }

    [Test]
    public void PythonVersion_CanRepresentPep440SuffixSegments()
    {
        PythonVersion version = new PythonVersion(
            "1!2.0rc1.post2.dev3+ubuntu.1",
            "1",
            new string[] { "2", "0" },
            true,
            "rc",
            "1",
            true,
            "2",
            true,
            "3",
            new PythonLocalVersionSegment[]
            {
                new PythonLocalVersionSegment("ubuntu", false),
                new PythonLocalVersionSegment("1", true)
            });

        Assert.That(version.GetEpoch(), Is.EqualTo("1"));
        Assert.That(version.GetReleaseSegmentCount(), Is.EqualTo(2));
        Assert.That(version.HasPreRelease(), Is.True);
        Assert.That(version.GetPreReleaseLabel(), Is.EqualTo("rc"));
        Assert.That(version.GetPreReleaseNumber(), Is.EqualTo("1"));
        Assert.That(version.HasPostRelease(), Is.True);
        Assert.That(version.GetPostReleaseNumber(), Is.EqualTo("2"));
        Assert.That(version.HasDevelopmentRelease(), Is.True);
        Assert.That(version.GetDevelopmentReleaseNumber(), Is.EqualTo("3"));
        Assert.That(version.HasLocalVersion(), Is.True);
        Assert.That(version.GetLocalSegmentCount(), Is.EqualTo(2));
        Assert.That(version.GetLocalSegment(0).GetText(), Is.EqualTo("ubuntu"));
        Assert.That(version.GetLocalSegment(0).IsText(), Is.True);
        Assert.That(version.GetLocalSegment(1).GetText(), Is.EqualTo("1"));
        Assert.That(version.GetLocalSegment(1).IsNumeric(), Is.True);
        Assert.That(version.IsPrerelease(), Is.True);
        Assert.That(version.IsFinalRelease(), Is.False);
    }

    [Test]
    public void PythonVersion_CanRepresentDevelopmentReleaseAsPrerelease()
    {
        PythonVersion version = new PythonVersion(
            "1.0.dev1",
            "0",
            new string[] { "1", "0" },
            false,
            "",
            "",
            false,
            "",
            true,
            "1",
            new PythonLocalVersionSegment[0]);

        Assert.That(version.HasDevelopmentRelease(), Is.True);
        Assert.That(version.IsPrerelease(), Is.True);
        Assert.That(version.IsFinalRelease(), Is.False);
    }

    [Test]
    public void PythonVersion_DefensivelyCopiesArrays()
    {
        string[] releaseSegments = new string[] { "1", "2" };
        PythonLocalVersionSegment alpha = new PythonLocalVersionSegment("alpha", false);
        PythonLocalVersionSegment one = new PythonLocalVersionSegment("1", true);
        PythonLocalVersionSegment[] localSegments = new PythonLocalVersionSegment[] { alpha };

        PythonVersion version = new PythonVersion(
            "1.2+alpha",
            "0",
            releaseSegments,
            false,
            "",
            "",
            false,
            "",
            false,
            "",
            localSegments);

        releaseSegments[0] = "9";
        localSegments[0] = one;

        string[] returnedReleaseSegments = version.GetReleaseSegments();
        PythonLocalVersionSegment[] returnedLocalSegments = version.GetLocalSegments();

        returnedReleaseSegments[1] = "9";
        returnedLocalSegments[0] = one;

        Assert.That(version.GetReleaseSegment(0), Is.EqualTo("1"));
        Assert.That(version.GetReleaseSegment(1), Is.EqualTo("2"));
        Assert.That(version.GetLocalSegment(0).GetText(), Is.EqualTo("alpha"));
    }

    [Test]
    public void PythonVersion_RejectsInvalidRepresentationState()
    {
        Assert.That(
            delegate
            {
                new PythonVersion(
                    "bad",
                    "",
                    new string[] { "1" },
                    false,
                    "",
                    "",
                    false,
                    "",
                    false,
                    "",
                    new PythonLocalVersionSegment[0]);
            },
            Throws.TypeOf<System.ArgumentException>());

        Assert.That(
            delegate
            {
                new PythonVersion(
                    "bad",
                    "0",
                    new string[] { "1", "" },
                    false,
                    "",
                    "",
                    false,
                    "",
                    false,
                    "",
                    new PythonLocalVersionSegment[0]);
            },
            Throws.TypeOf<System.ArgumentException>());

        Assert.That(
            delegate
            {
                new PythonVersion(
                    "bad",
                    "0",
                    new string[] { "1" },
                    true,
                    "alpha",
                    "1",
                    false,
                    "",
                    false,
                    "",
                    new PythonLocalVersionSegment[0]);
            },
            Throws.TypeOf<System.ArgumentException>());
    }

    [Test]
    public void PythonLocalVersionSegment_RejectsInvalidSegmentShape()
    {
        Assert.That(
            delegate
            {
                new PythonLocalVersionSegment("abc-1", false);
            },
            Throws.TypeOf<System.ArgumentException>());

        Assert.That(
            delegate
            {
                new PythonLocalVersionSegment("abc1", true);
            },
            Throws.TypeOf<System.ArgumentException>());

        Assert.That(
            delegate
            {
                new PythonLocalVersionSegment("123", false);
            },
            Throws.TypeOf<System.ArgumentException>());
    }

    [Test]
    public void ParserPlanning_CoversPep440NormalizationTargets()
    {
        AssertPlannedNormalizedVersion(
            "v1.1RC1",
            "0",
            new string[] { "1", "1" },
            true,
            "rc",
            "1",
            false,
            "",
            false,
            "",
            new PythonLocalVersionSegment[0]);

        AssertPlannedNormalizedVersion(
            "1.2-post2",
            "0",
            new string[] { "1", "2" },
            false,
            "",
            "",
            true,
            "2",
            false,
            "",
            new PythonLocalVersionSegment[0]);

        AssertPlannedNormalizedVersion(
            "1.0+ubuntu-1",
            "0",
            new string[] { "1", "0" },
            false,
            "",
            "",
            false,
            "",
            false,
            "",
            new PythonLocalVersionSegment[]
            {
                new PythonLocalVersionSegment("ubuntu", false),
                new PythonLocalVersionSegment("1", true)
            });
    }

    static void AssertPlannedNormalizedVersion(
        string sourceText,
        string epoch,
        string[] releaseSegments,
        bool preReleasePresent,
        string preReleaseLabel,
        string preReleaseNumber,
        bool postReleasePresent,
        string postReleaseNumber,
        bool developmentReleasePresent,
        string developmentReleaseNumber,
        PythonLocalVersionSegment[] localSegments)
    {
        PythonVersion version = new PythonVersion(
            sourceText,
            epoch,
            releaseSegments,
            preReleasePresent,
            preReleaseLabel,
            preReleaseNumber,
            postReleasePresent,
            postReleaseNumber,
            developmentReleasePresent,
            developmentReleaseNumber,
            localSegments);

        Assert.That(version.GetOriginalText(), Is.EqualTo(sourceText));
        Assert.That(version.GetEpoch(), Is.EqualTo(epoch));
        Assert.That(version.GetReleaseSegments(), Is.EqualTo(releaseSegments));
        Assert.That(version.HasPreRelease(), Is.EqualTo(preReleasePresent));
        Assert.That(version.GetPreReleaseLabel(), Is.EqualTo(preReleaseLabel));
        Assert.That(version.GetPreReleaseNumber(), Is.EqualTo(preReleaseNumber));
        Assert.That(version.HasPostRelease(), Is.EqualTo(postReleasePresent));
        Assert.That(version.GetPostReleaseNumber(), Is.EqualTo(postReleaseNumber));
        Assert.That(version.HasDevelopmentRelease(), Is.EqualTo(developmentReleasePresent));
        Assert.That(version.GetDevelopmentReleaseNumber(), Is.EqualTo(developmentReleaseNumber));
        Assert.That(version.GetLocalSegmentCount(), Is.EqualTo(localSegments.Length));
    }
}
