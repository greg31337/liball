namespace barcode.Tests;

public class DataMatrixProjectiveDecoderTests
{
    [Test]
    public void ProjectiveModuleVotingRecoversPayloadAndImageMetadata()
    {
        byte[] data = global::System.Text.Encoding.ASCII.GetBytes("PROJECTIVE GRID");
        DataMatrixEncodingResult encoded = EncodeVersion(8, data);
        ModuleMatrix image = new ModuleMatrix(145, 125);
        BarcodeQuadrilateral location = GeometryMath.CreateQuadrilateral(
            GeometryMath.CreatePoint(20.0, 16.0),
            GeometryMath.CreatePoint(121.0, 22.0),
            GeometryMath.CreatePoint(112.0, 108.0),
            GeometryMath.CreatePoint(14.0, 101.0));
        DrawProjective(image, encoded.SymbolMatrix, location, false, false);
        DataMatrixFinderEvidence[] found = DataMatrixImageFinder.Locate(
            image,
            BarcodePolarity.DarkOnLight);

        DataMatrixFinderEvidence evidence = FindVersionEvidence(found, 8);
        DataMatrixImageDecodeResult? attempted = DataMatrixProjectiveDecoder.TryDecodeCandidate(
            image,
            evidence,
            null);
        Assert.That(
            attempted,
            Is.Not.Null,
            $"Finder rotation {evidence.Rotation}, mirror {evidence.MirrorMode}, location {FormatLocation(evidence.Location)}");
        DataMatrixImageDecodeResult decoded = attempted!;

        Assert.Multiple(() =>
        {
            Assert.That(decoded.Decode.Result.Payload!.RawBytes, Is.EqualTo(data));
            Assert.That(decoded.Decode.SymbolInfo.Version, Is.EqualTo(8));
            Assert.That(decoded.Decode.Result.Location, Is.Not.Null);
            Assert.That(decoded.Decode.Result.Metadata!.CorrectedErrorCount, Is.Zero);
            Assert.That(decoded.GridSample.Modules.CopyModules(), Is.EqualTo(encoded.SymbolMatrix.CopyModules()));
            Assert.That(decoded.GridSample.VoteConfidence, Is.GreaterThanOrEqualTo(0.95));
            Assert.That(decoded.GridSample.UncertainModuleCount, Is.LessThanOrEqualTo(1));
            Assert.That(decoded.Decode.Result.OrientationDegrees, Is.GreaterThan(0.0));
            Assert.That(decoded.Decode.Result.OrientationDegrees, Is.LessThan(10.0));
        });
    }

    [Test]
    public void CentreVotingRejectsBoundaryNoiseWithoutChangingModules()
    {
        byte[] data = global::System.Text.Encoding.ASCII.GetBytes("VOTING");
        DataMatrixEncodingResult encoded = EncodeVersion(5, data);
        ModuleMatrix image = new ModuleMatrix(120, 115);
        BarcodeQuadrilateral location = GeometryMath.CreateQuadrilateral(
            GeometryMath.CreatePoint(17.0, 13.0),
            GeometryMath.CreatePoint(103.0, 16.0),
            GeometryMath.CreatePoint(99.0, 101.0),
            GeometryMath.CreatePoint(13.0, 97.0));
        DrawProjective(image, encoded.SymbolMatrix, location, false, true);
        DataMatrixFinderEvidence evidence = CreateEvidence(
            encoded.SymbolInfo,
            location,
            BarcodeRotation.None,
            BarcodePolarity.DarkOnLight);

        DataMatrixGridSampleResult sampled = DataMatrixProjectiveDecoder.SampleCandidate(
            image,
            encoded.SymbolInfo,
            BarcodeRotation.None,
            location,
            0.0,
            0.0);
        DataMatrixImageDecodeResult decoded = DataMatrixProjectiveDecoder.DecodeCandidate(
            image,
            evidence,
            null);

        Assert.Multiple(() =>
        {
            Assert.That(sampled.Modules.CopyModules(), Is.EqualTo(encoded.SymbolMatrix.CopyModules()));
            Assert.That(sampled.VoteConfidence, Is.GreaterThan(0.70));
            Assert.That(decoded.Decode.Result.Payload!.RawBytes, Is.EqualTo(data));
            Assert.That(decoded.Decode.Result.Metadata!.CorrectedErrorCount, Is.Zero);
        });
    }

    [Test]
    public void LocalQuadrilateralAndGridPhaseRefinementRecoverShiftedEvidence()
    {
        byte[] data = global::System.Text.Encoding.ASCII.GetBytes("REFINE");
        DataMatrixEncodingResult encoded = EncodeVersion(4, data);
        ModuleMatrix image = new ModuleMatrix(105, 100);
        BarcodeQuadrilateral actual = GeometryMath.CreateQuadrilateral(
            GeometryMath.CreatePoint(18.0, 14.0),
            GeometryMath.CreatePoint(86.0, 18.0),
            GeometryMath.CreatePoint(81.0, 86.0),
            GeometryMath.CreatePoint(13.0, 81.0));
        DrawProjective(image, encoded.SymbolMatrix, actual, false, false);
        BarcodeQuadrilateral shifted = GeometryMath.CreateQuadrilateral(
            GeometryMath.CreatePoint(19.0, 15.0),
            GeometryMath.CreatePoint(87.0, 19.0),
            GeometryMath.CreatePoint(82.0, 87.0),
            GeometryMath.CreatePoint(14.0, 82.0));
        DataMatrixFinderEvidence evidence = CreateEvidence(
            encoded.SymbolInfo,
            shifted,
            BarcodeRotation.None,
            BarcodePolarity.DarkOnLight);

        DataMatrixImageDecodeResult decoded = DataMatrixProjectiveDecoder.DecodeCandidate(
            image,
            evidence,
            null);

        Assert.Multiple(() =>
        {
            Assert.That(decoded.Decode.Result.Payload!.RawBytes, Is.EqualTo(data));
            Assert.That(
                decoded.UsedRefinedQuadrilateral
                    || global::System.Math.Abs(decoded.GridSample.GridOffsetX) > 0.0
                    || global::System.Math.Abs(decoded.GridSample.GridOffsetY) > 0.0,
                Is.True);
            Assert.That(decoded.GridSample.Location.TopLeft!.X, Is.EqualTo(actual.TopLeft!.X).Within(3.0));
            Assert.That(decoded.GridSample.Location.BottomRight!.Y, Is.EqualTo(actual.BottomRight!.Y).Within(3.0));
            Assert.That(decoded.GridSample.VoteConfidence, Is.GreaterThanOrEqualTo(0.90));
        });
    }

    [Test]
    public void InvertedMirroredAndRotatedRectangularSymbolIsDecodedByDefaultAttempts()
    {
        byte[] data = global::System.Text.Encoding.ASCII.GetBytes("MIRR");
        DataMatrixEncodingResult encoded = EncodeVersion(25, data);
        ModuleMatrix observed = encoded.SymbolMatrix
            .ReflectHorizontal()
            .RotateClockwise90();
        ModuleMatrix image = new ModuleMatrix(90, 115);
        BarcodeQuadrilateral location = GeometryMath.CreateQuadrilateral(
            GeometryMath.CreatePoint(14.0, 12.0),
            GeometryMath.CreatePoint(55.0, 15.0),
            GeometryMath.CreatePoint(51.0, 103.0),
            GeometryMath.CreatePoint(10.0, 99.0));
        DrawProjective(image, observed, location, true, false);
        DataMatrixFinderEvidence[] found = DataMatrixImageFinder.Locate(
            image,
            BarcodePolarity.Automatic);

        DataMatrixFinderEvidence evidence = FindVersionEvidence(found, 25);
        DataMatrixImageDecodeResult? attempted = DataMatrixProjectiveDecoder.TryDecodeCandidate(
            image,
            evidence,
            null);
        Assert.That(
            attempted,
            Is.Not.Null,
            $"Finder rotation {evidence.Rotation}, mirror {evidence.MirrorMode}, location {FormatLocation(evidence.Location)}");
        DataMatrixImageDecodeResult decoded = attempted!;

        Assert.Multiple(() =>
        {
            Assert.That(evidence.SymbolInfo.Version, Is.EqualTo(25));
            Assert.That(evidence.Polarity, Is.EqualTo(BarcodePolarity.LightOnDark));
            Assert.That(decoded.Decode.Result.Payload!.RawBytes, Is.EqualTo(data));
            Assert.That(decoded.Decode.Result.Polarity, Is.EqualTo(BarcodePolarity.LightOnDark));
            Assert.That(decoded.Decode.Result.MirrorMode, Is.EqualTo(BarcodeMirrorMode.Mirrored));
            Assert.That(decoded.Decode.Result.Rotation, Is.EqualTo(BarcodeRotation.Clockwise90));
        });
    }

    [Test]
    public void CallerTransformRestrictionsArePreservedAcrossSamplingAttempts()
    {
        byte[] data = global::System.Text.Encoding.ASCII.GetBytes("OPTIONS");
        DataMatrixEncodingResult encoded = EncodeVersion(3, data);
        ModuleMatrix observed = encoded.SymbolMatrix.ReflectHorizontal();
        ModuleMatrix image = new ModuleMatrix(75, 75);
        BarcodeQuadrilateral location = GeometryMath.CreateQuadrilateral(
            GeometryMath.CreatePoint(8.0, 8.0),
            GeometryMath.CreatePoint(64.0, 8.0),
            GeometryMath.CreatePoint(64.0, 64.0),
            GeometryMath.CreatePoint(8.0, 64.0));
        DrawProjective(image, observed, location, false, false);
        DataMatrixFinderEvidence evidence = CreateEvidence(
            encoded.SymbolInfo,
            location,
            BarcodeRotation.None,
            BarcodePolarity.DarkOnLight);
        BarcodeDecodeOptions restricted = new BarcodeDecodeOptions();
        restricted.Polarity = BarcodePolarity.DarkOnLight;
        restricted.MirrorMode = BarcodeMirrorMode.Normal;
        BarcodeDecodeOptions allowed = new BarcodeDecodeOptions();
        allowed.Polarity = BarcodePolarity.Automatic;
        allowed.MirrorMode = BarcodeMirrorMode.Both;

        DataMatrixImageDecodeResult? rejected = DataMatrixProjectiveDecoder.TryDecodeCandidate(
            image,
            evidence,
            restricted);
        DataMatrixImageDecodeResult? accepted = DataMatrixProjectiveDecoder.TryDecodeCandidate(
            image,
            evidence,
            allowed);

        Assert.Multiple(() =>
        {
            Assert.That(rejected, Is.Null);
            Assert.That(accepted, Is.Not.Null);
            Assert.That(accepted!.Decode.Result.Payload!.RawBytes, Is.EqualTo(data));
            Assert.That(accepted.Decode.Result.MirrorMode, Is.EqualTo(BarcodeMirrorMode.Mirrored));
            Assert.That(restricted.MirrorMode, Is.EqualTo(BarcodeMirrorMode.Normal));
        });
    }

    [Test]
    public void SampledDataDamageIsCorrectedAfterProjectiveRecovery()
    {
        byte[] data = global::System.Text.Encoding.ASCII.GetBytes("A");
        DataMatrixEncodingResult encoded = EncodeVersion(1, data);
        ModuleMatrix damaged = CopyMatrix(encoded.SymbolMatrix);
        damaged.SetModule(2, 2, !damaged.GetModule(2, 2));
        ModuleMatrix image = new ModuleMatrix(60, 60);
        BarcodeQuadrilateral location = GeometryMath.CreateQuadrilateral(
            GeometryMath.CreatePoint(8.0, 8.0),
            GeometryMath.CreatePoint(47.0, 8.0),
            GeometryMath.CreatePoint(47.0, 47.0),
            GeometryMath.CreatePoint(8.0, 47.0));
        DrawProjective(image, damaged, location, false, false);
        DataMatrixFinderEvidence evidence = CreateEvidence(
            encoded.SymbolInfo,
            location,
            BarcodeRotation.None,
            BarcodePolarity.DarkOnLight);

        DataMatrixImageDecodeResult decoded = DataMatrixProjectiveDecoder.DecodeCandidate(
            image,
            evidence,
            null);

        Assert.Multiple(() =>
        {
            Assert.That(decoded.Decode.Result.Payload!.RawBytes, Is.EqualTo(data));
            Assert.That(decoded.Decode.Result.Metadata!.ErrorCorrectionApplied, Is.True);
            Assert.That(decoded.Decode.Result.Metadata.CorrectedErrorCount, Is.EqualTo(1));
        });
    }

    [Test]
    public void InvalidSamplingInputsAndUndecodableCandidatesReportFailure()
    {
        ModuleMatrix image = new ModuleMatrix(30, 30);
        DataMatrixSymbolInfo symbol = DataMatrixSymbolInfoTable.GetSymbolByVersion(1);
        BarcodeQuadrilateral location = GeometryMath.CreateQuadrilateral(
            GeometryMath.CreatePoint(2.0, 2.0),
            GeometryMath.CreatePoint(22.0, 2.0),
            GeometryMath.CreatePoint(22.0, 22.0),
            GeometryMath.CreatePoint(2.0, 22.0));
        DataMatrixFinderEvidence evidence = CreateEvidence(
            symbol,
            location,
            BarcodeRotation.None,
            BarcodePolarity.DarkOnLight);

        Assert.Multiple(() =>
        {
            Assert.That(
                () => DataMatrixProjectiveDecoder.SampleCandidate(null!, symbol, BarcodeRotation.None, location, 0.0, 0.0),
                Throws.TypeOf<BarcodeValidationException>());
            Assert.That(
                () => DataMatrixProjectiveDecoder.SampleCandidate(image, symbol, BarcodeRotation.None, location, 0.6, 0.0),
                Throws.TypeOf<BarcodeValidationException>());
            Assert.That(
                () => DataMatrixProjectiveDecoder.SampleCandidate(image, symbol, (BarcodeRotation)99, location, 0.0, 0.0),
                Throws.TypeOf<BarcodeValidationException>());
            Assert.That(
                DataMatrixProjectiveDecoder.TryDecodeCandidate(image, evidence, null),
                Is.Null);
            Assert.That(
                () => DataMatrixProjectiveDecoder.DecodeCandidate(image, evidence, null),
                Throws.TypeOf<BarcodeDecodingException>());
            Assert.That(
                () => DataMatrixProjectiveDecoder.TryDecodeCandidate(image, null!, null),
                Throws.TypeOf<BarcodeValidationException>());
        });
    }

    static DataMatrixEncodingResult EncodeVersion(int version, byte[] data)
    {
        DataMatrixSymbolInfo symbol = DataMatrixSymbolInfoTable.GetSymbolByVersion(version);
        DataMatrixOptions options = new DataMatrixOptions();
        options.Rows = symbol.SymbolRows;
        options.Columns = symbol.SymbolColumns;
        BarcodePayload payload = new BarcodePayload();
        payload.RawBytes = CopyBytes(data);
        payload.Text = global::System.Text.Encoding.ASCII.GetString(data);
        return DataMatrixEncoder.Encode(data, payload, options, null, false);
    }

    static DataMatrixFinderEvidence FindVersionEvidence(
        DataMatrixFinderEvidence[] evidence,
        int version)
    {
        int index = 0;
        while (index < evidence.Length)
        {
            if (evidence[index].SymbolInfo.Version == version)
            {
                return evidence[index];
            }

            index++;
        }

        throw new global::System.InvalidOperationException("Expected Data Matrix finder evidence was not produced.");
    }

    static DataMatrixFinderEvidence CreateEvidence(
        DataMatrixSymbolInfo symbol,
        BarcodeQuadrilateral location,
        BarcodeRotation rotation,
        BarcodePolarity polarity)
    {
        DataMatrixFinderEvidence evidence = new DataMatrixFinderEvidence();
        evidence.Location = CopyLocation(location);
        evidence.SymbolInfo = symbol.CopySymbolInfo();
        evidence.Rotation = rotation;
        evidence.Polarity = polarity;
        evidence.MirrorMode = BarcodeMirrorMode.Normal;
        evidence.SolidBorderScore = 1.0;
        evidence.ClockTrackScore = 1.0;
        evidence.DimensionScore = 1.0;
        evidence.ModuleWidth = 4.0;
        evidence.ModuleHeight = 4.0;
        evidence.Confidence = 1.0;
        return evidence;
    }

    static void DrawProjective(
        ModuleMatrix image,
        ModuleMatrix symbol,
        BarcodeQuadrilateral location,
        bool inverted,
        bool addBoundaryNoise)
    {
        if (inverted)
        {
            image.SetAllModules(true);
        }

        BarcodeQuadrilateral destination = GeometryMath.CreateQuadrilateral(
            GeometryMath.CreatePoint(0.0, 0.0),
            GeometryMath.CreatePoint(symbol.Width, 0.0),
            GeometryMath.CreatePoint(symbol.Width, symbol.Height),
            GeometryMath.CreatePoint(0.0, symbol.Height));
        PerspectiveTransform imageToSymbol = PerspectiveTransform.CreateQuadrilateralMapping(
            location,
            destination);
        int row = 0;
        while (row < image.Height)
        {
            int column = 0;
            while (column < image.Width)
            {
                BarcodePoint mapped = imageToSymbol.TransformPoint(
                    GeometryMath.CreatePoint(column, row));
                if (mapped.X >= 0.0
                    && mapped.X < symbol.Width
                    && mapped.Y >= 0.0
                    && mapped.Y < symbol.Height)
                {
                    int moduleColumn = (int)global::System.Math.Floor(mapped.X);
                    int moduleRow = (int)global::System.Math.Floor(mapped.Y);
                    bool value = symbol.GetModule(moduleColumn, moduleRow);
                    if (addBoundaryNoise && IsNearModuleBoundary(mapped))
                    {
                        value = !value;
                    }

                    image.SetModule(column, row, inverted ? !value : value);
                }

                column++;
            }

            row++;
        }
    }

    static bool IsNearModuleBoundary(BarcodePoint point)
    {
        double horizontalFraction = point.X - global::System.Math.Floor(point.X);
        double verticalFraction = point.Y - global::System.Math.Floor(point.Y);
        return horizontalFraction < 0.10
            || horizontalFraction > 0.90
            || verticalFraction < 0.10
            || verticalFraction > 0.90;
    }

    static BarcodeQuadrilateral CopyLocation(BarcodeQuadrilateral source)
    {
        return GeometryMath.CreateQuadrilateral(
            source.TopLeft!,
            source.TopRight!,
            source.BottomRight!,
            source.BottomLeft!);
    }

    static string FormatLocation(BarcodeQuadrilateral location)
    {
        return $"{location.TopLeft!.X},{location.TopLeft.Y}; {location.TopRight!.X},{location.TopRight.Y}; {location.BottomRight!.X},{location.BottomRight.Y}; {location.BottomLeft!.X},{location.BottomLeft.Y}";
    }

    static ModuleMatrix CopyMatrix(ModuleMatrix source)
    {
        ModuleMatrix copy = new ModuleMatrix(source.Width, source.Height);
        copy.LoadModules(source.CopyModules());
        return copy;
    }

    static byte[] CopyBytes(byte[] source)
    {
        byte[] copy = new byte[source.Length];
        global::System.Array.Copy(source, copy, source.Length);
        return copy;
    }
}
