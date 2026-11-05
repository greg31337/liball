using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace html;
/// <summary>Defines instance-based access to the HtmlTokenLists operations.</summary>
public interface IHtmlTokenListsApi
{
    /// <summary>Executes the ParseSpaceSeparated operation.</summary>
    public string[] ParseSpaceSeparated(string value);
    /// <summary>Executes the ParseCommaSeparated operation.</summary>
    public string[] ParseCommaSeparated(string value);
    /// <summary>Parses a space-separated list with limits and cancellation.</summary>
    public HtmlTokenListResult ParseSpaceSeparatedWithLimits(string value, HtmlLimits limits, CancellationToken cancellationToken);
    /// <summary>Parses a comma-separated list with limits and cancellation.</summary>
    public HtmlTokenListResult ParseCommaSeparatedWithLimits(string value, HtmlLimits limits, CancellationToken cancellationToken);
    /// <summary>Executes the SerializeSpaceSeparated operation.</summary>
    public string SerializeSpaceSeparated(string[] tokens);
    /// <summary>Executes the SerializeCommaSeparated operation.</summary>
    public string SerializeCommaSeparated(string[] tokens);
    /// <summary>Serializes a space-separated list with output limits and cancellation.</summary>
    public string SerializeSpaceSeparatedWithLimits(string[] tokens, HtmlLimits limits, CancellationToken cancellationToken);
    /// <summary>Serializes a comma-separated list with output limits and cancellation.</summary>
    public string SerializeCommaSeparatedWithLimits(string[] tokens, HtmlLimits limits, CancellationToken cancellationToken);
    /// <summary>Executes the Contains operation.</summary>
    public bool Contains(string[] tokens, string expected);
    /// <summary>Executes the ContainsAsciiInsensitive operation.</summary>
    public bool ContainsAsciiInsensitive(string[] tokens, string expected);
    /// <summary>Executes the ContainsAsciiInsensitivePrefix operation.</summary>
    public bool ContainsAsciiInsensitivePrefix(string[] tokens, int count, string expected);
}
