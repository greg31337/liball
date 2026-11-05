using System;
using System.Collections.Generic;
using System.Text;

namespace email;

internal class EmailTextAnalyzer
{
    internal static EmailTextAnalysisResult Analyze(string value, EmailTextAnalysisOptions options)
    {
        if (value.Length > options.GetMaximumCharacters()) throw new EmailLimitException("MaxTextAnalysisCharacters", options.GetMaximumCharacters(), value.Length);
        List<EmailTextAnalysisLine> lines = SplitLines(value, options.GetMaximumLines());
        int replyLine = FindReplyStart(lines);
        int signatureLine = FindSignatureStart(lines, replyLine);
        StringBuilder visible = new StringBuilder();
        StringBuilder quoted = new StringBuilder();
        StringBuilder signature = new StringBuilder();
        int quotedCount = 0;
        int signatureCount = 0;
        int firstQuotedOffset = -1;
        int index = 0;
        while (index < lines.Count)
        {
            EmailTextAnalysisLine line = lines[index];
            bool quotedLine = replyLine >= 0 && index >= replyLine || IsQuotedLine(line.GetContent());
            bool signatureLineSelected = !quotedLine && signatureLine >= 0 && index >= signatureLine;
            if (quotedLine)
            {
                quoted.Append(line.GetText());
                quotedCount++;
                if (firstQuotedOffset < 0) firstQuotedOffset = line.GetOffset();
            }
            else if (signatureLineSelected)
            {
                signature.Append(line.GetText());
                signatureCount++;
            }
            else
            {
                visible.Append(line.GetText());
            }

            index++;
        }

        string visibleText = TrimRegion(visible.ToString());
        string quotedText = TrimRegion(quoted.ToString());
        string signatureText = TrimRegion(signature.ToString());
        StringBuilder snippetSource = new StringBuilder(visibleText);
        if (options.GetIncludeSignatureInSnippet() && signatureText.Length > 0) AppendRegion(snippetSource, signatureText);
        if (options.GetIncludeQuotedTextInSnippet() && quotedText.Length > 0) AppendRegion(snippetSource, quotedText);
        EmailTextSnippet snippet = CreateSnippet(snippetSource.ToString(), options.GetMaximumSnippetCharacters());
        EmailIssueList issues = new EmailIssueList();
        if (firstQuotedOffset >= 0) issues.Add(new EmailIssueOperations().CreateWithSeverityAndCodeAndDescription(EmailIssueSeverity.Information, EmailIssueCode.TextQuotedReplyDetected, "Quoted reply text was identified using conventional line markers."));
        int signatureOffset = signatureLine >= 0 ? lines[signatureLine].GetOffset() : -1;
        if (signatureOffset >= 0) issues.Add(new EmailIssueOperations().CreateWithSeverityAndCodeAndDescription(EmailIssueSeverity.Information, EmailIssueCode.TextSignatureDetected, "A signature region was identified using a conventional separator or sign-off marker."));
        return new EmailTextAnalysisResult(visibleText, quotedText, signatureText, snippet.GetValue(), lines.Count, quotedCount, signatureCount, firstQuotedOffset, signatureOffset, snippet.WasTruncated(), issues);
    }

    static List<EmailTextAnalysisLine> SplitLines(string value, int maximumLines)
    {
        List<EmailTextAnalysisLine> lines = new List<EmailTextAnalysisLine>();
        int index = 0;
        while (index < value.Length)
        {
            if (lines.Count >= maximumLines) throw new EmailLimitException("MaxTextAnalysisLineCount", maximumLines, (long)lines.Count + 1);
            int start = index;
            while (index < value.Length && value[index] != '\r' && value[index] != '\n') index++;
            string content = value.Substring(start, index - start);
            if (index < value.Length && value[index] == '\r')
            {
                index++;
                if (index < value.Length && value[index] == '\n') index++;
            }
            else if (index < value.Length)
            {
                index++;
            }

            lines.Add(new EmailTextAnalysisLine(content, value.Substring(start, index - start), start));
        }

        if (value.Length == 0) return lines;
        if ((value[value.Length - 1] == '\r' || value[value.Length - 1] == '\n') && lines.Count >= maximumLines) throw new EmailLimitException("MaxTextAnalysisLineCount", maximumLines, (long)lines.Count + 1);
        if (value[value.Length - 1] == '\r' || value[value.Length - 1] == '\n') lines.Add(new EmailTextAnalysisLine("", "", value.Length));
        return lines;
    }

    static int FindReplyStart(List<EmailTextAnalysisLine> lines)
    {
        int index = 0;
        while (index < lines.Count)
        {
            string value = lines[index].GetContent().Trim();
            if (string.Equals(value, "-----Original Message-----", StringComparison.OrdinalIgnoreCase)) return index;
            if (string.Equals(value, "Begin forwarded message:", StringComparison.OrdinalIgnoreCase)) return index;
            if (value.StartsWith("On ", StringComparison.OrdinalIgnoreCase) && value.EndsWith(" wrote:", StringComparison.OrdinalIgnoreCase)) return index;
            index++;
        }

        return -1;
    }

    static int FindSignatureStart(List<EmailTextAnalysisLine> lines, int replyLine)
    {
        int end = replyLine < 0 ? lines.Count : replyLine;
        int index = 0;
        while (index < end)
        {
            string value = lines[index].GetContent();
            string trimmed = value.Trim();
            if (string.Equals(value, "-- ", StringComparison.Ordinal) || string.Equals(value, "--", StringComparison.Ordinal)) return index;
            if (trimmed.StartsWith("Sent from my ", StringComparison.OrdinalIgnoreCase) || string.Equals(trimmed, "Sent from Mail for Windows", StringComparison.OrdinalIgnoreCase)) return index;
            index++;
        }

        return -1;
    }

    static bool IsQuotedLine(string value)
    {
        int index = 0;
        while (index < value.Length && (value[index] == ' ' || value[index] == '\t')) index++;
        return index < value.Length && value[index] == '>';
    }

    static EmailTextSnippet CreateSnippet(string value, int maximumCharacters)
    {
        StringBuilder collapsed = new StringBuilder();
        bool pendingSpace = false;
        int index = 0;
        while (index < value.Length)
        {
            char selected = value[index];
            if (char.IsWhiteSpace(selected))
            {
                if (collapsed.Length > 0) pendingSpace = true;
            }
            else
            {
                if (pendingSpace)
                {
                    collapsed.Append(' ');
                    pendingSpace = false;
                }

                collapsed.Append(selected);
            }

            index++;
        }

        if (collapsed.Length <= maximumCharacters) return new EmailTextSnippet(collapsed.ToString(), false);
        if (maximumCharacters == 1) return new EmailTextSnippet("…", true);
        int selectedLength = maximumCharacters - 1;
        if (selectedLength > 0 && char.IsHighSurrogate(collapsed[selectedLength - 1])) selectedLength--;
        string truncated = collapsed.ToString(0, selectedLength).TrimEnd() + "…";
        return new EmailTextSnippet(truncated, true);
    }

    static void AppendRegion(StringBuilder output, string value)
    {
        if (output.Length > 0) output.Append('\n');
        output.Append(value);
    }

    static string TrimRegion(string value)
    {
        return value.Trim();
    }
}
internal class EmailTextAnalysisLine
{
    string content;
    string text;
    int offset;

    internal EmailTextAnalysisLine(string content, string text, int offset)
    {
        this.content = content;
        this.text = text;
        this.offset = offset;
    }

    internal string GetContent() { return content; }
    internal string GetText() { return text; }
    internal int GetOffset() { return offset; }
}
internal class EmailTextSnippet
{
    string value;
    bool truncated;

    internal EmailTextSnippet(string value, bool truncated)
    {
        this.value = value;
        this.truncated = truncated;
    }

    internal string GetValue() { return value; }
    internal bool WasTruncated() { return truncated; }
}
