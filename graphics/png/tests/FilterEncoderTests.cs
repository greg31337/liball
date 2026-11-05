using NUnit.Framework;

namespace png.Tests;

[TestFixture]
public class FilterEncoderTests
{
    [Test]
    public void EncodeNone_PrependsFilterByteAndCopiesRowPayload()
    {
        byte[] rowData = new byte[] { 1, 2, 3, 255 };

        byte[] filteredRow = PngFilterEncoder.EncodeNone(rowData);

        Assert.That(filteredRow, Is.EqualTo(new byte[] { PngConstants.FilterTypeNone, 1, 2, 3, 255 }));
    }

    [Test]
    public void EncodeNone_AllowsEmptyRowPayload()
    {
        byte[] rowData = new byte[0];

        byte[] filteredRow = PngFilterEncoder.EncodeNone(rowData);

        Assert.That(filteredRow, Is.EqualTo(new byte[] { PngConstants.FilterTypeNone }));
    }

    [Test]
    public void EncodeNone_UsesOffsetAndRowDataBytes()
    {
        byte[] rowData = new byte[] { 99, 10, 20, 30, 88 };

        byte[] filteredRow = PngFilterEncoder.EncodeNone(rowData, 1, 3);

        Assert.That(filteredRow, Is.EqualTo(new byte[] { PngConstants.FilterTypeNone, 10, 20, 30 }));
    }

    [Test]
    public void EncodeNone_DoesNotReadPastRequestedRowBytes()
    {
        byte[] rowData = new byte[] { 1, 2, 3, 4 };

        byte[] filteredRow = PngFilterEncoder.EncodeNone(rowData, 0, 2);

        Assert.That(filteredRow, Is.EqualTo(new byte[] { PngConstants.FilterTypeNone, 1, 2 }));
    }

    [Test]
    public void EncodeNone_ReturnsIndependentRow()
    {
        byte[] rowData = new byte[] { 1, 2, 3 };

        byte[] filteredRow = PngFilterEncoder.EncodeNone(rowData);
        rowData[0] = 99;

        Assert.That(filteredRow, Is.EqualTo(new byte[] { PngConstants.FilterTypeNone, 1, 2, 3 }));
    }

    [Test]
    public void EncodeNone_RoundTripsThroughDecoder()
    {
        byte[] rowData = new byte[] { 5, 10, 15, 20 };

        byte[] filteredRow = PngFilterEncoder.EncodeNone(rowData);
        byte[] decodedRow = PngFilterDecoder.DecodeNone(filteredRow);

        Assert.That(decodedRow, Is.EqualTo(rowData));
    }

    [Test]
    public void EncodeNone_RejectsInvalidInputs()
    {
        byte[] rowData = new byte[] { 1, 2 };

        Assert.Throws<ArgumentNullException>(() => PngFilterEncoder.EncodeNone(null!));
        Assert.Throws<ArgumentNullException>(() => PngFilterEncoder.EncodeNone(null!, 0, 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngFilterEncoder.EncodeNone(rowData, -1, 1));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngFilterEncoder.EncodeNone(rowData, rowData.Length + 1, 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngFilterEncoder.EncodeNone(rowData, 0, -1));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngFilterEncoder.EncodeNone(rowData, 0, 3));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngFilterEncoder.EncodeNone(rowData, rowData.Length, 1));
    }

    [Test]
    public void EncodeSub_StoresDifferenceFromLeftByte()
    {
        byte[] rowData = new byte[] { 10, 20, 30, 15, 25, 35 };

        byte[] filteredRow = PngFilterEncoder.EncodeSub(rowData, 3);

        Assert.That(filteredRow, Is.EqualTo(new byte[] { PngConstants.FilterTypeSub, 10, 20, 30, 5, 5, 5 }));
    }

    [Test]
    public void EncodeSub_StoresCumulativeSingleBytePixels()
    {
        byte[] rowData = new byte[] { 10, 15, 9, 10 };

        byte[] filteredRow = PngFilterEncoder.EncodeSub(rowData, 1);

        Assert.That(filteredRow, Is.EqualTo(new byte[] { PngConstants.FilterTypeSub, 10, 5, 250, 1 }));
    }

    [Test]
    public void EncodeSub_UsesOffsetAndRowDataBytes()
    {
        byte[] rowData = new byte[] { 99, 10, 20, 30, 15, 25, 35, 88 };

        byte[] filteredRow = PngFilterEncoder.EncodeSub(rowData, 1, 6, 3);

        Assert.That(filteredRow, Is.EqualTo(new byte[] { PngConstants.FilterTypeSub, 10, 20, 30, 5, 5, 5 }));
    }

    [Test]
    public void EncodeSub_CopiesWhenBytesPerPixelExceedsRowLength()
    {
        byte[] rowData = new byte[] { 1, 2, 3 };

        byte[] filteredRow = PngFilterEncoder.EncodeSub(rowData, 8);

        Assert.That(filteredRow, Is.EqualTo(new byte[] { PngConstants.FilterTypeSub, 1, 2, 3 }));
    }

    [Test]
    public void EncodeSub_AllowsEmptyRowPayload()
    {
        byte[] rowData = new byte[0];

        byte[] filteredRow = PngFilterEncoder.EncodeSub(rowData, 1);

        Assert.That(filteredRow, Is.EqualTo(new byte[] { PngConstants.FilterTypeSub }));
    }

    [Test]
    public void EncodeSub_RoundTripsThroughDecoder()
    {
        byte[] rowData = new byte[] { 5, 10, 15, 20 };

        byte[] filteredRow = PngFilterEncoder.EncodeSub(rowData, 1);
        byte[] decodedRow = PngFilterDecoder.DecodeSub(filteredRow, 1);

        Assert.That(decodedRow, Is.EqualTo(rowData));
    }

    [Test]
    public void EncodeSub_RejectsInvalidInputs()
    {
        byte[] rowData = new byte[] { 1, 2 };

        Assert.Throws<ArgumentNullException>(() => PngFilterEncoder.EncodeSub(null!, 1));
        Assert.Throws<ArgumentNullException>(() => PngFilterEncoder.EncodeSub(null!, 0, 0, 1));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngFilterEncoder.EncodeSub(rowData, -1, 1, 1));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngFilterEncoder.EncodeSub(rowData, rowData.Length + 1, 0, 1));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngFilterEncoder.EncodeSub(rowData, 0, -1, 1));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngFilterEncoder.EncodeSub(rowData, 0, 3, 1));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngFilterEncoder.EncodeSub(rowData, rowData.Length, 1, 1));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngFilterEncoder.EncodeSub(rowData, 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngFilterEncoder.EncodeSub(rowData, 0, 2, 0));
    }

    [Test]
    public void EncodeUp_StoresDifferenceFromPreviousRow()
    {
        byte[] rowData = new byte[] { 11, 22, 4 };
        byte[] previousRow = new byte[] { 10, 20, 250 };

        byte[] filteredRow = PngFilterEncoder.EncodeUp(rowData, previousRow);

        Assert.That(filteredRow, Is.EqualTo(new byte[] { PngConstants.FilterTypeUp, 1, 2, 10 }));
    }

    [Test]
    public void EncodeUp_UsesZeroPreviousRowWhenPreviousRowIsMissing()
    {
        byte[] rowData = new byte[] { 1, 2, 250 };

        byte[] filteredRow = PngFilterEncoder.EncodeUp(rowData);

        Assert.That(filteredRow, Is.EqualTo(new byte[] { PngConstants.FilterTypeUp, 1, 2, 250 }));
    }

    [Test]
    public void EncodeUp_HandlesModuloUnderflow()
    {
        byte[] rowData = new byte[] { 4 };
        byte[] previousRow = new byte[] { 250 };

        byte[] filteredRow = PngFilterEncoder.EncodeUp(rowData, previousRow);

        Assert.That(filteredRow, Is.EqualTo(new byte[] { PngConstants.FilterTypeUp, 10 }));
    }

    [Test]
    public void EncodeUp_UsesOffsetAndRowDataBytes()
    {
        byte[] rowData = new byte[] { 99, 11, 22, 4, 88 };
        byte[] previousRow = new byte[] { 10, 20, 250 };

        byte[] filteredRow = PngFilterEncoder.EncodeUp(rowData, 1, 3, previousRow);

        Assert.That(filteredRow, Is.EqualTo(new byte[] { PngConstants.FilterTypeUp, 1, 2, 10 }));
    }

    [Test]
    public void EncodeUp_DoesNotReadPastRequestedRowBytes()
    {
        byte[] rowData = new byte[] { 11, 22, 4, 99 };
        byte[] previousRow = new byte[] { 10, 20, 250, 99 };

        byte[] filteredRow = PngFilterEncoder.EncodeUp(rowData, 0, 2, previousRow);

        Assert.That(filteredRow, Is.EqualTo(new byte[] { PngConstants.FilterTypeUp, 1, 2 }));
    }

    [Test]
    public void EncodeUp_AllowsEmptyRowPayload()
    {
        byte[] rowData = new byte[0];

        byte[] filteredRow = PngFilterEncoder.EncodeUp(rowData);

        Assert.That(filteredRow, Is.EqualTo(new byte[] { PngConstants.FilterTypeUp }));
    }

    [Test]
    public void EncodeUp_RoundTripsThroughDecoder()
    {
        byte[] rowData = new byte[] { 11, 22, 4 };
        byte[] previousRow = new byte[] { 10, 20, 250 };

        byte[] filteredRow = PngFilterEncoder.EncodeUp(rowData, previousRow);
        byte[] decodedRow = PngFilterDecoder.DecodeUp(filteredRow, previousRow);

        Assert.That(decodedRow, Is.EqualTo(rowData));
    }

    [Test]
    public void EncodeUp_RejectsInvalidInputs()
    {
        byte[] rowData = new byte[] { 1, 2 };

        Assert.Throws<ArgumentNullException>(() => PngFilterEncoder.EncodeUp(null!));
        Assert.Throws<ArgumentNullException>(() => PngFilterEncoder.EncodeUp(null!, new byte[0]));
        Assert.Throws<ArgumentNullException>(() => PngFilterEncoder.EncodeUp(rowData, (byte[])null!));
        Assert.Throws<ArgumentNullException>(() => PngFilterEncoder.EncodeUp(null!, 0, 0));
        Assert.Throws<ArgumentNullException>(() => PngFilterEncoder.EncodeUp(rowData, 0, 2, null!));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngFilterEncoder.EncodeUp(rowData, -1, 1));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngFilterEncoder.EncodeUp(rowData, rowData.Length + 1, 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngFilterEncoder.EncodeUp(rowData, 0, -1));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngFilterEncoder.EncodeUp(rowData, 0, 3));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngFilterEncoder.EncodeUp(rowData, rowData.Length, 1));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngFilterEncoder.EncodeUp(rowData, 0, 2, new byte[1]));
    }

    [Test]
    public void EncodeAverage_StoresDifferenceFromAverageOfLeftAndPreviousRow()
    {
        byte[] rowData = new byte[] { 10, 20, 35, 27, 38, 51 };
        byte[] previousRow = new byte[] { 10, 20, 30, 40, 50, 60 };

        byte[] filteredRow = PngFilterEncoder.EncodeAverage(rowData, 3, previousRow);

        Assert.That(filteredRow, Is.EqualTo(new byte[] { PngConstants.FilterTypeAverage, 5, 10, 20, 2, 3, 4 }));
    }

    [Test]
    public void EncodeAverage_UsesZeroPreviousRowWhenPreviousRowIsMissing()
    {
        byte[] rowData = new byte[] { 10, 20, 30, 10, 15, 20 };

        byte[] filteredRow = PngFilterEncoder.EncodeAverage(rowData, 3);

        Assert.That(filteredRow, Is.EqualTo(new byte[] { PngConstants.FilterTypeAverage, 10, 20, 30, 5, 5, 5 }));
    }

    [Test]
    public void EncodeAverage_HandlesModuloUnderflow()
    {
        byte[] rowData = new byte[] { 119, 178 };
        byte[] previousRow = new byte[] { 250, 250 };

        byte[] filteredRow = PngFilterEncoder.EncodeAverage(rowData, 1, previousRow);

        Assert.That(filteredRow, Is.EqualTo(new byte[] { PngConstants.FilterTypeAverage, 250, 250 }));
    }

    [Test]
    public void EncodeAverage_UsesOffsetAndRowDataBytes()
    {
        byte[] rowData = new byte[] { 99, 10, 20, 35, 27, 38, 51, 88 };
        byte[] previousRow = new byte[] { 10, 20, 30, 40, 50, 60 };

        byte[] filteredRow = PngFilterEncoder.EncodeAverage(rowData, 1, 6, 3, previousRow);

        Assert.That(filteredRow, Is.EqualTo(new byte[] { PngConstants.FilterTypeAverage, 5, 10, 20, 2, 3, 4 }));
    }

    [Test]
    public void EncodeAverage_DoesNotReadPastRequestedRowBytes()
    {
        byte[] rowData = new byte[] { 10, 20, 35, 99 };
        byte[] previousRow = new byte[] { 10, 20, 30, 99 };

        byte[] filteredRow = PngFilterEncoder.EncodeAverage(rowData, 0, 3, 3, previousRow);

        Assert.That(filteredRow, Is.EqualTo(new byte[] { PngConstants.FilterTypeAverage, 5, 10, 20 }));
    }

    [Test]
    public void EncodeAverage_AllowsEmptyRowPayload()
    {
        byte[] rowData = new byte[0];

        byte[] filteredRow = PngFilterEncoder.EncodeAverage(rowData, 1);

        Assert.That(filteredRow, Is.EqualTo(new byte[] { PngConstants.FilterTypeAverage }));
    }

    [Test]
    public void EncodeAverage_RoundTripsThroughDecoder()
    {
        byte[] rowData = new byte[] { 10, 20, 35, 27, 38, 51 };
        byte[] previousRow = new byte[] { 10, 20, 30, 40, 50, 60 };

        byte[] filteredRow = PngFilterEncoder.EncodeAverage(rowData, 3, previousRow);
        byte[] decodedRow = PngFilterDecoder.DecodeAverage(filteredRow, 3, previousRow);

        Assert.That(decodedRow, Is.EqualTo(rowData));
    }

    [Test]
    public void EncodeAverage_RejectsInvalidInputs()
    {
        byte[] rowData = new byte[] { 1, 2 };

        Assert.Throws<ArgumentNullException>(() => PngFilterEncoder.EncodeAverage(null!, 1));
        Assert.Throws<ArgumentNullException>(() => PngFilterEncoder.EncodeAverage(null!, 1, new byte[0]));
        Assert.Throws<ArgumentNullException>(() => PngFilterEncoder.EncodeAverage(rowData, 1, (byte[])null!));
        Assert.Throws<ArgumentNullException>(() => PngFilterEncoder.EncodeAverage(null!, 0, 0, 1));
        Assert.Throws<ArgumentNullException>(() => PngFilterEncoder.EncodeAverage(rowData, 0, 2, 1, null!));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngFilterEncoder.EncodeAverage(rowData, -1, 1, 1));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngFilterEncoder.EncodeAverage(rowData, rowData.Length + 1, 0, 1));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngFilterEncoder.EncodeAverage(rowData, 0, -1, 1));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngFilterEncoder.EncodeAverage(rowData, 0, 3, 1));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngFilterEncoder.EncodeAverage(rowData, rowData.Length, 1, 1));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngFilterEncoder.EncodeAverage(rowData, 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngFilterEncoder.EncodeAverage(rowData, 0, 2, 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngFilterEncoder.EncodeAverage(rowData, 0, 2, 1, new byte[1]));
    }

    [Test]
    public void EncodePaeth_StoresDifferenceFromPaethPredictor()
    {
        byte[] rowData = new byte[] { 15, 30, 50, 42, 53, 64 };
        byte[] previousRow = new byte[] { 10, 20, 30, 40, 50, 60 };

        byte[] filteredRow = PngFilterEncoder.EncodePaeth(rowData, 3, previousRow);

        Assert.That(filteredRow, Is.EqualTo(new byte[] { PngConstants.FilterTypePaeth, 5, 10, 20, 2, 3, 4 }));
    }

    [Test]
    public void EncodePaeth_UsesZeroPreviousRowWhenPreviousRowIsMissing()
    {
        byte[] rowData = new byte[] { 10, 20, 30, 15, 25, 35 };

        byte[] filteredRow = PngFilterEncoder.EncodePaeth(rowData, 3);

        Assert.That(filteredRow, Is.EqualTo(new byte[] { PngConstants.FilterTypePaeth, 10, 20, 30, 5, 5, 5 }));
    }

    [Test]
    public void EncodePaeth_ChoosesUpperLeftPredictorWhenClosest()
    {
        byte[] rowData = new byte[] { 50, 50, 55 };
        byte[] previousRow = new byte[] { 0, 55, 60 };

        byte[] filteredRow = PngFilterEncoder.EncodePaeth(rowData, 1, previousRow);

        Assert.That(filteredRow, Is.EqualTo(new byte[] { PngConstants.FilterTypePaeth, 50, 251, 0 }));
    }

    [Test]
    public void EncodePaeth_HandlesModuloUnderflow()
    {
        byte[] rowData = new byte[] { 244, 238 };
        byte[] previousRow = new byte[] { 250, 250 };

        byte[] filteredRow = PngFilterEncoder.EncodePaeth(rowData, 1, previousRow);

        Assert.That(filteredRow, Is.EqualTo(new byte[] { PngConstants.FilterTypePaeth, 250, 250 }));
    }

    [Test]
    public void EncodePaeth_UsesOffsetAndRowDataBytes()
    {
        byte[] rowData = new byte[] { 99, 15, 30, 50, 42, 53, 64, 88 };
        byte[] previousRow = new byte[] { 10, 20, 30, 40, 50, 60 };

        byte[] filteredRow = PngFilterEncoder.EncodePaeth(rowData, 1, 6, 3, previousRow);

        Assert.That(filteredRow, Is.EqualTo(new byte[] { PngConstants.FilterTypePaeth, 5, 10, 20, 2, 3, 4 }));
    }

    [Test]
    public void EncodePaeth_DoesNotReadPastRequestedRowBytes()
    {
        byte[] rowData = new byte[] { 15, 30, 50, 99 };
        byte[] previousRow = new byte[] { 10, 20, 30, 99 };

        byte[] filteredRow = PngFilterEncoder.EncodePaeth(rowData, 0, 3, 3, previousRow);

        Assert.That(filteredRow, Is.EqualTo(new byte[] { PngConstants.FilterTypePaeth, 5, 10, 20 }));
    }

    [Test]
    public void EncodePaeth_AllowsEmptyRowPayload()
    {
        byte[] rowData = new byte[0];

        byte[] filteredRow = PngFilterEncoder.EncodePaeth(rowData, 1);

        Assert.That(filteredRow, Is.EqualTo(new byte[] { PngConstants.FilterTypePaeth }));
    }

    [Test]
    public void EncodePaeth_RoundTripsThroughDecoder()
    {
        byte[] rowData = new byte[] { 15, 30, 50, 42, 53, 64 };
        byte[] previousRow = new byte[] { 10, 20, 30, 40, 50, 60 };

        byte[] filteredRow = PngFilterEncoder.EncodePaeth(rowData, 3, previousRow);
        byte[] decodedRow = PngFilterDecoder.DecodePaeth(filteredRow, 3, previousRow);

        Assert.That(decodedRow, Is.EqualTo(rowData));
    }

    [Test]
    public void EncodePaeth_RejectsInvalidInputs()
    {
        byte[] rowData = new byte[] { 1, 2 };

        Assert.Throws<ArgumentNullException>(() => PngFilterEncoder.EncodePaeth(null!, 1));
        Assert.Throws<ArgumentNullException>(() => PngFilterEncoder.EncodePaeth(null!, 1, new byte[0]));
        Assert.Throws<ArgumentNullException>(() => PngFilterEncoder.EncodePaeth(rowData, 1, (byte[])null!));
        Assert.Throws<ArgumentNullException>(() => PngFilterEncoder.EncodePaeth(null!, 0, 0, 1));
        Assert.Throws<ArgumentNullException>(() => PngFilterEncoder.EncodePaeth(rowData, 0, 2, 1, null!));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngFilterEncoder.EncodePaeth(rowData, -1, 1, 1));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngFilterEncoder.EncodePaeth(rowData, rowData.Length + 1, 0, 1));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngFilterEncoder.EncodePaeth(rowData, 0, -1, 1));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngFilterEncoder.EncodePaeth(rowData, 0, 3, 1));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngFilterEncoder.EncodePaeth(rowData, rowData.Length, 1, 1));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngFilterEncoder.EncodePaeth(rowData, 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngFilterEncoder.EncodePaeth(rowData, 0, 2, 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngFilterEncoder.EncodePaeth(rowData, 0, 2, 1, new byte[1]));
    }

    [Test]
    public void EncodeFixed_DispatchesEveryFilterType()
    {
        byte[] rowData = new byte[] { 15, 30, 50, 42, 53, 64 };
        byte[] previousRow = new byte[] { 10, 20, 30, 40, 50, 60 };

        byte[] none = PngFilterEncoder.EncodeFixed(rowData, 3, previousRow, PngFilterType.None);
        byte[] sub = PngFilterEncoder.EncodeFixed(rowData, 3, previousRow, PngFilterType.Sub);
        byte[] up = PngFilterEncoder.EncodeFixed(rowData, 3, previousRow, PngFilterType.Up);
        byte[] average = PngFilterEncoder.EncodeFixed(rowData, 3, previousRow, PngFilterType.Average);
        byte[] paeth = PngFilterEncoder.EncodeFixed(rowData, 3, previousRow, PngFilterType.Paeth);

        Assert.That(none[0], Is.EqualTo(PngConstants.FilterTypeNone));
        Assert.That(sub[0], Is.EqualTo(PngConstants.FilterTypeSub));
        Assert.That(up[0], Is.EqualTo(PngConstants.FilterTypeUp));
        Assert.That(average[0], Is.EqualTo(PngConstants.FilterTypeAverage));
        Assert.That(paeth[0], Is.EqualTo(PngConstants.FilterTypePaeth));
        Assert.That(PngFilterDecoder.DecodeNone(none), Is.EqualTo(rowData));
        Assert.That(PngFilterDecoder.DecodeSub(sub, 3), Is.EqualTo(rowData));
        Assert.That(PngFilterDecoder.DecodeUp(up, previousRow), Is.EqualTo(rowData));
        Assert.That(PngFilterDecoder.DecodeAverage(average, 3, previousRow), Is.EqualTo(rowData));
        Assert.That(PngFilterDecoder.DecodePaeth(paeth, 3, previousRow), Is.EqualTo(rowData));
    }

    [Test]
    public void EncodeFixed_UsesZeroPreviousRowWhenPreviousRowIsMissing()
    {
        byte[] rowData = new byte[] { 10, 20, 30, 15, 25, 35 };

        byte[] filteredRow = PngFilterEncoder.EncodeFixed(rowData, 3, PngFilterType.Paeth);

        Assert.That(filteredRow, Is.EqualTo(new byte[] { PngConstants.FilterTypePaeth, 10, 20, 30, 5, 5, 5 }));
    }

    [Test]
    public void EncodeFixed_UsesEncoderOptionsFixedFilterType()
    {
        byte[] rowData = new byte[] { 10, 20, 35, 27, 38, 51 };
        byte[] previousRow = new byte[] { 10, 20, 30, 40, 50, 60 };
        PngEncoderOptions options = new PngEncoderOptions();
        options.UseFixedFilter = true;
        options.FixedFilterType = PngFilterType.Average;

        byte[] filteredRow = PngFilterEncoder.EncodeFixed(rowData, 3, previousRow, options);

        Assert.That(filteredRow, Is.EqualTo(new byte[] { PngConstants.FilterTypeAverage, 5, 10, 20, 2, 3, 4 }));
    }

    [Test]
    public void EncodeFixed_UsesOffsetAndRowDataBytesWithOptions()
    {
        byte[] rowData = new byte[] { 99, 10, 20, 30, 15, 25, 35, 88 };
        PngEncoderOptions options = new PngEncoderOptions();
        options.UseFixedFilter = true;
        options.FixedFilterType = PngFilterType.Sub;

        byte[] filteredRow = PngFilterEncoder.EncodeFixed(rowData, 1, 6, 3, options);

        Assert.That(filteredRow, Is.EqualTo(new byte[] { PngConstants.FilterTypeSub, 10, 20, 30, 5, 5, 5 }));
    }

    [Test]
    public void EncodeFixed_RejectsInactiveOrInvalidFixedFilterOptions()
    {
        byte[] rowData = new byte[] { 1, 2 };
        PngEncoderOptions inactive = new PngEncoderOptions();
        PngEncoderOptions invalid = new PngEncoderOptions();
        invalid.UseFixedFilter = true;
        invalid.FixedFilterType = (PngFilterType)99;

        Assert.Throws<InvalidOperationException>(() => PngFilterEncoder.EncodeFixed(rowData, 1, inactive));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngFilterEncoder.EncodeFixed(rowData, 1, invalid));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngFilterEncoder.EncodeFixed(rowData, 1, (PngFilterType)99));
    }

    [Test]
    public void EncodeFixed_RejectsInvalidInputs()
    {
        byte[] rowData = new byte[] { 1, 2 };
        byte[] previousRow = new byte[] { 1, 2 };
        PngEncoderOptions options = new PngEncoderOptions();
        options.UseFixedFilter = true;
        options.FixedFilterType = PngFilterType.Up;

        Assert.Throws<ArgumentNullException>(() => PngFilterEncoder.EncodeFixed(null!, 1, PngFilterType.None));
        Assert.Throws<ArgumentNullException>(() => PngFilterEncoder.EncodeFixed(null!, 1, previousRow, PngFilterType.Up));
        Assert.Throws<ArgumentNullException>(() => PngFilterEncoder.EncodeFixed(rowData, 1, (byte[])null!, PngFilterType.Up));
        Assert.Throws<ArgumentNullException>(() => PngFilterEncoder.EncodeFixed(null!, 1, options));
        Assert.Throws<ArgumentNullException>(() => PngFilterEncoder.EncodeFixed(rowData, 1, (PngEncoderOptions)null!));
        Assert.Throws<ArgumentNullException>(() => PngFilterEncoder.EncodeFixed(rowData, 1, previousRow, null!));
        Assert.Throws<ArgumentNullException>(() => PngFilterEncoder.EncodeFixed(rowData, 0, 2, 1, null!, options));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngFilterEncoder.EncodeFixed(rowData, -1, 1, 1, PngFilterType.None));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngFilterEncoder.EncodeFixed(rowData, 0, 3, 1, PngFilterType.None));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngFilterEncoder.EncodeFixed(rowData, 0, 2, 0, PngFilterType.Sub));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngFilterEncoder.EncodeFixed(rowData, 0, 2, 1, new byte[1], PngFilterType.Up));
    }

    [Test]
    public void CalculateRowScore_UsesSignedByteMagnitudesAndSkipsFilterByte()
    {
        byte[] filteredRow = new byte[] { PngConstants.FilterTypeNone, 0, 1, 127, 128, 255 };

        long score = PngFilterEncoder.CalculateRowScore(filteredRow);

        Assert.That(score, Is.EqualTo(257));
    }

    [Test]
    public void EncodeAdaptive_ChoosesLowestScoringSubRow()
    {
        byte[] rowData = new byte[] { 10, 15, 9, 10 };

        byte[] filteredRow = PngFilterEncoder.EncodeAdaptive(rowData, 1);

        Assert.That(filteredRow, Is.EqualTo(new byte[] { PngConstants.FilterTypeSub, 10, 5, 250, 1 }));
    }

    [Test]
    public void EncodeAdaptive_ChoosesLowestScoringUpRowWithPreviousRow()
    {
        byte[] rowData = new byte[] { 11, 22, 4 };
        byte[] previousRow = new byte[] { 10, 20, 250 };

        byte[] filteredRow = PngFilterEncoder.EncodeAdaptive(rowData, 3, previousRow);

        Assert.That(filteredRow, Is.EqualTo(new byte[] { PngConstants.FilterTypeUp, 1, 2, 10 }));
    }

    [Test]
    public void EncodeAdaptive_PreservesFilterOrderForScoreTies()
    {
        byte[] rowData = new byte[0];

        byte[] filteredRow = PngFilterEncoder.EncodeAdaptive(rowData, 1);

        Assert.That(filteredRow, Is.EqualTo(new byte[] { PngConstants.FilterTypeNone }));
    }

    [Test]
    public void EncodeAdaptive_UsesOffsetAndRowDataBytes()
    {
        byte[] rowData = new byte[] { 99, 10, 15, 9, 10, 88 };

        byte[] filteredRow = PngFilterEncoder.EncodeAdaptive(rowData, 1, 4, 1);

        Assert.That(filteredRow, Is.EqualTo(new byte[] { PngConstants.FilterTypeSub, 10, 5, 250, 1 }));
    }

    [Test]
    public void EncodeRow_UsesAdaptiveFilteringByDefault()
    {
        byte[] rowData = new byte[] { 10, 15, 9, 10 };
        PngEncoderOptions options = new PngEncoderOptions();

        byte[] filteredRow = PngFilterEncoder.EncodeRow(rowData, 1, options);

        Assert.That(filteredRow, Is.EqualTo(new byte[] { PngConstants.FilterTypeSub, 10, 5, 250, 1 }));
    }

    [Test]
    public void EncodeRow_HonorsFixedFilterOptionBeforeAdaptiveScoring()
    {
        byte[] rowData = new byte[] { 10, 15, 9, 10 };
        PngEncoderOptions options = new PngEncoderOptions();
        options.UseFixedFilter = true;
        options.FixedFilterType = PngFilterType.None;

        byte[] filteredRow = PngFilterEncoder.EncodeRow(rowData, 1, options);

        Assert.That(filteredRow, Is.EqualTo(new byte[] { PngConstants.FilterTypeNone, 10, 15, 9, 10 }));
    }

    [Test]
    public void EncodeRow_AppliesFastFixedAndExhaustiveStrategies()
    {
        byte[] rowData = new byte[] { 2, 1 };
        byte[] previousRow = new byte[] { 1, 0 };
        PngEncoderOptions fast = new PngEncoderOptions();
        fast.RowFilterStrategy = PngRowFilterStrategy.Fast;
        PngEncoderOptions fixedOptions = new PngEncoderOptions();
        fixedOptions.RowFilterStrategy = PngRowFilterStrategy.Fixed;
        fixedOptions.FixedFilterType = PngFilterType.None;
        PngEncoderOptions exhaustive = new PngEncoderOptions();
        exhaustive.RowFilterStrategy = PngRowFilterStrategy.Exhaustive;

        byte[] fastFirstRow = PngFilterEncoder.EncodeRow(rowData, 1, fast);
        byte[] fastLaterRow = PngFilterEncoder.EncodeRow(rowData, 1, previousRow, fast);
        byte[] fixedRow = PngFilterEncoder.EncodeRow(rowData, 1, previousRow, fixedOptions);
        byte[] exhaustiveRow = PngFilterEncoder.EncodeRow(rowData, 1, previousRow, exhaustive);

        Assert.That(fastFirstRow[0], Is.EqualTo(PngConstants.FilterTypeSub));
        Assert.That(fastLaterRow[0], Is.EqualTo(PngConstants.FilterTypeUp));
        Assert.That(fixedRow[0], Is.EqualTo(PngConstants.FilterTypeNone));
        Assert.That(exhaustiveRow, Is.EqualTo(new byte[] { PngConstants.FilterTypePaeth, 1, 0 }));
    }

    [Test]
    public void EncodeRow_UsesOffsetAndPreviousRowWithOptions()
    {
        byte[] rowData = new byte[] { 99, 11, 22, 4, 88 };
        byte[] previousRow = new byte[] { 10, 20, 250 };
        PngEncoderOptions options = new PngEncoderOptions();

        byte[] filteredRow = PngFilterEncoder.EncodeRow(rowData, 1, 3, 3, previousRow, options);

        Assert.That(filteredRow, Is.EqualTo(new byte[] { PngConstants.FilterTypeUp, 1, 2, 10 }));
    }

    [Test]
    public void EncodeAdaptiveAndEncodeRow_RejectInvalidInputs()
    {
        byte[] rowData = new byte[] { 1, 2 };
        byte[] previousRow = new byte[] { 1, 2 };
        PngEncoderOptions options = new PngEncoderOptions();
        PngEncoderOptions invalidMethod = new PngEncoderOptions();
        PngEncoderOptions invalidStrategy = new PngEncoderOptions();
        invalidMethod.FilterMethod = (PngFilterMethod)99;
        invalidStrategy.RowFilterStrategy = (PngRowFilterStrategy)99;

        Assert.Throws<ArgumentNullException>(() => PngFilterEncoder.CalculateRowScore(null!));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngFilterEncoder.CalculateRowScore(new byte[0]));
        Assert.Throws<ArgumentNullException>(() => PngFilterEncoder.EncodeAdaptive(null!, 1));
        Assert.Throws<ArgumentNullException>(() => PngFilterEncoder.EncodeAdaptive(null!, 1, previousRow));
        Assert.Throws<ArgumentNullException>(() => PngFilterEncoder.EncodeAdaptive(rowData, 1, (byte[])null!));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngFilterEncoder.EncodeAdaptive(rowData, 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngFilterEncoder.EncodeAdaptive(rowData, 0, 3, 1));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngFilterEncoder.EncodeAdaptive(rowData, 0, 2, 1, new byte[1]));
        Assert.Throws<ArgumentNullException>(() => PngFilterEncoder.EncodeRow(null!, 1, options));
        Assert.Throws<ArgumentNullException>(() => PngFilterEncoder.EncodeRow(rowData, 1, (PngEncoderOptions)null!));
        Assert.Throws<ArgumentNullException>(() => PngFilterEncoder.EncodeRow(rowData, 1, previousRow, null!));
        Assert.Throws<ArgumentNullException>(() => PngFilterEncoder.EncodeRow(rowData, 0, 2, 1, null!, options));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngFilterEncoder.EncodeRow(rowData, 1, invalidMethod));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngFilterEncoder.EncodeRow(rowData, 1, invalidStrategy));
    }
}
