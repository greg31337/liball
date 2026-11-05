namespace raw;

/// <summary>Provides the default stateless implementation of <see cref="IRawWriter"/>.</summary>
public class RawWriterService : IRawWriter
{
    /// <summary>Creates a stateless writer service.</summary>
    public RawWriterService()
    {
    }

    /// <summary>Rewrites an opened CR2 through a validated sibling temporary file before atomic destination commit.</summary>
    public RawPathWriteResult RewriteCr2ToPath(string destinationPath, RawDocument document, RawWriteOptions options)
    {
        return RawWriter.RewriteCr2(destinationPath, document, options);
    }

    /// <summary>Rewrites an opened NEF through a validated sibling temporary file before atomic destination commit.</summary>
    public RawPathWriteResult RewriteNefToPath(string destinationPath, RawDocument document, RawWriteOptions options)
    {
        return RawWriter.RewriteNef(destinationPath, document, options);
    }

    /// <summary>Rewrites an opened CR2 to a bounded caller-owned stream without closing it.</summary>
    public void RewriteCr2ToStream(System.IO.Stream destination, RawDocument document, RawWriteOptions options)
    {
        RequireContainerStream(destination, RawFormat.Cr2);
        byte[] bytes = RawWriter.RewriteCr2(document, options);
        WriteContainerStream(destination, bytes, options);
    }

    /// <summary>Rewrites an opened NEF to a bounded caller-owned stream without closing it.</summary>
    public void RewriteNefToStream(System.IO.Stream destination, RawDocument document, RawWriteOptions options)
    {
        RequireContainerStream(destination, RawFormat.Nef);
        byte[] bytes = RawWriter.RewriteNef(document, options);
        WriteContainerStream(destination, bytes, options);
    }

    /// <summary>Rewrites an opened CR2 while preserving supported opaque content.</summary>
    public byte[] RewriteCr2(RawDocument document, RawWriteOptions options)
    {
        return RawWriter.RewriteCr2(document, options);
    }

    /// <summary>Rewrites an opened NEF while preserving supported opaque content.</summary>
    public byte[] RewriteNef(RawDocument document, RawWriteOptions options)
    {
        return RawWriter.RewriteNef(document, options);
    }

    /// <summary>Encodes one profile-gated CR2 in memory.</summary>
    public byte[] EncodeCr2(RawCr2WriteInput input, RawWriteOptions options)
    {
        return RawWriter.EncodeCr2(input, options);
    }

    /// <summary>Encodes one profile-gated NEF in memory.</summary>
    public byte[] EncodeNef(RawNefWriteInput input, RawWriteOptions options)
    {
        return RawWriter.EncodeNef(input, options);
    }

    /// <summary>Encodes one CR2 using a caller-supplied profile source.</summary>
    public byte[] EncodeCr2WithProfiles(RawCr2WriteInput input, IRawProfileCatalogue catalogue, RawWriteOptions options)
    {
        return RawWriter.EncodeCr2(input, catalogue, options);
    }

    /// <summary>Encodes one NEF using a caller-supplied profile source.</summary>
    public byte[] EncodeNefWithProfiles(RawNefWriteInput input, IRawProfileCatalogue catalogue, RawWriteOptions options)
    {
        return RawWriter.EncodeNef(input, catalogue, options);
    }

    /// <summary>Encodes one profile-gated CR2 to a bounded caller-owned stream without closing it.</summary>
    public void EncodeCr2ToStream(System.IO.Stream destination, RawCr2WriteInput input, RawWriteOptions options)
    {
        RequireContainerStream(destination, RawFormat.Cr2);
        byte[] bytes = RawWriter.EncodeCr2(input, options);
        WriteContainerStream(destination, bytes, options);
    }

    /// <summary>Encodes one profile-gated NEF to a bounded caller-owned stream without closing it.</summary>
    public void EncodeNefToStream(System.IO.Stream destination, RawNefWriteInput input, RawWriteOptions options)
    {
        RequireContainerStream(destination, RawFormat.Nef);
        byte[] bytes = RawWriter.EncodeNef(input, options);
        WriteContainerStream(destination, bytes, options);
    }

    /// <summary>Encodes one CR2 to a bounded caller-owned stream using a caller-supplied profile source.</summary>
    public void EncodeCr2WithProfilesToStream(System.IO.Stream destination, RawCr2WriteInput input, IRawProfileCatalogue catalogue, RawWriteOptions options)
    {
        RequireContainerStream(destination, RawFormat.Cr2);
        byte[] bytes = RawWriter.EncodeCr2(input, catalogue, options);
        WriteContainerStream(destination, bytes, options);
    }

    /// <summary>Encodes one NEF to a bounded caller-owned stream using a caller-supplied profile source.</summary>
    public void EncodeNefWithProfilesToStream(System.IO.Stream destination, RawNefWriteInput input, IRawProfileCatalogue catalogue, RawWriteOptions options)
    {
        RequireContainerStream(destination, RawFormat.Nef);
        byte[] bytes = RawWriter.EncodeNef(input, catalogue, options);
        WriteContainerStream(destination, bytes, options);
    }

    /// <summary>Encodes one profile-gated CR2 through a validated sibling temporary file before atomic destination commit.</summary>
    public RawPathWriteResult EncodeCr2ToPath(string destinationPath, RawCr2WriteInput input, RawWriteOptions options)
    {
        return RawWriter.EncodeCr2(destinationPath, input, options);
    }

    /// <summary>Encodes one profile-gated NEF through a validated sibling temporary file before atomic destination commit.</summary>
    public RawPathWriteResult EncodeNefToPath(string destinationPath, RawNefWriteInput input, RawWriteOptions options)
    {
        return RawWriter.EncodeNef(destinationPath, input, options);
    }

    /// <summary>Encodes one CR2 through a validated sibling temporary file using a caller-supplied profile source.</summary>
    public RawPathWriteResult EncodeCr2WithProfilesToPath(string destinationPath, RawCr2WriteInput input, IRawProfileCatalogue catalogue, RawWriteOptions options)
    {
        return RawWriter.EncodeCr2(destinationPath, input, catalogue, options);
    }

    /// <summary>Encodes one NEF through a validated sibling temporary file using a caller-supplied profile source.</summary>
    public RawPathWriteResult EncodeNefWithProfilesToPath(string destinationPath, RawNefWriteInput input, IRawProfileCatalogue catalogue, RawWriteOptions options)
    {
        return RawWriter.EncodeNef(destinationPath, input, catalogue, options);
    }

    /// <summary>Plans one profile-gated CR2 encoding operation.</summary>
    public RawWritePlan PlanCr2(RawCr2WriteInput input, RawWriteOptions options)
    {
        return RawWriter.PlanCr2(input, options);
    }

    /// <summary>Plans one profile-gated NEF encoding operation.</summary>
    public RawWritePlan PlanNef(RawNefWriteInput input, RawWriteOptions options)
    {
        return RawWriter.PlanNef(input, options);
    }

    /// <summary>Plans one CR2 encoding operation using a caller-supplied profile source.</summary>
    public RawWritePlan PlanCr2WithProfiles(RawCr2WriteInput input, IRawProfileCatalogue catalogue, RawWriteOptions options)
    {
        return RawWriter.PlanCr2(input, catalogue, options);
    }

    /// <summary>Plans one NEF encoding operation using a caller-supplied profile source.</summary>
    public RawWritePlan PlanNefWithProfiles(RawNefWriteInput input, IRawProfileCatalogue catalogue, RawWriteOptions options)
    {
        return RawWriter.PlanNef(input, catalogue, options);
    }

    /// <summary>Plans one preserving CR2 rewrite.</summary>
    public RawWritePlan PlanRewriteCr2(RawDocument document, RawWriteOptions options)
    {
        return RawWriter.PlanRewriteCr2(document, options);
    }

    /// <summary>Plans one preserving NEF rewrite.</summary>
    public RawWritePlan PlanRewriteNef(RawDocument document, RawWriteOptions options)
    {
        return RawWriter.PlanRewriteNef(document, options);
    }

    /// <summary>Plans eligible same-size TIFF, CR2, or NEF metadata value replacements without changing source bytes.</summary>
    public RawWritePlan PlanMetadataPatch(RawDocument document, RawMetadataPatchEdit[] edits, RawWriteOptions options)
    {
        return RawMetadataPatcher.Plan(document, edits, options);
    }

    /// <summary>Applies eligible same-size TIFF, CR2, or NEF metadata value replacements to an independent source copy.</summary>
    public byte[] ApplyMetadataPatch(RawDocument document, RawMetadataPatchEdit[] edits, RawWriteOptions options)
    {
        return RawMetadataPatcher.Apply(document, edits, options);
    }

    /// <summary>Encodes exact unsigned stored samples using a generic RAW descriptor.</summary>
    public byte[] EncodeGenericUnsigned(RawDescriptor descriptor, uint[] source, int sourceOffset, int sourceRowStride, RawGenericEncodeOptions options)
    {
        return GenericRawEncoder.EncodeStored(descriptor, source, sourceOffset, sourceRowStride, options);
    }

    /// <summary>Encodes exact signed stored samples using a generic RAW descriptor.</summary>
    public byte[] EncodeGenericSigned(RawDescriptor descriptor, int[] source, int sourceOffset, int sourceRowStride, RawGenericEncodeOptions options)
    {
        return GenericRawEncoder.EncodeStored(descriptor, source, sourceOffset, sourceRowStride, options);
    }

    /// <summary>Encodes exact floating stored samples using a generic RAW descriptor.</summary>
    public byte[] EncodeGenericFloating(RawDescriptor descriptor, float[] source, int sourceOffset, int sourceRowStride, RawGenericEncodeOptions options)
    {
        return GenericRawEncoder.EncodeStored(descriptor, source, sourceOffset, sourceRowStride, options);
    }

    /// <summary>Encodes normalized unsigned samples using a generic RAW descriptor.</summary>
    public byte[] EncodeGenericNormalizedUnsigned(RawDescriptor descriptor, ushort[] source, int sourceOffset, int sourceRowStride, RawGenericEncodeOptions options)
    {
        return GenericRawEncoder.EncodeNormalized(descriptor, source, sourceOffset, sourceRowStride, options);
    }

    /// <summary>Encodes normalized floating samples using a generic RAW descriptor.</summary>
    public byte[] EncodeGenericNormalizedFloating(RawDescriptor descriptor, float[] source, int sourceOffset, int sourceRowStride, RawGenericEncodeOptions options)
    {
        return GenericRawEncoder.EncodeNormalized(descriptor, source, sourceOffset, sourceRowStride, options);
    }

    /// <summary>Plans encoding exact unsigned stored samples using a generic RAW descriptor.</summary>
    public RawWritePlan PlanGenericUnsigned(RawDescriptor descriptor, uint[] source, int sourceOffset, int sourceRowStride, RawGenericEncodeOptions options)
    {
        return GenericRawEncoder.PlanStoredUnsigned(descriptor, source, sourceOffset, sourceRowStride, options);
    }

    /// <summary>Plans encoding exact signed stored samples using a generic RAW descriptor.</summary>
    public RawWritePlan PlanGenericSigned(RawDescriptor descriptor, int[] source, int sourceOffset, int sourceRowStride, RawGenericEncodeOptions options)
    {
        return GenericRawEncoder.PlanStoredSigned(descriptor, source, sourceOffset, sourceRowStride, options);
    }

    /// <summary>Plans encoding exact floating stored samples using a generic RAW descriptor.</summary>
    public RawWritePlan PlanGenericFloating(RawDescriptor descriptor, float[] source, int sourceOffset, int sourceRowStride, RawGenericEncodeOptions options)
    {
        return GenericRawEncoder.PlanStoredFloating(descriptor, source, sourceOffset, sourceRowStride, options);
    }

    /// <summary>Plans encoding normalized unsigned samples using a generic RAW descriptor.</summary>
    public RawWritePlan PlanGenericNormalizedUnsigned(RawDescriptor descriptor, ushort[] source, int sourceOffset, int sourceRowStride, RawGenericEncodeOptions options)
    {
        return GenericRawEncoder.PlanNormalizedUnsigned(descriptor, source, sourceOffset, sourceRowStride, options);
    }

    /// <summary>Plans encoding normalized floating samples using a generic RAW descriptor.</summary>
    public RawWritePlan PlanGenericNormalizedFloating(RawDescriptor descriptor, float[] source, int sourceOffset, int sourceRowStride, RawGenericEncodeOptions options)
    {
        return GenericRawEncoder.PlanNormalizedFloating(descriptor, source, sourceOffset, sourceRowStride, options);
    }

    /// <summary>Encodes exact unsigned stored samples to a caller-owned stream.</summary>
    public void EncodeGenericUnsignedToStream(System.IO.Stream destination, RawDescriptor descriptor, uint[] source, int sourceOffset, int sourceRowStride, RawGenericEncodeOptions options)
    {
        GenericRawEncoder.EncodeStored(destination, descriptor, source, sourceOffset, sourceRowStride, options);
    }

    /// <summary>Encodes exact signed stored samples to a caller-owned stream.</summary>
    public void EncodeGenericSignedToStream(System.IO.Stream destination, RawDescriptor descriptor, int[] source, int sourceOffset, int sourceRowStride, RawGenericEncodeOptions options)
    {
        GenericRawEncoder.EncodeStored(destination, descriptor, source, sourceOffset, sourceRowStride, options);
    }

    /// <summary>Encodes exact floating stored samples to a caller-owned stream.</summary>
    public void EncodeGenericFloatingToStream(System.IO.Stream destination, RawDescriptor descriptor, float[] source, int sourceOffset, int sourceRowStride, RawGenericEncodeOptions options)
    {
        GenericRawEncoder.EncodeStored(destination, descriptor, source, sourceOffset, sourceRowStride, options);
    }

    /// <summary>Encodes normalized unsigned samples to a caller-owned stream.</summary>
    public void EncodeGenericNormalizedUnsignedToStream(System.IO.Stream destination, RawDescriptor descriptor, ushort[] source, int sourceOffset, int sourceRowStride, RawGenericEncodeOptions options)
    {
        GenericRawEncoder.EncodeNormalized(destination, descriptor, source, sourceOffset, sourceRowStride, options);
    }

    /// <summary>Encodes normalized floating samples to a caller-owned stream.</summary>
    public void EncodeGenericNormalizedFloatingToStream(System.IO.Stream destination, RawDescriptor descriptor, float[] source, int sourceOffset, int sourceRowStride, RawGenericEncodeOptions options)
    {
        GenericRawEncoder.EncodeNormalized(destination, descriptor, source, sourceOffset, sourceRowStride, options);
    }

    /// <summary>Encodes exact unsigned stored samples through a validated temporary file before destination commit.</summary>
    public RawPathWriteResult EncodeGenericUnsignedToPath(string destinationPath, RawDescriptor descriptor, uint[] source, int sourceOffset, int sourceRowStride, RawGenericEncodeOptions options)
    {
        byte[] bytes = GenericRawEncoder.EncodeStored(descriptor, source, sourceOffset, sourceRowStride, options);
        return RawStagedPathWriter.WriteGenericValidated(destinationPath, bytes, descriptor, options);
    }

    /// <summary>Encodes exact signed stored samples through a validated temporary file before destination commit.</summary>
    public RawPathWriteResult EncodeGenericSignedToPath(string destinationPath, RawDescriptor descriptor, int[] source, int sourceOffset, int sourceRowStride, RawGenericEncodeOptions options)
    {
        byte[] bytes = GenericRawEncoder.EncodeStored(descriptor, source, sourceOffset, sourceRowStride, options);
        return RawStagedPathWriter.WriteGenericValidated(destinationPath, bytes, descriptor, options);
    }

    /// <summary>Encodes exact floating stored samples through a validated temporary file before destination commit.</summary>
    public RawPathWriteResult EncodeGenericFloatingToPath(string destinationPath, RawDescriptor descriptor, float[] source, int sourceOffset, int sourceRowStride, RawGenericEncodeOptions options)
    {
        byte[] bytes = GenericRawEncoder.EncodeStored(descriptor, source, sourceOffset, sourceRowStride, options);
        return RawStagedPathWriter.WriteGenericValidated(destinationPath, bytes, descriptor, options);
    }

    /// <summary>Encodes normalized unsigned samples through a validated temporary file before destination commit.</summary>
    public RawPathWriteResult EncodeGenericNormalizedUnsignedToPath(string destinationPath, RawDescriptor descriptor, ushort[] source, int sourceOffset, int sourceRowStride, RawGenericEncodeOptions options)
    {
        byte[] bytes = GenericRawEncoder.EncodeNormalized(descriptor, source, sourceOffset, sourceRowStride, options);
        return RawStagedPathWriter.WriteGenericValidated(destinationPath, bytes, descriptor, options);
    }

    /// <summary>Encodes normalized floating samples through a validated temporary file before destination commit.</summary>
    public RawPathWriteResult EncodeGenericNormalizedFloatingToPath(string destinationPath, RawDescriptor descriptor, float[] source, int sourceOffset, int sourceRowStride, RawGenericEncodeOptions options)
    {
        byte[] bytes = GenericRawEncoder.EncodeNormalized(descriptor, source, sourceOffset, sourceRowStride, options);
        return RawStagedPathWriter.WriteGenericValidated(destinationPath, bytes, descriptor, options);
    }

    internal static void RequireContainerStream(System.IO.Stream destination, RawFormat format)
    {
        if (destination == null || !destination.CanWrite || format != RawFormat.Cr2 && format != RawFormat.Nef)
        {
            throw new RawWriteException(RawDiagnosticCode.InvalidArgument, "A writable CR2 or NEF destination stream is required.", -1);
        }
    }

    internal static void WriteContainerStream(System.IO.Stream destination, byte[] bytes, RawWriteOptions options)
    {
        if (options == null || options.Limits == null || bytes == null || bytes.Length == 0 || bytes.LongLength > options.Limits.MaxFileSizeBytes || bytes.LongLength > options.Limits.MaxAllocationBytes)
        {
            throw new RawWriteException(RawDiagnosticCode.InvalidArgument, "A bounded encoded container and complete write options are required.", -1);
        }
        options.CancellationToken.ThrowIfCancellationRequested();
        destination.Write(bytes, 0, bytes.Length);
    }
}
