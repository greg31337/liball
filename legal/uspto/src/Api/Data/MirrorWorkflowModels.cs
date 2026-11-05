namespace uspto;

/// <summary>
/// Reports the complete refresh outcome for one mirrored dataset file.
/// </summary>
public class MirrorWorkflowFileResult
{
    /// <summary>The dataset family containing the file.</summary>
    public DatasetFamily Family = DatasetFamily.Unknown;

    /// <summary>The bulk product identifier.</summary>
    public string? ProductIdentifier = null;

    /// <summary>The catalog file name.</summary>
    public string? FileName = null;

    /// <summary>The planned mirror action.</summary>
    public MirrorAction Action = MirrorAction.Unknown;

    /// <summary>The transfer outcome, when a transfer was attempted.</summary>
    public DownloadResult? Transfer = null;

    /// <summary>Whether at least one transfer or processing stage ran.</summary>
    public bool WasProcessed = false;

    /// <summary>Whether processing was intentionally skipped.</summary>
    public bool WasSkipped = false;

    /// <summary>Whether all requested stages and verification completed for this file.</summary>
    public bool IsComplete = false;

    /// <summary>The retained file or stage failure.</summary>
    public SectionError? Error = null;
}

/// <summary>
/// Holds a bulk-to-local mirror refresh without discarding successful files when others fail.
/// </summary>
public class MirrorWorkflowResult
{
    /// <summary>The outcome of every selected mirror file.</summary>
    public MirrorWorkflowFileResult[] Files = new MirrorWorkflowFileResult[0];

    /// <summary>The structured family, transfer, stage, and verification failures.</summary>
    public SectionError[] Errors = new SectionError[0];

    /// <summary>The final persisted dataset manifest.</summary>
    public DatasetManifest Manifest = new DatasetManifest();

    /// <summary>The number of selected files that completed.</summary>
    public int CompletedFileCount = 0;

    /// <summary>The number of selected files that failed.</summary>
    public int FailedFileCount = 0;

    /// <summary>The number of unavailable selected dataset families.</summary>
    public int UnavailableFamilyCount = 0;

    /// <summary>The number of files for which at least one stage was processed.</summary>
    public int ProcessedFileCount = 0;

    /// <summary>The number of files intentionally skipped by the refresh plan.</summary>
    public int SkippedFileCount = 0;

    /// <summary>Whether all selected families and files completed successfully.</summary>
    public bool IsComplete = false;

    /// <summary>Whether successful files were retained alongside one or more failures.</summary>
    public bool HasPartialSuccess = false;

    /// <summary>The non-authoritative client provenance of the aggregate workflow.</summary>
    public PatentDataProvenance Provenance = new PatentDataProvenance();
}
