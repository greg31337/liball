namespace uspto;

internal class ConfiguredBulkCatalogClient : IBulkCatalogClient
{
    internal ConfiguredClientComponents Components;
    internal BulkCatalogSearchOperation Search;
    internal BulkCatalogRetrievalOperation Retrieval;
    internal BulkDatasetPresetOperation Presets;

    internal ConfiguredBulkCatalogClient(ConfiguredClientComponents components)
    {
        Components = components;
        Search = new BulkCatalogSearchOperation(components.Client, components.Options);
        Retrieval = new BulkCatalogRetrievalOperation(components.Client, components.Options, components.DownloadHostPolicy);
        Presets = new BulkDatasetPresetOperation(Search);
    }

    public BulkProductResponse SearchBulkProducts(SearchRequest request)
    {
        return Search.Search(request);
    }

    public BulkProductResponse GetAllBulkProducts(SearchRequest request)
    {
        return Search.SearchAll(request);
    }

    public BulkProduct GetBulkProduct(string productIdentifier, RequestContext context)
    {
        return Retrieval.GetProduct(productIdentifier, context);
    }

    public BulkProductFile GetBulkFileMetadata(string productIdentifier, string fileName, RequestContext context)
    {
        return Retrieval.GetFile(productIdentifier, fileName, context);
    }

    public BulkDatasetPreset[] GetDatasetPresets(DatasetFamily[] families, RequestContext context)
    {
        return Presets.GetPresets(families, context);
    }
}

internal class ConfiguredBulkTransferClient : IBulkTransferClient
{
    internal ConfiguredClientComponents Components;
    internal BulkFileDownloadOperation Download;
    internal BulkFileResumeOperation Resume;

    internal ConfiguredBulkTransferClient(ConfiguredClientComponents components)
    {
        Components = components;
        Download = new BulkFileDownloadOperation(components.Client, components.Options, components.DownloadHostPolicy);
        Resume = components.CreateSharedBulkResume();
    }

    public DownloadResult DownloadBulkFile(DownloadRequest request)
    {
        return Download.Download(request);
    }

    public DownloadResult ResumeBulkFile(DownloadRequest request, DownloadResumeState resumeState)
    {
        return Resume.Resume(request, resumeState);
    }

    public DownloadResult VerifyBulkFile(DownloadRequest request)
    {
        if (request == null || global::System.String.IsNullOrWhiteSpace(request.DestinationPath) || request.ExpectedChecksums == null || request.Context == null)
        {
            throw new ValidationException("A completed bulk file verification request is required.", "request", "REQUIRED", null);
        }

        request.Context.CancellationToken.ThrowIfCancellationRequested();
        string path = global::System.IO.Path.GetFullPath(request.DestinationPath ?? string.Empty);
        if (!global::System.IO.File.Exists(path))
        {
            throw new ValidationException("The bulk file to verify does not exist.", "DestinationPath", "FILE_NOT_FOUND", null);
        }

        global::System.IO.FileInfo information = new global::System.IO.FileInfo(path);
        DownloadResult result = new DownloadResult();
        result.Request = request;
        result.FinalPath = path;
        result.Progress.BytesTransferred = information.Length;
        result.Progress.TotalBytes = request.HasExpectedSize ? request.ExpectedSize : information.Length;
        result.Progress.HasTotalBytes = true;
        result.Checksums = BulkChecksumVerifier.VerifyFile(path, request.ExpectedChecksums, 81920, request.Context.CancellationToken);
        if (request.HasExpectedSize && information.Length != request.ExpectedSize)
        {
            result.State = DownloadState.Failed;
            result.FailureCode = "DOWNLOAD_SIZE_MISMATCH";
            result.FailureMessage = "The local bulk file size does not match the expected size.";
        }
        else if (BulkChecksumVerifier.HasMismatch(result.Checksums))
        {
            result.State = DownloadState.Failed;
            result.FailureCode = "CHECKSUM_MISMATCH";
            result.FailureMessage = "The local bulk file checksum does not match the expected value.";
        }
        else
        {
            result.State = DownloadState.Completed;
            result.Progress.State = DownloadState.Completed;
        }

        return result;
    }

    public ArchiveListingResult ListArchiveEntries(string archivePath, RequestContext context)
    {
        return ArchiveListingOperation.ListWithLimits(archivePath, context, Components.Options.Extraction);
    }

    public ExtractionResult ExtractBulkArchive(ExtractionOptions options)
    {
        if (options == null || global::System.String.IsNullOrWhiteSpace(options.ArchivePath))
        {
            throw new ValidationException("Archive extraction options are required.", "options", "REQUIRED", null);
        }

        CompressionDetectionResult detection = ArchiveFormatDetector.Detect(options.ArchivePath ?? string.Empty);
        if (detection.Format == CompressionFormat.Zip)
        {
            return ZipExtractionOperation.Extract(options);
        }

        if (detection.Format == CompressionFormat.Tar || detection.Format == CompressionFormat.TarGzip)
        {
            return TarExtractionOperation.Extract(options);
        }

        ExtractionResult result = new ExtractionResult();
        result.Options = options;
        result.Detection = detection;
        result.FailureCode = "UNSUPPORTED_ARCHIVE_FORMAT";
        result.FailureMessage = "The detected archive format does not support extraction.";
        return result;
    }
}

internal class ConfiguredBulkDataReader : IBulkDataReader
{
    internal ConfiguredClientComponents Components;

    internal ConfiguredBulkDataReader(ConfiguredClientComponents components)
    {
        Components = components;
    }

    public string[] ReadBulkJsonRecords(global::System.IO.Stream input, RequestContext context)
    {
        ValidateReaderContext(context);
        JsonRecord[] records = new JsonRecordReader(input, false, null, Components.Options.Limits, context.CancellationToken).ReadBatch(Components.Options.Limits.MaximumResults);
        string[] result = new string[records.Length];
        int index;
        for (index = 0; index < records.Length; index += 1)
        {
            result[index] = records[index].RawJson;
        }

        return result;
    }

    public string[] ReadBulkXmlRecords(global::System.IO.Stream input, RequestContext context)
    {
        ValidateReaderContext(context);
        CompoundXmlReader reader = new CompoundXmlReader(input, null, 81920, Components.Options.Limits);
        global::System.Collections.Generic.List<string> records = new global::System.Collections.Generic.List<string>();
        while (records.Count < Components.Options.Limits.MaximumResults)
        {
            context.CancellationToken.ThrowIfCancellationRequested();
            CompoundXmlDocument? document = reader.ReadDocument();
            if (document == null)
            {
                break;
            }

            records.Add(global::System.Text.Encoding.UTF8.GetString(document.Bytes));
        }

        return records.ToArray();
    }

    public string[] ReadBulkDelimitedRecords(global::System.IO.Stream input, char delimiter, RequestContext context)
    {
        ValidateReaderContext(context);
        DelimitedRow[] rows = new DelimitedReader(input, delimiter, true, null, Components.Options.Limits, context.CancellationToken).ReadBatch(Components.Options.Limits.MaximumResults);
        string[] result = new string[rows.Length];
        int index;
        for (index = 0; index < rows.Length; index += 1)
        {
            result[index] = global::System.String.Join(delimiter, rows[index].Values);
        }

        return result;
    }

    public string[] ReadPatentGrantRecords(global::System.IO.Stream input, RequestContext context)
    {
        return ReadBulkXmlRecords(input, context);
    }

    public string[] ReadPatentApplicationRecords(global::System.IO.Stream input, RequestContext context)
    {
        return ReadBulkXmlRecords(input, context);
    }

    public string[] ReadPatentFileWrapperRecords(global::System.IO.Stream input, RequestContext context)
    {
        return ReadBulkJsonRecords(input, context);
    }

    public string[] ReadPatentAssignmentRecords(global::System.IO.Stream input, RequestContext context)
    {
        return ReadBulkXmlRecords(input, context);
    }

    public string[] ReadOfficeActionRecords(global::System.IO.Stream input, RequestContext context)
    {
        return ReadBulkJsonRecords(input, context);
    }

    public string[] ReadTrademarkApplicationRecords(global::System.IO.Stream input, RequestContext context)
    {
        return ReadBulkXmlRecords(input, context);
    }

    public string[] ReadTrademarkAssignmentRecords(global::System.IO.Stream input, RequestContext context)
    {
        return ReadBulkXmlRecords(input, context);
    }

    public string[] ReadTtabRecords(global::System.IO.Stream input, RequestContext context)
    {
        return ReadBulkXmlRecords(input, context);
    }

    public string[] ReadPatentsViewGrantRecords(global::System.IO.Stream input, RequestContext context)
    {
        return ReadBulkDelimitedRecords(input, '\t', context);
    }

    public string[] ReadPatentsViewPreGrantRecords(global::System.IO.Stream input, RequestContext context)
    {
        return ReadBulkDelimitedRecords(input, '\t', context);
    }

    internal static void ValidateReaderContext(RequestContext context)
    {
        if (context == null)
        {
            throw new ValidationException("A bulk reader context is required.", "context", "REQUIRED", null);
        }

        context.CancellationToken.ThrowIfCancellationRequested();
    }
}
