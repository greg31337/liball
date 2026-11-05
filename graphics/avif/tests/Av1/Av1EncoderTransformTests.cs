namespace avif.Tests;

public class Av1EncoderTransformTests
{
    [Test]
    public void LosslessTransformQuantizationAndReconstructionAreExact()
    {
        int[] residual =
        [
            -7, 3, 12, -1,
            5, -9, 0, 4,
            17, 2, -6, 8,
            -3, 11, 1, -13
        ];
        Av1TransformBlockEncodeRequest request = CreateRequest(residual);
        request.Lossless = true;

        Av1TransformBlockEncodeResult result =
            Av1TransformBlockEncoder.Encode(request);
        Av1CoefficientDecodeResult decoded = DecodeTokens(result, request);

        Assert.Multiple(() =>
        {
            Assert.That(result.Code, Is.EqualTo(AvifResultCode.Success));
            Assert.That(result.ReconstructedResidual, Is.EqualTo(residual));
            Assert.That(decoded.Code, Is.EqualTo(AvifResultCode.Success));
            Assert.That(decoded.QuantizedCoefficients,
                Is.EqualTo(result.QuantizedCoefficients));
            Assert.That(decoded.DequantizedCoefficients,
                Is.EqualTo(result.DequantizedCoefficients));
            Assert.That(decoded.EndOfBlock, Is.EqualTo(result.EndOfBlock));
        });
    }

    [Test]
    public void CoefficientTokensRoundTripZerosSignsBaseRangeAndGolombLevels()
    {
        int[] coefficients = new int[16];
        int[] scan = Av1ScanTables.GetScan(
            Av1TransformSize.Transform4x4,
            Av1TransformType.DctDct);
        coefficients[scan[0]] = -20;
        coefficients[scan[1]] = 1;
        coefficients[scan[3]] = -4;
        coefficients[scan[7]] = 15;
        Av1CoefficientTokenizationRequest tokenRequest =
            new Av1CoefficientTokenizationRequest();
        tokenRequest.QuantizedCoefficients = coefficients;
        tokenRequest.EndOfBlock = 8;
        tokenRequest.DcSignContext = 2;
        Av1CoefficientTokenizationResult tokens =
            Av1CoefficientTokenizer.Tokenize(tokenRequest);

        Av1CoefficientDecodeRequest decodeRequest =
            new Av1CoefficientDecodeRequest();
        decodeRequest.Reader = Av1CoefficientSymbolReader.Create(
            tokens.Symbols,
            tokens.Bits);
        decodeRequest.DcSignContext = 2;
        decodeRequest.Lossless = true;
        Av1CoefficientDecodeResult decoded =
            Av1CoefficientDecoder.Decode(decodeRequest);

        Assert.Multiple(() =>
        {
            Assert.That(tokens.Code, Is.EqualTo(AvifResultCode.Success));
            Assert.That(decoded.Code, Is.EqualTo(AvifResultCode.Success));
            Assert.That(decoded.EndOfBlock, Is.EqualTo(8));
            Assert.That(decoded.QuantizedCoefficients, Is.EqualTo(coefficients));
            Assert.That(decoded.DequantizedCoefficients,
                Is.EqualTo(coefficients.Select(value => value * 4)));
        });
    }

    [Test]
    public void CdfBackedCoefficientTokensRoundTripThroughArithmeticCoder()
    {
        int[] coefficients = new int[16];
        int[] scan = Av1ScanTables.GetScan(
            Av1TransformSize.Transform4x4,
            Av1TransformType.DctDct);
        coefficients[scan[0]] = 3;
        coefficients[scan[2]] = -2;
        coefficients[scan[5]] = 7;
        Av1CoefficientTokenizationRequest tokenRequest =
            new Av1CoefficientTokenizationRequest();
        tokenRequest.QuantizedCoefficients = coefficients;
        tokenRequest.EndOfBlock = 6;
        Av1CoefficientTokenizationResult tokens =
            Av1CoefficientTokenizer.Tokenize(tokenRequest);

        Av1FrameCdfContext? encoderFrame = Av1FrameCdfContext.CreateDefault(0);
        Av1TileCdfContext encoderContext = (encoderFrame ?? throw new InvalidOperationException("Invalid internal state.")).CreateTileContext(1, 0);
        Av1SymbolEncoder symbolEncoder = new Av1SymbolEncoder(false);
        AvifResultCode writeCode = Av1CoefficientTokenizer.WriteTokens(
            tokens,
            symbolEncoder,
            encoderContext);
        AvifResultCode exitCode = symbolEncoder.Exit();
        byte[] payload = symbolEncoder.GetOutput();

        Av1FrameCdfContext? decoderFrame = Av1FrameCdfContext.CreateDefault(0);
        Av1TileCdfContext decoderContext = (decoderFrame ?? throw new InvalidOperationException("Invalid internal state.")).CreateTileContext(1, 0);
        Av1SymbolDecoder symbolDecoder = new Av1SymbolDecoder(
            payload,
            0,
            payload.Length,
            false);
        Av1CoefficientDecodeRequest decodeRequest =
            new Av1CoefficientDecodeRequest();
        decodeRequest.Reader = Av1CoefficientSymbolReader.Create(
            symbolDecoder,
            decoderContext);
        decodeRequest.Lossless = true;
        Av1CoefficientDecodeResult decoded =
            Av1CoefficientDecoder.Decode(decodeRequest);
        AvifResultCode decoderExit = symbolDecoder.Exit();

        Assert.Multiple(() =>
        {
            Assert.That(writeCode, Is.EqualTo(AvifResultCode.Success));
            Assert.That(exitCode, Is.EqualTo(AvifResultCode.Success));
            Assert.That(decoded.Code, Is.EqualTo(AvifResultCode.Success));
            Assert.That(decoderExit, Is.EqualTo(AvifResultCode.Success));
            Assert.That(decoded.QuantizedCoefficients, Is.EqualTo(coefficients));
        });
    }

    [TestCase(8)]
    [TestCase(10)]
    [TestCase(12)]
    public void QuantizerProducesDecoderIdenticalDequantizationAtEveryDepth(
        int bitDepth)
    {
        Av1CoefficientQuantizationRequest request =
            new Av1CoefficientQuantizationRequest();
        request.BitDepth = bitDepth;
        request.QIndex = 173;
        request.DcDelta = -7;
        request.AcDelta = 9;
        request.UseQuantizerMatrix = true;
        request.MatrixLevel = 3;
        request.Coefficients =
        [
            3000, -1400, 709, 0,
            25, -91, 480, 17,
            0, 0, -5, 63,
            19, 800, -222, 1
        ];
        Av1CoefficientQuantizationResult quantized =
            Av1CoefficientQuantizer.Quantize(
                request,
                Av1TransformType.DctDct);
        Av1CoefficientTokenizationRequest tokenRequest =
            new Av1CoefficientTokenizationRequest();
        tokenRequest.QuantizedCoefficients = quantized.QuantizedCoefficients;
        tokenRequest.EndOfBlock = quantized.EndOfBlock;
        Av1CoefficientTokenizationResult tokens =
            Av1CoefficientTokenizer.Tokenize(tokenRequest);
        Av1CoefficientDecodeRequest decodeRequest =
            new Av1CoefficientDecodeRequest();
        decodeRequest.Reader = Av1CoefficientSymbolReader.Create(
            tokens.Symbols,
            tokens.Bits);
        decodeRequest.BitDepth = bitDepth;
        decodeRequest.QIndex = 173;
        decodeRequest.DcDelta = -7;
        decodeRequest.AcDelta = 9;
        decodeRequest.UseQuantizerMatrix = true;
        decodeRequest.MatrixLevel = 3;
        Av1CoefficientDecodeResult decoded =
            Av1CoefficientDecoder.Decode(decodeRequest);

        Assert.Multiple(() =>
        {
            Assert.That(quantized.Code, Is.EqualTo(AvifResultCode.Success));
            Assert.That(decoded.Code, Is.EqualTo(AvifResultCode.Success));
            Assert.That(decoded.QuantizedCoefficients,
                Is.EqualTo(quantized.QuantizedCoefficients));
            Assert.That(decoded.DequantizedCoefficients,
                Is.EqualTo(quantized.DequantizedCoefficients));
        });
    }

    [Test]
    public void ForwardDctPreservesConstantResidualAtFineQuantization()
    {
        int[] residual = Enumerable.Repeat(5, 16).ToArray();
        Av1TransformBlockEncodeRequest request = CreateRequest(residual);

        Av1TransformBlockEncodeResult result =
            Av1TransformBlockEncoder.Encode(request);

        Assert.Multiple(() =>
        {
            Assert.That(result.Code, Is.EqualTo(AvifResultCode.Success));
            Assert.That(result.EndOfBlock, Is.EqualTo(1));
            Assert.That(result.ReconstructedResidual, Is.EqualTo(residual));
            Assert.That(result.TransformCoefficients[0], Is.EqualTo(160));
        });
    }

    [Test]
    public void FourByFourForwardTransformSupportsEveryDeclaredType()
    {
        int[] residual =
        [
            4, -2, 7, 1,
            -3, 9, 0, -5,
            6, 2, -8, 3,
            1, -4, 5, -1
        ];
        int type = 0;
        while (type < Av1SyntaxConstants.TransformTypeCount)
        {
            Av1ForwardTransformRequest request = new Av1ForwardTransformRequest();
            request.TransformType = (Av1TransformType)type;
            request.Residual = residual;
            Av1ForwardTransformResult forward =
                Av1ForwardTransformer.Transform(request);
            Av1InverseTransformRequest inverseRequest =
                new Av1InverseTransformRequest();
            inverseRequest.TransformType = request.TransformType;
            inverseRequest.Coefficients = forward.Coefficients;
            Av1InverseTransformResult inverse =
                Av1InverseTransformer.Transform(inverseRequest);

            Assert.Multiple(() =>
            {
                Assert.That(forward.Code, Is.EqualTo(AvifResultCode.Success),
                    $"forward transform type {type}");
                Assert.That(forward.Coefficients, Has.Length.EqualTo(16));
                Assert.That(inverse.Code, Is.EqualTo(AvifResultCode.Success),
                    $"inverse transform type {type}");
                Assert.That(inverse.Residual, Has.Length.EqualTo(16));
            });
            type++;
        }
    }

    [Test]
    public void ForwardDctSupportsEveryTransformSizeAndReducedDomain()
    {
        int size = 0;
        while (size < Av1SyntaxConstants.TransformSizeCount)
        {
            Av1TransformSize transformSize = (Av1TransformSize)size;
            int width = Av1ConversionTables.GetTransformWidth(transformSize);
            int height = Av1ConversionTables.GetTransformHeight(transformSize);
            Av1ForwardTransformRequest request = new Av1ForwardTransformRequest();
            request.TransformSize = transformSize;
            request.Residual = new int[width * height];
            int index = 0;
            while (index < request.Residual.Length)
            {
                request.Residual[index] = index % 7 - 3;
                index++;
            }
            Av1ForwardTransformResult result =
                Av1ForwardTransformer.Transform(request);

            Assert.Multiple(() =>
            {
                Assert.That(result.Code, Is.EqualTo(AvifResultCode.Success),
                    $"transform size {size}");
                Assert.That(result.Width, Is.EqualTo(width));
                Assert.That(result.Height, Is.EqualTo(height));
                Assert.That(result.Coefficients, Has.Length.EqualTo(
                    Av1ConversionTables.GetTransformCoefficientCount(transformSize)));
            });
            size++;
        }
    }

    [Test]
    public void EveryTransformSizeUsesItsNormativeCoefficientDomainAndScan()
    {
        int size = 0;
        while (size < Av1SyntaxConstants.TransformSizeCount)
        {
            Av1TransformSize transformSize = (Av1TransformSize)size;
            int count = Av1ConversionTables.GetTransformCoefficientCount(
                transformSize);
            int[] coefficients = new int[count];
            int[] scan = Av1ScanTables.GetScan(
                transformSize,
                Av1TransformType.DctDct);
            coefficients[scan[^1]] = size % 2 == 0 ? 1 : -1;
            Av1CoefficientTokenizationRequest request =
                new Av1CoefficientTokenizationRequest();
            request.TransformSize = transformSize;
            request.QuantizedCoefficients = coefficients;
            request.EndOfBlock = count;
            Av1CoefficientTokenizationResult tokens =
                Av1CoefficientTokenizer.Tokenize(request);
            Av1CoefficientDecodeRequest decodeRequest =
                new Av1CoefficientDecodeRequest();
            decodeRequest.Reader = Av1CoefficientSymbolReader.Create(
                tokens.Symbols,
                tokens.Bits);
            decodeRequest.TransformSize = transformSize;
            Av1CoefficientDecodeResult decoded =
                Av1CoefficientDecoder.Decode(decodeRequest);

            Assert.Multiple(() =>
            {
                Assert.That(tokens.Code, Is.EqualTo(AvifResultCode.Success));
                Assert.That(decoded.Code, Is.EqualTo(AvifResultCode.Success));
                Assert.That(decoded.EndOfBlock, Is.EqualTo(count));
                Assert.That(decoded.QuantizedCoefficients, Is.EqualTo(coefficients));
            });
            size++;
        }
    }

    [Test]
    public void InvalidLosslessTransformsAndInconsistentEndOfBlockAreRejected()
    {
        Av1ForwardTransformRequest badTransform = new Av1ForwardTransformRequest();
        badTransform.TransformSize = Av1TransformSize.Transform8x8;
        badTransform.Residual = new int[64];
        badTransform.Lossless = true;
        Av1CoefficientTokenizationRequest badEnd =
            new Av1CoefficientTokenizationRequest();
        badEnd.QuantizedCoefficients = new int[16];
        badEnd.QuantizedCoefficients[0] = 1;
        badEnd.EndOfBlock = 0;

        Assert.Multiple(() =>
        {
            Assert.That(Av1ForwardTransformer.Transform(badTransform).Code,
                Is.EqualTo(AvifResultCode.InvalidArgument));
            Assert.That(Av1CoefficientTokenizer.Tokenize(badEnd).Code,
                Is.EqualTo(AvifResultCode.InvalidArgument));
        });
    }

    static Av1TransformBlockEncodeRequest CreateRequest(int[] residual)
    {
        Av1TransformBlockEncodeRequest request =
            new Av1TransformBlockEncodeRequest();
        request.Residual = residual;
        return request;
    }

    static Av1CoefficientDecodeResult DecodeTokens(
        Av1TransformBlockEncodeResult encoded,
        Av1TransformBlockEncodeRequest request)
    {
        Av1CoefficientDecodeRequest decodeRequest =
            new Av1CoefficientDecodeRequest();
        decodeRequest.Reader = Av1CoefficientSymbolReader.Create(
            (encoded.Tokenization ?? throw new InvalidOperationException("Invalid internal state.")).Symbols,
            encoded.Tokenization.Bits);
        decodeRequest.TransformSize = request.TransformSize;
        decodeRequest.TransformType = request.TransformType;
        decodeRequest.Plane = request.Plane;
        decodeRequest.BitDepth = request.BitDepth;
        decodeRequest.QIndex = request.QIndex;
        decodeRequest.DcDelta = request.DcDelta;
        decodeRequest.AcDelta = request.AcDelta;
        decodeRequest.Lossless = request.Lossless;
        decodeRequest.UseQuantizerMatrix = request.UseQuantizerMatrix;
        decodeRequest.MatrixLevel = request.MatrixLevel;
        decodeRequest.TransformBlockSkipContext =
            request.TransformBlockSkipContext;
        decodeRequest.DcSignContext = request.DcSignContext;
        return Av1CoefficientDecoder.Decode(decodeRequest);
    }
}
