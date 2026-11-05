using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace html;
/// <summary>Provides instance-based access to the HtmlTokenLists operations.</summary>
public class HtmlTokenListsApi : IHtmlTokenListsApi
{
    /// <summary>Executes the ParseSpaceSeparated operation.</summary>
    public string[] ParseSpaceSeparated(string value)
    {
        return HtmlTokenLists.ParseSpaceSeparated(value);
    }

    /// <summary>Executes the ParseCommaSeparated operation.</summary>
    public string[] ParseCommaSeparated(string value)
    {
        return HtmlTokenLists.ParseCommaSeparated(value);
    }

    /// <summary>Parses a space-separated list with limits and cancellation.</summary>
    public HtmlTokenListResult ParseSpaceSeparatedWithLimits(string value,
        HtmlLimits limits, CancellationToken cancellationToken)
    {
        return HtmlTokenLists.ParseSpaceSeparatedWithLimits(value, limits,
            cancellationToken);
    }

    /// <summary>Parses a comma-separated list with limits and cancellation.</summary>
    public HtmlTokenListResult ParseCommaSeparatedWithLimits(string value,
        HtmlLimits limits, CancellationToken cancellationToken)
    {
        return HtmlTokenLists.ParseCommaSeparatedWithLimits(value, limits,
            cancellationToken);
    }

    /// <summary>Executes the SerializeSpaceSeparated operation.</summary>
    public string SerializeSpaceSeparated(string[] tokens)
    {
        return HtmlTokenLists.SerializeSpaceSeparated(tokens);
    }

    /// <summary>Executes the SerializeCommaSeparated operation.</summary>
    public string SerializeCommaSeparated(string[] tokens)
    {
        return HtmlTokenLists.SerializeCommaSeparated(tokens);
    }

    /// <summary>Serializes a space-separated list with output limits and cancellation.</summary>
    public string SerializeSpaceSeparatedWithLimits(string[] tokens,
        HtmlLimits limits, CancellationToken cancellationToken)
    {
        return HtmlTokenLists.SerializeSpaceSeparatedWithLimits(tokens,
            limits, cancellationToken);
    }

    /// <summary>Serializes a comma-separated list with output limits and cancellation.</summary>
    public string SerializeCommaSeparatedWithLimits(string[] tokens,
        HtmlLimits limits, CancellationToken cancellationToken)
    {
        return HtmlTokenLists.SerializeCommaSeparatedWithLimits(tokens,
            limits, cancellationToken);
    }

    /// <summary>Executes the Contains operation.</summary>
    public bool Contains(string[] tokens, string expected)
    {
        return HtmlTokenLists.Contains(tokens, expected);
    }

    /// <summary>Executes the ContainsAsciiInsensitive operation.</summary>
    public bool ContainsAsciiInsensitive(string[] tokens, string expected)
    {
        return HtmlTokenLists.ContainsAsciiInsensitive(tokens, expected);
    }

    /// <summary>Executes the ContainsAsciiInsensitivePrefix operation.</summary>
    public bool ContainsAsciiInsensitivePrefix(string[] tokens, int count, string expected)
    {
        return HtmlTokenLists.ContainsAsciiInsensitive(tokens, count, expected);
    }
}
