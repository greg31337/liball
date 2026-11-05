using ttf;

namespace ttf.Tests;

public class FontDataTests
{
    [Test]
    public void ByteArrayFactoryCopiesInput()
    {
        byte[] source = new byte[] { 1, 2, 3, 4 };
        FontData data = FontData.Create(source);

        source[1] = 99;

        Assert.That(data.CopyBytes(), Is.EqualTo(new byte[] { 1, 2, 3, 4 }));
    }

    [Test]
    public void ReadOnlyMemoryFactoryCopiesInput()
    {
        byte[] source = new byte[] { 5, 6, 7, 8 };
        ReadOnlyMemory<byte> input = new ReadOnlyMemory<byte>(source, 1, 2);
        FontData data = FontData.Create(input);

        source[1] = 99;
        source[2] = 100;

        Assert.That(data.CopyBytes(), Is.EqualTo(new byte[] { 6, 7 }));
    }

    [Test]
    public void ReadOnlySpanFactoryCopiesInput()
    {
        byte[] source = new byte[] { 9, 10, 11, 12 };
        FontData data = FontData.Create(source.AsSpan(1, 2));

        source[1] = 99;
        source[2] = 100;

        Assert.That(data.CopyBytes(), Is.EqualTo(new byte[] { 10, 11 }));
    }

    [Test]
    public void ByteArrayFactoryRejectsNull()
    {
        Assert.Throws<ArgumentNullException>(() => FontData.Create((byte[]?)null!));
    }

    [Test]
    public void CopyBytesReturnsIndependentArrays()
    {
        FontData data = FontData.Create(new byte[] { 1, 2, 3 });
        byte[] first = data.CopyBytes();
        byte[] second = data.CopyBytes();

        first[0] = 99;

        Assert.Multiple(() =>
        {
            Assert.That(second, Is.EqualTo(new byte[] { 1, 2, 3 }));
            Assert.That(first, Is.Not.SameAs(second));
        });
    }

    [Test]
    public void SliceReturnsRequestedBytesAndAbsoluteOffset()
    {
        FontData data = FontData.Create(new byte[] { 10, 20, 30, 40, 50 });

        FontDataSlice slice = data.GetSlice(1, 3);

        Assert.Multiple(() =>
        {
            Assert.That(data.GetLength(), Is.EqualTo(5));
            Assert.That(slice.GetLength(), Is.EqualTo(3));
            Assert.That(slice.GetAbsoluteOffset(), Is.EqualTo(1));
            Assert.That(slice.CopyBytes(), Is.EqualTo(new byte[] { 20, 30, 40 }));
        });
    }

    [Test]
    public void NestedSliceStaysWithinParentAndTracksAbsoluteOffset()
    {
        FontData data = FontData.Create(new byte[] { 0, 1, 2, 3, 4, 5 });
        FontDataSlice parent = data.GetSlice(1, 4);

        FontDataSlice child = parent.GetSlice(2, 2);

        Assert.Multiple(() =>
        {
            Assert.That(child.GetAbsoluteOffset(), Is.EqualTo(3));
            Assert.That(child.CopyBytes(), Is.EqualTo(new byte[] { 3, 4 }));
            Assert.Throws<FontFormatException>(() => parent.GetSlice(4, 1));
        });
    }

    [Test]
    public void EmptySlicesAtBoundariesAreAccepted()
    {
        FontData data = FontData.Create(new byte[] { 1, 2, 3 });
        FontDataSlice start = data.GetSlice(0, 0);
        FontDataSlice end = data.GetSlice(3, 0);
        FontDataSlice nestedEnd = data.GetSlice(1, 2).GetSlice(2, 0);

        Assert.Multiple(() =>
        {
            Assert.That(start.GetLength(), Is.EqualTo(0));
            Assert.That(start.GetAbsoluteOffset(), Is.EqualTo(0));
            Assert.That(end.GetLength(), Is.EqualTo(0));
            Assert.That(end.GetAbsoluteOffset(), Is.EqualTo(3));
            Assert.That(nestedEnd.GetAbsoluteOffset(), Is.EqualTo(3));
        });
    }

    [Test]
    public void EveryRootSubrangeProducesExactlyItsBoundedBytes()
    {
        byte[] bytes = new byte[] { 10, 20, 30, 40 };
        FontData data = FontData.Create(bytes);
        int offset = 0;

        while (offset <= bytes.Length)
        {
            int length = 0;

            while (length <= bytes.Length - offset)
            {
                FontDataSlice slice = data.GetSlice(offset, length);
                byte[] expected = bytes.AsSpan(offset, length).ToArray();

                Assert.Multiple(() =>
                {
                    Assert.That(slice.GetAbsoluteOffset(), Is.EqualTo(offset));
                    Assert.That(slice.GetLength(), Is.EqualTo(length));
                    Assert.That(slice.CopyBytes(), Is.EqualTo(expected));
                });

                length += 1;
            }

            offset += 1;
        }
    }

    [Test]
    public void EveryNestedSubrangeRemainsRelativeToItsParent()
    {
        byte[] bytes = new byte[] { 10, 20, 30, 40, 50 };
        FontDataSlice parent = FontData.Create(bytes).GetSlice(1, 3);
        int offset = 0;

        while (offset <= parent.GetLength())
        {
            int length = 0;

            while (length <= parent.GetLength() - offset)
            {
                FontDataSlice child = parent.GetSlice(offset, length);
                byte[] expected = bytes.AsSpan(1 + offset, length).ToArray();

                Assert.Multiple(() =>
                {
                    Assert.That(child.GetAbsoluteOffset(), Is.EqualTo(1 + offset));
                    Assert.That(child.GetLength(), Is.EqualTo(length));
                    Assert.That(child.CopyBytes(), Is.EqualTo(expected));
                });

                length += 1;
            }

            offset += 1;
        }
    }

    [TestCase(-1, 0)]
    [TestCase(0, -1)]
    [TestCase(4, 0)]
    [TestCase(3, 1)]
    [TestCase(2, 2)]
    [TestCase(int.MaxValue, 1)]
    [TestCase(1, int.MaxValue)]
    public void RootSliceRejectsInvalidRange(int offset, int length)
    {
        FontData data = FontData.Create(new byte[] { 1, 2, 3 });

        Assert.Throws<FontFormatException>(() => data.GetSlice(offset, length));
    }

    [TestCase(-1, 0)]
    [TestCase(0, -1)]
    [TestCase(3, 0)]
    [TestCase(2, 1)]
    [TestCase(1, 2)]
    [TestCase(int.MaxValue, 1)]
    [TestCase(1, int.MaxValue)]
    public void NestedSliceRejectsInvalidRange(int offset, int length)
    {
        FontDataSlice slice = FontData.Create(new byte[] { 1, 2, 3, 4 }).GetSlice(1, 2);

        Assert.Throws<FontFormatException>(() => slice.GetSlice(offset, length));
    }

    [Test]
    public void MemoryViewsContainOnlyTheBoundedRegion()
    {
        FontData data = FontData.Create(new byte[] { 1, 2, 3, 4 });
        FontDataSlice slice = data.GetSlice(1, 2);

        Assert.Multiple(() =>
        {
            Assert.That(data.GetMemory().ToArray(), Is.EqualTo(new byte[] { 1, 2, 3, 4 }));
            Assert.That(slice.GetMemory().ToArray(), Is.EqualTo(new byte[] { 2, 3 }));
        });
    }

    [Test]
    public void SliceConstructorRejectsNegativeAbsoluteOffset()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new FontDataSlice(
            new ReadOnlyMemory<byte>(new byte[] { 1 }),
            -1));
    }

    [Test]
    public void SliceConstructorRejectsAbsoluteRangeOverflow()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new FontDataSlice(
            new ReadOnlyMemory<byte>(new byte[] { 1, 2 }),
            long.MaxValue - 1));
    }

    [Test]
    public void NestedEmptySliceCanReachMaximumAbsoluteOffset()
    {
        FontDataSlice parent = new FontDataSlice(
            new ReadOnlyMemory<byte>(new byte[] { 1, 2 }),
            long.MaxValue - 2);

        FontDataSlice end = parent.GetSlice(2, 0);

        Assert.That(end.GetAbsoluteOffset(), Is.EqualTo(long.MaxValue));
    }
}
