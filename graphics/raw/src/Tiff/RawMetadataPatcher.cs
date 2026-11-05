namespace raw;

internal class RawMetadataPatchState
{
    internal RawDocument Document;
    internal RawWriteOptions Options;
    internal TiffPatchPlan PatchPlan;
    internal RawWritePlan WritePlan;

    internal RawMetadataPatchState(RawDocument document, RawWriteOptions options, TiffPatchPlan patchPlan, RawWritePlan writePlan)
    {
        Document = document;
        Options = options;
        PatchPlan = patchPlan;
        WritePlan = writePlan;
    }
}

internal static class RawMetadataPatcher
{
    internal static RawWritePlan Plan(RawDocument document, RawMetadataPatchEdit[] edits, RawWriteOptions options)
    {
        return CreateState(document, edits, options).WritePlan;
    }

    internal static byte[] Apply(RawDocument document, RawMetadataPatchEdit[] edits, RawWriteOptions options)
    {
        RawMetadataPatchState state = CreateState(document, edits, options);
        if (!state.WritePlan.CanWrite)
        {
            throw new RawWriteException(RawDiagnosticCode.DataLossConsentRequired, "The fixed-size metadata patch requires explicit consent because retained authenticity data will be invalidated.", -1);
        }
        byte[] result = state.Document.Source.ToArray();
        int index = 0;
        while (index < state.PatchPlan.Regions.Length)
        {
            state.Options.CancellationToken.ThrowIfCancellationRequested();
            TiffPatchRegion region = state.PatchPlan.Regions[index];
            int offset = CheckedMath.ToInt32(region.Offset);
            region.Bytes.CopyTo(result, offset);
            index++;
        }
        ValidateOutput(state, result);
        return result;
    }

    internal static RawMetadataPatchState CreateState(RawDocument document, RawMetadataPatchEdit[] edits, RawWriteOptions options)
    {
        ValidateRequest(document, edits, options);
        TiffPatchEdit[] internalEdits = CreateInternalEdits(document, edits);
        TiffPatchPlan patchPlan = TiffPatchAnalyzer.Plan(document.TiffGraph, internalEdits, options.Limits, options.CancellationToken);
        RawPayloadBlock[] authenticity = FindAuthenticity(document);
        bool authenticityInvalidated = patchPlan.Regions.Length != 0 && authenticity.Length != 0;
        bool consentMissing = authenticityInvalidated && !options.DataLossConsent;
        RawWriteChange[] changes = CreateChanges(patchPlan, authenticityInvalidated);
        System.Collections.Generic.List<RawDiagnostic> diagnostics = new System.Collections.Generic.List<RawDiagnostic>();
        if (authenticityInvalidated)
        {
            diagnostics.Add(Diagnostic(RawDiagnosticCode.SignatureInvalidated, RawSeverity.Warning, document.Format, "Retained authenticity data is preserved byte for byte but no longer describes the patched file; no replacement signature will be fabricated."));
        }
        if (consentMissing)
        {
            diagnostics.Add(Diagnostic(RawDiagnosticCode.DataLossConsentRequired, RawSeverity.Error, document.Format, "Invalidating retained authenticity data requires explicit data-loss consent before the fixed-size patch can be applied."));
        }
        RawWritePlan writePlan = new RawWritePlan(!consentMissing, document.Source.Length, document.ProfileName, changes, diagnostics.ToArray());
        writePlan.AuthenticityBlocks = authenticity;
        writePlan.AuthenticityInvalidated = authenticityInvalidated;
        return new RawMetadataPatchState(document, options, patchPlan, writePlan);
    }

    internal static void ValidateRequest(RawDocument document, RawMetadataPatchEdit[] edits, RawWriteOptions options)
    {
        if (document == null || options == null || edits == null)
        {
            throw new RawWriteException(RawDiagnosticCode.InvalidArgument, "A document, fixed-size metadata edits, and write options are required.", -1);
        }
        if (document.Format != RawFormat.Tiff && document.Format != RawFormat.Cr2 && document.Format != RawFormat.Nef)
        {
            throw new RawWriteException(RawDiagnosticCode.UnsupportedFormat, "Fixed-size metadata patches support TIFF, CR2, and NEF documents.", -1);
        }
        if (!document.SourceRetained || document.Source.Length == 0 || document.TiffGraph == null)
        {
            throw new RawWriteException(RawDiagnosticCode.InvalidArgument, "A fully opened source-retaining TIFF, CR2, or NEF document is required.", -1);
        }
        if (options.Mode != RawWriteMode.MetadataPatch || options.UnknownDataPolicy != RawUnknownDataPolicy.Preserve || !options.Validate().IsValid())
        {
            throw new RawWriteException(RawDiagnosticCode.InvalidArgument, "Metadata patch mode, byte preservation, post-write validation, and valid resource limits are required.", -1);
        }
        if (edits.Length == 0)
        {
            throw new RawWriteException(RawDiagnosticCode.InvalidArgument, "At least one fixed-size metadata edit is required.", -1);
        }
        CheckedMath.FileSize(document.Source.Length, options.Limits);
    }

    internal static TiffPatchEdit[] CreateInternalEdits(RawDocument document, RawMetadataPatchEdit[] edits)
    {
        TiffPatchEdit[] result = new TiffPatchEdit[edits.Length];
        RawByteOrder byteOrder = (document.TiffGraph ?? throw new System.InvalidOperationException("A required value is unavailable.")).Header.ByteOrder;
        int index = 0;
        while (index < edits.Length)
        {
            RawMetadataPatchEdit edit = edits[index];
            if (edit == null || edit.IfdPath == null || edit.IfdPath.Length == 0 || edit.Value == null || edit.Value.RawBytes == null)
            {
                throw new RawWriteException(RawDiagnosticCode.InvalidArgument, "Every fixed-size metadata edit requires a directory, tag, and exact value.", -1);
            }
            if (ByteOrderMatters(edit.Value.Type) && edit.Value.ByteOrder != byteOrder)
            {
                throw new RawWriteException(RawDiagnosticCode.WritePlanningFailed, "A numeric fixed-size metadata value must use the container byte order.", -1);
            }
            byte[] bytes = new byte[edit.Value.RawBytes.Length];
            System.Array.Copy(edit.Value.RawBytes, bytes, bytes.Length);
            result[index] = new TiffPatchEdit(edit.IfdPath, edit.TagId, (TiffFieldType)(ushort)edit.Value.Type, edit.Value.Count, bytes);
            index++;
        }
        return result;
    }

    internal static bool ByteOrderMatters(RawTagType type)
    {
        return type != RawTagType.Byte && type != RawTagType.Ascii && type != RawTagType.SignedByte && type != RawTagType.Undefined && type != RawTagType.Utf8;
    }

    internal static RawWriteChange[] CreateChanges(TiffPatchPlan patchPlan, bool requiresConsent)
    {
        RawWriteChange[] changes = new RawWriteChange[patchPlan.Regions.Length];
        int index = 0;
        while (index < patchPlan.Regions.Length)
        {
            TiffPatchRegion region = patchPlan.Regions[index];
            string tag = region.Tag.ToString("X4", System.Globalization.CultureInfo.InvariantCulture);
            changes[index] = new RawWriteChange(RawWriteChangeKind.Changed, region.IfdPath + "/0x" + tag, "Replace the existing TIFF tag with an exact same-type, same-count encoded value.", region.Offset, region.Bytes.Length, region.Offset, region.Bytes.Length, requiresConsent);
            index++;
        }
        return changes;
    }

    internal static RawPayloadBlock[] FindAuthenticity(RawDocument document)
    {
        System.Collections.Generic.List<RawPayloadBlock> blocks = new System.Collections.Generic.List<RawPayloadBlock>();
        int index = 0;
        while (document.PayloadMap != null && document.PayloadMap.Blocks != null && index < document.PayloadMap.Blocks.Length)
        {
            RawPayloadBlock block = document.PayloadMap.Blocks[index];
            if (block != null && block.Kind == RawPayloadKind.Authenticity)
            {
                blocks.Add(block);
            }
            index++;
        }
        return blocks.ToArray();
    }

    internal static void ValidateOutput(RawMetadataPatchState state, byte[] result)
    {
        if (result == null || result.Length != state.Document.Source.Length)
        {
            throw OutputFailure("The fixed-size metadata patch changed the bounded file length.", -1);
        }
        RequireOnlyPlannedBytesChanged(state, result);
        RequirePreservedPayloads(state, result);
        RawOpenOptions openOptions = new RawOpenOptions();
        openOptions.Limits = state.Options.Limits;
        openOptions.CancellationToken = state.Options.CancellationToken;
        openOptions.ReadMetadata = true;
        openOptions.ReadPreviews = true;
        openOptions.ReadRawPayload = true;
        openOptions.AllowExperimentalProfiles = true;
        RawDocument reopened = RawCodec.Open(result, null, openOptions);
        if (reopened.Format != state.Document.Format || reopened.TiffGraph == null || !TiffValidator.Validate(reopened.TiffGraph, false).IsValid())
        {
            throw OutputFailure("The patched output did not reopen as a structurally valid document of the original format.", -1);
        }
        int index = 0;
        while (index < state.PatchPlan.Regions.Length)
        {
            TiffPatchRegion region = state.PatchPlan.Regions[index];
            TiffIfd? ifd = reopened.TiffGraph.FindIfd(region.IfdPath);
            TiffEntry? entry = ifd == null ? null : ifd.FindEntry(region.Tag);
            if (entry == null || !TiffPatchAnalyzer.BytesEqual(entry.RawBytes, region.Bytes))
            {
                throw OutputFailure("The patched output did not retain an exact planned TIFF tag value.", region.Offset);
            }
            index++;
        }
    }

    internal static void RequireOnlyPlannedBytesChanged(RawMetadataPatchState state, byte[] result)
    {
        System.ReadOnlySpan<byte> source = state.Document.Source.Span;
        int index = 0;
        while (index < result.Length)
        {
            state.Options.CancellationToken.ThrowIfCancellationRequested();
            if (source[index] != result[index] && !InsidePatchRegion(index, state.PatchPlan.Regions))
            {
                throw OutputFailure("A byte outside the planned fixed-size metadata regions changed.", index);
            }
            index++;
        }
    }

    internal static bool InsidePatchRegion(long offset, TiffPatchRegion[] regions)
    {
        int index = 0;
        while (index < regions.Length)
        {
            TiffPatchRegion region = regions[index];
            if (offset >= region.Offset && offset < CheckedMath.Add(region.Offset, region.Bytes.Length))
            {
                return true;
            }
            index++;
        }
        return false;
    }

    internal static void RequirePreservedPayloads(RawMetadataPatchState state, byte[] result)
    {
        RawPayloadBlock[] blocks = state.Document.PayloadMap.Blocks;
        int index = 0;
        while (index < blocks.Length)
        {
            RawPayloadBlock block = blocks[index];
            if (block != null && (block.PreserveByteExact || block.Kind == RawPayloadKind.RawImage || block.Kind == RawPayloadKind.Authenticity))
            {
                if (block.Offset < 0 || block.Length <= 0 || block.Offset > int.MaxValue || block.Length > int.MaxValue || block.Offset > state.Document.Source.Length - block.Length)
                {
                    throw OutputFailure("A preserved payload has an invalid bounded source range.", block.Offset);
                }
                int position = CheckedMath.ToInt32(block.Offset);
                int end = CheckedMath.ToInt32(CheckedMath.Add(block.Offset, block.Length));
                while (position < end)
                {
                    if (state.Document.Source.Span[position] != result[position])
                    {
                        throw OutputFailure("A raw, unknown, private, or authenticity payload changed during the metadata patch.", position);
                    }
                    position++;
                }
            }
            index++;
        }
    }

    internal static RawDiagnostic Diagnostic(RawDiagnosticCode code, RawSeverity severity, RawFormat format, string message)
    {
        return new RawDiagnostic(code, severity, format, -1, string.Empty, -1, "MetadataPatch", message, false);
    }

    internal static RawWriteException OutputFailure(string message, long offset)
    {
        return new RawWriteException(RawDiagnosticCode.OutputValidationFailed, message, offset);
    }
}
