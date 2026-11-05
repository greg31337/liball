using System.Collections.Generic;
using NUnit.Framework;

namespace png.Tests;

[TestFixture]
public class FilterPathCoverageTests
{
    [TestCaseSource(nameof(CreateFilterTypes))]
    public void EncodeFixed_RoundTripsEveryFilterWithoutPreviousRow(PngFilterType filterType)
    {
        byte[] rowData = new byte[] { 2, 1, 8, 13, 21, 34 };

        byte[] filteredRow = PngFilterEncoder.EncodeFixed(rowData, 2, filterType);
        byte[] decodedRow = DecodeFixed(filteredRow, 2, null);

        Assert.That(filteredRow[0], Is.EqualTo((byte)filterType));
        Assert.That(decodedRow, Is.EqualTo(rowData));
    }

    [TestCaseSource(nameof(CreateFilterTypes))]
    public void EncodeFixed_RoundTripsEveryFilterWithPreviousRowAndOffset(PngFilterType filterType)
    {
        byte[] sourceRow = new byte[] { 99, 2, 1, 8, 13, 21, 34, 88 };
        byte[] rowData = new byte[] { 2, 1, 8, 13, 21, 34 };
        byte[] previousRow = new byte[] { 1, 0, 5, 10, 20, 30 };

        byte[] filteredRow = PngFilterEncoder.EncodeFixed(sourceRow, 1, 6, 2, previousRow, filterType);
        byte[] decodedRow = DecodeFixed(filteredRow, 2, previousRow);

        Assert.That(filteredRow[0], Is.EqualTo((byte)filterType));
        Assert.That(decodedRow, Is.EqualTo(rowData));
    }

    [Test]
    public void EncodeAdaptive_CanChooseAverageFilter()
    {
        byte[] rowData = new byte[] { 0, 1 };
        byte[] previousRow = new byte[] { 0, 2 };

        byte[] filteredRow = PngFilterEncoder.EncodeAdaptive(rowData, 1, previousRow);
        byte[] decodedRow = PngFilterDecoder.DecodeAverage(filteredRow, 1, previousRow);

        Assert.That(filteredRow, Is.EqualTo(new byte[] { PngConstants.FilterTypeAverage, 0, 0 }));
        Assert.That(decodedRow, Is.EqualTo(rowData));
    }

    [Test]
    public void EncodeAdaptive_CanChoosePaethFilter()
    {
        byte[] rowData = new byte[] { 2, 1 };
        byte[] previousRow = new byte[] { 1, 0 };

        byte[] filteredRow = PngFilterEncoder.EncodeAdaptive(rowData, 1, previousRow);
        byte[] decodedRow = PngFilterDecoder.DecodePaeth(filteredRow, 1, previousRow);

        Assert.That(filteredRow, Is.EqualTo(new byte[] { PngConstants.FilterTypePaeth, 1, 0 }));
        Assert.That(decodedRow, Is.EqualTo(rowData));
    }

    static IEnumerable<TestCaseData> CreateFilterTypes()
    {
        yield return CreateFilterTypeCase(PngFilterType.None);
        yield return CreateFilterTypeCase(PngFilterType.Sub);
        yield return CreateFilterTypeCase(PngFilterType.Up);
        yield return CreateFilterTypeCase(PngFilterType.Average);
        yield return CreateFilterTypeCase(PngFilterType.Paeth);
    }

    static TestCaseData CreateFilterTypeCase(PngFilterType filterType)
    {
        return new TestCaseData(filterType).SetName(filterType.ToString());
    }

    static byte[] DecodeFixed(byte[] filteredRow, int bytesPerPixel, byte[]? previousRow)
    {
        if (filteredRow[0] == PngConstants.FilterTypeNone)
        {
            return PngFilterDecoder.DecodeNone(filteredRow);
        }

        if (filteredRow[0] == PngConstants.FilterTypeSub)
        {
            return PngFilterDecoder.DecodeSub(filteredRow, bytesPerPixel);
        }

        if (filteredRow[0] == PngConstants.FilterTypeUp)
        {
            if (previousRow == null)
            {
                return PngFilterDecoder.DecodeUp(filteredRow);
            }

            return PngFilterDecoder.DecodeUp(filteredRow, previousRow);
        }

        if (filteredRow[0] == PngConstants.FilterTypeAverage)
        {
            if (previousRow == null)
            {
                return PngFilterDecoder.DecodeAverage(filteredRow, bytesPerPixel);
            }

            return PngFilterDecoder.DecodeAverage(filteredRow, bytesPerPixel, previousRow);
        }

        if (previousRow == null)
        {
            return PngFilterDecoder.DecodePaeth(filteredRow, bytesPerPixel);
        }

        return PngFilterDecoder.DecodePaeth(filteredRow, bytesPerPixel, previousRow);
    }
}
