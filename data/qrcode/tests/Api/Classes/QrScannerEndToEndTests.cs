using System.Text;
using System.Text.Json;
using qrcode;
using static qrcode.Tests.ImageScanPipelineTests;

namespace qrcode.Tests;

[TestFixture]
public class QrScannerEndToEndTests
{
    static IEnumerable<TestCaseData> Orientations()
    {
        foreach (QrSymbolKind kind in new[] { QrSymbolKind.Model2, QrSymbolKind.Micro, QrSymbolKind.rMQR, QrSymbolKind.LegacyModel1 })
            for (int turns = 0; turns < 4; turns++)
                foreach (bool mirrored in new[] { false, true })
                    foreach (bool inverted in new[] { false, true })
                        if (kind != QrSymbolKind.LegacyModel1 || !mirrored && !inverted)
                            yield return new TestCaseData(kind, turns, mirrored, inverted);
    }

    [TestCaseSource(nameof(Orientations))]
    public void SingleSymbolsAgreeAcrossImageAndBinaryPaths(QrSymbolKind kind, int turns, bool mirrored, bool inverted)
    {
        QrSymbol symbol = kind == QrSymbolKind.LegacyModel1 ? Legacy() : Encode("123", kind);
        QrImage image = Transform(Render(symbol, inverted), turns, mirrored);
        QrDecodeSet[] scans = ScanBoth(image, new QrDetectionOptions(), new QrDecodeOptions { EnableRecovery = false }, true);
        foreach (QrDecodeSet scan in scans)
        {
            Assert.That(scan.Results, Has.Length.EqualTo(1), string.Join("; ", scan.Diagnostics));
            QrDecodeResult result = scan.Results[0];
            Assert.That(result.Text, Is.EqualTo("123"));
            Assert.That(result.Symbol!.SymbolKind, Is.EqualTo(kind));
            Assert.That(result.RawBytes, Is.EqualTo("123"u8.ToArray()));
            Assert.That(result.Detection!.Polarity, Is.EqualTo(inverted ? QrPolarity.LightOnDark : QrPolarity.DarkOnLight));
            Assert.That(result.Detection.SampledMatrix, Is.Not.Null);
            Assert.That(scan.CandidateCount, Is.GreaterThanOrEqualTo(1));
            Assert.That(scan.AssembledResults, Is.Empty);
            Assert.That(scan.UnassembledResults, Is.Empty);
        }
        AssertEquivalent(scans[0], scans[1]);
    }

    [Test]
    public void NonSymbolPatternsReturnEmptySets(
        [Values("white", "black", "stripes", "checker")] string pattern, [Values(false, true)] bool recovery,
        [Values(false, true)] bool retainFailures)
    {
        QrImage image = Blank(96, 72);
        for (int y = 0; y < image.Height; y++)
            for (int x = 0; x < image.Width; x++)
                image.Pixels[y * image.Stride + x] = pattern switch
                {
                    "black" => 0,
                    "stripes" => x / 4 % 2 == 0 ? (byte)0 : (byte)255,
                    "checker" => (x / 4 + y / 4) % 2 == 0 ? (byte)0 : (byte)255,
                    _ => 255
                };
        foreach (QrDecodeSet scan in ScanBoth(image, new QrDetectionOptions(), new QrDecodeOptions { EnableRecovery = recovery }, retainFailures))
        {
            Assert.That(scan.Results, Is.Empty);
            Assert.That(scan.AssembledResults, Is.Empty);
            Assert.That(scan.UnassembledResults, Is.Empty);
            Assert.That(scan.FailedAttempts, Is.Empty);
            Assert.That(scan.CandidateCount, Is.Zero);
            Assert.That(scan.Diagnostics, Is.Not.Empty);
        }
    }

    [Test]
    public void MixedProfilesKeepSpatialOrderAndEqualPayloadsAtDistinctLocations([Values(false, true)] bool recovery)
    {
        QrImage image = Grid([
            Render(Encode("REPEAT")), Transform(Render(Encode("123", QrSymbolKind.Micro), true), 1, true),
            Transform(Render(Encode("456", QrSymbolKind.rMQR)), 1, false),
            Render(Legacy()), Transform(Render(Encode("REPEAT"), true), 2, false), Render(Encode("LAST"))
        ], 3);
        QrDecodeSet[] scans = ScanBoth(image, new QrDetectionOptions(), new QrDecodeOptions { EnableRecovery = recovery }, true);
        foreach (QrDecodeSet scan in scans)
        {
            Assert.That(scan.Results.Select(r => r.Text), Is.EqualTo(new[] { "REPEAT", "123", "456", "123", "REPEAT", "LAST" }));
            Assert.That(scan.Results.Select(r => r.Symbol!.SymbolKind), Is.EqualTo(new[]
                { QrSymbolKind.Model2, QrSymbolKind.Micro, QrSymbolKind.rMQR, QrSymbolKind.LegacyModel1, QrSymbolKind.Model2, QrSymbolKind.Model2 }));
            Assert.That(scan.Results.Select(r => r.Detection!.CandidateIndex), Is.Unique);
            Assert.That(scan.AssembledResults, Is.Empty);
            Assert.That(scan.UnassembledResults, Is.Empty);
            Assert.That(scan.FailedAttempts, Is.Empty);
            Assert.That(scan.CandidateCount, Is.GreaterThanOrEqualTo(6));
        }
        AssertEquivalent(scans[0], scans[1]);
    }

    [Test]
    public void FailedCandidatesDoNotSpendTheNextCandidatesBudget(
        [Values(false, true)] bool single, [Values(false, true)] bool retainFailures)
    {
        QrSymbol corrupt = Encode("BAD");
        QrMatrix matrix = corrupt.Matrix!;
        for (int i = 0; i < matrix.Values.Length; i++)
            if (matrix.Roles![i] is QrModuleRole.Data or QrModuleRole.ErrorCorrection) matrix.Values[i] = QrModuleValue.Light;
        QrSymbol oversized = new QrEncoder().EncodeText("TOO LARGE", new QrEncodeOptions
            { ForceSymbolKind = true, SymbolKind = QrSymbolKind.Model2, MinimumVersion = 7, MaximumVersion = 7, QuietZoneModules = 0 });
        QrImage image = Grid([Render(corrupt), Render(oversized), Render(Encode("GOOD")), Render(Encode("LAST"))], 4);
        QrDecodeSet[] scans = ScanBoth(image, new QrDetectionOptions { DetectMultiple = !single },
            new QrDecodeOptions { EnableRecovery = false, MaximumRecoveryAttempts = 1, MaximumMatrixDimension = 25 }, retainFailures);
        foreach (QrDecodeSet scan in scans)
        {
            Assert.That(scan.Results.Select(r => r.Text), Is.EqualTo(single ? new[] { "GOOD" } : new[] { "GOOD", "LAST" }));
            Assert.That(scan.CandidateCount, Is.EqualTo(single ? 3 : 4));
            Assert.That(scan.FailedAttempts, Has.Length.EqualTo(retainFailures ? 2 : 0));
            Assert.That(scan.Diagnostics.Count(d => d.StartsWith("Candidate ")), Is.EqualTo(retainFailures ? 2 : 0));
            if (retainFailures)
            {
                Assert.That(scan.FailedAttempts.Select(f => f.AttemptCount), Is.EqualTo(new[] { 1, 0 }));
                foreach (QrAttemptResult failure in scan.FailedAttempts)
                {
                    Assert.That(failure.Success, Is.False);
                    Assert.That(failure.FailureReason, Is.EqualTo(QrFailureReason.ResourceLimit));
                    Assert.That(failure.CandidateCount, Is.EqualTo(1));
                    Assert.That(failure.Result, Is.Null);
                    Assert.That(failure.Text, Is.Null);
                    Assert.That(failure.RawBytes, Is.Empty);
                    Assert.That(failure.Segments, Is.Empty);
                }
            }
        }
        AssertEquivalent(scans[0], scans[1]);
    }

    [Test]
    public void StructuredAppendAssemblyPreservesIndividualSpatialResults(
        [Values("complete", "duplicate", "missing", "conflict", "mixed", "utf8")] string scenario,
        [Values(false, true)] bool recovery)
    {
        QrSymbol[] sequence = Parts([[65], [66], [67]]);
        List<QrSymbol> symbols = [sequence[2], sequence[0], sequence[1]];
        string[] assembled = ["ABC"];
        string[] unassembled = [];
        if (scenario == "duplicate") symbols.Insert(1, sequence[0]);
        if (scenario == "missing") { symbols.Remove(sequence[1]); assembled = []; unassembled = ["C", "A"]; }
        if (scenario == "conflict")
        {
            symbols.Insert(1, CreatePart(0, 3, 65 ^ 66 ^ 67, [90], -1));
            assembled = []; unassembled = ["C", "Z", "A", "B"];
        }
        if (scenario == "mixed")
        {
            QrSymbol[] other = Parts([[88], [89]]);
            QrSymbol[] incomplete = Parts([[77], [78], [79]]);
            symbols.Insert(1, other[1]); symbols.Add(incomplete[1]); symbols.Add(other[0]);
            assembled = ["ABC", "XY"]; unassembled = ["N"];
        }
        if (scenario == "utf8")
        {
            QrSymbol[] utf8 = Parts([[0xE2, 0x82], [0xAC]], 26);
            symbols = [utf8[1], utf8[0]]; assembled = ["€"];
        }
        symbols.Add(Encode("ORDINARY"));
        string?[] individualText = symbols.Select(s => new QrDecoder().DecodeMatrix(s.Matrix!, new QrDecodeOptions()).Text).ToArray();
        QrImage image = Grid(symbols.Select(s => Render(s)).ToArray(), 3);
        QrDecodeSet[] scans = ScanBoth(image, new QrDetectionOptions(), new QrDecodeOptions { EnableRecovery = recovery }, false);
        foreach (QrDecodeSet scan in scans)
        {
            Assert.That(scan.Results.Select(r => r.Text), Is.EqualTo(individualText));
            Assert.That(scan.AssembledResults.Select(r => r.Text), Is.EqualTo(assembled));
            Assert.That(scan.UnassembledResults.Select(r => r.Text), Is.EqualTo(unassembled));
            Assert.That(scan.FailedAttempts, Is.Empty);
            foreach (QrDecodeResult part in scan.UnassembledResults) Assert.That(scan.Results, Has.Some.SameAs(part));
            foreach (QrDecodeResult result in scan.AssembledResults)
            {
                Assert.That(result.StructuredAppend!.IsComplete, Is.True);
                Assert.That(result.Detection, Is.Null);
                Assert.That(result.Symbol, Is.Null);
                Assert.That(result.RawBytes, Is.EqualTo(Encoding.UTF8.GetBytes(result.Text!)));
            }
            if (scenario == "duplicate") Assert.That(scan.AssembledResults[0].Warnings, Has.Member("Ignored 1 equivalent duplicate observations."));
            if (scenario == "utf8") Assert.That(scan.AssembledResults[0].Warnings, Has.Member("Recovered text across compatible byte-segment boundaries."));
            if (scenario == "missing" || scenario == "mixed") Assert.That(scan.Diagnostics, Has.Some.Contains("IncompleteSequence"));
            if (scenario == "conflict") Assert.That(scan.Diagnostics, Has.Some.Contains("ConflictingSequence"));
        }
        AssertEquivalent(scans[0], scans[1]);
    }

    [Test]
    public void PaddedRawPixelFormatsMatchBinaryPixelsAndIgnoreBinaryModuleMetadata(
        [Values(QrPixelFormat.Gray8, QrPixelFormat.Rgb24, QrPixelFormat.Rgba32, QrPixelFormat.Bgra32)] QrPixelFormat format)
    {
        QrImage gray = Grid([Render(Encode("LEFT")), Render(Encode("RIGHT"), true)], 2);
        int channels = format == QrPixelFormat.Gray8 ? 1 : format == QrPixelFormat.Rgb24 ? 3 : 4;
        int stride = gray.Width * channels + 11;
        QrImage image = new() { Width = gray.Width, Height = gray.Height, Stride = stride, PixelFormat = format,
            Pixels = Enumerable.Repeat((byte)71, stride * (gray.Height - 1) + gray.Width * channels).ToArray() };
        for (int y = 0; y < gray.Height; y++)
            for (int x = 0; x < gray.Width; x++)
                for (int c = 0; c < channels; c++) image.Pixels[y * stride + x * channels + c] = c == 3 ? (byte)255 : gray.Pixels[y * gray.Stride + x];
        QrMatrix binary = Binary(gray);
        binary.Confidence = [0];
        binary.Roles = [QrModuleRole.Unknown];
        string before = Snapshot(image);
        string matrixBefore = Snapshot(binary);
        IQrScanner scanner = new QrScanner();
        QrDecodeSet raw = scanner.ScanImage(image, new QrDetectionOptions(), new QrDecodeOptions { EnableRecovery = false }, true);
        QrDecodeSet supplied = scanner.ScanBinaryMatrix(binary, new QrDetectionOptions { NormalizeContrast = true }, new QrDecodeOptions { EnableRecovery = false }, true);
        Assert.That(raw.Results.Select(r => r.Text), Is.EqualTo(new[] { "LEFT", "RIGHT" }));
        AssertEquivalent(raw, supplied);
        Assert.That(Snapshot(image), Is.EqualTo(before));
        Assert.That(Snapshot(binary), Is.EqualTo(matrixBefore));
    }

    static QrDecodeSet[] ScanBoth(QrImage image, QrDetectionOptions detection, QrDecodeOptions decoding, bool retainFailures)
    {
        QrMatrix binary = Binary(image);
        string imageBefore = Snapshot(image);
        string binaryBefore = Snapshot(binary);
        string detectionBefore = Snapshot(detection);
        string decodingBefore = Snapshot(decoding);
        IQrScanner scanner = new QrScanner();
        QrDecodeSet raw = scanner.ScanImage(image, detection, decoding, retainFailures);
        QrDecodeSet supplied = scanner.ScanBinaryMatrix(binary, detection, decoding, retainFailures);
        Assert.That(Snapshot(image), Is.EqualTo(imageBefore));
        Assert.That(Snapshot(binary), Is.EqualTo(binaryBefore));
        Assert.That(Snapshot(detection), Is.EqualTo(detectionBefore));
        Assert.That(Snapshot(decoding), Is.EqualTo(decodingBefore));
        return [raw, supplied];
    }

    static void AssertEquivalent(QrDecodeSet raw, QrDecodeSet binary)
    {
        // Threshold-pass diagnostics and total examined candidates differ by path.
        // Compare the retained payload, geometry, repair, and assembly semantics.
        Assert.That(Semantics(raw.Results), Is.EqualTo(Semantics(binary.Results)));
        Assert.That(Semantics(raw.AssembledResults), Is.EqualTo(Semantics(binary.AssembledResults)));
        Assert.That(Semantics(raw.UnassembledResults), Is.EqualTo(Semantics(binary.UnassembledResults)));
        Assert.That(Snapshot(raw.FailedAttempts), Is.EqualTo(Snapshot(binary.FailedAttempts)));
    }

    static string Semantics(QrDecodeResult[] results) => Snapshot(results.Select(r => new
    {
        r.Text, r.RawBytes, r.Segments, r.StructuredAppend, r.EciAssignments, r.SymbologyIdentifier,
        r.CorrectedErrorCount, r.CorrectedErasureCount, r.IsConformant, r.UsesFnc1First, r.Fnc1SecondApplicationIndicator,
        Kind = r.Symbol?.SymbolKind.ToString(), Version = r.Symbol?.Version.ToString(), r.Symbol?.Codewords,
        r.Detection?.Corners, Polarity = r.Detection?.Polarity.ToString()
    }).ToArray());

    static QrSymbol Legacy()
    {
        QrMatrix matrix = CodewordErasureRecoveryTests.Build(CodewordErasureRecoveryTests.Words(QrSymbolKind.LegacyModel1, 1, 0), QrSymbolKind.LegacyModel1, 1, 0);
        return new QrSymbol { SymbolKind = QrSymbolKind.LegacyModel1, Version = 1, Width = matrix.Width, Height = matrix.Height, Matrix = matrix };
    }

    static QrSymbol[] Parts(byte[][] payloads, int assignment = -1)
    {
        int parity = payloads.SelectMany(p => p).Aggregate(0, (p, b) => p ^ b);
        return payloads.Select((p, i) => CreatePart(i, payloads.Length, parity, p, assignment)).ToArray();
    }

    static QrSymbol CreatePart(int index, int count, int parity, byte[] payload, int assignment)
    {
        List<QrSegment> segments = [new QrSegment { Mode = QrEncodingMode.StructuredAppend,
            StructuredAppend = new QrStructuredAppendInfo { Index = index, Total = count, Parity = parity } }];
        if (assignment >= 0) segments.Add(EciEncoder.CreateControlSegment(assignment));
        segments.Add(new QrSegment { Mode = QrEncodingMode.Byte, Data = payload, CharacterCount = payload.Length, EciAssignment = assignment });
        return new QrEncoder().EncodeSegments(segments.ToArray(), new QrEncodeOptions
            { ForceSymbolKind = true, SymbolKind = QrSymbolKind.Model2, QuietZoneModules = 0 });
    }

    static QrImage Grid(QrImage[] tiles, int columns)
    {
        int cellWidth = tiles.Max(t => t.Width) + 40;
        // Distinct top edges make spatial order independent of subpixel ties.
        int cellHeight = tiles.Max(t => t.Height) + 40 + columns * 8;
        QrImage image = Blank(cellWidth * columns, cellHeight * ((tiles.Length + columns - 1) / columns));
        for (int i = 0; i < tiles.Length; i++)
            for (int y = 0; y < tiles[i].Height; y++)
                Array.Copy(tiles[i].Pixels, y * tiles[i].Stride, image.Pixels,
                    (i / columns * cellHeight + i % columns * 8 + y) * image.Stride + i % columns * cellWidth, tiles[i].Width);
        return image;
    }

    static QrImage Transform(QrImage image, int turns, bool mirror)
    {
        if (mirror) for (int y = 0; y < image.Height; y++) Array.Reverse(image.Pixels, y * image.Stride, image.Width);
        for (int turn = 0; turn < turns; turn++)
        {
            byte[] pixels = new byte[image.Pixels.Length];
            for (int y = 0; y < image.Height; y++)
                for (int x = 0; x < image.Width; x++) pixels[x * image.Height + image.Height - 1 - y] = image.Pixels[y * image.Stride + x];
            (image.Width, image.Height) = (image.Height, image.Width);
            image.Stride = image.Width;
            image.Pixels = pixels;
        }
        return image;
    }

    static QrImage Blank(int width, int height) => new() { Width = width, Height = height, Stride = width, Pixels = Enumerable.Repeat((byte)255, width * height).ToArray() };
    static QrMatrix Binary(QrImage image) => new() { Width = image.Width, Height = image.Height, Values = image.Pixels.Select(p => p == 0 ? QrModuleValue.Dark : QrModuleValue.Light).ToArray() };
    static string Snapshot(object value) => JsonSerializer.Serialize(value, new JsonSerializerOptions { IncludeFields = true });
}
