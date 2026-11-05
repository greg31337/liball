using System;
using System.Collections.Generic;

namespace xlsx;

/// <summary>
/// Defines slicer and timeline creation, binding, selection, style, and relationship operations.
/// </summary>
public interface IXlsxSlicerTimelineService
{
    /// <summary>Creates a table-bound slicer cache and slicers part.</summary>
    /// <param name="cachePath">The slicer cache part path.</param>
    /// <param name="slicersPath">The slicers part path.</param>
    /// <param name="name">The slicer name.</param>
    /// <param name="sourceName">The source field name.</param>
    /// <param name="tableId">The source table identifier.</param>
    /// <param name="columnName">The source column name.</param>
    /// <param name="caption">The displayed caption.</param>
    /// <returns>The authoring result.</returns>
    XlsxSlicerTimelineOperationResult CreateTableSlicer(string cachePath, string slicersPath, string name, string sourceName, uint tableId, string columnName, string caption);
    /// <summary>Creates a pivot-bound slicer cache and slicers part.</summary>
    /// <param name="cachePath">The slicer cache part path.</param>
    /// <param name="slicersPath">The slicers part path.</param>
    /// <param name="name">The slicer name.</param>
    /// <param name="sourceName">The source field name.</param>
    /// <param name="pivotTableName">The source pivot-table name.</param>
    /// <param name="tabId">The source worksheet identifier.</param>
    /// <param name="caption">The displayed caption.</param>
    /// <returns>The authoring result.</returns>
    XlsxSlicerTimelineOperationResult CreatePivotSlicer(string cachePath, string slicersPath, string name, string sourceName, string pivotTableName, uint tabId, string caption);
    /// <summary>Creates a timeline cache and timelines part.</summary>
    /// <param name="cachePath">The timeline cache part path.</param>
    /// <param name="timelinesPath">The timelines part path.</param>
    /// <param name="name">The timeline name.</param>
    /// <param name="sourceName">The source date field name.</param>
    /// <param name="pivotTableName">The source pivot-table name.</param>
    /// <param name="tabId">The source worksheet identifier.</param>
    /// <param name="caption">The displayed caption.</param>
    /// <returns>The authoring result.</returns>
    XlsxSlicerTimelineOperationResult CreateTimeline(string cachePath, string timelinesPath, string name, string sourceName, string pivotTableName, uint tabId, string caption);
    /// <summary>Rebinds a slicer cache to a table column.</summary>
    /// <param name="cache">The slicer cache.</param>
    /// <param name="tableId">The table identifier.</param>
    /// <param name="columnName">The table column name.</param>
    /// <returns>The operation result.</returns>
    XlsxSlicerTimelineOperationResult BindTable(XlsxSlicerCachePart cache, uint tableId, string columnName);
    /// <summary>Adds a pivot-table binding to a slicer cache.</summary>
    /// <param name="cache">The slicer cache.</param>
    /// <param name="pivotTableName">The pivot-table name.</param>
    /// <param name="tabId">The worksheet identifier.</param>
    /// <returns>The operation result.</returns>
    XlsxSlicerTimelineOperationResult BindPivot(XlsxSlicerCachePart cache, string pivotTableName, uint tabId);
    /// <summary>Sets selected slicer item indexes.</summary>
    /// <param name="cache">The slicer cache.</param>
    /// <param name="itemCount">The number of source items.</param>
    /// <param name="selectedIndexes">The selected zero-based item indexes.</param>
    /// <returns>The operation result.</returns>
    XlsxSlicerTimelineOperationResult SetSelections(XlsxSlicerCachePart cache, int itemCount, List<int>? selectedIndexes);
    /// <summary>Sets slicer visual style and layout.</summary>
    /// <param name="part">The slicers part.</param>
    /// <param name="slicerName">The slicer name.</param>
    /// <param name="styleName">The slicer style name.</param>
    /// <param name="columnCount">The displayed column count.</param>
    /// <param name="showCaption">Whether the caption is shown.</param>
    /// <returns>The operation result.</returns>
    XlsxSlicerTimelineOperationResult SetSlicerStyle(XlsxSlicersPart part, string slicerName, string styleName, uint columnCount, bool showCaption);
    /// <summary>Sets timeline filtering and selection state.</summary>
    /// <param name="cache">The timeline cache.</param>
    /// <param name="state">The timeline state.</param>
    /// <returns>The operation result.</returns>
    XlsxSlicerTimelineOperationResult SetTimelineState(XlsxSlicerTimelinePart cache, XlsxTimelineState? state);
    /// <summary>Sets timeline visual style and levels.</summary>
    /// <param name="part">The timelines part.</param>
    /// <param name="timelineName">The timeline name.</param>
    /// <param name="styleName">The timeline style name.</param>
    /// <param name="level">The displayed level.</param>
    /// <param name="selectionLevel">The selection level.</param>
    /// <returns>The operation result.</returns>
    XlsxSlicerTimelineOperationResult SetTimelineStyle(XlsxSlicerTimelinePart part, string timelineName, string styleName, uint level, uint selectionLevel);
    /// <summary>Adds a package relationship used by a slicer or timeline.</summary>
    /// <param name="relationships">The relationship part to update.</param>
    /// <param name="id">The relationship identifier.</param>
    /// <param name="type">The relationship type.</param>
    /// <param name="target">The relationship target.</param>
    /// <param name="targetPath">The resolved target path.</param>
    /// <returns>The operation result.</returns>
    XlsxSlicerTimelineOperationResult AddRelationship(XlsxRelationshipsPart relationships, string id, string type, string target, string targetPath);
    /// <summary>Removes a slicer or timeline relationship.</summary>
    /// <param name="relationships">The relationship part to update.</param>
    /// <param name="id">The relationship identifier.</param>
    /// <returns>The operation result.</returns>
    XlsxSlicerTimelineOperationResult RemoveRelationship(XlsxRelationshipsPart relationships, string id);
    /// <summary>Sets preserved extension XML on a slicer cache.</summary>
    /// <param name="cache">The slicer cache.</param>
    /// <param name="extensionXml">The extension-list XML.</param>
    /// <returns>The operation result.</returns>
    XlsxSlicerTimelineOperationResult PreserveSlicerExtension(XlsxSlicerCachePart cache, string extensionXml);
    /// <summary>Sets preserved extension XML on a timeline part.</summary>
    /// <param name="part">The timeline part.</param>
    /// <param name="extensionXml">The extension-list XML.</param>
    /// <returns>The operation result.</returns>
    XlsxSlicerTimelineOperationResult PreserveTimelineExtension(XlsxSlicerTimelinePart part, string extensionXml);
}
