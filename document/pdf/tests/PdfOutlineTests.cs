using System.Text;
using System.Text.RegularExpressions;
using NUnit.Framework;
using pdf;

namespace pdf.Tests;

[TestFixture]
public class PdfOutlineTests
{
    [Test]
    public void AddOutline_StoresRootOutlineOnDocument()
    {
        PdfDocument document = new PdfDocument();
        PdfPage page = document.AddPage(200, 100);

        PdfOutlineItem outline = document.AddOutline("Chapter 1", page, 12, 88);

        Assert.That(document.Outlines.Count, Is.EqualTo(1));
        Assert.That(document.Outlines[0], Is.SameAs(outline));
        Assert.That(outline.Title, Is.EqualTo("Chapter 1"));
        Assert.That(outline.Page, Is.SameAs(page));
        Assert.That(outline.X, Is.EqualTo(12));
        Assert.That(outline.Y, Is.EqualTo(88));
        Assert.That(outline.Parent, Is.Null);
        Assert.That(outline.Children.Count, Is.EqualTo(0));
    }

    [Test]
    public void AddOutlineChild_StoresNestedOutline()
    {
        PdfDocument document = new PdfDocument();
        PdfPage page = document.AddPage(200, 100);
        PdfOutlineItem parent = document.AddOutlineAtPageTop("Chapter 1", page);

        PdfOutlineItem child = document.AddOutlineChild(parent, "Section 1.1", page, 20, 80);

        Assert.That(parent.Children.Count, Is.EqualTo(1));
        Assert.That(parent.Children[0], Is.SameAs(child));
        Assert.That(child.Parent, Is.SameAs(parent));
        Assert.That(child.Title, Is.EqualTo("Section 1.1"));
        Assert.That(child.X, Is.EqualTo(20));
        Assert.That(child.Y, Is.EqualTo(80));
    }

    [Test]
    public void Save_WritesOutlineCatalogRootAndNestedItems()
    {
        PdfDocument document = new PdfDocument();
        PdfPage firstPage = document.AddPage(200, 100);
        PdfPage secondPage = document.AddPage(200, 100);

        firstPage.Canvas.DrawText("Chapter", 10, 50, 12);
        secondPage.Canvas.DrawText("Section", 10, 50, 12);
        PdfOutlineItem chapter = document.AddOutline("Chapter 1", firstPage, 0, 100);
        document.AddOutlineChild(chapter, "Section (A)", secondPage, 5, 95);

        string pdf = Encoding.ASCII.GetString(document.ToBytes());

        Assert.That(GetObjectBody(pdf, 1), Does.Contain("/Outlines 5 0 R /PageMode /UseOutlines"));
        Assert.That(GetObjectBody(pdf, 5), Does.Contain("<< /Type /Outlines /First 6 0 R /Last 6 0 R /Count 2 >>"));
        Assert.That(GetObjectBody(pdf, 6), Does.Contain("/Title (Chapter 1)"));
        Assert.That(GetObjectBody(pdf, 6), Does.Contain("/Parent 5 0 R"));
        Assert.That(GetObjectBody(pdf, 6), Does.Contain("/Dest [8 0 R /XYZ 0 100 0]"));
        Assert.That(GetObjectBody(pdf, 6), Does.Contain("/First 7 0 R /Last 7 0 R /Count 1"));
        Assert.That(GetObjectBody(pdf, 7), Does.Contain("/Title (Section \\(A\\))"));
        Assert.That(GetObjectBody(pdf, 7), Does.Contain("/Parent 6 0 R"));
        Assert.That(GetObjectBody(pdf, 7), Does.Contain("/Dest [10 0 R /XYZ 5 95 0]"));
    }

    [Test]
    public void Save_WritesOutlineSiblingLinks()
    {
        PdfDocument document = new PdfDocument();
        PdfPage page = document.AddPage(200, 100);

        document.AddOutline("First", page, 0, 100);
        document.AddOutline("Second", page, 0, 90);

        string pdf = Encoding.ASCII.GetString(document.ToBytes());

        Assert.That(GetObjectBody(pdf, 5), Does.Contain("/First 6 0 R /Last 7 0 R /Count 2"));
        Assert.That(GetObjectBody(pdf, 6), Does.Contain("/Next 7 0 R"));
        Assert.That(GetObjectBody(pdf, 6), Does.Not.Contain("/Prev"));
        Assert.That(GetObjectBody(pdf, 7), Does.Contain("/Prev 6 0 R"));
        Assert.That(GetObjectBody(pdf, 7), Does.Not.Contain("/Next"));
    }

    [Test]
    public void OutlinesRejectInvalidInputs()
    {
        PdfDocument document = new PdfDocument();
        PdfPage page = document.AddPage(200, 100);
        PdfPage otherPage = new PdfPage(200, 100);
        PdfOutlineItem parent = document.AddOutlineAtPageTop("Chapter", page);
        PdfOutlineItem otherParent = new PdfOutlineItem("Other", page, 0, 0);

        Assert.Throws<ArgumentNullException>(() => document.AddOutline(null!, page, 0, 0));
        Assert.Throws<ArgumentException>(() => document.AddOutline("", page, 0, 0));
        Assert.Throws<ArgumentNullException>(() => document.AddOutline("x", null!, 0, 0));
        Assert.Throws<ArgumentException>(() => document.AddOutline("x", otherPage, 0, 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => document.AddOutline("nan", page, double.NaN, 0));
        Assert.Throws<ArgumentException>(() => new PdfOutlineItem("", page, 0, 0));
        Assert.Throws<ArgumentNullException>(() => document.AddOutlineChild(null!, "x", page, 0, 0));
        Assert.Throws<ArgumentException>(() => document.AddOutlineChild(otherParent, "x", page, 0, 0));
        Assert.Throws<ArgumentNullException>(() => document.AddOutlineChild(parent, null!, page, 0, 0));
        Assert.Throws<ArgumentException>(() => document.AddOutlineChild(parent, "", page, 0, 0));
        Assert.Throws<ArgumentNullException>(() => document.AddOutlineChild(parent, "x", null!, 0, 0));
        Assert.Throws<ArgumentException>(() => document.AddOutlineChild(parent, "x", otherPage, 0, 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => document.AddOutlineChild(parent, "x", page, 0, double.PositiveInfinity));
    }

    static string GetObjectBody(string pdf, int objectId)
    {
        Match match = Regex.Match(pdf, objectId.ToString() + @" 0 obj\n(?<body>.*?)\nendobj\n", RegexOptions.Singleline);
        Assert.That(match.Success, Is.True, "object " + objectId.ToString() + " should exist");
        return match.Groups["body"].Value;
    }
}
