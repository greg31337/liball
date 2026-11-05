using System;
using System.Collections.Generic;
using System.Text;

namespace odf;

internal class PackageConformanceValidator
{
    OdfSecurityLimits Limits;
    List<OdfDiagnostic> Diagnostics = new List<OdfDiagnostic>();
    List<OdfSignatureContainer> Signatures = new List<OdfSignatureContainer>();
    ZipEntryIndex? Index;

    internal PackageConformanceValidator(OdfSecurityLimits limits)
    {
        if (!LimitsAreValid(limits))
        {
            throw new OdfValidationException(
                "Positive bounded package validation limits are required.");
        }
        Limits = limits;
    }

    internal PackageValidationResult ValidatePackage(OdfPackage package)
    {
        return ValidatePackageCore(package, true);
    }

    internal PackageValidationResult ValidateNestedPackage(OdfPackage package)
    {
        return ValidatePackageCore(package, false);
    }

    PackageValidationResult ValidatePackageCore(
        OdfPackage package,
        bool inspectEmbedded)
    {
        if (package == null)
        {
            throw new OdfValidationException("A package is required for validation.");
        }
        Diagnostics = new List<OdfDiagnostic>();
        Signatures = new List<OdfSignatureContainer>();
        Index = package.EntryIndex;
        AppendExistingDiagnostics(package.Diagnostics);
        if (!package.IsValid && package.Diagnostics.Length == 0)
        {
            AddError(
                OdfDiagnosticCodes.InvalidPackage,
                "The package was not loaded as a valid ZIP package.",
                "");
        }
        InspectPackageEntries(package);
        string mimetype = InspectMimetype(package);
        InspectManifest(package, mimetype);
        InspectSignatureStructures(package);

        EmbeddedPackageNode[] embedded = [];
        if (inspectEmbedded && Index != null)
        {
            EmbeddedPackageTraversal traversal =
                Index.DiscoverEmbeddedPackages(Limits);
            embedded = traversal.Packages;
            AppendExistingDiagnostics(traversal.Diagnostics);
            InspectEmbeddedNodes(embedded, package);
        }

        PackageValidationResult result = new PackageValidationResult();
        result.Diagnostics = Diagnostics.ToArray();
        result.SignatureContainers = Signatures.ToArray();
        result.EmbeddedPackages = embedded;
        result.IsValid = !HasErrorDiagnostics();
        return result;
    }

    void InspectPackageEntries(OdfPackage package)
    {
        if (package.Entries.Length > Limits.MaximumPackageEntries)
        {
            AddFatal(
                OdfDiagnosticCodes.EntryLimitExceeded,
                "The package contains more entries than the configured limit.",
                "");
        }
        Dictionary<string, string> lexicalPaths =
            new Dictionary<string, string>(StringComparer.Ordinal);
        Dictionary<string, string> canonicalPaths =
            new Dictionary<string, string>(StringComparer.Ordinal);
        long totalExpanded = 0;
        int index = 0;
        while (index < package.Entries.Length)
        {
            OdfPackageEntry entry = package.Entries[index];
            PackagePathValue path = PackagePathRules.CanonicalizeEntryPath(
                entry.Path);
            if (entry.Path.Length > Limits.MaximumPathLength)
            {
                AddError(
                    OdfDiagnosticCodes.InvalidEntryPath,
                    "The package entry path exceeds the configured length limit.",
                    entry.Path);
            }
            if (path.IsTraversal)
            {
                AddError(
                    OdfDiagnosticCodes.PathTraversal,
                    "The package entry path attempts to escape the package root.",
                    entry.Path);
            }
            else if (!path.IsValid || !path.IsCanonical
                || !entry.IsCanonicalPath)
            {
                AddError(
                    OdfDiagnosticCodes.InvalidEntryPath,
                    "The package entry path is not canonical.",
                    entry.Path);
            }
            if (lexicalPaths.ContainsKey(entry.Path))
            {
                AddError(
                    OdfDiagnosticCodes.DuplicateEntry,
                    "The package contains a duplicate entry path.",
                    entry.Path);
            }
            else
            {
                lexicalPaths.Add(entry.Path, entry.Path);
            }
            if (path.CanonicalPath.Length > 0)
            {
                if (canonicalPaths.ContainsKey(path.CanonicalPath)
                    && canonicalPaths[path.CanonicalPath] != entry.Path)
                {
                    AddRelatedError(
                        OdfDiagnosticCodes.PathNormalizationCollision,
                        "Package entry paths normalize to the same path.",
                        entry.Path,
                        canonicalPaths[path.CanonicalPath]);
                }
                else if (!canonicalPaths.ContainsKey(path.CanonicalPath))
                {
                    canonicalPaths.Add(path.CanonicalPath, entry.Path);
                }
            }
            if (entry.CompressionMethod == OdfCompressionMethod.Unsupported)
            {
                AddError(
                    OdfDiagnosticCodes.UnsupportedCompression,
                    "The package entry uses unsupported ZIP compression.",
                    entry.Path);
            }
            if (entry.IsEncrypted)
            {
                AddError(
                    OdfDiagnosticCodes.InvalidEncryptionStructure,
                    "OpenDocument entries must use manifest encryption rather than ZIP encryption.",
                    entry.Path);
            }
            if (!entry.IsEncrypted
                && entry.CompressionMethod != OdfCompressionMethod.Unsupported
                && !entry.HasValidCrc)
            {
                AddError(
                    OdfDiagnosticCodes.CrcMismatch,
                    "The package entry CRC does not match its expanded content.",
                    entry.Path);
            }
            if (entry.ExpandedSize < 0
                || entry.ExpandedSize > Limits.MaximumSingleEntryBytes)
            {
                AddFatal(
                    OdfDiagnosticCodes.ExpandedSizeLimitExceeded,
                    "The package entry exceeds the configured expanded-size limit.",
                    entry.Path);
            }
            if (entry.ExpandedSize > long.MaxValue - totalExpanded
                || totalExpanded + entry.ExpandedSize
                    > Limits.MaximumTotalExpandedBytes)
            {
                AddFatal(
                    OdfDiagnosticCodes.ExpandedSizeLimitExceeded,
                    "The package exceeds the configured total expanded-size limit.",
                    entry.Path);
            }
            else
            {
                totalExpanded = totalExpanded + entry.ExpandedSize;
            }
            if (CompressionRatioIsExceeded(entry))
            {
                AddFatal(
                    OdfDiagnosticCodes.CompressionRatioExceeded,
                    "The package entry exceeds the configured compression ratio.",
                    entry.Path);
            }
            index = index + 1;
        }
    }

    string InspectMimetype(OdfPackage package)
    {
        int count = CountPackageEntries(package, OdfPartNames.Mimetype);
        if (count == 0)
        {
            AddError(
                OdfDiagnosticCodes.MissingMimetype,
                "The package has no mimetype entry.",
                OdfPartNames.Mimetype);
            return package.MediaType;
        }
        if (count > 1)
        {
            AddError(
                OdfDiagnosticCodes.DuplicateEntry,
                "The package contains more than one mimetype entry.",
                OdfPartNames.Mimetype);
        }
        OdfPackageEntry? entry = FindPackageEntry(
            package,
            OdfPartNames.Mimetype);
        if (entry == null)
        {
            return package.MediaType;
        }
        if (!entry.IsFirstEntry)
        {
            AddError(
                OdfDiagnosticCodes.MimetypeNotFirst,
                "The mimetype entry is not the first local ZIP entry.",
                OdfPartNames.Mimetype);
        }
        if (entry.CompressionMethod != OdfCompressionMethod.Stored)
        {
            AddError(
                OdfDiagnosticCodes.MimetypeCompressed,
                "The mimetype entry is not stored without compression.",
                OdfPartNames.Mimetype);
        }
        if (entry.LocalExtraFieldLength != 0
            || entry.CentralExtraFieldLength != 0)
        {
            AddError(
                OdfDiagnosticCodes.MimetypeExtraField,
                "The mimetype entry contains a ZIP extra field.",
                OdfPartNames.Mimetype);
        }
        string mediaType = ReadEditableText(OdfPartNames.Mimetype);
        if (mediaType.Length == 0)
        {
            mediaType = package.MediaType;
        }
        if (!IsOdfMediaType(mediaType))
        {
            AddError(
                OdfDiagnosticCodes.InvalidMimetype,
                "The mimetype entry does not contain a valid OpenDocument media type.",
                OdfPartNames.Mimetype);
        }
        if (package.MediaType.Length > 0
            && mediaType.Length > 0
            && package.MediaType != mediaType)
        {
            AddError(
                OdfDiagnosticCodes.ManifestMediaTypeMismatch,
                "The loaded package media type does not match the mimetype entry.",
                OdfPartNames.Mimetype);
        }
        return mediaType;
    }

    void InspectManifest(OdfPackage package, string mimetype)
    {
        int manifestPartCount = CountPackageEntries(
            package,
            OdfPartNames.Manifest);
        if (manifestPartCount == 0)
        {
            AddError(
                OdfDiagnosticCodes.MissingManifest,
                "The package has no META-INF/manifest.xml entry.",
                OdfPartNames.Manifest);
        }
        else if (manifestPartCount > 1)
        {
            AddError(
                OdfDiagnosticCodes.DuplicateEntry,
                "The package contains more than one manifest part.",
                OdfPartNames.Manifest);
        }
        if (package.Manifest == null)
        {
            AddError(
                OdfDiagnosticCodes.MissingManifest,
                "The package manifest is unavailable or could not be parsed.",
                OdfPartNames.Manifest);
            InspectRequiredContent(package, false);
            return;
        }
        OdfManifest manifest = package.Manifest;
        AppendExistingDiagnostics(manifest.Diagnostics);
        Dictionary<string, string> paths =
            new Dictionary<string, string>(StringComparer.Ordinal);
        int rootCount = 0;
        bool contentDeclared = false;
        bool usesPgp = false;
        int entryIndex = 0;
        while (entryIndex < manifest.Entries.Length)
        {
            OdfManifestEntry entry = manifest.Entries[entryIndex];
            if (paths.ContainsKey(entry.FullPath))
            {
                AddError(
                    OdfDiagnosticCodes.InvalidManifestEntry,
                    "The manifest contains a duplicate full path.",
                    ManifestDiagnosticPath(entry.FullPath));
            }
            else
            {
                paths.Add(entry.FullPath, entry.FullPath);
            }
            if (entry.FullPath == OdfPartNames.Root)
            {
                rootCount = rootCount + 1;
                InspectRootManifestEntry(entry, mimetype);
            }
            else
            {
                if (entry.FullPath == OdfPartNames.Content)
                {
                    contentDeclared = true;
                }
                InspectManifestPartEntry(package, entry);
            }
            if (entry.Encryption != null
                && entry.Encryption.KeyDerivation.IsPgp)
            {
                usesPgp = true;
            }
            entryIndex = entryIndex + 1;
        }
        if (rootCount != 1)
        {
            AddError(
                OdfDiagnosticCodes.InvalidManifestEntry,
                "The manifest must contain exactly one package-root file entry.",
                OdfPartNames.Manifest);
        }
        InspectPhysicalManifestCoverage(package, manifest);
        InspectEncryptedKeys(manifest, usesPgp);
        InspectRequiredContent(package, contentDeclared);
    }

    void InspectRootManifestEntry(
        OdfManifestEntry entry,
        string mimetype)
    {
        if (!IsOdfMediaType(entry.MediaType))
        {
            AddError(
                OdfDiagnosticCodes.InvalidMediaType,
                "The package-root manifest entry has an invalid OpenDocument media type.",
                OdfPartNames.Manifest);
        }
        if (mimetype.Length > 0 && entry.MediaType != mimetype)
        {
            AddError(
                OdfDiagnosticCodes.ManifestMediaTypeMismatch,
                "The package-root manifest media type does not match the mimetype entry.",
                OdfPartNames.Manifest);
        }
        if (entry.Encryption != null || entry.EncryptionData != null)
        {
            AddError(
                OdfDiagnosticCodes.InvalidEncryptionStructure,
                "The package-root manifest entry cannot be encrypted.",
                OdfPartNames.Manifest);
        }
    }

    void InspectManifestPartEntry(
        OdfPackage package,
        OdfManifestEntry entry)
    {
        if (entry.FullPath.Length == 0)
        {
            AddError(
                OdfDiagnosticCodes.InvalidManifestEntry,
                "A manifest file entry has an empty full path.",
                OdfPartNames.Manifest);
            return;
        }
        if (entry.FullPath == OdfPartNames.Mimetype
            || entry.FullPath == OdfPartNames.Manifest)
        {
            AddError(
                OdfDiagnosticCodes.InvalidManifestEntry,
                "The mimetype and manifest infrastructure entries must not list themselves in the manifest.",
                entry.FullPath);
        }
        PackagePathValue path = PackagePathRules.CanonicalizeEntryPath(
            entry.FullPath);
        if (path.IsTraversal)
        {
            AddError(
                OdfDiagnosticCodes.PathTraversal,
                "A manifest file entry attempts to escape the package root.",
                entry.FullPath);
        }
        else if (!path.IsValid || !path.IsCanonical)
        {
            AddError(
                OdfDiagnosticCodes.InvalidManifestEntry,
                "A manifest full path is not canonical.",
                entry.FullPath);
        }
        if (entry.MediaType.Length > 0
            && !OdfMediaTypeRules.ParseMediaType(entry.MediaType).IsValid)
        {
            AddError(
                OdfDiagnosticCodes.InvalidMediaType,
                "A manifest file entry has an invalid media type.",
                entry.FullPath);
        }
        InspectKnownPartMediaType(entry);
        if (!ManifestTargetExists(package, entry.FullPath))
        {
            AddError(
                OdfDiagnosticCodes.ManifestEntryMissing,
                "A manifest entry has no matching package entry.",
                entry.FullPath);
        }
        InspectEncryptionEntry(package, entry);
    }

    void InspectKnownPartMediaType(OdfManifestEntry entry)
    {
        string expected = "";
        if (entry.FullPath == OdfPartNames.Content
            || entry.FullPath == OdfPartNames.Styles
            || entry.FullPath == OdfPartNames.Metadata
            || entry.FullPath == OdfPartNames.Settings
            || IsSignaturePath(entry.FullPath))
        {
            expected = OdfMediaTypes.Xml;
        }
        else if (entry.FullPath == OdfPartNames.MetadataManifest)
        {
            expected = OdfMediaTypes.RdfXml;
        }
        else if (entry.FullPath == OdfPartNames.Thumbnail)
        {
            expected = OdfMediaTypes.Png;
        }
        if (expected.Length > 0 && entry.MediaType != expected)
        {
            AddError(
                OdfDiagnosticCodes.ManifestMediaTypeMismatch,
                "A standard package part has an inconsistent manifest media type.",
                entry.FullPath);
        }
    }

    void InspectPhysicalManifestCoverage(
        OdfPackage package,
        OdfManifest manifest)
    {
        int index = 0;
        while (index < package.Entries.Length)
        {
            OdfPackageEntry entry = package.Entries[index];
            if (!entry.IsDirectory
                && entry.Path != OdfPartNames.Mimetype
                && entry.Path != OdfPartNames.Manifest
                && FindManifestEntry(manifest, entry.Path) == null)
            {
                AddError(
                    OdfDiagnosticCodes.ManifestEntryMissing,
                    "A package entry is absent from the manifest.",
                    entry.Path);
            }
            index = index + 1;
        }
    }

    void InspectRequiredContent(OdfPackage package, bool contentDeclared)
    {
        if (CountPackageEntries(package, OdfPartNames.Content) != 1)
        {
            AddError(
                OdfDiagnosticCodes.MissingRequiredPackagePart,
                "An OpenDocument package must contain exactly one content.xml part.",
                OdfPartNames.Content);
        }
        if (package.Manifest != null && !contentDeclared)
        {
            AddError(
                OdfDiagnosticCodes.ManifestEntryMissing,
                "The required content.xml part is absent from the manifest.",
                OdfPartNames.Content);
        }
    }

    void InspectEncryptionEntry(
        OdfPackage package,
        OdfManifestEntry entry)
    {
        if (entry.EncryptionData != null && entry.Encryption == null)
        {
            AddError(
                OdfDiagnosticCodes.InvalidEncryptionStructure,
                "Manifest encryption data has no typed encryption description.",
                entry.FullPath);
            return;
        }
        if (entry.Encryption == null)
        {
            if (entry.HasSize)
            {
                AddError(
                    OdfDiagnosticCodes.InvalidEncryptionStructure,
                    "A manifest size is present on an unencrypted entry.",
                    entry.FullPath);
            }
            return;
        }
        OdfEncryptionInfo encryption = entry.Encryption;
        if (!encryption.IsValid
            || !encryption.HasSize
            || !encryption.SizeIsValid)
        {
            AddError(
                OdfDiagnosticCodes.InvalidEncryptionStructure,
                "The manifest encryption description is incomplete or invalid.",
                entry.FullPath);
        }
        if (encryption.Method == OdfEncryptionMethod.Unknown
            || encryption.Method == OdfEncryptionMethod.Unsupported)
        {
            AddError(
                OdfDiagnosticCodes.UnsupportedEncryption,
                "The manifest entry uses an unsupported encryption algorithm.",
                entry.FullPath);
        }
        if (!IsSupportedChecksum(encryption.ChecksumType))
        {
            AddError(
                OdfDiagnosticCodes.UnsupportedEncryption,
                "The manifest entry uses an unsupported encryption checksum.",
                entry.FullPath);
        }
        if (!encryption.KeyDerivation.IsPgp
            && encryption.KeyDerivation.Name != OdfAlgorithmIdentifiers.Pbkdf2
            && encryption.KeyDerivation.Name != OdfAlgorithmIdentifiers.Pbkdf2Name)
        {
            AddError(
                OdfDiagnosticCodes.UnsupportedEncryption,
                "The manifest entry uses an unsupported key derivation method.",
                entry.FullPath);
        }
        if (entry.FullPath == OdfPartNames.Mimetype
            || entry.FullPath == OdfPartNames.Manifest
            || IsSignaturePath(entry.FullPath))
        {
            AddError(
                OdfDiagnosticCodes.InvalidEncryptionStructure,
                "Package infrastructure and signature containers cannot carry entry encryption data.",
                entry.FullPath);
        }
        OdfPackageEntry? packageEntry = FindPackageEntry(
            package,
            entry.FullPath);
        if (packageEntry != null
            && packageEntry.CompressionMethod != OdfCompressionMethod.Stored)
        {
            AddError(
                OdfDiagnosticCodes.InvalidEncryptionStructure,
                "A manifest-encrypted package entry must be stored without ZIP compression.",
                entry.FullPath);
        }
    }

    void InspectEncryptedKeys(OdfManifest manifest, bool usesPgp)
    {
        int index = 0;
        while (index < manifest.EncryptedKeyModels.Length)
        {
            if (!manifest.EncryptedKeyModels[index].IsValid)
            {
                AddError(
                    OdfDiagnosticCodes.InvalidEncryptionStructure,
                    "A manifest encrypted-key structure is invalid.",
                    OdfPartNames.Manifest);
            }
            index = index + 1;
        }
        if (usesPgp && manifest.EncryptedKeyModels.Length == 0)
        {
            AddError(
                OdfDiagnosticCodes.InvalidEncryptionStructure,
                "OpenPGP-encrypted entries require at least one encrypted-key structure.",
                OdfPartNames.Manifest);
        }
        if (!usesPgp && manifest.EncryptedKeyModels.Length > 0)
        {
            AddError(
                OdfDiagnosticCodes.InvalidEncryptionStructure,
                "Manifest encrypted-key structures are not referenced by an OpenPGP-encrypted entry.",
                OdfPartNames.Manifest);
        }
    }

    void InspectSignatureStructures(OdfPackage package)
    {
        if (Index == null)
        {
            if (HasSignatureEntry(package))
            {
                AddError(
                    OdfDiagnosticCodes.SignatureInvalid,
                    "Signature containers cannot be inspected without package entry data.",
                    OdfPartNames.MetaInfDirectory);
            }
            return;
        }
        OdfSignatureContainer[] containers =
            SecurityServiceCore.VerifySignatures(package, Limits);
        int index = 0;
        while (index < containers.Length)
        {
            OdfSignatureContainer container = containers[index];
            Signatures.Add(container);
            AppendExistingDiagnostics(container.Diagnostics);
            if (!container.IsValid && container.Diagnostics.Length == 0)
            {
                AddError(
                    OdfDiagnosticCodes.SignatureInvalid,
                    "A package signature container is structurally invalid.",
                    container.PartPath);
            }
            if (package.Manifest != null
                && package.Manifest.Version.Length > 0
                && container.Version.Length > 0
                && package.Manifest.Version != container.Version)
            {
                AddError(
                    OdfDiagnosticCodes.SignatureInvalid,
                    "The signature-container version does not match the manifest version.",
                    container.PartPath);
            }
            index = index + 1;
        }
    }

    void InspectEmbeddedNodes(
        EmbeddedPackageNode[] nodes,
        OdfPackage outerPackage)
    {
        int index = 0;
        while (index < nodes.Length)
        {
            EmbeddedPackageNode node = nodes[index];
            if (!node.IsValid)
            {
                AddError(
                    OdfDiagnosticCodes.InvalidEmbeddedPackage,
                    "An embedded OpenDocument package is invalid.",
                    node.QualifiedPath);
            }
            if (node.IsArchivePackage)
            {
                InspectEmbeddedArchive(node);
            }
            else if (node.IsDirectoryPackage)
            {
                InspectEmbeddedDirectory(node, outerPackage);
            }
            InspectEmbeddedNodes(node.Children, outerPackage);
            index = index + 1;
        }
    }

    void InspectEmbeddedArchive(EmbeddedPackageNode node)
    {
        if (node.MediaType.Length > 0
            && node.DetectedMediaType.Length > 0
            && node.MediaType != node.DetectedMediaType)
        {
            AddError(
                OdfDiagnosticCodes.ManifestMediaTypeMismatch,
                "An embedded package media type does not match its own mimetype entry.",
                node.QualifiedPath);
        }
        if (node.EntryIndex == null)
        {
            return;
        }
        OdfPackage nested = new OdfPackage();
        nested.Entries = node.Entries;
        nested.Manifest = node.Manifest;
        nested.MediaType = node.DetectedMediaType.Length > 0
            ? node.DetectedMediaType
            : node.MediaType;
        nested.ManifestMediaType = RootManifestMediaType(node.Manifest);
        nested.IsValid = node.IsValid;
        nested.EntryIndex = node.EntryIndex;
        PackageConformanceValidator validator =
            new PackageConformanceValidator(Limits);
        PackageValidationResult validation =
            validator.ValidateNestedPackage(nested);
        if (!validation.IsValid)
        {
            AddError(
                OdfDiagnosticCodes.InvalidEmbeddedPackage,
                "An embedded archive package does not satisfy package conformance rules.",
                node.QualifiedPath);
        }
        int diagnosticIndex = 0;
        while (diagnosticIndex < validation.Diagnostics.Length)
        {
            OdfDiagnostic qualified = QualifyDiagnostic(
                validation.Diagnostics[diagnosticIndex],
                node.QualifiedPath);
            AppendOneDiagnostic(qualified);
            diagnosticIndex = diagnosticIndex + 1;
        }
        int signatureIndex = 0;
        while (signatureIndex < validation.SignatureContainers.Length)
        {
            OdfSignatureContainer container =
                validation.SignatureContainers[signatureIndex];
            if (!container.PartPath.StartsWith(
                node.QualifiedPath,
                StringComparison.Ordinal))
            {
                container.PartPath = node.QualifiedPath
                    + "!/"
                    + container.PartPath;
            }
            Signatures.Add(container);
            signatureIndex = signatureIndex + 1;
        }
    }

    void InspectEmbeddedDirectory(
        EmbeddedPackageNode node,
        OdfPackage outerPackage)
    {
        string contentPath = node.Path + OdfPartNames.Content;
        if (FindPackageEntry(outerPackage, contentPath) == null)
        {
            AddError(
                OdfDiagnosticCodes.InvalidEmbeddedPackage,
                "An embedded directory package has no content.xml part.",
                node.QualifiedPath);
        }
        string innerMimetype = ReadEditableText(
            node.Path + OdfPartNames.Mimetype);
        if (innerMimetype.Length > 0
            && node.MediaType.Length > 0
            && innerMimetype != node.MediaType)
        {
            AddError(
                OdfDiagnosticCodes.ManifestMediaTypeMismatch,
                "An embedded directory media type does not match its mimetype entry.",
                node.QualifiedPath);
        }
    }

    bool CompressionRatioIsExceeded(OdfPackageEntry entry)
    {
        if (entry.ExpandedSize < 0)
        {
            return true;
        }
        if (entry.CompressedSize < 0)
        {
            return false;
        }
        if (entry.CompressedSize == 0)
        {
            return entry.ExpandedSize > 0;
        }
        long quotient = entry.ExpandedSize / entry.CompressedSize;
        long remainder = entry.ExpandedSize % entry.CompressedSize;
        return quotient > Limits.MaximumCompressionRatio
            || quotient == Limits.MaximumCompressionRatio && remainder > 0;
    }

    bool ManifestTargetExists(OdfPackage package, string path)
    {
        if (FindPackageEntry(package, path) != null)
        {
            return true;
        }
        if (!path.EndsWith("/", StringComparison.Ordinal))
        {
            return false;
        }
        int index = 0;
        while (index < package.Entries.Length)
        {
            if (package.Entries[index].Path.StartsWith(
                path,
                StringComparison.Ordinal))
            {
                return true;
            }
            index = index + 1;
        }
        return false;
    }

    OdfPackageEntry? FindPackageEntry(OdfPackage package, string path)
    {
        int index = 0;
        while (index < package.Entries.Length)
        {
            OdfPackageEntry entry = package.Entries[index];
            if (entry.Path == path || entry.CanonicalPath == path)
            {
                return entry;
            }
            index = index + 1;
        }
        return null;
    }

    OdfManifestEntry? FindManifestEntry(
        OdfManifest manifest,
        string path)
    {
        int index = 0;
        while (index < manifest.Entries.Length)
        {
            if (manifest.Entries[index].FullPath == path)
            {
                return manifest.Entries[index];
            }
            index = index + 1;
        }
        return null;
    }

    int CountPackageEntries(OdfPackage package, string path)
    {
        int result = 0;
        int index = 0;
        while (index < package.Entries.Length)
        {
            if (package.Entries[index].Path == path)
            {
                result = result + 1;
            }
            index = index + 1;
        }
        return result;
    }

    string ReadEditableText(string path)
    {
        if (Index == null)
        {
            return "";
        }
        EditablePackageEntry[] entries = Index.GetEditableEntries();
        int index = 0;
        while (index < entries.Length)
        {
            if (entries[index].Entry.Path == path
                && entries[index].IsContentAvailable)
            {
                return Encoding.UTF8.GetString(entries[index].Data);
            }
            index = index + 1;
        }
        return "";
    }

    string RootManifestMediaType(OdfManifest? manifest)
    {
        if (manifest == null)
        {
            return "";
        }
        int index = 0;
        while (index < manifest.Entries.Length)
        {
            if (manifest.Entries[index].FullPath == OdfPartNames.Root)
            {
                return manifest.Entries[index].MediaType;
            }
            index = index + 1;
        }
        return "";
    }

    bool HasSignatureEntry(OdfPackage package)
    {
        return FindPackageEntry(package, OdfPartNames.DocumentSignatures) != null
            || FindPackageEntry(package, OdfPartNames.MacroSignatures) != null
            || FindPackageEntry(package, OdfPartNames.PackageSignatures) != null;
    }

    bool IsSignaturePath(string path)
    {
        return path == OdfPartNames.DocumentSignatures
            || path == OdfPartNames.MacroSignatures
            || path == OdfPartNames.PackageSignatures;
    }

    bool IsOdfMediaType(string value)
    {
        return value.StartsWith(
                "application/vnd.oasis.opendocument.",
                StringComparison.Ordinal)
            && OdfMediaTypeRules.ParseMediaType(value).IsValid;
    }

    bool IsSupportedChecksum(string value)
    {
        return value == OdfAlgorithmIdentifiers.Sha1OneKilobyteName
            || value == OdfAlgorithmIdentifiers.Sha1OneKilobyte
            || value == OdfAlgorithmIdentifiers.Sha256OneKilobyte
            || value == OdfAlgorithmIdentifiers.Sha1
            || value == OdfAlgorithmIdentifiers.Sha256
            || value == OdfAlgorithmIdentifiers.Sha384
            || value == OdfAlgorithmIdentifiers.Sha512;
    }

    string ManifestDiagnosticPath(string path)
    {
        return path.Length == 0 ? OdfPartNames.Manifest : path;
    }

    OdfDiagnostic QualifyDiagnostic(
        OdfDiagnostic source,
        string packagePath)
    {
        OdfDiagnostic result = CopyDiagnostic(source);
        if (!result.Location.PartPath.StartsWith(
            packagePath,
            StringComparison.Ordinal))
        {
            result.Location.PartPath = result.Location.PartPath.Length == 0
                ? packagePath
                : packagePath + "!/" + result.Location.PartPath;
        }
        if (result.HasRelatedLocation
            && !result.RelatedLocation.PartPath.StartsWith(
                packagePath,
                StringComparison.Ordinal))
        {
            result.RelatedLocation.PartPath =
                result.RelatedLocation.PartPath.Length == 0
                ? packagePath
                : packagePath + "!/" + result.RelatedLocation.PartPath;
        }
        return result;
    }

    OdfDiagnostic CopyDiagnostic(OdfDiagnostic source)
    {
        return OdfDiagnosticFactory.Normalize(source, OdfVersion.Unknown);
    }

    void AppendExistingDiagnostics(OdfDiagnostic[] diagnostics)
    {
        int index = 0;
        while (index < diagnostics.Length)
        {
            AppendOneDiagnostic(diagnostics[index]);
            index = index + 1;
        }
    }

    void AppendOneDiagnostic(OdfDiagnostic diagnostic)
    {
        OdfDiagnostic normalized = OdfDiagnosticFactory.Normalize(
            diagnostic,
            OdfVersion.Unknown);
        if (!ContainsDiagnostic(normalized))
        {
            Diagnostics.Add(normalized);
        }
    }

    bool ContainsDiagnostic(OdfDiagnostic diagnostic)
    {
        int index = 0;
        while (index < Diagnostics.Count)
        {
            OdfDiagnostic current = Diagnostics[index];
            if (current.Code == diagnostic.Code
                && current.Message == diagnostic.Message
                && current.Location.PartPath == diagnostic.Location.PartPath)
            {
                return true;
            }
            index = index + 1;
        }
        return false;
    }

    void AddError(string code, string message, string path)
    {
        OdfDiagnostic diagnostic = CreateDiagnostic(
            OdfValidationSeverity.Error,
            code,
            message,
            path);
        AppendOneDiagnostic(diagnostic);
    }

    void AddFatal(string code, string message, string path)
    {
        OdfDiagnostic diagnostic = CreateDiagnostic(
            OdfValidationSeverity.Fatal,
            code,
            message,
            path);
        AppendOneDiagnostic(diagnostic);
    }

    void AddRelatedError(
        string code,
        string message,
        string path,
        string relatedPath)
    {
        OdfDiagnostic diagnostic = CreateDiagnostic(
            OdfValidationSeverity.Error,
            code,
            message,
            path);
        diagnostic.HasRelatedLocation = true;
        diagnostic.RelatedLocation.PartPath = relatedPath;
        AppendOneDiagnostic(diagnostic);
    }

    OdfDiagnostic CreateDiagnostic(
        OdfValidationSeverity severity,
        string code,
        string message,
        string path)
    {
        OdfLocation location = new OdfLocation();
        location.PartPath = path;
        return OdfDiagnosticFactory.Create(
            code,
            message,
            severity,
            location,
            null,
            OdfVersion.Unknown);
    }

    bool HasErrorDiagnostics()
    {
        int index = 0;
        while (index < Diagnostics.Count)
        {
            OdfValidationSeverity severity = Diagnostics[index].Severity;
            if (severity == OdfValidationSeverity.Error
                || severity == OdfValidationSeverity.Fatal)
            {
                return true;
            }
            index = index + 1;
        }
        return false;
    }

    bool LimitsAreValid(OdfSecurityLimits limits)
    {
        return limits != null
            && limits.MaximumPackageEntries > 0
            && limits.MaximumTotalExpandedBytes > 0
            && limits.MaximumSingleEntryBytes > 0
            && limits.MaximumCompressionRatio > 0
            && limits.MaximumPathLength > 0
            && limits.MaximumXmlDepth > 0
            && limits.MaximumXmlAttributesPerElement >= 0
            && limits.MaximumXmlTextLength >= 0
            && limits.MaximumEmbeddedPackageDepth >= 0;
    }
}
