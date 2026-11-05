namespace barcode.Tests;

public class LinearLocalizerTests
{
    [Test]
    public void TransitionMapAndHistogramMeasureVerticalBarEdgesByRow()
    {
        ModuleMatrix matrix = new ModuleMatrix(7, 4);
        FillVerticalBar(matrix, 1, 0, 4);
        FillVerticalBar(matrix, 3, 0, 4);
        FillVerticalBar(matrix, 5, 0, 4);

        LinearTransitionMap map = LinearTransitionAnalyzer.CreateMap(matrix);
        LinearOrientationHistogram histogram = LinearTransitionAnalyzer.CreateHistogram(map);

        Assert.Multiple(() =>
        {
            Assert.That(map.HorizontalTransitions.GetModule(1, 0), Is.True);
            Assert.That(map.HorizontalTransitions.GetModule(6, 3), Is.True);
            Assert.That(map.VerticalTransitions.CopyModules(), Is.All.False);
            Assert.That(histogram.VerticalBarEvidenceByRow, Is.EqualTo(new int[] { 6, 6, 6, 6 }));
            Assert.That(histogram.HorizontalBarEvidenceByColumn, Is.EqualTo(new int[7]));
            Assert.That(histogram.VerticalBarEvidence, Is.EqualTo(24));
            Assert.That(histogram.HorizontalBarEvidence, Is.Zero);
            Assert.That(histogram.VerticalBarsDominate, Is.True);
        });
    }

    [Test]
    public void ConnectedBlockFinderReturnsIndividualBarsAndNoise()
    {
        ModuleMatrix matrix = new ModuleMatrix(10, 8);
        FillRectangle(matrix, 1, 1, 1, 6);
        FillRectangle(matrix, 4, 1, 2, 6);
        matrix.SetModule(9, 7, true);

        LinearConnectedBlock[] blocks = LinearConnectedBlockFinder.FindDarkBlocks(matrix);

        Assert.Multiple(() =>
        {
            Assert.That(blocks, Has.Length.EqualTo(3));
            Assert.That(blocks[0].Left, Is.EqualTo(1));
            Assert.That(blocks[0].PixelCount, Is.EqualTo(6));
            Assert.That(blocks[0].IsVertical, Is.True);
            Assert.That(blocks[0].MajorToMinorRatio, Is.EqualTo(6.0));
            Assert.That(blocks[1].Left, Is.EqualTo(4));
            Assert.That(blocks[1].Width, Is.EqualTo(2));
            Assert.That(blocks[1].PixelCount, Is.EqualTo(12));
            Assert.That(blocks[1].MajorToMinorRatio, Is.EqualTo(3.0));
            Assert.That(blocks[2].PixelCount, Is.EqualTo(1));
            Assert.That(blocks[2].MajorToMinorRatio, Is.EqualTo(1.0));
        });
    }

    [Test]
    public void LocalizerSeparatesVerticalBarGroupsIntoStableCandidateBoxes()
    {
        ModuleMatrix matrix = new ModuleMatrix(24, 12);
        FillVerticalBar(matrix, 2, 2, 8);
        FillVerticalBar(matrix, 4, 2, 8);
        FillVerticalBar(matrix, 6, 2, 8);
        FillVerticalBar(matrix, 16, 3, 7);
        FillVerticalBar(matrix, 18, 3, 7);
        FillVerticalBar(matrix, 20, 3, 7);
        matrix.SetModule(12, 0, true);

        LinearBarcodeCandidate[] candidates = LinearLocalizer.Locate(matrix, 3, 2);

        Assert.Multiple(() =>
        {
            Assert.That(candidates, Has.Length.EqualTo(2));
            Assert.That(candidates[0].BarsVertical, Is.True);
            Assert.That(candidates[0].BarCount, Is.EqualTo(3));
            Assert.That(candidates[0].Bounds.X, Is.EqualTo(2.0));
            Assert.That(candidates[0].Bounds.Y, Is.EqualTo(2.0));
            Assert.That(candidates[0].Bounds.Width, Is.EqualTo(5.0));
            Assert.That(candidates[0].Bounds.Height, Is.EqualTo(8.0));
            Assert.That(candidates[0].TransitionCount, Is.EqualTo(48));
            Assert.That(candidates[0].OrientationScore, Is.GreaterThan(0.8));
            Assert.That(candidates[1].Bounds.X, Is.EqualTo(16.0));
            Assert.That(candidates[1].Bounds.Y, Is.EqualTo(3.0));
            Assert.That(candidates[1].Bounds.Width, Is.EqualTo(5.0));
            Assert.That(candidates[1].Bounds.Height, Is.EqualTo(7.0));
        });
    }

    [Test]
    public void LocalizerFindsHorizontalBarsAndUsesReadingOrder()
    {
        ModuleMatrix matrix = new ModuleMatrix(16, 20);
        FillHorizontalBar(matrix, 3, 2, 8);
        FillHorizontalBar(matrix, 3, 4, 8);
        FillHorizontalBar(matrix, 3, 6, 8);
        FillHorizontalBar(matrix, 5, 13, 7);
        FillHorizontalBar(matrix, 5, 15, 7);
        FillHorizontalBar(matrix, 5, 17, 7);

        LinearBarcodeCandidate[] candidates = LinearLocalizer.Locate(matrix, 3, 2);

        Assert.Multiple(() =>
        {
            Assert.That(candidates, Has.Length.EqualTo(2));
            Assert.That(candidates[0].BarsVertical, Is.False);
            Assert.That(candidates[0].BarCount, Is.EqualTo(3));
            Assert.That(candidates[0].Bounds.X, Is.EqualTo(3.0));
            Assert.That(candidates[0].Bounds.Y, Is.EqualTo(2.0));
            Assert.That(candidates[0].Bounds.Width, Is.EqualTo(8.0));
            Assert.That(candidates[0].Bounds.Height, Is.EqualTo(5.0));
            Assert.That(candidates[0].Location.Bounds!.Width, Is.EqualTo(8.0));
            Assert.That(candidates[1].Bounds.X, Is.EqualTo(5.0));
            Assert.That(candidates[1].Bounds.Y, Is.EqualTo(13.0));
        });
    }

    [Test]
    public void LocalizerFindsTheBarRegionOfAnEncodedCode128Symbol()
    {
        Code128EncodedData encoded = Code128Encoder.EncodeBytes(
            new byte[] { 65, 66, 67, 49, 50, 51 },
            null);
        ModuleMatrix matrix = new ModuleMatrix(encoded.Modules.Length + 20, 16);
        int y = 2;
        while (y < 14)
        {
            int module = 0;
            while (module < encoded.Modules.Length)
            {
                if (encoded.Modules[module] != 0)
                {
                    matrix.SetModule(module + 10, y, true);
                }

                module = module + 1;
            }

            y = y + 1;
        }

        LinearBarcodeCandidate[] candidates = LinearLocalizer.Locate(matrix, 10, 4);

        Assert.That(candidates, Has.Length.EqualTo(1));
        Assert.Multiple(() =>
        {
            Assert.That(candidates[0].BarsVertical, Is.True);
            Assert.That(candidates[0].BarCount, Is.GreaterThan(10));
            Assert.That(candidates[0].Bounds.X, Is.EqualTo(10.0));
            Assert.That(candidates[0].Bounds.Y, Is.EqualTo(2.0));
            Assert.That(candidates[0].Bounds.Width, Is.EqualTo((double)encoded.Modules.Length));
            Assert.That(candidates[0].Bounds.Height, Is.EqualTo(12.0));
            Assert.That(candidates[0].OrientationScore, Is.GreaterThan(0.85));
        });
    }

    [Test]
    public void LocalizerRejectsPartialGroupsAndInvalidLimits()
    {
        ModuleMatrix matrix = new ModuleMatrix(8, 8);
        FillVerticalBar(matrix, 1, 1, 6);
        FillVerticalBar(matrix, 3, 1, 6);

        Assert.Multiple(() =>
        {
            Assert.That(LinearLocalizer.Locate(matrix, 3, 2), Is.Empty);
            Assert.That(
                () => LinearLocalizer.Locate(matrix, 1, 2),
                Throws.TypeOf<BarcodeValidationException>());
            Assert.That(
                () => LinearLocalizer.Locate(matrix, 2, -1),
                Throws.TypeOf<BarcodeValidationException>());
            Assert.That(
                () => LinearTransitionAnalyzer.CountWithin(
                    LinearTransitionAnalyzer.CreateMap(matrix),
                    0,
                    0,
                    9,
                    8,
                    true),
                Throws.TypeOf<BarcodeValidationException>());
        });
    }

    static void FillVerticalBar(ModuleMatrix matrix, int x, int top, int height)
    {
        FillRectangle(matrix, x, top, 1, height);
    }

    static void FillHorizontalBar(ModuleMatrix matrix, int left, int y, int width)
    {
        FillRectangle(matrix, left, y, width, 1);
    }

    static void FillRectangle(
        ModuleMatrix matrix,
        int left,
        int top,
        int width,
        int height)
    {
        int y = top;
        while (y < top + height)
        {
            int x = left;
            while (x < left + width)
            {
                matrix.SetModule(x, y, true);
                x = x + 1;
            }

            y = y + 1;
        }
    }
}
