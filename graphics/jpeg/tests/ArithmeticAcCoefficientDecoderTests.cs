using System.Collections.Generic;

namespace jpeg.Tests;

internal class ArithmeticAcCoefficientDecoderTests
{
    [Test]
    public void Decoder_DecodesAcFirstPositiveCoefficientInSpectralRange()
    {
        global::jpeg.JpegArithmeticAcCoefficientDecoder decoder = new global::jpeg.JpegArithmeticAcCoefficientDecoder(new byte[0]);
        decoder.SetDecisionBits(new int[] { 0, 1, 0, 0, 1 });
        global::jpeg.JpegArithmeticConditioningTable table = decoder.CreateDefaultAcConditioningTable(0);
        int[] coefficients = new int[global::jpeg.JpegConstants.BlockSampleCount];

        decoder.DecodeAcFirstBlock(coefficients, table, 5, 10, 1);

        Assert.That(coefficients[5], Is.EqualTo(2));
        Assert.That(coefficients[4], Is.EqualTo(0));
        Assert.That(coefficients[6], Is.EqualTo(0));
        Assert.That(decoder.DecisionIndex, Is.EqualTo(5));
    }

    [Test]
    public void Decoder_DecodesAcFirstNegativeMagnitude()
    {
        global::jpeg.JpegArithmeticAcCoefficientDecoder decoder = new global::jpeg.JpegArithmeticAcCoefficientDecoder(new byte[0]);
        decoder.SetDecisionBits(new int[] { 0, 1, 1, 1, 0, 1 });
        global::jpeg.JpegArithmeticConditioningTable table = decoder.CreateDefaultAcConditioningTable(0);
        int[] coefficients = new int[global::jpeg.JpegConstants.BlockSampleCount];

        decoder.DecodeAcFirstBlock(coefficients, table, 1, 5, 0);

        Assert.That(coefficients[1], Is.EqualTo(-2));
        Assert.That(coefficients[2], Is.EqualTo(0));
    }

    [Test]
    public void Decoder_HandlesZeroRunInAcFirstScan()
    {
        global::jpeg.JpegArithmeticAcCoefficientDecoder decoder = new global::jpeg.JpegArithmeticAcCoefficientDecoder(new byte[0]);
        decoder.SetDecisionBits(CreateAcFirstZeroRunDecisions(17));
        global::jpeg.JpegArithmeticConditioningTable table = decoder.CreateDefaultAcConditioningTable(0);
        int[] coefficients = new int[global::jpeg.JpegConstants.BlockSampleCount];

        decoder.DecodeAcFirstBlock(coefficients, table, 1, 20, 0);

        Assert.That(coefficients[18], Is.EqualTo(1));
        Assert.That(coefficients[17], Is.EqualTo(0));
        Assert.That(coefficients[19], Is.EqualTo(0));
    }

    [Test]
    public void Decoder_StopsAcFirstAtEobFlag()
    {
        global::jpeg.JpegArithmeticAcCoefficientDecoder decoder = new global::jpeg.JpegArithmeticAcCoefficientDecoder(new byte[0]);
        decoder.SetDecisionBits(new int[] { 1 });
        global::jpeg.JpegArithmeticConditioningTable table = decoder.CreateDefaultAcConditioningTable(0);
        int[] coefficients = new int[global::jpeg.JpegConstants.BlockSampleCount];

        decoder.DecodeAcFirstBlock(coefficients, table, 1, 63, 0);

        Assert.That(coefficients[1], Is.EqualTo(0));
        Assert.That(decoder.DecisionIndex, Is.EqualTo(1));
    }

    [Test]
    public void Decoder_DecodesAcRefinementExistingAndNewCoefficients()
    {
        global::jpeg.JpegArithmeticAcCoefficientDecoder decoder = new global::jpeg.JpegArithmeticAcCoefficientDecoder(new byte[0]);
        decoder.SetDecisionBits(new int[] { 1, 0, 1, 0, 1 });
        global::jpeg.JpegArithmeticConditioningTable table = decoder.CreateDefaultAcConditioningTable(0);
        int[] coefficients = new int[global::jpeg.JpegConstants.BlockSampleCount];
        coefficients[1] = 2;

        decoder.DecodeAcRefinementBlock(coefficients, table, 1, 5, 0);

        Assert.That(coefficients[1], Is.EqualTo(3));
        Assert.That(coefficients[2], Is.EqualTo(1));
        Assert.That(coefficients[3], Is.EqualTo(0));
    }

    [Test]
    public void Decoder_DecodesAcRefinementNegativeExistingAndNewCoefficients()
    {
        global::jpeg.JpegArithmeticAcCoefficientDecoder decoder = new global::jpeg.JpegArithmeticAcCoefficientDecoder(new byte[0]);
        decoder.SetDecisionBits(new int[] { 1, 0, 1, 1, 1 });
        global::jpeg.JpegArithmeticConditioningTable table = decoder.CreateDefaultAcConditioningTable(0);
        int[] coefficients = new int[global::jpeg.JpegConstants.BlockSampleCount];
        coefficients[1] = -2;

        decoder.DecodeAcRefinementBlock(coefficients, table, 1, 5, 0);

        Assert.That(coefficients[1], Is.EqualTo(-3));
        Assert.That(coefficients[2], Is.EqualTo(-1));
    }

    [Test]
    public void Decoder_DecodesAcBlocksFromScanHeader()
    {
        global::jpeg.JpegArithmeticAcCoefficientDecoder decoder = new global::jpeg.JpegArithmeticAcCoefficientDecoder(new byte[0]);
        decoder.SetDecisionBits(new int[] { 0, 1, 0, 0, 1 });
        global::jpeg.JpegArithmeticConditioningTable table = decoder.CreateDefaultAcConditioningTable(0);
        global::jpeg.JpegScanHeader scan = ProgressiveHuffmanAcFirstTests.CreateAcFirstScan(2, 6, 1);
        int[] coefficients = new int[global::jpeg.JpegConstants.BlockSampleCount];

        decoder.DecodeAcFirstBlockForScan(scan, table, coefficients);

        Assert.That(coefficients[2], Is.EqualTo(2));
    }

    [Test]
    public void Decoder_FindsConditioningTableOrUsesDefaultAndResetsState()
    {
        global::jpeg.JpegArithmeticAcCoefficientDecoder decoder = new global::jpeg.JpegArithmeticAcCoefficientDecoder(new byte[0]);
        List<global::jpeg.JpegArithmeticConditioningTable> tables = new List<global::jpeg.JpegArithmeticConditioningTable>();
        tables.Add(ArithmeticConditioningTableWriterTests.CreateAcTable(2, 9));
        decoder.AcStatistics[0][1] = 5;
        decoder.FixedStatistics[0] = 1;

        global::jpeg.JpegArithmeticConditioningTable found = decoder.FindOrCreateDefaultAcTable(tables, 2);
        global::jpeg.JpegArithmeticConditioningTable created = decoder.FindOrCreateDefaultAcTable(tables, 1);
        decoder.ResetAcState();

        Assert.That(found.Kx, Is.EqualTo(9));
        Assert.That(created.Kx, Is.EqualTo(5));
        Assert.That(created.ConditioningValue, Is.EqualTo(5));
        Assert.That(decoder.AcStatistics[0][1], Is.EqualTo(0));
        Assert.That(decoder.FixedStatistics[0], Is.EqualTo(113));
    }

    [Test]
    public void Decoder_RejectsInvalidAcInputs()
    {
        global::jpeg.JpegArithmeticAcCoefficientDecoder decoder = new global::jpeg.JpegArithmeticAcCoefficientDecoder(new byte[0]);
        global::jpeg.JpegArithmeticConditioningTable table = decoder.CreateDefaultAcConditioningTable(0);
        int[] coefficients = new int[global::jpeg.JpegConstants.BlockSampleCount];

        Assert.Throws<System.ArgumentNullException>(
            delegate
            {
                global::jpeg.JpegArithmeticAcCoefficientDecoder.FromStateMachine((global::jpeg.JpegArithmeticDecoderStateMachine)null!);
            });

        Assert.Throws<System.ArgumentNullException>(
            delegate
            {
                decoder.DecodeAcFirstBlock(null!, table, 1, 5, 0);
            });

        Assert.Throws<global::jpeg.JpegInvalidDataException>(
            delegate
            {
                decoder.DecodeAcFirstBlock(new int[1], table, 1, 5, 0);
            });

        Assert.Throws<global::jpeg.JpegInvalidDataException>(
            delegate
            {
                decoder.DecodeAcFirstBlock(coefficients, table, 0, 5, 0);
            });

        Assert.Throws<global::jpeg.JpegInvalidDataException>(
            delegate
            {
                decoder.DecodeAcFirstBlock(coefficients, ArithmeticConditioningTableWriterTests.CreateDcTable(0, 0, 1), 1, 5, 0);
            });

        global::jpeg.JpegArithmeticConditioningTable badKx = decoder.CreateDefaultAcConditioningTable(0);
        badKx.Kx = 64;
        Assert.Throws<global::jpeg.JpegInvalidDataException>(
            delegate
            {
                decoder.DecodeAcFirstBlock(coefficients, badKx, 1, 5, 0);
            });

        Assert.Throws<System.ArgumentNullException>(
            delegate
            {
                decoder.SetDecisionBits(null!);
            });

        decoder.SetDecisionBits(new int[] { 2 });
        Assert.Throws<global::jpeg.JpegInvalidDataException>(
            delegate
            {
                decoder.DecodeAcFirstBlock(coefficients, table, 1, 5, 0);
            });
    }

    public static int[] CreateAcFirstZeroRunDecisions(int zeroRun)
    {
        int[] decisions = new int[zeroRun + 5];
        int index = 0;
        decisions[index] = 0;
        index = index + 1;
        while (index <= zeroRun)
        {
            decisions[index] = 0;
            index = index + 1;
        }

        decisions[index] = 1;
        index = index + 1;
        decisions[index] = 0;
        index = index + 1;
        decisions[index] = 0;
        index = index + 1;
        decisions[index] = 1;
        return decisions;
    }
}
