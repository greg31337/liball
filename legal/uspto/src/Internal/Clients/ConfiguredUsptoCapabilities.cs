namespace uspto;

internal class ConfiguredUsptoCapabilities : IUsptoCapabilities
{
    internal ConfiguredClientComponents Components;

    internal ConfiguredUsptoCapabilities(ConfiguredClientComponents components)
    {
        Components = components;
    }

    public UsptoClientOptions GetRecommendedClientOptions()
    {
        return new UsptoClientOptions();
    }

    public SearchRequest GetRecommendedSearchRequest()
    {
        return new SearchRequest();
    }

    public SearchResponseMetadata GetEmptySearchResponseMetadata()
    {
        return new SearchResponseMetadata();
    }

    public DownloadResult GetEmptyDownloadResult()
    {
        return new DownloadResult();
    }

    public ArchiveListingResult GetEmptyArchiveListingResult()
    {
        return new ArchiveListingResult();
    }

    public ExtractionResult GetEmptyExtractionResult()
    {
        return new ExtractionResult();
    }

    public ApiService[] GetSupportedApiServices()
    {
        return new ApiService[] { ApiService.Odp, ApiService.OfficeAction, ApiService.Tsdr, ApiService.TsdrPdfAndZip, ApiService.PatentsView };
    }

    public IdentifierKind[] GetSupportedPatentIdentifierKinds()
    {
        return new IdentifierKind[] { IdentifierKind.PatentApplication, IdentifierKind.PatentPublication, IdentifierKind.PatentGrant, IdentifierKind.PctApplication, IdentifierKind.Ambiguous };
    }

    public TrademarkIdentifierKind[] GetSupportedTrademarkIdentifierKinds()
    {
        return new TrademarkIdentifierKind[] { TrademarkIdentifierKind.SerialNumber, TrademarkIdentifierKind.RegistrationNumber, TrademarkIdentifierKind.InternationalRegistrationNumber };
    }

    public SearchOperator[] GetSupportedSearchOperators()
    {
        return new SearchOperator[] { SearchOperator.Equal, SearchOperator.NotEqual, SearchOperator.LessThan, SearchOperator.LessThanOrEqual, SearchOperator.GreaterThan, SearchOperator.GreaterThanOrEqual, SearchOperator.Contains, SearchOperator.In, SearchOperator.And, SearchOperator.Or, SearchOperator.Not };
    }

    public SortDirection[] GetSupportedSortDirections()
    {
        return new SortDirection[] { SortDirection.Ascending, SortDirection.Descending };
    }

    public ExportFormat[] GetSupportedExportFormats()
    {
        return new ExportFormat[] { ExportFormat.Json, ExportFormat.Csv };
    }

    public DocumentFormat[] GetSupportedDocumentFormats()
    {
        return new DocumentFormat[] { DocumentFormat.Json, DocumentFormat.Xml, DocumentFormat.Csv, DocumentFormat.Text, DocumentFormat.Html, DocumentFormat.Pdf, DocumentFormat.Zip };
    }

    public CompressionFormat[] GetSupportedCompressionFormats()
    {
        return new CompressionFormat[] { CompressionFormat.None, CompressionFormat.Zip, CompressionFormat.Gzip, CompressionFormat.Tar, CompressionFormat.TarGzip };
    }

    public ChecksumAlgorithm[] GetSupportedChecksumAlgorithms()
    {
        return new ChecksumAlgorithm[] { ChecksumAlgorithm.Md5, ChecksumAlgorithm.Sha1, ChecksumAlgorithm.Sha256, ChecksumAlgorithm.Sha512 };
    }

    public CacheMode[] GetSupportedCacheModes()
    {
        return new CacheMode[] { CacheMode.Disabled, CacheMode.Memory, CacheMode.File, CacheMode.MemoryAndFile };
    }

    public DownloadState[] GetReportableDownloadStates()
    {
        return new DownloadState[] { DownloadState.NotStarted, DownloadState.Downloading, DownloadState.Completed, DownloadState.Cancelled, DownloadState.Failed };
    }

    public DatasetFamily[] GetSupportedDatasetFamilies()
    {
        return new DatasetFamily[] { DatasetFamily.PatentFileWrapperComplete, DatasetFamily.PatentFileWrapperDelta, DatasetFamily.PatentGrantFullText, DatasetFamily.PatentApplicationFullText, DatasetFamily.PatentAssignment, DatasetFamily.OfficeAction, DatasetFamily.Ptab, DatasetFamily.TrademarkApplicationAndStatus, DatasetFamily.TrademarkAssignment, DatasetFamily.Ttab, DatasetFamily.PatentsViewGrant, DatasetFamily.PatentsViewPreGrant, DatasetFamily.PatentsViewAnnualized, DatasetFamily.PatentsViewSorted };
    }

    public DataProvenance[] GetReportableDataProvenance()
    {
        return new DataProvenance[] { DataProvenance.Unknown, DataProvenance.Odp, DataProvenance.Tsdr, DataProvenance.PatentsView, DataProvenance.Bulk, DataProvenance.LocalMirror, DataProvenance.Cache, DataProvenance.ClientDerived };
    }

    public FallbackAccess[] GetSupportedFallbackAccessPaths()
    {
        return new FallbackAccess[] { FallbackAccess.Online, FallbackAccess.Cache, FallbackAccess.Local };
    }

    public FallbackAuthority[] GetSupportedFallbackAuthorities()
    {
        return new FallbackAuthority[] { FallbackAuthority.AuthoritativeOnly, FallbackAuthority.ResearchOnly, FallbackAuthority.Either };
    }

    public MirrorAction[] GetSupportedMirrorActions()
    {
        return new MirrorAction[] { MirrorAction.Unchanged, MirrorAction.New, MirrorAction.Changed, MirrorAction.Missing, MirrorAction.Corrupt, MirrorAction.Resumable, MirrorAction.Orphaned };
    }

    public PtabCollection[] GetSupportedPtabCollections()
    {
        return new PtabCollection[] { PtabCollection.TrialProceedings, PtabCollection.TrialDecisions, PtabCollection.TrialDocuments, PtabCollection.AppealDecisions, PtabCollection.InterferenceDecisions };
    }
}
