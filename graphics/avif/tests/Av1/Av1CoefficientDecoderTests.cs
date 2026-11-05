namespace avif.Tests;

public class Av1CoefficientDecoderTests
{
    [Test]
    public void QuantizerTablesMatchNormativeEndpointsAndClampDeltas()
    {
        Assert.Multiple(() =>
        {
            Assert.That(Av1QuantizationTables.GetDcQuantizer(0, 0, 8), Is.EqualTo(4));
            Assert.That(Av1QuantizationTables.GetDcQuantizer(255, 0, 8), Is.EqualTo(1336));
            Assert.That(Av1QuantizationTables.GetDcQuantizer(255, 0, 10), Is.EqualTo(5347));
            Assert.That(Av1QuantizationTables.GetDcQuantizer(255, 0, 12), Is.EqualTo(21387));
            Assert.That(Av1QuantizationTables.GetAcQuantizer(255, 0, 8), Is.EqualTo(1828));
            Assert.That(Av1QuantizationTables.GetAcQuantizer(255, 0, 10), Is.EqualTo(7312));
            Assert.That(Av1QuantizationTables.GetAcQuantizer(255, 0, 12), Is.EqualTo(29247));
            Assert.That(Av1QuantizationTables.GetDcQuantizer(10, -20, 8), Is.EqualTo(4));
            Assert.That(Av1QuantizationTables.GetAcQuantizer(250, 20, 8), Is.EqualTo(1828));
            Assert.That(Av1QuantizationTables.GetAcQuantizer(-1, 0, 8), Is.Zero);
        });
    }

    [Test]
    public void TransformBlockSkipReturnsAnEmptyCoefficientBlock()
    {
        Av1CoefficientDecodeRequest request = CreateRequest(
            new int[] { 1 }, System.Array.Empty<int>());

        Av1CoefficientDecodeResult result = Av1CoefficientDecoder.Decode(request);

        Assert.Multiple(() =>
        {
            Assert.That(result.Code, Is.EqualTo(AvifResultCode.Success));
            Assert.That(result.Skipped, Is.True);
            Assert.That(result.EndOfBlock, Is.Zero);
            Assert.That(result.QuantizedCoefficients, Has.Length.EqualTo(16));
            Assert.That(result.QuantizedCoefficients, Is.All.Zero);
            Assert.That(result.DequantizedCoefficients, Is.All.Zero);
        });
    }

    [Test]
    public void SingleDcCoefficientDecodesSignContextAndDequantizes()
    {
        Av1CoefficientDecodeRequest positive = CreateRequest(
            new int[] { 0,0,0,0 }, System.Array.Empty<int>());
        positive.QIndex = 100;
        Av1CoefficientDecodeRequest negative = CreateRequest(
            new int[] { 0,0,0,1 }, System.Array.Empty<int>());
        negative.QIndex = 100;

        Av1CoefficientDecodeResult positiveResult =
            Av1CoefficientDecoder.Decode(positive);
        Av1CoefficientDecodeResult negativeResult =
            Av1CoefficientDecoder.Decode(negative);
        int quantizer = Av1QuantizationTables.GetDcQuantizer(100, 0, 8);

        Assert.Multiple(() =>
        {
            Assert.That(positiveResult.Code, Is.EqualTo(AvifResultCode.Success));
            Assert.That(positiveResult.EndOfBlock, Is.EqualTo(1));
            Assert.That(positiveResult.QuantizedCoefficients[0], Is.EqualTo(1));
            Assert.That(positiveResult.DequantizedCoefficients[0],
                Is.EqualTo(quantizer));
            Assert.That(positiveResult.CulLevel, Is.EqualTo(129));
            Assert.That(negativeResult.QuantizedCoefficients[0], Is.EqualTo(-1));
            Assert.That(negativeResult.DequantizedCoefficients[0],
                Is.EqualTo(-quantizer));
            Assert.That(negativeResult.CulLevel, Is.EqualTo(65));
        });
    }

    [TestCase(0, 4, 1)]
    [TestCase(10, 4, 8)]
    [TestCase(11, 1, 2)]
    public void TransformClassesPlaceCoefficientsInTheirScanOrder(
        int transformType,
        int secondPosition,
        int thirdPosition)
    {
        Av1CoefficientDecodeRequest request = CreateRequest(
            new int[] { 0,2,0,0,1,2,0 },
            new int[] { 1,0 });
        request.TransformType = (Av1TransformType)transformType;

        Av1CoefficientDecodeResult result = Av1CoefficientDecoder.Decode(request);

        Assert.Multiple(() =>
        {
            Assert.That(result.Code, Is.EqualTo(AvifResultCode.Success));
            Assert.That(result.EndOfBlock, Is.EqualTo(3));
            Assert.That(result.QuantizedCoefficients[0], Is.EqualTo(2));
            Assert.That(result.QuantizedCoefficients[secondPosition], Is.EqualTo(-1));
            Assert.That(result.QuantizedCoefficients[thirdPosition], Is.EqualTo(1));
            Assert.That(NonzeroCount(result.QuantizedCoefficients), Is.EqualTo(3));
        });
    }

    [Test]
    public void EndOfBlockExtraBitsSelectLongerCoefficientRuns()
    {
        int[] symbols = new int[]
        {
            0,3,0,
            0,
            0,0,0,0,0,
            0
        };
        int[] bits = new int[] { 1, 0,0,0,0,0 };
        Av1CoefficientDecodeRequest request = CreateRequest(symbols, bits);

        Av1CoefficientDecodeResult result = Av1CoefficientDecoder.Decode(request);

        Assert.Multiple(() =>
        {
            Assert.That(result.Code, Is.EqualTo(AvifResultCode.Success));
            Assert.That(result.EndOfBlock, Is.EqualTo(6));
            Assert.That(NonzeroCount(result.QuantizedCoefficients), Is.EqualTo(1));
        });
    }

    [Test]
    public void BaseRangeAndGolombExtensionDecodeLargeLevels()
    {
        Av1CoefficientDecodeRequest request = CreateRequest(
            new int[] { 0,0,2,3,3,3,3,0 },
            new int[] { 0,0,1,1,0 });
        request.Lossless = true;

        Av1CoefficientDecodeResult result = Av1CoefficientDecoder.Decode(request);

        Assert.Multiple(() =>
        {
            Assert.That(result.Code, Is.EqualTo(AvifResultCode.Success));
            Assert.That(result.QuantizedCoefficients[0], Is.EqualTo(20));
            Assert.That(result.DequantizedCoefficients[0], Is.EqualTo(80));
            Assert.That(result.CulLevel, Is.EqualTo(148));
        });
    }

    [Test]
    public void QuantizerMatricesWeightAcCoefficients()
    {
        Av1CoefficientDecodeRequest flat = CreateRequest(
            new int[] { 0,1,0,0 }, new int[] { 0 });
        Av1CoefficientDecodeRequest matrix = CreateRequest(
            new int[] { 0,1,0,0 }, new int[] { 0 });
        matrix.UseQuantizerMatrix = true;
        matrix.MatrixLevel = 0;

        Av1CoefficientDecodeResult flatResult = Av1CoefficientDecoder.Decode(flat);
        Av1CoefficientDecodeResult matrixResult = Av1CoefficientDecoder.Decode(matrix);
        int position = Av1ScanTables.GetScan(
            Av1TransformSize.Transform4x4, Av1TransformType.DctDct)[1];

        Assert.Multiple(() =>
        {
            Assert.That(flatResult.Code, Is.EqualTo(AvifResultCode.Success));
            Assert.That(matrixResult.Code, Is.EqualTo(AvifResultCode.Success));
            Assert.That(flatResult.DequantizedCoefficients[position], Is.EqualTo(4));
            Assert.That(matrixResult.DequantizedCoefficients[position], Is.EqualTo(5));
        });
    }

    [TestCase(8)]
    [TestCase(10)]
    [TestCase(12)]
    public void EveryBitDepthUsesItsOwnDequantizer(int bitDepth)
    {
        Av1CoefficientDecodeRequest request = CreateRequest(
            new int[] { 0,0,0,0 }, System.Array.Empty<int>());
        request.BitDepth = bitDepth;
        request.QIndex = 255;

        Av1CoefficientDecodeResult result = Av1CoefficientDecoder.Decode(request);

        Assert.Multiple(() =>
        {
            Assert.That(result.Code, Is.EqualTo(AvifResultCode.Success));
            Assert.That(result.DequantizedCoefficients[0], Is.EqualTo(
                Av1QuantizationTables.GetDcQuantizer(255, 0, bitDepth)));
        });
    }

    [Test]
    public void EveryTransformSizeAllocatesItsAdjustedCoefficientDomain()
    {
        int size = 0;
        while (size < Av1SyntaxConstants.TransformSizeCount)
        {
            Av1TransformSize transformSize = (Av1TransformSize)size;
            Av1CoefficientDecodeRequest request = CreateRequest(
                new int[] { 1 }, System.Array.Empty<int>());
            request.TransformSize = transformSize;

            Av1CoefficientDecodeResult result = Av1CoefficientDecoder.Decode(request);

            Assert.Multiple(() =>
            {
                Assert.That(result.Code, Is.EqualTo(AvifResultCode.Success));
                Assert.That(result.QuantizedCoefficients, Has.Length.EqualTo(
                    Av1ConversionTables.GetTransformCoefficientCount(transformSize)));
            });
            size++;
        }
    }

    [Test]
    public void EveryTransformSizeAndTypeUsesItsDeclaredScanOrder()
    {
        int size = 0;
        while (size < Av1SyntaxConstants.TransformSizeCount)
        {
            int type = 0;
            while (type < Av1SyntaxConstants.TransformTypeCount)
            {
                Av1TransformSize transformSize = (Av1TransformSize)size;
                Av1TransformType transformType = (Av1TransformType)type;
                Av1CoefficientDecodeRequest request = CreateRequest(
                    new int[] { 0,2,0,0,1,1,0 },
                    new int[] { 0,0 });
                request.TransformSize = transformSize;
                request.TransformType = transformType;
                int[] scan = Av1ScanTables.GetScan(transformSize, transformType);

                Av1CoefficientDecodeResult result =
                    Av1CoefficientDecoder.Decode(request);

                Assert.Multiple(() =>
                {
                    Assert.That(result.Code, Is.EqualTo(AvifResultCode.Success));
                    Assert.That(result.EndOfBlock, Is.EqualTo(3));
                    Assert.That(result.QuantizedCoefficients[scan[0]], Is.EqualTo(1));
                    Assert.That(result.QuantizedCoefficients[scan[1]], Is.EqualTo(1));
                    Assert.That(result.QuantizedCoefficients[scan[2]], Is.EqualTo(1));
                    Assert.That(NonzeroCount(result.QuantizedCoefficients), Is.EqualTo(3));
                });
                type++;
            }
            size++;
        }
    }

    [TestCase(2, 4)]
    [TestCase(3, 2)]
    [TestCase(4, 1)]
    public void LargeTransformsApplyNormativeCoefficientScaling(
        int transformSize,
        int expected)
    {
        Av1CoefficientDecodeRequest request = CreateRequest(
            new int[] { 0,0,0,0 }, System.Array.Empty<int>());
        request.TransformSize = (Av1TransformSize)transformSize;

        Av1CoefficientDecodeResult result = Av1CoefficientDecoder.Decode(request);

        Assert.Multiple(() =>
        {
            Assert.That(result.Code, Is.EqualTo(AvifResultCode.Success));
            Assert.That(result.DequantizedCoefficients[0], Is.EqualTo(expected));
        });
    }

    [Test]
    public void CdfBackedReaderRoutesSymbolsThroughTheTileContext()
    {
        Av1FrameCdfContext? frame = Av1FrameCdfContext.CreateDefault(0);
        Av1TileCdfContext firstContext = (frame ?? throw new InvalidOperationException("Invalid internal state.")).CreateTileContext(1, 0);
        Av1TileCdfContext secondContext = frame.CreateTileContext(1, 1);
        byte[] payload = new byte[] { 0x70,0x00 };
        Av1SymbolDecoder firstDecoder = new Av1SymbolDecoder(payload, 0, 2, false);
        Av1SymbolDecoder secondDecoder = new Av1SymbolDecoder(payload, 0, 2, false);
        Av1CoefficientSymbolReader reader = Av1CoefficientSymbolReader.Create(
            firstDecoder, firstContext);

        int actual = reader.ReadSymbol("Default_Txb_Skip_Cdf", 0);
        int expected = secondDecoder.ReadCdfSymbol(
            secondContext.GetCdf("Default_Txb_Skip_Cdf", 0));

        Assert.Multiple(() =>
        {
            Assert.That(reader.Code, Is.EqualTo(AvifResultCode.Success));
            Assert.That(actual, Is.EqualTo(expected));
            Assert.That(firstDecoder.BitPosition, Is.EqualTo(secondDecoder.BitPosition));
        });
    }

    [Test]
    public void CdfBackedCoefficientDecodeHandlesARealSkipSymbol()
    {
        Av1FrameCdfContext? frame = Av1FrameCdfContext.CreateDefault(0);
        Av1TileCdfContext context = (frame ?? throw new InvalidOperationException("Invalid internal state.")).CreateTileContext(1, 0);
        Av1SymbolDecoder decoder = new Av1SymbolDecoder(
            new byte[] { 0xff,0xfe }, 0, 2, false);
        Av1CoefficientDecodeRequest request = new Av1CoefficientDecodeRequest();
        request.Reader = Av1CoefficientSymbolReader.Create(decoder, context);

        Av1CoefficientDecodeResult result = Av1CoefficientDecoder.Decode(request);

        Assert.Multiple(() =>
        {
            Assert.That(result.Code, Is.EqualTo(AvifResultCode.Success));
            Assert.That(result.Skipped, Is.True);
            Assert.That(result.QuantizedCoefficients, Is.All.Zero);
        });
    }

    [Test]
    public void MalformedTokensAndInvalidLosslessConfigurationAreRejected()
    {
        Av1CoefficientDecodeRequest invalidSymbol = CreateRequest(
            new int[] { 0,5 }, System.Array.Empty<int>());
        Av1CoefficientDecodeRequest exhausted = CreateRequest(
            new int[] { 0,0 }, System.Array.Empty<int>());
        Av1CoefficientDecodeRequest invalidLossless = CreateRequest(
            new int[] { 1 }, System.Array.Empty<int>());
        invalidLossless.Lossless = true;
        invalidLossless.QIndex = 1;

        Av1CoefficientDecodeResult symbolResult =
            Av1CoefficientDecoder.Decode(invalidSymbol);
        Av1CoefficientDecodeResult exhaustedResult =
            Av1CoefficientDecoder.Decode(exhausted);
        Av1CoefficientDecodeResult losslessResult =
            Av1CoefficientDecoder.Decode(invalidLossless);

        Assert.Multiple(() =>
        {
            Assert.That(symbolResult.Code,
                Is.EqualTo(AvifResultCode.InvalidAv1Bitstream));
            Assert.That(exhaustedResult.Code,
                Is.EqualTo(AvifResultCode.InvalidAv1Bitstream));
            Assert.That(losslessResult.Code,
                Is.EqualTo(AvifResultCode.InvalidArgument));
            Assert.That(symbolResult.QuantizedCoefficients, Is.All.Zero);
            Assert.That(exhaustedResult.QuantizedCoefficients, Is.All.Zero);
        });
    }

    static Av1CoefficientDecodeRequest CreateRequest(int[] symbols, int[] bits)
    {
        Av1CoefficientDecodeRequest request = new Av1CoefficientDecodeRequest();
        request.Reader = Av1CoefficientSymbolReader.Create(symbols, bits);
        return request;
    }

    static int NonzeroCount(int[] values)
    {
        int count = 0;
        int index = 0;
        while (index < values.Length)
        {
            if (values[index] != 0)
            {
                count++;
            }
            index++;
        }
        return count;
    }
}
