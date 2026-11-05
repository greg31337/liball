using NUnit.Framework;

namespace gif.Tests;

[TestFixture]
public class GifInterlaceTests
{
    [Test]
    public void CreateRowOrder_UsesGifFourPassOrder()
    {
        GifInterlaceRowMapper mapper = new GifInterlaceRowMapper();

        GifInterlaceRowOrderResult heightEight = mapper.CreateRowOrder(8);
        GifInterlaceRowOrderResult heightTen = mapper.CreateRowOrder(10);

        Assert.That(heightEight.Success, Is.True);
        Assert.That(heightEight.Rows, Is.EqualTo(new int[] { 0, 4, 2, 6, 1, 3, 5, 7 }));
        Assert.That(heightTen.Success, Is.True);
        Assert.That(heightTen.Rows, Is.EqualTo(new int[] { 0, 8, 4, 2, 6, 1, 3, 5, 7, 9 }));
    }

    [Test]
    public void CreateRowOrder_HandlesShortHeights()
    {
        GifInterlaceRowMapper mapper = new GifInterlaceRowMapper();

        Assert.That(mapper.CreateRowOrder(0).Rows, Is.EqualTo(new int[0]));
        Assert.That(mapper.CreateRowOrder(1).Rows, Is.EqualTo(new int[] { 0 }));
        Assert.That(mapper.CreateRowOrder(2).Rows, Is.EqualTo(new int[] { 0, 1 }));
        Assert.That(mapper.CreateRowOrder(3).Rows, Is.EqualTo(new int[] { 0, 2, 1 }));
        Assert.That(mapper.CreateRowOrder(5).Rows, Is.EqualTo(new int[] { 0, 4, 2, 1, 3 }));
    }

    [Test]
    public void CreateRowOrder_RejectsNegativeHeight()
    {
        GifInterlaceRowMapper mapper = new GifInterlaceRowMapper();

        GifInterlaceRowOrderResult result = mapper.CreateRowOrder(-1);

        Assert.That(result.Success, Is.False);
        Assert.That(result.Rows, Is.EqualTo(new int[0]));
        Assert.That(result.Error.Code, Is.EqualTo(GifErrorCode.InvalidImageRectangle));
        Assert.That(result.Error.Position.BlockName, Is.EqualTo("Interlace"));
    }

    [Test]
    public void CreateRowOrder_CoversEveryRowExactlyOnce()
    {
        GifInterlaceRowMapper mapper = new GifInterlaceRowMapper();

        GifInterlaceRowOrderResult result = mapper.CreateRowOrder(17);

        Assert.That(result.Success, Is.True);
        Assert.That(result.Rows.Length, Is.EqualTo(17));

        bool[] seen = new bool[17];
        int index = 0;
        while (index < result.Rows.Length)
        {
            int row = result.Rows[index];
            Assert.That(row, Is.GreaterThanOrEqualTo(0));
            Assert.That(row, Is.LessThan(17));
            Assert.That(seen[row], Is.False);
            seen[row] = true;
            index = index + 1;
        }

        int rowIndex = 0;
        while (rowIndex < seen.Length)
        {
            Assert.That(seen[rowIndex], Is.True);
            rowIndex = rowIndex + 1;
        }
    }

    [Test]
    public void GetRowForInterlacedIndex_ReturnsMappedRowOrMinusOne()
    {
        GifInterlaceRowMapper mapper = new GifInterlaceRowMapper();

        Assert.That(mapper.GetRowForInterlacedIndex(8, 0), Is.EqualTo(0));
        Assert.That(mapper.GetRowForInterlacedIndex(8, 1), Is.EqualTo(4));
        Assert.That(mapper.GetRowForInterlacedIndex(8, 7), Is.EqualTo(7));
        Assert.That(mapper.GetRowForInterlacedIndex(8, -1), Is.EqualTo(-1));
        Assert.That(mapper.GetRowForInterlacedIndex(8, 8), Is.EqualTo(-1));
        Assert.That(mapper.GetRowForInterlacedIndex(-1, 0), Is.EqualTo(-1));
    }

    [Test]
    public void DeinterlaceIndexedPixels_ReordersInterlacedRowsToRowMajorRows()
    {
        GifInterlaceRowMapper mapper = new GifInterlaceRowMapper();
        byte[] interlacedPixels = new byte[]
        {
            0, 0, 0,
            4, 4, 4,
            2, 2, 2,
            6, 6, 6,
            1, 1, 1,
            3, 3, 3,
            5, 5, 5,
            7, 7, 7
        };

        GifDeinterlaceResult result = mapper.DeinterlaceIndexedPixels(interlacedPixels, 3, 8);

        Assert.That(result.Success, Is.True);
        Assert.That(result.IndexedPixels, Is.EqualTo(new byte[]
        {
            0, 0, 0,
            1, 1, 1,
            2, 2, 2,
            3, 3, 3,
            4, 4, 4,
            5, 5, 5,
            6, 6, 6,
            7, 7, 7
        }));
        Assert.That(result.IndexedPixels, Is.Not.SameAs(interlacedPixels));
    }

    [Test]
    public void DeinterlaceIndexedPixels_HandlesPartialPassesForShortImages()
    {
        GifInterlaceRowMapper mapper = new GifInterlaceRowMapper();
        byte[] interlacedPixels = new byte[]
        {
            0, 10,
            4, 14,
            2, 12,
            1, 11,
            3, 13
        };

        GifDeinterlaceResult result = mapper.DeinterlaceIndexedPixels(interlacedPixels, 2, 5);

        Assert.That(result.Success, Is.True);
        Assert.That(result.IndexedPixels, Is.EqualTo(new byte[]
        {
            0, 10,
            1, 11,
            2, 12,
            3, 13,
            4, 14
        }));
    }

    [Test]
    public void DeinterlaceIndexedPixels_ValidatesDimensionsAndPixelCount()
    {
        GifInterlaceRowMapper mapper = new GifInterlaceRowMapper();

        GifDeinterlaceResult negativeWidth = mapper.DeinterlaceIndexedPixels(new byte[0], -1, 1);
        GifDeinterlaceResult shortPixels = mapper.DeinterlaceIndexedPixels(new byte[] { 0, 1, 2 }, 2, 2);
        GifDeinterlaceResult longPixels = mapper.DeinterlaceIndexedPixels(new byte[] { 0, 1, 2, 3, 4 }, 2, 2);

        Assert.That(negativeWidth.Success, Is.False);
        Assert.That(negativeWidth.Error.Code, Is.EqualTo(GifErrorCode.InvalidImageRectangle));
        Assert.That(shortPixels.Success, Is.False);
        Assert.That(shortPixels.Error.Code, Is.EqualTo(GifErrorCode.DecodedPixelCountMismatch));
        Assert.That(longPixels.Success, Is.False);
        Assert.That(longPixels.Error.Code, Is.EqualTo(GifErrorCode.DecodedPixelCountMismatch));
    }

    [Test]
    public void DeinterlaceIndexedPixels_TreatsNullAsEmptyInputForCountValidation()
    {
        GifInterlaceRowMapper mapper = new GifInterlaceRowMapper();

        GifDeinterlaceResult empty = mapper.DeinterlaceIndexedPixels(null!, 0, 0);
        GifDeinterlaceResult missingPixels = mapper.DeinterlaceIndexedPixels(null!, 1, 1);

        Assert.That(empty.Success, Is.True);
        Assert.That(empty.IndexedPixels, Is.EqualTo(new byte[0]));
        Assert.That(missingPixels.Success, Is.False);
        Assert.That(missingPixels.Error.Code, Is.EqualTo(GifErrorCode.DecodedPixelCountMismatch));
    }

    [Test]
    public void DeinterlaceIndexedPixels_RejectsOversizedAllocation()
    {
        GifInterlaceRowMapper mapper = new GifInterlaceRowMapper();

        GifDeinterlaceResult result = mapper.DeinterlaceIndexedPixels(new byte[0], 46341, 46341);

        Assert.That(result.Success, Is.False);
        Assert.That(result.Error.Code, Is.EqualTo(GifErrorCode.MemoryLimitExceeded));
    }

    [Test]
    public void InterlaceIndexedPixels_ReordersRowMajorRowsToGifWriteOrder()
    {
        GifInterlaceRowMapper mapper = new GifInterlaceRowMapper();
        byte[] rowMajorPixels = new byte[]
        {
            0, 0, 0,
            1, 1, 1,
            2, 2, 2,
            3, 3, 3,
            4, 4, 4,
            5, 5, 5,
            6, 6, 6,
            7, 7, 7
        };

        GifInterlaceWriteResult result = mapper.InterlaceIndexedPixels(rowMajorPixels, 3, 8);

        Assert.That(result.Success, Is.True);
        Assert.That(result.IndexedPixels, Is.EqualTo(new byte[]
        {
            0, 0, 0,
            4, 4, 4,
            2, 2, 2,
            6, 6, 6,
            1, 1, 1,
            3, 3, 3,
            5, 5, 5,
            7, 7, 7
        }));
        Assert.That(result.IndexedPixels, Is.Not.SameAs(rowMajorPixels));
    }

    [Test]
    public void PrepareIndexedPixelsForWrite_CopiesNonInterlacedRowsWithoutReordering()
    {
        GifInterlaceRowMapper mapper = new GifInterlaceRowMapper();
        byte[] rowMajorPixels = new byte[] { 0, 1, 2, 3, 4, 5 };

        GifInterlaceWriteResult result = mapper.PrepareIndexedPixelsForWrite(rowMajorPixels, 3, 2, false);

        Assert.That(result.Success, Is.True);
        Assert.That(result.IndexedPixels, Is.EqualTo(rowMajorPixels));
        Assert.That(result.IndexedPixels, Is.Not.SameAs(rowMajorPixels));
    }

    [Test]
    public void PrepareIndexedPixelsForWrite_UsesInterlaceOrderWhenRequested()
    {
        GifInterlaceRowMapper mapper = new GifInterlaceRowMapper();
        byte[] rowMajorPixels = new byte[]
        {
            0, 10,
            1, 11,
            2, 12,
            3, 13,
            4, 14
        };

        GifInterlaceWriteResult result = mapper.PrepareIndexedPixelsForWrite(rowMajorPixels, 2, 5, true);

        Assert.That(result.Success, Is.True);
        Assert.That(result.IndexedPixels, Is.EqualTo(new byte[]
        {
            0, 10,
            4, 14,
            2, 12,
            1, 11,
            3, 13
        }));
    }

    [Test]
    public void InterlaceIndexedPixels_RoundTripsThroughDeinterlace()
    {
        GifInterlaceRowMapper mapper = new GifInterlaceRowMapper();
        byte[] rowMajorPixels = new byte[]
        {
            0, 10,
            1, 11,
            2, 12,
            3, 13,
            4, 14,
            5, 15,
            6, 16,
            7, 17,
            8, 18,
            9, 19
        };

        GifInterlaceWriteResult interlaced = mapper.InterlaceIndexedPixels(rowMajorPixels, 2, 10);
        GifDeinterlaceResult rowMajor = mapper.DeinterlaceIndexedPixels(interlaced.IndexedPixels, 2, 10);

        Assert.That(interlaced.Success, Is.True);
        Assert.That(rowMajor.Success, Is.True);
        Assert.That(rowMajor.IndexedPixels, Is.EqualTo(rowMajorPixels));
    }

    [Test]
    public void InterlaceIndexedPixels_ValidatesWriteInputs()
    {
        GifInterlaceRowMapper mapper = new GifInterlaceRowMapper();

        GifInterlaceWriteResult negativeHeight = mapper.InterlaceIndexedPixels(new byte[0], 1, -1);
        GifInterlaceWriteResult countMismatch = mapper.InterlaceIndexedPixels(new byte[] { 0, 1, 2 }, 2, 2);
        GifInterlaceWriteResult oversized = mapper.InterlaceIndexedPixels(new byte[0], 46341, 46341);

        Assert.That(negativeHeight.Success, Is.False);
        Assert.That(negativeHeight.Error.Code, Is.EqualTo(GifErrorCode.InvalidImageRectangle));
        Assert.That(countMismatch.Success, Is.False);
        Assert.That(countMismatch.Error.Code, Is.EqualTo(GifErrorCode.DecodedPixelCountMismatch));
        Assert.That(oversized.Success, Is.False);
        Assert.That(oversized.Error.Code, Is.EqualTo(GifErrorCode.MemoryLimitExceeded));
    }
}
