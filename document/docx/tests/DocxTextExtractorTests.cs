using NUnit.Framework;

namespace docx.Tests;

[TestFixture]
public class DocxTextExtractorTests
{
    [Test]
    public void Extract_ReturnsWholeMainDocumentTextInBlockOrder()
    {
        global::docx.DocxDocument document = global::docx.DocxDocument.OpenPackage(CreatePackageWithBodyChildren(new string[]
        {
            CreateParagraphXml("One"),
            "<w:tbl><w:tr><w:tc>" + CreateParagraphXml("A") + "</w:tc><w:tc>" + CreateParagraphXml("B") + "</w:tc></w:tr></w:tbl>",
            CreateContentControlXml("Inside"),
            CreateParagraphXml("Last")
        }));

        Assert.That(document.GetText(), Is.EqualTo("One\nA\tB\nInside\nLast"));
        Assert.That(document.Body.GetText(), Is.EqualTo("One\nA\tB\nInside\nLast"));
        Assert.That(global::docx.DocxTextExtractor.Extract(document), Is.EqualTo("One\nA\tB\nInside\nLast"));
        Assert.That(global::docx.DocxTextExtractor.Extract(document.Body), Is.EqualTo("One\nA\tB\nInside\nLast"));
    }

    [Test]
    public void Extract_UsesConfiguredParagraphAndTableSeparators()
    {
        global::docx.DocxDocument document = global::docx.DocxDocument.OpenPackage(CreatePackageWithBodyChildren(new string[]
        {
            CreateParagraphXml("One"),
            "<w:tbl><w:tr><w:tc>" + CreateParagraphXml("A") + "</w:tc><w:tc>" + CreateParagraphXml("B") + "</w:tc></w:tr><w:tr><w:tc>" + CreateParagraphXml("C") + "</w:tc><w:tc>" + CreateParagraphXml("D") + "</w:tc></w:tr></w:tbl>",
            CreateParagraphXml("Last")
        }));
        global::docx.DocxTextOptions options = new global::docx.DocxTextOptions();

        options.ParagraphSeparator = "|";
        options.TableCellSeparator = ",";
        options.TableRowSeparator = "/";

        Assert.That(document.GetTextWithOptions(options), Is.EqualTo("One|A,B/C,D|Last"));
        Assert.That(global::docx.DocxTextExtractor.Extract(document, options), Is.EqualTo("One|A,B/C,D|Last"));
        Assert.That(global::docx.DocxTextExtractor.Extract(document.Body, options), Is.EqualTo("One|A,B/C,D|Last"));
    }

    [Test]
    public void Extract_UsesConfiguredRunControlText()
    {
        global::docx.DocxDocument document = global::docx.DocxDocument.OpenPackage(CreatePackageWithBodyChildren(new string[]
        {
            "<w:p><w:r><w:t>A</w:t><w:tab /><w:t>B</w:t><w:br w:type=\"page\" /><w:t>C</w:t><w:softHyphen /><w:t>D</w:t><w:noBreakHyphen /><w:t>E</w:t></w:r></w:p>"
        }));
        global::docx.DocxTextOptions options = new global::docx.DocxTextOptions();

        options.TabSeparator = "[tab]";
        options.PageBreakSeparator = "[page]";
        options.SoftHyphenText = "-";
        options.NoBreakHyphenText = "~";

        Assert.That(document.GetTextWithOptions(options), Is.EqualTo("A[tab]B[page]C-D~E"));
        Assert.That(global::docx.DocxTextExtractor.Extract(document, options), Is.EqualTo("A[tab]B[page]C-D~E"));
    }

    [Test]
    public void Extract_ComplexValidDocumentPreservesMixedContentOrderAndOptions()
    {
        global::docx.DocxDocument document = global::docx.DocxDocument.OpenPackage(CreatePackageWithBodyChildren(new string[]
        {
            "<w:p><w:pPr><w:sectPr><w:pgSz w:w=\"12240\" w:h=\"15840\" /></w:sectPr></w:pPr>"
                + CreateRunXml("Intro ")
                + "<w:hyperlink w:anchor=\"bookmark1\">" + CreateRunXml("Link") + "</w:hyperlink>"
                + "<w:fldSimple w:instr=\"DATE\">" + CreateRunXml(" 2026") + "</w:fldSimple>"
                + CreateInlineContentControlXml(" Control")
                + "<w:r><w:t> A</w:t><w:tab /><w:t>B</w:t><w:br /><w:t>C</w:t></w:r>"
                + "</w:p>",
            "<w:tbl>"
                + "<w:tblPr><w:tblW w:w=\"5000\" w:type=\"pct\" /></w:tblPr>"
                + "<w:tr>"
                + "<w:tc><w:tcPr><w:gridSpan w:val=\"2\" /></w:tcPr>" + CreateParagraphXml("R1C1-A") + CreateParagraphXml("R1C1-B") + "</w:tc>"
                + "<w:tc>" + CreateParagraphXml("R1C2-") + CreateParagraphXml("Value") + "</w:tc>"
                + "</w:tr>"
                + "<w:tr>"
                + "<w:tc><w:tbl><w:tr><w:tc>" + CreateParagraphXml("NestedA") + "</w:tc><w:tc>" + CreateParagraphXml("NestedB") + "</w:tc></w:tr></w:tbl></w:tc>"
                + "<w:tc><w:p><w:r><w:t>Tail</w:t><w:tab /><w:t>Done</w:t></w:r></w:p></w:tc>"
                + "</w:tr>"
                + "</w:tbl>",
            CreateContentControlXml("Block Control"),
            CreateParagraphXml("End")
        }));
        global::docx.DocxTextOptions options = new global::docx.DocxTextOptions();

        options.ParagraphSeparator = "|";
        options.TableCellSeparator = ",";
        options.TableRowSeparator = "/";
        options.TabSeparator = "[tab]";
        options.LineBreakSeparator = "[br]";

        Assert.That(document.Body.BlockCount(), Is.EqualTo(4));
        Assert.That(document.Body.Sections.Count, Is.EqualTo(1));
        Assert.That(document.GetTextWithOptions(options), Is.EqualTo("Intro Link 2026 Control A[tab]B[br]C|R1C1-A|R1C1-B,R1C2-|Value/NestedA,NestedB,Tail[tab]Done|Block Control|End"));

        options.IncludeFieldResults = false;

        Assert.That(global::docx.DocxTextExtractor.Extract(document, options), Is.EqualTo("Intro Link Control A[tab]B[br]C|R1C1-A|R1C1-B,R1C2-|Value/NestedA,NestedB,Tail[tab]Done|Block Control|End"));
    }

    [Test]
    public void Extract_LargeDocumentPreservesParagraphOrderAndSeparators()
    {
        int paragraphCount = 750;
        string[] children = new string[paragraphCount];
        string[] expected = new string[paragraphCount];

        for (int index = 0; index < paragraphCount; index = index + 1)
        {
            expected[index] = "Paragraph " + index.ToString();
            children[index] = CreateParagraphXml(expected[index]);
        }

        global::docx.DocxDocument document = global::docx.DocxDocument.OpenPackage(CreatePackageWithBodyChildren(children));
        string extracted = document.GetText();
        string expectedText = string.Join("\n", expected);

        Assert.That(extracted, Is.EqualTo(expectedText));
        Assert.That(global::docx.DocxTextExtractor.Extract(document), Is.EqualTo(expectedText));
        Assert.That(global::docx.DocxTextExtractor.Extract(document.Body), Is.EqualTo(expectedText));
        Assert.That(extracted.StartsWith("Paragraph 0\nParagraph 1\nParagraph 2"), Is.True);
        Assert.That(extracted.EndsWith("\nParagraph 749"), Is.True);
    }

    [Test]
    public void Extract_LargeDocumentTablesUseConfiguredSeparators()
    {
        int rowCount = 40;
        int columnCount = 8;
        global::docx.DocxDocument document = global::docx.DocxDocument.OpenPackage(CreatePackageWithBodyChildren(new string[]
        {
            CreateParagraphXml("Before"),
            CreateTableXml(rowCount, columnCount),
            CreateParagraphXml("After")
        }));
        global::docx.DocxTextOptions options = new global::docx.DocxTextOptions();
        string expectedText = "Before|" + CreateTableExpectedText(rowCount, columnCount, ",", "/") + "|After";

        options.ParagraphSeparator = "|";
        options.TableCellSeparator = ",";
        options.TableRowSeparator = "/";

        Assert.That(document.GetTextWithOptions(options), Is.EqualTo(expectedText));
        Assert.That(global::docx.DocxTextExtractor.Extract(document, options), Is.EqualTo(expectedText));
        Assert.That(global::docx.DocxTextExtractor.Extract(document.Body, options), Is.EqualTo(expectedText));
    }

    [Test]
    public void ExtractStoryText_ReadsTablesHeadersFootersNotesAndComments()
    {
        global::docx.DocxTextOptions options = new global::docx.DocxTextOptions();
        global::docx.DocxTable table = ParseTable("<w:tbl xmlns:w=\"http://schemas.openxmlformats.org/wordprocessingml/2006/main\"><w:tr><w:tc>" + CreateParagraphXml("A") + "</w:tc><w:tc>" + CreateParagraphXml("B") + "</w:tc></w:tr></w:tbl>");
        global::docx.DocxHeader header = new global::docx.DocxHeader("<w:hdr xmlns:w=\"http://schemas.openxmlformats.org/wordprocessingml/2006/main\">" + CreateParagraphXml("Header") + "<w:tbl><w:tr><w:tc>" + CreateParagraphXml("H1") + "</w:tc><w:tc>" + CreateParagraphXml("H2") + "</w:tc></w:tr></w:tbl></w:hdr>");
        global::docx.DocxFooter footer = new global::docx.DocxFooter("<w:ftr xmlns:w=\"http://schemas.openxmlformats.org/wordprocessingml/2006/main\">" + CreateParagraphXml("Footer") + "</w:ftr>");
        global::docx.DocxFootnotes footnotes = global::docx.DocxFootnotes.ParseXml("<w:footnotes xmlns:w=\"http://schemas.openxmlformats.org/wordprocessingml/2006/main\"><w:footnote w:id=\"1\">" + CreateParagraphXml("Foot A") + "</w:footnote><w:footnote w:id=\"2\">" + CreateParagraphXml("Foot B") + "</w:footnote></w:footnotes>");
        global::docx.DocxEndnotes endnotes = global::docx.DocxEndnotes.ParseXml("<w:endnotes xmlns:w=\"http://schemas.openxmlformats.org/wordprocessingml/2006/main\"><w:endnote w:id=\"1\">" + CreateParagraphXml("End A") + "</w:endnote><w:endnote w:id=\"2\">" + CreateParagraphXml("End B") + "</w:endnote></w:endnotes>");
        global::docx.DocxComments comments = global::docx.DocxComments.ParseXml("<w:comments xmlns:w=\"http://schemas.openxmlformats.org/wordprocessingml/2006/main\"><w:comment w:id=\"1\" w:author=\"A\">" + CreateParagraphXml("Comment A") + "</w:comment><w:comment w:id=\"2\" w:author=\"B\">" + CreateParagraphXml("Comment B") + "</w:comment></w:comments>");

        options.ParagraphSeparator = "|";
        options.TableCellSeparator = ",";

        Assert.That(global::docx.DocxTextExtractor.ExtractTable(table, options), Is.EqualTo("A,B"));
        Assert.That(header.GetTextWithOptions(options), Is.EqualTo("Header|H1,H2"));
        Assert.That(global::docx.DocxTextExtractor.ExtractHeader(header, options), Is.EqualTo("Header|H1,H2"));
        Assert.That(footer.GetTextWithOptions(options), Is.EqualTo("Footer"));
        Assert.That(global::docx.DocxTextExtractor.ExtractFooter(footer, options), Is.EqualTo("Footer"));
        Assert.That(footnotes.GetTextWithOptions(options), Is.EqualTo("Foot A|Foot B"));
        Assert.That(footnotes.GetFootnote(0).GetTextWithOptions(options), Is.EqualTo("Foot A"));
        Assert.That(global::docx.DocxTextExtractor.ExtractFootnotes(footnotes, options), Is.EqualTo("Foot A|Foot B"));
        Assert.That(global::docx.DocxTextExtractor.ExtractFootnote(footnotes.GetFootnote(1), options), Is.EqualTo("Foot B"));
        Assert.That(endnotes.GetTextWithOptions(options), Is.EqualTo("End A|End B"));
        Assert.That(endnotes.GetEndnote(0).GetTextWithOptions(options), Is.EqualTo("End A"));
        Assert.That(global::docx.DocxTextExtractor.ExtractEndnotes(endnotes, options), Is.EqualTo("End A|End B"));
        Assert.That(global::docx.DocxTextExtractor.ExtractEndnote(endnotes.GetEndnote(1), options), Is.EqualTo("End B"));
        Assert.That(comments.GetTextWithOptions(options), Is.EqualTo("Comment A|Comment B"));
        Assert.That(comments.GetComment(0).GetTextWithOptions(options), Is.EqualTo("Comment A"));
        Assert.That(global::docx.DocxTextExtractor.ExtractComments(comments, options), Is.EqualTo("Comment A|Comment B"));
        Assert.That(global::docx.DocxTextExtractor.ExtractComment(comments.GetComment(1), options), Is.EqualTo("Comment B"));
    }

    [Test]
    public void Extract_ReturnsEmptyTextForMinimalDocument()
    {
        global::docx.DocxDocument document = global::docx.DocxDocument.Create();

        Assert.That(document.GetText(), Is.EqualTo(""));
        Assert.That(document.Body.GetText(), Is.EqualTo(""));
        Assert.That(global::docx.DocxTextExtractor.Extract(document), Is.EqualTo(""));
    }

    [Test]
    public void Extract_RejectsInvalidArguments()
    {
        global::docx.DocxDocument document = global::docx.DocxDocument.Create();
        global::docx.DocxBody body = global::docx.DocxBody.Create();
        global::docx.DocxTextOptions options = new global::docx.DocxTextOptions();
        global::docx.DocxTable table = new global::docx.DocxTable("");
        global::docx.DocxHeader header = new global::docx.DocxHeader("");
        global::docx.DocxFooter footer = new global::docx.DocxFooter("");
        global::docx.DocxFootnotes footnotes = global::docx.DocxFootnotes.Create();
        global::docx.DocxFootnote footnote = new global::docx.DocxFootnote("");
        global::docx.DocxEndnotes endnotes = global::docx.DocxEndnotes.Create();
        global::docx.DocxEndnote endnote = new global::docx.DocxEndnote("");
        global::docx.DocxComments comments = global::docx.DocxComments.Create();
        global::docx.DocxComment comment = new global::docx.DocxComment("");

        Assert.Throws<ArgumentNullException>(() => document.GetTextWithOptions(null!));
        Assert.Throws<ArgumentNullException>(() => body.GetTextWithOptions(null!));
        Assert.Throws<ArgumentNullException>(() => global::docx.DocxTextExtractor.Extract((global::docx.DocxDocument)null!));
        Assert.Throws<ArgumentNullException>(() => global::docx.DocxTextExtractor.Extract(document, null!));
        Assert.Throws<ArgumentNullException>(() => global::docx.DocxTextExtractor.Extract((global::docx.DocxBody)null!));
        Assert.Throws<ArgumentNullException>(() => global::docx.DocxTextExtractor.Extract(body, null!));
        Assert.Throws<ArgumentNullException>(() => global::docx.DocxTextExtractor.ExtractTable(null!));
        Assert.Throws<ArgumentNullException>(() => global::docx.DocxTextExtractor.ExtractTable(table, null!));
        Assert.Throws<ArgumentNullException>(() => header.GetTextWithOptions(null!));
        Assert.Throws<ArgumentNullException>(() => global::docx.DocxTextExtractor.ExtractHeader(null!));
        Assert.Throws<ArgumentNullException>(() => global::docx.DocxTextExtractor.ExtractHeader(header, null!));
        Assert.Throws<ArgumentNullException>(() => footer.GetTextWithOptions(null!));
        Assert.Throws<ArgumentNullException>(() => global::docx.DocxTextExtractor.ExtractFooter(null!));
        Assert.Throws<ArgumentNullException>(() => global::docx.DocxTextExtractor.ExtractFooter(footer, null!));
        Assert.Throws<ArgumentNullException>(() => footnotes.GetTextWithOptions(null!));
        Assert.Throws<ArgumentNullException>(() => global::docx.DocxTextExtractor.ExtractFootnotes(null!));
        Assert.Throws<ArgumentNullException>(() => global::docx.DocxTextExtractor.ExtractFootnotes(footnotes, null!));
        Assert.Throws<ArgumentNullException>(() => footnote.GetTextWithOptions(null!));
        Assert.Throws<ArgumentNullException>(() => global::docx.DocxTextExtractor.ExtractFootnote(null!));
        Assert.Throws<ArgumentNullException>(() => global::docx.DocxTextExtractor.ExtractFootnote(footnote, null!));
        Assert.Throws<ArgumentNullException>(() => endnotes.GetTextWithOptions(null!));
        Assert.Throws<ArgumentNullException>(() => global::docx.DocxTextExtractor.ExtractEndnotes(null!));
        Assert.Throws<ArgumentNullException>(() => global::docx.DocxTextExtractor.ExtractEndnotes(endnotes, null!));
        Assert.Throws<ArgumentNullException>(() => endnote.GetTextWithOptions(null!));
        Assert.Throws<ArgumentNullException>(() => global::docx.DocxTextExtractor.ExtractEndnote(null!));
        Assert.Throws<ArgumentNullException>(() => global::docx.DocxTextExtractor.ExtractEndnote(endnote, null!));
        Assert.Throws<ArgumentNullException>(() => comments.GetTextWithOptions(null!));
        Assert.Throws<ArgumentNullException>(() => global::docx.DocxTextExtractor.ExtractComments(null!));
        Assert.Throws<ArgumentNullException>(() => global::docx.DocxTextExtractor.ExtractComments(comments, null!));
        Assert.Throws<ArgumentNullException>(() => comment.GetTextWithOptions(null!));
        Assert.Throws<ArgumentNullException>(() => global::docx.DocxTextExtractor.ExtractComment(null!));
        Assert.Throws<ArgumentNullException>(() => global::docx.DocxTextExtractor.ExtractComment(comment, null!));
        Assert.Throws<ArgumentNullException>(() => global::docx.DocxTextExtractor.ExtractBlocks(null!, options));
        Assert.Throws<ArgumentNullException>(() => global::docx.DocxTextExtractor.ExtractBlocks(new List<global::docx.DocxBlock>(), null!));
    }

    static global::docx.DocxPackage CreatePackageWithBodyChildren(string[] children)
    {
        global::docx.DocxPackage package = new global::docx.DocxPackage();
        global::docx.DocxRelationshipSet rootRelationships = global::docx.DocxPackage.CreateRootRelationshipSet("/word/document.xml");

        package.ContentTypes.SetDefault("xml", global::docx.DocxConstants.ContentTypeXml);
        package.AddXmlPartWithContent("/word/document.xml", global::docx.DocxConstants.ContentTypeMainDocument, CreateDocumentXml(children));
        package.AddXmlPartWithContent("_rels/.rels", global::docx.DocxConstants.ContentTypeRelationships, rootRelationships.WriteXml());

        return package;
    }

    static string CreateDocumentXml(string[] bodyChildren)
    {
        return "<w:document xmlns:w=\"http://schemas.openxmlformats.org/wordprocessingml/2006/main\"><w:body>" + string.Join("", bodyChildren) + "</w:body></w:document>";
    }

    static string CreateParagraphXml(string text)
    {
        return "<w:p><w:r><w:t>" + text + "</w:t></w:r></w:p>";
    }

    static string CreateRunXml(string text)
    {
        return "<w:r><w:t>" + text + "</w:t></w:r>";
    }

    static string CreateContentControlXml(string text)
    {
        return "<w:sdt><w:sdtContent>" + CreateParagraphXml(text) + "</w:sdtContent></w:sdt>";
    }

    static string CreateInlineContentControlXml(string text)
    {
        return "<w:sdt><w:sdtContent>" + CreateRunXml(text) + "</w:sdtContent></w:sdt>";
    }

    static string CreateTableXml(int rowCount, int columnCount)
    {
        string xml = "<w:tbl>";

        for (int rowIndex = 0; rowIndex < rowCount; rowIndex = rowIndex + 1)
        {
            xml = xml + "<w:tr>";

            for (int columnIndex = 0; columnIndex < columnCount; columnIndex = columnIndex + 1)
            {
                xml = xml + "<w:tc>" + CreateParagraphXml(CreateCellText(rowIndex, columnIndex)) + "</w:tc>";
            }

            xml = xml + "</w:tr>";
        }

        return xml + "</w:tbl>";
    }

    static string CreateTableExpectedText(int rowCount, int columnCount, string cellSeparator, string rowSeparator)
    {
        string text = "";

        for (int rowIndex = 0; rowIndex < rowCount; rowIndex = rowIndex + 1)
        {
            if (rowIndex > 0)
            {
                text = text + rowSeparator;
            }

            for (int columnIndex = 0; columnIndex < columnCount; columnIndex = columnIndex + 1)
            {
                if (columnIndex > 0)
                {
                    text = text + cellSeparator;
                }

                text = text + CreateCellText(rowIndex, columnIndex);
            }
        }

        return text;
    }

    static string CreateCellText(int rowIndex, int columnIndex)
    {
        return "R" + rowIndex.ToString() + "C" + columnIndex.ToString();
    }

    static global::docx.DocxTable ParseTable(string xml)
    {
        global::docx.DocxXmlReader reader = global::docx.DocxXmlReader.Create(xml);

        Assert.That(reader.ReadToElement("tbl", global::docx.DocxConstants.NamespaceWordprocessingMl), Is.True);

        return global::docx.DocxTable.Parse(reader);
    }
}

[TestFixture]
public class DocxTextProcessorTests
{
    [Test]
    public void ExtractDocument_SelectsStoriesAndUsesConfiguredStorySeparator()
    {
        global::docx.DocxTextProcessor processor = new global::docx.DocxTextProcessor();
        global::docx.DocxDocument document = new global::docx.DocxDocument(CreateStoryPackage());
        global::docx.DocxTextOptions options = new global::docx.DocxTextOptions();
        options.ParagraphSeparator = "|";

        Assert.That(processor.ExtractDocument(document, options), Is.EqualTo("Main|Header|Footer|Footnote|Endnote"));

        options.IncludeHeadersAndFooters = false;
        options.IncludeFootnotesAndEndnotes = false;
        options.IncludeComments = true;

        Assert.That(processor.ExtractDocument(document, options), Is.EqualTo("Main|Comment"));
        Assert.That(processor.GetDocumentTokens(document, options).Select(token => token.Kind), Is.EqualTo(new global::docx.DocxTextTokenKind[]
        {
            global::docx.DocxTextTokenKind.Text,
            global::docx.DocxTextTokenKind.StorySeparator,
            global::docx.DocxTextTokenKind.CompositeText
        }));
        Assert.That(processor.GetDocumentTokens(document, options)[2].StoryType, Is.EqualTo(global::docx.DocxStoryType.Comments));
    }

    [Test]
    public void ExtractContent_AppliesSeparatorsAndRevisionAndFieldOptions()
    {
        global::docx.DocxTextProcessor processor = new global::docx.DocxTextProcessor();
        global::docx.DocxTextOptions options = CreateDistinctSeparatorOptions();
        global::docx.DocxRun run = new global::docx.DocxRun(
            "<w:r xmlns:w=\"" + global::docx.DocxConstants.NamespaceWordprocessingMl
            + "\"><w:t>A</w:t><w:tab/><w:cr/><w:br/><w:br w:type=\"page\"/><w:br w:type=\"column\"/><w:softHyphen/><w:noBreakHyphen/><w:sym w:font=\"Wingdings\" w:char=\"F04A\"/></w:r>");
        global::docx.DocxParagraph paragraph = new global::docx.DocxParagraph(
            "<w:p xmlns:w=\"" + global::docx.DocxConstants.NamespaceWordprocessingMl
            + "\"><w:fldSimple w:instr=\"MERGEFIELD Name\"><w:r><w:t>Field</w:t></w:r></w:fldSimple>"
            + "<w:ins><w:r><w:t>Inserted</w:t></w:r></w:ins><w:del><w:r><w:delText>Deleted</w:delText></w:r></w:del></w:p>");

        Assert.That(processor.ExtractContent(run, options), Is.EqualTo("ATLLPOSNY"));
        Assert.That(processor.ExtractContent(paragraph, options), Is.EqualTo("FieldInserted"));

        options.IncludeFieldResults = false;
        options.IncludeInsertedText = false;
        options.IncludeDeletedText = true;

        Assert.That(processor.ExtractContent(paragraph, options), Is.EqualTo("Deleted"));
    }

    [Test]
    public void GetRunTokens_TraversesUnicodeXmlSpaceAndControlsInOrder()
    {
        global::docx.DocxTextProcessor processor = new global::docx.DocxTextProcessor();
        global::docx.DocxTextOptions options = CreateDistinctSeparatorOptions();
        global::docx.DocxRun run = new global::docx.DocxRun(
            "<w:r xmlns:w=\"" + global::docx.DocxConstants.NamespaceWordprocessingMl
            + "\"><w:t xml:space=\"preserve\"> 😀 café </w:t><w:tab/><w:cr/><w:br/><w:br w:type=\"page\"/>"
            + "<w:br w:type=\"column\"/><w:softHyphen/><w:noBreakHyphen/><w:sym w:font=\"Wingdings\" w:char=\"F04A\"/></w:r>");

        List<global::docx.DocxTextToken> tokens = processor.GetRunTokens(run, options);

        Assert.That(tokens.Select(token => token.Kind), Is.EqualTo(new global::docx.DocxTextTokenKind[]
        {
            global::docx.DocxTextTokenKind.Text,
            global::docx.DocxTextTokenKind.Tab,
            global::docx.DocxTextTokenKind.CarriageReturn,
            global::docx.DocxTextTokenKind.LineBreak,
            global::docx.DocxTextTokenKind.PageBreak,
            global::docx.DocxTextTokenKind.ColumnBreak,
            global::docx.DocxTextTokenKind.SoftHyphen,
            global::docx.DocxTextTokenKind.NoBreakHyphen,
            global::docx.DocxTextTokenKind.Symbol
        }));
        Assert.That(tokens.Select(token => token.Text), Is.EqualTo(new string[] { " 😀 café ", "T", "L", "L", "P", "O", "S", "N", "Y" }));
        Assert.That(tokens[0].PreserveSpace, Is.True);
        Assert.That(tokens[0].Run, Is.SameAs(run));
        Assert.That(tokens[0].TextNode, Is.Not.Null);
    }

    [Test]
    public void GetParagraphTokens_IdentifiesFieldAndRevisionText()
    {
        global::docx.DocxTextProcessor processor = new global::docx.DocxTextProcessor();
        global::docx.DocxTextOptions options = new global::docx.DocxTextOptions();
        global::docx.DocxParagraph paragraph = new global::docx.DocxParagraph(
            "<w:p xmlns:w=\"" + global::docx.DocxConstants.NamespaceWordprocessingMl
            + "\"><w:r><w:t>Visible</w:t></w:r><w:fldSimple w:instr=\"DATE\"><w:r><w:t>Field</w:t></w:r></w:fldSimple>"
            + "<w:ins><w:r><w:t>Inserted</w:t></w:r></w:ins><w:del><w:r><w:delText>Deleted</w:delText></w:r></w:del></w:p>");

        List<global::docx.DocxTextToken> tokens = processor.GetParagraphTokens(paragraph, options);

        Assert.That(tokens.Select(token => token.Kind), Is.EqualTo(new global::docx.DocxTextTokenKind[]
        {
            global::docx.DocxTextTokenKind.Text,
            global::docx.DocxTextTokenKind.FieldResult,
            global::docx.DocxTextTokenKind.RevisionText
        }));
        Assert.That(tokens.Select(token => token.Text), Is.EqualTo(new string[] { "Visible", "Field", "Inserted" }));

        options.IncludeFieldResults = false;
        options.IncludeInsertedText = false;
        options.IncludeDeletedText = true;

        Assert.That(processor.GetParagraphTokens(paragraph, options).Select(token => token.Text), Is.EqualTo(new string[] { "Visible", "Deleted" }));
    }

    [Test]
    public void GetBodyTokens_IgnoresPreservedBlocksWhenAddingSeparators()
    {
        global::docx.DocxTextProcessor processor = new global::docx.DocxTextProcessor();
        global::docx.DocxTextOptions options = new global::docx.DocxTextOptions();
        options.ParagraphSeparator = "|";
        global::docx.DocxBody body = new global::docx.DocxBody(
            "<w:body xmlns:w=\"" + global::docx.DocxConstants.NamespaceWordprocessingMl
            + "\" xmlns:vendor=\"urn:vendor:text\"><w:p><w:r><w:t>Before</w:t></w:r></w:p>"
            + "<vendor:unknown vendor:value=\"preserve\"/>"
            + "<w:p><w:r><w:t>After</w:t></w:r></w:p></w:body>");

        List<global::docx.DocxTextToken> tokens = processor.GetBodyTokens(body, options);

        Assert.That(tokens.Select(token => token.Kind), Is.EqualTo(new global::docx.DocxTextTokenKind[]
        {
            global::docx.DocxTextTokenKind.Text,
            global::docx.DocxTextTokenKind.ParagraphSeparator,
            global::docx.DocxTextTokenKind.Text
        }));
        Assert.That(tokens.Select(token => token.Text), Is.EqualTo(new string[] { "Before", "|", "After" }));
        Assert.That(body.GetBlock(1).IsPreserved(), Is.True);
        Assert.That(body.GetBlock(1).GetPreservedXml(), Does.Contain("vendor:value=\"preserve\""));
    }

    [Test]
    public void SearchAndReplacement_WorkAcrossRunsAndRetainRunFormatting()
    {
        global::docx.DocxTextProcessor processor = new global::docx.DocxTextProcessor();
        global::docx.DocxParagraph paragraph = new global::docx.DocxParagraph(
            "<w:p xmlns:w=\"" + global::docx.DocxConstants.NamespaceWordprocessingMl
            + "\"><w:r><w:rPr><w:b/></w:rPr><w:t>Hello {{</w:t></w:r>"
            + "<w:r><w:rPr><w:i/></w:rPr><w:t>Name}}</w:t></w:r><w:r><w:t>!</w:t></w:r></w:p>");

        List<global::docx.DocxTextMatch> matches = processor.FindInParagraph(paragraph, "{{Name}}", new global::docx.DocxTextOptions());

        Assert.That(matches, Has.Count.EqualTo(1));
        Assert.That(matches[0].StartRunIndex, Is.EqualTo(0));
        Assert.That(matches[0].EndRunIndex, Is.EqualTo(1));
        Assert.That(processor.ReplaceInParagraph(paragraph, "{{Name}}", " Ada "), Is.EqualTo(1));
        Assert.That(paragraph.GetText(), Is.EqualTo("Hello  Ada !"));
        Assert.That(paragraph.GetRun(0).GetTextNode(0).PreserveSpace, Is.True);
        Assert.That(paragraph.GetRun(0).WriteXml(), Does.Contain("w:b"));
        Assert.That(paragraph.GetRun(1).WriteXml(), Does.Contain("w:i"));
    }

    [Test]
    public void ImportPlainText_CreatesParagraphsTabsUnicodeAndTrailingEmptyLine()
    {
        global::docx.DocxTextProcessor processor = new global::docx.DocxTextProcessor();

        global::docx.DocxBody body = processor.ImportPlainText("  Alpha\t😀\r\nBeta\rGamma\n");
        List<global::docx.DocxTextToken> tokens = processor.GetBodyTokens(body, new global::docx.DocxTextOptions());

        Assert.That(body.BlockCount(), Is.EqualTo(4));
        Assert.That(body.GetText(), Is.EqualTo("  Alpha\t😀\nBeta\nGamma\n"));
        Assert.That(body.GetBlock(0).Paragraph!.GetRun(0).GetTextNode(0).PreserveSpace, Is.True);
        Assert.That(body.GetBlock(3).Paragraph!.GetText(), Is.EqualTo(""));
        Assert.That(tokens.Count(token => token.Kind == global::docx.DocxTextTokenKind.Tab), Is.EqualTo(1));
        Assert.That(tokens.Count(token => token.Kind == global::docx.DocxTextTokenKind.ParagraphSeparator), Is.EqualTo(3));

        global::docx.DocxTextOptions emptySeparatorOptions = new global::docx.DocxTextOptions();
        emptySeparatorOptions.ParagraphSeparator = null!;
        Assert.That(processor.GetBodyTokens(body, emptySeparatorOptions).Where(token => token.Kind == global::docx.DocxTextTokenKind.ParagraphSeparator).All(token => token.Text == ""), Is.True);
    }

    [Test]
    public void TextProcessor_RejectsInvalidArguments()
    {
        global::docx.DocxTextProcessor processor = new global::docx.DocxTextProcessor();
        global::docx.DocxTextOptions options = new global::docx.DocxTextOptions();
        global::docx.DocxParagraph paragraph = new global::docx.DocxParagraph("");

        Assert.Throws<ArgumentNullException>(() => processor.ExtractContent(null!, options));
        Assert.Throws<ArgumentNullException>(() => processor.ExtractContent(paragraph, null!));
        Assert.Throws<ArgumentNullException>(() => processor.GetRunTokens(null!, options));
        Assert.Throws<ArgumentNullException>(() => processor.GetParagraphTokens(null!, options));
        Assert.Throws<ArgumentNullException>(() => processor.GetBodyTokens(null!, options));
        Assert.Throws<ArgumentNullException>(() => processor.GetDocumentTokens(null!, options));
        Assert.Throws<ArgumentNullException>(() => processor.FindInParagraph(paragraph, null!, options));
        Assert.Throws<ArgumentException>(() => processor.FindInParagraph(paragraph, "", options));
        Assert.Throws<ArgumentNullException>(() => processor.ReplaceInParagraph(paragraph, "x", null!));
        Assert.Throws<ArgumentNullException>(() => processor.ImportPlainText(null!));
        Assert.Throws<ArgumentNullException>(() => new global::docx.DocxTextToken(global::docx.DocxTextTokenKind.Text, null!, false, global::docx.DocxStoryType.MainDocument, null, null, null));
    }

    static global::docx.DocxTextOptions CreateDistinctSeparatorOptions()
    {
        global::docx.DocxTextOptions options = new global::docx.DocxTextOptions();
        options.TabSeparator = "T";
        options.LineBreakSeparator = "L";
        options.PageBreakSeparator = "P";
        options.ColumnBreakSeparator = "O";
        options.SoftHyphenText = "S";
        options.NoBreakHyphenText = "N";
        options.SymbolText = "Y";
        return options;
    }

    static global::docx.DocxPackage CreateStoryPackage()
    {
        global::docx.DocxPackage package = new global::docx.DocxPackagePersistence().CreatePackage();
        string mainPartName = "/word/document.xml";
        package.ReplaceXmlPart(mainPartName, global::docx.DocxConstants.ContentTypeMainDocument, CreateDocumentXml("Main"));
        AddStoryPart(package, mainPartName, global::docx.DocxConstants.RelationshipTypeHeader, "header1.xml", "/word/header1.xml", global::docx.DocxConstants.ContentTypeHeader, "hdr", "Header");
        AddStoryPart(package, mainPartName, global::docx.DocxConstants.RelationshipTypeFooter, "footer1.xml", "/word/footer1.xml", global::docx.DocxConstants.ContentTypeFooter, "ftr", "Footer");
        AddStoryPart(package, mainPartName, global::docx.DocxConstants.RelationshipTypeFootnotes, "footnotes.xml", "/word/footnotes.xml", global::docx.DocxConstants.ContentTypeFootnotes, "footnotes", "Footnote");
        AddStoryPart(package, mainPartName, global::docx.DocxConstants.RelationshipTypeEndnotes, "endnotes.xml", "/word/endnotes.xml", global::docx.DocxConstants.ContentTypeEndnotes, "endnotes", "Endnote");
        AddStoryPart(package, mainPartName, global::docx.DocxConstants.RelationshipTypeComments, "comments.xml", "/word/comments.xml", global::docx.DocxConstants.ContentTypeComments, "comments", "Comment");
        return package;
    }

    static void AddStoryPart(global::docx.DocxPackage package, string sourcePartName, string relationshipType, string target, string partName, string contentType, string rootName, string text)
    {
        string childName = "p";
        string attributes = "";

        if (rootName == "footnotes")
        {
            childName = "footnote";
            attributes = " w:id=\"1\"";
        }
        else if (rootName == "endnotes")
        {
            childName = "endnote";
            attributes = " w:id=\"1\"";
        }
        else if (rootName == "comments")
        {
            childName = "comment";
            attributes = " w:id=\"1\" w:author=\"Author\"";
        }

        string content = "<w:p><w:r><w:t>" + text + "</w:t></w:r></w:p>";

        if (childName != "p")
        {
            content = "<w:" + childName + attributes + ">" + content + "</w:" + childName + ">";
        }

        string xml = "<w:" + rootName + " xmlns:w=\"" + global::docx.DocxConstants.NamespaceWordprocessingMl + "\">" + content + "</w:" + rootName + ">";
        package.AddXmlPartWithContent(partName, contentType, xml);
        package.CreateRelationship(sourcePartName, relationshipType, target, false);
    }

    static string CreateDocumentXml(string text)
    {
        return "<w:document xmlns:w=\"" + global::docx.DocxConstants.NamespaceWordprocessingMl
            + "\"><w:body><w:p><w:r><w:t>" + text + "</w:t></w:r></w:p></w:body></w:document>";
    }
}
