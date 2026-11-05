namespace barcode.Tests;

public class Pdf417ProjectiveDecoderTests
{
    [Test]
    public void PerspectiveQuadrilateralRowsDecodeThroughTheCompletePipeline()
    {
        Pdf417EncodingResult encoded = CreateEncoded(
            "PERSPECTIVE",
            Pdf417Variant.Standard);
        ModuleMatrix image = new ModuleMatrix(270, 105);
        BarcodeQuadrilateral location = GeometryMath.CreateQuadrilateral(
            GeometryMath.CreatePoint(24.0, 18.0),
            GeometryMath.CreatePoint(238.0, 12.0),
            GeometryMath.CreatePoint(226.0, 82.0),
            GeometryMath.CreatePoint(35.0, 76.0));
        DrawPerspectiveSymbol(image, encoded.Symbol, location);
        Pdf417FinderEvidence evidence = CreateEvidence(
            encoded,
            location,
            BarcodePolarity.DarkOnLight);

        Pdf417ImageDecodeResult decoded =
            Pdf417ProjectiveDecoder.DecodeCandidate(
                image,
                evidence,
                null);

        Assert.Multiple(() =>
        {
            Assert.That(decoded.Decode.Result.Payload?.Text,
                Is.EqualTo("PERSPECTIVE"));
            Assert.That(decoded.Decode.Result.Payload?.RawBytes,
                Is.EqualTo(Bytes("PERSPECTIVE")));
            Assert.That(decoded.Decode.Result.Format,
                Is.EqualTo(BarcodeFormat.Pdf417));
            Assert.That(decoded.Decode.Result.Location?.TopLeft?.X,
                Is.EqualTo(location.TopLeft!.X));
            Assert.That(decoded.SampledRows.Length,
                Is.GreaterThanOrEqualTo(encoded.SymbolPlan.Rows));
            Assert.That(decoded.Decode.CorrectedCodewords,
                Is.EqualTo(encoded.SymbolCodewords));
            Assert.That(decoded.MirrorMode,
                Is.EqualTo(BarcodeMirrorMode.Normal));
        });
    }

    [Test]
    public void AutomaticPolarityDecodesAnInvertedSymbol()
    {
        Pdf417EncodingResult encoded = CreateEncoded(
            "INVERTED",
            Pdf417Variant.Standard);
        ModuleMatrix image = CreateScaledImage(encoded.Symbol, 10, 9, 2, 28);
        ModuleMatrix inverted = InvertMatrix(image);
        Pdf417ImageDecodeResult[] decoded =
            Pdf417ProjectiveDecoder.DecodeImage(inverted, null);

        Assert.Multiple(() =>
        {
            Assert.That(decoded, Has.Length.EqualTo(1));
            Assert.That(decoded[0].Decode.Result.Payload?.Text,
                Is.EqualTo("INVERTED"));
            Assert.That(decoded[0].FinderEvidence.Polarity,
                Is.EqualTo(BarcodePolarity.LightOnDark));
            Assert.That(decoded[0].Decode.Result.Polarity,
                Is.EqualTo(BarcodePolarity.LightOnDark));
        });
    }

    [Test]
    public void MirroredSymbolIsRecoveredWhenMirrorAttemptsAreEnabled()
    {
        Pdf417EncodingResult encoded = CreateEncoded(
            "MIRRORED",
            Pdf417Variant.Standard);
        ModuleMatrix image = CreateScaledImage(encoded.Symbol, 11, 8, 2, 28);
        ModuleMatrix mirrored = image.ReflectHorizontal();
        Pdf417ImageDecodeResult[] decoded =
            Pdf417ProjectiveDecoder.DecodeImage(mirrored, null);

        Assert.Multiple(() =>
        {
            Assert.That(decoded, Has.Length.EqualTo(1));
            Assert.That(decoded[0].Decode.Result.Payload?.Text,
                Is.EqualTo("MIRRORED"));
            Assert.That(decoded[0].MirrorMode,
                Is.EqualTo(BarcodeMirrorMode.Mirrored));
            Assert.That(decoded[0].Decode.Result.MirrorMode,
                Is.EqualTo(BarcodeMirrorMode.Mirrored));
        });
    }

    [Test]
    public void MultipleSymbolsAreMaskedDecodedAndReturnedInReadingOrder()
    {
        Pdf417EncodingResult first = CreateEncoded(
            "FIRST",
            Pdf417Variant.Standard);
        Pdf417EncodingResult second = CreateEncoded(
            "SECOND",
            Pdf417Variant.Standard);
        int scale = 2;
        int width = global::System.Math.Max(
            first.Symbol.WidthModules,
            second.Symbol.WidthModules) * scale + 50;
        ModuleMatrix image = new ModuleMatrix(width, 145);
        DrawScaledSymbol(image, first.Symbol, 18, 10, scale);
        DrawScaledSymbol(image, second.Symbol, 7, 82, scale);
        BarcodeDecodeOptions options = new BarcodeDecodeOptions();
        options.MaximumResults = 4;

        Pdf417ImageDecodeResult[] decoded =
            Pdf417ProjectiveDecoder.DecodeImage(image, options);

        Assert.Multiple(() =>
        {
            Assert.That(decoded, Has.Length.EqualTo(2));
            Assert.That(decoded[0].Decode.Result.Payload?.Text,
                Is.EqualTo("FIRST"));
            Assert.That(decoded[1].Decode.Result.Payload?.Text,
                Is.EqualTo("SECOND"));
            Assert.That(
                GeometryMath.CalculateQuadrilateralBounds(
                    decoded[0].FinderEvidence.Location).Y,
                Is.LessThan(
                    GeometryMath.CalculateQuadrilateralBounds(
                        decoded[1].FinderEvidence.Location).Y));
        });
    }

    [Test]
    public void CompactTruncatedSideIsLocalizedSampledAndDecoded()
    {
        Pdf417EncodingResult encoded = CreateEncoded(
            "COMPACT",
            Pdf417Variant.Compact);
        ModuleMatrix image = CreateScaledImage(encoded.Symbol, 12, 10, 2, 30);
        BarcodeDecodeOptions options = new BarcodeDecodeOptions();
        options.AllowedFormats = new BarcodeFormat[]
        {
            BarcodeFormat.CompactPdf417
        };

        Pdf417ImageDecodeResult[] decoded =
            Pdf417ProjectiveDecoder.DecodeImage(image, options);

        Assert.Multiple(() =>
        {
            Assert.That(decoded, Has.Length.EqualTo(1));
            Assert.That(decoded[0].FinderEvidence.Variant,
                Is.EqualTo(Pdf417Variant.Compact));
            Assert.That(decoded[0].FinderEvidence.EstimatedColumnCount,
                Is.EqualTo(encoded.SymbolPlan.Columns));
            Assert.That(decoded[0].Decode.Result.Format,
                Is.EqualTo(BarcodeFormat.CompactPdf417));
            Assert.That(decoded[0].Decode.Result.Payload?.Text,
                Is.EqualTo("COMPACT"));
            Assert.That(decoded[0].Decode.CorrectedCodewords,
                Is.EqualTo(encoded.SymbolCodewords));
        });
    }

    [Test]
    public void ResultAndTransformFiltersAreApplied()
    {
        Pdf417EncodingResult encoded = CreateEncoded(
            "FILTER",
            Pdf417Variant.Standard);
        ModuleMatrix image = CreateScaledImage(encoded.Symbol, 10, 8, 2, 26);
        BarcodeDecodeOptions wrongLength = new BarcodeDecodeOptions();
        wrongLength.AllowedLengths = new int[] { 2 };
        BarcodeDecodeOptions wrongFormat = new BarcodeDecodeOptions();
        wrongFormat.AllowedFormats = new BarcodeFormat[]
        {
            BarcodeFormat.CompactPdf417
        };
        BarcodeDecodeOptions noResults = new BarcodeDecodeOptions();
        noResults.MaximumResults = 1;

        Pdf417ImageDecodeResult[] lengthFiltered =
            Pdf417ProjectiveDecoder.TryDecodeImage(image, wrongLength);
        Pdf417ImageDecodeResult[] formatFiltered =
            Pdf417ProjectiveDecoder.TryDecodeImage(image, wrongFormat);
        Pdf417ImageDecodeResult[] limited =
            Pdf417ProjectiveDecoder.TryDecodeImage(image, noResults);

        Assert.Multiple(() =>
        {
            Assert.That(lengthFiltered, Is.Empty);
            Assert.That(formatFiltered, Is.Empty);
            Assert.That(limited, Has.Length.EqualTo(1));
        });
    }

    [Test]
    public void InvalidProjectiveInputsAreRejected()
    {
        ModuleMatrix image = new ModuleMatrix(30, 30);
        BarcodeDecodeOptions invalidCount = new BarcodeDecodeOptions();
        invalidCount.MaximumResults = -1;
        BarcodeDecodeOptions invalidMirror = new BarcodeDecodeOptions();
        invalidMirror.MirrorMode = (BarcodeMirrorMode)99;
        Pdf417FinderEvidence evidence = new Pdf417FinderEvidence();

        Assert.Multiple(() =>
        {
            Assert.That(
                () => Pdf417ProjectiveDecoder.DecodeImage(null!, null),
                Throws.TypeOf<BarcodeValidationException>());
            Assert.That(
                () => Pdf417ProjectiveDecoder.TryDecodeImage(
                    image,
                    invalidCount),
                Throws.TypeOf<BarcodeValidationException>());
            Assert.That(
                () => Pdf417ProjectiveDecoder.TryDecodeImage(
                    image,
                    invalidMirror),
                Throws.TypeOf<BarcodeValidationException>());
            Assert.That(
                () => Pdf417ProjectiveDecoder.DecodeCandidate(
                    image,
                    null!,
                    null),
                Throws.TypeOf<BarcodeValidationException>());
            Assert.That(
                () => Pdf417ProjectiveDecoder.DecodeCandidate(
                    image,
                    evidence,
                    null),
                Throws.TypeOf<BarcodeValidationException>());
        });
    }

    static Pdf417EncodingResult CreateEncoded(
        string text,
        Pdf417Variant variant)
    {
        byte[] data = Bytes(text);
        BarcodePayload payload = new BarcodePayload();
        payload.Text = text;
        payload.RawBytes = (byte[])data.Clone();
        Pdf417Options options = new Pdf417Options();
        options.Columns = 2;
        options.ErrorCorrectionLevel = 2;
        options.Variant = variant;
        return Pdf417Encoder.Encode(data, payload, options, null);
    }

    static ModuleMatrix CreateScaledImage(
        BarcodeSymbol symbol,
        int left,
        int top,
        int scale,
        int margin)
    {
        ModuleMatrix image = new ModuleMatrix(
            left + (symbol.WidthModules * scale) + margin,
            top + (symbol.HeightModules * scale) + margin);
        DrawScaledSymbol(image, symbol, left, top, scale);
        return image;
    }

    static void DrawScaledSymbol(
        ModuleMatrix image,
        BarcodeSymbol symbol,
        int left,
        int top,
        int scale)
    {
        int row = 0;
        while (row < symbol.HeightModules)
        {
            int column = 0;
            while (column < symbol.WidthModules)
            {
                if (symbol.Modules![(row * symbol.WidthModules) + column] != 0)
                {
                    int pixelY = 0;
                    while (pixelY < scale)
                    {
                        int pixelX = 0;
                        while (pixelX < scale)
                        {
                            image.SetModule(
                                left + (column * scale) + pixelX,
                                top + (row * scale) + pixelY,
                                true);
                            pixelX++;
                        }

                        pixelY++;
                    }
                }

                column++;
            }

            row++;
        }
    }

    static void DrawPerspectiveSymbol(
        ModuleMatrix image,
        BarcodeSymbol symbol,
        BarcodeQuadrilateral destination)
    {
        BarcodeQuadrilateral source = GeometryMath.CreateQuadrilateral(
            GeometryMath.CreatePoint(0.0, 0.0),
            GeometryMath.CreatePoint(symbol.WidthModules, 0.0),
            GeometryMath.CreatePoint(
                symbol.WidthModules,
                symbol.HeightModules),
            GeometryMath.CreatePoint(0.0, symbol.HeightModules));
        PerspectiveTransform imageToSymbol =
            PerspectiveTransform.CreateQuadrilateralMapping(
                destination,
                source);
        BarcodeRectangle bounds = GeometryMath.CalculateQuadrilateralBounds(
            destination);
        int top = (int)global::System.Math.Floor(bounds.Y);
        int bottom = (int)global::System.Math.Ceiling(
            bounds.Y + bounds.Height);
        int y = top;
        while (y <= bottom)
        {
            int left = (int)global::System.Math.Floor(bounds.X);
            int right = (int)global::System.Math.Ceiling(
                bounds.X + bounds.Width);
            int x = left;
            while (x <= right)
            {
                BarcodePoint imagePoint = GeometryMath.CreatePoint(
                    x + 0.5,
                    y + 0.5);
                BarcodePoint symbolPoint = imageToSymbol.TransformPoint(
                    imagePoint);
                if (symbolPoint.X >= 0.0
                    && symbolPoint.X < symbol.WidthModules
                    && symbolPoint.Y >= 0.0
                    && symbolPoint.Y < symbol.HeightModules)
                {
                    int moduleColumn = (int)symbolPoint.X;
                    int moduleRow = (int)symbolPoint.Y;
                    if (symbol.Modules![(moduleRow * symbol.WidthModules)
                        + moduleColumn] != 0)
                    {
                        image.SetModule(x, y, true);
                    }
                }

                x++;
            }

            y++;
        }
    }

    static Pdf417FinderEvidence CreateEvidence(
        Pdf417EncodingResult encoded,
        BarcodeQuadrilateral location,
        BarcodePolarity polarity)
    {
        BarcodePoint topLeft = location.TopLeft!;
        BarcodePoint topRight = location.TopRight!;
        BarcodePoint bottomLeft = location.BottomLeft!;
        BarcodePoint bottomRight = location.BottomRight!;
        double topWidth = GeometryMath.DistanceBetweenPoints(
            topLeft,
            topRight);
        double bottomWidth = GeometryMath.DistanceBetweenPoints(
            bottomLeft,
            bottomRight);
        Pdf417FinderEvidence evidence = new Pdf417FinderEvidence();
        evidence.Location = location;
        evidence.Polarity = polarity;
        evidence.Variant = encoded.SymbolPlan.Variant;
        evidence.ModuleSize = ((topWidth + bottomWidth) * 0.5)
            / encoded.Symbol.WidthModules;
        evidence.EstimatedColumnCount = encoded.SymbolPlan.Columns;
        evidence.Confidence = 1.0;
        evidence.AngleDegrees = NormalizeAngle(
            global::System.Math.Atan2(
                topRight.Y - topLeft.Y,
                topRight.X - topLeft.X)
                * 180.0 / global::System.Math.PI);
        return evidence;
    }

    static ModuleMatrix InvertMatrix(ModuleMatrix source)
    {
        ModuleMatrix inverted = new ModuleMatrix(source.Width, source.Height);
        int y = 0;
        while (y < source.Height)
        {
            int x = 0;
            while (x < source.Width)
            {
                inverted.SetModule(x, y, !source.GetModule(x, y));
                x++;
            }

            y++;
        }

        return inverted;
    }

    static double NormalizeAngle(double angle)
    {
        double normalized = angle;
        while (normalized < 0.0)
        {
            normalized = normalized + 360.0;
        }

        while (normalized >= 360.0)
        {
            normalized = normalized - 360.0;
        }

        return normalized;
    }

    static byte[] Bytes(string text)
    {
        return global::System.Text.Encoding.Latin1.GetBytes(text);
    }
}
