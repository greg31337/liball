namespace barcode.Tests;

public class CandidateScorerTests
{
    [Test]
    public void CompleteEvidenceOutranksWeakInvalidAndDisagreeingEvidence()
    {
        ModuleMatrix binary = new ModuleMatrix(44, 20);
        ManagedGrayImage gray = CreateGrayImage(44, 20, 240);
        DrawStripedRegion(binary, gray, 3, 4, 14, 10, 20);
        DrawStripedRegion(binary, gray, 25, 4, 14, 10, 180);
        FillBinaryRectangle(binary, 24, 3, 16, 1, true);

        CommonLocalizationCandidate strong = CreateLinearCandidate(3, 4, 14, 10, 0.95);
        CommonLocalizationCandidate weak = CreateLinearCandidate(25, 4, 14, 10, 0.50);
        CandidateDecodeEvidence strongDecode = CreateChecksumEvidence(true, 5, 5);
        CandidateDecodeEvidence weakDecode = CreateChecksumEvidence(false, 5, 1);

        CandidateScore[] scores = CandidateScorer.ScoreAndRank(
            binary,
            gray,
            new CommonLocalizationCandidate[] { weak, strong },
            new CandidateDecodeEvidence[] { weakDecode, strongDecode });

        Assert.Multiple(() =>
        {
            Assert.That(scores[0].Source, Is.SameAs(strong));
            Assert.That(scores[0].FormatEvidence, Is.GreaterThan(scores[1].FormatEvidence));
            Assert.That(scores[0].QuietZoneEvidence, Is.GreaterThan(scores[1].QuietZoneEvidence));
            Assert.That(scores[0].ContrastEvidence, Is.GreaterThan(scores[1].ContrastEvidence));
            Assert.That(scores[0].IntegrityEvidence, Is.EqualTo(1.0));
            Assert.That(scores[0].AgreementEvidence, Is.EqualTo(1.0));
            Assert.That(scores[1].IntegrityEvidence, Is.EqualTo(0.0));
            Assert.That(scores[1].AgreementEvidence, Is.EqualTo(0.2));
            Assert.That(scores[0].Total, Is.GreaterThan(scores[1].Total));
            Assert.That(scores[0].Candidate.Confidence, Is.EqualTo(scores[0].Total));
            Assert.That(strong.Candidate.Confidence, Is.EqualTo(0.95));
        });
    }

    [Test]
    public void SpecializedFinderComponentsDriveFormatEvidence()
    {
        ModuleMatrix binary = new ModuleMatrix(32, 16);
        ManagedGrayImage gray = CreateGrayImage(32, 16, 230);
        DrawStripedRegion(binary, gray, 2, 3, 10, 9, 20);
        DrawStripedRegion(binary, gray, 19, 3, 10, 9, 20);
        CommonLocalizationCandidate dataMatrix = CreateCandidate(
            CommonCandidateKind.DataMatrix,
            BarcodeFormat.DataMatrix,
            BarcodeDimension.Matrix,
            2,
            3,
            10,
            9,
            0.1);
        DataMatrixFinderEvidence dataMatrixEvidence = new DataMatrixFinderEvidence();
        dataMatrixEvidence.SolidBorderScore = 0.9;
        dataMatrixEvidence.ClockTrackScore = 0.8;
        dataMatrixEvidence.DimensionScore = 0.7;
        dataMatrixEvidence.Confidence = 0.6;
        dataMatrix.DataMatrixEvidence = dataMatrixEvidence;
        CommonLocalizationCandidate pdf417 = CreateCandidate(
            CommonCandidateKind.Pdf417,
            BarcodeFormat.Pdf417,
            BarcodeDimension.StackedLinear,
            19,
            3,
            10,
            9,
            0.9);
        Pdf417FinderEvidence pdfEvidence = new Pdf417FinderEvidence();
        pdfEvidence.StartLineScore = 0.4;
        pdfEvidence.StopLineScore = 0.4;
        pdfEvidence.DimensionScore = 0.4;
        pdfEvidence.LineCoverage = 0.4;
        pdfEvidence.Confidence = 0.4;
        pdf417.Pdf417Evidence = pdfEvidence;

        CandidateScore[] scores = CandidateScorer.ScoreAndRank(
            binary,
            gray,
            new CommonLocalizationCandidate[] { pdf417, dataMatrix },
            null);

        Assert.Multiple(() =>
        {
            Assert.That(scores[0].Source, Is.SameAs(dataMatrix));
            Assert.That(scores[0].FormatEvidence, Is.EqualTo(0.77).Within(0.000000001));
            Assert.That(scores[1].FormatEvidence, Is.EqualTo(0.4).Within(0.000000001));
            Assert.That(scores[0].IntegrityEvidence, Is.Zero);
            Assert.That(scores[0].AgreementEvidence, Is.Zero);
        });
    }

    [Test]
    public void QuietZoneAndContrastAreMeasuredFromTheCandidateNeighborhood()
    {
        ModuleMatrix binary = new ModuleMatrix(34, 18);
        ManagedGrayImage gray = CreateGrayImage(34, 18, 210);
        DrawStripedRegion(binary, gray, 3, 4, 10, 9, 10);
        DrawStripedRegion(binary, gray, 21, 4, 10, 9, 190);
        FillBinaryRectangle(binary, 20, 3, 12, 1, true);
        FillBinaryRectangle(binary, 20, 13, 12, 1, true);
        CommonLocalizationCandidate clear = CreateLinearCandidate(3, 4, 10, 9, 0.8);
        CommonLocalizationCandidate obstructed = CreateLinearCandidate(21, 4, 10, 9, 0.8);

        CandidateScore[] scores = CandidateScorer.ScoreAndRank(
            binary,
            gray,
            new CommonLocalizationCandidate[] { obstructed, clear },
            null);

        Assert.Multiple(() =>
        {
            Assert.That(scores[0].Source, Is.SameAs(clear));
            Assert.That(scores[0].QuietZoneEvidence, Is.EqualTo(1.0));
            Assert.That(scores[1].QuietZoneEvidence, Is.LessThan(0.6));
            Assert.That(scores[0].ContrastEvidence, Is.GreaterThan(0.75));
            Assert.That(scores[1].ContrastEvidence, Is.LessThan(0.1));
        });
    }

    [Test]
    public void EccCorrectionsReduceIntegrityAndExactTiesRemainStable()
    {
        ModuleMatrix binary = new ModuleMatrix(30, 16);
        ManagedGrayImage gray = CreateGrayImage(30, 16, 240);
        DrawStripedRegion(binary, gray, 2, 3, 10, 10, 20);
        DrawStripedRegion(binary, gray, 18, 3, 10, 10, 20);
        CommonLocalizationCandidate first = CreateCandidate(
            CommonCandidateKind.DataMatrix,
            BarcodeFormat.DataMatrix,
            BarcodeDimension.Matrix,
            2,
            3,
            10,
            10,
            0.8);
        CommonLocalizationCandidate second = CreateCandidate(
            CommonCandidateKind.DataMatrix,
            BarcodeFormat.DataMatrix,
            BarcodeDimension.Matrix,
            18,
            3,
            10,
            10,
            0.8);
        CandidateDecodeEvidence firstDecode = CreateEccEvidence(10, 2, 1, 4, 3);
        CandidateDecodeEvidence secondDecode = CreateEccEvidence(10, 2, 1, 4, 3);

        CandidateScore[] scores = CandidateScorer.ScoreAndRank(
            binary,
            gray,
            new CommonLocalizationCandidate[] { first, second },
            new CandidateDecodeEvidence[] { firstDecode, secondDecode });

        Assert.Multiple(() =>
        {
            Assert.That(scores[0].OriginalIndex, Is.EqualTo(0));
            Assert.That(scores[1].OriginalIndex, Is.EqualTo(1));
            Assert.That(scores[0].IntegrityEvidence, Is.EqualTo(0.7).Within(0.000000001));
            Assert.That(scores[0].AgreementEvidence, Is.EqualTo(0.75));
            Assert.That(scores[0].GeometryEvidence, Is.EqualTo(1.0));
        });
    }

    [Test]
    public void ScorerRejectsMismatchedImagesCandidatesAndDecodeEvidence()
    {
        ModuleMatrix binary = new ModuleMatrix(10, 10);
        ManagedGrayImage wrongSize = CreateGrayImage(9, 10, 255);
        ManagedGrayImage gray = CreateGrayImage(10, 10, 255);
        CommonLocalizationCandidate candidate = CreateLinearCandidate(2, 2, 6, 6, 0.5);
        CandidateDecodeEvidence invalid = new CandidateDecodeEvidence();
        invalid.AttemptCount = 1;
        invalid.AgreementCount = 2;

        Assert.Multiple(() =>
        {
            Assert.That(
                () => CandidateScorer.ScoreAndRank(
                    binary,
                    wrongSize,
                    new CommonLocalizationCandidate[] { candidate },
                    null),
                Throws.TypeOf<BarcodeValidationException>());
            Assert.That(
                () => CandidateScorer.ScoreAndRank(
                    binary,
                    gray,
                    new CommonLocalizationCandidate[] { candidate },
                    new CandidateDecodeEvidence[0]),
                Throws.TypeOf<BarcodeValidationException>());
            Assert.That(
                () => CandidateScorer.ScoreAndRank(
                    binary,
                    gray,
                    new CommonLocalizationCandidate[] { candidate },
                    new CandidateDecodeEvidence[] { invalid }),
                Throws.TypeOf<BarcodeValidationException>());
        });
    }

    static CommonLocalizationCandidate CreateLinearCandidate(
        int left,
        int top,
        int width,
        int height,
        double confidence)
    {
        CommonLocalizationCandidate candidate = CreateCandidate(
            CommonCandidateKind.Linear,
            BarcodeFormat.Code128,
            BarcodeDimension.Linear,
            left,
            top,
            width,
            height,
            confidence);
        LinearBarcodeCandidate evidence = new LinearBarcodeCandidate();
        evidence.OrientationScore = confidence;
        evidence.Location = candidate.Candidate.Location!;
        evidence.Bounds = candidate.Candidate.Location!.Bounds!;
        candidate.LinearEvidence = evidence;
        return candidate;
    }

    static CommonLocalizationCandidate CreateCandidate(
        CommonCandidateKind kind,
        BarcodeFormat format,
        BarcodeDimension dimension,
        int left,
        int top,
        int width,
        int height,
        double confidence)
    {
        BarcodeQuadrilateral location = GeometryMath.CreateQuadrilateral(
            GeometryMath.CreatePoint(left, top),
            GeometryMath.CreatePoint(left + width, top),
            GeometryMath.CreatePoint(left + width, top + height),
            GeometryMath.CreatePoint(left, top + height));
        BarcodeCandidate publicCandidate = new BarcodeCandidate();
        publicCandidate.Format = format;
        publicCandidate.Dimension = dimension;
        publicCandidate.Location = location;
        publicCandidate.Confidence = confidence;
        publicCandidate.Polarity = BarcodePolarity.DarkOnLight;
        publicCandidate.MirrorMode = BarcodeMirrorMode.Normal;
        publicCandidate.Diagnostics = new BarcodeDiagnostic[0];
        CommonLocalizationCandidate candidate = new CommonLocalizationCandidate();
        candidate.Candidate = publicCandidate;
        candidate.Kind = kind;
        return candidate;
    }

    static CandidateDecodeEvidence CreateChecksumEvidence(
        bool valid,
        int attempts,
        int agreements)
    {
        BarcodeDecodeMetadata metadata = new BarcodeDecodeMetadata();
        metadata.HasChecksum = true;
        metadata.ChecksumValid = valid;
        BarcodeDecodeResult result = new BarcodeDecodeResult();
        result.Dimension = BarcodeDimension.Linear;
        result.Metadata = metadata;
        CandidateDecodeEvidence evidence = new CandidateDecodeEvidence();
        evidence.Result = result;
        evidence.AttemptCount = attempts;
        evidence.AgreementCount = agreements;
        return evidence;
    }

    static CandidateDecodeEvidence CreateEccEvidence(
        int codewordCount,
        int corrected,
        int erasures,
        int attempts,
        int agreements)
    {
        BarcodeDecodeMetadata metadata = new BarcodeDecodeMetadata();
        metadata.Codewords = new int[codewordCount];
        metadata.ErrorCorrectionApplied = corrected > 0 || erasures > 0;
        metadata.CorrectedErrorCount = corrected;
        metadata.ErasureCount = erasures;
        BarcodeDecodeResult result = new BarcodeDecodeResult();
        result.Dimension = BarcodeDimension.Matrix;
        result.Metadata = metadata;
        CandidateDecodeEvidence evidence = new CandidateDecodeEvidence();
        evidence.Result = result;
        evidence.AttemptCount = attempts;
        evidence.AgreementCount = agreements;
        return evidence;
    }

    static ManagedGrayImage CreateGrayImage(int width, int height, int value)
    {
        ManagedGrayImage image = new ManagedGrayImage(width, height);
        int y = 0;
        while (y < height)
        {
            int x = 0;
            while (x < width)
            {
                image.WriteSample(x, y, value);
                x = x + 1;
            }

            y = y + 1;
        }

        return image;
    }

    static void DrawStripedRegion(
        ModuleMatrix binary,
        ManagedGrayImage gray,
        int left,
        int top,
        int width,
        int height,
        int darkValue)
    {
        int x = left;
        while (x < left + width)
        {
            if ((x - left) % 2 == 0)
            {
                int y = top;
                while (y < top + height)
                {
                    binary.SetModule(x, y, true);
                    gray.WriteSample(x, y, darkValue);
                    y = y + 1;
                }
            }

            x = x + 1;
        }
    }

    static void FillBinaryRectangle(
        ModuleMatrix matrix,
        int left,
        int top,
        int width,
        int height,
        bool value)
    {
        int y = top;
        while (y < top + height)
        {
            int x = left;
            while (x < left + width)
            {
                matrix.SetModule(x, y, value);
                x = x + 1;
            }

            y = y + 1;
        }
    }
}
