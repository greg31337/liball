using NUnit.Framework;

namespace docx.Tests;

[TestFixture]
public class DocxSectionFormatTests
{
    [Test]
    public void Parse_ReadsCommonSectionProperties()
    {
        global::docx.DocxSectionFormat format = ParseFormat("<w:sectPr xmlns:w=\"http://schemas.openxmlformats.org/wordprocessingml/2006/main\"><w:type w:val=\"nextPage\" /><w:pgSz w:w=\"12240\" w:h=\"15840\" w:orient=\"portrait\" w:code=\"1\" /><w:pgMar w:top=\"1440\" w:right=\"1800\" w:bottom=\"1440\" w:left=\"1800\" w:header=\"720\" w:footer=\"720\" w:gutter=\"0\" /><w:cols w:num=\"2\" w:space=\"720\" w:equalWidth=\"1\" w:sep=\"true\" /><w:docGrid w:type=\"lines\" w:linePitch=\"360\" w:charSpace=\"0\" /><w:titlePg /><w:pgNumType w:start=\"5\" w:fmt=\"decimal\" w:chapStyle=\"1\" w:chapSep=\"hyphen\" /></w:sectPr>");

        Assert.That(format.HasAnyFormatting(), Is.True);
        Assert.That(format.SectionType, Is.EqualTo("nextPage"));
        Assert.That(format.PageWidth, Is.EqualTo("12240"));
        Assert.That(format.PageHeight, Is.EqualTo("15840"));
        Assert.That(format.PageOrientation, Is.EqualTo("portrait"));
        Assert.That(format.PageCode, Is.EqualTo("1"));
        Assert.That(format.MarginTop, Is.EqualTo("1440"));
        Assert.That(format.MarginRight, Is.EqualTo("1800"));
        Assert.That(format.MarginBottom, Is.EqualTo("1440"));
        Assert.That(format.MarginLeft, Is.EqualTo("1800"));
        Assert.That(format.MarginHeader, Is.EqualTo("720"));
        Assert.That(format.MarginFooter, Is.EqualTo("720"));
        Assert.That(format.MarginGutter, Is.EqualTo("0"));
        Assert.That(format.ColumnCount, Is.EqualTo("2"));
        Assert.That(format.ColumnSpace, Is.EqualTo("720"));
        Assert.That(format.HasEqualWidthColumns, Is.True);
        Assert.That(format.EqualWidthColumns, Is.True);
        Assert.That(format.HasColumnSeparator, Is.True);
        Assert.That(format.ColumnSeparator, Is.True);
        Assert.That(format.DocGridType, Is.EqualTo("lines"));
        Assert.That(format.DocGridLinePitch, Is.EqualTo("360"));
        Assert.That(format.DocGridCharSpace, Is.EqualTo("0"));
        Assert.That(format.HasTitlePage, Is.True);
        Assert.That(format.TitlePage, Is.True);
        Assert.That(format.PageNumberStart, Is.EqualTo("5"));
        Assert.That(format.PageNumberFormat, Is.EqualTo("decimal"));
        Assert.That(format.PageNumberChapterStyle, Is.EqualTo("1"));
        Assert.That(format.PageNumberChapterSeparator, Is.EqualTo("hyphen"));
    }

    [Test]
    public void Parse_ReadsHeaderAndFooterReferences()
    {
        global::docx.DocxSectionFormat format = ParseFormat("<w:sectPr xmlns:w=\"http://schemas.openxmlformats.org/wordprocessingml/2006/main\" xmlns:r=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships\"><w:headerReference w:type=\"default\" r:id=\"rHeaderDefault\" /><w:headerReference w:type=\"even\" r:id=\"rHeaderEven\" /><w:headerReference w:type=\"first\" r:id=\"rHeaderFirst\" /><w:footerReference w:type=\"default\" r:id=\"rFooterDefault\" /><w:footerReference w:type=\"even\" r:id=\"rFooterEven\" /><w:footerReference w:type=\"first\" r:id=\"rFooterFirst\" /></w:sectPr>");

        Assert.That(format.HasAnyFormatting(), Is.True);
        Assert.That(format.HasHeaderReferences(), Is.True);
        Assert.That(format.HasFooterReferences(), Is.True);
        Assert.That(format.DefaultHeaderRelationshipId, Is.EqualTo("rHeaderDefault"));
        Assert.That(format.EvenHeaderRelationshipId, Is.EqualTo("rHeaderEven"));
        Assert.That(format.FirstHeaderRelationshipId, Is.EqualTo("rHeaderFirst"));
        Assert.That(format.DefaultFooterRelationshipId, Is.EqualTo("rFooterDefault"));
        Assert.That(format.EvenFooterRelationshipId, Is.EqualTo("rFooterEven"));
        Assert.That(format.FirstFooterRelationshipId, Is.EqualTo("rFooterFirst"));
        Assert.That(format.GetHeaderReference(global::docx.DocxSectionFormat.HeaderFooterReferenceDefault), Is.EqualTo("rHeaderDefault"));
        Assert.That(format.GetHeaderReference(global::docx.DocxSectionFormat.HeaderFooterReferenceEven), Is.EqualTo("rHeaderEven"));
        Assert.That(format.GetHeaderReference(global::docx.DocxSectionFormat.HeaderFooterReferenceFirst), Is.EqualTo("rHeaderFirst"));
        Assert.That(format.GetFooterReference(global::docx.DocxSectionFormat.HeaderFooterReferenceDefault), Is.EqualTo("rFooterDefault"));
        Assert.That(format.GetFooterReference(global::docx.DocxSectionFormat.HeaderFooterReferenceEven), Is.EqualTo("rFooterEven"));
        Assert.That(format.GetFooterReference(global::docx.DocxSectionFormat.HeaderFooterReferenceFirst), Is.EqualTo("rFooterFirst"));
    }

    [Test]
    public void Parse_TreatsMissingOrUnknownHeaderFooterReferenceTypeAsDefault()
    {
        global::docx.DocxSectionFormat format = ParseFormat("<w:sectPr xmlns:w=\"http://schemas.openxmlformats.org/wordprocessingml/2006/main\" xmlns:r=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships\"><w:headerReference r:id=\"rHeaderDefault\" /><w:footerReference w:type=\"custom\" r:id=\"rFooterDefault\" /></w:sectPr>");

        Assert.That(format.DefaultHeaderRelationshipId, Is.EqualTo("rHeaderDefault"));
        Assert.That(format.DefaultFooterRelationshipId, Is.EqualTo("rFooterDefault"));
    }

    [Test]
    public void SectionParsing_ExposesFormat()
    {
        global::docx.DocxSection section = ParseSection("<w:sectPr xmlns:w=\"http://schemas.openxmlformats.org/wordprocessingml/2006/main\"><w:type w:val=\"continuous\" /><w:pgSz w:w=\"15840\" w:h=\"12240\" w:orient=\"landscape\" /></w:sectPr>");

        Assert.That(section.HasXml(), Is.True);
        Assert.That(section.HasFormat(), Is.True);
        Assert.That(section.Format.SectionType, Is.EqualTo("continuous"));
        Assert.That(section.Format.PageWidth, Is.EqualTo("15840"));
        Assert.That(section.Format.PageHeight, Is.EqualTo("12240"));
        Assert.That(section.Format.PageOrientation, Is.EqualTo("landscape"));
    }

    [Test]
    public void Parse_HandlesEmptyAndMissingSectionProperties()
    {
        global::docx.DocxSection emptySection = ParseSection("<w:sectPr xmlns:w=\"http://schemas.openxmlformats.org/wordprocessingml/2006/main\" />");
        global::docx.DocxSectionFormat emptyFormat = global::docx.DocxSectionFormat.ParseXml("");
        global::docx.DocxSectionFormat missingFormat = global::docx.DocxSectionFormat.ParseXml("<root />");

        Assert.That(emptySection.HasFormat(), Is.False);
        Assert.That(emptyFormat.HasAnyFormatting(), Is.False);
        Assert.That(missingFormat.HasAnyFormatting(), Is.False);
    }

    [Test]
    public void SetXml_UpdatesParsedSectionFormat()
    {
        global::docx.DocxSection section = new global::docx.DocxSection("");

        section.SetXml("<w:sectPr xmlns:w=\"http://schemas.openxmlformats.org/wordprocessingml/2006/main\"><w:pgMar w:top=\"720\" w:left=\"1440\" /><w:titlePg w:val=\"0\" /></w:sectPr>");

        Assert.That(section.HasFormat(), Is.True);
        Assert.That(section.Format.MarginTop, Is.EqualTo("720"));
        Assert.That(section.Format.MarginLeft, Is.EqualTo("1440"));
        Assert.That(section.Format.HasTitlePage, Is.True);
        Assert.That(section.Format.TitlePage, Is.False);
    }

    [Test]
    public void SetFormat_ReplacesParsedSectionFormat()
    {
        global::docx.DocxSection section = ParseSection("<w:sectPr xmlns:w=\"http://schemas.openxmlformats.org/wordprocessingml/2006/main\"><w:type w:val=\"nextPage\" /></w:sectPr>");
        global::docx.DocxSectionFormat format = new global::docx.DocxSectionFormat();

        format.SetSectionType("continuous");
        section.SetFormat(format);

        Assert.That(section.HasFormat(), Is.True);
        Assert.That(section.Format.SectionType, Is.EqualTo("continuous"));
    }

    [Test]
    public void WriteXml_WritesCommonSectionProperties()
    {
        global::docx.DocxSectionFormat format = new global::docx.DocxSectionFormat();

        format.SetSectionType("nextPage");
        format.SetPageSize("12240", "15840", "portrait", "1");
        format.SetPageMargins("1440", "1800", "1440", "1800", "720", "720", "0");
        format.SetColumns("2", "720");
        format.SetEqualWidthColumns(true);
        format.SetColumnSeparator(false);
        format.SetDocGrid("lines", "360", "0");
        format.SetTitlePage(false);
        format.SetPageNumbering("5", "decimal", "1", "hyphen");

        global::docx.DocxSectionFormat parsed = ParseFormat(format.WriteXml());

        Assert.That(parsed.SectionType, Is.EqualTo("nextPage"));
        Assert.That(parsed.PageWidth, Is.EqualTo("12240"));
        Assert.That(parsed.PageHeight, Is.EqualTo("15840"));
        Assert.That(parsed.PageOrientation, Is.EqualTo("portrait"));
        Assert.That(parsed.PageCode, Is.EqualTo("1"));
        Assert.That(parsed.MarginTop, Is.EqualTo("1440"));
        Assert.That(parsed.MarginRight, Is.EqualTo("1800"));
        Assert.That(parsed.MarginBottom, Is.EqualTo("1440"));
        Assert.That(parsed.MarginLeft, Is.EqualTo("1800"));
        Assert.That(parsed.MarginHeader, Is.EqualTo("720"));
        Assert.That(parsed.MarginFooter, Is.EqualTo("720"));
        Assert.That(parsed.MarginGutter, Is.EqualTo("0"));
        Assert.That(parsed.ColumnCount, Is.EqualTo("2"));
        Assert.That(parsed.ColumnSpace, Is.EqualTo("720"));
        Assert.That(parsed.EqualWidthColumns, Is.True);
        Assert.That(parsed.ColumnSeparator, Is.False);
        Assert.That(parsed.DocGridType, Is.EqualTo("lines"));
        Assert.That(parsed.DocGridLinePitch, Is.EqualTo("360"));
        Assert.That(parsed.DocGridCharSpace, Is.EqualTo("0"));
        Assert.That(parsed.HasTitlePage, Is.True);
        Assert.That(parsed.TitlePage, Is.False);
        Assert.That(parsed.PageNumberStart, Is.EqualTo("5"));
        Assert.That(parsed.PageNumberFormat, Is.EqualTo("decimal"));
        Assert.That(parsed.PageNumberChapterStyle, Is.EqualTo("1"));
        Assert.That(parsed.PageNumberChapterSeparator, Is.EqualTo("hyphen"));
    }

    [Test]
    public void PageSetupHelpers_SetSizeOrientationMarginsColumnsAndPageNumbering()
    {
        global::docx.DocxSectionFormat format = new global::docx.DocxSectionFormat();

        format.SetLetterPageLandscape();
        format.SetPageSideMargins("720", "1080", "720", "1080");
        format.SetHeaderFooterMargins("360", "420");
        format.SetGutter("180");
        format.SetColumnLayout("3", "240", true, true);
        format.RestartPageNumbering("7", "lowerRoman");

        global::docx.DocxSectionFormat parsed = ParseFormat(format.WriteXml());

        Assert.That(parsed.PageWidth, Is.EqualTo(global::docx.DocxSectionFormat.PageSizeLetterHeight));
        Assert.That(parsed.PageHeight, Is.EqualTo(global::docx.DocxSectionFormat.PageSizeLetterWidth));
        Assert.That(parsed.PageOrientation, Is.EqualTo(global::docx.DocxSectionFormat.PageOrientationLandscape));
        Assert.That(parsed.MarginTop, Is.EqualTo("720"));
        Assert.That(parsed.MarginRight, Is.EqualTo("1080"));
        Assert.That(parsed.MarginBottom, Is.EqualTo("720"));
        Assert.That(parsed.MarginLeft, Is.EqualTo("1080"));
        Assert.That(parsed.MarginHeader, Is.EqualTo("360"));
        Assert.That(parsed.MarginFooter, Is.EqualTo("420"));
        Assert.That(parsed.MarginGutter, Is.EqualTo("180"));
        Assert.That(parsed.ColumnCount, Is.EqualTo("3"));
        Assert.That(parsed.ColumnSpace, Is.EqualTo("240"));
        Assert.That(parsed.HasEqualWidthColumns, Is.True);
        Assert.That(parsed.EqualWidthColumns, Is.True);
        Assert.That(parsed.HasColumnSeparator, Is.True);
        Assert.That(parsed.ColumnSeparator, Is.True);
        Assert.That(parsed.PageNumberStart, Is.EqualTo("7"));
        Assert.That(parsed.PageNumberFormat, Is.EqualTo("lowerRoman"));
        Assert.That(parsed.PageNumberChapterStyle, Is.EqualTo(""));
        Assert.That(parsed.PageNumberChapterSeparator, Is.EqualTo(""));
    }

    [Test]
    public void PageSetupHelpers_SetCommonPaperSizesAndSingleColumn()
    {
        global::docx.DocxSectionFormat format = new global::docx.DocxSectionFormat();

        format.SetA4PagePortrait();
        format.SetPageOrientation(global::docx.DocxSectionFormat.PageOrientationPortrait);
        format.SetPageDimensions("1000", "2000");
        format.SetLandscapeOrientation();
        format.SetSingleColumn();
        format.SetPageNumberStart("3");
        format.SetPageNumberFormat(global::docx.DocxSectionFormat.PageNumberFormatDecimal);

        global::docx.DocxSectionFormat parsed = ParseFormat(format.WriteXml());

        Assert.That(parsed.PageWidth, Is.EqualTo("1000"));
        Assert.That(parsed.PageHeight, Is.EqualTo("2000"));
        Assert.That(parsed.PageOrientation, Is.EqualTo(global::docx.DocxSectionFormat.PageOrientationLandscape));
        Assert.That(parsed.ColumnCount, Is.EqualTo("1"));
        Assert.That(parsed.ColumnSpace, Is.EqualTo(""));
        Assert.That(parsed.HasEqualWidthColumns, Is.False);
        Assert.That(parsed.HasColumnSeparator, Is.False);
        Assert.That(parsed.PageNumberStart, Is.EqualTo("3"));
        Assert.That(parsed.PageNumberFormat, Is.EqualTo(global::docx.DocxSectionFormat.PageNumberFormatDecimal));
    }

    [Test]
    public void ClearPageNumbering_RemovesPageNumberingFields()
    {
        global::docx.DocxSectionFormat format = new global::docx.DocxSectionFormat();

        format.SetPageNumbering("5", "decimal", "1", "hyphen");
        format.ClearPageNumbering();

        global::docx.DocxSectionFormat parsed = ParseFormat(format.WriteXml());

        Assert.That(parsed.PageNumberStart, Is.EqualTo(""));
        Assert.That(parsed.PageNumberFormat, Is.EqualTo(""));
        Assert.That(parsed.PageNumberChapterStyle, Is.EqualTo(""));
        Assert.That(parsed.PageNumberChapterSeparator, Is.EqualTo(""));
    }

    [Test]
    public void WriteXml_WritesHeaderAndFooterReferences()
    {
        global::docx.DocxSectionFormat format = new global::docx.DocxSectionFormat();

        format.SetHeaderReference(global::docx.DocxSectionFormat.HeaderFooterReferenceDefault, "rHeaderDefault");
        format.SetHeaderReference(global::docx.DocxSectionFormat.HeaderFooterReferenceEven, "rHeaderEven");
        format.SetHeaderReference(global::docx.DocxSectionFormat.HeaderFooterReferenceFirst, "rHeaderFirst");
        format.SetFooterReference(global::docx.DocxSectionFormat.HeaderFooterReferenceDefault, "rFooterDefault");
        format.SetFooterReference(global::docx.DocxSectionFormat.HeaderFooterReferenceEven, "rFooterEven");
        format.SetFooterReference(global::docx.DocxSectionFormat.HeaderFooterReferenceFirst, "rFooterFirst");

        string xml = format.WriteXml();
        global::docx.DocxSectionFormat parsed = ParseFormat(xml);

        Assert.That(xml, Does.Contain("headerReference"));
        Assert.That(xml, Does.Contain("footerReference"));
        Assert.That(xml, Does.Contain("r:id=\"rHeaderDefault\""));
        Assert.That(xml, Does.Contain("r:id=\"rFooterFirst\""));
        Assert.That(parsed.DefaultHeaderRelationshipId, Is.EqualTo("rHeaderDefault"));
        Assert.That(parsed.EvenHeaderRelationshipId, Is.EqualTo("rHeaderEven"));
        Assert.That(parsed.FirstHeaderRelationshipId, Is.EqualTo("rHeaderFirst"));
        Assert.That(parsed.DefaultFooterRelationshipId, Is.EqualTo("rFooterDefault"));
        Assert.That(parsed.EvenFooterRelationshipId, Is.EqualTo("rFooterEven"));
        Assert.That(parsed.FirstFooterRelationshipId, Is.EqualTo("rFooterFirst"));
    }

    [Test]
    public void WriteXml_WritesOnlyAssignedHeaderFooterReferencesAndMargins()
    {
        global::docx.DocxSectionFormat format = new global::docx.DocxSectionFormat();

        format.SetHeaderReference(global::docx.DocxSectionFormat.HeaderFooterReferenceFirst, "rFirstHeader");
        format.SetFooterReference(global::docx.DocxSectionFormat.HeaderFooterReferenceEven, "rEvenFooter");
        format.SetHeaderFooterMargins("360", "420");

        string xml = format.WriteXml();
        global::docx.DocxSectionFormat parsed = ParseFormat(xml);

        Assert.That(parsed.DefaultHeaderRelationshipId, Is.EqualTo(""));
        Assert.That(parsed.EvenHeaderRelationshipId, Is.EqualTo(""));
        Assert.That(parsed.FirstHeaderRelationshipId, Is.EqualTo("rFirstHeader"));
        Assert.That(parsed.DefaultFooterRelationshipId, Is.EqualTo(""));
        Assert.That(parsed.EvenFooterRelationshipId, Is.EqualTo("rEvenFooter"));
        Assert.That(parsed.FirstFooterRelationshipId, Is.EqualTo(""));
        Assert.That(parsed.MarginHeader, Is.EqualTo("360"));
        Assert.That(parsed.MarginFooter, Is.EqualTo("420"));
        Assert.That(xml, Does.Not.Contain("headerReference w:type=\"default\""));
        Assert.That(xml, Does.Not.Contain("footerReference w:type=\"first\""));
    }

    [Test]
    public void ClearHeaderAndFooterReference_EnablesLinkToPrevious()
    {
        global::docx.DocxSectionFormat format = new global::docx.DocxSectionFormat();

        format.SetHeaderReference(global::docx.DocxSectionFormat.HeaderFooterReferenceDefault, "rHeaderDefault");
        format.SetFooterReference(global::docx.DocxSectionFormat.HeaderFooterReferenceEven, "rFooterEven");

        Assert.That(format.IsHeaderLinkedToPrevious(global::docx.DocxSectionFormat.HeaderFooterReferenceDefault), Is.False);
        Assert.That(format.IsFooterLinkedToPrevious(global::docx.DocxSectionFormat.HeaderFooterReferenceEven), Is.False);

        format.ClearHeaderReference(global::docx.DocxSectionFormat.HeaderFooterReferenceDefault);
        format.ClearFooterReference(global::docx.DocxSectionFormat.HeaderFooterReferenceEven);

        Assert.That(format.DefaultHeaderRelationshipId, Is.EqualTo(""));
        Assert.That(format.EvenFooterRelationshipId, Is.EqualTo(""));
        Assert.That(format.IsHeaderLinkedToPrevious(global::docx.DocxSectionFormat.HeaderFooterReferenceDefault), Is.True);
        Assert.That(format.IsFooterLinkedToPrevious(global::docx.DocxSectionFormat.HeaderFooterReferenceEven), Is.True);
        Assert.That(format.WriteXml(), Does.Not.Contain("headerReference"));
        Assert.That(format.WriteXml(), Does.Not.Contain("footerReference"));
    }

    [Test]
    public void SectionWriting_WritesStructuredFormat()
    {
        global::docx.DocxSection section = new global::docx.DocxSection("");

        section.Format.SetSectionType("continuous");
        section.Format.SetPageSize("15840", "12240", "landscape", "");
        section.Format.SetPageMargins("720", "", "", "720", "", "", "");

        string xml = section.WriteXml();
        global::docx.DocxSection parsed = ParseSection(xml);

        Assert.That(xml, Does.Contain("<w:sectPr"));
        Assert.That(parsed.HasFormat(), Is.True);
        Assert.That(parsed.Format.SectionType, Is.EqualTo("continuous"));
        Assert.That(parsed.Format.PageOrientation, Is.EqualTo("landscape"));
        Assert.That(parsed.Format.MarginLeft, Is.EqualTo("720"));
    }

    [Test]
    public void SectionWriting_PreservesUntouchedRawXml()
    {
        string rawXml = "<w:sectPr xmlns:w=\"http://schemas.openxmlformats.org/wordprocessingml/2006/main\"><w:type w:val=\"nextPage\" /><w:unknown marker=\"preserved\" /></w:sectPr>";
        global::docx.DocxSection section = ParseSection(rawXml);

        string xml = section.WriteXml();

        Assert.That(xml, Does.Contain("marker=\"preserved\""));
        Assert.That(xml, Does.Contain("<w:type"));
    }

    [Test]
    public void SetFormat_ReplacesRawSectionXmlForWriting()
    {
        global::docx.DocxSection section = ParseSection("<w:sectPr xmlns:w=\"http://schemas.openxmlformats.org/wordprocessingml/2006/main\"><w:type w:val=\"nextPage\" /><w:unknown marker=\"old\" /></w:sectPr>");
        global::docx.DocxSectionFormat format = new global::docx.DocxSectionFormat();

        format.SetSectionType("continuous");
        section.SetFormat(format);

        string xml = section.WriteXml();
        global::docx.DocxSection parsed = ParseSection(xml);

        Assert.That(xml, Does.Not.Contain("marker=\"old\""));
        Assert.That(parsed.Format.SectionType, Is.EqualTo("continuous"));
    }

    [Test]
    public void ClearBooleanFormatting_RemovesBooleanAttributesFromWrittenProperties()
    {
        global::docx.DocxSectionFormat format = new global::docx.DocxSectionFormat();

        format.SetEqualWidthColumns(true);
        format.ClearEqualWidthColumns();
        format.SetColumnSeparator(true);
        format.ClearColumnSeparator();
        format.SetTitlePage(true);
        format.ClearTitlePage();

        global::docx.DocxSectionFormat parsed = ParseFormat(format.WriteXml());

        Assert.That(parsed.HasAnyFormatting(), Is.False);
    }

    [Test]
    public void PublicFormattingService_RoundTripsCompleteSectionFormatAndExplicitColumns()
    {
        global::docx.DocxSection section = ParseSection("<w:sectPr xmlns:w=\"http://schemas.openxmlformats.org/wordprocessingml/2006/main\"><w:type w:val=\"nextPage\" /><w:unknown marker=\"replaced\" /></w:sectPr>");
        global::docx.DocxSectionColumn firstColumn = new global::docx.DocxSectionColumn();
        global::docx.DocxSectionColumn secondColumn = new global::docx.DocxSectionColumn();
        global::docx.IDocxSectionFormatting formatting = new global::docx.DocxSectionFormatting();
        global::docx.DocxSectionFormat format = formatting.GetFormat(section);

        format.SectionType = "continuous";
        format.PageWidth = "15840";
        format.PageHeight = "12240";
        format.PageOrientation = "landscape";
        format.PageCode = "9";
        format.MarginTop = "720";
        format.MarginRight = "900";
        format.MarginBottom = "1080";
        format.MarginLeft = "1260";
        format.MarginHeader = "360";
        format.MarginFooter = "420";
        format.MarginGutter = "180";
        format.ColumnCount = "2";
        format.ColumnSpace = "240";
        format.HasEqualWidthColumns = true;
        format.EqualWidthColumns = false;
        format.HasColumnSeparator = true;
        format.ColumnSeparator = true;
        firstColumn.Width = "4000";
        firstColumn.Space = "240";
        secondColumn.Width = "7000";
        secondColumn.Space = "0";
        format.Columns.Add(firstColumn);
        format.Columns.Add(secondColumn);
        format.HasTitlePage = true;
        format.TitlePage = true;
        format.PageNumberStart = "4";
        format.PageNumberFormat = "lowerRoman";
        format.PageNumberChapterStyle = "2";
        format.PageNumberChapterSeparator = "colon";
        format.DefaultHeaderRelationshipId = "rHeaderDefault";
        format.EvenHeaderRelationshipId = "rHeaderEven";
        format.FirstFooterRelationshipId = "rFooterFirst";

        formatting.SetFormat(section, format);

        string xml = section.WriteXml();
        global::docx.DocxSection parsed = ParseSection(xml);

        Assert.That(formatting.HasFormat(section), Is.True);
        Assert.That(formatting.GetFormat(section), Is.SameAs(format));
        Assert.That(xml, Does.Not.Contain("marker=\"replaced\""));
        Assert.That(parsed.Format.SectionType, Is.EqualTo("continuous"));
        Assert.That(parsed.Format.PageWidth, Is.EqualTo("15840"));
        Assert.That(parsed.Format.PageHeight, Is.EqualTo("12240"));
        Assert.That(parsed.Format.PageOrientation, Is.EqualTo("landscape"));
        Assert.That(parsed.Format.PageCode, Is.EqualTo("9"));
        Assert.That(parsed.Format.MarginTop, Is.EqualTo("720"));
        Assert.That(parsed.Format.MarginRight, Is.EqualTo("900"));
        Assert.That(parsed.Format.MarginBottom, Is.EqualTo("1080"));
        Assert.That(parsed.Format.MarginLeft, Is.EqualTo("1260"));
        Assert.That(parsed.Format.MarginHeader, Is.EqualTo("360"));
        Assert.That(parsed.Format.MarginFooter, Is.EqualTo("420"));
        Assert.That(parsed.Format.MarginGutter, Is.EqualTo("180"));
        Assert.That(parsed.Format.ColumnCount, Is.EqualTo("2"));
        Assert.That(parsed.Format.ColumnSpace, Is.EqualTo("240"));
        Assert.That(parsed.Format.EqualWidthColumns, Is.False);
        Assert.That(parsed.Format.ColumnSeparator, Is.True);
        Assert.That(parsed.Format.Columns, Has.Count.EqualTo(2));
        Assert.That(parsed.Format.Columns[0].Width, Is.EqualTo("4000"));
        Assert.That(parsed.Format.Columns[0].Space, Is.EqualTo("240"));
        Assert.That(parsed.Format.Columns[1].Width, Is.EqualTo("7000"));
        Assert.That(parsed.Format.Columns[1].Space, Is.EqualTo("0"));
        Assert.That(parsed.Format.TitlePage, Is.True);
        Assert.That(parsed.Format.PageNumberStart, Is.EqualTo("4"));
        Assert.That(parsed.Format.PageNumberFormat, Is.EqualTo("lowerRoman"));
        Assert.That(parsed.Format.PageNumberChapterStyle, Is.EqualTo("2"));
        Assert.That(parsed.Format.PageNumberChapterSeparator, Is.EqualTo("colon"));
        Assert.That(parsed.Format.DefaultHeaderRelationshipId, Is.EqualTo("rHeaderDefault"));
        Assert.That(parsed.Format.EvenHeaderRelationshipId, Is.EqualTo("rHeaderEven"));
        Assert.That(parsed.Format.FirstFooterRelationshipId, Is.EqualTo("rFooterFirst"));
    }

    [Test]
    public void PublicFormattingService_RejectsInvalidArguments()
    {
        global::docx.IDocxSectionFormatting formatting = new global::docx.DocxSectionFormatting();
        global::docx.DocxSection section = new global::docx.DocxSection("");

        Assert.Throws<ArgumentNullException>(() => formatting.HasFormat(null!));
        Assert.Throws<ArgumentNullException>(() => formatting.GetFormat(null!));
        Assert.Throws<ArgumentNullException>(() => formatting.SetFormat(null!, new global::docx.DocxSectionFormat()));
        Assert.Throws<ArgumentNullException>(() => formatting.SetFormat(section, null!));
        Assert.Throws<ArgumentNullException>(() => formatting.GetBodyFormat(null!, 0));
        Assert.Throws<ArgumentNullException>(() => formatting.SetBodyFormat(null!, 0, new global::docx.DocxSectionFormat()));
    }

    [Test]
    public void PublicFormattingService_PersistsSectionBreakChangesThroughContainingBody()
    {
        global::docx.DocxBody body = new global::docx.DocxBody("<w:body xmlns:w=\"http://schemas.openxmlformats.org/wordprocessingml/2006/main\"><w:p><w:pPr><w:keepNext /><w:sectPr><w:type w:val=\"nextPage\" /><w:pgSz w:w=\"12240\" w:h=\"15840\" /></w:sectPr></w:pPr><w:r><w:t>First</w:t></w:r></w:p><w:sectPr><w:type w:val=\"continuous\" /></w:sectPr></w:body>");
        global::docx.IDocxSectionFormatting formatting = new global::docx.DocxSectionFormatting();
        global::docx.DocxSectionFormat format = formatting.GetBodyFormat(body, 0);

        format.SectionType = "oddPage";
        format.PageWidth = "15840";
        format.PageHeight = "12240";
        format.PageOrientation = "landscape";
        formatting.SetBodyFormat(body, 0, format);

        string xml = body.WriteXml();
        global::docx.DocxBody parsed = new global::docx.DocxBody(xml);

        Assert.That(parsed.SectionCount(), Is.EqualTo(2));
        Assert.That(parsed.GetSection(0).Format.SectionType, Is.EqualTo("oddPage"));
        Assert.That(parsed.GetSection(0).Format.PageWidth, Is.EqualTo("15840"));
        Assert.That(parsed.GetSection(0).Format.PageHeight, Is.EqualTo("12240"));
        Assert.That(parsed.GetSection(0).Format.PageOrientation, Is.EqualTo("landscape"));
        Assert.That(parsed.GetSection(1).Format.SectionType, Is.EqualTo("continuous"));
        Assert.That(xml, Does.Contain("keepNext"));
    }

    [Test]
    public void SectionFormatParsing_RejectsInvalidArguments()
    {
        global::docx.DocxXmlReader reader = global::docx.DocxXmlReader.Create("<root />");
        global::docx.DocxSectionFormat format = new global::docx.DocxSectionFormat();
        global::docx.DocxSection section = new global::docx.DocxSection("");

        Assert.Throws<ArgumentNullException>(() => global::docx.DocxSectionFormat.Parse(null!));
        Assert.Throws<ArgumentNullException>(() => global::docx.DocxSectionFormat.ParseXml(null!));
        Assert.Throws<ArgumentNullException>(() => format.ReadProperty(null!));
        Assert.Throws<ArgumentNullException>(() => format.ReadPageSize(null!));
        Assert.Throws<ArgumentNullException>(() => format.ReadPageMargins(null!));
        Assert.Throws<ArgumentNullException>(() => format.ReadColumns(null!));
        Assert.Throws<ArgumentNullException>(() => format.ReadDocGrid(null!));
        Assert.Throws<ArgumentNullException>(() => format.ReadPageNumbering(null!));
        Assert.Throws<ArgumentNullException>(() => format.ReadHeaderReference(null!));
        Assert.Throws<ArgumentNullException>(() => format.ReadFooterReference(null!));
        Assert.Throws<ArgumentNullException>(() => format.ReadEqualWidthColumns(null!));
        Assert.Throws<ArgumentNullException>(() => format.ReadColumnSeparator(null!));
        Assert.Throws<ArgumentNullException>(() => format.SetHeaderReference(null!, ""));
        Assert.Throws<ArgumentNullException>(() => format.SetHeaderReference("", null!));
        Assert.Throws<ArgumentNullException>(() => format.SetFooterReference(null!, ""));
        Assert.Throws<ArgumentNullException>(() => format.SetFooterReference("", null!));
        Assert.Throws<ArgumentNullException>(() => format.GetHeaderReference(null!));
        Assert.Throws<ArgumentNullException>(() => format.GetFooterReference(null!));
        Assert.Throws<ArgumentNullException>(() => format.ClearHeaderReference(null!));
        Assert.Throws<ArgumentNullException>(() => format.ClearFooterReference(null!));
        Assert.Throws<ArgumentNullException>(() => format.IsHeaderLinkedToPrevious(null!));
        Assert.Throws<ArgumentNullException>(() => format.IsFooterLinkedToPrevious(null!));
        Assert.Throws<ArgumentNullException>(() => format.SetSectionType(null!));
        Assert.Throws<ArgumentNullException>(() => format.SetPageSize(null!, "", "", ""));
        Assert.Throws<ArgumentNullException>(() => format.SetPageSize("", null!, "", ""));
        Assert.Throws<ArgumentNullException>(() => format.SetPageSize("", "", null!, ""));
        Assert.Throws<ArgumentNullException>(() => format.SetPageSize("", "", "", null!));
        Assert.Throws<ArgumentNullException>(() => format.SetPageDimensions(null!, ""));
        Assert.Throws<ArgumentNullException>(() => format.SetPageDimensions("", null!));
        Assert.Throws<ArgumentNullException>(() => format.SetPageOrientation(null!));
        Assert.Throws<ArgumentNullException>(() => format.SetPageMargins(null!, "", "", "", "", "", ""));
        Assert.Throws<ArgumentNullException>(() => format.SetPageMargins("", null!, "", "", "", "", ""));
        Assert.Throws<ArgumentNullException>(() => format.SetPageMargins("", "", null!, "", "", "", ""));
        Assert.Throws<ArgumentNullException>(() => format.SetPageMargins("", "", "", null!, "", "", ""));
        Assert.Throws<ArgumentNullException>(() => format.SetPageMargins("", "", "", "", null!, "", ""));
        Assert.Throws<ArgumentNullException>(() => format.SetPageMargins("", "", "", "", "", null!, ""));
        Assert.Throws<ArgumentNullException>(() => format.SetPageMargins("", "", "", "", "", "", null!));
        Assert.Throws<ArgumentNullException>(() => format.SetPageSideMargins(null!, "", "", ""));
        Assert.Throws<ArgumentNullException>(() => format.SetPageSideMargins("", null!, "", ""));
        Assert.Throws<ArgumentNullException>(() => format.SetPageSideMargins("", "", null!, ""));
        Assert.Throws<ArgumentNullException>(() => format.SetPageSideMargins("", "", "", null!));
        Assert.Throws<ArgumentNullException>(() => format.SetHeaderFooterMargins(null!, ""));
        Assert.Throws<ArgumentNullException>(() => format.SetHeaderFooterMargins("", null!));
        Assert.Throws<ArgumentNullException>(() => format.SetGutter(null!));
        Assert.Throws<ArgumentNullException>(() => format.SetColumns(null!, ""));
        Assert.Throws<ArgumentNullException>(() => format.SetColumns("", null!));
        Assert.Throws<ArgumentNullException>(() => format.SetColumnLayout(null!, "", true, true));
        Assert.Throws<ArgumentNullException>(() => format.SetColumnLayout("", null!, true, true));
        Assert.Throws<ArgumentNullException>(() => format.SetDocGrid(null!, "", ""));
        Assert.Throws<ArgumentNullException>(() => format.SetDocGrid("", null!, ""));
        Assert.Throws<ArgumentNullException>(() => format.SetDocGrid("", "", null!));
        Assert.Throws<ArgumentNullException>(() => format.SetPageNumbering(null!, "", "", ""));
        Assert.Throws<ArgumentNullException>(() => format.SetPageNumbering("", null!, "", ""));
        Assert.Throws<ArgumentNullException>(() => format.SetPageNumbering("", "", null!, ""));
        Assert.Throws<ArgumentNullException>(() => format.SetPageNumbering("", "", "", null!));
        Assert.Throws<ArgumentNullException>(() => format.SetPageNumberStart(null!));
        Assert.Throws<ArgumentNullException>(() => format.SetPageNumberFormat(null!));
        Assert.Throws<ArgumentNullException>(() => format.RestartPageNumbering(null!, ""));
        Assert.Throws<ArgumentNullException>(() => format.RestartPageNumbering("", null!));
        Assert.Throws<ArgumentNullException>(() => format.WriteTo(null!));
        Assert.Throws<ArgumentNullException>(() => format.WriteChildrenTo(null!));
        Assert.Throws<ArgumentNullException>(() => format.WriteHeaderReferencesTo(null!));
        Assert.Throws<ArgumentNullException>(() => format.WriteFooterReferencesTo(null!));
        Assert.Throws<ArgumentNullException>(() => format.WriteReferenceTo(null!, "headerReference", "default", "rId1"));
        Assert.Throws<ArgumentNullException>(() => format.WriteReferenceTo(global::docx.DocxXmlWriter.Create(), null!, "default", "rId1"));
        Assert.Throws<ArgumentNullException>(() => format.WriteReferenceTo(global::docx.DocxXmlWriter.Create(), "headerReference", null!, "rId1"));
        Assert.Throws<ArgumentNullException>(() => format.WriteReferenceTo(global::docx.DocxXmlWriter.Create(), "headerReference", "default", null!));
        Assert.Throws<ArgumentNullException>(() => format.WriteStringProperty(null!, "type", "val", "continuous"));
        Assert.Throws<ArgumentNullException>(() => format.WriteStringProperty(global::docx.DocxXmlWriter.Create(), null!, "val", "continuous"));
        Assert.Throws<ArgumentNullException>(() => format.WriteStringProperty(global::docx.DocxXmlWriter.Create(), "type", null!, "continuous"));
        Assert.Throws<ArgumentNullException>(() => format.WriteStringProperty(global::docx.DocxXmlWriter.Create(), "type", "val", null!));
        Assert.Throws<ArgumentNullException>(() => format.WritePageSizeTo(null!));
        Assert.Throws<ArgumentNullException>(() => format.WritePageMarginsTo(null!));
        Assert.Throws<ArgumentNullException>(() => format.WriteColumnsTo(null!));
        Assert.Throws<ArgumentNullException>(() => format.WriteDocGridTo(null!));
        Assert.Throws<ArgumentNullException>(() => format.WriteTitlePageTo(null!));
        Assert.Throws<ArgumentNullException>(() => format.WritePageNumberingTo(null!));
        Assert.Throws<ArgumentNullException>(() => format.WriteOptionalAttribute(null!, "w", "12240"));
        Assert.Throws<ArgumentNullException>(() => format.WriteOptionalAttribute(global::docx.DocxXmlWriter.Create(), null!, "12240"));
        Assert.Throws<ArgumentNullException>(() => format.WriteOptionalAttribute(global::docx.DocxXmlWriter.Create(), "w", null!));
        Assert.Throws<ArgumentNullException>(() => format.WriteOptionalBoolAttribute(null!, "sep", true, true));
        Assert.Throws<ArgumentNullException>(() => format.WriteOptionalBoolAttribute(global::docx.DocxXmlWriter.Create(), null!, true, true));
        Assert.Throws<ArgumentNullException>(() => new global::docx.DocxSection(null!));
        Assert.Throws<ArgumentNullException>(() => global::docx.DocxSection.Parse(null!));
        Assert.Throws<ArgumentNullException>(() => section.SetXml(null!));
        Assert.Throws<ArgumentNullException>(() => section.SetFormat(null!));
        Assert.Throws<ArgumentNullException>(() => section.WriteTo(null!));
        Assert.Throws<FormatException>(() => global::docx.DocxSectionFormat.ParseXml("<w:sectPr xmlns:w=\"http://schemas.openxmlformats.org/wordprocessingml/2006/main\"><w:titlePg w:val=\"maybe\" /></w:sectPr>"));
        Assert.Throws<FormatException>(() => global::docx.DocxSectionFormat.ParseXml("<w:sectPr xmlns:w=\"http://schemas.openxmlformats.org/wordprocessingml/2006/main\"><w:cols w:equalWidth=\"maybe\" /></w:sectPr>"));
        Assert.Throws<FormatException>(() => global::docx.DocxSectionFormat.ParseXml("<w:sectPr xmlns:w=\"http://schemas.openxmlformats.org/wordprocessingml/2006/main\"><w:cols w:sep=\"maybe\" /></w:sectPr>"));

        Assert.That(reader.ReadToElement("root", ""), Is.True);
        Assert.Throws<InvalidOperationException>(() => global::docx.DocxSectionFormat.Parse(reader));
        Assert.Throws<InvalidOperationException>(() => global::docx.DocxSection.Parse(reader));
        Assert.Throws<InvalidOperationException>(() => format.ReadPageSize(reader));
        Assert.Throws<InvalidOperationException>(() => format.ReadPageMargins(reader));
        Assert.Throws<InvalidOperationException>(() => format.ReadColumns(reader));
        Assert.Throws<InvalidOperationException>(() => format.ReadDocGrid(reader));
        Assert.Throws<InvalidOperationException>(() => format.ReadPageNumbering(reader));
        Assert.Throws<InvalidOperationException>(() => format.ReadHeaderReference(reader));
        Assert.Throws<InvalidOperationException>(() => format.ReadFooterReference(reader));
    }

    static global::docx.DocxSectionFormat ParseFormat(string xml)
    {
        global::docx.DocxXmlReader reader = global::docx.DocxXmlReader.Create(xml);

        Assert.That(reader.ReadToElement("sectPr", global::docx.DocxConstants.NamespaceWordprocessingMl), Is.True);

        return global::docx.DocxSectionFormat.Parse(reader);
    }

    static global::docx.DocxSection ParseSection(string xml)
    {
        global::docx.DocxXmlReader reader = global::docx.DocxXmlReader.Create(xml);

        Assert.That(reader.ReadToElement("sectPr", global::docx.DocxConstants.NamespaceWordprocessingMl), Is.True);

        return global::docx.DocxSection.Parse(reader);
    }
}
