using NUnit.Framework;
using xlsx;

namespace xlsx.Tests;

[TestFixture]
public class SlicerReaderWriterTests
{
    [Test]
    public void SlicerCacheReader_ReadsTableAndPivotSlicerCaches()
    {
        string tableXml = "<slicerCacheDefinition xmlns=\"" + XlsxNamespaces.Office2010Spreadsheet + "\" name=\"Slicer_TableRegion\" sourceName=\"Region\"><tableSlicerCache tableId=\"7\" column=\"Region\" /></slicerCacheDefinition>";
        string pivotXml = "<slicerCacheDefinition xmlns=\"" + XlsxNamespaces.Office2010Spreadsheet + "\" name=\"Slicer_PivotRegion\" sourceName=\"Region\"><pivotTables><pivotTable tabId=\"3\" name=\"PivotTable1\" /></pivotTables></slicerCacheDefinition>";

        XlsxSlicerCachePart table = new XlsxSlicerCachePartReader().ReadString("/xl/slicerCaches/slicerCache1.xml", tableXml);
        XlsxSlicerCachePart pivot = new XlsxSlicerCachePartReader().ReadString("/xl/slicerCaches/slicerCache2.xml", pivotXml);

        Assert.That(table.IsValid, Is.True);
        Assert.That(table.Name, Is.EqualTo("Slicer_TableRegion"));
        Assert.That(table.SourceName, Is.EqualTo("Region"));
        Assert.That(table.HasTableSlicerCache, Is.True);
        Assert.That(table.TableId, Is.EqualTo(7));
        Assert.That(table.TableColumnName, Is.EqualTo("Region"));
        Assert.That(table.RawXml, Is.EqualTo(tableXml));
        Assert.That(pivot.IsValid, Is.True);
        Assert.That(pivot.Name, Is.EqualTo("Slicer_PivotRegion"));
        Assert.That(pivot.PivotTables.Count, Is.EqualTo(1));
        Assert.That(pivot.PivotTables[0].TabId, Is.EqualTo(3));
        Assert.That(pivot.PivotTables[0].Name, Is.EqualTo("PivotTable1"));
    }

    [Test]
    public void SlicerReader_ReadsSlicerDefinitions()
    {
        string xml = "<slicers xmlns=\"" + XlsxNamespaces.Office2010Spreadsheet + "\"><slicer name=\"Slicer_Region\" cache=\"Slicer_Region\" caption=\"Region\" columnCount=\"2\" /></slicers>";

        XlsxSlicersPart part = new XlsxSlicersPartReader().ReadString("/xl/slicers/slicer1.xml", xml);

        Assert.That(part.IsValid, Is.True);
        Assert.That(part.RawXml, Is.EqualTo(xml));
        Assert.That(part.Slicers.Count, Is.EqualTo(1));
        Assert.That(part.Slicers[0].Name, Is.EqualTo("Slicer_Region"));
        Assert.That(part.Slicers[0].CacheName, Is.EqualTo("Slicer_Region"));
        Assert.That(part.Slicers[0].Caption, Is.EqualTo("Region"));
        Assert.That(part.Slicers[0].ColumnCount, Is.EqualTo(2));
    }

    [Test]
    public void SlicerWriters_CreateTableAndPivotSlicerXml()
    {
        XlsxSlicerCachePartWriter cacheWriter = new XlsxSlicerCachePartWriter();
        XlsxSlicersPartWriter slicerWriter = new XlsxSlicersPartWriter();
        XlsxSlicerCachePart tableCache = cacheWriter.CreateTableSlicerCache("/xl/slicerCaches/slicerCache1.xml", "Slicer_TableRegion", "Region", 7, "Region");
        XlsxSlicerCachePart pivotCache = cacheWriter.CreatePivotSlicerCache("/xl/slicerCaches/slicerCache2.xml", "Slicer_PivotRegion", "Region", "PivotTable1", 3);
        XlsxSlicersPart slicers = slicerWriter.CreateSlicersPart("/xl/slicers/slicer1.xml", "Slicer_Region", "Slicer_PivotRegion", "Region");

        XlsxSlicerPartWriteResult tableWrite = cacheWriter.WriteString(tableCache);
        XlsxSlicerPartWriteResult pivotWrite = cacheWriter.WriteString(pivotCache);
        XlsxSlicerPartWriteResult slicersWrite = slicerWriter.WriteString(slicers);
        XlsxSlicerCachePart readTable = new XlsxSlicerCachePartReader().ReadString("/xl/slicerCaches/slicerCache1.xml", tableWrite.Xml);
        XlsxSlicerCachePart readPivot = new XlsxSlicerCachePartReader().ReadString("/xl/slicerCaches/slicerCache2.xml", pivotWrite.Xml);
        XlsxSlicersPart readSlicers = new XlsxSlicersPartReader().ReadString("/xl/slicers/slicer1.xml", slicersWrite.Xml);

        Assert.That(tableWrite.IsValid, Is.True);
        Assert.That(tableWrite.Xml, Does.Contain("<tableSlicerCache tableId=\"7\" column=\"Region\" />"));
        Assert.That(pivotWrite.IsValid, Is.True);
        Assert.That(pivotWrite.Xml, Does.Contain("<pivotTables><pivotTable tabId=\"3\" name=\"PivotTable1\" /></pivotTables>"));
        Assert.That(slicersWrite.IsValid, Is.True);
        Assert.That(slicersWrite.Xml, Does.Contain("<slicer name=\"Slicer_Region\" cache=\"Slicer_PivotRegion\" caption=\"Region\" />"));
        Assert.That(readTable.HasTableSlicerCache, Is.True);
        Assert.That(readTable.TableId, Is.EqualTo(7));
        Assert.That(readPivot.PivotTables.Count, Is.EqualTo(1));
        Assert.That(readPivot.PivotTables[0].Name, Is.EqualTo("PivotTable1"));
        Assert.That(readSlicers.Slicers.Count, Is.EqualTo(1));
        Assert.That(readSlicers.Slicers[0].CacheName, Is.EqualTo("Slicer_PivotRegion"));
    }

    [Test]
    public void SlicerWriters_ReportInvalidParts()
    {
        XlsxSlicerCachePart invalidCache = new XlsxSlicerCachePart();
        XlsxSlicersPart invalidSlicers = new XlsxSlicersPart();
        invalidSlicers.Slicers.Add(new XlsxSlicerDefinition());

        XlsxSlicerPartWriteResult cacheWrite = new XlsxSlicerCachePartWriter().WriteString(invalidCache);
        XlsxSlicerPartWriteResult slicersWrite = new XlsxSlicersPartWriter().WriteString(invalidSlicers);

        Assert.That(cacheWrite.IsValid, Is.False);
        Assert.That(slicersWrite.IsValid, Is.False);
        Assert.That(HasDiagnostic(cacheWrite, "slicerCacheWriter.nameMissing"), Is.True);
        Assert.That(HasDiagnostic(cacheWrite, "slicerCacheWriter.sourceMissing"), Is.True);
        Assert.That(HasDiagnostic(slicersWrite, "slicersWriter.nameMissing"), Is.True);
        Assert.That(HasDiagnostic(slicersWrite, "slicersWriter.cacheMissing"), Is.True);
    }

    static bool HasDiagnostic(XlsxSlicerPartWriteResult result, string code)
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
