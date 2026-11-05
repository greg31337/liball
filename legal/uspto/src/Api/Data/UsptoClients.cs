namespace uspto;

/// <summary>
/// Holds the contract-typed components of a configured USPTO client suite.
/// </summary>
public class UsptoClients
{
    /// <summary>The options used to configure this client suite.</summary>
    public UsptoClientOptions Options = new UsptoClientOptions();

    /// <summary>The credential provider shared by remote clients.</summary>
    public IApiKeyProvider? ApiKeyProvider = null;

    /// <summary>The offline patent and trademark identifier normalizer.</summary>
    public IIdentifierNormalizer? IdentifierNormalizer = null;

    /// <summary>The Patent File Wrapper and patent application client.</summary>
    public IPatentClient? PatentClient = null;

    /// <summary>The final petition decision client.</summary>
    public IPetitionClient? PetitionClient = null;

    /// <summary>The Patent Trial and Appeal Board client.</summary>
    public IPtabClient? PtabClient = null;

    /// <summary>The Office Action dataset client.</summary>
    public IOfficeActionClient? OfficeActionClient = null;

    /// <summary>The Trademark Status and Document Retrieval client.</summary>
    public ITrademarkClient? TrademarkClient = null;

    /// <summary>The USPTO bulk product catalog client.</summary>
    public IBulkCatalogClient? BulkCatalogClient = null;

    /// <summary>The verified bulk transfer and archive client.</summary>
    public IBulkTransferClient? BulkTransferClient = null;

    /// <summary>The generic and typed bulk record reader.</summary>
    public IBulkDataReader? BulkDataReader = null;

    /// <summary>The local dataset mirror component.</summary>
    public IDatasetMirror? DatasetMirror = null;

    /// <summary>The local mirrored dataset query component.</summary>
    public ILocalDatasetQuery? LocalDatasetQuery = null;

    /// <summary>The capability-detected PatentsView client.</summary>
    public IPatentsViewClient? PatentsViewClient = null;

    /// <summary>The configured capability and supported-value provider.</summary>
    public IUsptoCapabilities? Capabilities = null;

    /// <summary>The guarded online, cache, and local-mirror fallback client.</summary>
    public IDataFallbackClient? DataFallbackClient = null;

    /// <summary>The normalized Office Action and PatentsView citation graph input builder.</summary>
    public ICitationGraphInputBuilder? CitationGraphInputBuilder = null;

    /// <summary>The lifetime handle that releases resources owned by this configured suite.</summary>
    public global::System.IDisposable? Lifetime = null;
}
