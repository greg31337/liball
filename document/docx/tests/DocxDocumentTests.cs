using NUnit.Framework;

namespace docx.Tests;

[TestFixture]
public class DocxDocumentTests
{
    [Test]
    public void CreateMinimalMainDocumentXml_GeneratesDocumentRootAndEmptyBody()
    {
        string xml = global::docx.DocxDocument.CreateMinimalMainDocumentXml();
        global::docx.DocxXmlReader reader = global::docx.DocxXmlReader.Create(xml);

        Assert.That(xml, Is.EqualTo("<w:document xmlns:w=\"http://schemas.openxmlformats.org/wordprocessingml/2006/main\"><w:body /></w:document>"));
        Assert.That(reader.MoveToContent(), Is.True);
        Assert.That(reader.IsElement("document", global::docx.DocxConstants.NamespaceWordprocessingMl), Is.True);
        Assert.That(reader.ReadToChildElement("body", global::docx.DocxConstants.NamespaceWordprocessingMl), Is.True);
    }

    [Test]
    public void Create_BuildsMinimalDocumentPackageAndParsesBody()
    {
        global::docx.DocxDocument document = global::docx.DocxDocument.Create();

        Assert.That(document.Package.HasMainDocumentPart(), Is.True);
        Assert.That(document.MainDocumentPart, Is.Not.Null);
        Assert.That(document.MainDocumentPart!.GetText(), Is.EqualTo(global::docx.DocxDocument.CreateMinimalMainDocumentXml()));
        Assert.That(document.HasMainDocumentRoot, Is.True);
        Assert.That(document.HasBody(), Is.True);
    }

    [Test]
    public void Create_InitializesOnlyMainDocumentAndEmptyModels()
    {
        global::docx.DocxDocument document = global::docx.DocxDocument.Create();

        Assert.That(document.Body.BlockCount(), Is.EqualTo(0));
        Assert.That(document.Body.ParagraphCount(), Is.EqualTo(0));
        Assert.That(document.SectionCount(), Is.EqualTo(0));
        Assert.That(document.HeaderCount(), Is.EqualTo(0));
        Assert.That(document.FooterCount(), Is.EqualTo(0));
        Assert.That(document.FootnoteCount(), Is.EqualTo(0));
        Assert.That(document.EndnoteCount(), Is.EqualTo(0));
        Assert.That(document.CommentCount(), Is.EqualTo(0));
        Assert.That(document.ImageCount(), Is.EqualTo(0));
        Assert.That(document.CustomXmlPartCount(), Is.EqualTo(0));
        Assert.That(document.GetText(), Is.EqualTo(""));
        Assert.That(document.StylesPart, Is.Null);
        Assert.That(document.NumberingPart, Is.Null);
        Assert.That(document.SettingsPart, Is.Null);
        Assert.That(document.HasStylesRoot, Is.False);
        Assert.That(document.HasNumberingRoot, Is.False);
        Assert.That(document.HasSettingsRoot, Is.False);
    }

    [Test]
    public void Create_MinimalDocumentBodyCanBePopulatedAndSerialized()
    {
        global::docx.DocxDocument document = global::docx.DocxDocument.Create();
        global::docx.DocxParagraph paragraph = CreateParagraph("Hello minimal");

        document.Body.AddBlock(global::docx.DocxBlock.FromParagraph(paragraph));

        string bodyXml = document.Body.WriteXml();

        Assert.That(document.Body.BlockCount(), Is.EqualTo(1));
        Assert.That(document.Body.GetText(), Is.EqualTo("Hello minimal"));
        Assert.That(bodyXml, Does.Contain("<w:t"));
        Assert.That(bodyXml, Does.Contain(">Hello minimal</w:t>"));
        Assert.That(bodyXml, Does.StartWith("<w:body"));
    }

    [Test]
    public void Create_MinimalDocumentCanBeSavedAndOpenedAsDocument()
    {
        global::docx.DocxDocument document = global::docx.DocxDocument.Create();
        MemoryStream saved = new MemoryStream();

        document.Package.Save(saved);

        saved.Position = 0;

        global::docx.DocxDocument reopened = global::docx.DocxDocument.OpenPackage(global::docx.DocxPackage.Open(saved));

        Assert.That(reopened.HasMainDocumentRoot, Is.True);
        Assert.That(reopened.HasBody(), Is.True);
        Assert.That(reopened.GetText(), Is.EqualTo(""));
        Assert.That(reopened.Body.BlockCount(), Is.EqualTo(0));
        Assert.That(reopened.Package.GetMainDocumentPartName(), Is.EqualTo("/word/document.xml"));
    }

    [Test]
    public void WriteMinimalMainDocument_WritesToExistingWriter()
    {
        global::docx.DocxXmlWriter writer = global::docx.DocxXmlWriter.Create();

        global::docx.DocxDocument.WriteMinimalMainDocument(writer);

        Assert.That(writer.GetXml(), Is.EqualTo(global::docx.DocxDocument.CreateMinimalMainDocumentXml()));
    }

    [Test]
    public void OpenPackage_ParsesMainDocumentRootAndBody()
    {
        global::docx.DocxPackage package = CreatePackageWithMainDocumentXml(CreateDocumentXml("<w:p />"));

        global::docx.DocxDocument document = global::docx.DocxDocument.OpenPackage(package);

        Assert.That(document.Package, Is.SameAs(package));
        Assert.That(document.MainDocumentPart, Is.SameAs(package.GetMainDocumentPart()));
        Assert.That(document.HasMainDocumentRoot, Is.True);
        Assert.That(document.HasBody(), Is.True);

        global::docx.DocxXmlReader bodyReader = global::docx.DocxXmlReader.Create(document.GetBody().GetXml());

        Assert.That(bodyReader.ReadToElement("body", global::docx.DocxConstants.NamespaceWordprocessingMl), Is.True);
        Assert.That(bodyReader.ReadToChildElement("p", global::docx.DocxConstants.NamespaceWordprocessingMl), Is.True);
    }

    [Test]
    public void OpenPackage_ExposesDiscoveredSections()
    {
        global::docx.DocxPackage package = CreatePackageWithMainDocumentXml(CreateDocumentXml("<w:p><w:pPr><w:sectPr><w:type w:val=\"continuous\" /></w:sectPr></w:pPr></w:p><w:sectPr><w:pgSz w:w=\"12240\" w:h=\"15840\" /></w:sectPr>"));

        global::docx.DocxDocument document = global::docx.DocxDocument.OpenPackage(package);

        Assert.That(document.SectionCount(), Is.EqualTo(2));
        Assert.That(document.GetSections()[0].Format.SectionType, Is.EqualTo("continuous"));
        Assert.That(document.GetSections()[1].Format.PageWidth, Is.EqualTo("12240"));
        Assert.That(document.GetSections()[1].Format.PageHeight, Is.EqualTo("15840"));
    }

    [Test]
    public void OpenPackage_ResolvesSectionHeaderAndFooterReferences()
    {
        global::docx.DocxPackage package = CreatePackageWithHeaderFooterReferences();

        global::docx.DocxDocument document = global::docx.DocxDocument.OpenPackage(package);
        global::docx.DocxSection section = document.GetSections()[0];

        Assert.That(document.SectionCount(), Is.EqualTo(1));
        Assert.That(document.HeaderCount(), Is.EqualTo(3));
        Assert.That(document.FooterCount(), Is.EqualTo(3));
        Assert.That(section.Format.DefaultHeaderRelationshipId, Is.EqualTo("rHeaderDefault"));
        Assert.That(section.Format.EvenHeaderRelationshipId, Is.EqualTo("rHeaderEven"));
        Assert.That(section.Format.FirstHeaderRelationshipId, Is.EqualTo("rHeaderFirst"));
        Assert.That(section.Format.DefaultFooterRelationshipId, Is.EqualTo("rFooterDefault"));
        Assert.That(section.Format.EvenFooterRelationshipId, Is.EqualTo("rFooterEven"));
        Assert.That(section.Format.FirstFooterRelationshipId, Is.EqualTo("rFooterFirst"));
        Assert.That(document.FindHeaderByRelationshipId("rHeaderDefault")!.PartName, Is.EqualTo("/word/header-default.xml"));
        Assert.That(document.FindFooterByRelationshipId("rFooterDefault")!.PartName, Is.EqualTo("/word/footer-default.xml"));
        Assert.That(document.FindDefaultHeader(section)!.RelationshipId, Is.EqualTo("rHeaderDefault"));
        Assert.That(document.FindEvenHeader(section)!.RelationshipId, Is.EqualTo("rHeaderEven"));
        Assert.That(document.FindFirstHeader(section)!.RelationshipId, Is.EqualTo("rHeaderFirst"));
        Assert.That(document.FindDefaultFooter(section)!.RelationshipId, Is.EqualTo("rFooterDefault"));
        Assert.That(document.FindEvenFooter(section)!.RelationshipId, Is.EqualTo("rFooterEven"));
        Assert.That(document.FindFirstFooter(section)!.RelationshipId, Is.EqualTo("rFooterFirst"));
        Assert.That(document.FindHeaderByRelationshipId(""), Is.Null);
        Assert.That(document.FindHeaderByRelationshipId("missing"), Is.Null);
        Assert.That(document.FindFooterByRelationshipId(""), Is.Null);
        Assert.That(document.FindFooterByRelationshipId("missing"), Is.Null);
    }

    [Test]
    public void OpenPackage_ResolvesLinkedHeaderAndFooterFromPreviousSection()
    {
        global::docx.DocxPackage package = CreatePackageWithLinkedHeaderFooterSections();

        global::docx.DocxDocument document = global::docx.DocxDocument.OpenPackage(package);
        global::docx.DocxSection firstSection = document.GetSections()[0];
        global::docx.DocxSection secondSection = document.GetSections()[1];

        Assert.That(document.SectionCount(), Is.EqualTo(2));
        Assert.That(document.FindDefaultHeader(firstSection)!.RelationshipId, Is.EqualTo("rHeaderFirstDefault"));
        Assert.That(document.FindDefaultFooter(firstSection)!.RelationshipId, Is.EqualTo("rFooterFirstDefault"));
        Assert.That(document.FindDefaultHeader(secondSection), Is.Null);
        Assert.That(document.FindDefaultFooter(secondSection), Is.Null);
        Assert.That(document.IsHeaderLinkedToPrevious(firstSection, global::docx.DocxSectionFormat.HeaderFooterReferenceDefault), Is.False);
        Assert.That(document.IsFooterLinkedToPrevious(firstSection, global::docx.DocxSectionFormat.HeaderFooterReferenceDefault), Is.False);
        Assert.That(document.IsHeaderLinkedToPrevious(secondSection, global::docx.DocxSectionFormat.HeaderFooterReferenceDefault), Is.True);
        Assert.That(document.IsFooterLinkedToPrevious(secondSection, global::docx.DocxSectionFormat.HeaderFooterReferenceDefault), Is.True);
        Assert.That(document.FindEffectiveDefaultHeader(secondSection)!.RelationshipId, Is.EqualTo("rHeaderFirstDefault"));
        Assert.That(document.FindEffectiveDefaultFooter(secondSection)!.RelationshipId, Is.EqualTo("rFooterFirstDefault"));
        Assert.That(document.FindEffectiveFirstHeader(secondSection)!.RelationshipId, Is.EqualTo("rHeaderSecondFirst"));
        Assert.That(document.FindEffectiveEvenFooter(secondSection)!.RelationshipId, Is.EqualTo("rFooterSecondEven"));
        Assert.That(document.FindEffectiveEvenHeader(secondSection), Is.Null);
    }

    [Test]
    public void CreateHeaderAndFooter_AddsPartsRelationshipsAndModels()
    {
        global::docx.DocxDocument document = global::docx.DocxDocument.Create();

        global::docx.DocxHeader header = document.CreateHeader();
        global::docx.DocxFooter footer = document.CreateFooter();
        global::docx.DocxHeader secondHeader = document.CreateHeader();

        global::docx.DocxRelationshipSet? relationshipSet = document.Package.FindRelationshipSet("/word/document.xml");
        global::docx.DocxPart? relationshipPart = document.Package.FindPart("/word/_rels/document.xml.rels");

        Assert.That(document.HeaderCount(), Is.EqualTo(2));
        Assert.That(document.FooterCount(), Is.EqualTo(1));
        Assert.That(header.PartName, Is.EqualTo("/word/header1.xml"));
        Assert.That(header.RelationshipId, Is.EqualTo("rId1"));
        Assert.That(footer.PartName, Is.EqualTo("/word/footer1.xml"));
        Assert.That(footer.RelationshipId, Is.EqualTo("rId2"));
        Assert.That(secondHeader.PartName, Is.EqualTo("/word/header2.xml"));
        Assert.That(secondHeader.RelationshipId, Is.EqualTo("rId3"));
        Assert.That(document.Package.ContainsPart("/word/header1.xml"), Is.True);
        Assert.That(document.Package.ContainsPart("/word/header2.xml"), Is.True);
        Assert.That(document.Package.ContainsPart("/word/footer1.xml"), Is.True);
        Assert.That(document.Package.FindPart("/word/header1.xml")!.GetText(), Is.EqualTo(global::docx.DocxHeader.CreateMinimalXml()));
        Assert.That(document.Package.FindPart("/word/footer1.xml")!.GetText(), Is.EqualTo(global::docx.DocxFooter.CreateMinimalXml()));
        Assert.That(document.Package.ContentTypes.GetContentType("/word/header1.xml"), Is.EqualTo(global::docx.DocxConstants.ContentTypeHeader));
        Assert.That(document.Package.ContentTypes.GetContentType("/word/footer1.xml"), Is.EqualTo(global::docx.DocxConstants.ContentTypeFooter));
        Assert.That(relationshipSet, Is.Not.Null);
        Assert.That(relationshipSet!.Count(), Is.EqualTo(3));
        Assert.That(relationshipSet.FindById("rId1")!.Type, Is.EqualTo(global::docx.DocxConstants.RelationshipTypeHeader));
        Assert.That(relationshipSet.FindById("rId1")!.Target, Is.EqualTo("header1.xml"));
        Assert.That(relationshipSet.FindById("rId2")!.Type, Is.EqualTo(global::docx.DocxConstants.RelationshipTypeFooter));
        Assert.That(relationshipSet.FindById("rId2")!.Target, Is.EqualTo("footer1.xml"));
        Assert.That(relationshipSet.FindById("rId3")!.Target, Is.EqualTo("header2.xml"));
        Assert.That(relationshipPart, Is.Not.Null);
        Assert.That(relationshipPart!.GetText(), Does.Contain("header1.xml"));
        Assert.That(relationshipPart.GetText(), Does.Contain("footer1.xml"));
        Assert.That(document.FindHeaderByRelationshipId("rId1"), Is.SameAs(header));
        Assert.That(document.FindFooterByRelationshipId("rId2"), Is.SameAs(footer));
    }

    [Test]
    public void CreateHeaderAndFooter_WithSectionAssignsReferences()
    {
        global::docx.DocxDocument document = global::docx.DocxDocument.Create();
        global::docx.DocxSection section = new global::docx.DocxSection("<w:sectPr xmlns:w=\"http://schemas.openxmlformats.org/wordprocessingml/2006/main\"><w:type w:val=\"continuous\" /></w:sectPr>");

        global::docx.DocxHeader header = document.CreateHeaderForSection(section, global::docx.DocxSectionFormat.HeaderFooterReferenceFirst);
        global::docx.DocxFooter footer = document.CreateFooterForSection(section, global::docx.DocxSectionFormat.HeaderFooterReferenceEven);
        string xml = section.WriteXml();

        Assert.That(section.Format.FirstHeaderRelationshipId, Is.EqualTo(header.RelationshipId));
        Assert.That(section.Format.EvenFooterRelationshipId, Is.EqualTo(footer.RelationshipId));
        Assert.That(document.FindFirstHeader(section), Is.SameAs(header));
        Assert.That(document.FindEvenFooter(section), Is.SameAs(footer));
        Assert.That(xml, Does.Contain("headerReference"));
        Assert.That(xml, Does.Contain("footerReference"));
        Assert.That(xml, Does.Contain("r:id=\"" + header.RelationshipId + "\""));
        Assert.That(xml, Does.Contain("r:id=\"" + footer.RelationshipId + "\""));
        Assert.That(xml, Does.Contain("continuous"));
    }

    [Test]
    public void LinkHeaderAndFooterToPrevious_ClearsDirectReferenceAndUsesPreviousSection()
    {
        global::docx.DocxDocument document = global::docx.DocxDocument.Create();
        global::docx.DocxSection firstSection = new global::docx.DocxSection("");
        global::docx.DocxSection secondSection = new global::docx.DocxSection("");

        document.Body.AddSection(firstSection);
        document.Body.AddSection(secondSection);

        global::docx.DocxHeader firstHeader = document.CreateHeaderForSection(firstSection, global::docx.DocxSectionFormat.HeaderFooterReferenceDefault);
        global::docx.DocxHeader secondHeader = document.CreateHeaderForSection(secondSection, global::docx.DocxSectionFormat.HeaderFooterReferenceDefault);
        global::docx.DocxFooter firstFooter = document.CreateFooterForSection(firstSection, global::docx.DocxSectionFormat.HeaderFooterReferenceFirst);
        global::docx.DocxFooter secondFooter = document.CreateFooterForSection(secondSection, global::docx.DocxSectionFormat.HeaderFooterReferenceFirst);

        Assert.That(document.FindDefaultHeader(secondSection), Is.SameAs(secondHeader));
        Assert.That(document.FindFirstFooter(secondSection), Is.SameAs(secondFooter));

        document.LinkHeaderToPrevious(secondSection, global::docx.DocxSectionFormat.HeaderFooterReferenceDefault);
        document.LinkFooterToPrevious(secondSection, global::docx.DocxSectionFormat.HeaderFooterReferenceFirst);

        Assert.That(secondSection.Format.DefaultHeaderRelationshipId, Is.EqualTo(""));
        Assert.That(secondSection.Format.FirstFooterRelationshipId, Is.EqualTo(""));
        Assert.That(document.IsHeaderLinkedToPrevious(secondSection, global::docx.DocxSectionFormat.HeaderFooterReferenceDefault), Is.True);
        Assert.That(document.IsFooterLinkedToPrevious(secondSection, global::docx.DocxSectionFormat.HeaderFooterReferenceFirst), Is.True);
        Assert.That(document.FindDefaultHeader(secondSection), Is.Null);
        Assert.That(document.FindFirstFooter(secondSection), Is.Null);
        Assert.That(document.FindEffectiveDefaultHeader(secondSection), Is.SameAs(firstHeader));
        Assert.That(document.FindEffectiveFirstFooter(secondSection), Is.SameAs(firstFooter));
        Assert.That(secondSection.WriteXml(), Does.Not.Contain("r:id=\"" + secondHeader.RelationshipId + "\""));
        Assert.That(secondSection.WriteXml(), Does.Not.Contain("r:id=\"" + secondFooter.RelationshipId + "\""));
    }

    [Test]
    public void SectionManagement_CreatesInsertsRemovesAndRoundTripsMultipleSections()
    {
        global::docx.IDocxDocumentPersistence persistence = new global::docx.DocxPackagePersistence();
        global::docx.IDocxSectionManagement sections = new global::docx.DocxSectionManagement();
        global::docx.DocxContentFactory factory = new global::docx.DocxContentFactory();
        global::docx.DocxDocument document = persistence.CreateDocument();
        document.GetBody().AddBlock(factory.CreateParagraphBlock(factory.CreateTextParagraph("Before")));
        document.GetBody().AddBlock(factory.CreateParagraphBlock(factory.CreateTextParagraph("After")));
        global::docx.DocxSection first = sections.CreateSection();
        global::docx.DocxSection second = sections.CreateSection();
        global::docx.DocxSection final = sections.CreateSection();
        first.Format.SectionType = "continuous";
        second.Format.SectionType = "nextPage";
        final.Format.SectionType = "oddPage";

        sections.InsertSectionBreak(document, 1, first);
        sections.AppendSectionBreak(document, second);
        sections.SetFinalSection(document, final);

        Assert.That(sections.GetSections(document), Is.EqualTo(new[] { first, second, final }));
        Assert.That(document.GetBody().BlockCount(), Is.EqualTo(4));
        Assert.That(sections.RemoveSection(document, first), Is.True);
        Assert.That(sections.RemoveSection(document, first), Is.False);
        Assert.That(sections.GetSections(document), Is.EqualTo(new[] { second, final }));

        global::docx.DocxDocument reopened = RoundTripDocument(persistence, document);
        Assert.That(sections.GetSections(reopened).Count, Is.EqualTo(2));
        Assert.That(sections.GetSections(reopened)[0].Format.SectionType, Is.EqualTo("nextPage"));
        Assert.That(sections.GetSections(reopened)[1].Format.SectionType, Is.EqualTo("oddPage"));
        Assert.That(reopened.GetBody().GetText(), Does.Contain("Before"));
        Assert.That(reopened.GetBody().GetText(), Does.Contain("After"));
    }

    [Test]
    public void SectionManagement_CreatesAllStoryKindsAndResolvesInheritance()
    {
        global::docx.IDocxDocumentPersistence persistence = new global::docx.DocxPackagePersistence();
        global::docx.IDocxSectionManagement sections = new global::docx.DocxSectionManagement();
        global::docx.DocxDocument document = persistence.CreateDocument();
        global::docx.DocxSection first = sections.CreateSection();
        global::docx.DocxSection second = sections.CreateSection();
        sections.AppendSectionBreak(document, first);
        sections.SetFinalSection(document, second);
        sections.SetFirstPageStories(first, true);
        sections.SetEvenPageStories(document, true);

        global::docx.DocxHeader defaultHeader = sections.CreateHeader(document, first, global::docx.DocxHeaderFooterKind.Default);
        global::docx.DocxHeader evenHeader = sections.CreateHeader(document, first, global::docx.DocxHeaderFooterKind.Even);
        global::docx.DocxHeader firstHeader = sections.CreateHeader(document, first, global::docx.DocxHeaderFooterKind.First);
        global::docx.DocxFooter defaultFooter = sections.CreateFooter(document, first, global::docx.DocxHeaderFooterKind.Default);
        global::docx.DocxFooter evenFooter = sections.CreateFooter(document, first, global::docx.DocxHeaderFooterKind.Even);
        global::docx.DocxFooter firstFooter = sections.CreateFooter(document, first, global::docx.DocxHeaderFooterKind.First);

        Assert.That(first.Format.HasTitlePage, Is.True);
        Assert.That(first.Format.TitlePage, Is.True);
        Assert.That(document.GetSettings().HasEvenAndOddHeaders, Is.True);
        Assert.That(document.GetSettings().EvenAndOddHeaders, Is.True);
        Assert.That(sections.FindHeader(document, first, global::docx.DocxHeaderFooterKind.Default), Is.SameAs(defaultHeader));
        Assert.That(sections.FindHeader(document, first, global::docx.DocxHeaderFooterKind.Even), Is.SameAs(evenHeader));
        Assert.That(sections.FindHeader(document, first, global::docx.DocxHeaderFooterKind.First), Is.SameAs(firstHeader));
        Assert.That(sections.FindFooter(document, first, global::docx.DocxHeaderFooterKind.Default), Is.SameAs(defaultFooter));
        Assert.That(sections.FindFooter(document, first, global::docx.DocxHeaderFooterKind.Even), Is.SameAs(evenFooter));
        Assert.That(sections.FindFooter(document, first, global::docx.DocxHeaderFooterKind.First), Is.SameAs(firstFooter));
        Assert.That(sections.IsHeaderLinkedToPrevious(document, second, global::docx.DocxHeaderFooterKind.Default), Is.True);
        Assert.That(sections.IsFooterLinkedToPrevious(document, second, global::docx.DocxHeaderFooterKind.First), Is.True);
        Assert.That(sections.FindEffectiveHeader(document, second, global::docx.DocxHeaderFooterKind.Default), Is.SameAs(defaultHeader));
        Assert.That(sections.FindEffectiveHeader(document, second, global::docx.DocxHeaderFooterKind.Even), Is.SameAs(evenHeader));
        Assert.That(sections.FindEffectiveFooter(document, second, global::docx.DocxHeaderFooterKind.First), Is.SameAs(firstFooter));

        global::docx.DocxHeader replacement = sections.CreateHeader(document, second, global::docx.DocxHeaderFooterKind.Default);
        sections.AssignHeader(document, second, global::docx.DocxHeaderFooterKind.Even, replacement);
        Assert.That(sections.FindHeader(document, second, global::docx.DocxHeaderFooterKind.Even), Is.SameAs(replacement));
        sections.LinkHeaderToPrevious(document, second, global::docx.DocxHeaderFooterKind.Default);
        Assert.That(sections.FindEffectiveHeader(document, second, global::docx.DocxHeaderFooterKind.Default), Is.SameAs(defaultHeader));
        Assert.That(sections.GetHeaders(document).Count, Is.EqualTo(4));
        Assert.That(sections.GetFooters(document).Count, Is.EqualTo(3));
    }

    [Test]
    public void SectionManagement_EditsHeaderFooterBlocksAndSavesStoryParts()
    {
        global::docx.IDocxDocumentPersistence persistence = new global::docx.DocxPackagePersistence();
        global::docx.IDocxSectionManagement sections = new global::docx.DocxSectionManagement();
        global::docx.DocxContentFactory factory = new global::docx.DocxContentFactory();
        global::docx.DocxDocument document = persistence.CreateDocument();
        global::docx.DocxSection section = sections.CreateSection();
        sections.SetFinalSection(document, section);
        global::docx.DocxHeader header = sections.CreateHeader(document, section, global::docx.DocxHeaderFooterKind.Default);
        global::docx.DocxFooter footer = sections.CreateFooter(document, section, global::docx.DocxHeaderFooterKind.Default);
        global::docx.DocxBlock headerFirst = factory.CreateParagraphBlock(factory.CreateTextParagraph("Header First"));
        global::docx.DocxBlock headerSecond = factory.CreateParagraphBlock(factory.CreateTextParagraph("Header Second"));
        global::docx.DocxBlock headerTable = factory.CreateTableBlock(factory.CreateTable(1, 1));
        sections.AppendHeaderBlock(header, headerFirst);
        sections.AppendHeaderBlock(header, headerTable);
        sections.InsertHeaderBlock(header, 1, headerSecond);
        sections.MoveHeaderBlock(header, 2, 0);
        Assert.That(sections.RemoveHeaderBlock(header, 1), Is.SameAs(headerFirst));
        global::docx.DocxBlock footerFirst = factory.CreateParagraphBlock(factory.CreateTextParagraph("Footer First"));
        global::docx.DocxBlock footerSecond = factory.CreateParagraphBlock(factory.CreateTextParagraph("Footer Second"));
        sections.AppendFooterBlock(footer, footerFirst);
        sections.InsertFooterBlock(footer, 0, footerSecond);
        sections.MoveFooterBlock(footer, 1, 0);

        global::docx.DocxDocument reopened = RoundTripDocument(persistence, document);
        global::docx.DocxSection reopenedSection = sections.GetSections(reopened)[0];
        global::docx.DocxHeader? reopenedHeader = sections.FindHeader(reopened, reopenedSection, global::docx.DocxHeaderFooterKind.Default);
        global::docx.DocxFooter? reopenedFooter = sections.FindFooter(reopened, reopenedSection, global::docx.DocxHeaderFooterKind.Default);
        Assert.That(reopenedHeader, Is.Not.Null);
        Assert.That(reopenedFooter, Is.Not.Null);
        Assert.That(sections.GetHeaderBlocks(reopenedHeader!).Count, Is.EqualTo(2));
        Assert.That(sections.GetHeaderBlocks(reopenedHeader!)[0].IsTable(), Is.True);
        Assert.That(sections.GetHeaderBlocks(reopenedHeader!)[1].GetText(), Is.EqualTo("Header Second"));
        Assert.That(sections.GetFooterBlocks(reopenedFooter!)[0].GetText(), Is.EqualTo("Footer First"));
        Assert.That(sections.GetFooterBlocks(reopenedFooter!)[1].GetText(), Is.EqualTo("Footer Second"));
        Assert.That(reopenedHeader!.GetText(), Does.Contain("Header Second"));
        Assert.That(reopenedFooter!.GetText(), Is.EqualTo("Footer First\nFooter Second"));
    }

    [Test]
    public void SectionManagement_RemovesStoryPartsRelationshipsAndReferences()
    {
        global::docx.IDocxDocumentPersistence persistence = new global::docx.DocxPackagePersistence();
        global::docx.IDocxSectionManagement sections = new global::docx.DocxSectionManagement();
        global::docx.DocxDocument document = persistence.CreateDocument();
        global::docx.DocxSection first = sections.CreateSection();
        global::docx.DocxSection final = sections.CreateSection();
        sections.AppendSectionBreak(document, first);
        sections.SetFinalSection(document, final);
        global::docx.DocxHeader header = sections.CreateHeader(document, first, global::docx.DocxHeaderFooterKind.Default);
        global::docx.DocxFooter footer = sections.CreateFooter(document, first, global::docx.DocxHeaderFooterKind.Default);
        sections.AssignHeader(document, final, global::docx.DocxHeaderFooterKind.First, header);
        sections.AssignFooter(document, final, global::docx.DocxHeaderFooterKind.Even, footer);

        Assert.That(sections.RemoveHeader(document, header), Is.True);
        Assert.That(sections.RemoveHeader(document, header), Is.False);
        Assert.That(sections.RemoveFooter(document, footer), Is.True);
        Assert.That(sections.FindHeader(document, first, global::docx.DocxHeaderFooterKind.Default), Is.Null);
        Assert.That(sections.FindHeader(document, final, global::docx.DocxHeaderFooterKind.First), Is.Null);
        Assert.That(sections.FindFooter(document, first, global::docx.DocxHeaderFooterKind.Default), Is.Null);
        Assert.That(sections.FindFooter(document, final, global::docx.DocxHeaderFooterKind.Even), Is.Null);
        Assert.That(sections.GetHeaders(document), Is.Empty);
        Assert.That(sections.GetFooters(document), Is.Empty);

        global::docx.DocxDocument reopened = RoundTripDocument(persistence, document);
        Assert.That(sections.GetHeaders(reopened), Is.Empty);
        Assert.That(sections.GetFooters(reopened), Is.Empty);
        Assert.That(sections.GetSections(reopened)[0].Format.DefaultHeaderRelationshipId, Is.EqualTo(""));
        Assert.That(sections.GetSections(reopened)[1].Format.EvenFooterRelationshipId, Is.EqualTo(""));
    }

    [Test]
    public void SectionManagement_PersistsRelationshipEditsOnParsedSectionBreaks()
    {
        global::docx.IDocxDocumentPersistence persistence = new global::docx.DocxPackagePersistence();
        global::docx.IDocxSectionManagement sections = new global::docx.DocxSectionManagement();
        global::docx.DocxDocument document = persistence.CreateDocument();
        global::docx.DocxSection first = sections.CreateSection();
        global::docx.DocxSection linked = sections.CreateSection();
        global::docx.DocxSection removed = sections.CreateSection();
        global::docx.DocxSection assigned = sections.CreateSection();
        global::docx.DocxSection final = sections.CreateSection();
        sections.AppendSectionBreak(document, first);
        sections.AppendSectionBreak(document, linked);
        sections.AppendSectionBreak(document, removed);
        sections.AppendSectionBreak(document, assigned);
        sections.SetFinalSection(document, final);
        global::docx.DocxHeader inheritedHeader = sections.CreateHeader(document, first, global::docx.DocxHeaderFooterKind.Default);
        global::docx.DocxFooter inheritedFooter = sections.CreateFooter(document, first, global::docx.DocxHeaderFooterKind.Default);
        sections.CreateHeader(document, linked, global::docx.DocxHeaderFooterKind.Default);
        sections.CreateFooter(document, linked, global::docx.DocxHeaderFooterKind.Default);
        sections.CreateHeader(document, removed, global::docx.DocxHeaderFooterKind.Even);
        sections.CreateFooter(document, removed, global::docx.DocxHeaderFooterKind.Even);
        string inheritedHeaderId = inheritedHeader.RelationshipId;
        string inheritedFooterId = inheritedFooter.RelationshipId;

        document = RoundTripDocument(persistence, document);
        List<global::docx.DocxSection> parsedSections = sections.GetSections(document);
        global::docx.DocxHeader removedHeader = sections.FindHeader(document, parsedSections[2], global::docx.DocxHeaderFooterKind.Even)!;
        global::docx.DocxFooter removedFooter = sections.FindFooter(document, parsedSections[2], global::docx.DocxHeaderFooterKind.Even)!;
        sections.LinkHeaderToPrevious(document, parsedSections[1], global::docx.DocxHeaderFooterKind.Default);
        sections.LinkFooterToPrevious(document, parsedSections[1], global::docx.DocxHeaderFooterKind.Default);
        Assert.That(sections.RemoveHeader(document, removedHeader), Is.True);
        Assert.That(sections.RemoveFooter(document, removedFooter), Is.True);
        global::docx.DocxHeader createdHeader = sections.CreateHeader(document, parsedSections[3], global::docx.DocxHeaderFooterKind.Even);
        global::docx.DocxFooter createdFooter = sections.CreateFooter(document, parsedSections[3], global::docx.DocxHeaderFooterKind.Even);
        sections.AssignHeader(document, parsedSections[3], global::docx.DocxHeaderFooterKind.First, createdHeader);
        sections.AssignFooter(document, parsedSections[3], global::docx.DocxHeaderFooterKind.First, createdFooter);
        string createdHeaderId = createdHeader.RelationshipId;
        string createdFooterId = createdFooter.RelationshipId;

        global::docx.DocxDocument reopened = RoundTripDocument(persistence, document);
        List<global::docx.DocxSection> reopenedSections = sections.GetSections(reopened);

        Assert.That(sections.FindEffectiveHeader(reopened, reopenedSections[1], global::docx.DocxHeaderFooterKind.Default)!.RelationshipId, Is.EqualTo(inheritedHeaderId));
        Assert.That(sections.FindEffectiveFooter(reopened, reopenedSections[1], global::docx.DocxHeaderFooterKind.Default)!.RelationshipId, Is.EqualTo(inheritedFooterId));
        Assert.That(sections.FindHeader(reopened, reopenedSections[2], global::docx.DocxHeaderFooterKind.Even), Is.Null);
        Assert.That(sections.FindFooter(reopened, reopenedSections[2], global::docx.DocxHeaderFooterKind.Even), Is.Null);
        Assert.That(sections.FindHeader(reopened, reopenedSections[3], global::docx.DocxHeaderFooterKind.Even)!.RelationshipId, Is.EqualTo(createdHeaderId));
        Assert.That(sections.FindHeader(reopened, reopenedSections[3], global::docx.DocxHeaderFooterKind.First)!.RelationshipId, Is.EqualTo(createdHeaderId));
        Assert.That(sections.FindFooter(reopened, reopenedSections[3], global::docx.DocxHeaderFooterKind.Even)!.RelationshipId, Is.EqualTo(createdFooterId));
        Assert.That(sections.FindFooter(reopened, reopenedSections[3], global::docx.DocxHeaderFooterKind.First)!.RelationshipId, Is.EqualTo(createdFooterId));
    }

    [Test]
    public void ParseMainDocument_ReturnsFalseWhenMainDocumentPartIsMissing()
    {
        global::docx.DocxDocument document = new global::docx.DocxDocument(new global::docx.DocxPackage());

        Assert.That(document.ParseMainDocument(), Is.False);
        Assert.That(document.MainDocumentPart, Is.Null);
        Assert.That(document.HasMainDocumentRoot, Is.False);
        Assert.That(document.HasBody(), Is.False);
    }

    [Test]
    public void ParseMainDocument_RejectsMissingWordprocessingRoot()
    {
        global::docx.DocxPackage package = CreatePackageWithMainDocumentXml("<root />");

        Assert.Throws<InvalidDataException>(() => new global::docx.DocxDocument(package));
    }

    [Test]
    public void ParseMainDocument_RejectsMissingBodyElement()
    {
        global::docx.DocxPackage package = CreatePackageWithMainDocumentXml("<w:document xmlns:w=\"http://schemas.openxmlformats.org/wordprocessingml/2006/main\" />");

        Assert.Throws<InvalidDataException>(() => new global::docx.DocxDocument(package));
    }

    [Test]
    public void ParseMainDocument_RejectsMalformedMainDocumentXml()
    {
        global::docx.DocxPackage package = CreatePackageWithMainDocumentXml("<w:document xmlns:w=\"http://schemas.openxmlformats.org/wordprocessingml/2006/main\"><w:body>");

        Assert.Throws<System.Xml.XmlException>(() => new global::docx.DocxDocument(package));
    }

    [TestCaseSource(nameof(InvalidRelatedPartCases))]
    public void OpenPackage_RejectsInvalidRelatedXmlPartRoots(string partName, string relationshipType, string contentType, string xml, string expectedMessage)
    {
        global::docx.DocxPackage package = CreatePackageWithInvalidRelatedXmlPart(partName, relationshipType, contentType, xml);

        InvalidDataException exception = Assert.Throws<InvalidDataException>(() => new global::docx.DocxDocument(package))!;

        Assert.That(exception.Message, Is.EqualTo(expectedMessage));
    }

    [Test]
    public void Body_ParseCapturesBodySubtree()
    {
        global::docx.DocxXmlReader reader = global::docx.DocxXmlReader.Create(CreateDocumentXml("<w:p />"));

        Assert.That(reader.ReadToElement("body", global::docx.DocxConstants.NamespaceWordprocessingMl), Is.True);

        global::docx.DocxBody body = global::docx.DocxBody.Parse(reader);

        Assert.That(body.HasXml(), Is.True);
        Assert.That(body.GetXml(), Does.Contain("w:body"));
        Assert.That(body.GetXml(), Does.Contain("w:p"));
    }

    [Test]
    public void DocumentAndBody_RejectInvalidArguments()
    {
        global::docx.DocxXmlReader reader = global::docx.DocxXmlReader.Create("<root />");
        global::docx.DocxDocument document = global::docx.DocxDocument.Create();
        global::docx.DocxDocument missingMainDocument = new global::docx.DocxDocument(new global::docx.DocxPackage());
        global::docx.DocxSection section = new global::docx.DocxSection("");

        Assert.Throws<ArgumentNullException>(() => new global::docx.DocxDocument(null!));
        Assert.Throws<ArgumentNullException>(() => global::docx.DocxDocument.OpenPackage(null!));
        Assert.Throws<ArgumentNullException>(() => new global::docx.DocxBody(null!));
        Assert.Throws<ArgumentNullException>(() => global::docx.DocxBody.Parse(null!));
        Assert.Throws<ArgumentNullException>(() => global::docx.DocxDocument.WriteMinimalMainDocument(null!));
        Assert.Throws<ArgumentNullException>(() => document.CreateHeaderForSection(null!, "default"));
        Assert.Throws<ArgumentNullException>(() => document.CreateHeaderForSection(section, null!));
        Assert.Throws<ArgumentNullException>(() => document.CreateFooterForSection(null!, "default"));
        Assert.Throws<ArgumentNullException>(() => document.CreateFooterForSection(section, null!));
        Assert.Throws<ArgumentNullException>(() => document.FindHeaderByRelationshipId(null!));
        Assert.Throws<ArgumentNullException>(() => document.FindFooterByRelationshipId(null!));
        Assert.Throws<ArgumentNullException>(() => document.FindDefaultHeader(null!));
        Assert.Throws<ArgumentNullException>(() => document.FindEvenHeader(null!));
        Assert.Throws<ArgumentNullException>(() => document.FindFirstHeader(null!));
        Assert.Throws<ArgumentNullException>(() => document.FindEffectiveHeader(null!, "default"));
        Assert.Throws<ArgumentNullException>(() => document.FindEffectiveHeader(section, null!));
        Assert.Throws<ArgumentNullException>(() => document.IsHeaderLinkedToPrevious(null!, "default"));
        Assert.Throws<ArgumentNullException>(() => document.IsHeaderLinkedToPrevious(section, null!));
        Assert.Throws<ArgumentNullException>(() => document.LinkHeaderToPrevious(null!, "default"));
        Assert.Throws<ArgumentNullException>(() => document.LinkHeaderToPrevious(section, null!));
        Assert.Throws<ArgumentNullException>(() => document.FindDefaultFooter(null!));
        Assert.Throws<ArgumentNullException>(() => document.FindEvenFooter(null!));
        Assert.Throws<ArgumentNullException>(() => document.FindFirstFooter(null!));
        Assert.Throws<ArgumentNullException>(() => document.FindEffectiveFooter(null!, "default"));
        Assert.Throws<ArgumentNullException>(() => document.FindEffectiveFooter(section, null!));
        Assert.Throws<ArgumentNullException>(() => document.IsFooterLinkedToPrevious(null!, "default"));
        Assert.Throws<ArgumentNullException>(() => document.IsFooterLinkedToPrevious(section, null!));
        Assert.Throws<ArgumentNullException>(() => document.LinkFooterToPrevious(null!, "default"));
        Assert.Throws<ArgumentNullException>(() => document.LinkFooterToPrevious(section, null!));
        Assert.Throws<ArgumentNullException>(() => document.FindPreviousSection(null!));
        Assert.Throws<ArgumentNullException>(() => document.AllocateStoryPartName(null!));
        Assert.Throws<ArgumentException>(() => document.AllocateStoryPartName(""));
        Assert.Throws<ArgumentNullException>(() => document.GetMainDocumentRelativeTarget(null!));
        Assert.Throws<ArgumentNullException>(() => document.WriteRelationshipSetPart(null!));
        Assert.Throws<InvalidOperationException>(() => missingMainDocument.CreateHeader());
        Assert.Throws<InvalidOperationException>(() => missingMainDocument.CreateFooter());
        Assert.Throws<InvalidOperationException>(() => missingMainDocument.GetOrCreateMainDocumentRelationshipSet());
        Assert.Throws<InvalidOperationException>(() => missingMainDocument.AllocateHeaderPartName());
        Assert.Throws<InvalidOperationException>(() => missingMainDocument.AllocateFooterPartName());
        Assert.Throws<InvalidOperationException>(() => missingMainDocument.GetMainDocumentRelativeTarget("/word/header1.xml"));

        Assert.That(reader.ReadToElement("root", ""), Is.True);
        Assert.Throws<InvalidOperationException>(() => global::docx.DocxBody.Parse(reader));
    }

    static global::docx.DocxPackage CreatePackageWithMainDocumentXml(string xml)
    {
        global::docx.DocxPackage package = new global::docx.DocxPackage();
        global::docx.DocxRelationshipSet rootRelationships = global::docx.DocxPackage.CreateRootRelationshipSet("/word/document.xml");

        package.ContentTypes.SetDefault("rels", global::docx.DocxConstants.ContentTypeRelationships);
        package.ContentTypes.SetDefault("xml", global::docx.DocxConstants.ContentTypeXml);
        package.AddXmlPartWithContent("/word/document.xml", global::docx.DocxConstants.ContentTypeMainDocument, xml);
        package.AddXmlPartWithContent("_rels/.rels", global::docx.DocxConstants.ContentTypeRelationships, rootRelationships.WriteXml());

        return package;
    }

    static global::docx.DocxPackage CreatePackageWithInvalidRelatedXmlPart(string partName, string relationshipType, string contentType, string xml)
    {
        global::docx.DocxPackage package = CreatePackageWithMainDocumentXml(CreateDocumentXml("<w:p />"));
        global::docx.DocxRelationshipSet documentRelationships = new global::docx.DocxRelationshipSet("/word/document.xml");
        string target = partName.Substring("/word/".Length);

        documentRelationships.CreateRelationshipWithId("rInvalid", relationshipType, target, false);
        package.RelationshipSets.Add(documentRelationships);
        package.ContentTypes.SetOverride(partName, contentType);
        package.AddXmlPartWithContent("/word/_rels/document.xml.rels", global::docx.DocxConstants.ContentTypeRelationships, documentRelationships.WriteXml());
        package.AddXmlPartWithContent(partName, contentType, xml);

        return package;
    }

    static IEnumerable<object[]> InvalidRelatedPartCases()
    {
        yield return new object[]
        {
            "/word/settings.xml",
            global::docx.DocxConstants.RelationshipTypeSettings,
            global::docx.DocxConstants.ContentTypeSettings,
            "<w:notSettings xmlns:w=\"http://schemas.openxmlformats.org/wordprocessingml/2006/main\" />",
            "Settings root element is missing."
        };

        yield return new object[]
        {
            "/word/styles.xml",
            global::docx.DocxConstants.RelationshipTypeStyles,
            global::docx.DocxConstants.ContentTypeStyles,
            "<w:notStyles xmlns:w=\"http://schemas.openxmlformats.org/wordprocessingml/2006/main\" />",
            "Styles root element is missing."
        };

        yield return new object[]
        {
            "/word/numbering.xml",
            global::docx.DocxConstants.RelationshipTypeNumbering,
            global::docx.DocxConstants.ContentTypeNumbering,
            "<w:notNumbering xmlns:w=\"http://schemas.openxmlformats.org/wordprocessingml/2006/main\" />",
            "Numbering root element is missing."
        };

        yield return new object[]
        {
            "/word/footnotes.xml",
            global::docx.DocxConstants.RelationshipTypeFootnotes,
            global::docx.DocxConstants.ContentTypeFootnotes,
            "<w:notFootnotes xmlns:w=\"http://schemas.openxmlformats.org/wordprocessingml/2006/main\" />",
            "Footnotes root element is missing."
        };

        yield return new object[]
        {
            "/word/endnotes.xml",
            global::docx.DocxConstants.RelationshipTypeEndnotes,
            global::docx.DocxConstants.ContentTypeEndnotes,
            "<w:notEndnotes xmlns:w=\"http://schemas.openxmlformats.org/wordprocessingml/2006/main\" />",
            "Endnotes root element is missing."
        };

        yield return new object[]
        {
            "/word/comments.xml",
            global::docx.DocxConstants.RelationshipTypeComments,
            global::docx.DocxConstants.ContentTypeComments,
            "<w:notComments xmlns:w=\"http://schemas.openxmlformats.org/wordprocessingml/2006/main\" />",
            "Comments root element is missing."
        };
    }

    static global::docx.DocxParagraph CreateParagraph(string text)
    {
        global::docx.DocxRun run = new global::docx.DocxRun("");
        global::docx.DocxParagraph paragraph = new global::docx.DocxParagraph("");

        run.AddText(global::docx.DocxText.Create(text));
        paragraph.AddRun(run);
        paragraph.AddChild(new global::docx.DocxXmlBuffer(run.WriteXml()));

        return paragraph;
    }

    static global::docx.DocxDocument RoundTripDocument(global::docx.IDocxDocumentPersistence persistence, global::docx.DocxDocument document)
    {
        MemoryStream stream = new MemoryStream();
        persistence.SaveDocument(document, stream, new global::docx.DocxSaveOptions());
        stream.Position = 0;
        return persistence.OpenDocument(stream, new global::docx.DocxLoadOptions());
    }

    static global::docx.DocxPackage CreatePackageWithHeaderFooterReferences()
    {
        global::docx.DocxPackage package = new global::docx.DocxPackage();
        global::docx.DocxRelationshipSet rootRelationships = global::docx.DocxPackage.CreateRootRelationshipSet("/word/document.xml");
        global::docx.DocxRelationshipSet documentRelationships = new global::docx.DocxRelationshipSet("/word/document.xml");

        documentRelationships.CreateRelationshipWithId("rHeaderDefault", global::docx.DocxConstants.RelationshipTypeHeader, "header-default.xml", false);
        documentRelationships.CreateRelationshipWithId("rHeaderEven", global::docx.DocxConstants.RelationshipTypeHeader, "header-even.xml", false);
        documentRelationships.CreateRelationshipWithId("rHeaderFirst", global::docx.DocxConstants.RelationshipTypeHeader, "header-first.xml", false);
        documentRelationships.CreateRelationshipWithId("rFooterDefault", global::docx.DocxConstants.RelationshipTypeFooter, "footer-default.xml", false);
        documentRelationships.CreateRelationshipWithId("rFooterEven", global::docx.DocxConstants.RelationshipTypeFooter, "footer-even.xml", false);
        documentRelationships.CreateRelationshipWithId("rFooterFirst", global::docx.DocxConstants.RelationshipTypeFooter, "footer-first.xml", false);

        package.ContentTypes.SetDefault("rels", global::docx.DocxConstants.ContentTypeRelationships);
        package.ContentTypes.SetDefault("xml", global::docx.DocxConstants.ContentTypeXml);
        package.AddXmlPartWithContent("/word/document.xml", global::docx.DocxConstants.ContentTypeMainDocument, CreateDocumentWithHeaderFooterReferencesXml());
        package.AddXmlPartWithContent("/word/header-default.xml", global::docx.DocxConstants.ContentTypeHeader, CreateHeaderXml("Default Header"));
        package.AddXmlPartWithContent("/word/header-even.xml", global::docx.DocxConstants.ContentTypeHeader, CreateHeaderXml("Even Header"));
        package.AddXmlPartWithContent("/word/header-first.xml", global::docx.DocxConstants.ContentTypeHeader, CreateHeaderXml("First Header"));
        package.AddXmlPartWithContent("/word/footer-default.xml", global::docx.DocxConstants.ContentTypeFooter, CreateFooterXml("Default Footer"));
        package.AddXmlPartWithContent("/word/footer-even.xml", global::docx.DocxConstants.ContentTypeFooter, CreateFooterXml("Even Footer"));
        package.AddXmlPartWithContent("/word/footer-first.xml", global::docx.DocxConstants.ContentTypeFooter, CreateFooterXml("First Footer"));
        package.AddXmlPartWithContent("_rels/.rels", global::docx.DocxConstants.ContentTypeRelationships, rootRelationships.WriteXml());
        package.AddXmlPartWithContent("/word/_rels/document.xml.rels", global::docx.DocxConstants.ContentTypeRelationships, documentRelationships.WriteXml());

        return package;
    }

    static global::docx.DocxPackage CreatePackageWithLinkedHeaderFooterSections()
    {
        global::docx.DocxPackage package = new global::docx.DocxPackage();
        global::docx.DocxRelationshipSet rootRelationships = global::docx.DocxPackage.CreateRootRelationshipSet("/word/document.xml");
        global::docx.DocxRelationshipSet documentRelationships = new global::docx.DocxRelationshipSet("/word/document.xml");

        documentRelationships.CreateRelationshipWithId("rHeaderFirstDefault", global::docx.DocxConstants.RelationshipTypeHeader, "header-first-default.xml", false);
        documentRelationships.CreateRelationshipWithId("rFooterFirstDefault", global::docx.DocxConstants.RelationshipTypeFooter, "footer-first-default.xml", false);
        documentRelationships.CreateRelationshipWithId("rHeaderSecondFirst", global::docx.DocxConstants.RelationshipTypeHeader, "header-second-first.xml", false);
        documentRelationships.CreateRelationshipWithId("rFooterSecondEven", global::docx.DocxConstants.RelationshipTypeFooter, "footer-second-even.xml", false);

        package.ContentTypes.SetDefault("rels", global::docx.DocxConstants.ContentTypeRelationships);
        package.ContentTypes.SetDefault("xml", global::docx.DocxConstants.ContentTypeXml);
        package.AddXmlPartWithContent("/word/document.xml", global::docx.DocxConstants.ContentTypeMainDocument, CreateDocumentWithLinkedHeaderFooterSectionsXml());
        package.AddXmlPartWithContent("/word/header-first-default.xml", global::docx.DocxConstants.ContentTypeHeader, CreateHeaderXml("First Default Header"));
        package.AddXmlPartWithContent("/word/footer-first-default.xml", global::docx.DocxConstants.ContentTypeFooter, CreateFooterXml("First Default Footer"));
        package.AddXmlPartWithContent("/word/header-second-first.xml", global::docx.DocxConstants.ContentTypeHeader, CreateHeaderXml("Second First Header"));
        package.AddXmlPartWithContent("/word/footer-second-even.xml", global::docx.DocxConstants.ContentTypeFooter, CreateFooterXml("Second Even Footer"));
        package.AddXmlPartWithContent("_rels/.rels", global::docx.DocxConstants.ContentTypeRelationships, rootRelationships.WriteXml());
        package.AddXmlPartWithContent("/word/_rels/document.xml.rels", global::docx.DocxConstants.ContentTypeRelationships, documentRelationships.WriteXml());

        return package;
    }

    static string CreateDocumentWithHeaderFooterReferencesXml()
    {
        return "<w:document xmlns:w=\"http://schemas.openxmlformats.org/wordprocessingml/2006/main\" xmlns:r=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships\"><w:body><w:sectPr><w:headerReference w:type=\"default\" r:id=\"rHeaderDefault\" /><w:headerReference w:type=\"even\" r:id=\"rHeaderEven\" /><w:headerReference w:type=\"first\" r:id=\"rHeaderFirst\" /><w:footerReference w:type=\"default\" r:id=\"rFooterDefault\" /><w:footerReference w:type=\"even\" r:id=\"rFooterEven\" /><w:footerReference w:type=\"first\" r:id=\"rFooterFirst\" /></w:sectPr></w:body></w:document>";
    }

    static string CreateDocumentWithLinkedHeaderFooterSectionsXml()
    {
        return "<w:document xmlns:w=\"http://schemas.openxmlformats.org/wordprocessingml/2006/main\" xmlns:r=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships\"><w:body><w:p><w:pPr><w:sectPr><w:headerReference w:type=\"default\" r:id=\"rHeaderFirstDefault\" /><w:footerReference w:type=\"default\" r:id=\"rFooterFirstDefault\" /></w:sectPr></w:pPr></w:p><w:sectPr><w:headerReference w:type=\"first\" r:id=\"rHeaderSecondFirst\" /><w:footerReference w:type=\"even\" r:id=\"rFooterSecondEven\" /></w:sectPr></w:body></w:document>";
    }

    static string CreateHeaderXml(string text)
    {
        return "<w:hdr xmlns:w=\"http://schemas.openxmlformats.org/wordprocessingml/2006/main\"><w:p><w:r><w:t>" + text + "</w:t></w:r></w:p></w:hdr>";
    }

    static string CreateFooterXml(string text)
    {
        return "<w:ftr xmlns:w=\"http://schemas.openxmlformats.org/wordprocessingml/2006/main\"><w:p><w:r><w:t>" + text + "</w:t></w:r></w:p></w:ftr>";
    }

    static string CreateDocumentXml(string bodyContent)
    {
        return "<w:document xmlns:w=\"http://schemas.openxmlformats.org/wordprocessingml/2006/main\"><w:body>" + bodyContent + "</w:body></w:document>";
    }
}
