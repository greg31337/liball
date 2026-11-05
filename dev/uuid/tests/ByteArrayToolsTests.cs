using NUnit.Framework;

namespace uuid.Tests;

[TestFixture]
public class ByteArrayToolsTests
{
    [Test]
    public void Copy_ReturnsIndependentByteArray()
    {
        byte[] original = new byte[] { 1, 2, 3 };

        byte[] copy = ByteArrayTools.Copy(original);
        copy[0] = 9;

        Assert.That(copy, Is.EqualTo(new byte[] { 9, 2, 3 }));
        Assert.That(original, Is.EqualTo(new byte[] { 1, 2, 3 }));
    }

    [Test]
    public void CopyLength_ReturnsCopyWhenLengthMatches()
    {
        byte[] original = new byte[] { 4, 5 };

        byte[] copy = ByteArrayTools.CopyLength(original, 2);
        copy[1] = 9;

        Assert.That(copy, Is.EqualTo(new byte[] { 4, 9 }));
        Assert.That(original, Is.EqualTo(new byte[] { 4, 5 }));
    }

    [Test]
    public void CopyLength_ThrowsWhenLengthDiffers()
    {
        byte[] original = new byte[] { 1, 2, 3 };

        Assert.That(() => ByteArrayTools.CopyLength(original, 2), Throws.ArgumentException);
    }

    [Test]
    public void Equals_ComparesByByteContent()
    {
        Assert.That(ByteArrayTools.AreEqual(new byte[] { 1, 2, 3 }, new byte[] { 1, 2, 3 }), Is.True);
        Assert.That(ByteArrayTools.AreEqual(new byte[] { 1, 2, 3 }, new byte[] { 1, 2, 4 }), Is.False);
        Assert.That(ByteArrayTools.AreEqual(new byte[] { 1, 2 }, new byte[] { 1, 2, 0 }), Is.False);
        Assert.That(ByteArrayTools.AreEqual(null, new byte[] { 1 }), Is.False);
        Assert.That(ByteArrayTools.AreEqual(null, null), Is.True);
    }

    [Test]
    public void Compare_UsesUnsignedLexicographicOrdering()
    {
        Assert.That(ByteArrayTools.Compare(new byte[] { 0, 255 }, new byte[] { 1, 0 }), Is.LessThan(0));
        Assert.That(ByteArrayTools.Compare(new byte[] { 1, 0 }, new byte[] { 0, 255 }), Is.GreaterThan(0));
        Assert.That(ByteArrayTools.Compare(new byte[] { 1, 2 }, new byte[] { 1, 2, 0 }), Is.LessThan(0));
        Assert.That(ByteArrayTools.Compare(new byte[] { 1, 2, 0 }, new byte[] { 1, 2 }), Is.GreaterThan(0));
        Assert.That(ByteArrayTools.Compare(new byte[] { 1, 2 }, new byte[] { 1, 2 }), Is.EqualTo(0));
    }

    [Test]
    public void HasLength_ReturnsFalseForNullOrWrongLength()
    {
        Assert.That(ByteArrayTools.HasLength(new byte[] { 1, 2 }, 2), Is.True);
        Assert.That(ByteArrayTools.HasLength(new byte[] { 1 }, 2), Is.False);
        Assert.That(ByteArrayTools.HasLength(null, 2), Is.False);
    }

    [Test]
    public void ValidateLength_ReturnsValidationResult()
    {
        IdentifierValidationResult valid = ByteArrayTools.ValidateLength(new byte[16], 16, IdentifierKind.Uuid);
        IdentifierValidationResult invalid = ByteArrayTools.ValidateLength(new byte[15], 16, IdentifierKind.Uuid);

        Assert.That(valid.Ok, Is.True);
        Assert.That(valid.Kind, Is.EqualTo(IdentifierKind.Uuid));
        Assert.That(valid.NormalizedText, Is.EqualTo(""));
        Assert.That(valid.Error, Is.EqualTo(""));
        Assert.That(invalid.Ok, Is.False);
        Assert.That(invalid.Kind, Is.EqualTo(IdentifierKind.Unknown));
        Assert.That(invalid.Error, Is.EqualTo("expected 16 bytes but found 15"));
    }
}
