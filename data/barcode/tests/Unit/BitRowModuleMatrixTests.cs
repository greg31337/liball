namespace barcode.Tests;

public class BitRowModuleMatrixTests
{
    [Test]
    public void BitRowPacksBitsAcrossWordBoundaries()
    {
        BitRow row = new BitRow(35);
        row.SetBit(0, true);
        row.SetBit(31, true);
        row.SetBit(32, true);
        row.SetBit(34, true);

        Assert.Multiple(() =>
        {
            Assert.That(row.GetBit(0), Is.True);
            Assert.That(row.GetBit(1), Is.False);
            Assert.That(row.GetBit(31), Is.True);
            Assert.That(row.GetBit(32), Is.True);
            Assert.That(row.GetBit(34), Is.True);
            Assert.That(row.CountDarkBits(), Is.EqualTo(4));
            Assert.That(row.CopyPackedWords(), Is.EqualTo(new uint[] { 0x80000001u, 0x00000005u }));
        });
    }

    [Test]
    public void BitRowLoadsCopiesReversesAndClearsWithoutAliasing()
    {
        bool[] source = new bool[] { true, false, true, true, false };
        BitRow row = new BitRow(5);
        row.LoadBits(source);
        source[0] = false;
        bool[] copy = row.CopyBits();
        copy[2] = false;
        row.ReverseBits();

        Assert.That(row.CopyBits(), Is.EqualTo(new bool[] { false, true, true, false, true }));

        row.ClearBits();

        Assert.Multiple(() =>
        {
            Assert.That(row.CopyBits(), Is.EqualTo(new bool[] { false, false, false, false, false }));
            Assert.That(row.CountDarkBits(), Is.EqualTo(0));
        });
    }

    [Test]
    public void BitRowFillMasksUnusedTailBits()
    {
        BitRow row = new BitRow(35);

        row.SetAllBits(true);

        Assert.Multiple(() =>
        {
            Assert.That(row.CountDarkBits(), Is.EqualTo(35));
            Assert.That(row.CopyPackedWords(), Is.EqualTo(new uint[] { uint.MaxValue, 0x00000007u }));
        });

        row.SetBit(34, false);
        Assert.That(row.CountDarkBits(), Is.EqualTo(34));

        row.SetAllBits(false);
        Assert.That(row.CopyPackedWords(), Is.EqualTo(new uint[] { 0u, 0u }));
    }

    [Test]
    public void BitRowRejectsInvalidDimensionsIndicesAndSources()
    {
        BitRow row = new BitRow(3);

        Assert.Multiple(() =>
        {
            Assert.That(() => new BitRow(0), Throws.TypeOf<BarcodeValidationException>());
            Assert.That(() => row.GetBit(-1), Throws.TypeOf<BarcodeValidationException>());
            Assert.That(() => row.GetBit(3), Throws.TypeOf<BarcodeValidationException>());
            Assert.That(() => row.SetBit(3, true), Throws.TypeOf<BarcodeValidationException>());
            Assert.That(() => row.LoadBits(null!), Throws.TypeOf<BarcodeValidationException>().With.Message.EqualTo("Bit source must not be null."));
            Assert.That(() => row.LoadBits(new bool[2]), Throws.TypeOf<BarcodeValidationException>().With.Message.EqualTo("Bit source length does not match the row length."));
        });
    }

    [Test]
    public void MatrixLoadsCopiesMutatesAndClearsWithoutAliasing()
    {
        bool[] source = new bool[] { true, false, false, false, true, true };
        ModuleMatrix matrix = new ModuleMatrix(3, 2);
        matrix.LoadModules(source);
        source[0] = false;
        bool[] copy = matrix.CopyModules();
        copy[4] = false;

        Assert.Multiple(() =>
        {
            Assert.That(matrix.Width, Is.EqualTo(3));
            Assert.That(matrix.Height, Is.EqualTo(2));
            Assert.That(matrix.GetModule(0, 0), Is.True);
            Assert.That(matrix.GetModule(1, 1), Is.True);
            Assert.That(matrix.CopyModules(), Is.EqualTo(new bool[] { true, false, false, false, true, true }));
        });

        matrix.SetModule(2, 0, true);
        Assert.That(matrix.GetModule(2, 0), Is.True);

        matrix.SetAllModules(true);
        Assert.That(matrix.CopyModules(), Is.EqualTo(new bool[] { true, true, true, true, true, true }));

        matrix.ClearModules();
        Assert.That(matrix.CopyModules(), Is.EqualTo(new bool[] { false, false, false, false, false, false }));
    }

    [Test]
    public void MatrixCopiesAndReplacesRowsAndColumns()
    {
        ModuleMatrix matrix = new ModuleMatrix(4, 3);
        bool[] row = new bool[] { true, false, true, false };
        bool[] column = new bool[] { true, true, false };
        matrix.SetRow(1, row);
        matrix.SetColumn(3, column);
        row[0] = false;
        column[0] = false;
        bool[] rowCopy = matrix.CopyRow(1);
        bool[] columnCopy = matrix.CopyColumn(3);
        rowCopy[0] = false;
        columnCopy[0] = false;

        Assert.Multiple(() =>
        {
            Assert.That(matrix.CopyRow(0), Is.EqualTo(new bool[] { false, false, false, true }));
            Assert.That(matrix.CopyRow(1), Is.EqualTo(new bool[] { true, false, true, true }));
            Assert.That(matrix.CopyColumn(3), Is.EqualTo(new bool[] { true, true, false }));
            Assert.That(matrix.GetModule(0, 1), Is.True);
            Assert.That(matrix.GetModule(3, 0), Is.True);
        });
    }

    [Test]
    public void MatrixCropsRequestedRegion()
    {
        ModuleMatrix matrix = CreatePatternMatrix();

        ModuleMatrix crop = matrix.Crop(1, 0, 2, 2);

        Assert.Multiple(() =>
        {
            Assert.That(crop.Width, Is.EqualTo(2));
            Assert.That(crop.Height, Is.EqualTo(2));
            Assert.That(crop.CopyModules(), Is.EqualTo(new bool[] { false, false, true, true }));
        });
    }

    [Test]
    public void MatrixRotatesNonSquarePatternAtEveryRightAngle()
    {
        ModuleMatrix matrix = CreatePatternMatrix();

        ModuleMatrix clockwise90 = matrix.RotateClockwise90();
        ModuleMatrix rotated180 = matrix.Rotate180();
        ModuleMatrix clockwise270 = matrix.RotateClockwise270();

        Assert.Multiple(() =>
        {
            Assert.That(clockwise90.Width, Is.EqualTo(2));
            Assert.That(clockwise90.Height, Is.EqualTo(3));
            Assert.That(clockwise90.CopyModules(), Is.EqualTo(new bool[] { false, true, true, false, true, false }));
            Assert.That(rotated180.Width, Is.EqualTo(3));
            Assert.That(rotated180.Height, Is.EqualTo(2));
            Assert.That(rotated180.CopyModules(), Is.EqualTo(new bool[] { true, true, false, false, false, true }));
            Assert.That(clockwise270.Width, Is.EqualTo(2));
            Assert.That(clockwise270.Height, Is.EqualTo(3));
            Assert.That(clockwise270.CopyModules(), Is.EqualTo(new bool[] { false, true, false, true, true, false }));
        });
    }

    [Test]
    public void MatrixReflectsAcrossHorizontalAndVerticalAxes()
    {
        ModuleMatrix matrix = CreatePatternMatrix();

        ModuleMatrix horizontal = matrix.ReflectHorizontal();
        ModuleMatrix vertical = matrix.ReflectVertical();

        Assert.Multiple(() =>
        {
            Assert.That(horizontal.CopyModules(), Is.EqualTo(new bool[] { false, false, true, true, true, false }));
            Assert.That(vertical.CopyModules(), Is.EqualTo(new bool[] { false, true, true, true, false, false }));
        });
    }

    [Test]
    public void MatrixAddsLightAndDarkBordersWithoutChangingInteriorModules()
    {
        ModuleMatrix matrix = new ModuleMatrix(2, 1);
        matrix.LoadModules(new bool[] { true, false });

        ModuleMatrix light = matrix.AddBorder(1, 1, 2, 1, false);
        ModuleMatrix dark = matrix.AddBorder(1, 1, 2, 1, true);

        Assert.Multiple(() =>
        {
            Assert.That(light.Width, Is.EqualTo(5));
            Assert.That(light.Height, Is.EqualTo(3));
            Assert.That(light.CopyModules(), Is.EqualTo(new bool[]
            {
                false, false, false, false, false,
                false, true, false, false, false,
                false, false, false, false, false
            }));
            Assert.That(dark.CopyModules(), Is.EqualTo(new bool[]
            {
                true, true, true, true, true,
                true, true, false, true, true,
                true, true, true, true, true
            }));
        });
    }

    [Test]
    public void MatrixReportsDarkBoundsOrNullForEmptyMatrix()
    {
        ModuleMatrix matrix = new ModuleMatrix(5, 4);
        Assert.That(matrix.GetDarkBounds(), Is.Null);

        matrix.SetModule(1, 1, true);
        matrix.SetModule(3, 2, true);

        Assert.That(matrix.GetDarkBounds(), Is.EqualTo(new int[] { 1, 1, 3, 2 }));
    }

    [Test]
    public void MatrixRejectsInvalidDimensionsCoordinatesSourcesAndTransforms()
    {
        ModuleMatrix matrix = new ModuleMatrix(3, 2);

        Assert.Multiple(() =>
        {
            Assert.That(() => new ModuleMatrix(0, 1), Throws.TypeOf<BarcodeValidationException>());
            Assert.That(() => new ModuleMatrix(50000, 50000), Throws.TypeOf<BarcodeValidationException>());
            Assert.That(() => matrix.GetModule(-1, 0), Throws.TypeOf<BarcodeValidationException>());
            Assert.That(() => matrix.GetModule(3, 0), Throws.TypeOf<BarcodeValidationException>());
            Assert.That(() => matrix.SetModule(0, 2, true), Throws.TypeOf<BarcodeValidationException>());
            Assert.That(() => matrix.LoadModules(null!), Throws.TypeOf<BarcodeValidationException>().With.Message.EqualTo("Module source must not be null."));
            Assert.That(() => matrix.LoadModules(new bool[5]), Throws.TypeOf<BarcodeValidationException>().With.Message.EqualTo("Module source length does not match the matrix dimensions."));
            Assert.That(() => matrix.CopyRow(2), Throws.TypeOf<BarcodeValidationException>());
            Assert.That(() => matrix.SetRow(0, null!), Throws.TypeOf<BarcodeValidationException>());
            Assert.That(() => matrix.SetRow(0, new bool[2]), Throws.TypeOf<BarcodeValidationException>());
            Assert.That(() => matrix.CopyColumn(3), Throws.TypeOf<BarcodeValidationException>());
            Assert.That(() => matrix.SetColumn(0, null!), Throws.TypeOf<BarcodeValidationException>().With.Message.EqualTo("Column source must not be null."));
            Assert.That(() => matrix.SetColumn(0, new bool[1]), Throws.TypeOf<BarcodeValidationException>().With.Message.EqualTo("Column source length does not match the matrix height."));
            Assert.That(() => matrix.Crop(0, 0, 0, 1), Throws.TypeOf<BarcodeValidationException>());
            Assert.That(() => matrix.Crop(2, 0, 2, 1), Throws.TypeOf<BarcodeValidationException>());
            Assert.That(() => matrix.AddBorder(-1, 0, 0, 0, false), Throws.TypeOf<BarcodeValidationException>().With.Message.EqualTo("Border dimensions must not be negative."));
            Assert.That(() => matrix.AddBorder(int.MaxValue, 0, 0, 0, false), Throws.TypeOf<BarcodeValidationException>());
        });
    }

    private static ModuleMatrix CreatePatternMatrix()
    {
        ModuleMatrix matrix = new ModuleMatrix(3, 2);
        matrix.LoadModules(new bool[] { true, false, false, false, true, true });
        return matrix;
    }
}
