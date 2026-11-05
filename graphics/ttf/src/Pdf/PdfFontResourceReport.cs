namespace ttf;

internal static class PdfFontResourceReportBuilder
{
    internal static PdfFontResourceReport Build(
        PdfFontOptions options,
        PdfFontKind fontKind,
        int[] allocatedCodes,
        PdfToUnicodeMappingArtifact mappings,
        FontSubsetEmbeddingDecision decision)
    {
        if (options == null || allocatedCodes == null ||
            mappings == null || decision == null)
        {
            throw new System.ArgumentNullException("values");
        }

        if (allocatedCodes.Length == 0 || allocatedCodes[0] != 0)
        {
            throw new System.ArgumentException(
                "Allocated PDF codes must begin with reserved code zero.",
                "allocatedCodes");
        }

        int[] missingCodes = FindMissingCodes(
            allocatedCodes,
            mappings);
        int actualTextCount =
            mappings.GetActualTextRecommendationCount();
        System.Collections.Generic.List<PdfFontResourceConcern> concerns =
            new System.Collections.Generic.List<PdfFontResourceConcern>();
        PdfVersion minimumVersion = GetMinimumPdfVersion(fontKind);
        AddVersionConcern(
            concerns,
            minimumVersion,
            options.GetVersion());
        AddPdfAConcerns(
            concerns,
            options.GetArchivalProfile(),
            fontKind,
            decision);
        AddPdfUaConcerns(
            concerns,
            options.GetAccessibilityProfile(),
            missingCodes.Length,
            actualTextCount);
        return new PdfFontResourceReport(
            minimumVersion,
            options.GetVersion(),
            GetEmbeddingRightsOutcome(decision),
            decision,
            missingCodes,
            actualTextCount,
            options.GetArchivalProfile(),
            options.GetAccessibilityProfile(),
            concerns.ToArray());
    }

    static void AddVersionConcern(
        System.Collections.Generic.List<PdfFontResourceConcern> concerns,
        PdfVersion minimumVersion,
        PdfVersion targetVersion)
    {
        if (targetVersion < minimumVersion)
        {
            concerns.Add(new PdfFontResourceConcern(
                PdfFontResourceConcernKind.PdfVersionTooLow,
                1,
                "The target PDF version predates the selected font resource strategy."));
        }
    }

    static int[] FindMissingCodes(
        int[] allocatedCodes,
        PdfToUnicodeMappingArtifact mappings)
    {
        System.Collections.Generic.List<int> missing =
            new System.Collections.Generic.List<int>();
        int index = 1;

        while (index < allocatedCodes.Length)
        {
            int code = allocatedCodes[index];
            if (mappings.FindMapping(code) == null)
            {
                missing.Add(code);
            }

            index += 1;
        }

        return missing.ToArray();
    }

    static PdfVersion GetMinimumPdfVersion(PdfFontKind fontKind)
    {
        if (fontKind == PdfFontKind.SimpleTrueType)
        {
            return PdfVersion.Pdf1_1;
        }

        if (fontKind == PdfFontKind.Type0OpenType)
        {
            return PdfVersion.Pdf1_6;
        }

        return PdfVersion.Pdf1_2;
    }

    static PdfFontEmbeddingRightsOutcome GetEmbeddingRightsOutcome(
        FontSubsetEmbeddingDecision decision)
    {
        if (!decision.HasRightsInformation())
        {
            return PdfFontEmbeddingRightsOutcome.Unavailable;
        }

        if (decision.WasFullFontForced())
        {
            return PdfFontEmbeddingRightsOutcome.FullFontForced;
        }

        if (decision.WasConflictReported())
        {
            return PdfFontEmbeddingRightsOutcome.ConflictReported;
        }

        return PdfFontEmbeddingRightsOutcome.Permitted;
    }

    static void AddPdfAConcerns(
        System.Collections.Generic.List<PdfFontResourceConcern> concerns,
        PdfArchivalProfile profile,
        PdfFontKind fontKind,
        FontSubsetEmbeddingDecision decision)
    {
        if (profile == PdfArchivalProfile.None)
        {
            return;
        }

        if (decision.WasConflictReported())
        {
            concerns.Add(new PdfFontResourceConcern(
                PdfFontResourceConcernKind.PdfAEmbeddingRightsConflict,
                1,
                "The embedded font program retains a reported source-rights conflict."));
        }

        if (profile == PdfArchivalProfile.PdfA1 &&
            fontKind == PdfFontKind.Type0OpenType)
        {
            concerns.Add(new PdfFontResourceConcern(
                PdfFontResourceConcernKind.PdfA1OpenTypeFontProgram,
                1,
                "PDF/A-1 is based on PDF 1.4 and cannot represent an OpenType FontFile3 program."));
        }
    }

    static void AddPdfUaConcerns(
        System.Collections.Generic.List<PdfFontResourceConcern> concerns,
        PdfAccessibilityProfile profile,
        int missingMappingCount,
        int actualTextCount)
    {
        if (profile == PdfAccessibilityProfile.None)
        {
            return;
        }

        if (missingMappingCount != 0)
        {
            concerns.Add(new PdfFontResourceConcern(
                PdfFontResourceConcernKind.PdfUaMissingUnicodeMappings,
                missingMappingCount,
                "Allocated character codes lack original Unicode mappings."));
        }

        if (actualTextCount != 0)
        {
            concerns.Add(new PdfFontResourceConcern(
                PdfFontResourceConcernKind.PdfUaActualTextRequired,
                actualTextCount,
                "Ambiguous code occurrences require caller-supplied ActualText."));
        }
    }
}
