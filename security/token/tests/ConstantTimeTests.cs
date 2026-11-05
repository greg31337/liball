using token;

namespace token.Tests;

public class ConstantTimeTests
{
    [Test]
    public void AreEqualReturnsTrueForMatchingArrays()
    {
        byte[] left = new byte[] { 0, 1, 2, 127, 255 };
        byte[] right = new byte[] { 0, 1, 2, 127, 255 };

        Assert.That(ConstantTime.AreEqual(left, right), Is.True);
    }

    [Test]
    public void AreEqualReturnsFalseForDifferentArrayContents()
    {
        byte[] expected = new byte[] { 10, 20, 30, 40 };

        Assert.Multiple(() =>
        {
            Assert.That(ConstantTime.AreEqual(expected, new byte[] { 11, 20, 30, 40 }), Is.False);
            Assert.That(ConstantTime.AreEqual(expected, new byte[] { 10, 21, 30, 40 }), Is.False);
            Assert.That(ConstantTime.AreEqual(expected, new byte[] { 10, 20, 30, 41 }), Is.False);
        });
    }

    [Test]
    public void AreEqualReturnsFalseForDifferentArrayLengths()
    {
        Assert.That(
            ConstantTime.AreEqual(new byte[] { 1, 2, 3 }, new byte[] { 1, 2, 3, 0 }),
            Is.False);
    }

    [Test]
    public void AreEqualReturnsTrueForEmptyArrays()
    {
        Assert.That(ConstantTime.AreEqual(Array.Empty<byte>(), Array.Empty<byte>()), Is.True);
    }

    [Test]
    public void AreEqualReturnsFalseWhenEitherArrayIsNull()
    {
        byte[] missing = null;
        byte[] present = Array.Empty<byte>();

        Assert.Multiple(() =>
        {
            Assert.That(ConstantTime.AreEqual(missing, present), Is.False);
            Assert.That(ConstantTime.AreEqual(present, missing), Is.False);
            Assert.That(ConstantTime.AreEqual(missing, missing), Is.False);
        });
    }

    [Test]
    public void AreEqualComparesReadOnlySpanSlices()
    {
        byte[] leftBytes = new byte[] { 9, 1, 2, 3, 9 };
        byte[] rightBytes = new byte[] { 8, 1, 2, 3, 8 };
        ReadOnlySpan<byte> left = leftBytes.AsSpan(1, 3);
        ReadOnlySpan<byte> right = rightBytes.AsSpan(1, 3);

        Assert.That(ConstantTime.AreEqual(left, right), Is.True);
    }
}
