using NUnit.Framework;
using xlsx;

namespace xlsx.Tests;

[TestFixture]
public class StyleInternPoolTests
{
    [Test]
    public void Deduplicate_CollapsesEquivalentRecordsAndRemapsCellFormatReferences()
    {
        XlsxStylesPart part = new XlsxStylesPart();
        AddNumberFormat(part, 164, "0.00");
        AddNumberFormat(part, 200, "0.00");
        AddNumberFormat(part, 201, "m/d/yy");
        AddRaw(part.Fonts, "<font><b /></font>");
        AddRaw(part.Fonts, "<font><b /></font>");
        AddRaw(part.Fonts, "<font><i /></font>");
        AddRaw(part.Fills, "<fill><patternFill patternType=\"none\" /></fill>");
        AddRaw(part.Fills, "<fill><patternFill patternType=\"none\" /></fill>");
        AddRaw(part.Borders, "<border><left /></border>");
        AddRaw(part.Borders, "<border><left /></border>");
        AddRaw(part.DifferentialFormats, "<dxf><font><b /></font></dxf>");
        AddRaw(part.DifferentialFormats, "<dxf><font><b /></font></dxf>");
        part.CellFormats.Add(CreateCellFormat(200, 1, 1, 1, true));
        part.CellFormats.Add(CreateCellFormat(164, 0, 0, 0, true));
        part.CellFormats.Add(CreateCellFormat(201, 2, 0, 0, true));

        XlsxStyleInternResult result = new XlsxStyleInternPool().Deduplicate(part);
        XlsxStylesPartWriteResult writeResult = new XlsxStylesPartWriter().WriteString(part);

        Assert.That(result.IsValid, Is.True);
        Assert.That(result.NumberFormatsRemoved, Is.EqualTo(1));
        Assert.That(result.FontsRemoved, Is.EqualTo(1));
        Assert.That(result.FillsRemoved, Is.EqualTo(1));
        Assert.That(result.BordersRemoved, Is.EqualTo(1));
        Assert.That(result.CellFormatsRemoved, Is.EqualTo(1));
        Assert.That(result.DifferentialFormatsRemoved, Is.EqualTo(1));
        Assert.That(part.NumberFormats.Count, Is.EqualTo(2));
        Assert.That(part.NumberFormatsCount, Is.EqualTo(2));
        Assert.That(part.Fonts.Count, Is.EqualTo(2));
        Assert.That(part.FontsCount, Is.EqualTo(2));
        Assert.That(part.Fills.Count, Is.EqualTo(1));
        Assert.That(part.Borders.Count, Is.EqualTo(1));
        Assert.That(part.CellFormats.Count, Is.EqualTo(2));
        Assert.That(part.CellFormatsCount, Is.EqualTo(2));
        Assert.That(part.DifferentialFormats.Count, Is.EqualTo(1));
        Assert.That(FindNumberFormatMap(result, 200), Is.EqualTo(164));
        Assert.That(FindIndexMap(result.FontIndexMap, 1), Is.EqualTo(0));
        Assert.That(FindIndexMap(result.FillIndexMap, 1), Is.EqualTo(0));
        Assert.That(FindIndexMap(result.BorderIndexMap, 1), Is.EqualTo(0));
        Assert.That(FindIndexMap(result.CellFormatIndexMap, 1), Is.EqualTo(0));
        Assert.That(FindIndexMap(result.CellFormatIndexMap, 2), Is.EqualTo(1));
        Assert.That(part.CellFormats[0].NumberFormatId, Is.EqualTo(164));
        Assert.That(part.CellFormats[0].FontId, Is.EqualTo(0));
        Assert.That(part.CellFormats[0].FillId, Is.EqualTo(0));
        Assert.That(part.CellFormats[0].BorderId, Is.EqualTo(0));
        Assert.That(part.CellFormats[1].NumberFormatId, Is.EqualTo(201));
        Assert.That(part.CellFormats[1].FontId, Is.EqualTo(1));
        Assert.That(writeResult.IsValid, Is.True);
        Assert.That(writeResult.Xml, Does.Contain("<numFmts count=\"2\""));
        Assert.That(writeResult.Xml, Does.Contain("<fonts count=\"2\""));
        Assert.That(writeResult.Xml, Does.Contain("<cellXfs count=\"2\""));
    }

    [Test]
    public void GetOrAddMethods_ReturnExistingEquivalentRecordsAndAppendNewRecords()
    {
        XlsxStylesPart part = new XlsxStylesPart();
        XlsxStyleInternPool pool = new XlsxStyleInternPool();
        AddNumberFormat(part, 164, "0.00");
        AddRaw(part.Fonts, "<font><b /></font>");
        part.CellFormats.Add(CreateCellFormat(164, 0, 0, 0, true));
        XlsxNumberFormatDefinition duplicateNumberFormat = CreateNumberFormat(0, "0.00");
        XlsxNumberFormatDefinition newNumberFormat = CreateNumberFormat(0, "0.000");
        XlsxRawStyleElement duplicateFont = CreateRaw("<font><b /></font>");
        XlsxRawStyleElement newFont = CreateRaw("<font><i /></font>");
        XlsxCellFormatDefinition duplicateCellFormat = CreateCellFormat(164, 0, 0, 0, true);
        XlsxCellFormatDefinition newCellFormat = CreateCellFormat(165, 1, 0, 0, true);

        uint existingNumberFormatId = pool.GetOrAddNumberFormat(part, duplicateNumberFormat);
        uint newNumberFormatId = pool.GetOrAddNumberFormat(part, newNumberFormat);
        uint existingFontIndex = pool.GetOrAddFont(part, duplicateFont);
        uint newFontIndex = pool.GetOrAddFont(part, newFont);
        uint existingCellFormatIndex = pool.GetOrAddCellFormat(part, duplicateCellFormat);
        uint newCellFormatIndex = pool.GetOrAddCellFormat(part, newCellFormat);

        Assert.That(existingNumberFormatId, Is.EqualTo(164));
        Assert.That(newNumberFormatId, Is.EqualTo(165));
        Assert.That(part.NumberFormats.Count, Is.EqualTo(2));
        Assert.That(existingFontIndex, Is.EqualTo(0));
        Assert.That(newFontIndex, Is.EqualTo(1));
        Assert.That(part.Fonts.Count, Is.EqualTo(2));
        Assert.That(existingCellFormatIndex, Is.EqualTo(0));
        Assert.That(newCellFormatIndex, Is.EqualTo(1));
        Assert.That(part.CellFormats.Count, Is.EqualTo(2));
    }

    [Test]
    public void Deduplicate_ReportsInvalidStylesPart()
    {
        XlsxStyleInternResult? result = new XlsxStyleInternPool().Deduplicate(null!);

        Assert.That(result.IsValid, Is.False);
        Assert.That(HasDiagnostic(result, "styles.partNull"), Is.True);
    }

    static void AddNumberFormat(XlsxStylesPart part, uint id, string formatCode)
    {
        part.NumberFormats.Add(CreateNumberFormat(id, formatCode));
    }

    static XlsxNumberFormatDefinition CreateNumberFormat(uint id, string formatCode)
    {
        XlsxNumberFormatDefinition format = new XlsxNumberFormatDefinition();
        if (id > 0)
        {
            format.NumberFormatId = id;
            format.HasNumberFormatId = true;
        }

        format.FormatCode = formatCode;
        return format;
    }

    static void AddRaw(List<XlsxRawStyleElement> items, string rawXml)
    {
        items.Add(CreateRaw(rawXml));
    }

    static XlsxRawStyleElement CreateRaw(string rawXml)
    {
        XlsxRawStyleElement item = new XlsxRawStyleElement();
        item.RawXml = rawXml;
        return item;
    }

    static XlsxCellFormatDefinition CreateCellFormat(uint numberFormatId, uint fontId, uint fillId, uint borderId, bool applyNumberFormat)
    {
        XlsxCellFormatDefinition format = new XlsxCellFormatDefinition();
        format.NumberFormatId = numberFormatId;
        format.HasNumberFormatId = true;
        format.FontId = fontId;
        format.HasFontId = true;
        format.FillId = fillId;
        format.HasFillId = true;
        format.BorderId = borderId;
        format.HasBorderId = true;
        format.ApplyNumberFormat = applyNumberFormat;
        format.HasApplyNumberFormat = true;
        return format;
    }

    static uint FindIndexMap(List<XlsxStyleIndexMap> maps, uint oldIndex)
    {
        foreach (XlsxStyleIndexMap map in maps)
        {
            if (map.OldIndex == oldIndex)
            {
                return map.NewIndex;
            }
        }

        return oldIndex;
    }

    static uint FindNumberFormatMap(XlsxStyleInternResult result, uint oldId)
    {
        foreach (XlsxNumberFormatIdMap map in result.NumberFormatIdMap)
        {
            if (map.OldId == oldId)
            {
                return map.NewId;
            }
        }

        return oldId;
    }

    static bool HasDiagnostic(XlsxStyleInternResult result, string code)
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
