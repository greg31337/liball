using System;

namespace raw;

/// <summary>Encodes profile-gated new raw containers.</summary>
internal static class RawWriter
{
    /// <summary>Rewrites an opened NEF through a validated sibling temporary file before atomic destination commit.</summary>
    /// <param name="destinationPath">The destination file path.</param>
    /// <param name="document">A fully opened source-retaining NEF document.</param>
    /// <param name="options">Preserving rewrite and validation controls.</param>
    /// <returns>The committed destination or recoverable temporary-file result.</returns>
    /// <exception cref="RawWriteException">The path, rewrite, staging, or validation request is invalid.</exception>
    /// <exception cref="RawUnsupportedException">The document cannot be rewritten conservatively.</exception>
    public static RawPathWriteResult RewriteNef(string destinationPath, RawDocument document, RawWriteOptions options)
    {
        byte[] bytes = RewriteNef(document, options);
        return RawStagedPathWriter.WriteValidated(destinationPath, bytes, RawFormat.Nef, options);
    }

    /// <summary>Rewrites an opened CR2 through a validated sibling temporary file before atomic destination commit.</summary>
    /// <param name="destinationPath">The destination file path.</param>
    /// <param name="document">A fully opened source-retaining CR2 document.</param>
    /// <param name="options">Preserving rewrite and validation controls.</param>
    /// <returns>The committed destination or recoverable temporary-file result.</returns>
    /// <exception cref="RawWriteException">The path, rewrite, staging, or validation request is invalid.</exception>
    /// <exception cref="RawUnsupportedException">The document cannot be rewritten conservatively.</exception>
    public static RawPathWriteResult RewriteCr2(string destinationPath, RawDocument document, RawWriteOptions options)
    {
        byte[] bytes = RewriteCr2(document, options);
        return RawStagedPathWriter.WriteValidated(destinationPath, bytes, RawFormat.Cr2, options);
    }

    /// <summary>Encodes a new CR2 through a validated sibling temporary file before atomic destination commit.</summary>
    /// <param name="destinationPath">The destination file path.</param>
    /// <param name="input">Exact stored samples, previews, Canon blocks, and provenance metadata.</param>
    /// <param name="options">New-container profile and validation controls.</param>
    /// <returns>The committed destination or recoverable temporary-file result.</returns>
    /// <exception cref="RawWriteException">The path, input, staging, output, or validation fails.</exception>
    /// <exception cref="RawUnsupportedException">No enabled profile supports the request.</exception>
    public static RawPathWriteResult EncodeCr2(string destinationPath, RawCr2WriteInput input, RawWriteOptions options)
    {
        byte[] bytes = EncodeCr2(input, options);
        return RawStagedPathWriter.WriteValidated(destinationPath, bytes, RawFormat.Cr2, options);
    }

    /// <summary>Encodes a custom-catalogue CR2 through a validated sibling temporary file before atomic destination commit.</summary>
    /// <param name="destinationPath">The destination file path.</param>
    /// <param name="input">Exact stored samples, previews, Canon blocks, and provenance metadata.</param>
    /// <param name="catalogue">The validated profile catalogue.</param>
    /// <param name="options">New-container profile and validation controls.</param>
    /// <returns>The committed destination or recoverable temporary-file result.</returns>
    /// <exception cref="RawWriteException">The path, input, staging, output, or validation fails.</exception>
    /// <exception cref="RawUnsupportedException">The selected profile lacks required evidence.</exception>
    public static RawPathWriteResult EncodeCr2(string destinationPath, RawCr2WriteInput input, RawProfileCatalogue catalogue, RawWriteOptions options)
    {
        return EncodeCr2(destinationPath, input, (IRawProfileCatalogue)catalogue, options);
    }

    /// <summary>Encodes a CR2 through a substitutable profile source and a validated sibling temporary file before atomic destination commit.</summary>
    /// <param name="destinationPath">The destination file path.</param>
    /// <param name="input">Exact stored samples, previews, Canon blocks, and provenance metadata.</param>
    /// <param name="catalogue">The read-only profile source used for selection and evidence checks.</param>
    /// <param name="options">New-container profile and validation controls.</param>
    /// <returns>The committed destination or recoverable temporary-file result.</returns>
    /// <exception cref="RawWriteException">The path, input, staging, output, or validation fails.</exception>
    /// <exception cref="RawUnsupportedException">The selected profile lacks required evidence.</exception>
    public static RawPathWriteResult EncodeCr2(string destinationPath, RawCr2WriteInput input, IRawProfileCatalogue catalogue, RawWriteOptions options)
    {
        byte[] bytes = EncodeCr2(input, catalogue, options);
        return RawStagedPathWriter.WriteValidated(destinationPath, bytes, RawFormat.Cr2, options);
    }

    /// <summary>Encodes a new NEF through a validated sibling temporary file before atomic destination commit.</summary>
    /// <param name="destinationPath">The destination file path.</param>
    /// <param name="input">Exact stored samples, preview, and Nikon provenance metadata.</param>
    /// <param name="options">New-container profile and validation controls.</param>
    /// <returns>The committed destination or recoverable temporary-file result.</returns>
    /// <exception cref="RawWriteException">The path, input, staging, output, or validation fails.</exception>
    /// <exception cref="RawUnsupportedException">No enabled profile supports the request.</exception>
    public static RawPathWriteResult EncodeNef(string destinationPath, RawNefWriteInput input, RawWriteOptions options)
    {
        byte[] bytes = EncodeNef(input, options);
        return RawStagedPathWriter.WriteValidated(destinationPath, bytes, RawFormat.Nef, options);
    }

    /// <summary>Encodes a custom-catalogue NEF through a validated sibling temporary file before atomic destination commit.</summary>
    /// <param name="destinationPath">The destination file path.</param>
    /// <param name="input">Exact stored samples, preview, and Nikon provenance metadata.</param>
    /// <param name="catalogue">The validated profile catalogue.</param>
    /// <param name="options">New-container profile and validation controls.</param>
    /// <returns>The committed destination or recoverable temporary-file result.</returns>
    /// <exception cref="RawWriteException">The path, input, staging, output, or validation fails.</exception>
    /// <exception cref="RawUnsupportedException">The selected profile lacks required evidence.</exception>
    public static RawPathWriteResult EncodeNef(string destinationPath, RawNefWriteInput input, RawProfileCatalogue catalogue, RawWriteOptions options)
    {
        return EncodeNef(destinationPath, input, (IRawProfileCatalogue)catalogue, options);
    }

    /// <summary>Encodes a NEF through a substitutable profile source and a validated sibling temporary file before atomic destination commit.</summary>
    /// <param name="destinationPath">The destination file path.</param>
    /// <param name="input">Exact stored samples, preview, and Nikon provenance metadata.</param>
    /// <param name="catalogue">The read-only profile source used for selection and evidence checks.</param>
    /// <param name="options">New-container profile and validation controls.</param>
    /// <returns>The committed destination or recoverable temporary-file result.</returns>
    /// <exception cref="RawWriteException">The path, input, staging, output, or validation fails.</exception>
    /// <exception cref="RawUnsupportedException">The selected profile lacks required evidence.</exception>
    public static RawPathWriteResult EncodeNef(string destinationPath, RawNefWriteInput input, IRawProfileCatalogue catalogue, RawWriteOptions options)
    {
        byte[] bytes = EncodeNef(input, catalogue, options);
        return RawStagedPathWriter.WriteValidated(destinationPath, bytes, RawFormat.Nef, options);
    }

    /// <summary>Rewrites an opened NEF while preserving mapped payload bytes and applying staged metadata or preview edits.</summary>
    /// <param name="document">A fully opened NEF document retaining its source, metadata, previews, and TIFF graph.</param>
    /// <param name="options">Preserving rewrite policy, limits, cancellation, and verification controls.</param>
    /// <returns>The complete rewritten NEF bytes.</returns>
    /// <exception cref="RawWriteException">The input, options, preservation plan, output validation, or byte-identity check fails.</exception>
    /// <exception cref="RawUnsupportedException">A staged representation or maker-note layout cannot be rewritten conservatively.</exception>
    public static byte[] RewriteNef(RawDocument document, RawWriteOptions options)
    {
        return NefRewriter.Rewrite(document, options);
    }

    /// <summary>Rewrites an opened NEF to a caller-owned writable stream without closing it.</summary>
    /// <param name="output">The writable destination left open on return.</param>
    /// <param name="document">A fully opened NEF document retaining its source, metadata, previews, and TIFF graph.</param>
    /// <param name="options">Preserving rewrite policy, limits, cancellation, and verification controls.</param>
    /// <exception cref="RawWriteException">The stream, input, options, preservation plan, output validation, or byte-identity check fails.</exception>
    /// <exception cref="RawUnsupportedException">A staged representation or maker-note layout cannot be rewritten conservatively.</exception>
    public static void RewriteNef(System.IO.Stream output, RawDocument document, RawWriteOptions options)
    {
        if (output == null || !output.CanWrite)
        {
            throw new RawWriteException(RawDiagnosticCode.InvalidArgument, "A writable NEF rewrite destination is required.", -1);
        }
        byte[] bytes = RewriteNef(document, options);
        options.CancellationToken.ThrowIfCancellationRequested();
        output.Write(bytes, 0, bytes.Length);
    }

    /// <summary>Rewrites an opened CR2 while preserving mapped payload bytes and applying staged metadata or preview edits.</summary>
    /// <param name="document">A fully opened CR2 document retaining its source, metadata, previews, and TIFF graph.</param>
    /// <param name="options">Preserving rewrite policy, limits, cancellation, and verification controls.</param>
    /// <returns>The complete rewritten CR2 bytes.</returns>
    /// <exception cref="RawWriteException">The input, options, preservation plan, output validation, or byte-identity check fails.</exception>
    /// <exception cref="RawUnsupportedException">A staged representation or maker-note layout cannot be rewritten conservatively.</exception>
    public static byte[] RewriteCr2(RawDocument document, RawWriteOptions options)
    {
        return Cr2Rewriter.Rewrite(document, options);
    }

    /// <summary>Rewrites an opened CR2 to a caller-owned writable stream without closing it.</summary>
    /// <param name="output">The writable destination left open on return.</param>
    /// <param name="document">A fully opened CR2 document retaining its source, metadata, previews, and TIFF graph.</param>
    /// <param name="options">Preserving rewrite policy, limits, cancellation, and verification controls.</param>
    /// <exception cref="RawWriteException">The stream, input, options, preservation plan, output validation, or byte-identity check fails.</exception>
    /// <exception cref="RawUnsupportedException">A staged representation or maker-note layout cannot be rewritten conservatively.</exception>
    public static void RewriteCr2(System.IO.Stream output, RawDocument document, RawWriteOptions options)
    {
        if (output == null || !output.CanWrite)
        {
            throw new RawWriteException(RawDiagnosticCode.InvalidArgument, "A writable CR2 rewrite destination is required.", -1);
        }
        byte[] bytes = RewriteCr2(document, options);
        options.CancellationToken.ThrowIfCancellationRequested();
        output.Write(bytes, 0, bytes.Length);
    }

    /// <summary>Encodes one new classic Bayer CR2 file through a separately qualified write profile.</summary>
    /// <param name="input">Exact stored samples, previews, Canon blocks, and provenance metadata.</param>
    /// <param name="options">New-container write policy, profile opt-in, limits, and validation controls.</param>
    /// <returns>The complete CR2 bytes.</returns>
    /// <exception cref="RawWriteException">The input, options, selected profile, output structure, or pixel verification fails.</exception>
    /// <exception cref="RawUnsupportedException">The requested write profile is unavailable or requires explicit opt-in.</exception>
    public static byte[] EncodeCr2(RawCr2WriteInput input, RawWriteOptions options)
    {
        if (options == null)
        {
            throw new RawWriteException(RawDiagnosticCode.InvalidArgument, "Valid new-container write options are required for CR2 encoding.", -1);
        }
        RawProfileCatalogue catalogue = RawProfileCatalogue.CreateBuiltIn(options.Limits);
        RawWriteProfile profile = SelectCr2Profile(input, options, catalogue);
        return Cr2Encoder.Encode(input, profile, options);
    }

    /// <summary>Encodes one new classic Bayer CR2 file through an evidence-qualified profile in a caller-supplied catalogue.</summary>
    /// <param name="input">Exact stored samples, previews, Canon blocks, and provenance metadata.</param>
    /// <param name="catalogue">The validated catalogue containing the selected read and write profiles.</param>
    /// <param name="options">New-container write policy, profile selection, limits, and validation controls.</param>
    /// <returns>The complete CR2 bytes.</returns>
    /// <exception cref="RawWriteException">The input, options, selected profile, output structure, or pixel verification fails.</exception>
    /// <exception cref="RawUnsupportedException">The requested profile lacks independent structural and application evidence.</exception>
    public static byte[] EncodeCr2(RawCr2WriteInput input, RawProfileCatalogue catalogue, RawWriteOptions options)
    {
        return EncodeCr2(input, (IRawProfileCatalogue)catalogue, options);
    }

    /// <summary>Encodes one new classic Bayer CR2 file through an evidence-qualified profile from a substitutable profile source.</summary>
    /// <param name="input">Exact stored samples, previews, Canon blocks, and provenance metadata.</param>
    /// <param name="catalogue">The read-only profile source used for selection and evidence checks.</param>
    /// <param name="options">New-container write policy, profile selection, limits, and validation controls.</param>
    /// <returns>The complete CR2 bytes.</returns>
    /// <exception cref="RawWriteException">The input, options, selected profile, output structure, or pixel verification fails.</exception>
    /// <exception cref="RawUnsupportedException">The requested profile lacks independent structural and application evidence.</exception>
    public static byte[] EncodeCr2(RawCr2WriteInput input, IRawProfileCatalogue catalogue, RawWriteOptions options)
    {
        RawWriteProfile profile = SelectCr2Profile(input, options, catalogue);
        return Cr2Encoder.Encode(input, profile, options);
    }

    /// <summary>Encodes one new classic Bayer CR2 file to a caller-owned writable stream.</summary>
    /// <param name="output">The writable destination left open on return.</param>
    /// <param name="input">Exact stored samples, previews, Canon blocks, and provenance metadata.</param>
    /// <param name="options">New-container write policy, profile opt-in, limits, and validation controls.</param>
    /// <exception cref="RawWriteException">The stream, input, options, selected profile, output structure, or pixel verification fails.</exception>
    /// <exception cref="RawUnsupportedException">The requested write profile is unavailable or requires explicit opt-in.</exception>
    public static void EncodeCr2(System.IO.Stream output, RawCr2WriteInput input, RawWriteOptions options)
    {
        if (output == null || !output.CanWrite)
        {
            throw new RawWriteException(RawDiagnosticCode.InvalidArgument, "A writable CR2 destination stream is required.", -1);
        }
        byte[] bytes = EncodeCr2(input, options);
        options.CancellationToken.ThrowIfCancellationRequested();
        output.Write(bytes, 0, bytes.Length);
    }

    /// <summary>Encodes one new profile-gated Bayer NEF file, requiring explicit consent for lossy compression.</summary>
    /// <param name="input">Exact stored samples, preview, and reviewed Nikon provenance metadata.</param>
    /// <param name="options">New-container write policy, profile opt-in, limits, and validation controls.</param>
    /// <returns>The complete NEF bytes.</returns>
    /// <exception cref="RawWriteException">The input, options, selected profile, output structure, or pixel verification fails.</exception>
    /// <exception cref="RawUnsupportedException">The requested write profile is unavailable or requires explicit opt-in.</exception>
    public static byte[] EncodeNef(RawNefWriteInput input, RawWriteOptions options)
    {
        if (options == null)
        {
            throw new RawWriteException(RawDiagnosticCode.InvalidArgument, "Valid new-container write options are required for NEF encoding.", -1);
        }
        RawProfileCatalogue catalogue = RawProfileCatalogue.CreateBuiltIn(options.Limits);
        RawWriteProfile profile = SelectNefProfile(input, options, catalogue);
        return NefEncoder.Encode(input, profile, options);
    }

    /// <summary>Encodes one new Bayer NEF file through an evidence-qualified profile in a caller-supplied catalogue.</summary>
    /// <param name="input">Exact stored samples, preview, and reviewed Nikon provenance metadata.</param>
    /// <param name="catalogue">The validated catalogue containing the selected read and write profiles.</param>
    /// <param name="options">New-container write policy, profile selection, limits, and validation controls.</param>
    /// <returns>The complete NEF bytes.</returns>
    /// <exception cref="RawWriteException">The input, options, selected profile, output structure, or pixel verification fails.</exception>
    /// <exception cref="RawUnsupportedException">The requested profile lacks independent structural and application evidence.</exception>
    public static byte[] EncodeNef(RawNefWriteInput input, RawProfileCatalogue catalogue, RawWriteOptions options)
    {
        return EncodeNef(input, (IRawProfileCatalogue)catalogue, options);
    }

    /// <summary>Encodes one new Bayer NEF file through an evidence-qualified profile from a substitutable profile source.</summary>
    /// <param name="input">Exact stored samples, preview, and reviewed Nikon provenance metadata.</param>
    /// <param name="catalogue">The read-only profile source used for selection and evidence checks.</param>
    /// <param name="options">New-container write policy, profile selection, limits, and validation controls.</param>
    /// <returns>The complete NEF bytes.</returns>
    /// <exception cref="RawWriteException">The input, options, selected profile, output structure, or pixel verification fails.</exception>
    /// <exception cref="RawUnsupportedException">The requested profile lacks independent structural and application evidence.</exception>
    public static byte[] EncodeNef(RawNefWriteInput input, IRawProfileCatalogue catalogue, RawWriteOptions options)
    {
        RawWriteProfile profile = SelectNefProfile(input, options, catalogue);
        return NefEncoder.Encode(input, profile, options);
    }

    /// <summary>Encodes one new profile-gated Bayer NEF file to a caller-owned writable stream.</summary>
    /// <param name="output">The writable destination left open on return.</param>
    /// <param name="input">Exact stored samples, preview, and reviewed Nikon provenance metadata.</param>
    /// <param name="options">New-container write policy, profile opt-in, limits, and validation controls.</param>
    /// <exception cref="RawWriteException">The stream, input, options, selected profile, output structure, or pixel verification fails.</exception>
    /// <exception cref="RawUnsupportedException">The requested write profile is unavailable or requires explicit opt-in.</exception>
    public static void EncodeNef(System.IO.Stream output, RawNefWriteInput input, RawWriteOptions options)
    {
        if (output == null || !output.CanWrite)
        {
            throw new RawWriteException(RawDiagnosticCode.InvalidArgument, "A writable NEF destination stream is required.", -1);
        }
        byte[] bytes = EncodeNef(input, options);
        options.CancellationToken.ThrowIfCancellationRequested();
        output.Write(bytes, 0, bytes.Length);
    }

    /// <summary>Builds a validated CR2 write plan without producing or committing the complete output file.</summary>
    /// <param name="input">Exact stored samples, previews, Canon blocks, and provenance metadata.</param>
    /// <param name="options">New-container write policy, profile opt-in, limits, and validation controls.</param>
    /// <returns>The exact predicted size, compatibility evidence, warnings, and ordered encoding actions.</returns>
    /// <exception cref="RawWriteException">The input, options, selected profile, or output layout is invalid.</exception>
    /// <exception cref="RawUnsupportedException">The requested write profile is unavailable or requires explicit opt-in.</exception>
    public static RawWritePlan PlanCr2(RawCr2WriteInput input, RawWriteOptions options)
    {
        if (options == null)
        {
            throw new RawWriteException(RawDiagnosticCode.InvalidArgument, "Valid new-container write options are required for CR2 planning.", -1);
        }
        RawProfileCatalogue catalogue = RawProfileCatalogue.CreateBuiltIn(options.Limits);
        RawWriteProfile profile = SelectCr2Profile(input, options, catalogue);
        RawWritePlan plan = Cr2Encoder.Plan(input, profile, options);
        return RawWritePlanner.CompleteNewPlan(plan, profile, catalogue, options);
    }

    /// <summary>Builds a validated CR2 write plan through an evidence-qualified caller-supplied catalogue.</summary>
    /// <param name="input">Exact stored samples, previews, Canon blocks, and provenance metadata.</param>
    /// <param name="catalogue">The validated catalogue containing the selected read and write profiles.</param>
    /// <param name="options">New-container write policy, profile selection, limits, and validation controls.</param>
    /// <returns>The exact predicted size, compatibility evidence, warnings, and ordered encoding actions.</returns>
    /// <exception cref="RawWriteException">The input, options, selected profile, or output layout is invalid.</exception>
    /// <exception cref="RawUnsupportedException">The requested profile lacks independent structural and application evidence.</exception>
    public static RawWritePlan PlanCr2(RawCr2WriteInput input, RawProfileCatalogue catalogue, RawWriteOptions options)
    {
        return PlanCr2(input, (IRawProfileCatalogue)catalogue, options);
    }

    /// <summary>Builds a validated CR2 write plan through an evidence-qualified substitutable profile source.</summary>
    /// <param name="input">Exact stored samples, previews, Canon blocks, and provenance metadata.</param>
    /// <param name="catalogue">The read-only profile source used for selection and evidence checks.</param>
    /// <param name="options">New-container write policy, profile selection, limits, and validation controls.</param>
    /// <returns>The exact predicted size, compatibility evidence, warnings, and ordered encoding actions.</returns>
    /// <exception cref="RawWriteException">The input, options, selected profile, or output layout is invalid.</exception>
    /// <exception cref="RawUnsupportedException">The requested profile lacks independent structural and application evidence.</exception>
    public static RawWritePlan PlanCr2(RawCr2WriteInput input, IRawProfileCatalogue catalogue, RawWriteOptions options)
    {
        RawWriteProfile profile = SelectCr2Profile(input, options, catalogue);
        RawWritePlan plan = Cr2Encoder.Plan(input, profile, options);
        return RawWritePlanner.CompleteNewPlan(plan, profile, catalogue, options);
    }

    /// <summary>Builds a preserving CR2 rewrite plan without producing or committing output bytes.</summary>
    /// <param name="document">A fully opened source-retaining CR2 with any staged metadata or preview edits.</param>
    /// <param name="options">Preserving rewrite policy, limits, cancellation, and verification controls.</param>
    /// <returns>The exact predicted size and ordered changed, copied, relocated, and dropped actions.</returns>
    /// <exception cref="RawWriteException">The document, options, source mappings, or layout is invalid.</exception>
    /// <exception cref="RawUnsupportedException">A staged representation or maker-note layout cannot be rewritten conservatively.</exception>
    public static RawWritePlan PlanRewriteCr2(RawDocument document, RawWriteOptions options)
    {
        return RawWritePlanner.PlanCr2Rewrite(document, options);
    }

    /// <summary>Builds a preserving NEF rewrite plan without producing or committing output bytes.</summary>
    /// <param name="document">A fully opened source-retaining NEF with any staged metadata or preview edits.</param>
    /// <param name="options">Preserving rewrite policy, limits, cancellation, and verification controls.</param>
    /// <returns>The exact predicted size and ordered changed, copied, relocated, and dropped actions.</returns>
    /// <exception cref="RawWriteException">The document, options, source mappings, or layout is invalid.</exception>
    /// <exception cref="RawUnsupportedException">A staged representation or maker-note layout cannot be rewritten conservatively.</exception>
    public static RawWritePlan PlanRewriteNef(RawDocument document, RawWriteOptions options)
    {
        return RawWritePlanner.PlanNefRewrite(document, options);
    }

    /// <summary>Builds a preserving rewrite plan by dispatching from an opened CR2 or NEF document format.</summary>
    /// <param name="document">A fully opened source-retaining CR2 or NEF with any staged edits.</param>
    /// <param name="options">Preserving rewrite policy, limits, cancellation, and verification controls.</param>
    /// <returns>The exact predicted size and ordered block actions.</returns>
    /// <exception cref="RawWriteException">The document is absent or is not a supported preserving-rewrite format.</exception>
    /// <exception cref="RawUnsupportedException">A staged representation or maker-note layout cannot be rewritten conservatively.</exception>
    public static RawWritePlan PlanRewrite(RawDocument document, RawWriteOptions options)
    {
        if (document == null)
        {
            throw new RawWriteException(RawDiagnosticCode.InvalidArgument, "A source-retaining CR2 or NEF document is required for rewrite planning.", -1);
        }
        if (document.Format == RawFormat.Cr2)
        {
            return PlanRewriteCr2(document, options);
        }
        if (document.Format == RawFormat.Nef)
        {
            return PlanRewriteNef(document, options);
        }
        throw new RawWriteException(RawDiagnosticCode.UnsupportedFormat, "Dry-run preserving rewrite planning supports CR2 and NEF documents.", -1);
    }

    /// <summary>Builds a validated NEF write plan without committing bytes to a caller-owned destination.</summary>
    /// <param name="input">Exact stored samples, preview, and reviewed Nikon provenance metadata.</param>
    /// <param name="options">New-container write policy, profile opt-in, limits, and validation controls.</param>
    /// <returns>The exact predicted size and encoding actions, including irreversible quantization details.</returns>
    /// <exception cref="RawWriteException">The input, options, selected profile, output structure, or validation fails.</exception>
    /// <exception cref="RawUnsupportedException">The requested write profile is unavailable or requires explicit opt-in.</exception>
    public static RawWritePlan PlanNef(RawNefWriteInput input, RawWriteOptions options)
    {
        if (options == null)
        {
            throw new RawWriteException(RawDiagnosticCode.InvalidArgument, "Valid new-container write options are required for NEF planning.", -1);
        }
        RawProfileCatalogue catalogue = RawProfileCatalogue.CreateBuiltIn(options.Limits);
        RawWriteOptions? planning = RawWritePlanner.CreatePlanningOptions(options);
        RawWriteProfile profile = SelectNefProfile(input, planning, catalogue);
        RawWritePlan plan = NefEncoder.Plan(input, profile, planning);
        return RawWritePlanner.CompleteNewPlan(plan, profile, catalogue, options);
    }

    /// <summary>Builds a validated NEF write plan through an evidence-qualified caller-supplied catalogue.</summary>
    /// <param name="input">Exact stored samples, preview, and reviewed Nikon provenance metadata.</param>
    /// <param name="catalogue">The validated catalogue containing the selected read and write profiles.</param>
    /// <param name="options">New-container write policy, profile selection, limits, and validation controls.</param>
    /// <returns>The exact predicted size and encoding actions, including irreversible quantization details.</returns>
    /// <exception cref="RawWriteException">The input, options, selected profile, output structure, or validation fails.</exception>
    /// <exception cref="RawUnsupportedException">The requested profile lacks independent structural and application evidence.</exception>
    public static RawWritePlan PlanNef(RawNefWriteInput input, RawProfileCatalogue catalogue, RawWriteOptions options)
    {
        return PlanNef(input, (IRawProfileCatalogue)catalogue, options);
    }

    /// <summary>Builds a validated NEF write plan through an evidence-qualified substitutable profile source.</summary>
    /// <param name="input">Exact stored samples, preview, and reviewed Nikon provenance metadata.</param>
    /// <param name="catalogue">The read-only profile source used for selection and evidence checks.</param>
    /// <param name="options">New-container write policy, profile selection, limits, and validation controls.</param>
    /// <returns>The exact predicted size and encoding actions, including irreversible quantization details.</returns>
    /// <exception cref="RawWriteException">The input, options, selected profile, output structure, or validation fails.</exception>
    /// <exception cref="RawUnsupportedException">The requested profile lacks independent structural and application evidence.</exception>
    public static RawWritePlan PlanNef(RawNefWriteInput input, IRawProfileCatalogue catalogue, RawWriteOptions options)
    {
        RawWriteOptions? planning = RawWritePlanner.CreatePlanningOptions(options);
        RawWriteProfile profile = SelectNefProfile(input, planning, catalogue);
        RawWritePlan plan = NefEncoder.Plan(input, profile, planning);
        return RawWritePlanner.CompleteNewPlan(plan, profile, catalogue, options);
    }

    internal static RawWriteProfile SelectNefProfile(RawNefWriteInput input, RawWriteOptions? options, IRawProfileCatalogue catalogue)
    {
        if (options == null || !options.Validate().IsValid() || options.Mode != RawWriteMode.NewContainer)
        {
            throw new RawWriteException(RawDiagnosticCode.InvalidArgument, "Valid new-container write options are required for NEF encoding.", -1);
        }
        options.CancellationToken.ThrowIfCancellationRequested();
        if (catalogue == null)
        {
            throw new RawWriteException(RawDiagnosticCode.InvalidArgument, "A validated NEF profile catalogue is required.", -1);
        }
        RawWriteProfile? profile = null;
        if (options.ProfileName != null && options.ProfileName.Length != 0)
        {
            profile = catalogue.FindWriteProfile(options.ProfileName);
        }
        else if (input != null)
        {
            RawWriteProfile[] profiles = catalogue.GetWriteProfiles();
            if (profiles == null)
            {
                throw new RawWriteException(RawDiagnosticCode.InvalidProfile, "The configured NEF profile source returned no write-profile snapshot.", -1);
            }
            int index = 0;
            while (index < profiles.Length)
            {
                RawWriteProfile candidate = profiles[index];
                if (NefEncoder.IsSupportedProfile(candidate) && candidate.Geometry.StoredWidth == input.Width && candidate.Geometry.StoredHeight == input.Height)
                {
                    if (profile != null)
                    {
                        throw new RawUnsupportedException(RawDiagnosticCode.AmbiguousProfile, "More than one supported NEF write profile matches the supplied geometry.", -1);
                    }
                    profile = candidate;
                }
                index++;
            }
        }
        if (!NefEncoder.IsSupportedProfile(profile))
        {
            throw new RawUnsupportedException(RawDiagnosticCode.UnsupportedProfile, "No conservative NEF write profile matches the request.", -1);
        }
        if (NefEncoder.IsLossyProfile(profile) && !options.AllowLossy)
        {
            throw new RawUnsupportedException(RawDiagnosticCode.UnsupportedProfile, "The selected lossy NEF profile requires explicit AllowLossy opt-in.", -1);
        }
        if ((profile ?? throw new System.InvalidOperationException("A required value is unavailable.")).Experimental && !options.AllowExperimentalProfiles)
        {
            throw new RawUnsupportedException(RawDiagnosticCode.UnsupportedProfile, "The NEF write profile is experimental and requires explicit opt-in.", -1);
        }
        NefWriteProfileQualification.Require(profile, catalogue);
        return profile;
    }

    internal static RawWriteProfile SelectCr2Profile(RawCr2WriteInput input, RawWriteOptions options, IRawProfileCatalogue catalogue)
    {
        if (options == null || !options.Validate().IsValid() || options.Mode != RawWriteMode.NewContainer)
        {
            throw new RawWriteException(RawDiagnosticCode.InvalidArgument, "Valid new-container write options are required for CR2 encoding.", -1);
        }
        options.CancellationToken.ThrowIfCancellationRequested();
        if (catalogue == null)
        {
            throw new RawWriteException(RawDiagnosticCode.InvalidArgument, "A validated CR2 profile catalogue is required.", -1);
        }
        RawWriteProfile? profile = null;
        if (options.ProfileName != null && options.ProfileName.Length != 0)
        {
            profile = catalogue.FindWriteProfile(options.ProfileName);
        }
        else if (input != null)
        {
            RawWriteProfile[] profiles = catalogue.GetWriteProfiles();
            if (profiles == null)
            {
                throw new RawWriteException(RawDiagnosticCode.InvalidProfile, "The configured CR2 profile source returned no write-profile snapshot.", -1);
            }
            int index = 0;
            while (index < profiles.Length)
            {
                RawWriteProfile candidate = profiles[index];
                if (candidate.Identity.Format == RawFormat.Cr2 && candidate.Geometry.StoredWidth == input.Width && candidate.Geometry.StoredHeight == input.Height)
                {
                    if (profile != null)
                    {
                        throw new RawUnsupportedException(RawDiagnosticCode.AmbiguousProfile, "More than one CR2 write profile matches the supplied geometry.", -1);
                    }
                    profile = candidate;
                }
                index++;
            }
        }
        if (profile == null || profile.Identity.Format != RawFormat.Cr2)
        {
            throw new RawUnsupportedException(RawDiagnosticCode.UnsupportedProfile, "No conservative CR2 write profile matches the request.", -1);
        }
        if (profile.Experimental && !options.AllowExperimentalProfiles)
        {
            throw new RawUnsupportedException(RawDiagnosticCode.UnsupportedProfile, "The CR2 write profile is experimental and requires explicit opt-in.", -1);
        }
        Cr2WriteProfileQualification.Require(profile, catalogue);
        return profile;
    }
}

internal static class Cr2WriteProfileQualification
{
    internal static void Require(RawWriteProfile profile, IRawProfileCatalogue catalogue)
    {
        if (profile.Id == RawBuiltInProfileCatalogue.Eos6DMarkIiWriteProfile)
        {
            return;
        }
        if (profile.ReadProfileId == null || catalogue.FindReadProfile(profile.ReadProfileId) == null)
        {
            throw Unsupported("An additional CR2 write profile must reference a registered read profile.");
        }
        RawProfileEvidence? structural = null;
        RawProfileEvidence? application = null;
        int index = 0;
        while (profile.Evidence != null && index < profile.Evidence.Length)
        {
            RawProfileEvidence evidence = profile.Evidence[index];
            if (evidence != null && evidence.Result == RawProfileEvidenceResult.Passed)
            {
                if (evidence.Kind == RawProfileEvidenceKind.Structural && structural == null)
                {
                    structural = evidence;
                }
                else if (evidence.Kind == RawProfileEvidenceKind.Application && application == null)
                {
                    application = evidence;
                }
            }
            index++;
        }
        if (structural == null || application == null || structural.Tool == null || application.Tool == null || application.Version == null || application.Version.Length == 0 || RawProfileMatcher.NormalizeIdentityText(structural.Tool) == RawProfileMatcher.NormalizeIdentityText(application.Tool))
        {
            throw Unsupported("An additional CR2 write profile requires passed structural and application checks from independently named tools.");
        }
        if (profile.Fingerprint == null || profile.Fingerprint.MakerNoteSignature != "Canon" || !Contains(profile.Fingerprint.RequiredIfds, "IFD0") || !Contains(profile.Fingerprint.RequiredIfds, "IFD1") || !Contains(profile.Fingerprint.RequiredIfds, "IFD2") || !Contains(profile.Fingerprint.RequiredIfds, "IFD3") || !Contains(profile.Fingerprint.RequiredIfds, "IFD0/Exif") || !Contains(profile.Fingerprint.RequiredTags, "IFD3/50752") || !Contains(profile.Fingerprint.RequiredTags, "IFD0/Exif/37500"))
        {
            throw Unsupported("An additional CR2 write profile must retain the reviewed container, slice, preview, and Canon maker-note requirements.");
        }
    }

    internal static bool Contains(string[] values, string expected)
    {
        if (values == null) return false;
        int index = 0;
        while (index < values.Length)
        {
            if (values[index] == expected) return true;
            index++;
        }
        return false;
    }

    internal static RawUnsupportedException Unsupported(string message)
    {
        return new RawUnsupportedException(RawDiagnosticCode.UnsupportedProfile, message, -1);
    }
}

internal static class Cr2Encoder
{
    internal static byte[] Encode(RawCr2WriteInput input, RawWriteProfile profile, RawWriteOptions options)
    {
        Validate(input, profile, options);
        byte[] raw = EncodeRaw(input, profile, options);
        byte[] makerNote = CreateCanonMakerNote(input, profile, 0);
        TiffLayoutRequest request = CreateRequest(input, profile, raw, makerNote);
        TiffLayoutPlan plan = TiffLayoutPlanner.Plan(request, options.Limits, options.CancellationToken);
        TiffLayoutIfdPlan rawIfd = TiffLayoutPlanner.FindIfd(plan, "IFD3");
        EndianWriter.WriteUInt32(plan.HeaderExtraBytes.AsSpan(4, 4), (uint)rawIfd.Offset, RawByteOrder.LittleEndian);
        TiffLayoutIfdPlan exif = TiffLayoutPlanner.FindIfd(plan, "IFD0/Exif");
        TiffLayoutEntryPlan makerEntry = FindEntry(exif, 37500);
        makerEntry.ValueBytes = CreateCanonMakerNote(input, profile, makerEntry.ValueOffset);
        byte[] output = TiffWriter.WriteToArray(plan, options.Limits, options.CancellationToken);
        ValidateOutput(output, input, profile, options);
        return output;
    }

    internal static RawWritePlan Plan(RawCr2WriteInput input, RawWriteProfile profile, RawWriteOptions options)
    {
        Validate(input, profile, options);
        byte[] raw = EncodeRaw(input, profile, options);
        byte[] makerNote = CreateCanonMakerNote(input, profile, 0);
        TiffLayoutRequest request = CreateRequest(input, profile, raw, makerNote);
        TiffLayoutPlan layout = TiffLayoutPlanner.Plan(request, options.Limits, options.CancellationToken);
        TiffLayoutIfdPlan rawIfd = TiffLayoutPlanner.FindIfd(layout, "IFD3");
        EndianWriter.WriteUInt32(layout.HeaderExtraBytes.AsSpan(4, 4), (uint)rawIfd.Offset, RawByteOrder.LittleEndian);
        TiffLayoutIfdPlan exif = TiffLayoutPlanner.FindIfd(layout, "IFD0/Exif");
        TiffLayoutEntryPlan makerEntry = FindEntry(exif, 37500);
        makerEntry.ValueBytes = CreateCanonMakerNote(input, profile, makerEntry.ValueOffset);
        RawWriteChange[] changes = new RawWriteChange[(layout.Blocks ?? throw new System.InvalidOperationException("A required value is unavailable.")).Length + 1];
        changes[0] = new RawWriteChange(RawWriteChangeKind.Added, "TIFF/Metadata", "Create the profile-defined CR2 container, directories, Exif, and Canon maker-note metadata.", -1, 0, 0, layout.Blocks.Length == 0 ? layout.PredictedSize : (long)layout.Blocks[0].Offset, false);
        int index = 0;
        while (index < layout.Blocks.Length)
        {
            TiffLayoutBlockPlan block = layout.Blocks[index];
            bool rawBlock = block.Name == "raw-ifd3";
            RawWriteChangeKind kind = rawBlock ? RawWriteChangeKind.Reencoded : RawWriteChangeKind.Added;
            string location = rawBlock ? "IFD3/Strip0" : block.Name == "preview-ifd0" ? "IFD0/Preview" : block.Name == "thumbnail-ifd1" ? "IFD1/Thumbnail" : "IFD2/Preview";
            string description = rawBlock ? "Encode exact stored samples with the selected lossless JPEG CR2 profile." : "Add the caller-supplied encoded preview payload.";
            changes[index + 1] = new RawWriteChange(kind, location, description, -1, 0, (long)block.Offset, block.Length, false);
            index++;
        }
        RawWritePlan result = new RawWritePlan(true, layout.PredictedSize, profile.Id, changes, null);
        long sampleCount = CheckedMath.Multiply(input.Width, input.Height);
        result.PixelValidation = new RawPixelValidationReport(RawPixelValidationKind.Identity, options.VerifyPixelIdentity, false, false, sampleCount, -1, 0, "Reopen the lossless CR2 and require every decoded stored sample to equal its source sample.");
        return result;
    }

    internal static void Validate(RawCr2WriteInput input, RawWriteProfile profile, RawWriteOptions options)
    {
        if (input == null || profile == null || options == null || options.Limits == null)
        {
            throw new RawWriteException(RawDiagnosticCode.InvalidArgument, "A CR2 input, write profile, options, and limits are required.", -1);
        }
        if (profile.Identity == null || profile.Identity.Format != RawFormat.Cr2 || profile.Fingerprint == null || profile.Geometry == null || profile.Codec == null || profile.Geometry.Cfa == null || profile.Codec.Compression != 6 || profile.Codec.StoredWordBits < 2 || profile.Codec.StoredWordBits > 16 || profile.Codec.Predictor != 1 || profile.Codec.SliceWidths == null || profile.Codec.SliceWidths.Length == 0)
        {
            throw new RawWriteException(RawDiagnosticCode.InvalidProfile, "The selected CR2 write profile does not define classic Bayer SOF3 geometry and slices.", -1);
        }
        if (input.Width != profile.Geometry.StoredWidth || input.Height != profile.Geometry.StoredHeight || input.Width <= 0 || input.Height <= 0 || (input.Width & 1) != 0 || (input.Height & 1) != 0 || input.RowStrideSamples < input.Width || input.StoredSamples == null)
        {
            throw new RawWriteException(RawDiagnosticCode.InvalidLength, "CR2 samples must exactly match the selected even Bayer profile geometry and row stride.", -1);
        }
        long required = CheckedMath.Add(CheckedMath.Multiply(input.Height - 1, input.RowStrideSamples), input.Width);
        if (input.StoredSamples.LongLength < required)
        {
            throw new RawWriteException(RawDiagnosticCode.InvalidLength, "The CR2 stored-sample buffer is shorter than its declared geometry.", -1);
        }
        CheckedMath.PixelCount(input.Width, input.Height, options.Limits);
        CheckedMath.DecodedSize(CheckedMath.Multiply(input.Width, input.Height), 4, options.Limits);
        Cr2LosslessJpegDecoder.ValidateSlices(profile.Codec.SliceWidths, input.Width);
        ValidateSamples(input, profile.Codec.StoredWordBits, options.CancellationToken);
        ValidatePreview(input.PrimaryPreview, options, "IFD0 preview");
        ValidatePreview(input.Thumbnail, options, "IFD1 thumbnail");
        ValidatePreview(input.SecondaryPreview, options, "IFD2 preview");
        if (input.CanonCameraSettings == null || input.CanonCameraSettings.Length != 47 || input.CanonCameraSettings[3] != 4 || input.CanonCameraSettings[46] != 0)
        {
            throw new RawWriteException(RawDiagnosticCode.InvalidMetadata, "The reviewed classic Bayer profile requires exact caller-supplied 47-word Canon CameraSettings with RAW quality.", -1);
        }
        if (input.CanonColorData == null || input.CanonColorData.Length != 1602 || input.CanonColorData[0] != 15)
        {
            throw new RawWriteException(RawDiagnosticCode.InvalidMetadata, "The reviewed profile requires exact caller-supplied Canon ColorData version 15 with 1602 words.", -1);
        }
        if (input.CanonModelId == 0)
        {
            throw new RawWriteException(RawDiagnosticCode.InvalidMetadata, "The reviewed profile requires a caller-supplied Canon ModelID.", -1);
        }
        if (input.Metadata == null || !RequiredAscii(input.Metadata.Software) || !OptionalAscii(input.Metadata.Artist) || !OptionalAscii(input.Metadata.Copyright) || input.Metadata.OriginalTime == null || !input.Metadata.OriginalTime.Valid)
        {
            throw new RawWriteException(RawDiagnosticCode.InvalidMetadata, "Caller-supplied ASCII software, optional creator/rights text, and a valid original timestamp are required.", -1);
        }
    }

    internal static void ValidateSamples(RawCr2WriteInput input, int precision, System.Threading.CancellationToken cancellationToken)
    {
        int maximum = precision == 16 ? ushort.MaxValue : (1 << precision) - 1;
        int y = 0;
        while (y < input.Height)
        {
            cancellationToken.ThrowIfCancellationRequested();
            int row = y * input.RowStrideSamples;
            int x = 0;
            while (x < input.Width)
            {
                if (input.StoredSamples[row + x] > maximum)
                {
                    throw new RawWriteException(RawDiagnosticCode.InvalidArgument, "A CR2 stored sample exceeds the selected profile precision.", -1);
                }
                x++;
            }
            y++;
        }
    }

    internal static void ValidatePreview(RawThumbnail? preview, RawWriteOptions options, string name)
    {
        if (preview == null || preview.Kind != RawThumbnailKind.Jpeg || preview.EncodedBytes == null || preview.EncodedBytes.Length == 0 || !preview.Validate(options.Limits).IsValid())
        {
            throw new RawWriteException(RawDiagnosticCode.InvalidMetadata, "A valid baseline-JPEG " + name + " is required.", -1);
        }
        BaselineJpegImage decoded = BaselineJpegCodec.Decode(preview.EncodedBytes, options.Limits, options.CancellationToken);
        if (decoded.Width != preview.Width || decoded.Height != preview.Height)
        {
            throw new RawWriteException(RawDiagnosticCode.InvalidMetadata, "The declared " + name + " dimensions disagree with its JPEG frame.", -1);
        }
    }

    internal static byte[] EncodeRaw(RawCr2WriteInput input, RawWriteProfile profile, RawWriteOptions options)
    {
        int sampleCount = CheckedMath.ToInt32(CheckedMath.Multiply(input.Width, input.Height));
        CheckedMath.AllocationSize(sampleCount, 2, options.Limits);
        ushort[] sliced = new ushort[sampleCount];
        int destination = 0;
        int sliceX = 0;
        int sliceIndex = 0;
        while (sliceIndex < profile.Codec.SliceWidths.Length)
        {
            int sliceWidth = profile.Codec.SliceWidths[sliceIndex];
            int y = 0;
            while (y < input.Height)
            {
                options.CancellationToken.ThrowIfCancellationRequested();
                int source = y * input.RowStrideSamples + sliceX;
                System.Array.Copy(input.StoredSamples, source, sliced, destination, sliceWidth);
                destination += sliceWidth;
                y++;
            }
            sliceX += sliceWidth;
            sliceIndex++;
        }
        if (destination != sampleCount)
        {
            throw new RawWriteException(RawDiagnosticCode.InvalidLength, "CR2 slicing did not consume the exact stored sample geometry.", -1);
        }
        int codedWidth = input.Width / 2;
        int codedHeight = input.Height / 2;
        int planeCount = CheckedMath.ToInt32(CheckedMath.Multiply(codedWidth, codedHeight));
        LosslessJpegImage jpeg = new LosslessJpegImage();
        jpeg.Width = codedWidth;
        jpeg.Height = codedHeight;
        jpeg.Precision = profile.Codec.StoredWordBits;
        int component = 0;
        while (component < 4)
        {
            ushort[] samples = new ushort[planeCount];
            int index = 0;
            while (index < planeCount)
            {
                samples[index] = sliced[index * 4 + component];
                index++;
            }
            LosslessJpegPlane plane = new LosslessJpegPlane();
            plane.Identifier = (byte)(component + 1);
            plane.Width = codedWidth;
            plane.Height = codedHeight;
            plane.HorizontalSampling = 1;
            plane.VerticalSampling = 1;
            plane.Samples = samples;
            jpeg.Planes.Add(plane);
            component++;
        }
        LosslessJpegEncodeParameters parameters = new LosslessJpegEncodeParameters(1, 0, 0, true, true);
        return LosslessJpegEncoder.Encode(jpeg, parameters, options.Limits, options.CancellationToken);
    }

    internal static TiffLayoutRequest CreateRequest(RawCr2WriteInput input, RawWriteProfile profile, byte[] raw, byte[] makerNote)
    {
        RawByteOrder byteOrder = RawByteOrder.LittleEndian;
        System.Collections.Generic.List<TiffLayoutEntryInput> ifd0 = ImageEntries((input.PrimaryPreview ?? throw new System.InvalidOperationException("A required value is unavailable.")).Width, input.PrimaryPreview.Height, input.PrimaryPreview.EncodedBytes.Length, "preview-ifd0", byteOrder);
        ifd0.Add(AsciiEntry(271, profile.Identity.Make));
        ifd0.Add(AsciiEntry(272, profile.Identity.Model));
        ifd0.Add(AsciiEntry(305, input.Metadata.Software));
        if (input.Metadata.Artist.Length != 0) ifd0.Add(AsciiEntry(315, input.Metadata.Artist));
        if (input.Metadata.Copyright.Length != 0) ifd0.Add(AsciiEntry(33432, input.Metadata.Copyright));
        ifd0.Add(LongEntry(34665, 0, byteOrder, new TiffLayoutReference[] { new TiffLayoutReference(0, TiffLayoutReferenceKind.Ifd, "IFD0/Exif") }));

        System.Collections.Generic.List<TiffLayoutEntryInput> ifd1 = new System.Collections.Generic.List<TiffLayoutEntryInput>();
        ifd1.Add(LongEntry(256, (uint)(input.Thumbnail ?? throw new System.InvalidOperationException("A required value is unavailable.")).Width, byteOrder, null));
        ifd1.Add(LongEntry(257, (uint)input.Thumbnail.Height, byteOrder, null));
        ifd1.Add(ShortEntry(259, 6, byteOrder));
        ifd1.Add(ShortEntry(262, 2, byteOrder));
        ifd1.Add(LongEntry(513, 0, byteOrder, new TiffLayoutReference[] { new TiffLayoutReference(0, TiffLayoutReferenceKind.Block, "thumbnail-ifd1") }));
        ifd1.Add(LongEntry(514, (uint)input.Thumbnail.EncodedBytes.Length, byteOrder, null));

        System.Collections.Generic.List<TiffLayoutEntryInput> ifd2 = ImageEntries((input.SecondaryPreview ?? throw new System.InvalidOperationException("A required value is unavailable.")).Width, input.SecondaryPreview.Height, input.SecondaryPreview.EncodedBytes.Length, "preview-ifd2", byteOrder);
        System.Collections.Generic.List<TiffLayoutEntryInput> ifd3 = new System.Collections.Generic.List<TiffLayoutEntryInput>();
        ifd3.Add(LongEntry(256, (uint)input.Width, byteOrder, null));
        ifd3.Add(LongEntry(257, (uint)input.Height, byteOrder, null));
        ifd3.Add(ShortEntry(258, (ushort)profile.Codec.StoredWordBits, byteOrder));
        ifd3.Add(ShortEntry(259, 6, byteOrder));
        ifd3.Add(ShortEntry(262, 32803, byteOrder));
        ifd3.Add(LongEntry(273, 0, byteOrder, new TiffLayoutReference[] { new TiffLayoutReference(0, TiffLayoutReferenceKind.Block, "raw-ifd3") }));
        ifd3.Add(ShortEntry(277, 1, byteOrder));
        ifd3.Add(LongEntry(278, (uint)input.Height, byteOrder, null));
        ifd3.Add(LongEntry(279, (uint)raw.Length, byteOrder, null));
        ifd3.Add(ShortArrayEntry(50752, SliceTag(profile.Codec.SliceWidths), byteOrder));

        System.Collections.Generic.List<TiffLayoutEntryInput> exif = new System.Collections.Generic.List<TiffLayoutEntryInput>();
        exif.Add(UndefinedEntry(36864, new byte[] { 0x30, 0x32, 0x33, 0x31 }));
        exif.Add(AsciiEntry(36867, (input.Metadata.OriginalTime ?? throw new System.InvalidOperationException("A required value is unavailable.")).DateTimeText));
        if (input.Metadata.OriginalTime.HasOffset) exif.Add(AsciiEntry(36881, input.Metadata.OriginalTime.OffsetText));
        if (input.Metadata.OriginalTime.Subseconds.Length != 0) exif.Add(AsciiEntry(37521, input.Metadata.OriginalTime.Subseconds));
        exif.Add(UndefinedEntry(37500, makerNote));
        exif.Add(LongEntry(40962, (uint)(profile.Geometry.ActiveArea ?? throw new System.InvalidOperationException("A required value is unavailable.")).Width, byteOrder, null));
        exif.Add(LongEntry(40963, (uint)profile.Geometry.ActiveArea.Height, byteOrder, null));

        byte[] cr2Header = new byte[] { 0x43, 0x52, 2, 0, 0, 0, 0, 0 };
        return TiffLayoutRequest.CreateWithHeaderExtraBytes(false, byteOrder, cr2Header, "IFD0", new TiffLayoutIfdInput[]
        {
            new TiffLayoutIfdInput("IFD0", ifd0.ToArray(), "IFD1"),
            new TiffLayoutIfdInput("IFD1", ifd1.ToArray(), "IFD2"),
            new TiffLayoutIfdInput("IFD2", ifd2.ToArray(), "IFD3"),
            new TiffLayoutIfdInput("IFD3", ifd3.ToArray(), null),
            new TiffLayoutIfdInput("IFD0/Exif", exif.ToArray(), null)
        }, new TiffLayoutBlockInput[]
        {
            new TiffLayoutBlockInput("preview-ifd0", TiffLayoutBlockKind.Strip, input.PrimaryPreview.EncodedBytes.Length, input.PrimaryPreview.EncodedBytes),
            new TiffLayoutBlockInput("thumbnail-ifd1", TiffLayoutBlockKind.Opaque, input.Thumbnail.EncodedBytes.Length, input.Thumbnail.EncodedBytes),
            new TiffLayoutBlockInput("preview-ifd2", TiffLayoutBlockKind.Strip, input.SecondaryPreview.EncodedBytes.Length, input.SecondaryPreview.EncodedBytes),
            new TiffLayoutBlockInput("raw-ifd3", TiffLayoutBlockKind.Strip, raw.Length, raw)
        });
    }

    internal static System.Collections.Generic.List<TiffLayoutEntryInput> ImageEntries(int width, int height, int length, string block, RawByteOrder byteOrder)
    {
        System.Collections.Generic.List<TiffLayoutEntryInput> entries = new System.Collections.Generic.List<TiffLayoutEntryInput>();
        entries.Add(LongEntry(256, (uint)width, byteOrder, null));
        entries.Add(LongEntry(257, (uint)height, byteOrder, null));
        entries.Add(ShortArrayEntry(258, new ushort[] { 8, 8, 8 }, byteOrder));
        entries.Add(ShortEntry(259, 6, byteOrder));
        entries.Add(ShortEntry(262, 2, byteOrder));
        entries.Add(LongEntry(273, 0, byteOrder, new TiffLayoutReference[] { new TiffLayoutReference(0, TiffLayoutReferenceKind.Block, block) }));
        entries.Add(ShortEntry(277, 3, byteOrder));
        entries.Add(LongEntry(278, (uint)height, byteOrder, null));
        entries.Add(LongEntry(279, (uint)length, byteOrder, null));
        return entries;
    }

    internal static ushort[] SliceTag(int[] widths)
    {
        if (widths.Length == 1)
        {
            return new ushort[] { 0, 0, (ushort)widths[0] };
        }
        ushort[] result = new ushort[3];
        result[0] = (ushort)(widths.Length - 1);
        int index = 0;
        while (index < widths.Length - 1)
        {
            if (widths[index] != widths[0])
            {
                throw new RawWriteException(RawDiagnosticCode.InvalidProfile, "CR2 regular slices must have one repeated width.", -1);
            }
            index++;
        }
        if (widths.Length - 1 > ushort.MaxValue || widths[0] > ushort.MaxValue || widths[widths.Length - 1] > ushort.MaxValue)
        {
            throw new RawWriteException(RawDiagnosticCode.ArithmeticOverflow, "A CR2 slice count or width exceeds the SHORT representation.", -1);
        }
        result[1] = (ushort)widths[0];
        result[2] = (ushort)widths[widths.Length - 1];
        return result;
    }

    internal static byte[] CreateCanonMakerNote(RawCr2WriteInput input, RawWriteProfile profile, ulong makerOffset)
    {
        short[] sensor = new short[13];
        sensor[1] = (short)input.Width;
        sensor[2] = (short)input.Height;
        sensor[5] = (short)(profile.Geometry.ActiveArea ?? throw new System.InvalidOperationException("A required value is unavailable.")).X;
        sensor[6] = (short)profile.Geometry.ActiveArea.Y;
        sensor[7] = (short)(profile.Geometry.ActiveArea.X + profile.Geometry.ActiveArea.Width - 1);
        sensor[8] = (short)(profile.Geometry.ActiveArea.Y + profile.Geometry.ActiveArea.Height - 1);
        sensor[9] = -1;
        sensor[10] = -1;
        sensor[11] = -1;
        sensor[12] = -1;
        byte[][] values = new byte[][]
        {
            SignedShortBytes(input.CanonCameraSettings, RawByteOrder.LittleEndian),
            AsciiBytes(profile.Identity.FirmwareMinimum),
            UInt32Bytes(input.CanonModelId, RawByteOrder.LittleEndian),
            SignedShortBytes(sensor, RawByteOrder.LittleEndian),
            UInt16Bytes(input.CanonColorData, RawByteOrder.LittleEndian)
        };
        ushort[] ids = new ushort[] { 0x0001, 0x0007, 0x0010, 0x00e0, 0x4001 };
        TiffFieldType[] types = new TiffFieldType[] { TiffFieldType.SignedShort, TiffFieldType.Ascii, TiffFieldType.Long, TiffFieldType.SignedShort, TiffFieldType.Short };
        uint[] counts = new uint[] { 47, (uint)values[1].Length, 1, 13, 1602 };
        int headerLength = 2 + ids.Length * 12 + 4;
        int length = headerLength;
        int sizeIndex = 0;
        while (sizeIndex < values.Length)
        {
            if (values[sizeIndex].Length > 4) length += values[sizeIndex].Length;
            sizeIndex++;
        }
        byte[] bytes = new byte[length];
        EndianWriter.WriteUInt16(bytes.AsSpan(0, 2), (ushort)ids.Length, RawByteOrder.LittleEndian);
        int position = headerLength;
        int index = 0;
        while (index < ids.Length)
        {
            int entry = 2 + index * 12;
            EndianWriter.WriteUInt16(bytes.AsSpan(entry, 2), ids[index], RawByteOrder.LittleEndian);
            EndianWriter.WriteUInt16(bytes.AsSpan(entry + 2, 2), (ushort)types[index], RawByteOrder.LittleEndian);
            EndianWriter.WriteUInt32(bytes.AsSpan(entry + 4, 4), counts[index], RawByteOrder.LittleEndian);
            if (values[index].Length <= 4)
            {
                values[index].AsSpan().CopyTo(bytes.AsSpan(entry + 8));
            }
            else
            {
                long absoluteValue = CheckedMath.Add((long)makerOffset, position);
                ulong absolute = (ulong)absoluteValue;
                if (absolute > uint.MaxValue) throw new RawWriteException(RawDiagnosticCode.ArithmeticOverflow, "A Canon maker-note value offset exceeds classic TIFF.", -1);
                EndianWriter.WriteUInt32(bytes.AsSpan(entry + 8, 4), (uint)absolute, RawByteOrder.LittleEndian);
                values[index].AsSpan().CopyTo(bytes.AsSpan(position));
                position += values[index].Length;
            }
            index++;
        }
        return bytes;
    }

    internal static RawPixelValidationReport ValidateOutput(byte[] output, RawCr2WriteInput input, RawWriteProfile profile, RawWriteOptions options)
    {
        RawOpenOptions openOptions = new RawOpenOptions();
        openOptions.AllowExperimentalProfiles = true;
        openOptions.ReadPreviews = true;
        RawDocument document = RawCodec.Open(output, null, openOptions);
        if (document.Format != RawFormat.Cr2 || document.FormatDetails == null || document.FormatDetails.Cr2Header == null || document.FormatDetails.Cr2Header.RawIfdPath != "IFD3" || document.Thumbnails.Length != 3)
        {
            throw new RawWriteException(RawDiagnosticCode.OutputValidationFailed, "The encoded CR2 does not retain its required header, raw IFD, and previews.", -1);
        }
        if (!options.VerifyPixelIdentity)
        {
            long skippedCount = CheckedMath.Multiply(input.Width, input.Height);
            return new RawPixelValidationReport(RawPixelValidationKind.Identity, false, false, false, skippedCount, -1, 0, "Decoded stored-pixel identity verification was not requested.");
        }
        RawImageInfo? rawInfo = null;
        int imageIndex = 0;
        while (imageIndex < (document.Images ?? throw new System.InvalidOperationException("A required value is unavailable.")).Length)
        {
            if (document.Images[imageIndex].Kind == RawImageKind.Raw)
            {
                rawInfo = document.Images[imageIndex];
                break;
            }
            imageIndex++;
        }
        if (rawInfo == null)
        {
            throw new RawWriteException(RawDiagnosticCode.OutputValidationFailed, "The encoded CR2 has no raw payload.", -1);
        }
        byte[] encoded = document.ExtractPayload(rawInfo.Payload);
        LosslessJpegImage jpeg = LosslessJpegCodec.Decode(encoded, options.Limits, options.CancellationToken);
        ushort[] decoded = Cr2LosslessJpegDecoder.DecodeSamples(jpeg, input.Width, input.Height, profile.Codec.SliceWidths, 4, profile.Codec.StoredWordBits, 1, options.Limits, options.CancellationToken);
        int y = 0;
        while (y < input.Height)
        {
            options.CancellationToken.ThrowIfCancellationRequested();
            int x = 0;
            while (x < input.Width)
            {
                if (decoded[y * input.Width + x] != input.StoredSamples[y * input.RowStrideSamples + x])
                {
                    throw new RawWriteException(RawDiagnosticCode.OutputValidationFailed, "The encoded CR2 failed stored-pixel identity verification.", -1);
                }
                x++;
            }
            y++;
        }
        long sampleCount = CheckedMath.Multiply(input.Width, input.Height);
        return new RawPixelValidationReport(RawPixelValidationKind.Identity, true, true, true, sampleCount, 0, 0, "The reopened lossless CR2 decoded to stored samples identical to the source.");
    }

    internal static TiffLayoutEntryPlan FindEntry(TiffLayoutIfdPlan ifd, ushort tag)
    {
        int index = 0;
        while (index < (ifd.Entries ?? throw new System.InvalidOperationException("A required value is unavailable.")).Length)
        {
            if (ifd.Entries[index].Tag == tag) return ifd.Entries[index];
            index++;
        }
        throw new RawWriteException(RawDiagnosticCode.OutputValidationFailed, "A required planned TIFF entry is missing.", -1);
    }

    internal static TiffLayoutEntryInput LongEntry(ushort tag, uint value, RawByteOrder byteOrder, TiffLayoutReference[]? references)
    {
        return new TiffLayoutEntryInput(tag, TiffFieldType.Long, 1, UInt32Bytes(value, byteOrder), references);
    }

    internal static TiffLayoutEntryInput ShortEntry(ushort tag, ushort value, RawByteOrder byteOrder)
    {
        return ShortArrayEntry(tag, new ushort[] { value }, byteOrder);
    }

    internal static TiffLayoutEntryInput ShortArrayEntry(ushort tag, ushort[] values, RawByteOrder byteOrder)
    {
        return new TiffLayoutEntryInput(tag, TiffFieldType.Short, (ulong)values.Length, UInt16Bytes(values, byteOrder), null);
    }

    internal static TiffLayoutEntryInput AsciiEntry(ushort tag, string? value)
    {
        byte[] bytes = AsciiBytes((value ?? throw new System.InvalidOperationException("A required value is unavailable.")));
        return new TiffLayoutEntryInput(tag, TiffFieldType.Ascii, (ulong)bytes.Length, bytes, null);
    }

    internal static TiffLayoutEntryInput UndefinedEntry(ushort tag, byte[] value)
    {
        return new TiffLayoutEntryInput(tag, TiffFieldType.Undefined, (ulong)value.Length, value, null);
    }

    internal static byte[] UInt32Bytes(uint value, RawByteOrder byteOrder)
    {
        byte[] bytes = new byte[4];
        EndianWriter.WriteUInt32(bytes, value, byteOrder);
        return bytes;
    }

    internal static byte[] UInt16Bytes(ushort[] values, RawByteOrder byteOrder)
    {
        byte[] bytes = new byte[values.Length * 2];
        int index = 0;
        while (index < values.Length)
        {
            EndianWriter.WriteUInt16(bytes.AsSpan(index * 2, 2), values[index], byteOrder);
            index++;
        }
        return bytes;
    }

    internal static byte[] SignedShortBytes(short[] values, RawByteOrder byteOrder)
    {
        byte[] bytes = new byte[values.Length * 2];
        int index = 0;
        while (index < values.Length)
        {
            EndianWriter.WriteInt16(bytes.AsSpan(index * 2, 2), values[index], byteOrder);
            index++;
        }
        return bytes;
    }

    internal static byte[] AsciiBytes(string value)
    {
        byte[] text = System.Text.Encoding.ASCII.GetBytes(value);
        byte[] bytes = new byte[text.Length + 1];
        text.AsSpan().CopyTo(bytes);
        return bytes;
    }

    internal static bool RequiredAscii(string value)
    {
        return value != null && value.Length != 0 && OptionalAscii(value);
    }

    internal static bool OptionalAscii(string value)
    {
        if (value == null) return false;
        int index = 0;
        while (index < value.Length)
        {
            if (value[index] == 0 || value[index] > 127) return false;
            index++;
        }
        return true;
    }
}
