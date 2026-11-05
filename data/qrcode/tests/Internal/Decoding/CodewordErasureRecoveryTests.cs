using qrcode;

namespace qrcode.Tests;

[TestFixture]
public class CodewordErasureRecoveryTests
{
    static IEnumerable<TestCaseData> Configurations()
    {
        for (int version = 1; version <= 40; version++)
            for (int level = 0; level < 4; level++) yield return new TestCaseData(QrSymbolKind.Model2, version, level);
        for (int version = 2; version <= 4; version++)
            for (int level = 0; level <= (version == 4 ? 2 : 1); level++) yield return new TestCaseData(QrSymbolKind.Micro, version, level);
        for (int version = 0; version < 32; version++)
            for (int level = 0; level < 2; level++) yield return new TestCaseData(QrSymbolKind.rMQR, version, level);
        for (int version = 1; version <= 14; version++)
            for (int level = 0; level < 4; level++) yield return new TestCaseData(QrSymbolKind.LegacyModel1, version, level);
    }

    [TestCaseSource(nameof(Configurations))]
    public void MapsDataAndParityErasuresForEveryConfiguration(QrSymbolKind kind, int version, int level)
    {
        byte[] original = Words(kind, version, level);
        int lastProtected = original.Length - 1 - (kind == QrSymbolKind.LegacyModel1 ? LegacyModel1Table.GetRemainderCodewordCount(version, level) : 0);
        QrMatrix matrix = Build(original, kind, version, level);
        Mark(matrix, kind, version, level, 0, 51, false);
        Mark(matrix, kind, version, level, lastProtected, 0, true);
        QrModuleValue[] before = (QrModuleValue[])matrix.Values.Clone();
        byte[] confidence = (byte[])matrix.Confidence!.Clone();
        // Caller role claims cannot change the codeword mapping.
        matrix.Roles = Enumerable.Repeat(QrModuleRole.Finder, matrix.Values.Length).ToArray();
        MatrixCodewordResult result = MatrixCodewordReader.Read(matrix, kind, new QrDecodeOptions { EnableRecovery = false });
        Assert.That(result.Success, Is.True);
        Assert.That(result.ErasurePositions, Is.EqualTo(new[] { 0, lastProtected }));
        Assert.That(result.LowConfidencePayloadModuleCount, Is.EqualTo(kind == QrSymbolKind.LegacyModel1 ? 4 : 8));
        Assert.That(result.UnknownPayloadModuleCount, Is.EqualTo(8));
        Assert.That(result.Correction!.Codewords, Is.EqualTo(original));
        // Unknown bits can coincidentally retain a valid parity byte. A clean
        // block requires no repair even though its erasure position is recorded.
        Assert.That(result.Correction.CorrectedErasureCount, Is.InRange(1, 2));
        Assert.That(result.Correction.CorrectedErrorCount, Is.Zero);
        Assert.That(matrix.Values, Is.EqualTo(before));
        Assert.That(matrix.Confidence, Is.EqualTo(confidence));
    }

    [TestCase(QrSymbolKind.Model2, 5, 2)]
    [TestCase(QrSymbolKind.Model2, 40, 3)]
    [TestCase(QrSymbolKind.Micro, 3, 1)]
    [TestCase(QrSymbolKind.Micro, 4, 2)]
    [TestCase(QrSymbolKind.rMQR, 31, 1)]
    [TestCase(QrSymbolKind.LegacyModel1, 14, 3)]
    public void RepairsMixedErrorsAndErasuresAtEachBlocksCapacity(QrSymbolKind kind, int version, int level)
    {
        byte[] original = Words(kind, version, level);
        QrMatrix matrix = Build(original, kind, version, level);
        List<int>[] blocks = BlockPositions(kind, version, level);
        int ecc = EccCount(kind, version, level);
        List<int> erased = new List<int>();
        foreach (List<int> block in blocks)
        {
            foreach (int position in block.Take(ecc - 2))
            {
                Mark(matrix, kind, version, level, position, 51, false);
                erased.Add(position);
            }
            foreach (int cell in Cells(kind, version, level, block[^1])) Flip(matrix, cell);
        }
        MatrixCodewordResult result = MatrixCodewordReader.Read(matrix, kind, new QrDecodeOptions());
        Assert.That(result.Success, Is.True);
        Assert.That(result.ErasurePositions, Is.EqualTo(erased.Order()));
        Assert.That(result.Correction!.Codewords, Is.EqualTo(original));
        Assert.That(result.Correction.CorrectedErasureCount, Is.EqualTo((ecc - 2) * blocks.Length));
        Assert.That(result.Correction.CorrectedErrorCount, Is.EqualTo(blocks.Length));
        Assert.That(result.Correction.BlockResults.All(b => b.Verified), Is.True);
    }

    [TestCase(QrSymbolKind.Model2, 5, 2)]
    [TestCase(QrSymbolKind.Micro, 3, 1)]
    [TestCase(QrSymbolKind.rMQR, 31, 1)]
    [TestCase(QrSymbolKind.LegacyModel1, 14, 3)]
    public void TooManyErasuresInOneBlockFailEvenIfOtherBlocksAreClean(QrSymbolKind kind, int version, int level)
    {
        QrMatrix matrix = Build(Words(kind, version, level), kind, version, level);
        foreach (int position in BlockPositions(kind, version, level)[0].Take(EccCount(kind, version, level) + 1))
            Mark(matrix, kind, version, level, position, 0, false);
        MatrixCodewordResult result = MatrixCodewordReader.Read(matrix, kind, new QrDecodeOptions());
        Assert.That(result.Success, Is.False);
        Assert.That(result.FailureReason, Is.EqualTo(QrFailureReason.ChecksumFailure));
        Assert.That(result.DataCodewords, Is.Empty);
        Assert.That(result.Correction!.Success, Is.False);
    }

    [TestCase(0, true)]
    [TestCase(1, true)]
    [TestCase(51, true)]
    [TestCase(127, true)]
    [TestCase(128, false)]
    [TestCase(153, false)]
    [TestCase(255, false)]
    public void UsesConfidenceBoundaryAndDeduplicatesMultipleUncertainBits(int confidence, bool erased)
    {
        QrMatrix matrix = Build(Words(QrSymbolKind.Model2, 1, 0), QrSymbolKind.Model2, 1, 0);
        Mark(matrix, QrSymbolKind.Model2, 1, 0, 0, (byte)confidence, false);
        MatrixCodewordResult result = MatrixCodewordReader.Read(matrix, QrSymbolKind.Model2, new QrDecodeOptions());
        Assert.That(result.Success, Is.True);
        Assert.That(result.ErasurePositions, Is.EqualTo(erased ? new[] { 0 } : Array.Empty<int>()));
        Assert.That(result.Correction!.CorrectedErasureCount, Is.EqualTo(erased ? 1 : 0));
        Assert.That(result.Correction.CorrectedErrorCount, Is.EqualTo(erased ? 0 : 1));
    }

    [Test]
    public void MicroM3PartialDataByteAndFirstParityByteStaySeparate()
    {
        int version = 3, level = 1;
        byte[] original = Words(QrSymbolKind.Micro, version, level);
        int dataCount = MicroVersionTable.GetDataSymbolCharacterCount(version, level);
        QrMatrix matrix = Build(original, QrSymbolKind.Micro, version, level);
        Mark(matrix, QrSymbolKind.Micro, version, level, dataCount - 1, 51, false);
        Mark(matrix, QrSymbolKind.Micro, version, level, dataCount, 51, false);
        MatrixCodewordResult result = MatrixCodewordReader.Read(matrix, QrSymbolKind.Micro, new QrDecodeOptions());
        Assert.That(Cells(QrSymbolKind.Micro, version, level, dataCount - 1), Has.Length.EqualTo(4));
        Assert.That(result.ErasurePositions, Is.EqualTo(new[] { dataCount - 1, dataCount }));
        Assert.That(result.Correction!.Codewords, Is.EqualTo(original));
        Assert.That(result.DataCodewords[^1] & 15, Is.Zero);
    }

    [Test]
    public void DetectionOnlyMicroM1CannotAcceptErasureRepair()
    {
        QrMatrix matrix = Build(Words(QrSymbolKind.Micro, 1, -1), QrSymbolKind.Micro, 1, -1);
        Mark(matrix, QrSymbolKind.Micro, 1, -1, 0, 51, false);
        MatrixCodewordResult result = MatrixCodewordReader.Read(matrix, QrSymbolKind.Micro, new QrDecodeOptions());
        Assert.That(result.Success, Is.False);
        Assert.That(result.FailureReason, Is.EqualTo(QrFailureReason.ChecksumFailure));
        Assert.That(result.DataCodewords, Is.Empty);
    }

    [Test]
    public void QuietZonesAndOrientationKeepErasureCoordinatesAndResultCounts()
    {
        QrMatrix matrix = Build(Words(QrSymbolKind.Model2, 2, 0), QrSymbolKind.Model2, 2, 0);
        Mark(matrix, QrSymbolKind.Model2, 2, 0, 0, 51, false);
        matrix = MatrixTransformer.AddQuietZone(matrix, QrSymbolKind.Model2, 4);
        matrix = MatrixTransformer.InvertPolarity(MatrixTransformer.Mirror(MatrixTransformer.Rotate(matrix, 1)));
        QrDecodeResult decoded = MatrixDecodeRecovery.Decode(matrix, new QrDecodeOptions());
        Assert.That(decoded.Text, Is.EqualTo("123"));
        Assert.That(decoded.CorrectedErasureCount, Is.EqualTo(1));
        Assert.That(decoded.CorrectedErrorCount, Is.Zero);
    }

    [Test]
    public void CorrectLowConfidenceValuesAreVerifiedWithoutInventingRepairs()
    {
        QrMatrix matrix = Build(Words(QrSymbolKind.Model2, 2, 0), QrSymbolKind.Model2, 2, 0);
        matrix.Confidence = Enumerable.Repeat((byte)255, matrix.Values.Length).ToArray();
        foreach (int cell in Cells(QrSymbolKind.Model2, 2, 0, 0)) matrix.Confidence[cell] = 51;
        MatrixCodewordResult result = MatrixCodewordReader.Read(matrix, QrSymbolKind.Model2, new QrDecodeOptions());
        Assert.That(result.Success, Is.True);
        Assert.That(result.ErasurePositions, Is.EqualTo(new[] { 0 }));
        Assert.That(result.Correction!.CorrectedErasureCount, Is.Zero);
        Assert.That(result.Correction.CorrectedErrorCount, Is.Zero);
    }

    [TestCase(QrSymbolKind.Model2, 2, 0)]
    [TestCase(QrSymbolKind.rMQR, 1, 0)]
    [TestCase(QrSymbolKind.LegacyModel1, 7, 3)]
    public void UncertainRemaindersCannotBeRepairedAsCodewords(QrSymbolKind kind, int version, int level)
    {
        QrMatrix matrix = Build(Words(kind, version, level), kind, version, level);
        int cell;
        if (kind == QrSymbolKind.LegacyModel1)
        {
            Assert.That(LegacyModel1Table.GetRemainderCodewordCount(version, level), Is.GreaterThan(0));
            cell = LegacyModel1Placement.CreateOffsets(version)[^1];
        }
        else
        {
            cell = Array.IndexOf(ModuleClassifier.CreateEncodingRoleMap(kind, version, level), QrModuleRole.Remainder);
            Assert.That(cell, Is.GreaterThanOrEqualTo(0));
        }
        matrix.Confidence = Enumerable.Repeat((byte)255, matrix.Values.Length).ToArray();
        matrix.Confidence[cell] = 51;
        MatrixCodewordResult result = MatrixCodewordReader.Read(matrix, kind, new QrDecodeOptions());
        Assert.That(result.Success, Is.False);
        Assert.That(result.FailureReason, Is.EqualTo(QrFailureReason.InvalidData));
        Assert.That(result.ErasurePositions, Is.Empty);
        Assert.That(result.InvalidRemainderBitCount, Is.EqualTo(1));
    }

    [Test]
    public void ImageSamplingVoteMarginsRecoverMoreThanTheHardDecisionErrorLimit()
    {
        QrSymbol symbol = new QrEncoder().EncodeText("123", new QrEncodeOptions
        {
            ForceSymbolKind = true, SymbolKind = QrSymbolKind.Model2, MinimumVersion = 1, MaximumVersion = 1,
            ForceErrorCorrectionLevel = true, ErrorCorrectionLevel = QrErrorCorrectionLevel.Low,
            BoostErrorCorrectionLevel = false, QuietZoneModules = 0, MaskPattern = QrMaskPattern.Five
        });
        QrRaster raster = new QrRenderer().RenderSymbol(symbol, new QrRenderOptions { Scale = 8, QuietZoneModules = 4 });
        for (int word = 0; word < 5; word++)
            foreach (int cell in Cells(QrSymbolKind.Model2, 1, 0, word))
            {
                int x = (cell % 21 + 4) * 8 + 4;
                int y = (cell / 21 + 4) * 8 + 4;
                // Three incorrect votes, two correct: the observed value is
                // wrong with confidence 51, so its codeword needs an erasure.
                foreach (int dx in new[] { -2, 0, 2 })
                    raster.Pixels[y * raster.Stride + x + dx] ^= 255;
            }
        QrImage image = new QrImage { Width = raster.Width, Height = raster.Height, Stride = raster.Stride, Pixels = raster.Pixels };
        QrDetection detection = new QrDetection
        {
            HasSymbolKindEstimate = true, EstimatedSymbolKind = QrSymbolKind.Model2, EstimatedVersion = 1,
            EstimatedWidth = 21, EstimatedHeight = 21, ModuleSize = 8, Polarity = QrPolarity.DarkOnLight,
            Corners = new QrQuadrilateral
            {
                TopLeft = new QrPoint { X = 32, Y = 32 }, TopRight = new QrPoint { X = 200, Y = 32 },
                BottomRight = new QrPoint { X = 200, Y = 200 }, BottomLeft = new QrPoint { X = 32, Y = 200 }
            }
        };
        QrDetectionOptions sampling = new QrDetectionOptions { BinarizationMode = QrBinarizationMode.SuppliedBinary };
        QrDecodeOptions decoding = new QrDecodeOptions { EnableRecovery = false };
        QrDecodeResult result = ImageDecodeRecovery.Decode(image, detection, null, sampling, decoding);
        Assert.That(result.Text, Is.EqualTo("123"));
        Assert.That(result.CorrectedErasureCount, Is.EqualTo(5));
        Assert.That(result.CorrectedErrorCount, Is.Zero);
        Assert.That(result.Symbol!.Codewords, Is.EqualTo(symbol.Codewords));
        QrMatrix hardDecisions = result.Detection!.SampledMatrix!;
        hardDecisions.Confidence = null;
        Assert.That(MatrixCodewordReader.Read(hardDecisions, QrSymbolKind.Model2, decoding).Success, Is.False);
    }

    internal static byte[] Words(QrSymbolKind kind, int version, int level)
    {
        byte[] data = CodewordFinalizer.CreateDataCodewords([new QrSegment { Mode = QrEncodingMode.Numeric, Text = "123", CharacterCount = 3 }], kind, version, level, false);
        return BlockInterleaver.CreateCodewords(data, kind, version, level);
    }

    internal static QrMatrix Build(byte[] words, QrSymbolKind kind, int version, int level)
    {
        if (kind == QrSymbolKind.LegacyModel1) return LegacyModel1DecoderTests.Build(words, version, level, 5, false);
        BitMatrixBuffer bits = kind == QrSymbolKind.Model2 ? Model2MatrixBuilder.CreateSelectedModel2Matrix(words, version, level, QrMaskPattern.Five)
            : kind == QrSymbolKind.Micro ? MicroMatrixBuilder.CreateSelectedMicroMatrix(words, version, level, QrMaskPattern.Three)
            : RectangularMicroMatrixBuilder.CreateSelectedRectangularMicroMatrix(words, version, level, QrMaskPattern.Four);
        return new QrMatrix { Width = bits.GetWidth(), Height = bits.GetHeight(), Values = Enumerable.Range(0, bits.GetWidth() * bits.GetHeight())
            .Select(i => bits.GetModule(i / bits.GetWidth(), i % bits.GetWidth()) ? QrModuleValue.Dark : QrModuleValue.Light).ToArray() };
    }

    internal static int[] Cells(QrSymbolKind kind, int version, int level, int word)
    {
        if (kind == QrSymbolKind.LegacyModel1) return LegacyModel1Placement.CreateOffsets(version).Skip(word * 8).Take(8).Where(i => i >= 0).ToArray();
        int width = kind == QrSymbolKind.Model2 ? 17 + 4 * version : kind == QrSymbolKind.Micro ? 9 + 2 * version : RectangularMicroVersionTable.GetWidth(version);
        int height = kind == QrSymbolKind.rMQR ? RectangularMicroVersionTable.GetHeight(version) : width;
        int wordCount = kind == QrSymbolKind.Model2 ? Model2VersionTable.GetTotalCodewordCount(version)
            : kind == QrSymbolKind.Micro ? MicroVersionTable.GetDataSymbolCharacterCount(version, level) + MicroVersionTable.GetEccCodewordCount(version, level)
            : RectangularMicroVersionTable.GetTotalCodewordCount(version);
        byte[] marker = new byte[wordCount];
        marker[word] = kind == QrSymbolKind.Micro && version % 2 == 1 && word == MicroVersionTable.GetDataSymbolCharacterCount(version, level) - 1 ? (byte)0xf0 : (byte)255;
        BitMatrixBuffer bits = new BitMatrixBuffer(width, height);
        if (kind == QrSymbolKind.Model2) DataModuleWalker.PlaceModel2Codewords(marker, version, bits);
        else if (kind == QrSymbolKind.Micro) DataModuleWalker.PlaceMicroCodewords(marker, version, level, bits);
        else DataModuleWalker.PlaceRectangularMicroCodewords(marker, version, bits);
        return Enumerable.Range(0, width * height).Where(i => bits.GetModule(i / width, i % width)).ToArray();
    }

    static void Mark(QrMatrix matrix, QrSymbolKind kind, int version, int level, int word, byte confidence, bool unknown)
    {
        matrix.Confidence ??= Enumerable.Repeat((byte)255, matrix.Values.Length).ToArray();
        foreach (int cell in Cells(kind, version, level, word))
        {
            matrix.Confidence[cell] = confidence;
            if (unknown) matrix.Values[cell] = QrModuleValue.Unknown;
            else Flip(matrix, cell);
        }
    }

    static void Flip(QrMatrix matrix, int cell) => matrix.Values[cell] = matrix.Values[cell] == QrModuleValue.Dark ? QrModuleValue.Light : QrModuleValue.Dark;

    internal static int EccCount(QrSymbolKind kind, int version, int level) => kind == QrSymbolKind.Model2 ? EccBlockTable.GetEccCodewordCountPerBlock(version, level)
        : kind == QrSymbolKind.Micro ? MicroVersionTable.GetEccCodewordCount(version, level)
        : kind == QrSymbolKind.rMQR ? RectangularMicroVersionTable.GetEccCodewordCountPerBlock(version, level)
        : LegacyModel1Table.GetErrorCorrectionCodewordCountPerBlock(version, level);

    internal static List<int>[] BlockPositions(QrSymbolKind kind, int version, int level)
    {
        int blocks = kind == QrSymbolKind.Model2 ? EccBlockTable.GetBlockCount(version, level)
            : kind == QrSymbolKind.rMQR ? RectangularMicroVersionTable.GetBlockCount(version, level)
            : kind == QrSymbolKind.LegacyModel1 ? LegacyModel1Table.GetErrorCorrectionBlockCount(version, level) : 1;
        List<int>[] result = Enumerable.Range(0, blocks).Select(_ => new List<int>()).ToArray();
        if (kind == QrSymbolKind.LegacyModel1)
        {
            int data = LegacyModel1Table.GetDataCodewordCountPerBlock(version, level);
            for (int block = 0; block < blocks; block++)
            {
                result[block].AddRange(Enumerable.Range(block * data, data));
                result[block].AddRange(Enumerable.Range(data * blocks + block * EccCount(kind, version, level), EccCount(kind, version, level)));
            }
            return result;
        }
        int shortBlocks = kind == QrSymbolKind.Model2 ? EccBlockTable.GetShortBlockCount(version, level)
            : kind == QrSymbolKind.rMQR ? RectangularMicroVersionTable.GetShortBlockCount(version, level) : 1;
        int shortData = kind == QrSymbolKind.Model2 ? EccBlockTable.GetShortDataCodewordCount(version, level)
            : kind == QrSymbolKind.rMQR ? RectangularMicroVersionTable.GetShortDataCodewordCount(version, level) : MicroVersionTable.GetDataSymbolCharacterCount(version, level);
        int position = 0;
        for (int row = 0; row < shortData + (shortBlocks < blocks ? 1 : 0); row++)
            for (int block = 0; block < blocks; block++)
                if (row < shortData + (block >= shortBlocks ? 1 : 0)) result[block].Add(position++);
        for (int row = 0; row < EccCount(kind, version, level); row++)
            for (int block = 0; block < blocks; block++) result[block].Add(position++);
        return result;
    }
}
