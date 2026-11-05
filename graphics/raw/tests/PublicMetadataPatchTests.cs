using raw;

namespace raw.Tests;

public class PublicMetadataPatchTests
{
    [Test]
    public void ExactFixedSizePatchesApplyToTiffCr2AndNefAndReopenWithOnlyPlannedBytesChanged()
    {
        IRawWriter writer = new RawWriterService();
        IRawCodec codec = new RawCodecService();
        RawWriteOptions options = PatchOptions(true);

        byte[] tiffSource = TiffBytes();
        RawDocument tiff = Open(tiffSource);
        RawMetadataPatchEdit tiffEdit = new RawMetadataPatchEdit("IFD0", 274, codec.CreateUnsignedShortTagValue(new ushort[] { 6 }, RawByteOrder.LittleEndian));
        RawWritePlan tiffPlan = writer.PlanMetadataPatch(tiff, new RawMetadataPatchEdit[] { tiffEdit }, options);
        byte[] patchedTiff = writer.ApplyMetadataPatch(tiff, new RawMetadataPatchEdit[] { tiffEdit }, options);

        byte[] cr2Source = Cr2Encoder.Encode(Cr2EncoderTests.Input(8, 4, 8), Cr2EncoderTests.SmallProfile(), Cr2EncoderTests.Options());
        RawDocument cr2 = Open(cr2Source);
        RawMetadataPatchEdit cr2Edit = new RawMetadataPatchEdit("IFD0", 305, codec.CreateAsciiTagValue("raw-patch"));
        RawWritePlan cr2Plan = writer.PlanMetadataPatch(cr2, new RawMetadataPatchEdit[] { cr2Edit }, options);
        byte[] patchedCr2 = writer.ApplyMetadataPatch(cr2, new RawMetadataPatchEdit[] { cr2Edit }, options);

        byte[] nefSource = NefBytes();
        RawDocument nef = Open(nefSource);
        RawMetadataPatchEdit nefEdit = new RawMetadataPatchEdit("IFD0", 274, codec.CreateUnsignedShortTagValue(new ushort[] { 8 }, RawByteOrder.LittleEndian));
        RawWritePlan nefPlan = writer.PlanMetadataPatch(nef, new RawMetadataPatchEdit[] { nefEdit }, options);
        byte[] patchedNef = writer.ApplyMetadataPatch(nef, new RawMetadataPatchEdit[] { nefEdit }, options);

        Assert.Multiple(() =>
        {
            AssertPatch(tiffSource, patchedTiff, tiffPlan);
            AssertPatch(cr2Source, patchedCr2, cr2Plan);
            AssertPatch(nefSource, patchedNef, nefPlan);
            Assert.That(Open(patchedTiff).Format, Is.EqualTo(RawFormat.Tiff));
            Assert.That(Open(patchedCr2).Format, Is.EqualTo(RawFormat.Cr2));
            Assert.That(Open(patchedNef).Format, Is.EqualTo(RawFormat.Nef));
            Assert.That(codec.GetUnsignedShortTagValues((codec.FindMetadataTagByPathAndId(Open(patchedTiff).Metadata, "IFD0", 274) ?? throw new InvalidOperationException()).Value), Is.EqualTo(new ushort[] { 6 }));
            Assert.That(codec.GetAsciiTagText((codec.FindMetadataTagByPathAndId(Open(patchedCr2).Metadata, "IFD0", 305) ?? throw new InvalidOperationException()).Value).Text, Is.EqualTo("raw-patch"));
            Assert.That(codec.GetUnsignedShortTagValues((codec.FindMetadataTagByPathAndId(Open(patchedNef).Metadata, "IFD0", 274) ?? throw new InvalidOperationException()).Value), Is.EqualTo(new ushort[] { 8 }));
        });
    }

    [Test]
    public void AuthenticityInvalidationRequiresConsentAndPreservesAuthenticityAndRawPayloadBytes()
    {
        IRawWriter writer = new RawWriterService();
        IRawCodec codec = new RawCodecService();
        byte[] source = Cr2Encoder.Encode(Cr2EncoderTests.Input(8, 4, 8), Cr2EncoderTests.SmallProfile(), Cr2EncoderTests.Options());
        RawDocument document = Open(source);
        RawPayloadBlock raw = document.PayloadMap.Blocks.First(item => item.Kind == RawPayloadKind.RawImage);
        RawPayloadBlock authenticity = new RawPayloadBlock(RawPayloadKind.Authenticity, "IFD0/CanonAuthenticity", raw.Offset, raw.Length, -1, -1, -1, raw.Compression, false, true);
        document.PayloadMap.Blocks = document.PayloadMap.Blocks.Concat(new RawPayloadBlock[] { authenticity }).ToArray();
        RawMetadataPatchEdit edit = new RawMetadataPatchEdit("IFD0", 305, codec.CreateAsciiTagValue("raw-patch"));
        RawWriteOptions blockedOptions = PatchOptions(false);

        RawWritePlan blocked = writer.PlanMetadataPatch(document, new RawMetadataPatchEdit[] { edit }, blockedOptions);

        Assert.Multiple(() =>
        {
            Assert.That(blocked.CanWrite, Is.False);
            Assert.That(blocked.AuthenticityInvalidated, Is.True);
            Assert.That(blocked.AuthenticityBlocks, Has.Length.EqualTo(1));
            Assert.That(blocked.Changes, Is.All.Matches<RawWriteChange>(item => item.RequiresConsent));
            Assert.That(blocked.Diagnostics.Any(item => item.Code == RawDiagnosticCode.SignatureInvalidated && item.Severity == RawSeverity.Warning), Is.True);
            Assert.That(blocked.Diagnostics.Any(item => item.Code == RawDiagnosticCode.DataLossConsentRequired && item.Severity == RawSeverity.Error), Is.True);
            Assert.Throws<RawWriteException>(() => writer.ApplyMetadataPatch(document, new RawMetadataPatchEdit[] { edit }, blockedOptions));
        });

        RawWriteOptions allowedOptions = PatchOptions(true);
        RawWritePlan allowed = writer.PlanMetadataPatch(document, new RawMetadataPatchEdit[] { edit }, allowedOptions);
        byte[] patched = writer.ApplyMetadataPatch(document, new RawMetadataPatchEdit[] { edit }, allowedOptions);
        RawDocument reopened = Open(patched);

        Assert.Multiple(() =>
        {
            Assert.That(allowed.CanWrite, Is.True);
            Assert.That(allowed.AuthenticityInvalidated, Is.True);
            Assert.That(source.AsSpan((int)authenticity.Offset, (int)authenticity.Length).ToArray(), Is.EqualTo(patched.AsSpan((int)authenticity.Offset, (int)authenticity.Length).ToArray()));
            Assert.That(RawPayload(codec, reopened), Is.EqualTo(RawPayload(codec, document)));
            Assert.That(codec.GetAsciiTagText((codec.FindMetadataTagByPathAndId(reopened.Metadata, "IFD0", 305) ?? throw new InvalidOperationException()).Value).Text, Is.EqualTo("raw-patch"));
        });
    }

    [Test]
    public void PublicPlannerRejectsSizeTypeByteOrderStorageOffsetBaseAndMakerNoteChanges()
    {
        IRawWriter writer = new RawWriterService();
        IRawCodec codec = new RawCodecService();
        RawWriteOptions options = PatchOptions(true);
        RawDocument tiff = Open(TiffBytes());
        RawMetadataPatchEdit wrongCount = new RawMetadataPatchEdit("IFD0", 274, codec.CreateUnsignedShortTagValue(new ushort[] { 1, 2 }, RawByteOrder.LittleEndian));
        RawMetadataPatchEdit wrongType = new RawMetadataPatchEdit("IFD0", 274, codec.CreateByteTagValue(new byte[] { 1, 2 }));
        RawMetadataPatchEdit wrongOrder = new RawMetadataPatchEdit("IFD0", 274, codec.CreateUnsignedShortTagValue(new ushort[] { 6 }, RawByteOrder.BigEndian));
        RawMetadataPatchEdit storageOffset = new RawMetadataPatchEdit("IFD0", 273, codec.CreateUnsignedLongTagValue(new uint[] { 1 }, RawByteOrder.LittleEndian));
        RawDocument badBase = Open(TiffBytes());
        (badBase.TiffGraph ?? throw new InvalidOperationException()).Header.BaseOffset++;
        RawMetadataPatchEdit orientation = new RawMetadataPatchEdit("IFD0", 274, codec.CreateUnsignedShortTagValue(new ushort[] { 6 }, RawByteOrder.LittleEndian));
        RawDocument cr2 = Open(Cr2Encoder.Encode(Cr2EncoderTests.Input(8, 4, 8), Cr2EncoderTests.SmallProfile(), Cr2EncoderTests.Options()));
        RawTagValue makerValue = (codec.FindMetadataTagByPathAndId(cr2.Metadata, "IFD0/Exif", 37500) ?? throw new InvalidOperationException()).Value;
        byte[] makerBytes = codec.CopyTagValueBytes(makerValue);
        makerBytes[0] ^= 1;
        RawMetadataPatchEdit makerNote = new RawMetadataPatchEdit("IFD0/Exif", 37500, new RawTagValue(makerValue.Type, makerValue.Count, makerValue.ByteOrder, makerBytes));

        Assert.Multiple(() =>
        {
            Assert.Throws<RawWriteException>(() => writer.PlanMetadataPatch(tiff, new RawMetadataPatchEdit[] { wrongCount }, options));
            Assert.Throws<RawWriteException>(() => writer.PlanMetadataPatch(tiff, new RawMetadataPatchEdit[] { wrongType }, options));
            Assert.Throws<RawWriteException>(() => writer.PlanMetadataPatch(tiff, new RawMetadataPatchEdit[] { wrongOrder }, options));
            Assert.Throws<RawWriteException>(() => writer.PlanMetadataPatch(tiff, new RawMetadataPatchEdit[] { storageOffset }, options));
            Assert.Throws<RawWriteException>(() => writer.PlanMetadataPatch(badBase, new RawMetadataPatchEdit[] { orientation }, options));
            Assert.Throws<RawWriteException>(() => writer.PlanMetadataPatch(cr2, new RawMetadataPatchEdit[] { makerNote }, options));
        });
    }

    [Test]
    public void PublicPlannerRejectsAliasedPatchStorageAndDuplicateTargets()
    {
        IRawWriter writer = new RawWriterService();
        IRawCodec codec = new RawCodecService();
        RawDocument aliased = Open(TiffBytes());
        TiffIfd root = (aliased.TiffGraph ?? throw new InvalidOperationException()).FindIfd("IFD0") ?? throw new InvalidOperationException();
        TiffEntry first = root.FindEntry(65000) ?? throw new InvalidOperationException();
        TiffEntry second = root.FindEntry(65001) ?? throw new InvalidOperationException();
        second.ValueOffset = first.ValueOffset;
        second.RawBytes = first.RawBytes.ToArray();
        RawMetadataPatchEdit firstEdit = new RawMetadataPatchEdit("IFD0", 65000, codec.CreateUndefinedTagValue(new byte[] { 11, 12, 13, 14, 15, 16 }));
        RawMetadataPatchEdit secondEdit = new RawMetadataPatchEdit("IFD0", 65001, codec.CreateUndefinedTagValue(new byte[] { 21, 22, 23, 24, 25, 26 }));
        RawDocument duplicate = Open(TiffBytes());
        RawMetadataPatchEdit orientation = new RawMetadataPatchEdit("IFD0", 274, codec.CreateUnsignedShortTagValue(new ushort[] { 6 }, RawByteOrder.LittleEndian));

        Assert.Multiple(() =>
        {
            Assert.Throws<RawWriteException>(() => writer.PlanMetadataPatch(aliased, new RawMetadataPatchEdit[] { firstEdit, secondEdit }, PatchOptions(true)));
            Assert.Throws<RawWriteException>(() => writer.PlanMetadataPatch(duplicate, new RawMetadataPatchEdit[] { orientation, orientation }, PatchOptions(true)));
        });
    }

    static RawWriteOptions PatchOptions(bool consent)
    {
        RawWriteOptions options = new RawWriteOptions();
        options.Mode = RawWriteMode.MetadataPatch;
        options.DataLossConsent = consent;
        return options;
    }

    static RawDocument Open(byte[] source)
    {
        IRawCodec codec = new RawCodecService();
        RawOpenOptions options = new RawOpenOptions();
        options.AllowExperimentalProfiles = true;
        options.ReadMetadata = true;
        options.ReadPreviews = true;
        options.ReadRawPayload = true;
        return codec.OpenBytes(source, null, options);
    }

    static byte[] TiffBytes()
    {
        RawByteOrder order = RawByteOrder.LittleEndian;
        TiffLayoutReference[] reference = new TiffLayoutReference[] { new TiffLayoutReference(0, TiffLayoutReferenceKind.Block, "strip") };
        TiffLayoutIfdInput root = new TiffLayoutIfdInput("IFD0", new TiffLayoutEntryInput[]
        {
            LongEntry(256, 2, order, null),
            LongEntry(257, 2, order, null),
            ShortEntry(258, 8, order),
            ShortEntry(259, 1, order),
            ShortEntry(262, 1, order),
            LongEntry(273, 0, order, reference),
            ShortEntry(274, 1, order),
            ShortEntry(277, 1, order),
            LongEntry(278, 2, order, null),
            LongEntry(279, 4, order, null),
            ShortEntry(284, 1, order),
            new TiffLayoutEntryInput(65000, TiffFieldType.Undefined, 6, new byte[] { 1, 2, 3, 4, 5, 6 }, null),
            new TiffLayoutEntryInput(65001, TiffFieldType.Undefined, 6, new byte[] { 7, 8, 9, 10, 11, 12 }, null)
        }, null);
        TiffLayoutBlockInput strip = new TiffLayoutBlockInput("strip", TiffLayoutBlockKind.Strip, 4, new byte[] { 20, 40, 60, 80 });
        TiffLayoutRequest request = new TiffLayoutRequest(false, order, "IFD0", new TiffLayoutIfdInput[] { root }, new TiffLayoutBlockInput[] { strip });
        TiffLayoutPlan plan = TiffLayoutPlanner.Plan(request, new RawLimits(), CancellationToken.None);
        return TiffWriter.WriteToArray(plan, new RawLimits(), CancellationToken.None);
    }

    static byte[] NefBytes()
    {
        RawByteOrder order = RawByteOrder.LittleEndian;
        TiffLayoutReference[] reference = new TiffLayoutReference[] { new TiffLayoutReference(0, TiffLayoutReferenceKind.Block, "raw") };
        TiffLayoutIfdInput root = new TiffLayoutIfdInput("IFD0", new TiffLayoutEntryInput[]
        {
            LongEntry(256, 2, order, null),
            LongEntry(257, 2, order, null),
            ShortEntry(258, 8, order),
            ShortEntry(259, 1, order),
            ShortEntry(262, 32803, order),
            LongEntry(273, 0, order, reference),
            ShortEntry(274, 1, order),
            ShortEntry(277, 1, order),
            LongEntry(278, 2, order, null),
            LongEntry(279, 4, order, null),
            ShortEntry(284, 1, order),
            new TiffLayoutEntryInput(271, TiffFieldType.Ascii, 18, System.Text.Encoding.ASCII.GetBytes("NIKON CORPORATION\0"), null),
            new TiffLayoutEntryInput(272, TiffFieldType.Ascii, 10, System.Text.Encoding.ASCII.GetBytes("NIKON Z 8\0"), null),
            new TiffLayoutEntryInput(33421, TiffFieldType.Short, 2, new byte[] { 2, 0, 2, 0 }, null),
            new TiffLayoutEntryInput(33422, TiffFieldType.Byte, 4, new byte[] { 0, 1, 1, 2 }, null)
        }, null);
        TiffLayoutBlockInput raw = new TiffLayoutBlockInput("raw", TiffLayoutBlockKind.Strip, 4, new byte[] { 20, 40, 60, 80 });
        TiffLayoutRequest request = new TiffLayoutRequest(false, order, "IFD0", new TiffLayoutIfdInput[] { root }, new TiffLayoutBlockInput[] { raw });
        TiffLayoutPlan plan = TiffLayoutPlanner.Plan(request, new RawLimits(), CancellationToken.None);
        return TiffWriter.WriteToArray(plan, new RawLimits(), CancellationToken.None);
    }

    static TiffLayoutEntryInput ShortEntry(ushort tag, ushort value, RawByteOrder order)
    {
        byte[] bytes = new byte[2];
        EndianWriter.WriteUInt16(bytes, value, order);
        return new TiffLayoutEntryInput(tag, TiffFieldType.Short, 1, bytes, null);
    }

    static TiffLayoutEntryInput LongEntry(ushort tag, uint value, RawByteOrder order, TiffLayoutReference[]? references)
    {
        byte[] bytes = new byte[4];
        EndianWriter.WriteUInt32(bytes, value, order);
        return new TiffLayoutEntryInput(tag, TiffFieldType.Long, 1, bytes, references);
    }

    static void AssertPatch(byte[] source, byte[] patched, RawWritePlan plan)
    {
        Assert.That(plan.CanWrite, Is.True);
        Assert.That(plan.PredictedSize, Is.EqualTo(source.Length));
        Assert.That(patched.Length, Is.EqualTo(source.Length));
        Assert.That(plan.Changes, Is.Not.Empty);
        int index = 0;
        while (index < source.Length)
        {
            if (source[index] != patched[index])
            {
                Assert.That(plan.Changes.Any(item => index >= item.PlannedOffset && index < item.PlannedOffset + item.PlannedLength), Is.True);
            }
            index++;
        }
    }

    static byte[] RawPayload(IRawCodec codec, RawDocument document)
    {
        List<byte> bytes = new List<byte>();
        RawPayloadBlock[] blocks = document.PayloadMap.Blocks.Where(item => item.Kind == RawPayloadKind.RawImage).OrderBy(item => item.Offset).ToArray();
        int index = 0;
        while (index < blocks.Length)
        {
            bytes.AddRange(codec.ExtractDocumentPayload(document, blocks[index], new RawLimits(), CancellationToken.None));
            index++;
        }
        return bytes.ToArray();
    }
}
