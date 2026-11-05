using System.Collections.Generic;

namespace jpeg.Tests;

internal class ArithmeticDcCoefficientDecoderTests
{
    [Test]
    public void Decoder_DecodesZeroDcDifferenceFromArithmeticStateMachine()
    {
        global::jpeg.JpegArithmeticDcCoefficientDecoder decoder = new global::jpeg.JpegArithmeticDcCoefficientDecoder(new byte[] { 0, 0 });
        global::jpeg.JpegArithmeticConditioningTable table = decoder.CreateDefaultDcConditioningTable(0);
        decoder.DcPredictors[0] = 5;

        int[] coefficients = decoder.DecodeDcFirstBlock(table, 0, 2);

        Assert.That(coefficients[0], Is.EqualTo(20));
        Assert.That(decoder.DcPredictors[0], Is.EqualTo(5));
        Assert.That(decoder.DcContexts[0], Is.EqualTo(0));
    }

    [Test]
    public void Decoder_DecodesPositiveDcDifferenceAndUpdatesSmallContext()
    {
        global::jpeg.JpegArithmeticDcCoefficientDecoder decoder = new global::jpeg.JpegArithmeticDcCoefficientDecoder(new byte[0]);
        decoder.SetDecisionBits(new int[] { 1, 0, 1, 0 });
        global::jpeg.JpegArithmeticConditioningTable table = decoder.CreateDefaultDcConditioningTable(0);

        int[] coefficients = decoder.DecodeDcFirstBlock(table, 0, 1);

        Assert.That(coefficients[0], Is.EqualTo(4));
        Assert.That(decoder.DcPredictors[0], Is.EqualTo(2));
        Assert.That(decoder.DcContexts[0], Is.EqualTo(4));
        Assert.That(decoder.DecisionIndex, Is.EqualTo(4));
    }

    [Test]
    public void Decoder_DecodesNegativeDcDifferenceAndUpdatesLargeContext()
    {
        global::jpeg.JpegArithmeticDcCoefficientDecoder decoder = new global::jpeg.JpegArithmeticDcCoefficientDecoder(new byte[0]);
        decoder.SetDecisionBits(new int[] { 1, 1, 1, 1, 0, 1 });
        global::jpeg.JpegArithmeticConditioningTable table = decoder.CreateDefaultDcConditioningTable(0);
        decoder.DcPredictors[2] = 9;

        int[] coefficients = decoder.DecodeDcFirstBlock(table, 2, 0);

        Assert.That(coefficients[0], Is.EqualTo(5));
        Assert.That(decoder.DcPredictors[2], Is.EqualTo(5));
        Assert.That(decoder.DcContexts[2], Is.EqualTo(16));
    }

    [Test]
    public void Decoder_AppliesDcPredictorAcrossComponentsIndependently()
    {
        global::jpeg.JpegArithmeticDcCoefficientDecoder decoder = new global::jpeg.JpegArithmeticDcCoefficientDecoder(new byte[0]);
        decoder.SetDecisionBits(new int[] { 1, 0, 1, 0, 0, 0 });
        global::jpeg.JpegArithmeticConditioningTable table = decoder.CreateDefaultDcConditioningTable(0);

        int[] first = decoder.DecodeDcFirstBlock(table, 0, 0);
        int[] second = decoder.DecodeDcFirstBlock(table, 0, 0);
        int[] third = decoder.DecodeDcFirstBlock(table, 1, 0);

        Assert.That(first[0], Is.EqualTo(2));
        Assert.That(second[0], Is.EqualTo(2));
        Assert.That(third[0], Is.EqualTo(0));
        Assert.That(decoder.DcPredictors[0], Is.EqualTo(2));
        Assert.That(decoder.DcPredictors[1], Is.EqualTo(0));
    }

    [Test]
    public void Decoder_DecodesDcRefinementWithFixedProbabilityState()
    {
        global::jpeg.JpegArithmeticDcCoefficientDecoder decoder = new global::jpeg.JpegArithmeticDcCoefficientDecoder(new byte[0]);
        decoder.SetDecisionBits(new int[] { 1, 0 });
        int[] positive = ProgressiveHuffmanDcRefinementTests.CreateBlock(8);
        int[] unchanged = ProgressiveHuffmanDcRefinementTests.CreateBlock(-4);

        decoder.DecodeDcRefinementBlock(positive, 1);
        decoder.DecodeDcRefinementBlock(unchanged, 0);

        Assert.That(positive[0], Is.EqualTo(10));
        Assert.That(unchanged[0], Is.EqualTo(-4));
        Assert.That(decoder.FixedStatistics[0], Is.EqualTo(113));
    }

    [Test]
    public void Decoder_FindsConditioningTableForScanComponentOrUsesDefault()
    {
        global::jpeg.JpegArithmeticDcCoefficientDecoder decoder = new global::jpeg.JpegArithmeticDcCoefficientDecoder(new byte[0]);
        decoder.SetDecisionBits(new int[] { 0, 0 });
        List<global::jpeg.JpegArithmeticConditioningTable> tables = new List<global::jpeg.JpegArithmeticConditioningTable>();
        tables.Add(ArithmeticConditioningTableWriterTests.CreateDcTable(1, 2, 5));
        global::jpeg.JpegScanHeader scan = ProgressiveHuffmanDcFirstTests.CreateDcFirstScan(0);
        global::jpeg.JpegScanComponent component = ScanHeaderWriterTests.CreateComponent(1, 1, 0);

        int[] fromTable = decoder.DecodeDcFirstBlockForScanComponent(tables, scan, component, 0);
        component.DcEntropyTableIdentifier = 2;
        int[] fromDefault = decoder.DecodeDcFirstBlockForScanComponent(tables, scan, component, 1);

        Assert.That(fromTable[0], Is.EqualTo(0));
        Assert.That(fromDefault[0], Is.EqualTo(0));
        Assert.That(decoder.FindOrCreateDefaultDcTable(tables, 2).ConditioningValue, Is.EqualTo(0x10));
    }

    [Test]
    public void Decoder_ResetDcStateClearsPredictorsContextsAndStatistics()
    {
        global::jpeg.JpegArithmeticDcCoefficientDecoder decoder = new global::jpeg.JpegArithmeticDcCoefficientDecoder(new byte[0]);
        decoder.DcPredictors[0] = 5;
        decoder.DcContexts[0] = 12;
        decoder.DcStatistics[0][0] = 7;
        decoder.FixedStatistics[0] = 1;

        decoder.ResetDcState();

        Assert.That(decoder.DcPredictors[0], Is.EqualTo(0));
        Assert.That(decoder.DcContexts[0], Is.EqualTo(0));
        Assert.That(decoder.DcStatistics[0][0], Is.EqualTo(0));
        Assert.That(decoder.FixedStatistics[0], Is.EqualTo(113));
    }

    [Test]
    public void Decoder_RejectsInvalidDcInputs()
    {
        global::jpeg.JpegArithmeticDcCoefficientDecoder decoder = new global::jpeg.JpegArithmeticDcCoefficientDecoder(new byte[0]);
        global::jpeg.JpegArithmeticConditioningTable table = decoder.CreateDefaultDcConditioningTable(0);

        Assert.Throws<System.ArgumentNullException>(
            delegate
            {
                global::jpeg.JpegArithmeticDcCoefficientDecoder.FromStateMachine((global::jpeg.JpegArithmeticDecoderStateMachine)null!);
            });

        Assert.Throws<System.ArgumentNullException>(
            delegate
            {
                decoder.DecodeDcFirstBlock(null!, 0, 0);
            });

        Assert.Throws<System.ArgumentOutOfRangeException>(
            delegate
            {
                decoder.DecodeDcFirstBlock(table, 256, 0);
            });

        Assert.Throws<global::jpeg.JpegInvalidDataException>(
            delegate
            {
                decoder.DecodeDcFirstBlock(ArithmeticConditioningTableWriterTests.CreateAcTable(0, 5), 0, 0);
            });

        Assert.Throws<global::jpeg.JpegInvalidDataException>(
            delegate
            {
                decoder.DecodeDcFirstBlock(table, 0, 16);
            });

        Assert.Throws<System.ArgumentNullException>(
            delegate
            {
                decoder.DecodeDcRefinementBlock(null!, 0);
            });

        Assert.Throws<global::jpeg.JpegInvalidDataException>(
            delegate
            {
                decoder.DecodeDcRefinementBlock(new int[1], 0);
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
                decoder.DecodeDcFirstBlock(table, 0, 0);
            });
    }
}
