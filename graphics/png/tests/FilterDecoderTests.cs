using NUnit.Framework;

namespace png.Tests;

[TestFixture]
public class FilterDecoderTests
{
    [Test]
    public void DecodeNone_CopiesRowPayloadUnchanged()
    {
        byte[] filteredRow = new byte[] { PngConstants.FilterTypeNone, 1, 2, 3, 255 };

        byte[] row = PngFilterDecoder.DecodeNone(filteredRow);

        Assert.That(row, Is.EqualTo(new byte[] { 1, 2, 3, 255 }));
    }

    [Test]
    public void DecodeNone_UsesOffsetAndRowDataBytes()
    {
        byte[] filteredRows = new byte[] { 99, PngConstants.FilterTypeNone, 10, 20, 30, 88 };

        byte[] row = PngFilterDecoder.DecodeNone(filteredRows, 1, 3);

        Assert.That(row, Is.EqualTo(new byte[] { 10, 20, 30 }));
    }

    [Test]
    public void DecodeNone_AllowsEmptyRowPayload()
    {
        byte[] filteredRow = new byte[] { PngConstants.FilterTypeNone };

        byte[] row = PngFilterDecoder.DecodeNone(filteredRow);

        Assert.That(row, Is.Not.Null);
        Assert.That(row, Is.Empty);
    }

    [Test]
    public void DecodeNone_DoesNotReadPastRequestedRowBytes()
    {
        byte[] filteredRows = new byte[] { PngConstants.FilterTypeNone, 1, 2, 3, 4 };

        byte[] row = PngFilterDecoder.DecodeNone(filteredRows, 0, 2);

        Assert.That(row, Is.EqualTo(new byte[] { 1, 2 }));
    }

    [Test]
    public void DecodeNone_RejectsWrongFilterType()
    {
        byte[] filteredRow = new byte[] { PngConstants.FilterTypeSub, 1, 2, 3 };

        Assert.Throws<InvalidDataException>(() => PngFilterDecoder.DecodeNone(filteredRow));
    }

    [Test]
    public void DecodeNone_RejectsInvalidInputs()
    {
        byte[] filteredRow = new byte[] { PngConstants.FilterTypeNone, 1, 2 };

        Assert.Throws<ArgumentNullException>(() => PngFilterDecoder.DecodeNone(null!));
        Assert.Throws<ArgumentNullException>(() => PngFilterDecoder.DecodeNone(null!, 0, 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngFilterDecoder.DecodeNone(new byte[0]));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngFilterDecoder.DecodeNone(filteredRow, -1, 1));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngFilterDecoder.DecodeNone(filteredRow, filteredRow.Length, 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngFilterDecoder.DecodeNone(filteredRow, 0, -1));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngFilterDecoder.DecodeNone(filteredRow, 0, 3));
    }

    [Test]
    public void DecodeSub_ReconstructsRowUsingLeftByte()
    {
        byte[] filteredRow = new byte[] { PngConstants.FilterTypeSub, 10, 20, 30, 5, 5, 5 };

        byte[] row = PngFilterDecoder.DecodeSub(filteredRow, 3);

        Assert.That(row, Is.EqualTo(new byte[] { 10, 20, 30, 15, 25, 35 }));
    }

    [Test]
    public void DecodeSub_ReconstructsCumulativeSingleBytePixels()
    {
        byte[] filteredRow = new byte[] { PngConstants.FilterTypeSub, 10, 5, 250, 1 };

        byte[] row = PngFilterDecoder.DecodeSub(filteredRow, 1);

        Assert.That(row, Is.EqualTo(new byte[] { 10, 15, 9, 10 }));
    }

    [Test]
    public void DecodeSub_UsesOffsetAndRowDataBytes()
    {
        byte[] filteredRows = new byte[] { 99, PngConstants.FilterTypeSub, 10, 20, 30, 5, 5, 5, 88 };

        byte[] row = PngFilterDecoder.DecodeSub(filteredRows, 1, 6, 3);

        Assert.That(row, Is.EqualTo(new byte[] { 10, 20, 30, 15, 25, 35 }));
    }

    [Test]
    public void DecodeSub_CopiesWhenBytesPerPixelExceedsRowLength()
    {
        byte[] filteredRow = new byte[] { PngConstants.FilterTypeSub, 1, 2, 3 };

        byte[] row = PngFilterDecoder.DecodeSub(filteredRow, 8);

        Assert.That(row, Is.EqualTo(new byte[] { 1, 2, 3 }));
    }

    [Test]
    public void DecodeSub_AllowsEmptyRowPayload()
    {
        byte[] filteredRow = new byte[] { PngConstants.FilterTypeSub };

        byte[] row = PngFilterDecoder.DecodeSub(filteredRow, 1);

        Assert.That(row, Is.Not.Null);
        Assert.That(row, Is.Empty);
    }

    [Test]
    public void DecodeSub_RejectsWrongFilterType()
    {
        byte[] filteredRow = new byte[] { PngConstants.FilterTypeNone, 1, 2, 3 };

        Assert.Throws<InvalidDataException>(() => PngFilterDecoder.DecodeSub(filteredRow, 1));
    }

    [Test]
    public void DecodeSub_RejectsInvalidInputs()
    {
        byte[] filteredRow = new byte[] { PngConstants.FilterTypeSub, 1, 2 };

        Assert.Throws<ArgumentNullException>(() => PngFilterDecoder.DecodeSub(null!, 1));
        Assert.Throws<ArgumentNullException>(() => PngFilterDecoder.DecodeSub(null!, 0, 0, 1));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngFilterDecoder.DecodeSub(new byte[0], 1));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngFilterDecoder.DecodeSub(filteredRow, -1, 1, 1));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngFilterDecoder.DecodeSub(filteredRow, filteredRow.Length, 0, 1));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngFilterDecoder.DecodeSub(filteredRow, 0, -1, 1));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngFilterDecoder.DecodeSub(filteredRow, 0, 3, 1));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngFilterDecoder.DecodeSub(filteredRow, 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngFilterDecoder.DecodeSub(filteredRow, 0, 2, 0));
    }

    [Test]
    public void DecodeUp_ReconstructsRowUsingPreviousRow()
    {
        byte[] filteredRow = new byte[] { PngConstants.FilterTypeUp, 1, 2, 10 };
        byte[] previousRow = new byte[] { 10, 20, 250 };

        byte[] row = PngFilterDecoder.DecodeUp(filteredRow, previousRow);

        Assert.That(row, Is.EqualTo(new byte[] { 11, 22, 4 }));
    }

    [Test]
    public void DecodeUp_UsesZeroPreviousRowWhenPreviousRowIsMissing()
    {
        byte[] filteredRow = new byte[] { PngConstants.FilterTypeUp, 1, 2, 250 };

        byte[] row = PngFilterDecoder.DecodeUp(filteredRow);

        Assert.That(row, Is.EqualTo(new byte[] { 1, 2, 250 }));
    }

    [Test]
    public void DecodeUp_UsesOffsetAndRowDataBytes()
    {
        byte[] filteredRows = new byte[] { 99, PngConstants.FilterTypeUp, 1, 2, 10, 88 };
        byte[] previousRow = new byte[] { 10, 20, 250 };

        byte[] row = PngFilterDecoder.DecodeUp(filteredRows, 1, 3, previousRow);

        Assert.That(row, Is.EqualTo(new byte[] { 11, 22, 4 }));
    }

    [Test]
    public void DecodeUp_DoesNotReadPastRequestedRowBytes()
    {
        byte[] filteredRows = new byte[] { PngConstants.FilterTypeUp, 1, 2, 3, 4 };
        byte[] previousRow = new byte[] { 10, 20, 30, 40 };

        byte[] row = PngFilterDecoder.DecodeUp(filteredRows, 0, 2, previousRow);

        Assert.That(row, Is.EqualTo(new byte[] { 11, 22 }));
    }

    [Test]
    public void DecodeUp_AllowsEmptyRowPayload()
    {
        byte[] filteredRow = new byte[] { PngConstants.FilterTypeUp };

        byte[] row = PngFilterDecoder.DecodeUp(filteredRow);

        Assert.That(row, Is.Not.Null);
        Assert.That(row, Is.Empty);
    }

    [Test]
    public void DecodeUp_RejectsWrongFilterType()
    {
        byte[] filteredRow = new byte[] { PngConstants.FilterTypeNone, 1, 2, 3 };

        Assert.Throws<InvalidDataException>(() => PngFilterDecoder.DecodeUp(filteredRow));
    }

    [Test]
    public void DecodeUp_RejectsInvalidInputs()
    {
        byte[] filteredRow = new byte[] { PngConstants.FilterTypeUp, 1, 2 };

        Assert.Throws<ArgumentNullException>(() => PngFilterDecoder.DecodeUp(null!));
        Assert.Throws<ArgumentNullException>(() => PngFilterDecoder.DecodeUp(null!, new byte[0]));
        Assert.Throws<ArgumentNullException>(() => PngFilterDecoder.DecodeUp(filteredRow, (byte[])null!));
        Assert.Throws<ArgumentNullException>(() => PngFilterDecoder.DecodeUp(null!, 0, 0));
        Assert.Throws<ArgumentNullException>(() => PngFilterDecoder.DecodeUp(filteredRow, 0, 2, null!));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngFilterDecoder.DecodeUp(new byte[0]));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngFilterDecoder.DecodeUp(filteredRow, -1, 1));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngFilterDecoder.DecodeUp(filteredRow, filteredRow.Length, 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngFilterDecoder.DecodeUp(filteredRow, 0, -1));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngFilterDecoder.DecodeUp(filteredRow, 0, 3));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngFilterDecoder.DecodeUp(filteredRow, 0, 2, new byte[1]));
    }

    [Test]
    public void DecodeAverage_ReconstructsRowUsingLeftAndPreviousRow()
    {
        byte[] filteredRow = new byte[] { PngConstants.FilterTypeAverage, 5, 10, 20, 2, 3, 4 };
        byte[] previousRow = new byte[] { 10, 20, 30, 40, 50, 60 };

        byte[] row = PngFilterDecoder.DecodeAverage(filteredRow, 3, previousRow);

        Assert.That(row, Is.EqualTo(new byte[] { 10, 20, 35, 27, 38, 51 }));
    }

    [Test]
    public void DecodeAverage_UsesZeroPreviousRowWhenPreviousRowIsMissing()
    {
        byte[] filteredRow = new byte[] { PngConstants.FilterTypeAverage, 10, 20, 30, 5, 5, 5 };

        byte[] row = PngFilterDecoder.DecodeAverage(filteredRow, 3);

        Assert.That(row, Is.EqualTo(new byte[] { 10, 20, 30, 10, 15, 20 }));
    }

    [Test]
    public void DecodeAverage_HandlesModuloOverflow()
    {
        byte[] filteredRow = new byte[] { PngConstants.FilterTypeAverage, 250, 250 };
        byte[] previousRow = new byte[] { 250, 250 };

        byte[] row = PngFilterDecoder.DecodeAverage(filteredRow, 1, previousRow);

        Assert.That(row, Is.EqualTo(new byte[] { 119, 178 }));
    }

    [Test]
    public void DecodeAverage_UsesOffsetAndRowDataBytes()
    {
        byte[] filteredRows = new byte[] { 99, PngConstants.FilterTypeAverage, 5, 10, 20, 2, 3, 4, 88 };
        byte[] previousRow = new byte[] { 10, 20, 30, 40, 50, 60 };

        byte[] row = PngFilterDecoder.DecodeAverage(filteredRows, 1, 6, 3, previousRow);

        Assert.That(row, Is.EqualTo(new byte[] { 10, 20, 35, 27, 38, 51 }));
    }

    [Test]
    public void DecodeAverage_DoesNotReadPastRequestedRowBytes()
    {
        byte[] filteredRows = new byte[] { PngConstants.FilterTypeAverage, 5, 10, 20, 2, 3, 4 };
        byte[] previousRow = new byte[] { 10, 20, 30, 40, 50, 60 };

        byte[] row = PngFilterDecoder.DecodeAverage(filteredRows, 0, 3, 3, previousRow);

        Assert.That(row, Is.EqualTo(new byte[] { 10, 20, 35 }));
    }

    [Test]
    public void DecodeAverage_AllowsEmptyRowPayload()
    {
        byte[] filteredRow = new byte[] { PngConstants.FilterTypeAverage };

        byte[] row = PngFilterDecoder.DecodeAverage(filteredRow, 1);

        Assert.That(row, Is.Not.Null);
        Assert.That(row, Is.Empty);
    }

    [Test]
    public void DecodeAverage_RejectsWrongFilterType()
    {
        byte[] filteredRow = new byte[] { PngConstants.FilterTypeNone, 1, 2, 3 };

        Assert.Throws<InvalidDataException>(() => PngFilterDecoder.DecodeAverage(filteredRow, 1));
    }

    [Test]
    public void DecodeAverage_RejectsInvalidInputs()
    {
        byte[] filteredRow = new byte[] { PngConstants.FilterTypeAverage, 1, 2 };

        Assert.Throws<ArgumentNullException>(() => PngFilterDecoder.DecodeAverage(null!, 1));
        Assert.Throws<ArgumentNullException>(() => PngFilterDecoder.DecodeAverage(null!, 1, new byte[0]));
        Assert.Throws<ArgumentNullException>(() => PngFilterDecoder.DecodeAverage(filteredRow, 1, (byte[])null!));
        Assert.Throws<ArgumentNullException>(() => PngFilterDecoder.DecodeAverage(null!, 0, 0, 1));
        Assert.Throws<ArgumentNullException>(() => PngFilterDecoder.DecodeAverage(filteredRow, 0, 2, 1, null!));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngFilterDecoder.DecodeAverage(new byte[0], 1));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngFilterDecoder.DecodeAverage(filteredRow, -1, 1, 1));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngFilterDecoder.DecodeAverage(filteredRow, filteredRow.Length, 0, 1));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngFilterDecoder.DecodeAverage(filteredRow, 0, -1, 1));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngFilterDecoder.DecodeAverage(filteredRow, 0, 3, 1));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngFilterDecoder.DecodeAverage(filteredRow, 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngFilterDecoder.DecodeAverage(filteredRow, 0, 2, 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngFilterDecoder.DecodeAverage(filteredRow, 0, 2, 1, new byte[1]));
    }

    [Test]
    public void DecodePaeth_ReconstructsRowUsingLeftUpAndUpperLeft()
    {
        byte[] filteredRow = new byte[] { PngConstants.FilterTypePaeth, 5, 10, 20, 2, 3, 4 };
        byte[] previousRow = new byte[] { 10, 20, 30, 40, 50, 60 };

        byte[] row = PngFilterDecoder.DecodePaeth(filteredRow, 3, previousRow);

        Assert.That(row, Is.EqualTo(new byte[] { 15, 30, 50, 42, 53, 64 }));
    }

    [Test]
    public void DecodePaeth_UsesZeroPreviousRowWhenPreviousRowIsMissing()
    {
        byte[] filteredRow = new byte[] { PngConstants.FilterTypePaeth, 10, 20, 30, 5, 5, 5 };

        byte[] row = PngFilterDecoder.DecodePaeth(filteredRow, 3);

        Assert.That(row, Is.EqualTo(new byte[] { 10, 20, 30, 15, 25, 35 }));
    }

    [Test]
    public void DecodePaeth_ChoosesUpperLeftPredictorWhenClosest()
    {
        byte[] filteredRow = new byte[] { PngConstants.FilterTypePaeth, 50, 251, 0 };
        byte[] previousRow = new byte[] { 0, 55, 60 };

        byte[] row = PngFilterDecoder.DecodePaeth(filteredRow, 1, previousRow);

        Assert.That(row, Is.EqualTo(new byte[] { 50, 50, 55 }));
    }

    [Test]
    public void DecodePaeth_HandlesModuloOverflow()
    {
        byte[] filteredRow = new byte[] { PngConstants.FilterTypePaeth, 250, 250 };
        byte[] previousRow = new byte[] { 250, 250 };

        byte[] row = PngFilterDecoder.DecodePaeth(filteredRow, 1, previousRow);

        Assert.That(row, Is.EqualTo(new byte[] { 244, 238 }));
    }

    [Test]
    public void DecodePaeth_UsesOffsetAndRowDataBytes()
    {
        byte[] filteredRows = new byte[] { 99, PngConstants.FilterTypePaeth, 5, 10, 20, 2, 3, 4, 88 };
        byte[] previousRow = new byte[] { 10, 20, 30, 40, 50, 60 };

        byte[] row = PngFilterDecoder.DecodePaeth(filteredRows, 1, 6, 3, previousRow);

        Assert.That(row, Is.EqualTo(new byte[] { 15, 30, 50, 42, 53, 64 }));
    }

    [Test]
    public void DecodePaeth_DoesNotReadPastRequestedRowBytes()
    {
        byte[] filteredRows = new byte[] { PngConstants.FilterTypePaeth, 5, 10, 20, 2, 3, 4 };
        byte[] previousRow = new byte[] { 10, 20, 30, 40, 50, 60 };

        byte[] row = PngFilterDecoder.DecodePaeth(filteredRows, 0, 3, 3, previousRow);

        Assert.That(row, Is.EqualTo(new byte[] { 15, 30, 50 }));
    }

    [Test]
    public void DecodePaeth_AllowsEmptyRowPayload()
    {
        byte[] filteredRow = new byte[] { PngConstants.FilterTypePaeth };

        byte[] row = PngFilterDecoder.DecodePaeth(filteredRow, 1);

        Assert.That(row, Is.Not.Null);
        Assert.That(row, Is.Empty);
    }

    [Test]
    public void DecodePaeth_RejectsWrongFilterType()
    {
        byte[] filteredRow = new byte[] { PngConstants.FilterTypeNone, 1, 2, 3 };

        Assert.Throws<InvalidDataException>(() => PngFilterDecoder.DecodePaeth(filteredRow, 1));
    }

    [Test]
    public void DecodePaeth_RejectsInvalidInputs()
    {
        byte[] filteredRow = new byte[] { PngConstants.FilterTypePaeth, 1, 2 };

        Assert.Throws<ArgumentNullException>(() => PngFilterDecoder.DecodePaeth(null!, 1));
        Assert.Throws<ArgumentNullException>(() => PngFilterDecoder.DecodePaeth(null!, 1, new byte[0]));
        Assert.Throws<ArgumentNullException>(() => PngFilterDecoder.DecodePaeth(filteredRow, 1, (byte[])null!));
        Assert.Throws<ArgumentNullException>(() => PngFilterDecoder.DecodePaeth(null!, 0, 0, 1));
        Assert.Throws<ArgumentNullException>(() => PngFilterDecoder.DecodePaeth(filteredRow, 0, 2, 1, null!));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngFilterDecoder.DecodePaeth(new byte[0], 1));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngFilterDecoder.DecodePaeth(filteredRow, -1, 1, 1));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngFilterDecoder.DecodePaeth(filteredRow, filteredRow.Length, 0, 1));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngFilterDecoder.DecodePaeth(filteredRow, 0, -1, 1));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngFilterDecoder.DecodePaeth(filteredRow, 0, 3, 1));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngFilterDecoder.DecodePaeth(filteredRow, 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngFilterDecoder.DecodePaeth(filteredRow, 0, 2, 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngFilterDecoder.DecodePaeth(filteredRow, 0, 2, 1, new byte[1]));
    }
}
