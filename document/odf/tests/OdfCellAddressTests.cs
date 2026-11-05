namespace odf.Tests;

public class OdfCellAddressTests
{
    [TestCase("Heading_1")]
    [TestCase("paragraph-style")]
    [TestCase("Résumé")]
    [TestCase("文書スタイル")]
    public void StyleNamesUseNcNameSyntax(string lexical)
    {
        OdfStyleNameValue result = OdfStyleNameRules.ParseStyleName(lexical);

        Assert.Multiple(() =>
        {
            Assert.That(result.IsValid, Is.True);
            Assert.That(result.Names, Is.EqualTo(new[] { lexical }));
            Assert.That(OdfStyleNameRules.FormatStyleName(result), Is.EqualTo(lexical));
        });
    }

    [TestCase(null)]
    [TestCase("")]
    [TestCase("1Heading")]
    [TestCase("text:body")]
    [TestCase("two styles")]
    public void InvalidStyleNamesArePreserved(string? lexical)
    {
        OdfStyleNameValue result = OdfStyleNameRules.ParseStyleName(lexical);

        Assert.Multiple(() =>
        {
            Assert.That(result.IsValid, Is.False);
            Assert.That(OdfStyleNameRules.FormatStyleName(result), Is.EqualTo(lexical ?? ""));
        });
    }

    [Test]
    public void StyleNameReferencesAllowAnEmptyValueButNotNull()
    {
        OdfStyleNameValue empty = OdfStyleNameRules.ParseStyleNameReference("");
        OdfStyleNameValue named = OdfStyleNameRules.ParseStyleNameReference("BodyText");
        OdfStyleNameValue missing = OdfStyleNameRules.ParseStyleNameReference(null);

        Assert.Multiple(() =>
        {
            Assert.That(empty.IsValid, Is.True);
            Assert.That(empty.IsReference, Is.True);
            Assert.That(OdfStyleNameRules.FormatStyleName(empty), Is.Empty);
            Assert.That(named.IsValid, Is.True);
            Assert.That(named.Names, Is.EqualTo(new[] { "BodyText" }));
            Assert.That(missing.IsValid, Is.False);
        });
    }

    [TestCase("Heading BodyText Emphasis", new[] { "Heading", "BodyText", "Emphasis" }, "Heading BodyText Emphasis")]
    [TestCase(" Heading\tBodyText\r\nEmphasis ", new[] { "Heading", "BodyText", "Emphasis" }, "Heading BodyText Emphasis")]
    [TestCase("", new string[0], "")]
    [TestCase(" \t\r\n", new string[0], "")]
    public void StyleNameListsCollapseXmlWhitespace(string lexical, string[] names, string canonical)
    {
        OdfStyleNameValue result = OdfStyleNameRules.ParseStyleNameList(lexical);

        Assert.Multiple(() =>
        {
            Assert.That(result.IsValid, Is.True);
            Assert.That(result.IsList, Is.True);
            Assert.That(result.Names, Is.EqualTo(names));
            Assert.That(OdfStyleNameRules.FormatStyleName(result), Is.EqualTo(canonical));
        });
    }

    [TestCase(null)]
    [TestCase("Heading text:body")]
    [TestCase("Heading 2Body")]
    public void InvalidStyleNameListsArePreserved(string? lexical)
    {
        OdfStyleNameValue result = OdfStyleNameRules.ParseStyleNameList(lexical);

        Assert.Multiple(() =>
        {
            Assert.That(result.IsValid, Is.False);
            Assert.That(OdfStyleNameRules.FormatStyleName(result), Is.EqualTo(lexical ?? ""));
        });
    }

    [TestCase("'Sheet 1'", "Sheet 1", false, "'Sheet 1'")]
    [TestCase("$'Bob''s.Data'", "Bob's.Data", true, "$'Bob''s.Data'")]
    [TestCase("''", "", false, "''")]
    public void QuotedSheetNamesDecodeDoubledApostrophes(
        string lexical,
        string sheet,
        bool absolute,
        string canonical)
    {
        OdfQuotedSheetValue result = OdfCellAddressParser.ParseQuotedSheetName(lexical);

        Assert.Multiple(() =>
        {
            Assert.That(result.IsValid, Is.True);
            Assert.That(result.SheetName, Is.EqualTo(sheet));
            Assert.That(result.IsAbsolute, Is.EqualTo(absolute));
            Assert.That(OdfCellAddressFormatter.FormatQuotedSheetName(result), Is.EqualTo(canonical));
        });
    }

    [TestCase(null)]
    [TestCase("")]
    [TestCase("Sheet")]
    [TestCase("'Sheet")]
    [TestCase("Sheet'")]
    [TestCase("'Bob's'")]
    [TestCase("$$'Sheet'")]
    public void InvalidQuotedSheetNamesArePreserved(string? lexical)
    {
        OdfQuotedSheetValue result = OdfCellAddressParser.ParseQuotedSheetName(lexical);

        Assert.Multiple(() =>
        {
            Assert.That(result.IsValid, Is.False);
            Assert.That(OdfCellAddressFormatter.FormatQuotedSheetName(result), Is.EqualTo(lexical ?? ""));
        });
    }

    [TestCase(".A1", "", "A", "1", false, false, false, ".A1")]
    [TestCase("Sheet1.Z99", "Sheet1", "Z", "99", false, false, false, "Sheet1.Z99")]
    [TestCase("$Sheet.$AA$10", "Sheet", "AA", "10", true, true, true, "$Sheet.$AA$10")]
    [TestCase("'My Sheet'.B2", "My Sheet", "B", "2", false, false, false, "'My Sheet'.B2")]
    [TestCase("$'Bob''s.Sheet'.$XFD$1048576", "Bob's.Sheet", "XFD", "1048576", true, true, true, "$'Bob''s.Sheet'.$XFD$1048576")]
    [TestCase("Sheet.$C4", "Sheet", "C", "4", false, true, false, "Sheet.$C4")]
    [TestCase("Sheet.D$5", "Sheet", "D", "5", false, false, true, "Sheet.D$5")]
    [TestCase("Section:One.E6", "Section:One", "E", "6", false, false, false, "Section:One.E6")]
    public void CellAddressesExposeSheetColumnRowAndAbsoluteFlags(
        string lexical,
        string sheet,
        string column,
        string row,
        bool sheetAbsolute,
        bool columnAbsolute,
        bool rowAbsolute,
        string canonical)
    {
        OdfCellAddressValue result = OdfCellAddressParser.ParseCellAddress(lexical);

        Assert.Multiple(() =>
        {
            Assert.That(result.IsValid, Is.True);
            Assert.That(result.HasSheet, Is.EqualTo(sheet.Length > 0));
            Assert.That(result.SheetName, Is.EqualTo(sheet));
            Assert.That(result.Column, Is.EqualTo(column));
            Assert.That(result.Row.ToString(), Is.EqualTo(row));
            Assert.That(result.SheetAbsolute, Is.EqualTo(sheetAbsolute));
            Assert.That(result.ColumnAbsolute, Is.EqualTo(columnAbsolute));
            Assert.That(result.RowAbsolute, Is.EqualTo(rowAbsolute));
            Assert.That(OdfCellAddressFormatter.FormatCellAddress(result), Is.EqualTo(canonical));
        });
    }

    [TestCase(".A1", "1")]
    [TestCase(".Z1", "26")]
    [TestCase(".AA1", "27")]
    [TestCase(".XFD1", "16384")]
    [TestCase(".ZZZZZZ1", "321272406")]
    public void ColumnLabelsMapToArbitrarySizeOrdinals(string lexical, string ordinal)
    {
        OdfCellAddressValue result = OdfCellAddressParser.ParseCellAddress(lexical);

        Assert.Multiple(() =>
        {
            Assert.That(result.ColumnIndex.ToString(), Is.EqualTo(ordinal));
            Assert.That(OdfCellAddressFormatter.FormatCellAddress(result), Is.EqualTo(lexical));
        });
    }

    [Test]
    public void RowOrdinalsAreNotLimitedToRuntimeIntegerWidths()
    {
        string lexical = ".A1234567890123456789012345678901234567890";

        OdfCellAddressValue result = OdfCellAddressParser.ParseCellAddress(lexical);

        Assert.Multiple(() =>
        {
            Assert.That(result.IsValid, Is.True);
            Assert.That(result.Row.ToString(), Is.EqualTo("1234567890123456789012345678901234567890"));
            Assert.That(OdfCellAddressFormatter.FormatCellAddress(result), Is.EqualTo(lexical));
        });
    }

    [TestCase(null)]
    [TestCase("")]
    [TestCase("A1")]
    [TestCase(".a1")]
    [TestCase(".A0")]
    [TestCase(".A01")]
    [TestCase(".A")]
    [TestCase(".1")]
    [TestCase("Sheet A1")]
    [TestCase("My Sheet.A1")]
    [TestCase("'My Sheet.A1")]
    [TestCase("'My'Sheet'.A1")]
    [TestCase("Sheet..A1")]
    [TestCase("Sheet.A1:B2")]
    public void InvalidCellAddressesArePreserved(string? lexical)
    {
        OdfCellAddressValue result = OdfCellAddressParser.ParseCellAddress(lexical);

        Assert.Multiple(() =>
        {
            Assert.That(result.IsValid, Is.False);
            Assert.That(OdfCellAddressFormatter.FormatCellAddress(result), Is.EqualTo(lexical ?? ""));
        });
    }

    [TestCase(".A1", false, true, true, ".A1")]
    [TestCase(".A1:.B2", true, true, true, ".A1:.B2")]
    [TestCase("Sheet.A1:.B2", true, true, true, "Sheet.A1:.B2")]
    [TestCase("Sheet1.A1:Sheet2.B2", true, true, true, "Sheet1.A1:Sheet2.B2")]
    [TestCase("Section:One.A1:Section:Two.B2", true, true, true, "Section:One.A1:Section:Two.B2")]
    [TestCase("'One Sheet'.$A$1:'Two Sheet'.$B$2", true, true, true, "'One Sheet'.$A$1:'Two Sheet'.$B$2")]
    [TestCase(".A:.C", true, true, false, ".A:.C")]
    [TestCase(".$1:.$10", true, false, true, ".$1:.$10")]
    public void CellRangesSupportCellsWholeColumnsAndWholeRows(
        string lexical,
        bool hasEnd,
        bool hasColumn,
        bool hasRow,
        string canonical)
    {
        OdfCellRangeValue result = OdfCellAddressParser.ParseCellRange(lexical);

        Assert.Multiple(() =>
        {
            Assert.That(result.IsValid, Is.True);
            Assert.That(result.HasEnd, Is.EqualTo(hasEnd));
            Assert.That(result.Start.HasColumn, Is.EqualTo(hasColumn));
            Assert.That(result.Start.HasRow, Is.EqualTo(hasRow));
            Assert.That(OdfCellAddressFormatter.FormatCellRange(result), Is.EqualTo(canonical));
        });
    }

    [TestCase(null)]
    [TestCase("")]
    [TestCase(".A")]
    [TestCase(".1")]
    [TestCase(".A1:")]
    [TestCase(":.B2:")]
    [TestCase(".A1:.B2:.C3")]
    [TestCase(".A1:.B")]
    [TestCase(".A:.2")]
    [TestCase("[.A1:.B2]")]
    public void InvalidCellRangesArePreserved(string? lexical)
    {
        OdfCellRangeValue result = OdfCellAddressParser.ParseCellRange(lexical);

        Assert.Multiple(() =>
        {
            Assert.That(result.IsValid, Is.False);
            Assert.That(OdfCellAddressFormatter.FormatCellRange(result), Is.EqualTo(lexical ?? ""));
        });
    }

    [Test]
    public void RangeListsRespectWhitespaceInsideQuotedSheetNames()
    {
        string lexical = " Sheet.A1:Sheet.B2\t'My Sheet'.C3:'My Sheet'.D4\r\n.$5:.$10 ";
        OdfCellRangeListValue result = OdfCellAddressParser.ParseRangeList(lexical);

        Assert.Multiple(() =>
        {
            Assert.That(result.IsValid, Is.True);
            Assert.That(result.Ranges, Has.Length.EqualTo(3));
            Assert.That(result.Ranges[1].Start.SheetName, Is.EqualTo("My Sheet"));
            Assert.That(
                OdfCellAddressFormatter.FormatRangeList(result),
                Is.EqualTo("Sheet.A1:Sheet.B2 'My Sheet'.C3:'My Sheet'.D4 .$5:.$10"));
        });
    }

    [TestCase(null)]
    [TestCase("")]
    [TestCase("   ")]
    [TestCase(".A1 invalid")]
    [TestCase("'Open Sheet.A1 .B2")]
    public void InvalidRangeListsArePreserved(string? lexical)
    {
        OdfCellRangeListValue result = OdfCellAddressParser.ParseRangeList(lexical);

        Assert.Multiple(() =>
        {
            Assert.That(result.IsValid, Is.False);
            Assert.That(OdfCellAddressFormatter.FormatRangeList(result), Is.EqualTo(lexical ?? ""));
        });
    }

    [TestCase("[.A1]", "[.A1]")]
    [TestCase("[$'Sheet 1'.$A$1:.$B$2]", "[$'Sheet 1'.$A$1:.$B$2]")]
    [TestCase("[Sheet1.A1:Sheet2.B2]", "[Sheet1.A1:Sheet2.B2]")]
    [TestCase("[.A:.C]", "[.A:.C]")]
    [TestCase("[.1:.10]", "[.1:.10]")]
    public void FormulaReferencesRequireAndPreserveBrackets(string lexical, string canonical)
    {
        OdfCellRangeValue result = OdfCellAddressParser.ParseFormulaReference(lexical);

        Assert.Multiple(() =>
        {
            Assert.That(result.IsValid, Is.True);
            Assert.That(result.IsBracketed, Is.True);
            Assert.That(OdfCellAddressFormatter.FormatFormulaReference(result), Is.EqualTo(canonical));
            Assert.That(OdfCellAddressFormatter.FormatCellRange(result), Is.EqualTo(canonical));
        });
    }

    [TestCase("['../books/data.ods'#Sheet.A1]", "../books/data.ods", "Sheet", "['../books/data.ods'#Sheet.A1]")]
    [TestCase("['file:///tmp/O''Brien.ods'#$'Data Sheet'.$A$1:.$B$2]", "file:///tmp/O'Brien.ods", "Data Sheet", "['file:///tmp/O''Brien.ods'#$'Data Sheet'.$A$1:.$B$2]")]
    [TestCase("[''#.A1]", "", "", "[''#.A1]")]
    public void ExternalReferencesDecodeAndFormatQuotedIriSources(
        string lexical,
        string source,
        string sheet,
        string canonical)
    {
        OdfCellRangeValue result = OdfCellAddressParser.ParseExternalReference(lexical);

        Assert.Multiple(() =>
        {
            Assert.That(result.IsValid, Is.True);
            Assert.That(result.Start.HasSource, Is.True);
            Assert.That(result.Start.Source, Is.EqualTo(source));
            Assert.That(result.Start.SheetName, Is.EqualTo(sheet));
            Assert.That(OdfCellAddressFormatter.FormatFormulaReference(result), Is.EqualTo(canonical));
        });
    }

    [TestCase(null)]
    [TestCase("")]
    [TestCase(".A1")]
    [TestCase("[.A1]")]
    [TestCase("['has space'#.A1]")]
    [TestCase("['file.ods'.A1]")]
    [TestCase("['file.ods'#.A1")]
    [TestCase("'file.ods'#.A1]")]
    public void InvalidExternalReferencesArePreserved(string? lexical)
    {
        OdfCellRangeValue result = OdfCellAddressParser.ParseExternalReference(lexical);

        Assert.Multiple(() =>
        {
            Assert.That(result.IsValid, Is.False);
            Assert.That(OdfCellAddressFormatter.FormatFormulaReference(result), Is.EqualTo(lexical ?? ""));
        });
    }

    [Test]
    public void NullAddressFormatInputsProduceEmptyText()
    {
        Assert.Multiple(() =>
        {
            Assert.That(OdfStyleNameRules.FormatStyleName(null), Is.Empty);
            Assert.That(OdfCellAddressFormatter.FormatQuotedSheetName(null), Is.Empty);
            Assert.That(OdfCellAddressFormatter.FormatCellAddress(null), Is.Empty);
            Assert.That(OdfCellAddressFormatter.FormatCellRange(null), Is.Empty);
            Assert.That(OdfCellAddressFormatter.FormatFormulaReference(null), Is.Empty);
            Assert.That(OdfCellAddressFormatter.FormatRangeList(null), Is.Empty);
        });
    }
}
