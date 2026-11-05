using System.Text.Json;

namespace jmap;

/// <summary>
/// Represents a request-level JMAP problem response.
/// </summary>
public class JmapRequestException : System.Exception
{
    /// <summary>The HTTP status code returned by the endpoint.</summary>
    public int HttpStatus;

    /// <summary>The exact problem type token or URI returned by the endpoint.</summary>
    public string? ErrorToken;

    /// <summary>The decoded problem details, or null when decoding was not possible.</summary>
    public JmapProblem? Problem;

    /// <summary>The complete structured problem data returned by the endpoint.</summary>
    public JsonElement StructuredData;

    /// <summary>The unmodified HTTP response body, or null when no response body was received.</summary>
    public byte[]? ResponseBody;

    /// <summary>Whether retrying the request can be considered by the caller.</summary>
    public bool IsRetryable;

    /// <summary>Creates a request exception with HTTP, problem, raw response, and retry context.</summary>
    /// <param name="message">The diagnostic failure message.</param>
    /// <param name="httpStatus">The HTTP status code returned by the endpoint.</param>
    /// <param name="errorToken">The exact problem type token or URI.</param>
    /// <param name="problem">The decoded problem details, or null.</param>
    /// <param name="data">The complete structured problem data.</param>
    /// <param name="responseBody">The unmodified response body, or null.</param>
    /// <param name="isRetryable">Whether retrying can be considered by the caller.</param>
    public JmapRequestException(string message, int httpStatus, string? errorToken, JmapProblem? problem, JsonElement data, byte[]? responseBody, bool isRetryable) : base(message)
    {
        HttpStatus = httpStatus;
        ErrorToken = errorToken;
        Problem = problem;
        StructuredData = data;
        ResponseBody = responseBody;
        IsRetryable = isRetryable;
    }
}
