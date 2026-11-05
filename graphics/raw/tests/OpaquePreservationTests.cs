using raw;

namespace raw.Tests;

public class OpaquePreservationTests
{
    [Test]
    public void OpaqueTrailingIslandIsPreservedByteExactlyAndSerializationIsDeterministic()
    {
        byte[] encoded = Cr2Source();
        byte[] opaque = new byte[] { 0xde, 0xad, 0, 0xbe, 0xef, 0x51 };
        byte[] source = new byte[encoded.Length + opaque.Length];
        Array.Copy(encoded, source, encoded.Length);
        Array.Copy(opaque, 0, source, encoded.Length, opaque.Length);
        RawDocument document = Open(source);
        RawPayloadBlock island = document.PayloadMap.Find(RawPayloadKind.Opaque).Single();

        byte[] first = RawWriter.RewriteCr2(document, new RawWriteOptions());
        byte[] second = RawWriter.RewriteCr2(document, new RawWriteOptions());
        RawDocument reopened = Open(first);
        RawPayloadBlock preserved = reopened.PayloadMap.Find(RawPayloadKind.Opaque).Single(block => block.Path == island.Path);

        Assert.Multiple(() =>
        {
            Assert.That(document.ExtractPayload(island), Is.EqualTo(opaque));
            Assert.That(reopened.ExtractPayload(preserved), Is.EqualTo(opaque));
            Assert.That(first, Is.EqualTo(second));
            Assert.That(preserved.PreserveByteExact, Is.True);
        });
    }

    [Test]
    public void OriginalDecisionDataIsRelocatedAndPreservedButEditsReportSignatureInvalidation()
    {
        byte[] source = SourceWithCanonOriginalDecisionData();
        RawDocument document = Open(source);
        RawPayloadBlock authenticity = document.PayloadMap.Find(RawPayloadKind.Authenticity).Single();
        byte[] originalBytes = document.ExtractPayload(authenticity);
        RawMetadataEditSet edits = new RawMetadataEditSet();
        edits.Replace("Artist", new string[] { "Authenticity-aware editor" }, null, RawMetadataSource.Exif);
        Assert.That(edits.Apply(document, false).Succeeded(), Is.True);

        RawWritePlan plan = RawWriter.PlanRewriteCr2(document, new RawWriteOptions());
        byte[] rewritten = RawWriter.RewriteCr2(document, new RawWriteOptions());
        RawDocument reopened = Open(rewritten);
        RawPayloadBlock retained = reopened.PayloadMap.Find(RawPayloadKind.Authenticity).Single(block => block.Path == authenticity.Path);

        Assert.Multiple(() =>
        {
            Assert.That(plan.AuthenticityBlocks, Has.Length.EqualTo(1));
            Assert.That(plan.AuthenticityInvalidated, Is.True);
            Assert.That(plan.Diagnostics.Any(item => item.Code == RawDiagnosticCode.SignatureInvalidated && item.Severity == RawSeverity.Warning), Is.True);
            Assert.That(plan.Diagnostics.Single(item => item.Code == RawDiagnosticCode.SignatureInvalidated).Message, Does.Contain("no replacement signature"));
            Assert.That(reopened.ExtractPayload(retained), Is.EqualTo(originalBytes));
            Assert.That(reopened.PayloadMap.Find(RawPayloadKind.Authenticity), Has.Length.EqualTo(1));
            Assert.That(retained.Offset, Is.Not.EqualTo(authenticity.Offset));
        });
    }

    [Test]
    public void UneditedAuthenticityRemainsValidAndNewWriterDoesNotFabricateIt()
    {
        RawDocument signed = Open(SourceWithCanonOriginalDecisionData());
        RawWritePlan rewritePlan = RawWriter.PlanRewriteCr2(signed, new RawWriteOptions());
        RawDocument ordinary = Open(Cr2Source());

        Assert.Multiple(() =>
        {
            Assert.That(rewritePlan.AuthenticityBlocks, Has.Length.EqualTo(1));
            Assert.That(rewritePlan.AuthenticityInvalidated, Is.False);
            Assert.That(rewritePlan.Diagnostics.Any(item => item.Code == RawDiagnosticCode.SignatureInvalidated), Is.False);
            Assert.That(ordinary.PayloadMap.Find(RawPayloadKind.Authenticity), Is.Empty);
        });
    }

    [Test]
    public void PayloadMapAllowsAliasesAndNestingButRejectsCrossingOverlaps()
    {
        RawPayloadBlock outer = new RawPayloadBlock(RawPayloadKind.MakerNote, "MakerNote", 10, 20, -1, -1, -1, RawCompression.None, false, true);
        RawPayloadBlock nested = new RawPayloadBlock(RawPayloadKind.Authenticity, "MakerNote/Decision", 15, 5, -1, -1, -1, RawCompression.None, false, true);
        RawPayloadBlock alias = new RawPayloadBlock(RawPayloadKind.Metadata, "Alias", 10, 20, -1, -1, -1, RawCompression.None, false, true);
        RawPayloadBlock crossing = new RawPayloadBlock(RawPayloadKind.Opaque, "Crossing", 25, 10, -1, -1, -1, RawCompression.None, false, true);

        RawPayloadMap valid = new RawPayloadMap(40, new RawPayloadBlock[] { outer, nested, alias });
        RawPayloadMap invalid = new RawPayloadMap(40, new RawPayloadBlock[] { outer, crossing });

        Assert.Multiple(() =>
        {
            Assert.That(valid.Validate().IsValid(), Is.True);
            Assert.That(invalid.Validate().IsValid(), Is.False);
            Assert.That(((invalid.Validate().Messages ?? throw new System.InvalidOperationException("A required value is unavailable.")).Any(item => item.Code == RawDiagnosticCode.InvalidContainer && item.Context == "PayloadMap.Overlap")), Is.True);
        });
    }

    [Test]
    public void PayloadMapRejectsAmbiguousOpaquePaths()
    {
        RawPayloadBlock first = new RawPayloadBlock(RawPayloadKind.Opaque, "Opaque/Island0", 8, 4, -1, -1, -1, RawCompression.None, false, true);
        RawPayloadBlock second = new RawPayloadBlock(RawPayloadKind.Opaque, "Opaque/Island0", 16, 4, -1, -1, -1, RawCompression.None, false, true);
        RawPayloadBlock unnamed = new RawPayloadBlock(RawPayloadKind.Opaque, string.Empty, 20, 4, -1, -1, -1, RawCompression.None, false, true);

        RawValidationReport report = new RawPayloadMap(24, new RawPayloadBlock[] { first, second, unnamed }).Validate();

        Assert.Multiple(() =>
        {
            Assert.That(report.IsValid(), Is.False);
            Assert.That(((report.Messages ?? throw new System.InvalidOperationException("A required value is unavailable.")).Any(item => item.Context == "PayloadMap.Paths" && item.Code == RawDiagnosticCode.InvalidDescriptor)), Is.True);
            Assert.That(report.Messages.Any(item => item.Context == "Payload.Path" && item.Code == RawDiagnosticCode.InvalidArgument), Is.True);
        });
    }

    [Test]
    public void ReopenedRawPayloadIdentityCheckDetectsAChangedCompressedBlock()
    {
        RawDocument document = Open(Cr2Source());
        RawWriteOptions options = new RawWriteOptions();
        options.VerifyPixelIdentity = false;
        options.VerifyRawPayloadIdentity = true;
        Cr2RewriteState state = Cr2Rewriter.CreateState(document, options);
        TiffLayoutBlockPlan? raw = ((state.Plan ?? throw new System.InvalidOperationException("A required value is unavailable.")).Blocks ?? throw new System.InvalidOperationException("A required value is unavailable.")).Single(block => state.Blocks.Single(source => source.Name == block.Name).Raw);
        (raw.Bytes ?? throw new System.InvalidOperationException("A required value is unavailable."))[raw.Bytes.Length - 1] ^= 1;
        byte[] output = TiffWriter.WriteToArray(state.Plan, options.Limits, options.CancellationToken);

        RawWriteException? exception = Assert.Throws<RawWriteException>(() => Cr2Rewriter.ValidateOutput(state, output));

        Assert.That((exception ?? throw new System.InvalidOperationException("A required value is unavailable.")).Code, Is.EqualTo(RawDiagnosticCode.OutputValidationFailed));
    }

    private static byte[] SourceWithCanonOriginalDecisionData()
    {
        byte[] source = Cr2Source();
        RawDocument document = Open(source);
        RawMakerNote note = document.Metadata.MakerNotes.Single();
        RawCanonMakerNoteTag? modelId = note.FindCanonTag(0x0010);
        byte[] decision = CanonOriginalDecisionData();
        byte[] result = new byte[source.Length + decision.Length];
        Array.Copy(source, result, source.Length);
        Array.Copy(decision, 0, result, source.Length, decision.Length);
        int entry = checked((int)note.Offset + (modelId ?? throw new System.InvalidOperationException("A required value is unavailable.")).EntryPosition);
        result[entry] = 0x83;
        result[entry + 1] = 0x00;
        EndianWriter.WriteUInt32(result.AsSpan(entry + 8, 4), (uint)source.Length, RawByteOrder.LittleEndian);
        return result;
    }

    private static byte[] CanonOriginalDecisionData()
    {
        byte[] result = new byte[64];
        result[0] = 0xff;
        result[1] = 0xff;
        result[2] = 0xff;
        result[3] = 0xff;
        EndianWriter.WriteUInt32(result.AsSpan(4, 4), 2, RawByteOrder.LittleEndian);
        EndianWriter.WriteUInt32(result.AsSpan(28, 4), 1, RawByteOrder.LittleEndian);
        int index = 32;
        while (index < result.Length)
        {
            result[index] = (byte)(index * 3 + 1);
            index++;
        }
        return result;
    }

    private static byte[] Cr2Source()
    {
        return Cr2Encoder.Encode(Cr2EncoderTests.Input(8, 4, 8), Cr2EncoderTests.SmallProfile(), Cr2EncoderTests.Options());
    }

    private static RawDocument Open(byte[] source)
    {
        RawOpenOptions options = new RawOpenOptions();
        options.AllowExperimentalProfiles = true;
        options.ReadMetadata = true;
        options.ReadPreviews = true;
        options.ReadRawPayload = true;
        return RawCodec.Open(source, null, options);
    }
}
