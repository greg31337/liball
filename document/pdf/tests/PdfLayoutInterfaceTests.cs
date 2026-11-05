namespace pdf.Tests;

public class PdfLayoutInterfaceTests
{
    [Test]
    public void LayoutDocument_CustomElementUsesGenericPaginationFlow()
    {
        PdfDocument document = new PdfDocument();
        PdfLayoutDocument layout = new PdfLayoutDocument(
            document,
            new PdfPageSizeFactory().Custom(100, 60),
            new PdfPageMargins(10, 5, 10, 5));
        RecordingLayoutElement element = new RecordingLayoutElement(20, 10);

        layout.AddBlock(PdfBlock.WithSize(10, 45));
        layout.AddItem(element);
        layout.Render();

        Assert.That(layout.Items[1], Is.SameAs(element));
        Assert.That(document.Pages.Count, Is.EqualTo(2));
        Assert.That(element.FlowCount, Is.EqualTo(1));
        Assert.That(element.MeasureCount, Is.EqualTo(2));
        Assert.That(element.DrawCount, Is.EqualTo(1));
        Assert.That(element.LastMeasureContext!.AvailableWidth, Is.EqualTo(80));
        Assert.That(element.LastMeasureContext.AvailableHeight, Is.EqualTo(50));
        Assert.That(element.LastDrawContext!.Page, Is.SameAs(document.Pages[1]));
    }

    [Test]
    public void ContainerAndTemplate_AcceptInterfaceTestDoubleDirectly()
    {
        RecordingLayoutElement element = new RecordingLayoutElement(12, 8);
        PdfStackBlock stack = new PdfStackBlock();
        stack.AddElement(element);

        PdfLayoutResult measured = stack.Measure(new PdfLayoutContext(80, 40));
        PdfPage page = new PdfPage(100, 60);
        PdfLayoutResult drawn = stack.Draw(new PdfLayoutDrawContext(page, 10, 10, 80, 40));
        PdfPageTemplate template = new PdfPageTemplate(element, 8);

        Assert.That(stack.Items[0], Is.SameAs(element));
        Assert.That(template.Item, Is.SameAs(element));
        Assert.That(measured.Size.Width, Is.EqualTo(12));
        Assert.That(drawn.Size.Height, Is.EqualTo(8));
        Assert.That(element.MeasureCount, Is.EqualTo(3));
        Assert.That(element.DrawCount, Is.EqualTo(1));
    }

    [Test]
    public void LayoutItem_DecoratesAnyInterfaceImplementation()
    {
        RecordingLayoutElement element = new RecordingLayoutElement(10, 10);
        PdfLayoutItem item = PdfLayoutItem.FromElement(element);

        PdfLayoutResult result = item.Measure(new PdfLayoutContext(20, 20));

        Assert.That(item.Element, Is.SameAs(element));
        Assert.That(result.Size.Width, Is.EqualTo(10));
        Assert.That(element.MeasureCount, Is.EqualTo(1));
    }

    class RecordingLayoutElement : IPdfLayoutElement
    {
        public double Width;
        public double Height;
        public int MeasureCount;
        public int DrawCount;
        public int FlowCount;
        public PdfLayoutContext? LastMeasureContext;
        public PdfLayoutDrawContext? LastDrawContext;

        public RecordingLayoutElement(double width, double height)
        {
            Width = width;
            Height = height;
        }

        public PdfLayoutResult Measure(PdfLayoutContext context)
        {
            MeasureCount++;
            LastMeasureContext = context;
            bool fits = Width <= context.AvailableWidth && Height <= context.AvailableHeight;
            return Result(fits);
        }

        public PdfLayoutResult Draw(PdfLayoutDrawContext context)
        {
            DrawCount++;
            LastDrawContext = context;
            bool fits = Width <= context.AvailableWidth && Height <= context.AvailableHeight;
            return Result(fits);
        }

        public PdfLayoutResult Flow(PdfPageCursor cursor)
        {
            FlowCount++;
            return cursor.DrawItem(this);
        }

        PdfLayoutResult Result(bool fits)
        {
            PdfLayoutSize size = new PdfLayoutSize(Width, Height);
            return new PdfLayoutResult(size, PdfBox.FromCoordinates(0, 0, Width, Height), fits);
        }
    }
}
