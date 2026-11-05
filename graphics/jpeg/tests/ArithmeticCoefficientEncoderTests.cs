namespace jpeg.Tests;

internal class ArithmeticCoefficientEncoderTests
{
    [Test]
    public void Encoder_RoundTripsDcFirstBlocksThroughArithmeticDecoder()
    {
        global::jpeg.JpegArithmeticDcCoefficientDecoder tableFactory = new global::jpeg.JpegArithmeticDcCoefficientDecoder(new byte[0]);
        global::jpeg.JpegArithmeticConditioningTable table = tableFactory.CreateDefaultDcConditioningTable(0);
        int[] first = ProgressiveHuffmanDcRefinementTests.CreateBlock(2);
        int[] second = ProgressiveHuffmanDcRefinementTests.CreateBlock(-2);
        global::jpeg.JpegArithmeticCoefficientEncoder encoder = new global::jpeg.JpegArithmeticCoefficientEncoder();

        encoder.EncodeDcFirstBlock(first, table, 0, 0);
        encoder.EncodeDcFirstBlock(second, table, 0, 0);
        encoder.Finish();

        global::jpeg.JpegArithmeticDcCoefficientDecoder decoder = new global::jpeg.JpegArithmeticDcCoefficientDecoder(AppendEndOfImageMarker(encoder.ToArray()));
        int[] decodedFirst = decoder.DecodeDcFirstBlock(table, 0, 0);
        int[] decodedSecond = decoder.DecodeDcFirstBlock(table, 0, 0);

        Assert.That(decodedFirst[0], Is.EqualTo(2));
        Assert.That(decodedSecond[0], Is.EqualTo(-2));
        Assert.That(decoder.DcPredictors[0], Is.EqualTo(-2));
    }

    [Test]
    public void Encoder_RoundTripsDcRefinementBitsThroughArithmeticDecoder()
    {
        int[] positive = ProgressiveHuffmanDcRefinementTests.CreateBlock(10);
        int[] negative = ProgressiveHuffmanDcRefinementTests.CreateBlock(-3);
        global::jpeg.JpegArithmeticCoefficientEncoder encoder = new global::jpeg.JpegArithmeticCoefficientEncoder();

        encoder.EncodeDcRefinementBlock(positive, 1);
        encoder.EncodeDcRefinementBlock(negative, 0);
        encoder.Finish();

        global::jpeg.JpegArithmeticDcCoefficientDecoder decoder = new global::jpeg.JpegArithmeticDcCoefficientDecoder(AppendEndOfImageMarker(encoder.ToArray()));
        int[] decodedPositive = ProgressiveHuffmanDcRefinementTests.CreateBlock(8);
        int[] decodedNegative = ProgressiveHuffmanDcRefinementTests.CreateBlock(-4);
        decoder.DecodeDcRefinementBlock(decodedPositive, 1);
        decoder.DecodeDcRefinementBlock(decodedNegative, 0);

        Assert.That(decodedPositive[0], Is.EqualTo(10));
        Assert.That(decodedNegative[0], Is.EqualTo(-3));
    }

    [Test]
    public void Encoder_RoundTripsAcFirstBlockThroughArithmeticDecoder()
    {
        global::jpeg.JpegArithmeticAcCoefficientDecoder tableFactory = new global::jpeg.JpegArithmeticAcCoefficientDecoder(new byte[0]);
        global::jpeg.JpegArithmeticConditioningTable table = tableFactory.CreateDefaultAcConditioningTable(0);
        int[] coefficients = new int[global::jpeg.JpegConstants.BlockSampleCount];
        coefficients[5] = 2;
        coefficients[8] = -4;
        global::jpeg.JpegArithmeticCoefficientEncoder encoder = new global::jpeg.JpegArithmeticCoefficientEncoder();

        encoder.EncodeAcFirstBlock(coefficients, table, 5, 10, 1);
        encoder.Finish();

        global::jpeg.JpegArithmeticAcCoefficientDecoder decoder = new global::jpeg.JpegArithmeticAcCoefficientDecoder(AppendEndOfImageMarker(encoder.ToArray()));
        int[] decoded = new int[global::jpeg.JpegConstants.BlockSampleCount];
        decoder.DecodeAcFirstBlock(decoded, table, 5, 10, 1);

        Assert.That(decoded[5], Is.EqualTo(2));
        Assert.That(decoded[8], Is.EqualTo(-4));
        Assert.That(decoded[6], Is.EqualTo(0));
    }

    [Test]
    public void Encoder_RoundTripsAcRefinementBlockThroughArithmeticDecoder()
    {
        global::jpeg.JpegArithmeticAcCoefficientDecoder tableFactory = new global::jpeg.JpegArithmeticAcCoefficientDecoder(new byte[0]);
        global::jpeg.JpegArithmeticConditioningTable table = tableFactory.CreateDefaultAcConditioningTable(0);
        int[] target = new int[global::jpeg.JpegConstants.BlockSampleCount];
        target[1] = 3;
        target[2] = 1;
        global::jpeg.JpegArithmeticCoefficientEncoder encoder = new global::jpeg.JpegArithmeticCoefficientEncoder();

        encoder.EncodeAcRefinementBlock(target, table, 1, 5, 0);
        encoder.Finish();

        global::jpeg.JpegArithmeticAcCoefficientDecoder decoder = new global::jpeg.JpegArithmeticAcCoefficientDecoder(AppendEndOfImageMarker(encoder.ToArray()));
        int[] decoded = new int[global::jpeg.JpegConstants.BlockSampleCount];
        decoded[1] = 2;
        decoder.DecodeAcRefinementBlock(decoded, table, 1, 5, 0);

        Assert.That(decoded[1], Is.EqualTo(3));
        Assert.That(decoded[2], Is.EqualTo(1));
    }

    [Test]
    public void EncoderDecoder_ReconstructsBlockAcrossSuccessiveArithmeticScans()
    {
        global::jpeg.JpegArithmeticDcCoefficientDecoder dcFactory = new global::jpeg.JpegArithmeticDcCoefficientDecoder(new byte[0]);
        global::jpeg.JpegArithmeticAcCoefficientDecoder acFactory = new global::jpeg.JpegArithmeticAcCoefficientDecoder(new byte[0]);
        global::jpeg.JpegArithmeticConditioningTable dcTable = dcFactory.CreateDefaultDcConditioningTable(0);
        global::jpeg.JpegArithmeticConditioningTable acTable = acFactory.CreateDefaultAcConditioningTable(0);
        int[] coarse = new int[global::jpeg.JpegConstants.BlockSampleCount];
        coarse[0] = 6;
        coarse[5] = 2;
        int[] target = new int[global::jpeg.JpegConstants.BlockSampleCount];
        target[0] = 7;
        target[5] = 3;
        target[6] = -1;

        int[] decoded = DecodeDcFirst(EncodeDcFirst(coarse, dcTable, 1), dcTable, 1);
        DecodeDcRefinement(EncodeDcRefinement(target, 0), decoded, 0);
        DecodeAcFirst(EncodeAcFirst(coarse, acTable, 5, 7, 1), decoded, acTable, 5, 7, 1);
        DecodeAcRefinement(EncodeAcRefinement(target, acTable, 5, 7, 0), decoded, acTable, 5, 7, 0);

        Assert.That(decoded[0], Is.EqualTo(7));
        Assert.That(decoded[5], Is.EqualTo(3));
        Assert.That(decoded[6], Is.EqualTo(-1));
        Assert.That(decoded[7], Is.EqualTo(0));
    }

    [Test]
    public void EncoderDecoder_RoundTripsInterleavedArithmeticDcPredictorsIndependently()
    {
        global::jpeg.JpegArithmeticDcCoefficientDecoder tableFactory = new global::jpeg.JpegArithmeticDcCoefficientDecoder(new byte[0]);
        global::jpeg.JpegArithmeticConditioningTable table = tableFactory.CreateDefaultDcConditioningTable(0);
        global::jpeg.JpegArithmeticCoefficientEncoder encoder = new global::jpeg.JpegArithmeticCoefficientEncoder();

        encoder.EncodeDcFirstBlock(ProgressiveHuffmanDcRefinementTests.CreateBlock(2), table, 0, 0);
        encoder.EncodeDcFirstBlock(ProgressiveHuffmanDcRefinementTests.CreateBlock(-2), table, 1, 0);
        encoder.EncodeDcFirstBlock(ProgressiveHuffmanDcRefinementTests.CreateBlock(4), table, 0, 0);
        encoder.EncodeDcFirstBlock(ProgressiveHuffmanDcRefinementTests.CreateBlock(-2), table, 1, 0);
        encoder.Finish();

        global::jpeg.JpegArithmeticDcCoefficientDecoder decoder =
            new global::jpeg.JpegArithmeticDcCoefficientDecoder(AppendEndOfImageMarker(encoder.ToArray()));
        int[] firstY = decoder.DecodeDcFirstBlock(table, 0, 0);
        int[] firstCb = decoder.DecodeDcFirstBlock(table, 1, 0);
        int[] secondY = decoder.DecodeDcFirstBlock(table, 0, 0);
        int[] secondCb = decoder.DecodeDcFirstBlock(table, 1, 0);

        Assert.That(firstY[0], Is.EqualTo(2));
        Assert.That(firstCb[0], Is.EqualTo(-2));
        Assert.That(secondY[0], Is.EqualTo(4));
        Assert.That(secondCb[0], Is.EqualTo(-2));
        Assert.That(decoder.DcPredictors[0], Is.EqualTo(4));
        Assert.That(decoder.DcPredictors[1], Is.EqualTo(-2));
    }

    [Test]
    public void Encoder_TracksDcPredictorAndContexts()
    {
        global::jpeg.JpegArithmeticDcCoefficientDecoder tableFactory = new global::jpeg.JpegArithmeticDcCoefficientDecoder(new byte[0]);
        global::jpeg.JpegArithmeticConditioningTable table = tableFactory.CreateDefaultDcConditioningTable(0);
        int[] first = ProgressiveHuffmanDcRefinementTests.CreateBlock(2);
        int[] second = ProgressiveHuffmanDcRefinementTests.CreateBlock(2);
        global::jpeg.JpegArithmeticCoefficientEncoder encoder = new global::jpeg.JpegArithmeticCoefficientEncoder();

        encoder.EncodeDcFirstBlock(first, table, 0, 0);
        encoder.EncodeDcFirstBlock(second, table, 0, 0);

        Assert.That(encoder.DcPredictors[0], Is.EqualTo(2));
        Assert.That(encoder.DcContexts[0], Is.EqualTo(0));
    }

    [Test]
    public void Encoder_RejectsInvalidInputs()
    {
        global::jpeg.JpegArithmeticCoefficientEncoder encoder = new global::jpeg.JpegArithmeticCoefficientEncoder();
        global::jpeg.JpegArithmeticDcCoefficientDecoder dcFactory = new global::jpeg.JpegArithmeticDcCoefficientDecoder(new byte[0]);
        global::jpeg.JpegArithmeticAcCoefficientDecoder acFactory = new global::jpeg.JpegArithmeticAcCoefficientDecoder(new byte[0]);
        global::jpeg.JpegArithmeticConditioningTable dcTable = dcFactory.CreateDefaultDcConditioningTable(0);
        global::jpeg.JpegArithmeticConditioningTable acTable = acFactory.CreateDefaultAcConditioningTable(0);
        int[] coefficients = new int[global::jpeg.JpegConstants.BlockSampleCount];

        Assert.Throws<System.ArgumentNullException>(
            delegate
            {
                global::jpeg.JpegArithmeticCoefficientEncoder.FromStateMachine((global::jpeg.JpegArithmeticEncoderStateMachine)null!);
            });

        Assert.Throws<System.ArgumentNullException>(
            delegate
            {
                encoder.EncodeDcFirstBlock(null!, dcTable, 0, 0);
            });

        Assert.Throws<System.ArgumentOutOfRangeException>(
            delegate
            {
                encoder.EncodeDcFirstBlock(coefficients, dcTable, 256, 0);
            });

        Assert.Throws<global::jpeg.JpegWriteValidationException>(
            delegate
            {
                encoder.EncodeDcFirstBlock(coefficients, acTable, 0, 0);
            });

        Assert.Throws<global::jpeg.JpegWriteValidationException>(
            delegate
            {
                encoder.EncodeAcFirstBlock(coefficients, acTable, 0, 5, 0);
            });

        Assert.Throws<global::jpeg.JpegWriteValidationException>(
            delegate
            {
                encoder.EncodeAcFirstBlock(coefficients, dcTable, 1, 5, 0);
            });

        Assert.Throws<global::jpeg.JpegWriteValidationException>(
            delegate
            {
                encoder.EncodeAcRefinementBlock(new int[1], acTable, 1, 5, 0);
            });
        Assert.Throws<global::jpeg.JpegWriteValidationException>(
            delegate
            {
                encoder.GetMagnitude(int.MinValue);
            });
    }

    public byte[] AppendEndOfImageMarker(byte[] entropy)
    {
        byte[] result = new byte[entropy.Length + 2];
        int index = 0;
        while (index < entropy.Length)
        {
            result[index] = entropy[index];
            index = index + 1;
        }

        result[index] = 0xFF;
        result[index + 1] = 0xD9;
        return result;
    }

    public byte[] EncodeDcFirst(int[] coefficients, global::jpeg.JpegArithmeticConditioningTable table, int successiveLow)
    {
        global::jpeg.JpegArithmeticCoefficientEncoder encoder = new global::jpeg.JpegArithmeticCoefficientEncoder();
        encoder.EncodeDcFirstBlock(coefficients, table, 0, successiveLow);
        encoder.Finish();
        return AppendEndOfImageMarker(encoder.ToArray());
    }

    public int[] DecodeDcFirst(byte[] entropy, global::jpeg.JpegArithmeticConditioningTable table, int successiveLow)
    {
        global::jpeg.JpegArithmeticDcCoefficientDecoder decoder = new global::jpeg.JpegArithmeticDcCoefficientDecoder(entropy);
        return decoder.DecodeDcFirstBlock(table, 0, successiveLow);
    }

    public byte[] EncodeDcRefinement(int[] coefficients, int successiveLow)
    {
        global::jpeg.JpegArithmeticCoefficientEncoder encoder = new global::jpeg.JpegArithmeticCoefficientEncoder();
        encoder.EncodeDcRefinementBlock(coefficients, successiveLow);
        encoder.Finish();
        return AppendEndOfImageMarker(encoder.ToArray());
    }

    public void DecodeDcRefinement(byte[] entropy, int[] coefficients, int successiveLow)
    {
        global::jpeg.JpegArithmeticDcCoefficientDecoder decoder = new global::jpeg.JpegArithmeticDcCoefficientDecoder(entropy);
        decoder.DecodeDcRefinementBlock(coefficients, successiveLow);
    }

    public byte[] EncodeAcFirst(
        int[] coefficients,
        global::jpeg.JpegArithmeticConditioningTable table,
        int spectralStart,
        int spectralEnd,
        int successiveLow)
    {
        global::jpeg.JpegArithmeticCoefficientEncoder encoder = new global::jpeg.JpegArithmeticCoefficientEncoder();
        encoder.EncodeAcFirstBlock(coefficients, table, spectralStart, spectralEnd, successiveLow);
        encoder.Finish();
        return AppendEndOfImageMarker(encoder.ToArray());
    }

    public void DecodeAcFirst(
        byte[] entropy,
        int[] coefficients,
        global::jpeg.JpegArithmeticConditioningTable table,
        int spectralStart,
        int spectralEnd,
        int successiveLow)
    {
        global::jpeg.JpegArithmeticAcCoefficientDecoder decoder = new global::jpeg.JpegArithmeticAcCoefficientDecoder(entropy);
        decoder.DecodeAcFirstBlock(coefficients, table, spectralStart, spectralEnd, successiveLow);
    }

    public byte[] EncodeAcRefinement(
        int[] coefficients,
        global::jpeg.JpegArithmeticConditioningTable table,
        int spectralStart,
        int spectralEnd,
        int successiveLow)
    {
        global::jpeg.JpegArithmeticCoefficientEncoder encoder = new global::jpeg.JpegArithmeticCoefficientEncoder();
        encoder.EncodeAcRefinementBlock(coefficients, table, spectralStart, spectralEnd, successiveLow);
        encoder.Finish();
        return AppendEndOfImageMarker(encoder.ToArray());
    }

    public void DecodeAcRefinement(
        byte[] entropy,
        int[] coefficients,
        global::jpeg.JpegArithmeticConditioningTable table,
        int spectralStart,
        int spectralEnd,
        int successiveLow)
    {
        global::jpeg.JpegArithmeticAcCoefficientDecoder decoder = new global::jpeg.JpegArithmeticAcCoefficientDecoder(entropy);
        decoder.DecodeAcRefinementBlock(coefficients, table, spectralStart, spectralEnd, successiveLow);
    }
}
