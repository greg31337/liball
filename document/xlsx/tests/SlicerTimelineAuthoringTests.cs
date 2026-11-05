using NUnit.Framework;
using System.Collections.Generic;
using xlsx;

namespace xlsx.Tests;

[TestFixture]
public class SlicerTimelineAuthoringTests
{
    [Test]
    public void TableSlicerAuthoring_WritesBindingsSelectionsStylesAndExtensions()
    {
        XlsxSlicerTimelineService service = new XlsxSlicerTimelineService();
        XlsxSlicerTimelineOperationResult created = service.CreateTableSlicer("/xl/slicerCaches/slicerCache1.xml", "/xl/slicers/slicer1.xml", "Slicer_Region", "Region", 7, "Region", "Sales region");
        XlsxSlicerTimelineOperationResult rebound = service.BindTable(created.SlicerCache!, 8, "Territory");
        List<int> selected = new List<int>();
        selected.Add(0);
        selected.Add(2);
        XlsxSlicerTimelineOperationResult selection = service.SetSelections(created.SlicerCache!, 3, selected);
        XlsxSlicerTimelineOperationResult style = service.SetSlicerStyle(created.Slicers!, "Slicer_Region", "SlicerStyleLight2", 2, true);
        string extension = "<extLst xmlns=\"" + XlsxNamespaces.Office2010Spreadsheet + "\"><ext uri=\"future\"><future xmlns=\"urn:future\" value=\"1\" /></ext></extLst>";
        XlsxSlicerTimelineOperationResult preserved = service.PreserveSlicerExtension(created.SlicerCache!, extension);
        XlsxSlicerPartWriteResult cacheWrite = new XlsxSlicerCachePartWriter().WriteString(created.SlicerCache!);
        XlsxSlicerPartWriteResult slicersWrite = new XlsxSlicersPartWriter().WriteString(created.Slicers!);
        XlsxSlicerCachePart cacheRoundTrip = new XlsxSlicerCachePartReader().ReadString("/xl/slicerCaches/slicerCache1.xml", cacheWrite.Xml);
        XlsxSlicersPart slicersRoundTrip = new XlsxSlicersPartReader().ReadString("/xl/slicers/slicer1.xml", slicersWrite.Xml);

        Assert.That(created.IsValid, Is.True);
        Assert.That(rebound.IsValid, Is.True);
        Assert.That(created.SlicerCache!.TableId, Is.EqualTo(8));
        Assert.That(selection.IsValid, Is.True);
        Assert.That(style.IsValid, Is.True);
        Assert.That(preserved.IsValid, Is.True);
        Assert.That(cacheWrite.IsValid, Is.True);
        Assert.That(cacheWrite.Xml, Does.Contain("<tableSlicerCache tableId=\"8\" column=\"Territory\""));
        Assert.That(cacheWrite.Xml, Does.Contain("<slicerCacheItems count=\"3\">"));
        Assert.That(cacheWrite.Xml, Does.Contain("<slicerCacheItem x=\"2\" s=\"1\" />"));
        Assert.That(cacheWrite.Xml, Does.Contain("urn:future"));
        Assert.That(slicersWrite.Xml, Does.Contain("slicerStyle=\"SlicerStyleLight2\""));
        Assert.That(slicersWrite.Xml, Does.Contain("columnCount=\"2\""));
        Assert.That(cacheRoundTrip.Selections.Count, Is.EqualTo(3));
        Assert.That(cacheRoundTrip.Selections[1].Selected, Is.False);
        Assert.That(cacheRoundTrip.ExtensionListRawXml, Does.Contain("urn:future"));
        Assert.That(slicersRoundTrip.Slicers[0].StyleName, Is.EqualTo("SlicerStyleLight2"));
        Assert.That(slicersRoundTrip.Slicers[0].ShowCaption, Is.True);
    }

    [Test]
    public void PivotSlicerBindingsAndRelationships_CanBeAddedChangedAndRemoved()
    {
        XlsxSlicerTimelineService service = new XlsxSlicerTimelineService();
        XlsxSlicerTimelineOperationResult created = service.CreatePivotSlicer("/xl/slicerCaches/slicerCache2.xml", "/xl/slicers/slicer2.xml", "Slicer_Channel", "Channel", "PivotTable1", 3, "Channel");
        XlsxSlicerTimelineOperationResult secondBinding = service.BindPivot(created.SlicerCache!, "PivotTable2", 4);
        XlsxRelationshipsPart relationships = new XlsxRelationshipsPart();
        XlsxSlicerTimelineOperationResult cacheRelationship = service.AddRelationship(relationships, "rIdCache1", XlsxRelationshipTypes.SlicerCache, "slicerCaches/slicerCache2.xml", "/xl/slicerCaches/slicerCache2.xml");
        XlsxSlicerTimelineOperationResult visualRelationship = service.AddRelationship(relationships, "rIdSlicer1", XlsxRelationshipTypes.Slicer, "../slicers/slicer2.xml", "/xl/slicers/slicer2.xml");
        XlsxSlicerTimelineOperationResult removed = service.RemoveRelationship(relationships, "rIdCache1");
        XlsxSlicerPartWriteResult write = new XlsxSlicerCachePartWriter().WriteString(created.SlicerCache!);

        Assert.That(created.IsValid, Is.True);
        Assert.That(secondBinding.IsValid, Is.True);
        Assert.That(created.SlicerCache!.PivotTables.Count, Is.EqualTo(2));
        Assert.That(cacheRelationship.IsValid, Is.True);
        Assert.That(visualRelationship.IsValid, Is.True);
        Assert.That(removed.IsValid, Is.True);
        Assert.That(relationships.Relationships.Count, Is.EqualTo(1));
        Assert.That(relationships.Relationships[0].Type, Is.EqualTo(XlsxRelationshipTypes.Slicer));
        Assert.That(relationships.Relationships[0].TargetPath, Is.EqualTo("/xl/slicers/slicer2.xml"));
        Assert.That(write.Xml, Does.Contain("name=\"PivotTable1\""));
        Assert.That(write.Xml, Does.Contain("name=\"PivotTable2\""));
    }

    [Test]
    public void TimelineAuthoring_WritesStateStyleBindingsAndPreservedExtensions()
    {
        XlsxSlicerTimelineService service = new XlsxSlicerTimelineService();
        XlsxSlicerTimelineOperationResult created = service.CreateTimeline("/xl/timelineCaches/timelineCache1.xml", "/xl/timelines/timeline1.xml", "Timeline_Date", "OrderDate", "PivotTable1", 2, "Order date");
        XlsxTimelineState state = new XlsxTimelineState();
        state.PivotCacheId = 5;
        state.HasPivotCacheId = true;
        state.FilterId = 8;
        state.HasFilterId = true;
        state.FilterType = "dateBetween";
        state.SingleRange = true;
        state.HasSingleRange = true;
        state.StartDate = new System.DateTime(2026, 1, 1);
        state.EndDate = new System.DateTime(2026, 6, 30);
        state.HasSelection = true;
        state.Level = 2;
        state.HasLevel = true;
        XlsxSlicerTimelineOperationResult stateResult = service.SetTimelineState(created.TimelineCache!, state);
        XlsxSlicerTimelineOperationResult styleResult = service.SetTimelineStyle(created.Timelines!, "Timeline_Date", "TimeSlicerStyleLight1", 2, 1);
        string extension = "<extLst xmlns=\"" + XlsxNamespaces.Office2013Spreadsheet + "\"><ext uri=\"future\"><future xmlns=\"urn:timeline-future\" /></ext></extLst>";
        service.PreserveTimelineExtension(created.TimelineCache!, extension);
        XlsxSlicerPartWriteResult cacheWrite = new XlsxSlicerTimelinePartWriter().WriteString(created.TimelineCache);
        XlsxSlicerPartWriteResult timelinesWrite = new XlsxSlicerTimelinePartWriter().WriteString(created.Timelines);
        XlsxSlicerTimelinePart cacheRoundTrip = new XlsxSlicerTimelinePartReader().ReadString("/xl/timelineCaches/timelineCache1.xml", cacheWrite.Xml, "timelineCacheDefinition");
        XlsxSlicerTimelinePart timelinesRoundTrip = new XlsxSlicerTimelinePartReader().ReadString("/xl/timelines/timeline1.xml", timelinesWrite.Xml, "timelines");

        Assert.That(stateResult.IsValid, Is.True);
        Assert.That(styleResult.IsValid, Is.True);
        Assert.That(cacheWrite.IsValid, Is.True);
        Assert.That(cacheWrite.Xml, Does.Contain("<pivotTable tabId=\"2\" name=\"PivotTable1\" />"));
        Assert.That(cacheWrite.Xml, Does.Contain("filterType=\"dateBetween\" singleRange=\"1\""));
        Assert.That(cacheWrite.Xml, Does.Contain("startDate=\"2026-01-01T00:00:00\""));
        Assert.That(cacheWrite.Xml, Does.Contain("urn:timeline-future"));
        Assert.That(timelinesWrite.Xml, Does.Contain("style=\"TimeSlicerStyleLight1\""));
        Assert.That(timelinesWrite.Xml, Does.Contain("selectionLevel=\"1\""));
        Assert.That(cacheRoundTrip.PivotTables.Count, Is.EqualTo(1));
        Assert.That(cacheRoundTrip.HasState, Is.True);
        Assert.That(cacheRoundTrip.State.PivotCacheId, Is.EqualTo(5));
        Assert.That(cacheRoundTrip.State.StartDate, Is.EqualTo(new System.DateTime(2026, 1, 1)));
        Assert.That(cacheRoundTrip.ExtensionListRawXml, Does.Contain("urn:timeline-future"));
        Assert.That(timelinesRoundTrip.Items[0].StyleName, Is.EqualTo("TimeSlicerStyleLight1"));
        Assert.That(timelinesRoundTrip.Items[0].SelectionLevel, Is.EqualTo(1));
    }

    [Test]
    public void AuthoringOperations_ReportInvalidSelectionsBindingsStateAndRelationships()
    {
        XlsxSlicerTimelineService service = new XlsxSlicerTimelineService();
        XlsxSlicerCachePart cache = new XlsxSlicerCachePart();
        List<int> selections = new List<int>();
        selections.Add(2);
        XlsxTimelineState state = new XlsxTimelineState();
        state.HasSelection = true;
        state.StartDate = new System.DateTime(2026, 2, 1);
        state.EndDate = new System.DateTime(2026, 1, 1);

        XlsxSlicerTimelineOperationResult create = service.CreateTableSlicer("", "", "", "", 0, "", "");
        XlsxSlicerTimelineOperationResult selected = service.SetSelections(cache, 1, selections);
        XlsxSlicerTimelineOperationResult timeline = service.SetTimelineState(new XlsxSlicerTimelinePart(), state);
        XlsxSlicerTimelineOperationResult relationship = service.AddRelationship(new XlsxRelationshipsPart(), "", "bad", "", "");
        XlsxSlicerPartWriteResult writer = new XlsxSlicerTimelinePartWriter().WriteString(null);

        Assert.That(create.IsValid, Is.False);
        Assert.That(selected.IsValid, Is.False);
        Assert.That(timeline.IsValid, Is.False);
        Assert.That(relationship.IsValid, Is.False);
        Assert.That(writer.IsValid, Is.False);
    }
}
