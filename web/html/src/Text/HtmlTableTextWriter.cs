using System;
using System.Collections.Generic;
using System.Text;

namespace html;

internal class HtmlTableTextSlot
{
    internal string text;
    internal bool continuation;
    internal bool header;

    internal HtmlTableTextSlot(string textValue, bool continuationValue,
        bool headerValue)
    {
        if (textValue == null)
        {
            throw new ArgumentNullException("textValue");
        }

        this.text = textValue;
        this.continuation = continuationValue;
        this.header = headerValue;
    }

    internal string GetText()
    {
        return this.text;
    }

    internal bool IsContinuation()
    {
        return this.continuation;
    }

    internal bool IsHeader()
    {
        return this.header;
    }
}

internal class HtmlTableTextWriter
{
    internal HtmlTextWriter writer;
    internal HtmlCompiledTextOptions options;
    internal List<HtmlNode> rows;
    internal HtmlNode? caption;
    internal bool tableCellLimitReached;

    internal HtmlTableTextWriter(HtmlTextWriter writerValue,
        HtmlCompiledTextOptions optionsValue)
    {
        if (writerValue == null)
        {
            throw new ArgumentNullException("writerValue");
        }

        if (optionsValue == null)
        {
            throw new ArgumentNullException("optionsValue");
        }

        this.writer = writerValue;
        this.options = optionsValue;
        this.rows = new List<HtmlNode>();
        this.caption = null;
        this.tableCellLimitReached = false;
    }

    internal void Write(HtmlNode table, int depth)
    {
        if (table == null)
        {
            throw new ArgumentNullException("table");
        }

        if (this.options.GetTableStyle() == HtmlTextTableStyle.Omit)
        {
            return;
        }

        this.CollectChildren(table, depth, false);
        List<List<HtmlTableTextSlot?>> grid = this.BuildGrid();
        string captionText = this.caption == null
            ? "" : this.GetCellText(this.caption);
        string rendered = this.Render(grid, captionText);
        if (rendered.Length == 0)
        {
            return;
        }

        this.writer.WriteBlockBoundary();
        this.writer.WritePreformatted(rendered);
        this.writer.WriteBlockBoundary();
    }

    internal void CollectChildren(HtmlNode parent, int depth, bool nestedTable)
    {
        HtmlNode? container = parent.GetTemplateContentStorage();
        if (container == null)
        {
            container = parent;
        }

        HtmlNode[] children = container.GetChildrenStorage();
        int index = 0;
        while (index < children.Length)
        {
            HtmlNode child = children[index];
            if (!this.writer.TryVisitTableNode(depth + 1))
            {
                return;
            }

            HtmlNodeKind kind = child.GetKindStorage();
            HtmlCompiledTextRule? rule = kind == HtmlNodeKind.Element
                ? this.writer.FindTableRule(child) : null;
            bool suppressed = rule != null
                && rule.GetAction() != HtmlTextRuleAction.Include;
            bool excluded = rule != null
                && rule.GetAction() == HtmlTextRuleAction.Exclude;
            bool childNestedTable = nestedTable;
            if (IsHtmlElement(child, "table"))
            {
                childNestedTable = true;
            }

            bool hidden = IsHiddenOrInert(child);
            if (!hidden && !excluded && !nestedTable
                && IsHtmlElement(child, "caption")
                && this.caption == null)
            {
                this.caption = child;
            }

            if (!hidden && !excluded && !nestedTable
                && IsHtmlElement(child, "tr"))
            {
                this.rows.Add(child);
            }

            if ((kind == HtmlNodeKind.Element
                    || kind == HtmlNodeKind.Fragment)
                && !hidden && !suppressed)
            {
                this.CollectChildren(child, depth + 1, childNestedTable);
            }

            index = index + 1;
        }
    }

    internal List<List<HtmlTableTextSlot?>> BuildGrid()
    {
        List<List<HtmlTableTextSlot?>> grid =
            new List<List<HtmlTableTextSlot?>>();
        int rowIndex = 0;
        while (rowIndex < this.rows.Count)
        {
            grid.Add(new List<HtmlTableTextSlot?>());
            rowIndex = rowIndex + 1;
        }

        rowIndex = 0;
        while (rowIndex < this.rows.Count && !this.tableCellLimitReached)
        {
            HtmlCompiledTextRule? rowRule =
                this.writer.FindTableRule(this.rows[rowIndex]);
            if (rowRule != null
                && rowRule.GetAction() == HtmlTextRuleAction.Replace)
            {
                this.PlaceCell(grid, rowIndex, 0, 1, 1,
                    rowRule.GetReplacement(), false);
                rowIndex = rowIndex + 1;
                continue;
            }

            HtmlNode[] children = this.rows[rowIndex].GetChildrenStorage();
            int childIndex = 0;
            int column = 0;
            while (childIndex < children.Length
                && !this.tableCellLimitReached)
            {
                HtmlNode cell = children[childIndex];
                bool header = IsHtmlElement(cell, "th");
                if (header || IsHtmlElement(cell, "td"))
                {
                    int columnSpan = ParseColumnSpan(cell);
                    int rowSpan = ParseRowSpan(cell,
                        this.rows.Count - rowIndex);
                    column = FindFreeRange(grid[rowIndex], column,
                        columnSpan);
                    this.PlaceCell(grid, rowIndex, column, rowSpan,
                        columnSpan, this.GetCellText(cell), header);
                    column = column + columnSpan;
                }

                childIndex = childIndex + 1;
            }

            rowIndex = rowIndex + 1;
        }

        return grid;
    }

    internal void PlaceCell(List<List<HtmlTableTextSlot?>> grid, int row,
        int column, int rowSpan, int columnSpan, string text, bool header)
    {
        int rowOffset = 0;
        while (rowOffset < rowSpan && !this.tableCellLimitReached)
        {
            List<HtmlTableTextSlot?> targetRow = grid[row + rowOffset];
            EnsureWidth(targetRow, column + columnSpan);
            int columnOffset = 0;
            while (columnOffset < columnSpan
                && !this.tableCellLimitReached)
            {
                int targetColumn = column + columnOffset;
                if (targetRow[targetColumn] == null)
                {
                    if (!this.writer.TryConsumeTableCells(1))
                    {
                        this.tableCellLimitReached = true;
                    }
                    else
                    {
                        bool continuation = rowOffset != 0
                            || columnOffset != 0;
                        string slotText = continuation ? "" : text;
                        targetRow[targetColumn] = new HtmlTableTextSlot(
                            slotText, continuation, header);
                    }
                }

                columnOffset = columnOffset + 1;
            }

            rowOffset = rowOffset + 1;
        }
    }

    internal string Render(List<List<HtmlTableTextSlot?>> grid, string captionText)
    {
        StringBuilder builder = new StringBuilder();
        if (captionText.Length > 0)
        {
            builder.Append(captionText);
        }

        string body;
        HtmlTextTableStyle style = this.options.GetTableStyle();
        if (style == HtmlTextTableStyle.Tsv)
        {
            body = RenderTsv(grid);
        }
        else if (style == HtmlTextTableStyle.Grid)
        {
            body = RenderAlignedGrid(grid);
        }
        else
        {
            body = RenderReadingOrder(grid);
        }

        if (body.Length > 0)
        {
            if (builder.Length > 0)
            {
                builder.Append('\n');
            }

            builder.Append(body);
        }

        return builder.ToString();
    }

    internal static string RenderReadingOrder(
        List<List<HtmlTableTextSlot?>> grid)
    {
        StringBuilder builder = new StringBuilder();
        int rowIndex = 0;
        while (rowIndex < grid.Count)
        {
            List<HtmlTableTextSlot?> row = grid[rowIndex];
            bool wroteCell = false;
            int column = 0;
            while (column < row.Count)
            {
                HtmlTableTextSlot? slot = row[column];
                if (slot != null && !slot.IsContinuation()
                    && slot.GetText().Length > 0)
                {
                    if (wroteCell)
                    {
                        builder.Append(' ');
                    }

                    builder.Append(slot.GetText());
                    wroteCell = true;
                }

                column = column + 1;
            }

            if (wroteCell && HasLaterReadingRow(grid, rowIndex + 1))
            {
                builder.Append('\n');
            }

            rowIndex = rowIndex + 1;
        }

        return builder.ToString();
    }

    internal static bool HasLaterReadingRow(
        List<List<HtmlTableTextSlot?>> grid, int start)
    {
        int rowIndex = start;
        while (rowIndex < grid.Count)
        {
            List<HtmlTableTextSlot?> row = grid[rowIndex];
            int column = 0;
            while (column < row.Count)
            {
                HtmlTableTextSlot? slot = row[column];
                if (slot != null && !slot.IsContinuation()
                    && slot.GetText().Length > 0)
                {
                    return true;
                }

                column = column + 1;
            }

            rowIndex = rowIndex + 1;
        }

        return false;
    }

    internal static string RenderTsv(List<List<HtmlTableTextSlot?>> grid)
    {
        int columns = GetColumnCount(grid);
        if (columns == 0)
        {
            return "";
        }

        StringBuilder builder = new StringBuilder();
        int rowIndex = 0;
        while (rowIndex < grid.Count)
        {
            int column = 0;
            while (column < columns)
            {
                if (column > 0)
                {
                    builder.Append('\t');
                }

                HtmlTableTextSlot? slot = GetSlot(grid[rowIndex], column);
                if (slot != null)
                {
                    builder.Append(slot.GetText());
                }

                column = column + 1;
            }

            if (rowIndex + 1 < grid.Count)
            {
                builder.Append('\n');
            }

            rowIndex = rowIndex + 1;
        }

        return builder.ToString();
    }

    internal static string RenderAlignedGrid(
        List<List<HtmlTableTextSlot?>> grid)
    {
        int columns = GetColumnCount(grid);
        if (columns == 0)
        {
            return "";
        }

        int[] widths = GetColumnWidths(grid, columns);
        StringBuilder builder = new StringBuilder();
        int rowIndex = 0;
        while (rowIndex < grid.Count)
        {
            AppendAlignedRow(builder, grid[rowIndex], widths);
            if (rowIndex == 0 && HasHeader(grid[rowIndex]))
            {
                builder.Append('\n');
                AppendHeaderSeparator(builder, widths);
            }

            if (rowIndex + 1 < grid.Count)
            {
                builder.Append('\n');
            }

            rowIndex = rowIndex + 1;
        }

        return builder.ToString();
    }

    internal static int[] GetColumnWidths(
        List<List<HtmlTableTextSlot?>> grid, int columns)
    {
        int[] widths = new int[columns];
        int rowIndex = 0;
        while (rowIndex < grid.Count)
        {
            int column = 0;
            while (column < columns)
            {
                HtmlTableTextSlot? slot = GetSlot(grid[rowIndex], column);
                int length = slot == null ? 0 : slot.GetText().Length;
                if (length > widths[column])
                {
                    widths[column] = length;
                }

                column = column + 1;
            }

            rowIndex = rowIndex + 1;
        }

        int index = 0;
        while (index < widths.Length)
        {
            if (widths[index] < 1)
            {
                widths[index] = 1;
            }

            index = index + 1;
        }

        return widths;
    }

    internal static void AppendAlignedRow(StringBuilder builder,
        List<HtmlTableTextSlot?> row, int[] widths)
    {
        int column = 0;
        while (column < widths.Length)
        {
            if (column > 0)
            {
                builder.Append(" | ");
            }

            HtmlTableTextSlot? slot = GetSlot(row, column);
            string text = slot == null ? "" : slot.GetText();
            builder.Append(text);
            if (column + 1 < widths.Length)
            {
                AppendRepeated(builder, ' ', widths[column] - text.Length);
            }

            column = column + 1;
        }
    }

    internal static void AppendHeaderSeparator(StringBuilder builder, int[] widths)
    {
        int column = 0;
        while (column < widths.Length)
        {
            if (column > 0)
            {
                builder.Append("-+-");
            }

            AppendRepeated(builder, '-', widths[column]);
            column = column + 1;
        }
    }

    internal static void AppendRepeated(StringBuilder builder, char value, int count)
    {
        int index = 0;
        while (index < count)
        {
            builder.Append(value);
            index = index + 1;
        }
    }

    internal static bool HasHeader(List<HtmlTableTextSlot?> row)
    {
        int column = 0;
        while (column < row.Count)
        {
            HtmlTableTextSlot? slot = row[column];
            if (slot != null && !slot.IsContinuation() && slot.IsHeader())
            {
                return true;
            }

            column = column + 1;
        }

        return false;
    }

    internal static int GetColumnCount(List<List<HtmlTableTextSlot?>> grid)
    {
        int columns = 0;
        int rowIndex = 0;
        while (rowIndex < grid.Count)
        {
            if (grid[rowIndex].Count > columns)
            {
                columns = grid[rowIndex].Count;
            }

            rowIndex = rowIndex + 1;
        }

        return columns;
    }

    internal static HtmlTableTextSlot? GetSlot(List<HtmlTableTextSlot?> row,
        int column)
    {
        return column < row.Count ? row[column] : null;
    }

    internal static int FindFreeRange(List<HtmlTableTextSlot?> row, int start,
        int width)
    {
        int column = start;
        while (!IsFreeRange(row, column, width))
        {
            column = column + 1;
        }

        return column;
    }

    internal static bool IsFreeRange(List<HtmlTableTextSlot?> row, int start,
        int width)
    {
        int offset = 0;
        while (offset < width)
        {
            int column = start + offset;
            if (column < row.Count && row[column] != null)
            {
                return false;
            }

            offset = offset + 1;
        }

        return true;
    }

    internal static void EnsureWidth(List<HtmlTableTextSlot?> row, int width)
    {
        while (row.Count < width)
        {
            row.Add(null);
        }
    }

    internal static int ParseColumnSpan(HtmlNode cell)
    {
        string? value = cell.GetAttribute("colspan");
        return HtmlNumbers.ParseColumnSpan(value == null ? "" : value);
    }

    internal static int ParseRowSpan(HtmlNode cell, int remainingRows)
    {
        string? value = cell.GetAttribute("rowspan");
        int span = HtmlNumbers.ParseRowSpan(value == null ? "" : value);
        if (span == 0 || span > remainingRows)
        {
            return remainingRows;
        }

        return span;
    }

    internal string GetCellText(HtmlNode cell)
    {
        StringBuilder builder = new StringBuilder();
        this.AppendCellText(cell, builder, true);
        return HtmlTextContent.Normalize(builder.ToString());
    }

    internal void AppendCellText(HtmlNode node, StringBuilder builder, bool applyRule)
    {
        if (applyRule && node.GetKindStorage() == HtmlNodeKind.Element)
        {
            HtmlCompiledTextRule? rule = this.writer.FindTableRule(node);
            if (rule != null)
            {
                if (rule.GetAction() == HtmlTextRuleAction.Exclude)
                {
                    return;
                }

                if (rule.GetAction() == HtmlTextRuleAction.Replace)
                {
                    builder.Append(rule.GetReplacement());
                    return;
                }
            }
        }

        HtmlNode? container = node.GetTemplateContentStorage();
        if (container == null)
        {
            container = node;
        }

        HtmlNode[] children = container.GetChildrenStorage();
        int index = 0;
        while (index < children.Length)
        {
            HtmlNode child = children[index];
            HtmlNodeKind kind = child.GetKindStorage();
            if (kind == HtmlNodeKind.Text)
            {
                builder.Append(child.GetData());
            }
            else if (kind == HtmlNodeKind.Element
                && !IsHiddenOrInert(child))
            {
                string name = child.GetNamespaceStorage() == HtmlNamespace.Html
                    ? child.GetLocalNameStorage() : "";
                if (name == "br" || IsCellBlockElement(name))
                {
                    builder.Append(' ');
                }

                if (name == "img")
                {
                    AppendImageText(child, builder);
                }
                else
                {
                    this.AppendCellText(child, builder, true);
                }

                if (IsCellBlockElement(name))
                {
                    builder.Append(' ');
                }
            }

            index = index + 1;
        }
    }

    internal static void AppendImageText(HtmlNode image, StringBuilder builder)
    {
        string? label = image.GetAttribute("alt");
        if (label == null || label.Length == 0)
        {
            label = image.GetAttribute("title");
        }

        if (label != null)
        {
            builder.Append(label);
        }
    }

    internal static bool IsCellBlockElement(string name)
    {
        return name == "address" || name == "article" || name == "aside"
            || name == "blockquote" || name == "div" || name == "dl"
            || name == "dt" || name == "dd" || name == "fieldset"
            || name == "figcaption" || name == "figure" || name == "footer"
            || name == "form" || name == "h1" || name == "h2"
            || name == "h3" || name == "h4" || name == "h5"
            || name == "h6" || name == "header" || name == "li"
            || name == "main" || name == "nav" || name == "ol"
            || name == "p" || name == "section" || name == "table"
            || name == "ul";
    }

    internal static bool IsHiddenOrInert(HtmlNode node)
    {
        if (node.GetNamespaceStorage() != HtmlNamespace.Html)
        {
            return false;
        }

        if (node.HasAttribute("hidden") || node.HasAttribute("inert"))
        {
            return true;
        }

        if (EqualsTrimmedIgnoreCase(node.GetAttribute("aria-hidden"),
            "true"))
        {
            return true;
        }

        return node.GetLocalNameStorage() == "input"
            && EqualsTrimmedIgnoreCase(node.GetAttribute("type"), "hidden");
    }

    internal static bool EqualsTrimmedIgnoreCase(string? value, string expected)
    {
        if (value == null)
        {
            return false;
        }

        int start = 0;
        while (start < value.Length && Ascii.IsHtmlWhitespace(value[start]))
        {
            start = start + 1;
        }

        int end = value.Length;
        while (end > start && Ascii.IsHtmlWhitespace(value[end - 1]))
        {
            end = end - 1;
        }

        return Ascii.EqualsIgnoreCase(value.AsSpan(start, end - start),
            expected.AsSpan());
    }

    internal static bool IsHtmlElement(HtmlNode node, string name)
    {
        return node.GetKindStorage() == HtmlNodeKind.Element
            && node.GetNamespaceStorage() == HtmlNamespace.Html
            && node.GetLocalNameStorage() == name;
    }
}
