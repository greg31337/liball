namespace avif;

internal class AvifCodedProfileFacts
{
    internal int Av1Profile;
    internal int LevelIndex;
    internal int Width;
    internal int Height;
    internal bool IsSequence;

    internal AvifCodedProfileFacts(
        int av1Profile,
        int levelIndex,
        int width,
        int height,
        bool isSequence)
    {
        Av1Profile = av1Profile;
        LevelIndex = levelIndex;
        Width = width;
        Height = height;
        IsSequence = isSequence;
    }
}

internal class BrandValidationResult
{
    internal AvifResultCode Code;
    internal DiagnosticLog Diagnostics;

    internal BrandValidationResult(AvifResultCode code, DiagnosticLog diagnostics)
    {
        Code = code;
        Diagnostics = diagnostics;
    }
}

internal static class BrandValidator
{
    internal static BrandValidationResult Validate(
        FileTypeBox? fileType,
        AvifCodedProfileFacts? facts)
    {
        DiagnosticLog diagnostics = new DiagnosticLog();
        diagnostics.Context.PushBox("ftyp");
        if (fileType == null || fileType.Code != AvifResultCode.Success)
        {
            AddError(
                diagnostics,
                AvifResultCode.InvalidArgument,
                "A valid FileTypeBox is required for brand validation.");
            diagnostics.Context.PopBox();
            return new BrandValidationResult(diagnostics.Code, diagnostics);
        }

        BrandCapabilityReport capabilities = BrandResolver.Resolve(fileType);
        bool hasGeneralAvifBrand = capabilities.HasImageItems || capabilities.HasImageSequence;
        bool hasAvifSpecificBrand = hasGeneralAvifBrand
            || capabilities.IsIntraOnly
            || capabilities.HasBaselineProfile
            || capabilities.HasAdvancedProfile;

        if (hasAvifSpecificBrand && !hasGeneralAvifBrand)
        {
            AddError(
                diagnostics,
                AvifResultCode.NonConformingData,
                "An AVIF profile or intra-only brand requires the avif or avis brand.");
        }

        if (hasAvifSpecificBrand && !capabilities.HasMiaf)
        {
            AddError(
                diagnostics,
                AvifResultCode.NonConformingData,
                "An AVIF file must declare the miaf brand.");
        }

        if (capabilities.HasImageItems && !capabilities.HasHeifImageBrand)
        {
            AddWarning(
                diagnostics,
                "An AVIF image file is expected to declare the mif1 brand.");
        }

        if (capabilities.HasImageSequence && !capabilities.HasHeifSequenceBrand)
        {
            AddWarning(
                diagnostics,
                "An AVIF image sequence is expected to declare the msf1 brand.");
        }

        if (facts != null)
        {
            ValidateCodedProfile(capabilities, facts, diagnostics);
        }

        diagnostics.Context.PopBox();
        return new BrandValidationResult(diagnostics.Code, diagnostics);
    }

    internal static void ValidateCodedProfile(
        BrandCapabilityReport capabilities,
        AvifCodedProfileFacts facts,
        DiagnosticLog diagnostics)
    {
        if (facts.Av1Profile < 0
            || facts.Av1Profile > 2
            || facts.LevelIndex < 0
            || facts.LevelIndex > 31
            || facts.Width <= 0
            || facts.Height <= 0)
        {
            AddError(
                diagnostics,
                AvifResultCode.InvalidArgument,
                "The coded profile facts are outside their valid ranges.");
            return;
        }

        CheckedIntegerResult pixels = CheckedMath.ComputePixelCount(
            facts.Width,
            facts.Height,
            long.MaxValue);
        if (!pixels.Success)
        {
            AddError(
                diagnostics,
                pixels.Code,
                "The coded image pixel count cannot be represented.");
            return;
        }

        if (capabilities.HasBaselineProfile)
        {
            if (facts.Av1Profile != 0
                || facts.LevelIndex > 13
                || facts.Width > 8192
                || facts.Height > 4352
                || pixels.Value > 8912896)
            {
                AddError(
                    diagnostics,
                    AvifResultCode.NonConformingData,
                    "The coded image exceeds the MA1B profile constraints.");
            }
        }

        if (capabilities.HasAdvancedProfile)
        {
            if (facts.IsSequence)
            {
                if ((facts.Av1Profile != 0 && facts.Av1Profile != 1)
                    || facts.LevelIndex > 13
                    || facts.Width > 8192
                    || facts.Height > 4352
                    || pixels.Value > 8912896)
                {
                    AddError(
                        diagnostics,
                        AvifResultCode.NonConformingData,
                        "The coded sequence exceeds the MA1A sequence constraints.");
                }
            }
            else if (facts.Av1Profile != 1
                || facts.LevelIndex > 16
                || facts.Width > 16384
                || facts.Height > 8704
                || pixels.Value > 35651584)
            {
                AddError(
                    diagnostics,
                    AvifResultCode.NonConformingData,
                    "The coded image exceeds the MA1A item constraints.");
            }
        }
    }

    internal static void AddError(
        DiagnosticLog diagnostics,
        AvifResultCode code,
        string message)
    {
        diagnostics.Add(
            code,
            AvifDiagnosticSeverity.Error,
            AvifDiagnosticCategory.Validation,
            message);
    }

    internal static void AddWarning(DiagnosticLog diagnostics, string message)
    {
        diagnostics.Add(
            AvifResultCode.NonConformingData,
            AvifDiagnosticSeverity.Warning,
            AvifDiagnosticCategory.Validation,
            message);
    }
}
