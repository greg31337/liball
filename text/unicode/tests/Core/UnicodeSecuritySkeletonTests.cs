using NUnit.Framework;

namespace unicode.Tests;

[TestFixture]
public class UnicodeSecuritySkeletonTests
{
    [Test]
    public void GetSkeleton_MapsSingleCodePointConfusables()
    {
        Assert.That(new UnicodeSecurity().GetSkeleton("A"), Is.EqualTo("A"));
        Assert.That(new UnicodeSecurity().GetSkeleton("\u0391"), Is.EqualTo("A"));
    }

    [Test]
    public void GetSkeleton_UsesLongestSequenceMappings()
    {
        Assert.That(new UnicodeSecurity().GetSkeleton("m\u0300"), Is.EqualTo("rn\u0300"));
        Assert.That(new UnicodeSecurity().GetSkeleton("xm\u0300y"), Is.EqualTo("xrn\u0300y"));
    }

    [Test]
    public void GetSkeleton_LeavesUnmappedScalarsUnchanged()
    {
        Assert.That(new UnicodeSecurity().GetSkeleton("B\U0001F600"), Is.EqualTo("B\U0001F600"));
    }

    [Test]
    public void GetSkeleton_NormalizesCanonicallyAndRemovesDefaultIgnorables()
    {
        UnicodeSecurity security = new UnicodeSecurity();

        Assert.That(security.GetSkeleton("\u00E9"), Is.EqualTo(security.GetSkeleton("e\u0301")));
        Assert.That(security.GetSkeleton("A\u00ADB"), Is.EqualTo(security.GetSkeleton("AB")));
        Assert.That(security.GetSkeleton("\u00AD"), Is.Empty);
    }

    [Test]
    public void GetSkeletonCodePoints_ReturnsFlattenedSkeletonCodePoints()
    {
        int[] skeleton = UnicodeSecurity.GetSkeletonCodePoints(new int[] { 0x0041, 0x006D, 0x0300 });

        Assert.That(skeleton, Is.EqualTo(new int[] { 0x0041, 0x0072, 0x006E, 0x0300 }));
    }

    [Test]
    public void GetSkeletonCodePoints_ReappliesMappingsUntilStable()
    {
        int[] skeleton = UnicodeSecurity.GetSkeletonCodePoints(new int[] { 0x0391 });

        Assert.That(skeleton, Is.EqualTo(new int[] { 0x0041 }));
    }

    [Test]
    public void FindConfusableMapping_ReturnsLongestMatchingSource()
    {
        int[] sourceOffsets = new int[] { 0, 1 };
        int[] sourceLengths = new int[] { 1, 2 };
        int[] sourceCodePoints = new int[] { 0x006D, 0x006D, 0x0300 };

        Assert.That(
            UnicodeSecurity.FindConfusableMapping(
                new int[] { 0x006D, 0x0300 },
                0,
                sourceOffsets,
                sourceLengths,
                sourceCodePoints),
            Is.EqualTo(1));
    }

    [Test]
    public void ConfusableSourceMatches_RejectsOutOfRangeSlicesWithoutOverflow()
    {
        Assert.That(
            UnicodeSecurity.ConfusableSourceMatches(
                new int[] { 0x006D },
                1,
                0,
                int.MaxValue,
                new int[] { 0x006D }),
            Is.False);
        Assert.That(
            UnicodeSecurity.ConfusableSourceMatches(
                new int[] { 0x006D },
                0,
                1,
                int.MaxValue,
                new int[] { 0x006D }),
            Is.False);
    }

    [Test]
    public void GetSkeleton_RejectsNullText()
    {
        Assert.Throws<ArgumentNullException>(() => new UnicodeSecurity().GetSkeleton(null!));
        Assert.Throws<ArgumentNullException>(() => UnicodeSecurity.GetSkeletonCodePoints(null!));
    }
}
