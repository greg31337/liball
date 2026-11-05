using System;
using System.Collections.Generic;
using System.IO;

namespace iso20022;

/// <summary>Imports raw or archived ISO 20022 repositories into normalized snapshots.</summary>
public class RepositoryImporter : IRepositoryImporter
{
    /// <summary>Initializes a repository importer.</summary>
    public RepositoryImporter()
    {
    }

    /// <summary>Imports one raw XML repository stream.</summary>
    /// <param name="input">The raw repository stream.</param><param name="options">The import options.</param><returns>The import result.</returns>
    public RepositoryImportResult ImportRawStream(
        Stream? input,
        RepositoryImportOptions? options)
    {
        if (input == null || options == null)
        {
            return CreateImportInputFailure(input == null, options == null);
        }

        return ImportArtifact(input, options, false, options.SourceName);
    }

    /// <summary>Imports one ZIP repository stream.</summary>
    /// <param name="input">The ZIP artifact stream.</param><param name="options">The import options.</param><returns>The import result.</returns>
    public RepositoryImportResult ImportZipStream(
        Stream? input,
        RepositoryImportOptions? options)
    {
        if (input == null || options == null)
        {
            return CreateImportInputFailure(input == null, options == null);
        }

        return ImportArtifact(input, options, true, options.SourceName);
    }

    /// <summary>Imports one raw XML repository file.</summary>
    /// <param name="path">The raw repository path.</param><param name="options">The import options.</param><returns>The import result.</returns>
    public RepositoryImportResult ImportRawFile(
        string? path,
        RepositoryImportOptions? options)
    {
        if (path == null || options == null)
        {
            return CreateImportInputFailure(path == null, options == null);
        }

        options.CancellationToken.ThrowIfCancellationRequested();
        using (FileStream input = File.OpenRead(path))
        {
            return ImportArtifact(input, options, false, path);
        }
    }

    /// <summary>Imports one ZIP repository file.</summary>
    /// <param name="path">The ZIP artifact path.</param><param name="options">The import options.</param><returns>The import result.</returns>
    public RepositoryImportResult ImportZipFile(
        string? path,
        RepositoryImportOptions? options)
    {
        if (path == null || options == null)
        {
            return CreateImportInputFailure(path == null, options == null);
        }

        options.CancellationToken.ThrowIfCancellationRequested();
        using (FileStream input = File.OpenRead(path))
        {
            return ImportArtifact(input, options, true, path);
        }
    }

    internal static RepositoryImportResult ImportArtifact(
        Stream input,
        RepositoryImportOptions options,
        bool expectArchive,
        string sourceName)
    {
        if (options.Limits == null)
        {
            return CreateImportFailure(
                "repository.import.limits_missing",
                "Repository import limits are required.");
        }

        options.CancellationToken.ThrowIfCancellationRequested();
        RepositoryArtifactOpenResult opened = RepositoryArtifactOpener.OpenArtifact(
            input,
            CreateArtifactOptions(options));
        if (!opened.Success)
        {
            return CreateArtifactFailure(opened);
        }

        if (opened.WasArchive != expectArchive)
        {
            if (expectArchive)
            {
                return CreateOpenedFailure(
                    opened,
                    "repository.import.zip_required",
                    "The ZIP import operation requires an archived repository artifact.");
            }

            return CreateOpenedFailure(
                opened,
                "repository.import.raw_required",
                "The raw import operation requires an unarchived XML repository artifact.");
        }

        Stream content = RequireOpenedContent(opened);

        string effectiveSourceName = sourceName;
        if (opened.EntryName.Length > 0)
        {
            effectiveSourceName = opened.EntryName;
        }

        using (content)
        {
            return ImportOpenedContent(
                content,
                opened,
                options,
                effectiveSourceName);
        }
    }

    internal static RepositoryImportResult ImportOpenedContent(
        Stream content,
        RepositoryArtifactOpenResult opened,
        RepositoryImportOptions options,
        string sourceName)
    {
        options.CancellationToken.ThrowIfCancellationRequested();
        EcoreMetamodelDiscoveryResult discovery = EcoreMetamodelDiscovery.DiscoverMetamodel(
            content,
            sourceName,
            SelectCharacterLimit(options));
        if (!discovery.Success)
        {
            return CreateDiscoveryFailure(opened, discovery);
        }
        Iso20022Ecore2013Adapter adapter = RequireDiscoveryAdapter(discovery);

        options.CancellationToken.ThrowIfCancellationRequested();
        XmiPassOneOptions passOneOptions = CreatePassOneOptions(options);
        XmiPassOneResult passOne = XmiPassOneReader.BuildIndex(
            content,
            sourceName,
            adapter,
            passOneOptions);
        options.CancellationToken.ThrowIfCancellationRequested();
        XmiPassTwoOptions passTwoOptions = CreatePassTwoOptions(options, passOneOptions);
        XmiPassTwoResult graph = XmiPassTwoReader.BuildGraph(
            content,
            sourceName,
            adapter,
            passOne,
            passTwoOptions);
        options.CancellationToken.ThrowIfCancellationRequested();
        RepositorySnapshot snapshot = BuildSnapshot(graph, opened, discovery, options);
        RepositorySnapshotIndex index = RepositorySnapshotIndexer.BuildIndex(snapshot);
        RepositoryIntegrityResult integrity = RepositoryIntegrityChecker.ValidateSnapshot(
            snapshot,
            index,
            options.Limits.MaxDiagnosticCount);
        return CreateImportSuccessOrIntegrityFailure(
            snapshot,
            opened,
            passOne,
            graph,
            index,
            integrity);
    }

    internal static RepositorySnapshot BuildSnapshot(
        XmiPassTwoResult graph,
        RepositoryArtifactOpenResult opened,
        EcoreMetamodelDiscoveryResult discovery,
        RepositoryImportOptions options)
    {
        RepositorySnapshot snapshot = new RepositorySnapshot();
        DataDictionaryImporter.PopulateDataDictionary(graph, snapshot);
        BusinessProcessCatalogueImporter.PopulateBusinessProcessCatalogue(graph, snapshot);
        MessageModelImporter.PopulateMessageModel(graph, snapshot);
        RepositoryUnknownContentImporter.PopulateUnknownContent(graph, snapshot);
        DeduplicateSnapshotReferences(snapshot);
        snapshot.Edition = options.Coordinate.Edition;
        snapshot.Provenance = opened.Provenance;
        snapshot.Provenance.RepositoryNamespace = discovery.RootName.NamespaceUri;
        snapshot.Provenance.MetamodelNamespace = discovery.MetamodelNamespace;
        snapshot.Provenance.MetamodelVersion = discovery.MetamodelVersion;
        snapshot.Provenance.AdapterIdentifier = discovery.AdapterIdentifier;
        snapshot.Provenance.ImportedAt = options.ImportedAt;
        return snapshot;
    }

    internal static void DeduplicateSnapshotReferences(RepositorySnapshot snapshot)
    {
        HashSet<string> seen = new HashSet<string>(StringComparer.Ordinal);
        List<RepositoryReference> unique = new List<RepositoryReference>();
        int referenceIndex = 0;
        while (referenceIndex < snapshot.References.Count)
        {
            RepositoryReference reference = snapshot.References[referenceIndex];
            string key = CreateReferenceIdentityKey(reference);
            if (!seen.Contains(key))
            {
                seen.Add(key);
                unique.Add(reference);
            }

            referenceIndex++;
        }

        snapshot.References = unique;
    }

    internal static string CreateReferenceIdentityKey(RepositoryReference reference)
    {
        string sourceName = reference.Location.SourceName ?? string.Empty;
        return reference.SourceIdentifier.Length + ":" + reference.SourceIdentifier + "|" +
            (int)reference.SourceKind + "|" +
            reference.PropertyName.Length + ":" + reference.PropertyName + "|" +
            reference.Position + "|" +
            reference.TargetIdentifier.Length + ":" + reference.TargetIdentifier + "|" +
            (int)reference.TargetKind + "|" +
            reference.IsContainment + "|" + reference.IsResolved + "|" +
            reference.ErrorCode.Length + ":" + reference.ErrorCode + "|" +
            sourceName.Length + ":" + sourceName + "|" +
            reference.Location.HasLineInformation + "|" +
            reference.Location.LineNumber + "|" + reference.Location.ColumnNumber;
    }

    internal static RepositoryArtifactOpenOptions CreateArtifactOptions(
        RepositoryImportOptions options)
    {
        RepositoryArtifactOpenOptions result = new RepositoryArtifactOpenOptions();
        result.MaxArtifactBytes = options.MaxArtifactBytes;
        result.MaxArchiveEntryCount = options.MaxArchiveEntryCount;
        result.MaxArchiveEntryBytes = options.MaxArchiveEntryBytes;
        result.MaxArchiveExpandedBytes = options.MaxArchiveExpandedBytes;
        result.MaxArchiveCompressionRatio = options.MaxArchiveCompressionRatio;
        result.EntryName = options.ArchiveEntryName;
        result.Coordinate = options.Coordinate;
        result.OriginUri = options.OriginUri;
        result.RetrievedAt = options.RetrievedAt;
        result.PublicationDate = options.PublicationDate;
        result.LicenseNotice = options.LicenseNotice;
        result.IsApproved = options.IsApproved;
        return result;
    }

    internal static XmiPassOneOptions CreatePassOneOptions(RepositoryImportOptions options)
    {
        XmiPassOneOptions result = new XmiPassOneOptions();
        result.MaximumCharacters = SelectCharacterLimit(options);
        result.MaximumDepth = options.Limits.MaxDepth;
        result.MaximumAttributesPerElement = options.Limits.MaxAttributesPerElement;
        result.MaximumElementCount = options.Limits.MaxElementCount;
        result.MaximumIdentifierCount = options.Limits.MaxElementCount;
        result.MaximumDuplicateCount = options.Limits.MaxDiagnosticCount;
        return result;
    }

    internal static XmiPassTwoOptions CreatePassTwoOptions(
        RepositoryImportOptions options,
        XmiPassOneOptions readerLimits)
    {
        XmiPassTwoOptions result = new XmiPassTwoOptions();
        result.ReaderLimits = readerLimits;
        result.MaximumAttributeCount = options.MaxAttributeCount;
        result.MaximumRelationshipCount = options.MaxRelationshipCount;
        result.MaximumDiagnosticCount = options.Limits.MaxDiagnosticCount;
        result.MaximumAttributeValueLength = options.Limits.MaxTextLength;
        return result;
    }

    internal static int SelectCharacterLimit(RepositoryImportOptions options)
    {
        if (options.MaxArtifactBytes > options.MaxArchiveEntryBytes)
        {
            return options.MaxArtifactBytes;
        }

        return options.MaxArchiveEntryBytes;
    }

    internal static RepositoryImportResult CreateImportSuccessOrIntegrityFailure(
        RepositorySnapshot snapshot,
        RepositoryArtifactOpenResult opened,
        XmiPassOneResult passOne,
        XmiPassTwoResult graph,
        RepositorySnapshotIndex index,
        RepositoryIntegrityResult integrity)
    {
        RepositoryImportResult result = new RepositoryImportResult();
        result.Success = integrity.IsValid;
        result.Snapshot = snapshot;
        result.Artifact = opened.Manifest;
        result.Provenance = snapshot.Provenance;
        result.SourceEntityCount = graph.Entities.Count;
        result.SourceReferenceCount = graph.Relationships.Count;
        result.SourceDuplicateIdentifierCount = passOne.Duplicates.Count;
        result.SourceUnresolvedReferenceCount = graph.UnresolvedReferenceCount;
        result.NormalizedReferenceCount = snapshot.References.Count;
        result.IndexedEntityCount = index.Entries.Count;
        int diagnosticIndex = 0;
        while (diagnosticIndex < integrity.Diagnostics.Count)
        {
            RepositoryIntegrityDiagnostic diagnostic = integrity.Diagnostics[diagnosticIndex];
            result.DiagnosticCodes.Add(diagnostic.Code);
            result.DiagnosticMessages.Add(diagnostic.Message);
            diagnosticIndex++;
        }

        if (!integrity.IsValid)
        {
            result.ErrorCode = "repository.import.integrity";
            result.ErrorMessage = "The normalized repository failed integrity validation.";
        }

        return result;
    }

    internal static RepositoryImportResult CreateImportInputFailure(
        bool inputMissing,
        bool optionsMissing)
    {
        if (inputMissing)
        {
            return CreateImportFailure(
                "repository.import.input_missing",
                "Repository input is required.");
        }

        if (optionsMissing)
        {
            return CreateImportFailure(
                "repository.import.options_missing",
                "Repository import options are required.");
        }

        return CreateImportFailure(
            "repository.import.input_invalid",
            "Repository import input is invalid.");
    }

    internal static RepositoryImportResult CreateArtifactFailure(
        RepositoryArtifactOpenResult opened)
    {
        RepositoryImportResult result = CreateImportFailure(
            opened.ErrorCode,
            opened.ErrorMessage);
        result.Artifact = opened.Manifest;
        result.Provenance = opened.Provenance;
        return result;
    }

    internal static RepositoryImportResult CreateOpenedFailure(
        RepositoryArtifactOpenResult opened,
        string code,
        string message)
    {
        RepositoryImportResult result = CreateImportFailure(code, message);
        result.Artifact = opened.Manifest;
        result.Provenance = opened.Provenance;
        if (opened.Content != null)
        {
            opened.Content.Dispose();
        }

        return result;
    }

    internal static RepositoryImportResult CreateDiscoveryFailure(
        RepositoryArtifactOpenResult opened,
        EcoreMetamodelDiscoveryResult discovery)
    {
        RepositoryImportResult result = CreateImportFailure(
            discovery.ErrorCode,
            discovery.ErrorMessage);
        result.Artifact = opened.Manifest;
        result.Provenance = opened.Provenance;
        return result;
    }

    internal static RepositoryImportResult CreateImportFailure(string code, string message)
    {
        RepositoryImportResult result = new RepositoryImportResult();
        result.ErrorCode = code;
        result.ErrorMessage = message;
        return result;
    }

    internal static Stream RequireOpenedContent(RepositoryArtifactOpenResult opened)
    {
        if (opened.Content == null)
        {
            throw new InvalidOperationException(
                "A successful repository artifact open must provide content.");
        }
        return opened.Content;
    }

    internal static Iso20022Ecore2013Adapter RequireDiscoveryAdapter(
        EcoreMetamodelDiscoveryResult discovery)
    {
        if (discovery.Adapter == null)
        {
            throw new InvalidOperationException(
                "A successful repository metamodel discovery must provide an adapter.");
        }
        return discovery.Adapter;
    }
}
