namespace barcode.Tests;

public class DataMatrixImageFinderTests
{
    [Test]
    public void ScaledSymbolIsLocalizedFromItsLBorderAndClockTracks()
    {
        DataMatrixEncodingResult encoded = EncodeVersion(1, "A");
        ModuleMatrix image = new ModuleMatrix(50, 45);
        DrawScaled(image, encoded.SymbolMatrix, 7, 6, 3);

        DataMatrixFinderEvidence[] found = DataMatrixImageFinder.Locate(
            image,
            BarcodePolarity.DarkOnLight);

        Assert.That(found, Has.Length.EqualTo(1));
        DataMatrixFinderEvidence evidence = found[0];
        Assert.Multiple(() =>
        {
            Assert.That(evidence.SymbolInfo.Version, Is.EqualTo(1));
            Assert.That(evidence.Rotation, Is.EqualTo(BarcodeRotation.None));
            Assert.That(evidence.Polarity, Is.EqualTo(BarcodePolarity.DarkOnLight));
            Assert.That(evidence.SolidBorderScore, Is.EqualTo(1.0));
            Assert.That(evidence.ClockTrackScore, Is.EqualTo(1.0));
            Assert.That(evidence.Confidence, Is.GreaterThanOrEqualTo(0.99));
            Assert.That(evidence.Location.TopLeft!.X, Is.EqualTo(7.0));
            Assert.That(evidence.Location.TopLeft.Y, Is.EqualTo(6.0));
            Assert.That(evidence.Location.BottomRight!.X, Is.EqualTo(36.0));
            Assert.That(evidence.Location.BottomRight.Y, Is.EqualTo(35.0));
            Assert.That(evidence.Candidate.Format, Is.EqualTo(BarcodeFormat.DataMatrix));
            Assert.That(evidence.Candidate.Dimension, Is.EqualTo(BarcodeDimension.Matrix));
            Assert.That(evidence.Candidate.AngleDegrees, Is.EqualTo(0.0));
        });
    }

    [Test]
    public void EveryRightAngleRotationOfARectangularSymbolIsIdentified()
    {
        DataMatrixEncodingResult encoded = EncodeVersion(25, "R");
        ModuleMatrix[] symbols = new ModuleMatrix[]
        {
            CopyMatrix(encoded.SymbolMatrix),
            encoded.SymbolMatrix.RotateClockwise90(),
            encoded.SymbolMatrix.Rotate180(),
            encoded.SymbolMatrix.RotateClockwise270()
        };
        BarcodeRotation[] rotations = new BarcodeRotation[]
        {
            BarcodeRotation.None,
            BarcodeRotation.Clockwise90,
            BarcodeRotation.Clockwise180,
            BarcodeRotation.Clockwise270
        };

        int index = 0;
        while (index < symbols.Length)
        {
            ModuleMatrix image = new ModuleMatrix(80, 80);
            DrawScaled(image, symbols[index], 8, 9, 3);
            DataMatrixFinderEvidence[] found = DataMatrixImageFinder.Locate(
                image,
                BarcodePolarity.DarkOnLight);

            Assert.That(found, Has.Length.EqualTo(1), $"Rotation {rotations[index]}");
            Assert.Multiple(() =>
            {
                Assert.That(found[0].SymbolInfo.Version, Is.EqualTo(25));
                Assert.That(found[0].Rotation, Is.EqualTo(rotations[index]));
                Assert.That(found[0].Candidate.AngleDegrees, Is.EqualTo(index * 90.0));
                Assert.That(found[0].ClockTrackScore, Is.GreaterThanOrEqualTo(0.95));
            });
            index++;
        }
    }

    [Test]
    public void AutomaticPolarityFindsAnInvertedSymbolWithoutTreatingItsBackgroundAsFinderEvidence()
    {
        DataMatrixEncodingResult encoded = EncodeVersion(3, "INVERT");
        ModuleMatrix image = new ModuleMatrix(55, 55);
        DrawScaled(image, encoded.SymbolMatrix, 6, 7, 3);
        ModuleMatrix inverted = InvertMatrix(image);

        DataMatrixFinderEvidence[] automatic = DataMatrixImageFinder.Locate(
            inverted,
            BarcodePolarity.Automatic);
        DataMatrixFinderEvidence[] wrong = DataMatrixImageFinder.Locate(
            inverted,
            BarcodePolarity.DarkOnLight);

        Assert.Multiple(() =>
        {
            Assert.That(automatic, Has.Length.EqualTo(1));
            Assert.That(automatic[0].SymbolInfo.Version, Is.EqualTo(3));
            Assert.That(automatic[0].Polarity, Is.EqualTo(BarcodePolarity.LightOnDark));
            Assert.That(automatic[0].Candidate.Polarity, Is.EqualTo(BarcodePolarity.LightOnDark));
            Assert.That(wrong, Is.Empty);
        });
    }

    [Test]
    public void MultipleSymbolsAreDeduplicatedAndReturnedInReadingOrder()
    {
        DataMatrixEncodingResult first = EncodeVersion(1, "A");
        DataMatrixEncodingResult second = EncodeVersion(25, "B");
        ModuleMatrix image = new ModuleMatrix(105, 75);
        DrawScaled(image, first.SymbolMatrix, 55, 6, 3);
        DrawScaled(image, second.SymbolMatrix, 8, 42, 2);

        DataMatrixFinderEvidence[] found = DataMatrixImageFinder.Locate(
            image,
            BarcodePolarity.DarkOnLight);
        BarcodePoint firstTopLeft = found[0].Location.TopLeft!;
        BarcodePoint secondTopLeft = found[1].Location.TopLeft!;

        Assert.Multiple(() =>
        {
            Assert.That(found, Has.Length.EqualTo(2));
            Assert.That(found[0].SymbolInfo.Version, Is.EqualTo(1));
            Assert.That(firstTopLeft.X, Is.EqualTo(55.0));
            Assert.That(found[1].SymbolInfo.Version, Is.EqualTo(25));
            Assert.That(secondTopLeft.X, Is.EqualTo(8.0));
            Assert.That(firstTopLeft.Y, Is.LessThan(secondTopLeft.Y));
        });
    }

    [Test]
    public void ClockDamageIsToleratedButMissingSolidBordersAndPlainBoxesAreRejected()
    {
        DataMatrixEncodingResult encoded = EncodeVersion(3, "DAMAGE");
        ModuleMatrix damagedClock = new ModuleMatrix(60, 60);
        DrawScaled(damagedClock, encoded.SymbolMatrix, 8, 8, 3);
        EraseScaledModule(damagedClock, 8, 8, 3, 2, 0);
        EraseScaledModule(damagedClock, 8, 8, 3, encoded.SymbolMatrix.Width - 1, 3);
        ModuleMatrix brokenSolid = CopyMatrix(damagedClock);
        EraseScaledModule(brokenSolid, 8, 8, 3, 4, encoded.SymbolMatrix.Height - 1);
        EraseScaledModule(brokenSolid, 8, 8, 3, 5, encoded.SymbolMatrix.Height - 1);
        EraseScaledModule(brokenSolid, 8, 8, 3, 0, 5);
        EraseScaledModule(brokenSolid, 8, 8, 3, 0, 6);
        ModuleMatrix box = new ModuleMatrix(60, 60);
        DrawBox(box, 10, 10, 40, 40, 3);

        DataMatrixFinderEvidence[] damaged = DataMatrixImageFinder.Locate(
            damagedClock,
            BarcodePolarity.DarkOnLight);
        DataMatrixFinderEvidence[] broken = DataMatrixImageFinder.Locate(
            brokenSolid,
            BarcodePolarity.DarkOnLight);
        DataMatrixFinderEvidence[] boxes = DataMatrixImageFinder.Locate(
            box,
            BarcodePolarity.DarkOnLight);

        Assert.Multiple(() =>
        {
            Assert.That(damaged, Has.Length.EqualTo(1));
            Assert.That(damaged[0].ClockTrackScore, Is.LessThan(1.0));
            Assert.That(damaged[0].ClockTrackScore, Is.GreaterThanOrEqualTo(0.80));
            Assert.That(broken, Is.Empty);
            Assert.That(boxes, Is.Empty);
        });
    }

    [Test]
    public void PerspectiveFinderEvidenceCanRefineAnApproximateQuadrilateral()
    {
        DataMatrixEncodingResult encoded = EncodeVersion(4, "PROJECT");
        ModuleMatrix image = new ModuleMatrix(105, 90);
        BarcodeQuadrilateral actual = GeometryMath.CreateQuadrilateral(
            GeometryMath.CreatePoint(20.0, 14.0),
            GeometryMath.CreatePoint(82.0, 20.0),
            GeometryMath.CreatePoint(76.0, 76.0),
            GeometryMath.CreatePoint(14.0, 70.0));
        DrawParallelogram(image, encoded.SymbolMatrix, actual);
        BarcodeQuadrilateral estimate = GeometryMath.CreateQuadrilateral(
            GeometryMath.CreatePoint(22.0, 15.0),
            GeometryMath.CreatePoint(83.0, 18.0),
            GeometryMath.CreatePoint(74.0, 74.0),
            GeometryMath.CreatePoint(13.0, 72.0));
        DataMatrixFinderEvidence? exact = DataMatrixImageFinder.EvaluateQuadrilateral(
            image,
            actual,
            BarcodePolarity.DarkOnLight);
        DataMatrixFinderEvidence measured = DataMatrixImageFinder.MeasureQuadrilateral(
            image,
            actual,
            BarcodePolarity.DarkOnLight);

        DataMatrixFinderEvidence? refined = DataMatrixImageFinder.RefineQuadrilateral(
            image,
            estimate,
            BarcodePolarity.DarkOnLight,
            8);
        DataMatrixFinderEvidence[] localized = DataMatrixImageFinder.Locate(
            image,
            BarcodePolarity.DarkOnLight);

        Assert.That(
            exact,
            Is.Not.Null,
            $"Version {measured.SymbolInfo.Version}, rotation {measured.Rotation}, solid {measured.SolidBorderScore}, clock {measured.ClockTrackScore}, dimension {measured.DimensionScore}, confidence {measured.Confidence}");
        Assert.That(refined, Is.Not.Null);
        Assert.That(localized, Has.Length.EqualTo(1));
        Assert.Multiple(() =>
        {
            Assert.That(refined!.SymbolInfo.Version, Is.EqualTo(4));
            Assert.That(refined.SolidBorderScore, Is.GreaterThanOrEqualTo(0.90));
            Assert.That(refined.ClockTrackScore, Is.GreaterThanOrEqualTo(0.80));
            Assert.That(refined.Location.TopLeft!.X, Is.EqualTo(actual.TopLeft!.X).Within(3.0));
            Assert.That(refined.Location.TopLeft.Y, Is.EqualTo(actual.TopLeft.Y).Within(3.0));
            Assert.That(refined.Location.BottomRight!.X, Is.EqualTo(actual.BottomRight!.X).Within(3.0));
            Assert.That(refined.Location.BottomRight.Y, Is.EqualTo(actual.BottomRight.Y).Within(3.0));
            Assert.That(localized[0].SymbolInfo.Version, Is.EqualTo(4));
            Assert.That(localized[0].Location.TopLeft!.X, Is.EqualTo(actual.TopLeft.X).Within(4.0));
            Assert.That(localized[0].Location.BottomRight!.Y, Is.EqualTo(actual.BottomRight.Y).Within(4.0));
        });
    }

    [Test]
    public void InvalidFinderInputsAreRejected()
    {
        ModuleMatrix image = new ModuleMatrix(20, 20);
        BarcodeQuadrilateral quadrilateral = GeometryMath.CreateQuadrilateral(
            GeometryMath.CreatePoint(1.0, 1.0),
            GeometryMath.CreatePoint(10.0, 1.0),
            GeometryMath.CreatePoint(10.0, 10.0),
            GeometryMath.CreatePoint(1.0, 10.0));

        Assert.Multiple(() =>
        {
            Assert.That(() => DataMatrixImageFinder.Locate(null!, BarcodePolarity.Automatic), Throws.TypeOf<BarcodeValidationException>());
            Assert.That(() => DataMatrixImageFinder.Locate(image, (BarcodePolarity)99), Throws.TypeOf<BarcodeValidationException>());
            Assert.That(
                () => DataMatrixImageFinder.EvaluateQuadrilateral(image, null!, BarcodePolarity.DarkOnLight),
                Throws.TypeOf<BarcodeValidationException>());
            Assert.That(
                () => DataMatrixImageFinder.RefineQuadrilateral(image, quadrilateral, BarcodePolarity.DarkOnLight, -1),
                Throws.TypeOf<BarcodeValidationException>());
            Assert.That(
                () => DataMatrixImageFinder.RefineQuadrilateral(image, quadrilateral, BarcodePolarity.DarkOnLight, 33),
                Throws.TypeOf<BarcodeValidationException>());
        });
    }

    static DataMatrixEncodingResult EncodeVersion(int version, string text)
    {
        byte[] data = global::System.Text.Encoding.ASCII.GetBytes(text);
        DataMatrixSymbolInfo symbol = DataMatrixSymbolInfoTable.GetSymbolByVersion(version);
        DataMatrixOptions options = new DataMatrixOptions();
        options.Rows = symbol.SymbolRows;
        options.Columns = symbol.SymbolColumns;
        BarcodePayload payload = new BarcodePayload();
        payload.Text = text;
        payload.RawBytes = data;
        return DataMatrixEncoder.Encode(data, payload, options, null, false);
    }

    static void DrawScaled(
        ModuleMatrix image,
        ModuleMatrix symbol,
        int left,
        int top,
        int scale)
    {
        int row = 0;
        while (row < symbol.Height)
        {
            int column = 0;
            while (column < symbol.Width)
            {
                if (symbol.GetModule(column, row))
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

    static void EraseScaledModule(
        ModuleMatrix image,
        int left,
        int top,
        int scale,
        int moduleColumn,
        int moduleRow)
    {
        int pixelY = 0;
        while (pixelY < scale)
        {
            int pixelX = 0;
            while (pixelX < scale)
            {
                image.SetModule(
                    left + (moduleColumn * scale) + pixelX,
                    top + (moduleRow * scale) + pixelY,
                    false);
                pixelX++;
            }

            pixelY++;
        }
    }

    static void DrawBox(
        ModuleMatrix image,
        int left,
        int top,
        int width,
        int height,
        int thickness)
    {
        int row = top;
        while (row < top + height)
        {
            int column = left;
            while (column < left + width)
            {
                if (column < left + thickness
                    || column >= left + width - thickness
                    || row < top + thickness
                    || row >= top + height - thickness)
                {
                    image.SetModule(column, row, true);
                }

                column++;
            }

            row++;
        }
    }

    static void DrawParallelogram(
        ModuleMatrix image,
        ModuleMatrix symbol,
        BarcodeQuadrilateral quadrilateral)
    {
        BarcodePoint topLeft = quadrilateral.TopLeft!;
        BarcodePoint topRight = quadrilateral.TopRight!;
        BarcodePoint bottomLeft = quadrilateral.BottomLeft!;
        double horizontalX = topRight.X - topLeft.X;
        double horizontalY = topRight.Y - topLeft.Y;
        double verticalX = bottomLeft.X - topLeft.X;
        double verticalY = bottomLeft.Y - topLeft.Y;
        double determinant = (horizontalX * verticalY) - (horizontalY * verticalX);
        int row = 0;
        while (row < image.Height)
        {
            int column = 0;
            while (column < image.Width)
            {
                double offsetX = column - topLeft.X;
                double offsetY = row - topLeft.Y;
                double horizontalFraction = ((offsetX * verticalY) - (offsetY * verticalX)) / determinant;
                double verticalFraction = ((horizontalX * offsetY) - (horizontalY * offsetX)) / determinant;
                if (horizontalFraction >= 0.0
                    && horizontalFraction <= 1.0
                    && verticalFraction >= 0.0
                    && verticalFraction <= 1.0)
                {
                    int moduleColumn = (int)(horizontalFraction * symbol.Width);
                    int moduleRow = (int)(verticalFraction * symbol.Height);
                    if (moduleColumn == symbol.Width)
                    {
                        moduleColumn--;
                    }

                    if (moduleRow == symbol.Height)
                    {
                        moduleRow--;
                    }

                    if (symbol.GetModule(moduleColumn, moduleRow))
                    {
                        image.SetModule(column, row, true);
                    }
                }

                column++;
            }

            row++;
        }
    }

    static ModuleMatrix CopyMatrix(ModuleMatrix source)
    {
        ModuleMatrix copy = new ModuleMatrix(source.Width, source.Height);
        copy.LoadModules(source.CopyModules());
        return copy;
    }

    static ModuleMatrix InvertMatrix(ModuleMatrix source)
    {
        ModuleMatrix result = new ModuleMatrix(source.Width, source.Height);
        int row = 0;
        while (row < source.Height)
        {
            int column = 0;
            while (column < source.Width)
            {
                result.SetModule(column, row, !source.GetModule(column, row));
                column++;
            }

            row++;
        }

        return result;
    }
}
