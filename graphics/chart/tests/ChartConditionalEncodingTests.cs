namespace chart.Tests;

[TestFixture]
public class ChartConditionalEncodingTests
{
    [Test]
    public void Selection_NormalizesRowsAndReportsCombinedState()
    {
        ChartSelection selection = new ChartSelection("primary", "linked");
        selection.SetSelectedRows(new int[] { 3, 1, 3 });
        selection.SetHighlightedRows(new int[] { 2, 3, 2 });

        Assert.That(selection.CopySelectedRows(), Is.EqualTo(new int[] { 1, 3 }));
        Assert.That(selection.CopyHighlightedRows(), Is.EqualTo(new int[] { 2, 3 }));
        Assert.That(selection.StateForRow(0), Is.EqualTo(ChartSelectionState.Normal));
        Assert.That(selection.StateForRow(1), Is.EqualTo(ChartSelectionState.Selected));
        Assert.That(selection.StateForRow(2), Is.EqualTo(ChartSelectionState.Highlighted));
        Assert.That(selection.StateForRow(3), Is.EqualTo(ChartSelectionState.SelectedHighlighted));

        ChartSelection copy = selection.Copy();
        selection.Clear();
        Assert.That(copy.CopySelectedRows(), Is.EqualTo(new int[] { 1, 3 }));
        Assert.That(selection.CopySelectedRows(), Is.Empty);
    }

    [Test]
    public void ConditionalEncodings_ChangeSelectedAndHighlightedMarkerOutput()
    {
        ChartEncodingBindingFactory bindings = new ChartEncodingBindingFactory();
        ChartSelection selection = new ChartSelection("points-selection", "linked-points");
        selection.SetSelectedRows(new int[] { 1, 2 });
        selection.SetHighlightedRows(new int[] { 2 });
        ChartVisualEncoding encoding = XYEncoding();
        encoding.SetSelection(selection);
        encoding.SetConditionalEncoding(ColorCondition(bindings));
        encoding.SetConditionalEncoding(NumberCondition(bindings, ChartEncodingChannel.Opacity, 0.2, 0.8, 1.0));
        encoding.SetConditionalEncoding(NumberCondition(bindings, ChartEncodingChannel.Size, 4.0, 6.0, 10.0));
        encoding.SetConditionalEncoding(ShapeCondition(bindings));
        ChartDataTable table = PointTable();

        ChartCartesianSeries series = new ChartDeclarativeCharts().CreateScatterSeries("points", table, encoding, ChartRenderStyle.CreateFill(ChartColor.Black()));

        AssertMarker(series.CommandAt(0), 180, 180, 180, 0.2, 4.0, ChartMarkerShape.Circle);
        AssertMarker(series.CommandAt(1), 220, 20, 20, 0.8, 6.0, ChartMarkerShape.Square);
        AssertMarker(series.CommandAt(2), 20, 40, 220, 1.0, 10.0, ChartMarkerShape.Diamond);

        selection.SetHighlightedRows(new int[] { 0 });
        ChartCartesianSeries refreshed = new ChartDeclarativeCharts().CreateScatterSeries("points-new", table, encoding, ChartRenderStyle.CreateFill(ChartColor.Black()));
        AssertMarker(refreshed.CommandAt(0), 20, 40, 220, 1.0, 10.0, ChartMarkerShape.Diamond);
        AssertMarker(refreshed.CommandAt(2), 220, 20, 20, 0.8, 6.0, ChartMarkerShape.Square);
    }

    [Test]
    public void SelectionSidecar_ProducesSortedSnapshotMetadataAndSvgOutput()
    {
        ChartSelection beta = new ChartSelection("beta", "shared");
        beta.SetSelectedRows(new int[] { 4 });
        beta.SetHighlightedRows(new int[] { 5, 0 });
        ChartSelection alpha = new ChartSelection("alpha", "shared");
        alpha.SetSelectedRows(new int[] { 3, 1 });
        alpha.SetHighlightedRows(new int[] { 2 });
        ChartSelectionSidecar sidecar = new ChartSelectionSidecar();
        sidecar.AddSelection(beta);
        sidecar.AddSelection(alpha);
        beta.Clear();

        string expected = "linked-selection alpha group shared: selected=1,3; highlighted=2\nlinked-selection beta group shared: selected=4; highlighted=0,5";
        Assert.That(sidecar.MetadataText(), Is.EqualTo(expected));
        Assert.That(sidecar.SelectionAt(0).GetSelectionId(), Is.EqualTo("beta"));

        ChartAccessibilityMetadata metadata = new ChartAccessibilityMetadata();
        metadata.SetSelectionSidecar(sidecar);
        string svg = new ChartExport().ToSvg(new ChartRenderScene(20, 20), metadata);

        Assert.That(metadata.HasMetadata(), Is.True);
        Assert.That(metadata.MetadataText(), Is.EqualTo(expected));
        Assert.That(svg, Does.Contain("linked-selection alpha group shared"));
        Assert.That(metadata.Copy().GetSelectionSidecar()!.MetadataText(), Is.EqualTo(expected));
    }

    [Test]
    public void ConditionalSelectionModels_ValidateInputs()
    {
        Assert.Throws<System.ArgumentOutOfRangeException>(() => new ChartSelection("selection", "group").SetSelectedRows(new int[] { -1 }));
        Assert.Throws<System.ArgumentNullException>(() => new ChartSelection("selection", "group").SetHighlightedRows(null!));
        Assert.Throws<System.ArgumentOutOfRangeException>(() => new ChartSelection("selection", "group").StateForRow(-1));
        Assert.Throws<System.ArgumentOutOfRangeException>(() => ChartConditionalEncoding.ValidateState((ChartSelectionState)99));

        ChartConditionalEncoding condition = new ChartConditionalEncoding(ChartEncodingChannel.FillColor);
        ChartEncodingBinding wrong = new ChartEncodingBindingFactory().CreateColorConstant(ChartEncodingChannel.StrokeColor, ChartColor.Black());
        Assert.Throws<System.ArgumentException>(() => condition.SetSelectedBinding(wrong));
        Assert.Throws<System.ArgumentNullException>(() => condition.SetHighlightedBinding(null!));

        ChartSelection duplicate = new ChartSelection("duplicate", "group");
        ChartSelectionSidecar sidecar = new ChartSelectionSidecar();
        sidecar.AddSelection(duplicate);
        Assert.Throws<System.ArgumentException>(() => sidecar.AddSelection(duplicate));
        Assert.Throws<System.ArgumentOutOfRangeException>(() => sidecar.SelectionAt(1));
        Assert.Throws<System.ArgumentNullException>(() => new ChartAccessibilityMetadata().SetSelectionSidecar(null!));
        Assert.Throws<System.ArgumentNullException>(() => new ChartVisualEncoding().SetSelection(null!));

        ChartVisualEncoding incomplete = XYEncoding();
        incomplete.SetSelection(new ChartSelection("incomplete", "group"));
        ChartConditionalEncoding selectedOnly = new ChartConditionalEncoding(ChartEncodingChannel.FillColor);
        selectedOnly.SetSelectedBinding(new ChartEncodingBindingFactory().CreateColorConstant(ChartEncodingChannel.FillColor, ChartColor.Black()));
        incomplete.SetConditionalEncoding(selectedOnly);
        Assert.Throws<System.ArgumentException>(() => new ChartDeclarativeCharts().CreateScatterSeries("incomplete", PointTable(), incomplete, ChartRenderStyle.CreateFill(ChartColor.White())));
    }

    internal static ChartVisualEncoding XYEncoding()
    {
        ChartVisualEncoding encoding = new ChartVisualEncoding();
        encoding.SetX("x", ChartDataKind.Number);
        encoding.SetY("y", ChartDataKind.Number);
        return encoding;
    }

    internal static ChartDataTable PointTable()
    {
        ChartDataTable table = new ChartDataTable();
        table.AddNumberColumn("x", new double[] { 1.0, 2.0, 3.0 });
        table.AddNumberColumn("y", new double[] { 4.0, 5.0, 6.0 });
        return table;
    }

    internal static ChartConditionalEncoding ColorCondition(ChartEncodingBindingFactory bindings)
    {
        ChartConditionalEncoding condition = new ChartConditionalEncoding(ChartEncodingChannel.FillColor);
        condition.SetUnselectedBinding(bindings.CreateColorConstant(ChartEncodingChannel.FillColor, ChartColor.FromRgb(180, 180, 180)));
        condition.SetSelectedBinding(bindings.CreateColorConstant(ChartEncodingChannel.FillColor, ChartColor.FromRgb(220, 20, 20)));
        condition.SetHighlightedBinding(bindings.CreateColorConstant(ChartEncodingChannel.FillColor, ChartColor.FromRgb(20, 40, 220)));
        return condition;
    }

    internal static ChartConditionalEncoding NumberCondition(ChartEncodingBindingFactory bindings, ChartEncodingChannel channel, double normal, double selected, double highlighted)
    {
        ChartConditionalEncoding condition = new ChartConditionalEncoding(channel);
        condition.SetUnselectedBinding(bindings.CreateNumberConstant(channel, normal));
        condition.SetSelectedBinding(bindings.CreateNumberConstant(channel, selected));
        condition.SetHighlightedBinding(bindings.CreateNumberConstant(channel, highlighted));
        return condition;
    }

    internal static ChartConditionalEncoding ShapeCondition(ChartEncodingBindingFactory bindings)
    {
        ChartConditionalEncoding condition = new ChartConditionalEncoding(ChartEncodingChannel.Shape);
        condition.SetUnselectedBinding(bindings.CreateShapeConstant(ChartEncodingChannel.Shape, ChartMarkerShape.Circle));
        condition.SetSelectedBinding(bindings.CreateShapeConstant(ChartEncodingChannel.Shape, ChartMarkerShape.Square));
        condition.SetHighlightedBinding(bindings.CreateShapeConstant(ChartEncodingChannel.Shape, ChartMarkerShape.Diamond));
        return condition;
    }

    internal static void AssertMarker(ChartRenderCommand command, int red, int green, int blue, double opacity, double size, ChartMarkerShape shape)
    {
        Assert.That(command.GetStyle().GetFillColor().GetR(), Is.EqualTo(red));
        Assert.That(command.GetStyle().GetFillColor().GetG(), Is.EqualTo(green));
        Assert.That(command.GetStyle().GetFillColor().GetB(), Is.EqualTo(blue));
        Assert.That(command.GetStyle().GetOpacity(), Is.EqualTo(opacity));
        Assert.That(command.GetMarkerSize(), Is.EqualTo(size));
        Assert.That(command.GetMarkerShape(), Is.EqualTo(shape));
    }
}
