using System;
using System.Collections.Generic;

namespace http;

/// <summary>
/// Contains stored-response metadata and timestamps for cache calculations.
/// </summary>
public class HttpCacheResponseContext
{
    /// <summary>The stored response, or <c>null</c> when no candidate is available.</summary>
    public HttpResponse? Response;
    /// <summary>Additional stored-response contexts matching the validation request.</summary>
    public List<HttpCacheResponseContext> MatchingResponseContexts =
        new List<HttpCacheResponseContext>();
    /// <summary>The parsed response Cache-Control directives.</summary>
    public HttpCacheControl CacheControl = new HttpCacheControl();
    /// <summary>The time at which the originating request was sent.</summary>
    public DateTimeOffset RequestTime;
    /// <summary>The time at which the response was received.</summary>
    public DateTimeOffset ResponseTime;
    /// <summary>The time at which the response was stored.</summary>
    public DateTimeOffset StoredTime;
    /// <summary>The time for which a cache decision is being made.</summary>
    public DateTimeOffset CurrentTime;
    /// <summary>Indicates that a valid Date field value is available.</summary>
    public bool HasDate;
    /// <summary>The parsed Date field value.</summary>
    public DateTimeOffset Date;
    /// <summary>Indicates that a valid Age field value is available.</summary>
    public bool HasAge;
    /// <summary>The parsed Age value in seconds.</summary>
    public long Age;
    /// <summary>Indicates that a valid Expires field value is available.</summary>
    public bool HasExpires;
    /// <summary>The parsed Expires field value.</summary>
    public DateTimeOffset Expires;
    /// <summary>Indicates that a valid Last-Modified field value is available.</summary>
    public bool HasLastModified;
    /// <summary>The parsed Last-Modified field value.</summary>
    public DateTimeOffset LastModified;
    /// <summary>Indicates that the stored response contains complete content.</summary>
    public bool IsComplete;
    /// <summary>The successful validation merge that refreshed this response, or <c>null</c>.</summary>
    public HttpCacheValidationResult? ValidationResult;
}
