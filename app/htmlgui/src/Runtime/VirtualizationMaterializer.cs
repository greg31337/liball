using System;
using System.Collections.Generic;

namespace htmlgui;

internal class VirtualAccessiblePosition
{
    internal string Key = string.Empty;
    internal int LogicalIndex;
    internal int PositionInSet;
    internal bool HasSetSize;
    internal int SetSize;
    internal int RowIndex;
    internal int ColumnIndex;
    internal string? ParentKey;
    internal int Depth;
}

internal class VirtualListMaterialization
{
    internal UiCollectionItem[] Items = Array.Empty<UiCollectionItem>();
    internal UiPageRange Range = new UiPageRange();
    internal VirtualAccessiblePosition[] Positions = Array.Empty<VirtualAccessiblePosition>();
}

internal class VirtualGridMaterialization
{
    internal UiTableRow[] Rows = Array.Empty<UiTableRow>();
    internal UiTableColumn[] Columns = Array.Empty<UiTableColumn>();
    internal VirtualAccessiblePosition[] RowPositions = Array.Empty<VirtualAccessiblePosition>();
    internal VirtualAccessiblePosition[] ColumnPositions = Array.Empty<VirtualAccessiblePosition>();
    internal bool HasRowCount;
    internal int RowCount;
    internal bool HasColumnCount;
    internal int ColumnCount;
}

internal class VirtualTreeMaterialization
{
    internal UiTreeItem[] Items = Array.Empty<UiTreeItem>();
    internal UiTreeRange Range = new UiTreeRange();
    internal VirtualAccessiblePosition[] Positions = Array.Empty<VirtualAccessiblePosition>();
}

internal class VirtualizationMaterializer
{
    internal VirtualListMaterialization MaterializeListRange(UiCollectionItem[]? items, int start, bool hasTotalCount, int totalCount, bool isEnd)
    {
        UiCollectionItem[] values = items ?? Array.Empty<UiCollectionItem>();
        ValidateMaterializedRange(start, values.Length, hasTotalCount, totalCount, "items");
        ValidateAccessibleIndexRange(start, values.Length, 1, "items");
        ValidateCollectionKeys(values);
        UiCollectionItem[] copied = CopyCollectionItems(values);
        VirtualAccessiblePosition[] positions = new VirtualAccessiblePosition[copied.Length];
        int index = 0;
        while (index < copied.Length)
        {
            positions[index] = CreateListPosition(copied[index].Key, start + index, hasTotalCount, totalCount);
            index++;
        }

        bool reachedEnd = isEnd;
        if (hasTotalCount && (long)start + copied.Length >= totalCount)
        {
            reachedEnd = true;
        }

        return new VirtualListMaterialization
        {
            Items = copied,
            Range = new UiPageRange
            {
                Start = start,
                Count = copied.Length,
                HasTotalCount = hasTotalCount,
                TotalCount = hasTotalCount ? totalCount : 0,
                IsEnd = reachedEnd
            },
            Positions = positions
        };
    }

    internal VirtualGridMaterialization MaterializeGridRange(UiTableRow[]? rows, int rowStart, bool hasRowTotal, int rowTotal, UiTableColumn[]? columns, int columnStart, bool hasColumnTotal, int columnTotal)
    {
        UiTableRow[] rowValues = rows ?? Array.Empty<UiTableRow>();
        UiTableColumn[] columnValues = columns ?? Array.Empty<UiTableColumn>();
        ValidateMaterializedRange(rowStart, rowValues.Length, hasRowTotal, rowTotal, "rows");
        ValidateMaterializedRange(columnStart, columnValues.Length, hasColumnTotal, columnTotal, "columns");
        ValidateAccessibleIndexRange(rowStart, rowValues.Length, 2, "rows");
        ValidateAccessibleIndexRange(columnStart, columnValues.Length, 1, "columns");
        if (hasRowTotal && rowTotal == int.MaxValue)
        {
            throw new ArgumentOutOfRangeException("rowTotal", "The accessible row count must include the header row.");
        }

        ValidateGridRowKeys(rowValues);
        ValidateGridColumnKeys(columnValues);
        UiTableRow[] copiedRows = CopyGridRows(rowValues);
        UiTableColumn[] copiedColumns = CopyGridColumns(columnValues);
        VirtualAccessiblePosition[] rowPositions = new VirtualAccessiblePosition[copiedRows.Length];
        int rowIndex = 0;
        while (rowIndex < copiedRows.Length)
        {
            int logicalIndex = rowStart + rowIndex;
            rowPositions[rowIndex] = new VirtualAccessiblePosition
            {
                Key = copiedRows[rowIndex].Key,
                LogicalIndex = logicalIndex,
                PositionInSet = logicalIndex + 1,
                HasSetSize = hasRowTotal,
                SetSize = hasRowTotal ? rowTotal : 0,
                RowIndex = logicalIndex + 2
            };
            rowIndex++;
        }

        VirtualAccessiblePosition[] columnPositions = new VirtualAccessiblePosition[copiedColumns.Length];
        int columnIndex = 0;
        while (columnIndex < copiedColumns.Length)
        {
            int logicalIndex = columnStart + columnIndex;
            columnPositions[columnIndex] = new VirtualAccessiblePosition
            {
                Key = copiedColumns[columnIndex].Key,
                LogicalIndex = logicalIndex,
                PositionInSet = logicalIndex + 1,
                HasSetSize = hasColumnTotal,
                SetSize = hasColumnTotal ? columnTotal : 0,
                ColumnIndex = logicalIndex + 1
            };
            columnIndex++;
        }

        return new VirtualGridMaterialization
        {
            Rows = copiedRows,
            Columns = copiedColumns,
            RowPositions = rowPositions,
            ColumnPositions = columnPositions,
            HasRowCount = hasRowTotal,
            RowCount = hasRowTotal ? rowTotal + 1 : -1,
            HasColumnCount = hasColumnTotal,
            ColumnCount = hasColumnTotal ? columnTotal : -1
        };
    }

    internal VirtualTreeMaterialization MaterializeTreeRange(UiTreeItem[]? roots, int start, int count, bool hasTotalCount, int totalCount, string? anchorKey)
    {
        if (start < 0 || count < 0)
        {
            throw new ArgumentOutOfRangeException("range", "Flattened tree starts and counts cannot be negative.");
        }

        UiTreeItem[] rootValues = roots ?? Array.Empty<UiTreeItem>();
        List<UiTreeItem> flattened = new List<UiTreeItem>();
        List<VirtualAccessiblePosition> allPositions = new List<VirtualAccessiblePosition>();
        HashSet<string> keys = new HashSet<string>(StringComparer.Ordinal);
        HashSet<UiTreeItem> ancestors = new HashSet<UiTreeItem>();
        AddVisibleTreeItems(rootValues, null, 1, flattened, allPositions, keys, ancestors);
        int availableCount = flattened.Count;
        int materializedCount = 0;
        if (start < availableCount)
        {
            materializedCount = Math.Min(count, availableCount - start);
        }

        ValidateMaterializedRange(start, materializedCount, hasTotalCount, totalCount, "range");
        ValidateAccessibleIndexRange(start, materializedCount, 1, "range");
        string? normalizedAnchor = string.IsNullOrWhiteSpace(anchorKey) ? null : anchorKey;
        int anchorIndex = normalizedAnchor == null ? -1 : FindTreeKey(flattened, normalizedAnchor);
        if (normalizedAnchor != null && (anchorIndex < start || anchorIndex >= start + materializedCount))
        {
            throw new ArgumentException("The tree anchor must identify a materialized visible item.", "anchorKey");
        }

        UiTreeItem[] items = new UiTreeItem[materializedCount];
        VirtualAccessiblePosition[] positions = new VirtualAccessiblePosition[materializedCount];
        int index = 0;
        while (index < materializedCount)
        {
            items[index] = flattened[start + index];
            VirtualAccessiblePosition source = allPositions[start + index];
            source.LogicalIndex = start + index;
            source.RowIndex = start + index + 1;
            positions[index] = source;
            index++;
        }

        return new VirtualTreeMaterialization
        {
            Items = items,
            Range = new UiTreeRange
            {
                Start = start,
                Count = materializedCount,
                HasTotalCount = hasTotalCount,
                TotalCount = hasTotalCount ? totalCount : 0,
                AnchorKey = normalizedAnchor
            },
            Positions = positions
        };
    }

    internal void AddVisibleTreeItems(UiTreeItem[] items, string? parentKey, int depth, List<UiTreeItem> flattened, List<VirtualAccessiblePosition> positions, HashSet<string> keys, HashSet<UiTreeItem> ancestors)
    {
        int index = 0;
        while (index < items.Length)
        {
            UiTreeItem? item = items[index];
            if (item == null || string.IsNullOrWhiteSpace(item.Key) || item.Label == null || item.Value == null || item.Children == null)
            {
                throw new ArgumentException("Tree items require stable keys, labels, values, and child arrays.", "roots");
            }

            if (!keys.Add(item.Key))
            {
                throw new ArgumentException("Visible tree item keys must be unique.", "roots");
            }

            if (!ancestors.Add(item))
            {
                throw new ArgumentException("Visible tree item graphs cannot contain cycles.", "roots");
            }

            if (item.HasLazyChildren && item.Children.Length > 0)
            {
                throw new ArgumentException("Lazy tree children cannot also be materialized.", "roots");
            }

            UiTreeItem copy = CopyFlatTreeItem(item, parentKey, depth, index + 1, items.Length);
            flattened.Add(copy);
            positions.Add(new VirtualAccessiblePosition
            {
                Key = copy.Key,
                PositionInSet = index + 1,
                HasSetSize = true,
                SetSize = items.Length,
                ParentKey = parentKey,
                Depth = depth
            });
            if (item.IsExpanded)
            {
                AddVisibleTreeItems(item.Children, item.Key, depth + 1, flattened, positions, keys, ancestors);
            }

            ancestors.Remove(item);
            index++;
        }
    }

    internal UiTreeItem CopyFlatTreeItem(UiTreeItem source, string? parentKey, int depth, int positionInSet, int setSize)
    {
        object? valueCopy = new ModelNormalizer().CopyValue(source.Value);
        object? contentCopy = new ModelNormalizer().CopyValue(source.Content);
        return new UiTreeItem
        {
            Key = source.Key,
            Label = source.Label,
            Value = valueCopy == null ? new UiValue() : (UiValue)valueCopy,
            Content = contentCopy == null ? null : (UiNode)contentCopy,
            Children = Array.Empty<UiTreeItem>(),
            Relationship = new UiTreeRelationship
            {
                ParentKey = parentKey,
                Depth = depth,
                PositionInSet = positionInSet,
                HasSetSize = true,
                SetSize = setSize
            },
            IsExpanded = source.IsExpanded,
            HasLazyChildren = source.HasLazyChildren,
            IsSelected = source.IsSelected,
            IsDisabled = source.IsDisabled,
            HasCheckState = source.HasCheckState,
            CheckState = source.CheckState
        };
    }

    internal VirtualAccessiblePosition CreateListPosition(string key, int logicalIndex, bool hasTotalCount, int totalCount)
    {
        return new VirtualAccessiblePosition
        {
            Key = key,
            LogicalIndex = logicalIndex,
            PositionInSet = logicalIndex + 1,
            HasSetSize = hasTotalCount,
            SetSize = hasTotalCount ? totalCount : 0
        };
    }

    internal void ValidateMaterializedRange(int start, int count, bool hasTotalCount, int totalCount, string parameterName)
    {
        if (start < 0 || count < 0)
        {
            throw new ArgumentOutOfRangeException(parameterName, "Materialized range starts and counts cannot be negative.");
        }

        if (hasTotalCount && (totalCount < 0 || (long)start + count > totalCount))
        {
            throw new ArgumentOutOfRangeException(parameterName, "The materialized range must fit within the known total.");
        }
    }

    internal void ValidateAccessibleIndexRange(int start, int count, int indexOffset, string parameterName)
    {
        if (count > 0 && (long)start + count - 1 + indexOffset > int.MaxValue)
        {
            throw new ArgumentOutOfRangeException(parameterName, "The logical range cannot be represented by accessible one-based indices.");
        }
    }

    internal void ValidateCollectionKeys(UiCollectionItem[] items)
    {
        HashSet<string> keys = new HashSet<string>(StringComparer.Ordinal);
        int index = 0;
        while (index < items.Length)
        {
            UiCollectionItem? item = items[index];
            if (item == null || string.IsNullOrWhiteSpace(item.Key) || item.Value == null || !keys.Add(item.Key))
            {
                throw new ArgumentException("Materialized collection items require unique stable keys and values.", "items");
            }

            index++;
        }
    }

    internal void ValidateGridRowKeys(UiTableRow[] rows)
    {
        HashSet<string> keys = new HashSet<string>(StringComparer.Ordinal);
        int index = 0;
        while (index < rows.Length)
        {
            UiTableRow? row = rows[index];
            if (row == null || string.IsNullOrWhiteSpace(row.Key) || row.Cells == null || !keys.Add(row.Key))
            {
                throw new ArgumentException("Materialized grid rows require unique stable keys and cell arrays.", "rows");
            }

            index++;
        }
    }

    internal void ValidateGridColumnKeys(UiTableColumn[] columns)
    {
        HashSet<string> keys = new HashSet<string>(StringComparer.Ordinal);
        int index = 0;
        while (index < columns.Length)
        {
            UiTableColumn? column = columns[index];
            if (column == null || string.IsNullOrWhiteSpace(column.Key) || column.Header == null || !keys.Add(column.Key))
            {
                throw new ArgumentException("Materialized grid columns require unique stable keys and headers.", "columns");
            }

            index++;
        }
    }

    internal UiCollectionItem[] CopyCollectionItems(UiCollectionItem[] source)
    {
        object? copied = new ModelNormalizer().CopyValue(source);
        return copied == null ? Array.Empty<UiCollectionItem>() : (UiCollectionItem[])copied;
    }

    internal UiTableRow[] CopyGridRows(UiTableRow[] source)
    {
        object? copied = new ModelNormalizer().CopyValue(source);
        return copied == null ? Array.Empty<UiTableRow>() : (UiTableRow[])copied;
    }

    internal UiTableColumn[] CopyGridColumns(UiTableColumn[] source)
    {
        object? copied = new ModelNormalizer().CopyValue(source);
        return copied == null ? Array.Empty<UiTableColumn>() : (UiTableColumn[])copied;
    }

    internal int FindTreeKey(List<UiTreeItem> items, string key)
    {
        int index = 0;
        while (index < items.Count)
        {
            if (items[index].Key == key)
            {
                return index;
            }

            index++;
        }

        return -1;
    }
}
