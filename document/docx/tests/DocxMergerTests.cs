using NUnit.Framework;

namespace docx.Tests;

[TestFixture]
public class DocxMergerTests
{
    [Test]
    public void Append_AppendsSourceBlocksAndUpdatesPackage()
    {
        global::docx.DocxDocument target = global::docx.DocxDocument.OpenPackage(CreatePackageWithBodyChildren(new string[]
        {
            CreateParagraphXml("Before")
        }));
        global::docx.DocxDocument source = global::docx.DocxDocument.OpenPackage(CreatePackageWithBodyChildren(new string[]
        {
            CreateParagraphXml("Inserted"),
            "<w:tbl><w:tr><w:tc>" + CreateParagraphXml("Table") + "</w:tc></w:tr></w:tbl>"
        }));

        int count = target.Append(source);

        Assert.That(count, Is.EqualTo(2));
        Assert.That(target.GetText(), Is.EqualTo("Before\nInserted\nTable"));
        Assert.That(target.Package.GetMainDocumentPart()!.GetText(), Does.Contain("Inserted"));
        Assert.That(target.Package.GetMainDocumentPart()!.GetText(), Does.Contain("Table"));
        Assert.That(source.GetText(), Is.EqualTo("Inserted\nTable"));
    }

    [Test]
    public void AppendBody_ReturnsZeroForEmptySource()
    {
        global::docx.DocxBody targetBody = ParseBody(CreateBodyXml(new string[]
        {
            CreateParagraphXml("Before")
        }));
        global::docx.DocxBody sourceBody = global::docx.DocxBody.Create();

        Assert.That(global::docx.DocxMerger.Append(targetBody, sourceBody), Is.EqualTo(0));
        Assert.That(targetBody.GetText(), Is.EqualTo("Before"));
        Assert.That(targetBody.Append(ParseBody(CreateBodyXml(new string[] { CreateParagraphXml("After") }))), Is.EqualTo(1));
        Assert.That(targetBody.GetText(), Is.EqualTo("Before\nAfter"));
    }

    [Test]
    public void InsertAtBookmark_InsertsSourceBlocksAfterBookmarkParagraphAndUpdatesPackage()
    {
        global::docx.DocxDocument target = global::docx.DocxDocument.OpenPackage(CreatePackageWithBodyChildren(new string[]
        {
            CreateParagraphXml("Before"),
            CreateBookmarkParagraphXml("Place", "Bookmark"),
            CreateParagraphXml("After")
        }));
        global::docx.DocxDocument source = global::docx.DocxDocument.OpenPackage(CreatePackageWithBodyChildren(new string[]
        {
            CreateParagraphXml("Inserted 1"),
            CreateParagraphXml("Inserted 2")
        }));

        int count = target.InsertAtBookmark("Place", source);

        Assert.That(count, Is.EqualTo(2));
        Assert.That(target.GetText(), Is.EqualTo("Before\nBookmark\nInserted 1\nInserted 2\nAfter"));
        Assert.That(target.Package.GetMainDocumentPart()!.GetText(), Does.Contain("Inserted 1"));
        Assert.That(target.Package.GetMainDocumentPart()!.GetText(), Does.Contain("Inserted 2"));
        Assert.That(source.GetText(), Is.EqualTo("Inserted 1\nInserted 2"));
    }

    [Test]
    public void InsertAtBookmark_InsertsAfterTopLevelTableContainingBookmark()
    {
        global::docx.DocxBody targetBody = ParseBody(CreateBodyXml(new string[]
        {
            CreateParagraphXml("Before"),
            "<w:tbl><w:tr><w:tc>" + CreateBookmarkParagraphXml("InsideTable", "Table bookmark") + "</w:tc></w:tr></w:tbl>",
            CreateParagraphXml("After")
        }));
        global::docx.DocxBody sourceBody = ParseBody(CreateBodyXml(new string[]
        {
            CreateParagraphXml("Inserted")
        }));

        Assert.That(targetBody.InsertAtBookmark("InsideTable", sourceBody), Is.EqualTo(1));
        Assert.That(targetBody.GetText(), Is.EqualTo("Before\nTable bookmark\nInserted\nAfter"));
        Assert.That(global::docx.DocxMerger.FindBookmarkBlockIndex(targetBody, "InsideTable"), Is.EqualTo(1));
    }

    [Test]
    public void InsertAtBookmark_InsertsAfterContentControlContainingNestedBookmark()
    {
        global::docx.DocxBody targetBody = ParseBody(CreateBodyXml(new string[]
        {
            CreateParagraphXml("Before"),
            "<w:sdt><w:sdtContent><w:tbl><w:tr><w:tc>" + CreateBookmarkParagraphXml("InsideControl", "Control bookmark") + "</w:tc></w:tr></w:tbl></w:sdtContent></w:sdt>",
            CreateParagraphXml("After")
        }));
        global::docx.DocxBody sourceBody = ParseBody(CreateBodyXml(new string[]
        {
            CreateParagraphXml("Inserted")
        }));

        Assert.That(targetBody.InsertAtBookmark("InsideControl", sourceBody), Is.EqualTo(1));
        Assert.That(targetBody.GetText(), Is.EqualTo("Before\nControl bookmark\nInserted\nAfter"));
        Assert.That(global::docx.DocxMerger.FindBookmarkBlockIndex(targetBody, "InsideControl"), Is.EqualTo(1));
    }

    [Test]
    public void InsertAtBookmark_ReturnsZeroWhenBookmarkIsMissing()
    {
        global::docx.DocxBody targetBody = ParseBody(CreateBodyXml(new string[]
        {
            CreateParagraphXml("Before"),
            CreateParagraphXml("After")
        }));
        global::docx.DocxBody sourceBody = ParseBody(CreateBodyXml(new string[]
        {
            CreateParagraphXml("Inserted")
        }));

        Assert.That(global::docx.DocxMerger.InsertAtBookmark(targetBody, "Missing", sourceBody), Is.EqualTo(0));
        Assert.That(targetBody.GetText(), Is.EqualTo("Before\nAfter"));
    }

    [Test]
    public void PublicBookmarkInsertion_DoesNotImportResourcesWhenBookmarkIsMissing()
    {
        global::docx.DocxDocument target = global::docx.DocxDocument.OpenPackage(CreatePackageWithBodyChildren(new string[]
        {
            CreateParagraphXml("Target")
        }));
        global::docx.DocxDocument source = global::docx.DocxDocument.OpenPackage(CreateMergePackage(new string[]
        {
            CreateReferencedParagraphXml()
        }, false));
        global::docx.IDocxTemplateManagement management = new global::docx.DocxTemplateManagement();
        int partCount = target.Package.PartCount();
        int relationshipSetCount = target.Package.RelationshipSetCount();

        Assert.That(management.InsertDocumentAtBookmark(target, "Missing", source), Is.EqualTo(0));
        Assert.That(target.GetText(), Is.EqualTo("Target"));
        Assert.That(target.Package.PartCount(), Is.EqualTo(partCount));
        Assert.That(target.Package.RelationshipSetCount(), Is.EqualTo(relationshipSetCount));
        Assert.That(new global::docx.DocxMediaManagement().GetImages(target), Is.Empty);
        Assert.That(new global::docx.DocxNoteManagement().GetFootnotes(target), Is.Empty);
        Assert.That(new global::docx.DocxNoteManagement().GetEndnotes(target), Is.Empty);
        Assert.That(new global::docx.DocxCommentManagement().GetComments(target), Is.Empty);
        Assert.That(new global::docx.DocxNumberingManagement().GetNumberingInstances(target.GetNumbering()), Is.Empty);
        Assert.That(target.Package.FindRelationshipSet("/word/document.xml"), Is.Null);
    }

    [Test]
    public void AppendDocument_RemapsReferencesAndImportsRelatedParts()
    {
        global::docx.DocxDocument target = global::docx.DocxDocument.OpenPackage(CreateMergePackage(
            new string[]
            {
                CreateBookmarkParagraphXml("TargetBookmark", "Before")
            },
            true));
        global::docx.DocxDocument source = global::docx.DocxDocument.OpenPackage(CreateMergePackage(
            new string[]
            {
                CreateReferencedParagraphXml()
            },
            false));

        int count = target.Append(source);

        string mainXml = target.Package.GetMainDocumentPart()!.GetText();

        Assert.That(count, Is.EqualTo(1));
        Assert.That(mainXml, Does.Contain("w:bookmarkStart w:id=\"0\""));
        Assert.That(mainXml, Does.Contain("w:bookmarkEnd w:id=\"0\""));
        Assert.That(mainXml, Does.Contain("w:footnoteReference w:id=\"2\""));
        Assert.That(mainXml, Does.Contain("w:endnoteReference w:id=\"2\""));
        Assert.That(mainXml, Does.Contain("w:commentRangeStart w:id=\"2\""));
        Assert.That(mainXml, Does.Contain("w:commentReference w:id=\"2\""));
        Assert.That(mainXml, Does.Contain("w:numId w:val=\"2\""));
        Assert.That(mainXml, Does.Contain("r:embed=\"rId2\""));
        Assert.That(target.FindFootnoteById("2")!.GetText(), Is.EqualTo("Source footnote"));
        Assert.That(target.FindEndnoteById("2")!.GetText(), Is.EqualTo("Source endnote"));
        Assert.That(target.FindCommentById("2")!.GetText(), Is.EqualTo("Source comment"));
        Assert.That(target.GetNumbering().FindNumberingInstanceById("2")!.AbstractNumberingId, Is.EqualTo("1"));
        Assert.That(target.GetNumbering().FindEffectiveLevel("2", "0")!.NumberFormat, Is.EqualTo("lowerLetter"));
        Assert.That(target.FindImage("/word/document.xml", "rId2")!.PartName, Is.EqualTo("/word/media/image2.png"));
        Assert.That(target.Package.FindPart("/word/media/image2.png")!.GetBytes(), Is.EqualTo(new byte[] { 1, 2, 3 }));
        Assert.That(target.Package.FindPart("/word/numbering.xml")!.GetText(), Does.Contain("w:numId=\"2\""));
        Assert.That(target.Package.FindPart("/word/footnotes.xml")!.GetText(), Does.Contain("w:id=\"2\""));
        Assert.That(target.Package.FindPart("/word/endnotes.xml")!.GetText(), Does.Contain("w:id=\"2\""));
        Assert.That(target.Package.FindPart("/word/comments.xml")!.GetText(), Does.Contain("w:id=\"2\""));
        Assert.That(source.Package.GetMainDocumentPart()!.GetText(), Does.Contain("r:embed=\"rImageMain\""));
    }

    [Test]
    public void PublicAssembly_AppendsAndReportsStyleBookmarkRelationshipAndDrawingRemapping()
    {
        global::docx.DocxPackage targetPackage = CreatePackageWithBodyChildren(new string[]
        {
            "<w:p xmlns:wp=\"http://schemas.openxmlformats.org/drawingml/2006/wordprocessingDrawing\"><w:bookmarkStart w:id=\"1\" w:name=\"SharedBookmark\" /><w:r><w:t>Target</w:t><w:drawing><wp:inline><wp:docPr id=\"1\" name=\"Target drawing\" /></wp:inline></w:drawing></w:r><w:bookmarkEnd w:id=\"1\" /></w:p>"
        });
        AddStylesPart(targetPackage, "<w:style w:type=\"paragraph\" w:styleId=\"Shared\"><w:name w:val=\"Target style\" /></w:style>");
        global::docx.DocxPackage sourcePackage = CreatePackageWithBodyChildren(new string[] { CreateParagraphXml("Temporary") });
        global::docx.DocxRelationship hyperlink = sourcePackage.CreateRelationship("/word/document.xml", global::docx.DocxConstants.RelationshipTypeHyperlink, "https://example.com/source", true);
        string sourceParagraph = "<w:p xmlns:r=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships\" xmlns:wp=\"http://schemas.openxmlformats.org/drawingml/2006/wordprocessingDrawing\"><w:pPr><w:pStyle w:val=\"Imported\" /></w:pPr><w:bookmarkStart w:id=\"1\" w:name=\"SharedBookmark\" /><w:hyperlink r:id=\"" + hyperlink.Id + "\"><w:r><w:t>Source link</w:t></w:r></w:hyperlink><w:r><w:drawing><wp:inline><wp:docPr id=\"1\" name=\"Source drawing\" /></wp:inline></w:drawing></w:r><w:bookmarkEnd w:id=\"1\" /></w:p>";
        sourcePackage.ReplaceXmlPart("/word/document.xml", global::docx.DocxConstants.ContentTypeMainDocument, CreateDocumentXml(new string[] { sourceParagraph }));
        AddStylesPart(sourcePackage, "<w:style w:type=\"paragraph\" w:styleId=\"Shared\"><w:name w:val=\"Source style\" /></w:style><w:style w:type=\"paragraph\" w:styleId=\"Imported\"><w:name w:val=\"Imported style\" /></w:style>");
        global::docx.DocxDocument target = global::docx.DocxDocument.OpenPackage(targetPackage);
        global::docx.DocxDocument source = global::docx.DocxDocument.OpenPackage(sourcePackage);
        global::docx.DocxAssemblyManagement management = new global::docx.DocxAssemblyManagement();

        global::docx.DocxAssemblyResult result = management.AppendDocument(target, source);
        string mainXml = target.Package.GetMainDocumentPart()!.GetText();

        Assert.That(result.AffectedBlockCount, Is.EqualTo(1));
        Assert.That(result.ImportedStyleCount, Is.EqualTo(1));
        Assert.That(result.RemappedRelationshipCount, Is.EqualTo(1));
        Assert.That(result.RemappedBookmarkCount, Is.EqualTo(1));
        Assert.That(result.RemappedDrawingCount, Is.EqualTo(1));
        Assert.That(result.Conflicts.Select(conflict => conflict.Code), Does.Contain(global::docx.DocxAssemblyDiagnosticCodes.StyleConflict));
        Assert.That(result.Conflicts.Select(conflict => conflict.Code), Does.Contain(global::docx.DocxAssemblyDiagnosticCodes.BookmarkNameConflict));
        Assert.That(mainXml, Does.Contain("wp:docPr id=\"2\""));
        Assert.That(mainXml, Does.Contain("w:bookmarkStart w:id=\"0\""));
        Assert.That(target.GetStyles().FindById("Imported"), Is.Not.Null);
        global::docx.DocxRelationship importedHyperlink = target.Package.GetRelationships("/word/document.xml").Single(value => value.Type == global::docx.DocxConstants.RelationshipTypeHyperlink);
        Assert.That(importedHyperlink.Target, Is.EqualTo("https://example.com/source"));
        Assert.That(mainXml, Does.Contain("r:id=\"" + importedHyperlink.Id + "\""));
    }

    [Test]
    public void PublicAssembly_SplitsBodyAndPreservesPackageResources()
    {
        global::docx.DocxPackage package = CreatePackageWithBodyChildren(new string[]
        {
            CreateParagraphXml("First"),
            CreateParagraphXml("Second"),
            CreateParagraphXml("Third")
        });
        package.AddBinaryPart("/custom/payload.bin", "application/octet-stream", new byte[] { 4, 5, 6 });
        global::docx.DocxDocument document = global::docx.DocxDocument.OpenPackage(package);
        global::docx.DocxAssemblyManagement management = new global::docx.DocxAssemblyManagement();

        global::docx.DocxDocument trailing = management.SplitDocumentAtBlock(document, 1);

        Assert.That(document.GetText(), Is.EqualTo("First"));
        Assert.That(trailing.GetText(), Is.EqualTo("Second\nThird"));
        Assert.That(document.Package.GetPartBytes("/custom/payload.bin"), Is.EqualTo(new byte[] { 4, 5, 6 }));
        Assert.That(trailing.Package.GetPartBytes("/custom/payload.bin"), Is.EqualTo(new byte[] { 4, 5, 6 }));
    }

    [Test]
    public void PublicAssembly_ImportsSectionAndRemapsHeaderRelationship()
    {
        global::docx.DocxDocument target = global::docx.DocxDocument.OpenPackage(CreatePackageWithBodyChildren(new string[] { CreateParagraphXml("Target") }));
        string sectionParagraph = "<w:p xmlns:r=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships\"><w:pPr><w:sectPr><w:headerReference w:type=\"default\" r:id=\"rHeader\" /><w:pgSz w:w=\"12240\" w:h=\"15840\" /></w:sectPr></w:pPr><w:r><w:t>Section one</w:t></w:r></w:p>";
        global::docx.DocxPackage sourcePackage = CreatePackageWithBodyChildren(new string[]
        {
            sectionParagraph,
            CreateParagraphXml("Section two"),
            "<w:sectPr><w:pgSz w:w=\"11900\" w:h=\"16840\" /></w:sectPr>"
        });
        sourcePackage.CreateRelationshipWithId("/word/document.xml", "rHeader", global::docx.DocxConstants.RelationshipTypeHeader, "header1.xml", false);
        sourcePackage.AddXmlPartWithContent("/word/header1.xml", global::docx.DocxConstants.ContentTypeHeader, "<w:hdr xmlns:w=\"http://schemas.openxmlformats.org/wordprocessingml/2006/main\">" + CreateParagraphXml("Imported header") + "</w:hdr>");
        global::docx.DocxDocument source = global::docx.DocxDocument.OpenPackage(sourcePackage);
        global::docx.DocxAssemblyManagement management = new global::docx.DocxAssemblyManagement();

        global::docx.DocxAssemblyResult result = management.ImportDocumentSection(target, source, 0);

        Assert.That(result.ImportedSectionCount, Is.EqualTo(1));
        Assert.That(result.AffectedBlockCount, Is.EqualTo(1));
        Assert.That(result.RemappedRelationshipCount, Is.EqualTo(1));
        Assert.That(target.GetText(), Is.EqualTo("Target\nSection one"));
        Assert.That(target.GetHeaders(), Has.Count.EqualTo(1));
        Assert.That(target.GetHeaders()[0].GetText(), Is.EqualTo("Imported header"));
        Assert.That(target.Package.GetMainDocumentPart()!.GetText(), Does.Contain("w:headerReference"));
        Assert.That(target.Package.GetMainDocumentPart()!.GetText(), Does.Not.Contain("r:id=\"rHeader\""));
    }

    [Test]
    public void PublicAssembly_ImportsStylesNumberingAndMediaIndependently()
    {
        global::docx.DocxPackage targetPackage = CreateMergePackage(new string[] { CreateParagraphXml("Target") }, true);
        global::docx.DocxPackage sourcePackage = CreateMergePackage(new string[] { CreateParagraphXml("Source") }, false);
        AddStylesPart(targetPackage, "<w:style w:type=\"paragraph\" w:styleId=\"Target\"><w:name w:val=\"Target\" /></w:style>");
        AddStylesPart(sourcePackage, "<w:style w:type=\"paragraph\" w:styleId=\"Source\"><w:name w:val=\"Source\" /></w:style>");
        global::docx.DocxDocument target = global::docx.DocxDocument.OpenPackage(targetPackage);
        global::docx.DocxDocument source = global::docx.DocxDocument.OpenPackage(sourcePackage);
        global::docx.DocxAssemblyManagement management = new global::docx.DocxAssemblyManagement();

        global::docx.DocxAssemblyResult styles = management.ImportDocumentStyles(target, source);
        global::docx.DocxAssemblyResult numbering = management.ImportDocumentNumbering(target, source);
        global::docx.DocxAssemblyResult media = management.ImportDocumentMedia(target, source);

        Assert.That(styles.ImportedStyleCount, Is.EqualTo(1));
        Assert.That(target.GetStyles().FindById("Source"), Is.Not.Null);
        Assert.That(numbering.ImportedAbstractNumberingCount, Is.EqualTo(1));
        Assert.That(numbering.ImportedNumberingCount, Is.EqualTo(1));
        Assert.That(media.ImportedMediaCount, Is.EqualTo(1));
        Assert.That(target.Package.FindPart("/word/media/image2.png")!.GetBytes(), Is.EqualTo(new byte[] { 1, 2, 3 }));
    }

    [Test]
    public void InsertAtBookmark_RejectsInvalidArguments()
    {
        global::docx.DocxDocument document = global::docx.DocxDocument.Create();
        global::docx.DocxBody body = global::docx.DocxBody.Create();
        global::docx.DocxParagraph paragraph = new global::docx.DocxParagraph("");
        global::docx.DocxBlock block = global::docx.DocxBlock.FromParagraph(paragraph);
        global::docx.DocxTable table = new global::docx.DocxTable("");
        global::docx.DocxTableCell cell = global::docx.DocxTableCell.Create();
        global::docx.DocxContentControl contentControl = new global::docx.DocxContentControl("");

        Assert.Throws<ArgumentNullException>(() => global::docx.DocxMerger.Append((global::docx.DocxDocument)null!, document));
        Assert.Throws<ArgumentNullException>(() => global::docx.DocxMerger.Append(document, null!));
        Assert.Throws<ArgumentNullException>(() => global::docx.DocxMerger.Append((global::docx.DocxBody)null!, body));
        Assert.Throws<ArgumentNullException>(() => global::docx.DocxMerger.Append(body, null!));
        Assert.Throws<ArgumentNullException>(() => global::docx.DocxMerger.InsertAtBookmark((global::docx.DocxDocument)null!, "Place", document));
        Assert.Throws<ArgumentNullException>(() => global::docx.DocxMerger.InsertAtBookmark(document, "Place", null!));
        Assert.Throws<ArgumentNullException>(() => global::docx.DocxMerger.InsertAtBookmark((global::docx.DocxBody)null!, "Place", body));
        Assert.Throws<ArgumentNullException>(() => global::docx.DocxMerger.InsertAtBookmark(body, null!, body));
        Assert.Throws<ArgumentException>(() => global::docx.DocxMerger.InsertAtBookmark(body, "", body));
        Assert.Throws<ArgumentNullException>(() => global::docx.DocxMerger.InsertAtBookmark(body, "Place", null!));
        Assert.Throws<ArgumentNullException>(() => global::docx.DocxMerger.Append(body, body, null!));
        Assert.Throws<ArgumentNullException>(() => global::docx.DocxMerger.Append((global::docx.DocxBody)null!, body, new global::docx.DocxMergeContext()));
        Assert.Throws<ArgumentNullException>(() => global::docx.DocxMerger.Append(body, null!, new global::docx.DocxMergeContext()));
        Assert.Throws<ArgumentNullException>(() => global::docx.DocxMerger.InsertAtBookmark(body, "Place", body, null!));
        Assert.Throws<ArgumentNullException>(() => global::docx.DocxMerger.InsertAtBookmark((global::docx.DocxBody)null!, "Place", body, new global::docx.DocxMergeContext()));
        Assert.Throws<ArgumentNullException>(() => global::docx.DocxMerger.InsertAtBookmark(body, null!, body, new global::docx.DocxMergeContext()));
        Assert.Throws<ArgumentNullException>(() => global::docx.DocxMerger.InsertAtBookmark(body, "Place", null!, new global::docx.DocxMergeContext()));
        Assert.Throws<ArgumentNullException>(() => global::docx.DocxMerger.FindBookmarkBlockIndex(null!, "Place"));
        Assert.Throws<ArgumentNullException>(() => global::docx.DocxMerger.FindBookmarkBlockIndex(body, null!));
        Assert.Throws<ArgumentNullException>(() => global::docx.DocxMerger.BlockContainsBookmark(null!, "Place"));
        Assert.Throws<ArgumentNullException>(() => global::docx.DocxMerger.BlockContainsBookmark(block, null!));
        Assert.Throws<ArgumentNullException>(() => global::docx.DocxMerger.TableContainsBookmark(null!, "Place"));
        Assert.Throws<ArgumentNullException>(() => global::docx.DocxMerger.TableContainsBookmark(table, null!));
        Assert.Throws<ArgumentNullException>(() => global::docx.DocxMerger.CellContainsBookmark(null!, "Place"));
        Assert.Throws<ArgumentNullException>(() => global::docx.DocxMerger.CellContainsBookmark(cell, null!));
        Assert.Throws<ArgumentNullException>(() => global::docx.DocxMerger.ContentControlContainsBookmark(null!, "Place"));
        Assert.Throws<ArgumentNullException>(() => global::docx.DocxMerger.ContentControlContainsBookmark(contentControl, null!));
        Assert.Throws<ArgumentNullException>(() => global::docx.DocxMerger.ParagraphContainsBookmark(null!, "Place"));
        Assert.Throws<ArgumentNullException>(() => global::docx.DocxMerger.ParagraphContainsBookmark(paragraph, null!));
        Assert.Throws<ArgumentNullException>(() => global::docx.DocxMerger.CloneBlock(null!));
        Assert.Throws<ArgumentNullException>(() => global::docx.DocxMerger.CloneBlock(null!, new global::docx.DocxMergeContext()));
        Assert.Throws<ArgumentNullException>(() => global::docx.DocxMerger.CloneBlock(block, null!));
        Assert.Throws<ArgumentNullException>(() => global::docx.DocxMerger.PrepareDocumentMerge(null!, document));
        Assert.Throws<ArgumentNullException>(() => global::docx.DocxMerger.PrepareDocumentMerge(document, null!));
        Assert.Throws<ArgumentNullException>(() => global::docx.DocxMerger.ImportNumbering(null!, document, new global::docx.DocxMergeContext()));
        Assert.Throws<ArgumentNullException>(() => global::docx.DocxMerger.ImportNumbering(document, null!, new global::docx.DocxMergeContext()));
        Assert.Throws<ArgumentNullException>(() => global::docx.DocxMerger.ImportNumbering(document, document, null!));
        Assert.Throws<ArgumentNullException>(() => global::docx.DocxMerger.ImportMedia(null!, document, new global::docx.DocxMergeContext()));
        Assert.Throws<ArgumentNullException>(() => global::docx.DocxMerger.ImportMedia(document, null!, new global::docx.DocxMergeContext()));
        Assert.Throws<ArgumentNullException>(() => global::docx.DocxMerger.ImportMedia(document, document, null!));
        Assert.Throws<ArgumentNullException>(() => global::docx.DocxMerger.ImportFootnotes(null!, document, new global::docx.DocxMergeContext()));
        Assert.Throws<ArgumentNullException>(() => global::docx.DocxMerger.ImportFootnotes(document, null!, new global::docx.DocxMergeContext()));
        Assert.Throws<ArgumentNullException>(() => global::docx.DocxMerger.ImportFootnotes(document, document, null!));
        Assert.Throws<ArgumentNullException>(() => global::docx.DocxMerger.ImportEndnotes(null!, document, new global::docx.DocxMergeContext()));
        Assert.Throws<ArgumentNullException>(() => global::docx.DocxMerger.ImportEndnotes(document, null!, new global::docx.DocxMergeContext()));
        Assert.Throws<ArgumentNullException>(() => global::docx.DocxMerger.ImportEndnotes(document, document, null!));
        Assert.Throws<ArgumentNullException>(() => global::docx.DocxMerger.ImportComments(null!, document, new global::docx.DocxMergeContext()));
        Assert.Throws<ArgumentNullException>(() => global::docx.DocxMerger.ImportComments(document, null!, new global::docx.DocxMergeContext()));
        Assert.Throws<ArgumentNullException>(() => global::docx.DocxMerger.ImportComments(document, document, null!));
        Assert.Throws<ArgumentNullException>(() => global::docx.DocxMerger.WriteImportedParts(null!, new global::docx.DocxMergeContext()));
        Assert.Throws<ArgumentNullException>(() => global::docx.DocxMerger.WriteImportedParts(document, null!));
        Assert.Throws<ArgumentNullException>(() => global::docx.DocxMerger.RemapXml(null!, new global::docx.DocxMergeContext()));
        Assert.Throws<ArgumentNullException>(() => global::docx.DocxMerger.RemapXml("<root />", null!));
        Assert.Throws<ArgumentNullException>(() => global::docx.DocxMerger.GetBlockXml(null!));
        Assert.Throws<ArgumentNullException>(() => global::docx.DocxMerger.ValidateBookmarkName(null!));
        Assert.Throws<ArgumentException>(() => global::docx.DocxMerger.ValidateBookmarkName(""));
        Assert.Throws<ArgumentNullException>(() => document.Append(null!));
        Assert.Throws<ArgumentNullException>(() => body.Append(null!));
        Assert.Throws<ArgumentNullException>(() => document.InsertAtBookmark(null!, document));
        Assert.Throws<ArgumentNullException>(() => document.InsertAtBookmark("Place", null!));
        Assert.Throws<ArgumentNullException>(() => body.InsertAtBookmark(null!, body));
        Assert.Throws<ArgumentNullException>(() => body.InsertAtBookmark("Place", null!));

        global::docx.DocxMergeContext context = new global::docx.DocxMergeContext();

        Assert.Throws<ArgumentNullException>(() => context.AddFootnoteId(null!, "1"));
        Assert.Throws<ArgumentNullException>(() => context.AddFootnoteId("1", null!));
        Assert.Throws<ArgumentNullException>(() => context.GetFootnoteId(null!));
        Assert.Throws<ArgumentNullException>(() => context.AddEndnoteId(null!, "1"));
        Assert.Throws<ArgumentNullException>(() => context.AddEndnoteId("1", null!));
        Assert.Throws<ArgumentNullException>(() => context.GetEndnoteId(null!));
        Assert.Throws<ArgumentNullException>(() => context.AddCommentId(null!, "1"));
        Assert.Throws<ArgumentNullException>(() => context.AddCommentId("1", null!));
        Assert.Throws<ArgumentNullException>(() => context.GetCommentId(null!));
        Assert.Throws<ArgumentNullException>(() => context.GetBookmarkId(null!));
        Assert.Throws<ArgumentNullException>(() => context.CollectBookmarkIds(null!));
        Assert.Throws<ArgumentNullException>(() => context.AddUsedBookmarkId(null!));
        Assert.Throws<ArgumentNullException>(() => global::docx.DocxMergeContext.AddMapValue(null!, "1", "2"));
        Assert.Throws<ArgumentNullException>(() => global::docx.DocxMergeContext.AddMapValue(new Dictionary<string, string>(), null!, "2"));
        Assert.Throws<ArgumentNullException>(() => global::docx.DocxMergeContext.AddMapValue(new Dictionary<string, string>(), "1", null!));
        Assert.Throws<ArgumentNullException>(() => global::docx.DocxMergeContext.GetMapValue(null!, "1"));
        Assert.Throws<ArgumentNullException>(() => global::docx.DocxMergeContext.GetMapValue(new Dictionary<string, string>(), null!));
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

    static global::docx.DocxPackage CreateMergePackage(string[] bodyChildren, bool target)
    {
        global::docx.DocxPackage package = new global::docx.DocxPackage();
        global::docx.DocxRelationshipSet rootRelationships = global::docx.DocxPackage.CreateRootRelationshipSet("/word/document.xml");
        global::docx.DocxRelationshipSet documentRelationships = new global::docx.DocxRelationshipSet("/word/document.xml");

        if (target)
        {
            documentRelationships.CreateRelationshipWithId("rId1", global::docx.DocxConstants.RelationshipTypeImage, "media/image1.png", false);
            package.AddBinaryPart("/word/media/image1.png", global::docx.DocxConstants.ContentTypePng, new byte[] { 9 });
        }
        else
        {
            documentRelationships.CreateRelationshipWithId("rImageMain", global::docx.DocxConstants.RelationshipTypeImage, "media/image1.png", false);
            package.AddBinaryPart("/word/media/image1.png", global::docx.DocxConstants.ContentTypePng, new byte[] { 1, 2, 3 });
        }

        documentRelationships.CreateRelationshipWithId("rNumbering", global::docx.DocxConstants.RelationshipTypeNumbering, "numbering.xml", false);
        documentRelationships.CreateRelationshipWithId("rFootnotes", global::docx.DocxConstants.RelationshipTypeFootnotes, "footnotes.xml", false);
        documentRelationships.CreateRelationshipWithId("rEndnotes", global::docx.DocxConstants.RelationshipTypeEndnotes, "endnotes.xml", false);
        documentRelationships.CreateRelationshipWithId("rComments", global::docx.DocxConstants.RelationshipTypeComments, "comments.xml", false);
        package.ContentTypes.SetDefault("xml", global::docx.DocxConstants.ContentTypeXml);
        package.ContentTypes.SetDefault("png", global::docx.DocxConstants.ContentTypePng);
        package.AddXmlPartWithContent("/word/document.xml", global::docx.DocxConstants.ContentTypeMainDocument, CreateDocumentXml(bodyChildren));
        package.AddXmlPartWithContent("/word/numbering.xml", global::docx.DocxConstants.ContentTypeNumbering, CreateMergeNumberingXml(target));
        package.AddXmlPartWithContent("/word/footnotes.xml", global::docx.DocxConstants.ContentTypeFootnotes, CreateMergeFootnotesXml(target));
        package.AddXmlPartWithContent("/word/endnotes.xml", global::docx.DocxConstants.ContentTypeEndnotes, CreateMergeEndnotesXml(target));
        package.AddXmlPartWithContent("/word/comments.xml", global::docx.DocxConstants.ContentTypeComments, CreateMergeCommentsXml(target));
        package.AddXmlPartWithContent("_rels/.rels", global::docx.DocxConstants.ContentTypeRelationships, rootRelationships.WriteXml());
        package.AddXmlPartWithContent("/word/_rels/document.xml.rels", global::docx.DocxConstants.ContentTypeRelationships, documentRelationships.WriteXml());

        return package;
    }

    static void AddStylesPart(global::docx.DocxPackage package, string styleChildren)
    {
        package.CreateRelationship("/word/document.xml", global::docx.DocxConstants.RelationshipTypeStyles, "styles.xml", false);
        package.AddXmlPartWithContent("/word/styles.xml", global::docx.DocxConstants.ContentTypeStyles, "<w:styles xmlns:w=\"http://schemas.openxmlformats.org/wordprocessingml/2006/main\">" + styleChildren + "</w:styles>");
    }

    static string CreateDocumentXml(string[] bodyChildren)
    {
        return "<w:document xmlns:w=\"http://schemas.openxmlformats.org/wordprocessingml/2006/main\"><w:body>" + string.Join("", bodyChildren) + "</w:body></w:document>";
    }

    static string CreateBodyXml(string[] bodyChildren)
    {
        return "<w:body xmlns:w=\"http://schemas.openxmlformats.org/wordprocessingml/2006/main\">" + string.Join("", bodyChildren) + "</w:body>";
    }

    static string CreateParagraphXml(string text)
    {
        return "<w:p><w:r><w:t>" + text + "</w:t></w:r></w:p>";
    }

    static string CreateBookmarkParagraphXml(string bookmarkName, string text)
    {
        return "<w:p><w:bookmarkStart w:id=\"1\" w:name=\"" + bookmarkName + "\" /><w:r><w:t>" + text + "</w:t></w:r><w:bookmarkEnd w:id=\"1\" /></w:p>";
    }

    static string CreateReferencedParagraphXml()
    {
        return "<w:p xmlns:w=\"http://schemas.openxmlformats.org/wordprocessingml/2006/main\" xmlns:r=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships\" xmlns:wp=\"http://schemas.openxmlformats.org/drawingml/2006/wordprocessingDrawing\" xmlns:a=\"http://schemas.openxmlformats.org/drawingml/2006/main\" xmlns:pic=\"http://schemas.openxmlformats.org/drawingml/2006/picture\"><w:pPr><w:numPr><w:ilvl w:val=\"0\" /><w:numId w:val=\"1\" /></w:numPr></w:pPr><w:bookmarkStart w:id=\"1\" w:name=\"SourceBookmark\" /><w:commentRangeStart w:id=\"1\" /><w:r><w:t>Inserted</w:t></w:r><w:r><w:footnoteReference w:id=\"1\" /></w:r><w:r><w:endnoteReference w:id=\"1\" /></w:r><w:r><w:commentReference w:id=\"1\" /></w:r><w:r><w:drawing><wp:inline><a:graphic><a:graphicData uri=\"http://schemas.openxmlformats.org/drawingml/2006/picture\"><pic:pic><pic:blipFill><a:blip r:embed=\"rImageMain\" /></pic:blipFill></pic:pic></a:graphicData></a:graphic></wp:inline></w:drawing></w:r><w:commentRangeEnd w:id=\"1\" /><w:bookmarkEnd w:id=\"1\" /></w:p>";
    }

    static string CreateMergeNumberingXml(bool target)
    {
        if (target)
        {
            return "<w:numbering xmlns:w=\"http://schemas.openxmlformats.org/wordprocessingml/2006/main\"><w:abstractNum w:abstractNumId=\"0\"><w:lvl w:ilvl=\"0\"><w:numFmt w:val=\"decimal\" /></w:lvl></w:abstractNum><w:num w:numId=\"1\"><w:abstractNumId w:val=\"0\" /></w:num></w:numbering>";
        }

        return "<w:numbering xmlns:w=\"http://schemas.openxmlformats.org/wordprocessingml/2006/main\"><w:abstractNum w:abstractNumId=\"0\"><w:lvl w:ilvl=\"0\"><w:numFmt w:val=\"lowerLetter\" /></w:lvl></w:abstractNum><w:num w:numId=\"1\"><w:abstractNumId w:val=\"0\" /></w:num></w:numbering>";
    }

    static string CreateMergeFootnotesXml(bool target)
    {
        string text = "Source footnote";

        if (target)
        {
            text = "Target footnote";
        }

        return "<w:footnotes xmlns:w=\"http://schemas.openxmlformats.org/wordprocessingml/2006/main\"><w:footnote w:id=\"1\"><w:p><w:r><w:t>" + text + "</w:t></w:r></w:p></w:footnote></w:footnotes>";
    }

    static string CreateMergeEndnotesXml(bool target)
    {
        string text = "Source endnote";

        if (target)
        {
            text = "Target endnote";
        }

        return "<w:endnotes xmlns:w=\"http://schemas.openxmlformats.org/wordprocessingml/2006/main\"><w:endnote w:id=\"1\"><w:p><w:r><w:t>" + text + "</w:t></w:r></w:p></w:endnote></w:endnotes>";
    }

    static string CreateMergeCommentsXml(bool target)
    {
        string text = "Source comment";

        if (target)
        {
            text = "Target comment";
        }

        return "<w:comments xmlns:w=\"http://schemas.openxmlformats.org/wordprocessingml/2006/main\"><w:comment w:id=\"1\" w:author=\"Author\" w:initials=\"A\"><w:p><w:r><w:t>" + text + "</w:t></w:r></w:p></w:comment></w:comments>";
    }

    static global::docx.DocxBody ParseBody(string xml)
    {
        global::docx.DocxXmlReader reader = global::docx.DocxXmlReader.Create(xml);

        Assert.That(reader.ReadToElement("body", global::docx.DocxConstants.NamespaceWordprocessingMl), Is.True);

        return global::docx.DocxBody.Parse(reader);
    }
}
