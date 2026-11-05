namespace chart.Tests;

[TestFixture]
public class ChartFoundationalDataTests
{
    [Test]
    public void Angle_ConvertsNormalizesCopiesAndValidates()
    {
        ChartAngle angle = new ChartAngle(-450.0);
        IChartAngle contract = angle;

        Assert.That(contract.ToRadians(), Is.EqualTo(-2.5 * System.Math.PI).Within(0.0000001));
        Assert.That(contract.Normalize().GetDegrees(), Is.EqualTo(270.0));

        ChartAngle copy = contract.Copy();
        copy.SetDegrees(90.0);

        Assert.That(angle.GetDegrees(), Is.EqualTo(-450.0));
        Assert.That(copy.ToRadians(), Is.EqualTo(System.Math.PI * 0.5).Within(0.0000001));
        Assert.Throws<System.ArgumentOutOfRangeException>(() => new ChartAngle(double.NaN));
        Assert.Throws<System.ArgumentOutOfRangeException>(() => angle.SetDegrees(double.PositiveInfinity));
    }

    [Test]
    public void NumericInterval_HandlesEndpointInclusionAndLength()
    {
        ChartNumericInterval interval = new ChartNumericInterval(1.0, 3.0, false, true);
        IChartNumericInterval contract = interval;

        Assert.That(contract.Length(), Is.EqualTo(2.0));
        Assert.That(contract.Contains(1.0), Is.False);
        Assert.That(contract.Contains(2.0), Is.True);
        Assert.That(contract.Contains(3.0), Is.True);
        Assert.That(contract.IsEmpty(), Is.False);

        contract.SetBounds(2.0, 2.0, true, false);

        Assert.That(contract.IsEmpty(), Is.True);
        Assert.That(contract.Contains(2.0), Is.False);
    }

    [Test]
    public void NumericInterval_IntersectsOnlyWhenSharedBoundaryIsIncluded()
    {
        ChartNumericInterval leftClosed = new ChartNumericInterval(0.0, 1.0, true, true);
        ChartNumericInterval rightClosed = new ChartNumericInterval(1.0, 2.0, true, true);
        ChartNumericInterval rightOpen = new ChartNumericInterval(1.0, 2.0, false, true);

        Assert.That(leftClosed.Intersects(rightClosed), Is.True);
        Assert.That(leftClosed.Intersects(rightOpen), Is.False);
        Assert.That(rightClosed.Intersects(new ChartNumericInterval(1.5, 3.0, true, true)), Is.True);

        ChartNumericInterval copy = leftClosed.Copy();
        copy.SetBounds(5.0, 6.0, true, true);

        Assert.That(leftClosed.GetMinimum(), Is.EqualTo(0.0));
        Assert.That(copy.GetMaximum(), Is.EqualTo(6.0));
        Assert.Throws<System.ArgumentException>(() => new ChartNumericInterval(2.0, 1.0, true, true));
        Assert.Throws<System.ArgumentNullException>(() => leftClosed.Intersects(null!));
        Assert.Throws<System.ArgumentOutOfRangeException>(() => leftClosed.Contains(double.NaN));
    }

    [Test]
    public void TextBlock_ReturnsPublicBuiltInMeasurement()
    {
        ChartTextBlock block = new ChartTextBlock();
        block.SetText("A\nABC");

        IChartTextMeasurement measurement = block.MeasureBuiltIn(2);

        Assert.That(measurement.GetWidth(), Is.EqualTo(34));
        Assert.That(measurement.GetHeight(), Is.EqualTo(30));
        Assert.That(measurement.GetLineCount(), Is.EqualTo(2));
        Assert.That(measurement.GetLineHeight(), Is.EqualTo(14));
        Assert.That(measurement.GetBaseline(), Is.EqualTo(14));
        Assert.Throws<System.ArgumentOutOfRangeException>(() => block.MeasureBuiltIn(0));
        Assert.Throws<System.ArgumentOutOfRangeException>(() => new ChartTextMeasurement(-1, 1, 1, 1, 1));
    }

    [Test]
    public void ExportScaleSettings_CombineScaleRoundPixelsAndScaleLogicalSize()
    {
        ChartExportScaleSettings settings = new ChartExportScaleSettings(1.5, 2.0);
        IChartExportScaleSettings contract = settings;

        Assert.That(contract.EffectiveScale(), Is.EqualTo(3.0));
        Assert.That(contract.ScaleWidth(11), Is.EqualTo(33));
        Assert.That(contract.ScaleHeight(7), Is.EqualTo(21));

        ChartSize scaled = contract.ScaleSize(new ChartSize(10.0, 4.0));
        Assert.That(scaled.Width, Is.EqualTo(30.0));
        Assert.That(scaled.Height, Is.EqualTo(12.0));

        ChartExportScaleSettings copy = contract.Copy();
        copy.SetScales(2.0, 1.0);

        Assert.That(settings.GetDevicePixelRatio(), Is.EqualTo(1.5));
        Assert.That(copy.GetExportScale(), Is.EqualTo(1.0));
    }

    [Test]
    public void ExportScaleSettings_ValidateScalesSizesAndOverflow()
    {
        Assert.Throws<System.ArgumentOutOfRangeException>(() => new ChartExportScaleSettings(0.0, 1.0));
        Assert.Throws<System.ArgumentOutOfRangeException>(() => new ChartExportScaleSettings(1.0, double.NaN));
        Assert.Throws<System.ArgumentOutOfRangeException>(() => new ChartExportScaleSettings(double.MaxValue, 2.0));

        ChartExportScaleSettings settings = new ChartExportScaleSettings(2.0, 1.0);

        Assert.Throws<System.ArgumentOutOfRangeException>(() => settings.ScaleWidth(0));
        Assert.Throws<System.ArgumentOutOfRangeException>(() => settings.ScaleHeight(int.MaxValue));
        Assert.Throws<System.ArgumentNullException>(() => settings.ScaleSize(null!));
        Assert.Throws<System.ArgumentOutOfRangeException>(() => settings.ScaleSize(new ChartSize(0.0, 2.0)));
    }

    [Test]
    public void RawBitmap_SpansProvideZeroCopyBufferAndRowAccess()
    {
        RawBitmap bitmap = RawBitmap.CreateWithStride(2, 2, 10, ChartPixelFormat.Rgba32);

        System.Span<byte> buffer = bitmap.GetBufferSpan();
        System.Span<byte> secondRow = bitmap.GetRowSpan(1);
        buffer[0] = 11;
        secondRow[0] = 22;
        secondRow[9] = 33;

        System.ReadOnlySpan<byte> readOnly = bitmap.GetReadOnlyBufferSpan();
        Assert.That(bitmap.Buffer[0], Is.EqualTo(11));
        Assert.That(bitmap.Buffer[10], Is.EqualTo(22));
        Assert.That(bitmap.Buffer[19], Is.EqualTo(33));
        Assert.That(readOnly[10], Is.EqualTo(22));
        Assert.That(secondRow.Length, Is.EqualTo(bitmap.Stride));
        Assert.Throws<System.ArgumentOutOfRangeException>(() => bitmap.GetRowSpan(2));
    }

    [Test]
    public void NumericSpansExposeColumnWithoutCopyAndMutableMatrixRows()
    {
        ChartDataColumn column = new ChartDataColumnFactory().CreateNumber("values", new double[] { 1.0, 2.0, 3.0 });
        System.ReadOnlySpan<double> columnValues = column.GetNumberSpan();
        ChartDataMatrix matrix = new ChartDataMatrix(2, 3);

        System.Span<double> values = matrix.GetValueSpan();
        values[0] = 4.0;
        System.Span<double> secondRow = matrix.GetRowSpan(1);
        secondRow[2] = 9.0;

        Assert.That(columnValues.ToArray(), Is.EqualTo(new double[] { 1.0, 2.0, 3.0 }));
        Assert.That(matrix.GetValue(0, 0), Is.EqualTo(4.0));
        Assert.That(matrix.GetValue(1, 2), Is.EqualTo(9.0));
        Assert.That(secondRow.Length, Is.EqualTo(3));
        Assert.Throws<System.InvalidOperationException>(() => new ChartDataColumn("text", ChartDataKind.Text).GetNumberSpan());
        Assert.Throws<System.ArgumentOutOfRangeException>(() => matrix.GetRowSpan(-1));
    }
}
