using raw;
using System.Text;

namespace raw.Tests;

public class MetadataEditTests
{
    const string Rdf = "http://www.w3.org/1999/02/22-rdf-syntax-ns#";
    const string Photoshop = "http://ns.adobe.com/photoshop/1.0/";

    [Test]
    public void DryRunAndApplyUseTheSameOrderedAddReplaceRemovePlan()
    {
        RawMetadata metadata = CreateParallelMetadata();
        byte[] xmpBytes = metadata.XmpPackets[0].CopyBytes();
        byte[] iptcBytes = metadata.IptcRecords[0].CopyBytes();
        RawMetadataEditSet edits = new RawMetadataEditSet();
        edits.Replace("Headline", new string[] { "new headline" }, null, RawMetadataSource.None);
        edits.Remove("Title", null, RawMetadataSource.None);
        edits.Add("Credit", new string[] { "News desk" }, null, RawMetadataSource.Xmp);

        RawMetadataEditReport dryRun = metadata.ApplyEdits(edits, true);

        Assert.Multiple(() =>
        {
            Assert.That(dryRun.Succeeded(), Is.True);
            Assert.That(dryRun.DryRun, Is.True);
            Assert.That(dryRun.Applied, Is.False);
            Assert.That(dryRun.Changes, Has.Length.EqualTo(5));
            Assert.That(dryRun.Changes.All(change => !change.Applied), Is.True);
            Assert.That((metadata.Get("Headline", null) ?? throw new System.InvalidOperationException("A required value is unavailable.")).GetValue(), Is.EqualTo("XMP headline"));
            Assert.That(metadata.Get("Title", null), Is.Not.Null);
            Assert.That(metadata.Get("Credit", null), Is.Null);
            Assert.That(metadata.PendingEdits, Is.Empty);
        });

        RawMetadataEditReport applied = edits.ApplyToMetadata(metadata, false);
        RawMetadataOptions refreshedOptions = new RawMetadataOptions();
        refreshedOptions.Reconcile = true;
        refreshedOptions.Precedence = RawMetadataPrecedence.Xmp;
        metadata.Reconcile(refreshedOptions);

        Assert.Multiple(() =>
        {
            Assert.That(applied.Succeeded(), Is.True);
            Assert.That(applied.Applied, Is.True);
            Assert.That(applied.Changes.Select(change => change.Kind), Is.EqualTo(dryRun.Changes.Select(change => change.Kind)));
            Assert.That(applied.Changes.All(change => change.Applied), Is.True);
            Assert.That((metadata.Get("Headline", null) ?? throw new System.InvalidOperationException("A required value is unavailable.")).GetValue(), Is.EqualTo("new headline"));
            Assert.That((metadata.Get("Headline", null) ?? throw new System.InvalidOperationException("A required value is unavailable.")).Source, Is.EqualTo(RawMetadataSource.Xmp));
            Assert.That(metadata.Get("Title", null), Is.Null);
            Assert.That((metadata.Get("Credit", null) ?? throw new System.InvalidOperationException("A required value is unavailable.")).GetValue(), Is.EqualTo("News desk"));
            Assert.That(metadata.Conflicts, Is.Empty);
            Assert.That(metadata.PendingEdits, Has.Length.EqualTo(3));
            Assert.That(metadata.XmpPackets[0].CopyBytes(), Is.EqualTo(xmpBytes));
            Assert.That(metadata.IptcRecords[0].CopyBytes(), Is.EqualTo(iptcBytes));
        });
    }

    [Test]
    public void InvalidEditMakesTheWholeApplicationAtomic()
    {
        RawMetadata metadata = CreateParallelMetadata();
        RawMetadataEditSet edits = new RawMetadataEditSet();
        edits.Replace("Headline", new string[] { "would change" }, null, RawMetadataSource.Xmp);
        edits.Add("Headline", new string[] { "duplicate" }, null, RawMetadataSource.Xmp);

        RawMetadataEditReport report = edits.ApplyToMetadata(metadata, false);

        Assert.Multiple(() =>
        {
            Assert.That(report.Succeeded(), Is.False);
            Assert.That(report.Applied, Is.False);
            Assert.That(report.Diagnostics.Single().Code, Is.EqualTo(RawDiagnosticCode.InvalidMetadata));
            Assert.That((metadata.Get("Headline", null) ?? throw new System.InvalidOperationException("A required value is unavailable.")).GetValue(), Is.EqualTo("XMP headline"));
            Assert.That(metadata.PendingEdits, Is.Empty);
        });
    }

    [Test]
    public void PrivacyScrubExpandsToExplicitSourceChangesAndKeepsNonPrivateFields()
    {
        RawMetadata metadata = new RawMetadata(null);
        metadata.SourceFields = new RawMetadataField[]
        {
            Field("GPSLatitude", "37.5", RawMetadataSource.Exif),
            Field("City", "Seattle", RawMetadataSource.Xmp),
            Field("Creator", "Reporter", RawMetadataSource.Iptc),
            Field("Make", "Camera", RawMetadataSource.Exif),
            Field("DateCreated", "2026-08-19T10:00:00Z", RawMetadataSource.Xmp),
            Field("Headline", "Public headline", RawMetadataSource.Xmp)
        };
        metadata.UnifiedFields = metadata.SourceFields.ToArray();
        RawMetadataEditSet edits = new RawMetadataEditSet();
        edits.Scrub(RawPrivacyScrubPreset.All);

        RawMetadataEditReport report = edits.ApplyToMetadata(metadata, false);

        Assert.Multiple(() =>
        {
            Assert.That(report.Succeeded(), Is.True);
            Assert.That(report.Changes, Has.Length.EqualTo(5));
            Assert.That(report.Changes.All(change => change.Kind == RawMetadataEditKind.Remove), Is.True);
            Assert.That(metadata.Get("GPSLatitude", null), Is.Null);
            Assert.That(metadata.Get("City", null), Is.Null);
            Assert.That(metadata.Get("Creator", null), Is.Null);
            Assert.That(metadata.Get("Make", null), Is.Null);
            Assert.That(metadata.Get("DateCreated", null), Is.Null);
            Assert.That((metadata.Get("Headline", null) ?? throw new System.InvalidOperationException("A required value is unavailable.")).GetValue(), Is.EqualTo("Public headline"));
            Assert.That(metadata.PendingEdits.Length, Is.GreaterThan(report.Changes.Length));
        });
    }

    [Test]
    public void PreviewReplacementSynchronizesModelAndTiffMetadataAfterDryRun()
    {
        RawPayloadBlock payload = new RawPayloadBlock(RawPayloadKind.Preview, "IFD1/Strip0", 100, 6, 7, 0, 0, RawCompression.None, true, true);
        RawThumbnail thumbnail = new RawThumbnail(0, RawThumbnailKind.Rgb8, 2, 1, RawOrientation.Normal, "image/x-rgb", new byte[] { 1, 2, 3, 4, 5, 6 }, payload, null);
        RawImageInfo info = CreatePreviewInfo(payload);
        RawIfd ifd = new RawIfd("IFD1", RawIfdKind.Image, 20, new RawTag[]
        {
            Tag(256, "ImageWidth", 2),
            Tag(257, "ImageLength", 1),
            Tag(274, "Orientation", 1)
        });
        RawMetadata metadata = new RawMetadata(new RawIfd[] { ifd });
        RawDocument document = new RawDocument(RawFormat.Tiff, string.Empty, null, new RawImageInfo[] { info }, new RawThumbnail[] { thumbnail }, new RawPayloadMap(106, new RawPayloadBlock[] { payload }), new RawCapabilities(true, true, true, false, true, false, false), null, true, 1, true, false);
        document.Metadata = metadata;
        RawMetadataEditSet edits = new RawMetadataEditSet();
        byte[] replacement = new byte[] { 9, 8, 7, 6, 5, 4 };
        edits.ReplacePreview(0, RawThumbnailKind.Gray8, 3, 2, RawOrientation.Rotate90Clockwise, "image/x-gray", replacement);

        RawMetadataEditReport dryRun = document.EditMetadata(edits, true);

        Assert.Multiple(() =>
        {
            Assert.That(dryRun.Succeeded(), Is.True);
            Assert.That(dryRun.Changes.Single().Kind, Is.EqualTo(RawMetadataEditKind.PreviewUpdate));
            Assert.That(thumbnail.Width, Is.EqualTo(2));
            Assert.That((ifd.FindTag(256) ?? throw new System.InvalidOperationException("A required value is unavailable.")).Value.GetUInt16Values()[0], Is.EqualTo(2));
            Assert.That(document.PendingPreviewEdits, Is.Empty);
        });

        RawMetadataEditReport applied = edits.Apply(document, false);

        Assert.Multiple(() =>
        {
            Assert.That(applied.Succeeded(), Is.True);
            Assert.That(thumbnail.Width, Is.EqualTo(3));
            Assert.That(thumbnail.Height, Is.EqualTo(2));
            Assert.That(thumbnail.Orientation, Is.EqualTo(RawOrientation.Rotate90Clockwise));
            Assert.That(thumbnail.EncodedBytes, Is.EqualTo(replacement));
            Assert.That(thumbnail.Payload, Is.Null);
            Assert.That(info.Width, Is.EqualTo(3));
            Assert.That(info.Height, Is.EqualTo(2));
            Assert.That(info.Orientation, Is.EqualTo(RawOrientation.Rotate90Clockwise));
            Assert.That((ifd.FindTag(256) ?? throw new System.InvalidOperationException("A required value is unavailable.")).Value.GetUInt16Values()[0], Is.EqualTo(3));
            Assert.That((ifd.FindTag(257) ?? throw new System.InvalidOperationException("A required value is unavailable.")).Value.GetUInt16Values()[0], Is.EqualTo(2));
            Assert.That((ifd.FindTag(274) ?? throw new System.InvalidOperationException("A required value is unavailable.")).Value.GetUInt16Values()[0], Is.EqualTo(6));
            Assert.That((ifd.FindTag(258) ?? throw new System.InvalidOperationException("A required value is unavailable.")).Value.GetUInt16Values()[0], Is.EqualTo(8));
            Assert.That((ifd.FindTag(259) ?? throw new System.InvalidOperationException("A required value is unavailable.")).Value.GetUInt16Values()[0], Is.EqualTo(1));
            Assert.That((ifd.FindTag(262) ?? throw new System.InvalidOperationException("A required value is unavailable.")).Value.GetUInt16Values()[0], Is.EqualTo(1));
            Assert.That((ifd.FindTag(277) ?? throw new System.InvalidOperationException("A required value is unavailable.")).Value.GetUInt16Values()[0], Is.EqualTo(1));
            Assert.That(document.PendingPreviewEdits, Has.Length.EqualTo(1));
            Assert.That(thumbnail.Validate(new RawLimits()).IsValid(), Is.True);
        });
    }

    static RawMetadata CreateParallelMetadata()
    {
        string xml = "<x:xmpmeta xmlns:x='adobe:ns:meta/'><rdf:RDF xmlns:rdf='" + Rdf + "'><rdf:Description rdf:about='' xmlns:photoshop='" + Photoshop + "' xmlns:dc='http://purl.org/dc/elements/1.1/' photoshop:Headline='XMP headline'><dc:title><rdf:Alt><rdf:li xml:lang='x-default'>Shared title</rdf:li></rdf:Alt></dc:title></rdf:Description></rdf:RDF></x:xmpmeta>";
        RawIptcRecord iptc = RawIptcRecord.Create(new RawIptcDataset[]
        {
            RawIptcDataset.CreateUnsignedInteger(2, 0, 4),
            RawIptcDataset.CreateText(2, 105, "IPTC headline", false),
            RawIptcDataset.CreateText(2, 5, "Shared title", false)
        }, new RawLimits());
        RawMetadata metadata = new RawMetadata(null);
        metadata.XmpPackets = new RawXmpPacket[] { RawXmpPacket.Parse(Encoding.UTF8.GetBytes(xml), new RawLimits()) };
        metadata.IptcRecords = new RawIptcRecord[] { iptc };
        RawMetadataOptions options = new RawMetadataOptions();
        options.Reconcile = true;
        options.Precedence = RawMetadataPrecedence.Xmp;
        metadata.Reconcile(options);
        return metadata;
    }

    static RawMetadataField Field(string name, string value, RawMetadataSource source)
    {
        return new RawMetadataField(name, new string[] { value }, string.Empty, source, -1, "Test", -1);
    }

    static RawTag Tag(ushort id, string name, ushort value)
    {
        return new RawTag(id, name, "IFD1", 20 + id, true, RawTagValue.CreateUInt16(new ushort[] { value }, RawByteOrder.LittleEndian));
    }

    static RawImageInfo CreatePreviewInfo(RawPayloadBlock payload)
    {
        RawChannelDescription[] channels = new RawChannelDescription[]
        {
            new RawChannelDescription("R", RawChannelKind.Red),
            new RawChannelDescription("G", RawChannelKind.Green),
            new RawChannelDescription("B", RawChannelKind.Blue)
        };
        RawPlaneInfo plane = new RawPlaneInfo(0, "RGB", RawChannelKind.Unknown, 2, 1, 8, 6);
        return new RawImageInfo(7, RawImageKind.Preview, 2, 1, 8, RawSampleKind.UnsignedInteger, RawPhotometricInterpretation.Rgb, RawCompression.None, RawOrientation.Normal, channels, new RawPlaneInfo[] { plane }, null, new RawArea(RawAreaKind.StoredImage, 0, 0, 2, 1), null, null, payload);
    }
}
