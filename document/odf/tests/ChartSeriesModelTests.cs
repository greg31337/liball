using System.Text;

namespace odf.Tests;

public class ChartSeriesModelTests
{
    [TestCase("chart:bar", OdfChartKind.Bar)]
    [TestCase("chart:line", OdfChartKind.Line)]
    [TestCase("chart:area", OdfChartKind.Area)]
    [TestCase("chart:circle", OdfChartKind.Pie)]
    [TestCase("chart:ring", OdfChartKind.Ring)]
    [TestCase("chart:scatter", OdfChartKind.Scatter)]
    [TestCase("chart:radar", OdfChartKind.Radar)]
    [TestCase("chart:filled-radar", OdfChartKind.FilledRadar)]
    [TestCase("chart:stock", OdfChartKind.Stock)]
    [TestCase("chart:bubble", OdfChartKind.Bubble)]
    [TestCase("chart:surface", OdfChartKind.Surface)]
    [TestCase("chart:gantt", OdfChartKind.Gantt)]
    public void RecognizesEveryStandardSeriesClass(
        string className,
        OdfChartKind expected)
    {
        OdfDocument document = ReadBytes(
            Encoding.UTF8.GetBytes(MinimalChartXml(className)));

        Assert.That(
            document.Charts.Single().PlotArea?.Series.Single().Kind,
            Is.EqualTo(expected));
    }

    [Test]
    public void SeriesWithoutAClassUsesTheContainingChartClass()
    {
        OdfDocument document = ReadBytes(Encoding.UTF8.GetBytes(
            MinimalChartXml("")));

        Assert.That(
            document.Charts.Single().PlotArea?.Series.Single().Kind,
            Is.EqualTo(OdfChartKind.Bar));
    }

    [Test]
    public void LoadsSeriesDomainsPointsLabelsMarkersVariantsAndScene()
    {
        OdfDocument document = ReadChart();
        OdfChartPlotArea plot = document.Charts.Single().PlotArea!;
        OdfChartSeries series = plot.Series.Single();
        OdfChartStyleMetadata style = series.StyleMetadata;

        Assert.Multiple(() =>
        {
            Assert.That(plot.StyleMetadata.IsVertical, Is.True);
            Assert.That(plot.StyleMetadata.IsStacked, Is.True);
            Assert.That(plot.StyleMetadata.IsPercentage, Is.False);
            Assert.That(plot.StyleMetadata.IsThreeDimensional, Is.True);
            Assert.That(plot.StyleMetadata.IsDeep, Is.True);
            Assert.That(plot.StyleMetadata.SolidType,
                Is.EqualTo("cylinder"));
            Assert.That(series.Kind, Is.EqualTo(OdfChartKind.Line));
            Assert.That(series.ValuesCellRangeAddress,
                Is.EqualTo("Local.C2:C4"));
            Assert.That(series.LabelCellAddress, Is.EqualTo("Local.C1"));
            Assert.That(series.AttachedAxis, Is.EqualTo("primary-y"));
            Assert.That(series.Domains.Select(item => item.CellRangeAddress),
                Is.EqualTo(new[] { "Local.A2:A4", "Local.B2:B4" }));
            Assert.That(series.DataPoints, Has.Length.EqualTo(2));
            Assert.That(series.DataPoints[0].RepeatCount, Is.EqualTo(2));
            Assert.That(series.DataPoints[0].IsRepeatCountValid, Is.True);
            Assert.That(series.DataPoints[0].Label?.Text,
                Is.EqualTo("Peak"));
            Assert.That(series.DataPoints[1].StyleMetadata.PieOffset,
                Is.EqualTo("15"));
            Assert.That(series.Label?.Text, Is.EqualTo("Series label"));
            Assert.That(style.SymbolType, Is.EqualTo("named-symbol"));
            Assert.That(style.SymbolName, Is.EqualTo("diamond"));
            Assert.That(style.SymbolWidth, Is.EqualTo("0.3cm"));
            Assert.That(style.SymbolImageHref,
                Is.EqualTo("Pictures/marker.png"));
            Assert.That(style.Interpolation, Is.EqualTo("cubic-spline"));
            Assert.That(style.SplineOrder, Is.EqualTo("3"));
            Assert.That(style.HasLines, Is.True);
            Assert.That(style.ConnectBars, Is.False);
            Assert.That(style.DataLabelNumber, Is.EqualTo("value"));
            Assert.That(style.HasDataLabelText, Is.True);
            Assert.That(style.HasDataLabelSymbol, Is.False);
            Assert.That(plot.ThreeDimensional.Transform,
                Is.EqualTo("matrix(1 0 0 1 2 3)"));
            Assert.That(plot.ThreeDimensional.Camera.Projection,
                Is.EqualTo(OdfThreeDimensionalProjection.Perspective));
            Assert.That(plot.ThreeDimensional.Camera.ViewReferencePoint.X,
                Is.EqualTo(1));
            Assert.That(plot.ThreeDimensional.Lights, Has.Length.EqualTo(1));
            Assert.That(plot.ThreeDimensional.Lights[0].IsEnabled, Is.True);
        });
    }

    [Test]
    public void WritesSeriesLabelsStyleVariantsMarkersAndSceneChanges()
    {
        OdfDocument document = ReadChart();
        OdfChartPlotArea plot = document.Charts.Single().PlotArea!;
        OdfChartSeries series = plot.Series.Single();

        series.ClassName = "chart:area";
        series.ValuesCellRangeAddress = "Local.D2:D4";
        series.LabelCellAddress = "Local.D1";
        series.AttachedAxis = "secondary-y";
        series.Domains[0].CellRangeAddress = "Local.B2:B4";
        series.DataPoints[0].Repeated = "3";
        series.DataPoints[0].StyleName = "point-style";
        series.DataPoints[0].Label!.Text = "Changed peak";
        series.Label!.Text = "Changed series";
        series.Label.X = "4cm";
        series.StyleMetadata.SymbolName = "star";
        series.StyleMetadata.SymbolWidth = "0.5cm";
        series.StyleMetadata.SymbolImageHref = "Pictures/new-marker.png";
        series.StyleMetadata.Interpolation = "b-spline";
        series.StyleMetadata.SplineOrder = "4";
        series.StyleMetadata.HasLines = false;
        series.StyleMetadata.ConnectBars = true;
        series.StyleMetadata.HasDataLabelText = false;
        series.StyleMetadata.HasDataLabelSymbol = true;
        plot.StyleMetadata.IsVertical = false;
        plot.StyleMetadata.IsStacked = false;
        plot.StyleMetadata.IsPercentage = true;
        plot.StyleMetadata.IsThreeDimensional = false;
        plot.StyleMetadata.SolidType = "cone";
        plot.ThreeDimensional.Transform = "matrix(1 0 0 1 5 6)";
        plot.ThreeDimensional.Camera.Distance = "7cm";
        plot.ThreeDimensional.Lights[0].IsEnabled = false;

        OdfDocument reread = ReadBytes(Write(document));
        OdfChartPlotArea changedPlot = reread.Charts.Single().PlotArea!;
        OdfChartSeries changed = changedPlot.Series.Single();

        Assert.Multiple(() =>
        {
            Assert.That(changed.Kind, Is.EqualTo(OdfChartKind.Area));
            Assert.That(changed.ValuesCellRangeAddress,
                Is.EqualTo("Local.D2:D4"));
            Assert.That(changed.LabelCellAddress, Is.EqualTo("Local.D1"));
            Assert.That(changed.AttachedAxis, Is.EqualTo("secondary-y"));
            Assert.That(changed.Domains[0].CellRangeAddress,
                Is.EqualTo("Local.B2:B4"));
            Assert.That(changed.DataPoints[0].RepeatCount, Is.EqualTo(3));
            Assert.That(changed.DataPoints[0].Label?.Text,
                Is.EqualTo("Changed peak"));
            Assert.That(changed.Label?.Text, Is.EqualTo("Changed series"));
            Assert.That(changed.Label?.X, Is.EqualTo("4cm"));
            Assert.That(changed.StyleMetadata.SymbolName,
                Is.EqualTo("star"));
            Assert.That(changed.StyleMetadata.SymbolWidth,
                Is.EqualTo("0.5cm"));
            Assert.That(changed.StyleMetadata.SymbolImageHref,
                Is.EqualTo("Pictures/new-marker.png"));
            Assert.That(changed.StyleMetadata.Interpolation,
                Is.EqualTo("b-spline"));
            Assert.That(changed.StyleMetadata.SplineOrder, Is.EqualTo("4"));
            Assert.That(changed.StyleMetadata.HasLines, Is.False);
            Assert.That(changed.StyleMetadata.ConnectBars, Is.True);
            Assert.That(changed.StyleMetadata.HasDataLabelText, Is.False);
            Assert.That(changed.StyleMetadata.HasDataLabelSymbol, Is.True);
            Assert.That(changedPlot.StyleMetadata.IsVertical, Is.False);
            Assert.That(changedPlot.StyleMetadata.IsStacked, Is.False);
            Assert.That(changedPlot.StyleMetadata.IsPercentage, Is.True);
            Assert.That(changedPlot.StyleMetadata.IsThreeDimensional,
                Is.False);
            Assert.That(changedPlot.StyleMetadata.SolidType,
                Is.EqualTo("cone"));
            Assert.That(changedPlot.ThreeDimensional.Transform,
                Is.EqualTo("matrix(1 0 0 1 5 6)"));
            Assert.That(changedPlot.ThreeDimensional.Camera.Distance,
                Is.EqualTo("7cm"));
            Assert.That(changedPlot.ThreeDimensional.Lights[0].IsEnabled,
                Is.False);
        });
    }

    static OdfDocument ReadChart()
    {
        return ReadBytes(Encoding.UTF8.GetBytes(ChartXml()));
    }

    static OdfDocument ReadBytes(byte[] data)
    {
        return new OdfDocumentReader().ReadDocumentBytes(
            data,
            "series.fodc",
            new OdfLoadOptions());
    }

    static byte[] Write(OdfDocument document)
    {
        return new OdfDocumentWriter().WriteDocumentBytes(
            document,
            new OdfSaveOptions
            {
                Deterministic = true
            });
    }

    static string MinimalChartXml(string seriesClass)
    {
        string classAttribute = seriesClass.Length == 0
            ? ""
            : " chart:class='" + seriesClass + "'";
        return ChartRootStart()
            + "<office:body><office:chart><chart:chart chart:class='chart:bar'>"
            + "<chart:plot-area><chart:series" + classAttribute
            + "/></chart:plot-area>"
            + "</chart:chart></office:chart></office:body></office:document>";
    }

    static string ChartXml()
    {
        return ChartRootStart()
            + "<office:automatic-styles>"
            + ChartStyle("plot-style",
                " chart:vertical='true' chart:stacked='true' "
                + "chart:percentage='false' chart:three-dimensional='true' "
                + "chart:deep='true' chart:solid-type='cylinder'")
            + "<style:style style:name='series-style' style:family='chart'>"
            + "<style:chart-properties chart:symbol-type='named-symbol' "
            + "chart:symbol-name='diamond' chart:symbol-width='0.3cm' "
            + "chart:symbol-height='0.3cm' chart:interpolation='cubic-spline' "
            + "chart:spline-order='3' chart:spline-resolution='20' "
            + "chart:lines='true' chart:connect-bars='false' "
            + "chart:data-label-number='value' chart:data-label-text='true' "
            + "chart:data-label-symbol='false'>"
            + "<chart:symbol-image xlink:href='Pictures/marker.png'/>"
            + "</style:chart-properties></style:style>"
            + ChartStyle("point-style", " chart:pie-offset='15'")
            + ChartStyle("label-style", " chart:data-label-number='percentage'")
            + "</office:automatic-styles>"
            + "<office:body><office:chart><chart:chart chart:class='chart:bar'>"
            + "<chart:plot-area chart:style-name='plot-style' "
            + "dr3d:transform='matrix(1 0 0 1 2 3)' dr3d:vrp='(1 2 3)' "
            + "dr3d:vpn='(0 0 1)' dr3d:vup='(0 1 0)' "
            + "dr3d:projection='perspective' dr3d:distance='5cm'>"
            + "<dr3d:light dr3d:diffuse-color='#ffffff' "
            + "dr3d:direction='(1 0 0)' dr3d:enabled='true' "
            + "dr3d:specular='false'/>"
            + "<chart:series chart:class='chart:line' "
            + "chart:values-cell-range-address='Local.C2:C4' "
            + "chart:label-cell-address='Local.C1' "
            + "chart:attached-axis='primary-y' chart:style-name='series-style'>"
            + "<chart:domain table:cell-range-address='Local.A2:A4'/>"
            + "<chart:domain table:cell-range-address='Local.B2:B4'/>"
            + "<chart:data-point chart:repeated='2'>"
            + "<chart:data-label chart:style-name='label-style'>"
            + "<text:p>Peak</text:p></chart:data-label></chart:data-point>"
            + "<chart:data-point chart:style-name='point-style'/>"
            + "<chart:data-label chart:style-name='label-style' svg:x='3cm'>"
            + "<text:p>Series label</text:p></chart:data-label>"
            + "</chart:series></chart:plot-area></chart:chart>"
            + "</office:chart></office:body></office:document>";
    }

    static string ChartRootStart()
    {
        return "<?xml version='1.0' encoding='UTF-8'?>"
            + "<office:document xmlns:office='" + OdfNamespaces.Office + "'"
            + " xmlns:chart='" + OdfNamespaces.Chart + "'"
            + " xmlns:table='" + OdfNamespaces.Table + "'"
            + " xmlns:text='" + OdfNamespaces.Text + "'"
            + " xmlns:svg='" + OdfNamespaces.Svg + "'"
            + " xmlns:style='" + OdfNamespaces.Style + "'"
            + " xmlns:dr3d='" + OdfNamespaces.Dr3d + "'"
            + " xmlns:xlink='" + OdfNamespaces.XLink + "'"
            + " office:version='1.4' office:mimetype='"
            + OdfMediaTypes.Chart + "'>";
    }

    static string ChartStyle(string name, string properties)
    {
        return "<style:style style:name='" + name
            + "' style:family='chart'><style:chart-properties"
            + properties + "/></style:style>";
    }
}
