using qrcode;

namespace qrcode.Tests;

[TestFixture]
public class DetectionDeduplicatorTests
{
    [Test]
    public void KeepsTheStrongestDuplicateAndPreservesItsMetadataAndCallerOrder()
    {
        QrDetection weaker = Create(9, 10, 20, 42, 42, 0.7);
        QrDetection stronger = Create(4, 10.4, 19.8, 42, 42, 0.95);
        stronger.SampledMatrix = new QrMatrix { Values = [QrModuleValue.Unknown, QrModuleValue.Dark] };
        stronger.Diagnostics = ["refined geometry", "inverted preprocessing pass"];
        stronger.HasMirrorEstimate = true;
        stronger.IsMirrored = true;
        stronger.Polarity = QrPolarity.LightOnDark;
        weaker.HasMirrorEstimate = false;
        weaker.Polarity = QrPolarity.Automatic;
        QrDetection[] input = [weaker, stronger, stronger];
        QrQuadrilateral corners = stronger.Corners!;
        QrPoint topLeft = corners.TopLeft!;
        QrMatrix matrix = stronger.SampledMatrix;
        string[] diagnostics = stronger.Diagnostics;

        QrDetection[] result = DetectionDeduplicator.SelectDetections(input, new QrDetectionOptions());

        Assert.That(result, Has.Length.EqualTo(1));
        Assert.That(result[0], Is.SameAs(stronger));
        Assert.That(input, Is.EqualTo(new[] { weaker, stronger, stronger }));
        Assert.That(stronger.Corners, Is.SameAs(corners));
        Assert.That(corners.TopLeft, Is.SameAs(topLeft));
        Assert.That(topLeft.X, Is.EqualTo(10.4));
        Assert.That(stronger.SampledMatrix, Is.SameAs(matrix));
        Assert.That(matrix.Values, Is.EqualTo(new[] { QrModuleValue.Unknown, QrModuleValue.Dark }));
        Assert.That(stronger.Diagnostics, Is.SameAs(diagnostics));
        Assert.That(stronger.CandidateIndex, Is.EqualTo(4));
        Assert.That(stronger.IsMirrored, Is.True);
        Assert.That(stronger.Polarity, Is.EqualTo(QrPolarity.LightOnDark));
    }

    [TestCase(0, false)]
    [TestCase(1, false)]
    [TestCase(2, false)]
    [TestCase(3, false)]
    [TestCase(0, true)]
    [TestCase(1, true)]
    [TestCase(2, true)]
    [TestCase(3, true)]
    public void MatchesCyclicAndReversedCornerLabelsForRectangularSymbols(int start, bool reverse)
    {
        QrDetection first = Create(1, 10, 20, 86, 14, 0.8);
        first.EstimatedSymbolKind = QrSymbolKind.rMQR;
        first.EstimatedWidth = 43;
        first.EstimatedHeight = 7;
        QrDetection second = Create(2, 10.1, 20.2, 86, 14, 0.9);
        second.EstimatedSymbolKind = QrSymbolKind.rMQR;
        second.EstimatedWidth = 7;
        second.EstimatedHeight = 43;
        QrPoint[] points = Points(second.Corners!);
        QrPoint[] reordered = new QrPoint[4];
        for (int index = 0; index < 4; index++)
            reordered[index] = points[(start + (reverse ? -index : index) + 4) % 4];
        second.Corners = Quad(reordered);

        QrDetection[] result = DetectionDeduplicator.SelectDetections([first, second], new QrDetectionOptions());
        Assert.That(result, Is.EqualTo(new[] { second }));
        Assert.That(second.Corners.TopLeft, Is.SameAs(reordered[0]));
        Assert.That(second.EstimatedWidth, Is.EqualTo(7));
        Assert.That(second.EstimatedHeight, Is.EqualTo(43));
    }

    [Test]
    public void MatchesPerspectiveQuadrilateralsWithoutRequiringAxisAlignment()
    {
        QrDetection first = Create(0, 0, 0, 42, 42, 0.9);
        first.Corners = Quad([Point(10, 20), Point(55, 12), Point(68, 63), Point(15, 55)]);
        QrDetection second = Create(1, 0, 0, 42, 42, 0.8);
        second.Corners = Quad([Point(10.2, 19.8), Point(54.7, 12.2), Point(68.4, 63.1), Point(15.1, 54.7)]);
        Assert.That(DetectionDeduplicator.SelectDetections([second, first], new QrDetectionOptions()), Is.EqualTo(new[] { first }));
    }

    [Test]
    public void PreservesDistinctSymbolsDespiteLargeBoundingBoxOverlap()
    {
        QrDetection square = Create(0, 0, 0, 42, 42, 0.9);
        QrDetection shifted = Create(1, 4, 0, 42, 42, 0.8);
        QrDetection nested = Create(2, 4, 4, 34, 34, 0.7);
        QrDetection diamond = Create(3, 0, 0, 42, 42, 0.6);
        diamond.Corners = Quad([Point(21, 0), Point(42, 21), Point(21, 42), Point(0, 21)]);
        QrDetection[] result = DetectionDeduplicator.SelectDetections([nested, diamond, shifted, square], new QrDetectionOptions());
        Assert.That(result, Has.Length.EqualTo(4));
        Assert.That(result, Is.EquivalentTo(new[] { square, shifted, nested, diamond }));
    }

    [TestCase("family")]
    [TestCase("dimensions")]
    [TestCase("polarity")]
    [TestCase("mirror")]
    public void PreservesConflictingKnownMetadataAtTheSameLocation(string conflict)
    {
        QrDetection first = Create(0, 0, 0, 42, 42, 0.9);
        QrDetection second = Create(1, 0, 0, 42, 42, 0.8);
        switch (conflict)
        {
            case "family": second.EstimatedSymbolKind = QrSymbolKind.LegacyModel1; break;
            case "dimensions": second.EstimatedWidth = 25; second.EstimatedHeight = 25; break;
            case "polarity": second.Polarity = QrPolarity.LightOnDark; break;
            case "mirror":
                first.HasMirrorEstimate = true;
                second.HasMirrorEstimate = true;
                second.IsMirrored = true;
                break;
        }
        Assert.That(DetectionDeduplicator.SelectDetections([second, first], new QrDetectionOptions()), Has.Length.EqualTo(2));
    }

    [Test]
    public void AnUnresolvedWinnerCannotBridgeConflictingKnownHypotheses()
    {
        QrDetection unresolved = Create(0, 0, 0, 42, 42, 0.99);
        unresolved.HasSymbolKindEstimate = false;
        QrDetection model2 = Create(1, 0, 0, 42, 42, 0.9);
        QrDetection legacy = Create(2, 0, 0, 42, 42, 0.8);
        legacy.EstimatedSymbolKind = QrSymbolKind.LegacyModel1;
        Assert.That(DetectionDeduplicator.SelectDetections([legacy, model2, unresolved], new QrDetectionOptions()),
            Is.EqualTo(new[] { unresolved, legacy }));
    }

    [Test]
    public void DoesNotMergeDistinctEndpointsThroughAChainOfDuplicates()
    {
        QrDetection first = Create(0, 0, 0, 42, 42, 0.95);
        QrDetection bridge = Create(1, 1, 0, 42, 42, 0.9);
        QrDetection last = Create(2, 2, 0, 42, 42, 0.85);
        Assert.That(DetectionDeduplicator.SelectDetections([bridge, last, first], new QrDetectionOptions()),
            Is.EqualTo(new[] { first, last }));
    }

    [TestCase(1.5, 1)]
    [TestCase(1.500001, 2)]
    public void UsesModuleRelativeCornerTolerance(double shift, int expectedCount)
    {
        QrDetection first = Create(0, 0, 0, 42, 42, 0.9);
        QrDetection second = Create(1, shift, 0, 42, 42, 0.8);
        Assert.That(DetectionDeduplicator.SelectDetections([first, second], new QrDetectionOptions()), Has.Length.EqualTo(expectedCount));
    }

    [Test]
    public void CapsToleranceByGeometryAndUsesTheSmallerModuleEstimate()
    {
        QrDetection first = Create(0, 0, 0, 42, 42, 0.9);
        QrDetection second = Create(1, 5, 0, 42, 42, 0.8);
        first.ModuleSize = 100;
        second.ModuleSize = 100;
        Assert.That(DetectionDeduplicator.SelectDetections([first, second], new QrDetectionOptions()), Has.Length.EqualTo(2));
        second = Create(2, 2, 0, 42, 42, 0.8);
        Assert.That(DetectionDeduplicator.SelectDetections([first, second], new QrDetectionOptions()), Has.Length.EqualTo(2));
    }

    [Test]
    public void OrdersByImagePositionAndChoosesTiesIndependentlyOfInputOrder()
    {
        QrDetection topLeft = Create(8, 0, 0, 42, 42, 0.7);
        QrDetection duplicate = Create(9, 0.2, 0.2, 42, 42, 0.7);
        QrDetection topRight = Create(6, 100, 0, 42, 42, 0.8);
        QrDetection bottom = Create(4, -10, 100, 42, 42, 0.95);
        QrDetection[] candidates = [topRight, bottom, duplicate, topLeft];
        Random random = new Random(1090);
        for (int iteration = 0; iteration < 30; iteration++)
        {
            random.Shuffle(candidates);
            QrDetection[] result = DetectionDeduplicator.SelectDetections(candidates, new QrDetectionOptions());
            Assert.That(result, Is.EqualTo(new[] { topLeft, topRight, bottom }));
        }
    }

    [Test]
    public void PrefersResolvedMetadataWhenConfidenceIsTied()
    {
        QrDetection uncertain = Create(0, 0, 0, 42, 42, 0.9);
        uncertain.HasSymbolKindEstimate = false;
        QrDetection resolved = Create(1, 0, 0, 42, 42, 0.9);
        Assert.That(DetectionDeduplicator.SelectDetections([uncertain, resolved], new QrDetectionOptions()), Is.EqualTo(new[] { resolved }));
        uncertain.HasSymbolKindEstimate = true;
        resolved.HasMirrorEstimate = true;
        Assert.That(DetectionDeduplicator.SelectDetections([uncertain, resolved], new QrDetectionOptions()), Is.EqualTo(new[] { resolved }));
    }

    [Test]
    public void AppliesOutputLimitsAfterSuppressionAndPicksTheBestSingleSymbol()
    {
        QrDetection best = Create(0, 100, 100, 42, 42, 0.99);
        QrDetection duplicate = Create(1, 100.1, 100, 42, 42, 0.98);
        QrDetection second = Create(2, 0, 0, 42, 42, 0.9);
        QrDetection third = Create(3, 100, 0, 42, 42, 0.8);
        QrDetection[] candidates = [third, duplicate, second, best];
        Assert.That(DetectionDeduplicator.SelectDetections(candidates, new QrDetectionOptions { MaximumCandidates = 2 }),
            Is.EqualTo(new[] { second, best }));
        Assert.That(DetectionDeduplicator.SelectDetections(candidates, new QrDetectionOptions { DetectMultiple = false }),
            Is.EqualTo(new[] { best }));
    }

    [Test]
    public void HandlesEmptyInputAndPassBudgetsWithoutIntegerOverflow()
    {
        Assert.That(DetectionDeduplicator.SelectDetections([], new QrDetectionOptions()), Is.Empty);
        QrDetection[] candidates = Enumerable.Range(0, 5).Select(i => Create(i, i * 50, 0, 42, 42, 0.9)).ToArray();
        Assert.Throws<QrResourceLimitException>(() => DetectionDeduplicator.SelectDetections(candidates,
            new QrDetectionOptions { MaximumCandidates = 2, MaximumPyramidLevels = 1 }));
        Assert.That(DetectionDeduplicator.SelectDetections(candidates,
            new QrDetectionOptions { MaximumCandidates = int.MaxValue, MaximumPyramidLevels = int.MaxValue }), Has.Length.EqualTo(5));
    }

    [Test]
    public void RejectsInvalidConfigurationAndNonUniqueStableIndices()
    {
        Assert.Throws<QrInputException>(() => DetectionDeduplicator.SelectDetections(null!, new QrDetectionOptions()));
        Assert.Throws<QrInputException>(() => DetectionDeduplicator.SelectDetections([], null!));
        Assert.Throws<QrInputException>(() => DetectionDeduplicator.SelectDetections([], new QrDetectionOptions { MaximumCandidates = 0 }));
        Assert.Throws<QrInputException>(() => DetectionDeduplicator.SelectDetections([], new QrDetectionOptions { MaximumPyramidLevels = -1 }));
        Assert.Throws<QrInputException>(() => DetectionDeduplicator.SelectDetections([null!], new QrDetectionOptions()));
        Assert.Throws<QrInputException>(() => DetectionDeduplicator.SelectDetections(
            [Create(1, 0, 0, 42, 42, 0.9), Create(1, 100, 0, 42, 42, 0.8)], new QrDetectionOptions()));
    }

    [Test]
    public void ValidatesEveryCandidateEvenWhenOnlyOneResultIsRequested()
    {
        QrDetection valid = Create(0, 0, 0, 42, 42, 1);
        Action<QrDetection>[] corruptions =
        [
            c => c.Confidence = double.NaN, c => c.Confidence = -0.1, c => c.Confidence = 1.1,
            c => c.ModuleSize = 0, c => c.ModuleSize = double.PositiveInfinity,
            c => c.CandidateIndex = -1, c => c.EstimatedWidth = -1, c => c.EstimatedVersion = -1,
            c => c.Polarity = (QrPolarity)99, c => c.EstimatedSymbolKind = (QrSymbolKind)99,
            c => c.Corners = null, c => c.Corners!.TopLeft = null,
            c => c.Corners!.TopRight!.X = double.NegativeInfinity,
            c => c.Corners!.BottomRight = c.Corners.TopLeft
        ];
        foreach (Action<QrDetection> corrupt in corruptions)
        {
            QrDetection invalid = Create(1, 100, 0, 42, 42, 0.1);
            corrupt(invalid);
            Assert.Throws<QrInputException>(() => DetectionDeduplicator.SelectDetections([valid, invalid],
                new QrDetectionOptions { DetectMultiple = false }));
        }
    }

    static QrDetection Create(int index, double x, double y, double width, double height, double confidence)
    {
        return new QrDetection
        {
            CandidateIndex = index, Confidence = confidence, ModuleSize = 2,
            HasSymbolKindEstimate = true, EstimatedSymbolKind = QrSymbolKind.Model2,
            EstimatedVersion = 1, EstimatedWidth = 21, EstimatedHeight = 21,
            Polarity = QrPolarity.DarkOnLight,
            Corners = Quad([Point(x, y), Point(x + width, y), Point(x + width, y + height), Point(x, y + height)])
        };
    }

    static QrPoint Point(double x, double y)
    {
        return new QrPoint { X = x, Y = y };
    }

    static QrPoint[] Points(QrQuadrilateral corners)
    {
        return [corners.TopLeft!, corners.TopRight!, corners.BottomRight!, corners.BottomLeft!];
    }

    static QrQuadrilateral Quad(QrPoint[] points)
    {
        return new QrQuadrilateral { TopLeft = points[0], TopRight = points[1], BottomRight = points[2], BottomLeft = points[3] };
    }
}
