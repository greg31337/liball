namespace barcode.Tests;

public class Pdf417ImageFinderTests
{
    [Test]
    public void ScaledStandardSymbolIsLocalizedFromRepeatedGuardLines()
    {
        Pdf417EncodingResult encoded = CreateEncoded();
        int scale = 2;
        int left = 12;
        int top = 10;
        ModuleMatrix image = new ModuleMatrix(
            (encoded.Symbol.WidthModules * scale) + 30,
            (encoded.Symbol.HeightModules * scale) + 28);
        DrawScaledSymbol(image, encoded.Symbol, left, top, scale);

        Pdf417FinderEvidence? found = Pdf417ImageFinder.Locate(
            image,
            BarcodePolarity.DarkOnLight);

        Assert.That(found, Is.Not.Null);
        Assert.Multiple(() =>
        {
            Assert.That(found!.EstimatedColumnCount,
                Is.EqualTo(encoded.SymbolPlan.Columns));
            Assert.That(found.AngleDegrees,
                Is.EqualTo(0.0).Within(0.75));
            Assert.That(found.RowEvidenceCount,
                Is.GreaterThanOrEqualTo(encoded.Symbol.HeightModules * scale - 2));
            Assert.That(found.StartLineScore, Is.GreaterThanOrEqualTo(0.95));
            Assert.That(found.StopLineScore, Is.GreaterThanOrEqualTo(0.95));
            Assert.That(found.Confidence, Is.GreaterThanOrEqualTo(0.90));
            Assert.That(found.Location.TopLeft!.X,
                Is.EqualTo(left - 0.5).Within(1.0));
            Assert.That(found.Location.TopLeft.Y,
                Is.EqualTo(top - 0.5).Within(1.0));
            Assert.That(found.Location.BottomRight!.X,
                Is.EqualTo(left + (encoded.Symbol.WidthModules * scale) - 0.5)
                    .Within(1.0));
            Assert.That(found.Location.BottomRight.Y,
                Is.EqualTo(top + (encoded.Symbol.HeightModules * scale) - 0.5)
                    .Within(1.0));
            Assert.That(found.Candidate.Format,
                Is.EqualTo(BarcodeFormat.Pdf417));
            Assert.That(found.Candidate.Dimension,
                Is.EqualTo(BarcodeDimension.StackedLinear));
        });
    }

    [Test]
    public void ArbitrarilyRotatedSymbolReportsAngleAndFittedQuadrilateral()
    {
        Pdf417EncodingResult encoded = CreateEncoded();
        int scale = 2;
        double angle = 27.0;
        BarcodePoint origin = GeometryMath.CreatePoint(36.0, 18.0);
        ModuleMatrix image = new ModuleMatrix(260, 180);
        BarcodeQuadrilateral actual = DrawRotatedSymbol(
            image,
            encoded.Symbol,
            origin,
            scale,
            angle);

        Pdf417FinderEvidence? found = Pdf417ImageFinder.Locate(
            image,
            BarcodePolarity.DarkOnLight);

        Assert.That(found, Is.Not.Null);
        Assert.Multiple(() =>
        {
            Assert.That(found!.AngleDegrees, Is.EqualTo(angle).Within(1.25));
            Assert.That(found.EstimatedColumnCount,
                Is.EqualTo(encoded.SymbolPlan.Columns));
            Assert.That(found.Location.TopLeft!.X,
                Is.EqualTo(actual.TopLeft!.X).Within(3.0));
            Assert.That(found.Location.TopLeft.Y,
                Is.EqualTo(actual.TopLeft.Y).Within(3.0));
            Assert.That(found.Location.TopRight!.X,
                Is.EqualTo(actual.TopRight!.X).Within(3.0));
            Assert.That(found.Location.TopRight.Y,
                Is.EqualTo(actual.TopRight.Y).Within(3.0));
            Assert.That(found.Location.BottomLeft!.X,
                Is.EqualTo(actual.BottomLeft!.X).Within(3.0));
            Assert.That(found.Location.BottomLeft.Y,
                Is.EqualTo(actual.BottomLeft.Y).Within(3.0));
            Assert.That(found.LineCoverage, Is.GreaterThanOrEqualTo(0.80));
        });
    }

    [Test]
    public void QuadrilateralEvidenceRefinesAnApproximateRegion()
    {
        Pdf417EncodingResult encoded = CreateEncoded();
        int scale = 2;
        int left = 10;
        int top = 12;
        ModuleMatrix image = new ModuleMatrix(
            (encoded.Symbol.WidthModules * scale) + 35,
            (encoded.Symbol.HeightModules * scale) + 35);
        DrawScaledSymbol(image, encoded.Symbol, left, top, scale);
        BarcodeQuadrilateral actual = CreateScaledBounds(
            encoded.Symbol,
            left,
            top,
            scale);
        BarcodeQuadrilateral estimate = GeometryMath.CreateQuadrilateral(
            GeometryMath.CreatePoint(actual.TopLeft!.X - 2.0, actual.TopLeft.Y - 1.0),
            GeometryMath.CreatePoint(actual.TopRight!.X + 3.0, actual.TopRight.Y - 2.0),
            GeometryMath.CreatePoint(actual.BottomRight!.X + 2.0, actual.BottomRight.Y + 2.0),
            GeometryMath.CreatePoint(actual.BottomLeft!.X - 2.0, actual.BottomLeft.Y + 3.0));

        Pdf417FinderEvidence? exact = Pdf417ImageFinder.EvaluateQuadrilateral(
            image,
            actual,
            BarcodePolarity.DarkOnLight);
        Pdf417FinderEvidence measured = Pdf417ImageFinder.MeasureQuadrilateral(
            image,
            estimate,
            BarcodePolarity.DarkOnLight);
        Pdf417FinderEvidence? refined = Pdf417ImageFinder.RefineQuadrilateral(
            image,
            estimate,
            BarcodePolarity.DarkOnLight,
            10);

        Assert.That(exact, Is.Not.Null);
        Assert.That(refined, Is.Not.Null);
        Assert.Multiple(() =>
        {
            Assert.That(refined!.Confidence,
                Is.GreaterThanOrEqualTo(measured.Confidence));
            Assert.That(refined.EstimatedColumnCount,
                Is.EqualTo(encoded.SymbolPlan.Columns));
            Assert.That(refined.StartLineScore, Is.GreaterThanOrEqualTo(0.80));
            Assert.That(refined.StopLineScore, Is.GreaterThanOrEqualTo(0.80));
            Assert.That(refined.Location.TopLeft!.X,
                Is.EqualTo(actual.TopLeft.X).Within(4.0));
            Assert.That(refined.Location.BottomRight!.Y,
                Is.EqualTo(actual.BottomRight.Y).Within(4.0));
        });
    }

    [Test]
    public void PartialGuardDamageRetainsEvidenceButGuardlessShapesAreRejected()
    {
        Pdf417EncodingResult encoded = CreateEncoded();
        int scale = 2;
        int left = 10;
        int top = 8;
        ModuleMatrix damaged = new ModuleMatrix(
            (encoded.Symbol.WidthModules * scale) + 30,
            (encoded.Symbol.HeightModules * scale) + 30);
        DrawScaledSymbol(damaged, encoded.Symbol, left, top, scale);
        int stopStart = left
            + ((encoded.Symbol.WidthModules
                - Pdf417CodewordTable.StopPatternModuleCount) * scale);
        int damagedRow = top;
        while (damagedRow < top + 6)
        {
            int column = stopStart;
            while (column < stopStart + (4 * scale))
            {
                damaged.SetModule(column, damagedRow, false);
                column++;
            }

            damagedRow++;
        }

        ModuleMatrix box = new ModuleMatrix(damaged.Width, damaged.Height);
        DrawBox(box, 10, 10, encoded.Symbol.WidthModules * scale, 25, 2);

        Pdf417FinderEvidence? damagedEvidence = Pdf417ImageFinder.Locate(
            damaged,
            BarcodePolarity.DarkOnLight);
        Pdf417FinderEvidence? boxEvidence = Pdf417ImageFinder.Locate(
            box,
            BarcodePolarity.DarkOnLight);

        Assert.Multiple(() =>
        {
            Assert.That(damagedEvidence, Is.Not.Null);
            Assert.That(damagedEvidence!.RowEvidenceCount,
                Is.LessThan(encoded.Symbol.HeightModules * scale));
            Assert.That(damagedEvidence.EstimatedColumnCount,
                Is.EqualTo(encoded.SymbolPlan.Columns));
            Assert.That(boxEvidence, Is.Null);
        });
    }

    [Test]
    public void InvalidFinderInputsAreRejected()
    {
        ModuleMatrix image = new ModuleMatrix(30, 30);
        ModuleMatrix small = new ModuleMatrix(7, 30);
        BarcodeQuadrilateral quadrilateral = GeometryMath.CreateQuadrilateral(
            GeometryMath.CreatePoint(2.0, 2.0),
            GeometryMath.CreatePoint(25.0, 2.0),
            GeometryMath.CreatePoint(25.0, 20.0),
            GeometryMath.CreatePoint(2.0, 20.0));

        Assert.Multiple(() =>
        {
            Assert.That(
                () => Pdf417ImageFinder.Locate(null!, BarcodePolarity.DarkOnLight),
                Throws.TypeOf<BarcodeValidationException>());
            Assert.That(
                () => Pdf417ImageFinder.Locate(small, BarcodePolarity.DarkOnLight),
                Throws.TypeOf<BarcodeValidationException>());
            Assert.That(
                () => Pdf417ImageFinder.Locate(image, (BarcodePolarity)99),
                Throws.TypeOf<BarcodeValidationException>());
            Assert.That(
                () => Pdf417ImageFinder.EvaluateQuadrilateral(
                    image,
                    null!,
                    BarcodePolarity.DarkOnLight),
                Throws.TypeOf<BarcodeValidationException>());
            Assert.That(
                () => Pdf417ImageFinder.RefineQuadrilateral(
                    image,
                    quadrilateral,
                    BarcodePolarity.DarkOnLight,
                    -1),
                Throws.TypeOf<BarcodeValidationException>());
            Assert.That(
                () => Pdf417ImageFinder.RefineQuadrilateral(
                    image,
                    quadrilateral,
                    BarcodePolarity.DarkOnLight,
                    25),
                Throws.TypeOf<BarcodeValidationException>());
        });
    }

    static Pdf417EncodingResult CreateEncoded()
    {
        byte[] data = Bytes("LOCATE");
        BarcodePayload payload = new BarcodePayload();
        payload.RawBytes = (byte[])data.Clone();
        Pdf417Options options = new Pdf417Options();
        options.Columns = 2;
        options.ErrorCorrectionLevel = 2;
        return Pdf417Encoder.Encode(data, payload, options, null);
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

    static BarcodeQuadrilateral DrawRotatedSymbol(
        ModuleMatrix image,
        BarcodeSymbol symbol,
        BarcodePoint origin,
        int scale,
        double angleDegrees)
    {
        double radians = angleDegrees * global::System.Math.PI / 180.0;
        double cosine = global::System.Math.Cos(radians);
        double sine = global::System.Math.Sin(radians);
        int width = symbol.WidthModules * scale;
        int height = symbol.HeightModules * scale;
        int y = 0;
        while (y < image.Height)
        {
            int x = 0;
            while (x < image.Width)
            {
                double offsetX = (x + 0.5) - origin.X;
                double offsetY = (y + 0.5) - origin.Y;
                double sourceX = (offsetX * cosine) + (offsetY * sine);
                double sourceY = (-offsetX * sine) + (offsetY * cosine);
                if (sourceX >= 0.0
                    && sourceX < width
                    && sourceY >= 0.0
                    && sourceY < height)
                {
                    int moduleColumn = (int)(sourceX / scale);
                    int moduleRow = (int)(sourceY / scale);
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

        BarcodePoint topLeft = TransformPoint(
            origin,
            -0.5,
            -0.5,
            cosine,
            sine);
        BarcodePoint topRight = TransformPoint(
            origin,
            width - 0.5,
            -0.5,
            cosine,
            sine);
        BarcodePoint bottomRight = TransformPoint(
            origin,
            width - 0.5,
            height - 0.5,
            cosine,
            sine);
        BarcodePoint bottomLeft = TransformPoint(
            origin,
            -0.5,
            height - 0.5,
            cosine,
            sine);
        return GeometryMath.CreateQuadrilateral(
            topLeft,
            topRight,
            bottomRight,
            bottomLeft);
    }

    static BarcodePoint TransformPoint(
        BarcodePoint origin,
        double x,
        double y,
        double cosine,
        double sine)
    {
        return GeometryMath.CreatePoint(
            origin.X + (x * cosine) - (y * sine),
            origin.Y + (x * sine) + (y * cosine));
    }

    static BarcodeQuadrilateral CreateScaledBounds(
        BarcodeSymbol symbol,
        int left,
        int top,
        int scale)
    {
        double right = left + (symbol.WidthModules * scale) - 0.5;
        double bottom = top + (symbol.HeightModules * scale) - 0.5;
        return GeometryMath.CreateQuadrilateral(
            GeometryMath.CreatePoint(left - 0.5, top - 0.5),
            GeometryMath.CreatePoint(right, top - 0.5),
            GeometryMath.CreatePoint(right, bottom),
            GeometryMath.CreatePoint(left - 0.5, bottom));
    }

    static void DrawBox(
        ModuleMatrix image,
        int left,
        int top,
        int width,
        int height,
        int thickness)
    {
        int y = top;
        while (y < top + height)
        {
            int x = left;
            while (x < left + width)
            {
                if (x < left + thickness
                    || x >= left + width - thickness
                    || y < top + thickness
                    || y >= top + height - thickness)
                {
                    image.SetModule(x, y, true);
                }

                x++;
            }

            y++;
        }
    }

    static byte[] Bytes(string text)
    {
        return global::System.Text.Encoding.Latin1.GetBytes(text);
    }
}
