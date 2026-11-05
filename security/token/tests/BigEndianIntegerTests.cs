using token;

namespace token.Tests;

public class BigEndianIntegerTests
{
    [Test]
    public void ToFixedWidthLeftPadsUnsignedMagnitude()
    {
        TokenBytesResult result = BigEndianInteger.ToFixedWidth(new byte[] { 0x12, 0x34 }, 5);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.Error, Is.Null);
            Assert.That(result.Bytes, Is.EqualTo(new byte[] { 0, 0, 0, 0x12, 0x34 }));
        });
    }

    [Test]
    public void ToFixedWidthRemovesRedundantLeadingZeroes()
    {
        TokenBytesResult result = BigEndianInteger.ToFixedWidth(new byte[] { 0, 0, 0x80, 0x01 }, 2);

        Assert.That(result.Bytes, Is.EqualTo(new byte[] { 0x80, 0x01 }));
    }

    [Test]
    public void ToFixedWidthRejectsSignificantOverflowWithoutReturningPartialBytes()
    {
        TokenBytesResult result = BigEndianInteger.ToFixedWidth(new byte[] { 0, 1, 2, 3 }, 2);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.Bytes, Is.Null);
            Assert.That(result.Error.Code, Is.EqualTo("integer_too_large"));
            Assert.That(result.Error.Position, Is.EqualTo(1));
        });
    }

    [Test]
    public void ToFixedWidthSupportsSpanSlices()
    {
        byte[] bytes = new byte[] { 9, 0, 0xAB, 0xCD, 9 };

        TokenBytesResult result = BigEndianInteger.ToFixedWidth(bytes.AsSpan(1, 3), 4);

        Assert.That(result.Bytes, Is.EqualTo(new byte[] { 0, 0, 0xAB, 0xCD }));
    }

    [Test]
    public void WriteFixedWidthClearsPaddingAndWritesAtRightEdge()
    {
        byte[] destination = new byte[] { 9, 9, 9, 9, 9 };

        TokenResult result = BigEndianInteger.WriteFixedWidth(
            new ReadOnlySpan<byte>(new byte[] { 0, 0x12, 0x34 }),
            destination.AsSpan(1, 3));

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(destination, Is.EqualTo(new byte[] { 9, 0, 0x12, 0x34, 9 }));
        });
    }

    [Test]
    public void WriteFixedWidthFailureLeavesDestinationUnchanged()
    {
        byte[] destination = new byte[] { 9, 9 };

        TokenResult result = BigEndianInteger.WriteFixedWidth(new byte[] { 1, 2, 3 }, destination);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.Error.Code, Is.EqualTo("integer_too_large"));
            Assert.That(destination, Is.EqualTo(new byte[] { 9, 9 }));
        });
    }

    [TestCase(new byte[] { 0, 0, 1, 2 }, new byte[] { 1, 2 })]
    [TestCase(new byte[] { 0, 0 }, new byte[] { 0 })]
    [TestCase(new byte[] { 0 }, new byte[] { 0 })]
    [TestCase(new byte[] { }, new byte[] { })]
    public void ToMinimalRemovesOnlyRedundantLeadingZeroes(byte[] input, byte[] expected)
    {
        TokenBytesResult result = BigEndianInteger.ToMinimal(input);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.Bytes, Is.EqualTo(expected));
        });
    }

    [Test]
    public void ToMinimalReturnsIndependentBytesFromSpanSlice()
    {
        byte[] input = new byte[] { 9, 0, 0xAB, 0xCD, 9 };

        TokenBytesResult result = BigEndianInteger.ToMinimal(input.AsSpan(1, 3));
        input[2] = 0;

        Assert.That(result.Bytes, Is.EqualTo(new byte[] { 0xAB, 0xCD }));
    }

    [TestCase(0UL, 0, new byte[] { })]
    [TestCase(0UL, 3, new byte[] { 0, 0, 0 })]
    [TestCase(1UL, 1, new byte[] { 1 })]
    [TestCase(0x1234UL, 4, new byte[] { 0, 0, 0x12, 0x34 })]
    [TestCase(0xFEDCBA9876543210UL, 8, new byte[] { 0xFE, 0xDC, 0xBA, 0x98, 0x76, 0x54, 0x32, 0x10 })]
    public void FromUInt64ProducesRequestedWidth(ulong value, int width, byte[] expected)
    {
        TokenBytesResult result = BigEndianInteger.FromUInt64(value, width);

        Assert.That(result.Bytes, Is.EqualTo(expected));
    }

    [TestCase(1UL, 0)]
    [TestCase(256UL, 1)]
    [TestCase(65536UL, 2)]
    public void FromUInt64RejectsValuesThatDoNotFit(ulong value, int width)
    {
        TokenBytesResult result = BigEndianInteger.FromUInt64(value, width);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.Bytes, Is.Null);
            Assert.That(result.Error.Code, Is.EqualTo("integer_too_large"));
        });
    }

    [Test]
    public void HelpersRejectInvalidArguments()
    {
        Assert.Multiple(() =>
        {
            Assert.That(() => BigEndianInteger.ToFixedWidth((byte[])null, 1), Throws.ArgumentNullException);
            Assert.That(() => BigEndianInteger.ToMinimal((byte[])null), Throws.ArgumentNullException);
            Assert.That(() => BigEndianInteger.ToFixedWidth(Array.Empty<byte>(), -1), Throws.TypeOf<ArgumentOutOfRangeException>());
            Assert.That(() => BigEndianInteger.FromUInt64(0, -1), Throws.TypeOf<ArgumentOutOfRangeException>());
            Assert.That(() => BigEndianInteger.FromUInt64(0, 9), Throws.TypeOf<ArgumentOutOfRangeException>());
        });
    }
}
