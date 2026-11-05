using qrcode;

namespace qrcode.Tests;

[TestFixture]
public class DetectionOrientationAnalyzerTests
{
    static IEnumerable<TestCaseData> ProfileCases()
    {
        foreach (QrSymbolKind kind in new[] { QrSymbolKind.Model2, QrSymbolKind.Micro, QrSymbolKind.rMQR })
            foreach (bool mirror in new[] { false, true })
                foreach (bool invertedImage in new[] { false, true })
                    foreach (bool invertedPass in new[] { false, true })
                        yield return new TestCaseData(kind, mirror, invertedImage, invertedPass);
    }

    [TestCaseSource(nameof(ProfileCases))]
    public void ReportsMirrorAndSourceReflectanceForRawAndNormalizedSamples(
        QrSymbolKind kind, bool mirror, bool invertedImage, bool invertedPass)
    {
        QrMatrix expected = CreateSymbol(kind).Matrix!;
        QrMatrix sampled = Transform(expected, mirror, invertedImage != invertedPass);
        QrDetection detection = new QrDetection
        {
            SampledMatrix = sampled,
            HasSymbolKindEstimate = true,
            EstimatedSymbolKind = kind,
            EstimatedWidth = sampled.Width,
            EstimatedHeight = sampled.Height
        };
        DetectionOrientationAnalyzer.Annotate(detection, expected,
            invertedPass ? QrPolarity.LightOnDark : QrPolarity.DarkOnLight);

        Assert.That(detection.HasMirrorEstimate, Is.True);
        Assert.That(detection.IsMirrored, Is.EqualTo(mirror));
        Assert.That(detection.Polarity, Is.EqualTo(invertedImage ? QrPolarity.LightOnDark : QrPolarity.DarkOnLight));
        Assert.That(detection.SampledMatrix, Is.SameAs(sampled));
        Assert.That(detection.EstimatedWidth, Is.EqualTo(sampled.Width));
        Assert.That(detection.EstimatedHeight, Is.EqualTo(sampled.Height));
    }

    [TestCase(0, false)]
    [TestCase(1, false)]
    [TestCase(2, false)]
    [TestCase(3, false)]
    [TestCase(0, true)]
    [TestCase(1, true)]
    [TestCase(2, true)]
    [TestCase(3, true)]
    public void CombinesImageGeometryHandednessWithSampleOrientation(int quarterTurns, bool mirror)
    {
        QrMatrix expected = CreateSymbol(QrSymbolKind.Model2).Matrix!;
        QrQuadrilateral corners = CreateCorners(expected.Width, expected.Height, quarterTurns, mirror);
        QrDetection detection = new QrDetection { SampledMatrix = Transform(expected, false, false), Corners = corners };
        DetectionOrientationAnalyzer.Annotate(detection, expected, QrPolarity.DarkOnLight);
        Assert.That(detection.HasMirrorEstimate, Is.True);
        Assert.That(detection.IsMirrored, Is.EqualTo(mirror));
        Assert.That(detection.Corners, Is.SameAs(corners));

        // A reflected sampling geometry can cancel a transpose in the samples.
        detection.SampledMatrix = Transform(expected, true, false);
        DetectionOrientationAnalyzer.Annotate(detection, expected, QrPolarity.DarkOnLight);
        Assert.That(detection.HasMirrorEstimate, Is.True);
        Assert.That(detection.IsMirrored, Is.EqualTo(!mirror));
    }

    [Test]
    public void AnnotatesWithoutRewritingPayloadOrOtherDetectionMetadata()
    {
        QrMatrix expected = CreateSymbol(QrSymbolKind.Model2).Matrix!;
        QrModuleValue[] expectedValues = (QrModuleValue[])expected.Values.Clone();
        QrMatrix sampled = Transform(expected, true, true);
        sampled.Roles = (QrModuleRole[])expected.Roles!.Clone();
        sampled.Confidence = Enumerable.Range(0, sampled.Values.Length).Select(i => (byte)(i % 256)).ToArray();
        QrModuleValue[] values = (QrModuleValue[])sampled.Values.Clone();
        QrModuleRole[] roles = (QrModuleRole[])sampled.Roles.Clone();
        byte[] confidence = (byte[])sampled.Confidence.Clone();
        QrQuadrilateral corners = CreateCorners(45, 45, 1, false);
        QrPoint topLeft = corners.TopLeft!;
        double x = topLeft.X;
        double y = topLeft.Y;
        string[] diagnostics = ["candidate evidence"];
        QrDetection detection = new QrDetection
        {
            SampledMatrix = sampled, Corners = corners,
            EstimatedVersion = 7, EstimatedWidth = 45, EstimatedHeight = 45,
            HasSymbolKindEstimate = true, EstimatedSymbolKind = QrSymbolKind.Model2,
            Confidence = 0.9, ModuleSize = 3, CandidateIndex = 4, Diagnostics = diagnostics
        };
        DetectionOrientationAnalyzer.Annotate(detection, expected, QrPolarity.DarkOnLight);
        DetectionOrientationAnalyzer.Annotate(detection, expected, QrPolarity.DarkOnLight);

        Assert.That(detection.HasMirrorEstimate, Is.True);
        Assert.That(detection.IsMirrored, Is.True);
        Assert.That(detection.Polarity, Is.EqualTo(QrPolarity.LightOnDark));
        Assert.That(sampled.Values, Is.EqualTo(values));
        Assert.That(sampled.Roles, Is.EqualTo(roles));
        Assert.That(sampled.Confidence, Is.EqualTo(confidence));
        Assert.That(expected.Values, Is.EqualTo(expectedValues));
        Assert.That(detection.Corners, Is.SameAs(corners));
        Assert.That(corners.TopLeft, Is.SameAs(topLeft));
        Assert.That(topLeft.X, Is.EqualTo(x));
        Assert.That(topLeft.Y, Is.EqualTo(y));
        Assert.That(detection.EstimatedVersion, Is.EqualTo(7));
        Assert.That(detection.EstimatedWidth, Is.EqualTo(45));
        Assert.That(detection.EstimatedHeight, Is.EqualTo(45));
        Assert.That(detection.Confidence, Is.EqualTo(0.9));
        Assert.That(detection.ModuleSize, Is.EqualTo(3));
        Assert.That(detection.CandidateIndex, Is.EqualTo(4));
        Assert.That(detection.Diagnostics, Is.SameAs(diagnostics));
    }

    [Test]
    public void IgnoresPayloadEvidenceAndToleratesLimitedFunctionDamage()
    {
        QrMatrix expected = CreateSymbol(QrSymbolKind.Model2).Matrix!;
        QrMatrix sampled = Transform(expected, true, false);
        int damaged = 0;
        for (int index = 0; index < expected.Values.Length; index++)
        {
            QrModuleRole role = expected.Roles![index];
            if (role == QrModuleRole.Data || role == QrModuleRole.ErrorCorrection)
                expected.Values[index] = QrModuleValue.Unknown;
            if (role == QrModuleRole.Format && damaged < 2)
            {
                int sampleIndex = index % expected.Width * sampled.Width + index / expected.Width;
                sampled.Values[sampleIndex] = QrModuleValue.Unknown;
                damaged++;
            }
        }
        QrDetection detection = new QrDetection { SampledMatrix = sampled };
        DetectionOrientationAnalyzer.Annotate(detection, expected, QrPolarity.DarkOnLight);
        Assert.That(detection.HasMirrorEstimate, Is.True);
        Assert.That(detection.IsMirrored, Is.True);
        Assert.That(detection.Polarity, Is.EqualTo(QrPolarity.DarkOnLight));
    }

    [Test]
    public void SymmetricFinderGeometryAloneDoesNotClaimMirrorCertainty()
    {
        QrMatrix expected = CreateSymbol(QrSymbolKind.Model2).Matrix!;
        for (int index = 0; index < expected.Values.Length; index++)
            if (expected.Roles![index] != QrModuleRole.Finder)
                expected.Values[index] = QrModuleValue.Unknown;
        QrDetection detection = new QrDetection { SampledMatrix = Transform(expected, true, true) };
        DetectionOrientationAnalyzer.Annotate(detection, expected, QrPolarity.DarkOnLight);
        Assert.That(detection.HasMirrorEstimate, Is.False);
        Assert.That(detection.IsMirrored, Is.False);
        Assert.That(detection.Polarity, Is.EqualTo(QrPolarity.LightOnDark));
    }

    [TestCase(true)]
    [TestCase(false)]
    public void MissingEvidenceClearsStaleEstimates(bool missingSamples)
    {
        QrMatrix expected = CreateSymbol(QrSymbolKind.Micro).Matrix!;
        QrMatrix sampled = Transform(expected, false, false);
        Array.Fill(missingSamples ? sampled.Values : expected.Values, QrModuleValue.Unknown);
        QrDetection detection = new QrDetection
        {
            SampledMatrix = sampled, HasMirrorEstimate = true, IsMirrored = true, Polarity = QrPolarity.LightOnDark
        };
        DetectionOrientationAnalyzer.Annotate(detection, expected, QrPolarity.DarkOnLight);
        Assert.That(detection.HasMirrorEstimate, Is.False);
        Assert.That(detection.IsMirrored, Is.False);
        Assert.That(detection.Polarity, Is.EqualTo(QrPolarity.Automatic));
    }

    [Test]
    public void RejectsUniformFalsePositivesAndDoesNotInventUnknownLightBits()
    {
        QrMatrix expected = CreateSymbol(QrSymbolKind.Model2).Matrix!;
        QrMatrix sampled = Transform(expected, false, false);
        Array.Fill(sampled.Values, QrModuleValue.Light);
        QrDetection detection = new QrDetection { SampledMatrix = sampled };
        DetectionOrientationAnalyzer.Annotate(detection, expected, QrPolarity.DarkOnLight);
        Assert.That(detection.HasMirrorEstimate, Is.False);
        Assert.That(detection.Polarity, Is.EqualTo(QrPolarity.Automatic));
    }

    [Test]
    public void RejectsInvalidInputsBeforeChangingMetadata()
    {
        QrMatrix expected = CreateSymbol(QrSymbolKind.Micro).Matrix!;
        QrDetection detection = new QrDetection
        {
            SampledMatrix = Transform(expected, false, false), HasMirrorEstimate = true,
            IsMirrored = true, Polarity = QrPolarity.LightOnDark
        };
        Assert.Throws<QrInputException>(() => DetectionOrientationAnalyzer.Annotate(null!, expected, QrPolarity.DarkOnLight));
        Assert.Throws<QrInputException>(() => DetectionOrientationAnalyzer.Annotate(new QrDetection(), expected, QrPolarity.DarkOnLight));
        Assert.Throws<QrInputException>(() => DetectionOrientationAnalyzer.Annotate(detection, null!, QrPolarity.DarkOnLight));
        Assert.Throws<QrInputException>(() => DetectionOrientationAnalyzer.Annotate(detection, expected, QrPolarity.Automatic));
        Assert.Throws<QrInputException>(() => DetectionOrientationAnalyzer.Annotate(detection, expected, (QrPolarity)99));
        detection.Corners = new QrQuadrilateral();
        Assert.Throws<QrInputException>(() => DetectionOrientationAnalyzer.Annotate(detection, expected, QrPolarity.DarkOnLight));
        detection.Corners = CreateCorners(17, 17, 0, false);
        detection.Corners.TopLeft!.X = double.NaN;
        Assert.Throws<QrInputException>(() => DetectionOrientationAnalyzer.Annotate(detection, expected, QrPolarity.DarkOnLight));
        Assert.That(detection.HasMirrorEstimate, Is.True);
        Assert.That(detection.IsMirrored, Is.True);
        Assert.That(detection.Polarity, Is.EqualTo(QrPolarity.LightOnDark));
    }

    [Test]
    public void RejectsMalformedMatricesAndMismatchedDimensions()
    {
        QrMatrix expected = CreateSymbol(QrSymbolKind.Micro).Matrix!;
        QrMatrix[] invalid =
        [
            new QrMatrix(),
            new QrMatrix { Width = 1, Height = 1, Values = null! },
            new QrMatrix { Width = 1, Height = 1, Values = [(QrModuleValue)99] },
            new QrMatrix { Width = 1, Height = 1, Values = [QrModuleValue.Dark], Roles = [] },
            new QrMatrix { Width = 1, Height = 1, Values = [QrModuleValue.Dark], Roles = [(QrModuleRole)99] },
            new QrMatrix { Width = 2, Height = 1, Values = [QrModuleValue.Dark, QrModuleValue.Dark] }
        ];
        foreach (QrMatrix matrix in invalid)
        {
            Assert.Throws<QrInputException>(() => DetectionOrientationAnalyzer.Annotate(
                new QrDetection { SampledMatrix = matrix }, expected, QrPolarity.DarkOnLight));
            Assert.Throws<QrInputException>(() => DetectionOrientationAnalyzer.Annotate(
                new QrDetection { SampledMatrix = expected }, matrix, QrPolarity.DarkOnLight));
        }
        Assert.Throws<QrResourceLimitException>(() => DetectionOrientationAnalyzer.Annotate(
            new QrDetection { SampledMatrix = new QrMatrix { Width = int.MaxValue, Height = 2 } },
            expected, QrPolarity.DarkOnLight));
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
        if (kind == QrSymbolKind.Micro)
        {
            options.MinimumVersion = 4;
            options.MaximumVersion = 4;
        }
        return new QrEncoder().EncodeText("12345", options);
    }

    static QrMatrix Transform(QrMatrix source, bool transpose, bool invert)
    {
        QrMatrix result = new QrMatrix
        {
            Width = transpose ? source.Height : source.Width,
            Height = transpose ? source.Width : source.Height,
            Values = new QrModuleValue[source.Values.Length]
        };
        for (int row = 0; row < source.Height; row++)
            for (int column = 0; column < source.Width; column++)
            {
                QrModuleValue value = source.Values[row * source.Width + column];
                if (invert && value != QrModuleValue.Unknown)
                    value = value == QrModuleValue.Dark ? QrModuleValue.Light : QrModuleValue.Dark;
                int index = transpose ? column * result.Width + row : row * result.Width + column;
                result.Values[index] = value;
            }
        return result;
    }

    static QrQuadrilateral CreateCorners(int width, int height, int turns, bool mirror)
    {
        QrPoint[] points = [new(), new() { X = width }, new() { X = width, Y = height }, new() { Y = height }];
        foreach (QrPoint point in points)
        {
            if (mirror) point.X = -point.X;
            for (int turn = 0; turn < turns; turn++)
                (point.X, point.Y) = (-point.Y, point.X);
            point.X += 200;
            point.Y += 200;
        }
        return new QrQuadrilateral { TopLeft = points[0], TopRight = points[1], BottomRight = points[2], BottomLeft = points[3] };
    }
}
