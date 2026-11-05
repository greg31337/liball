using System;
using System.Collections.Generic;

namespace htmlgui;

internal class TreeFlatItem
{
    internal UiTreeItem Item = new UiTreeItem();
    internal string? ParentKey;
    internal int Depth;
    internal int PositionInSet;
    internal int SetSize;
    internal bool IsVisible;
}

internal class TreeTemplateExpander
{
    internal bool IsTreeControlName(string name)
    {
        switch (name)
        {
            case "tree":
            case "tree-view":
            case "treegrid":
            case "tree-grid":
                return true;
            default:
                return false;
        }
    }

    internal TreeFlatItem[] PrepareTree(UiNode node)
    {
        UiTree tree = RequireTree(node);
        if (tree.Items == null)
        {
            throw CreateRenderException(node, "Tree items cannot be null.");
        }

        List<TreeFlatItem> result = new List<TreeFlatItem>();
        HashSet<string> keys = new HashSet<string>(StringComparer.Ordinal);
        HashSet<UiTreeItem> ancestors = new HashSet<UiTreeItem>();
        AddItems(node, tree.Items, null, 1, true, result, keys, ancestors);
        TreeFlatItem[] flattened = result.ToArray();
        ValidateRange(node, tree, flattened);
        ValidateSelection(node, tree, flattened);
        ValidateTypeAhead(node, tree, flattened);
        return flattened;
    }

    internal DomNode ExpandTree(UiNode node, TreeFlatItem[] flattened, DomNode?[] contents)
    {
        UiTree tree = RequireTree(node);
        if (flattened.Length != contents.Length)
        {
            throw CreateRenderException(node, "Expanded tree content must align with flattened tree items.");
        }

        bool treeGrid = tree.IsTreeGrid || node.Name == "treegrid" || node.Name == "tree-grid";
        int visibleCount = CountVisibleItems(flattened);
        List<DomAttribute> attributes = CreateRootAttributes(node, tree, flattened, visibleCount, treeGrid);
        DomNode[] children = new DomNode[visibleCount];
        int sourceIndex = 0;
        int visibleIndex = 0;
        while (sourceIndex < flattened.Length)
        {
            if (flattened[sourceIndex].IsVisible)
            {
                children[visibleIndex] = CreateItemNode(node, tree, flattened[sourceIndex], contents[sourceIndex], visibleIndex, treeGrid);
                visibleIndex++;
            }

            sourceIndex++;
        }

        return new DomNode
        {
            Kind = DomNodeKind.Element,
            Key = GetNodeKey(node),
            Id = GetNodeId(node),
            Name = HtmlElements.Division,
            Attributes = attributes.ToArray(),
            Children = children,
            Version = GetNodeVersion(node)
        };
    }

    internal UiTree RequireTree(UiNode node)
    {
        UiTree? tree = node.Tree;
        if (tree == null)
        {
            throw CreateRenderException(node, "A tree control requires a tree description.");
        }

        if (string.IsNullOrWhiteSpace(tree.Key))
        {
            throw CreateRenderException(node, "Trees require a non-empty stable key.");
        }

        return tree;
    }

    internal void AddItems(UiNode node, UiTreeItem[] items, string? parentKey, int depth, bool visible, List<TreeFlatItem> result, HashSet<string> keys, HashSet<UiTreeItem> ancestors)
    {
        int index = 0;
        while (index < items.Length)
        {
            UiTreeItem? item = items[index];
            if (item == null)
            {
                throw CreateRenderException(node, "Tree items cannot be null.");
            }

            ValidateItem(node, item, items.Length, parentKey, depth, index, keys, ancestors);
            int position = item.Relationship.PositionInSet;
            if (position == 0)
            {
                position = index + 1;
            }

            int setSize = items.Length;
            if (item.Relationship.HasSetSize)
            {
                setSize = item.Relationship.SetSize;
            }

            result.Add(new TreeFlatItem
            {
                Item = item,
                ParentKey = parentKey,
                Depth = depth,
                PositionInSet = position,
                SetSize = setSize,
                IsVisible = visible
            });
            AddItems(node, item.Children, item.Key, depth + 1, visible && item.IsExpanded, result, keys, ancestors);
            ancestors.Remove(item);
            index++;
        }
    }

    internal void ValidateItem(UiNode node, UiTreeItem item, int siblingCount, string? parentKey, int depth, int index, HashSet<string> keys, HashSet<UiTreeItem> ancestors)
    {
        if (string.IsNullOrWhiteSpace(item.Key) || item.Label == null || item.Value == null || item.Children == null || item.Relationship == null)
        {
            throw CreateRenderException(node, "Tree items require keys, labels, values, children, and relationship metadata.");
        }

        if (!keys.Add(item.Key))
        {
            throw CreateRenderException(node, "Tree item keys must be unique.");
        }

        if (!ancestors.Add(item))
        {
            throw CreateRenderException(node, "Tree item graphs cannot contain cycles.");
        }

        if (item.Relationship.ParentKey != parentKey)
        {
            throw CreateRenderException(node, "Tree parent metadata must match the containing item.");
        }

        if (item.Relationship.Depth < 0 || (item.Relationship.Depth > 0 && item.Relationship.Depth != depth))
        {
            throw CreateRenderException(node, "Tree depth metadata must match the containing hierarchy.");
        }

        int position = item.Relationship.PositionInSet;
        if (position < 0)
        {
            throw CreateRenderException(node, "Tree item positions cannot be negative.");
        }

        if (position == 0)
        {
            position = index + 1;
        }

        int setSize = siblingCount;
        if (item.Relationship.HasSetSize)
        {
            setSize = item.Relationship.SetSize;
        }

        if (setSize < 1 || position > setSize)
        {
            throw CreateRenderException(node, "Tree set metadata must contain each item position.");
        }

        if (item.HasLazyChildren && item.Children.Length > 0)
        {
            throw CreateRenderException(node, "Lazy tree children cannot also be materialized.");
        }

        if (item.HasCheckState)
        {
            ValidateCheckState(item.CheckState);
        }

        ValueJsonCodec.ValidateValue(item.Value, 64, 10000, 1048576, 1048576);
    }

    internal void ValidateRange(UiNode node, UiTree tree, TreeFlatItem[] flattened)
    {
        UiTreeRange? range = tree.Range;
        if (range == null)
        {
            throw CreateRenderException(node, "Trees require flattened range state.");
        }

        int visibleCount = CountVisibleItems(flattened);
        if (range.Start < 0 || range.Count < 0)
        {
            throw CreateRenderException(node, "Tree range starts and counts cannot be negative.");
        }

        int count = range.Count;
        if (count == 0 && visibleCount > 0)
        {
            count = visibleCount;
        }

        if (count != visibleCount)
        {
            throw CreateRenderException(node, "The tree range count must match the visible materialized items.");
        }

        if (range.HasTotalCount && (range.TotalCount < 0 || (long)range.Start + count > range.TotalCount))
        {
            throw CreateRenderException(node, "The materialized tree range must fit within the total flattened count.");
        }

        if (range.AnchorKey != null && FindVisibleItemIndex(flattened, range.AnchorKey) < 0)
        {
            throw CreateRenderException(node, "The tree range anchor must identify a visible materialized item.");
        }
    }

    internal void ValidateSelection(UiNode node, UiTree tree, TreeFlatItem[] flattened)
    {
        UiSelection? selection = tree.Selection;
        if (selection == null || selection.SelectedKeys == null)
        {
            throw CreateRenderException(node, "Trees require complete selection state.");
        }

        ValidateSelectionMode(selection.Mode);
        int selectedCount = 0;
        int itemIndex = 0;
        while (itemIndex < flattened.Length)
        {
            if (flattened[itemIndex].Item.IsSelected || ContainsKey(selection.SelectedKeys, flattened[itemIndex].Item.Key))
            {
                selectedCount++;
            }

            itemIndex++;
        }

        int keyIndex = 0;
        while (keyIndex < selection.SelectedKeys.Length)
        {
            string? key = selection.SelectedKeys[keyIndex];
            if (string.IsNullOrWhiteSpace(key) || FindItemIndex(flattened, key) < 0)
            {
                throw CreateRenderException(node, "Selected tree keys must identify tree items.");
            }

            int earlierIndex = 0;
            while (earlierIndex < keyIndex)
            {
                if (selection.SelectedKeys[earlierIndex] == key)
                {
                    throw CreateRenderException(node, "Selected tree keys must be unique.");
                }

                earlierIndex++;
            }

            keyIndex++;
        }

        if (selection.Mode == UiSelectionMode.None && selectedCount > 0)
        {
            throw CreateRenderException(node, "Selection-disabled trees cannot contain selected items.");
        }

        if (selection.Mode == UiSelectionMode.Single && selectedCount > 1)
        {
            throw CreateRenderException(node, "Single-selection trees permit at most one selected item.");
        }

        if (selection.ActiveKey != null && FindVisibleItemIndex(flattened, selection.ActiveKey) < 0)
        {
            throw CreateRenderException(node, "The active tree key must identify a visible item.");
        }

        if (selection.AnchorKey != null && (selection.Mode != UiSelectionMode.Range || FindItemIndex(flattened, selection.AnchorKey) < 0))
        {
            throw CreateRenderException(node, "A tree selection anchor must identify an item in range-selection mode.");
        }
    }

    internal void ValidateTypeAhead(UiNode node, UiTree tree, TreeFlatItem[] flattened)
    {
        UiTypeAheadState? typeAhead = tree.TypeAhead;
        if (typeAhead == null || typeAhead.Buffer == null || typeAhead.Generation < 0)
        {
            throw CreateRenderException(node, "Trees require valid type-ahead state.");
        }

        if (typeAhead.MatchedKey != null && FindVisibleItemIndex(flattened, typeAhead.MatchedKey) < 0)
        {
            throw CreateRenderException(node, "The type-ahead match must identify a visible tree item.");
        }
    }

    internal List<DomAttribute> CreateRootAttributes(UiNode node, UiTree tree, TreeFlatItem[] flattened, int visibleCount, bool treeGrid)
    {
        List<DomAttribute> attributes = new List<DomAttribute>();
        int rangeCount = tree.Range.Count;
        if (rangeCount == 0 && visibleCount > 0)
        {
            rangeCount = visibleCount;
        }

        attributes.Add(new DomAttribute { Name = HtmlAttributes.Class, Value = treeGrid ? "htmlgui-tree htmlgui-tree-grid" : "htmlgui-tree" });
        attributes.Add(new DomAttribute { Name = HtmlAttributes.DataPrefix + "range-count", Value = InvariantFormatter.FormatInt32(rangeCount) });
        attributes.Add(new DomAttribute { Name = HtmlAttributes.DataPrefix + "range-start", Value = InvariantFormatter.FormatInt32(tree.Range.Start) });
        attributes.Add(new DomAttribute { Name = HtmlAttributes.DataPrefix + "selection-mode", Value = GetSelectionModeToken(tree.Selection.Mode) });
        attributes.Add(new DomAttribute { Name = HtmlAttributes.DataPrefix + "tree-key", Value = tree.Key });
        attributes.Add(new DomAttribute { Name = HtmlAttributes.DataPrefix + "typeahead-buffer", Value = tree.TypeAhead.Buffer });
        attributes.Add(new DomAttribute { Name = HtmlAttributes.DataPrefix + "typeahead-generation", Value = InvariantFormatter.FormatInt32(tree.TypeAhead.Generation) });
        attributes.Add(new DomAttribute { Name = HtmlAttributes.DataPrefix + "typeahead-ignore-case", Value = FormatBoolean(tree.TypeAhead.IgnoreCase) });
        attributes.Add(new DomAttribute { Name = HtmlAttributes.Role, Value = treeGrid ? AriaRoles.TreeGrid : AriaRoles.Tree });
        if (tree.Range.HasTotalCount)
        {
            attributes.Add(new DomAttribute { Name = HtmlAttributes.DataPrefix + "total-count", Value = InvariantFormatter.FormatInt32(tree.Range.TotalCount) });
        }

        if (tree.Range.AnchorKey != null)
        {
            attributes.Add(new DomAttribute { Name = HtmlAttributes.DataPrefix + "range-anchor", Value = tree.Range.AnchorKey });
        }

        if (tree.TypeAhead.MatchedKey != null)
        {
            attributes.Add(new DomAttribute { Name = HtmlAttributes.DataPrefix + "typeahead-match", Value = tree.TypeAhead.MatchedKey });
            attributes.Add(new DomAttribute { Name = HtmlAttributes.DataPrefix + "typeahead-match-id", Value = CreateSyntheticId(node, "item", tree.TypeAhead.MatchedKey) });
        }

        if (tree.CanReorder)
        {
            attributes.Add(new DomAttribute { Name = HtmlAttributes.DataPrefix + "reorder", Value = "true" });
        }

        if (tree.Selection.Mode == UiSelectionMode.Multiple || tree.Selection.Mode == UiSelectionMode.Range)
        {
            attributes.Add(new DomAttribute { Name = AriaAttributes.MultiSelectable, Value = "true" });
        }

        if (tree.Selection.ActiveKey != null)
        {
            attributes.Add(new DomAttribute { Name = AriaAttributes.ActiveDescendant, Value = CreateSyntheticId(node, "item", tree.Selection.ActiveKey) });
        }

        if (treeGrid)
        {
            attributes.Add(new DomAttribute { Name = AriaAttributes.ColumnCount, Value = "1" });
            string rowCount = "-1";
            if (tree.Range.HasTotalCount)
            {
                rowCount = InvariantFormatter.FormatInt32(tree.Range.TotalCount);
            }

            attributes.Add(new DomAttribute { Name = AriaAttributes.RowCount, Value = rowCount });
        }

        string tabIndex = "0";
        if (node.State != null && node.State.HasTabIndex)
        {
            tabIndex = InvariantFormatter.FormatInt32(node.State.TabIndex);
        }
        else if (node.State != null && !node.State.IsEnabled)
        {
            tabIndex = "-1";
        }

        attributes.Add(new DomAttribute { Name = HtmlAttributes.TabIndex, Value = tabIndex });
        if (node.State != null && node.State.IsBusy)
        {
            attributes.Add(new DomAttribute { Name = AriaAttributes.Busy, Value = "true" });
        }

        if (node.State != null && !node.State.IsEnabled)
        {
            attributes.Add(new DomAttribute { Name = AriaAttributes.Disabled, Value = "true" });
        }

        if (node.State != null && !node.State.IsVisible)
        {
            attributes.Add(new DomAttribute { Name = HtmlAttributes.Hidden, Value = string.Empty, IsBoolean = true });
        }

        SortAttributes(attributes);
        return attributes;
    }

    internal DomNode CreateItemNode(UiNode node, UiTree tree, TreeFlatItem flattened, DomNode? content, int visibleIndex, bool treeGrid)
    {
        UiTreeItem item = flattened.Item;
        List<DomAttribute> attributes = new List<DomAttribute>();
        bool selected = item.IsSelected || ContainsKey(tree.Selection.SelectedKeys, item.Key);
        bool expandable = item.Children.Length > 0 || item.HasLazyChildren;
        attributes.Add(new DomAttribute { Name = HtmlAttributes.Class, Value = treeGrid ? "htmlgui-tree-row" : "htmlgui-tree-item" });
        attributes.Add(new DomAttribute { Name = HtmlAttributes.DataPrefix + "flat-index", Value = InvariantFormatter.FormatInt32(tree.Range.Start + visibleIndex) });
        attributes.Add(new DomAttribute { Name = HtmlAttributes.DataPrefix + "item-key", Value = item.Key });
        attributes.Add(new DomAttribute { Name = AriaAttributes.Level, Value = InvariantFormatter.FormatInt32(flattened.Depth) });
        attributes.Add(new DomAttribute { Name = AriaAttributes.PositionInSet, Value = InvariantFormatter.FormatInt32(flattened.PositionInSet) });
        attributes.Add(new DomAttribute { Name = AriaAttributes.Selected, Value = FormatBoolean(selected) });
        attributes.Add(new DomAttribute { Name = AriaAttributes.SetSize, Value = InvariantFormatter.FormatInt32(flattened.SetSize) });
        attributes.Add(new DomAttribute { Name = HtmlAttributes.Role, Value = treeGrid ? AriaRoles.Row : AriaRoles.TreeItem });
        attributes.Add(new DomAttribute { Name = HtmlAttributes.TabIndex, Value = "-1" });
        if (flattened.ParentKey != null)
        {
            attributes.Add(new DomAttribute { Name = HtmlAttributes.DataPrefix + "parent-key", Value = flattened.ParentKey });
        }

        if (expandable)
        {
            attributes.Add(new DomAttribute { Name = AriaAttributes.Expanded, Value = FormatBoolean(item.IsExpanded) });
        }

        if (item.HasLazyChildren)
        {
            attributes.Add(new DomAttribute { Name = HtmlAttributes.DataPrefix + "lazy-children", Value = "true" });
            if (item.IsExpanded)
            {
                attributes.Add(new DomAttribute { Name = AriaAttributes.Busy, Value = "true" });
            }
        }

        if (item.IsDisabled || (node.State != null && !node.State.IsEnabled))
        {
            attributes.Add(new DomAttribute { Name = AriaAttributes.Disabled, Value = "true" });
        }

        if (item.HasCheckState)
        {
            attributes.Add(new DomAttribute { Name = AriaAttributes.Checked, Value = FormatCheckState(item.CheckState) });
        }

        if (tree.CanReorder)
        {
            attributes.Add(new DomAttribute { Name = HtmlAttributes.DataPrefix + "reorderable", Value = "true" });
            attributes.Add(new DomAttribute { Name = "draggable", Value = "true" });
        }

        if (tree.Selection.ActiveKey == item.Key)
        {
            attributes.Add(new DomAttribute { Name = HtmlAttributes.DataPrefix + "active", Value = "true" });
        }

        if (treeGrid)
        {
            attributes.Add(new DomAttribute { Name = AriaAttributes.RowIndex, Value = InvariantFormatter.FormatInt32(tree.Range.Start + visibleIndex + 1) });
        }

        SortAttributes(attributes);
        DomNode[] valueContent = CreateItemContent(node, item, content);
        DomNode[] children = valueContent;
        if (treeGrid)
        {
            List<DomAttribute> cellAttributes = new List<DomAttribute>();
            cellAttributes.Add(new DomAttribute { Name = HtmlAttributes.Class, Value = "htmlgui-tree-grid-cell" });
            cellAttributes.Add(new DomAttribute { Name = AriaAttributes.ColumnIndex, Value = "1" });
            cellAttributes.Add(new DomAttribute { Name = HtmlAttributes.Role, Value = AriaRoles.GridCell });
            SortAttributes(cellAttributes);
            children = new DomNode[]
            {
                new DomNode
                {
                    Kind = DomNodeKind.Element,
                    Key = "$cell",
                    Id = CreateSyntheticId(node, "cell", item.Key),
                    Name = HtmlElements.Division,
                    Attributes = cellAttributes.ToArray(),
                    Children = valueContent,
                    Version = GetNodeVersion(node)
                }
            };
        }

        return new DomNode
        {
            Kind = DomNodeKind.Element,
            Key = item.Key,
            Id = CreateSyntheticId(node, "item", item.Key),
            Name = HtmlElements.Division,
            Attributes = attributes.ToArray(),
            Children = children,
            Version = GetNodeVersion(node)
        };
    }

    internal DomNode[] CreateItemContent(UiNode node, UiTreeItem item, DomNode? content)
    {
        if (content != null)
        {
            return new DomNode[] { content };
        }

        string text = item.Label;
        if (text.Length == 0)
        {
            text = new DocumentComposer().FormatMetadataValue(item.Value);
        }

        if (text.Length == 0)
        {
            return Array.Empty<DomNode>();
        }

        return new DomNode[]
        {
            new DomNode
            {
                Kind = DomNodeKind.Text,
                Key = "$label",
                Id = CreateSyntheticId(node, "label", item.Key),
                Text = text,
                Version = GetNodeVersion(node)
            }
        };
    }

    internal int CountVisibleItems(TreeFlatItem[] flattened)
    {
        int count = 0;
        int index = 0;
        while (index < flattened.Length)
        {
            if (flattened[index].IsVisible)
            {
                count++;
            }

            index++;
        }

        return count;
    }

    internal int FindItemIndex(TreeFlatItem[] flattened, string key)
    {
        int index = 0;
        while (index < flattened.Length)
        {
            if (flattened[index].Item.Key == key)
            {
                return index;
            }

            index++;
        }

        return -1;
    }

    internal int FindVisibleItemIndex(TreeFlatItem[] flattened, string key)
    {
        int index = 0;
        while (index < flattened.Length)
        {
            if (flattened[index].IsVisible && flattened[index].Item.Key == key)
            {
                return index;
            }

            index++;
        }

        return -1;
    }

    internal bool ContainsKey(string[] keys, string key)
    {
        int index = 0;
        while (index < keys.Length)
        {
            if (keys[index] == key)
            {
                return true;
            }

            index++;
        }

        return false;
    }

    internal void ValidateSelectionMode(UiSelectionMode mode)
    {
        switch (mode)
        {
            case UiSelectionMode.None:
            case UiSelectionMode.Single:
            case UiSelectionMode.Multiple:
            case UiSelectionMode.Range:
                return;
            default:
                throw new ArgumentOutOfRangeException("selectionMode", mode, "The selection mode is not supported.");
        }
    }

    internal void ValidateCheckState(UiCommandCheckState state)
    {
        switch (state)
        {
            case UiCommandCheckState.Unchecked:
            case UiCommandCheckState.Checked:
            case UiCommandCheckState.Mixed:
                return;
            default:
                throw new ArgumentOutOfRangeException("checkState", state, "The tree check state is not supported.");
        }
    }

    internal string GetSelectionModeToken(UiSelectionMode mode)
    {
        ValidateSelectionMode(mode);
        switch (mode)
        {
            case UiSelectionMode.Single:
                return "single";
            case UiSelectionMode.Multiple:
                return "multiple";
            case UiSelectionMode.Range:
                return "range";
            default:
                return "none";
        }
    }

    internal string FormatCheckState(UiCommandCheckState state)
    {
        ValidateCheckState(state);
        if (state == UiCommandCheckState.Checked)
        {
            return "true";
        }

        if (state == UiCommandCheckState.Mixed)
        {
            return "mixed";
        }

        return "false";
    }

    internal string FormatBoolean(bool value)
    {
        if (value)
        {
            return "true";
        }

        return "false";
    }

    internal string CreateSyntheticId(UiNode node, string suffix, string identity)
    {
        string token = StableHash.HashParts(new string[] { GetNodeId(node), GetNodeKey(node), "tree", suffix, identity });
        return "ui-tree-" + token.Substring(0, 20);
    }

    internal string GetNodeKey(UiNode node)
    {
        if (node.Identity == null)
        {
            return string.Empty;
        }

        if (node.Identity.AuthorKey.Length > 0)
        {
            return node.Identity.AuthorKey;
        }

        return node.Identity.SiblingIdentity;
    }

    internal string GetNodeId(UiNode node)
    {
        if (node.Identity == null)
        {
            return string.Empty;
        }

        return node.Identity.StableId;
    }

    internal int GetNodeVersion(UiNode node)
    {
        if (node.State == null)
        {
            return 0;
        }

        return node.State.Version;
    }

    internal void SortAttributes(List<DomAttribute> attributes)
    {
        int index = 1;
        while (index < attributes.Count)
        {
            DomAttribute candidate = attributes[index];
            int insertionIndex = index - 1;
            while (insertionIndex >= 0 && string.CompareOrdinal(attributes[insertionIndex].Name, candidate.Name) > 0)
            {
                attributes[insertionIndex + 1] = attributes[insertionIndex];
                insertionIndex--;
            }

            attributes[insertionIndex + 1] = candidate;
            index++;
        }
    }

    internal UiRenderException CreateRenderException(UiNode node, string message)
    {
        string nodeId = GetNodeId(node);
        if (nodeId.Length == 0)
        {
            nodeId = "unknown";
        }

        return new UiRenderException(message, nodeId, "dom");
    }
}
