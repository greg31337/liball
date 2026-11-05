namespace chart.Tests;

[TestFixture]
public class ChartDocumentTests
{
    [Test]
    public void ChartDocument_InitializesFigureMarginsTextGridAndBackground()
    {
        ChartDocument document = new ChartDocument(640.0, 480.0);

        Assert.That(document.Width, Is.EqualTo(640.0));
        Assert.That(document.Height, Is.EqualTo(480.0));
        Assert.That(document.HasBackground, Is.True);
        Assert.That(document.BackgroundColor.EqualsColor(ChartColor.White()), Is.True);
        Assert.That(document.Margins.Left, Is.EqualTo(48.0));
        Assert.That(document.Title.Visible, Is.False);
        Assert.That(document.Grid.Rows, Is.EqualTo(1));
        Assert.That(document.Grid.Columns, Is.EqualTo(1));

        ChartRect figure = document.FigureRect();
        ChartRect content = document.ContentRect();

        Assert.That(figure.Width, Is.EqualTo(640.0));
        Assert.That(figure.Height, Is.EqualTo(480.0));
        Assert.That(content.X, Is.EqualTo(48.0));
        Assert.That(content.Y, Is.EqualTo(48.0));
        Assert.That(content.Width, Is.EqualTo(544.0));
        Assert.That(content.Height, Is.EqualTo(384.0));
    }

    [Test]
    public void ChartDocument_SetsTitleSubtitleCaptionMarginsGridAndBackground()
    {
        ChartDocument document = new ChartDocument(400.0, 300.0);
        ChartGrid grid = new ChartGrid(2, 3);
        ChartTextBlock text = ChartTextBlock.Create("Factory text");

        grid.SetGaps(10.0, 20.0);
        document.SetMargins(new ChartPadding(10.0, 20.0, 30.0, 40.0));
        document.SetBackground(ChartColor.Transparent());
        document.SetTitle("Revenue");
        document.SetSubtitle("Quarterly");
        document.SetCaption("Source");
        document.Title.SetStyle(ChartColor.FromRgb(10, 20, 30), "Inter", 18);
        document.Title.SetSpacing(12.0);
        document.SetGrid(grid);
        grid.SetGaps(99.0, 99.0);

        Assert.That(document.HasBackground, Is.False);
        Assert.That(document.Title.Text, Is.EqualTo("Revenue"));
        Assert.That(document.Title.Visible, Is.True);
        Assert.That(document.Title.Color.ToHexRgb(), Is.EqualTo("#0A141E"));
        Assert.That(document.Title.FontFamily, Is.EqualTo("Inter"));
        Assert.That(document.Title.FontSize, Is.EqualTo(18));
        Assert.That(document.Title.Spacing, Is.EqualTo(12.0));
        Assert.That(document.Subtitle.Text, Is.EqualTo("Quarterly"));
        Assert.That(document.Caption.Text, Is.EqualTo("Source"));
        Assert.That(document.Margins.Left, Is.EqualTo(10.0));
        Assert.That(document.Margins.Bottom, Is.EqualTo(40.0));
        Assert.That(document.Grid.Rows, Is.EqualTo(2));
        Assert.That(document.Grid.Columns, Is.EqualTo(3));
        Assert.That(document.Grid.RowGap, Is.EqualTo(10.0));
        Assert.That(document.Grid.ColumnGap, Is.EqualTo(20.0));
        Assert.That(text.Text, Is.EqualTo("Factory text"));
        Assert.That(text.Visible, Is.True);
        Assert.That(text.FontFamily, Is.EqualTo("Arial"));
    }

    [Test]
    public void ChartGrid_ComputesCellRectangles()
    {
        ChartGrid grid = new ChartGrid(2, 3);
        ChartRect area = new ChartRect(10.0, 20.0, 320.0, 110.0);

        grid.SetGaps(10.0, 20.0);
        ChartRect cell = grid.CellRect(area, 1, 2);

        Assert.That(cell.X, Is.EqualTo(236.66666666666666).Within(0.0000001));
        Assert.That(cell.Y, Is.EqualTo(80.0));
        Assert.That(cell.Width, Is.EqualTo(280.0 / 3.0).Within(0.0000001));
        Assert.That(cell.Height, Is.EqualTo(50.0));
    }

    [Test]
    public void ChartPlot_StoresBoundsMarginsBackgroundAndAxes()
    {
        ChartPlot plot = new ChartPlot("main");
        ChartAxis axis = ChartAxis.CreateBottom(
            ChartTickGeneratorOperations.GenerateExplicitTicks(new double[] { 0.0, 10.0 }),
            new ChartRange(0.0, 10.0),
            new ChartRect(10.0, 20.0, 100.0, 60.0));

        plot.SetBounds(new ChartRect(10.0, 20.0, 200.0, 100.0));
        plot.SetMargins(new ChartPadding(5.0, 10.0, 15.0, 20.0));
        plot.SetBackground(ChartColor.FromRgb(240, 241, 242));
        ChartAxis added = plot.AddAxis(axis);
        axis.Id = "changed";

        ChartRect inner = plot.InnerBounds();

        Assert.That(plot.Id, Is.EqualTo("main"));
        Assert.That(plot.HasBackground, Is.True);
        Assert.That(plot.BackgroundColor.ToHexRgb(), Is.EqualTo("#F0F1F2"));
        Assert.That(added.Id, Is.EqualTo("axis-bottom"));
        Assert.That(plot.AxisCount, Is.EqualTo(1));
        Assert.That(plot.AxisAt(0).Id, Is.EqualTo("axis-bottom"));
        Assert.That(inner.X, Is.EqualTo(15.0));
        Assert.That(inner.Y, Is.EqualTo(30.0));
        Assert.That(inner.Width, Is.EqualTo(180.0));
        Assert.That(inner.Height, Is.EqualTo(70.0));
    }

    [Test]
    public void ChartDocument_AddsIndexesAndCopiesPlots()
    {
        ChartDocument document = new ChartDocument(400.0, 300.0);
        ChartPlot plot = new ChartPlot("main");

        plot.SetBounds(new ChartRect(10.0, 20.0, 200.0, 100.0));
        ChartPlot added = document.AddPlot(plot);
        plot.Id = "changed";
        added.SetBackground(ChartColor.FromRgb(1, 2, 3));

        ChartDocument copy = document.Copy();
        added.SetBackground(ChartColor.FromRgb(9, 9, 9));

        Assert.That(document.ContainsPlot("main"), Is.True);
        Assert.That(document.PlotIndex("main"), Is.EqualTo(0));
        Assert.That(document.PlotAt(0).Id, Is.EqualTo("main"));
        Assert.That(document.PlotAt(0).Bounds.X, Is.EqualTo(10.0));
        Assert.That(copy.PlotCount, Is.EqualTo(1));
        Assert.That(copy.PlotAt(0).BackgroundColor.ToHexRgb(), Is.EqualTo("#010203"));
    }

    [Test]
    public void ChartDocumentModel_ValidatesInputs()
    {
        ChartDocument document = new ChartDocument(400.0, 300.0);
        ChartPlot plot = new ChartPlot("main");
        ChartGrid grid = new ChartGrid(1, 1);

        Assert.Throws<System.ArgumentOutOfRangeException>(() => new ChartDocument(0.0, 300.0));
        Assert.Throws<System.ArgumentOutOfRangeException>(() => document.SetSize(400.0, 0.0));
        Assert.Throws<System.ArgumentNullException>(() => document.SetMargins(null!));
        Assert.Throws<System.ArgumentOutOfRangeException>(() => document.SetMargins(new ChartPadding(-1.0, 0.0, 0.0, 0.0)));
        Assert.Throws<System.ArgumentNullException>(() => document.SetBackground(null!));
        Assert.Throws<System.ArgumentNullException>(() => document.SetTitle(null!));
        Assert.Throws<System.ArgumentNullException>(() => document.SetSubtitle(null!));
        Assert.Throws<System.ArgumentNullException>(() => document.SetCaption(null!));
        Assert.Throws<System.ArgumentNullException>(() => document.SetGrid(null!));
        Assert.Throws<System.ArgumentNullException>(() => document.AddPlot((ChartPlot)null!));
        document.AddPlot(plot);
        Assert.Throws<System.ArgumentException>(() => document.AddPlot(new ChartPlot("main")));
        Assert.Throws<System.ArgumentOutOfRangeException>(() => document.PlotAt(1));
        Assert.Throws<System.ArgumentNullException>(() => document.PlotIndex(null!));
        Assert.Throws<System.ArgumentException>(() => document.PlotIndex(""));

        Assert.Throws<System.ArgumentNullException>(() => ChartTextBlock.Create(null!));
        Assert.Throws<System.ArgumentNullException>(() => new ChartTextBlock().SetStyle(null!, "Arial", 10));
        Assert.Throws<System.ArgumentNullException>(() => new ChartTextBlock().SetStyle(ChartColor.Black(), null!, 10));
        Assert.Throws<System.ArgumentOutOfRangeException>(() => new ChartTextBlock().SetStyle(ChartColor.Black(), "Arial", 0));
        Assert.Throws<System.ArgumentOutOfRangeException>(() => new ChartTextBlock().SetSpacing(-1.0));

        Assert.Throws<System.ArgumentOutOfRangeException>(() => new ChartGrid(0, 1));
        Assert.Throws<System.ArgumentOutOfRangeException>(() => grid.SetGaps(-1.0, 0.0));
        Assert.Throws<System.ArgumentNullException>(() => grid.CellRect(null!, 0, 0));
        Assert.Throws<System.ArgumentOutOfRangeException>(() => grid.CellRect(new ChartRect(0.0, 0.0, 10.0, 10.0), 1, 0));
        Assert.Throws<System.ArgumentOutOfRangeException>(() => grid.CellRect(new ChartRect(0.0, 0.0, 10.0, 10.0), 0, 1));

        Assert.Throws<System.ArgumentNullException>(() => new ChartPlot(null!));
        Assert.Throws<System.ArgumentException>(() => new ChartPlot(""));
        Assert.Throws<System.ArgumentNullException>(() => plot.SetBounds(null!));
        Assert.Throws<System.ArgumentException>(() => plot.SetBounds(new ChartRect(0.0, 0.0, 0.0, 1.0)));
        Assert.Throws<System.ArgumentNullException>(() => plot.SetMargins(null!));
        Assert.Throws<System.ArgumentOutOfRangeException>(() => plot.SetMargins(new ChartPadding(0.0, -1.0, 0.0, 0.0)));
        Assert.Throws<System.ArgumentNullException>(() => plot.SetBackground(null!));
        Assert.Throws<System.ArgumentNullException>(() => plot.AddAxis(null!));
        Assert.Throws<System.ArgumentOutOfRangeException>(() => plot.AxisAt(0));
    }
}
