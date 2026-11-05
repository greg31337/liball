using System.Collections.Generic;
using System.Text;
using NUnit.Framework;
using xlsx;

namespace xlsx.Tests;

[TestFixture]
public class SlicerTimelinePreservationTests
{
    [Test]
    public void SlicerTimelineReaders_PreserveRawXmlAndReadMetadata()
    {
        string slicerCacheXml = SlicerCacheXml();
        string slicerXml = SlicerXml();
        string timelineCacheXml = TimelineCacheXml();
        string timelineXml = TimelineXml();

        XlsxSlicerTimelinePart slicerCache = new XlsxSlicerTimelinePartReader().ReadString("/xl/slicerCaches/slicerCache1.xml", slicerCacheXml, "slicerCacheDefinition");
        XlsxSlicerTimelinePart slicer = new XlsxSlicerTimelinePartReader().ReadString("/xl/slicers/slicer1.xml", slicerXml, "slicers");
        XlsxSlicerTimelinePart timelineCache = new XlsxSlicerTimelinePartReader().ReadString("/xl/timelineCaches/timelineCache1.xml", timelineCacheXml, "timelineCacheDefinition");
        XlsxSlicerTimelinePart timeline = new XlsxSlicerTimelinePartReader().ReadString("/xl/timelines/timeline1.xml", timelineXml, "timelines");

        Assert.That(slicerCache.IsValid, Is.True);
        Assert.That(slicerCache.RawXml, Is.EqualTo(slicerCacheXml));
        Assert.That(slicerCache.Name, Is.EqualTo("Slicer_Region"));
        Assert.That(slicerCache.SourceName, Is.EqualTo("Region"));
        Assert.That(slicer.Items.Count, Is.EqualTo(1));
        Assert.That(slicer.Items[0].Name, Is.EqualTo("Slicer_Region"));
        Assert.That(slicer.Items[0].CacheName, Is.EqualTo("Slicer_Region"));
        Assert.That(timelineCache.Name, Is.EqualTo("Timeline_Date"));
        Assert.That(timelineCache.SourceName, Is.EqualTo("Date"));
        Assert.That(timeline.Items.Count, Is.EqualTo(1));
        Assert.That(timeline.Items[0].Name, Is.EqualTo("Timeline_Date"));
        Assert.That(timeline.Items[0].CacheName, Is.EqualTo("Timeline_Date"));
    }

    [Test]
    public void WorkbookOpenSave_PreservesSlicerTimelinePartsAndRelationships()
    {
        byte[] bytes = CreateSlicerTimelinePackage();

        XlsxWorkbookOpenResult open = XlsxWorkbook.OpenBytes(bytes);
        XlsxWorkbookSaveResult save = open.Workbook!.SaveBytes();
        XlsxZipPackageReadResult savedPackage = new XlsxZipPackageReader().ReadBytes(save.Bytes, new XlsxReaderOptions());

        Assert.That(open.IsValid, Is.True);
        Assert.That(open.Workbook.SlicerCaches.Count, Is.EqualTo(1));
        Assert.That(open.Workbook.SlicerCaches[0].Name, Is.EqualTo("Slicer_Region"));
        Assert.That(open.Workbook.Slicers.Count, Is.EqualTo(1));
        Assert.That(open.Workbook.Slicers[0].Items[0].CacheName, Is.EqualTo("Slicer_Region"));
        Assert.That(open.Workbook.TimelineCaches.Count, Is.EqualTo(1));
        Assert.That(open.Workbook.TimelineCaches[0].Name, Is.EqualTo("Timeline_Date"));
        Assert.That(open.Workbook.Timelines.Count, Is.EqualTo(1));
        Assert.That(open.Workbook.Timelines[0].Items[0].CacheName, Is.EqualTo("Timeline_Date"));
        Assert.That(open.Workbook.UnknownParts.ContainsPart("/xl/slicerCaches/slicerCache1.xml"), Is.True);
        Assert.That(open.Workbook.UnknownParts.ContainsPart("/xl/slicers/slicer1.xml"), Is.True);
        Assert.That(open.Workbook.UnknownParts.ContainsPart("/xl/timelineCaches/timelineCache1.xml"), Is.True);
        Assert.That(open.Workbook.UnknownParts.ContainsPart("/xl/timelines/timeline1.xml"), Is.True);
        Assert.That(save.IsValid, Is.True);
        Assert.That(savedPackage.IsValid, Is.True);
        AssertEntryText(savedPackage, XlsxPackagePaths.WorkbookRelationships, "Type=\"" + XlsxRelationshipTypes.SlicerCache + "\"");
        AssertEntryText(savedPackage, XlsxPackagePaths.WorkbookRelationships, "Type=\"" + XlsxRelationshipTypes.TimelineCache + "\"");
        AssertEntryText(savedPackage, "/xl/worksheets/_rels/sheet1.xml.rels", "Type=\"" + XlsxRelationshipTypes.Slicer + "\"");
        AssertEntryText(savedPackage, "/xl/worksheets/_rels/sheet1.xml.rels", "Type=\"" + XlsxRelationshipTypes.Timeline + "\"");
        AssertEntryText(savedPackage, "/xl/slicerCaches/slicerCache1.xml", "sourceName=\"Region\"");
        AssertEntryText(savedPackage, "/xl/slicers/slicer1.xml", "cache=\"Slicer_Region\"");
        AssertEntryText(savedPackage, "/xl/timelineCaches/timelineCache1.xml", "sourceName=\"Date\"");
        AssertEntryText(savedPackage, "/xl/timelines/timeline1.xml", "cache=\"Timeline_Date\"");
        AssertEntryText(savedPackage, XlsxPackagePaths.ContentTypes, XlsxContentTypes.SlicerCache);
        AssertEntryText(savedPackage, XlsxPackagePaths.ContentTypes, XlsxContentTypes.Slicer);
        AssertEntryText(savedPackage, XlsxPackagePaths.ContentTypes, XlsxContentTypes.TimelineCache);
        AssertEntryText(savedPackage, XlsxPackagePaths.ContentTypes, XlsxContentTypes.Timeline);
    }

    static byte[] CreateSlicerTimelinePackage()
    {
        List<XlsxPackageEntry> entries = new List<XlsxPackageEntry>();
        entries.Add(CreateEntry(XlsxPackagePaths.ContentTypes,
            "<Types xmlns=\"http://schemas.openxmlformats.org/package/2006/content-types\">" +
            "<Default Extension=\"rels\" ContentType=\"application/vnd.openxmlformats-package.relationships+xml\" />" +
            "<Default Extension=\"xml\" ContentType=\"application/xml\" />" +
            "<Override PartName=\"/xl/workbook.xml\" ContentType=\"" + XlsxContentTypes.Workbook + "\" />" +
            "<Override PartName=\"/xl/worksheets/sheet1.xml\" ContentType=\"" + XlsxContentTypes.Worksheet + "\" />" +
            "<Override PartName=\"/xl/slicerCaches/slicerCache1.xml\" ContentType=\"" + XlsxContentTypes.SlicerCache + "\" />" +
            "<Override PartName=\"/xl/slicers/slicer1.xml\" ContentType=\"" + XlsxContentTypes.Slicer + "\" />" +
            "<Override PartName=\"/xl/timelineCaches/timelineCache1.xml\" ContentType=\"" + XlsxContentTypes.TimelineCache + "\" />" +
            "<Override PartName=\"/xl/timelines/timeline1.xml\" ContentType=\"" + XlsxContentTypes.Timeline + "\" />" +
            "</Types>"));
        entries.Add(CreateEntry(XlsxPackagePaths.PackageRelationships,
            "<Relationships xmlns=\"http://schemas.openxmlformats.org/package/2006/relationships\">" +
            Relationship("rId1", XlsxRelationshipTypes.OfficeDocument, "xl/workbook.xml") +
            "</Relationships>"));
        entries.Add(CreateEntry(XlsxPackagePaths.Workbook,
            "<workbook xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\" xmlns:r=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships\"><sheets><sheet name=\"Sheet1\" sheetId=\"1\" r:id=\"rIdSheet1\" /></sheets></workbook>"));
        entries.Add(CreateEntry(XlsxPackagePaths.WorkbookRelationships,
            "<Relationships xmlns=\"http://schemas.openxmlformats.org/package/2006/relationships\">" +
            Relationship("rIdSheet1", XlsxRelationshipTypes.Worksheet, "worksheets/sheet1.xml") +
            Relationship("rIdSlicerCache1", XlsxRelationshipTypes.SlicerCache, "slicerCaches/slicerCache1.xml") +
            Relationship("rIdTimelineCache1", XlsxRelationshipTypes.TimelineCache, "timelineCaches/timelineCache1.xml") +
            "</Relationships>"));
        entries.Add(CreateEntry(XlsxPackagePaths.Worksheet1,
            "<worksheet xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\"><sheetData /></worksheet>"));
        entries.Add(CreateEntry("/xl/worksheets/_rels/sheet1.xml.rels",
            "<Relationships xmlns=\"http://schemas.openxmlformats.org/package/2006/relationships\">" +
            Relationship("rIdSlicer1", XlsxRelationshipTypes.Slicer, "../slicers/slicer1.xml") +
            Relationship("rIdTimeline1", XlsxRelationshipTypes.Timeline, "../timelines/timeline1.xml") +
            "</Relationships>"));
        entries.Add(CreateEntry("/xl/slicerCaches/slicerCache1.xml", SlicerCacheXml()));
        entries.Add(CreateEntry("/xl/slicers/slicer1.xml", SlicerXml()));
        entries.Add(CreateEntry("/xl/timelineCaches/timelineCache1.xml", TimelineCacheXml()));
        entries.Add(CreateEntry("/xl/timelines/timeline1.xml", TimelineXml()));

        XlsxZipPackageWriteResult write = new XlsxZipPackageWriter().WriteBytes(entries, new XlsxWriterOptions());
        Assert.That(write.IsValid, Is.True);
        return write.Bytes;
    }

    static string SlicerCacheXml()
    {
        return "<slicerCacheDefinition xmlns=\"" + XlsxNamespaces.Office2010Spreadsheet + "\" name=\"Slicer_Region\" sourceName=\"Region\"><pivotTables><pivotTable tabId=\"1\" name=\"PivotTable1\" /></pivotTables></slicerCacheDefinition>";
    }

    static string SlicerXml()
    {
        return "<slicers xmlns=\"" + XlsxNamespaces.Office2010Spreadsheet + "\"><slicer name=\"Slicer_Region\" cache=\"Slicer_Region\" caption=\"Region\" /></slicers>";
    }

    static string TimelineCacheXml()
    {
        return "<timelineCacheDefinition xmlns=\"" + XlsxNamespaces.Office2013Spreadsheet + "\" name=\"Timeline_Date\" sourceName=\"Date\" />";
    }

    static string TimelineXml()
    {
        return "<timelines xmlns=\"" + XlsxNamespaces.Office2013Spreadsheet + "\"><timeline name=\"Timeline_Date\" cache=\"Timeline_Date\" caption=\"Date\" /></timelines>";
    }

    static XlsxPackageEntry CreateEntry(string path, string text)
    {
        XlsxPackageEntry entry = new XlsxPackageEntry();
        entry.Path = path;
        entry.Data = Encoding.UTF8.GetBytes(text);
        entry.UncompressedBytes = entry.Data.Length;
        return entry;
    }

    static string Relationship(string id, string type, string target)
    {
        return "<Relationship Id=\"" + id + "\" Type=\"" + type + "\" Target=\"" + target + "\" />";
    }

    static void AssertEntryText(XlsxZipPackageReadResult package, string path, string expected)
    {
        XlsxPackageEntry? entry = package.GetEntry(path);
        Assert.That(entry, Is.Not.Null);
        Assert.That(entry!.GetText(), Does.Contain(expected));
    }
}
