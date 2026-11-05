using qrcode;
using static qrcode.Tests.QrRandomizedRoundTripTests;

namespace qrcode.Tests;

[TestFixture]
public class QrRandomizedRecoveryTests
{
    static IEnumerable<TestCaseData> DamageCases()
    {
        foreach (QrSymbolKind kind in Enum.GetValues<QrSymbolKind>())
            for (int sample = 0; sample < 12; sample++)
            {
                int seed = 1528000 + (int)kind * 100 + sample;
                yield return new TestCaseData(kind, seed).SetName($"RandomDamage_{kind}_seed{seed}");
            }
    }

    [TestCaseSource(nameof(DamageCases))]
    public void RandomErrorsAndErasuresRecoverWithinEachSelectedBlocksBudget(QrSymbolKind kind, int seed)
    {
        Random random = new Random(seed);
        int version = kind == QrSymbolKind.Model2 ? random.Next(1, 41) : kind == QrSymbolKind.Micro ? random.Next(2, 5)
            : kind == QrSymbolKind.rMQR ? random.Next(32) : random.Next(1, 15);
        int level = Level(random, kind, version);
        QrSegment segment = Segment(random, QrEncodingMode.Numeric, 3);
        QrSymbol symbol = Build([segment], Options(random, kind, version, level), version, level);
        QrMatrix matrix = symbol.Matrix!;
        matrix.Confidence = Enumerable.Repeat((byte)255, matrix.Values.Length).ToArray();
        List<int>[] blocks = CodewordErasureRecoveryTests.BlockPositions(kind, version, level);
        int ecc = CodewordErasureRecoveryTests.EccCount(kind, version, level);
        int[] selected = Enumerable.Range(0, blocks.Length).ToArray();
        random.Shuffle(selected);
        selected = selected.Take(Math.Min(2, blocks.Length)).ToArray();
        int expectedErrors = 0, expectedErasures = 0;
        TestContext.Out.WriteLine($"seed={seed}; family={kind}; version={version}; level={level}; payload={segment.Text}");
        foreach (int block in selected)
        {
            int errors = random.Next(ecc / 2 + 1);
            int erasures = random.Next(ecc - 2 * errors + 1);
            if (errors + erasures == 0) erasures = 1;
            int[] positions = blocks[block].ToArray();
            random.Shuffle(positions);
            TestContext.Out.WriteLine($"block={block}; errors={errors}; erasures={erasures}; positions={string.Join(',', positions.Take(errors + erasures))}");
            for (int index = 0; index < errors + erasures; index++)
            {
                int[] cells = CodewordErasureRecoveryTests.Cells(kind, version, level, positions[index]);
                random.Shuffle(cells);
                int flips = random.Next(1, cells.Length + 1);
                foreach (int cell in cells.Take(flips))
                {
                    matrix.Values[cell] = matrix.Values[cell] == QrModuleValue.Dark ? QrModuleValue.Light : QrModuleValue.Dark;
                    matrix.Confidence[cell] = (byte)(index < errors ? random.Next(128, 256) : random.Next(128));
                }
            }
            expectedErrors += errors;
            expectedErasures += erasures;
        }
        int turns = random.Next(4);
        bool mirror = kind != QrSymbolKind.LegacyModel1 && random.Next(2) == 1;
        bool invert = kind != QrSymbolKind.LegacyModel1 && random.Next(2) == 1;
        QrMatrix transformed = new QrMatrixProcessor().TransformMatrix(matrix, kind, turns, mirror, invert, 0, 0, 0, 0, true, random.Next(5), 1);
        string before = Snapshot(transformed);
        QrDecodeOptions decoding = Decoding();
        IQrDecoder decoder = new QrDecoder();
        QrDecodeResult decoded = decoder.DecodeMatrix(transformed, decoding);
        Assert.That(decoded.RawBytes, Is.EqualTo(Bytes(segment)));
        Assert.That(decoded.Symbol!.Codewords, Is.EqualTo(symbol.Codewords));
        Assert.That(decoded.CorrectedErrorCount, Is.EqualTo(expectedErrors));
        Assert.That(decoded.CorrectedErasureCount, Is.EqualTo(expectedErasures));
        Assert.That(decoded.ErrorCorrectionBlocks.All(b => b.Success && b.Verified), Is.True);
        QrAttemptResult attempt = decoder.TryDecodeMatrix(transformed, decoding);
        Assert.That(attempt.Success, Is.True);
        Assert.That(attempt.RawBytes, Is.EqualTo(decoded.RawBytes));
        Assert.That(attempt.CorrectedErrorCount, Is.EqualTo(expectedErrors));
        Assert.That(attempt.CorrectedErasureCount, Is.EqualTo(expectedErasures));
        Assert.That(Snapshot(transformed), Is.EqualTo(before));

        // Excess known erasures have a deterministic rejection guarantee;
        // arbitrary excess errors could instead approach another valid codeword.
        QrMatrix excessive = Build([segment], Options(new Random(seed), kind, version, level), version, level).Matrix!;
        excessive.Confidence = Enumerable.Repeat((byte)255, excessive.Values.Length).ToArray();
        int[] excessPositions = blocks[selected[0]].ToArray();
        random.Shuffle(excessPositions);
        foreach (int word in excessPositions.Take(ecc + 1))
            foreach (int cell in CodewordErasureRecoveryTests.Cells(kind, version, level, word)) excessive.Confidence[cell] = 0;
        QrAttemptResult failure = decoder.TryDecodeMatrix(excessive, decoding);
        Assert.That(failure.Success, Is.False);
        Assert.That(failure.FailureReason, Is.EqualTo(QrFailureReason.ChecksumFailure));
        Assert.That(failure.Result, Is.Null);
        Assert.That(failure.RawBytes, Is.Empty);
        Assert.That(failure.Segments, Is.Empty);
    }

    [TestCase(1528400)]
    [TestCase(1528401)]
    [TestCase(1528402)]
    [TestCase(1528403)]
    public void MicroM1RandomDamageRemainsDetectionOnly(int seed)
    {
        Random random = new Random(seed);
        QrSegment segment = Segment(random, QrEncodingMode.Numeric, random.Next(1, 6));
        QrSymbol symbol = Build([segment], Options(random, QrSymbolKind.Micro, 1, -1), 1, -1);
        int word = random.Next(symbol.Codewords.Length);
        int[] cells = CodewordErasureRecoveryTests.Cells(QrSymbolKind.Micro, 1, -1, word);
        int cell = cells[random.Next(cells.Length)];
        TestContext.Out.WriteLine($"seed={seed}; word={word}; cell={cell}; payload={segment.Text}");
        symbol.Matrix!.Values[cell] = symbol.Matrix.Values[cell] == QrModuleValue.Dark ? QrModuleValue.Light : QrModuleValue.Dark;
        QrAttemptResult attempt = new QrDecoder().TryDecodeMatrix(symbol.Matrix, Decoding());
        Assert.That(attempt.Success, Is.False);
        Assert.That(attempt.FailureReason, Is.EqualTo(QrFailureReason.ChecksumFailure));
        Assert.That(attempt.Result, Is.Null);
    }

    static IEnumerable<TestCaseData> NoiseCases()
    {
        foreach (QrSymbolKind kind in Enum.GetValues<QrSymbolKind>())
            for (int sample = 0; sample < 8; sample++)
            {
                int seed = 1529000 + (int)kind * 100 + sample;
                yield return new TestCaseData(kind, seed).SetName($"RandomImageNoise_{kind}_seed{seed}");
            }
    }

    [TestCaseSource(nameof(NoiseCases))]
    public void RandomPixelNoiseLayoutsAndSamplingOptionsPreserveScanPayload(QrSymbolKind kind, int seed)
    {
        Random random = new Random(seed);
        int version = kind == QrSymbolKind.Model2 ? random.Next(1, 5) : kind == QrSymbolKind.Micro ? 3
            : kind == QrSymbolKind.rMQR ? random.Next(3) : random.Next(1, 5);
        int level = kind == QrSymbolKind.rMQR ? 0 : 1;
        QrSegment segment = Segment(random, QrEncodingMode.Numeric, 3);
        QrSymbol symbol = Build([segment], Options(random, kind, version, level), version, level);
        bool invert = kind != QrSymbolKind.LegacyModel1 && random.Next(2) == 1;
        bool mirror = kind != QrSymbolKind.LegacyModel1 && random.Next(2) == 1;
        int turns = random.Next(4), scale = random.Next(6, 10), quiet = random.Next(4, 7);
        if (kind == QrSymbolKind.LegacyModel1)
        {
            symbol.Matrix!.Roles = new QrModuleRole[symbol.Matrix.Values.Length];
            for (int word = 0; word < symbol.Codewords.Length; word++)
                foreach (int cell in CodewordErasureRecoveryTests.Cells(kind, version, level, word)) symbol.Matrix.Roles[cell] = QrModuleRole.Data;
        }
        symbol.Matrix = new QrMatrixProcessor().TransformMatrix(symbol.Matrix!, kind, turns, mirror, false, 0, 0, 0, 0, false, 0, 1);
        QrRaster raster = new QrRenderer().RenderSymbol(symbol, new QrRenderOptions
        {
            Scale = scale, QuietZoneModules = quiet, DarkValue = 25, LightValue = 220, Invert = invert
        });
        for (int y = 0; y < raster.Height; y++)
            for (int x = 0; x < raster.Width; x++)
            {
                // A mild illumination ramp and independent bounded sensor noise.
                int cell = y * raster.Stride + x;
                raster.Pixels[cell] = (byte)(raster.Pixels[cell] + random.Next(-15, 16) + 15 * x / raster.Width);
            }
        // Restrict impulses to payload modules and leave sample centers intact:
        // arbitrary damage to the finder regions has no guaranteed scan outcome.
        int[] payloadCells = Enumerable.Range(0, symbol.Matrix.Values.Length)
            .Where(cell => symbol.Matrix.Roles![cell] is QrModuleRole.Data or QrModuleRole.ErrorCorrection).ToArray();
        for (int index = 0; index < 12; index++)
        {
            int module = payloadCells[random.Next(payloadCells.Length)];
            int x = (quiet + module % symbol.Matrix.Width) * scale + 1;
            int y = (quiet + module / symbol.Matrix.Width) * scale + 1;
            raster.Pixels[y * raster.Stride + x] = random.Next(2) == 0 ? (byte)0 : (byte)255;
        }
        QrPixelFormat format = (QrPixelFormat)random.Next(4);
        int channels = format == QrPixelFormat.Gray8 ? 1 : format == QrPixelFormat.Rgb24 ? 3 : 4;
        int stride = raster.Width * channels + random.Next(8);
        QrImage image = new QrImage { Width = raster.Width, Height = raster.Height, Stride = stride, PixelFormat = format,
            Pixels = Enumerable.Repeat((byte)93, (raster.Height - 1) * stride + raster.Width * channels).ToArray() };
        for (int y = 0; y < image.Height; y++)
            for (int x = 0; x < image.Width; x++)
                for (int channel = 0; channel < channels; channel++)
                    image.Pixels[y * stride + x * channels + channel] = channel == 3 ? (byte)255 : raster.Pixels[y * raster.Stride + x];
        QrDetectionOptions sampling = new QrDetectionOptions
        {
            Polarity = invert ? QrPolarity.LightOnDark : QrPolarity.DarkOnLight,
            BinarizationMode = random.Next(2) == 0 ? QrBinarizationMode.GlobalHistogram : QrBinarizationMode.HybridLocal,
            UseMultiPointSampling = random.Next(2) == 1, RefineGrid = random.Next(2) == 1,
            NormalizeContrast = random.Next(2) == 1, MaximumPyramidLevels = 1
        };
        TestContext.Out.WriteLine($"seed={seed}; version={version}; turns={turns}; mirror={mirror}; invert={invert}; scale={scale}; quiet={quiet}; format={format}; stride={stride}; sampling={Snapshot(sampling)}");
        byte[] before = (byte[])image.Pixels.Clone();
        IQrScanner scanner = new QrScanner();
        QrDecodeSet scan = scanner.ScanImage(image, sampling, Decoding(), true);
        Assert.That(scan.Results, Has.Length.EqualTo(1), Snapshot(scan.Diagnostics));
        Assert.That(scan.Results[0].RawBytes, Is.EqualTo(Bytes(segment)));
        Assert.That(scan.Results[0].Symbol!.SymbolKind, Is.EqualTo(kind));
        QrMatrix binary = new QrBinarizer().BinarizeImage(image, sampling)[0];
        // BinarizeImage returns polarity-normalized modules. Restore the
        // image polarity before passing them to the supplied-binary scanner.
        if (invert) binary = new QrMatrixProcessor().TransformMatrix(binary, kind, 0, false, true, 0, 0, 0, 0, false, 0, 1);
        QrDecodeSet binaryScan = scanner.ScanBinaryMatrix(binary, sampling, Decoding(), true);
        Assert.That(binaryScan.Results.Select(r => r.Text), Is.EqualTo(scan.Results.Select(r => r.Text)));
        QrDecodeSet repeated = scanner.ScanImage(image, sampling, Decoding(), true);
        Assert.That(repeated.Results.Select(r => r.Text), Is.EqualTo(scan.Results.Select(r => r.Text)));
        Assert.That(repeated.CandidateCount, Is.EqualTo(scan.CandidateCount));
        Assert.That(repeated.Diagnostics, Is.EqualTo(scan.Diagnostics));
        Assert.That(image.Pixels, Is.EqualTo(before));
    }
}
