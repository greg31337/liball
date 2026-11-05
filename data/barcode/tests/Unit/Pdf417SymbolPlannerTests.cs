namespace barcode.Tests;

public class Pdf417SymbolPlannerTests
{
    [Test]
    public void AutomaticStandardDimensionsCreateLengthDescriptorAndPadding()
    {
        int[] highLevel = new int[] { 453, 178, 121, 239 };

        Pdf417SymbolPlan plan = Pdf417SymbolPlanner.CreateSymbolPlan(
            highLevel,
            null,
            null);

        Assert.Multiple(() =>
        {
            Assert.That(plan.Variant, Is.EqualTo(Pdf417Variant.Standard));
            Assert.That(plan.Columns, Is.EqualTo(2));
            Assert.That(plan.Rows, Is.EqualTo(7));
            Assert.That(plan.ErrorCorrectionLevel, Is.EqualTo(2));
            Assert.That(plan.ErrorCorrectionCodewordCount, Is.EqualTo(8));
            Assert.That(plan.LengthDescriptor, Is.EqualTo(6));
            Assert.That(plan.PaddingCodewordCount, Is.EqualTo(1));
            Assert.That(plan.TotalCodewordCount, Is.EqualTo(14));
            Assert.That(plan.RowWidthModules, Is.EqualTo(103));
            Assert.That(plan.NominalAspectRatio, Is.EqualTo(2.6265).Within(0.0000001));
            Assert.That(plan.TargetAspectRatio, Is.EqualTo(3.0));
            Assert.That(plan.DataCodewords, Is.EqualTo(new int[]
            {
                6, 453, 178, 121, 239, 900
            }));
        });
    }

    [Test]
    public void CompactAndCustomAspectRatiosSelectDifferentDeterministicGrids()
    {
        int[] highLevel = new int[] { 453, 178, 121, 239 };
        Pdf417Options compactOptions = new Pdf417Options();
        compactOptions.ErrorCorrectionLevel = -1;
        compactOptions.Variant = Pdf417Variant.Compact;
        BarcodeSizing wideSizing = new BarcodeSizing();
        wideSizing.TargetAspectRatio = 10.0;

        Pdf417SymbolPlan compact = Pdf417SymbolPlanner.CreateSymbolPlan(
            highLevel,
            compactOptions,
            null);
        Pdf417SymbolPlan wide = Pdf417SymbolPlanner.CreateSymbolPlan(
            highLevel,
            null,
            wideSizing);

        Assert.Multiple(() =>
        {
            Assert.That(compact.Columns, Is.EqualTo(3));
            Assert.That(compact.Rows, Is.EqualTo(5));
            Assert.That(compact.RowWidthModules, Is.EqualTo(86));
            Assert.That(compact.PaddingCodewordCount, Is.EqualTo(2));
            Assert.That(compact.NominalAspectRatio, Is.EqualTo(3.0702).Within(0.0000001));
            Assert.That(wide.Columns, Is.EqualTo(6));
            Assert.That(wide.Rows, Is.EqualTo(3));
            Assert.That(wide.RowWidthModules, Is.EqualTo(171));
            Assert.That(wide.TargetAspectRatio, Is.EqualTo(10.0));
            Assert.That(wide.NominalAspectRatio, Is.EqualTo(10.1745).Within(0.0000001));
        });
    }

    [Test]
    public void ExactRowsAndColumnsAreHonoredWithoutChangingCapacityAccounting()
    {
        Pdf417Options options = new Pdf417Options();
        options.Columns = 5;
        options.Rows = 4;
        options.ErrorCorrectionLevel = 1;
        int[] highLevel = new int[] { 10, 20, 30, 40 };

        Pdf417SymbolPlan plan = Pdf417SymbolPlanner.CreateSymbolPlan(
            highLevel,
            options,
            null);

        Assert.Multiple(() =>
        {
            Assert.That(plan.Columns, Is.EqualTo(5));
            Assert.That(plan.Rows, Is.EqualTo(4));
            Assert.That(plan.TotalCodewordCount, Is.EqualTo(20));
            Assert.That(plan.ErrorCorrectionCodewordCount, Is.EqualTo(4));
            Assert.That(plan.LengthDescriptor, Is.EqualTo(16));
            Assert.That(plan.PaddingCodewordCount, Is.EqualTo(11));
            Assert.That(plan.DataCodewords[0], Is.EqualTo(16));
            Assert.That(plan.DataCodewords.Skip(1).Take(4), Is.EqualTo(highLevel));
            Assert.That(plan.DataCodewords.Skip(5), Has.All.EqualTo(900));
        });
    }

    [Test]
    public void OneRequestedDimensionUsesTheMinimumRequiredComplement()
    {
        int[] highLevel = new int[] { 1, 2, 3, 4 };
        Pdf417Options rows = new Pdf417Options();
        rows.Rows = 5;
        rows.ErrorCorrectionLevel = -1;
        Pdf417Options columns = new Pdf417Options();
        columns.Columns = 4;
        columns.ErrorCorrectionLevel = -1;

        Pdf417SymbolPlan fixedRows = Pdf417SymbolPlanner.CreateSymbolPlan(
            highLevel,
            rows,
            null);
        Pdf417SymbolPlan fixedColumns = Pdf417SymbolPlanner.CreateSymbolPlan(
            highLevel,
            columns,
            null);

        Assert.Multiple(() =>
        {
            Assert.That(fixedRows.Rows, Is.EqualTo(5));
            Assert.That(fixedRows.Columns, Is.EqualTo(3));
            Assert.That(fixedRows.TotalCodewordCount, Is.EqualTo(15));
            Assert.That(fixedColumns.Columns, Is.EqualTo(4));
            Assert.That(fixedColumns.Rows, Is.EqualTo(4));
            Assert.That(fixedColumns.TotalCodewordCount, Is.EqualTo(16));
        });
    }

    [TestCase(1, 2)]
    [TestCase(40, 2)]
    [TestCase(41, 3)]
    [TestCase(160, 3)]
    [TestCase(161, 4)]
    [TestCase(320, 4)]
    [TestCase(321, 5)]
    [TestCase(863, 5)]
    [TestCase(864, 6)]
    public void AutomaticErrorCorrectionUsesRecommendedCodewordBoundaries(
        int highLevelCount,
        int expectedLevel)
    {
        Assert.That(
            Pdf417SymbolPlanner.SelectErrorCorrectionLevel(highLevelCount, -1),
            Is.EqualTo(expectedLevel));
    }

    [TestCase(0, 2)]
    [TestCase(1, 4)]
    [TestCase(2, 8)]
    [TestCase(3, 16)]
    [TestCase(4, 32)]
    [TestCase(5, 64)]
    [TestCase(6, 128)]
    [TestCase(7, 256)]
    [TestCase(8, 512)]
    public void EveryForcedErrorCorrectionLevelReservesItsExactPowerOfTwo(
        int level,
        int expectedCount)
    {
        Pdf417Options options = new Pdf417Options();
        options.ErrorCorrectionLevel = level;

        Pdf417SymbolPlan plan = Pdf417SymbolPlanner.CreateSymbolPlan(
            new int[] { 1 },
            options,
            null);

        Assert.Multiple(() =>
        {
            Assert.That(plan.ErrorCorrectionLevel, Is.EqualTo(level));
            Assert.That(plan.ErrorCorrectionCodewordCount, Is.EqualTo(expectedCount));
            Assert.That(plan.DataCodewords.Length + expectedCount, Is.EqualTo(plan.TotalCodewordCount));
            Assert.That(plan.TotalCodewordCount, Is.LessThanOrEqualTo(928));
        });
    }

    [Test]
    public void MaximumLevelZeroPayloadUsesExactlyNineHundredTwentyEightCodewords()
    {
        int[] highLevel = CreateCodewords(925);
        Pdf417Options options = new Pdf417Options();
        options.ErrorCorrectionLevel = 0;

        Pdf417SymbolPlan plan = Pdf417SymbolPlanner.CreateSymbolPlan(
            highLevel,
            options,
            null);

        Assert.Multiple(() =>
        {
            Assert.That(plan.Columns, Is.EqualTo(29));
            Assert.That(plan.Rows, Is.EqualTo(32));
            Assert.That(plan.TotalCodewordCount, Is.EqualTo(928));
            Assert.That(plan.LengthDescriptor, Is.EqualTo(926));
            Assert.That(plan.PaddingCodewordCount, Is.Zero);
            Assert.That(plan.DataCodewords[0], Is.EqualTo(926));
            Assert.That(plan.DataCodewords.Skip(1), Is.EqualTo(highLevel));
        });
    }

    [Test]
    public void CompactionPlannerOutputCanBePreparedAsACompleteDataBlock()
    {
        Pdf417CompactionPlan compacted = Pdf417CompactionPlanner.Plan(
            global::System.Text.Encoding.ASCII.GetBytes("PDF417"),
            Pdf417CompactionMode.Automatic,
            Pdf417CompactionPlanner.NoEciAssignmentNumber);

        Pdf417SymbolPlan symbol = Pdf417SymbolPlanner.CreateSymbolPlan(
            compacted.Codewords,
            null,
            null);

        Assert.Multiple(() =>
        {
            Assert.That(symbol.HighLevelCodewords, Is.EqualTo(compacted.Codewords));
            Assert.That(symbol.DataCodewords[0], Is.EqualTo(symbol.DataCodewords.Length));
            Assert.That(
                symbol.DataCodewords.Length + symbol.ErrorCorrectionCodewordCount,
                Is.EqualTo(symbol.Rows * symbol.Columns));
        });
    }

    [Test]
    public void PlansOwnHighLevelAndDataCodewordBuffers()
    {
        int[] source = new int[] { 1, 2, 3 };
        Pdf417SymbolPlan plan = Pdf417SymbolPlanner.CreateSymbolPlan(
            source,
            null,
            null);
        int expectedData = plan.DataCodewords[1];
        source[0] = 500;
        plan.HighLevelCodewords[0] = 600;

        Assert.Multiple(() =>
        {
            Assert.That(plan.DataCodewords[1], Is.EqualTo(expectedData));
            Assert.That(plan.HighLevelCodewords[0], Is.EqualTo(600));
            Assert.That(plan.DataCodewords, Is.Not.SameAs(plan.HighLevelCodewords));
        });
    }

    [Test]
    public void CapacityDimensionAndValueViolationsAreRejected()
    {
        Pdf417Options tooSmall = new Pdf417Options();
        tooSmall.Columns = 2;
        tooSmall.Rows = 3;
        tooSmall.ErrorCorrectionLevel = 2;
        Pdf417Options tooMuchEcc = new Pdf417Options();
        tooMuchEcc.ErrorCorrectionLevel = 1;
        Pdf417Options tooManyCells = new Pdf417Options();
        tooManyCells.Columns = 30;
        tooManyCells.Rows = 90;
        Pdf417Options invalidColumns = new Pdf417Options();
        invalidColumns.Columns = 31;
        Pdf417Options invalidRows = new Pdf417Options();
        invalidRows.Rows = 2;
        Pdf417Options invalidLevel = new Pdf417Options();
        invalidLevel.ErrorCorrectionLevel = 9;
        Pdf417Options invalidVariant = new Pdf417Options();
        invalidVariant.Variant = (Pdf417Variant)99;
        BarcodeSizing invalidAspect = new BarcodeSizing();
        invalidAspect.TargetAspectRatio = double.NaN;

        Assert.Multiple(() =>
        {
            Assert.That(
                () => Pdf417SymbolPlanner.CreateSymbolPlan(null!, null, null),
                Throws.TypeOf<BarcodeEncodingException>());
            Assert.That(
                () => Pdf417SymbolPlanner.CreateSymbolPlan(new int[0], null, null),
                Throws.TypeOf<BarcodeEncodingException>());
            Assert.That(
                () => Pdf417SymbolPlanner.CreateSymbolPlan(new int[] { -1 }, null, null),
                Throws.TypeOf<BarcodeEncodingException>());
            Assert.That(
                () => Pdf417SymbolPlanner.CreateSymbolPlan(new int[] { 929 }, null, null),
                Throws.TypeOf<BarcodeEncodingException>());
            Assert.That(
                () => Pdf417SymbolPlanner.CreateSymbolPlan(new int[926], null, null),
                Throws.TypeOf<BarcodeEncodingException>());
            Assert.That(
                () => Pdf417SymbolPlanner.CreateSymbolPlan(CreateCodewords(10), tooSmall, null),
                Throws.TypeOf<BarcodeEncodingException>());
            Assert.That(
                () => Pdf417SymbolPlanner.CreateSymbolPlan(CreateCodewords(925), tooMuchEcc, null),
                Throws.TypeOf<BarcodeEncodingException>());
            Assert.That(
                () => Pdf417SymbolPlanner.CreateSymbolPlan(CreateCodewords(864), null, null),
                Throws.TypeOf<BarcodeEncodingException>());
            Assert.That(
                () => Pdf417SymbolPlanner.CreateSymbolPlan(new int[] { 1 }, tooManyCells, null),
                Throws.TypeOf<BarcodeEncodingException>());
            Assert.That(
                () => Pdf417SymbolPlanner.CreateSymbolPlan(new int[] { 1 }, invalidColumns, null),
                Throws.TypeOf<BarcodeEncodingException>());
            Assert.That(
                () => Pdf417SymbolPlanner.CreateSymbolPlan(new int[] { 1 }, invalidRows, null),
                Throws.TypeOf<BarcodeEncodingException>());
            Assert.That(
                () => Pdf417SymbolPlanner.CreateSymbolPlan(new int[] { 1 }, invalidLevel, null),
                Throws.TypeOf<BarcodeEncodingException>());
            Assert.That(
                () => Pdf417SymbolPlanner.CreateSymbolPlan(new int[] { 1 }, invalidVariant, null),
                Throws.TypeOf<BarcodeEncodingException>());
            Assert.That(
                () => Pdf417SymbolPlanner.CreateSymbolPlan(new int[] { 1 }, null, invalidAspect),
                Throws.TypeOf<BarcodeEncodingException>());
            Assert.That(
                () => Pdf417SymbolPlanner.CalculateRowWidthModules(0, Pdf417Variant.Standard),
                Throws.TypeOf<BarcodeValidationException>());
        });
    }

    static int[] CreateCodewords(int count)
    {
        int[] codewords = new int[count];
        int index = 0;
        while (index < count)
        {
            codewords[index] = index % 929;
            index++;
        }

        return codewords;
    }
}
