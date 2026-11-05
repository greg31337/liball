using qrcode;

namespace qrcode.Tests;

[TestFixture]
public class GridRefinerTests
{
    [TestCase(QrSymbolKind.Model2, false)]
    [TestCase(QrSymbolKind.Model2, true)]
    [TestCase(QrSymbolKind.Micro, false)]
    [TestCase(QrSymbolKind.Micro, true)]
    [TestCase(QrSymbolKind.rMQR, false)]
    [TestCase(QrSymbolKind.rMQR, true)]
    public void RefinesShiftedGridsAndRecoversRenderedModules(QrSymbolKind kind, bool multiPoint)
    {
        QrSymbol symbol = CreateSymbol(kind);
        QrMatrix expected = symbol.Matrix!;
        QrRaster raster = Render(symbol);
        PerspectiveTransform initial = Affine(6, 6, 12 + 3.6, 12 - 3.3);
        QrDetectionOptions options = new QrDetectionOptions { UseMultiPointSampling = multiPoint };
        QrMatrix before = GridSampler.Sample(raster, initial, expected.Width, expected.Height, options);
        GridRefinementResult result = GridRefiner.Refine(raster, initial, expected, options);

        Assert.That(result.Improved, Is.True);
        Assert.That(result.FinalScore, Is.GreaterThan(result.InitialScore));
        Assert.That(result.FinalScore, Is.GreaterThan(0.98));
        Assert.That(MismatchCount(result.Matrix, expected), Is.LessThan(MismatchCount(before, expected)));
        Assert.That(result.Matrix.Values, Is.EqualTo(expected.Values));
        Assert.That(result.Evaluations, Is.InRange(2, GridRefiner.MaximumEvaluations));
        Assert.That(result.Iterations, Is.InRange(1, GridRefiner.MaximumIterations));
        Assert.That(initial.M02, Is.EqualTo(15.6));
        Assert.That(initial.M12, Is.EqualTo(8.7));
    }

    [Test]
    public void ImprovesAPerspectiveGridWithScaleAndShearErrors()
    {
        QrSymbol symbol = CreateSymbol(QrSymbolKind.Model2);
        QrMatrix expected = symbol.Matrix!;
        PerspectiveTransform actual = new PerspectiveTransform
        {
            M00 = 6, M01 = 0.4, M02 = 12,
            M10 = -0.2, M11 = 6, M12 = 15,
            M20 = 0.001, M21 = -0.0008, M22 = 1
        };
        QrRaster raster = Warp(expected, actual, 320, 310);
        PerspectiveTransform initial = new PerspectiveTransform
        {
            M00 = 6.08, M01 = 0.45, M02 = 14,
            M10 = -0.25, M11 = 5.95, M12 = 13,
            M20 = 0.0013, M21 = -0.0005, M22 = 1
        };
        QrDetectionOptions options = new QrDetectionOptions();
        QrMatrix before = GridSampler.Sample(raster, initial, expected.Width, expected.Height, options);
        GridRefinementResult result = GridRefiner.Refine(raster, initial, expected, options);

        Assert.That(result.Improved, Is.True);
        Assert.That(result.FinalScore, Is.GreaterThan(result.InitialScore + 0.05));
        Assert.That(result.FinalScore, Is.GreaterThan(0.95));
        Assert.That(MismatchCount(result.Matrix, expected), Is.LessThan(MismatchCount(before, expected)));
        Assert.That(result.Transform.M20, Is.Not.Zero);
        Assert.That(result.Transform.M21, Is.Not.Zero);
        Assert.That(result.Iterations, Is.LessThanOrEqualTo(GridRefiner.MaximumIterations));
    }

    [TestCase(QrSymbolKind.Model2)]
    [TestCase(QrSymbolKind.Micro)]
    [TestCase(QrSymbolKind.rMQR)]
    public void LeavesPerfectGridsUnchangedAndDoesNotShareCallerStorage(QrSymbolKind kind)
    {
        QrSymbol symbol = CreateSymbol(kind);
        QrRaster raster = Render(symbol);
        QrMatrix expected = symbol.Matrix!;
        byte[] originalPixels = (byte[])raster.Pixels.Clone();
        QrModuleValue[] originalValues = (QrModuleValue[])expected.Values.Clone();
        QrModuleRole[] originalRoles = (QrModuleRole[])expected.Roles!.Clone();
        PerspectiveTransform initial = Affine(6, 6, 12, 12);
        GridRefinementResult result = GridRefiner.Refine(raster, initial, expected, new QrDetectionOptions());

        Assert.That(result.Improved, Is.False);
        Assert.That(result.InitialScore, Is.EqualTo(1));
        Assert.That(result.FinalScore, Is.EqualTo(1));
        Assert.That(result.Evaluations, Is.EqualTo(1));
        Assert.That(result.Iterations, Is.Zero);
        Assert.That(result.Matrix.Values, Is.EqualTo(originalValues));
        Assert.That(result.Matrix.Values, Is.Not.SameAs(expected.Values));
        Assert.That(result.Transform, Is.Not.SameAs(initial));
        result.Transform.M02 = 999;
        result.Matrix.Values[0] = QrModuleValue.Unknown;
        Assert.That(initial.M02, Is.EqualTo(12));
        Assert.That(raster.Pixels, Is.EqualTo(originalPixels));
        Assert.That(expected.Values, Is.EqualTo(originalValues));
        Assert.That(expected.Roles, Is.EqualTo(originalRoles));
    }

    [Test]
    public void DisabledRefinementSamplesTheOriginalGrid()
    {
        QrSymbol symbol = CreateSymbol(QrSymbolKind.Model2);
        QrRaster raster = Render(symbol);
        PerspectiveTransform initial = Affine(6, 6, 16, 8);
        QrDetectionOptions options = new QrDetectionOptions { RefineGrid = false, UseMultiPointSampling = false };
        QrMatrix expected = symbol.Matrix!;
        QrMatrix original = GridSampler.Sample(raster, initial, expected.Width, expected.Height, options);
        GridRefinementResult result = GridRefiner.Refine(raster, initial, expected, options);

        Assert.That(result.Improved, Is.False);
        Assert.That(result.InitialScore, Is.LessThan(1));
        Assert.That(result.FinalScore, Is.EqualTo(result.InitialScore));
        Assert.That(result.Evaluations, Is.EqualTo(1));
        Assert.That(result.Iterations, Is.Zero);
        Assert.That(result.Matrix.Values, Is.EqualTo(original.Values));
        Assert.That(result.Matrix.Confidence, Is.EqualTo(original.Confidence));
    }

    [TestCase(QrModuleRole.Finder)]
    [TestCase(QrModuleRole.Timing)]
    [TestCase(QrModuleRole.Alignment)]
    [TestCase(QrModuleRole.Format)]
    public void EachEvidenceFamilyCanDriveRefinement(QrModuleRole role)
    {
        QrSymbol symbol = CreateSymbol(QrSymbolKind.Model2);
        QrMatrix expected = symbol.Matrix!;
        for (int index = 0; index < expected.Roles!.Length; index++)
            if (expected.Roles[index] != role) expected.Roles[index] = QrModuleRole.Unknown;
        GridRefinementResult result = GridRefiner.Refine(Render(symbol),
            Affine(6, 6, 15.6, 12), expected, new QrDetectionOptions());
        Assert.That(result.Improved, Is.True);
        Assert.That(result.FinalScore, Is.GreaterThan(result.InitialScore));
        Assert.That(result.FinalScore, Is.GreaterThan(0.98));
    }

    [Test]
    public void IgnoresPayloadAndUnknownEvidenceAndRepeatsDeterministically()
    {
        QrSymbol symbol = CreateSymbol(QrSymbolKind.Model2);
        QrMatrix expected = symbol.Matrix!;
        QrRaster raster = Render(symbol);
        PerspectiveTransform initial = Affine(6, 6, 15.6, 8.7);
        // Unknown format observations are unavailable evidence, not light bits.
        for (int index = 0; index < expected.Values.Length; index++)
            if (expected.Roles![index] == QrModuleRole.Format) expected.Values[index] = QrModuleValue.Unknown;
        GridRefinementResult first = GridRefiner.Refine(raster, initial, expected, new QrDetectionOptions());
        for (int index = 0; index < expected.Values.Length; index++)
            if (expected.Roles![index] == QrModuleRole.Data || expected.Roles[index] == QrModuleRole.ErrorCorrection)
                expected.Values[index] = expected.Values[index] == QrModuleValue.Dark
                    ? QrModuleValue.Light : QrModuleValue.Dark;
        GridRefinementResult second = GridRefiner.Refine(raster, initial, expected, new QrDetectionOptions());

        Assert.That(first.Improved, Is.True);
        Assert.That(second.InitialScore, Is.EqualTo(first.InitialScore));
        Assert.That(second.FinalScore, Is.EqualTo(first.FinalScore));
        Assert.That(second.Evaluations, Is.EqualTo(first.Evaluations));
        Assert.That(second.Matrix.Values, Is.EqualTo(first.Matrix.Values));
        Assert.That(second.Matrix.Confidence, Is.EqualTo(first.Matrix.Confidence));
        Assert.That(second.Transform.M00, Is.EqualTo(first.Transform.M00));
        Assert.That(second.Transform.M20, Is.EqualTo(first.Transform.M20));
    }

    [Test]
    public void ScoresEvidenceFamiliesEquallyAndPenalizesUnavailableSamples()
    {
        QrMatrix expected = new QrMatrix
        {
            Width = 2, Height = 2,
            Values = [QrModuleValue.Dark, QrModuleValue.Dark, QrModuleValue.Dark, QrModuleValue.Dark],
            Roles = [QrModuleRole.Finder, QrModuleRole.Finder, QrModuleRole.Timing, QrModuleRole.Format]
        };
        QrRaster raster = new QrRaster { Width = 2, Height = 2, Stride = 2, Pixels = [255, 0, 0, 0] };
        QrDetectionOptions options = new QrDetectionOptions { RefineGrid = false };
        GridRefinementResult damaged = GridRefiner.Refine(raster, Affine(1, 1, 0, 0), expected, options);
        Assert.That(damaged.FinalScore, Is.EqualTo(5.0 / 6).Within(1E-12));

        GridRefinementResult cropped = GridRefiner.Refine(raster, Affine(1, 1, -1, 0), expected, options);
        // Finder gets zero matches, timing is outside, format is dark.
        Assert.That(cropped.FinalScore, Is.EqualTo(1.0 / 3).Within(1E-12));
        Assert.That(cropped.Matrix.Values[2], Is.EqualTo(QrModuleValue.Unknown));
        Assert.That(cropped.Matrix.Confidence![2], Is.Zero);
    }

    [TestCase(true)]
    [TestCase(false)]
    public void MissingOrUninformativeEvidenceKeepsTheInitialGrid(bool noEvidence)
    {
        QrMatrix expected = new QrMatrix
        {
            Width = 7, Height = 7,
            Values = Enumerable.Repeat(noEvidence ? QrModuleValue.Unknown : QrModuleValue.Dark, 49).ToArray(),
            Roles = Enumerable.Repeat(QrModuleRole.Finder, 49).ToArray()
        };
        QrRaster raster = new QrRaster
        {
            Width = 7, Height = 7, Stride = 7, Pixels = Enumerable.Repeat((byte)255, 49).ToArray()
        };
        GridRefinementResult result = GridRefiner.Refine(raster, Affine(1, 1, 0, 0), expected, new QrDetectionOptions());
        Assert.That(result.Improved, Is.False);
        Assert.That(result.InitialScore, Is.Zero);
        Assert.That(result.FinalScore, Is.Zero);
        Assert.That(result.Transform.M00, Is.EqualTo(1));
        Assert.That(result.Transform.M02, Is.Zero);
        Assert.That(result.Evaluations, Is.LessThanOrEqualTo(GridRefiner.MaximumEvaluations));
        Assert.That(result.Iterations, Is.LessThanOrEqualTo(GridRefiner.MaximumIterations));
        if (noEvidence) Assert.That(result.Evaluations, Is.EqualTo(1));
    }

    [Test]
    public void HandlesDamageAndCroppedGeometryWithinTheSearchBudget()
    {
        QrSymbol symbol = CreateSymbol(QrSymbolKind.Model2);
        QrRaster raster = Render(symbol);
        for (int index = 0; index < raster.Pixels.Length; index += 17)
            raster.Pixels[index] = raster.Pixels[index] == 0 ? (byte)255 : (byte)0;
        GridRefinementResult result = GridRefiner.Refine(raster, Affine(6, 6, -2, -2),
            symbol.Matrix!, new QrDetectionOptions());
        Assert.That(result.FinalScore, Is.GreaterThanOrEqualTo(result.InitialScore));
        Assert.That(result.FinalScore, Is.InRange(0.0, 1.0));
        Assert.That(result.Iterations, Is.InRange(1, GridRefiner.MaximumIterations));
        Assert.That(result.Evaluations, Is.InRange(1, GridRefiner.MaximumEvaluations));
    }

    [Test]
    public void RejectsInvalidEvidenceGeometryStorageAndEnums()
    {
        QrSymbol symbol = CreateSymbol(QrSymbolKind.Micro);
        QrRaster raster = Render(symbol);
        QrMatrix[] invalid =
        [
            null!, new QrMatrix(),
            new QrMatrix { Width = 1, Height = 1, Values = null!, Roles = [QrModuleRole.Finder] },
            new QrMatrix { Width = 1, Height = 1, Values = [QrModuleValue.Dark] },
            new QrMatrix { Width = 1, Height = 1, Values = [QrModuleValue.Dark], Roles = [] },
            new QrMatrix { Width = 1, Height = 1, Values = [(QrModuleValue)99], Roles = [QrModuleRole.Finder] },
            new QrMatrix { Width = 1, Height = 1, Values = [QrModuleValue.Dark], Roles = [(QrModuleRole)99] }
        ];
        foreach (QrMatrix evidence in invalid)
            Assert.Throws<QrInputException>(() => GridRefiner.Refine(raster, Affine(1, 1, 0, 0),
                evidence, new QrDetectionOptions()));
        Assert.Throws<QrResourceLimitException>(() => GridRefiner.Refine(raster, Affine(1, 1, 0, 0),
            new QrMatrix { Width = int.MaxValue, Height = 2 }, new QrDetectionOptions()));
    }

    [Test]
    public void RejectsInvalidImagesTransformsAndOptions()
    {
        QrSymbol symbol = CreateSymbol(QrSymbolKind.Micro);
        QrRaster raster = Render(symbol);
        QrMatrix expected = symbol.Matrix!;
        PerspectiveTransform initial = Affine(6, 6, 12, 12);
        foreach (PerspectiveTransform invalid in new PerspectiveTransform[]
        {
            null!, new PerspectiveTransform(),
            new PerspectiveTransform { M00 = double.NaN, M11 = 1, M22 = 1 }
        })
            Assert.Throws<QrInputException>(() => GridRefiner.Refine(raster, invalid, expected, new QrDetectionOptions()));
        Assert.Throws<QrInputException>(() => GridRefiner.Refine(null!, initial, expected, new QrDetectionOptions()));
        Assert.Throws<QrInputException>(() => GridRefiner.Refine(raster, initial, expected, null!));
        Assert.Throws<QrResourceLimitException>(() => GridRefiner.Refine(raster, initial, expected,
            new QrDetectionOptions { MaximumImagePixels = 1 }));
        raster.Pixels[0] = 127;
        Assert.Throws<QrInputException>(() => GridRefiner.Refine(raster, initial, expected, new QrDetectionOptions()));
    }

    static QrSymbol CreateSymbol(QrSymbolKind kind)
    {
        QrEncodeOptions options = new QrEncodeOptions
        {
            ForceSymbolKind = true, SymbolKind = kind, QuietZoneModules = 0
        };
        if (kind == QrSymbolKind.Model2)
        {
            options.MinimumVersion = 7;
            options.MaximumVersion = 7;
        }
        return new QrEncoder().EncodeText("12345", options);
    }

    static QrRaster Render(QrSymbol symbol)
    {
        return new QrRenderer().RenderSymbol(symbol, new QrRenderOptions
        {
            Scale = 6, IncludeQuietZone = true, QuietZoneModules = 2
        });
    }

    static PerspectiveTransform Affine(double xScale, double yScale, double x, double y)
    {
        return new PerspectiveTransform { M00 = xScale, M11 = yScale, M02 = x, M12 = y, M22 = 1 };
    }

    static int MismatchCount(QrMatrix actual, QrMatrix expected)
    {
        int count = 0;
        for (int index = 0; index < expected.Values.Length; index++)
            if (actual.Values[index] != expected.Values[index]) count++;
        return count;
    }

    static QrRaster Warp(QrMatrix matrix, PerspectiveTransform actual, int width, int height)
    {
        QrRaster raster = new QrRaster
        {
            Width = width, Height = height, Stride = width,
            Pixels = Enumerable.Repeat((byte)255, width * height).ToArray()
        };
        PerspectiveTransform inverse = actual.Invert();
        for (int row = 0; row < height; row++)
        {
            for (int column = 0; column < width; column++)
            {
                QrPoint module = inverse.MapPoint(new QrPoint { X = column + 0.5, Y = row + 0.5 });
                if (module.X >= 0 && module.Y >= 0 && module.X < matrix.Width && module.Y < matrix.Height)
                    raster.Pixels[row * width + column] = matrix.Values[(int)module.Y * matrix.Width + (int)module.X]
                        == QrModuleValue.Dark ? (byte)0 : (byte)255;
            }
        }
        return raster;
    }
}
