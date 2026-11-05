using email;

namespace email.Tests;

public class EmailBase64SizeTests
{
    [TestCase(0, 0)]
    [TestCase(1, 3)]
    [TestCase(2, 3)]
    [TestCase(3, 3)]
    [TestCase(4, 3)]
    [TestCase(5, 6)]
    [TestCase(8, 6)]
    public void MaximumDecodedSizeRoundsToWholeQuantums(long encodedBytes, long expected)
    {
        Assert.That(new EmailBase64().GetMaximumDecodedSize(encodedBytes), Is.EqualTo(expected));
    }

    [Test]
    public void MaximumDecodedSizeHandlesInt64BoundaryWithoutOverflow()
    {
        long expected = 6917529027641081856;

        Assert.That(new EmailBase64().GetMaximumDecodedSize(long.MaxValue), Is.EqualTo(expected));
        Assert.Throws<ArgumentOutOfRangeException>(() => new EmailBase64().GetMaximumDecodedSize(-1));
    }

    [TestCase(0, 0, 0)]
    [TestCase(1, 0, 3)]
    [TestCase(1, 1, 2)]
    [TestCase(1, 2, 1)]
    [TestCase(2, 0, 6)]
    [TestCase(2, 1, 5)]
    [TestCase(2, 2, 4)]
    public void QuantumPredictionAccountsForTerminalPadding(long quantums, int padding, long expected)
    {
        Assert.That(new EmailBase64().GetDecodedSizeFromQuantumCount(quantums, padding), Is.EqualTo(expected));
    }

    [Test]
    public void QuantumPredictionValidatesArgumentsAndOverflow()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new EmailBase64().GetDecodedSizeFromQuantumCount(-1, 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => new EmailBase64().GetDecodedSizeFromQuantumCount(1, -1));
        Assert.Throws<ArgumentOutOfRangeException>(() => new EmailBase64().GetDecodedSizeFromQuantumCount(1, 3));
        Assert.Throws<ArgumentException>(() => new EmailBase64().GetDecodedSizeFromQuantumCount(0, 1));
        Assert.Throws<OverflowException>(() => new EmailBase64().GetDecodedSizeFromQuantumCount(long.MaxValue, 0));
    }

    [TestCase(0, 4, 0)]
    [TestCase(1, 4, 4)]
    [TestCase(3, 4, 4)]
    [TestCase(4, 4, 10)]
    [TestCase(6, 4, 10)]
    [TestCase(7, 8, 14)]
    [TestCase(57, 76, 76)]
    [TestCase(58, 76, 82)]
    public void EncodedSizeIncludesCrlfBetweenLines(long inputBytes, int lineLength, long expected)
    {
        Assert.That(new EmailBase64().GetEncodedSize(inputBytes, lineLength), Is.EqualTo(expected));
    }

    [Test]
    public void EncodedSizeMatchesActualOutputAcrossLengthsAndWidths()
    {
        int[] widths = new int[] { 4, 8, 12, 76 };
        int length = 0;
        while (length <= 200)
        {
            int widthIndex = 0;
            while (widthIndex < widths.Length)
            {
                Assert.That(
                    new EmailBase64().GetEncodedSize(length, widths[widthIndex]),
                    Is.EqualTo(new EmailBase64().EncodeWithInputAndLineLength(new byte[length], widths[widthIndex]).Length),
                    "length " + length + ", width " + widths[widthIndex]);
                widthIndex++;
            }

            length++;
        }
    }

    [Test]
    public void EncodedSizeValidatesArgumentsAndOverflow()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new EmailBase64().GetEncodedSize(-1, 76));
        Assert.Throws<ArgumentOutOfRangeException>(() => new EmailBase64().GetEncodedSize(1, 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => new EmailBase64().GetEncodedSize(1, 5));
        Assert.Throws<ArgumentOutOfRangeException>(() => new EmailBase64().GetEncodedSize(1, 80));
        Assert.Throws<OverflowException>(() => new EmailBase64().GetEncodedSize(long.MaxValue, 76));
    }
}
