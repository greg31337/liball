using System.Collections.Generic;
using System.Text.Json;

namespace jmap;

/// <summary>
/// Contains escaped and highlighted subject or body text for one Email search result.
/// </summary>
public class SearchSnippet
{
    /// <summary>The identifier of the Email to which this snippet applies.</summary>
    public string? EmailId;

    /// <summary>The escaped subject with matching terms marked, or null when it did not match.</summary>
    public string? Subject;

    /// <summary>The escaped plain-text body excerpt with matching terms marked, or null when it did not match.</summary>
    public string? Preview;

    /// <summary>Stores unrecognized SearchSnippet properties.</summary>
    public Dictionary<string, JsonElement> UnknownProperties = new Dictionary<string, JsonElement>();
}

/// <summary>
/// Contains the arguments for SearchSnippet/get.
/// </summary>
public class SearchSnippetGetRequest
{
    /// <summary>The account containing the Emails.</summary>
    public string? AccountId;

    /// <summary>The Email query filter used to determine matching terms.</summary>
    public EmailFilter? Filter;

    /// <summary>The Email identifiers for which snippets are requested.</summary>
    public List<string> EmailIds = new List<string>();

    /// <summary>Stores unrecognized SearchSnippet/get arguments.</summary>
    public Dictionary<string, JsonElement> UnknownProperties = new Dictionary<string, JsonElement>();
}

/// <summary>
/// Contains the result fields for SearchSnippet/get.
/// </summary>
public class SearchSnippetGetResponse
{
    /// <summary>The account containing the Emails.</summary>
    public string? AccountId;

    /// <summary>The returned snippets in server-selected order.</summary>
    public List<SearchSnippet> List = new List<SearchSnippet>();

    /// <summary>The requested Email identifiers that were not found, or null when all were found.</summary>
    public List<string>? NotFound;

    /// <summary>Stores unrecognized SearchSnippet/get result fields.</summary>
    public Dictionary<string, JsonElement> UnknownProperties = new Dictionary<string, JsonElement>();
}
