using System;
using System.Collections.Generic;

namespace email;

internal class EmailTextSearcher
{
    EmailTextSearchOptions options;
    string? query;
    List<EmailTextSearchMatch> matches;
    EmailIssueList issues;
    int searchedEntityCount;
    int searchedCharacterCount;
    bool truncated;

    internal EmailTextSearcher(string? query, EmailTextSearchOptions options)
    {
        this.query = query;
        this.options = options;
        matches = new List<EmailTextSearchMatch>();
        issues = new EmailIssueList();
        searchedEntityCount = 0;
        searchedCharacterCount = 0;
        truncated = false;
    }

    internal EmailTextSearchResult Search(MimeEntity root)
    {
        List<EmailAttachmentView> views = new EmailAttachmentFinder().EnumerateWithRootAndMaximumDepthAndMaximumEntityCount(root, options.GetMaximumDepth(), options.GetMaximumEntityCount());
        int index = 0;
        while (index < views.Count && !truncated)
        {
            EmailAttachmentView view = views[index];
            if (ShouldSearch(view)) SearchView(view);
            index++;
        }

        if (truncated) issues.Add(new EmailIssueOperations().CreateWithSeverityAndCodeAndDescription(EmailIssueSeverity.Warning, EmailIssueCode.TextSearchLimitReached, "Decoded-text search stopped after reaching its configured character or match limit."));
        return new EmailTextSearchResult((query ?? throw new InvalidOperationException()), matches, issues, searchedEntityCount, searchedCharacterCount, truncated);
    }

    bool ShouldSearch(EmailAttachmentView view)
    {
        if (!string.Equals((view.GetMediaType() ?? throw new InvalidOperationException()).GetTopLevelType(), "text", StringComparison.OrdinalIgnoreCase)) return false;
        if (!options.GetIncludeTextAttachments() && !view.IsBody()) return false;
        if (!options.GetIncludeHtml() && (view.GetMediaType() ?? throw new InvalidOperationException()).IsType("text", "html")) return false;
        return true;
    }

    void SearchView(EmailAttachmentView view)
    {
        EmailTextDecodeResult decoded = new EmailTextProcessor().DecodeWithAttachmentAndTextOptionsAndFlowedOptions(view, options.GetTextOptions(), options.GetFlowedOptions());
        issues.AddRange(decoded.GetIssues());
        string text = decoded.GetText();
        bool html = (view.GetMediaType() ?? throw new InvalidOperationException()).IsType("text", "html");
        if (html)
        {
            EmailHtmlTextResult converted = new EmailHtmlProcessor().ToTextWithHtmlAndOptions(text, options.GetHtmlOptions());
            issues.AddRange(converted.GetIssues());
            text = converted.GetText();
        }

        searchedEntityCount++;
        int remaining = options.GetMaximumTotalCharacters() - searchedCharacterCount;
        if (text.Length > remaining)
        {
            text = text.Substring(0, remaining);
            truncated = true;
        }

        searchedCharacterCount += text.Length;
        AddMatches(view, text, html);
        if (searchedCharacterCount >= options.GetMaximumTotalCharacters()) truncated = true;
    }

    void AddMatches(EmailAttachmentView view, string text, bool html)
    {
        StringComparison comparison = options.GetCaseSensitive() ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase;
        int index = 0;
        while (index <= text.Length - (query ?? throw new InvalidOperationException()).Length)
        {
            int found = text.IndexOf(query, index, comparison);
            if (found < 0) return;
            if (matches.Count >= options.GetMaximumMatches())
            {
                truncated = true;
                return;
            }

            matches.Add(new EmailTextSearchMatch(view.GetEntity(), view.GetPath(), found, query.Length, CreateContext(text, found, query.Length), html));
            index = found + query.Length;
        }
    }

    string CreateContext(string text, int matchOffset, int matchLength)
    {
        int maximum = options.GetMaximumContextCharacters();
        if (text.Length <= maximum) return CollapseWhitespace(text);
        int before = (maximum - matchLength) / 2;
        if (before < 0) before = 0;
        int start = matchOffset - before;
        if (start < 0) start = 0;
        int length = Math.Min(maximum, text.Length - start);
        if (matchOffset + matchLength > start + length)
        {
            start = matchOffset + matchLength - length;
            if (start < 0) start = 0;
        }

        string selected = text.Substring(start, length);
        string prefix = start > 0 ? "…" : "";
        string suffix = start + length < text.Length ? "…" : "";
        string result = prefix + CollapseWhitespace(selected) + suffix;
        return result.Length <= maximum ? result : result.Substring(0, maximum);
    }

    static string CollapseWhitespace(string value)
    {
        System.Text.StringBuilder output = new System.Text.StringBuilder();
        bool pending = false;
        int index = 0;
        while (index < value.Length)
        {
            if (char.IsWhiteSpace(value[index]))
            {
                if (output.Length > 0) pending = true;
            }
            else
            {
                if (pending)
                {
                    output.Append(' ');
                    pending = false;
                }

                output.Append(value[index]);
            }

            index++;
        }

        return output.ToString();
    }
}
