using System.Collections.Generic;
using NUnit.Framework;
using xlsx;

namespace xlsx.Tests;

[TestFixture]
public class PivotTimelineStateIsolationCompletionTests
{
    [Test]
    public void SetCacheRefresh_ClonesSettingsAndRoundTripsAllRefreshFields()
    {
        XlsxPivotCacheDefinitionPart cache = CreateCacheDefinition();
        XlsxPivotCacheRefreshSettings settings = CreateRefreshSettings();
        XlsxPivotService service = new XlsxPivotService();

        XlsxPivotTableOperationResult result = service.SetCacheRefresh(cache, settings);
        XlsxPivotCacheRefreshSettings stored = cache.RefreshSettings;
        settings.RefreshOnLoad = false;
        settings.MissingItemsLimit = 1;
        settings.RefreshedDate = 1;
        XlsxPivotCacheDefinitionWriteResult write = new XlsxPivotCacheDefinitionWriter().WriteString(cache);
        XlsxPivotCacheDefinitionPart roundTrip = new XlsxPivotCacheDefinitionReader().ReadString(write.Xml);

        Assert.That(result.IsValid, Is.True);
        Assert.That(stored, Is.Not.SameAs(settings));
        Assert.That(stored.SaveData, Is.False);
        Assert.That(stored.RefreshOnLoad, Is.True);
        Assert.That(stored.EnableRefresh, Is.True);
        Assert.That(stored.BackgroundQuery, Is.True);
        Assert.That(stored.UpgradeOnRefresh, Is.True);
        Assert.That(stored.CreatedVersion, Is.EqualTo(3));
        Assert.That(stored.RefreshedVersion, Is.EqualTo(8));
        Assert.That(stored.MinimumRefreshableVersion, Is.EqualTo(2));
        Assert.That(stored.MissingItemsLimit, Is.EqualTo(25));
        Assert.That(stored.RefreshedDate, Is.EqualTo(46000.5));
        Assert.That(write.IsValid, Is.True);
        Assert.That(roundTrip.RefreshSettings.RefreshOnLoad, Is.True);
        Assert.That(roundTrip.RefreshSettings.MissingItemsLimit, Is.EqualTo(25));
        Assert.That(roundTrip.RefreshSettings.RefreshedDate, Is.EqualTo(46000.5));
    }

    [Test]
    public void AddFormat_DeepClonesAreaAndReferencesBeforeRoundTrip()
    {
        List<string> fields = new List<string>();
        fields.Add("Region");
        fields.Add("Amount");
        XlsxPivotTableCreateResult created = new XlsxPivotTableDefinitionWriter().CreateFromRange("SalesPivot", 1, "Data", "A1:B20", "D3:F12", fields);
        XlsxPivotTableArea area = new XlsxPivotTableArea();
        area.Type = "data";
        area.FieldIndex = 1;
        area.HasFieldIndex = true;
        area.Axis = "axisRow";
        XlsxPivotService service = new XlsxPivotService();
        service.AddAreaReference(area, 1, true, false);

        XlsxPivotTableOperationResult result = service.AddFormat(created.PivotTableDefinition, area, 4, "formatting");
        XlsxPivotTableArea stored = created.PivotTableDefinition.Formats[0].Area;
        area.Type = "changed";
        area.Axis = "changed";
        area.References[0].FieldIndex = 9;
        area.References[0].Selected = false;
        XlsxPivotTableDefinitionWriteResult write = new XlsxPivotTableDefinitionWriter().WriteString(created.PivotTableDefinition);
        XlsxPivotTableDefinitionPart roundTrip = new XlsxPivotTableDefinitionReader().ReadString(write.Xml);

        Assert.That(result.IsValid, Is.True);
        Assert.That(stored, Is.Not.SameAs(area));
        Assert.That(stored.Type, Is.EqualTo("data"));
        Assert.That(stored.Axis, Is.EqualTo("axisRow"));
        Assert.That(stored.References[0], Is.Not.SameAs(area.References[0]));
        Assert.That(stored.References[0].FieldIndex, Is.EqualTo(1));
        Assert.That(stored.References[0].Selected, Is.True);
        Assert.That(write.IsValid, Is.True);
        Assert.That(roundTrip.Formats[0].Area.Type, Is.EqualTo("data"));
        Assert.That(roundTrip.Formats[0].Area.Axis, Is.EqualTo("axisRow"));
        Assert.That(roundTrip.Formats[0].Area.References[0].FieldIndex, Is.EqualTo(1));
        Assert.That(roundTrip.Formats[0].Area.References[0].Selected, Is.True);
    }

    [Test]
    public void SetTimelineState_ClonesSelectionStateBeforeRoundTrip()
    {
        XlsxSlicerTimelineService service = new XlsxSlicerTimelineService();
        XlsxSlicerTimelineOperationResult created = service.CreateTimeline("/xl/timelineCaches/timelineCache1.xml", "/xl/timelines/timeline1.xml", "Timeline_Date", "OrderDate", "PivotTable1", 2, "Order date");
        XlsxTimelineState state = CreateTimelineState();

        XlsxSlicerTimelineOperationResult result = service.SetTimelineState(created.TimelineCache!, state);
        XlsxTimelineState stored = created.TimelineCache!.State;
        state.FilterType = "changed";
        state.StartDate = new System.DateTime(2030, 1, 1);
        state.Level = 9;
        XlsxSlicerPartWriteResult write = new XlsxSlicerTimelinePartWriter().WriteString(created.TimelineCache);
        XlsxSlicerTimelinePart roundTrip = new XlsxSlicerTimelinePartReader().ReadString("/xl/timelineCaches/timelineCache1.xml", write.Xml, "timelineCacheDefinition");

        Assert.That(result.IsValid, Is.True);
        Assert.That(stored, Is.Not.SameAs(state));
        Assert.That(stored.PivotCacheId, Is.EqualTo(5));
        Assert.That(stored.FilterId, Is.EqualTo(8));
        Assert.That(stored.FilterType, Is.EqualTo("dateBetween"));
        Assert.That(stored.StartDate, Is.EqualTo(new System.DateTime(2026, 1, 1)));
        Assert.That(stored.EndDate, Is.EqualTo(new System.DateTime(2026, 6, 30)));
        Assert.That(stored.Level, Is.EqualTo(2));
        Assert.That(write.IsValid, Is.True);
        Assert.That(roundTrip.State.FilterType, Is.EqualTo("dateBetween"));
        Assert.That(roundTrip.State.StartDate, Is.EqualTo(new System.DateTime(2026, 1, 1)));
        Assert.That(roundTrip.State.Level, Is.EqualTo(2));
    }

    static XlsxPivotCacheDefinitionPart CreateCacheDefinition()
    {
        string xml = "<pivotCacheDefinition xmlns=\"" + XlsxNamespaces.Spreadsheet + "\" recordCount=\"1\"><cacheSource type=\"worksheet\"><worksheetSource ref=\"A1:A2\" sheet=\"Data\" /></cacheSource><cacheFields count=\"1\"><cacheField name=\"Amount\"><sharedItems count=\"1\"><n v=\"10\" /></sharedItems></cacheField></cacheFields></pivotCacheDefinition>";
        return new XlsxPivotCacheDefinitionReader().ReadString(xml);
    }

    static XlsxPivotCacheRefreshSettings CreateRefreshSettings()
    {
        XlsxPivotCacheRefreshSettings settings = new XlsxPivotCacheRefreshSettings();
        settings.SaveData = false;
        settings.HasSaveData = true;
        settings.RefreshOnLoad = true;
        settings.HasRefreshOnLoad = true;
        settings.EnableRefresh = true;
        settings.HasEnableRefresh = true;
        settings.BackgroundQuery = true;
        settings.HasBackgroundQuery = true;
        settings.UpgradeOnRefresh = true;
        settings.HasUpgradeOnRefresh = true;
        settings.CreatedVersion = 3;
        settings.HasCreatedVersion = true;
        settings.RefreshedVersion = 8;
        settings.HasRefreshedVersion = true;
        settings.MinimumRefreshableVersion = 2;
        settings.HasMinimumRefreshableVersion = true;
        settings.MissingItemsLimit = 25;
        settings.HasMissingItemsLimit = true;
        settings.RefreshedDate = 46000.5;
        settings.HasRefreshedDate = true;
        return settings;
    }

    static XlsxTimelineState CreateTimelineState()
    {
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
        return state;
    }
}
