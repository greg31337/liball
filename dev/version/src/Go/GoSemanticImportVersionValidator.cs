namespace version;

internal static class GoSemanticImportVersionValidator
{
    internal static GoSemanticImportVersionResult Validate(string modulePathText, string versionText)
    {
        GoModulePathParseResult modulePathResult = GoModulePathParser.Parse(modulePathText);
        if (modulePathResult.IsFailure())
        {
            VersionDiagnostic diagnostic = modulePathResult.GetDiagnostic(0);
            return Failure(diagnostic.GetSourceText(), diagnostic.GetCode(), diagnostic.GetMessage(), diagnostic.GetIndex());
        }

        if (versionText == null)
        {
            return Failure(
                "",
                SemVerDiagnosticCodes.MissingMajor,
                "Go module version cannot be null.",
                0);
        }

        VersionParseResult versionResult = GoVersionParser.Parse(versionText);
        if (versionResult.IsFailure())
        {
            VersionDiagnostic diagnostic = versionResult.GetDiagnostic(0);
            return Failure(diagnostic.GetSourceText(), diagnostic.GetCode(), diagnostic.GetMessage(), diagnostic.GetIndex());
        }

        GoModulePath modulePath = modulePathResult.GetModulePath();
        UniversalVersion version = versionResult.GetVersion();
        string major = version.GetNumericComponent(0);
        string expectedSuffix = "v" + major;
        bool incompatibleVersion = IsIncompatibleVersion(version);

        if (modulePath.IsGopkgInPath())
        {
            if (incompatibleVersion)
            {
                return InvalidIncompatibleVersionFailure(modulePath.GetValue());
            }

            if (modulePath.GetMajorVersionSuffix() != expectedSuffix)
            {
                return Failure(
                    modulePath.GetValue(),
                    GoDiagnosticCodes.MajorVersionMismatch,
                    "Go gopkg.in module path suffix must match version major " + expectedSuffix + ".",
                    0);
            }

            return GoSemanticImportVersionResult.CreateSuccess(modulePath, version);
        }

        if (IsZeroOrOne(major))
        {
            if (incompatibleVersion)
            {
                return InvalidIncompatibleVersionFailure(modulePath.GetValue());
            }

            if (modulePath.HasMajorVersionSuffix())
            {
                return Failure(
                    modulePath.GetValue(),
                    GoDiagnosticCodes.UnexpectedMajorVersionSuffix,
                    "Go module versions v0 and v1 must not use a path major version suffix.",
                    0);
            }

            return GoSemanticImportVersionResult.CreateSuccess(modulePath, version);
        }

        if (!modulePath.HasMajorVersionSuffix())
        {
            if (incompatibleVersion)
            {
                return GoSemanticImportVersionResult.CreateSuccess(modulePath, version);
            }

            return Failure(
                modulePath.GetValue(),
                GoDiagnosticCodes.MissingMajorVersionSuffix,
                "Go module version " + expectedSuffix + " requires module path suffix /" + expectedSuffix + ".",
                0);
        }

        if (modulePath.GetMajorVersionSuffix() != expectedSuffix)
        {
            return Failure(
                modulePath.GetValue(),
                GoDiagnosticCodes.MajorVersionMismatch,
                "Go module path suffix must match version major " + expectedSuffix + ".",
                0);
        }

        if (incompatibleVersion)
        {
            return InvalidIncompatibleVersionFailure(modulePath.GetValue());
        }

        return GoSemanticImportVersionResult.CreateSuccess(modulePath, version);
    }

    internal static bool IsIncompatibleVersion(string versionText)
    {
        if (versionText == null)
        {
            return false;
        }

        VersionParseResult result = GoVersionParser.Parse(versionText);
        if (result.IsFailure())
        {
            return false;
        }

        return IsIncompatibleVersion(result.GetVersion());
    }

    internal static bool IsIncompatibleVersion(UniversalVersion version)
    {
        System.ArgumentNullException.ThrowIfNull(version);

        if (!version.HasBuildMetadata() || version.GetBuildIdentifierCount() != 1)
        {
            return false;
        }

        return version.GetBuildIdentifier(0).GetText() == "incompatible";
    }

    internal static string CanonicalizeModuleVersion(string modulePathText, string versionText)
    {
        GoSemanticImportVersionResult result = Validate(modulePathText, versionText);
        if (result.IsFailure())
        {
            return "";
        }

        if (IsIncompatibleVersion(result.GetVersion()))
        {
            return GoVersionComparer.Canonicalize(result.GetVersion()) + "+incompatible";
        }

        return GoVersionComparer.Canonicalize(result.GetVersion());
    }

    internal static int CompareModuleVersions(string leftText, string rightText)
    {
        return GoVersionComparer.Compare(leftText, rightText);
    }

    internal static string[] FilterSemanticImportVersions(string modulePathText, string[] versionTexts)
    {
        System.ArgumentNullException.ThrowIfNull(versionTexts);

        System.Collections.Generic.List<string> filtered = new System.Collections.Generic.List<string>();
        int index = 0;

        while (index < versionTexts.Length)
        {
            if (versionTexts[index] == null)
            {
                throw new System.ArgumentException("Go module version candidate arrays cannot contain null values.");
            }

            if (Validate(modulePathText, versionTexts[index]).IsSuccess())
            {
                filtered.Add(versionTexts[index]);
            }

            index = index + 1;
        }

        return filtered.ToArray();
    }

    internal static string? SelectHighestSemanticImportVersion(string modulePathText, string[] versionTexts)
    {
        string[] filtered = FilterSemanticImportVersions(modulePathText, versionTexts);
        string? selected = null;
        int index = 0;

        while (index < filtered.Length)
        {
            if (selected == null || GoVersionComparer.Compare(filtered[index], selected) > 0)
            {
                selected = filtered[index];
            }

            index = index + 1;
        }

        return selected;
    }

    internal static bool IsZeroOrOne(string major)
    {
        return major == "0" || major == "1";
    }

    internal static GoSemanticImportVersionResult InvalidIncompatibleVersionFailure(string modulePathText)
    {
        return Failure(
            modulePathText,
            GoDiagnosticCodes.InvalidIncompatibleVersion,
            "Go +incompatible versions require a v2 or higher version on a module path without a major version suffix.",
            0);
    }

    internal static GoSemanticImportVersionResult Failure(string sourceText, string code, string message, int diagnosticIndex)
    {
        return GoSemanticImportVersionResult.CreateFailure(
            VersionDiagnostic.FromIndex(code, message, sourceText, diagnosticIndex));
    }
}
