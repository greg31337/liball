using NUnit.Framework;
using xlsx;

namespace xlsx.Tests;

[TestFixture]
public class SharedStringTableWriterTests
{
    [Test]
    public void GetOrAddText_DeduplicatesPlainTextAndTracksReferenceCounts()
    {
        XlsxSharedStringTablePart part = new XlsxSharedStringTablePart();

        int first = part.GetOrAddText("Alpha");
        int duplicate = part.AddTextReference("Alpha");
        int second = part.GetOrAddText("Beta");
        XlsxSharedStringTableWriteResult result = new XlsxSharedStringTableWriter().WriteString(part);
        XlsxSharedStringTablePart roundTrip = new XlsxSharedStringTableReader().ReadString(result.Xml);

        Assert.That(first, Is.EqualTo(0));
        Assert.That(duplicate, Is.EqualTo(0));
        Assert.That(second, Is.EqualTo(1));
        Assert.That(part.Count, Is.EqualTo(3));
        Assert.That(part.UniqueCount, Is.EqualTo(2));
        Assert.That(part.Items[0].ReferenceCount, Is.EqualTo(2));
        Assert.That(result.IsValid, Is.True);
        Assert.That(result.Count, Is.EqualTo(3));
        Assert.That(result.UniqueCount, Is.EqualTo(2));
        Assert.That(result.Xml, Does.Contain("count=\"3\""));
        Assert.That(result.Xml, Does.Contain("uniqueCount=\"2\""));
        Assert.That(roundTrip.IsValid, Is.True);
        Assert.That(roundTrip.Items.Count, Is.EqualTo(2));
        Assert.That(roundTrip.GetText(0), Is.EqualTo("Alpha"));
        Assert.That(roundTrip.GetText(1), Is.EqualTo("Beta"));
    }

    [Test]
    public void WriteString_WritesPlainRichTextWhitespaceAndUnknowns()
    {
        XlsxSharedStringTablePart part = new XlsxSharedStringTablePart();
        part.UnknownRootAttributes.Add(CreateAttribute("urn:x", "x", "root", "r"));
        part.UnknownXml.Add("<x:future xmlns:x=\"urn:x\" />");
        XlsxSharedStringItem plain = new XlsxSharedStringItem();
        plain.Text = " spaced ";
        plain.ReferenceCount = 2;
        XlsxSharedStringItem rich = new XlsxSharedStringItem();
        rich.Text = "BoldTail";
        rich.IsRichText = true;
        rich.ReferenceCount = 1;
        rich.RichTextRuns.Add(CreateRun("Bold", "<rPr><b /></rPr>"));
        rich.RichTextRuns.Add(CreateRun("Tail", ""));
        part.Items.Add(plain);
        part.Items.Add(rich);

        XlsxSharedStringTableWriteResult result = new XlsxSharedStringTableWriter().WriteString(part);
        XlsxSharedStringTablePart roundTrip = new XlsxSharedStringTableReader().ReadString(result.Xml);

        Assert.That(result.IsValid, Is.True);
        Assert.That(result.Count, Is.EqualTo(3));
        Assert.That(result.UniqueCount, Is.EqualTo(2));
        Assert.That(result.Xml, Does.Contain("xml:space=\"preserve\""));
        Assert.That(result.Xml, Does.Contain("<b"));
        Assert.That(result.Xml, Does.Contain("x:future"));
        Assert.That(roundTrip.IsValid, Is.True);
        Assert.That(roundTrip.UnknownRootAttributes.Count, Is.EqualTo(1));
        Assert.That(roundTrip.UnknownXml.Count, Is.EqualTo(1));
        Assert.That(roundTrip.Items.Count, Is.EqualTo(2));
        Assert.That(roundTrip.Items[0].Text, Is.EqualTo(" spaced "));
        Assert.That(roundTrip.Items[1].IsRichText, Is.True);
        Assert.That(roundTrip.Items[1].Text, Is.EqualTo("BoldTail"));
        Assert.That(roundTrip.Items[1].RichTextRuns.Count, Is.EqualTo(2));
    }

    [Test]
    public void WriteString_DeduplicatesDuplicateInputItemsAndCombinesReferenceCounts()
    {
        XlsxSharedStringTablePart part = new XlsxSharedStringTablePart();
        part.Items.Add(CreateItem("Same", 2));
        part.Items.Add(CreateItem("Same", 3));
        part.Items.Add(CreateItem("Other", 0));

        XlsxSharedStringTableWriteResult result = new XlsxSharedStringTableWriter().WriteString(part);
        XlsxSharedStringTablePart roundTrip = new XlsxSharedStringTableReader().ReadString(result.Xml);

        Assert.That(result.IsValid, Is.True);
        Assert.That(result.Count, Is.EqualTo(6));
        Assert.That(result.UniqueCount, Is.EqualTo(2));
        Assert.That(result.Xml, Does.Contain("count=\"6\""));
        Assert.That(result.Xml, Does.Contain("uniqueCount=\"2\""));
        Assert.That(roundTrip.Items.Count, Is.EqualTo(2));
        Assert.That(roundTrip.GetText(0), Is.EqualTo("Same"));
        Assert.That(roundTrip.GetText(1), Is.EqualTo("Other"));
    }

    [Test]
    public void WriteBytes_RejectsNullPartItemsAndRichTextRuns()
    {
        XlsxSharedStringTableWriteResult? nullPart = new XlsxSharedStringTableWriter().WriteBytes(null!);
        XlsxSharedStringTablePart nullItemsPart = new XlsxSharedStringTablePart();
        nullItemsPart.Items = null!;
        XlsxSharedStringTablePart nullItemPart = new XlsxSharedStringTablePart();
        nullItemPart.Items.Add(null!);
        XlsxSharedStringTablePart nullRunsPart = new XlsxSharedStringTablePart();
        XlsxSharedStringItem rich = new XlsxSharedStringItem();
        rich.IsRichText = true;
        rich.RichTextRuns = null!;
        nullRunsPart.Items.Add(rich);

        XlsxSharedStringTableWriteResult nullItems = new XlsxSharedStringTableWriter().WriteBytes(nullItemsPart);
        XlsxSharedStringTableWriteResult nullItem = new XlsxSharedStringTableWriter().WriteBytes(nullItemPart);
        XlsxSharedStringTableWriteResult nullRuns = new XlsxSharedStringTableWriter().WriteBytes(nullRunsPart);

        Assert.That(nullPart.IsValid, Is.False);
        Assert.That(HasDiagnostic(nullPart, "sharedStringTableWriter.partNull"), Is.True);
        Assert.That(nullItems.IsValid, Is.False);
        Assert.That(HasDiagnostic(nullItems, "sharedStringTableWriter.itemsNull"), Is.True);
        Assert.That(nullItem.IsValid, Is.False);
        Assert.That(HasDiagnostic(nullItem, "sharedStringTableWriter.itemNull"), Is.True);
        Assert.That(nullRuns.IsValid, Is.False);
        Assert.That(HasDiagnostic(nullRuns, "sharedStringTableWriter.richTextRunsNull"), Is.True);
    }

    static XlsxSharedStringItem CreateItem(string text, int referenceCount)
    {
        XlsxSharedStringItem item = new XlsxSharedStringItem();
        item.Text = text;
        item.ReferenceCount = referenceCount;
        return item;
    }

    static XlsxSharedStringRichTextRun CreateRun(string text, string propertiesRawXml)
    {
        XlsxSharedStringRichTextRun run = new XlsxSharedStringRichTextRun();
        run.Text = text;
        run.PropertiesRawXml = propertiesRawXml;
        return run;
    }

    static XlsxXmlAttribute CreateAttribute(string namespaceUri, string prefix, string localName, string value)
    {
        XlsxXmlAttribute attribute = new XlsxXmlAttribute();
        attribute.NamespaceUri = namespaceUri;
        attribute.Prefix = prefix;
        attribute.LocalName = localName;
        attribute.Value = value;
        return attribute;
    }

    static bool HasDiagnostic(XlsxSharedStringTableWriteResult result, string code)
    {
        foreach (XlsxDiagnostic diagnostic in result.Validation.Diagnostics)
        {
            if (diagnostic.Code == code)
            {
                return true;
            }
        }

        return false;
    }
}
