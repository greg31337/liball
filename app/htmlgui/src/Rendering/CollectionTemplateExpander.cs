using System;
using System.Collections.Generic;

namespace htmlgui;

internal class CollectionTemplateExpander
{
    internal bool IsCollectionControlName(string name)
    {
        switch (name)
        {
            case "collection":
            case "list":
            case "unordered-list":
            case "ordered-list":
            case "description-list":
            case "interactive-list":
            case "selection-list":
            case "listbox":
            case "ol":
            case "ul":
            case "dl":
                return true;
            default:
                return false;
        }
    }

    internal DomNode ExpandCollection(UiNode node, DomNode?[] itemContents, DomNode? emptyContent, DomNode? loadingContent, DomNode? errorContent)
    {
        UiCollection collection = RequireCollection(node);
        UiCollectionItem[] items = ValidateItems(node, collection, itemContents);
        ValidateSelection(node, collection, items);
        ValidateQuery(node, collection);
        ValidateGroups(node, collection, items);
        int pageCount = ValidatePageRange(node, collection, items.Length);
        if (collection.HasError && collection.IsLoading)
        {
            throw CreateRenderException(node, "A collection cannot be loading and in an error state at the same time.");
        }

        if (collection.HasError && errorContent == null)
        {
            throw CreateRenderException(node, "An active collection error requires error content.");
        }

        bool interactive = IsInteractiveCollection(node, collection);
        string rootName = GetRootElementName(node, interactive);
        List<DomAttribute> attributes = CreateRootAttributes(node, collection, items, pageCount, interactive, rootName);
        DomNode[] children = CreateCollectionChildren(node, collection, items, itemContents, emptyContent, loadingContent, errorContent, interactive, rootName);
        return new DomNode
        {
            Kind = DomNodeKind.Element,
            Key = GetNodeKey(node),
            Id = GetNodeId(node),
            Name = rootName,
            Attributes = attributes.ToArray(),
            Children = children,
            Version = GetNodeVersion(node)
        };
    }

    internal UiCollection RequireCollection(UiNode node)
    {
        UiCollection? collection = node.Collection;
        if (collection == null)
        {
            throw CreateRenderException(node, "A list control requires a collection description.");
        }

        if (string.IsNullOrWhiteSpace(collection.Key))
        {
            throw CreateRenderException(node, "Collections require a non-empty stable key.");
        }

        return collection;
    }

    internal UiCollectionItem[] ValidateItems(UiNode node, UiCollection collection, DomNode?[] itemContents)
    {
        UiCollectionItem[]? items = collection.Items;
        if (items == null)
        {
            throw CreateRenderException(node, "Collection items cannot be null.");
        }

        if (itemContents.Length != items.Length)
        {
            throw CreateRenderException(node, "Expanded item content must align with collection items.");
        }

        int index = 0;
        while (index < items.Length)
        {
            UiCollectionItem? item = items[index];
            if (item == null || string.IsNullOrWhiteSpace(item.Key))
            {
                throw CreateRenderException(node, "Collection items require non-empty stable keys.");
            }

            if (item.Value == null)
            {
                throw CreateRenderException(node, "Collection items require portable values.");
            }

            ValueJsonCodec.ValidateValue(item.Value, 64, 10000, 1048576, 1048576);
            int earlierIndex = 0;
            while (earlierIndex < index)
            {
                if (items[earlierIndex].Key == item.Key)
                {
                    throw CreateRenderException(node, "Collection item keys must be unique.");
                }

                earlierIndex++;
            }

            index++;
        }

        return items;
    }

    internal void ValidateSelection(UiNode node, UiCollection collection, UiCollectionItem[] items)
    {
        UiSelection? selection = collection.Selection;
        if (selection == null)
        {
            throw CreateRenderException(node, "Collections require selection state.");
        }

        switch (selection.Mode)
        {
            case UiSelectionMode.None:
            case UiSelectionMode.Single:
            case UiSelectionMode.Multiple:
            case UiSelectionMode.Range:
                break;
            default:
                throw new ArgumentOutOfRangeException("selectionMode", selection.Mode, "The selection mode is not supported.");
        }

        string[]? selectedKeys = selection.SelectedKeys;
        if (selectedKeys == null)
        {
            throw CreateRenderException(node, "Selected item keys cannot be null.");
        }

        if (selection.Mode == UiSelectionMode.None && selectedKeys.Length > 0)
        {
            throw CreateRenderException(node, "Selection-disabled collections cannot contain selected keys.");
        }

        if (selection.Mode == UiSelectionMode.Single && selectedKeys.Length > 1)
        {
            throw CreateRenderException(node, "Single-selection collections permit at most one selected key.");
        }

        int index = 0;
        while (index < selectedKeys.Length)
        {
            string? key = selectedKeys[index];
            if (string.IsNullOrWhiteSpace(key) || FindItemIndex(items, key) < 0)
            {
                throw CreateRenderException(node, "Selected keys must identify collection items.");
            }

            int earlierIndex = 0;
            while (earlierIndex < index)
            {
                if (selectedKeys[earlierIndex] == key)
                {
                    throw CreateRenderException(node, "Selected item keys must be unique.");
                }

                earlierIndex++;
            }

            index++;
        }

        if (selection.ActiveKey != null && FindItemIndex(items, selection.ActiveKey) < 0)
        {
            throw CreateRenderException(node, "The active key must identify a collection item.");
        }

        if (selection.AnchorKey != null)
        {
            if (selection.Mode != UiSelectionMode.Range || FindItemIndex(items, selection.AnchorKey) < 0)
            {
                throw CreateRenderException(node, "A range-selection anchor must identify a collection item.");
            }
        }
    }

    internal void ValidateQuery(UiNode node, UiCollection collection)
    {
        UiSort[]? sorts = collection.Sorts;
        if (sorts == null)
        {
            throw CreateRenderException(node, "Collection sort descriptions cannot be null.");
        }

        int sortIndex = 0;
        while (sortIndex < sorts.Length)
        {
            UiSort? sort = sorts[sortIndex];
            if (sort == null || string.IsNullOrWhiteSpace(sort.Key) || sort.Priority < 0)
            {
                throw CreateRenderException(node, "Sort descriptions require keys and non-negative priorities.");
            }

            ValidateSortDirection(sort.Direction);
            int earlierSortIndex = 0;
            while (earlierSortIndex < sortIndex)
            {
                if (sorts[earlierSortIndex].Key == sort.Key || sorts[earlierSortIndex].Priority == sort.Priority)
                {
                    throw CreateRenderException(node, "Sort keys and priorities must be unique.");
                }

                earlierSortIndex++;
            }

            sortIndex++;
        }

        UiFilter[]? filters = collection.Filters;
        if (filters == null)
        {
            throw CreateRenderException(node, "Collection filter descriptions cannot be null.");
        }

        int filterIndex = 0;
        while (filterIndex < filters.Length)
        {
            UiFilter? filter = filters[filterIndex];
            if (filter == null || string.IsNullOrWhiteSpace(filter.Key) || string.IsNullOrWhiteSpace(filter.Operator) || filter.Value == null)
            {
                throw CreateRenderException(node, "Filters require keys, operators, and portable operands.");
            }

            ValueJsonCodec.ValidateValue(filter.Value, 64, 10000, 1048576, 1048576);
            filterIndex++;
        }
    }

    internal void ValidateSortDirection(UiSortDirection direction)
    {
        switch (direction)
        {
            case UiSortDirection.None:
            case UiSortDirection.Ascending:
            case UiSortDirection.Descending:
                return;
            default:
                throw new ArgumentOutOfRangeException("sortDirection", direction, "The sort direction is not supported.");
        }
    }

    internal void ValidateGroups(UiNode node, UiCollection collection, UiCollectionItem[] items)
    {
        UiGroup[]? groups = collection.Groups;
        if (groups == null)
        {
            throw CreateRenderException(node, "Collection groups cannot be null.");
        }

        int groupIndex = 0;
        while (groupIndex < groups.Length)
        {
            UiGroup? group = groups[groupIndex];
            if (group == null || string.IsNullOrWhiteSpace(group.Key) || group.Label == null || group.ItemKeys == null)
            {
                throw CreateRenderException(node, "Groups require keys, labels, and item-key collections.");
            }

            int earlierGroupIndex = 0;
            while (earlierGroupIndex < groupIndex)
            {
                if (groups[earlierGroupIndex].Key == group.Key)
                {
                    throw CreateRenderException(node, "Group keys must be unique.");
                }

                earlierGroupIndex++;
            }

            int keyIndex = 0;
            while (keyIndex < group.ItemKeys.Length)
            {
                string? itemKey = group.ItemKeys[keyIndex];
                if (string.IsNullOrWhiteSpace(itemKey) || FindItemIndex(items, itemKey) < 0)
                {
                    throw CreateRenderException(node, "Group item keys must identify collection items.");
                }

                int earlierKeyIndex = 0;
                while (earlierKeyIndex < keyIndex)
                {
                    if (group.ItemKeys[earlierKeyIndex] == itemKey)
                    {
                        throw CreateRenderException(node, "Group item keys must be unique.");
                    }

                    earlierKeyIndex++;
                }

                int otherGroupIndex = 0;
                while (otherGroupIndex < groupIndex)
                {
                    if (ContainsKey(groups[otherGroupIndex].ItemKeys, itemKey))
                    {
                        throw CreateRenderException(node, "An item cannot belong to more than one group.");
                    }

                    otherGroupIndex++;
                }

                UiCollectionItem item = items[FindItemIndex(items, itemKey)];
                if (item.GroupKey != null && item.GroupKey != group.Key)
                {
                    throw CreateRenderException(node, "Item and group membership metadata must agree.");
                }

                keyIndex++;
            }

            groupIndex++;
        }

        int itemIndex = 0;
        while (itemIndex < items.Length)
        {
            string? groupKey = items[itemIndex].GroupKey;
            if (groupKey != null)
            {
                int matchingGroupIndex = FindGroupIndex(groups, groupKey);
                if (matchingGroupIndex < 0)
                {
                    throw CreateRenderException(node, "Item group keys must identify collection groups.");
                }

                UiGroup matchingGroup = groups[matchingGroupIndex];
                if (matchingGroup.ItemKeys.Length > 0 && !ContainsKey(matchingGroup.ItemKeys, items[itemIndex].Key))
                {
                    throw CreateRenderException(node, "Explicit group membership must include every item that names the group.");
                }
            }

            itemIndex++;
        }
    }

    internal int ValidatePageRange(UiNode node, UiCollection collection, int itemCount)
    {
        UiPageRange? range = collection.PageRange;
        if (range == null)
        {
            throw CreateRenderException(node, "Collections require page-range state.");
        }

        if (range.Start < 0 || range.Count < 0)
        {
            throw CreateRenderException(node, "Page starts and counts cannot be negative.");
        }

        int count = range.Count;
        if (count == 0 && itemCount > 0)
        {
            count = itemCount;
        }

        if (range.HasTotalCount)
        {
            if (range.TotalCount < 0 || (long)range.Start + count > range.TotalCount)
            {
                throw CreateRenderException(node, "The materialized page must fit within the total collection count.");
            }
        }

        return count;
    }

    internal bool IsInteractiveCollection(UiNode node, UiCollection collection)
    {
        if (collection.Selection.Mode != UiSelectionMode.None || collection.CanReorder)
        {
            return true;
        }

        return node.Name == "interactive-list" || node.Name == "selection-list" || node.Name == "listbox";
    }

    internal string GetRootElementName(UiNode node, bool interactive)
    {
        if (interactive)
        {
            return HtmlElements.Division;
        }

        if (node.Name == "ordered-list" || node.Name == "ol")
        {
            return HtmlElements.OrderedList;
        }

        if (node.Name == "description-list" || node.Name == "dl")
        {
            return HtmlElements.DescriptionList;
        }

        return HtmlElements.UnorderedList;
    }

    internal List<DomAttribute> CreateRootAttributes(UiNode node, UiCollection collection, UiCollectionItem[] items, int pageCount, bool interactive, string rootName)
    {
        List<DomAttribute> attributes = new List<DomAttribute>();
        string className = "htmlgui-list htmlgui-unordered-list";
        if (rootName == HtmlElements.OrderedList)
        {
            className = "htmlgui-list htmlgui-ordered-list";
        }
        else if (rootName == HtmlElements.DescriptionList)
        {
            className = "htmlgui-list htmlgui-description-list";
        }
        else if (interactive)
        {
            className = "htmlgui-list htmlgui-interactive-list";
        }

        attributes.Add(new DomAttribute { Name = HtmlAttributes.Class, Value = className });
        attributes.Add(new DomAttribute { Name = HtmlAttributes.DataPrefix + "collection-key", Value = collection.Key });
        attributes.Add(new DomAttribute { Name = HtmlAttributes.DataPrefix + "page-count", Value = InvariantFormatter.FormatInt32(pageCount) });
        attributes.Add(new DomAttribute { Name = HtmlAttributes.DataPrefix + "page-end", Value = FormatBoolean(collection.PageRange.IsEnd) });
        attributes.Add(new DomAttribute { Name = HtmlAttributes.DataPrefix + "page-start", Value = InvariantFormatter.FormatInt32(collection.PageRange.Start) });
        attributes.Add(new DomAttribute { Name = HtmlAttributes.DataPrefix + "selection-mode", Value = GetSelectionModeToken(collection.Selection.Mode) });
        if (collection.PageRange.HasTotalCount)
        {
            attributes.Add(new DomAttribute { Name = HtmlAttributes.DataPrefix + "total-count", Value = InvariantFormatter.FormatInt32(collection.PageRange.TotalCount) });
        }

        if (collection.CanReorder)
        {
            attributes.Add(new DomAttribute { Name = HtmlAttributes.DataPrefix + "reorder", Value = "true" });
        }

        if (collection.Selection.AnchorKey != null)
        {
            attributes.Add(new DomAttribute { Name = HtmlAttributes.DataPrefix + "selection-anchor", Value = collection.Selection.AnchorKey });
        }

        AddQueryAttributes(collection, attributes);
        AddRootState(node, collection, attributes);
        if (interactive)
        {
            attributes.Add(new DomAttribute { Name = HtmlAttributes.Role, Value = AriaRoles.ListBox });
            if (collection.Selection.Mode == UiSelectionMode.Multiple || collection.Selection.Mode == UiSelectionMode.Range)
            {
                attributes.Add(new DomAttribute { Name = AriaAttributes.MultiSelectable, Value = "true" });
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
            if (collection.Selection.ActiveKey != null)
            {
                attributes.Add(new DomAttribute
                {
                    Name = AriaAttributes.ActiveDescendant,
                    Value = CreateSyntheticId(node, "item", collection.Selection.ActiveKey)
                });
            }
        }

        SortAttributes(attributes);
        return attributes;
    }

    internal void AddQueryAttributes(UiCollection collection, List<DomAttribute> attributes)
    {
        UiSort[] sorts = CopyAndSortDescriptions(collection.Sorts);
        int sortIndex = 0;
        while (sortIndex < sorts.Length)
        {
            UiSort sort = sorts[sortIndex];
            string prefix = HtmlAttributes.DataPrefix + "sort-" + InvariantFormatter.FormatInt32(sortIndex) + "-";
            attributes.Add(new DomAttribute { Name = prefix + "direction", Value = GetSortDirectionToken(sort.Direction) });
            attributes.Add(new DomAttribute { Name = prefix + "key", Value = sort.Key });
            attributes.Add(new DomAttribute { Name = prefix + "priority", Value = InvariantFormatter.FormatInt32(sort.Priority) });
            sortIndex++;
        }

        int filterIndex = 0;
        while (filterIndex < collection.Filters.Length)
        {
            UiFilter filter = collection.Filters[filterIndex];
            string prefix = HtmlAttributes.DataPrefix + "filter-" + InvariantFormatter.FormatInt32(filterIndex) + "-";
            attributes.Add(new DomAttribute { Name = prefix + "ignore-case", Value = FormatBoolean(filter.IgnoreCase) });
            attributes.Add(new DomAttribute { Name = prefix + "key", Value = filter.Key });
            attributes.Add(new DomAttribute { Name = prefix + "operator", Value = filter.Operator });
            attributes.Add(new DomAttribute { Name = prefix + "value", Value = new DocumentComposer().FormatMetadataValue(filter.Value) });
            filterIndex++;
        }
    }

    internal UiSort[] CopyAndSortDescriptions(UiSort[] source)
    {
        UiSort[] result = new UiSort[source.Length];
        Array.Copy(source, result, source.Length);
        int index = 1;
        while (index < result.Length)
        {
            UiSort candidate = result[index];
            int insertionIndex = index - 1;
            while (insertionIndex >= 0 && CompareSort(result[insertionIndex], candidate) > 0)
            {
                result[insertionIndex + 1] = result[insertionIndex];
                insertionIndex--;
            }

            result[insertionIndex + 1] = candidate;
            index++;
        }

        return result;
    }

    internal int CompareSort(UiSort left, UiSort right)
    {
        if (left.Priority < right.Priority)
        {
            return -1;
        }

        if (left.Priority > right.Priority)
        {
            return 1;
        }

        return string.CompareOrdinal(left.Key, right.Key);
    }

    internal void AddRootState(UiNode node, UiCollection collection, List<DomAttribute> attributes)
    {
        if (collection.HasError)
        {
            attributes.Add(new DomAttribute { Name = HtmlAttributes.DataPrefix + "collection-state", Value = "error" });
        }
        else if (collection.IsLoading)
        {
            attributes.Add(new DomAttribute { Name = HtmlAttributes.DataPrefix + "collection-state", Value = "loading" });
        }
        else if (collection.Items.Length == 0)
        {
            attributes.Add(new DomAttribute { Name = HtmlAttributes.DataPrefix + "collection-state", Value = "empty" });
        }
        else
        {
            attributes.Add(new DomAttribute { Name = HtmlAttributes.DataPrefix + "collection-state", Value = "ready" });
        }

        if (collection.IsLoading || (node.State != null && node.State.IsBusy))
        {
            attributes.Add(new DomAttribute { Name = AriaAttributes.Busy, Value = "true" });
        }

        if (node.State != null && !node.State.IsVisible)
        {
            attributes.Add(new DomAttribute { Name = HtmlAttributes.Hidden, Value = string.Empty, IsBoolean = true });
        }

        if (node.State != null && !node.State.IsEnabled)
        {
            attributes.Add(new DomAttribute { Name = AriaAttributes.Disabled, Value = "true" });
        }
    }

    internal DomNode[] CreateCollectionChildren(UiNode node, UiCollection collection, UiCollectionItem[] items, DomNode?[] itemContents, DomNode? emptyContent, DomNode? loadingContent, DomNode? errorContent, bool interactive, string rootName)
    {
        if (collection.HasError)
        {
            return new DomNode[] { CreateStateNode(node, "error", errorContent, interactive, rootName) };
        }

        if (items.Length == 0)
        {
            if (collection.IsLoading)
            {
                return new DomNode[] { CreateStateNode(node, "loading", loadingContent, interactive, rootName) };
            }

            if (emptyContent != null)
            {
                return new DomNode[] { CreateStateNode(node, "empty", emptyContent, interactive, rootName) };
            }

            return Array.Empty<DomNode>();
        }

        List<DomNode> result = CreateGroupedItems(node, collection, items, itemContents, interactive, rootName);
        if (collection.IsLoading && loadingContent != null)
        {
            result.Add(CreateStateNode(node, "loading", loadingContent, interactive, rootName));
        }

        return result.ToArray();
    }

    internal List<DomNode> CreateGroupedItems(UiNode node, UiCollection collection, UiCollectionItem[] items, DomNode?[] itemContents, bool interactive, string rootName)
    {
        List<DomNode> result = new List<DomNode>();
        bool[] grouped = new bool[items.Length];
        int groupIndex = 0;
        while (groupIndex < collection.Groups.Length)
        {
            UiGroup group = collection.Groups[groupIndex];
            int[] indexes = GetGroupItemIndexes(group, items);
            int markIndex = 0;
            while (markIndex < indexes.Length)
            {
                grouped[indexes[markIndex]] = true;
                markIndex++;
            }

            result.Add(CreateGroupNode(node, collection, group, items, itemContents, indexes, interactive, rootName));
            groupIndex++;
        }

        int itemIndex = 0;
        while (itemIndex < items.Length)
        {
            if (!grouped[itemIndex])
            {
                result.Add(CreateItemNode(node, collection, items[itemIndex], itemContents[itemIndex], itemIndex, interactive, rootName));
            }

            itemIndex++;
        }

        return result;
    }

    internal int[] GetGroupItemIndexes(UiGroup group, UiCollectionItem[] items)
    {
        List<int> indexes = new List<int>();
        if (group.ItemKeys.Length > 0)
        {
            int keyIndex = 0;
            while (keyIndex < group.ItemKeys.Length)
            {
                indexes.Add(FindItemIndex(items, group.ItemKeys[keyIndex]));
                keyIndex++;
            }
        }
        else
        {
            int itemIndex = 0;
            while (itemIndex < items.Length)
            {
                if (items[itemIndex].GroupKey == group.Key)
                {
                    indexes.Add(itemIndex);
                }

                itemIndex++;
            }
        }

        return indexes.ToArray();
    }

    internal DomNode CreateGroupNode(UiNode node, UiCollection collection, UiGroup group, UiCollectionItem[] items, DomNode?[] itemContents, int[] indexes, bool interactive, string rootName)
    {
        List<DomAttribute> attributes = new List<DomAttribute>();
        attributes.Add(new DomAttribute { Name = HtmlAttributes.Class, Value = "htmlgui-list-group" });
        attributes.Add(new DomAttribute { Name = HtmlAttributes.DataPrefix + "expanded", Value = FormatBoolean(group.IsExpanded) });
        attributes.Add(new DomAttribute { Name = HtmlAttributes.DataPrefix + "group-key", Value = group.Key });
        if (interactive)
        {
            attributes.Add(new DomAttribute { Name = AriaAttributes.Label, Value = group.Label });
            attributes.Add(new DomAttribute { Name = HtmlAttributes.Role, Value = AriaRoles.Group });
        }

        SortAttributes(attributes);
        DomNode label = CreateTextElement(node, "$group-label", "group-label", group.Key, group.Label, "htmlgui-list-group-label");
        List<DomAttribute> contentAttributes = new List<DomAttribute>();
        contentAttributes.Add(new DomAttribute { Name = HtmlAttributes.Class, Value = "htmlgui-list-group-items" });
        if (!group.IsExpanded)
        {
            contentAttributes.Add(new DomAttribute { Name = HtmlAttributes.Hidden, Value = string.Empty, IsBoolean = true });
        }

        SortAttributes(contentAttributes);
        DomNode[] children = new DomNode[indexes.Length];
        int index = 0;
        while (index < indexes.Length)
        {
            int itemIndex = indexes[index];
            children[index] = CreateItemNode(node, collection, items[itemIndex], itemContents[itemIndex], itemIndex, interactive, rootName);
            index++;
        }

        string contentName = HtmlElements.Division;
        if (!interactive && rootName != HtmlElements.DescriptionList)
        {
            contentName = rootName;
        }

        DomNode content = new DomNode
        {
            Kind = DomNodeKind.Element,
            Key = "$group-items",
            Id = CreateSyntheticId(node, "group-items", group.Key),
            Name = contentName,
            Attributes = contentAttributes.ToArray(),
            Children = children,
            Version = GetNodeVersion(node)
        };
        string groupName = HtmlElements.Division;
        if (!interactive && rootName != HtmlElements.DescriptionList)
        {
            groupName = HtmlElements.ListItem;
        }
        else if (!interactive)
        {
            label.Name = HtmlElements.DescriptionTerm;
            DomNode nestedDescriptions = new DomNode
            {
                Kind = DomNodeKind.Element,
                Key = "$group-description-items",
                Id = CreateSyntheticId(node, "group-description-items", group.Key),
                Name = HtmlElements.DescriptionList,
                Attributes = new DomAttribute[] { new DomAttribute { Name = HtmlAttributes.Class, Value = "htmlgui-list-group-items" } },
                Children = children,
                Version = GetNodeVersion(node)
            };
            content.Name = HtmlElements.DescriptionDetails;
            content.Children = new DomNode[] { nestedDescriptions };
        }

        return new DomNode
        {
            Kind = DomNodeKind.Element,
            Key = group.Key,
            Id = CreateSyntheticId(node, "group", group.Key),
            Name = groupName,
            Attributes = attributes.ToArray(),
            Children = new DomNode[] { label, content },
            Version = GetNodeVersion(node)
        };
    }

    internal DomNode CreateItemNode(UiNode node, UiCollection collection, UiCollectionItem item, DomNode? content, int itemIndex, bool interactive, string rootName)
    {
        if (rootName == HtmlElements.DescriptionList && !interactive)
        {
            return CreateDescriptionItem(node, item, content);
        }

        List<DomAttribute> attributes = new List<DomAttribute>();
        attributes.Add(new DomAttribute { Name = HtmlAttributes.Class, Value = "htmlgui-list-item" });
        attributes.Add(new DomAttribute { Name = HtmlAttributes.DataPrefix + "item-key", Value = item.Key });
        if (item.GroupKey != null)
        {
            attributes.Add(new DomAttribute { Name = HtmlAttributes.DataPrefix + "group-key", Value = item.GroupKey });
        }

        if (item.IsPlaceholder)
        {
            attributes.Add(new DomAttribute { Name = AriaAttributes.Busy, Value = "true" });
            attributes.Add(new DomAttribute { Name = HtmlAttributes.DataPrefix + "placeholder", Value = "true" });
        }

        if (collection.CanReorder)
        {
            attributes.Add(new DomAttribute { Name = "draggable", Value = "true" });
            attributes.Add(new DomAttribute { Name = HtmlAttributes.DataPrefix + "reorderable", Value = "true" });
        }

        if (interactive)
        {
            bool selected = ContainsKey(collection.Selection.SelectedKeys, item.Key);
            attributes.Add(new DomAttribute { Name = AriaAttributes.PositionInSet, Value = InvariantFormatter.FormatInt32(collection.PageRange.Start + itemIndex + 1) });
            attributes.Add(new DomAttribute { Name = AriaAttributes.Selected, Value = FormatBoolean(selected) });
            attributes.Add(new DomAttribute { Name = HtmlAttributes.Role, Value = AriaRoles.Option });
            attributes.Add(new DomAttribute { Name = HtmlAttributes.TabIndex, Value = "-1" });
            if (collection.PageRange.HasTotalCount)
            {
                attributes.Add(new DomAttribute { Name = AriaAttributes.SetSize, Value = InvariantFormatter.FormatInt32(collection.PageRange.TotalCount) });
            }

            if (item.IsDisabled || (node.State != null && !node.State.IsEnabled))
            {
                attributes.Add(new DomAttribute { Name = AriaAttributes.Disabled, Value = "true" });
            }

            if (collection.Selection.ActiveKey == item.Key)
            {
                attributes.Add(new DomAttribute { Name = HtmlAttributes.DataPrefix + "active", Value = "true" });
            }
        }

        SortAttributes(attributes);
        DomNode[] children = CreateItemContent(node, item, content);
        return new DomNode
        {
            Kind = DomNodeKind.Element,
            Key = item.Key,
            Id = CreateSyntheticId(node, "item", item.Key),
            Name = interactive ? HtmlElements.Division : HtmlElements.ListItem,
            Attributes = attributes.ToArray(),
            Children = children,
            Version = GetNodeVersion(node)
        };
    }

    internal DomNode CreateDescriptionItem(UiNode node, UiCollectionItem item, DomNode? content)
    {
        string term = FormatItemValue(item.Value);
        if (term.Length == 0)
        {
            term = item.Key;
        }

        DomNode termNode = CreateTextElement(node, "$description-term", "description-term", item.Key, term, "htmlgui-description-term");
        termNode.Name = HtmlElements.DescriptionTerm;
        DomNode[] detailsChildren = Array.Empty<DomNode>();
        if (content != null)
        {
            detailsChildren = new DomNode[] { content };
        }

        DomNode details = new DomNode
        {
            Kind = DomNodeKind.Element,
            Key = "$description-details",
            Id = CreateSyntheticId(node, "description-details", item.Key),
            Name = HtmlElements.DescriptionDetails,
            Attributes = new DomAttribute[] { new DomAttribute { Name = HtmlAttributes.Class, Value = "htmlgui-description-details" } },
            Children = detailsChildren,
            Version = GetNodeVersion(node)
        };
        return new DomNode
        {
            Kind = DomNodeKind.Element,
            Key = item.Key,
            Id = CreateSyntheticId(node, "item", item.Key),
            Name = HtmlElements.Division,
            Attributes = new DomAttribute[]
            {
                new DomAttribute { Name = HtmlAttributes.Class, Value = "htmlgui-description-item" },
                new DomAttribute { Name = HtmlAttributes.DataPrefix + "item-key", Value = item.Key }
            },
            Children = new DomNode[] { termNode, details },
            Version = GetNodeVersion(node)
        };
    }

    internal DomNode[] CreateItemContent(UiNode node, UiCollectionItem item, DomNode? content)
    {
        if (content != null)
        {
            return new DomNode[] { content };
        }

        string text = FormatItemValue(item.Value);
        if (text.Length == 0)
        {
            return Array.Empty<DomNode>();
        }

        return new DomNode[]
        {
            new DomNode
            {
                Kind = DomNodeKind.Text,
                Key = "$item-value",
                Id = CreateSyntheticId(node, "item-value", item.Key),
                Text = text,
                Version = GetNodeVersion(node)
            }
        };
    }

    internal DomNode CreateStateNode(UiNode node, string state, DomNode? content, bool interactive, string rootName)
    {
        List<DomAttribute> attributes = new List<DomAttribute>();
        attributes.Add(new DomAttribute { Name = HtmlAttributes.Class, Value = "htmlgui-list-state htmlgui-list-" + state });
        if (state == "error")
        {
            attributes.Add(new DomAttribute { Name = HtmlAttributes.Role, Value = AriaRoles.Alert });
        }
        else
        {
            attributes.Add(new DomAttribute { Name = HtmlAttributes.Role, Value = AriaRoles.Status });
        }

        SortAttributes(attributes);
        DomNode[] children = Array.Empty<DomNode>();
        if (content != null)
        {
            children = new DomNode[] { content };
        }

        string name = HtmlElements.Division;
        if (!interactive && rootName != HtmlElements.DescriptionList)
        {
            name = HtmlElements.ListItem;
        }

        return new DomNode
        {
            Kind = DomNodeKind.Element,
            Key = "$collection-" + state,
            Id = CreateSyntheticId(node, "state", state),
            Name = name,
            Attributes = attributes.ToArray(),
            Children = children,
            Version = GetNodeVersion(node)
        };
    }

    internal DomNode CreateTextElement(UiNode node, string key, string suffix, string identity, string text, string className)
    {
        return new DomNode
        {
            Kind = DomNodeKind.Element,
            Key = key,
            Id = CreateSyntheticId(node, suffix, identity),
            Name = HtmlElements.Span,
            Attributes = new DomAttribute[] { new DomAttribute { Name = HtmlAttributes.Class, Value = className } },
            Children = new DomNode[]
            {
                new DomNode
                {
                    Kind = DomNodeKind.Text,
                    Key = "$text",
                    Id = CreateSyntheticId(node, suffix + "-text", identity),
                    Text = text,
                    Version = GetNodeVersion(node)
                }
            },
            Version = GetNodeVersion(node)
        };
    }

    internal int FindItemIndex(UiCollectionItem[] items, string key)
    {
        int index = 0;
        while (index < items.Length)
        {
            if (items[index].Key == key)
            {
                return index;
            }

            index++;
        }

        return -1;
    }

    internal int FindGroupIndex(UiGroup[] groups, string key)
    {
        int index = 0;
        while (index < groups.Length)
        {
            if (groups[index].Key == key)
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

    internal string FormatItemValue(UiValue value)
    {
        return new DocumentComposer().FormatMetadataValue(value);
    }

    internal string GetSelectionModeToken(UiSelectionMode mode)
    {
        switch (mode)
        {
            case UiSelectionMode.None:
                return "none";
            case UiSelectionMode.Single:
                return "single";
            case UiSelectionMode.Multiple:
                return "multiple";
            case UiSelectionMode.Range:
                return "range";
            default:
                throw new ArgumentOutOfRangeException("selectionMode", mode, "The selection mode is not supported.");
        }
    }

    internal string GetSortDirectionToken(UiSortDirection direction)
    {
        switch (direction)
        {
            case UiSortDirection.None:
                return "none";
            case UiSortDirection.Ascending:
                return "ascending";
            case UiSortDirection.Descending:
                return "descending";
            default:
                throw new ArgumentOutOfRangeException("sortDirection", direction, "The sort direction is not supported.");
        }
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
        string token = StableHash.HashParts(new string[] { GetNodeId(node), GetNodeKey(node), "collection", suffix, identity });
        return "ui-collection-" + token.Substring(0, 20);
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
