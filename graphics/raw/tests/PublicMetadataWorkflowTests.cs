using raw;
using System.Buffers.Binary;
using System.Text;

namespace raw.Tests;

public class PublicMetadataWorkflowTests
{
    [TestCase(false, RawMetadataPrecedence.None, RawMetadataSource.None, null)]
    [TestCase(true, RawMetadataPrecedence.Exif, RawMetadataSource.Exif, "Exif copyright")]
    [TestCase(true, RawMetadataPrecedence.Xmp, RawMetadataSource.Xmp, "XMP copyright")]
    [TestCase(true, RawMetadataPrecedence.Iptc, RawMetadataSource.Iptc, "IPTC copyright")]
    public void ReconciliationReportsConflictsAndHonorsEveryPrecedence(bool reconcile, RawMetadataPrecedence precedence, RawMetadataSource expectedSource, string? expectedValue)
    {
        IRawCodec codec = new RawCodecService();
        RawMetadata metadata = ConflictingCopyrightMetadata(codec);
        RawMetadataOptions options = new RawMetadataOptions();
        options.Reconcile = reconcile;
        options.Precedence = precedence;

        codec.ReconcileMetadata(metadata, options);

        RawMetadataConflict conflict = metadata.Conflicts.Single(item => item.Name == "Copyright");
        RawMetadataField? selected = codec.FindUnifiedMetadataField(metadata, "Copyright", null);
        Assert.Multiple(() =>
        {
            Assert.That(conflict.Candidates.Select(item => item.Source), Is.EqualTo(new RawMetadataSource[] { RawMetadataSource.Exif, RawMetadataSource.Xmp, RawMetadataSource.Iptc }));
            Assert.That(conflict.Resolved, Is.EqualTo(reconcile));
            Assert.That(conflict.SelectedSource, Is.EqualTo(expectedSource));
            Assert.That(selected?.Source ?? RawMetadataSource.None, Is.EqualTo(expectedSource));
            Assert.That(selected?.Values.SingleOrDefault(), Is.EqualTo(expectedValue));
        });
    }

    [Test]
    public void ReconciliationAndUnifiedLookupUseExplicitPrecedence()
    {
        IRawCodec codec = new RawCodecService();
        string xml = "<x:xmpmeta xmlns:x='adobe:ns:meta/'><rdf:RDF xmlns:rdf='http://www.w3.org/1999/02/22-rdf-syntax-ns#'><rdf:Description rdf:about='' xmlns:tiff='http://ns.adobe.com/tiff/1.0/' tiff:Make='XMP Camera'/></rdf:RDF></x:xmpmeta>";
        RawMetadata metadata = new RawMetadata(null);
        metadata.Make = "Exif Camera";
        metadata.XmpPackets = new RawXmpPacket[] { codec.ParseXmpPacket(Encoding.UTF8.GetBytes(xml), new RawLimits()) };
        RawMetadataOptions options = new RawMetadataOptions();
        options.Reconcile = true;
        options.Precedence = RawMetadataPrecedence.Exif;

        codec.ReconcileMetadata(metadata, options);
        RawMetadataField exif = codec.FindUnifiedMetadataField(metadata, "Make", null) ?? throw new InvalidOperationException();
        options.Precedence = RawMetadataPrecedence.Xmp;
        codec.ReconcileMetadata(metadata, options);
        RawMetadataField xmp = codec.FindUnifiedMetadataField(metadata, "Make", null) ?? throw new InvalidOperationException();

        Assert.Multiple(() =>
        {
            Assert.That(exif.Values, Is.EqualTo(new string[] { "Exif Camera" }));
            Assert.That(exif.Source, Is.EqualTo(RawMetadataSource.Exif));
            Assert.That(xmp.Values, Is.EqualTo(new string[] { "XMP Camera" }));
            Assert.That(xmp.Source, Is.EqualTo(RawMetadataSource.Xmp));
            Assert.That(metadata.Conflicts.Single().Resolved, Is.True);
            Assert.That(codec.FindUnifiedMetadataField(metadata, "Missing", null), Is.Null);
        });
    }

    [Test]
    public void PublicEditWorkflowDryRunsThenAppliesScrubGpsAndTimestampChangesAtomically()
    {
        IRawCodec codec = new RawCodecService();
        RawMetadata metadata = new RawMetadata(null);
        metadata.SourceFields = new RawMetadataField[]
        {
            Field("GPSLatitude", "37.5", RawMetadataSource.Exif),
            Field("GPSLongitude", "-122.3", RawMetadataSource.Xmp),
            Field("GPSDateStamp", "2026:08:26", RawMetadataSource.Exif),
            Field("GPSTimeStamp", "17:00:00Z", RawMetadataSource.Exif),
            Field("DateCreated", "2026-08-26T10:00:00Z", RawMetadataSource.Exif),
            Field("CreateDate", "2026-08-26T10:00:00Z", RawMetadataSource.Xmp),
            Field("ModifyDate", "2026-08-26T11:00:00Z", RawMetadataSource.Xmp),
            Field("Creator", "Reporter", RawMetadataSource.Iptc),
            Field("Headline", "Old headline", RawMetadataSource.Xmp),
            Field("Copyright", "Remove directly", RawMetadataSource.Exif),
            Field("Make", "Public camera", RawMetadataSource.Exif)
        };
        metadata.UnifiedFields = metadata.SourceFields.ToArray();
        RawMetadataEditSet edits = codec.CreateMetadataEditSet();
        codec.ReplaceMetadataFieldEdit(edits, "Headline", new string[] { "New headline" }, null, RawMetadataSource.Xmp);
        codec.AddMetadataFieldEdit(edits, "Category", new string[] { "News" }, null, RawMetadataSource.Xmp);
        codec.RemoveMetadataFieldEdit(edits, "Copyright", null, RawMetadataSource.Exif);
        codec.AddGpsRemovalEdits(edits);
        codec.AddTimestampRemovalEdits(edits);
        codec.AddPrivacyScrubPreset(edits, RawPrivacyScrubPreset.Personal);

        RawMetadataEditReport dryRun = codec.DryRunMetadataEdits(metadata, edits);

        Assert.Multiple(() =>
        {
            Assert.That(codec.MetadataEditSucceeded(dryRun), Is.True);
            Assert.That(dryRun.DryRun, Is.True);
            Assert.That(dryRun.Applied, Is.False);
            Assert.That(codec.FindUnifiedMetadataField(metadata, "Headline", null)?.Values, Is.EqualTo(new string[] { "Old headline" }));
            Assert.That(metadata.PendingEdits, Is.Empty);
        });

        RawMetadataEditReport applied = codec.ApplyMetadataEdits(metadata, edits);

        Assert.Multiple(() =>
        {
            Assert.That(codec.MetadataEditSucceeded(applied), Is.True);
            Assert.That(applied.Applied, Is.True);
            Assert.That(codec.FindUnifiedMetadataField(metadata, "Headline", null)?.Values, Is.EqualTo(new string[] { "New headline" }));
            Assert.That(codec.FindUnifiedMetadataField(metadata, "Category", null)?.Values, Is.EqualTo(new string[] { "News" }));
            Assert.That(codec.FindUnifiedMetadataField(metadata, "GPSLatitude", null), Is.Null);
            Assert.That(codec.FindUnifiedMetadataField(metadata, "GPSLongitude", null), Is.Null);
            Assert.That(codec.FindUnifiedMetadataField(metadata, "GPSDateStamp", null), Is.Null);
            Assert.That(codec.FindUnifiedMetadataField(metadata, "GPSTimeStamp", null), Is.Null);
            Assert.That(codec.FindUnifiedMetadataField(metadata, "DateCreated", null), Is.Null);
            Assert.That(codec.FindUnifiedMetadataField(metadata, "CreateDate", null), Is.Null);
            Assert.That(codec.FindUnifiedMetadataField(metadata, "ModifyDate", null), Is.Null);
            Assert.That(codec.FindUnifiedMetadataField(metadata, "Creator", null), Is.Null);
            Assert.That(codec.FindUnifiedMetadataField(metadata, "Copyright", null), Is.Null);
            Assert.That(codec.FindUnifiedMetadataField(metadata, "Make", null)?.Values, Is.EqualTo(new string[] { "Public camera" }));
            Assert.That(metadata.PendingEdits, Has.Length.GreaterThan(applied.Changes.Length));
        });
    }

    private static RawMetadata ConflictingCopyrightMetadata(IRawCodec codec)
    {
        RawTag copyright = new RawTag(33432, "Copyright", "IFD0", 20, true, codec.CreateAsciiTagValue("Exif copyright"));
        RawMetadata metadata = new RawMetadata(new RawIfd[] { new RawIfd("IFD0", RawIfdKind.Image, 8, new RawTag[] { copyright }) });
        string xml = "<x:xmpmeta xmlns:x='adobe:ns:meta/'><rdf:RDF xmlns:rdf='http://www.w3.org/1999/02/22-rdf-syntax-ns#'><rdf:Description rdf:about='' xmlns:dc='http://purl.org/dc/elements/1.1/'><dc:rights><rdf:Alt><rdf:li xml:lang='x-default'>XMP copyright</rdf:li></rdf:Alt></dc:rights></rdf:Description></rdf:RDF></x:xmpmeta>";
        metadata.XmpPackets = new RawXmpPacket[] { codec.ParseXmpPacket(Encoding.UTF8.GetBytes(xml), new RawLimits()) };
        RawIptcDataset iptcCopyright = codec.CreateTextIptcDataset(2, 116, "IPTC copyright", false);
        metadata.IptcRecords = new RawIptcRecord[] { codec.CreateIptcRecord(new RawIptcDataset[] { iptcCopyright }, new RawLimits()) };
        return metadata;
    }

    [Test]
    public void PreviewReplacementHasEquivalentDryRunAndAppliedDocumentResults()
    {
        IRawCodec codec = new RawCodecService();
        RawThumbnail thumbnail = new RawThumbnail(0, RawThumbnailKind.Rgb8, 2, 1, RawOrientation.Normal, "image/x-rgb", new byte[] { 1, 2, 3, 4, 5, 6 }, null, null);
        RawDocument document = new RawDocument(RawFormat.Tiff, string.Empty, null, Array.Empty<RawImageInfo>(), new RawThumbnail[] { thumbnail }, new RawPayloadMap(0, null), new RawCapabilities(true, true, false, false, true, false, false), null, true, 0, true, false);
        RawMetadataEditSet edits = codec.CreateMetadataEditSet();
        codec.AddPreviewReplacementEdit(edits, 0, RawThumbnailKind.Gray8, 3, 2, RawOrientation.Rotate90Clockwise, "image/x-gray", new byte[] { 9, 8, 7, 6, 5, 4 });

        RawMetadataEditReport dryRun = codec.DryRunDocumentMetadataEdits(document, edits);

        Assert.Multiple(() =>
        {
            Assert.That(codec.MetadataEditSucceeded(dryRun), Is.True);
            Assert.That(dryRun.Changes.Single().Kind, Is.EqualTo(RawMetadataEditKind.PreviewUpdate));
            Assert.That(thumbnail.Width, Is.EqualTo(2));
            Assert.That(document.PendingPreviewEdits, Is.Empty);
        });

        RawMetadataEditReport applied = codec.ApplyDocumentMetadataEdits(document, edits);

        Assert.Multiple(() =>
        {
            Assert.That(codec.MetadataEditSucceeded(applied), Is.True);
            Assert.That(thumbnail.Kind, Is.EqualTo(RawThumbnailKind.Gray8));
            Assert.That(thumbnail.Width, Is.EqualTo(3));
            Assert.That(thumbnail.Height, Is.EqualTo(2));
            Assert.That(thumbnail.Orientation, Is.EqualTo(RawOrientation.Rotate90Clockwise));
            Assert.That(thumbnail.EncodedBytes, Is.EqualTo(new byte[] { 9, 8, 7, 6, 5, 4 }));
            Assert.That(document.PendingPreviewEdits, Has.Length.EqualTo(1));
        });
    }

    [Test]
    public void CanonMakerNoteLookupEditAndRelocationPreserveUnknownBytes()
    {
        IRawCodec codec = new RawCodecService();
        RawLimits limits = new RawLimits();
        RawMakerNote original = codec.ParseMakerNote(CreateCanonNote(), 100, 42, "IFD0/Exif", "Canon", 0, RawByteOrder.LittleEndian, limits);
        RawCanonMakerNoteTag firmware = codec.FindCanonMakerNoteTag(original, 0x0007) ?? throw new InvalidOperationException();
        RawCanonMakerNoteTag fileNumber = codec.FindCanonMakerNoteTag(original, 0x0008) ?? throw new InvalidOperationException();
        RawCanonMakerNoteTag unknown = codec.FindCanonMakerNoteTag(original, 0xBEEF) ?? throw new InvalidOperationException();
        RawCanonMakerNoteEdit firmwareEdit = codec.CreateCanonMakerNoteEdit(0x0007, RawCanonMakerNoteVersion.Baseline, codec.CreateAsciiTagValue("Firmware 2.0"));
        RawCanonMakerNoteEdit numberEdit = codec.CreateCanonMakerNoteEdit(0x0008, RawCanonMakerNoteVersion.Baseline, codec.CreateUnsignedLongTagValue(new uint[] { 7654321 }, RawByteOrder.BigEndian));
        RawMakerNote edited = codec.ApplyCanonMakerNoteEdits(original, new RawCanonMakerNoteEdit[] { firmwareEdit, numberEdit }, limits);
        RawMakerNoteWritePlan relocation = codec.PlanMakerNoteRelocation(edited, 200, limits);
        byte[] copied = codec.CopyMakerNoteBytes(original);
        copied[0] ^= 0xFF;

        Assert.Multiple(() =>
        {
            Assert.That(codec.ValidateMakerNote(original, limits).IsValid(), Is.True);
            Assert.That(codec.GetCanonMakerNoteTagText(firmware), Is.EqualTo("Firmware 1.0"));
            Assert.That(codec.GetCanonMakerNoteTagText(codec.FindCanonMakerNoteTag(edited, 0x0007) ?? throw new InvalidOperationException()), Is.EqualTo("Firmware 2.0"));
            Assert.That(codec.GetUnsignedLongTagValues(fileNumber.Value).Single(), Is.EqualTo(1234567));
            Assert.That(codec.GetUnsignedLongTagValues((codec.FindCanonMakerNoteTag(edited, 0x0008) ?? throw new InvalidOperationException()).Value).Single(), Is.EqualTo(7654321));
            Assert.That(codec.CopyCanonMakerNoteTagBytes(codec.FindCanonMakerNoteTag(edited, 0xBEEF) ?? throw new InvalidOperationException()), Is.EqualTo(codec.CopyCanonMakerNoteTagBytes(unknown)));
            Assert.That(original.RawBytes, Is.Not.EqualTo(copied));
            Assert.That(relocation.CanWrite, Is.True);
            Assert.That(relocation.Strategy, Is.EqualTo(RawMakerNoteWriteStrategy.PatchedRelocation));
        });
    }

    [Test]
    public void NikonMakerNoteLookupEditAndRelocationPreserveProtectedAndUnknownBytes()
    {
        IRawCodec codec = new RawCodecService();
        RawLimits limits = new RawLimits();
        RawMakerNote original = codec.ParseMakerNote(CreateNikonNote(), 500, 74, "IFD0/Exif", "NIKON CORPORATION", 0, RawByteOrder.LittleEndian, limits);
        RawNikonMakerNoteTag whiteBalance = codec.FindNikonMakerNoteTag(original, 0x0005) ?? throw new InvalidOperationException();
        RawNikonMakerNoteTag version = codec.FindNikonMakerNoteTag(original, 0x0001) ?? throw new InvalidOperationException();
        RawNikonMakerNoteTag unknown = codec.FindNikonMakerNoteTag(original, 0xBEEF) ?? throw new InvalidOperationException();
        RawNikonMakerNoteEdit edit = codec.CreateNikonMakerNoteEdit(0x0005, RawNikonMakerNoteVersion.Baseline, codec.CreateAsciiTagValue("Cloudy"));
        RawMakerNote edited = codec.ApplyNikonMakerNoteEdits(original, new RawNikonMakerNoteEdit[] { edit }, limits);
        RawMakerNoteWritePlan relocation = codec.PlanMakerNoteRelocation(edited, 900, limits);

        Assert.Multiple(() =>
        {
            Assert.That(codec.GetNikonMakerNoteTagText(whiteBalance), Is.EqualTo("Auto"));
            Assert.That(codec.GetNikonMakerNoteTagText(codec.FindNikonMakerNoteTag(edited, 0x0005) ?? throw new InvalidOperationException()), Is.EqualTo("Cloudy"));
            Assert.That(version.Protected, Is.True);
            Assert.That(codec.CopyNikonMakerNoteTagBytes(codec.FindNikonMakerNoteTag(edited, 0x0001) ?? throw new InvalidOperationException()), Is.EqualTo(codec.CopyNikonMakerNoteTagBytes(version)));
            Assert.That(codec.CopyNikonMakerNoteTagBytes(codec.FindNikonMakerNoteTag(edited, 0xBEEF) ?? throw new InvalidOperationException()), Is.EqualTo(codec.CopyNikonMakerNoteTagBytes(unknown)));
            Assert.That(relocation.CanWrite, Is.True);
            Assert.That(relocation.Strategy, Is.EqualTo(RawMakerNoteWriteStrategy.RelativeRelocation));
            Assert.That(relocation.Bytes, Is.EqualTo(edited.RawBytes));
        });
    }

    private static RawMetadataField Field(string name, string value, RawMetadataSource source)
    {
        return new RawMetadataField(name, new string[] { value }, null, source, -1, "Test", -1);
    }

    private static byte[] CreateCanonNote()
    {
        const int count = 3;
        const int noteOffset = 100;
        int dataPosition = 2 + count * 12 + 4;
        int firmwarePosition = dataPosition;
        int unknownPosition = firmwarePosition + 16;
        byte[] bytes = new byte[unknownPosition + 6];
        WriteUInt16(bytes, 0, count);
        WriteEntry(bytes, 2, 0x0007, RawTagType.Ascii, 16, (uint)(noteOffset + firmwarePosition));
        WriteEntry(bytes, 14, 0x0008, RawTagType.Long, 1, 1234567);
        WriteEntry(bytes, 26, 0xBEEF, RawTagType.Undefined, 6, (uint)(noteOffset + unknownPosition));
        WriteUInt32(bytes, 38, 0);
        WriteFixedAscii(bytes, firmwarePosition, 16, "Firmware 1.0");
        new byte[] { 91, 92, 93, 94, 95, 96 }.CopyTo(bytes, unknownPosition);
        return bytes;
    }

    private static byte[] CreateNikonNote()
    {
        const int count = 3;
        int dataPosition = 18 + 2 + count * 12 + 4;
        int whiteBalancePosition = dataPosition;
        int unknownPosition = whiteBalancePosition + 16;
        byte[] bytes = new byte[unknownPosition + 6];
        Encoding.ASCII.GetBytes("Nikon\0").CopyTo(bytes, 0);
        bytes[6] = 2;
        bytes[10] = (byte)'I';
        bytes[11] = (byte)'I';
        WriteUInt16(bytes, 12, 42);
        WriteUInt32(bytes, 14, 8);
        WriteUInt16(bytes, 18, count);
        WriteEntry(bytes, 20, 0x0001, RawTagType.Undefined, 4, 0);
        Encoding.ASCII.GetBytes("0200").CopyTo(bytes, 28);
        WriteEntry(bytes, 32, 0x0005, RawTagType.Ascii, 16, (uint)(whiteBalancePosition - 10));
        WriteEntry(bytes, 44, 0xBEEF, RawTagType.Undefined, 6, (uint)(unknownPosition - 10));
        WriteUInt32(bytes, 56, 0);
        WriteFixedAscii(bytes, whiteBalancePosition, 16, "Auto");
        new byte[] { 71, 72, 73, 74, 75, 76 }.CopyTo(bytes, unknownPosition);
        return bytes;
    }

    private static void WriteEntry(byte[] bytes, int position, ushort tag, RawTagType type, uint count, uint value)
    {
        WriteUInt16(bytes, position, tag);
        WriteUInt16(bytes, position + 2, (ushort)type);
        WriteUInt32(bytes, position + 4, count);
        WriteUInt32(bytes, position + 8, value);
    }

    private static void WriteFixedAscii(byte[] bytes, int position, int length, string value)
    {
        byte[] encoded = Encoding.ASCII.GetBytes(value);
        encoded.CopyTo(bytes, position);
        bytes[position + encoded.Length] = 0;
        for (int index = encoded.Length + 1; index < length; index++)
        {
            bytes[position + index] = (byte)(index % 2 == 0 ? 0 : 0x20);
        }
    }

    private static void WriteUInt16(byte[] bytes, int offset, ushort value)
    {
        BinaryPrimitives.WriteUInt16LittleEndian(bytes.AsSpan(offset, 2), value);
    }

    private static void WriteUInt32(byte[] bytes, int offset, uint value)
    {
        BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(offset, 4), value);
    }
}
