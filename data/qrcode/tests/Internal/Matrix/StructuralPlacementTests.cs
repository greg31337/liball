using System.IO.Compression;
using System.Text.Json;
using qrcode;

namespace qrcode.Tests;

[TestFixture]
public class StructuralPlacementTests
{
    // Test-owned alignment coordinates (ISO Table E.1, also in the pinned Zint qr.h).
    // Every version is included, covering each grid-size transition and irregular spacing.
    static readonly string[] Model2Alignment =
    [
        "", "6 18", "6 22", "6 26", "6 30", "6 34", "6 22 38", "6 24 42",
        "6 26 46", "6 28 50", "6 30 54", "6 32 58", "6 34 62", "6 26 46 66",
        "6 26 48 70", "6 26 50 74", "6 30 54 78", "6 30 56 82", "6 30 58 86",
        "6 34 62 90", "6 28 50 72 94", "6 26 50 74 98", "6 30 54 78 102",
        "6 28 54 80 106", "6 32 58 84 110", "6 30 58 86 114", "6 34 62 90 118",
        "6 26 50 74 98 122", "6 30 54 78 102 126", "6 26 52 78 104 130",
        "6 30 56 82 108 134", "6 34 60 86 112 138", "6 30 58 86 114 142",
        "6 34 62 90 118 146", "6 30 54 78 102 126 150", "6 24 50 76 102 128 154",
        "6 28 54 80 106 132 158", "6 32 58 84 110 136 162",
        "6 26 54 82 110 138 166", "6 30 58 86 114 142 170"
    ];

    [TestCaseSource(nameof(Cases))]
    public void FunctionPatternsAndReservationsMatchIndependentGeometry(Layout sample)
    {
        QrModuleRole[] expected = ExpectedRoles(sample);
        int ecc = InternalLevel(sample);
        BitMatrixBuffer functions;
        BitMatrixBuffer reserved;
        switch (sample.Kind)
        {
            case QrSymbolKind.Model2:
                functions = FunctionPatternBuilder.CreateModel2FunctionPatterns(sample.Version, ecc, sample.Mask);
                reserved = FunctionPatternBuilder.CreateModel2ReservedMap(sample.Version);
                break;
            case QrSymbolKind.Micro:
                functions = FunctionPatternBuilder.CreateMicroFunctionPatterns(sample.Version, ecc, sample.Mask);
                reserved = FunctionPatternBuilder.CreateMicroReservedMap(sample.Version);
                break;
            default:
                functions = FunctionPatternBuilder.CreateRectangularMicroFunctionPatterns(sample.Version, ecc);
                reserved = FunctionPatternBuilder.CreateRectangularMicroReservedMap(sample.Version);
                break;
        }

        for (int y = 0; y < sample.Height; y++)
            for (int x = 0; x < sample.Width; x++)
            {
                int i = y * sample.Width + x;
                bool isFunction = expected[i] < QrModuleRole.Data;
                Assert.That(reserved.GetModule(y, x), Is.EqualTo(isFunction), $"Reservation at ({x}, {y}), {expected[i]}");
                Assert.That(functions.GetModule(y, x), Is.EqualTo(isFunction && sample.Golden[i]),
                    $"Function value at ({x}, {y}), {expected[i]}");
            }

        // Fill every payload bit, starting with dark remainder cells. Placement must
        // clear precisely the trailing remainder cells and preserve all function cells.
        for (int y = 0; y < sample.Height; y++)
            for (int x = 0; x < sample.Width; x++)
                if (expected[y * sample.Width + x] >= QrModuleRole.Data)
                    functions.SetModule(y, x, true);
        byte[] full = Enumerable.Repeat((byte)255, sample.Codewords.Length).ToArray();
        switch (sample.Kind)
        {
            case QrSymbolKind.Model2:
                DataModuleWalker.PlaceModel2Codewords(full, sample.Version, functions);
                break;
            case QrSymbolKind.Micro:
                DataModuleWalker.PlaceMicroCodewords(full, sample.Version, ecc, functions);
                break;
            default:
                DataModuleWalker.PlaceRectangularMicroCodewords(full, sample.Version, functions);
                break;
        }

        for (int y = 0; y < sample.Height; y++)
            for (int x = 0; x < sample.Width; x++)
            {
                int i = y * sample.Width + x;
                bool dark = expected[i] < QrModuleRole.Data ? sample.Golden[i] : expected[i] != QrModuleRole.Remainder;
                Assert.That(functions.GetModule(y, x), Is.EqualTo(dark), $"Placement at ({x}, {y}), {expected[i]}");
            }
    }

    [TestCaseSource(nameof(Cases))]
    public void BuiltMatricesHaveExactRolesAndQuietZones(Layout sample)
    {
        IQrMatrixProcessor processor = new QrMatrixProcessor();
        QrModuleRole[] coreRoles = ExpectedRoles(sample);
        foreach (int requestedQuietZone in new[] { 0, 1, -1 })
        {
            int quiet = requestedQuietZone == -1 ? sample.Kind == QrSymbolKind.Model2 ? 4 : 2 : requestedQuietZone;
            QrSymbol symbol = new QrSymbol
            {
                SymbolKind = sample.Kind, Version = sample.Version, ErrorCorrectionLevel = sample.Level,
                MaskPattern = (QrMaskPattern)sample.Mask, Codewords = (byte[])sample.Codewords.Clone()
            };
            QrMatrix matrix = processor.BuildMatrix(symbol, requestedQuietZone);
            Assert.Multiple(() =>
            {
                Assert.That(matrix.Width, Is.EqualTo(sample.Width + quiet * 2));
                Assert.That(matrix.Height, Is.EqualTo(sample.Height + quiet * 2));
                Assert.That(new[] { matrix.QuietZoneLeft, matrix.QuietZoneTop, matrix.QuietZoneRight, matrix.QuietZoneBottom },
                    Is.EqualTo(new[] { quiet, quiet, quiet, quiet }));
                Assert.That(matrix.Roles, Is.EqualTo(PadRoles(sample, coreRoles, quiet, quiet, quiet, quiet)));
                Assert.That(matrix.Values, Is.EqualTo(PadValues(sample, quiet, quiet, quiet, quiet)));
            });
        }
    }

    [TestCaseSource(nameof(Cases))]
    public void ClassificationUsesGeometryWithAsymmetricQuietZonesAndPreservesSamples(Layout sample)
    {
        IQrMatrixProcessor processor = new QrMatrixProcessor();
        // Classification must work on unknown/damaged samples, without a pre-existing role map.
        QrMatrix matrix = new QrMatrix
        {
            Width = sample.Width + 3, Height = sample.Height + 7,
            QuietZoneLeft = 1, QuietZoneRight = 2, QuietZoneTop = 3, QuietZoneBottom = 4,
            Values = new QrModuleValue[(sample.Width + 3) * (sample.Height + 7)],
            Roles = new[] { QrModuleRole.Unknown }
        };
        for (int i = 0; i < matrix.Values.Length; i++)
            matrix.Values[i] = (QrModuleValue)(i % 3);
        matrix.Confidence = Enumerable.Range(0, matrix.Values.Length).Select(i => (byte)(i % 256)).ToArray();
        QrModuleValue[] beforeValues = (QrModuleValue[])matrix.Values.Clone();
        byte[] beforeConfidence = (byte[])matrix.Confidence.Clone();

        QrModuleRole[] roles = processor.ClassifyModules(matrix, sample.Kind, sample.Version, sample.Level);

        Assert.Multiple(() =>
        {
            Assert.That(roles, Is.EqualTo(PadRoles(sample, ExpectedRoles(sample), 1, 3, 2, 4)));
            Assert.That(matrix.Roles, Is.SameAs(roles));
            Assert.That(matrix.Values, Is.EqualTo(beforeValues));
            Assert.That(matrix.Confidence, Is.EqualTo(beforeConfidence));
        });
    }

    static QrModuleRole[] ExpectedRoles(Layout sample)
    {
        int[] centers = sample.Kind == QrSymbolKind.Model2
            ? Model2Alignment[sample.Version - 1].Split(' ', StringSplitOptions.RemoveEmptyEntries).Select(int.Parse).ToArray()
            : sample.Kind == QrSymbolKind.rMQR ? sample.Width switch
            {
                27 => [], 43 => [21], 59 => [19, 39], 77 => [25, 51],
                99 => [23, 49, 75], 139 => [27, 55, 83, 111],
                _ => throw new AssertionException("Unexpected rMQR width")
            } : [];
        QrModuleRole[] roles = new QrModuleRole[sample.Width * sample.Height];
        for (int y = 0; y < sample.Height; y++)
            for (int x = 0; x < sample.Width; x++)
                roles[y * sample.Width + x] = FunctionRole(sample, x, y, centers);

        int bit = 0;
        bool up = true;
        int firstColumn = sample.Width - (sample.Kind == QrSymbolKind.rMQR ? 2 : 1);
        for (int right = firstColumn; right > 0; right -= 2, up = !up)
        {
            if (sample.Kind == QrSymbolKind.Model2 && right == 6) right--;
            for (int row = 0; row < sample.Height; row++)
                for (int side = 0; side < 2; side++)
                {
                    int y = up ? sample.Height - 1 - row : row;
                    int i = y * sample.Width + right - side;
                    if (roles[i] != QrModuleRole.Unknown) continue;
                    roles[i] = bit < sample.DataBits ? QrModuleRole.Data
                        : bit < sample.DataBits + sample.EccBits ? QrModuleRole.ErrorCorrection : QrModuleRole.Remainder;
                    bit++;
                }
        }

        Assert.That(bit, Is.EqualTo(sample.DataBits + sample.EccBits + sample.RemainderBits), "Reference layout payload area");
        Assert.That(roles, Has.None.EqualTo(QrModuleRole.Unknown));
        return roles;
    }

    static QrModuleRole FunctionRole(Layout s, int x, int y, int[] centers)
    {
        int w = s.Width;
        int h = s.Height;
        if (s.Kind == QrSymbolKind.rMQR)
        {
            if (x >= w - 5 && y >= h - 5 || x >= w - 2 && y < 2
                || y == h - 1 && x < 3 || h >= 11 && y == h - 2 && x < 2)
                return QrModuleRole.Finder;
            if (y >= 1 && y <= 5 && x >= 8 && x <= 10 || x == 11 && y >= 1 && y <= 3
                || y >= h - 6 && y <= h - 2 && x >= w - 8 && x <= w - 6
                || y == h - 6 && x >= w - 5 && x <= w - 3)
                return QrModuleRole.Format;
            if (centers.Any(c => Math.Abs(x - c) <= 1) && (y < 3 || y >= h - 3))
                return QrModuleRole.Alignment;
        }

        if (x < 7 && y < 7 || s.Kind == QrSymbolKind.Model2 && (x >= w - 7 && y < 7 || x < 7 && y >= h - 7))
            return QrModuleRole.Finder;
        if (x < 8 && y < 8 || s.Kind == QrSymbolKind.Model2 && (x >= w - 8 && y < 8 || x < 8 && y >= h - 8))
            return QrModuleRole.Separator;

        if (s.Kind == QrSymbolKind.Model2)
        {
            foreach (int cy in centers)
                foreach (int cx in centers)
                    if (!(cx == 6 && (cy == 6 || cy == h - 7) || cy == 6 && cx == w - 7)
                        && Math.Abs(x - cx) <= 2 && Math.Abs(y - cy) <= 2)
                        return QrModuleRole.Alignment;
            if (x == 6 || y == 6) return QrModuleRole.Timing;
            if (x == 8 && y == h - 8) return QrModuleRole.DarkModule;
            if (x == 8 && (y <= 8 || y >= h - 7) || y == 8 && (x <= 8 || x >= w - 8))
                return QrModuleRole.Format;
            if (s.Version >= 7 && (x < 6 && y >= h - 11 && y < h - 8 || y < 6 && x >= w - 11 && x < w - 8))
                return QrModuleRole.Version;
        }
        else if (s.Kind == QrSymbolKind.Micro)
        {
            if (x == 0 || y == 0) return QrModuleRole.Timing;
            if (x == 8 && y <= 8 || y == 8 && x <= 8) return QrModuleRole.Format;
        }
        else if (x == 0 || x == w - 1 || y == 0 || y == h - 1 || centers.Contains(x))
            return QrModuleRole.Timing;
        return QrModuleRole.Unknown;
    }

    static QrModuleRole[] PadRoles(Layout sample, QrModuleRole[] core, int left, int top, int right, int bottom)
    {
        int width = sample.Width + left + right;
        QrModuleRole[] result = Enumerable.Repeat(QrModuleRole.QuietZone, width * (sample.Height + top + bottom)).ToArray();
        for (int y = 0; y < sample.Height; y++)
            Array.Copy(core, y * sample.Width, result, (y + top) * width + left, sample.Width);
        return result;
    }

    static QrModuleValue[] PadValues(Layout sample, int left, int top, int right, int bottom)
    {
        int width = sample.Width + left + right;
        QrModuleValue[] result = Enumerable.Repeat(QrModuleValue.Light, width * (sample.Height + top + bottom)).ToArray();
        for (int y = 0; y < sample.Height; y++)
            for (int x = 0; x < sample.Width; x++)
                result[(y + top) * width + x + left] = sample.Golden[y * sample.Width + x] ? QrModuleValue.Dark : QrModuleValue.Light;
        return result;
    }

    static int InternalLevel(Layout sample) => sample.Kind == QrSymbolKind.rMQR
        ? sample.Level == QrErrorCorrectionLevel.Medium ? 0 : 1 : (int)sample.Level;

    static IEnumerable<TestCaseData> Cases()
    {
        // Reuse independent test-owned capacity rows, not production capacity tables.
        var capacities = Model2CapacityCalculatorTests.PublicBoundaryCases()
            .Concat(MicroCapacityCalculatorTests.PublicBoundaryCases())
            .Concat(RectangularMicroCapacityCalculatorTests.PublicBoundaryCases())
            .Where(c => ((QrCapacityRequest)c.Arguments[0]!).EncodingMode == QrEncodingMode.Numeric)
            .ToDictionary(c => Key((QrCapacityRequest)c.Arguments[0]!), c => c.Arguments);
        string path = Path.GetFullPath(Path.Combine(TestContext.CurrentContext.TestDirectory,
            "..", "..", "..", "..", "tests", "Fixtures", "Golden", "matrix-combinations.json.gz"));
        using FileStream file = File.OpenRead(path);
        using GZipStream stream = new GZipStream(file, CompressionMode.Decompress);
        using JsonDocument document = JsonDocument.Parse(stream);
        foreach (JsonElement v in document.RootElement.GetProperty("vectors").EnumerateArray())
        {
            QrSymbolKind kind = Enum.Parse<QrSymbolKind>(v.GetProperty("family").GetString()!);
            int mask = v.GetProperty("mask").GetInt32();
            if (mask != (kind == QrSymbolKind.rMQR ? 4 : 0)) continue;
            Layout sample = new Layout
            {
                Kind = kind, Version = v.GetProperty("version").GetInt32(), Mask = mask,
                Level = Enum.Parse<QrErrorCorrectionLevel>(v.GetProperty("level").GetString()!),
                Width = v.GetProperty("width").GetInt32(), Height = v.GetProperty("height").GetInt32(),
                Codewords = v.GetProperty("codewords").GetBytesFromBase64()
            };
            object?[] capacity = capacities[Key(new QrCapacityRequest
            {
                SymbolKind = kind, Version = sample.Version, Width = sample.Width, Height = sample.Height,
                ErrorCorrectionLevel = sample.Level
            })];
            sample.DataBits = (int)capacity[3]!;
            sample.EccBits = ((int)capacity[5]! - (int)capacity[4]!) * 8;
            sample.RemainderBits = (int)capacity[6]!;
            byte[] packed = v.GetProperty("matrix_bits").GetBytesFromBase64();
            sample.Golden = Enumerable.Range(0, sample.Width * sample.Height)
                .Select(i => (packed[i / 8] & (1 << (7 - i % 8))) != 0).ToArray();
            yield return new TestCaseData(sample).SetName("{m}_" + v.GetProperty("id").GetString());
        }
    }

    static string Key(QrCapacityRequest request) => request.SymbolKind == QrSymbolKind.rMQR
        ? $"rMQR-{request.Width}-{request.Height}-{request.ErrorCorrectionLevel}"
        : $"{request.SymbolKind}-{request.Version}-{request.ErrorCorrectionLevel}";

    public class Layout
    {
        public QrSymbolKind Kind;
        public int Version, Width, Height, Mask, DataBits, EccBits, RemainderBits;
        public QrErrorCorrectionLevel Level;
        public byte[] Codewords = [];
        public bool[] Golden = [];
    }
}
