using qrcode;

namespace qrcode.Tests;

[TestFixture]
public class QrMatrixTransformTests
{
    [TestCaseSource(nameof(OrientationCases))]
    public void OrientationAndInversePreserveEveryCellAndMargin(
        QrSymbolKind kind, int width, int height, int turns, bool mirror, int metadata)
    {
        IQrMatrixProcessor processor = new QrMatrixProcessor();
        QrMatrix source = CreateSource(width, height, metadata);
        Cell[][] original = ReadCells(source);
        foreach (bool invert in new[] { false, true })
        {
            QrMatrix result = processor.TransformMatrix(source, kind, turns, mirror, invert, 0, 0, 0, 0, false, 0, 1);
            Cell[][] expected = Orient(original, turns, mirror);
            if (invert) expected = Invert(expected);
            AssertGrid(result, expected, metadata);
            AssertIndependent(source, result);

            // Transpose followed by rotation is a reflection and is its own inverse.
            int inverseTurns = mirror ? turns : (4 - turns) % 4;
            QrMatrix restored = processor.TransformMatrix(result, kind, inverseTurns, mirror, invert, 0, 0, 0, 0, false, 0, 1);
            AssertGrid(restored, original, metadata);
            AssertIndependent(result, restored);
            AssertGrid(source, original, metadata);
        }
    }

    [TestCaseSource(nameof(PipelineCases))]
    public void CombinedPipelineMatchesIndependentCellGrid(
        QrSymbolKind kind, int turns, bool mirror, bool invert, int metadata)
    {
        IQrMatrixProcessor processor = new QrMatrixProcessor();
        QrMatrix source = CreateSource(5, 3, metadata);
        Cell[][] original = ReadCells(source);
        // Crop removes part of the left encoding edge and different amounts of each margin.
        Cell[][] cropped = original.Skip(1).Take(source.Height - 5)
            .Select(row => row.Skip(2).Take(source.Width - 3).ToArray()).ToArray();
        Cell[][] oriented = Orient(cropped, turns, mirror);
        foreach (int quietMode in new[] { -2, -1, 0, 3 })
            foreach (int scale in new[] { 1, 2 })
            {
                bool replace = quietMode != -2;
                int quiet = quietMode == -1 ? kind is QrSymbolKind.Model2 or QrSymbolKind.LegacyModel1 ? 4 : 2 : quietMode;
                Cell[][] expected = replace ? ReplaceQuietZone(oriented, quiet) : oriented;
                if (invert) expected = Invert(expected);
                expected = expected.SelectMany(row => Enumerable.Repeat(
                    row.SelectMany(cell => Enumerable.Repeat(cell, scale)).ToArray(), scale)).ToArray();

                QrMatrix result = processor.TransformMatrix(source, kind, turns, mirror, invert,
                    2, 1, 1, 4, replace, replace ? quietMode : 0, scale);

                AssertGrid(result, expected, metadata);
                AssertIndependent(source, result);
                AssertGrid(source, original, metadata);
            }
    }

    [TestCase(0)]
    [TestCase(1)]
    [TestCase(2)]
    [TestCase(3)]
    public void EveryCropRectangleRetainsTheCorrectMarginsOrRejectsQuietZoneOnly(int metadata)
    {
        IQrMatrixProcessor processor = new QrMatrixProcessor();
        QrMatrix source = CreateSource(2, 1, metadata);
        Cell[][] original = ReadCells(source);
        for (int top = 0; top < source.Height; top++)
            for (int left = 0; left < source.Width; left++)
                for (int bottom = top + 1; bottom <= source.Height; bottom++)
                    for (int right = left + 1; right <= source.Width; right++)
                    {
                        Cell[][] expected = original.Skip(top).Take(bottom - top)
                            .Select(row => row.Skip(left).Take(right - left).ToArray()).ToArray();
                        if (!expected.Any(row => row.Any(cell => cell.Encoding)))
                        {
                            Assert.Throws<QrInputException>(() => processor.TransformMatrix(source,
                                QrSymbolKind.rMQR, 0, false, false, left, top, source.Width - right,
                                source.Height - bottom, false, 0, 1));
                            Assert.Throws<QrInputException>(() => MatrixTransformer.Crop(source, left, top, right - left, bottom - top));
                        }
                        else
                        {
                            QrMatrix result = processor.TransformMatrix(source, QrSymbolKind.rMQR,
                                0, false, false, left, top, source.Width - right, source.Height - bottom, false, 0, 1);
                            AssertGrid(result, expected, metadata);
                            AssertIndependent(source, result);
                        }
                    }
        AssertGrid(source, original, metadata);
    }

    [TestCase(0)]
    [TestCase(1)]
    [TestCase(2)]
    [TestCase(3)]
    public void PaddingRemovalAndScaleSamplingRestoreOriginalCells(int metadata)
    {
        QrMatrix padded = CreateSource(3, 2, metadata);
        Cell[][] original = ReadCells(padded);
        Cell[][] expectedCore = ReplaceQuietZone(original, 0);
        QrMatrix core = MatrixTransformer.RemoveQuietZone(padded);
        AssertGrid(core, expectedCore, metadata);
        AssertIndependent(padded, core);
        foreach (QrSymbolKind kind in Enum.GetValues<QrSymbolKind>())
            foreach (int quiet in new[] { 0, 1, 4 })
            {
                QrMatrix added = MatrixTransformer.AddQuietZone(core, kind, quiet);
                AssertGrid(added, ReplaceQuietZone(expectedCore, quiet), metadata);
                QrMatrix restored = MatrixTransformer.RemoveQuietZone(added);
                AssertGrid(restored, expectedCore, metadata);
                AssertIndependent(core, added);
                AssertIndependent(added, restored);
            }
        foreach (int scale in new[] { 1, 2, 3, 7 })
        {
            QrMatrix scaled = MatrixTransformer.Scale(padded, scale);
            Cell[][] cells = ReadCells(scaled);
            // Sample each possible phase within a replicated module, not just its top-left.
            for (int dy = 0; dy < scale; dy++)
                for (int dx = 0; dx < scale; dx++)
                {
                    Cell[][] sampled = cells.Where((row, y) => y % scale == dy)
                        .Select(row => row.Where((cell, x) => x % scale == dx).ToArray()).ToArray();
                    Assert.That(sampled.SelectMany(row => row), Is.EqualTo(original.SelectMany(row => row)));
                }
            AssertIndependent(padded, scaled);
        }
        AssertGrid(padded, original, metadata);
    }

    [TestCase(-1)]
    [TestCase(0)]
    [TestCase(int.MinValue)]
    public void NonpositiveScaleIsRejectedWithoutChangingInput(int scale)
    {
        QrMatrix source = CreateSource(3, 2, 3);
        Cell[][] original = ReadCells(source);
        IQrMatrixProcessor processor = new QrMatrixProcessor();
        Assert.Throws<QrInputException>(() => processor.TransformMatrix(source,
            QrSymbolKind.rMQR, 3, true, true, 0, 0, 0, 0, true, 2, scale));
        Assert.Throws<QrInputException>(() => MatrixTransformer.Scale(source, scale));
        AssertGrid(source, original, 3);
    }

    [TestCase(0)]
    [TestCase(1)]
    [TestCase(2)]
    [TestCase(3)]
    public void OversizedScaleAndQuietZoneFailWithResourceLimitExceptions(int metadata)
    {
        QrMatrix source = CreateSource(3, 2, metadata);
        Cell[][] original = ReadCells(source);
        IQrMatrixProcessor processor = new QrMatrixProcessor();
        // 300 stays within axis/pixel limits, but exceeds the allocation budget.
        foreach (int scale in new[] { 300, 16385, int.MaxValue })
        {
            Assert.Throws<QrResourceLimitException>(() => MatrixTransformer.Scale(source, scale));
            Assert.Throws<QrResourceLimitException>(() => processor.TransformMatrix(source,
                QrSymbolKind.rMQR, 0, false, false, 0, 0, 0, 0, false, 0, scale));
        }
        Assert.Throws<QrResourceLimitException>(() => processor.TransformMatrix(source,
            QrSymbolKind.rMQR, 0, false, false, 0, 0, 0, 0, true, int.MaxValue, 1));
        AssertGrid(source, original, metadata);
    }

    [TestCaseSource(nameof(InvalidCropCases))]
    public void InvalidCropEdgesAreRejectedWithoutChangingInput(int left, int top, int right, int bottom)
    {
        QrMatrix source = CreateSource(3, 2, 3); // 7 x 8 including margins
        Cell[][] original = ReadCells(source);
        IQrMatrixProcessor processor = new QrMatrixProcessor();
        Assert.Throws<QrInputException>(() => processor.TransformMatrix(source,
            QrSymbolKind.rMQR, 0, false, false, left, top, right, bottom, false, 0, 1));
        AssertGrid(source, original, 3);
    }

    static IEnumerable<TestCaseData> InvalidCropCases()
    {
        for (int side = 0; side < 4; side++)
            foreach (int amount in new[] { -1, int.MinValue, side % 2 == 0 ? 7 : 8, int.MaxValue })
            {
                int[] edges = new int[4];
                edges[side] = amount;
                yield return new TestCaseData(edges[0], edges[1], edges[2], edges[3]);
            }
        yield return new TestCaseData(3, 0, 4, 0);
        yield return new TestCaseData(0, 4, 0, 4);
        yield return new TestCaseData(int.MaxValue, 0, int.MaxValue, 0);
        yield return new TestCaseData(0, int.MaxValue, 0, int.MaxValue);
    }

    static IEnumerable<TestCaseData> PipelineCases()
    {
        foreach (QrSymbolKind kind in Enum.GetValues<QrSymbolKind>())
            for (int turns = 0; turns < 4; turns++)
                foreach (bool mirror in new[] { false, true })
                    foreach (bool invert in new[] { false, true })
                        for (int metadata = 0; metadata < 4; metadata++)
                            yield return new TestCaseData(kind, turns, mirror, invert, metadata);
    }

    static IEnumerable<TestCaseData> OrientationCases()
    {
        List<(QrSymbolKind Kind, int Width, int Height)> shapes =
        [
            (QrSymbolKind.Model2, 21, 21), (QrSymbolKind.Model2, 177, 177),
            (QrSymbolKind.Micro, 11, 11), (QrSymbolKind.Micro, 17, 17),
            (QrSymbolKind.LegacyModel1, 21, 21), (QrSymbolKind.LegacyModel1, 73, 73),
            (QrSymbolKind.rMQR, 1, 1), (QrSymbolKind.rMQR, 1, 5),
            (QrSymbolKind.rMQR, 5, 1), (QrSymbolKind.rMQR, 3, 2)
        ];
        // Independent standardized rMQR dimensions, including every non-square size.
        foreach (int height in new[] { 7, 9, 11, 13, 15, 17 })
            foreach (int width in new[] { 27, 43, 59, 77, 99, 139 })
                if (width != 27 || height is 11 or 13) shapes.Add((QrSymbolKind.rMQR, width, height));
        foreach (var shape in shapes)
            for (int turns = 0; turns < 4; turns++)
                foreach (bool mirror in new[] { false, true })
                    for (int metadata = 0; metadata < 4; metadata++)
                        yield return new TestCaseData(shape.Kind, shape.Width, shape.Height, turns, mirror, metadata);
    }

    // The reference model manipulates rows/columns of cells. Encoding membership travels
    // with each cell, so expected margins are derived from geometry instead of copied
    // from the production transform's edge-permutation formulas.
    record Cell(QrModuleValue Value, QrModuleRole Role, byte Confidence, bool Encoding);

    static QrMatrix CreateSource(int width, int height, int metadata)
    {
        QrMatrix result = new QrMatrix
        {
            Width = width + 4, Height = height + 6,
            QuietZoneLeft = 1, QuietZoneTop = 2, QuietZoneRight = 3, QuietZoneBottom = 4
        };
        int area = result.Width * result.Height;
        Random random = new Random(12345);
        result.Values = Enumerable.Range(0, area).Select(i => (QrModuleValue)random.Next(3)).ToArray();
        if ((metadata & 1) != 0) result.Roles = Enumerable.Range(0, area).Select(i => (QrModuleRole)random.Next(12)).ToArray();
        if ((metadata & 2) != 0) result.Confidence = Enumerable.Range(0, area).Select(i => (byte)random.Next(256)).ToArray();
        return result;
    }

    static Cell[][] ReadCells(QrMatrix matrix) => Enumerable.Range(0, matrix.Height).Select(y =>
        Enumerable.Range(0, matrix.Width).Select(x => new Cell(matrix.Values[y * matrix.Width + x],
            matrix.Roles == null ? QrModuleRole.Unknown : matrix.Roles[y * matrix.Width + x],
            matrix.Confidence == null ? (byte)0 : matrix.Confidence[y * matrix.Width + x],
            x >= matrix.QuietZoneLeft && x < matrix.Width - matrix.QuietZoneRight
            && y >= matrix.QuietZoneTop && y < matrix.Height - matrix.QuietZoneBottom)).ToArray()).ToArray();

    static Cell[][] Transpose(Cell[][] rows) => Enumerable.Range(0, rows[0].Length)
        .Select(x => rows.Select(row => row[x]).ToArray()).ToArray();

    static Cell[][] Orient(Cell[][] rows, int turns, bool mirror)
    {
        for (int i = 0; i < turns; i++) rows = Transpose(rows.Reverse().ToArray());
        return mirror ? Transpose(rows) : rows;
    }

    static Cell[][] Invert(Cell[][] rows) => rows.Select(row => row.Select(cell => cell with
    {
        Value = cell.Value switch
        {
            QrModuleValue.Light => QrModuleValue.Dark,
            QrModuleValue.Dark => QrModuleValue.Light,
            _ => cell.Value
        }
    }).ToArray()).ToArray();

    static Cell[][] ReplaceQuietZone(Cell[][] rows, int quiet)
    {
        Cell[][] core = rows.Where(row => row.Any(cell => cell.Encoding))
            .Select(row => row.Where(cell => cell.Encoding).ToArray()).ToArray();
        Cell blank = new Cell(QrModuleValue.Light, QrModuleRole.QuietZone, 255, false);
        int width = core[0].Length + 2 * quiet;
        Cell[] border = Enumerable.Repeat(blank, width).ToArray();
        return Enumerable.Repeat(border, quiet)
            .Concat(core.Select(row => Enumerable.Repeat(blank, quiet).Concat(row).Concat(Enumerable.Repeat(blank, quiet)).ToArray()))
            .Concat(Enumerable.Repeat(border, quiet)).ToArray();
    }

    static void AssertGrid(QrMatrix actual, Cell[][] expected, int metadata)
    {
        var encoding = expected.SelectMany((row, y) => row.Select((cell, x) => (cell.Encoding, X: x, Y: y)))
            .Where(cell => cell.Encoding).ToArray();
        Cell[] flat = expected.SelectMany(row => row).ToArray();
        Assert.Multiple(() =>
        {
            Assert.That(actual.Width, Is.EqualTo(expected[0].Length));
            Assert.That(actual.Height, Is.EqualTo(expected.Length));
            Assert.That(actual.QuietZoneLeft, Is.EqualTo(encoding.Min(cell => cell.X)));
            Assert.That(actual.QuietZoneTop, Is.EqualTo(encoding.Min(cell => cell.Y)));
            Assert.That(actual.QuietZoneRight, Is.EqualTo(expected[0].Length - 1 - encoding.Max(cell => cell.X)));
            Assert.That(actual.QuietZoneBottom, Is.EqualTo(expected.Length - 1 - encoding.Max(cell => cell.Y)));
            Assert.That(actual.Values, Is.EqualTo(flat.Select(cell => cell.Value)));
            if ((metadata & 1) != 0) Assert.That(actual.Roles, Is.EqualTo(flat.Select(cell => cell.Role)));
            else Assert.That(actual.Roles, Is.Null);
            if ((metadata & 2) != 0) Assert.That(actual.Confidence, Is.EqualTo(flat.Select(cell => cell.Confidence)));
            else Assert.That(actual.Confidence, Is.Null);
        });
    }

    static void AssertIndependent(QrMatrix source, QrMatrix result)
    {
        Assert.That(result, Is.Not.SameAs(source));
        Assert.That(result.Values, Is.Not.SameAs(source.Values));
        if (source.Roles != null) Assert.That(result.Roles, Is.Not.SameAs(source.Roles));
        if (source.Confidence != null) Assert.That(result.Confidence, Is.Not.SameAs(source.Confidence));
    }
}
