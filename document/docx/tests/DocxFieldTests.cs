using System.IO;
using NUnit.Framework;

namespace docx.Tests;

[TestFixture]
public class DocxFieldTests
{
    [Test]
    public void Parse_ReadsInstructionAttributesChildrenAndRuns()
    {
        global::docx.DocxField field = ParseField("<w:fldSimple xmlns:w=\"http://schemas.openxmlformats.org/wordprocessingml/2006/main\" w:instr=\"PAGE\" w:fldLock=\"1\" w:dirty=\"true\"><w:r><w:t>Page </w:t></w:r><w:proofErr w:type=\"spellStart\" /><w:r><w:t>1</w:t></w:r></w:fldSimple>");

        Assert.That(field.Instruction, Is.EqualTo("PAGE"));
        Assert.That(field.FieldLock, Is.EqualTo("1"));
        Assert.That(field.Dirty, Is.EqualTo("true"));
        Assert.That(field.IsLocked(), Is.True);
        Assert.That(field.IsDirty(), Is.True);
        Assert.That(field.ChildCount(), Is.EqualTo(3));
        Assert.That(field.RunCount(), Is.EqualTo(2));
        Assert.That(field.GetRun(0).GetText(), Is.EqualTo("Page "));
        Assert.That(field.GetText(), Is.EqualTo("Page 1"));
        Assert.That(field.GetChildXml(1), Does.Contain("proofErr"));
    }

    [Test]
    public void ParagraphParsing_ExposesDirectSimpleFields()
    {
        global::docx.DocxParagraph paragraph = ParseParagraph("<w:p xmlns:w=\"http://schemas.openxmlformats.org/wordprocessingml/2006/main\"><w:r><w:t>Before</w:t></w:r><w:fldSimple w:instr=\"DATE\"><w:r><w:t>Today</w:t></w:r></w:fldSimple><w:smartTag><w:fldSimple w:instr=\"PAGE\" /></w:smartTag></w:p>");

        Assert.That(paragraph.ChildCount(), Is.EqualTo(3));
        Assert.That(paragraph.RunElementCount(), Is.EqualTo(1));
        Assert.That(paragraph.RunCount(), Is.EqualTo(1));
        Assert.That(paragraph.FieldElementCount(), Is.EqualTo(1));
        Assert.That(paragraph.FieldCount(), Is.EqualTo(1));
        Assert.That(paragraph.GetField(0).Instruction, Is.EqualTo("DATE"));
        Assert.That(paragraph.GetField(0).GetText(), Is.EqualTo("Today"));
    }

    [Test]
    public void CreateAndWriteXml_RoundTripsSimpleField()
    {
        global::docx.DocxField field = global::docx.DocxField.Create("DATE \\@ \"yyyy-MM-dd\"", "2026-08-03");

        field.SetFieldLock(true);
        field.SetDirty(false);

        global::docx.DocxField parsed = ParseField(field.WriteXml());

        Assert.That(parsed.Instruction, Is.EqualTo("DATE \\@ \"yyyy-MM-dd\""));
        Assert.That(parsed.FieldLock, Is.EqualTo("1"));
        Assert.That(parsed.Dirty, Is.EqualTo("0"));
        Assert.That(parsed.IsLocked(), Is.True);
        Assert.That(parsed.IsDirty(), Is.False);
        Assert.That(parsed.GetText(), Is.EqualTo("2026-08-03"));
    }

    [Test]
    public void ParagraphWriting_WritesAddedSimpleField()
    {
        global::docx.DocxParagraph paragraph = new global::docx.DocxParagraph("");

        paragraph.AddField(global::docx.DocxField.Create("PAGE", "1"));

        global::docx.DocxParagraph parsed = ParseParagraph(paragraph.WriteXml());

        Assert.That(parsed.ChildCount(), Is.EqualTo(1));
        Assert.That(parsed.FieldCount(), Is.EqualTo(1));
        Assert.That(parsed.GetField(0).Instruction, Is.EqualTo("PAGE"));
        Assert.That(parsed.GetField(0).GetText(), Is.EqualTo("1"));
    }

    [Test]
    public void FieldState_WritesFalseFlagsAndClearsOptionalFlags()
    {
        global::docx.DocxField field = global::docx.DocxField.Create("PAGE", "1");

        field.SetFieldLock(true);
        field.SetDirty(true);
        field.SetFieldLock(false);
        field.SetDirty(false);

        global::docx.DocxField falseFlags = ParseField(field.WriteXml());

        Assert.That(falseFlags.FieldLock, Is.EqualTo("0"));
        Assert.That(falseFlags.Dirty, Is.EqualTo("0"));
        Assert.That(falseFlags.IsLocked(), Is.False);
        Assert.That(falseFlags.IsDirty(), Is.False);

        field.ClearFieldLock();
        field.ClearDirty();

        string xml = field.WriteXml();
        global::docx.DocxField cleared = ParseField(xml);

        Assert.That(xml, Does.Not.Contain("fldLock"));
        Assert.That(xml, Does.Not.Contain("dirty"));
        Assert.That(cleared.FieldLock, Is.EqualTo(""));
        Assert.That(cleared.Dirty, Is.EqualTo(""));
    }

    [Test]
    public void ComplexFieldParsing_ExposesAndPreservesRunLevelTokens()
    {
        global::docx.DocxParagraph paragraph = ParseParagraph("<w:p xmlns:w=\"http://schemas.openxmlformats.org/wordprocessingml/2006/main\"><w:r><w:fldChar w:fldCharType=\"begin\" w:dirty=\"1\" /></w:r><w:r><w:instrText xml:space=\"preserve\"> PAGE </w:instrText></w:r><w:r><w:fldChar w:fldCharType=\"separate\" /></w:r><w:r><w:t>1</w:t></w:r><w:r><w:fldChar w:fldCharType=\"end\" /></w:r></w:p>");

        Assert.That(global::docx.DocxFieldParser.ContainsComplexField(paragraph), Is.True);
        Assert.That(global::docx.DocxFieldParser.CountComplexFieldElements(paragraph), Is.EqualTo(4));
        Assert.That(global::docx.DocxFieldParser.GetComplexFieldInstructionText(paragraph), Is.EqualTo(" PAGE "));
        Assert.That(paragraph.GetRun(0).GetComplexFieldElement(0).FieldCharType, Is.EqualTo("begin"));
        Assert.That(paragraph.GetRun(0).GetComplexFieldElement(0).Dirty, Is.EqualTo("1"));
        Assert.That(paragraph.GetRun(1).GetComplexFieldElement(0).InstructionText, Is.EqualTo(" PAGE "));
        Assert.That(paragraph.GetRun(1).GetComplexFieldElement(0).PreserveSpace, Is.True);
        Assert.That(paragraph.GetRun(2).GetComplexFieldElement(0).FieldCharType, Is.EqualTo("separate"));
        Assert.That(paragraph.GetRun(4).GetComplexFieldElement(0).FieldCharType, Is.EqualTo("end"));

        global::docx.DocxParagraph written = ParseParagraph(paragraph.WriteXml());

        Assert.That(global::docx.DocxFieldParser.CountComplexFieldElements(written), Is.EqualTo(4));
        Assert.That(written.WriteXml(), Does.Contain("fldCharType=\"begin\""));
        Assert.That(written.WriteXml(), Does.Contain("xml:space=\"preserve\""));
    }

    [Test]
    public void ComplexFieldElementWriting_RoundTripsSupportedElements()
    {
        global::docx.DocxComplexFieldElement begin = new global::docx.DocxComplexFieldElement("", global::docx.DocxComplexFieldElement.KindCharacter, "begin", "", false, "1", "0");
        global::docx.DocxComplexFieldElement instruction = new global::docx.DocxComplexFieldElement("", global::docx.DocxComplexFieldElement.KindInstructionText, "", " DATE ", true, "", "");
        global::docx.DocxComplexFieldElement deleted = new global::docx.DocxComplexFieldElement("", global::docx.DocxComplexFieldElement.KindDeletedInstructionText, "", " OLD ", true, "", "");

        global::docx.DocxComplexFieldElement parsedBegin = ParseComplexFieldElement(begin.WriteXml());
        global::docx.DocxComplexFieldElement parsedInstruction = ParseComplexFieldElement(instruction.WriteXml());
        global::docx.DocxComplexFieldElement parsedDeleted = ParseComplexFieldElement(deleted.WriteXml());

        Assert.That(parsedBegin.IsFieldCharacter(), Is.True);
        Assert.That(parsedBegin.FieldCharType, Is.EqualTo("begin"));
        Assert.That(parsedBegin.FieldLock, Is.EqualTo("1"));
        Assert.That(parsedBegin.Dirty, Is.EqualTo("0"));
        Assert.That(parsedInstruction.IsInstructionText(), Is.True);
        Assert.That(parsedInstruction.InstructionText, Is.EqualTo(" DATE "));
        Assert.That(parsedInstruction.PreserveSpace, Is.True);
        Assert.That(parsedDeleted.IsDeletedInstructionText(), Is.True);
        Assert.That(parsedDeleted.InstructionText, Is.EqualTo(" OLD "));
    }

    [Test]
    public void RunWriting_WritesAddedComplexFieldElements()
    {
        global::docx.DocxRun run = new global::docx.DocxRun("");

        run.AddComplexFieldElement(new global::docx.DocxComplexFieldElement("", global::docx.DocxComplexFieldElement.KindCharacter, "begin", "", false, "", ""));
        run.AddComplexFieldElement(new global::docx.DocxComplexFieldElement("", global::docx.DocxComplexFieldElement.KindInstructionText, "", " PAGE ", true, "", ""));

        global::docx.DocxRun parsed = ParseRun(run.WriteXml());

        Assert.That(parsed.ChildCount(), Is.EqualTo(2));
        Assert.That(parsed.ComplexFieldElementCount(), Is.EqualTo(2));
        Assert.That(parsed.GetComplexFieldElement(0).FieldCharType, Is.EqualTo("begin"));
        Assert.That(parsed.GetComplexFieldInstructionText(), Is.EqualTo(" PAGE "));
    }

    [Test]
    public void FieldParsing_RejectsInvalidArguments()
    {
        global::docx.DocxXmlReader reader = global::docx.DocxXmlReader.Create("<root />");
        global::docx.DocxField field = global::docx.DocxField.Create("PAGE", "1");
        global::docx.DocxParagraph paragraph = new global::docx.DocxParagraph("");
        global::docx.DocxComplexFieldElement complex = new global::docx.DocxComplexFieldElement("", global::docx.DocxComplexFieldElement.KindInstructionText, "", " PAGE ", true, "", "");
        global::docx.DocxRun run = new global::docx.DocxRun("");

        Assert.Throws<ArgumentNullException>(() => new global::docx.DocxField(null!));
        Assert.Throws<ArgumentNullException>(() => global::docx.DocxField.Parse(null!));
        Assert.Throws<ArgumentNullException>(() => global::docx.DocxField.Create(null!, "1"));
        Assert.Throws<ArgumentNullException>(() => global::docx.DocxField.Create("PAGE", null!));
        Assert.Throws<ArgumentException>(() => global::docx.DocxField.Create("", "1"));
        Assert.Throws<ArgumentNullException>(() => global::docx.DocxField.IsSupported(null!));
        Assert.Throws<ArgumentNullException>(() => global::docx.DocxField.ReadRequiredAttribute(null!, "instr"));
        Assert.Throws<ArgumentNullException>(() => global::docx.DocxField.ReadRequiredAttribute(ParseReader("<w:fldSimple xmlns:w=\"http://schemas.openxmlformats.org/wordprocessingml/2006/main\" w:instr=\"PAGE\" />"), null!));
        Assert.Throws<ArgumentException>(() => global::docx.DocxField.ReadRequiredAttribute(ParseReader("<w:fldSimple xmlns:w=\"http://schemas.openxmlformats.org/wordprocessingml/2006/main\" w:instr=\"PAGE\" />"), ""));
        Assert.Throws<ArgumentNullException>(() => field.AddChild(null!));
        Assert.Throws<ArgumentNullException>(() => field.AddRun(null!));
        Assert.Throws<ArgumentNullException>(() => field.SetInstruction(null!));
        Assert.Throws<ArgumentNullException>(() => field.WriteTo(null!));
        Assert.Throws<ArgumentNullException>(() => field.WriteChildrenTo(null!));
        Assert.Throws<ArgumentNullException>(() => global::docx.DocxField.DocxRunFromBuffer(null!));
        Assert.Throws<ArgumentNullException>(() => paragraph.AddField(null!));
        Assert.Throws<ArgumentNullException>(() => global::docx.DocxParagraph.DocxFieldFromBuffer(null!));
        Assert.Throws<ArgumentNullException>(() => new global::docx.DocxComplexFieldElement(null!, "character", "begin", "", false, "", ""));
        Assert.Throws<ArgumentNullException>(() => new global::docx.DocxComplexFieldElement("", null!, "begin", "", false, "", ""));
        Assert.Throws<ArgumentNullException>(() => new global::docx.DocxComplexFieldElement("", "character", null!, "", false, "", ""));
        Assert.Throws<ArgumentNullException>(() => new global::docx.DocxComplexFieldElement("", "character", "begin", null!, false, "", ""));
        Assert.Throws<ArgumentNullException>(() => new global::docx.DocxComplexFieldElement("", "character", "begin", "", false, null!, ""));
        Assert.Throws<ArgumentNullException>(() => new global::docx.DocxComplexFieldElement("", "character", "begin", "", false, "", null!));
        Assert.Throws<ArgumentException>(() => new global::docx.DocxComplexFieldElement("", "", "begin", "", false, "", ""));
        Assert.Throws<ArgumentNullException>(() => global::docx.DocxComplexFieldElement.Parse(null!));
        Assert.Throws<ArgumentNullException>(() => global::docx.DocxComplexFieldElement.ParseXml(null!));
        Assert.Throws<ArgumentNullException>(() => global::docx.DocxComplexFieldElement.IsSupported(null!));
        Assert.Throws<ArgumentNullException>(() => global::docx.DocxComplexFieldElement.ReadInstructionElement(null!, "", global::docx.DocxComplexFieldElement.KindInstructionText));
        Assert.Throws<ArgumentNullException>(() => global::docx.DocxComplexFieldElement.ReadInstructionElement(ParseReader("<w:instrText xmlns:w=\"http://schemas.openxmlformats.org/wordprocessingml/2006/main\">PAGE</w:instrText>"), null!, global::docx.DocxComplexFieldElement.KindInstructionText));
        Assert.Throws<ArgumentNullException>(() => global::docx.DocxComplexFieldElement.ReadInstructionElement(ParseReader("<w:instrText xmlns:w=\"http://schemas.openxmlformats.org/wordprocessingml/2006/main\">PAGE</w:instrText>"), "", null!));
        Assert.Throws<ArgumentNullException>(() => complex.WriteTo(null!));
        Assert.Throws<ArgumentNullException>(() => complex.WriteFieldCharacterTo(null!));
        Assert.Throws<ArgumentNullException>(() => complex.WriteInstructionTextTo(null!, "instrText"));
        Assert.Throws<ArgumentNullException>(() => complex.WriteInstructionTextTo(global::docx.DocxXmlWriter.Create(), null!));
        Assert.Throws<ArgumentNullException>(() => run.AddComplexFieldElement(null!));
        Assert.Throws<ArgumentNullException>(() => global::docx.DocxRun.DocxComplexFieldElementFromBuffer(null!));
        Assert.Throws<ArgumentNullException>(() => global::docx.DocxFieldParser.ContainsComplexField(null!));
        Assert.Throws<ArgumentNullException>(() => global::docx.DocxFieldParser.CountComplexFieldElements(null!));
        Assert.Throws<ArgumentNullException>(() => global::docx.DocxFieldParser.GetComplexFieldInstructionText(null!));
        Assert.Throws<InvalidDataException>(() => new global::docx.DocxField("<w:fldSimple xmlns:w=\"http://schemas.openxmlformats.org/wordprocessingml/2006/main\" />"));
        Assert.Throws<InvalidDataException>(() => global::docx.DocxComplexFieldElement.ParseXml("<w:fldChar xmlns:w=\"http://schemas.openxmlformats.org/wordprocessingml/2006/main\" />"));
        Assert.Throws<InvalidOperationException>(() => new global::docx.DocxField("").WriteXml());
        Assert.Throws<InvalidOperationException>(() => global::docx.DocxComplexFieldElement.ParseXml("<root />"));
        Assert.Throws<InvalidOperationException>(() => new global::docx.DocxComplexFieldElement("", "other", "", "", false, "", "").WriteXml());
        Assert.Throws<InvalidOperationException>(() => new global::docx.DocxComplexFieldElement("", global::docx.DocxComplexFieldElement.KindCharacter, "", "", false, "", "").WriteXml());

        Assert.That(reader.ReadToElement("root", ""), Is.True);
        Assert.That(global::docx.DocxField.IsSupported(reader), Is.False);
        Assert.That(global::docx.DocxComplexFieldElement.IsSupported(reader), Is.False);
        Assert.Throws<InvalidOperationException>(() => global::docx.DocxField.Parse(reader));
        Assert.Throws<InvalidOperationException>(() => global::docx.DocxComplexFieldElement.Parse(reader));
    }

    static global::docx.DocxField ParseField(string xml)
    {
        global::docx.DocxXmlReader reader = ParseReader(xml);

        return global::docx.DocxField.Parse(reader);
    }

    static global::docx.DocxXmlReader ParseReader(string xml)
    {
        global::docx.DocxXmlReader reader = global::docx.DocxXmlReader.Create(xml);

        Assert.That(reader.MoveToContent(), Is.True);

        return reader;
    }

    static global::docx.DocxComplexFieldElement ParseComplexFieldElement(string xml)
    {
        global::docx.DocxXmlReader reader = ParseReader(xml);

        return global::docx.DocxComplexFieldElement.Parse(reader);
    }

    static global::docx.DocxRun ParseRun(string xml)
    {
        global::docx.DocxXmlReader reader = global::docx.DocxXmlReader.Create(xml);

        Assert.That(reader.ReadToElement("r", global::docx.DocxConstants.NamespaceWordprocessingMl), Is.True);

        return global::docx.DocxRun.Parse(reader);
    }

    static global::docx.DocxParagraph ParseParagraph(string xml)
    {
        global::docx.DocxXmlReader reader = global::docx.DocxXmlReader.Create(xml);

        Assert.That(reader.ReadToElement("p", global::docx.DocxConstants.NamespaceWordprocessingMl), Is.True);

        return global::docx.DocxParagraph.Parse(reader);
    }
}

[TestFixture]
public class DocxLinkBookmarkFieldManagementTests
{
    [Test]
    public void Hyperlinks_CreateExternalAndInternalTargetsAndRoundTrip()
    {
        global::docx.IDocxLinkBookmarkFieldManagement links = new global::docx.DocxLinkBookmarkFieldManagement();
        global::docx.IDocxDocumentPersistence persistence = new global::docx.DocxPackagePersistence();
        global::docx.IDocxContentFactory factory = new global::docx.DocxContentFactory();
        global::docx.DocxDocument document = persistence.CreateDocument();
        global::docx.DocxParagraph paragraph = factory.CreateParagraph();
        global::docx.DocxHyperlink external = links.CreateExternalHyperlink(document, "https://example.com/report", "Report");
        global::docx.DocxHyperlink internalLink = links.CreateInternalHyperlink("Summary", "Jump to summary");

        links.SetHyperlinkTooltip(external, "Open report");
        links.AppendHyperlink(paragraph, external);
        links.AppendHyperlink(paragraph, internalLink);
        document.GetBody().AddBlock(factory.CreateParagraphBlock(paragraph));

        Assert.That(links.IsExternalHyperlink(external), Is.True);
        Assert.That(links.GetHyperlinkTarget(external), Is.EqualTo("https://example.com/report"));
        Assert.That(links.GetHyperlinkRelationshipId(external), Is.Not.Empty);
        Assert.That(links.GetHyperlinkAnchor(internalLink), Is.EqualTo("Summary"));
        Assert.That(links.GetHyperlinkTooltip(external), Is.EqualTo("Open report"));

        global::docx.DocxDocument reopened = RoundTripDocument(persistence, document);
        global::docx.DocxParagraph parsed = reopened.GetBody().GetParagraph(0);
        global::docx.DocxHyperlink parsedExternal = parsed.GetHyperlink(0);

        Assert.That(links.ResolveMainDocumentHyperlink(reopened, parsedExternal), Is.True);
        Assert.That(links.GetHyperlinkTarget(parsedExternal), Is.EqualTo("https://example.com/report"));
        Assert.That(links.IsExternalHyperlink(parsedExternal), Is.True);
        Assert.That(links.GetHyperlinkTooltip(parsedExternal), Is.EqualTo("Open report"));
        Assert.That(links.GetHyperlinkAnchor(parsed.GetHyperlink(1)), Is.EqualTo("Summary"));
        Assert.That(parsed.GetHyperlink(1).GetText(), Is.EqualTo("Jump to summary"));
    }

    [Test]
    public void Bookmarks_InsertRangeWithDocumentUniqueIdAndSupportLookup()
    {
        global::docx.IDocxLinkBookmarkFieldManagement links = new global::docx.DocxLinkBookmarkFieldManagement();
        global::docx.IDocxDocumentPersistence persistence = new global::docx.DocxPackagePersistence();
        global::docx.IDocxContentFactory factory = new global::docx.DocxContentFactory();
        global::docx.DocxDocument document = persistence.CreateDocument();
        global::docx.DocxParagraph existing = factory.CreateParagraph();
        global::docx.DocxParagraph paragraph = factory.CreateParagraph();

        links.InsertBookmarkStart(existing, 0, "0", "Existing");
        links.InsertBookmarkEnd(existing, 1, "0");
        paragraph.InsertRun(0, factory.CreateTextRun("First"));
        paragraph.InsertRun(1, factory.CreateTextRun("Second"));
        document.GetBody().AddBlock(factory.CreateParagraphBlock(existing));
        document.GetBody().AddBlock(factory.CreateParagraphBlock(paragraph));

        string id = links.InsertBookmarkRange(document, paragraph, 0, 2, "SelectedText");

        Assert.That(id, Is.EqualTo("1"));
        Assert.That(paragraph.ChildCount(), Is.EqualTo(4));
        Assert.That(paragraph.GetChild(0).Kind, Is.EqualTo(global::docx.DocxParagraphChildKind.Bookmark));
        Assert.That(paragraph.GetChild(3).Kind, Is.EqualTo(global::docx.DocxParagraphChildKind.Bookmark));
        Assert.That(links.GetBookmarkStarts(paragraph).Count, Is.EqualTo(1));
        Assert.That(links.FindBookmarkStart(paragraph, "SelectedText")!.Id, Is.EqualTo("1"));
        Assert.That(links.FindBookmarkEnd(paragraph, "1"), Is.Not.Null);
        Assert.That(links.FindDocumentBookmarkStart(document, "SelectedText"), Is.SameAs(links.FindBookmarkStart(paragraph, "SelectedText")));

        global::docx.DocxDocument reopened = RoundTripDocument(persistence, document);
        global::docx.DocxBookmark parsed = links.FindDocumentBookmarkStart(reopened, "SelectedText")!;

        Assert.That(parsed.Id, Is.EqualTo("1"));
        Assert.That(links.FindBookmarkEnd(reopened.GetBody().GetParagraph(1), parsed.Id), Is.Not.Null);
        Assert.That(reopened.GetBody().GetParagraph(1).GetText(), Is.EqualTo("FirstSecond"));
    }

    [Test]
    public void Fields_ExposeInstructionsResultsUpdateMarkersAndCommonCreation()
    {
        global::docx.IDocxLinkBookmarkFieldManagement fields = new global::docx.DocxLinkBookmarkFieldManagement();
        global::docx.DocxParagraph paragraph = new global::docx.DocxContentFactory().CreateParagraph();
        global::docx.DocxField crossReference = fields.CreateCrossReferenceField("Summary", "Section 2");
        global::docx.DocxField page = fields.CreatePageField("1");
        global::docx.DocxField date = fields.CreateDateField("yyyy-MM-dd", "2026-08-25");
        global::docx.DocxField author = fields.CreateAuthorField("Ada");
        global::docx.DocxField title = fields.CreateTitleField("Report");
        global::docx.DocxField contents = fields.CreateTableOfContentsField("Contents");

        fields.SetFieldResult(page, "7");
        fields.MarkFieldForUpdate(page);
        fields.SetFieldLocked(page, true);
        fields.AppendField(paragraph, crossReference);
        fields.AppendField(paragraph, page);
        fields.AppendField(paragraph, date);
        fields.AppendField(paragraph, author);
        fields.AppendField(paragraph, title);
        fields.AppendField(paragraph, contents);

        Assert.That(fields.IsCrossReferenceField(crossReference), Is.True);
        Assert.That(fields.IsPageField(page), Is.True);
        Assert.That(fields.IsDateField(date), Is.True);
        Assert.That(fields.IsAuthorField(author), Is.True);
        Assert.That(fields.IsTitleField(title), Is.True);
        Assert.That(fields.IsTableOfContentsField(contents), Is.True);
        Assert.That(fields.GetFieldInstruction(crossReference), Is.EqualTo("REF Summary \\h"));
        Assert.That(fields.GetFieldInstruction(date), Is.EqualTo("DATE \\@ \"yyyy-MM-dd\""));
        Assert.That(fields.GetFieldInstruction(contents), Is.EqualTo("TOC \\o \"1-3\" \\h \\z \\u"));
        Assert.That(fields.GetFieldResult(page), Is.EqualTo("7"));
        Assert.That(fields.IsFieldUpdateRequested(page), Is.True);
        Assert.That(fields.IsFieldLocked(page), Is.True);
        Assert.That(paragraph.FieldCount(), Is.EqualTo(6));

        fields.ClearFieldUpdateMarker(page);
        Assert.That(fields.IsFieldUpdateRequested(page), Is.False);
    }

    [Test]
    public void Fields_ClassifyExactFieldCodesWithoutCaseSensitivity()
    {
        global::docx.IDocxLinkBookmarkFieldManagement fields = new global::docx.DocxLinkBookmarkFieldManagement();

        Assert.That(fields.IsCrossReferenceField(fields.CreateField("ref", "Reference")), Is.True);
        Assert.That(fields.IsPageField(fields.CreateField("page", "1")), Is.True);
        Assert.That(fields.IsDateField(fields.CreateField("date", "Date")), Is.True);
        Assert.That(fields.IsDateField(fields.CreateField("createdate", "Created")), Is.True);
        Assert.That(fields.IsDateField(fields.CreateField("savedate", "Saved")), Is.True);
        Assert.That(fields.IsDateField(fields.CreateField("printdate", "Printed")), Is.True);
        Assert.That(fields.IsAuthorField(fields.CreateField("author", "Author")), Is.True);
        Assert.That(fields.IsTitleField(fields.CreateField("title", "Title")), Is.True);
        Assert.That(fields.IsTableOfContentsField(fields.CreateField("toc", "Contents")), Is.True);
    }

    [Test]
    public void Fields_PreserveCommonSwitchesAndComplexFieldMarkupAcrossRoundTrip()
    {
        global::docx.IDocxLinkBookmarkFieldManagement fields = new global::docx.DocxLinkBookmarkFieldManagement();
        global::docx.IDocxDocumentPersistence persistence = new global::docx.DocxPackagePersistence();
        global::docx.IDocxContentFactory factory = new global::docx.DocxContentFactory();
        global::docx.DocxDocument document = persistence.CreateDocument();
        global::docx.DocxParagraph paragraph = new global::docx.DocxParagraph(
            "<w:p xmlns:w=\"http://schemas.openxmlformats.org/wordprocessingml/2006/main\">"
            + "<w:fldSimple w:instr=\"REF Target \\h \\* MERGEFORMAT\"><w:r><w:t>Reference</w:t></w:r></w:fldSimple>"
            + "<w:fldSimple w:instr=\"PAGE \\* Arabic\"><w:r><w:t>3</w:t></w:r></w:fldSimple>"
            + "<w:fldSimple w:instr=\"DATE \\@ &quot;MMMM d, yyyy&quot;\"><w:r><w:t>August 25, 2026</w:t></w:r></w:fldSimple>"
            + "<w:fldSimple w:instr=\"AUTHOR\"><w:r><w:t>Author</w:t></w:r></w:fldSimple>"
            + "<w:fldSimple w:instr=\"TITLE\"><w:r><w:t>Title</w:t></w:r></w:fldSimple>"
            + "<w:fldSimple w:instr=\"TOC \\o &quot;1-3&quot; \\h \\z \\u\"><w:r><w:t>Contents</w:t></w:r></w:fldSimple>"
            + "<w:r><w:fldChar w:fldCharType=\"begin\" w:dirty=\"1\" /></w:r>"
            + "<w:r><w:instrText xml:space=\"preserve\"> PAGEREF Target \\h </w:instrText></w:r>"
            + "<w:r><w:fldChar w:fldCharType=\"separate\" /></w:r>"
            + "<w:r><w:t>3</w:t></w:r>"
            + "<w:r><w:fldChar w:fldCharType=\"end\" /></w:r>"
            + "</w:p>");
        document.GetBody().AddBlock(factory.CreateParagraphBlock(paragraph));

        global::docx.DocxDocument reopened = RoundTripDocument(persistence, document);
        global::docx.DocxParagraph parsed = reopened.GetBody().GetParagraph(0);

        Assert.That(parsed.FieldCount(), Is.EqualTo(6));
        Assert.That(fields.IsCrossReferenceField(parsed.GetField(0)), Is.True);
        Assert.That(fields.IsPageField(parsed.GetField(1)), Is.True);
        Assert.That(fields.IsDateField(parsed.GetField(2)), Is.True);
        Assert.That(fields.IsAuthorField(parsed.GetField(3)), Is.True);
        Assert.That(fields.IsTitleField(parsed.GetField(4)), Is.True);
        Assert.That(fields.IsTableOfContentsField(parsed.GetField(5)), Is.True);
        Assert.That(fields.GetFieldInstruction(parsed.GetField(0)), Is.EqualTo("REF Target \\h \\* MERGEFORMAT"));
        Assert.That(fields.GetFieldInstruction(parsed.GetField(5)), Is.EqualTo("TOC \\o \"1-3\" \\h \\z \\u"));
        Assert.That(fields.ContainsComplexField(parsed), Is.True);
        Assert.That(fields.ComplexFieldElementCount(parsed), Is.EqualTo(4));
        Assert.That(fields.GetComplexFieldInstruction(parsed), Is.EqualTo(" PAGEREF Target \\h "));
        Assert.That(parsed.GetText(), Does.EndWith("3"));
    }

    [Test]
    public void BookmarkRange_RejectsInvalidBoundsWithoutChangingParagraph()
    {
        global::docx.IDocxLinkBookmarkFieldManagement links = new global::docx.DocxLinkBookmarkFieldManagement();
        global::docx.DocxDocument document = new global::docx.DocxPackagePersistence().CreateDocument();
        global::docx.DocxParagraph paragraph = new global::docx.DocxContentFactory().CreateTextParagraph("Text");

        Assert.Throws<ArgumentOutOfRangeException>(() => links.InsertBookmarkRange(document, paragraph, -1, 1, "Bad"));
        Assert.Throws<ArgumentOutOfRangeException>(() => links.InsertBookmarkRange(document, paragraph, 1, 0, "Bad"));
        Assert.Throws<ArgumentOutOfRangeException>(() => links.InsertBookmarkRange(document, paragraph, 0, 2, "Bad"));
        Assert.That(paragraph.BookmarkCount(), Is.EqualTo(0));
        Assert.That(paragraph.GetText(), Is.EqualTo("Text"));
    }

    static global::docx.DocxDocument RoundTripDocument(global::docx.IDocxDocumentPersistence persistence, global::docx.DocxDocument document)
    {
        MemoryStream stream = new MemoryStream();
        persistence.SaveDocument(document, stream, new global::docx.DocxSaveOptions());
        stream.Position = 0;
        return persistence.OpenDocument(stream, new global::docx.DocxLoadOptions());
    }
}
