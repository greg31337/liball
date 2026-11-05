using NUnit.Framework;

namespace uuid.Tests;

[TestFixture]
public class RandomToolsTests
{
    [Test]
    public void FillBytes_FillsProvidedBuffer()
    {
        byte[] bytes = new byte[1024];

        RandomTools.FillBytes(bytes);

        Assert.That(ContainsNonZeroByte(bytes), Is.True);
    }

    [Test]
    public void FillBytes_AllowsEmptyBuffer()
    {
        byte[] bytes = new byte[0];

        RandomTools.FillBytes(bytes);

        Assert.That(bytes, Has.Length.EqualTo(0));
    }

    [Test]
    public void FillBytes_RejectsNullBuffer()
    {
        Assert.That(() => RandomTools.FillBytes(null), Throws.TypeOf<ArgumentNullException>());
    }

    [Test]
    public void NewBytes_ReturnsRequestedLength()
    {
        byte[] bytes = RandomTools.NewBytes(32);

        Assert.That(bytes, Has.Length.EqualTo(32));
        Assert.That(ContainsNonZeroByte(bytes), Is.True);
    }

    [Test]
    public void NewBytes_RejectsNegativeLength()
    {
        Assert.That(() => RandomTools.NewBytes(-1), Throws.TypeOf<ArgumentOutOfRangeException>());
    }

    [Test]
    public void UnbiasedByteLimit_ReturnsLargestAcceptedMultipleOfAlphabetLength()
    {
        Assert.That(RandomTools.UnbiasedByteLimit(2), Is.EqualTo(256));
        Assert.That(RandomTools.UnbiasedByteLimit(3), Is.EqualTo(255));
        Assert.That(RandomTools.UnbiasedByteLimit(10), Is.EqualTo(250));
        Assert.That(RandomTools.UnbiasedByteLimit(255), Is.EqualTo(255));
        Assert.That(RandomTools.UnbiasedByteLimit(256), Is.EqualTo(256));
    }

    [Test]
    public void UnbiasedByteLimit_RejectsInvalidAlphabetLengths()
    {
        Assert.That(() => RandomTools.UnbiasedByteLimit(1), Throws.TypeOf<ArgumentOutOfRangeException>());
        Assert.That(() => RandomTools.UnbiasedByteLimit(257), Throws.TypeOf<ArgumentOutOfRangeException>());
    }

    [Test]
    public void RandomIndex_ReturnsValuesInsideAlphabetBounds()
    {
        AssertRandomIndexBounds(2);
        AssertRandomIndexBounds(3);
        AssertRandomIndexBounds(64);
        AssertRandomIndexBounds(255);
        AssertRandomIndexBounds(256);
    }

    [Test]
    public void RandomIndex_CanReachEveryIndexForSmallAlphabet()
    {
        bool[] seen = new bool[3];
        int iteration = 0;
        while (iteration < 2000)
        {
            int index = RandomTools.RandomIndex(3);
            seen[index] = true;
            iteration = iteration + 1;
        }

        Assert.That(seen[0], Is.True);
        Assert.That(seen[1], Is.True);
        Assert.That(seen[2], Is.True);
    }

    [Test]
    public void RandomIndex_RejectsInvalidAlphabetLengths()
    {
        Assert.That(() => RandomTools.RandomIndex(0), Throws.TypeOf<ArgumentOutOfRangeException>());
        Assert.That(() => RandomTools.RandomIndex(1), Throws.TypeOf<ArgumentOutOfRangeException>());
        Assert.That(() => RandomTools.RandomIndex(257), Throws.TypeOf<ArgumentOutOfRangeException>());
    }

    static bool ContainsNonZeroByte(byte[] bytes)
    {
        int index = 0;
        while (index < bytes.Length)
        {
            if (bytes[index] != 0)
            {
                return true;
            }

            index = index + 1;
        }

        return false;
    }

    static void AssertRandomIndexBounds(int alphabetLength)
    {
        int iteration = 0;
        while (iteration < 1000)
        {
            int index = RandomTools.RandomIndex(alphabetLength);
            Assert.That(index, Is.GreaterThanOrEqualTo(0));
            Assert.That(index, Is.LessThan(alphabetLength));
            iteration = iteration + 1;
        }
    }
}
