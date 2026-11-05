namespace barcode.Tests;

public class DataMatrixDpmPreprocessorTests
{
    [Test]
    public void ConvertsEveryPixelLayoutAndSignedStrideToOwnedLuminance()
    {
        BarcodeImage rgb = CreateColorImage(
            new byte[] { 255, 0, 0, 0, 255, 0 },
            2,
            1,
            6,
            0,
            BarcodePixelFormat.Rgb24);
        BarcodeImage bgr = CreateColorImage(
            new byte[]
            {
                255, 0, 0, 255,
                0, 0, 255, 255
            },
            1,
            2,
            -4,
            4,
            BarcodePixelFormat.Bgra32);
        BarcodeImage bgr24 = CreateColorImage(
            new byte[] { 255, 0, 0 },
            1,
            1,
            3,
            0,
            BarcodePixelFormat.Bgr24);
        BarcodeImage rgba = CreateColorImage(
            new byte[] { 0, 255, 0, 7 },
            1,
            1,
            4,
            0,
            BarcodePixelFormat.Rgba32);

        ManagedGrayImage rgbGray = DataMatrixDpmPreprocessor.ConvertToLuminance(rgb);
        ManagedGrayImage bgrGray = DataMatrixDpmPreprocessor.ConvertToLuminance(bgr);
        ManagedGrayImage bgr24Gray = DataMatrixDpmPreprocessor.ConvertToLuminance(bgr24);
        ManagedGrayImage rgbaGray = DataMatrixDpmPreprocessor.ConvertToLuminance(rgba);
        rgb.Pixels![0] = 0;
        bgr.Pixels![4] = 0;

        Assert.Multiple(() =>
        {
            Assert.That(rgbGray.ReadSample(0, 0), Is.EqualTo(77));
            Assert.That(rgbGray.ReadSample(1, 0), Is.EqualTo(149));
            Assert.That(bgrGray.ReadSample(0, 0), Is.EqualTo(77));
            Assert.That(bgrGray.ReadSample(0, 1), Is.EqualTo(29));
            Assert.That(bgr24Gray.ReadSample(0, 0), Is.EqualTo(29));
            Assert.That(rgbaGray.ReadSample(0, 0), Is.EqualTo(149));
            Assert.That(rgbGray.CopySamples(), Is.EqualTo(new int[] { 77, 149 }));
        });
    }

    [Test]
    public void LowContrastGradientIsFlattenedAndDecoded()
    {
        byte[] data = global::System.Text.Encoding.ASCII.GetBytes("LOW CONTRAST");
        DataMatrixEncodingResult encoded = EncodeVersion(5, data);
        int scale = 5;
        int left = 10;
        int top = 9;
        BarcodeImage image = CreateDpmImage(
            encoded.SymbolMatrix,
            scale,
            left,
            top,
            false,
            false,
            true,
            16);

        DataMatrixDpmPreprocessingResult preprocessed = DataMatrixDpmPreprocessor.Preprocess(
            image,
            BarcodeEffort.Thorough);
        DataMatrixImageDecodeResult? decoded = DecodeCandidates(
            preprocessed,
            encoded.SymbolInfo,
            left,
            top,
            scale,
            data,
            "AdaptiveDark");

        Assert.Multiple(() =>
        {
            Assert.That(decoded, Is.Not.Null);
            Assert.That(decoded!.Decode.Result.Payload!.RawBytes, Is.EqualTo(data));
            Assert.That(preprocessed.BackgroundRadius, Is.GreaterThanOrEqualTo(4));
            Assert.That(preprocessed.MeanLocalContrast, Is.GreaterThan(0.0));
            Assert.That(preprocessed.GlobalThreshold, Is.InRange(0, 255));
            Assert.That(preprocessed.Flattened.ReadSample(left + 2, top + 2), Is.LessThan(128));
        });
    }

    [Test]
    public void DotPeenMarksAreConnectedIntoFinderEvidenceAndDecoded()
    {
        byte[] data = global::System.Text.Encoding.ASCII.GetBytes("DOT");
        DataMatrixEncodingResult encoded = EncodeVersion(3, data);
        int scale = 6;
        int left = 9;
        int top = 8;
        BarcodeImage image = CreateDpmImage(
            encoded.SymbolMatrix,
            scale,
            left,
            top,
            false,
            true,
            false,
            70);

        DataMatrixDpmPreprocessingResult preprocessed = DataMatrixDpmPreprocessor.Preprocess(
            image,
            BarcodeEffort.Thorough);
        DataMatrixDpmBinaryCandidate connected = FindCandidate(
            preprocessed.Candidates,
            "DotConnectDark2");
        DataMatrixFinderEvidence[] evidence = DataMatrixImageFinder.Locate(
            connected.Modules,
            BarcodePolarity.DarkOnLight);
        DataMatrixImageDecodeResult? decoded = DecodeOneCandidate(
            connected,
            encoded.SymbolInfo,
            left,
            top,
            scale);

        Assert.Multiple(() =>
        {
            Assert.That(evidence, Is.Not.Empty);
            Assert.That(connected.ConnectionRadius, Is.EqualTo(2));
            Assert.That(decoded, Is.Not.Null);
            Assert.That(decoded!.Decode.Result.Payload!.RawBytes, Is.EqualTo(data));
        });
    }

    [Test]
    public void BrightMarksUnderGlareProduceALightOnDarkDecodeCandidate()
    {
        byte[] data = global::System.Text.Encoding.ASCII.GetBytes("GLARE");
        DataMatrixEncodingResult encoded = EncodeVersion(4, data);
        int scale = 5;
        int left = 11;
        int top = 10;
        BarcodeImage image = CreateDpmImage(
            encoded.SymbolMatrix,
            scale,
            left,
            top,
            true,
            false,
            true,
            30);

        DataMatrixDpmPreprocessingResult preprocessed = DataMatrixDpmPreprocessor.Preprocess(
            image,
            BarcodeEffort.Balanced);
        DataMatrixDpmBinaryCandidate candidate = FindCandidate(
            preprocessed.Candidates,
            "AdaptiveLight");
        DataMatrixImageDecodeResult? decoded = DecodeOneCandidate(
            candidate,
            encoded.SymbolInfo,
            left,
            top,
            scale);

        Assert.Multiple(() =>
        {
            Assert.That(decoded, Is.Not.Null);
            Assert.That(decoded!.Decode.Result.Payload!.RawBytes, Is.EqualTo(data));
            Assert.That(decoded.Decode.Result.Polarity, Is.EqualTo(BarcodePolarity.LightOnDark));
            Assert.That(candidate.ForegroundFraction, Is.GreaterThan(0.5));
        });
    }

    [Test]
    public void TextureSuppressionRecoversMarksFromCheckerAndDeterministicNoise()
    {
        byte[] data = global::System.Text.Encoding.ASCII.GetBytes("TEXTURE");
        DataMatrixEncodingResult encoded = EncodeVersion(5, data);
        int scale = 5;
        int left = 8;
        int top = 8;
        BarcodeImage image = CreateDpmImage(
            encoded.SymbolMatrix,
            scale,
            left,
            top,
            false,
            false,
            false,
            35);
        AddTexture(image, 14);

        DataMatrixDpmPreprocessingResult preprocessed = DataMatrixDpmPreprocessor.Preprocess(
            image,
            BarcodeEffort.Thorough);
        DataMatrixDpmBinaryCandidate candidate = FindCandidate(
            preprocessed.Candidates,
            "TextureSuppressedDark");
        DataMatrixImageDecodeResult? decoded = DecodeOneCandidate(
            candidate,
            encoded.SymbolInfo,
            left,
            top,
            scale);

        Assert.Multiple(() =>
        {
            Assert.That(decoded, Is.Not.Null);
            Assert.That(decoded!.Decode.Result.Payload!.RawBytes, Is.EqualTo(data));
            Assert.That(candidate.ForegroundFraction, Is.InRange(0.05, 0.70));
        });
    }

    [Test]
    public void EffortControlsCandidateBreadthAndCallerPixelsRemainUnmodified()
    {
        DataMatrixEncodingResult encoded = EncodeVersion(
            1,
            global::System.Text.Encoding.ASCII.GetBytes("A"));
        BarcodeImage image = CreateDpmImage(
            encoded.SymbolMatrix,
            4,
            5,
            5,
            false,
            false,
            false,
            80);
        byte[] original = CopyBytes(image.Pixels!);

        DataMatrixDpmPreprocessingResult fast = DataMatrixDpmPreprocessor.Preprocess(
            image,
            BarcodeEffort.Fast);
        DataMatrixDpmPreprocessingResult balanced = DataMatrixDpmPreprocessor.Preprocess(
            image,
            BarcodeEffort.Balanced);
        DataMatrixDpmPreprocessingResult thorough = DataMatrixDpmPreprocessor.Preprocess(
            image,
            BarcodeEffort.Thorough);

        Assert.Multiple(() =>
        {
            Assert.That(fast.Candidates.Length, Is.LessThan(balanced.Candidates.Length));
            Assert.That(balanced.Candidates.Length, Is.LessThanOrEqualTo(thorough.Candidates.Length));
            Assert.That(HasConnectionRadius(thorough.Candidates, 3), Is.True);
            Assert.That(HasConnectionRadius(balanced.Candidates, 3), Is.False);
            Assert.That(image.Pixels, Is.EqualTo(original));
        });
    }

    [Test]
    public void InvalidImagesSamplesAndEffortAreRejected()
    {
        BarcodeImage missing = new BarcodeImage();
        missing.Width = 1;
        missing.Height = 1;
        BarcodeImage shortStride = CreateColorImage(
            new byte[12],
            2,
            2,
            5,
            0,
            BarcodePixelFormat.Rgb24);
        BarcodeImage badOffset = CreateColorImage(
            new byte[4],
            2,
            2,
            2,
            1,
            BarcodePixelFormat.Gray8);
        BarcodeImage badFormat = CreateColorImage(
            new byte[1],
            1,
            1,
            1,
            0,
            (BarcodePixelFormat)99);
        ManagedGrayImage gray = new ManagedGrayImage(1, 1);

        Assert.Multiple(() =>
        {
            Assert.That(() => DataMatrixDpmPreprocessor.ConvertToLuminance(missing), Throws.TypeOf<BarcodeValidationException>());
            Assert.That(() => DataMatrixDpmPreprocessor.ConvertToLuminance(shortStride), Throws.TypeOf<BarcodeValidationException>());
            Assert.That(() => DataMatrixDpmPreprocessor.ConvertToLuminance(badOffset), Throws.TypeOf<BarcodeValidationException>());
            Assert.That(() => DataMatrixDpmPreprocessor.ConvertToLuminance(badFormat), Throws.TypeOf<BarcodeValidationException>());
            Assert.That(() => DataMatrixDpmPreprocessor.Preprocess(missing, (BarcodeEffort)99), Throws.TypeOf<BarcodeValidationException>());
            Assert.That(() => gray.WriteSample(0, 0, -1), Throws.TypeOf<BarcodeValidationException>());
            Assert.That(() => gray.WriteSample(0, 0, 256), Throws.TypeOf<BarcodeValidationException>());
        });
    }

    static DataMatrixEncodingResult EncodeVersion(int version, byte[] data)
    {
        DataMatrixSymbolInfo symbol = DataMatrixSymbolInfoTable.GetSymbolByVersion(version);
        DataMatrixOptions options = new DataMatrixOptions();
        options.Rows = symbol.SymbolRows;
        options.Columns = symbol.SymbolColumns;
        BarcodePayload payload = new BarcodePayload();
        payload.RawBytes = CopyBytes(data);
        payload.Text = global::System.Text.Encoding.ASCII.GetString(data);
        return DataMatrixEncoder.Encode(data, payload, options, null, false);
    }

    static BarcodeImage CreateDpmImage(
        ModuleMatrix symbol,
        int scale,
        int left,
        int top,
        bool brightMarks,
        bool dots,
        bool glare,
        int contrast)
    {
        int width = left + (symbol.Width * scale) + 11;
        int height = top + (symbol.Height * scale) + 12;
        byte[] pixels = new byte[width * height];
        int row = 0;
        while (row < height)
        {
            int column = 0;
            while (column < width)
            {
                int illumination = brightMarks ? 65 : 175;
                illumination = illumination + ((column * 42) / width);
                if (glare)
                {
                    int distanceX = global::System.Math.Abs(column - ((width * 3) / 4));
                    int distanceY = global::System.Math.Abs(row - (height / 3));
                    int glareValue = 48 - distanceX - distanceY;
                    if (glareValue > 0)
                    {
                        illumination = illumination + glareValue;
                    }
                }

                bool marked = false;
                if (column >= left
                    && column < left + (symbol.Width * scale)
                    && row >= top
                    && row < top + (symbol.Height * scale))
                {
                    int localColumn = column - left;
                    int localRow = row - top;
                    int moduleColumn = localColumn / scale;
                    int moduleRow = localRow / scale;
                    marked = symbol.GetModule(moduleColumn, moduleRow);
                    if (marked && dots)
                    {
                        int centre = scale / 2;
                        int offsetX = (localColumn % scale) - centre;
                        int offsetY = (localRow % scale) - centre;
                        marked = (offsetX * offsetX) + (offsetY * offsetY) <= 2;
                    }
                }

                if (marked)
                {
                    illumination = brightMarks
                        ? illumination + contrast
                        : illumination - contrast;
                }

                pixels[(row * width) + column] = (byte)ClampByte(illumination);
                column++;
            }

            row++;
        }

        return CreateColorImage(
            pixels,
            width,
            height,
            width,
            0,
            BarcodePixelFormat.Gray8);
    }

    static void AddTexture(BarcodeImage image, int amplitude)
    {
        byte[] pixels = image.Pixels!;
        int row = 0;
        while (row < image.Height)
        {
            int column = 0;
            while (column < image.Width)
            {
                int checker = (column + row) % 2 == 0 ? amplitude : -amplitude;
                int noise = ((column * 17) + (row * 29)) % 9 - 4;
                int index = (row * image.Stride) + column;
                pixels[index] = (byte)ClampByte(pixels[index] + checker + noise);
                column++;
            }

            row++;
        }
    }

    static DataMatrixImageDecodeResult? DecodeCandidates(
        DataMatrixDpmPreprocessingResult result,
        DataMatrixSymbolInfo symbol,
        int left,
        int top,
        int scale,
        byte[] expected,
        string preferredMethod)
    {
        int index = 0;
        while (index < result.Candidates.Length)
        {
            DataMatrixDpmBinaryCandidate candidate = result.Candidates[index];
            if (candidate.Method == preferredMethod)
            {
                DataMatrixImageDecodeResult? decoded = DecodeOneCandidate(
                    candidate,
                    symbol,
                    left,
                    top,
                    scale);
                if (decoded != null
                    && decoded.Decode.Result.Payload != null
                    && decoded.Decode.Result.Payload.RawBytes != null
                    && decoded.Decode.Result.Payload.RawBytes.SequenceEqual(expected))
                {
                    return decoded;
                }
            }

            index++;
        }

        return null;
    }

    static DataMatrixImageDecodeResult? DecodeOneCandidate(
        DataMatrixDpmBinaryCandidate candidate,
        DataMatrixSymbolInfo symbol,
        int left,
        int top,
        int scale)
    {
        BarcodeQuadrilateral location = GeometryMath.CreateQuadrilateral(
            GeometryMath.CreatePoint(left, top),
            GeometryMath.CreatePoint(left + (symbol.SymbolColumns * scale) - 1, top),
            GeometryMath.CreatePoint(
                left + (symbol.SymbolColumns * scale) - 1,
                top + (symbol.SymbolRows * scale) - 1),
            GeometryMath.CreatePoint(left, top + (symbol.SymbolRows * scale) - 1));
        DataMatrixFinderEvidence evidence = new DataMatrixFinderEvidence();
        evidence.Location = location;
        evidence.SymbolInfo = symbol.CopySymbolInfo();
        evidence.Rotation = BarcodeRotation.None;
        evidence.Polarity = candidate.ForegroundFraction > 0.5
            ? BarcodePolarity.LightOnDark
            : BarcodePolarity.DarkOnLight;
        evidence.MirrorMode = BarcodeMirrorMode.Normal;
        evidence.SolidBorderScore = 1.0;
        evidence.ClockTrackScore = 1.0;
        evidence.DimensionScore = 1.0;
        evidence.ModuleWidth = scale;
        evidence.ModuleHeight = scale;
        evidence.Confidence = 1.0;
        return DataMatrixProjectiveDecoder.TryDecodeCandidate(
            candidate.Modules,
            evidence,
            null);
    }

    static DataMatrixDpmBinaryCandidate FindCandidate(
        DataMatrixDpmBinaryCandidate[] candidates,
        string method)
    {
        int index = 0;
        while (index < candidates.Length)
        {
            if (candidates[index].Method == method)
            {
                return candidates[index];
            }

            index++;
        }

        throw new global::System.InvalidOperationException($"DPM candidate {method} was not produced.");
    }

    static bool HasConnectionRadius(
        DataMatrixDpmBinaryCandidate[] candidates,
        int radius)
    {
        int index = 0;
        while (index < candidates.Length)
        {
            if (candidates[index].ConnectionRadius == radius)
            {
                return true;
            }

            index++;
        }

        return false;
    }

    static BarcodeImage CreateColorImage(
        byte[] pixels,
        int width,
        int height,
        int stride,
        int offset,
        BarcodePixelFormat format)
    {
        BarcodeImage image = new BarcodeImage();
        image.Pixels = pixels;
        image.Width = width;
        image.Height = height;
        image.Stride = stride;
        image.Offset = offset;
        image.PixelFormat = format;
        return image;
    }

    static int ClampByte(int value)
    {
        if (value < 0)
        {
            return 0;
        }

        if (value > 255)
        {
            return 255;
        }

        return value;
    }

    static byte[] CopyBytes(byte[] source)
    {
        byte[] copy = new byte[source.Length];
        global::System.Array.Copy(source, copy, source.Length);
        return copy;
    }
}
