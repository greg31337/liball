using System;
using System.Collections.Generic;
using System.Globalization;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

namespace odf;

internal class SecurityEncryptionMutation
{
    internal string Path = "";
    internal byte[] EncryptedBytes = [];
    internal long OriginalSize;
    internal OdfManifestNode EncryptionData = new OdfManifestNode();
}

internal class SecurityDecryptionMutation
{
    internal string Path = "";
    internal byte[] PlaintextBytes = [];
}

internal static class SecurityServiceCore
{
    const int IterationCount = 100000;
    const int SaltSize = 16;

    internal static OdfEditResult EncryptEntry(
        OdfPackage package,
        string path,
        string password,
        OdfEncryptionMethod method,
        OdfVersion version,
        OdfSecurityLimits limits)
    {
        ZipEntryIndex index = RequireSecurityIndex(package, password, limits);
        ValidateEncryptionMethod(method, version);
        if (package.Manifest == null)
        {
            return CreateEditResult(
                OdfEditStatus.ValidationFailed,
                false,
                0,
                "Package encryption requires a manifest.",
                path);
        }
        PackagePathValue canonical = PackagePathRules.CanonicalizeEntryPath(path);
        if (!canonical.IsValid || canonical.IsDirectory)
        {
            return CreateEditResult(
                OdfEditStatus.ValidationFailed,
                false,
                0,
                "The package entry path is not valid for encryption.",
                path);
        }
        if (!IsEncryptablePath(canonical.CanonicalPath))
        {
            return CreateEditResult(
                OdfEditStatus.ValidationFailed,
                false,
                0,
                "The selected ODF package entry must remain unencrypted.",
                canonical.CanonicalPath);
        }
        EditablePackageEntry? entry = FindEditableEntry(
            index.GetEditableEntries(),
            canonical.CanonicalPath);
        if (entry == null || !entry.IsContentAvailable)
        {
            return CreateEditResult(
                OdfEditStatus.Conflict,
                false,
                0,
                "The selected package entry is missing or unavailable.",
                canonical.CanonicalPath);
        }
        OdfManifestEntry? manifestEntry = FindManifestEntry(
            package.Manifest,
            canonical.CanonicalPath);
        if (manifestEntry == null)
        {
            return CreateEditResult(
                OdfEditStatus.ValidationFailed,
                false,
                0,
                "The selected package entry has no manifest entry.",
                canonical.CanonicalPath);
        }
        if (manifestEntry.Encryption != null)
        {
            return CreateEditResult(
                OdfEditStatus.NoChange,
                false,
                0,
                "The selected package entry is already encrypted.",
                canonical.CanonicalPath);
        }
        SecurityEncryptionMutation mutation = CreateEncryptionMutation(
            entry,
            password,
            method,
            version);
        ApplyEncryptionMutations(
            package,
            index,
            [mutation]);
        return CreateEditResult(
            OdfEditStatus.Success,
            true,
            1,
            "Encrypted the selected package entry.",
            canonical.CanonicalPath);
    }

    internal static OdfEditResult DecryptEntry(
        OdfPackage package,
        string path,
        string password,
        OdfVersion version,
        OdfSecurityLimits limits)
    {
        ZipEntryIndex index = RequireSecurityIndex(package, password, limits);
        PackagePathValue canonical = PackagePathRules.CanonicalizeEntryPath(path);
        if (!canonical.IsValid || canonical.IsDirectory)
        {
            return CreateEditResult(
                OdfEditStatus.ValidationFailed,
                false,
                0,
                "The package entry path is not valid for decryption.",
                path);
        }
        if (package.Manifest == null)
        {
            return CreateEditResult(
                OdfEditStatus.ValidationFailed,
                false,
                0,
                "Package decryption requires a manifest.",
                canonical.CanonicalPath);
        }
        EditablePackageEntry? entry = FindEditableEntry(
            index.GetEditableEntries(),
            canonical.CanonicalPath);
        OdfManifestEntry? manifestEntry = FindManifestEntry(
            package.Manifest,
            canonical.CanonicalPath);
        if (entry == null || !entry.IsContentAvailable || manifestEntry == null)
        {
            return CreateEditResult(
                OdfEditStatus.Conflict,
                false,
                0,
                "The selected encrypted package entry is missing or unavailable.",
                canonical.CanonicalPath);
        }
        if (manifestEntry.Encryption == null)
        {
            return CreateEditResult(
                OdfEditStatus.NoChange,
                false,
                0,
                "The selected package entry is not encrypted.",
                canonical.CanonicalPath);
        }
        if (manifestEntry.Encryption.KeyDerivation.IsPgp)
        {
            return CreateEditResult(
                OdfEditStatus.ValidationFailed,
                false,
                0,
                "The selected entry requires caller-supplied OpenPGP key material.",
                canonical.CanonicalPath);
        }
        SecurityDecryptionMutation mutation = CreateDecryptionMutation(
            entry,
            manifestEntry.Encryption,
            password,
            version,
            limits);
        ApplyDecryptionMutations(package, index, [mutation]);
        return CreateEditResult(
            OdfEditStatus.Success,
            true,
            1,
            "Decrypted the selected package entry.",
            canonical.CanonicalPath);
    }

    internal static OdfEditResult DecryptEntryWithOpenPgp(
        OdfPackage package,
        string path,
        IOdfOpenPgpKeyProvider provider,
        OdfEncryptedKey encryptedKey,
        OdfVersion version,
        OdfSecurityLimits limits)
    {
        ValidateInspectionInputs(package, limits);
        ZipEntryIndex index = PackageApiModel.RequireEntryIndex(package);
        PackagePathValue canonical = PackagePathRules.CanonicalizeEntryPath(path);
        if (!canonical.IsValid || canonical.IsDirectory)
        {
            return CreateEditResult(
                OdfEditStatus.ValidationFailed,
                false,
                0,
                "The package entry path is not valid for decryption.",
                path);
        }
        EditablePackageEntry? entry = FindEditableEntry(
            index.GetEditableEntries(),
            canonical.CanonicalPath);
        OdfManifestEntry? manifestEntry = FindManifestEntry(
            package.Manifest,
            canonical.CanonicalPath);
        if (entry == null || !entry.IsContentAvailable || manifestEntry == null)
        {
            return CreateEditResult(
                OdfEditStatus.Conflict,
                false,
                0,
                "The selected encrypted package entry is missing or unavailable.",
                canonical.CanonicalPath);
        }
        if (manifestEntry.Encryption == null)
        {
            return CreateEditResult(
                OdfEditStatus.NoChange,
                false,
                0,
                "The selected package entry is not encrypted.",
                canonical.CanonicalPath);
        }
        if (!manifestEntry.Encryption.KeyDerivation.IsPgp)
        {
            return CreateEditResult(
                OdfEditStatus.ValidationFailed,
                false,
                0,
                "The selected entry does not use OpenPGP key transport.",
                canonical.CanonicalPath);
        }
        SecurityDecryptionMutation mutation = CreateOpenPgpDecryptionMutation(
            entry,
            manifestEntry.Encryption,
            provider,
            encryptedKey,
            version,
            limits);
        ApplyDecryptionMutations(package, index, [mutation]);
        return CreateEditResult(
            OdfEditStatus.Success,
            true,
            1,
            "Decrypted the selected OpenPGP-key-transported package entry.",
            canonical.CanonicalPath);
    }

    internal static OdfEditResult EncryptDocument(
        OdfPackage package,
        string password,
        OdfEncryptionMethod method,
        OdfVersion version,
        OdfSecurityLimits limits)
    {
        ZipEntryIndex index = RequireSecurityIndex(package, password, limits);
        ValidateEncryptionMethod(method, version);
        if (package.Manifest == null)
        {
            return CreateEditResult(
                OdfEditStatus.ValidationFailed,
                false,
                0,
                "Package encryption requires a manifest.",
                "");
        }
        List<SecurityEncryptionMutation> mutations =
            new List<SecurityEncryptionMutation>();
        EditablePackageEntry[] entries = index.GetEditableEntries();
        int entryIndex = 0;
        while (entryIndex < entries.Length)
        {
            EditablePackageEntry entry = entries[entryIndex];
            string path = CanonicalEntryPath(entry);
            OdfManifestEntry? manifestEntry = FindManifestEntry(
                package.Manifest,
                path);
            if (entry.IsContentAvailable
                && !entry.Entry.IsDirectory
                && IsEncryptablePath(path)
                && manifestEntry != null
                && manifestEntry.Encryption == null)
            {
                mutations.Add(CreateEncryptionMutation(
                    entry,
                    password,
                    method,
                    version));
            }
            entryIndex = entryIndex + 1;
        }
        if (mutations.Count == 0)
        {
            return CreateEditResult(
                OdfEditStatus.NoChange,
                false,
                0,
                "The package has no eligible unencrypted document entries.",
                "");
        }
        ApplyEncryptionMutations(
            package,
            index,
            mutations.ToArray());
        return CreateEditResult(
            OdfEditStatus.Success,
            true,
            mutations.Count,
            "Encrypted the eligible package document entries.",
            "");
    }

    internal static OdfEditResult DecryptDocument(
        OdfPackage package,
        string password,
        OdfVersion version,
        OdfSecurityLimits limits)
    {
        ZipEntryIndex index = RequireSecurityIndex(package, password, limits);
        if (package.Manifest == null)
        {
            return CreateEditResult(
                OdfEditStatus.ValidationFailed,
                false,
                0,
                "Package decryption requires a manifest.",
                "");
        }
        List<SecurityDecryptionMutation> mutations =
            new List<SecurityDecryptionMutation>();
        EditablePackageEntry[] entries = index.GetEditableEntries();
        int entryIndex = 0;
        while (entryIndex < entries.Length)
        {
            EditablePackageEntry entry = entries[entryIndex];
            string path = CanonicalEntryPath(entry);
            OdfManifestEntry? manifestEntry = FindManifestEntry(
                package.Manifest,
                path);
            if (entry.IsContentAvailable
                && manifestEntry != null
                && manifestEntry.Encryption != null)
            {
                if (manifestEntry.Encryption.KeyDerivation.IsPgp)
                {
                    ClearDecryptionMutations(mutations.ToArray());
                    return CreateEditResult(
                        OdfEditStatus.ValidationFailed,
                        false,
                        0,
                        "The package contains an entry that requires caller-supplied OpenPGP key material.",
                        path);
                }
                mutations.Add(CreateDecryptionMutation(
                    entry,
                    manifestEntry.Encryption,
                    password,
                    version,
                    limits));
            }
            entryIndex = entryIndex + 1;
        }
        if (mutations.Count == 0)
        {
            return CreateEditResult(
                OdfEditStatus.NoChange,
                false,
                0,
                "The package has no password-encrypted document entries.",
                "");
        }
        ApplyDecryptionMutations(
            package,
            index,
            mutations.ToArray());
        return CreateEditResult(
            OdfEditStatus.Success,
            true,
            mutations.Count,
            "Decrypted the password-encrypted package document entries.",
            "");
    }

    internal static OdfSignatureContainer[] VerifySignatures(
        OdfPackage package,
        OdfSecurityLimits limits)
    {
        ValidateInspectionInputs(package, limits);
        ZipEntryIndex index = PackageApiModel.RequireEntryIndex(package);
        List<OdfSignatureContainer> result = new List<OdfSignatureContainer>();
        AddVerifiedContainer(
            result,
            index,
            OdfPartNames.DocumentSignatures,
            limits);
        AddVerifiedContainer(
            result,
            index,
            OdfPartNames.MacroSignatures,
            limits);
        AddVerifiedContainer(
            result,
            index,
            OdfPartNames.PackageSignatures,
            limits);
        return result.ToArray();
    }

    internal static OdfSignatureContainer SignEntries(
        OdfPackage package,
        string partPath,
        string version,
        string[] entryPaths,
        AsymmetricAlgorithm privateKey,
        X509Certificate2 certificate,
        string signatureMethod,
        OdfSecurityLimits limits)
    {
        ValidateInspectionInputs(package, limits);
        ZipEntryIndex index = PackageApiModel.RequireEntryIndex(package);
        OdfSignatureContainer result = XmlSignatureWriter.CreateContainer(
            index,
            partPath,
            version,
            entryPaths,
            privateKey,
            certificate,
            signatureMethod,
            limits);
        EditablePackageEntry? existing = FindEditableEntry(
            index.GetEditableEntries(),
            partPath);
        OdfEditResult edit;
        if (existing == null)
        {
            edit = index.AddEntry(
                partPath,
                result.OriginalBytes,
                OdfCompressionMethod.Deflated);
        }
        else
        {
            edit = index.ReplaceEntry(partPath, result.OriginalBytes);
        }
        if (edit.Status != OdfEditStatus.Success
            && edit.Status != OdfEditStatus.NoChange)
        {
            throw new OdfSecurityException(
                "The generated signature container could not be stored in the package.");
        }
        PackageApiModel.RefreshPackage(package);
        return result;
    }

    internal static OdfDiagnostic[] InspectIntegrity(
        OdfPackage package,
        OdfSecurityLimits limits)
    {
        ValidateInspectionInputs(package, limits);
        PackageConformanceValidator validator =
            new PackageConformanceValidator(limits);
        return validator.ValidatePackage(package).Diagnostics;
    }

    static ZipEntryIndex RequireSecurityIndex(
        OdfPackage package,
        string password,
        OdfSecurityLimits limits)
    {
        ValidateInspectionInputs(package, limits);
        if (password == null)
        {
            throw new OdfSecurityException(
                "A password value is required for this security operation.");
        }
        return PackageApiModel.RequireEntryIndex(package);
    }

    static void ValidateInspectionInputs(
        OdfPackage package,
        OdfSecurityLimits limits)
    {
        if (package == null)
        {
            throw new OdfSecurityException("A package is required.");
        }
        if (limits == null
            || limits.MaximumSingleEntryBytes <= 0
            || limits.MaximumTotalExpandedBytes <= 0
            || limits.MaximumXmlDepth <= 0
            || limits.MaximumXmlAttributesPerElement < 0
            || limits.MaximumXmlTextLength < 0)
        {
            throw new OdfSecurityException(
                "Security operation limits must be non-negative and bounded.");
        }
    }

    static void ValidateEncryptionMethod(
        OdfEncryptionMethod method,
        OdfVersion version)
    {
        bool supportedVersion = version == OdfVersion.Version10
            || version == OdfVersion.Version11
            || version == OdfVersion.Version12
            || version == OdfVersion.Version13
            || version == OdfVersion.Version14;
        if (!supportedVersion)
        {
            throw new OdfSecurityException(
                "Package encryption requires a supported ODF version.");
        }
        if (method == OdfEncryptionMethod.BlowfishCfb)
        {
            return;
        }
        if (version != OdfVersion.Version10
            && version != OdfVersion.Version11
            && (method == OdfEncryptionMethod.Aes128Cbc
                || method == OdfEncryptionMethod.Aes192Cbc
                || method == OdfEncryptionMethod.Aes256Cbc))
        {
            return;
        }
        throw new OdfSecurityException(
            "The requested encryption method is not supported for the selected ODF version.");
    }

    static SecurityEncryptionMutation CreateEncryptionMutation(
        EditablePackageEntry entry,
        string password,
        OdfEncryptionMethod method,
        OdfVersion version)
    {
        OdfStartKeyGeneration startSettings = CreateStartKeySettings(version);
        OdfKeyDerivation derivationSettings = CreateKeyDerivationSettings(
            method);
        string checksumType = IsLegacyVersion(version)
            ? OdfAlgorithmIdentifiers.Sha1OneKilobyte
            : OdfAlgorithmIdentifiers.Sha256OneKilobyte;
        PackageEncryptionResult encrypted;
        using (SecurityBuffer startKey = Pbkdf2Derivation.CreateStartKey(
            password,
            version,
            startSettings))
        using (SecurityBuffer entryKey = Pbkdf2Derivation.DeriveEntryKey(
            startKey,
            version,
            derivationSettings,
            method))
        {
            if (method == OdfEncryptionMethod.BlowfishCfb)
            {
                encrypted = PackageEncryption.EncryptBlowfishEntry(
                    entry.Data,
                    entryKey,
                    checksumType,
                    version);
            }
            else
            {
                encrypted = PackageEncryption.EncryptAesEntry(
                    entry.Data,
                    entryKey,
                    method,
                    checksumType,
                    version);
            }
        }
        SecurityEncryptionMutation result = new SecurityEncryptionMutation();
        result.Path = CanonicalEntryPath(entry);
        result.EncryptedBytes = encrypted.EncryptedBytes;
        result.OriginalSize = encrypted.UncompressedSize;
        result.EncryptionData = CreateEncryptionDataNode(
            method,
            checksumType,
            encrypted,
            startSettings,
            derivationSettings);
        return result;
    }

    static SecurityDecryptionMutation CreateDecryptionMutation(
        EditablePackageEntry entry,
        OdfEncryptionInfo encryption,
        string password,
        OdfVersion version,
        OdfSecurityLimits limits)
    {
        if (!encryption.IsValid)
        {
            throw new OdfSecurityException(
                "The package entry encryption metadata is invalid.");
        }
        ValidateEncryptionMethod(encryption.Method, version);
        byte[] plaintext;
        using (SecurityBuffer startKey = Pbkdf2Derivation.CreateStartKey(
            password,
            version,
            encryption.StartKeyGeneration))
        using (SecurityBuffer entryKey = Pbkdf2Derivation.DeriveEntryKey(
            startKey,
            version,
            encryption.KeyDerivation,
            encryption.Method))
        {
            long expectedSize = encryption.HasSize ? encryption.Size : -1;
            if (encryption.Method == OdfEncryptionMethod.BlowfishCfb)
            {
                plaintext = PackageEncryption.DecryptBlowfishEntry(
                    entry.Data,
                    entryKey,
                    encryption.Algorithm.InitializationVector,
                    encryption.ChecksumType,
                    encryption.Checksum,
                    expectedSize,
                    limits.MaximumSingleEntryBytes,
                    version);
            }
            else
            {
                plaintext = PackageEncryption.DecryptAesEntry(
                    entry.Data,
                    entryKey,
                    encryption.Algorithm.InitializationVector,
                    encryption.Method,
                    encryption.ChecksumType,
                    encryption.Checksum,
                    expectedSize,
                    limits.MaximumSingleEntryBytes,
                    version);
            }
        }
        SecurityDecryptionMutation result = new SecurityDecryptionMutation();
        result.Path = CanonicalEntryPath(entry);
        result.PlaintextBytes = plaintext;
        return result;
    }

    static SecurityDecryptionMutation CreateOpenPgpDecryptionMutation(
        EditablePackageEntry entry,
        OdfEncryptionInfo encryption,
        IOdfOpenPgpKeyProvider provider,
        OdfEncryptedKey encryptedKey,
        OdfVersion version,
        OdfSecurityLimits limits)
    {
        if (!encryption.IsValid)
        {
            throw new OdfSecurityException(
                "The package entry encryption metadata is invalid.");
        }
        ValidateEncryptionMethod(encryption.Method, version);
        byte[] plaintext;
        using (SecurityBuffer entryKey = OpenPgpKeyMaterial.AcquireEntryKey(
            provider,
            encryptedKey,
            encryption.Method))
        {
            long expectedSize = encryption.HasSize ? encryption.Size : -1;
            if (encryption.Method == OdfEncryptionMethod.BlowfishCfb)
            {
                plaintext = PackageEncryption.DecryptBlowfishEntry(
                    entry.Data,
                    entryKey,
                    encryption.Algorithm.InitializationVector,
                    encryption.ChecksumType,
                    encryption.Checksum,
                    expectedSize,
                    limits.MaximumSingleEntryBytes,
                    version);
            }
            else
            {
                plaintext = PackageEncryption.DecryptAesEntry(
                    entry.Data,
                    entryKey,
                    encryption.Algorithm.InitializationVector,
                    encryption.Method,
                    encryption.ChecksumType,
                    encryption.Checksum,
                    expectedSize,
                    limits.MaximumSingleEntryBytes,
                    version);
            }
        }
        SecurityDecryptionMutation result = new SecurityDecryptionMutation();
        result.Path = CanonicalEntryPath(entry);
        result.PlaintextBytes = plaintext;
        return result;
    }

    static OdfStartKeyGeneration CreateStartKeySettings(OdfVersion version)
    {
        OdfStartKeyGeneration result = new OdfStartKeyGeneration();
        result.Name = IsLegacyVersion(version)
            ? OdfAlgorithmIdentifiers.Sha1
            : OdfAlgorithmIdentifiers.Sha256Odf;
        result.KeySize = IsLegacyVersion(version) ? 20 : 32;
        result.KeySizeLexical = result.KeySize.ToString(
            CultureInfo.InvariantCulture);
        result.HasKeySize = true;
        result.KeySizeIsValid = true;
        result.IsValid = true;
        return result;
    }

    static OdfKeyDerivation CreateKeyDerivationSettings(
        OdfEncryptionMethod method)
    {
        OdfKeyDerivation result = new OdfKeyDerivation();
        result.Name = OdfAlgorithmIdentifiers.Pbkdf2;
        result.Salt = RandomNumberGenerator.GetBytes(SaltSize);
        result.SaltLexical = Convert.ToBase64String(result.Salt);
        result.SaltIsValid = true;
        result.IterationCount = IterationCount;
        result.IterationCountLexical = IterationCount.ToString(
            CultureInfo.InvariantCulture);
        result.HasIterationCount = true;
        result.IterationCountIsValid = true;
        result.KeySize = EncryptionKeySize(method);
        result.KeySizeLexical = result.KeySize.ToString(
            CultureInfo.InvariantCulture);
        result.HasKeySize = true;
        result.KeySizeIsValid = true;
        result.IsValid = true;
        return result;
    }

    static int EncryptionKeySize(OdfEncryptionMethod method)
    {
        if (method == OdfEncryptionMethod.Aes192Cbc)
        {
            return 24;
        }
        if (method == OdfEncryptionMethod.Aes256Cbc)
        {
            return 32;
        }
        return 16;
    }

    static OdfManifestNode CreateEncryptionDataNode(
        OdfEncryptionMethod method,
        string checksumType,
        PackageEncryptionResult encrypted,
        OdfStartKeyGeneration startSettings,
        OdfKeyDerivation derivationSettings)
    {
        OdfManifestNode algorithm = CreateManifestNode("algorithm");
        algorithm.Attributes =
        [
            CreateManifestAttribute(
                "algorithm-name",
                EncryptionAlgorithmName(method)),
            CreateManifestAttribute(
                "initialisation-vector",
                Convert.ToBase64String(encrypted.InitializationVector))
        ];
        OdfManifestNode startKey = CreateManifestNode(
            "start-key-generation");
        startKey.Attributes =
        [
            CreateManifestAttribute(
                "start-key-generation-name",
                startSettings.Name),
            CreateManifestAttribute("key-size", startSettings.KeySizeLexical)
        ];
        OdfManifestNode derivation = CreateManifestNode("key-derivation");
        derivation.Attributes =
        [
            CreateManifestAttribute(
                "key-derivation-name",
                derivationSettings.Name),
            CreateManifestAttribute("salt", derivationSettings.SaltLexical),
            CreateManifestAttribute(
                "iteration-count",
                derivationSettings.IterationCountLexical),
            CreateManifestAttribute(
                "key-size",
                derivationSettings.KeySizeLexical)
        ];
        OdfManifestNode result = CreateManifestNode("encryption-data");
        result.Attributes =
        [
            CreateManifestAttribute("checksum-type", checksumType),
            CreateManifestAttribute(
                "checksum",
                Convert.ToBase64String(encrypted.Checksum))
        ];
        result.Children = [algorithm, startKey, derivation];
        return result;
    }

    static OdfManifestNode CreateManifestNode(string localName)
    {
        OdfManifestNode result = new OdfManifestNode();
        result.Name.NamespaceName = OdfNamespaces.Manifest;
        result.Name.LocalName = localName;
        result.Name.Prefix = "manifest";
        result.IsManifestElement = true;
        return result;
    }

    static OdfAttribute CreateManifestAttribute(
        string localName,
        string value)
    {
        OdfAttribute result = new OdfAttribute();
        result.Name.NamespaceName = OdfNamespaces.Manifest;
        result.Name.LocalName = localName;
        result.Name.Prefix = "manifest";
        result.Value = value;
        return result;
    }

    static string EncryptionAlgorithmName(OdfEncryptionMethod method)
    {
        if (method == OdfEncryptionMethod.BlowfishCfb)
        {
            return OdfAlgorithmIdentifiers.BlowfishCfb;
        }
        if (method == OdfEncryptionMethod.Aes128Cbc)
        {
            return OdfAlgorithmIdentifiers.Aes128Cbc;
        }
        if (method == OdfEncryptionMethod.Aes192Cbc)
        {
            return OdfAlgorithmIdentifiers.Aes192Cbc;
        }
        return OdfAlgorithmIdentifiers.Aes256Cbc;
    }

    static void ApplyEncryptionMutations(
        OdfPackage package,
        ZipEntryIndex index,
        SecurityEncryptionMutation[] mutations)
    {
        int mutationIndex = 0;
        while (mutationIndex < mutations.Length)
        {
            SecurityEncryptionMutation mutation = mutations[mutationIndex];
            OdfEditResult replace = index.ReplaceEntry(
                mutation.Path,
                mutation.EncryptedBytes);
            if (replace.Status != OdfEditStatus.Success)
            {
                throw new OdfSecurityException(
                    "An encrypted package entry could not be stored.");
            }
            OdfManifestEntry? manifestEntry = FindManifestEntry(
                index.Manifest,
                mutation.Path);
            if (manifestEntry == null)
            {
                throw new OdfSecurityException(
                    "An encrypted package entry lost its manifest metadata.");
            }
            manifestEntry.Size = mutation.OriginalSize;
            manifestEntry.SizeLexical = mutation.OriginalSize.ToString(
                CultureInfo.InvariantCulture);
            manifestEntry.HasSize = true;
            manifestEntry.SizeIsValid = true;
            manifestEntry.EncryptionData = mutation.EncryptionData;
            OdfManifest parsingManifest = new OdfManifest();
            manifestEntry.Encryption = ManifestEncryptionParser.ParseEncryptionData(
                mutation.EncryptionData,
                manifestEntry,
                parsingManifest);
            if (!manifestEntry.Encryption.IsValid)
            {
                throw new OdfSecurityException(
                    "Generated package encryption metadata is invalid.");
            }
            mutationIndex = mutationIndex + 1;
        }
        index.SynchronizeManifest();
        PackageApiModel.RefreshPackage(package);
    }

    static void ApplyDecryptionMutations(
        OdfPackage package,
        ZipEntryIndex index,
        SecurityDecryptionMutation[] mutations)
    {
        int mutationIndex = 0;
        while (mutationIndex < mutations.Length)
        {
            SecurityDecryptionMutation mutation = mutations[mutationIndex];
            OdfEditResult replace = index.ReplaceEntry(
                mutation.Path,
                mutation.PlaintextBytes);
            if (replace.Status != OdfEditStatus.Success)
            {
                ClearDecryptionMutations(mutations);
                throw new OdfSecurityException(
                    "A decrypted package entry could not be stored.");
            }
            OdfManifestEntry? manifestEntry = FindManifestEntry(
                index.Manifest,
                mutation.Path);
            if (manifestEntry == null)
            {
                ClearDecryptionMutations(mutations);
                throw new OdfSecurityException(
                    "A decrypted package entry lost its manifest metadata.");
            }
            manifestEntry.EncryptionData = null;
            manifestEntry.Encryption = null;
            manifestEntry.Size = 0;
            manifestEntry.SizeLexical = "";
            manifestEntry.HasSize = false;
            manifestEntry.SizeIsValid = false;
            mutationIndex = mutationIndex + 1;
        }
        index.SynchronizeManifest();
        PackageApiModel.RefreshPackage(package);
        ClearDecryptionMutations(mutations);
    }

    static void ClearDecryptionMutations(
        SecurityDecryptionMutation[] mutations)
    {
        int index = 0;
        while (index < mutations.Length)
        {
            SecurityBuffer.ClearBytes(mutations[index].PlaintextBytes);
            index = index + 1;
        }
    }

    static void AddVerifiedContainer(
        List<OdfSignatureContainer> containers,
        ZipEntryIndex index,
        string path,
        OdfSecurityLimits limits)
    {
        EditablePackageEntry? entry = FindEditableEntry(
            index.GetEditableEntries(),
            path);
        if (entry == null)
        {
            return;
        }
        if (!entry.IsContentAvailable)
        {
            throw new OdfSecurityException(
                "A signature container entry is unavailable.");
        }
        if (entry.Data.LongLength > limits.MaximumSingleEntryBytes)
        {
            throw new OdfSecurityException(
                "A signature container exceeds the configured size limit.");
        }
        OdfSignatureContainer container = XmlSignatureReader.ParseBytes(
            entry.Data,
            path,
            limits);
        XmlSignatureVerifier.VerifyContainer(container, index, limits);
        containers.Add(container);
    }

    static EditablePackageEntry? FindEditableEntry(
        EditablePackageEntry[] entries,
        string path)
    {
        int index = 0;
        while (index < entries.Length)
        {
            string entryPath = CanonicalEntryPath(entries[index]);
            if (entryPath == path)
            {
                return entries[index];
            }
            index = index + 1;
        }
        return null;
    }

    static OdfManifestEntry? FindManifestEntry(
        OdfManifest? manifest,
        string path)
    {
        if (manifest == null)
        {
            return null;
        }
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

    static string CanonicalEntryPath(EditablePackageEntry entry)
    {
        return entry.Entry.CanonicalPath.Length == 0
            ? entry.Entry.Path
            : entry.Entry.CanonicalPath;
    }

    static bool IsEncryptablePath(string path)
    {
        return path != OdfPartNames.Mimetype
            && path != OdfPartNames.Manifest
            && path != OdfPartNames.DocumentSignatures
            && path != OdfPartNames.MacroSignatures
            && path != OdfPartNames.PackageSignatures;
    }

    static bool IsLegacyVersion(OdfVersion version)
    {
        return version == OdfVersion.Version10
            || version == OdfVersion.Version11;
    }

    static OdfEditResult CreateEditResult(
        OdfEditStatus status,
        bool changed,
        int affectedCount,
        string message,
        string path)
    {
        OdfEditResult result = new OdfEditResult();
        result.Status = status;
        result.Changed = changed;
        result.AffectedCount = affectedCount;
        result.Message = message;
        result.Location.PartPath = path;
        return result;
    }
}
