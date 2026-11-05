using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Security.Cryptography;
using System.Text.Json;

namespace CountryCodes.DataGenerator;

internal static class AcquisitionVerifier
{
    private const string Sha256Name = "SHA-256";

    internal static VerificationSummary Verify(string repositoryRoot, string manifestPath)
    {
        string fullRepositoryRoot = Path.GetFullPath(repositoryRoot);
        string fullManifestPath = Path.GetFullPath(manifestPath);
        List<string> issues = new List<string>();

        if (!Directory.Exists(fullRepositoryRoot))
        {
            issues.Add($"Repository root does not exist: {fullRepositoryRoot}");
            throw new VerificationException(issues);
        }

        if (!IsWithinRepository(fullRepositoryRoot, fullManifestPath))
        {
            issues.Add($"Manifest path is outside the repository root: {fullManifestPath}");
            throw new VerificationException(issues);
        }

        if (!File.Exists(fullManifestPath))
        {
            issues.Add($"Manifest file does not exist: {fullManifestPath}");
            throw new VerificationException(issues);
        }

        List<PinnedInput> pinnedInputs;
        int sourceCount;
        using (FileStream manifestStream = File.OpenRead(fullManifestPath))
        using (JsonDocument manifest = JsonDocument.Parse(manifestStream))
        {
            pinnedInputs = ValidateManifest(
                manifest.RootElement,
                fullRepositoryRoot,
                issues,
                out sourceCount);
        }

        if (issues.Count != 0)
        {
            throw new VerificationException(issues);
        }

        long totalBytes = VerifyPinnedFiles(fullRepositoryRoot, pinnedInputs, issues);
        if (issues.Count != 0)
        {
            throw new VerificationException(issues);
        }

        return new VerificationSummary(sourceCount, pinnedInputs.Count, totalBytes);
    }

    private static List<PinnedInput> ValidateManifest(
        JsonElement root,
        string repositoryRoot,
        List<string> issues,
        out int sourceCount)
    {
        List<PinnedInput> pinnedInputs = new List<PinnedInput>();
        sourceCount = 0;

        if (root.ValueKind != JsonValueKind.Object)
        {
            issues.Add("Manifest root must be a JSON object.");
            return pinnedInputs;
        }

        ValidatePolicy(root, issues);
        HashSet<string> licenseIds = ReadLicenseCatalog(root, issues);

        if (!root.TryGetProperty("sources", out JsonElement sources) ||
            sources.ValueKind != JsonValueKind.Array)
        {
            issues.Add("Manifest property 'sources' must be an array.");
            return pinnedInputs;
        }

        HashSet<string> sourceIds = new HashSet<string>(StringComparer.Ordinal);
        HashSet<string> localPaths = new HashSet<string>(StringComparer.Ordinal);
        int sourceIndex = 0;
        foreach (JsonElement source in sources.EnumerateArray())
        {
            string sourceContext = $"sources[{sourceIndex}]";
            sourceIndex += 1;
            sourceCount += 1;

            if (source.ValueKind != JsonValueKind.Object)
            {
                issues.Add($"{sourceContext} must be an object.");
                continue;
            }

            string? sourceId = ReadRequiredString(source, "id", sourceContext, issues);
            string? revision = ReadRequiredString(source, "revision", sourceContext, issues);
            string? retrievedOn = ReadRequiredString(source, "retrievedOn", sourceContext, issues);
            string? sourceBaseUrl = ReadRequiredString(source, "sourceBaseUrl", sourceContext, issues);
            string? sourceLicenseId = ReadRequiredString(source, "licenseId", sourceContext, issues);

            if (sourceId != null && !sourceIds.Add(sourceId))
            {
                issues.Add($"Duplicate source id '{sourceId}'.");
            }

            if (revision != null && revision.Length == 0)
            {
                issues.Add($"{sourceContext}.revision must not be empty.");
            }

            if (retrievedOn != null &&
                !DateOnly.TryParseExact(
                    retrievedOn,
                    "yyyy-MM-dd",
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.None,
                    out DateOnly _))
            {
                issues.Add($"{sourceContext}.retrievedOn must use yyyy-MM-dd format.");
            }

            Uri? baseUri = null;
            if (sourceBaseUrl != null &&
                (!Uri.TryCreate(sourceBaseUrl, UriKind.Absolute, out baseUri) ||
                 !string.Equals(baseUri.Scheme, Uri.UriSchemeHttps, StringComparison.Ordinal)))
            {
                issues.Add($"{sourceContext}.sourceBaseUrl must be an absolute HTTPS URL.");
            }

            ValidateLicenseId(sourceLicenseId, licenseIds, $"{sourceContext}.licenseId", issues);

            if (!source.TryGetProperty("files", out JsonElement files) ||
                files.ValueKind != JsonValueKind.Array)
            {
                issues.Add($"{sourceContext}.files must be an array.");
                continue;
            }

            int fileIndex = 0;
            foreach (JsonElement file in files.EnumerateArray())
            {
                string fileContext = $"{sourceContext}.files[{fileIndex}]";
                fileIndex += 1;
                ValidateFileDeclaration(
                    file,
                    fileContext,
                    sourceId,
                    sourceLicenseId,
                    baseUri,
                    repositoryRoot,
                    licenseIds,
                    localPaths,
                    pinnedInputs,
                    issues);
            }

            if (fileIndex == 0)
            {
                issues.Add($"{sourceContext}.files must contain at least one file.");
            }
        }

        if (sourceCount == 0)
        {
            issues.Add("Manifest must declare at least one source.");
        }

        ValidateExcludedSourceLicenses(root, licenseIds, issues);
        return pinnedInputs;
    }

    private static void ValidatePolicy(JsonElement root, List<string> issues)
    {
        if (!root.TryGetProperty("policy", out JsonElement policy) ||
            policy.ValueKind != JsonValueKind.Object)
        {
            issues.Add("Manifest property 'policy' must be an object.");
        }
        else
        {
            string? hashAlgorithm = ReadRequiredString(policy, "hashAlgorithm", "policy", issues);
            if (hashAlgorithm != null &&
                !string.Equals(hashAlgorithm, Sha256Name, StringComparison.Ordinal))
            {
                issues.Add($"policy.hashAlgorithm must be '{Sha256Name}'.");
            }

            string? pathBase = ReadRequiredString(policy, "pathBase", "policy", issues);
            if (pathBase != null &&
                !string.Equals(pathBase, "repository-root", StringComparison.Ordinal))
            {
                issues.Add("policy.pathBase must be 'repository-root'.");
            }
        }

        if (!root.TryGetProperty("failurePolicy", out JsonElement failurePolicy) ||
            failurePolicy.ValueKind != JsonValueKind.Object)
        {
            issues.Add("Manifest property 'failurePolicy' must be an object.");
            return;
        }

        ValidateRequiredStringValue(failurePolicy, "mode", "fail-closed", issues);
        ValidateRequiredBooleanValue(failurePolicy, "normalGenerationIsOffline", true, issues);
        ValidateRequiredBooleanValue(failurePolicy, "networkFallbackAllowed", false, issues);
        ValidateRequiredBooleanValue(failurePolicy, "automaticRepinAllowed", false, issues);
    }

    private static HashSet<string> ReadLicenseCatalog(JsonElement root, List<string> issues)
    {
        HashSet<string> licenseIds = new HashSet<string>(StringComparer.Ordinal);
        if (!root.TryGetProperty("licenseCatalog", out JsonElement catalog) ||
            catalog.ValueKind != JsonValueKind.Array)
        {
            issues.Add("Manifest property 'licenseCatalog' must be an array.");
            return licenseIds;
        }

        int index = 0;
        foreach (JsonElement license in catalog.EnumerateArray())
        {
            string context = $"licenseCatalog[{index}]";
            index += 1;
            if (license.ValueKind != JsonValueKind.Object)
            {
                issues.Add($"{context} must be an object.");
                continue;
            }

            string? licenseId = ReadRequiredString(license, "id", context, issues);
            ReadRequiredString(license, "terms", context, issues);
            ReadRequiredString(license, "packageUse", context, issues);
            if (licenseId != null && !licenseIds.Add(licenseId))
            {
                issues.Add($"Duplicate license id '{licenseId}'.");
            }
        }

        if (index == 0)
        {
            issues.Add("Manifest licenseCatalog must contain at least one license.");
        }

        return licenseIds;
    }

    private static void ValidateFileDeclaration(
        JsonElement file,
        string context,
        string? sourceId,
        string? sourceLicenseId,
        Uri? baseUri,
        string repositoryRoot,
        HashSet<string> licenseIds,
        HashSet<string> localPaths,
        List<PinnedInput> pinnedInputs,
        List<string> issues)
    {
        if (file.ValueKind != JsonValueKind.Object)
        {
            issues.Add($"{context} must be an object.");
            return;
        }

        string? localPath = ReadRequiredString(file, "path", context, issues);
        string? sourcePath = ReadRequiredString(file, "sourcePath", context, issues, true);
        string? expectedHash = ReadRequiredString(file, "sha256", context, issues);
        string? fileLicenseId = ReadOptionalString(file, "licenseId", context, issues);
        string? effectiveLicenseId = fileLicenseId ?? sourceLicenseId;

        ValidateLicenseId(effectiveLicenseId, licenseIds, $"{context}.licenseId", issues);

        if (expectedHash != null && !IsLowerHexSha256(expectedHash))
        {
            issues.Add($"{context}.sha256 must contain exactly 64 lowercase hexadecimal characters.");
        }

        if (baseUri != null && sourcePath != null &&
            (!Uri.TryCreate(baseUri, sourcePath, out Uri? sourceUri) ||
             !string.Equals(sourceUri.Scheme, Uri.UriSchemeHttps, StringComparison.Ordinal)))
        {
            issues.Add($"{context}.sourcePath must resolve to an HTTPS acquisition URL.");
        }

        string? fullPath = null;
        if (localPath != null)
        {
            if (Path.IsPathRooted(localPath))
            {
                issues.Add($"{context}.path must be relative to the repository root.");
            }
            else
            {
                fullPath = Path.GetFullPath(Path.Combine(repositoryRoot, localPath));
                if (!IsWithinRepository(repositoryRoot, fullPath))
                {
                    issues.Add($"{context}.path escapes the repository root: {localPath}");
                }
            }

            if (!localPaths.Add(localPath))
            {
                issues.Add($"Duplicate local input path '{localPath}'.");
            }
        }

        if (sourceId != null &&
            localPath != null &&
            fullPath != null &&
            expectedHash != null &&
            IsLowerHexSha256(expectedHash))
        {
            pinnedInputs.Add(new PinnedInput(sourceId, localPath, fullPath, expectedHash));
        }
    }

    private static void ValidateExcludedSourceLicenses(
        JsonElement root,
        HashSet<string> licenseIds,
        List<string> issues)
    {
        if (!root.TryGetProperty("excludedSources", out JsonElement excludedSources) ||
            excludedSources.ValueKind != JsonValueKind.Array)
        {
            issues.Add("Manifest property 'excludedSources' must be an array.");
            return;
        }

        int index = 0;
        foreach (JsonElement source in excludedSources.EnumerateArray())
        {
            string context = $"excludedSources[{index}]";
            index += 1;
            if (source.ValueKind != JsonValueKind.Object)
            {
                issues.Add($"{context} must be an object.");
                continue;
            }

            string? licenseId = ReadRequiredString(source, "licenseId", context, issues);
            ValidateLicenseId(licenseId, licenseIds, $"{context}.licenseId", issues);
        }
    }

    private static long VerifyPinnedFiles(
        string repositoryRoot,
        List<PinnedInput> pinnedInputs,
        List<string> issues)
    {
        long totalBytes = 0;
        foreach (PinnedInput input in pinnedInputs)
        {
            if (!File.Exists(input.FullPath))
            {
                issues.Add($"Missing pinned input '{input.LocalPath}' from source '{input.SourceId}'.");
                continue;
            }

            FileInfo fileInfo = new FileInfo(input.FullPath);
            FileSystemInfo? linkTarget = fileInfo.ResolveLinkTarget(true);
            if (linkTarget != null && !IsWithinRepository(repositoryRoot, linkTarget.FullName))
            {
                issues.Add($"Pinned input '{input.LocalPath}' resolves outside the repository root.");
                continue;
            }

            try
            {
                byte[] actualHash;
                using (FileStream stream = File.OpenRead(input.FullPath))
                {
                    actualHash = SHA256.HashData(stream);
                }

                string actualHashText = Convert.ToHexString(actualHash).ToLowerInvariant();
                if (!string.Equals(actualHashText, input.ExpectedHash, StringComparison.Ordinal))
                {
                    issues.Add(
                        $"SHA-256 mismatch for '{input.LocalPath}': expected " +
                        $"{input.ExpectedHash}, found {actualHashText}.");
                    continue;
                }

                totalBytes += fileInfo.Length;
            }
            catch (IOException exception)
            {
                issues.Add($"Could not read pinned input '{input.LocalPath}': {exception.Message}");
            }
            catch (UnauthorizedAccessException exception)
            {
                issues.Add($"Could not read pinned input '{input.LocalPath}': {exception.Message}");
            }
        }

        return totalBytes;
    }

    private static string? ReadRequiredString(
        JsonElement parent,
        string propertyName,
        string context,
        List<string> issues,
        bool allowEmpty = false)
    {
        if (!parent.TryGetProperty(propertyName, out JsonElement property) ||
            property.ValueKind != JsonValueKind.String)
        {
            issues.Add($"{context}.{propertyName} must be a string.");
            return null;
        }

        string? value = property.GetString();
        if (value == null || (!allowEmpty && value.Length == 0))
        {
            issues.Add($"{context}.{propertyName} must not be empty.");
            return null;
        }

        return value;
    }

    private static string? ReadOptionalString(
        JsonElement parent,
        string propertyName,
        string context,
        List<string> issues)
    {
        if (!parent.TryGetProperty(propertyName, out JsonElement property))
        {
            return null;
        }

        if (property.ValueKind != JsonValueKind.String)
        {
            issues.Add($"{context}.{propertyName} must be a string when present.");
            return null;
        }

        string? value = property.GetString();
        if (value == null || value.Length == 0)
        {
            issues.Add($"{context}.{propertyName} must not be empty when present.");
            return null;
        }

        return value;
    }

    private static void ValidateRequiredStringValue(
        JsonElement parent,
        string propertyName,
        string expectedValue,
        List<string> issues)
    {
        string? value = ReadRequiredString(parent, propertyName, "failurePolicy", issues);
        if (value != null && !string.Equals(value, expectedValue, StringComparison.Ordinal))
        {
            issues.Add($"failurePolicy.{propertyName} must be '{expectedValue}'.");
        }
    }

    private static void ValidateRequiredBooleanValue(
        JsonElement parent,
        string propertyName,
        bool expectedValue,
        List<string> issues)
    {
        if (!parent.TryGetProperty(propertyName, out JsonElement property) ||
            (property.ValueKind != JsonValueKind.True && property.ValueKind != JsonValueKind.False))
        {
            issues.Add($"failurePolicy.{propertyName} must be a boolean.");
            return;
        }

        if (property.GetBoolean() != expectedValue)
        {
            issues.Add($"failurePolicy.{propertyName} must be {expectedValue.ToString().ToLowerInvariant()}.");
        }
    }

    private static void ValidateLicenseId(
        string? licenseId,
        HashSet<string> licenseIds,
        string context,
        List<string> issues)
    {
        if (licenseId != null && !licenseIds.Contains(licenseId))
        {
            issues.Add($"{context} references unknown license id '{licenseId}'.");
        }
    }

    private static bool IsLowerHexSha256(string value)
    {
        if (value.Length != 64)
        {
            return false;
        }

        foreach (char character in value)
        {
            bool isDigit = character >= '0' && character <= '9';
            bool isLowerHexLetter = character >= 'a' && character <= 'f';
            if (!isDigit && !isLowerHexLetter)
            {
                return false;
            }
        }

        return true;
    }

    private static bool IsWithinRepository(string repositoryRoot, string candidatePath)
    {
        string normalizedRoot = Path.GetFullPath(repositoryRoot)
            .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        string normalizedCandidate = Path.GetFullPath(candidatePath);
        if (string.Equals(normalizedRoot, normalizedCandidate, PathComparison))
        {
            return true;
        }

        string rootPrefix = normalizedRoot + Path.DirectorySeparatorChar;
        return normalizedCandidate.StartsWith(rootPrefix, PathComparison);
    }

    private static StringComparison PathComparison
    {
        get
        {
            return OperatingSystem.IsWindows()
                ? StringComparison.OrdinalIgnoreCase
                : StringComparison.Ordinal;
        }
    }

    private sealed class PinnedInput
    {
        internal PinnedInput(
            string sourceId,
            string localPath,
            string fullPath,
            string expectedHash)
        {
            SourceId = sourceId;
            LocalPath = localPath;
            FullPath = fullPath;
            ExpectedHash = expectedHash;
        }

        internal string SourceId { get; }

        internal string LocalPath { get; }

        internal string FullPath { get; }

        internal string ExpectedHash { get; }
    }
}

internal sealed class VerificationSummary
{
    internal VerificationSummary(int sourceCount, int fileCount, long totalBytes)
    {
        SourceCount = sourceCount;
        FileCount = fileCount;
        TotalBytes = totalBytes;
    }

    internal int SourceCount { get; }

    internal int FileCount { get; }

    internal long TotalBytes { get; }
}

internal sealed class VerificationException : Exception
{
    internal VerificationException(IReadOnlyList<string> issues)
        : base("Pinned acquisition input verification failed.")
    {
        Issues = issues;
    }

    internal IReadOnlyList<string> Issues { get; }
}
