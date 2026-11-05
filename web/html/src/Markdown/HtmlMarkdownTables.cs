using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace html;

internal enum HtmlMarkdownTableAlignment
{
    None = 0,
    Left = 1,
    Center = 2,
    Right = 3
}

internal class HtmlMarkdownTableCell
{
    internal string text;
    internal HtmlMarkdownTableAlignment alignment;

    internal HtmlMarkdownTableCell(string textValue,
        HtmlMarkdownTableAlignment alignmentValue)
    {
        if (textValue == null)
        {
            throw new ArgumentNullException("textValue");
        }

        this.text = textValue;
        this.alignment = alignmentValue;
    }

    internal string GetText()
    {
        return this.text;
    }

    internal HtmlMarkdownTableAlignment GetAlignment()
    {
        return this.alignment;
    }
}

internal class HtmlMarkdownTableRow
{
    internal List<HtmlMarkdownTableCell> cells;

    internal HtmlMarkdownTableRow()
    {
        this.cells = new List<HtmlMarkdownTableCell>();
    }

    internal void AddCell(HtmlMarkdownTableCell cell)
    {
        if (cell == null)
        {
            throw new ArgumentNullException("cell");
        }

        this.cells.Add(cell);
    }

    internal int GetCellCount()
    {
        return this.cells.Count;
    }

    internal HtmlMarkdownTableCell GetCell(int index)
    {
        return this.cells[index];
    }
}

internal class HtmlMarkdownRenderedTableRow
{
    internal HtmlMarkdownTableRow row;
    internal bool hasSpans;

    internal HtmlMarkdownRenderedTableRow(HtmlMarkdownTableRow rowValue,
        bool hasSpansValue)
    {
        this.row = rowValue;
        this.hasSpans = hasSpansValue;
    }

    internal HtmlMarkdownTableRow GetRow()
    {
        return this.row;
    }

    internal bool HasSpans()
    {
        return this.hasSpans;
    }
}

internal static class HtmlMarkdownTables
{
    internal static string Render(HtmlNode table,
        HtmlCompiledMarkdownOptions options,
        CancellationToken cancellationToken, HtmlMarkdownLinks links)
    {
        ValidateTable(table);
        if (options == null)
        {
            throw new ArgumentNullException("options");
        }

        if (links == null)
        {
            throw new ArgumentNullException("links");
        }

        HtmlNode? caption = GetCaption(table);
        string captionText = caption == null ? ""
            : RenderNode(caption, options, cancellationToken, links);
        List<HtmlNode> rowNodes = CollectRows(table);
        List<HtmlMarkdownTableRow> rows =
            new List<HtmlMarkdownTableRow>();
        bool hasSpans = false;
        int index = 0;
        while (index < rowNodes.Count)
        {
            cancellationToken.ThrowIfCancellationRequested();
            HtmlMarkdownRenderedTableRow rendered = RenderRow(
                rowNodes[index], options, cancellationToken, links);
            HtmlMarkdownTableRow row = rendered.GetRow();
            hasSpans = hasSpans || rendered.HasSpans();
            if (row.GetCellCount() > 0)
            {
                rows.Add(row);
            }

            index = index + 1;
        }

        string body = options.GetEnableTables() && !hasSpans
            ? RenderGfm(rows, GetColumnAlignments(table, rows),
                options.GetNewLine())
            : RenderFallback(rows, options.GetNewLine());
        if (captionText.Length == 0)
        {
            return body;
        }

        if (body.Length == 0)
        {
            return captionText;
        }

        return captionText + options.GetNewLine()
            + options.GetNewLine() + body;
    }

    internal static List<HtmlNode> CollectRows(HtmlNode table)
    {
        List<HtmlNode> headers = new List<HtmlNode>();
        List<HtmlNode> bodies = new List<HtmlNode>();
        List<HtmlNode> footers = new List<HtmlNode>();
        HtmlNode[] children = table.GetChildrenStorage();
        int index = 0;
        while (index < children.Length)
        {
            HtmlNode child = children[index];
            if (!IsHiddenOrIgnored(child))
            {
                if (IsHtmlElement(child, "tr"))
                {
                    bodies.Add(child);
                }
                else if (IsHtmlElement(child, "thead"))
                {
                    AddDirectRows(child, headers);
                }
                else if (IsHtmlElement(child, "tbody"))
                {
                    AddDirectRows(child, bodies);
                }
                else if (IsHtmlElement(child, "tfoot"))
                {
                    AddDirectRows(child, footers);
                }
            }

            index = index + 1;
        }

        List<HtmlNode> rows = new List<HtmlNode>();
        AddAll(rows, headers);
        AddAll(rows, bodies);
        AddAll(rows, footers);
        return rows;
    }

    internal static void AddDirectRows(HtmlNode group, List<HtmlNode> rows)
    {
        HtmlNode[] children = group.GetChildrenStorage();
        int index = 0;
        while (index < children.Length)
        {
            if (IsHtmlElement(children[index], "tr")
                && !IsHiddenOrIgnored(children[index]))
            {
                rows.Add(children[index]);
            }

            index = index + 1;
        }
    }

    internal static void AddAll(List<HtmlNode> target, List<HtmlNode> values)
    {
        int index = 0;
        while (index < values.Count)
        {
            target.Add(values[index]);
            index = index + 1;
        }
    }

    internal static HtmlMarkdownRenderedTableRow RenderRow(HtmlNode row,
        HtmlCompiledMarkdownOptions options,
        CancellationToken cancellationToken, HtmlMarkdownLinks links)
    {
        HtmlMarkdownTableRow result = new HtmlMarkdownTableRow();
        bool hasSpans = false;
        HtmlNode[] children = row.GetChildrenStorage();
        int index = 0;
        while (index < children.Length)
        {
            HtmlNode cell = children[index];
            if ((IsHtmlElement(cell, "th") || IsHtmlElement(cell, "td"))
                && !IsHiddenOrIgnored(cell))
            {
                hasSpans = hasSpans || HasEffectiveSpan(cell);
                string text = RenderNode(cell, options,
                    cancellationToken, links);
                text = EscapePipes(HtmlTextContent.Normalize(text));
                result.AddCell(new HtmlMarkdownTableCell(text,
                    GetAlignment(cell)));
            }

            index = index + 1;
        }

        return new HtmlMarkdownRenderedTableRow(result, hasSpans);
    }

    internal static string RenderNode(HtmlNode node,
        HtmlCompiledMarkdownOptions options,
        CancellationToken cancellationToken, HtmlMarkdownLinks links)
    {
        HtmlMarkdownWriter writer = HtmlMarkdownWriter.Create(options,
            cancellationToken, links);
        writer.WriteNode(node);
        return writer.Complete();
    }

    internal static string RenderGfm(List<HtmlMarkdownTableRow> rows,
        HtmlMarkdownTableAlignment[] alignments, string newLine)
    {
        int columns = GetColumnCount(rows);
        if (columns == 0)
        {
            return "";
        }

        int[] widths = GetColumnWidths(rows, alignments, columns);
        StringBuilder builder = new StringBuilder();
        AppendRow(builder, rows[0], widths);
        builder.Append(newLine);
        AppendDelimiterRow(builder, widths, alignments);
        int rowIndex = 1;
        while (rowIndex < rows.Count)
        {
            builder.Append(newLine);
            AppendRow(builder, rows[rowIndex], widths);
            rowIndex = rowIndex + 1;
        }

        return builder.ToString();
    }

    internal static string RenderFallback(List<HtmlMarkdownTableRow> rows,
        string newLine)
    {
        StringBuilder builder = new StringBuilder();
        int rowIndex = 0;
        while (rowIndex < rows.Count)
        {
            HtmlMarkdownTableRow row = rows[rowIndex];
            if (rowIndex > 0)
            {
                builder.Append("  ");
                builder.Append(newLine);
            }

            int column = 0;
            while (column < row.GetCellCount())
            {
                if (column > 0)
                {
                    builder.Append(" \\| ");
                }

                builder.Append(row.GetCell(column).GetText());
                column = column + 1;
            }

            rowIndex = rowIndex + 1;
        }

        return builder.ToString();
    }

    internal static HtmlMarkdownTableAlignment[] GetColumnAlignments(HtmlNode table,
        List<HtmlMarkdownTableRow> rows)
    {
        int columns = GetColumnCount(rows);
        HtmlMarkdownTableAlignment[] result =
            GetColumnElementAlignments(table, columns);
        int column = 0;
        while (column < columns)
        {
            int rowIndex = 0;
            while (rowIndex < rows.Count)
            {
                if (column < rows[rowIndex].GetCellCount())
                {
                    HtmlMarkdownTableAlignment alignment =
                        rows[rowIndex].GetCell(column).GetAlignment();
                    if (alignment != HtmlMarkdownTableAlignment.None)
                    {
                        result[column] = alignment;
                        break;
                    }
                }

                rowIndex = rowIndex + 1;
            }

            column = column + 1;
        }

        return result;
    }

    internal static HtmlMarkdownTableAlignment[] GetColumnElementAlignments(
        HtmlNode table, int columns)
    {
        HtmlMarkdownTableAlignment[] result =
            new HtmlMarkdownTableAlignment[columns];
        HtmlNode[] children = table.GetChildrenStorage();
        int column = 0;
        int index = 0;
        while (index < children.Length && column < columns)
        {
            HtmlNode child = children[index];
            if (IsHtmlElement(child, "col"))
            {
                column = AddColumnAlignment(result, column, child,
                    GetAlignment(child));
            }
            else if (IsHtmlElement(child, "colgroup"))
            {
                HtmlMarkdownTableAlignment groupAlignment =
                    GetAlignment(child);
                HtmlNode[] groupChildren = child.GetChildrenStorage();
                int childIndex = 0;
                bool foundColumn = false;
                while (childIndex < groupChildren.Length
                    && column < columns)
                {
                    if (IsHtmlElement(groupChildren[childIndex], "col"))
                    {
                        foundColumn = true;
                        HtmlMarkdownTableAlignment alignment =
                            GetAlignment(groupChildren[childIndex]);
                        if (alignment == HtmlMarkdownTableAlignment.None)
                        {
                            alignment = groupAlignment;
                        }

                        column = AddColumnAlignment(result, column,
                            groupChildren[childIndex], alignment);
                    }

                    childIndex = childIndex + 1;
                }

                if (!foundColumn)
                {
                    column = AddColumnAlignment(result, column, child,
                        groupAlignment);
                }
            }

            index = index + 1;
        }

        return result;
    }

    internal static int AddColumnAlignment(
        HtmlMarkdownTableAlignment[] alignments, int start, HtmlNode column,
        HtmlMarkdownTableAlignment alignment)
    {
        string? spanValue = column.GetAttribute("span");
        int span = HtmlNumbers.ParseColumnSpan(
            spanValue == null ? "" : spanValue);
        int offset = 0;
        while (offset < span && start + offset < alignments.Length)
        {
            alignments[start + offset] = alignment;
            offset = offset + 1;
        }

        return start + span;
    }

    internal static int[] GetColumnWidths(List<HtmlMarkdownTableRow> rows,
        HtmlMarkdownTableAlignment[] alignments, int columns)
    {
        int[] widths = new int[columns];
        int column = 0;
        while (column < columns)
        {
            widths[column] = GetMinimumWidth(alignments[column]);
            int rowIndex = 0;
            while (rowIndex < rows.Count)
            {
                if (column < rows[rowIndex].GetCellCount())
                {
                    int length = rows[rowIndex].GetCell(column)
                        .GetText().Length;
                    if (length > widths[column])
                    {
                        widths[column] = length;
                    }
                }

                rowIndex = rowIndex + 1;
            }

            column = column + 1;
        }

        return widths;
    }

    internal static void AppendRow(StringBuilder builder, HtmlMarkdownTableRow row,
        int[] widths)
    {
        builder.Append("| ");
        int column = 0;
        while (column < widths.Length)
        {
            string text = column < row.GetCellCount()
                ? row.GetCell(column).GetText() : "";
            builder.Append(text);
            AppendRepeated(builder, ' ', widths[column] - text.Length);
            builder.Append(" |");
            if (column + 1 < widths.Length)
            {
                builder.Append(' ');
            }

            column = column + 1;
        }
    }

    internal static void AppendDelimiterRow(StringBuilder builder, int[] widths,
        HtmlMarkdownTableAlignment[] alignments)
    {
        builder.Append("| ");
        int column = 0;
        while (column < widths.Length)
        {
            HtmlMarkdownTableAlignment alignment = alignments[column];
            int colons = alignment == HtmlMarkdownTableAlignment.Center
                ? 2 : alignment == HtmlMarkdownTableAlignment.Left
                    || alignment == HtmlMarkdownTableAlignment.Right ? 1 : 0;
            if (alignment == HtmlMarkdownTableAlignment.Left
                || alignment == HtmlMarkdownTableAlignment.Center)
            {
                builder.Append(':');
            }

            AppendRepeated(builder, '-', widths[column] - colons);
            if (alignment == HtmlMarkdownTableAlignment.Right
                || alignment == HtmlMarkdownTableAlignment.Center)
            {
                builder.Append(':');
            }

            builder.Append(" |");
            if (column + 1 < widths.Length)
            {
                builder.Append(' ');
            }

            column = column + 1;
        }
    }

    internal static int GetMinimumWidth(HtmlMarkdownTableAlignment alignment)
    {
        if (alignment == HtmlMarkdownTableAlignment.Center)
        {
            return 5;
        }

        return alignment == HtmlMarkdownTableAlignment.Left
                || alignment == HtmlMarkdownTableAlignment.Right
            ? 4 : 3;
    }

    internal static int GetColumnCount(List<HtmlMarkdownTableRow> rows)
    {
        int result = 0;
        int index = 0;
        while (index < rows.Count)
        {
            if (rows[index].GetCellCount() > result)
            {
                result = rows[index].GetCellCount();
            }

            index = index + 1;
        }

        return result;
    }

    internal static bool HasEffectiveSpan(HtmlNode cell)
    {
        string? columnValue = cell.GetAttribute("colspan");
        string? rowValue = cell.GetAttribute("rowspan");
        int columnSpan = HtmlNumbers.ParseColumnSpan(
            columnValue == null ? "" : columnValue);
        int rowSpan = HtmlNumbers.ParseRowSpan(
            rowValue == null ? "" : rowValue);
        return columnSpan != 1 || rowSpan != 1;
    }

    internal static HtmlMarkdownTableAlignment GetAlignment(HtmlNode node)
    {
        HtmlMarkdownTableAlignment style = GetStyleAlignment(
            node.GetAttribute("style"));
        if (style != HtmlMarkdownTableAlignment.None)
        {
            return style;
        }

        return ParseAlignment(node.GetAttribute("align"));
    }

    internal static HtmlMarkdownTableAlignment GetStyleAlignment(
        string? value)
    {
        if (value == null)
        {
            return HtmlMarkdownTableAlignment.None;
        }

        HtmlMarkdownTableAlignment result =
            HtmlMarkdownTableAlignment.None;
        int start = 0;
        while (start <= value.Length)
        {
            int end = start;
            while (end < value.Length && value[end] != ';')
            {
                end = end + 1;
            }

            int colon = start;
            while (colon < end && value[colon] != ':')
            {
                colon = colon + 1;
            }

            if (colon < end)
            {
                string property = Trim(value, start, colon)
                    .ToLowerInvariant();
                if (property == "text-align")
                {
                    HtmlMarkdownTableAlignment parsed = ParseAlignment(
                        Trim(value, colon + 1, end));
                    if (parsed != HtmlMarkdownTableAlignment.None)
                    {
                        result = parsed;
                    }
                }
            }

            if (end == value.Length)
            {
                break;
            }

            start = end + 1;
        }

        return result;
    }

    internal static HtmlMarkdownTableAlignment ParseAlignment(string? value)
    {
        if (value == null)
        {
            return HtmlMarkdownTableAlignment.None;
        }

        string normalized = value.Trim().ToLowerInvariant();
        if (normalized.EndsWith("!important", StringComparison.Ordinal))
        {
            normalized = normalized.Substring(0,
                normalized.Length - 10).TrimEnd();
        }
        if (normalized == "left" || normalized == "start")
        {
            return HtmlMarkdownTableAlignment.Left;
        }

        if (normalized == "center")
        {
            return HtmlMarkdownTableAlignment.Center;
        }

        return normalized == "right" || normalized == "end"
            ? HtmlMarkdownTableAlignment.Right
            : HtmlMarkdownTableAlignment.None;
    }

    internal static string EscapePipes(string value)
    {
        StringBuilder builder = new StringBuilder(value.Length);
        int backslashes = 0;
        int index = 0;
        while (index < value.Length)
        {
            char current = value[index];
            if (current == '|')
            {
                if (backslashes % 2 == 0)
                {
                    builder.Append('\\');
                }

                builder.Append('|');
                backslashes = 0;
            }
            else
            {
                builder.Append(current);
                backslashes = current == '\\' ? backslashes + 1 : 0;
            }

            index = index + 1;
        }

        return builder.ToString();
    }

    internal static string Trim(string value, int start, int end)
    {
        while (start < end && UnicodeTools.IsUnicodeWhitespace(value[start]))
        {
            start = start + 1;
        }

        while (end > start
            && UnicodeTools.IsUnicodeWhitespace(value[end - 1]))
        {
            end = end - 1;
        }

        return value.Substring(start, end - start);
    }

    internal static HtmlNode? GetCaption(HtmlNode table)
    {
        HtmlNode[] children = table.GetChildrenStorage();
        int index = 0;
        while (index < children.Length)
        {
            if (IsHtmlElement(children[index], "caption")
                && !IsHiddenOrIgnored(children[index]))
            {
                return children[index];
            }

            index = index + 1;
        }

        return null;
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

    internal static bool IsHiddenOrIgnored(HtmlNode node)
    {
        if (node.GetNamespaceStorage() != HtmlNamespace.Html)
        {
            return false;
        }

        return node.HasAttribute("hidden") || node.HasAttribute("inert")
            || EqualsTrimmedIgnoreCase(node.GetAttribute("aria-hidden"),
                "true");
    }

    internal static bool EqualsTrimmedIgnoreCase(string? value, string expected)
    {
        if (value == null)
        {
            return false;
        }

        return Ascii.EqualsIgnoreCase(value.Trim().AsSpan(),
            expected.AsSpan());
    }

    internal static bool IsHtmlElement(HtmlNode node, string name)
    {
        return node.GetKindStorage() == HtmlNodeKind.Element
            && node.GetNamespaceStorage() == HtmlNamespace.Html
            && node.GetLocalNameStorage() == name;
    }

    internal static void ValidateTable(HtmlNode table)
    {
        if (table == null)
        {
            throw new ArgumentNullException("table");
        }

        if (!IsHtmlElement(table, "table"))
        {
            throw new ArgumentException(
                "Markdown table rendering requires an HTML table element.",
                "table");
        }
    }
}
