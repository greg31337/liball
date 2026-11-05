namespace raw;

internal static class RawWritePlanner
{
    internal static RawWriteOptions? CreatePlanningOptions(RawWriteOptions source)
    {
        if (source == null)
        {
            return null;
        }
        RawWriteOptions result = new RawWriteOptions();
        result.Limits = source.Limits;
        result.CancellationToken = source.CancellationToken;
        result.Mode = source.Mode;
        result.UnknownDataPolicy = source.UnknownDataPolicy;
        result.ProfileName = source.ProfileName;
        result.AllowExperimentalProfiles = source.AllowExperimentalProfiles;
        result.AllowLossy = source.AllowLossy;
        result.DataLossConsent = source.DataLossConsent || source.AllowLossy;
        result.ValidateAfterWrite = source.ValidateAfterWrite;
        result.VerifyPixelIdentity = source.VerifyPixelIdentity;
        result.VerifyRawPayloadIdentity = source.VerifyRawPayloadIdentity;
        return result;
    }

    internal static RawWritePlan CompleteNewPlan(RawWritePlan plan, RawWriteProfile profile, IRawProfileCatalogue catalogue, RawWriteOptions requested)
    {
        if (plan == null || profile == null || catalogue == null || requested == null)
        {
            throw new RawWriteException(RawDiagnosticCode.WritePlanningFailed, "A complete new-container planning result is required.", -1);
        }
        RawWriteCompatibilityMatrix matrix = catalogue.GetWriteCompatibilityMatrix(profile.Identity.Format);
        if (matrix == null)
        {
            throw new RawWriteException(RawDiagnosticCode.InvalidProfile, "The configured profile source returned no write compatibility matrix.", -1);
        }
        RawWriteCompatibilityEntry[] compatibility = matrix.FindProfile(profile.Id);
        System.Collections.Generic.List<RawDiagnostic> diagnostics = new System.Collections.Generic.List<RawDiagnostic>();
        int index = 0;
        while (index < plan.Diagnostics.Length)
        {
            if (plan.Diagnostics[index] != null)
            {
                diagnostics.Add(plan.Diagnostics[index]);
            }
            index++;
        }
        if (profile.Experimental)
        {
            diagnostics.Add(Diagnostic(RawDiagnosticCode.UnsupportedProfile, RawSeverity.Warning, profile.Identity.Format, "The selected write profile is experimental and required explicit opt-in."));
        }
        if (!Confirmed(compatibility))
        {
            diagnostics.Add(Diagnostic(RawDiagnosticCode.UnsupportedProfile, RawSeverity.Warning, profile.Identity.Format, "The selected write profile has no passed application/version compatibility result."));
        }
        bool requiresConsent = plan.RequiresDataLossConsent();
        bool canWrite = plan.CanWrite;
        if (requiresConsent && !requested.DataLossConsent)
        {
            diagnostics.Add(Diagnostic(RawDiagnosticCode.DataLossConsentRequired, RawSeverity.Error, profile.Identity.Format, "The planned irreversible write requires explicit data-loss consent before execution."));
            canWrite = false;
        }
        else if (requiresConsent)
        {
            diagnostics.Add(Diagnostic(RawDiagnosticCode.DataLossConsentRequired, RawSeverity.Warning, profile.Identity.Format, "The plan contains irreversible actions covered by explicit caller consent."));
        }
        RawWritePlan result = RawWritePlan.CreateWithCompatibility(canWrite, plan.PredictedSize, profile.Id, compatibility, plan.Changes, diagnostics.ToArray());
        result.PixelValidation = plan.PixelValidation;
        return result;
    }

    internal static RawWritePlan PlanCr2Rewrite(RawDocument document, RawWriteOptions options)
    {
        Cr2RewriteState state = Cr2Rewriter.CreateState(document, options);
        return CreateRewritePlan(state, RawFormat.Cr2);
    }

    internal static RawWritePlan PlanNefRewrite(RawDocument document, RawWriteOptions options)
    {
        Cr2RewriteState state = NefRewriter.CreateState(document, options);
        return CreateRewritePlan(state, RawFormat.Nef);
    }

    internal static RawWritePlan CreateRewritePlan(Cr2RewriteState state, RawFormat format)
    {
        System.Collections.Generic.List<RawWriteChange> changes = new System.Collections.Generic.List<RawWriteChange>();
        AddMetadataChanges(state, changes);
        AddBlockChanges(state, changes);
        System.Collections.Generic.List<RawDiagnostic> diagnostics = new System.Collections.Generic.List<RawDiagnostic>();
        bool dropped = false;
        int index = 0;
        while (index < changes.Count)
        {
            if (changes[index].Kind == RawWriteChangeKind.Dropped)
            {
                dropped = true;
                break;
            }
            index++;
        }
        if (dropped)
        {
            diagnostics.Add(Diagnostic(RawDiagnosticCode.None, RawSeverity.Warning, format, "The plan contains caller-staged metadata removals."));
        }
        string profileName = state.Document.ProfileName == null ? string.Empty : state.Document.ProfileName;
        RawPayloadBlock[] authenticity = state.Document.PayloadMap.Find(RawPayloadKind.Authenticity);
        bool authenticityInvalidated = authenticity.Length != 0 && HasEdits(state.Document);
        if (authenticityInvalidated)
        {
            diagnostics.Add(Diagnostic(RawDiagnosticCode.SignatureInvalidated, RawSeverity.Warning, format, "Authenticity data is preserved byte for byte but no longer describes the edited file; no replacement signature will be fabricated."));
        }
        RawWritePlan result = RawWritePlan.CreateWithCompatibility(true, (state.Plan ?? throw new System.InvalidOperationException("A required value is unavailable.")).PredictedSize, profileName, System.Array.Empty<RawWriteCompatibilityEntry>(), changes.ToArray(), diagnostics.ToArray());
        result.AuthenticityBlocks = authenticity;
        result.AuthenticityInvalidated = authenticityInvalidated;
        return result;
    }

    internal static bool HasEdits(RawDocument document)
    {
        return document != null && (document.Metadata != null && document.Metadata.PendingEdits != null && document.Metadata.PendingEdits.Length != 0 || document.PendingPreviewEdits != null && document.PendingPreviewEdits.Length != 0);
    }

    internal static void AddMetadataChanges(Cr2RewriteState state, System.Collections.Generic.List<RawWriteChange> changes)
    {
        int ifdIndex = 0;
        while (ifdIndex < ((state.Plan ?? throw new System.InvalidOperationException("A required value is unavailable.")).Ifds ?? throw new System.InvalidOperationException("A required value is unavailable.")).Length)
        {
            TiffLayoutIfdPlan plannedIfd = (state.Plan.Ifds ?? throw new System.InvalidOperationException("A required value is unavailable."))[ifdIndex];
            TiffIfd? sourceIfd = (state.Document.TiffGraph ?? throw new System.InvalidOperationException("A required value is unavailable.")).FindIfd(plannedIfd.Path);
            int entryIndex = 0;
            while (entryIndex < (plannedIfd.Entries ?? throw new System.InvalidOperationException("A required value is unavailable.")).Length)
            {
                TiffLayoutEntryPlan planned = plannedIfd.Entries[entryIndex];
                RawMetadataEdit? edit = Cr2Rewriter.PendingEdit(state.Document.Metadata, plannedIfd.Path, planned.Tag);
                if (edit != null)
                {
                    TiffEntry? source = sourceIfd == null ? null : sourceIfd.FindEntry(planned.Tag);
                    RawWriteChangeKind kind = source == null ? RawWriteChangeKind.Added : RawWriteChangeKind.Changed;
                    long originalOffset = source == null ? -1 : EntryOffset(source);
                    long originalLength = source == null ? 0 : source.RawBytes.Length;
                    long plannedOffset = planned.Inline ? planned.EntryOffset : (long)planned.ValueOffset;
                    string description = source == null ? "Add the staged metadata value." : "Apply the staged metadata replacement.";
                    changes.Add(new RawWriteChange(kind, TagLocation(plannedIfd.Path, planned.Tag), description, originalOffset, originalLength, plannedOffset, planned.ValueLength, false));
                }
                entryIndex++;
            }
            if (sourceIfd != null)
            {
                entryIndex = 0;
                while (entryIndex < sourceIfd.Entries.Length)
                {
                    TiffEntry source = sourceIfd.Entries[entryIndex];
                    RawMetadataEdit? edit = Cr2Rewriter.PendingEdit(state.Document.Metadata, sourceIfd.Path, source.Tag);
                    if (edit != null && edit.Kind == RawMetadataEditKind.Remove)
                    {
                        changes.Add(new RawWriteChange(RawWriteChangeKind.Dropped, TagLocation(sourceIfd.Path, source.Tag), "Remove the caller-staged metadata value.", EntryOffset(source), source.RawBytes.Length, -1, 0, false));
                    }
                    entryIndex++;
                }
            }
            ifdIndex++;
        }
    }

    internal static void AddBlockChanges(Cr2RewriteState state, System.Collections.Generic.List<RawWriteChange> changes)
    {
        int index = 0;
        while (index < ((state.Plan ?? throw new System.InvalidOperationException("A required value is unavailable.")).Blocks ?? throw new System.InvalidOperationException("A required value is unavailable.")).Length)
        {
            TiffLayoutBlockPlan planned = (state.Plan.Blocks ?? throw new System.InvalidOperationException("A required value is unavailable."))[index];
            Cr2RewriteBlock? source = FindRewriteBlock(state.Blocks, planned.Name);
            if (source == null)
            {
                throw new RawWriteException(RawDiagnosticCode.WritePlanningFailed, "A planned payload block has no preserving-rewrite source mapping.", (long)planned.Offset);
            }
            bool replacement = HasReplacement(state.Replacements, source.OriginalOffset, source.OriginalLength);
            RawWriteChangeKind kind;
            string description;
            if (replacement)
            {
                kind = RawWriteChangeKind.Changed;
                description = "Replace the encoded preview payload with caller-supplied bytes.";
            }
            else if ((long)planned.Offset != source.OriginalOffset)
            {
                kind = RawWriteChangeKind.Relocated;
                description = "Relocate the payload while preserving its bytes exactly.";
            }
            else
            {
                kind = RawWriteChangeKind.Copied;
                description = "Copy the payload bytes at their original location.";
            }
            changes.Add(new RawWriteChange(kind, source.Path, description, source.OriginalOffset, source.OriginalLength, (long)planned.Offset, planned.Length, false));
            index++;
        }
    }

    internal static Cr2RewriteBlock? FindRewriteBlock(Cr2RewriteBlock[] blocks, string? name)
    {
        int index = 0;
        while (index < blocks.Length)
        {
            if (blocks[index].Name == name)
            {
                return blocks[index];
            }
            index++;
        }
        return null;
    }

    internal static bool HasReplacement(Cr2RewriteReplacement[] replacements, long offset, long length)
    {
        int index = 0;
        while (index < replacements.Length)
        {
            RawPayloadBlock original = replacements[index].Original;
            if (original != null && original.Offset == offset && original.Length == length)
            {
                return true;
            }
            index++;
        }
        return false;
    }

    internal static long EntryOffset(TiffEntry entry)
    {
        return entry.Inline ? entry.EntryOffset : (long)entry.ValueOffset;
    }

    internal static string TagLocation(string? path, ushort tag)
    {
        return path + "/0x" + tag.ToString("X4", System.Globalization.CultureInfo.InvariantCulture);
    }

    internal static bool Confirmed(RawWriteCompatibilityEntry[] entries)
    {
        int index = 0;
        while (index < entries.Length)
        {
            if (entries[index] != null && entries[index].IsConfirmed())
            {
                return true;
            }
            index++;
        }
        return false;
    }

    internal static RawDiagnostic Diagnostic(RawDiagnosticCode code, RawSeverity severity, RawFormat format, string message)
    {
        return new RawDiagnostic(code, severity, format, -1, string.Empty, -1, "WritePlanner", message, false);
    }
}
