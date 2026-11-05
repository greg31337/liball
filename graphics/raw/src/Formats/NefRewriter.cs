namespace raw;

internal static class NefRewriter
{
    internal static byte[] Rewrite(RawDocument document, RawWriteOptions options)
    {
        Cr2RewriteState state = CreateState(document, options);
        byte[] output = TiffWriter.WriteToArray(state.Plan, options.Limits, options.CancellationToken);
        ValidateOutput(state, output);
        return output;
    }

    internal static Cr2RewriteState CreateState(RawDocument document, RawWriteOptions options)
    {
        Validate(document, options);
        Cr2RewriteState state = new Cr2RewriteState(document, options);
        state.Replacements = Cr2Rewriter.CreateReplacements(document, options);
        state.Blocks = Cr2Rewriter.CreateBlocks(state);
        TiffLayoutRequest request = Cr2Rewriter.CreateRequest(state, (document.TiffGraph ?? throw new System.InvalidOperationException("A required value is unavailable.")).Header.BigTiff, System.Array.Empty<byte>(), "IFD0");
        state.Plan = TiffLayoutPlanner.Plan(request, options.Limits, options.CancellationToken);
        Cr2Rewriter.ApplyMakerNotes(state);
        return state;
    }

    internal static void Validate(RawDocument document, RawWriteOptions options)
    {
        if (options == null || !options.Validate().IsValid() || options.Mode != RawWriteMode.PreservingRewrite)
        {
            throw new RawWriteException(RawDiagnosticCode.InvalidArgument, "Valid preserving-rewrite options are required for NEF rewriting.", -1);
        }
        options.CancellationToken.ThrowIfCancellationRequested();
        if (document == null || document.Format != RawFormat.Nef || document.TiffGraph == null || document.TiffGraph.Header == null || document.TiffGraph.FindIfd("IFD0") == null || !document.SourceRetained || document.Source.Length == 0)
        {
            throw new RawWriteException(RawDiagnosticCode.InvalidArgument, "A source-retaining TIFF NEF document with IFD0 is required for preserving rewrite.", -1);
        }
        if (!document.MetadataLoaded || document.Metadata == null || document.Metadata.MakerNotes == null || !document.PreviewsLoaded || document.Thumbnails == null || document.PayloadMap == null || !document.PayloadMap.Validate().IsValid())
        {
            throw new RawWriteException(RawDiagnosticCode.InvalidArgument, "NEF preserving rewrite requires fully loaded metadata, maker notes, previews, and a valid payload map.", -1);
        }
        if (options.UnknownDataPolicy != RawUnknownDataPolicy.Preserve)
        {
            throw new RawUnsupportedException(RawDiagnosticCode.UnsupportedProfile, "NEF preserving rewrite requires byte-preserving unknown-data policy.", -1);
        }
        Cr2Rewriter.ValidatePendingMetadata(document);
    }

    internal static void ValidateOutput(Cr2RewriteState state, byte[] output)
    {
        RawOpenOptions openOptions = new RawOpenOptions();
        openOptions.AllowExperimentalProfiles = true;
        openOptions.ReadMetadata = true;
        openOptions.ReadPreviews = true;
        openOptions.ReadRawPayload = true;
        RawDocument reopened = RawCodec.Open(output, null, openOptions);
        if (reopened.Format != RawFormat.Nef || reopened.TiffGraph == null || reopened.TiffGraph.Header == null || reopened.TiffGraph.Header.BigTiff != (state.Document.TiffGraph ?? throw new System.InvalidOperationException("A required value is unavailable.")).Header.BigTiff)
        {
            throw new RawWriteException(RawDiagnosticCode.OutputValidationFailed, "The rewritten NEF failed TIFF and format reopen validation.", -1);
        }
        if (state.Document.ProfileName.Length != 0 && reopened.ProfileName != state.Document.ProfileName)
        {
            throw new RawWriteException(RawDiagnosticCode.OutputValidationFailed, "The rewritten NEF did not reopen through its original read profile.", -1);
        }
        if (state.Options.VerifyRawPayloadIdentity)
        {
            VerifyRawBytes(state.Document, reopened);
        }
        Cr2Rewriter.VerifyPreviews(state, reopened);
        Cr2Rewriter.VerifyPreservedPayloads(state, reopened);
        VerifyNikonMakerNotes(state.Document, reopened);
    }

    internal static void VerifyRawBytes(RawDocument original, RawDocument reopened)
    {
        RawPayloadBlock[] sourceBlocks = original.PayloadMap.Find(RawPayloadKind.RawImage);
        RawPayloadBlock[] outputBlocks = reopened.PayloadMap.Find(RawPayloadKind.RawImage);
        if (sourceBlocks.Length == 0 || sourceBlocks.Length != outputBlocks.Length)
        {
            throw new RawWriteException(RawDiagnosticCode.OutputValidationFailed, "The rewritten NEF raw payload map changed.", -1);
        }
        int index = 0;
        while (index < sourceBlocks.Length)
        {
            if (!Cr2Rewriter.SameBytes(original.ExtractPayload(sourceBlocks[index]), reopened.ExtractPayload(outputBlocks[index])))
            {
                throw new RawWriteException(RawDiagnosticCode.OutputValidationFailed, "A rewritten NEF raw payload changed bytes.", outputBlocks[index].Offset);
            }
            index++;
        }
    }

    internal static void VerifyNikonMakerNotes(RawDocument original, RawDocument reopened)
    {
        RawMakerNote[] sourceNotes = original.Metadata.MakerNotes;
        RawMakerNote[] outputNotes = reopened.Metadata.MakerNotes;
        if (sourceNotes.Length != outputNotes.Length)
        {
            throw new RawWriteException(RawDiagnosticCode.OutputValidationFailed, "The rewritten NEF maker-note collection changed.", -1);
        }
        int noteIndex = 0;
        while (noteIndex < sourceNotes.Length)
        {
            RawMakerNote source = sourceNotes[noteIndex];
            RawMakerNote target = outputNotes[noteIndex];
            if (source == null || target == null || source.Signature != "NikonEmbeddedTiff" || target.Signature != source.Signature || target.OffsetBase != source.OffsetBase || source.NikonTags == null || target.NikonTags == null || source.NikonTags.Length != target.NikonTags.Length)
            {
                throw new RawWriteException(RawDiagnosticCode.OutputValidationFailed, "A rewritten NEF Nikon maker note changed structure.", -1);
            }
            int tagIndex = 0;
            while (tagIndex < source.NikonTags.Length)
            {
                RawNikonMakerNoteTag sourceTag = source.NikonTags[tagIndex];
                RawNikonMakerNoteTag targetTag = target.NikonTags[tagIndex];
                if (sourceTag == null || targetTag == null || sourceTag.Id != targetTag.Id || !Cr2Rewriter.SameBytes(sourceTag.CopyBytes(), targetTag.CopyBytes()))
                {
                    throw new RawWriteException(RawDiagnosticCode.OutputValidationFailed, "A rewritten NEF Nikon maker-note value was not preserved.", source.Offset);
                }
                tagIndex++;
            }
            noteIndex++;
        }
    }
}
