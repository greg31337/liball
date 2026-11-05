namespace gltf.Tests.Internal.Common;

[TestFixture]
public class OwnedBytesTests
{
    [Test]
    public void CopyArray_ReturnsIndependentStorage()
    {
        byte[] source = [1, 2, 3];

        byte[] copy = OwnedBytes.CopyArray(source);
        source[0] = 9;

        Assert.That(copy, Is.EqualTo(new byte[] { 1, 2, 3 }));
        Assert.That(copy, Is.Not.SameAs(source));
    }

    [Test]
    public void CopySpan_ReturnsIndependentSelectedStorage()
    {
        byte[] source = [1, 2, 3, 4];

        byte[] copy = OwnedBytes.CopySpan(source.AsSpan(1, 2));
        source[1] = 9;

        Assert.That(copy, Is.EqualTo(new byte[] { 2, 3 }));
    }

    [Test]
    public void CopyRange_ReturnsIndependentSelectedStorage()
    {
        byte[] source = [1, 2, 3, 4];

        byte[] copy = OwnedBytes.CopyRange(source, 1, 2);
        source[2] = 9;

        Assert.That(copy, Is.EqualTo(new byte[] { 2, 3 }));
    }

    [Test]
    public void CopyRange_RejectsInvalidRangesWithoutOverflow()
    {
        byte[] source = [1, 2, 3];

        Assert.Throws<ArgumentOutOfRangeException>(() => OwnedBytes.CopyRange(source, -1, 1));
        Assert.Throws<ArgumentOutOfRangeException>(() => OwnedBytes.CopyRange(source, 0, -1));
        Assert.Throws<ArgumentException>(() => OwnedBytes.CopyRange(source, 2, 2));
        Assert.Throws<ArgumentException>(() => OwnedBytes.CopyRange(source, int.MaxValue, 1));
    }

    [Test]
    public void CopyArray_RejectsNull()
    {
        Assert.Throws<ArgumentNullException>(() => OwnedBytes.CopyArray(null!));
    }
}
