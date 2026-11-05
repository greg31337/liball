namespace uspto;

internal static class DatasetManifestStore
{
    internal const int FormatVersion = 1;
    internal const int MaximumManifestBytes = 16777216;
    internal const string FileName = "dataset-manifest-v1.json";

    internal static string GetPath(string mirrorDirectory)
    {
        if (global::System.String.IsNullOrWhiteSpace(mirrorDirectory))
        {
            throw new ValidationException("A mirror directory is required.", "mirrorDirectory", "REQUIRED", null);
        }

        return global::System.IO.Path.Combine(global::System.IO.Path.GetFullPath(mirrorDirectory), FileName);
    }

    internal static DatasetManifest Save(string mirrorDirectory, DatasetManifest manifest, string[]? secrets)
    {
        string path = GetPath(mirrorDirectory);
        DatasetManifest persisted = CreateSafeManifest(manifest, secrets);
        byte[] bytes = global::System.Text.Encoding.UTF8.GetBytes(Serialize(persisted));
        if (bytes.Length > MaximumManifestBytes)
        {
            throw new ValidationException("The dataset manifest exceeds its size limit.", "manifest", "MANIFEST_TOO_LARGE", null);
        }

        global::System.IO.Directory.CreateDirectory(global::System.IO.Path.GetDirectoryName(path) ?? string.Empty);
        string temporaryPath = path + ".tmp";
        using (global::System.IO.FileStream output = new global::System.IO.FileStream(temporaryPath, global::System.IO.FileMode.Create, global::System.IO.FileAccess.Write, global::System.IO.FileShare.None))
        {
            output.Write(bytes, 0, bytes.Length);
            output.Flush(true);
        }

        global::System.IO.File.Move(temporaryPath, path, true);
        return persisted;
    }

    internal static DatasetManifest Load(string mirrorDirectory)
    {
        string path = GetPath(mirrorDirectory);
        if (!global::System.IO.File.Exists(path))
        {
            throw new ValidationException("The dataset manifest does not exist.", "mirrorDirectory", "MANIFEST_NOT_FOUND", null);
        }

        global::System.IO.FileInfo information = new global::System.IO.FileInfo(path);
        if (information.Length <= 0L || information.Length > MaximumManifestBytes || information.Length > global::System.Int32.MaxValue)
        {
            throw new ValidationException("The dataset manifest size is invalid.", "manifest", "MANIFEST_SIZE_INVALID", null);
        }

        DatasetManifest? result = global::System.Text.Json.JsonSerializer.Deserialize<DatasetManifest>(global::System.IO.File.ReadAllText(path), CreateOptions());
        if (result == null)
        {
            throw new ValidationException("The dataset manifest is empty.", "manifest", "MANIFEST_INVALID", null);
        }

        Validate(result);
        return result;
    }

    internal static string Serialize(DatasetManifest manifest)
    {
        Validate(manifest);
        return global::System.Text.Json.JsonSerializer.Serialize(manifest, CreateOptions());
    }

    internal static global::System.Text.Json.JsonSerializerOptions CreateOptions()
    {
        global::System.Text.Json.JsonSerializerOptions result = new global::System.Text.Json.JsonSerializerOptions();
        result.IncludeFields = true;
        result.WriteIndented = true;
        result.PropertyNamingPolicy = global::System.Text.Json.JsonNamingPolicy.CamelCase;
        return result;
    }

    internal static DatasetManifest CreateSafeManifest(DatasetManifest source, string[]? secrets)
    {
        Validate(source);
        DatasetManifest result = new DatasetManifest();
        result.FormatVersion = source.FormatVersion;
        result.ManifestIdentifier = SafeText(source.ManifestIdentifier, secrets);
        result.CreatedAt = source.CreatedAt;
        result.HasCreatedAt = source.HasCreatedAt;
        result.UpdatedAt = source.UpdatedAt;
        result.HasUpdatedAt = source.HasUpdatedAt;
        result.Provenance = CopyProvenance(source.Provenance, secrets);
        result.Products = new DatasetManifestProduct[source.Products.Length];
        int index;
        for (index = 0; index < source.Products.Length; index += 1)
        {
            result.Products[index] = CopyProduct(source.Products[index], secrets);
        }

        return result;
    }

    internal static DatasetManifestProduct CopyProduct(DatasetManifestProduct source, string[]? secrets)
    {
        DatasetManifestProduct result = new DatasetManifestProduct();
        result.ProductIdentifier = SafeText(source.ProductIdentifier, secrets);
        result.Title = SafeText(source.Title, secrets);
        result.Family = source.Family;
        result.CatalogUri = SafeUri(source.CatalogUri, secrets);
        result.LastModified = source.LastModified;
        result.HasLastModified = source.HasLastModified;
        result.Provenance = CopyProvenance(source.Provenance, secrets);
        result.Files = new DatasetManifestFile[source.Files.Length];
        int index;
        for (index = 0; index < source.Files.Length; index += 1)
        {
            result.Files[index] = CopyFile(source.Files[index], secrets);
        }

        return result;
    }

    internal static DatasetManifestFile CopyFile(DatasetManifestFile source, string[]? secrets)
    {
        DatasetManifestFile result = new DatasetManifestFile();
        result.ProductIdentifier = SafeText(source.ProductIdentifier, secrets);
        result.FileName = SafeText(source.FileName, secrets);
        result.CatalogUri = SafeUri(source.CatalogUri, secrets);
        result.DownloadUri = SafeUri(source.DownloadUri, secrets);
        result.LocalPath = SafeText(source.LocalPath, secrets);
        result.PartialPath = SafeText(source.PartialPath, secrets);
        result.ExtractedPaths = CopyPaths(source.ExtractedPaths, secrets);
        result.Size = source.Size;
        result.HasSize = source.HasSize;
        result.ETag = SafeText(source.ETag, secrets);
        result.LastModified = source.LastModified;
        result.HasLastModified = source.HasLastModified;
        result.DigestAlgorithm = source.DigestAlgorithm;
        result.Digest = SafeText(source.Digest, secrets);
        result.DigestVerified = source.DigestVerified;
        result.Provenance = CopyProvenance(source.Provenance, secrets);
        result.Stages = new DatasetManifestStage[source.Stages.Length];
        int index;
        for (index = 0; index < source.Stages.Length; index += 1)
        {
            DatasetManifestStage stage = new DatasetManifestStage();
            stage.Name = SafeText(source.Stages[index].Name, secrets);
            stage.State = source.Stages[index].State;
            stage.UpdatedAt = source.Stages[index].UpdatedAt;
            stage.HasUpdatedAt = source.Stages[index].HasUpdatedAt;
            stage.FailureCode = SafeText(source.Stages[index].FailureCode, secrets);
            stage.FailureMessage = SafeText(source.Stages[index].FailureMessage, secrets);
            result.Stages[index] = stage;
        }

        return result;
    }

    internal static PatentDataProvenance CopyProvenance(PatentDataProvenance source, string[]? secrets)
    {
        PatentDataProvenance result = OdpJsonParser.CopyProvenance(source);
        result.SourceUri = SafeUri(source.SourceUri, secrets);
        result.DatasetIdentifier = SafeText(source.DatasetIdentifier, secrets);
        result.DatasetVersion = SafeText(source.DatasetVersion, secrets);
        result.RequestIdentifier = SafeText(source.RequestIdentifier, secrets);
        return result;
    }

    internal static string[] CopyPaths(string[] source, string[]? secrets)
    {
        string[] result = new string[source.Length];
        int index;
        for (index = 0; index < source.Length; index += 1)
        {
            result[index] = SafeText(source[index], secrets) ?? string.Empty;
        }

        return result;
    }

    internal static string? SafeText(string? value, string[]? secrets)
    {
        return SecretRedactor.RedactText(value, secrets);
    }

    internal static global::System.Uri? SafeUri(global::System.Uri? value, string[]? secrets)
    {
        if (value == null)
        {
            return null;
        }

        return SecretRedactor.RedactUri(value, secrets);
    }

    internal static void Validate(DatasetManifest manifest)
    {
        if (manifest == null || manifest.Products == null || manifest.Provenance == null)
        {
            throw new ValidationException("The dataset manifest is incomplete.", "manifest", "MANIFEST_INVALID", null);
        }

        if (manifest.FormatVersion != FormatVersion)
        {
            throw new ValidationException("The dataset manifest version is unsupported.", "FormatVersion", "MANIFEST_VERSION_UNSUPPORTED", null);
        }

        int productIndex;
        for (productIndex = 0; productIndex < manifest.Products.Length; productIndex += 1)
        {
            DatasetManifestProduct product = manifest.Products[productIndex];
            if (product == null || global::System.String.IsNullOrWhiteSpace(product.ProductIdentifier) || product.Files == null || product.Provenance == null)
            {
                throw new ValidationException("A dataset manifest product is invalid.", "Products", "MANIFEST_PRODUCT_INVALID", null);
            }

            int fileIndex;
            for (fileIndex = 0; fileIndex < product.Files.Length; fileIndex += 1)
            {
                ValidateFile(product.Files[fileIndex]);
            }
        }
    }

    internal static void ValidateFile(DatasetManifestFile file)
    {
        if (file == null || global::System.String.IsNullOrWhiteSpace(file.ProductIdentifier) || global::System.String.IsNullOrWhiteSpace(file.FileName) || file.ExtractedPaths == null || file.Stages == null || file.Provenance == null)
        {
            throw new ValidationException("A dataset manifest file is invalid.", "Files", "MANIFEST_FILE_INVALID", null);
        }

        ValidatePath(file.LocalPath, "LocalPath");
        ValidatePath(file.PartialPath, "PartialPath");
        int index;
        for (index = 0; index < file.ExtractedPaths.Length; index += 1)
        {
            ValidatePath(file.ExtractedPaths[index], "ExtractedPaths");
        }

        for (index = 0; index < file.Stages.Length; index += 1)
        {
            if (file.Stages[index] == null || global::System.String.IsNullOrWhiteSpace(file.Stages[index].Name) || file.Stages[index].State < MirrorStageState.NotStarted || file.Stages[index].State > MirrorStageState.Failed)
            {
                throw new ValidationException("A dataset manifest stage is invalid.", "Stages", "MANIFEST_STAGE_INVALID", null);
            }
        }

        if (file.HasSize && file.Size < 0L)
        {
            throw new ValidationException("A manifest file size cannot be negative.", "Size", "MANIFEST_FILE_INVALID", null);
        }

        if (file.Digest != null)
        {
            ChecksumExpectation expectation = new ChecksumExpectation();
            expectation.Algorithm = file.DigestAlgorithm;
            expectation.Value = file.Digest;
            if (!BulkChecksumVerifier.NormalizeExpected(expectation).IsValid)
            {
                throw new ValidationException("A manifest file digest is invalid.", "Digest", "MANIFEST_DIGEST_INVALID", null);
            }
        }
    }

    internal static void ValidatePath(string? path, string fieldName)
    {
        if (path == null)
        {
            return;
        }

        if (global::System.String.IsNullOrWhiteSpace(path) || global::System.IO.Path.IsPathRooted(path))
        {
            throw new ValidationException("Manifest artifact paths must be relative.", fieldName, "MANIFEST_PATH_INVALID", null);
        }

        string normalized = path.Replace('\\', '/');
        string[] parts = normalized.Split('/');
        int index;
        for (index = 0; index < parts.Length; index += 1)
        {
            if (parts[index].Length == 0 || parts[index] == "." || parts[index] == "..")
            {
                throw new ValidationException("Manifest artifact paths cannot escape the mirror.", fieldName, "MANIFEST_PATH_INVALID", null);
            }
        }
    }
}
