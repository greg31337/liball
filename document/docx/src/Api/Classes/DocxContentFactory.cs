namespace docx;

/// <summary>
/// Creates editable WordprocessingML content without requiring raw XML.
/// </summary>
public class DocxContentFactory : IDocxContentFactory
{
    /// <summary>
    /// Creates a content factory.
    /// </summary>
    public DocxContentFactory()
    {
    }

    /// <summary>
    /// Creates an empty document body.
    /// </summary>
    /// <returns>The created body.</returns>
    public DocxBody CreateBody()
    {
        return DocxBody.Create();
    }

    /// <summary>
    /// Creates an empty paragraph.
    /// </summary>
    /// <returns>The created paragraph.</returns>
    public DocxParagraph CreateParagraph()
    {
        return new DocxParagraph("");
    }

    /// <summary>
    /// Creates a paragraph containing one text run.
    /// </summary>
    /// <param name="text">The paragraph text.</param>
    /// <returns>The created paragraph.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <c>text</c> is null.</exception>
    public DocxParagraph CreateTextParagraph(string text)
    {
        if (text == null)
        {
            throw new ArgumentNullException("text");
        }

        DocxParagraph paragraph = CreateParagraph();
        paragraph.InsertRun(0, CreateTextRun(text));
        return paragraph;
    }

    /// <summary>
    /// Creates an empty run.
    /// </summary>
    /// <returns>The created run.</returns>
    public DocxRun CreateRun()
    {
        return new DocxRun("");
    }

    /// <summary>
    /// Creates a run containing one text node.
    /// </summary>
    /// <param name="text">The run text.</param>
    /// <returns>The created run.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <c>text</c> is null.</exception>
    public DocxRun CreateTextRun(string text)
    {
        if (text == null)
        {
            throw new ArgumentNullException("text");
        }

        DocxRun run = CreateRun();
        run.InsertTextNode(0, CreateText(text));
        return run;
    }

    /// <summary>
    /// Creates a text node and selects XML space preservation when needed.
    /// </summary>
    /// <param name="value">The text value.</param>
    /// <returns>The created text node.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <c>value</c> is null.</exception>
    public DocxText CreateText(string value)
    {
        return DocxText.Create(value);
    }

    /// <summary>
    /// Creates a table with the requested dimensions.
    /// </summary>
    /// <param name="rows">The number of rows.</param>
    /// <param name="columns">The number of columns per row.</param>
    /// <returns>The created table.</returns>
    public DocxTable CreateTable(int rows, int columns)
    {
        return DocxTable.Create(rows, columns);
    }

    /// <summary>
    /// Creates a body block containing a paragraph.
    /// </summary>
    /// <param name="paragraph">The paragraph to wrap.</param>
    /// <returns>The created body block.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <c>paragraph</c> is null.</exception>
    public DocxBlock CreateParagraphBlock(DocxParagraph paragraph)
    {
        return DocxBlock.FromParagraph(paragraph);
    }

    /// <summary>
    /// Creates a body block containing a table.
    /// </summary>
    /// <param name="table">The table to wrap.</param>
    /// <returns>The created body block.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <c>table</c> is null.</exception>
    public DocxBlock CreateTableBlock(DocxTable table)
    {
        return DocxBlock.FromTable(table);
    }

    /// <summary>
    /// Creates a body block containing a content control.
    /// </summary>
    /// <param name="contentControl">The content control to wrap.</param>
    /// <returns>The created body block.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <c>contentControl</c> is null.</exception>
    public DocxBlock CreateContentControlBlock(DocxContentControl contentControl)
    {
        return DocxBlock.FromContentControl(contentControl);
    }

    /// <summary>
    /// Creates a hyperlink to a bookmark anchor with one display-text run.
    /// </summary>
    /// <param name="anchor">The bookmark name.</param>
    /// <param name="displayText">The displayed text.</param>
    /// <returns>The created hyperlink.</returns>
    /// <exception cref="ArgumentNullException">Thrown when a required argument is null.</exception>
    /// <exception cref="ArgumentException">Thrown when <c>anchor</c> is empty.</exception>
    public DocxHyperlink CreateAnchorHyperlink(string anchor, string displayText)
    {
        return DocxHyperlink.CreateAnchor(anchor, displayText);
    }

    /// <summary>
    /// Creates a hyperlink for an existing package relationship with one display-text run.
    /// </summary>
    /// <param name="relationshipId">The hyperlink relationship identifier.</param>
    /// <param name="displayText">The displayed text.</param>
    /// <returns>The created hyperlink.</returns>
    /// <exception cref="ArgumentNullException">Thrown when a required argument is null.</exception>
    /// <exception cref="ArgumentException">Thrown when <c>relationshipId</c> is empty.</exception>
    public DocxHyperlink CreateRelationshipHyperlink(string relationshipId, string displayText)
    {
        return DocxHyperlink.CreateRelationshipLink(relationshipId, displayText);
    }

    /// <summary>
    /// Creates a bookmark start marker.
    /// </summary>
    /// <param name="id">The bookmark identifier.</param>
    /// <param name="name">The bookmark name.</param>
    /// <returns>The created bookmark start marker.</returns>
    /// <exception cref="ArgumentNullException">Thrown when a required argument is null.</exception>
    /// <exception cref="ArgumentException">Thrown when an argument is empty.</exception>
    public DocxBookmark CreateBookmarkStart(string id, string name)
    {
        return DocxBookmark.CreateStart(id, name);
    }

    /// <summary>
    /// Creates a bookmark end marker.
    /// </summary>
    /// <param name="id">The bookmark identifier shared with its start marker.</param>
    /// <returns>The created bookmark end marker.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <c>id</c> is null.</exception>
    /// <exception cref="ArgumentException">Thrown when <c>id</c> is empty.</exception>
    public DocxBookmark CreateBookmarkEnd(string id)
    {
        return DocxBookmark.CreateEnd(id);
    }

    /// <summary>
    /// Creates a simple field with one result-text run.
    /// </summary>
    /// <param name="instruction">The field instruction.</param>
    /// <param name="resultText">The displayed field result.</param>
    /// <returns>The created simple field.</returns>
    /// <exception cref="ArgumentNullException">Thrown when a required argument is null.</exception>
    /// <exception cref="ArgumentException">Thrown when <c>instruction</c> is empty.</exception>
    public DocxField CreateSimpleField(string instruction, string resultText)
    {
        return DocxField.Create(instruction, resultText);
    }

    /// <summary>
    /// Creates a block-level content control containing one paragraph value.
    /// </summary>
    /// <param name="value">The content-control value.</param>
    /// <returns>The created content control.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <c>value</c> is null.</exception>
    public DocxContentControl CreateBlockContentControl(string value)
    {
        if (value == null)
        {
            throw new ArgumentNullException("value");
        }

        DocxContentControl contentControl = new DocxContentControl("");
        contentControl.ContentXml = DocxContentControl.CreateParagraphValueContentXml(value);
        contentControl.LoadContentFromXml(contentControl.ContentXml);
        return contentControl;
    }

    /// <summary>
    /// Creates an inline content control containing one run value.
    /// </summary>
    /// <param name="value">The content-control value.</param>
    /// <returns>The created content control.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <c>value</c> is null.</exception>
    public DocxContentControl CreateInlineContentControl(string value)
    {
        if (value == null)
        {
            throw new ArgumentNullException("value");
        }

        DocxContentControl contentControl = new DocxContentControl("");
        contentControl.ContentXml = DocxContentControl.CreateRunValueContentXml(value);
        contentControl.LoadContentFromXml(contentControl.ContentXml);
        return contentControl;
    }

    /// <summary>
    /// Creates a tab control.
    /// </summary>
    /// <returns>The created tab.</returns>
    public DocxBreak CreateTab()
    {
        return DocxBreak.CreateTab();
    }

    /// <summary>
    /// Creates a carriage-return control.
    /// </summary>
    /// <returns>The created carriage return.</returns>
    public DocxBreak CreateCarriageReturn()
    {
        return DocxBreak.CreateCarriageReturn();
    }

    /// <summary>
    /// Creates a line break.
    /// </summary>
    /// <returns>The created line break.</returns>
    public DocxBreak CreateLineBreak()
    {
        return DocxBreak.CreateLineBreak();
    }

    /// <summary>
    /// Creates a page break.
    /// </summary>
    /// <returns>The created page break.</returns>
    public DocxBreak CreatePageBreak()
    {
        return DocxBreak.CreatePageBreak();
    }

    /// <summary>
    /// Creates a column break.
    /// </summary>
    /// <returns>The created column break.</returns>
    public DocxBreak CreateColumnBreak()
    {
        return DocxBreak.CreateColumnBreak();
    }

    /// <summary>
    /// Creates a soft-hyphen control.
    /// </summary>
    /// <returns>The created soft hyphen.</returns>
    public DocxBreak CreateSoftHyphen()
    {
        return DocxBreak.CreateSoftHyphen();
    }

    /// <summary>
    /// Creates a non-breaking-hyphen control.
    /// </summary>
    /// <returns>The created non-breaking hyphen.</returns>
    public DocxBreak CreateNoBreakHyphen()
    {
        return DocxBreak.CreateNoBreakHyphen();
    }

    /// <summary>
    /// Creates a symbol control.
    /// </summary>
    /// <param name="font">The symbol font.</param>
    /// <param name="character">The hexadecimal symbol character.</param>
    /// <returns>The created symbol.</returns>
    /// <exception cref="ArgumentNullException">Thrown when a required argument is null.</exception>
    public DocxBreak CreateSymbol(string font, string character)
    {
        return DocxBreak.CreateSymbol(font, character);
    }
}
