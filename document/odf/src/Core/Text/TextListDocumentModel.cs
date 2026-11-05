using System;
using System.Collections.Generic;
using System.Text;

namespace odf;

internal class TextListDocumentState
{
    internal OdfNode[] OriginalListNodes = [];
    internal OdfNode[] OriginalNumberedParagraphNodes = [];
}

internal static class TextListDocumentModel
{
    internal static void LoadTextLists(OdfDocument document)
    {
        if (document == null)
        {
            throw new OdfDocumentException(
                "A document is required for list materialization.");
        }
        List<OdfTextList> lists = new List<OdfTextList>();
        List<OdfNumberedParagraph> numbered =
            new List<OdfNumberedParagraph>();
        List<OdfNode> listNodes = new List<OdfNode>();
        List<OdfNode> numberedNodes = new List<OdfNode>();
        int partIndex = 0;
        while (partIndex < document.Parts.Length)
        {
            OdfPart part = document.Parts[partIndex];
            if (part.Body != null)
            {
                CollectLists(
                    document,
                    part.Body,
                    part.Path,
                    lists,
                    numbered,
                    listNodes,
                    numberedNodes);
            }
            partIndex = partIndex + 1;
        }
        int masterIndex = 0;
        while (masterIndex < document.MasterPages.Length)
        {
            OdfMasterPage master = document.MasterPages[masterIndex];
            if (master.Node != null)
            {
                CollectLists(
                    document,
                    master.Node,
                    master.PartPath,
                    lists,
                    numbered,
                    listNodes,
                    numberedNodes);
            }
            masterIndex = masterIndex + 1;
        }
        document.TextLists = lists.ToArray();
        document.NumberedParagraphs = numbered.ToArray();
        ResolveListRelationships(document);
        ResolveNumberedParagraphs(document);
        TextListDocumentState state = new TextListDocumentState();
        state.OriginalListNodes = listNodes.ToArray();
        state.OriginalNumberedParagraphNodes = numberedNodes.ToArray();
        document.TextListState = state;
    }

    internal static void SynchronizeTextLists(OdfDocument document)
    {
        if (document == null
            || document.TextLists == null
            || document.NumberedParagraphs == null)
        {
            throw new OdfDocumentException(
                "List and numbered-paragraph collections are required for writing.");
        }
        TextListDocumentState state = document.TextListState
            ?? new TextListDocumentState();
        RemoveDeletedLists(
            document.TextLists,
            state.OriginalListNodes);
        RemoveDeletedNumberedParagraphs(
            document.NumberedParagraphs,
            state.OriginalNumberedParagraphNodes);
        List<OdfNode> listNodes = new List<OdfNode>();
        int index = 0;
        while (index < document.TextLists.Length)
        {
            OdfTextList list = document.TextLists[index];
            if (list == null || list.Node == null)
            {
                throw new OdfEditException(
                    "A document list requires an underlying XML element.");
            }
            ApplyListFields(list);
            ApplyItemFields(list.Header);
            int itemIndex = 0;
            while (itemIndex < list.Items.Length)
            {
                ApplyItemFields(list.Items[itemIndex]);
                itemIndex = itemIndex + 1;
            }
            listNodes.Add(list.Node);
            index = index + 1;
        }
        List<OdfNode> numberedNodes = new List<OdfNode>();
        index = 0;
        while (index < document.NumberedParagraphs.Length)
        {
            OdfNumberedParagraph paragraph =
                document.NumberedParagraphs[index];
            if (paragraph == null || paragraph.Node == null)
            {
                throw new OdfEditException(
                    "A numbered paragraph requires an underlying XML element.");
            }
            ApplyNumberedParagraphFields(paragraph);
            numberedNodes.Add(paragraph.Node);
            index = index + 1;
        }
        state.OriginalListNodes = listNodes.ToArray();
        state.OriginalNumberedParagraphNodes = numberedNodes.ToArray();
        document.TextListState = state;
        ResolveListRelationships(document);
        ResolveNumberedParagraphs(document);
    }

    internal static void ResolveListRelationships(OdfDocument document)
    {
        OdfTextList[] lists = document.TextLists;
        ResolveParents(lists);
        ResolveStyles(document, lists);
        ResolveContinuations(lists);
        MarkContinuationCycles(lists);
        List<OdfTextList> visiting = new List<OdfTextList>();
        List<OdfTextList> completed = new List<OdfTextList>();
        int index = 0;
        while (index < lists.Length)
        {
            AssignListOrdinals(lists[index], visiting, completed);
            index = index + 1;
        }
    }

    static void CollectLists(
        OdfDocument document,
        OdfNode node,
        string partPath,
        List<OdfTextList> lists,
        List<OdfNumberedParagraph> numbered,
        List<OdfNode> listNodes,
        List<OdfNode> numberedNodes)
    {
        if (IsElement(node, "list"))
        {
            lists.Add(CreateList(document, node, partPath));
            listNodes.Add(node);
        }
        else if (IsElement(node, "numbered-paragraph"))
        {
            numbered.Add(CreateNumberedParagraph(
                document,
                node,
                partPath));
            numberedNodes.Add(node);
        }
        int index = 0;
        while (index < node.Children.Length)
        {
            CollectLists(
                document,
                node.Children[index],
                partPath,
                lists,
                numbered,
                listNodes,
                numberedNodes);
            index = index + 1;
        }
    }

    static OdfTextList CreateList(
        OdfDocument document,
        OdfNode node,
        string partPath)
    {
        OdfTextList result = new OdfTextList();
        result.StyleName = Read(node, OdfNamespaces.Text, "style-name");
        result.ContinueNumbering = Read(
            node,
            OdfNamespaces.Text,
            "continue-numbering");
        result.ContinueListId = Read(
            node,
            OdfNamespaces.Text,
            "continue-list");
        result.XmlId = Read(node, OdfNamespaces.Xml, "id");
        if (result.XmlId.Length == 0)
        {
            result.XmlId = node.XmlId;
        }
        List<OdfTextListItem> items = new List<OdfTextListItem>();
        int index = 0;
        while (index < node.Children.Length)
        {
            OdfNode child = node.Children[index];
            if (IsElement(child, "list-header"))
            {
                result.Header = CreateItem(
                    document,
                    result,
                    child,
                    true);
            }
            else if (IsElement(child, "list-item"))
            {
                items.Add(CreateItem(
                    document,
                    result,
                    child,
                    false));
            }
            index = index + 1;
        }
        result.Items = items.ToArray();
        result.PartPath = partPath;
        result.Node = node;
        return result;
    }

    static OdfTextListItem CreateItem(
        OdfDocument document,
        OdfTextList list,
        OdfNode node,
        bool header)
    {
        OdfTextListItem result = new OdfTextListItem();
        result.IsHeader = header;
        result.StartValue = Read(
            node,
            OdfNamespaces.Text,
            "start-value");
        result.StyleOverride = Read(
            node,
            OdfNamespaces.Text,
            "style-override");
        result.TextBlocks = DirectItemBlocks(
            document.TextBlocks,
            node);
        result.List = list;
        result.Node = node;
        return result;
    }

    static OdfNumberedParagraph CreateNumberedParagraph(
        OdfDocument document,
        OdfNode node,
        string partPath)
    {
        OdfNumberedParagraph result = new OdfNumberedParagraph();
        result.ListId = Read(node, OdfNamespaces.Text, "list-id");
        result.Level = ParsePositiveInteger(
            Read(node, OdfNamespaces.Text, "level"),
            1);
        result.StyleName = Read(node, OdfNamespaces.Text, "style-name");
        result.ContinueNumbering = Read(
            node,
            OdfNamespaces.Text,
            "continue-numbering");
        result.StartValue = Read(
            node,
            OdfNamespaces.Text,
            "start-value");
        result.TextBlock = FirstContainedBlock(document.TextBlocks, node);
        result.PartPath = partPath;
        result.Node = node;
        return result;
    }

    static void ResolveParents(OdfTextList[] lists)
    {
        int index = 0;
        while (index < lists.Length)
        {
            OdfTextList list = lists[index];
            list.ParentList = null;
            list.ParentItem = null;
            if (list.Node != null)
            {
                OdfNode? current = list.Node.Parent;
                while (current != null && list.ParentList == null)
                {
                    OdfTextList? parent = FindListByNode(lists, current);
                    if (parent != null)
                    {
                        list.ParentList = parent;
                    }
                    if (list.ParentItem == null
                        && (IsElement(current, "list-item")
                            || IsElement(current, "list-header")))
                    {
                        list.ParentItem = FindItemByNode(lists, current);
                    }
                    current = current.Parent;
                }
            }
            list.Level = list.ParentList == null
                ? 1
                : list.ParentList.Level + 1;
            index = index + 1;
        }
        index = 0;
        while (index < lists.Length)
        {
            OdfTextList list = lists[index];
            AssignNestedLists(list.Header, lists);
            int itemIndex = 0;
            while (itemIndex < list.Items.Length)
            {
                AssignNestedLists(list.Items[itemIndex], lists);
                itemIndex = itemIndex + 1;
            }
            index = index + 1;
        }
    }

    static void AssignNestedLists(
        OdfTextListItem? item,
        OdfTextList[] lists)
    {
        if (item == null)
        {
            return;
        }
        List<OdfTextList> nested = new List<OdfTextList>();
        int index = 0;
        while (index < lists.Length)
        {
            if (lists[index].ParentItem == item)
            {
                nested.Add(lists[index]);
            }
            index = index + 1;
        }
        item.Lists = nested.ToArray();
    }

    static void ResolveStyles(
        OdfDocument document,
        OdfTextList[] lists)
    {
        int index = 0;
        while (index < lists.Length)
        {
            OdfTextList list = lists[index];
            string styleName = list.StyleName;
            if (styleName.Length == 0 && list.ParentList != null)
            {
                styleName = list.ParentList.StyleName;
            }
            list.Style = FindListStyle(document.ListStyles, styleName);
            if (list.Style == null && list.ParentList != null)
            {
                list.Style = list.ParentList.Style;
            }
            list.LevelStyle = FindLevelStyle(list.Style, list.Level);
            list.Kind = ListKind(list.Style, list.LevelStyle);
            ApplyItemLevel(list.Header, list.LevelStyle);
            int itemIndex = 0;
            while (itemIndex < list.Items.Length)
            {
                ApplyItemLevel(list.Items[itemIndex], list.LevelStyle);
                itemIndex = itemIndex + 1;
            }
            index = index + 1;
        }
    }

    static void ResolveContinuations(OdfTextList[] lists)
    {
        int index = 0;
        while (index < lists.Length)
        {
            OdfTextList list = lists[index];
            list.ContinuedList = null;
            list.HasBrokenContinuation = false;
            list.HasContinuationCycle = false;
            if (list.ContinueListId.Length > 0)
            {
                list.ContinuedList = FindUniqueListById(
                    lists,
                    list.ContinueListId,
                    list.PartPath);
                if (list.ContinuedList == null)
                {
                    list.HasBrokenContinuation = true;
                }
            }
            else if (list.ContinueNumbering == "true")
            {
                list.ContinuedList = FindPreviousCompatibleList(
                    lists,
                    index,
                    list);
                if (list.ContinuedList == null)
                {
                    list.HasBrokenContinuation = true;
                }
            }
            index = index + 1;
        }
    }

    static void MarkContinuationCycles(OdfTextList[] lists)
    {
        int index = 0;
        while (index < lists.Length)
        {
            List<OdfTextList> path = new List<OdfTextList>();
            OdfTextList? current = lists[index];
            while (current != null && !path.Contains(current))
            {
                path.Add(current);
                current = current.ContinuedList;
            }
            if (current != null)
            {
                int cycleStart = path.IndexOf(current);
                int cycleIndex = cycleStart;
                while (cycleIndex >= 0 && cycleIndex < path.Count)
                {
                    path[cycleIndex].HasContinuationCycle = true;
                    cycleIndex = cycleIndex + 1;
                }
            }
            index = index + 1;
        }
    }

    static void AssignListOrdinals(
        OdfTextList list,
        List<OdfTextList> visiting,
        List<OdfTextList> completed)
    {
        if (completed.Contains(list))
        {
            return;
        }
        if (visiting.Contains(list))
        {
            list.HasContinuationCycle = true;
            return;
        }
        visiting.Add(list);
        if (list.ContinuedList != null)
        {
            AssignListOrdinals(list.ContinuedList, visiting, completed);
        }
        int current = LevelStartValue(list.LevelStyle);
        if (list.ContinuedList != null
            && !list.HasContinuationCycle
            && !list.HasBrokenContinuation)
        {
            int continued = LastNumberedOrdinal(list.ContinuedList);
            if (continued > 0)
            {
                current = continued + 1;
            }
        }
        int itemIndex = 0;
        while (itemIndex < list.Items.Length)
        {
            OdfTextListItem item = list.Items[itemIndex];
            if (list.Kind == OdfTextListKind.Numbered
                || list.Kind == OdfTextListKind.Outline)
            {
                current = ParsePositiveInteger(item.StartValue, current);
                item.Ordinal = current;
                item.LabelText = FormatListLabel(list, item);
                current = current + 1;
            }
            else if (list.Kind == OdfTextListKind.Bullet)
            {
                item.Ordinal = 0;
                item.LabelText = list.LevelStyle == null
                    ? ""
                    : list.LevelStyle.BulletCharacter;
            }
            else if (list.Kind == OdfTextListKind.Image)
            {
                item.Ordinal = 0;
                item.LabelText = list.LevelStyle == null
                    ? ""
                    : list.LevelStyle.ImageHref;
            }
            else
            {
                item.Ordinal = 0;
                item.LabelText = "";
            }
            int nestedIndex = 0;
            while (nestedIndex < item.Lists.Length)
            {
                AssignListOrdinals(
                    item.Lists[nestedIndex],
                    visiting,
                    completed);
                nestedIndex = nestedIndex + 1;
            }
            itemIndex = itemIndex + 1;
        }
        visiting.Remove(list);
        if (!completed.Contains(list))
        {
            completed.Add(list);
        }
    }

    static void ResolveNumberedParagraphs(OdfDocument document)
    {
        int index = 0;
        while (index < document.NumberedParagraphs.Length)
        {
            OdfNumberedParagraph paragraph =
                document.NumberedParagraphs[index];
            paragraph.Style = FindListStyle(
                document.ListStyles,
                paragraph.StyleName);
            paragraph.LevelStyle = FindLevelStyle(
                paragraph.Style,
                paragraph.Level);
            paragraph.Kind = ListKind(
                paragraph.Style,
                paragraph.LevelStyle);
            int current = LevelStartValue(paragraph.LevelStyle);
            OdfNumberedParagraph? previous = FindPreviousNumberedParagraph(
                document.NumberedParagraphs,
                index,
                paragraph);
            if (previous != null
                && paragraph.ContinueNumbering != "false"
                && previous.Ordinal > 0)
            {
                current = previous.Ordinal + 1;
            }
            current = ParsePositiveInteger(paragraph.StartValue, current);
            if (paragraph.Kind == OdfTextListKind.Numbered
                || paragraph.Kind == OdfTextListKind.Outline)
            {
                paragraph.Ordinal = current;
                paragraph.LabelText = FormatNumberLabel(
                    paragraph.LevelStyle,
                    current);
            }
            else if (paragraph.Kind == OdfTextListKind.Bullet)
            {
                paragraph.Ordinal = 0;
                paragraph.LabelText = paragraph.LevelStyle == null
                    ? ""
                    : paragraph.LevelStyle.BulletCharacter;
            }
            else if (paragraph.Kind == OdfTextListKind.Image)
            {
                paragraph.Ordinal = 0;
                paragraph.LabelText = paragraph.LevelStyle == null
                    ? ""
                    : paragraph.LevelStyle.ImageHref;
            }
            index = index + 1;
        }
    }

    static string FormatListLabel(
        OdfTextList list,
        OdfTextListItem item)
    {
        OdfListLevel? level = list.LevelStyle;
        if (level == null)
        {
            return item.Ordinal.ToString();
        }
        int displayLevels = ParsePositiveInteger(level.DisplayLevels, 1);
        List<int> ordinals = new List<int>();
        CollectAncestorOrdinals(list, ordinals);
        ordinals.Add(item.Ordinal);
        int first = ordinals.Count - displayLevels;
        if (first < 0)
        {
            first = 0;
        }
        StringBuilder result = new StringBuilder();
        result.Append(level.NumberPrefix);
        int index = first;
        while (index < ordinals.Count)
        {
            if (index > first)
            {
                result.Append('.');
            }
            result.Append(FormatNumber(ordinals[index], level.NumberFormat));
            index = index + 1;
        }
        result.Append(level.NumberSuffix);
        return result.ToString();
    }

    static string FormatNumberLabel(OdfListLevel? level, int ordinal)
    {
        if (level == null)
        {
            return ordinal.ToString();
        }
        return level.NumberPrefix
            + FormatNumber(ordinal, level.NumberFormat)
            + level.NumberSuffix;
    }

    static void CollectAncestorOrdinals(
        OdfTextList list,
        List<int> ordinals)
    {
        if (list.ParentList != null)
        {
            CollectAncestorOrdinals(list.ParentList, ordinals);
        }
        if (list.ParentItem != null && list.ParentItem.Ordinal > 0)
        {
            ordinals.Add(list.ParentItem.Ordinal);
        }
    }

    static string FormatNumber(int value, string format)
    {
        if (format == "a")
        {
            return FormatAlphabetic(value, false);
        }
        if (format == "A")
        {
            return FormatAlphabetic(value, true);
        }
        if (format == "i")
        {
            return FormatRoman(value, false);
        }
        if (format == "I")
        {
            return FormatRoman(value, true);
        }
        return value.ToString();
    }

    static string FormatAlphabetic(int value, bool upper)
    {
        if (value <= 0)
        {
            return value.ToString();
        }
        StringBuilder result = new StringBuilder();
        int current = value;
        while (current > 0)
        {
            current = current - 1;
            char first = upper ? 'A' : 'a';
            result.Insert(0, (char)(first + current % 26));
            current = current / 26;
        }
        return result.ToString();
    }

    static string FormatRoman(int value, bool upper)
    {
        if (value <= 0 || value > 3999)
        {
            return value.ToString();
        }
        int[] values =
        [
            1000, 900, 500, 400, 100, 90, 50,
            40, 10, 9, 5, 4, 1
        ];
        string[] symbols =
        [
            "M", "CM", "D", "CD", "C", "XC", "L",
            "XL", "X", "IX", "V", "IV", "I"
        ];
        StringBuilder result = new StringBuilder();
        int current = value;
        int index = 0;
        while (index < values.Length)
        {
            while (current >= values[index])
            {
                result.Append(symbols[index]);
                current = current - values[index];
            }
            index = index + 1;
        }
        string text = result.ToString();
        return upper ? text : text.ToLowerInvariant();
    }

    static void ApplyListFields(OdfTextList list)
    {
        OdfNode node = list.Node ?? new OdfNode();
        WriteOptional(node, OdfNamespaces.Text, "style-name", list.StyleName);
        WriteOptional(
            node,
            OdfNamespaces.Text,
            "continue-numbering",
            list.ContinueNumbering);
        WriteOptional(
            node,
            OdfNamespaces.Text,
            "continue-list",
            list.ContinueListId);
        WriteOptional(node, OdfNamespaces.Xml, "id", list.XmlId);
        node.XmlId = list.XmlId;
    }

    static void ApplyItemFields(OdfTextListItem? item)
    {
        if (item == null || item.Node == null)
        {
            return;
        }
        WriteOptional(
            item.Node,
            OdfNamespaces.Text,
            "start-value",
            item.StartValue);
        WriteOptional(
            item.Node,
            OdfNamespaces.Text,
            "style-override",
            item.StyleOverride);
    }

    static void ApplyNumberedParagraphFields(
        OdfNumberedParagraph paragraph)
    {
        OdfNode node = paragraph.Node ?? new OdfNode();
        WriteOptional(node, OdfNamespaces.Text, "list-id", paragraph.ListId);
        WriteOptional(
            node,
            OdfNamespaces.Text,
            "level",
            paragraph.Level > 0 ? paragraph.Level.ToString() : "");
        WriteOptional(
            node,
            OdfNamespaces.Text,
            "style-name",
            paragraph.StyleName);
        WriteOptional(
            node,
            OdfNamespaces.Text,
            "continue-numbering",
            paragraph.ContinueNumbering);
        WriteOptional(
            node,
            OdfNamespaces.Text,
            "start-value",
            paragraph.StartValue);
    }

    static OdfTextBlock[] DirectItemBlocks(
        OdfTextBlock[] blocks,
        OdfNode itemNode)
    {
        List<OdfTextBlock> result = new List<OdfTextBlock>();
        int index = 0;
        while (index < blocks.Length)
        {
            OdfTextBlock block = blocks[index];
            if (block.Node != null
                && NearestListItem(block.Node) == itemNode)
            {
                result.Add(block);
            }
            index = index + 1;
        }
        return result.ToArray();
    }

    static OdfNode? NearestListItem(OdfNode node)
    {
        OdfNode? current = node.Parent;
        while (current != null)
        {
            if (IsElement(current, "list-item")
                || IsElement(current, "list-header"))
            {
                return current;
            }
            current = current.Parent;
        }
        return null;
    }

    static OdfTextBlock? FirstContainedBlock(
        OdfTextBlock[] blocks,
        OdfNode ancestor)
    {
        int index = 0;
        while (index < blocks.Length)
        {
            OdfTextBlock block = blocks[index];
            if (block.Node != null && IsDescendant(block.Node, ancestor))
            {
                return block;
            }
            index = index + 1;
        }
        return null;
    }

    static bool IsDescendant(OdfNode node, OdfNode ancestor)
    {
        OdfNode? current = node.Parent;
        while (current != null)
        {
            if (current == ancestor)
            {
                return true;
            }
            current = current.Parent;
        }
        return false;
    }

    static OdfTextList? FindListByNode(
        OdfTextList[] lists,
        OdfNode node)
    {
        int index = 0;
        while (index < lists.Length)
        {
            if (lists[index].Node == node)
            {
                return lists[index];
            }
            index = index + 1;
        }
        return null;
    }

    static OdfTextListItem? FindItemByNode(
        OdfTextList[] lists,
        OdfNode node)
    {
        int index = 0;
        while (index < lists.Length)
        {
            OdfTextList list = lists[index];
            if (list.Header != null && list.Header.Node == node)
            {
                return list.Header;
            }
            int itemIndex = 0;
            while (itemIndex < list.Items.Length)
            {
                if (list.Items[itemIndex].Node == node)
                {
                    return list.Items[itemIndex];
                }
                itemIndex = itemIndex + 1;
            }
            index = index + 1;
        }
        return null;
    }

    static OdfListStyle? FindListStyle(
        OdfListStyle[] styles,
        string name)
    {
        OdfListStyle? result = null;
        int count = 0;
        int index = 0;
        while (index < styles.Length)
        {
            if (styles[index].Name == name)
            {
                result = styles[index];
                count = count + 1;
            }
            index = index + 1;
        }
        return count == 1 ? result : null;
    }

    static OdfListLevel? FindLevelStyle(
        OdfListStyle? style,
        int level)
    {
        if (style == null)
        {
            return null;
        }
        string levelText = level.ToString();
        int index = 0;
        while (index < style.Levels.Length)
        {
            if (style.Levels[index].Level == levelText)
            {
                return style.Levels[index];
            }
            index = index + 1;
        }
        if (level > 0 && level <= style.Levels.Length)
        {
            return style.Levels[level - 1];
        }
        return null;
    }

    static OdfTextListKind ListKind(
        OdfListStyle? style,
        OdfListLevel? level)
    {
        if (style != null && style.Kind == OdfListStyleKind.Outline)
        {
            return OdfTextListKind.Outline;
        }
        if (level == null)
        {
            return OdfTextListKind.Unknown;
        }
        if (level.Kind == OdfListLevelKind.Bullet)
        {
            return OdfTextListKind.Bullet;
        }
        if (level.Kind == OdfListLevelKind.Image)
        {
            return OdfTextListKind.Image;
        }
        if (level.Kind == OdfListLevelKind.Outline)
        {
            return OdfTextListKind.Outline;
        }
        return OdfTextListKind.Numbered;
    }

    static void ApplyItemLevel(
        OdfTextListItem? item,
        OdfListLevel? level)
    {
        if (item != null)
        {
            item.LevelStyle = level;
            if (item.IsHeader)
            {
                item.Ordinal = 0;
                item.LabelText = "";
            }
        }
    }

    static OdfTextList? FindUniqueListById(
        OdfTextList[] lists,
        string id,
        string partPath)
    {
        OdfTextList? result = null;
        int count = 0;
        int index = 0;
        while (index < lists.Length)
        {
            OdfTextList list = lists[index];
            if (list.XmlId == id && list.PartPath == partPath)
            {
                result = list;
                count = count + 1;
            }
            index = index + 1;
        }
        return count == 1 ? result : null;
    }

    static OdfTextList? FindPreviousCompatibleList(
        OdfTextList[] lists,
        int beforeIndex,
        OdfTextList target)
    {
        int index = beforeIndex - 1;
        while (index >= 0)
        {
            OdfTextList candidate = lists[index];
            if (candidate.PartPath == target.PartPath
                && candidate.StyleName == target.StyleName
                && candidate.Level == target.Level
                && candidate.ParentList == target.ParentList)
            {
                return candidate;
            }
            index = index - 1;
        }
        return null;
    }

    static OdfNumberedParagraph? FindPreviousNumberedParagraph(
        OdfNumberedParagraph[] paragraphs,
        int beforeIndex,
        OdfNumberedParagraph target)
    {
        int index = beforeIndex - 1;
        while (index >= 0)
        {
            OdfNumberedParagraph candidate = paragraphs[index];
            bool sameList = target.ListId.Length > 0
                && candidate.ListId == target.ListId;
            bool implicitContinuation = target.ContinueNumbering == "true"
                && candidate.StyleName == target.StyleName;
            if (candidate.PartPath == target.PartPath
                && candidate.Level == target.Level
                && (sameList || implicitContinuation))
            {
                return candidate;
            }
            index = index - 1;
        }
        return null;
    }

    static int LastNumberedOrdinal(OdfTextList list)
    {
        int index = list.Items.Length - 1;
        while (index >= 0)
        {
            if (list.Items[index].Ordinal > 0)
            {
                return list.Items[index].Ordinal;
            }
            index = index - 1;
        }
        return 0;
    }

    static int LevelStartValue(OdfListLevel? level)
    {
        if (level == null)
        {
            return 1;
        }
        return ParsePositiveInteger(level.StartValue, 1);
    }

    static int ParsePositiveInteger(string text, int defaultValue)
    {
        if (text.Length == 0)
        {
            return defaultValue;
        }
        OdfLexicalValue parsed = OdfLexicalParser.ParseInteger(text);
        if (!parsed.IsValid
            || parsed.IntegerValue <= 0
            || parsed.IntegerValue > int.MaxValue)
        {
            return defaultValue;
        }
        return (int)parsed.IntegerValue;
    }

    static bool IsElement(OdfNode node, string localName)
    {
        return node.Kind == OdfNodeKind.Element
            && node.Name != null
            && node.Name.NamespaceName == OdfNamespaces.Text
            && node.Name.LocalName == localName;
    }

    static string Read(
        OdfNode node,
        string namespaceName,
        string localName)
    {
        return StyleDocumentModel.ReadAttribute(
            node,
            namespaceName,
            localName);
    }

    static void WriteOptional(
        OdfNode node,
        string namespaceName,
        string localName,
        string value)
    {
        if (value.Length == 0)
        {
            StyleDocumentModel.RemoveAttribute(
                node,
                namespaceName,
                localName);
        }
        else
        {
            string prefix = namespaceName == OdfNamespaces.Xml
                ? OdfNamespaces.XmlPrefix
                : OdfNamespaces.TextPrefix;
            StyleDocumentModel.WriteAttribute(
                node,
                namespaceName,
                localName,
                prefix,
                value);
        }
    }

    static void RemoveDeletedLists(
        OdfTextList[] lists,
        OdfNode[] originalNodes)
    {
        int index = 0;
        while (index < originalNodes.Length)
        {
            OdfNode node = originalNodes[index];
            bool retained = false;
            int listIndex = 0;
            while (listIndex < lists.Length && !retained)
            {
                retained = lists[listIndex] != null
                    && lists[listIndex].Node == node;
                listIndex = listIndex + 1;
            }
            if (!retained)
            {
                StyleDocumentModel.RemoveNodeFromParent(node);
            }
            index = index + 1;
        }
    }

    static void RemoveDeletedNumberedParagraphs(
        OdfNumberedParagraph[] paragraphs,
        OdfNode[] originalNodes)
    {
        int index = 0;
        while (index < originalNodes.Length)
        {
            OdfNode node = originalNodes[index];
            bool retained = false;
            int paragraphIndex = 0;
            while (paragraphIndex < paragraphs.Length && !retained)
            {
                retained = paragraphs[paragraphIndex] != null
                    && paragraphs[paragraphIndex].Node == node;
                paragraphIndex = paragraphIndex + 1;
            }
            if (!retained)
            {
                StyleDocumentModel.RemoveNodeFromParent(node);
            }
            index = index + 1;
        }
    }
}
