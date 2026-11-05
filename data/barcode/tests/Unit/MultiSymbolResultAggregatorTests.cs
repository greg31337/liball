namespace barcode.Tests;

public class MultiSymbolResultAggregatorTests
{
    [Test]
    public void ExactDuplicatesCollapseToTheStrongestDefensiveResult()
    {
        BarcodeDecodeResult weak = CreateResult(
            "SAME",
            BarcodeFormat.Code128,
            10,
            10,
            20,
            8,
            0.4,
            false,
            false);
        BarcodeDecodeResult strong = CreateResult(
            "SAME",
            BarcodeFormat.Code128,
            11,
            10,
            20,
            8,
            0.9,
            false,
            false);

        MultiSymbolAggregationResult aggregation = MultiSymbolResultAggregator.Combine(
            new BarcodeDecodeResult[] { weak, strong },
            10,
            0);
        strong.Payload!.RawBytes![0] = 0;
        strong.Location!.TopLeft!.X = 999.0;

        Assert.Multiple(() =>
        {
            Assert.That(aggregation.Results, Has.Length.EqualTo(1));
            Assert.That(aggregation.UniqueCount, Is.EqualTo(1));
            Assert.That(aggregation.DuplicateCount, Is.EqualTo(1));
            Assert.That(aggregation.ConflictCount, Is.Zero);
            Assert.That(aggregation.OriginalIndexes, Is.EqualTo(new int[] { 1 }));
            Assert.That(aggregation.Results[0].Metadata!.Confidence, Is.EqualTo(0.9));
            Assert.That(aggregation.Results[0].Payload!.RawBytes![0], Is.EqualTo((byte)'S'));
            Assert.That(aggregation.Results[0].Location!.TopLeft!.X, Is.EqualTo(11.0));
        });
    }

    [Test]
    public void GeometryPayloadConflictsPreferVerifiedIntegrityBeforeConfidence()
    {
        BarcodeDecodeResult invalid = CreateResult(
            "WRONG",
            BarcodeFormat.Code128,
            5,
            5,
            30,
            10,
            0.99,
            true,
            false);
        BarcodeDecodeResult verified = CreateResult(
            "RIGHT",
            BarcodeFormat.Code128,
            5,
            5,
            30,
            10,
            0.6,
            true,
            true);
        verified.Metadata!.ErrorCorrectionApplied = true;
        verified.Metadata.CorrectedErrorCount = 1;

        MultiSymbolAggregationResult aggregation = MultiSymbolResultAggregator.Combine(
            new BarcodeDecodeResult[] { invalid, verified },
            10,
            10);

        Assert.Multiple(() =>
        {
            Assert.That(aggregation.Results, Has.Length.EqualTo(1));
            Assert.That(aggregation.DuplicateCount, Is.Zero);
            Assert.That(aggregation.ConflictCount, Is.EqualTo(1));
            Assert.That(aggregation.Results[0].Payload!.Text, Is.EqualTo("RIGHT"));
            Assert.That(aggregation.Results[0].Metadata!.ChecksumValid, Is.True);
            Assert.That(aggregation.OriginalIndexes, Is.EqualTo(new int[] { 1 }));
            Assert.That(aggregation.Diagnostics, Has.Length.EqualTo(3));
            Assert.That(aggregation.Diagnostics[0].Code,
                Is.EqualTo(DiagnosticCodes.ErrorCorrection));
            Assert.That(aggregation.Diagnostics[1].Code,
                Is.EqualTo(DiagnosticCodes.ResultConfidence));
            Assert.That(aggregation.Diagnostics[2].Code,
                Is.EqualTo(DiagnosticCodes.ResultCoordinates));
        });
    }

    [Test]
    public void EqualPayloadsAtDistinctLocationsRemainInStableReadingOrder()
    {
        BarcodeDecodeResult bottom = CreateResult(
            "VALUE", BarcodeFormat.Code39, 5, 50, 20, 10, 0.5, false, false);
        BarcodeDecodeResult topRight = CreateResult(
            "VALUE", BarcodeFormat.Code39, 50, 5, 20, 10, 0.5, false, false);
        BarcodeDecodeResult topLeft = CreateResult(
            "VALUE", BarcodeFormat.Code39, 5, 7, 20, 10, 0.5, false, false);
        BarcodeDecodeResult withoutLocation = CreateResult(
            "VALUE", BarcodeFormat.Code39, 0, 0, 1, 1, 0.5, false, false);
        withoutLocation.Location = null;

        MultiSymbolAggregationResult aggregation = MultiSymbolResultAggregator.Combine(
            new BarcodeDecodeResult[] { bottom, topRight, topLeft, withoutLocation },
            3,
            0);

        Assert.Multiple(() =>
        {
            Assert.That(aggregation.UniqueCount, Is.EqualTo(4));
            Assert.That(aggregation.Results, Has.Length.EqualTo(3));
            Assert.That(aggregation.OriginalIndexes, Is.EqualTo(new int[] { 2, 1, 0 }));
            Assert.That(aggregation.DuplicateCount, Is.Zero);
            Assert.That(aggregation.ConflictCount, Is.Zero);
            Assert.That(aggregation.MaximumReached, Is.True);
        });
    }

    [Test]
    public void StronglyOverlappingSymbolsWithSeparatedCentersRemainDistinct()
    {
        BarcodeDecodeResult first = CreateResult(
            "FIRST", BarcodeFormat.Code128, 0, 0, 10, 10, 0.8, false, false);
        BarcodeDecodeResult second = CreateResult(
            "SECOND", BarcodeFormat.Code128, 3, 0, 10, 10, 0.8, false, false);

        MultiSymbolAggregationResult aggregation = MultiSymbolResultAggregator.Combine(
            new BarcodeDecodeResult[] { second, first },
            10,
            0);

        Assert.Multiple(() =>
        {
            Assert.That(aggregation.Results, Has.Length.EqualTo(2));
            Assert.That(aggregation.ConflictCount, Is.Zero);
            Assert.That(aggregation.Results[0].Payload!.Text, Is.EqualTo("FIRST"));
            Assert.That(aggregation.Results[1].Payload!.Text, Is.EqualTo("SECOND"));
        });
    }

    [Test]
    public void CandidateAggregationSuppressesRepeatsAndResolvesFormatConflicts()
    {
        ImagePipelineCandidate weak = CreateCandidate(
            BarcodeFormat.Code128,
            CommonCandidateKind.Linear,
            10,
            10,
            20,
            8,
            0.3,
            0);
        ImagePipelineCandidate strong = CreateCandidate(
            BarcodeFormat.Code128,
            CommonCandidateKind.Linear,
            11,
            10,
            20,
            8,
            0.9,
            1);
        ImagePipelineCandidate conflicting = CreateCandidate(
            BarcodeFormat.DataMatrix,
            CommonCandidateKind.DataMatrix,
            11,
            10,
            20,
            8,
            0.7,
            2);
        ImagePipelineCandidate separate = CreateCandidate(
            BarcodeFormat.Code128,
            CommonCandidateKind.Linear,
            40,
            10,
            20,
            8,
            0.5,
            3);

        ImageCandidateAggregationResult aggregation = ImageCandidateAggregator.Combine(
            new ImagePipelineCandidate[] { weak, strong, conflicting, separate },
            10);

        Assert.Multiple(() =>
        {
            Assert.That(aggregation.Candidates, Has.Length.EqualTo(2));
            Assert.That(aggregation.UniqueCount, Is.EqualTo(2));
            Assert.That(aggregation.DuplicateCount, Is.EqualTo(1));
            Assert.That(aggregation.ConflictCount, Is.EqualTo(1));
            Assert.That(aggregation.Candidates[0], Is.SameAs(strong));
            Assert.That(aggregation.Candidates[1], Is.SameAs(separate));
        });
    }

    [Test]
    public void NullInputsAndInvalidLimitsAreRejected()
    {
        Assert.Multiple(() =>
        {
            Assert.That(
                () => MultiSymbolResultAggregator.Combine(null!, 1, 0),
                Throws.TypeOf<BarcodeValidationException>());
            Assert.That(
                () => MultiSymbolResultAggregator.Combine(
                    new BarcodeDecodeResult[] { null! },
                    1,
                    0),
                Throws.TypeOf<BarcodeValidationException>());
            Assert.That(
                () => MultiSymbolResultAggregator.Combine(
                    new BarcodeDecodeResult[0],
                    -1,
                    0),
                Throws.TypeOf<BarcodeValidationException>());
            Assert.That(
                () => MultiSymbolResultAggregator.Combine(
                    new BarcodeDecodeResult[0],
                    1,
                    -1),
                Throws.TypeOf<BarcodeValidationException>());
            Assert.That(
                () => ImageCandidateAggregator.Combine(
                    new ImagePipelineCandidate[0],
                    0),
                Throws.TypeOf<BarcodeValidationException>());
        });
    }

    static BarcodeDecodeResult CreateResult(
        string text,
        BarcodeFormat format,
        double left,
        double top,
        double width,
        double height,
        double confidence,
        bool hasChecksum,
        bool checksumValid)
    {
        BarcodePayload payload = new BarcodePayload();
        payload.Text = text;
        payload.RawBytes = global::System.Text.Encoding.ASCII.GetBytes(text);
        BarcodeDecodeMetadata metadata = new BarcodeDecodeMetadata();
        metadata.Confidence = confidence;
        metadata.HasChecksum = hasChecksum;
        metadata.ChecksumValid = checksumValid;
        BarcodeDecodeResult result = new BarcodeDecodeResult();
        result.Payload = payload;
        result.Format = format;
        result.Dimension = FormatDispatcher.ResolveDimension(format);
        result.Location = CreateLocation(left, top, width, height);
        result.Metadata = metadata;
        return result;
    }

    static ImagePipelineCandidate CreateCandidate(
        BarcodeFormat format,
        CommonCandidateKind kind,
        double left,
        double top,
        double width,
        double height,
        double score,
        int stageIndex)
    {
        BarcodeCandidate candidate = new BarcodeCandidate();
        candidate.Format = format;
        candidate.Dimension = FormatDispatcher.ResolveDimension(format);
        candidate.Location = CreateLocation(left, top, width, height);
        CommonLocalizationCandidate localization = new CommonLocalizationCandidate();
        localization.Candidate = candidate;
        localization.Kind = kind;
        CandidateScore candidateScore = new CandidateScore();
        candidateScore.Source = localization;
        candidateScore.Candidate = candidate;
        candidateScore.Total = score;
        ImagePipelineCandidate pipelineCandidate = new ImagePipelineCandidate();
        pipelineCandidate.Localization = localization;
        pipelineCandidate.Score = candidateScore;
        pipelineCandidate.StageIndex = stageIndex;
        return pipelineCandidate;
    }

    static BarcodeQuadrilateral CreateLocation(
        double left,
        double top,
        double width,
        double height)
    {
        return GeometryMath.CreateQuadrilateral(
            GeometryMath.CreatePoint(left, top),
            GeometryMath.CreatePoint(left + width, top),
            GeometryMath.CreatePoint(left + width, top + height),
            GeometryMath.CreatePoint(left, top + height));
    }
}
