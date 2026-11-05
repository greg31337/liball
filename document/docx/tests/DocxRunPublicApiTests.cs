using NUnit.Framework;

namespace docx.Tests;

[TestFixture]
public class DocxRunPublicApiTests
{
    [Test]
    public void Navigation_ClassifiesDirectChildrenInDocumentOrder()
    {
        global::docx.DocxRun run = new global::docx.DocxRun(
            "<w:r xmlns:w=\"http://schemas.openxmlformats.org/wordprocessingml/2006/main\" xmlns:x=\"urn:test\">"
            + "<w:rPr><w:b /></w:rPr>"
            + "<w:t>Text</w:t><w:tab /><w:cr /><w:br /><w:br w:type=\"page\" />"
            + "<w:softHyphen /><w:noBreakHyphen /><w:sym w:font=\"Wingdings\" w:char=\"F0FC\" />"
            + "<w:fldChar w:fldCharType=\"begin\" /><w:instrText xml:space=\"preserve\"> PAGE </w:instrText>"
            + "<w:drawing /><w:ins w:id=\"1\"><w:t>Inserted</w:t></w:ins><x:preserved x:value=\"yes\" />"
            + "</w:r>");

        List<global::docx.DocxRunChild> children = run.GetChildren();

        Assert.That(children.Count, Is.EqualTo(13));
        Assert.That(children[0].Kind, Is.EqualTo(global::docx.DocxRunChildKind.Text));
        Assert.That(children[0].Text, Is.Not.Null);
        Assert.That(children[1].Kind, Is.EqualTo(global::docx.DocxRunChildKind.Tab));
        Assert.That(children[2].Kind, Is.EqualTo(global::docx.DocxRunChildKind.CarriageReturn));
        Assert.That(children[3].Kind, Is.EqualTo(global::docx.DocxRunChildKind.Break));
        Assert.That(children[4].Kind, Is.EqualTo(global::docx.DocxRunChildKind.Break));
        Assert.That(children[5].Kind, Is.EqualTo(global::docx.DocxRunChildKind.Hyphen));
        Assert.That(children[6].Kind, Is.EqualTo(global::docx.DocxRunChildKind.Hyphen));
        Assert.That(children[7].Kind, Is.EqualTo(global::docx.DocxRunChildKind.Symbol));
        Assert.That(children[8].Kind, Is.EqualTo(global::docx.DocxRunChildKind.FieldCharacter));
        Assert.That(children[9].Kind, Is.EqualTo(global::docx.DocxRunChildKind.FieldInstruction));
        Assert.That(children[10].Kind, Is.EqualTo(global::docx.DocxRunChildKind.Drawing));
        Assert.That(children[10].Drawing, Is.Not.Null);
        Assert.That(children[11].Kind, Is.EqualTo(global::docx.DocxRunChildKind.Revision));
        Assert.That(children[12].Kind, Is.EqualTo(global::docx.DocxRunChildKind.PreservedUnknown));

        Assert.That(run.TextCount(), Is.EqualTo(1));
        Assert.That(run.BreakCount(), Is.EqualTo(7));
        Assert.That(run.DrawingCount(), Is.EqualTo(1));
        Assert.That(run.TabCount(), Is.EqualTo(1));
        Assert.That(run.GetTab(0).Xml, Does.Contain(":tab"));
        Assert.That(run.CarriageReturnCount(), Is.EqualTo(1));
        Assert.That(run.GetCarriageReturn(0).Xml, Does.Contain(":cr"));
        Assert.That(run.BreakElementCount(), Is.EqualTo(2));
        Assert.That(run.GetBreakElement(1).Xml, Does.Contain("page"));
        Assert.That(run.HyphenCount(), Is.EqualTo(2));
        Assert.That(run.GetHyphen(0).Xml, Does.Contain("softHyphen"));
        Assert.That(run.SymbolCount(), Is.EqualTo(1));
        Assert.That(run.GetSymbol(0).Xml, Does.Contain("Wingdings"));
        Assert.That(run.FieldCharacterCount(), Is.EqualTo(1));
        Assert.That(run.GetFieldCharacter(0).Xml, Does.Contain("begin"));
        Assert.That(run.FieldInstructionCount(), Is.EqualTo(1));
        Assert.That(run.GetFieldInstruction(0).Xml, Does.Contain("PAGE"));
        Assert.That(run.PreservedUnknownChildCount(), Is.EqualTo(1));
        Assert.That(run.GetPreservedUnknownChild(0).Xml, Does.Contain("preserved"));

        children.Clear();
        run.GetTextNodes().Clear();
        run.GetBreaks().Clear();
        run.GetDrawings().Clear();

        Assert.That(run.ChildCount(), Is.EqualTo(13));
        Assert.That(run.GetTextNode(0).Value, Is.EqualTo("Text"));
        Assert.That(run.GetBreak(1).GetText(), Is.EqualTo("\n"));
        Assert.That(run.GetDrawing(0), Is.Not.Null);
    }

    [Test]
    public void CarriageReturn_UsesLineSeparatorAndRoundTrips()
    {
        global::docx.DocxRun run = new global::docx.DocxRun("<w:r xmlns:w=\"http://schemas.openxmlformats.org/wordprocessingml/2006/main\"><w:t>A</w:t><w:cr /><w:t>B</w:t></w:r>");
        global::docx.DocxTextOptions options = new global::docx.DocxTextOptions();

        options.LineBreakSeparator = "<cr>";

        Assert.That(run.GetText(), Is.EqualTo("A\nB"));
        Assert.That(run.GetTextWithOptions(options), Is.EqualTo("A<cr>B"));
        Assert.That(run.GetBreak(0).Kind, Is.EqualTo(global::docx.DocxBreak.KindCarriageReturn));
        Assert.That(new global::docx.DocxRun(run.WriteXml()).GetCarriageReturns().Count, Is.EqualTo(1));
        Assert.That(global::docx.DocxBreak.CreateCarriageReturn().WriteXml(), Does.Contain(":cr"));
    }

    [Test]
    public void GenericEditing_PreservesFormattingAndUnknownMarkup()
    {
        global::docx.DocxRun run = new global::docx.DocxRun(
            "<w:r xmlns:w=\"http://schemas.openxmlformats.org/wordprocessingml/2006/main\" xmlns:x=\"urn:test\">"
            + "<w:rPr><w:i /></w:rPr><w:t>Text</w:t><x:preserved x:value=\"yes\" /></w:r>");
        global::docx.DocxRunChild fieldCharacter = Child("<w:fldChar w:fldCharType=\"begin\" />");
        global::docx.DocxRunChild tab = Child("<w:tab />");
        global::docx.DocxRunChild symbol = Child("<w:sym w:font=\"Wingdings\" w:char=\"F0FC\" />");

        run.InsertRunChild(1, fieldCharacter);
        run.AddRunChild(tab);
        run.MoveRunChild(3, 0);
        global::docx.DocxRunChild replaced = run.ReplaceRunChild(2, symbol);
        global::docx.DocxRunChild removed = run.RemoveRunChildAt(3);

        Assert.That(replaced.Kind, Is.EqualTo(global::docx.DocxRunChildKind.FieldCharacter));
        Assert.That(removed.Kind, Is.EqualTo(global::docx.DocxRunChildKind.PreservedUnknown));
        Assert.That(run.GetChild(0).Kind, Is.EqualTo(global::docx.DocxRunChildKind.Tab));
        Assert.That(run.GetChild(1).Kind, Is.EqualTo(global::docx.DocxRunChildKind.Text));
        Assert.That(run.GetChild(2).Kind, Is.EqualTo(global::docx.DocxRunChildKind.Symbol));

        string xml = run.WriteXml();
        global::docx.DocxRun reparsed = new global::docx.DocxRun(xml);

        Assert.That(xml, Does.Contain("<w:i"));
        Assert.That(xml, Does.Contain("Wingdings"));
        Assert.That(xml, Does.Not.Contain("x:preserved"));
        Assert.That(reparsed.ChildCount(), Is.EqualTo(3));
        Assert.That(reparsed.GetText(), Is.EqualTo("\tText"));
    }

    [Test]
    public void TypedEditing_InsertsAndReplacesTypedChildren()
    {
        global::docx.DocxRun run = new global::docx.DocxRun("<w:r xmlns:w=\"http://schemas.openxmlformats.org/wordprocessingml/2006/main\" />");
        global::docx.DocxDrawing drawing = ParseDrawing("first");

        run.InsertTextNode(0, new global::docx.DocxText("", "Text", false));
        run.InsertBreak(1, global::docx.DocxBreak.CreateCarriageReturn());
        run.InsertDrawing(2, drawing);

        Assert.That(run.ReplaceChildWithBreak(0, global::docx.DocxBreak.CreateTab()).Kind, Is.EqualTo(global::docx.DocxRunChildKind.Text));
        Assert.That(run.ReplaceChildWithDrawing(1, ParseDrawing("replacement")).Kind, Is.EqualTo(global::docx.DocxRunChildKind.CarriageReturn));
        Assert.That(run.ReplaceChildWithText(2, new global::docx.DocxText("", "End", false)).Kind, Is.EqualTo(global::docx.DocxRunChildKind.Drawing));

        Assert.That(run.GetChild(0).Kind, Is.EqualTo(global::docx.DocxRunChildKind.Tab));
        Assert.That(run.GetChild(1).Kind, Is.EqualTo(global::docx.DocxRunChildKind.Drawing));
        Assert.That(run.GetChild(2).Kind, Is.EqualTo(global::docx.DocxRunChildKind.Text));
        Assert.That(run.GetText(), Is.EqualTo("\tEnd"));

        global::docx.DocxRun reparsed = new global::docx.DocxRun(run.WriteXml());

        Assert.That(reparsed.ChildCount(), Is.EqualTo(3));
        Assert.That(reparsed.GetChild(1).Xml, Does.Contain("name=\"replacement\""));
    }

    [Test]
    public void Editing_RejectsNullValuesAndInvalidPositions()
    {
        global::docx.DocxRun run = new global::docx.DocxRun("<w:r xmlns:w=\"http://schemas.openxmlformats.org/wordprocessingml/2006/main\"><w:t>Only</w:t></w:r>");

        Assert.Throws<ArgumentNullException>(() => new global::docx.DocxRunChild(null!));
        Assert.Throws<ArgumentException>(() => new global::docx.DocxRunChild(""));
        Assert.Throws<ArgumentException>(() => new global::docx.DocxRunChild(" "));
        Assert.Throws<ArgumentNullException>(() => run.AddRunChild(null!));
        Assert.Throws<ArgumentNullException>(() => run.InsertRunChild(0, null!));
        Assert.Throws<ArgumentNullException>(() => run.InsertTextNode(0, null!));
        Assert.Throws<ArgumentNullException>(() => run.InsertBreak(0, null!));
        Assert.Throws<ArgumentNullException>(() => run.InsertDrawing(0, null!));
        Assert.Throws<ArgumentNullException>(() => run.ReplaceRunChild(0, null!));
        Assert.Throws<ArgumentNullException>(() => run.ReplaceChildWithText(0, null!));
        Assert.Throws<ArgumentNullException>(() => run.ReplaceChildWithBreak(0, null!));
        Assert.Throws<ArgumentNullException>(() => run.ReplaceChildWithDrawing(0, null!));
        Assert.Throws<ArgumentOutOfRangeException>(() => run.InsertRunChild(2, Child("<w:tab />")));
        Assert.Throws<ArgumentOutOfRangeException>(() => run.MoveRunChild(0, 1));
        Assert.Throws<ArgumentOutOfRangeException>(() => run.MoveRunChild(1, 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => run.ReplaceRunChild(1, Child("<w:tab />")));
        Assert.Throws<ArgumentOutOfRangeException>(() => run.RemoveRunChildAt(1));
        Assert.Throws<ArgumentOutOfRangeException>(() => run.GetChild(1));
    }

    static global::docx.DocxRunChild Child(string xml)
    {
        int attributeStart = xml.IndexOf(' ');
        string namespacedXml = xml.Substring(0, attributeStart)
            + " xmlns:w=\"http://schemas.openxmlformats.org/wordprocessingml/2006/main\""
            + xml.Substring(attributeStart);
        return new global::docx.DocxRunChild(namespacedXml);
    }

    static global::docx.DocxDrawing ParseDrawing(string name)
    {
        global::docx.DocxRun run = new global::docx.DocxRun("<w:r xmlns:w=\"http://schemas.openxmlformats.org/wordprocessingml/2006/main\"><w:drawing name=\"" + name + "\" /></w:r>");
        global::docx.DocxDrawing drawing = run.GetDrawing(0);
        drawing.Name = name;
        return drawing;
    }
}
