namespace uspto;

internal class PatentsViewLocalManifestTable
{
    internal DatasetFamily Family = DatasetFamily.Unknown;
    internal string? ProductIdentifier = null;
    internal string? TableIdentifier = null;
    internal string? RelativePath = null;
    internal char Delimiter = '\0';
    internal string[] Headers = new string[0];
}

internal class PatentsViewLocalHeaderEvidence
{
    internal DatasetFamily Family = DatasetFamily.Unknown;
    internal string? ProductIdentifier = null;
    internal string? TableIdentifier = null;
    internal string? FilePath = null;
    internal char Delimiter = '\0';
    internal string[] Headers = new string[0];
}

internal class PatentsViewLocalTableDiscoveryRequest
{
    internal string? MirrorDirectory = null;
    internal DatasetFamily DefaultFamily = DatasetFamily.Unknown;
    internal PatentsViewLocalManifestTable[] ManifestTables = new PatentsViewLocalManifestTable[0];
    internal ExtractionResult[] ExtractionResults = new ExtractionResult[0];
    internal string[] ExtractedFiles = new string[0];
    internal PatentsViewLocalHeaderEvidence[] HeaderEvidence = new PatentsViewLocalHeaderEvidence[0];
    internal bool InspectFiles = true;
    internal int MaximumTables = 10000;
    internal UsptoLimits Limits = new UsptoLimits();
    internal global::System.Threading.CancellationToken CancellationToken = default(global::System.Threading.CancellationToken);
}

internal class PatentsViewLocalTableInfo
{
    internal DatasetFamily Family = DatasetFamily.Unknown;
    internal string TableIdentifier = string.Empty;
    internal string? FilePath = null;
    internal string? RelativePath = null;
    internal char Delimiter = '\t';
    internal bool FromManifest = false;
    internal bool FromExtraction = false;
    internal bool FromHeaderEvidence = false;
    internal bool FileExists = false;
    internal bool FileInspected = false;
    internal long FileSize = 0L;
    internal bool HasFileSize = false;
    internal PatentsViewDerivedTableSchema DeclaredSchema = new PatentsViewDerivedTableSchema();
    internal bool HasDeclaredSchema = false;
    internal PatentsViewDerivedTableSchema ObservedSchema = new PatentsViewDerivedTableSchema();
    internal bool HasObservedSchema = false;
    internal PatentsViewDerivedTableSchema Schema = new PatentsViewDerivedTableSchema();
    internal bool HasSchema = false;
    internal bool SchemaMatchesManifest = false;
    internal bool HasSchemaComparison = false;
    internal string? InspectionFailureCode = null;
    internal PatentDataProvenance Provenance = new PatentDataProvenance
    {
        Source = DataProvenance.LocalMirror,
        IsAuthoritative = false
    };
}

internal class PatentsViewLocalTableDiscoveryResult
{
    internal PatentsViewLocalTableInfo[] Tables = new PatentsViewLocalTableInfo[0];
    internal int ManifestCandidateCount = 0;
    internal int ExtractionCandidateCount = 0;
    internal int HeaderCandidateCount = 0;
    internal int MissingFileCount = 0;
    internal int UnsupportedFileCount = 0;
    internal bool IsComplete = false;
}

internal static class PatentsViewLocalTableDiscovery
{
    internal static PatentsViewLocalTableDiscoveryResult Discover(PatentsViewLocalTableDiscoveryRequest request)
    {
        ValidateRequest(request);
        PatentsViewLocalTableDiscoveryResult result = new PatentsViewLocalTableDiscoveryResult();
        global::System.Collections.Generic.List<PatentsViewLocalTableInfo> tables = new global::System.Collections.Generic.List<PatentsViewLocalTableInfo>();
        global::System.Collections.Generic.Dictionary<string, PatentsViewLocalTableInfo> indexed = new global::System.Collections.Generic.Dictionary<string, PatentsViewLocalTableInfo>(global::System.StringComparer.Ordinal);
        int index;
        for (index = 0; index < request.ManifestTables.Length; index += 1)
        {
            request.CancellationToken.ThrowIfCancellationRequested();
            PatentsViewLocalManifestTable manifest = request.ManifestTables[index];
            if (manifest == null)
            {
                throw new ValidationException("A manifest table entry cannot be null.", "ManifestTables", "NULL_ITEM", null);
            }

            result.ManifestCandidateCount += 1;
            PatentsViewLocalTableInfo candidate = CreateManifestCandidate(manifest, request, result);
            AddOrMerge(candidate, tables, indexed, request.MaximumTables);
        }

        for (index = 0; index < request.ExtractionResults.Length; index += 1)
        {
            request.CancellationToken.ThrowIfCancellationRequested();
            ExtractionResult extraction = request.ExtractionResults[index];
            if (extraction == null)
            {
                throw new ValidationException("An extraction result cannot be null.", "ExtractionResults", "NULL_ITEM", null);
            }

            if (extraction.Entries == null)
            {
                throw new ValidationException("Extraction result entries cannot be null.", "Entries", "REQUIRED", null);
            }

            int entryIndex;
            for (entryIndex = 0; entryIndex < extraction.Entries.Length; entryIndex += 1)
            {
                ExtractionEntryResult entry = extraction.Entries[entryIndex];
                if (entry != null && entry.Entry == null)
                {
                    throw new ValidationException("An extraction entry descriptor cannot be null.", "Entry", "REQUIRED", null);
                }

                if (entry != null && entry.IsExtracted && !entry.Entry.IsDirectory && !global::System.String.IsNullOrWhiteSpace(entry.DestinationPath))
                {
                    result.ExtractionCandidateCount += 1;
                    PatentsViewLocalTableInfo candidate = CreateFileCandidate(entry.DestinationPath ?? string.Empty, request.DefaultFamily, null, null, '\0', true, request, result);
                    AddOrMerge(candidate, tables, indexed, request.MaximumTables);
                }
            }
        }

        for (index = 0; index < request.ExtractedFiles.Length; index += 1)
        {
            request.CancellationToken.ThrowIfCancellationRequested();
            string path = request.ExtractedFiles[index];
            if (global::System.String.IsNullOrWhiteSpace(path))
            {
                throw new ValidationException("An extracted file path cannot be blank.", "ExtractedFiles", "REQUIRED", null);
            }

            result.ExtractionCandidateCount += 1;
            PatentsViewLocalTableInfo candidate = CreateFileCandidate(path, request.DefaultFamily, null, null, '\0', true, request, result);
            AddOrMerge(candidate, tables, indexed, request.MaximumTables);
        }

        for (index = 0; index < request.HeaderEvidence.Length; index += 1)
        {
            request.CancellationToken.ThrowIfCancellationRequested();
            PatentsViewLocalHeaderEvidence evidence = request.HeaderEvidence[index];
            if (evidence == null)
            {
                throw new ValidationException("A header-evidence entry cannot be null.", "HeaderEvidence", "NULL_ITEM", null);
            }

            result.HeaderCandidateCount += 1;
            PatentsViewLocalTableInfo candidate = CreateHeaderCandidate(evidence, request);
            AddOrMerge(candidate, tables, indexed, request.MaximumTables);
        }

        result.Tables = tables.ToArray();
        result.IsComplete = true;
        return result;
    }

    internal static PatentsViewLocalTableInfo CreateManifestCandidate(PatentsViewLocalManifestTable manifest, PatentsViewLocalTableDiscoveryRequest request, PatentsViewLocalTableDiscoveryResult result)
    {
        if (manifest.Headers == null)
        {
            throw new ValidationException("Manifest table headers cannot be null.", "Headers", "REQUIRED", null);
        }

        string? path = ResolveLocalPath(request.MirrorDirectory, manifest.RelativePath);
        DatasetFamily family = ResolveFamily(manifest.Family, request.DefaultFamily, manifest.ProductIdentifier, path);
        ValidateEvidenceFamily(family);
        char delimiter = ResolveDelimiter(manifest.Delimiter, path);
        string tableIdentifier = ResolveTableIdentifier(manifest.TableIdentifier, path, manifest.Headers);
        PatentsViewLocalTableInfo resultInfo = CreateBaseInfo(family, tableIdentifier, path, request.MirrorDirectory, delimiter);
        resultInfo.FromManifest = true;
        if (manifest.Headers.Length > 0)
        {
            PatentsViewDerivedTableSchema declared = InspectHeaders(manifest.Headers, tableIdentifier, family);
            ApplyDeclaredSchema(resultInfo, declared);
        }

        ObserveFile(resultInfo, request, result);
        return resultInfo;
    }

    internal static PatentsViewLocalTableInfo CreateFileCandidate(string filePath, DatasetFamily explicitFamily, string? productIdentifier, string? tableIdentifier, char delimiter, bool fromExtraction, PatentsViewLocalTableDiscoveryRequest request, PatentsViewLocalTableDiscoveryResult result)
    {
        string? path = ResolveLocalPath(request.MirrorDirectory, filePath);
        if (path == null)
        {
            throw new ValidationException("A local table file path is required.", "filePath", "REQUIRED", null);
        }

        DatasetFamily family = ResolveFamily(explicitFamily, request.DefaultFamily, productIdentifier, path);
        ValidateEvidenceFamily(family);
        char resolvedDelimiter = ResolveDelimiter(delimiter, path);
        string resolvedTable = ResolveTableIdentifier(tableIdentifier, path, new string[0]);
        PatentsViewLocalTableInfo resultInfo = CreateBaseInfo(family, resolvedTable, path, request.MirrorDirectory, resolvedDelimiter);
        resultInfo.FromExtraction = fromExtraction;
        ObserveFile(resultInfo, request, result);
        return resultInfo;
    }

    internal static PatentsViewLocalTableInfo CreateHeaderCandidate(PatentsViewLocalHeaderEvidence evidence, PatentsViewLocalTableDiscoveryRequest request)
    {
        if (evidence.Headers == null || evidence.Headers.Length == 0)
        {
            throw new ValidationException("Header evidence must contain at least one column.", "Headers", "REQUIRED", null);
        }

        string? path = ResolveLocalPath(request.MirrorDirectory, evidence.FilePath);
        DatasetFamily family = ResolveFamily(evidence.Family, request.DefaultFamily, evidence.ProductIdentifier, path);
        ValidateEvidenceFamily(family);
        char delimiter = ResolveDelimiter(evidence.Delimiter, path);
        string tableIdentifier = ResolveTableIdentifier(evidence.TableIdentifier, path, evidence.Headers);
        PatentsViewLocalTableInfo result = CreateBaseInfo(family, tableIdentifier, path, request.MirrorDirectory, delimiter);
        result.FromHeaderEvidence = true;
        PatentsViewDerivedTableSchema observed = InspectHeaders(evidence.Headers, tableIdentifier, family);
        ApplyObservedSchema(result, observed);
        if (path != null)
        {
            result.FileExists = global::System.IO.File.Exists(path);
        }

        return result;
    }

    internal static PatentsViewLocalTableInfo CreateBaseInfo(DatasetFamily family, string tableIdentifier, string? path, string? mirrorDirectory, char delimiter)
    {
        PatentsViewLocalTableInfo result = new PatentsViewLocalTableInfo();
        result.Family = family;
        result.TableIdentifier = tableIdentifier;
        result.FilePath = path;
        result.RelativePath = CreateRelativePath(mirrorDirectory, path);
        result.Delimiter = delimiter;
        result.Provenance.Source = DataProvenance.LocalMirror;
        result.Provenance.DatasetIdentifier = tableIdentifier;
        result.Provenance.IsAuthoritative = false;
        return result;
    }

    internal static void ObserveFile(PatentsViewLocalTableInfo info, PatentsViewLocalTableDiscoveryRequest request, PatentsViewLocalTableDiscoveryResult result)
    {
        if (info.FilePath == null)
        {
            return;
        }

        info.FileExists = global::System.IO.File.Exists(info.FilePath);
        if (!info.FileExists)
        {
            result.MissingFileCount += 1;
            info.InspectionFailureCode = "FILE_NOT_FOUND";
            return;
        }

        if (!IsDelimitedPath(info.FilePath))
        {
            result.UnsupportedFileCount += 1;
            info.InspectionFailureCode = "UNSUPPORTED_FILE_TYPE";
            return;
        }

        global::System.IO.FileAttributes attributes = global::System.IO.File.GetAttributes(info.FilePath);
        if ((attributes & global::System.IO.FileAttributes.ReparsePoint) != 0)
        {
            result.UnsupportedFileCount += 1;
            info.InspectionFailureCode = "SYMBOLIC_LINK_NOT_SUPPORTED";
            return;
        }

        global::System.IO.FileInfo file = new global::System.IO.FileInfo(info.FilePath);
        info.FileSize = file.Length;
        info.HasFileSize = true;
        if (!request.InspectFiles)
        {
            return;
        }

        request.CancellationToken.ThrowIfCancellationRequested();
        using (global::System.IO.FileStream input = global::System.IO.File.OpenRead(info.FilePath))
        {
            DelimitedReader reader = new DelimitedReader(input, info.Delimiter, true, info.FilePath, request.Limits, request.CancellationToken);
            DelimitedRow? first = reader.ReadRow();
            string[] headers = reader.Headers;
            if (first != null)
            {
                headers = first.Headers;
            }

            if (headers.Length == 0)
            {
                info.InspectionFailureCode = "EMPTY_OR_HEADERLESS_FILE";
                return;
            }

            PatentsViewDerivedTableSchema observed = InspectHeaders(headers, info.TableIdentifier, info.Family);
            ApplyObservedSchema(info, observed);
            info.FileInspected = true;
        }
    }

    internal static PatentsViewDerivedTableSchema InspectHeaders(string[] headers, string tableIdentifier, DatasetFamily family)
    {
        DelimitedRow row = new DelimitedRow();
        row.RowNumber = 0L;
        row.StartLineNumber = 1L;
        row.EndLineNumber = 1L;
        row.StartOffset = 0L;
        row.Headers = headers;
        row.Values = new string[headers.Length];
        row.ExpectedColumnCount = headers.Length;
        return PatentsViewDerivedTableParser.InspectSchema(row, tableIdentifier, family);
    }

    internal static void ApplyDeclaredSchema(PatentsViewLocalTableInfo info, PatentsViewDerivedTableSchema schema)
    {
        info.DeclaredSchema = schema;
        info.HasDeclaredSchema = true;
        if (!info.HasObservedSchema)
        {
            info.Schema = schema;
            info.HasSchema = true;
        }

        CompareSchemas(info);
    }

    internal static void ApplyObservedSchema(PatentsViewLocalTableInfo info, PatentsViewDerivedTableSchema schema)
    {
        info.ObservedSchema = schema;
        info.HasObservedSchema = true;
        info.Schema = schema;
        info.HasSchema = true;
        CompareSchemas(info);
    }

    internal static void CompareSchemas(PatentsViewLocalTableInfo info)
    {
        if (info.HasDeclaredSchema && info.HasObservedSchema)
        {
            info.HasSchemaComparison = true;
            info.SchemaMatchesManifest = info.DeclaredSchema.Fingerprint.Equals(info.ObservedSchema.Fingerprint, global::System.StringComparison.Ordinal);
        }
    }

    internal static void AddOrMerge(PatentsViewLocalTableInfo candidate, global::System.Collections.Generic.List<PatentsViewLocalTableInfo> tables, global::System.Collections.Generic.Dictionary<string, PatentsViewLocalTableInfo> indexed, int maximumTables)
    {
        string key = CreateTableKey(candidate);
        if (!indexed.ContainsKey(key))
        {
            if (tables.Count >= maximumTables)
            {
                throw new ValidationException("Local table discovery exceeded its configured table limit.", "MaximumTables", "TABLE_LIMIT_EXCEEDED", null);
            }

            indexed.Add(key, candidate);
            tables.Add(candidate);
            return;
        }

        PatentsViewLocalTableInfo existing = indexed[key];
        existing.FromManifest = existing.FromManifest || candidate.FromManifest;
        existing.FromExtraction = existing.FromExtraction || candidate.FromExtraction;
        existing.FromHeaderEvidence = existing.FromHeaderEvidence || candidate.FromHeaderEvidence;
        existing.FileExists = existing.FileExists || candidate.FileExists;
        existing.FileInspected = existing.FileInspected || candidate.FileInspected;
        if (candidate.HasFileSize)
        {
            existing.FileSize = candidate.FileSize;
            existing.HasFileSize = true;
        }

        if (candidate.HasDeclaredSchema)
        {
            ApplyDeclaredSchema(existing, candidate.DeclaredSchema);
        }

        if (candidate.HasObservedSchema)
        {
            ApplyObservedSchema(existing, candidate.ObservedSchema);
        }

        if (existing.InspectionFailureCode == null)
        {
            existing.InspectionFailureCode = candidate.InspectionFailureCode;
        }
    }

    internal static string CreateTableKey(PatentsViewLocalTableInfo info)
    {
        if (info.FilePath != null)
        {
            return "path:" + info.FilePath;
        }

        return "table:" + (int)info.Family + ":" + PatentsViewGrantBulkParser.NormalizeName(info.TableIdentifier);
    }

    internal static DatasetFamily ResolveFamily(DatasetFamily explicitFamily, DatasetFamily defaultFamily, string? productIdentifier, string? path)
    {
        if (explicitFamily != DatasetFamily.Unknown)
        {
            return explicitFamily;
        }

        if (defaultFamily != DatasetFamily.Unknown)
        {
            return defaultFamily;
        }

        DatasetFamily product = BulkParserRouter.DetectRouterProduct(productIdentifier);
        if (IsPatentsViewFamily(product))
        {
            return product;
        }

        string? fileName = null;
        if (path != null)
        {
            fileName = global::System.IO.Path.GetFileName(path);
        }

        DatasetFamily file = BulkParserRouter.DetectRouterFileName(fileName);
        if (IsPatentsViewFamily(file))
        {
            return file;
        }

        return DatasetFamily.Unknown;
    }

    internal static bool IsPatentsViewFamily(DatasetFamily family)
    {
        return family == DatasetFamily.PatentsViewGrant || family == DatasetFamily.PatentsViewPreGrant || family == DatasetFamily.PatentsViewAnnualized || family == DatasetFamily.PatentsViewSorted;
    }

    internal static void ValidateEvidenceFamily(DatasetFamily family)
    {
        if (family != DatasetFamily.Unknown && !IsPatentsViewFamily(family))
        {
            throw new ValidationException("Local table discovery supports only PatentsView dataset families.", "Family", "UNSUPPORTED_DATASET_FAMILY", null);
        }
    }

    internal static string ResolveTableIdentifier(string? explicitIdentifier, string? path, string[] headers)
    {
        string? result = PatentsViewDerivedTableParser.ReadTableIdentifier(explicitIdentifier ?? string.Empty);
        if (result == null && path != null)
        {
            result = PatentsViewDerivedTableParser.ReadTableIdentifier(path);
        }

        if (result == null && headers.Length > 0)
        {
            string[] normalized = new string[headers.Length];
            int index;
            for (index = 0; index < headers.Length; index += 1)
            {
                normalized[index] = PatentsViewGrantBulkParser.NormalizeName(headers[index]);
            }

            result = PatentsViewDerivedTableParser.DiscoverTableFromHeaders(normalized);
        }

        return result ?? "table";
    }

    internal static char ResolveDelimiter(char declared, string? path)
    {
        if (declared == ',' || declared == '\t')
        {
            return declared;
        }

        if (declared != '\0')
        {
            throw new ValidationException("A local table delimiter must be a comma or tab.", "Delimiter", "UNSUPPORTED_DELIMITER", null);
        }

        if (path != null && path.EndsWith(".csv", global::System.StringComparison.OrdinalIgnoreCase))
        {
            return ',';
        }

        return '\t';
    }

    internal static bool IsDelimitedPath(string path)
    {
        return path.EndsWith(".tsv", global::System.StringComparison.OrdinalIgnoreCase) || path.EndsWith(".csv", global::System.StringComparison.OrdinalIgnoreCase) || path.EndsWith(".txt", global::System.StringComparison.OrdinalIgnoreCase);
    }

    internal static string? ResolveLocalPath(string? mirrorDirectory, string? candidatePath)
    {
        if (global::System.String.IsNullOrWhiteSpace(candidatePath))
        {
            return null;
        }

        string fullPath;
        if (!global::System.String.IsNullOrWhiteSpace(mirrorDirectory) && !global::System.IO.Path.IsPathRooted(candidatePath))
        {
            fullPath = global::System.IO.Path.GetFullPath(global::System.IO.Path.Combine(mirrorDirectory ?? string.Empty, candidatePath));
        }
        else
        {
            fullPath = global::System.IO.Path.GetFullPath(candidatePath);
        }

        if (!global::System.String.IsNullOrWhiteSpace(mirrorDirectory))
        {
            string root = global::System.IO.Path.GetFullPath(mirrorDirectory ?? string.Empty);
            string prefix = root.EndsWith(global::System.IO.Path.DirectorySeparatorChar.ToString(), global::System.StringComparison.Ordinal) ? root : root + global::System.IO.Path.DirectorySeparatorChar;
            if (!fullPath.StartsWith(prefix, global::System.StringComparison.Ordinal))
            {
                throw new ValidationException("A discovered local table path must remain beneath the mirror directory.", "path", "PATH_OUTSIDE_MIRROR", null);
            }
        }

        return fullPath;
    }

    internal static string? CreateRelativePath(string? mirrorDirectory, string? path)
    {
        if (path == null)
        {
            return null;
        }

        if (global::System.String.IsNullOrWhiteSpace(mirrorDirectory))
        {
            return global::System.IO.Path.GetFileName(path);
        }

        return global::System.IO.Path.GetRelativePath(global::System.IO.Path.GetFullPath(mirrorDirectory ?? string.Empty), path).Replace(global::System.IO.Path.DirectorySeparatorChar, '/');
    }

    internal static void ValidateRequest(PatentsViewLocalTableDiscoveryRequest request)
    {
        if (request == null)
        {
            throw new ValidationException("A local table discovery request is required.", "request", "REQUIRED", null);
        }

        if (request.ManifestTables == null || request.ExtractionResults == null || request.ExtractedFiles == null || request.HeaderEvidence == null)
        {
            throw new ValidationException("Local table discovery collections cannot be null.", "request", "REQUIRED", null);
        }

        if (request.Limits == null)
        {
            throw new ValidationException("Local table discovery limits are required.", "Limits", "REQUIRED", null);
        }

        if (request.MaximumTables <= 0)
        {
            throw new ValidationException("The local table discovery limit must be positive.", "MaximumTables", "INVALID_LIMIT", null);
        }

        ValidateEvidenceFamily(request.DefaultFamily);
        if (!global::System.String.IsNullOrWhiteSpace(request.MirrorDirectory))
        {
            string root = global::System.IO.Path.GetFullPath(request.MirrorDirectory ?? string.Empty);
            if (!global::System.IO.Directory.Exists(root))
            {
                throw new ValidationException("The local mirror directory does not exist.", "MirrorDirectory", "DIRECTORY_NOT_FOUND", null);
            }
        }
    }
}
