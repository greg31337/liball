namespace raw;

/// <summary>Defines canonical generic RAW, CR2, and NEF write operations.</summary>
public interface IRawWriter
{
    /// <summary>Rewrites an opened CR2 through a validated sibling temporary file before atomic destination commit.</summary>
    /// <param name="destinationPath">The destination file path.</param>
    /// <param name="document">The fully opened CR2 document.</param>
    /// <param name="options">The rewrite and validation controls.</param>
    /// <returns>The committed destination or recoverable temporary-file result.</returns>
    RawPathWriteResult RewriteCr2ToPath(string destinationPath, RawDocument document, RawWriteOptions options);

    /// <summary>Rewrites an opened NEF through a validated sibling temporary file before atomic destination commit.</summary>
    /// <param name="destinationPath">The destination file path.</param>
    /// <param name="document">The fully opened NEF document.</param>
    /// <param name="options">The rewrite and validation controls.</param>
    /// <returns>The committed destination or recoverable temporary-file result.</returns>
    RawPathWriteResult RewriteNefToPath(string destinationPath, RawDocument document, RawWriteOptions options);

    /// <summary>Rewrites an opened CR2 to a bounded caller-owned stream without closing it.</summary>
    /// <param name="destination">The writable caller-owned stream.</param>
    /// <param name="document">The fully opened CR2 document.</param>
    /// <param name="options">The rewrite and validation controls.</param>
    void RewriteCr2ToStream(System.IO.Stream destination, RawDocument document, RawWriteOptions options);

    /// <summary>Rewrites an opened NEF to a bounded caller-owned stream without closing it.</summary>
    /// <param name="destination">The writable caller-owned stream.</param>
    /// <param name="document">The fully opened NEF document.</param>
    /// <param name="options">The rewrite and validation controls.</param>
    void RewriteNefToStream(System.IO.Stream destination, RawDocument document, RawWriteOptions options);

    /// <summary>Rewrites an opened CR2 while preserving supported opaque content.</summary>
    /// <param name="document">The fully opened CR2 document.</param>
    /// <param name="options">The rewrite and validation controls.</param>
    /// <returns>The complete rewritten CR2 bytes.</returns>
    byte[] RewriteCr2(RawDocument document, RawWriteOptions options);

    /// <summary>Rewrites an opened NEF while preserving supported opaque content.</summary>
    /// <param name="document">The fully opened NEF document.</param>
    /// <param name="options">The rewrite and validation controls.</param>
    /// <returns>The complete rewritten NEF bytes.</returns>
    byte[] RewriteNef(RawDocument document, RawWriteOptions options);

    /// <summary>Encodes one profile-gated CR2 in memory.</summary>
    /// <param name="input">The exact CR2 samples, previews, and metadata.</param>
    /// <param name="options">The profile and validation controls.</param>
    /// <returns>The complete CR2 bytes.</returns>
    byte[] EncodeCr2(RawCr2WriteInput input, RawWriteOptions options);

    /// <summary>Encodes one profile-gated NEF in memory.</summary>
    /// <param name="input">The exact NEF samples, preview, and metadata.</param>
    /// <param name="options">The profile and validation controls.</param>
    /// <returns>The complete NEF bytes.</returns>
    byte[] EncodeNef(RawNefWriteInput input, RawWriteOptions options);

    /// <summary>Encodes one CR2 using a caller-supplied profile source.</summary>
    /// <param name="input">The exact CR2 samples, previews, and metadata.</param>
    /// <param name="catalogue">The profile source used for selection and evidence checks.</param>
    /// <param name="options">The profile and validation controls.</param>
    /// <returns>The complete CR2 bytes.</returns>
    byte[] EncodeCr2WithProfiles(RawCr2WriteInput input, IRawProfileCatalogue catalogue, RawWriteOptions options);

    /// <summary>Encodes one NEF using a caller-supplied profile source.</summary>
    /// <param name="input">The exact NEF samples, preview, and metadata.</param>
    /// <param name="catalogue">The profile source used for selection and evidence checks.</param>
    /// <param name="options">The profile and validation controls.</param>
    /// <returns>The complete NEF bytes.</returns>
    byte[] EncodeNefWithProfiles(RawNefWriteInput input, IRawProfileCatalogue catalogue, RawWriteOptions options);

    /// <summary>Encodes one profile-gated CR2 to a bounded caller-owned stream without closing it.</summary>
    /// <param name="destination">The writable caller-owned stream.</param>
    /// <param name="input">The exact CR2 samples, previews, and metadata.</param>
    /// <param name="options">The profile and validation controls.</param>
    void EncodeCr2ToStream(System.IO.Stream destination, RawCr2WriteInput input, RawWriteOptions options);

    /// <summary>Encodes one profile-gated NEF to a bounded caller-owned stream without closing it.</summary>
    /// <param name="destination">The writable caller-owned stream.</param>
    /// <param name="input">The exact NEF samples, preview, and metadata.</param>
    /// <param name="options">The profile and validation controls.</param>
    void EncodeNefToStream(System.IO.Stream destination, RawNefWriteInput input, RawWriteOptions options);

    /// <summary>Encodes one CR2 to a bounded caller-owned stream using a caller-supplied profile source.</summary>
    /// <param name="destination">The writable caller-owned stream.</param>
    /// <param name="input">The exact CR2 samples, previews, and metadata.</param>
    /// <param name="catalogue">The profile source used for selection and evidence checks.</param>
    /// <param name="options">The profile and validation controls.</param>
    void EncodeCr2WithProfilesToStream(System.IO.Stream destination, RawCr2WriteInput input, IRawProfileCatalogue catalogue, RawWriteOptions options);

    /// <summary>Encodes one NEF to a bounded caller-owned stream using a caller-supplied profile source.</summary>
    /// <param name="destination">The writable caller-owned stream.</param>
    /// <param name="input">The exact NEF samples, preview, and metadata.</param>
    /// <param name="catalogue">The profile source used for selection and evidence checks.</param>
    /// <param name="options">The profile and validation controls.</param>
    void EncodeNefWithProfilesToStream(System.IO.Stream destination, RawNefWriteInput input, IRawProfileCatalogue catalogue, RawWriteOptions options);

    /// <summary>Encodes one profile-gated CR2 through a validated sibling temporary file before atomic destination commit.</summary>
    /// <param name="destinationPath">The destination file path.</param>
    /// <param name="input">The exact CR2 samples, previews, and metadata.</param>
    /// <param name="options">The profile and validation controls.</param>
    /// <returns>The committed destination or recoverable temporary-file result.</returns>
    RawPathWriteResult EncodeCr2ToPath(string destinationPath, RawCr2WriteInput input, RawWriteOptions options);

    /// <summary>Encodes one profile-gated NEF through a validated sibling temporary file before atomic destination commit.</summary>
    /// <param name="destinationPath">The destination file path.</param>
    /// <param name="input">The exact NEF samples, preview, and metadata.</param>
    /// <param name="options">The profile and validation controls.</param>
    /// <returns>The committed destination or recoverable temporary-file result.</returns>
    RawPathWriteResult EncodeNefToPath(string destinationPath, RawNefWriteInput input, RawWriteOptions options);

    /// <summary>Encodes one CR2 through a validated sibling temporary file using a caller-supplied profile source.</summary>
    /// <param name="destinationPath">The destination file path.</param>
    /// <param name="input">The exact CR2 samples, previews, and metadata.</param>
    /// <param name="catalogue">The profile source used for selection and evidence checks.</param>
    /// <param name="options">The profile and validation controls.</param>
    /// <returns>The committed destination or recoverable temporary-file result.</returns>
    RawPathWriteResult EncodeCr2WithProfilesToPath(string destinationPath, RawCr2WriteInput input, IRawProfileCatalogue catalogue, RawWriteOptions options);

    /// <summary>Encodes one NEF through a validated sibling temporary file using a caller-supplied profile source.</summary>
    /// <param name="destinationPath">The destination file path.</param>
    /// <param name="input">The exact NEF samples, preview, and metadata.</param>
    /// <param name="catalogue">The profile source used for selection and evidence checks.</param>
    /// <param name="options">The profile and validation controls.</param>
    /// <returns>The committed destination or recoverable temporary-file result.</returns>
    RawPathWriteResult EncodeNefWithProfilesToPath(string destinationPath, RawNefWriteInput input, IRawProfileCatalogue catalogue, RawWriteOptions options);

    /// <summary>Plans one profile-gated CR2 encoding operation.</summary>
    /// <param name="input">The exact CR2 samples, previews, and metadata.</param>
    /// <param name="options">The profile and validation controls.</param>
    /// <returns>The complete dry-run plan.</returns>
    RawWritePlan PlanCr2(RawCr2WriteInput input, RawWriteOptions options);

    /// <summary>Plans one profile-gated NEF encoding operation.</summary>
    /// <param name="input">The exact NEF samples, preview, and metadata.</param>
    /// <param name="options">The profile and validation controls.</param>
    /// <returns>The complete dry-run plan.</returns>
    RawWritePlan PlanNef(RawNefWriteInput input, RawWriteOptions options);

    /// <summary>Plans one CR2 encoding operation using a caller-supplied profile source.</summary>
    /// <param name="input">The exact CR2 samples, previews, and metadata.</param>
    /// <param name="catalogue">The profile source used for selection and evidence checks.</param>
    /// <param name="options">The profile and validation controls.</param>
    /// <returns>The complete dry-run plan.</returns>
    RawWritePlan PlanCr2WithProfiles(RawCr2WriteInput input, IRawProfileCatalogue catalogue, RawWriteOptions options);

    /// <summary>Plans one NEF encoding operation using a caller-supplied profile source.</summary>
    /// <param name="input">The exact NEF samples, preview, and metadata.</param>
    /// <param name="catalogue">The profile source used for selection and evidence checks.</param>
    /// <param name="options">The profile and validation controls.</param>
    /// <returns>The complete dry-run plan.</returns>
    RawWritePlan PlanNefWithProfiles(RawNefWriteInput input, IRawProfileCatalogue catalogue, RawWriteOptions options);

    /// <summary>Plans one preserving CR2 rewrite.</summary>
    /// <param name="document">The fully opened CR2 document.</param>
    /// <param name="options">The rewrite and validation controls.</param>
    /// <returns>The complete dry-run plan.</returns>
    RawWritePlan PlanRewriteCr2(RawDocument document, RawWriteOptions options);

    /// <summary>Plans one preserving NEF rewrite.</summary>
    /// <param name="document">The fully opened NEF document.</param>
    /// <param name="options">The rewrite and validation controls.</param>
    /// <returns>The complete dry-run plan.</returns>
    RawWritePlan PlanRewriteNef(RawDocument document, RawWriteOptions options);

    /// <summary>Plans eligible same-size TIFF, CR2, or NEF metadata value replacements without changing source bytes.</summary>
    /// <param name="document">The fully opened source-retaining document.</param>
    /// <param name="edits">Exact existing-tag replacements.</param>
    /// <param name="options">Metadata-patch limits, consent, and validation controls.</param>
    /// <returns>The fixed-size patch plan.</returns>
    RawWritePlan PlanMetadataPatch(RawDocument document, RawMetadataPatchEdit[] edits, RawWriteOptions options);

    /// <summary>Applies eligible same-size TIFF, CR2, or NEF metadata value replacements to an independent source copy.</summary>
    /// <param name="document">The fully opened source-retaining document.</param>
    /// <param name="edits">Exact existing-tag replacements.</param>
    /// <param name="options">Metadata-patch limits, consent, and validation controls.</param>
    /// <returns>The complete fixed-length patched file after reopen validation.</returns>
    byte[] ApplyMetadataPatch(RawDocument document, RawMetadataPatchEdit[] edits, RawWriteOptions options);

    /// <summary>Encodes exact unsigned stored samples using a generic RAW descriptor.</summary>
    /// <param name="descriptor">The exact output descriptor.</param>
    /// <param name="source">The unsigned stored samples.</param>
    /// <param name="sourceOffset">The first source sample.</param>
    /// <param name="sourceRowStride">The source row stride in samples.</param>
    /// <param name="options">The generic encoding controls.</param>
    /// <returns>The complete generic RAW bytes.</returns>
    byte[] EncodeGenericUnsigned(RawDescriptor descriptor, uint[] source, int sourceOffset, int sourceRowStride, RawGenericEncodeOptions options);

    /// <summary>Encodes exact signed stored samples using a generic RAW descriptor.</summary>
    /// <param name="descriptor">The exact output descriptor.</param>
    /// <param name="source">The signed stored samples.</param>
    /// <param name="sourceOffset">The first source sample.</param>
    /// <param name="sourceRowStride">The source row stride in samples.</param>
    /// <param name="options">The generic encoding controls.</param>
    /// <returns>The complete generic RAW bytes.</returns>
    byte[] EncodeGenericSigned(RawDescriptor descriptor, int[] source, int sourceOffset, int sourceRowStride, RawGenericEncodeOptions options);

    /// <summary>Encodes exact floating stored samples using a generic RAW descriptor.</summary>
    /// <param name="descriptor">The exact output descriptor.</param>
    /// <param name="source">The floating stored samples.</param>
    /// <param name="sourceOffset">The first source sample.</param>
    /// <param name="sourceRowStride">The source row stride in samples.</param>
    /// <param name="options">The generic encoding controls.</param>
    /// <returns>The complete generic RAW bytes.</returns>
    byte[] EncodeGenericFloating(RawDescriptor descriptor, float[] source, int sourceOffset, int sourceRowStride, RawGenericEncodeOptions options);

    /// <summary>Encodes normalized unsigned samples using a generic RAW descriptor.</summary>
    /// <param name="descriptor">The exact output descriptor.</param>
    /// <param name="source">The normalized unsigned samples.</param>
    /// <param name="sourceOffset">The first source sample.</param>
    /// <param name="sourceRowStride">The source row stride in samples.</param>
    /// <param name="options">The generic encoding controls.</param>
    /// <returns>The complete generic RAW bytes.</returns>
    byte[] EncodeGenericNormalizedUnsigned(RawDescriptor descriptor, ushort[] source, int sourceOffset, int sourceRowStride, RawGenericEncodeOptions options);

    /// <summary>Encodes normalized floating samples using a generic RAW descriptor.</summary>
    /// <param name="descriptor">The exact output descriptor.</param>
    /// <param name="source">The normalized floating samples.</param>
    /// <param name="sourceOffset">The first source sample.</param>
    /// <param name="sourceRowStride">The source row stride in samples.</param>
    /// <param name="options">The generic encoding controls.</param>
    /// <returns>The complete generic RAW bytes.</returns>
    byte[] EncodeGenericNormalizedFloating(RawDescriptor descriptor, float[] source, int sourceOffset, int sourceRowStride, RawGenericEncodeOptions options);

    /// <summary>Plans encoding exact unsigned stored samples using a generic RAW descriptor.</summary>
    /// <param name="descriptor">The exact output descriptor.</param>
    /// <param name="source">The unsigned stored samples.</param>
    /// <param name="sourceOffset">The first source sample.</param>
    /// <param name="sourceRowStride">The source row stride in samples.</param>
    /// <param name="options">The generic encoding controls.</param>
    /// <returns>The complete dry-run plan.</returns>
    RawWritePlan PlanGenericUnsigned(RawDescriptor descriptor, uint[] source, int sourceOffset, int sourceRowStride, RawGenericEncodeOptions options);

    /// <summary>Plans encoding exact signed stored samples using a generic RAW descriptor.</summary>
    /// <param name="descriptor">The exact output descriptor.</param>
    /// <param name="source">The signed stored samples.</param>
    /// <param name="sourceOffset">The first source sample.</param>
    /// <param name="sourceRowStride">The source row stride in samples.</param>
    /// <param name="options">The generic encoding controls.</param>
    /// <returns>The complete dry-run plan.</returns>
    RawWritePlan PlanGenericSigned(RawDescriptor descriptor, int[] source, int sourceOffset, int sourceRowStride, RawGenericEncodeOptions options);

    /// <summary>Plans encoding exact floating stored samples using a generic RAW descriptor.</summary>
    /// <param name="descriptor">The exact output descriptor.</param>
    /// <param name="source">The floating stored samples.</param>
    /// <param name="sourceOffset">The first source sample.</param>
    /// <param name="sourceRowStride">The source row stride in samples.</param>
    /// <param name="options">The generic encoding controls.</param>
    /// <returns>The complete dry-run plan.</returns>
    RawWritePlan PlanGenericFloating(RawDescriptor descriptor, float[] source, int sourceOffset, int sourceRowStride, RawGenericEncodeOptions options);

    /// <summary>Plans encoding normalized unsigned samples using a generic RAW descriptor.</summary>
    /// <param name="descriptor">The exact output descriptor.</param>
    /// <param name="source">The normalized unsigned samples.</param>
    /// <param name="sourceOffset">The first source sample.</param>
    /// <param name="sourceRowStride">The source row stride in samples.</param>
    /// <param name="options">The generic encoding controls.</param>
    /// <returns>The complete dry-run plan.</returns>
    RawWritePlan PlanGenericNormalizedUnsigned(RawDescriptor descriptor, ushort[] source, int sourceOffset, int sourceRowStride, RawGenericEncodeOptions options);

    /// <summary>Plans encoding normalized floating samples using a generic RAW descriptor.</summary>
    /// <param name="descriptor">The exact output descriptor.</param>
    /// <param name="source">The normalized floating samples.</param>
    /// <param name="sourceOffset">The first source sample.</param>
    /// <param name="sourceRowStride">The source row stride in samples.</param>
    /// <param name="options">The generic encoding controls.</param>
    /// <returns>The complete dry-run plan.</returns>
    RawWritePlan PlanGenericNormalizedFloating(RawDescriptor descriptor, float[] source, int sourceOffset, int sourceRowStride, RawGenericEncodeOptions options);

    /// <summary>Encodes exact unsigned stored samples to a caller-owned stream.</summary>
    /// <param name="destination">The writable caller-owned stream, which remains open.</param>
    /// <param name="descriptor">The exact output descriptor.</param>
    /// <param name="source">The unsigned stored samples.</param>
    /// <param name="sourceOffset">The first source sample.</param>
    /// <param name="sourceRowStride">The source row stride in samples.</param>
    /// <param name="options">The generic encoding controls.</param>
    void EncodeGenericUnsignedToStream(System.IO.Stream destination, RawDescriptor descriptor, uint[] source, int sourceOffset, int sourceRowStride, RawGenericEncodeOptions options);

    /// <summary>Encodes exact signed stored samples to a caller-owned stream.</summary>
    /// <param name="destination">The writable caller-owned stream, which remains open.</param>
    /// <param name="descriptor">The exact output descriptor.</param>
    /// <param name="source">The signed stored samples.</param>
    /// <param name="sourceOffset">The first source sample.</param>
    /// <param name="sourceRowStride">The source row stride in samples.</param>
    /// <param name="options">The generic encoding controls.</param>
    void EncodeGenericSignedToStream(System.IO.Stream destination, RawDescriptor descriptor, int[] source, int sourceOffset, int sourceRowStride, RawGenericEncodeOptions options);

    /// <summary>Encodes exact floating stored samples to a caller-owned stream.</summary>
    /// <param name="destination">The writable caller-owned stream, which remains open.</param>
    /// <param name="descriptor">The exact output descriptor.</param>
    /// <param name="source">The floating stored samples.</param>
    /// <param name="sourceOffset">The first source sample.</param>
    /// <param name="sourceRowStride">The source row stride in samples.</param>
    /// <param name="options">The generic encoding controls.</param>
    void EncodeGenericFloatingToStream(System.IO.Stream destination, RawDescriptor descriptor, float[] source, int sourceOffset, int sourceRowStride, RawGenericEncodeOptions options);

    /// <summary>Encodes normalized unsigned samples to a caller-owned stream.</summary>
    /// <param name="destination">The writable caller-owned stream, which remains open.</param>
    /// <param name="descriptor">The exact output descriptor.</param>
    /// <param name="source">The normalized unsigned samples.</param>
    /// <param name="sourceOffset">The first source sample.</param>
    /// <param name="sourceRowStride">The source row stride in samples.</param>
    /// <param name="options">The generic encoding controls.</param>
    void EncodeGenericNormalizedUnsignedToStream(System.IO.Stream destination, RawDescriptor descriptor, ushort[] source, int sourceOffset, int sourceRowStride, RawGenericEncodeOptions options);

    /// <summary>Encodes normalized floating samples to a caller-owned stream.</summary>
    /// <param name="destination">The writable caller-owned stream, which remains open.</param>
    /// <param name="descriptor">The exact output descriptor.</param>
    /// <param name="source">The normalized floating samples.</param>
    /// <param name="sourceOffset">The first source sample.</param>
    /// <param name="sourceRowStride">The source row stride in samples.</param>
    /// <param name="options">The generic encoding controls.</param>
    void EncodeGenericNormalizedFloatingToStream(System.IO.Stream destination, RawDescriptor descriptor, float[] source, int sourceOffset, int sourceRowStride, RawGenericEncodeOptions options);

    /// <summary>Encodes exact unsigned stored samples through a validated temporary file before destination commit.</summary>
    /// <param name="destinationPath">The destination file path.</param>
    /// <param name="descriptor">The exact output descriptor.</param>
    /// <param name="source">The unsigned stored samples.</param>
    /// <param name="sourceOffset">The first source sample.</param>
    /// <param name="sourceRowStride">The source row stride in samples.</param>
    /// <param name="options">The generic encoding controls.</param>
    /// <returns>The committed destination or recoverable temporary-file result.</returns>
    RawPathWriteResult EncodeGenericUnsignedToPath(string destinationPath, RawDescriptor descriptor, uint[] source, int sourceOffset, int sourceRowStride, RawGenericEncodeOptions options);

    /// <summary>Encodes exact signed stored samples through a validated temporary file before destination commit.</summary>
    /// <param name="destinationPath">The destination file path.</param>
    /// <param name="descriptor">The exact output descriptor.</param>
    /// <param name="source">The signed stored samples.</param>
    /// <param name="sourceOffset">The first source sample.</param>
    /// <param name="sourceRowStride">The source row stride in samples.</param>
    /// <param name="options">The generic encoding controls.</param>
    /// <returns>The committed destination or recoverable temporary-file result.</returns>
    RawPathWriteResult EncodeGenericSignedToPath(string destinationPath, RawDescriptor descriptor, int[] source, int sourceOffset, int sourceRowStride, RawGenericEncodeOptions options);

    /// <summary>Encodes exact floating stored samples through a validated temporary file before destination commit.</summary>
    /// <param name="destinationPath">The destination file path.</param>
    /// <param name="descriptor">The exact output descriptor.</param>
    /// <param name="source">The floating stored samples.</param>
    /// <param name="sourceOffset">The first source sample.</param>
    /// <param name="sourceRowStride">The source row stride in samples.</param>
    /// <param name="options">The generic encoding controls.</param>
    /// <returns>The committed destination or recoverable temporary-file result.</returns>
    RawPathWriteResult EncodeGenericFloatingToPath(string destinationPath, RawDescriptor descriptor, float[] source, int sourceOffset, int sourceRowStride, RawGenericEncodeOptions options);

    /// <summary>Encodes normalized unsigned samples through a validated temporary file before destination commit.</summary>
    /// <param name="destinationPath">The destination file path.</param>
    /// <param name="descriptor">The exact output descriptor.</param>
    /// <param name="source">The normalized unsigned samples.</param>
    /// <param name="sourceOffset">The first source sample.</param>
    /// <param name="sourceRowStride">The source row stride in samples.</param>
    /// <param name="options">The generic encoding controls.</param>
    /// <returns>The committed destination or recoverable temporary-file result.</returns>
    RawPathWriteResult EncodeGenericNormalizedUnsignedToPath(string destinationPath, RawDescriptor descriptor, ushort[] source, int sourceOffset, int sourceRowStride, RawGenericEncodeOptions options);

    /// <summary>Encodes normalized floating samples through a validated temporary file before destination commit.</summary>
    /// <param name="destinationPath">The destination file path.</param>
    /// <param name="descriptor">The exact output descriptor.</param>
    /// <param name="source">The normalized floating samples.</param>
    /// <param name="sourceOffset">The first source sample.</param>
    /// <param name="sourceRowStride">The source row stride in samples.</param>
    /// <param name="options">The generic encoding controls.</param>
    /// <returns>The committed destination or recoverable temporary-file result.</returns>
    RawPathWriteResult EncodeGenericNormalizedFloatingToPath(string destinationPath, RawDescriptor descriptor, float[] source, int sourceOffset, int sourceRowStride, RawGenericEncodeOptions options);
}
