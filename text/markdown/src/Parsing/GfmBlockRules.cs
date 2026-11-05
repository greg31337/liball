using System;
using System.Collections.Generic;

namespace markdown;

internal class GfmBlockRules
{
    internal const string TableRuleName = "gfm.table";

    internal static void RegisterGfmBlockRules(MarkdownRuleList blockRules)
    {
        if (blockRules == null)
        {
            throw new ArgumentNullException("blockRules");
        }

        if (blockRules.Contains(TableRuleName))
        {
            return;
        }

        int paragraphIndex = blockRules.IndexOf(CommonMarkBlockRules.ParagraphRuleName);
        if (paragraphIndex < 0)
        {
            blockRules.Add(TableRuleName, CreateTableRule());
        }
        else
        {
            blockRules.Insert(paragraphIndex, TableRuleName, CreateTableRule());
        }
    }

    internal static MarkdownBlockRule CreateTableRule()
    {
        return new MarkdownBlockRule(TableRuleName, TryParseTable);
    }

    internal static bool TryParseTable(MarkdownBlockParseContext context)
    {
        if (context == null)
        {
            throw new ArgumentNullException("context");
        }

        MarkdownLine? headerLine = context.GetCurrentLine();
        if (headerLine == null || context.LineIndex + 1 >= context.GetLineCount())
        {
            return false;
        }

        MarkdownLine delimiterLine = context.Lines[context.LineIndex + 1];
        if (CommonMarkBlockRules.IsIndentedCodeLine(headerLine)
            || CommonMarkBlockRules.IsIndentedCodeLine(delimiterLine)
            || !LooksLikeTableRow(headerLine.Text))
        {
            return false;
        }

        List<string> headerCells = SplitTableRow(headerLine.Text);
        List<string> delimiterCells = SplitTableRow(delimiterLine.Text);
        if (headerCells.Count == 0
            || headerCells.Count != delimiterCells.Count
            || !IsDelimiterRow(delimiterCells))
        {
            return false;
        }

        MarkdownSourcePosition start = headerLine.ContentSpan.Start.Copy();
        MarkdownSourcePosition end = delimiterLine.ContentSpan.End.Copy();
        MarkdownNode table = MarkdownNode.Create(MarkdownNodeKind.Table, MarkdownSourceSpan.Create(start, end));
        table.SetAttribute("columns", headerCells.Count.ToString());

        MarkdownNode head = MarkdownNode.Create(MarkdownNodeKind.TableHead, headerLine.ContentSpan);
        MarkdownNode headerRow = CreateTableRow(headerLine, headerCells, delimiterCells);
        head.AppendChild(headerRow);
        table.AppendChild(head);

        MarkdownNode body = MarkdownNode.Create(MarkdownNodeKind.TableBody, delimiterLine.ContentSpan);
        int index = context.LineIndex + 2;
        while (index < context.GetLineCount())
        {
            MarkdownLine line = context.Lines[index];
            if (CommonMarkBlockRules.IsBlankLine(line)
                || CommonMarkBlockRules.IsIndentedCodeLine(line)
                || CommonMarkBlockRules.IsParagraphInterruptingBlockLine(line)
                || GenericAttributeRules.IsStandaloneAttributeLine(
                    line,
                    context.ParseContext.Pipeline))
            {
                break;
            }

            List<string> rowCells = NormalizeBodyCells(SplitTableRow(line.Text), headerCells.Count);
            MarkdownNode row = CreateTableRow(line, rowCells, delimiterCells);
            body.AppendChild(row);
            end = line.ContentSpan.End.Copy();
            index = index + 1;
        }

        table.Span = MarkdownSourceSpan.Create(start, end);
        body.Span = MarkdownSourceSpan.Create(delimiterLine.ContentSpan.Start, end);
        if (body.GetChildCount() > 0)
        {
            table.AppendChild(body);
        }

        while (context.LineIndex < index)
        {
            context.AdvanceLine();
        }

        context.ParseContext.Document.Root.AppendChild(table);
        context.NoteContentLine();
        return true;
    }

    internal static MarkdownNode CreateTableRow(MarkdownLine line, List<string> cells, List<string> delimiterCells)
    {
        if (line == null)
        {
            throw new ArgumentNullException("line");
        }

        if (cells == null)
        {
            throw new ArgumentNullException("cells");
        }

        if (delimiterCells == null)
        {
            throw new ArgumentNullException("delimiterCells");
        }

        MarkdownNode row = MarkdownNode.Create(MarkdownNodeKind.TableRow, line.ContentSpan);
        int index = 0;
        while (index < cells.Count)
        {
            MarkdownNode cell = MarkdownNode.Create(MarkdownNodeKind.TableCell, line.ContentSpan);
            cell.SetLiteral(cells[index]);
            string alignment = GetDelimiterAlignment(delimiterCells[index]);
            if (alignment.Length > 0)
            {
                cell.SetAttribute("align", alignment);
            }

            row.AppendChild(cell);
            index = index + 1;
        }

        return row;
    }

    internal static List<string> NormalizeBodyCells(List<string> cells, int columnCount)
    {
        if (cells == null)
        {
            throw new ArgumentNullException("cells");
        }

        if (columnCount < 1)
        {
            throw new ArgumentOutOfRangeException("columnCount");
        }

        List<string> normalized = new List<string>();
        int index = 0;
        while (index < columnCount)
        {
            if (index < cells.Count)
            {
                normalized.Add(cells[index]);
            }
            else
            {
                normalized.Add("");
            }

            index = index + 1;
        }

        return normalized;
    }

    internal static bool LooksLikeTableRow(string text)
    {
        if (text == null)
        {
            throw new ArgumentNullException("text");
        }

        return ContainsUnescapedPipe(text);
    }

    internal static List<string> SplitTableRow(string text)
    {
        if (text == null)
        {
            throw new ArgumentNullException("text");
        }

        string trimmed = text.Trim();
        if (trimmed.Length > 0 && trimmed[0] == '|')
        {
            trimmed = trimmed.Substring(1);
        }

        if (trimmed.Length > 0 && trimmed[trimmed.Length - 1] == '|')
        {
            trimmed = trimmed.Substring(0, trimmed.Length - 1);
        }

        List<string> cells = new List<string>();
        string current = "";
        int index = 0;
        while (index < trimmed.Length)
        {
            char character = trimmed[index];
            if (character == '\\' && index + 1 < trimmed.Length)
            {
                if (trimmed[index + 1] == '|')
                {
                    current = current + "|";
                }
                else
                {
                    current = current + character.ToString() + trimmed[index + 1].ToString();
                }

                index = index + 2;
            }
            else if (character == '|')
            {
                cells.Add(current.Trim());
                current = "";
                index = index + 1;
            }
            else
            {
                current = current + character.ToString();
                index = index + 1;
            }
        }

        cells.Add(current.Trim());
        return cells;
    }

    internal static bool IsDelimiterRow(List<string> cells)
    {
        if (cells == null)
        {
            throw new ArgumentNullException("cells");
        }

        int index = 0;
        while (index < cells.Count)
        {
            if (!IsDelimiterCell(cells[index]))
            {
                return false;
            }

            index = index + 1;
        }

        return cells.Count > 0;
    }

    internal static bool IsDelimiterCell(string text)
    {
        if (text == null)
        {
            throw new ArgumentNullException("text");
        }

        string trimmed = text.Trim();
        if (trimmed.Length == 0)
        {
            return false;
        }

        if (trimmed[0] == ':')
        {
            trimmed = trimmed.Substring(1);
        }

        if (trimmed.Length > 0 && trimmed[trimmed.Length - 1] == ':')
        {
            trimmed = trimmed.Substring(0, trimmed.Length - 1);
        }

        if (trimmed.Length < 1)
        {
            return false;
        }

        int index = 0;
        while (index < trimmed.Length)
        {
            if (trimmed[index] != '-')
            {
                return false;
            }

            index = index + 1;
        }

        return true;
    }

    internal static string GetDelimiterAlignment(string text)
    {
        if (text == null)
        {
            throw new ArgumentNullException("text");
        }

        string trimmed = text.Trim();
        bool left = trimmed.Length > 0 && trimmed[0] == ':';
        bool right = trimmed.Length > 0 && trimmed[trimmed.Length - 1] == ':';
        if (left && right)
        {
            return "center";
        }

        if (left)
        {
            return "left";
        }

        if (right)
        {
            return "right";
        }

        return "";
    }

    internal static bool ContainsUnescapedPipe(string text)
    {
        if (text == null)
        {
            throw new ArgumentNullException("text");
        }

        bool escaped = false;
        int index = 0;
        while (index < text.Length)
        {
            char character = text[index];
            if (escaped)
            {
                escaped = false;
            }
            else if (character == '\\')
            {
                escaped = true;
            }
            else if (character == '|')
            {
                return true;
            }

            index = index + 1;
        }

        return false;
    }

    internal static bool TryApplyTaskListMarker(MarkdownNode item, List<string> itemLines, MarkdownPipeline pipeline)
    {
        if (item == null)
        {
            throw new ArgumentNullException("item");
        }

        if (itemLines == null)
        {
            throw new ArgumentNullException("itemLines");
        }

        if (pipeline == null)
        {
            throw new ArgumentNullException("pipeline");
        }

        if (!pipeline.GetFeatures().GfmTaskLists || itemLines.Count == 0)
        {
            return false;
        }

        string line = itemLines[0];
        if (!IsTaskListMarkerLine(line))
        {
            return false;
        }

        item.SetKind(MarkdownNodeKind.TaskListItem);
        item.SetAttribute("checked", IsTaskListMarkerChecked(line) ? "true" : "false");
        itemLines[0] = RemoveTaskListMarker(line);
        return true;
    }

    internal static bool IsTaskListMarkerLine(string line)
    {
        if (line == null)
        {
            throw new ArgumentNullException("line");
        }

        int contentStart = GetTaskListMarkerContentStart(line);
        return contentStart >= 0;
    }

    internal static bool IsTaskListMarkerChecked(string line)
    {
        if (line == null)
        {
            throw new ArgumentNullException("line");
        }

        if (!IsTaskListMarkerLine(line))
        {
            return false;
        }

        return line[1] == 'x' || line[1] == 'X';
    }

    internal static string RemoveTaskListMarker(string line)
    {
        if (line == null)
        {
            throw new ArgumentNullException("line");
        }

        int contentStart = GetTaskListMarkerContentStart(line);
        if (contentStart < 0)
        {
            return line;
        }

        return line.Substring(contentStart);
    }

    internal static int GetTaskListMarkerContentStart(string line)
    {
        if (line == null)
        {
            throw new ArgumentNullException("line");
        }

        if (line.Length < 3
            || line[0] != '['
            || line[2] != ']'
            || (line[1] != ' ' && line[1] != 'x' && line[1] != 'X'))
        {
            return -1;
        }

        if (line.Length == 3)
        {
            return 3;
        }

        if (!MarkdownChar.IsWhitespace(line[3]))
        {
            return -1;
        }

        return 4;
    }
}
