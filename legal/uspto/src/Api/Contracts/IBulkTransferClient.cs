namespace uspto;

/// <summary>
/// Provides verified transfer, resume, checksum, listing, and extraction operations for bulk files.
/// </summary>
public interface IBulkTransferClient
{
    /// <summary>Downloads one bulk file.</summary>
    /// <param name="request">The download request.</param>
    /// <returns>The download result.</returns>
    DownloadResult DownloadBulkFile(DownloadRequest request);

    /// <summary>Resumes one bulk file from validated persisted state.</summary>
    /// <param name="request">The download request.</param>
    /// <param name="resumeState">The persisted partial state.</param>
    /// <returns>The download result.</returns>
    DownloadResult ResumeBulkFile(DownloadRequest request, DownloadResumeState resumeState);

    /// <summary>Verifies a local bulk file using expected size and checksums.</summary>
    /// <param name="request">The download request containing expected values.</param>
    /// <returns>The verification result.</returns>
    DownloadResult VerifyBulkFile(DownloadRequest request);

    /// <summary>Lists normalized entries in a supported archive.</summary>
    /// <param name="archivePath">The local archive path.</param>
    /// <param name="context">The request context.</param>
    /// <returns>The bounded archive listing result.</returns>
    ArchiveListingResult ListArchiveEntries(string archivePath, RequestContext context);

    /// <summary>Extracts selected safe entries from a supported archive.</summary>
    /// <param name="options">The extraction selection and limits.</param>
    /// <returns>The extraction result.</returns>
    ExtractionResult ExtractBulkArchive(ExtractionOptions options);
}
