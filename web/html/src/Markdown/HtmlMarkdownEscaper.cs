using System;
using System.Collections.Generic;
using System.Text;

namespace html;

internal enum HtmlMarkdownContextKind
{
    Text = 0,
    Emphasis = 1,
    Strong = 2,
    CodeSpan = 3,
    CodeBlock = 4,
    LinkText = 5,
    LinkDestination = 6,
    LinkTitle = 7,
    TableCell = 8,
    RawHtml = 9
}

internal class HtmlMarkdownContextFrame
{
    internal HtmlMarkdownContextKind kind;
    internal string delimiter;

    internal HtmlMarkdownContextFrame(HtmlMarkdownContextKind kindValue,
        string delimiterValue)
    {
        ValidateKind(kindValue);
        if (delimiterValue == null)
        {
            throw new ArgumentNullException("delimiterValue");
        }

        this.kind = kindValue;
        this.delimiter = delimiterValue;
    }

    internal HtmlMarkdownContextKind GetKind()
    {
        return this.kind;
    }

    internal string GetDelimiter()
    {
        return this.delimiter;
    }

    internal static void ValidateKind(HtmlMarkdownContextKind value)
    {
        if ((int)value < (int)HtmlMarkdownContextKind.Text
            || (int)value > (int)HtmlMarkdownContextKind.RawHtml)
        {
            throw new ArgumentOutOfRangeException("kindValue", (int)value,
                "Unknown Markdown output context.");
        }
    }
}

internal class HtmlMarkdownContextStack
{
    internal HtmlCompiledMarkdownOptions options;
    internal List<HtmlMarkdownContextFrame> frames;
    internal bool lineStart;

    internal HtmlMarkdownContextStack(HtmlCompiledMarkdownOptions optionsValue)
    {
        if (optionsValue == null)
        {
            throw new ArgumentNullException("optionsValue");
        }

        this.options = optionsValue;
        this.frames = new List<HtmlMarkdownContextFrame>();
        this.frames.Add(new HtmlMarkdownContextFrame(
            HtmlMarkdownContextKind.Text, ""));
        this.lineStart = true;
    }

    internal void Push(HtmlMarkdownContextKind kind)
    {
        this.PushWithDelimiter(kind, GetDefaultDelimiter(kind));
    }

    internal void PushWithDelimiter(HtmlMarkdownContextKind kind, string delimiter)
    {
        this.frames.Add(new HtmlMarkdownContextFrame(kind, delimiter));
    }

    internal HtmlMarkdownContextKind Pop()
    {
        if (this.frames.Count == 1)
        {
            throw new InvalidOperationException(
                "The root Markdown context cannot be removed.");
        }

        int index = this.frames.Count - 1;
        HtmlMarkdownContextKind kind = this.frames[index].GetKind();
        this.frames.RemoveAt(index);
        return kind;
    }

    internal HtmlMarkdownContextKind GetCurrentKind()
    {
        return this.frames[this.frames.Count - 1].GetKind();
    }

    internal string GetCurrentDelimiter()
    {
        return this.frames[this.frames.Count - 1].GetDelimiter();
    }

    internal int GetDepth()
    {
        return this.frames.Count;
    }

    internal bool IsAtLineStart()
    {
        return this.lineStart;
    }

    internal void SetLineStart(bool value)
    {
        this.lineStart = value;
    }

    internal string Escape(string value)
    {
        if (value == null)
        {
            throw new ArgumentNullException("value");
        }

        string escaped = HtmlMarkdownEscaper.Escape(value,
            this.GetCurrentKind(), this.GetCurrentDelimiter(),
            this.lineStart, this.options);
        this.ObserveLiteral(escaped);
        return escaped;
    }

    internal void ObserveLiteral(string value)
    {
        if (value == null)
        {
            throw new ArgumentNullException("value");
        }

        int index = 0;
        while (index < value.Length)
        {
            char current = value[index];
            if (current == '\r' || current == '\n')
            {
                this.lineStart = true;
            }
            else if (current != ' ' && current != '\t')
            {
                this.lineStart = false;
            }

            index = index + 1;
        }
    }

    internal static string GetDefaultDelimiter(HtmlMarkdownContextKind kind)
    {
        if (kind == HtmlMarkdownContextKind.Emphasis)
        {
            return "*";
        }

        if (kind == HtmlMarkdownContextKind.Strong)
        {
            return "**";
        }

        if (kind == HtmlMarkdownContextKind.CodeSpan)
        {
            return "`";
        }

        if (kind == HtmlMarkdownContextKind.CodeBlock)
        {
            return "```";
        }

        if (kind == HtmlMarkdownContextKind.LinkTitle)
        {
            return "\"";
        }

        return "";
    }
}

internal static class HtmlMarkdownEscaper
{
    internal static string EscapeHtmlLiteral(string value)
    {
        if (value == null)
        {
            throw new ArgumentNullException("value");
        }

        return EscapeHtml(value);
    }

    internal static string Escape(string value,
        HtmlMarkdownContextKind kind, string delimiter,
        bool lineStart, HtmlCompiledMarkdownOptions options)
    {
        if (value == null)
        {
            throw new ArgumentNullException("value");
        }

        if (delimiter == null)
        {
            throw new ArgumentNullException("delimiter");
        }

        if (options == null)
        {
            throw new ArgumentNullException("options");
        }

        ValidateKind(kind);
        if (kind == HtmlMarkdownContextKind.CodeSpan
            || kind == HtmlMarkdownContextKind.CodeBlock)
        {
            return value;
        }

        if (kind == HtmlMarkdownContextKind.LinkDestination)
        {
            return EscapeLinkDestination(value);
        }

        if (kind == HtmlMarkdownContextKind.LinkTitle)
        {
            return EscapeLinkTitle(value, delimiter);
        }

        if (kind == HtmlMarkdownContextKind.RawHtml)
        {
            return options.GetEnableTrustedRawHtml()
                ? value : EscapeHtml(value);
        }

        return EscapeMarkdownText(value, kind, lineStart, options);
    }

    internal static string EscapeMarkdownText(string value,
        HtmlMarkdownContextKind kind, bool startsAtLineStart,
        HtmlCompiledMarkdownOptions options)
    {
        StringBuilder builder = new StringBuilder(value.Length);
        bool lineStart = startsAtLineStart;
        int index = 0;
        while (index < value.Length)
        {
            char current = value[index];
            if (current == '\r' || current == '\n')
            {
                builder.Append(current);
                lineStart = true;
                index = index + 1;
                continue;
            }

            if (lineStart && (current == ' ' || current == '\t'))
            {
                builder.Append(current);
                index = index + 1;
                continue;
            }

            if (lineStart && current >= '0' && current <= '9')
            {
                int punctuation = GetOrderedMarkerPunctuation(value, index);
                if (punctuation >= 0)
                {
                    builder.Append(value, index, punctuation - index);
                    builder.Append('\\');
                    builder.Append(value[punctuation]);
                    index = punctuation + 1;
                    lineStart = false;
                    continue;
                }
            }

            bool escape = ShouldEscape(current, value, index, kind,
                lineStart, options);
            if (escape)
            {
                builder.Append('\\');
            }

            builder.Append(current);
            if (current != ' ' && current != '\t')
            {
                lineStart = false;
            }

            index = index + 1;
        }

        return builder.ToString();
    }

    internal static bool ShouldEscape(char value, string source, int index,
        HtmlMarkdownContextKind kind, bool lineStart,
        HtmlCompiledMarkdownOptions options)
    {
        if (value == '\\' || value == '`' || value == '*'
            || value == '_' || value == '[' || value == ']'
            || value == '<' || value == '&')
        {
            return true;
        }

        if (value == '!' && index + 1 < source.Length
            && source[index + 1] == '[')
        {
            return true;
        }

        if (kind == HtmlMarkdownContextKind.TableCell && value == '|')
        {
            return true;
        }

        if (value == '~' && options.GetEnableStrikethrough())
        {
            return true;
        }

        if (!lineStart)
        {
            return false;
        }

        if (value == '#' || value == '>' || value == '-'
            || value == '+' || value == '=' || value == '~')
        {
            return true;
        }

        return value == ':' && options.GetEnableDefinitionLists();
    }

    internal static int GetOrderedMarkerPunctuation(string value, int start)
    {
        int index = start;
        int digits = 0;
        while (index < value.Length && digits < 10
            && value[index] >= '0' && value[index] <= '9')
        {
            index = index + 1;
            digits = digits + 1;
        }

        if (digits == 0 || digits > 9 || index >= value.Length
            || value[index] != '.' && value[index] != ')')
        {
            return -1;
        }

        int after = index + 1;
        if (after < value.Length && value[after] != ' '
            && value[after] != '\t' && value[after] != '\r'
            && value[after] != '\n')
        {
            return -1;
        }

        return index;
    }

    internal static string EscapeLinkDestination(string value)
    {
        StringBuilder builder = new StringBuilder(value.Length);
        int index = 0;
        while (index < value.Length)
        {
            char current = value[index];
            if (current == '\\' || current == '(' || current == ')')
            {
                builder.Append('\\');
                builder.Append(current);
            }
            else if (current == ' ')
            {
                builder.Append("%20");
            }
            else if (current == '\t')
            {
                builder.Append("%09");
            }
            else if (current == '\r')
            {
                builder.Append("%0D");
            }
            else if (current == '\n')
            {
                builder.Append("%0A");
            }
            else if (current == '<')
            {
                builder.Append("%3C");
            }
            else if (current == '>')
            {
                builder.Append("%3E");
            }
            else if (current == '"')
            {
                builder.Append("%22");
            }
            else
            {
                builder.Append(current);
            }

            index = index + 1;
        }

        return builder.ToString();
    }

    internal static string EscapeLinkTitle(string value, string delimiter)
    {
        char closing = delimiter == "(" ? ')' : delimiter.Length == 0
            ? '"' : delimiter[delimiter.Length - 1];
        StringBuilder builder = new StringBuilder(value.Length);
        int index = 0;
        while (index < value.Length)
        {
            char current = value[index];
            if (current == '\\' || current == closing)
            {
                builder.Append('\\');
            }

            builder.Append(current);
            index = index + 1;
        }

        return builder.ToString();
    }

    internal static string EscapeHtml(string value)
    {
        StringBuilder builder = new StringBuilder(value.Length);
        int index = 0;
        while (index < value.Length)
        {
            char current = value[index];
            if (current == '&')
            {
                builder.Append("&amp;");
            }
            else if (current == '<')
            {
                builder.Append("&lt;");
            }
            else if (current == '>')
            {
                builder.Append("&gt;");
            }
            else if (current == '"')
            {
                builder.Append("&quot;");
            }
            else
            {
                builder.Append(current);
            }

            index = index + 1;
        }

        return builder.ToString();
    }

    internal static void ValidateKind(HtmlMarkdownContextKind value)
    {
        if ((int)value < (int)HtmlMarkdownContextKind.Text
            || (int)value > (int)HtmlMarkdownContextKind.RawHtml)
        {
            throw new ArgumentOutOfRangeException("kind", (int)value,
                "Unknown Markdown escaping context.");
        }
    }
}
