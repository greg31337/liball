using System.Text;
using NUnit.Framework;
using pdf;

namespace pdf.Tests;

[TestFixture]
public class PdfPublishingNavigationTests
{
    [Test]
    public void PageLabelRangesFormatLabelsAndSerializeCatalogNumberTree()
    {
        PdfDocument document = CreateDocumentWithPages(10);
        document.AddPageLabelRange(0, PdfPageLabelStyle.LowerRoman, string.Empty, 1);
        document.AddPageLabelRange(2, PdfPageLabelStyle.Decimal, "A-", 1);
        document.AddPageLabelRange(4, PdfPageLabelStyle.UpperLetters, string.Empty, 27);
        document.AddPageLabelRange(6, PdfPageLabelStyle.None, "Cover", 1);
        document.AddPageLabelRange(8, PdfPageLabelStyle.LowerLetters, string.Empty, 27);

        Assert.That(document.GetPageLabel(0), Is.EqualTo("i"));
        Assert.That(document.GetPageLabel(1), Is.EqualTo("ii"));
        Assert.That(document.GetPageLabel(2), Is.EqualTo("A-1"));
        Assert.That(document.GetPageLabel(3), Is.EqualTo("A-2"));
        Assert.That(document.GetPageLabel(4), Is.EqualTo("AA"));
        Assert.That(document.GetPageLabel(5), Is.EqualTo("BB"));
        Assert.That(document.GetPageLabel(6), Is.EqualTo("Cover"));
        Assert.That(document.GetPageLabel(8), Is.EqualTo("aa"));
        Assert.That(document.GetPageLabel(9), Is.EqualTo("bb"));
        Assert.That(document.GetPageLabelRangeCount(), Is.EqualTo(5));
        Assert.That(document.GetPageLabelRange(1).Page, Is.SameAs(document.GetPage(2)));

        string pdf = Encoding.ASCII.GetString(document.ToBytes());
        Assert.That(pdf, Does.Contain("/PageLabels << /Nums [0 << /S /r /St 1 >> 2 << /S /D /St 1 /P (A-) >> 4 << /S /A /St 27 >> 6 << /P (Cover) >> 8 << /S /a /St 27 >> ] >>"));
    }

    [Test]
    public void GeneratedIndexSortsMarkersAndLinksToLogicalPageLabels()
    {
        PdfDocument document = CreateDocumentWithPages(2);
        PdfPage first = document.GetPage(0);
        PdfPage second = document.GetPage(1);
        document.AddPageLabelRange(0, PdfPageLabelStyle.LowerRoman, string.Empty, 1);
        PdfIndexMarker zebra = document.AddIndexMarker("Zebra", second, 5, 95);
        PdfIndexMarker alpha = document.AddIndexSubentryMarker("Alpha", "API", first, 4, 94);
        PdfGeneratedIndex index = new PdfLayoutFactory().CreateGeneratedIndex();

        PdfPage indexPage = index.InsertPageWithLayout(document, 0, new PdfPageSizeFactory().Custom(300, 200), new PdfPageMargins(20, 20, 20, 20));

        string content = indexPage.GetCanvas().ToPdfString();
        Assert.That(content, Does.Contain("(Index) Tj"));
        Assert.That(content.IndexOf("(Alpha ", StringComparison.Ordinal), Is.LessThan(content.IndexOf("(Zebra) Tj", StringComparison.Ordinal)));
        Assert.That(content, Does.Contain("(i) Tj"));
        Assert.That(content, Does.Contain("(ii) Tj"));
        Assert.That(indexPage.InternalLinks.Count, Is.EqualTo(2));
        Assert.That(indexPage.InternalLinks[0].DestinationName, Is.EqualTo(alpha.DestinationName));
        Assert.That(indexPage.InternalLinks[1].DestinationName, Is.EqualTo(zebra.DestinationName));
        Assert.That(document.GetIndexMarkerCount(), Is.EqualTo(2));
        Assert.That(document.GetIndexMarker(0), Is.SameAs(zebra));
        Assert.DoesNotThrow(() => document.ToBytes());
    }

    [Test]
    public void GeneratedFigureAndTableListsFilterMarkersAndFollowDocumentOrder()
    {
        PdfDocument document = CreateDocumentWithPages(3);
        PdfPage listPage = document.GetPage(0);
        PdfPage firstTarget = document.GetPage(1);
        PdfPage secondTarget = document.GetPage(2);
        PdfPublishingMarker laterFigure = document.AddFigureMarker("Figure 2. Later", secondTarget, 0, 100);
        PdfPublishingMarker table = document.AddTableMarker("Table 1. Data", firstTarget, 0, 100);
        PdfPublishingMarker earlierFigure = document.AddFigureMarker("Figure 1. Earlier", firstTarget, 0, 90);
        PdfLayoutFactory factory = new PdfLayoutFactory();
        PdfGeneratedList figures = factory.CreateListOfFigures();
        PdfGeneratedList tables = factory.CreateListOfTables();

        figures.Draw(document, listPage, 10, 95, 180);
        tables.Draw(document, listPage, 10, 45, 180);

        string content = listPage.GetCanvas().ToPdfString();
        Assert.That(figures.GetMarkerKind(), Is.EqualTo(PdfPublishingMarkerKind.Figure));
        Assert.That(tables.GetMarkerKind(), Is.EqualTo(PdfPublishingMarkerKind.Table));
        Assert.That(content, Does.Contain("(List of Figures) Tj"));
        Assert.That(content, Does.Contain("(List of Tables) Tj"));
        Assert.That(content.IndexOf("(Figure 1. Earlier) Tj", StringComparison.Ordinal), Is.LessThan(content.IndexOf("(Figure 2. Later) Tj", StringComparison.Ordinal)));
        Assert.That(content, Does.Contain("(Table 1. Data) Tj"));
        Assert.That(listPage.InternalLinks.Count, Is.EqualTo(3));
        Assert.That(listPage.InternalLinks[0].DestinationName, Is.EqualTo(earlierFigure.DestinationName));
        Assert.That(listPage.InternalLinks[1].DestinationName, Is.EqualTo(laterFigure.DestinationName));
        Assert.That(listPage.InternalLinks[2].DestinationName, Is.EqualTo(table.DestinationName));
        Assert.That(document.GetPublishingMarkerCount(), Is.EqualTo(3));
        Assert.That(document.GetPublishingMarker(1), Is.SameAs(table));
    }

    [Test]
    public void CrossReferenceMarkersFormatLabelsAndCreateWorkingLinks()
    {
        PdfDocument document = CreateDocumentWithPages(2);
        PdfPage source = document.GetPage(0);
        PdfPage target = document.GetPage(1);
        document.AddPageLabelRange(0, PdfPageLabelStyle.UpperRoman, string.Empty, 3);
        PdfCrossReferenceMarker marker = document.AddCrossReferenceMarker("results", "Results", target, 8, 92);

        PdfInternalLink link = document.AddCrossReferenceLink(source, "results", new PdfRectangle(10, 20, 50, 12));

        Assert.That(document.GetCrossReferenceText("results"), Is.EqualTo("Results, page IV"));
        Assert.That(document.GetCrossReferenceMarkerCount(), Is.EqualTo(1));
        Assert.That(document.GetCrossReferenceMarker(0), Is.SameAs(marker));
        Assert.That(link.DestinationName, Is.EqualTo(marker.DestinationName));
        string pdf = Encoding.ASCII.GetString(document.ToBytes());
        Assert.That(pdf, Does.Contain("/" + marker.DestinationName + " ["));
        Assert.That(pdf, Does.Contain("/Dest /" + marker.DestinationName));
    }

    [Test]
    public void StructureLinkTargetsRetainNodeIdentityAndAccessibleAnnotationText()
    {
        PdfDocument document = CreateDocumentWithPages(2);
        PdfPage source = document.GetPage(0);
        PdfPage targetPage = document.GetPage(1);
        PdfAccessibilityNode node = document.AddAccessibilityNodeWithDescription(targetPage, "Figure", "Results chart");
        PdfStructureLinkTarget target = document.AddStructureLinkTarget("results-chart", node, 12, 88);

        PdfInternalLink link = source.AddStructureInternalLink(target, new PdfRectangle(10, 20, 60, 12));

        Assert.That(target.StructureNode, Is.SameAs(node));
        Assert.That(target.Destination.Page, Is.SameAs(targetPage));
        Assert.That(document.GetStructureLinkTargetCount(), Is.EqualTo(1));
        Assert.That(document.GetStructureLinkTarget(0), Is.SameAs(target));
        Assert.That(link.DestinationName, Is.EqualTo(target.Destination.Name));
        string pdf = Encoding.ASCII.GetString(document.ToBytes());
        Assert.That(pdf, Does.Contain("/Dest /" + target.Destination.Name + " /Contents (Results chart)"));
    }

    [Test]
    public void PublishingNavigationRejectsInvalidRegistrations()
    {
        PdfDocument document = CreateDocumentWithPages(1);
        PdfPage page = document.GetPage(0);
        PdfPage otherPage = new PdfPage(100, 100);
        PdfAccessibilityNode node = document.AddAccessibilityNode(page, "P");

        Assert.Throws<ArgumentException>(() => document.AddIndexMarker(string.Empty, page, 0, 0));
        Assert.Throws<ArgumentException>(() => document.AddIndexSubentryMarker("term", string.Empty, page, 0, 0));
        Assert.Throws<ArgumentException>(() => document.AddFigureMarker("figure", otherPage, 0, 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => document.GetIndexMarker(0));
        Assert.Throws<ArgumentOutOfRangeException>(() => document.GetPublishingMarker(0));
        Assert.Throws<ArgumentOutOfRangeException>(() => document.GetCrossReferenceMarker(0));
        Assert.Throws<ArgumentOutOfRangeException>(() => document.AddPageLabelRange(2, PdfPageLabelStyle.Decimal, string.Empty, 1));
        Assert.Throws<ArgumentException>(() => document.AddPageLabelRange(0, (PdfPageLabelStyle)99, string.Empty, 1));
        Assert.Throws<ArgumentException>(() => document.AddPageLabelRange(0, PdfPageLabelStyle.None, string.Empty, 1));
        Assert.Throws<ArgumentOutOfRangeException>(() => new PdfPageLabelRange(page, PdfPageLabelStyle.Decimal, string.Empty, 0));
        Assert.Throws<ArgumentException>(() => new PdfPageLabelRange(page, (PdfPageLabelStyle)99, string.Empty, 1));
        Assert.Throws<ArgumentOutOfRangeException>(() => document.AddIndexMarker("nan", page, double.NaN, 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => document.GetPageLabel(2));
        document.AddCrossReferenceMarker("one", "One", page, 0, 0);
        Assert.Throws<ArgumentException>(() => document.AddCrossReferenceMarker("one", "Other", page, 0, 0));
        Assert.Throws<ArgumentException>(() => document.AddCrossReferenceLink(page, "missing", new PdfRectangle(0, 0, 10, 10)));
        Assert.Throws<ArgumentException>(() => document.AddStructureLinkTarget("foreign", new PdfAccessibilityNode(page, "P", string.Empty), 0, 0));
        PdfStructureLinkTarget target = document.AddStructureLinkTarget("local", node, 0, 0);
        Assert.Throws<ArgumentException>(() => otherPage.AddStructureInternalLink(target, new PdfRectangle(0, 0, 10, 10)));
        Assert.Throws<ArgumentException>(() => new PdfGeneratedList((PdfPublishingMarkerKind)99));
    }

    static PdfDocument CreateDocumentWithPages(int count)
    {
        PdfDocument document = new PdfDocument();
        for (int index = 0; index < count; index++)
        {
            document.AddPage(200, 100);
        }

        return document;
    }
}
