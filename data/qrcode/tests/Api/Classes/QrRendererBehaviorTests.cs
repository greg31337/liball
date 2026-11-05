using System.Text.Json;
using qrcode;

namespace qrcode.Tests;

[TestFixture]
public class QrRendererBehaviorTests
{
    [Test]
    public void RawPixelsMatchExactScaledCoreAndReplacementQuietZone(
        [Values(QrSymbolKind.Model2, QrSymbolKind.Micro, QrSymbolKind.rMQR, QrSymbolKind.LegacyModel1)] QrSymbolKind kind,
        [Values(-1, 0, 1, 5)] int quietZone,
        [Values(1, 2, 5)] int scale,
        [Values(false, true)] bool invert)
    {
        QrSymbol symbol = AsymmetricSymbol(kind);
        string before = Snapshot(symbol);
        IQrRenderer renderer = new QrRenderer();
        foreach ((byte dark, byte light) in new[] { ((byte)0, (byte)255), ((byte)17, (byte)231), ((byte)240, (byte)12), ((byte)77, (byte)77) })
        {
            QrRenderOptions options = new()
            {
                Scale = scale, QuietZoneModules = quietZone, Invert = invert,
                DarkValue = dark, LightValue = light
            };
            string optionsBefore = Snapshot(options);
            byte foreground = invert ? light : dark;
            byte background = invert ? dark : light;
            byte[] core = [foreground, background, foreground, background, foreground, background];
            int border = quietZone >= 0 ? quietZone : kind is QrSymbolKind.Micro or QrSymbolKind.rMQR ? 2 : 4;
            AssertRaster(renderer.RenderSymbol(symbol, options), core, 3, 2, scale, border, background);
            options.IncludeQuietZone = false;
            AssertRaster(renderer.RenderSymbol(symbol, options), core, 3, 2, scale, 0, background);
            options.IncludeQuietZone = true;
            Assert.That(Snapshot(options), Is.EqualTo(optionsBefore));
        }
        Assert.That(Snapshot(symbol), Is.EqualTo(before));
    }

    [Test]
    public void EveryRoleHasExactPaletteAndHighlightThroughBothEntryPoints(
        [Values] QrModuleRole highlight,
        [Values(false, true)] bool invert,
        [Values(false, true)] bool lightModules,
        [Values(1, 3)] int scale)
    {
        QrSymbol symbol = RoleSymbol();
        Array.Fill(symbol.Matrix!.Values, lightModules ? QrModuleValue.Light : QrModuleValue.Dark);
        // Unknown, quiet zone, seven function roles, data, ECC, remainder.
        int[] categories = [6, 7, 1, 1, 1, 1, 1, 1, 1, 2, 3, 4];
        byte[] palette = invert
            ? [255, 219, 182, 146, 109, 73, 36, 0]
            : [0, 36, 73, 109, 146, 182, 219, 255];
        byte[] core = categories.Select((category, i) =>
            palette[highlight != QrModuleRole.Unknown && (int)highlight == i ? 0 : category]).ToArray();
        QrRenderOptions options = new()
        {
            Scale = scale, QuietZoneModules = 1, Invert = invert, HighlightRole = highlight
        };
        string before = Snapshot(symbol);
        string optionsBefore = Snapshot(options);
        IQrRenderer renderer = new QrRenderer();
        byte border = palette[highlight == QrModuleRole.QuietZone ? 0 : 7];
        AssertRaster(renderer.RenderRoleMap(symbol, options), core, 4, 3, scale, 1, border);
        Assert.That(Snapshot(options), Is.EqualTo(optionsBefore));
        options.RenderRoleMap = true;
        AssertRaster(renderer.RenderSymbol(symbol, options), core, 4, 3, scale, 1, border);
        Assert.That(Snapshot(symbol), Is.EqualTo(before));
    }

    [TestCase(10, 220, false, new byte[] { 160, 190, 160, 160, 70, 220 })]
    [TestCase(10, 220, true, new byte[] { 70, 40, 70, 70, 160, 10 })]
    [TestCase(77, 77, false, new byte[] { 77, 77, 77, 77, 77, 77 })]
    public void DamageAndUnknownValuesTakePrecedenceOverHighlights(
        byte dark, byte light, bool invert, byte[] expected)
    {
        QrSymbol symbol = AsymmetricSymbol(QrSymbolKind.Model2);
        QrMatrix matrix = symbol.Matrix!;
        // Core source indices: 8, 9, 10 / 14, 15, 16. Damage is unsorted,
        // duplicated, and includes discarded border cells on both sides.
        matrix.Roles![8] = QrModuleRole.Unknown;
        matrix.Roles[9] = QrModuleRole.Unknown;
        matrix.Roles[10] = QrModuleRole.Finder;
        matrix.Roles[14] = QrModuleRole.Finder;
        matrix.Roles[15] = QrModuleRole.Data;
        matrix.Roles[16] = QrModuleRole.QuietZone;
        matrix.Values[10] = QrModuleValue.Unknown;
        symbol.Validation = new QrValidationResult
        {
            DamagedModuleIndices = [14, 35, 8, 0, 14],
            DamagedRoles = [QrModuleRole.Finder, QrModuleRole.QuietZone, QrModuleRole.Unknown,
                QrModuleRole.QuietZone, QrModuleRole.Finder]
        };
        string before = Snapshot(symbol);
        QrRenderOptions options = new()
        {
            QuietZoneModules = 1, Scale = 3, DarkValue = dark, LightValue = light,
            Invert = invert, HighlightRole = QrModuleRole.Finder
        };
        IQrRenderer renderer = new QrRenderer();
        AssertRaster(renderer.RenderRoleMap(symbol, options), expected, 3, 2, 3, 1, invert ? dark : light);
        options.RenderRoleMap = true;
        AssertRaster(renderer.RenderSymbol(symbol, options), expected, 3, 2, 3, 1, invert ? dark : light);
        Assert.That(Snapshot(symbol), Is.EqualTo(before));
    }

    [Test]
    public void EncodedSymbolsRenderEveryModuleWithoutDuplicatingTheirQuietZone(
        [Values(QrSymbolKind.Model2, QrSymbolKind.Micro, QrSymbolKind.rMQR)] QrSymbolKind kind,
        [Values(false, true)] bool includeQuietZone)
    {
        QrSymbol symbol = new QrEncoder().EncodeText("12345", new QrEncodeOptions
        {
            ForceSymbolKind = true, SymbolKind = kind,
            ErrorCorrectionLevel = QrErrorCorrectionLevel.Medium, QuietZoneModules = 3
        });
        QrMatrix matrix = symbol.Matrix!;
        byte[] core = new byte[symbol.Width * symbol.Height];
        for (int y = 0; y < symbol.Height; y++)
            for (int x = 0; x < symbol.Width; x++)
                core[y * symbol.Width + x] = matrix.Values[(y + 3) * matrix.Width + x + 3] == QrModuleValue.Dark ? (byte)9 : (byte)249;
        int border = includeQuietZone ? kind == QrSymbolKind.Model2 ? 4 : 2 : 0;
        IQrRenderer renderer = new QrRenderer();
        AssertRaster(renderer.RenderSymbol(symbol, new QrRenderOptions
        {
            IncludeQuietZone = includeQuietZone, Scale = 3, DarkValue = 9, LightValue = 249
        }), core, symbol.Width, symbol.Height, 3, border, 249);
    }

    [Test]
    public void ReturnedBuffersAreIndependentAcrossCallsAndInputMutations([Values(0, 1, 2)] int path)
    {
        QrSymbol symbol = AsymmetricSymbol(QrSymbolKind.Micro);
        QrRenderOptions options = new() { QuietZoneModules = 0 };
        IQrRenderer renderer = new QrRenderer();
        QrRaster first = Render(renderer, symbol, options, path);
        QrRaster second = Render(renderer, symbol, options, path);
        byte[] original = (byte[])second.Pixels.Clone();
        Assert.That(first.Pixels, Is.Not.SameAs(second.Pixels));
        first.Pixels[0] ^= 255;
        symbol.Matrix!.Values[8] = QrModuleValue.Light;
        symbol.Matrix.Roles![8] = QrModuleRole.ErrorCorrection;
        options.DarkValue = 100;
        Assert.That(second.Pixels, Is.EqualTo(original));
        Assert.That(Render(renderer, symbol, options, path).Pixels, Is.Not.EqualTo(original));
    }

    [TestCaseSource(nameof(InvalidMatrixCases))]
    public void RejectsMalformedCallerMatricesThroughEveryEntryPoint(Action<QrSymbol> corrupt)
    {
        foreach (int path in new[] { 0, 1, 2 })
        {
            QrSymbol symbol = AsymmetricSymbol(QrSymbolKind.Model2);
            corrupt(symbol);
            string before = Snapshot(symbol);
            Assert.That(() => Render(new QrRenderer(), symbol, new QrRenderOptions(), path),
                Throws.TypeOf<QrInputException>(), $"entry point {path}");
            Assert.That(Snapshot(symbol), Is.EqualTo(before));
        }
    }

    private static IEnumerable<TestCaseData> InvalidMatrixCases()
    {
        (string name, Action<QrSymbol> corrupt)[] cases =
        [
            ("missing matrix", s => s.Matrix = null),
            ("unknown family", s => s.SymbolKind = (QrSymbolKind)99),
            ("zero width", s => s.Matrix!.Width = 0),
            ("negative height", s => s.Matrix!.Height = -1),
            ("null values", s => s.Matrix!.Values = null!),
            ("short values", s => s.Matrix!.Values = new QrModuleValue[35]),
            ("long values", s => s.Matrix!.Values = new QrModuleValue[37]),
            ("short roles", s => s.Matrix!.Roles = new QrModuleRole[35]),
            ("long roles", s => s.Matrix!.Roles = new QrModuleRole[37]),
            ("short confidence", s => s.Matrix!.Confidence = new byte[35]),
            ("long confidence", s => s.Matrix!.Confidence = new byte[37]),
            ("negative left", s => s.Matrix!.QuietZoneLeft = -1),
            ("negative right", s => s.Matrix!.QuietZoneRight = -1),
            ("negative top", s => s.Matrix!.QuietZoneTop = -1),
            ("negative bottom", s => s.Matrix!.QuietZoneBottom = -1),
            ("empty core width", s => s.Matrix!.QuietZoneLeft = 5),
            ("empty core height", s => s.Matrix!.QuietZoneTop = 3),
            ("overflowing horizontal border", s => { s.Matrix!.QuietZoneLeft = int.MaxValue; s.Matrix.QuietZoneRight = int.MaxValue; }),
            ("overflowing vertical border", s => { s.Matrix!.QuietZoneTop = int.MaxValue; s.Matrix.QuietZoneBottom = int.MaxValue; }),
            ("invalid negative value", s => s.Matrix!.Values[8] = (QrModuleValue)(-2)),
            ("invalid positive value", s => s.Matrix!.Values[8] = (QrModuleValue)99)
        ];
        foreach (var item in cases) yield return new TestCaseData(item.corrupt).SetName($"RejectsMalformedMatrix_{item.name}");
    }

    [Test]
    public void RejectsInvalidScaleAndQuietZoneEvenWhenBorderIsDisabled(
        [Values(0, 1, 2)] int path,
        [Values(0, -1, int.MinValue)] int scale)
    {
        QrSymbol symbol = AsymmetricSymbol(QrSymbolKind.Model2);
        QrInputException error = Assert.Throws<QrInputException>(() => Render(new QrRenderer(), symbol,
            new QrRenderOptions { Scale = scale }, path))!;
        Assert.That(error.InputName, Is.EqualTo("Scale"));
        foreach (int quietZone in new[] { -2, int.MinValue })
        {
            QrInputException quietError = Assert.Throws<QrInputException>(() => Render(new QrRenderer(), symbol,
                new QrRenderOptions { QuietZoneModules = quietZone, IncludeQuietZone = false }, path))!;
            Assert.That(quietError.InputName, Is.EqualTo("QuietZoneModules"));
        }
    }

    [TestCaseSource(nameof(InvalidRoleCases))]
    public void RejectsInvalidRoleMapMetadataAndHighlight(Action<QrSymbol, QrRenderOptions> corrupt)
    {
        foreach (int path in new[] { 1, 2 })
        {
            QrSymbol symbol = RoleSymbol();
            symbol.Validation = new QrValidationResult { DamagedModuleIndices = [0], DamagedRoles = [QrModuleRole.Unknown] };
            QrRenderOptions options = new();
            corrupt(symbol, options);
            Assert.That(() => Render(new QrRenderer(), symbol, options, path), Throws.TypeOf<QrInputException>());
        }
    }

    private static IEnumerable<TestCaseData> InvalidRoleCases()
    {
        (string name, Action<QrSymbol, QrRenderOptions> corrupt)[] cases =
        [
            ("missing roles", (s, _) => s.Matrix!.Roles = null),
            ("negative role", (s, _) => s.Matrix!.Roles![0] = (QrModuleRole)(-1)),
            ("unknown role", (s, _) => s.Matrix!.Roles![0] = (QrModuleRole)12),
            ("negative highlight", (_, o) => o.HighlightRole = (QrModuleRole)(-1)),
            ("unknown highlight", (_, o) => o.HighlightRole = (QrModuleRole)12),
            ("null damage indices", (s, _) => s.Validation!.DamagedModuleIndices = null!),
            ("null damage roles", (s, _) => s.Validation!.DamagedRoles = null!),
            ("mismatched damage roles", (s, _) => s.Validation!.DamagedRoles = []),
            ("negative damage index", (s, _) => s.Validation!.DamagedModuleIndices = [-1]),
            ("past end damage index", (s, _) => s.Validation!.DamagedModuleIndices = [12]),
            ("invalid damage role", (s, _) => s.Validation!.DamagedRoles = [(QrModuleRole)12])
        ];
        foreach (var item in cases) yield return new TestCaseData(item.corrupt).SetName($"RejectsRoleMetadata_{item.name}");
    }

    [Test]
    public void RejectsNullArgumentsOnEveryEntryPoint([Values(0, 1, 2)] int path)
    {
        IQrRenderer renderer = new QrRenderer();
        Assert.That(() => Render(renderer, null!, new QrRenderOptions(), path), Throws.TypeOf<QrInputException>());
        // Dispatch requires a non-null option; both actual public methods validate it.
        Assert.That(() => path == 1 ? renderer.RenderRoleMap(RoleSymbol(), null!) : renderer.RenderSymbol(RoleSymbol(), null!),
            Throws.TypeOf<QrInputException>());
    }

    [Test]
    public void ResourceLimitsRejectLargeOutputsWithoutIntegerOverflow(
        [Values(0, 1, 2)] int path,
        [Values("width", "height", "area", "allocation", "scale overflow", "border overflow")] string limit)
    {
        QrSymbol symbol = new()
        {
            SymbolKind = QrSymbolKind.Model2,
            Matrix = new QrMatrix { Width = 1, Height = 1, Values = [QrModuleValue.Dark], Roles = [QrModuleRole.Data] }
        };
        QrRenderOptions options = new() { IncludeQuietZone = false };
        switch (limit)
        {
            case "width": symbol.Matrix.Width = 2; options.Scale = 8193; break;
            case "height": symbol.Matrix.Height = 2; options.Scale = 8193; break;
            case "area": options.Scale = 8193; break;
            case "allocation": options.Scale = 4097; break;
            case "scale overflow": options.Scale = int.MaxValue; break;
            case "border overflow": options.IncludeQuietZone = true; options.QuietZoneModules = int.MaxValue; break;
        }
        int area = symbol.Matrix.Width * symbol.Matrix.Height;
        symbol.Matrix.Values = Enumerable.Repeat(QrModuleValue.Dark, area).ToArray();
        symbol.Matrix.Roles = Enumerable.Repeat(QrModuleRole.Data, area).ToArray();
        Assert.That(() => Render(new QrRenderer(), symbol, options, path), Throws.TypeOf<QrResourceLimitException>());
    }

    [Test]
    public void MaximumDimensionIsAcceptedForASkinnyRaster([Values(0, 1, 2)] int path, [Values(false, true)] bool vertical)
    {
        QrSymbol symbol = new()
        {
            SymbolKind = QrSymbolKind.Model2,
            Matrix = new QrMatrix
            {
                Width = vertical ? 1 : 16384, Height = vertical ? 16384 : 1,
                Values = Enumerable.Repeat(QrModuleValue.Dark, 16384).ToArray(),
                Roles = Enumerable.Repeat(QrModuleRole.Data, 16384).ToArray()
            }
        };
        QrRaster raster = Render(new QrRenderer(), symbol, new QrRenderOptions { IncludeQuietZone = false }, path);
        Assert.That(raster.Width, Is.EqualTo(symbol.Matrix.Width));
        Assert.That(raster.Height, Is.EqualTo(symbol.Matrix.Height));
        Assert.That(raster.Stride, Is.EqualTo(raster.Width));
        Assert.That(raster.Pixels, Has.Length.EqualTo(16384));
        Assert.That(raster.Pixels, Is.All.EqualTo(path == 0 ? 0 : 73));
    }

    private static QrRaster Render(IQrRenderer renderer, QrSymbol symbol, QrRenderOptions options, int path)
    {
        if (path == 1) return renderer.RenderRoleMap(symbol, options);
        options.RenderRoleMap = path == 2;
        return renderer.RenderSymbol(symbol, options);
    }

    private static QrSymbol AsymmetricSymbol(QrSymbolKind kind)
    {
        QrMatrix matrix = new()
        {
            Width = 6, Height = 6, QuietZoneLeft = 2, QuietZoneRight = 1, QuietZoneTop = 1, QuietZoneBottom = 3,
            Values = Enumerable.Repeat((QrModuleValue)99, 36).ToArray(),
            Roles = Enumerable.Repeat((QrModuleRole)99, 36).ToArray(),
            Confidence = Enumerable.Range(0, 36).Select(i => (byte)i).ToArray()
        };
        int[] indices = [8, 9, 10, 14, 15, 16];
        for (int i = 0; i < indices.Length; i++)
        {
            matrix.Values[indices[i]] = i % 2 == 0 ? QrModuleValue.Dark : QrModuleValue.Light;
            matrix.Roles[indices[i]] = QrModuleRole.Data;
        }
        return new QrSymbol { SymbolKind = kind, Width = 3, Height = 2, Matrix = matrix };
    }

    private static QrSymbol RoleSymbol() => new()
    {
        SymbolKind = QrSymbolKind.Model2, Width = 4, Height = 3,
        Matrix = new QrMatrix
        {
            Width = 4, Height = 3,
            Values = Enumerable.Repeat(QrModuleValue.Dark, 12).ToArray(),
            Roles = Enum.GetValues<QrModuleRole>()
        }
    };

    private static void AssertRaster(QrRaster raster, byte[] core, int width, int height, int scale, int border, byte borderValue)
    {
        int expectedWidth = (width + border * 2) * scale;
        int expectedHeight = (height + border * 2) * scale;
        byte[] expected = Enumerable.Repeat(borderValue, expectedWidth * expectedHeight).ToArray();
        // Expand each core row with independent sequence operations, then repeat it vertically.
        for (int row = 0; row < height; row++)
        {
            byte[] expanded = core.Skip(row * width).Take(width).SelectMany(value => Enumerable.Repeat(value, scale)).ToArray();
            for (int repeat = 0; repeat < scale; repeat++)
                expanded.CopyTo(expected, ((row + border) * scale + repeat) * expectedWidth + border * scale);
        }
        Assert.Multiple(() =>
        {
            Assert.That(raster.Width, Is.EqualTo(expectedWidth));
            Assert.That(raster.Height, Is.EqualTo(expectedHeight));
            Assert.That(raster.Stride, Is.EqualTo(expectedWidth));
            Assert.That(raster.Pixels, Is.EqualTo(expected));
        });
    }

    private static string Snapshot(object value) => JsonSerializer.Serialize(value, new JsonSerializerOptions { IncludeFields = true });
}
