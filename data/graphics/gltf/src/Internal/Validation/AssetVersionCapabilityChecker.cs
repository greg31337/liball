using System;
using System.Collections.Generic;

namespace gltf;

internal class AssetVersionCapabilityChecker
{
    internal DocumentRuleValidator DocumentRules = new DocumentRuleValidator();

    internal List<GltfValidationIssue> CheckReadCapabilities(GltfDocument document)
    {
        if (document == null)
        {
            throw new ArgumentNullException("document");
        }

        List<GltfValidationIssue> result = new List<GltfValidationIssue>();
        if (document.Asset == null)
        {
            return result;
        }

        string? minimumVersion = document.Asset.MinimumVersion;
        if (minimumVersion != null)
        {
            int minimumMajor = DocumentRules.GetAssetVersionMajor(minimumVersion);
            int minimumMinor = DocumentRules.GetAssetVersionMinor(minimumVersion);
            if (minimumMajor >= 0 && minimumMajor != 2)
            {
                result.Add(CreateIssue(
                    GltfIssueCodes.UnknownAssetMajorVersion,
                    $"Asset minimum version '{minimumVersion}' requires unsupported glTF major version {minimumMajor}.",
                    "/asset/minVersion",
                    GltfValidationSeverity.Error));
            }
            else if (minimumMajor == 2 && minimumMinor > 0)
            {
                result.Add(CreateIssue(
                    GltfIssueCodes.UnknownAssetMinorVersion,
                    $"Asset minimum version '{minimumVersion}' requires unsupported glTF minor version {minimumMinor}.",
                    "/asset/minVersion",
                    GltfValidationSeverity.Warning));
            }

            return result;
        }

        string? version = document.Asset.Version;
        int versionMajor = DocumentRules.GetAssetVersionMajor(version);
        if (versionMajor >= 0 && versionMajor != 2)
        {
            result.Add(CreateIssue(
                GltfIssueCodes.UnknownAssetMajorVersion,
                $"Asset version '{version}' requires unsupported glTF major version {versionMajor}.",
                "/asset/version",
                GltfValidationSeverity.Error));
        }

        return result;
    }

    internal GltfValidationIssue CreateIssue(
        string code,
        string message,
        string jsonPointer,
        GltfValidationSeverity severity)
    {
        GltfValidationIssue result = new GltfValidationIssue();
        result.Code = code;
        result.Severity = severity;
        result.Message = message;
        result.JsonPointer = jsonPointer;
        return result;
    }
}
