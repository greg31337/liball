namespace avif.Tests;

public class Av1EncoderReconstructionTests
{
    const int Width = 8;
    const int Height = 8;

    static object[] Formats = new object[]
    {
        new object[] { 8, AvifChromaFormat.Monochrome, 0 },
        new object[] { 10, AvifChromaFormat.Monochrome, 0 },
        new object[] { 12, AvifChromaFormat.Monochrome, 2 },
        new object[] { 8, AvifChromaFormat.Yuv420, 0 },
        new object[] { 10, AvifChromaFormat.Yuv420, 0 },
        new object[] { 12, AvifChromaFormat.Yuv420, 2 },
        new object[] { 8, AvifChromaFormat.Yuv422, 2 },
        new object[] { 10, AvifChromaFormat.Yuv422, 2 },
        new object[] { 12, AvifChromaFormat.Yuv422, 2 },
        new object[] { 8, AvifChromaFormat.Yuv444, 1 },
        new object[] { 10, AvifChromaFormat.Yuv444, 1 },
        new object[] { 12, AvifChromaFormat.Yuv444, 2 }
    };

    [TestCaseSource(nameof(Formats))]
    public void LosslessRoundTripReconstructsOriginalForEveryFormat(
        int bitDepth,
        AvifChromaFormat chromaFormat,
        int expectedProfile)
    {
        FormatRoundTrip result = RunRoundTrip(
            bitDepth,
            chromaFormat,
            true);

        Assert.Multiple(() =>
        {
            Assert.That(result.Code, Is.EqualTo(AvifResultCode.Success));
            Assert.That(result.Profile, Is.EqualTo(expectedProfile));
            AssertComparison(result.EncoderDecoderComparison);
            AssertComparison(result.SourceDecoderComparison);
            Assert.That(result.ComparedSampleCount,
                Is.EqualTo(ExpectedSampleCount(chromaFormat)));
        });
    }

    [TestCaseSource(nameof(Formats))]
    public void LossyEncoderReconstructionMatchesDecoderForEveryFormat(
        int bitDepth,
        AvifChromaFormat chromaFormat,
        int expectedProfile)
    {
        FormatRoundTrip result = RunRoundTrip(
            bitDepth,
            chromaFormat,
            false);

        Assert.Multiple(() =>
        {
            Assert.That(result.Code, Is.EqualTo(AvifResultCode.Success));
            Assert.That(result.Profile, Is.EqualTo(expectedProfile));
            AssertComparison(result.EncoderDecoderComparison);
            Assert.That(result.ComparedSampleCount,
                Is.EqualTo(ExpectedSampleCount(chromaFormat)));
            Assert.That(result.BlockCount, Is.GreaterThan(0));
        });
    }

    [TestCaseSource(nameof(Formats))]
    public void RepeatedEncodingIsByteDeterministicForEveryFormat(
        int bitDepth,
        AvifChromaFormat chromaFormat,
        int expectedProfile)
    {
        FormatRoundTrip first = RunRoundTrip(
            bitDepth,
            chromaFormat,
            false);
        FormatRoundTrip second = RunRoundTrip(
            bitDepth,
            chromaFormat,
            false);

        Assert.Multiple(() =>
        {
            Assert.That(first.Code, Is.EqualTo(AvifResultCode.Success));
            Assert.That(second.Code, Is.EqualTo(AvifResultCode.Success));
            Assert.That(first.Profile, Is.EqualTo(expectedProfile));
            Assert.That(second.Profile, Is.EqualTo(expectedProfile));
            Assert.That(first.Payload, Is.Not.Empty);
            Assert.That(second.Payload, Is.EqualTo(first.Payload));
        });
    }

    [TestCaseSource(nameof(Formats))]
    public void ScriptedAndArithmeticDecodersAgreeForEveryFormat(
        int bitDepth,
        AvifChromaFormat chromaFormat,
        int expectedProfile)
    {
        FormatRoundTrip result = RunRoundTrip(
            bitDepth,
            chromaFormat,
            false);

        Assert.Multiple(() =>
        {
            Assert.That(result.Code, Is.EqualTo(AvifResultCode.Success));
            Assert.That(result.Profile, Is.EqualTo(expectedProfile));
            Assert.That(result.DifferentialMatches, Is.True);
            Assert.That(result.DecoderExitCode,
                Is.EqualTo(AvifResultCode.Success));
        });
    }

    static FormatRoundTrip RunRoundTrip(
        int bitDepth,
        AvifChromaFormat chromaFormat,
        bool lossless)
    {
        FormatRoundTrip result = new FormatRoundTrip();
        Av1EncoderProfileRequest profileRequest =
            new Av1EncoderProfileRequest();
        profileRequest.BitDepth = bitDepth;
        profileRequest.ChromaFormat = chromaFormat;
        profileRequest.ChromaSamplePosition =
            AvifChromaSamplePosition.Colocated;
        Av1EncoderProfileDecision profile =
            Av1EncoderProfileSelector.Choose(profileRequest);
        result.Code = profile.Code;
        if (profile.Code != AvifResultCode.Success)
        {
            return result;
        }
        result.Profile = (int)profile.Profile;
        ParsedAv1SequenceHeader sequence = CreateHeader(profile);
        PlaneBuffer[] source = CreatePlanes(profile);
        PlaneBuffer[] prediction = CreatePlanes(profile);
        PlaneBuffer[] encoderReconstruction = CreatePlanes(profile);
        PlaneBuffer[] decoderReconstruction = CreatePlanes(profile);
        try
        {
            FillPlanes(source, prediction, bitDepth);
            CopyPlanes(prediction, encoderReconstruction);
            CopyPlanes(prediction, decoderReconstruction);
            int qIndex = lossless ? 0 : 111;
            Av1FrameCdfContext? encoderFrame =
                Av1FrameCdfContext.CreateDefault(qIndex);
            Av1TileCdfContext encoderContext =
                (encoderFrame ?? throw new InvalidOperationException("Invalid internal state.")).CreateTileContext(1, 0);
            Av1SymbolEncoder symbolEncoder = new Av1SymbolEncoder(false);
            List<EncodedBlock> blocks = new List<EncodedBlock>();
            EncodePlanes(
                source,
                prediction,
                encoderReconstruction,
                bitDepth,
                qIndex,
                lossless,
                symbolEncoder,
                encoderContext,
                blocks,
                result);
            if (result.Code != AvifResultCode.Success)
            {
                return result;
            }
            result.Code = symbolEncoder.Exit();
            if (result.Code != AvifResultCode.Success)
            {
                return result;
            }
            result.Payload = symbolEncoder.GetOutput();
            DecodeBlocks(
                blocks,
                decoderReconstruction,
                qIndex,
                result);
            if (result.Code != AvifResultCode.Success)
            {
                return result;
            }
            result.EncoderDecoderComparison = Av1PixelConformance.Compare(
                sequence,
                Width,
                Height,
                encoderReconstruction,
                decoderReconstruction);
            result.SourceDecoderComparison = Av1PixelConformance.Compare(
                sequence,
                Width,
                Height,
                source,
                decoderReconstruction);
            result.ComparedSampleCount =
                result.EncoderDecoderComparison.ComparedSampleCount;
            return result;
        }
        finally
        {
            ClosePlanes(source);
            ClosePlanes(prediction);
            ClosePlanes(encoderReconstruction);
            ClosePlanes(decoderReconstruction);
        }
    }

    static void EncodePlanes(
        PlaneBuffer[] source,
        PlaneBuffer[] prediction,
        PlaneBuffer[] reconstruction,
        int bitDepth,
        int qIndex,
        bool lossless,
        Av1SymbolEncoder symbolEncoder,
        Av1TileCdfContext encoderContext,
        List<EncodedBlock> blocks,
        FormatRoundTrip result)
    {
        int plane = 0;
        while (plane < source.Length
            && result.Code == AvifResultCode.Success)
        {
            int y = 0;
            while (y < source[plane].Height
                && result.Code == AvifResultCode.Success)
            {
                int x = 0;
                while (x < source[plane].Width
                    && result.Code == AvifResultCode.Success)
                {
                    Av1TransformBlockEncodeRequest request =
                        CreateBlockRequest(
                            source[plane],
                            prediction[plane],
                            x,
                            y,
                            plane,
                            bitDepth,
                            qIndex,
                            lossless);
                    Av1TransformBlockEncodeResult encoded =
                        Av1TransformBlockEncoder.Encode(request);
                    result.Code = encoded.Code;
                    if (result.Code == AvifResultCode.Success)
                    {
                        result.Code = Av1CoefficientTokenizer.WriteTokens(
                            encoded.Tokenization,
                            symbolEncoder,
                            encoderContext);
                    }
                    if (result.Code == AvifResultCode.Success)
                    {
                        ApplyResidual(
                            reconstruction[plane],
                            x,
                            y,
                            encoded.ReconstructedResidual,
                            bitDepth);
                        blocks.Add(new EncodedBlock(
                            request,
                            encoded,
                            plane,
                            x,
                            y));
                        result.BlockCount++;
                    }
                    x += 4;
                }
                y += 4;
            }
            plane++;
        }
    }

    static void DecodeBlocks(
        List<EncodedBlock> blocks,
        PlaneBuffer[] reconstruction,
        int qIndex,
        FormatRoundTrip result)
    {
        Av1FrameCdfContext? decoderFrame =
            Av1FrameCdfContext.CreateDefault(qIndex);
        Av1TileCdfContext decoderContext =
            (decoderFrame ?? throw new InvalidOperationException("Invalid internal state.")).CreateTileContext(1, 0);
        Av1SymbolDecoder symbolDecoder = new Av1SymbolDecoder(
            result.Payload,
            0,
            result.Payload.Length,
            false);
        result.DifferentialMatches = true;
        int index = 0;
        while (index < blocks.Count
            && result.Code == AvifResultCode.Success)
        {
            EncodedBlock block = blocks[index];
            Av1CoefficientDecodeRequest arithmeticRequest =
                CreateDecodeRequest(
                    block.Request,
                    Av1CoefficientSymbolReader.Create(
                        symbolDecoder,
                        decoderContext));
            Av1CoefficientDecodeResult arithmetic =
                Av1CoefficientDecoder.Decode(arithmeticRequest);
            Av1CoefficientDecodeRequest scriptedRequest =
                CreateDecodeRequest(
                    block.Request,
                    Av1CoefficientSymbolReader.Create(
                        (block.Encoded.Tokenization ?? throw new InvalidOperationException("Invalid internal state.")).Symbols,
                        block.Encoded.Tokenization.Bits));
            Av1CoefficientDecodeResult scripted =
                Av1CoefficientDecoder.Decode(scriptedRequest);
            result.Code = arithmetic.Code != AvifResultCode.Success
                ? arithmetic.Code
                : scripted.Code;
            result.DifferentialMatches = result.DifferentialMatches
                && SameCoefficients(arithmetic, scripted)
                && SameCoefficients(arithmetic, block.Encoded);
            if (result.Code == AvifResultCode.Success)
            {
                Av1InverseTransformRequest inverse =
                    new Av1InverseTransformRequest();
                inverse.TransformSize = block.Request.TransformSize;
                inverse.TransformType = block.Request.TransformType;
                inverse.BitDepth = block.Request.BitDepth;
                inverse.Coefficients = arithmetic.DequantizedCoefficients;
                inverse.EndOfBlock = arithmetic.EndOfBlock;
                inverse.Lossless = block.Request.Lossless;
                inverse.Destination = reconstruction[block.Plane];
                inverse.X = block.X;
                inverse.Y = block.Y;
                result.Code = Av1InverseTransformer.Reconstruct(inverse);
            }
            index++;
        }
        result.DecoderExitCode = symbolDecoder.Exit();
        if (result.Code == AvifResultCode.Success)
        {
            result.Code = result.DecoderExitCode;
        }
    }

    static Av1TransformBlockEncodeRequest CreateBlockRequest(
        PlaneBuffer source,
        PlaneBuffer prediction,
        int x,
        int y,
        int plane,
        int bitDepth,
        int qIndex,
        bool lossless)
    {
        Av1TransformBlockEncodeRequest request =
            new Av1TransformBlockEncodeRequest();
        request.Plane = plane;
        request.BitDepth = bitDepth;
        request.QIndex = qIndex;
        request.Lossless = lossless;
        request.UseQuantizerMatrix = !lossless;
        request.MatrixLevel = 3;
        request.TransformBlockSkipContext = (x + y + plane) % 3;
        request.DcSignContext = (x / 4 + y / 4 + plane) % 3;
        request.Residual = new int[16];
        int row = 0;
        while (row < 4)
        {
            int column = 0;
            while (column < 4)
            {
                request.Residual[row * 4 + column] =
                    ReadSample(source, x + column, y + row)
                    - ReadSample(prediction, x + column, y + row);
                column++;
            }
            row++;
        }
        return request;
    }

    static Av1CoefficientDecodeRequest CreateDecodeRequest(
        Av1TransformBlockEncodeRequest encoded,
        Av1CoefficientSymbolReader reader)
    {
        Av1CoefficientDecodeRequest request =
            new Av1CoefficientDecodeRequest();
        request.Reader = reader;
        request.TransformSize = encoded.TransformSize;
        request.TransformType = encoded.TransformType;
        request.Plane = encoded.Plane;
        request.BitDepth = encoded.BitDepth;
        request.QIndex = encoded.QIndex;
        request.DcDelta = encoded.DcDelta;
        request.AcDelta = encoded.AcDelta;
        request.Lossless = encoded.Lossless;
        request.UseQuantizerMatrix = encoded.UseQuantizerMatrix;
        request.MatrixLevel = encoded.MatrixLevel;
        request.TransformBlockSkipContext =
            encoded.TransformBlockSkipContext;
        request.DcSignContext = encoded.DcSignContext;
        return request;
    }

    static bool SameCoefficients(
        Av1CoefficientDecodeResult first,
        Av1CoefficientDecodeResult second)
    {
        return first.Code == second.Code
            && first.EndOfBlock == second.EndOfBlock
            && first.QuantizedCoefficients.SequenceEqual(
                second.QuantizedCoefficients)
            && first.DequantizedCoefficients.SequenceEqual(
                second.DequantizedCoefficients);
    }

    static bool SameCoefficients(
        Av1CoefficientDecodeResult decoded,
        Av1TransformBlockEncodeResult encoded)
    {
        return decoded.Code == encoded.Code
            && decoded.EndOfBlock == encoded.EndOfBlock
            && decoded.QuantizedCoefficients.SequenceEqual(
                encoded.QuantizedCoefficients)
            && decoded.DequantizedCoefficients.SequenceEqual(
                encoded.DequantizedCoefficients);
    }

    static PlaneBuffer[] CreatePlanes(Av1EncoderProfileDecision profile)
    {
        int planeCount = profile.Monochrome ? 1 : 3;
        PlaneBuffer[] planes = new PlaneBuffer[planeCount];
        int plane = 0;
        while (plane < planeCount)
        {
            int shiftX = plane == 0 || !profile.ChromaSubsamplingX ? 0 : 1;
            int shiftY = plane == 0 || !profile.ChromaSubsamplingY ? 0 : 1;
            planes[plane] = new PlaneBuffer(
                (Width + shiftX) >> shiftX,
                (Height + shiftY) >> shiftY,
                profile.HighBitDepth ? 2 : 1,
                1,
                1 << 20,
                true);
            plane++;
        }
        return planes;
    }

    static void FillPlanes(
        PlaneBuffer[] source,
        PlaneBuffer[] prediction,
        int bitDepth)
    {
        int scale = 1 << (bitDepth - 8);
        int plane = 0;
        while (plane < source.Length)
        {
            int y = 0;
            while (y < source[plane].Height)
            {
                int x = 0;
                while (x < source[plane].Width)
                {
                    int sourceBase =
                        (x * 53 + y * 31 + plane * 67 + 97) & 255;
                    int predictionBase =
                        (x * 17 + y * 29 + plane * 41 + 13) & 255;
                    WriteSample(
                        source[plane],
                        x,
                        y,
                        sourceBase * scale + (x + plane) % scale);
                    WriteSample(
                        prediction[plane],
                        x,
                        y,
                        predictionBase * scale + (y + plane) % scale);
                    x++;
                }
                y++;
            }
            plane++;
        }
    }

    static void CopyPlanes(PlaneBuffer[] source, PlaneBuffer[] destination)
    {
        int plane = 0;
        while (plane < source.Length)
        {
            int y = 0;
            while (y < source[plane].Height)
            {
                source[plane].GetRowSpan(y).CopyTo(
                    destination[plane].GetRowSpan(y));
                y++;
            }
            plane++;
        }
    }

    static void ApplyResidual(
        PlaneBuffer plane,
        int x,
        int y,
        int[] residual,
        int bitDepth)
    {
        int maximum = (1 << bitDepth) - 1;
        int row = 0;
        while (row < 4)
        {
            int column = 0;
            while (column < 4)
            {
                int value = ReadSample(plane, x + column, y + row)
                    + residual[row * 4 + column];
                value = Math.Clamp(value, 0, maximum);
                WriteSample(plane, x + column, y + row, value);
                column++;
            }
            row++;
        }
    }

    static int ReadSample(PlaneBuffer plane, int x, int y)
    {
        Span<byte> row = plane.GetRowSpan(y);
        int offset = x * plane.BytesPerSample;
        return plane.BytesPerSample == 1
            ? row[offset]
            : row[offset] | row[offset + 1] << 8;
    }

    static void WriteSample(PlaneBuffer plane, int x, int y, int value)
    {
        Span<byte> row = plane.GetRowSpan(y);
        int offset = x * plane.BytesPerSample;
        row[offset] = (byte)value;
        if (plane.BytesPerSample == 2)
        {
            row[offset + 1] = (byte)(value >> 8);
        }
    }

    static ParsedAv1SequenceHeader CreateHeader(
        Av1EncoderProfileDecision profile)
    {
        Av1SequenceFeatures features = new Av1SequenceFeatures();
        features.FrameWidthBits = 3;
        features.FrameHeightBits = 3;
        return new ParsedAv1SequenceHeader(
            (int)profile.Profile,
            true,
            true,
            null,
            null,
            false,
            new Av1OperatingPoint[] { new Av1OperatingPoint(0, 0, 0) },
            Width,
            Height,
            features,
            profile.HighBitDepth,
            profile.TwelveBit,
            profile.Monochrome,
            profile.ChromaSubsamplingX,
            profile.ChromaSubsamplingY,
            profile.ChromaSamplePosition,
            2,
            2,
            2,
            true,
            false,
            false,
            false,
            System.Array.Empty<byte>());
    }

    static long ExpectedSampleCount(AvifChromaFormat format)
    {
        if (format == AvifChromaFormat.Monochrome)
        {
            return Width * Height;
        }
        int chromaWidth = format == AvifChromaFormat.Yuv444
            ? Width
            : Width / 2;
        int chromaHeight = format == AvifChromaFormat.Yuv420
            ? Height / 2
            : Height;
        return Width * Height + 2 * chromaWidth * chromaHeight;
    }

    static void AssertComparison(Av1PixelComparisonResult? comparison)
    {
        Assert.That((comparison ?? throw new InvalidOperationException("Invalid internal state.")).Code, Is.EqualTo(AvifResultCode.Success));
        Assert.That(comparison.Matches, Is.True);
    }

    static void ClosePlanes(PlaneBuffer[] planes)
    {
        int plane = 0;
        while (plane < planes.Length)
        {
            planes[plane].Close();
            plane++;
        }
    }

    class EncodedBlock
    {
        internal Av1TransformBlockEncodeRequest Request;
        internal Av1TransformBlockEncodeResult Encoded;
        internal int Plane;
        internal int X;
        internal int Y;

        internal EncodedBlock(
            Av1TransformBlockEncodeRequest request,
            Av1TransformBlockEncodeResult encoded,
            int plane,
            int x,
            int y)
        {
            Request = request;
            Encoded = encoded;
            Plane = plane;
            X = x;
            Y = y;
        }
    }

    class FormatRoundTrip
    {
        internal AvifResultCode Code;
        internal int Profile;
        internal byte[] Payload;
        internal AvifResultCode DecoderExitCode;
        internal bool DifferentialMatches;
        internal int BlockCount;
        internal long ComparedSampleCount;
        internal Av1PixelComparisonResult? EncoderDecoderComparison;
        internal Av1PixelComparisonResult? SourceDecoderComparison;

        internal FormatRoundTrip()
        {
            Code = AvifResultCode.Success;
            Payload = System.Array.Empty<byte>();
        }
    }
}
