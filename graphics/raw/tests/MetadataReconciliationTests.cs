using raw;
using System.Text;

namespace raw.Tests;

public class MetadataReconciliationTests
{
    const string Rdf = "http://www.w3.org/1999/02/22-rdf-syntax-ns#";
    const string Dc = "http://purl.org/dc/elements/1.1/";
    const string Photoshop = "http://ns.adobe.com/photoshop/1.0/";
    const string Tiff = "http://ns.adobe.com/tiff/1.0/";

    [Test]
    public void NoReconciliationReportsConflictsWithoutSelectingTheirValues()
    {
        byte[] xmp;
        byte[] iptc;
        byte[] tiff = BuildConflictingTiff(out xmp, out iptc);
        RawMetadataOptions options = new RawMetadataOptions();

        RawDocument document = RawCodec.OpenMetadata(new ReadOnlyMemory<byte>(tiff), null, options);
        RawMetadata metadata = document.Metadata;
        RawMetadataConflict make = metadata.Conflicts.Single(value => value.Name == "Make");
        RawMetadataConflict headline = metadata.Conflicts.Single(value => value.Name == "Headline");
        RawValidationOptions validation = new RawValidationOptions();
        validation.ValidateStructure = false;
        validation.ValidateCodec = false;
        validation.ValidateSemantic = false;
        validation.ValidateProfile = false;
        validation.ValidateMetadata = true;
        validation.ValidateWriter = false;
        RawValidationReport report = document.Validate(validation);

        Assert.Multiple(() =>
        {
            Assert.That(metadata.Reconciled, Is.False);
            Assert.That(metadata.Precedence, Is.EqualTo(RawMetadataPrecedence.None));
            Assert.That(metadata.Make, Is.EqualTo("Exif Camera"));
            Assert.That(metadata.Get("Make", null), Is.Null);
            Assert.That(metadata.Get("Headline", null), Is.Null);
            Assert.That((metadata.Get("Title", null) ?? throw new System.InvalidOperationException("A required value is unavailable.")).GetValue(), Is.EqualTo("Shared title"));
            Assert.That((metadata.Get("Keywords", null) ?? throw new System.InvalidOperationException("A required value is unavailable.")).Values, Is.EqualTo(new string[] { "raw", "photo" }));
            Assert.That(make.Resolved, Is.False);
            Assert.That(make.SelectedSource, Is.EqualTo(RawMetadataSource.None));
            Assert.That(make.Candidates.Select(value => value.Source), Is.EqualTo(new RawMetadataSource[] { RawMetadataSource.Exif, RawMetadataSource.Xmp }));
            Assert.That(headline.Candidates.Select(value => value.Source), Is.EqualTo(new RawMetadataSource[] { RawMetadataSource.Xmp, RawMetadataSource.Iptc }));
            Assert.That(((report.Messages ?? throw new System.InvalidOperationException("A required value is unavailable.")).Count(value => value.Code == RawDiagnosticCode.MetadataConflict)), Is.EqualTo(2));
            Assert.That(report.Messages.All(value => value.Severity == RawSeverity.Warning), Is.True);
            Assert.That(metadata.XmpPackets.Single().CopyBytes(), Is.EqualTo(xmp));
            Assert.That(metadata.IptcRecords.Single().CopyBytes(), Is.EqualTo(iptc));
        });
    }

    [Test]
    public void ExplicitPrecedenceResolvesEveryConflictWithDocumentedFallbackOrder()
    {
        byte[] xmp;
        byte[] iptc;
        byte[] tiff = BuildConflictingTiff(out xmp, out iptc);
        RawMetadataOptions options = new RawMetadataOptions();
        options.Reconcile = true;
        options.Precedence = RawMetadataPrecedence.Iptc;

        RawMetadata metadata = RawCodec.OpenMetadata(new ReadOnlyMemory<byte>(tiff), null, options).Metadata;

        Assert.Multiple(() =>
        {
            Assert.That(metadata.Reconciled, Is.True);
            Assert.That((metadata.Get("Headline", null) ?? throw new System.InvalidOperationException("A required value is unavailable.")).GetValue(), Is.EqualTo("IPTC headline"));
            Assert.That((metadata.Get("Headline", null) ?? throw new System.InvalidOperationException("A required value is unavailable.")).Source, Is.EqualTo(RawMetadataSource.Iptc));
            Assert.That((metadata.Get("Make", null) ?? throw new System.InvalidOperationException("A required value is unavailable.")).GetValue(), Is.EqualTo("XMP Camera"));
            Assert.That((metadata.Get("Make", null) ?? throw new System.InvalidOperationException("A required value is unavailable.")).Source, Is.EqualTo(RawMetadataSource.Xmp));
            Assert.That(metadata.Conflicts.All(value => value.Resolved), Is.True);
        });

        options.Precedence = RawMetadataPrecedence.Exif;
        metadata.Reconcile(options);

        Assert.Multiple(() =>
        {
            Assert.That((metadata.Get("Make", null) ?? throw new System.InvalidOperationException("A required value is unavailable.")).GetValue(), Is.EqualTo("Exif Camera"));
            Assert.That((metadata.Get("Headline", null) ?? throw new System.InvalidOperationException("A required value is unavailable.")).GetValue(), Is.EqualTo("XMP headline"));
            Assert.That(metadata.XmpPackets.Single().CopyBytes(), Is.EqualTo(xmp));
            Assert.That(metadata.IptcRecords.Single().CopyBytes(), Is.EqualTo(iptc));
        });
    }

    [Test]
    public void MetadataSelectionsSkipUnrequestedRepresentationsAndPayloads()
    {
        byte[] iptc = CreateIptc("Selected headline", "Title", new string[] { "one" }, false);
        SyntheticTiffBuilder builder = new SyntheticTiffBuilder(RawByteOrder.LittleEndian, false);
        SyntheticTiffIfd root = builder.AddIfd();
        AddAscii(builder, root, 271, "Exif Camera");
        builder.AddField(root, 700, TiffFieldType.Byte, 4, new byte[] { 0, 1, 2, 3 });
        builder.AddField(root, 33723, TiffFieldType.Undefined, (ulong)iptc.Length, iptc);
        RawMetadataOptions options = new RawMetadataOptions();
        options.ReadExif = false;
        options.ReadGps = false;
        options.ReadXmp = false;
        options.ReadIptc = true;
        options.ReadIcc = false;
        options.ReadMakerNotes = false;

        RawDocument document = RawCodec.OpenMetadata(new ReadOnlyMemory<byte>(builder.Build()), null, options);

        Assert.Multiple(() =>
        {
            Assert.That(document.Metadata.Make, Is.Empty);
            Assert.That(document.Metadata.XmpPackets, Is.Empty);
            Assert.That(document.Metadata.IptcRecords, Has.Length.EqualTo(1));
            Assert.That((document.Metadata.Get("Headline", null) ?? throw new System.InvalidOperationException("A required value is unavailable.")).GetValue(), Is.EqualTo("Selected headline"));
            Assert.That(document.PayloadMap.Find(RawPayloadKind.Xmp), Is.Empty);
            Assert.That(document.PayloadMap.Find(RawPayloadKind.Iptc), Has.Length.EqualTo(1));
        });
    }

    [Test]
    public void EquivalentDatesAndRepeatableKeywordsReconcileWithoutFalseConflicts()
    {
        string xml = "<x:xmpmeta xmlns:x='adobe:ns:meta/'><rdf:RDF xmlns:rdf='" + Rdf + "'><rdf:Description rdf:about='' xmlns:dc='" + Dc + "' xmlns:photoshop='" + Photoshop + "' photoshop:DateCreated='2026-08-19T15:27:04-07:00'><dc:subject><rdf:Bag><rdf:li>raw</rdf:li><rdf:li>photo</rdf:li></rdf:Bag></dc:subject></rdf:Description></rdf:RDF></x:xmpmeta>";
        RawMetadata metadata = new RawMetadata(null);
        metadata.OriginalTime = new RawExifTimestamp("2026:08:19 15:27:04", "-07:00", string.Empty);
        metadata.XmpPackets = new RawXmpPacket[] { RawXmpPacket.Parse(Encoding.UTF8.GetBytes(xml), new RawLimits()) };
        metadata.IptcRecords = new RawIptcRecord[] { CreateDatedIptc() };
        RawMetadataOptions options = new RawMetadataOptions();

        metadata.Reconcile(options);

        Assert.Multiple(() =>
        {
            Assert.That(metadata.Conflicts, Is.Empty);
            Assert.That((metadata.Get("DateCreated", null) ?? throw new System.InvalidOperationException("A required value is unavailable.")).GetValue(), Is.EqualTo("2026-08-19T15:27:04-07:00"));
            Assert.That((metadata.Get("Keywords", null) ?? throw new System.InvalidOperationException("A required value is unavailable.")).Values, Is.EqualTo(new string[] { "raw", "photo" }));
        });
    }

    static byte[] BuildConflictingTiff(out byte[] xmp, out byte[] iptc)
    {
        string xml = "<x:xmpmeta xmlns:x='adobe:ns:meta/'><rdf:RDF xmlns:rdf='" + Rdf + "'><rdf:Description rdf:about='' xmlns:tiff='" + Tiff + "' xmlns:dc='" + Dc + "' xmlns:photoshop='" + Photoshop + "' tiff:Make='XMP Camera' photoshop:Headline='XMP headline'><dc:title><rdf:Alt><rdf:li xml:lang='x-default'>Shared title</rdf:li></rdf:Alt></dc:title><dc:subject><rdf:Bag><rdf:li>raw</rdf:li><rdf:li>photo</rdf:li></rdf:Bag></dc:subject></rdf:Description></rdf:RDF></x:xmpmeta>";
        xmp = Encoding.UTF8.GetBytes(xml);
        iptc = CreateIptc("IPTC headline", "Shared title", new string[] { "photo", "raw" }, false);
        SyntheticTiffBuilder builder = new SyntheticTiffBuilder(RawByteOrder.LittleEndian, false);
        SyntheticTiffIfd root = builder.AddIfd();
        AddAscii(builder, root, 271, "Exif Camera");
        builder.AddField(root, 700, TiffFieldType.Byte, (ulong)xmp.Length, xmp);
        builder.AddField(root, 33723, TiffFieldType.Undefined, (ulong)iptc.Length, iptc);
        return builder.Build();
    }

    static byte[] CreateIptc(string headline, string title, string[] keywords, bool dated)
    {
        List<RawIptcDataset> datasets = new List<RawIptcDataset>();
        datasets.Add(RawIptcDataset.CreateUnsignedInteger(2, 0, 4));
        datasets.Add(RawIptcDataset.CreateText(2, 105, headline, false));
        datasets.Add(RawIptcDataset.CreateText(2, 5, title, false));
        foreach (string keyword in keywords)
        {
            datasets.Add(RawIptcDataset.CreateText(2, 25, keyword, false));
        }
        if (dated)
        {
            datasets.Add(RawIptcDataset.CreateDate(2, 55, new DateTime(2026, 8, 19)));
            datasets.Add(RawIptcDataset.CreateTime(2, 60, 15, 27, 4, true, -420));
        }
        return RawIptcRecord.Create(datasets.ToArray(), new RawLimits()).Encode(new RawLimits());
    }

    static RawIptcRecord CreateDatedIptc()
    {
        byte[] bytes = CreateIptc(string.Empty, string.Empty, new string[] { "photo", "raw" }, true);
        return RawIptcRecord.Parse(bytes, new RawLimits());
    }

    static void AddAscii(SyntheticTiffBuilder builder, SyntheticTiffIfd ifd, ushort tag, string text)
    {
        byte[] content = Encoding.ASCII.GetBytes(text);
        byte[] bytes = new byte[content.Length + 1];
        content.CopyTo(bytes, 0);
        builder.AddField(ifd, tag, TiffFieldType.Ascii, (ulong)bytes.Length, bytes);
    }
}
