namespace whois;

/// <summary>
/// Reports that automatic server selection cannot route a classified resource.
/// </summary>
public class WhoisServerNotFoundException : System.Exception
{
    /// <summary>
    /// Contains the original and normalized query that could not be routed.
    /// </summary>
    public WhoisQuery Query;

    /// <summary>
    /// Initializes a server-selection exception with classified query context.
    /// </summary>
    /// <param name="message">The server-selection failure message.</param>
    /// <param name="query">The classified query that could not be routed.</param>
    /// <param name="innerException">The underlying selection exception, or <c>null</c>.</param>
    public WhoisServerNotFoundException(string message, WhoisQuery query, System.Exception? innerException) : base(message, innerException)
    {
        Query = query;
    }
}
