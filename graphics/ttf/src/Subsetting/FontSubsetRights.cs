namespace ttf;

internal static class FontSubsetRightsResolver
{
    internal static FontSubsetEmbeddingDecision Resolve(
        FontInstance instance,
        FontSubsetOptions options)
    {
        if (instance == null || options == null)
        {
            throw new System.ArgumentNullException("values");
        }

        FontFace face = instance.GetFace();
        FontEmbeddingRights? rights = ReadRights(face);
        FontSubsetEmbeddingMode requestedMode = options.GetEmbeddingMode();
        FontSubsetEmbeddingMode effectiveMode = requestedMode;
        FontSubsetRightsPolicy policy = options.GetRightsPolicy();

        if (rights == null)
        {
            return new FontSubsetEmbeddingDecision(
                null,
                requestedMode,
                effectiveMode,
                policy,
                false,
                false,
                false,
                new FontDiagnostic[0]);
        }

        FontDiagnostic[] diagnostics = CreateDiagnostics(face, rights);
        bool embeddingConflict = !rights.AllowsEmbedding();
        bool subsettingConflict = requestedMode ==
                FontSubsetEmbeddingMode.Subset &&
            rights.RequiresFullEmbedding();
        bool bitmapConflict = rights.RequiresBitmapOnlyEmbedding() &&
            face.GetOutlineKind() != FontOutlineKind.BitmapOnly;
        bool conflict = embeddingConflict || subsettingConflict ||
            bitmapConflict;
        bool reportedConflict = conflict &&
            policy == FontSubsetRightsPolicy.Report;
        bool forcedFullFont = false;

        if (conflict && policy == FontSubsetRightsPolicy.Reject)
        {
            throw FontEmbeddingException.Create(
                CreateConflictMessage(
                    embeddingConflict,
                    bitmapConflict,
                    subsettingConflict));
        }

        if (conflict && policy == FontSubsetRightsPolicy.EmbedFullFont)
        {
            if (embeddingConflict || bitmapConflict)
            {
                throw FontEmbeddingException.Create(
                    CreateConflictMessage(
                        embeddingConflict,
                        bitmapConflict,
                        subsettingConflict));
            }

            effectiveMode = FontSubsetEmbeddingMode.FullFont;
            forcedFullFont = true;
        }

        return new FontSubsetEmbeddingDecision(
            rights,
            requestedMode,
            effectiveMode,
            policy,
            conflict,
            reportedConflict,
            forcedFullFont,
            diagnostics);
    }

    static FontEmbeddingRights? ReadRights(FontFace face)
    {
        SfntTableData? table = face.GetTableStore().FindTable(FontTags.Os2);

        if (table == null || table.GetLength() < 10)
        {
            return null;
        }

        ReadOnlySpan<byte> data = table.GetDataSlice().GetMemory().Span;
        int version = data[0] << 8 | data[1];
        int flags = data[8] << 8 | data[9];
        return new FontEmbeddingRights(
            SfntEmbeddingRightsInterpreter.Interpret(version, flags));
    }

    static FontDiagnostic[] CreateDiagnostics(
        FontFace face,
        FontEmbeddingRights rights)
    {
        int count = rights.GetRestrictionCount();

        if (count > face.GetTableCache().GetLimits().GetMaxDiagnostics())
        {
            throw FontLimitException.Create(
                "The configured diagnostic count limit was exceeded.");
        }

        FontDiagnostic[] diagnostics = new FontDiagnostic[count];
        SfntTableData? table = face.GetTableStore().FindTable(FontTags.Os2);
        long offset = table == null ? -1 : table.GetOffset();
        int index = 0;

        while (index < diagnostics.Length)
        {
            FontDiagnosticCode code = rights.GetRestrictionCode(index);
            diagnostics[index] = new FontDiagnostic(
                FontDiagnosticSeverity.Warning,
                code,
                GetDiagnosticMessage(code),
                face.GetFaceIndex(),
                FontTags.Os2,
                offset,
                "subset.rights.fsType");
            index += 1;
        }

        return diagnostics;
    }

    static string GetDiagnosticMessage(FontDiagnosticCode code)
    {
        if (code == FontDiagnosticCode.EmbeddingRestricted)
        {
            return "OS/2 fsType requires explicit rights-holder permission before embedding.";
        }

        if (code == FontDiagnosticCode.SubsettingRestricted)
        {
            return "OS/2 fsType prohibits subsetting and requires complete-font embedding.";
        }

        return "OS/2 fsType permits embedding only bitmap glyph data.";
    }

    static string CreateConflictMessage(
        bool embeddingConflict,
        bool bitmapConflict,
        bool subsettingConflict)
    {
        if (embeddingConflict)
        {
            return "Font embedding requires explicit rights-holder permission under OS/2 fsType.";
        }

        if (bitmapConflict)
        {
            return "OS/2 fsType permits only bitmap embedding, but the selected face contains outline data.";
        }

        if (subsettingConflict)
        {
            return "OS/2 fsType prohibits subsetting and requires complete-font embedding.";
        }

        return "The requested embedding operation conflicts with OS/2 fsType.";
    }
}
