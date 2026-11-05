namespace barcode.Tests;

public class DecoderDetectorIntegrationTests
{
    [Test]
    public void PublicDecoderDecodesPureLinearMatrixAndStackedSymbols()
    {
        IBarcodeDecoder decoder = new BarcodeDecoder();
        BarcodeSymbol code128 = CreateCode128Symbol("PURE128");
        BarcodeSymbol dataMatrix = CreateDataMatrixSymbol("PUREDM");
        BarcodeSymbol pdf417 = CreatePdf417Symbol("PUREPDF");

        BarcodeDecodeResult[] linearResults = decoder.DecodeBarcode(
            CreatePureRequest(code128));
        BarcodeDecodeResult[] matrixResults = decoder.DecodeBarcode(
            CreatePureRequest(dataMatrix));
        BarcodeDecodeResult[] stackedResults = decoder.DecodeBarcode(
            CreatePureRequest(pdf417));

        Assert.Multiple(() =>
        {
            Assert.That(linearResults, Has.Length.EqualTo(1));
            Assert.That(linearResults[0].Format, Is.EqualTo(BarcodeFormat.Code128));
            Assert.That(linearResults[0].Payload!.Text, Is.EqualTo("PURE128"));
            Assert.That(matrixResults, Has.Length.EqualTo(1));
            Assert.That(matrixResults[0].Format, Is.EqualTo(BarcodeFormat.DataMatrix));
            Assert.That(matrixResults[0].Payload!.Text, Is.EqualTo("PUREDM"));
            Assert.That(stackedResults, Has.Length.EqualTo(1));
            Assert.That(stackedResults[0].Format, Is.EqualTo(BarcodeFormat.Pdf417));
            Assert.That(stackedResults[0].Payload!.Text, Is.EqualTo("PUREPDF"));
        });
    }

    [Test]
    public void PublicDecoderDecodesEverySupportedPureFormat()
    {
        BarcodeSymbol[] symbols = new BarcodeSymbol[]
        {
            CreateCode128Symbol("ALL128"),
            CreateGs1128Symbol(),
            CreateEanUpcSymbol("5512345", BarcodeFormat.Ean8),
            CreateEanUpcSymbol("400638133393", BarcodeFormat.Ean13),
            CreateEanUpcSymbol("03600029145", BarcodeFormat.UpcA),
            CreateUpcESymbol("123455"),
            CreateAddOnSymbol("12"),
            CreateAddOnSymbol("51234"),
            Code39Layout.CreateLayout(
                "ALL39",
                CreatePayload("ALL39", global::System.Text.Encoding.ASCII.GetBytes("ALL39")),
                null,
                null).Symbol,
            ItfLayout.CreateLayout(
                "123456",
                CreatePayload("123456", global::System.Text.Encoding.ASCII.GetBytes("123456")),
                null,
                null).Symbol,
            Itf14Profile.CreateLayout(
                "1234567890123",
                CreatePayload("1234567890123", global::System.Text.Encoding.ASCII.GetBytes("1234567890123")),
                null,
                null).Symbol,
            CodabarLayout.CreateLayout(
                "1234",
                CreatePayload("1234", global::System.Text.Encoding.ASCII.GetBytes("1234")),
                null,
                null).Symbol,
            CreateDataMatrixSymbol("ALLDM"),
            CreateGs1DataMatrixSymbol(),
            CreatePdf417Symbol("ALLPDF"),
            CreatePdf417Symbol("COMPACT", BarcodeFormat.CompactPdf417),
            CreatePdf417Symbol("MACRO", BarcodeFormat.MacroPdf417)
        };
        IBarcodeDecoder decoder = new BarcodeDecoder();

        foreach (BarcodeSymbol symbol in symbols)
        {
            BarcodeDecodeResult[] results = decoder.DecodeBarcode(
                CreatePureRequest(symbol));

            Assert.That(results, Has.Length.EqualTo(1), symbol.Format.ToString());
            Assert.That(results[0].Format, Is.EqualTo(symbol.Format), symbol.Format.ToString());
            Assert.That(results[0].Payload, Is.Not.Null, symbol.Format.ToString());
        }
    }

    [Test]
    public void PureDecodeHonorsFormatAndLengthFiltersAndReturnsDefensiveResults()
    {
        BarcodeSymbol symbol = CreateCode128Symbol("FILTER");
        BarcodeDecodeOptions rejectedOptions = new BarcodeDecodeOptions();
        rejectedOptions.AllowedFormats = new BarcodeFormat[] { BarcodeFormat.DataMatrix };
        rejectedOptions.Polarity = BarcodePolarity.Automatic;
        rejectedOptions.MirrorMode = BarcodeMirrorMode.Both;
        BarcodeDecodeRequest rejectedRequest = new BarcodeDecodeRequest();
        rejectedRequest.Symbol = symbol;
        rejectedRequest.Options = rejectedOptions;
        IBarcodeDecoder decoder = new BarcodeDecoder();

        BarcodeDecodeAttempt rejected = decoder.TryDecodeBarcode(rejectedRequest);
        BarcodeDecodeRequest acceptedRequest = CreatePureRequest(symbol);
        BarcodeDecodeResult[] first = decoder.DecodeBarcode(acceptedRequest);
        first[0].Payload!.RawBytes![0] = 0;
        first[0].Location!.TopLeft!.X = 999;
        BarcodeDecodeResult[] second = decoder.DecodeBarcode(acceptedRequest);

        Assert.Multiple(() =>
        {
            Assert.That(rejected.Success, Is.False);
            Assert.That(rejected.FailureCode, Is.EqualTo("PURE_SYMBOL_NOT_DECODED"));
            Assert.That(second[0].Payload!.RawBytes![0], Is.EqualTo((byte)'F'));
            Assert.That(second[0].Location!.TopLeft!.X, Is.Zero);
        });
    }

    [Test]
    public void DetectorReturnsLocalizationOnlyCandidatesInSourceCoordinates()
    {
        BarcodeSymbol symbol = CreateCode128Symbol("DETECT");
        BarcodeImage image = DrawSymbolImage(symbol, 3, 40, 12, 12);
        BarcodeDetectionOptions options = new BarcodeDetectionOptions();
        options.AllowedFormats = new BarcodeFormat[] { BarcodeFormat.Code128 };
        options.RegionOfInterest = GeometryMath.CreateRectangle(
            10,
            4,
            image.Width - 10,
            image.Height - 4);
        options.MaximumCandidates = 5;
        options.ExpectedCandidateCount = 1;
        options.Effort = BarcodeEffort.Fast;
        options.BinarizationMode = BarcodeBinarizationMode.Otsu;
        options.LocalizationMode = BarcodeLocalizationMode.Direct;
        options.Polarity = BarcodePolarity.DarkOnLight;
        options.MaximumDiagnostics = 10;
        BarcodeDetectionRequest request = new BarcodeDetectionRequest();
        request.Image = image;
        request.Options = options;

        BarcodeDetectionResult result = new BarcodeDetector().DetectBarcodes(request);

        Assert.That(result.Candidates, Has.Length.EqualTo(1));
        BarcodeCandidate candidate = result.Candidates![0];
        BarcodeDiagnostic coordinateDiagnostic = FindDiagnostic(
            result.Diagnostics!,
            DiagnosticCodes.CandidateCoordinates);
        BarcodeRectangle candidateBounds =
            GeometryMath.CalculateQuadrilateralBounds(candidate.Location!);
        Assert.Multiple(() =>
        {
            Assert.That(candidate.Format, Is.EqualTo(BarcodeFormat.Code128));
            Assert.That(candidate.Location!.TopLeft!.X,
                Is.GreaterThanOrEqualTo(40));
            Assert.That(candidate.Location.TopLeft.Y,
                Is.GreaterThanOrEqualTo(12));
            Assert.That(candidate.Confidence, Is.GreaterThan(0));
            Assert.That(result.Diagnostics, Is.Not.Empty);
            Assert.That(coordinateDiagnostic.Region!.X,
                Is.EqualTo(candidateBounds.X).Within(0.000000001));
            Assert.That(coordinateDiagnostic.Region.Y,
                Is.EqualTo(candidateBounds.Y).Within(0.000000001));
            Assert.That(coordinateDiagnostic.Region.Width,
                Is.EqualTo(candidateBounds.Width).Within(0.000000001));
            Assert.That(coordinateDiagnostic.Region.Height,
                Is.EqualTo(candidateBounds.Height).Within(0.000000001));
            Assert.That(coordinateDiagnostic.Location!.X,
                Is.EqualTo(candidateBounds.X + (candidateBounds.Width * 0.5))
                    .Within(0.000000001));
            Assert.That(coordinateDiagnostic.Location.Y,
                Is.EqualTo(candidateBounds.Y + (candidateBounds.Height * 0.5))
                    .Within(0.000000001));
        });
    }

    [TestCase(BarcodeRotation.None, 0.0)]
    [TestCase(BarcodeRotation.Clockwise90, 90.0)]
    [TestCase(BarcodeRotation.Clockwise180, 180.0)]
    [TestCase(BarcodeRotation.Clockwise270, 270.0)]
    public void PublicDetectorLocalizesEveryRightAngleRotation(
        BarcodeRotation rotation,
        double expectedAngle)
    {
        BarcodeSymbol symbol = CreateDataMatrixSymbol("ROTATE");
        BarcodeImage original = DrawSymbolImage(symbol, 3, 10, 9, 2);
        BarcodeImage image = TestImageTransforms.Rotate(original, rotation);
        BarcodeDetectionOptions options = new BarcodeDetectionOptions();
        options.AllowedFormats = new BarcodeFormat[]
        {
            BarcodeFormat.DataMatrix
        };
        options.MaximumCandidates = 4;
        options.ExpectedCandidateCount = 1;
        options.Effort = BarcodeEffort.Fast;
        options.BinarizationMode = BarcodeBinarizationMode.Otsu;
        options.LocalizationMode = BarcodeLocalizationMode.FinderPatterns;
        options.Polarity = BarcodePolarity.DarkOnLight;
        options.MirrorMode = BarcodeMirrorMode.Both;
        options.TryRotations = true;
        options.MaximumDiagnostics = 10;
        BarcodeDetectionRequest request = new BarcodeDetectionRequest();
        request.Image = image;
        request.Options = options;

        BarcodeDetectionResult result =
            new BarcodeDetector().DetectBarcodes(request);

        Assert.That(result.Candidates, Has.Length.EqualTo(1));
        Assert.Multiple(() =>
        {
            Assert.That(
                result.Candidates![0].Format,
                Is.EqualTo(BarcodeFormat.DataMatrix));
            Assert.That(
                result.Candidates[0].AngleDegrees,
                Is.EqualTo(expectedAngle).Within(0.000000001));
            Assert.That(result.Candidates[0].Location, Is.Not.Null);
            Assert.That(
                FindDiagnostic(
                    result.Diagnostics!,
                    DiagnosticCodes.CandidateCoordinates).Location,
                Is.Not.Null);
        });
    }

    [Test]
    public void PublicDecoderDecodesCleanLinearMatrixAndStackedImages()
    {
        BarcodeSymbol linear = CreateCode128Symbol("IMAGE128");
        BarcodeSymbol matrix = CreateDataMatrixSymbol("IMAGEDM");
        BarcodeSymbol stacked = CreatePdf417Symbol("IMAGEPDF");
        BarcodeDecodeRequest linearRequest = CreateImageRequest(
            DrawSymbolImage(linear, 3, 40, 8, 14),
            BarcodeFormat.Code128,
            BarcodeLocalizationMode.Direct);
        BarcodeDecodeRequest matrixRequest = CreateImageRequest(
            DrawSymbolImage(matrix, 3, 10, 10, 2),
            BarcodeFormat.DataMatrix,
            BarcodeLocalizationMode.Automatic);
        BarcodeDecodeRequest stackedRequest = CreateImageRequest(
            DrawSymbolImage(stacked, 3, 20, 10, 2),
            BarcodeFormat.Pdf417,
            BarcodeLocalizationMode.Automatic);
        IBarcodeDecoder decoder = new BarcodeDecoder();

        BarcodeDecodeResult[] linearResults = decoder.DecodeBarcode(linearRequest);
        BarcodeDecodeResult[] matrixResults = decoder.DecodeBarcode(matrixRequest);
        BarcodeDecodeResult[] stackedResults = decoder.DecodeBarcode(stackedRequest);

        Assert.Multiple(() =>
        {
            Assert.That(linearResults, Has.Length.EqualTo(1));
            Assert.That(linearResults[0].Payload!.Text, Is.EqualTo("IMAGE128"));
            Assert.That(matrixResults, Has.Length.EqualTo(1));
            Assert.That(matrixResults[0].Payload!.Text, Is.EqualTo("IMAGEDM"));
            Assert.That(stackedResults, Has.Length.EqualTo(1));
            Assert.That(stackedResults[0].Payload!.Text, Is.EqualTo("IMAGEPDF"));
        });
    }

    [Test]
    public void PublicDecoderReturnsMultipleDataMatrixImagesInReadingOrder()
    {
        BarcodeSymbol top = CreateDataMatrixSymbol("TOP");
        BarcodeSymbol bottom = CreateDataMatrixSymbol("BOTTOM");
        BarcodeImage image = BarcodeTestBuilder.CreateGrayImage(150, 125, 240);
        DrawMatrixSymbol(image.Pixels!, image.Stride, top, 3, 78, 8);
        DrawMatrixSymbol(image.Pixels!, image.Stride, bottom, 3, 8, 72);
        BarcodeDecodeRequest request = CreateImageRequest(
            image,
            BarcodeFormat.DataMatrix,
            BarcodeLocalizationMode.Automatic);
        request.Options!.MaximumResults = 4;
        request.Options.ExpectedResultCount = 2;
        IBarcodeDecoder decoder = new BarcodeDecoder();

        BarcodeDecodeResult[] results = decoder.DecodeBarcode(request);

        Assert.That(results, Has.Length.EqualTo(2));
        BarcodePoint topLocation = results[0].Location!.TopLeft!;
        BarcodePoint bottomLocation = results[1].Location!.TopLeft!;
        Assert.Multiple(() =>
        {
            Assert.That(results[0].Format, Is.EqualTo(BarcodeFormat.DataMatrix));
            Assert.That(results[0].Payload!.Text, Is.EqualTo("TOP"));
            Assert.That(results[1].Format, Is.EqualTo(BarcodeFormat.DataMatrix));
            Assert.That(results[1].Payload!.Text, Is.EqualTo("BOTTOM"));
            Assert.That(topLocation.Y, Is.LessThan(bottomLocation.Y));
            Assert.That(topLocation.X, Is.GreaterThan(bottomLocation.X));
        });
    }

    [Test]
    public void PublicDecoderReturnsMultiplePdf417ImagesInReadingOrder()
    {
        BarcodeSymbol top = CreatePdf417Symbol("TOPPDF");
        BarcodeSymbol bottom = CreatePdf417Symbol("BOTTOMPDF");
        BarcodeImage image = BarcodeTestBuilder.CreateGrayImage(310, 145, 240);
        DrawMatrixSymbol(image.Pixels!, image.Stride, top, 2, 38, 8);
        DrawMatrixSymbol(image.Pixels!, image.Stride, bottom, 2, 8, 84);
        BarcodeDecodeRequest request = CreateImageRequest(
            image,
            BarcodeFormat.Pdf417,
            BarcodeLocalizationMode.Automatic);
        request.Options!.MaximumResults = 4;
        request.Options.ExpectedResultCount = 2;
        IBarcodeDecoder decoder = new BarcodeDecoder();

        BarcodeDecodeResult[] results = decoder.DecodeBarcode(request);

        Assert.That(results, Has.Length.EqualTo(2));
        BarcodePoint topLocation = results[0].Location!.TopLeft!;
        BarcodePoint bottomLocation = results[1].Location!.TopLeft!;
        Assert.Multiple(() =>
        {
            Assert.That(results[0].Format, Is.EqualTo(BarcodeFormat.Pdf417));
            Assert.That(results[0].Payload!.Text, Is.EqualTo("TOPPDF"));
            Assert.That(results[1].Format, Is.EqualTo(BarcodeFormat.Pdf417));
            Assert.That(results[1].Payload!.Text, Is.EqualTo("BOTTOMPDF"));
            Assert.That(topLocation.Y, Is.LessThan(bottomLocation.Y));
            Assert.That(topLocation.X, Is.GreaterThan(bottomLocation.X));
        });
    }

    static BarcodeDecodeRequest CreatePureRequest(BarcodeSymbol symbol)
    {
        BarcodeDecodeOptions options = new BarcodeDecodeOptions();
        options.PureSymbol = true;
        options.Polarity = BarcodePolarity.Automatic;
        options.MirrorMode = BarcodeMirrorMode.Both;
        options.MaximumDiagnostics = 10;
        BarcodeDecodeRequest request = new BarcodeDecodeRequest();
        request.Symbol = symbol;
        request.Options = options;
        return request;
    }

    static BarcodeDecodeRequest CreateImageRequest(
        BarcodeImage image,
        BarcodeFormat format,
        BarcodeLocalizationMode localization)
    {
        BarcodeDecodeOptions options = new BarcodeDecodeOptions();
        options.AllowedFormats = new BarcodeFormat[] { format };
        options.MaximumResults = 5;
        options.ExpectedResultCount = 1;
        options.Polarity = BarcodePolarity.DarkOnLight;
        options.MirrorMode = BarcodeMirrorMode.Both;
        options.Effort = BarcodeEffort.Fast;
        options.BinarizationMode = BarcodeBinarizationMode.Otsu;
        options.LocalizationMode = localization;
        options.MaximumDiagnostics = 20;
        BarcodeDecodeRequest request = new BarcodeDecodeRequest();
        request.Image = image;
        request.Options = options;
        return request;
    }

    static BarcodeSymbol CreateCode128Symbol(string text)
    {
        byte[] bytes = global::System.Text.Encoding.ASCII.GetBytes(text);
        Code128EncodedData encoded = Code128Encoder.EncodeBytes(bytes, null);
        BarcodePayload payload = CreatePayload(text, bytes);
        return Code128Layout.CreateSymbol(encoded, payload, null);
    }

    static BarcodeSymbol CreateDataMatrixSymbol(string text)
    {
        byte[] bytes = global::System.Text.Encoding.ASCII.GetBytes(text);
        return DataMatrixEncoder.Encode(
            bytes,
            CreatePayload(text, bytes),
            null,
            null,
            false).Symbol;
    }

    static BarcodeSymbol CreateGs1128Symbol()
    {
        return Gs1128Codec.EncodeElements(
            new Gs1Element[] { CreateGs1Element() },
            null,
            null).Symbol;
    }

    static BarcodeSymbol CreateGs1DataMatrixSymbol()
    {
        return Gs1DataMatrixCodec.EncodeElements(
            new Gs1Element[] { CreateGs1Element() },
            null,
            null).Symbol;
    }

    static Gs1Element CreateGs1Element()
    {
        Gs1Element element = new Gs1Element();
        element.ApplicationIdentifier = "01";
        element.Value = "09506000134352";
        element.VariableLength = false;
        return element;
    }

    static BarcodeSymbol CreateEanUpcSymbol(string digits, BarcodeFormat format)
    {
        EanUpcAddonAssociation association = new EanUpcAddonAssociation();
        association.Main = EanUpcEncoder.Encode(
            digits,
            format,
            BarcodeChecksumMode.Calculate);
        byte[] bytes = global::System.Text.Encoding.ASCII.GetBytes(
            association.Main.Text);
        return EanUpcLayout.CreateSymbol(
            association,
            CreatePayload(association.Main.Text, bytes),
            null,
            null);
    }

    static BarcodeSymbol CreateUpcESymbol(string digits)
    {
        EanUpcAddonAssociation association = new EanUpcAddonAssociation();
        association.Main = EanUpcEncoder.EncodeUpcE(
            digits,
            0,
            BarcodeChecksumMode.Calculate);
        byte[] bytes = global::System.Text.Encoding.ASCII.GetBytes(
            association.Main.Text);
        return EanUpcLayout.CreateSymbol(
            association,
            CreatePayload(association.Main.Text, bytes),
            null,
            null);
    }

    static BarcodeSymbol CreateAddOnSymbol(string digits)
    {
        EanUpcEncodedData encoded = EanUpcAddon.Encode(digits);
        BarcodeSymbol symbol = new BarcodeSymbol();
        symbol.Format = encoded.Format;
        symbol.Dimension = BarcodeDimension.Linear;
        symbol.WidthModules = encoded.Modules.Length;
        symbol.HeightModules = 1;
        symbol.Modules = (byte[])encoded.Modules.Clone();
        symbol.Payload = CreatePayload(
            encoded.Text,
            global::System.Text.Encoding.ASCII.GetBytes(encoded.Text));
        return symbol;
    }

    static BarcodeSymbol CreatePdf417Symbol(string text)
    {
        byte[] bytes = global::System.Text.Encoding.ASCII.GetBytes(text);
        Pdf417Options options = new Pdf417Options();
        options.Columns = 3;
        options.Rows = 6;
        options.ErrorCorrectionLevel = 2;
        options.CompactionMode = Pdf417CompactionMode.Text;
        return Pdf417Encoder.Encode(
            bytes,
            CreatePayload(text, bytes),
            options,
            null).Symbol;
    }

    static BarcodeSymbol CreatePdf417Symbol(string text, BarcodeFormat format)
    {
        byte[] bytes = global::System.Text.Encoding.ASCII.GetBytes(text);
        Pdf417Options options = new Pdf417Options();
        options.Columns = 3;
        options.Rows = 6;
        options.ErrorCorrectionLevel = 2;
        options.CompactionMode = Pdf417CompactionMode.Text;
        if (format == BarcodeFormat.CompactPdf417)
        {
            options.Variant = Pdf417Variant.Compact;
        }
        else if (format == BarcodeFormat.MacroPdf417)
        {
            StructuredAppendInfo macro = new StructuredAppendInfo();
            macro.Format = BarcodeFormat.MacroPdf417;
            macro.Index = 0;
            macro.Count = 1;
            macro.FileIdentifier = "017053";
            macro.IsLastSegment = true;
            options.Macro = macro;
            options.Rows = 0;
        }

        return Pdf417Encoder.Encode(
            bytes,
            CreatePayload(text, bytes),
            options,
            null).Symbol;
    }

    static BarcodePayload CreatePayload(string text, byte[] bytes)
    {
        BarcodePayload payload = new BarcodePayload();
        payload.Text = text;
        payload.RawBytes = (byte[])bytes.Clone();
        return payload;
    }

    static BarcodeImage DrawSymbolImage(
        BarcodeSymbol symbol,
        int scale,
        int left,
        int top,
        int linearHeight)
    {
        int symbolHeight = symbol.Dimension == BarcodeDimension.Linear
            ? linearHeight
            : symbol.HeightModules * scale;
        int width = left + (symbol.WidthModules * scale) + (12 * scale);
        int height = top + symbolHeight + 12;
        byte[] pixels = new byte[width * height];
        int index = 0;
        while (index < pixels.Length)
        {
            pixels[index] = 240;
            index = index + 1;
        }

        if (symbol.Dimension == BarcodeDimension.Linear)
        {
            DrawLinearSymbol(pixels, width, symbol, scale, left, top, linearHeight);
        }
        else
        {
            DrawMatrixSymbol(pixels, width, symbol, scale, left, top);
        }

        BarcodeImage image = new BarcodeImage();
        image.Pixels = pixels;
        image.Width = width;
        image.Height = height;
        image.Stride = width;
        image.PixelFormat = BarcodePixelFormat.Gray8;
        return image;
    }

    static void DrawLinearSymbol(
        byte[] pixels,
        int stride,
        BarcodeSymbol symbol,
        int scale,
        int left,
        int top,
        int height)
    {
        int y = top;
        while (y < top + height)
        {
            int module = 0;
            while (module < symbol.WidthModules)
            {
                if (symbol.Modules![module] != 0)
                {
                    int repeat = 0;
                    while (repeat < scale)
                    {
                        pixels[(y * stride) + left + (module * scale) + repeat] = 20;
                        repeat = repeat + 1;
                    }
                }

                module = module + 1;
            }

            y = y + 1;
        }
    }

    static void DrawMatrixSymbol(
        byte[] pixels,
        int stride,
        BarcodeSymbol symbol,
        int scale,
        int left,
        int top)
    {
        int y = 0;
        while (y < symbol.HeightModules)
        {
            int x = 0;
            while (x < symbol.WidthModules)
            {
                if (symbol.Modules![(y * symbol.WidthModules) + x] != 0)
                {
                    int repeatY = 0;
                    while (repeatY < scale)
                    {
                        int repeatX = 0;
                        while (repeatX < scale)
                        {
                            int outputX = left + (x * scale) + repeatX;
                            int outputY = top + (y * scale) + repeatY;
                            pixels[(outputY * stride) + outputX] = 20;
                            repeatX = repeatX + 1;
                        }

                        repeatY = repeatY + 1;
                    }
                }

                x = x + 1;
            }

            y = y + 1;
        }
    }

    static BarcodeDiagnostic FindDiagnostic(
        BarcodeDiagnostic[] diagnostics,
        string code)
    {
        int index = 0;
        while (index < diagnostics.Length)
        {
            if (diagnostics[index].Code == code)
            {
                return diagnostics[index];
            }

            index = index + 1;
        }

        throw new global::System.InvalidOperationException(
            "The expected diagnostic was not present.");
    }
}
