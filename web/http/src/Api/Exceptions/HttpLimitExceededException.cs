namespace http;

/// <summary>
/// Reports that HTTP processing exceeded a configured resource limit.
/// </summary>
public class HttpLimitExceededException : System.Exception
{
    /// <summary>The stable name of the exceeded limit.</summary>
    public string LimitName;
    /// <summary>The configured signed limit.</summary>
    public long Limit;
    /// <summary>The observed signed value that exceeded the limit.</summary>
    public long Actual;

    /// <summary>Creates an HTTP resource-limit exception.</summary>
    /// <param name="message">The caller-readable failure explanation.</param>
    /// <param name="limitName">The stable name of the exceeded limit.</param>
    /// <param name="limit">The configured signed limit.</param>
    /// <param name="actual">The observed signed value.</param>
    public HttpLimitExceededException(string message, string limitName, long limit, long actual) : base(message)
    {
        LimitName = limitName;
        Limit = limit;
        Actual = actual;
    }
}
