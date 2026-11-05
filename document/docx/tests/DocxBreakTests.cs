using NUnit.Framework;

namespace docx.Tests;

[TestFixture]
public class DocxBreakTests
{
    [Test]
    public void RunParsing_ReadsTabsBreaksSymbolsAndHyphens()
    {
        global::docx.DocxRun run = ParseRun("<w:r xmlns:w=\"http://schemas.openxmlformats.org/wordprocessingml/2006/main\"><w:tab /><w:br /><w:br w:type=\"page\" /><w:br w:type=\"column\" /><w:sym w:font=\"Wingdings\" w:char=\"F0FC\" /><w:softHyphen /><w:noBreakHyphen /><w:drawing><w:br /></w:drawing></w:r>");

        Assert.That(run.ChildCount(), Is.EqualTo(8));
        Assert.That(run.BreakCount(), Is.EqualTo(7));
        Assert.That(run.GetBreak(0).Kind, Is.EqualTo(global::docx.DocxBreak.KindTab));
        Assert.That(run.GetBreak(1).Kind, Is.EqualTo(global::docx.DocxBreak.KindLineBreak));
        Assert.That(run.GetBreak(2).Kind, Is.EqualTo(global::docx.DocxBreak.KindPageBreak));
        Assert.That(run.GetBreak(2).Type, Is.EqualTo("page"));
        Assert.That(run.GetBreak(3).Kind, Is.EqualTo(global::docx.DocxBreak.KindColumnBreak));
        Assert.That(run.GetBreak(4).Kind, Is.EqualTo(global::docx.DocxBreak.KindSymbol));
        Assert.That(run.GetBreak(4).Font, Is.EqualTo("Wingdings"));
        Assert.That(run.GetBreak(4).Character, Is.EqualTo("F0FC"));
        Assert.That(run.GetBreak(5).Kind, Is.EqualTo(global::docx.DocxBreak.KindSoftHyphen));
        Assert.That(run.GetBreak(6).Kind, Is.EqualTo(global::docx.DocxBreak.KindNoBreakHyphen));
    }

    [Test]
    public void WriteXml_WritesTabsBreaksSymbolsAndHyphens()
    {
        AssertWrittenElement(global::docx.DocxBreak.CreateTab(), "tab", "");
        AssertWrittenElement(global::docx.DocxBreak.CreateLineBreak(), "br", "");
        AssertWrittenElement(global::docx.DocxBreak.CreatePageBreak(), "br", "page");
        AssertWrittenElement(global::docx.DocxBreak.CreateColumnBreak(), "br", "column");
        AssertWrittenElement(global::docx.DocxBreak.CreateSoftHyphen(), "softHyphen", "");
        AssertWrittenElement(global::docx.DocxBreak.CreateNoBreakHyphen(), "noBreakHyphen", "");

        global::docx.DocxBreak symbol = ParseBreak(global::docx.DocxBreak.CreateSymbol("Wingdings", "F0FC").WriteXml());

        Assert.That(symbol.Kind, Is.EqualTo(global::docx.DocxBreak.KindSymbol));
        Assert.That(symbol.Font, Is.EqualTo("Wingdings"));
        Assert.That(symbol.Character, Is.EqualTo("F0FC"));
    }

    [Test]
    public void RunWriting_WritesAddedControlsInChildOrder()
    {
        global::docx.DocxRun run = new global::docx.DocxRun("");

        run.AddText(global::docx.DocxText.Create("A"));
        run.AddBreak(global::docx.DocxBreak.CreateTab());
        run.AddText(global::docx.DocxText.Create("B"));
        run.AddBreak(global::docx.DocxBreak.CreatePageBreak());
        run.AddBreak(global::docx.DocxBreak.CreateSymbol("Wingdings", "F0FC"));

        global::docx.DocxRun parsed = ParseRun(run.WriteXml());

        Assert.That(parsed.ChildCount(), Is.EqualTo(5));
        Assert.That(parsed.TextCount(), Is.EqualTo(2));
        Assert.That(parsed.BreakCount(), Is.EqualTo(3));
        Assert.That(parsed.GetChildXml(0), Does.Contain(">A<"));
        Assert.That(parsed.GetChildXml(1), Does.Contain("tab"));
        Assert.That(parsed.GetChildXml(2), Does.Contain(">B<"));
        Assert.That(parsed.GetBreak(1).Kind, Is.EqualTo(global::docx.DocxBreak.KindPageBreak));
        Assert.That(parsed.GetBreak(2).Kind, Is.EqualTo(global::docx.DocxBreak.KindSymbol));
    }

    [Test]
    public void RunTextExtraction_UsesConfiguredRunControlSeparators()
    {
        global::docx.DocxRun run = ParseRun("<w:r xmlns:w=\"http://schemas.openxmlformats.org/wordprocessingml/2006/main\"><w:t>A</w:t><w:tab /><w:t>B</w:t><w:br /><w:t>C</w:t><w:br w:type=\"page\" /><w:t>D</w:t><w:br w:type=\"column\" /><w:t>E</w:t><w:sym w:font=\"Wingdings\" w:char=\"F0FC\" /><w:softHyphen /><w:noBreakHyphen /><w:t>F</w:t></w:r>");
        global::docx.DocxTextOptions options = new global::docx.DocxTextOptions();

        Assert.That(run.GetText(), Is.EqualTo("A\tB\nC\nD\nE-F"));
        Assert.That(run.GetBreak(0).GetText(), Is.EqualTo("\t"));
        Assert.That(run.GetBreak(6).GetText(), Is.EqualTo("-"));

        options.TabSeparator = "<tab>";
        options.LineBreakSeparator = "<line>";
        options.PageBreakSeparator = "<page>";
        options.ColumnBreakSeparator = "<column>";
        options.SymbolText = "<symbol>";
        options.SoftHyphenText = "<soft>";
        options.NoBreakHyphenText = "<nobr>";

        Assert.That(run.GetTextWithOptions(options), Is.EqualTo("A<tab>B<line>C<page>D<column>E<symbol><soft><nobr>F"));
        Assert.That(run.GetBreak(1).GetTextWithOptions(options), Is.EqualTo("<line>"));
        Assert.That(run.GetBreak(2).GetTextWithOptions(options), Is.EqualTo("<page>"));
        Assert.That(run.GetBreak(3).GetTextWithOptions(options), Is.EqualTo("<column>"));
        Assert.That(run.GetBreak(4).GetTextWithOptions(options), Is.EqualTo("<symbol>"));
        Assert.That(run.GetBreak(5).GetTextWithOptions(options), Is.EqualTo("<soft>"));
        Assert.That(run.GetBreak(6).GetTextWithOptions(options), Is.EqualTo("<nobr>"));
    }

    [Test]
    public void BreakParsing_RejectsInvalidArguments()
    {
        global::docx.DocxXmlReader reader = global::docx.DocxXmlReader.Create("<root />");
        global::docx.DocxBreak tab = global::docx.DocxBreak.CreateTab();
        global::docx.DocxRun run = new global::docx.DocxRun("");

        Assert.Throws<ArgumentNullException>(() => new global::docx.DocxBreak(null!, "tab", "", "", ""));
        Assert.Throws<ArgumentNullException>(() => new global::docx.DocxBreak("", null!, "", "", ""));
        Assert.Throws<ArgumentNullException>(() => new global::docx.DocxBreak("", "tab", null!, "", ""));
        Assert.Throws<ArgumentNullException>(() => new global::docx.DocxBreak("", "tab", "", null!, ""));
        Assert.Throws<ArgumentNullException>(() => new global::docx.DocxBreak("", "tab", "", "", null!));
        Assert.Throws<ArgumentException>(() => new global::docx.DocxBreak("", "", "", "", ""));
        Assert.Throws<ArgumentNullException>(() => global::docx.DocxBreak.Parse(null!));
        Assert.Throws<ArgumentNullException>(() => global::docx.DocxBreak.ParseXml(null!));
        Assert.Throws<ArgumentNullException>(() => global::docx.DocxBreak.IsSupported(null!));
        Assert.Throws<ArgumentNullException>(() => global::docx.DocxBreak.CreateSymbol(null!, "F0FC"));
        Assert.Throws<ArgumentNullException>(() => global::docx.DocxBreak.CreateSymbol("Wingdings", null!));
        Assert.Throws<ArgumentNullException>(() => tab.WriteTo(null!));
        Assert.Throws<ArgumentNullException>(() => tab.WriteBreakTo(null!));
        Assert.Throws<ArgumentNullException>(() => tab.WriteSymbolTo(null!));
        Assert.Throws<ArgumentNullException>(() => tab.GetTextWithOptions(null!));
        Assert.That(global::docx.DocxBreak.TextOrEmpty(null!), Is.EqualTo(""));
        Assert.Throws<ArgumentNullException>(() => run.AddBreak(null!));
        Assert.Throws<ArgumentNullException>(() => global::docx.DocxRun.DocxBreakFromBuffer(null!));
        Assert.Throws<InvalidOperationException>(() => global::docx.DocxBreak.ParseXml("<root />"));

        Assert.That(reader.ReadToElement("root", ""), Is.True);
        Assert.That(global::docx.DocxBreak.IsSupported(reader), Is.False);
        Assert.Throws<InvalidOperationException>(() => global::docx.DocxBreak.Parse(reader));
    }

    static void AssertWrittenElement(global::docx.DocxBreak value, string elementName, string type)
    {
        global::docx.DocxBreak parsed = ParseBreak(value.WriteXml());

        Assert.That(parsed.GetXml(), Does.Contain(elementName));

        if (type != "")
        {
            Assert.That(parsed.Type, Is.EqualTo(type));
        }
    }

    static global::docx.DocxBreak ParseBreak(string xml)
    {
        global::docx.DocxXmlReader reader = global::docx.DocxXmlReader.Create(xml);

        Assert.That(reader.MoveToContent(), Is.True);

        return global::docx.DocxBreak.Parse(reader);
    }

    static global::docx.DocxRun ParseRun(string xml)
    {
        global::docx.DocxXmlReader reader = global::docx.DocxXmlReader.Create(xml);

        Assert.That(reader.ReadToElement("r", global::docx.DocxConstants.NamespaceWordprocessingMl), Is.True);

        return global::docx.DocxRun.Parse(reader);
    }
}
