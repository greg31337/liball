using NUnit.Framework;
using pdf;

namespace pdf.Tests;

[TestFixture]
public class PdfSemanticLayoutElementTests
{
    [Test]
    public void FrameMeasuresAndDrawsDefensivelyCopiedStyleAroundContent()
    {
        PdfLayoutFactory factory = new PdfLayoutFactory();
        PdfElementStyle style = new PdfElementStyle();
        style.Padding = new PdfInsets(2, 2, 2, 2);
        style.Border = new PdfInsets(1, 1, 1, 1);
        style.Margin = new PdfInsets(3, 3, 3, 3);
        style.HasBackground = true;
        style.BackgroundColor = PdfColor.Gray(0.25);
        PdfFrame frame = factory.CreateFrame(factory.CreateFixedBlock(20, 10));
        frame.SetStyle(style);
        style.Padding = new PdfInsets(20, 20, 20, 20);
        style.BackgroundColor.First = 0.9;

        PdfLayoutResult measured = frame.Measure(new PdfLayoutContext(100, 100));
        PdfPage page = new PdfPage(100, 100);
        PdfLayoutResult drawn = frame.Draw(new PdfLayoutDrawContext(page, 10, 20, 80, 60));

        string content = page.GetCanvas().ToPdfString();
        Assert.That(measured.Size.Width, Is.EqualTo(32));
        Assert.That(measured.Size.Height, Is.EqualTo(22));
        Assert.That(drawn.Size.Width, Is.EqualTo(32));
        Assert.That(content, Does.Contain("0.25 g"));
        Assert.That(content, Does.Contain("13 23 26 16 re f"));
        Assert.That(content, Does.Not.Contain("0.9 g"));
    }

    [Test]
    public void FigureMeasuresContentCaptionAndReusableSpacingAsOneElement()
    {
        PdfLayoutFactory factory = new PdfLayoutFactory();
        PdfCaption caption = factory.CreateCaption(factory.CreateFixedBlock(10, 5));
        PdfFigure figure = factory.CreateFigure(factory.CreateFixedBlock(30, 20), caption);
        figure.SetCaptionSpacing(4);

        PdfLayoutResult explicitSpacing = figure.Measure(new PdfLayoutContext(100, 100));

        PdfElementStyle style = new PdfElementStyle();
        style.ItemSpacing = 6;
        figure.SetStyle(style);
        style.ItemSpacing = 50;
        PdfLayoutResult styledSpacing = figure.Measure(new PdfLayoutContext(100, 100));

        Assert.That(explicitSpacing.Size.Width, Is.EqualTo(30));
        Assert.That(explicitSpacing.Size.Height, Is.EqualTo(29));
        Assert.That(styledSpacing.Size.Height, Is.EqualTo(31));
        Assert.That(figure.GetAccessibilityRole(), Is.EqualTo("Figure"));
        Assert.That(caption.GetAccessibilityRole(), Is.EqualTo("Caption"));
    }

    [Test]
    public void SemanticElementsRegisterRolesAndDescriptionsIncludingNestedContent()
    {
        PdfLayoutFactory factory = new PdfLayoutFactory();
        PdfDocument document = new PdfDocument();
        PdfLayoutDocument layout = new PdfLayoutDocument(document, new PdfPageDimensions(100, 200), new PdfPageMargins(0, 0, 0, 0));
        PdfCallout nestedCallout = factory.CreateCallout(factory.CreateFixedBlock(10, 5));
        nestedCallout.SetAccessibilityDescription("Important note");
        PdfFrame frame = factory.CreateFrame(nestedCallout);
        frame.SetAccessibilityDescription("Framed note");
        PdfCaption standaloneCaption = factory.CreateCaption(factory.CreateFixedBlock(10, 5));
        standaloneCaption.SetAccessibilityDescription("Standalone caption");
        PdfCaption figureCaption = factory.CreateCaption(factory.CreateFixedBlock(10, 5));
        figureCaption.SetAccessibilityDescription("Figure caption");
        PdfFigure figure = factory.CreateFigure(factory.CreateFixedBlock(10, 5), figureCaption);
        figure.SetAccessibilityDescription("Measured figure");
        PdfSidebar sidebar = factory.CreateSidebar(factory.CreateFixedBlock(10, 5));
        PdfChartPlaceholder chart = factory.CreateChartPlaceholder("Quarterly sales", 20, 10);

        layout.AddFrame(frame);
        layout.AddCaption(standaloneCaption);
        layout.AddFigure(figure);
        layout.AddSidebar(sidebar);
        layout.AddChartPlaceholder(chart);
        layout.Render();

        Assert.That(document.AccessibilityNodes.Count, Is.EqualTo(7));
        Assert.That(document.AccessibilityNodes[0].Role, Is.EqualTo("Div"));
        Assert.That(document.AccessibilityNodes[0].Description, Is.EqualTo("Framed note"));
        Assert.That(document.AccessibilityNodes[1].Role, Is.EqualTo("Note"));
        Assert.That(document.AccessibilityNodes[1].Description, Is.EqualTo("Important note"));
        Assert.That(document.AccessibilityNodes[2].Role, Is.EqualTo("Caption"));
        Assert.That(document.AccessibilityNodes[3].Role, Is.EqualTo("Figure"));
        Assert.That(document.AccessibilityNodes[3].Description, Is.EqualTo("Measured figure"));
        Assert.That(document.AccessibilityNodes[4].Role, Is.EqualTo("Caption"));
        Assert.That(document.AccessibilityNodes[4].Description, Is.EqualTo("Figure caption"));
        Assert.That(document.AccessibilityNodes[5].Role, Is.EqualTo("Aside"));
        Assert.That(document.AccessibilityNodes[6].Role, Is.EqualTo("Figure"));
        Assert.That(document.AccessibilityNodes[6].Description, Is.EqualTo("Quarterly sales"));
        Assert.That(document.AccessibilityNodes[6].Page, Is.SameAs(document.GetPage(0)));
    }

    [Test]
    public void SemanticKeepTogetherMovesCompleteElementToNextPage()
    {
        PdfLayoutFactory factory = new PdfLayoutFactory();
        PdfDocument document = new PdfDocument();
        PdfLayoutDocument layout = new PdfLayoutDocument(document, new PdfPageDimensions(100, 60), new PdfPageMargins(10, 5, 10, 5));
        PdfSidebar sidebar = factory.CreateSidebar(factory.CreateFixedBlock(20, 20));
        sidebar.SetKeepTogether();

        layout.AddBlock(factory.CreateFixedBlock(20, 35));
        layout.AddItem(sidebar);
        layout.Render();

        Assert.That(sidebar.IsKeepTogether(), Is.True);
        Assert.That(document.GetPageCount(), Is.EqualTo(2));
        Assert.That(document.AccessibilityNodes.Count, Is.EqualTo(1));
        Assert.That(document.AccessibilityNodes[0].Page, Is.SameAs(document.GetPage(1)));
        sidebar.ClearKeepTogether();
        Assert.That(sidebar.IsKeepTogether(), Is.False);
    }

    [Test]
    public void ChartPlaceholderDrawsSizedCrossedBoxAndDefensivelyStyledLabel()
    {
        PdfLayoutFactory factory = new PdfLayoutFactory();
        PdfChartPlaceholder chart = factory.CreateChartPlaceholder("Sales", 40, 30);
        PdfTextStyle textStyle = new PdfTextStyle();
        textStyle.Font = PdfFont.Courier;
        textStyle.FontSize = 10;
        textStyle.FillColor = PdfColor.Rgb(0.2, 0.4, 0.6);
        chart.SetTextStyle(textStyle);
        textStyle.FillColor.First = 0.9;
        PdfPage page = new PdfPage(100, 100);

        PdfLayoutResult result = chart.Draw(new PdfLayoutDrawContext(page, 10, 20, 80, 60));

        string content = page.GetCanvas().ToPdfString();
        Assert.That(result.Size.Width, Is.EqualTo(40));
        Assert.That(result.Size.Height, Is.EqualTo(30));
        Assert.That(content, Does.Contain("10 20 40 30 re S"));
        Assert.That(content, Does.Contain("10 20 m"));
        Assert.That(content, Does.Contain("50 50 l"));
        Assert.That(content, Does.Contain("10 50 m"));
        Assert.That(content, Does.Contain("50 20 l"));
        Assert.That(content, Does.Contain("0.2 0.4 0.6 rg"));
        Assert.That(content, Does.Contain("(Sales) Tj"));
        Assert.That(content, Does.Not.Contain("0.9 0.4 0.6 rg"));
    }

    [Test]
    public void SemanticLayoutElementsRejectInvalidConfiguration()
    {
        PdfLayoutFactory factory = new PdfLayoutFactory();
        PdfBlock content = factory.CreateFixedBlock(10, 10);
        PdfCaption caption = factory.CreateCaption(content);
        PdfFrame frame = factory.CreateFrame(content);

        Assert.Throws<ArgumentNullException>(() => factory.CreateFrame(null!));
        Assert.Throws<ArgumentNullException>(() => factory.CreateCaption(null!));
        Assert.Throws<ArgumentNullException>(() => factory.CreateFigure(null!, caption));
        Assert.Throws<ArgumentNullException>(() => factory.CreateFigure(content, null!));
        Assert.Throws<ArgumentNullException>(() => factory.CreateSidebar(null!));
        Assert.Throws<ArgumentNullException>(() => factory.CreateCallout(null!));
        Assert.Throws<ArgumentNullException>(() => factory.CreateChartPlaceholder(null!, 10, 10));
        Assert.Throws<ArgumentOutOfRangeException>(() => factory.CreateChartPlaceholder("chart", 0, 10));
        Assert.Throws<ArgumentOutOfRangeException>(() => factory.CreateChartPlaceholder("chart", 10, double.PositiveInfinity));
        Assert.Throws<ArgumentNullException>(() => frame.SetStyle(null!));
        Assert.Throws<ArgumentNullException>(() => frame.SetAccessibilityDescription(null!));
        Assert.Throws<ArgumentOutOfRangeException>(() => factory.CreateFigure(content, caption).SetCaptionSpacing(-1));
        Assert.Throws<ArgumentOutOfRangeException>(() => factory.CreateFigure(content, caption).SetCaptionSpacing(double.NaN));
        Assert.Throws<ArgumentNullException>(() => factory.CreateChartPlaceholder("chart", 10, 10).SetTextStyle(null!));
        Assert.Throws<ArgumentNullException>(() => new PdfLayoutDocument(new PdfDocument(), new PdfPageDimensions(100, 100), new PdfPageMargins(0, 0, 0, 0)).AddCallout(null!));
    }
}
