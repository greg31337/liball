using System;
using System.Collections.Generic;

namespace xlsx;

/// <summary>
/// Provides slicer and timeline creation, binding, selection, style, and relationship operations.
/// </summary>
public class XlsxSlicerTimelineService : IXlsxSlicerTimelineService
{
    /// <summary>Initializes a slicer and timeline service.</summary>
    public XlsxSlicerTimelineService()
    {
    }

    /// <summary>Creates a table-bound slicer cache and slicers part.</summary>
    /// <param name="cachePath">The slicer cache part path.</param>
    /// <param name="slicersPath">The slicers part path.</param>
    /// <param name="name">The slicer name.</param>
    /// <param name="sourceName">The source field name.</param>
    /// <param name="tableId">The source table identifier.</param>
    /// <param name="columnName">The source column name.</param>
    /// <param name="caption">The displayed caption.</param>
    /// <returns>The authoring result.</returns>
    public XlsxSlicerTimelineOperationResult CreateTableSlicer(string cachePath, string slicersPath, string name, string sourceName, uint tableId, string columnName, string caption)
    {
        XlsxSlicerTimelineOperationResult result = ValidateIdentity(name, sourceName);
        if (tableId == 0) result.AddError("slicerAuthoring.tableIdMissing", "Table slicer source identifier must be greater than zero.");
        if (XlsxCellValueConverter.NormalizeText(columnName).Length == 0) result.AddError("slicerAuthoring.columnMissing", "Table slicer source column must not be empty.");
        if (!result.IsValid) return result;
        XlsxSlicerCachePart cache = new XlsxSlicerCachePartWriter().CreateTableSlicerCache(cachePath, name, sourceName, tableId, columnName);
        XlsxSlicersPart slicers = new XlsxSlicersPartWriter().CreateSlicersPart(slicersPath, name, name, caption);
        result.SlicerCache = cache;
        result.Slicers = slicers;
        return result;
    }

    /// <summary>Creates a pivot-bound slicer cache and slicers part.</summary>
    /// <param name="cachePath">The slicer cache part path.</param>
    /// <param name="slicersPath">The slicers part path.</param>
    /// <param name="name">The slicer name.</param>
    /// <param name="sourceName">The source field name.</param>
    /// <param name="pivotTableName">The source pivot-table name.</param>
    /// <param name="tabId">The source worksheet identifier.</param>
    /// <param name="caption">The displayed caption.</param>
    /// <returns>The authoring result.</returns>
    public XlsxSlicerTimelineOperationResult CreatePivotSlicer(string cachePath, string slicersPath, string name, string sourceName, string pivotTableName, uint tabId, string caption)
    {
        XlsxSlicerTimelineOperationResult result = ValidateIdentity(name, sourceName);
        if (XlsxCellValueConverter.NormalizeText(pivotTableName).Length == 0) result.AddError("slicerAuthoring.pivotNameMissing", "Pivot slicer source name must not be empty.");
        if (!result.IsValid) return result;
        result.SlicerCache = new XlsxSlicerCachePartWriter().CreatePivotSlicerCache(cachePath, name, sourceName, pivotTableName, tabId);
        result.Slicers = new XlsxSlicersPartWriter().CreateSlicersPart(slicersPath, name, name, caption);
        return result;
    }

    /// <summary>Creates a timeline cache and timelines part.</summary>
    /// <param name="cachePath">The timeline cache part path.</param>
    /// <param name="timelinesPath">The timelines part path.</param>
    /// <param name="name">The timeline name.</param>
    /// <param name="sourceName">The source date field name.</param>
    /// <param name="pivotTableName">The source pivot-table name.</param>
    /// <param name="tabId">The source worksheet identifier.</param>
    /// <param name="caption">The displayed caption.</param>
    /// <returns>The authoring result.</returns>
    public XlsxSlicerTimelineOperationResult CreateTimeline(string cachePath, string timelinesPath, string name, string sourceName, string pivotTableName, uint tabId, string caption)
    {
        XlsxSlicerTimelineOperationResult result = ValidateIdentity(name, sourceName);
        if (XlsxCellValueConverter.NormalizeText(pivotTableName).Length == 0) result.AddError("timelineAuthoring.pivotNameMissing", "Timeline pivot source name must not be empty.");
        if (!result.IsValid) return result;
        XlsxSlicerTimelinePart cache = new XlsxSlicerTimelinePart();
        cache.PartPath = XlsxCellValueConverter.NormalizeText(cachePath);
        cache.RootElementName = "timelineCacheDefinition";
        cache.Name = XlsxCellValueConverter.NormalizeText(name);
        cache.SourceName = XlsxCellValueConverter.NormalizeText(sourceName);
        XlsxSlicerPivotTableReference reference = new XlsxSlicerPivotTableReference();
        reference.Name = XlsxCellValueConverter.NormalizeText(pivotTableName);
        reference.TabId = tabId;
        reference.HasTabId = true;
        cache.PivotTables.Add(reference);
        XlsxSlicerTimelinePart timelines = new XlsxSlicerTimelinePart();
        timelines.PartPath = XlsxCellValueConverter.NormalizeText(timelinesPath);
        timelines.RootElementName = "timelines";
        XlsxSlicerTimelineItem item = new XlsxSlicerTimelineItem();
        item.Name = cache.Name;
        item.CacheName = cache.Name;
        item.Caption = XlsxCellValueConverter.NormalizeText(caption);
        timelines.Items.Add(item);
        result.TimelineCache = cache;
        result.Timelines = timelines;
        return result;
    }

    /// <summary>Rebinds a slicer cache to a table column.</summary>
    /// <param name="cache">The slicer cache.</param>
    /// <param name="tableId">The table identifier.</param>
    /// <param name="columnName">The table column name.</param>
    /// <returns>The operation result.</returns>
    public XlsxSlicerTimelineOperationResult BindTable(XlsxSlicerCachePart cache, uint tableId, string columnName)
    {
        XlsxSlicerTimelineOperationResult result = new XlsxSlicerTimelineOperationResult();
        if (cache == null) result.AddError("slicerBinding.cacheNull", "Table binding requires a slicer cache.");
        if (tableId == 0) result.AddError("slicerBinding.tableIdMissing", "Table binding identifier must be greater than zero.");
        if (XlsxCellValueConverter.NormalizeText(columnName).Length == 0) result.AddError("slicerBinding.columnMissing", "Table binding column must not be empty.");
        if (!result.IsValid || cache == null) return result;
        cache.HasTableSlicerCache = true;
        cache.TableId = tableId;
        cache.HasTableId = true;
        cache.TableColumnName = XlsxCellValueConverter.NormalizeText(columnName);
        cache.PivotTables.Clear();
        cache.RawXml = "";
        return result;
    }

    /// <summary>Adds a pivot-table binding to a slicer cache.</summary>
    /// <param name="cache">The slicer cache.</param>
    /// <param name="pivotTableName">The pivot-table name.</param>
    /// <param name="tabId">The worksheet identifier.</param>
    /// <returns>The operation result.</returns>
    public XlsxSlicerTimelineOperationResult BindPivot(XlsxSlicerCachePart cache, string pivotTableName, uint tabId)
    {
        XlsxSlicerTimelineOperationResult result = new XlsxSlicerTimelineOperationResult();
        string name = XlsxCellValueConverter.NormalizeText(pivotTableName);
        if (cache == null) result.AddError("slicerBinding.cacheNull", "Pivot binding requires a slicer cache.");
        if (name.Length == 0) result.AddError("slicerBinding.pivotNameMissing", "Pivot binding name must not be empty.");
        if (!result.IsValid || cache == null) return result;
        foreach (XlsxSlicerPivotTableReference existing in cache.PivotTables)
        {
            if (existing.Name == name && existing.TabId == tabId) return result;
        }
        XlsxSlicerPivotTableReference reference = new XlsxSlicerPivotTableReference();
        reference.Name = name;
        reference.TabId = tabId;
        reference.HasTabId = true;
        cache.HasTableSlicerCache = false;
        cache.PivotTables.Add(reference);
        cache.RawXml = "";
        return result;
    }

    /// <summary>Sets selected slicer item indexes.</summary>
    /// <param name="cache">The slicer cache.</param>
    /// <param name="itemCount">The number of source items.</param>
    /// <param name="selectedIndexes">The selected zero-based item indexes.</param>
    /// <returns>The operation result.</returns>
    public XlsxSlicerTimelineOperationResult SetSelections(XlsxSlicerCachePart cache, int itemCount, List<int>? selectedIndexes)
    {
        XlsxSlicerTimelineOperationResult result = new XlsxSlicerTimelineOperationResult();
        if (cache == null) result.AddError("slicerSelection.cacheNull", "Slicer selection requires a cache.");
        if (itemCount < 0) result.AddError("slicerSelection.countInvalid", "Slicer item count must not be negative.");
        if (selectedIndexes == null) result.AddError("slicerSelection.indexesNull", "Selected slicer indexes must not be null.");
        if (!result.IsValid || cache == null || selectedIndexes == null) return result;
        foreach (int selectedIndex in selectedIndexes)
        {
            if (selectedIndex < 0 || selectedIndex >= itemCount)
            {
                result.AddError("slicerSelection.indexOutsideItems", "Selected slicer index must identify an existing item.");
            }
        }
        if (!result.IsValid) return result;
        cache.Selections.Clear();
        int index = 0;
        while (index < itemCount)
        {
            XlsxSlicerSelectionItem item = new XlsxSlicerSelectionItem();
            item.Index = index;
            item.Selected = ContainsIndex(selectedIndexes, index);
            item.HasSelected = true;
            cache.Selections.Add(item);
            index = index + 1;
        }
        cache.RawXml = "";
        return result;
    }

    /// <summary>Sets slicer visual style and layout.</summary>
    /// <param name="part">The slicers part.</param>
    /// <param name="slicerName">The slicer name.</param>
    /// <param name="styleName">The slicer style name.</param>
    /// <param name="columnCount">The displayed column count.</param>
    /// <param name="showCaption">Whether the caption is shown.</param>
    /// <returns>The operation result.</returns>
    public XlsxSlicerTimelineOperationResult SetSlicerStyle(XlsxSlicersPart part, string slicerName, string styleName, uint columnCount, bool showCaption)
    {
        XlsxSlicerTimelineOperationResult result = new XlsxSlicerTimelineOperationResult();
        XlsxSlicerDefinition? slicer = FindSlicer(part, slicerName);
        if (slicer == null) result.AddError("slicerStyle.notFound", "Slicer definition was not found.");
        if (XlsxCellValueConverter.NormalizeText(styleName).Length == 0) result.AddError("slicerStyle.nameMissing", "Slicer style name must not be empty.");
        if (columnCount == 0) result.AddError("slicerStyle.columnsMissing", "Slicer column count must be greater than zero.");
        if (!result.IsValid || slicer == null) return result;
        slicer.StyleName = XlsxCellValueConverter.NormalizeText(styleName);
        slicer.ColumnCount = columnCount;
        slicer.HasColumnCount = true;
        slicer.ShowCaption = showCaption;
        slicer.HasShowCaption = true;
        part.RawXml = "";
        return result;
    }

    /// <summary>Sets timeline filtering and selection state.</summary>
    /// <param name="cache">The timeline cache.</param>
    /// <param name="state">The timeline state.</param>
    /// <returns>The operation result.</returns>
    public XlsxSlicerTimelineOperationResult SetTimelineState(XlsxSlicerTimelinePart cache, XlsxTimelineState? state)
    {
        XlsxSlicerTimelineOperationResult result = new XlsxSlicerTimelineOperationResult();
        if (cache == null) result.AddError("timelineState.cacheNull", "Timeline state requires a cache.");
        if (state == null) result.AddError("timelineState.stateNull", "Timeline state must not be null.");
        if (state != null && state.HasSelection && state.EndDate < state.StartDate) result.AddError("timelineState.rangeInvalid", "Timeline selection end must not precede its start.");
        if (!result.IsValid || cache == null || state == null) return result;
        cache.State = CloneTimelineState(state);
        cache.HasState = true;
        cache.RawXml = "";
        return result;
    }

    internal static XlsxTimelineState CloneTimelineState(XlsxTimelineState source)
    {
        XlsxTimelineState clone = new XlsxTimelineState();
        clone.PivotCacheId = source.PivotCacheId;
        clone.HasPivotCacheId = source.HasPivotCacheId;
        clone.FilterId = source.FilterId;
        clone.HasFilterId = source.HasFilterId;
        clone.FilterType = XlsxCellValueConverter.NormalizeText(source.FilterType);
        clone.SingleRange = source.SingleRange;
        clone.HasSingleRange = source.HasSingleRange;
        clone.StartDate = source.StartDate;
        clone.EndDate = source.EndDate;
        clone.HasSelection = source.HasSelection;
        clone.Level = source.Level;
        clone.HasLevel = source.HasLevel;
        return clone;
    }

    /// <summary>Sets timeline visual style and levels.</summary>
    /// <param name="part">The timelines part.</param>
    /// <param name="timelineName">The timeline name.</param>
    /// <param name="styleName">The timeline style name.</param>
    /// <param name="level">The displayed level.</param>
    /// <param name="selectionLevel">The selection level.</param>
    /// <returns>The operation result.</returns>
    public XlsxSlicerTimelineOperationResult SetTimelineStyle(XlsxSlicerTimelinePart part, string timelineName, string styleName, uint level, uint selectionLevel)
    {
        XlsxSlicerTimelineOperationResult result = new XlsxSlicerTimelineOperationResult();
        XlsxSlicerTimelineItem? item = FindTimeline(part, timelineName);
        if (item == null) result.AddError("timelineStyle.notFound", "Timeline definition was not found.");
        if (XlsxCellValueConverter.NormalizeText(styleName).Length == 0) result.AddError("timelineStyle.nameMissing", "Timeline style name must not be empty.");
        if (!result.IsValid || item == null) return result;
        item.StyleName = XlsxCellValueConverter.NormalizeText(styleName);
        item.Level = level;
        item.HasLevel = true;
        item.SelectionLevel = selectionLevel;
        item.HasSelectionLevel = true;
        part.RawXml = "";
        return result;
    }

    /// <summary>Adds a package relationship used by a slicer or timeline.</summary>
    /// <param name="relationships">The relationship part to update.</param>
    /// <param name="id">The relationship identifier.</param>
    /// <param name="type">The relationship type.</param>
    /// <param name="target">The relationship target.</param>
    /// <param name="targetPath">The resolved target path.</param>
    /// <returns>The operation result.</returns>
    public XlsxSlicerTimelineOperationResult AddRelationship(XlsxRelationshipsPart relationships, string id, string type, string target, string targetPath)
    {
        XlsxSlicerTimelineOperationResult result = new XlsxSlicerTimelineOperationResult();
        string normalizedId = XlsxCellValueConverter.NormalizeText(id);
        if (relationships == null || relationships.Relationships == null) result.AddError("slicerRelationship.storageMissing", "Relationship storage must be supplied.");
        if (normalizedId.Length == 0) result.AddError("slicerRelationship.idMissing", "Relationship identifier must not be empty.");
        if (!IsSupportedRelationshipType(type)) result.AddError("slicerRelationship.typeInvalid", "Relationship type must be a slicer or timeline relationship type.");
        if (XlsxCellValueConverter.NormalizeText(target).Length == 0) result.AddError("slicerRelationship.targetMissing", "Relationship target must not be empty.");
        if (!result.IsValid || relationships == null || relationships.Relationships == null) return result;
        foreach (XlsxRelationship existing in relationships.Relationships)
        {
            if (existing.Id == normalizedId)
            {
                result.AddError("slicerRelationship.idDuplicate", "Relationship identifier must be unique.");
                return result;
            }
        }
        XlsxRelationship relationship = new XlsxRelationship();
        relationship.Id = normalizedId;
        relationship.Type = type;
        relationship.Target = XlsxCellValueConverter.NormalizeText(target);
        relationship.TargetPath = XlsxCellValueConverter.NormalizeText(targetPath);
        relationships.Relationships.Add(relationship);
        result.Relationships = relationships;
        return result;
    }

    /// <summary>Removes a slicer or timeline relationship.</summary>
    /// <param name="relationships">The relationship part to update.</param>
    /// <param name="id">The relationship identifier.</param>
    /// <returns>The operation result.</returns>
    public XlsxSlicerTimelineOperationResult RemoveRelationship(XlsxRelationshipsPart relationships, string id)
    {
        XlsxSlicerTimelineOperationResult result = new XlsxSlicerTimelineOperationResult();
        if (relationships == null || relationships.Relationships == null)
        {
            result.AddError("slicerRelationship.storageMissing", "Relationship storage must be supplied.");
            return result;
        }
        int index = 0;
        while (index < relationships.Relationships.Count && relationships.Relationships[index].Id != id)
        {
            index = index + 1;
        }
        if (index >= relationships.Relationships.Count)
        {
            result.AddError("slicerRelationship.notFound", "Relationship was not found.");
            return result;
        }
        relationships.Relationships.RemoveAt(index);
        result.Relationships = relationships;
        return result;
    }

    /// <summary>Sets preserved extension XML on a slicer cache.</summary>
    /// <param name="cache">The slicer cache.</param>
    /// <param name="extensionXml">The extension-list XML.</param>
    /// <returns>The operation result.</returns>
    public XlsxSlicerTimelineOperationResult PreserveSlicerExtension(XlsxSlicerCachePart cache, string extensionXml)
    {
        XlsxSlicerTimelineOperationResult result = new XlsxSlicerTimelineOperationResult();
        if (cache == null) result.AddError("slicerExtension.cacheNull", "Extension preservation requires a slicer cache.");
        if (!result.IsValid || cache == null) return result;
        cache.ExtensionListRawXml = XlsxCellValueConverter.NormalizeText(extensionXml);
        cache.RawXml = "";
        return result;
    }

    /// <summary>Sets preserved extension XML on a timeline part.</summary>
    /// <param name="part">The timeline part.</param>
    /// <param name="extensionXml">The extension-list XML.</param>
    /// <returns>The operation result.</returns>
    public XlsxSlicerTimelineOperationResult PreserveTimelineExtension(XlsxSlicerTimelinePart part, string extensionXml)
    {
        XlsxSlicerTimelineOperationResult result = new XlsxSlicerTimelineOperationResult();
        if (part == null) result.AddError("timelineExtension.partNull", "Extension preservation requires a timeline part.");
        if (!result.IsValid || part == null) return result;
        part.ExtensionListRawXml = XlsxCellValueConverter.NormalizeText(extensionXml);
        part.RawXml = "";
        return result;
    }

    internal static XlsxSlicerTimelineOperationResult ValidateIdentity(string name, string sourceName)
    {
        XlsxSlicerTimelineOperationResult result = new XlsxSlicerTimelineOperationResult();
        if (XlsxCellValueConverter.NormalizeText(name).Length == 0) result.AddError("slicerAuthoring.nameMissing", "Slicer or timeline name must not be empty.");
        if (XlsxCellValueConverter.NormalizeText(sourceName).Length == 0) result.AddError("slicerAuthoring.sourceMissing", "Slicer or timeline source name must not be empty.");
        return result;
    }

    internal static bool ContainsIndex(List<int> indexes, int value)
    {
        foreach (int index in indexes)
        {
            if (index == value) return true;
        }
        return false;
    }

    internal static XlsxSlicerDefinition? FindSlicer(XlsxSlicersPart part, string name)
    {
        if (part == null || part.Slicers == null) return null;
        string normalized = XlsxCellValueConverter.NormalizeText(name);
        foreach (XlsxSlicerDefinition slicer in part.Slicers)
        {
            if (slicer != null && slicer.Name == normalized) return slicer;
        }
        return null;
    }

    internal static XlsxSlicerTimelineItem? FindTimeline(XlsxSlicerTimelinePart part, string name)
    {
        if (part == null || part.Items == null) return null;
        string normalized = XlsxCellValueConverter.NormalizeText(name);
        foreach (XlsxSlicerTimelineItem item in part.Items)
        {
            if (item != null && item.Name == normalized) return item;
        }
        return null;
    }

    internal static bool IsSupportedRelationshipType(string type)
    {
        return type == XlsxRelationshipTypes.Slicer || type == XlsxRelationshipTypes.SlicerCache || type == XlsxRelationshipTypes.Timeline || type == XlsxRelationshipTypes.TimelineCache;
    }
}
