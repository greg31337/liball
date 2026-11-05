namespace uri;

/// <summary>
/// Defines options shared by URI query operations.
/// </summary>
public class UriQueryOptions
{
    /// <summary>The processing limits for the operation.</summary>
    public OperationLimits Limits;

    /// <summary>The character separating ordered query pairs.</summary>
    public char PairDelimiter;

    /// <summary>Whether spaces and plus signs use <c>application/x-www-form-urlencoded</c> rules.</summary>
    public bool FormUrlEncoded;

    /// <summary>Initializes plain RFC query processing with ampersand separation and default limits.</summary>
    public UriQueryOptions()
    {
        Limits = new OperationLimits();
        PairDelimiter = '&';
        FormUrlEncoded = false;
    }
}
