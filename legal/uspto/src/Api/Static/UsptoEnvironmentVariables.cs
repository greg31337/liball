namespace uspto;

/// <summary>
/// Provides the supported environment-variable names for service credentials.
/// </summary>
public static class UsptoEnvironmentVariables
{
    /// <summary>The environment variable containing an Open Data Portal API key.</summary>
    public const string OdpApiKey = "USPTO_API_KEY";

    /// <summary>The environment variable containing a TSDR API key.</summary>
    public const string TsdrApiKey = "USPTO_TSDR_API_KEY";

    /// <summary>The environment variable containing a PatentsView PatentSearch API key.</summary>
    public const string PatentsViewApiKey = "PATENTSVIEW_API_KEY";
}
