using raw;
using System.Text;

namespace raw.Tests;

public class MetadataRoundTripInteroperabilityTests
{
    const string Rdf = "http://www.w3.org/1999/02/22-rdf-syntax-ns#";
    const string Dc = "http://purl.org/dc/elements/1.1/";
    const string Photoshop = "http://ns.adobe.com/photoshop/1.0/";
    const string Tiff = "http://ns.adobe.com/tiff/1.0/";

    [TestCase(RawByteOrder.LittleEndian)]
    [TestCase(RawByteOrder.BigEndian)]
    public void CombinedMetadataParseWriteParsePreservesConflictsEncodingsPaddingAndUnknownData(RawByteOrder byteOrder)
    {
        byte[] first = Write(CreateMetadataRequest(byteOrder));
        RawDocument firstDocument = Open(first);
        TiffIfdGraph firstGraph = Parse(first);
        byte[] second = Write(Rebuild(firstGraph, first));
        RawDocument secondDocument = Open(second);
        RawMetadata metadata = secondDocument.Metadata;
        RawXmpPacket xmp = metadata.XmpPackets.Single();
        RawIptcRecord iptc = metadata.IptcRecords.Single();
        RawMakerNote makerNote = metadata.MakerNotes.Single();

        Assert.Multiple(() =>
        {
            Assert.That(second, Is.EqualTo(first));
            Assert.That(metadata.Make, Is.EqualTo("NIKON CORPORATION"));
            Assert.That((metadata.OriginalTime ?? throw new System.InvalidOperationException("A required value is unavailable.")).DateTimeText, Is.EqualTo("2026:08:19 15:27:04"));
            Assert.That((metadata.UserComment ?? throw new System.InvalidOperationException("A required value is unavailable.")).Text, Is.EqualTo("東京の注釈"));
            Assert.That(xmp.Encoding, Is.EqualTo(RawXmpEncoding.Utf16LittleEndian));
            Assert.That(xmp.HasByteOrderMark, Is.True);
            Assert.That(xmp.PaddingBytes, Is.Not.Empty);
            Assert.That((xmp.GetField(Tiff, "Make", null) ?? throw new System.InvalidOperationException("A required value is unavailable.")).Value, Is.EqualTo("XMP Camera"));
            Assert.That((xmp.GetField(Dc, "creator", null) ?? throw new System.InvalidOperationException("A required value is unavailable.")).Value, Is.EqualTo("Zoë"));
            Assert.That(iptc.Utf8, Is.True);
            Assert.That((iptc.GetDataset(2, 105) ?? throw new System.InvalidOperationException("A required value is unavailable.")).Text, Is.EqualTo("IPTC café headline"));
            Assert.That((metadata.IccProfiles.Single().Header ?? throw new System.InvalidOperationException("A required value is unavailable.")).VersionMajor, Is.EqualTo(4));
            Assert.That(metadata.IccProfiles.Single().CopyBytes(), Is.EqualTo(CreateMinimalIccProfile()));
            Assert.That(makerNote.Signature, Is.EqualTo("NikonEmbeddedTiff"));
            Assert.That(makerNote.NikonVersion, Is.EqualTo("0200"));
            Assert.That((makerNote.FindNikonTag(0xbeef) ?? throw new System.InvalidOperationException("A required value is unavailable.")).CopyBytes(), Is.EqualTo(new byte[] { 91, 92, 93, 94, 95, 96, 97 }));
            Assert.That(metadata.Conflicts.Select(conflict => conflict.Name), Does.Contain("Make"));
            Assert.That(metadata.Conflicts.Select(conflict => conflict.Name), Does.Contain("Headline"));
            Assert.That(metadata.Get("Make", null), Is.Null);
            Assert.That(metadata.Get("Headline", null), Is.Null);
            Assert.That(secondDocument.ExtractPayload(secondDocument.PayloadMap.Find(RawPayloadKind.Xmp).Single()), Is.EqualTo(firstDocument.Metadata.XmpPackets.Single().CopyBytes()));
            Assert.That(secondDocument.ExtractPayload(secondDocument.PayloadMap.Find(RawPayloadKind.Iptc).Single()), Is.EqualTo(firstDocument.Metadata.IptcRecords.Single().CopyBytes()));
            Assert.That(secondDocument.ExtractPayload(secondDocument.PayloadMap.Find(RawPayloadKind.IccProfile).Single()), Is.EqualTo(firstDocument.Metadata.IccProfiles.Single().CopyBytes()));
            Assert.That(secondDocument.ExtractPayload(secondDocument.PayloadMap.Find(RawPayloadKind.MakerNote).Single()), Is.EqualTo(firstDocument.Metadata.MakerNotes.Single().CopyBytes()));
            Assert.That(((Parse(second).FindIfd("IFD0") ?? throw new System.InvalidOperationException("A required value is unavailable.")).FindEntry(65001) ?? throw new System.InvalidOperationException("A required value is unavailable.")).RawBytes, Is.EqualTo(new byte[] { 0xa1, 0xb2, 0xc3, 0xd4, 0xe5 }));
            Assert.That(((Parse(second).FindIfd("IFD0/Exif") ?? throw new System.InvalidOperationException("A required value is unavailable.")).FindEntry(65002) ?? throw new System.InvalidOperationException("A required value is unavailable.")).RawBytes, Is.EqualTo(new byte[] { 9, 7, 5, 3, 1, 0 }));
        });
    }

    [Test]
    public void ReconciledRoundTripSelectsRequestedPrecedenceWithoutMutatingSourcePayloads()
    {
        byte[] source = Write(CreateMetadataRequest(RawByteOrder.LittleEndian));
        RawMetadataOptions options = new RawMetadataOptions();
        options.Reconcile = true;
        options.Precedence = RawMetadataPrecedence.Iptc;

        RawMetadata metadata = RawCodec.OpenMetadata(new ReadOnlyMemory<byte>(source), null, options).Metadata;

        Assert.Multiple(() =>
        {
            Assert.That((metadata.Get("Headline", null) ?? throw new System.InvalidOperationException("A required value is unavailable.")).GetValue(), Is.EqualTo("IPTC café headline"));
            Assert.That((metadata.Get("Headline", null) ?? throw new System.InvalidOperationException("A required value is unavailable.")).Source, Is.EqualTo(RawMetadataSource.Iptc));
            Assert.That((metadata.Get("Make", null) ?? throw new System.InvalidOperationException("A required value is unavailable.")).GetValue(), Is.EqualTo("XMP Camera"));
            Assert.That((metadata.Get("Make", null) ?? throw new System.InvalidOperationException("A required value is unavailable.")).Source, Is.EqualTo(RawMetadataSource.Xmp));
            Assert.That(metadata.Conflicts.All(conflict => conflict.Resolved), Is.True);
            Assert.That(metadata.XmpPackets.Single().CopyBytes(), Is.EqualTo(CreateXmp()));
            Assert.That(metadata.IptcRecords.Single().CopyBytes(), Is.EqualTo(CreateIptc()));
            Assert.That(metadata.IccProfiles.Single().CopyBytes(), Is.EqualTo(CreateMinimalIccProfile()));
            Assert.That(metadata.MakerNotes.Single().CopyBytes(), Is.EqualTo(CreateNikonMakerNote()));
        });
    }

    [Test]
    public void ExifToolAgreesOnCombinedMetadataWhenAvailable()
    {
        ExternalToolAdapter tool = IndependentToolAdapters.ExifTool();
        tool.IgnoreIfUnavailable();
        string[] arguments =
        {
            "-G1", "-s",
            "-EXIF:Make", "-EXIF:DateTimeOriginal", "-EXIF:UserComment",
            "-XMP-tiff:Make", "-XMP-photoshop:Headline", "-XMP-dc:Creator",
            "-IPTC:Headline", "-ICC_Profile:ProfileVersion", "-MakerNotes:MakerNoteVersion"
        };

        ExternalToolResult result = tool.RunOnBytes(arguments, Write(CreateMetadataRequest(RawByteOrder.LittleEndian)), ".tif");

        Assert.Multiple(() =>
        {
            Assert.That(result.ExitCode, Is.Zero, result.StandardError);
            Assert.That(result.TimedOut, Is.False);
            Assert.That(result.StandardOutput, Does.Match("\\[IFD0\\].*Make\\s*:\\s*NIKON CORPORATION"));
            Assert.That(result.StandardOutput, Does.Match("\\[ExifIFD\\].*DateTimeOriginal\\s*:\\s*2026:08:19 15:27:04"));
            Assert.That(result.StandardOutput, Does.Contain("東京の注釈"));
            Assert.That(result.StandardOutput, Does.Match("\\[XMP-tiff\\].*Make\\s*:\\s*XMP Camera"));
            Assert.That(result.StandardOutput, Does.Match("\\[XMP-photoshop\\].*Headline\\s*:\\s*XMP headline"));
            Assert.That(result.StandardOutput, Does.Contain("Zoë"));
            Assert.That(result.StandardOutput, Does.Match("\\[IPTC\\].*Headline\\s*:\\s*IPTC café headline"));
            Assert.That(result.StandardOutput, Does.Match("ProfileVersion\\s*:\\s*4\\.4\\.0"));
            Assert.That(result.StandardOutput, Does.Contain("MakerNoteVersion"));
        });
    }

    [Test]
    public void Exiv2AgreesOnCombinedMetadataWhenAvailable()
    {
        ExternalToolAdapter tool = IndependentToolAdapters.Exiv2();
        tool.IgnoreIfUnavailable();

        ExternalToolResult result = tool.RunOnBytes(new string[] { "-pa" }, Write(CreateMetadataRequest(RawByteOrder.LittleEndian)), ".tif");

        Assert.Multiple(() =>
        {
            Assert.That(result.ExitCode, Is.Zero, result.StandardError);
            Assert.That(result.TimedOut, Is.False);
            Assert.That(result.StandardOutput, Does.Match("Exif\\.Image\\.Make.*NIKON CORPORATION"));
            Assert.That(result.StandardOutput, Does.Match("Exif\\.Photo\\.DateTimeOriginal.*2026:08:19 15:27:04"));
            Assert.That(result.StandardOutput, Does.Contain("東京の注釈"));
            Assert.That(result.StandardOutput, Does.Match("Xmp\\.tiff\\.Make.*XMP Camera"));
            Assert.That(result.StandardOutput, Does.Match("Xmp\\.photoshop\\.Headline.*XMP headline"));
            Assert.That(result.StandardOutput, Does.Match("Iptc\\.Application2\\.Headline.*IPTC café headline"));
            Assert.That(result.StandardOutput, Does.Contain("Exif.Image.InterColorProfile"));
            Assert.That(result.StandardOutput, Does.Contain("Exif.Nikon3.MakerNoteVersion"));
        });
    }

    static TiffLayoutRequest CreateMetadataRequest(RawByteOrder byteOrder)
    {
        byte[] xmp = CreateXmp();
        byte[] iptc = CreateIptc();
        byte[] icc = CreateMinimalIccProfile();
        byte[] makerNote = CreateNikonMakerNote();
        RawTagValue comment = RawTagValue.CreatePrefixedText("東京の注釈", RawTextEncoding.Unicode, byteOrder);
        List<TiffLayoutEntryInput> rootEntries = new List<TiffLayoutEntryInput>
        {
            LongEntry(256, 1, byteOrder),
            LongEntry(257, 1, byteOrder),
            ShortEntry(258, 8, byteOrder),
            ShortEntry(259, 1, byteOrder),
            ShortEntry(262, 1, byteOrder),
            BlockOffsetEntry(273, byteOrder),
            ShortEntry(277, 1, byteOrder),
            LongEntry(278, 1, byteOrder),
            LongEntry(279, 1, byteOrder),
            AsciiEntry(271, "NIKON CORPORATION"),
            new TiffLayoutEntryInput(700, TiffFieldType.Byte, (ulong)xmp.Length, xmp, null),
            new TiffLayoutEntryInput(33723, TiffFieldType.Undefined, (ulong)iptc.Length, iptc, null),
            new TiffLayoutEntryInput(34675, TiffFieldType.Undefined, (ulong)icc.Length, icc, null),
            IfdOffsetEntry(34665, byteOrder, "IFD0/Exif"),
            new TiffLayoutEntryInput(65001, TiffFieldType.Undefined, 5, new byte[] { 0xa1, 0xb2, 0xc3, 0xd4, 0xe5 }, null)
        };
        List<TiffLayoutEntryInput> exifEntries = new List<TiffLayoutEntryInput>
        {
            AsciiEntry(36867, "2026:08:19 15:27:04"),
            new TiffLayoutEntryInput(36864, TiffFieldType.Undefined, 4, Encoding.ASCII.GetBytes("0300"), null),
            new TiffLayoutEntryInput(37510, TiffFieldType.Undefined, comment.Count, comment.CopyBytes(), null),
            new TiffLayoutEntryInput(37500, TiffFieldType.Undefined, (ulong)makerNote.Length, makerNote, null),
            new TiffLayoutEntryInput(65002, TiffFieldType.Undefined, 6, new byte[] { 9, 7, 5, 3, 1, 0 }, null)
        };
        TiffLayoutIfdInput root = new TiffLayoutIfdInput("IFD0", rootEntries.ToArray(), null);
        TiffLayoutIfdInput exif = new TiffLayoutIfdInput("IFD0/Exif", exifEntries.ToArray(), null);
        TiffLayoutBlockInput strip = new TiffLayoutBlockInput("strip-0", TiffLayoutBlockKind.Strip, 1, new byte[] { 127 });
        return new TiffLayoutRequest(false, byteOrder, "IFD0", new TiffLayoutIfdInput[] { root, exif }, new TiffLayoutBlockInput[] { strip });
    }

    static TiffLayoutRequest Rebuild(TiffIfdGraph graph, byte[] source)
    {
        List<TiffLayoutIfdInput> directories = new List<TiffLayoutIfdInput>();
        foreach (TiffIfd ifd in graph.Ifds)
        {
            List<TiffLayoutEntryInput> entries = new List<TiffLayoutEntryInput>();
            foreach (TiffEntry entry in ifd.Entries)
            {
                TiffLayoutReference[]? references = null;
                if (entry.Tag == 273)
                {
                    references = new TiffLayoutReference[] { new TiffLayoutReference(0, TiffLayoutReferenceKind.Block, "strip-0") };
                }
                else if (entry.Tag == 34665)
                {
                    references = new TiffLayoutReference[] { new TiffLayoutReference(0, TiffLayoutReferenceKind.Ifd, "IFD0/Exif") };
                }
                entries.Add(new TiffLayoutEntryInput(entry.Tag, entry.FieldType, entry.Count, entry.RawBytes.ToArray(), references));
            }
            directories.Add(new TiffLayoutIfdInput(ifd.Path, entries.ToArray(), null));
        }
        TiffDataBlock block = graph.Images.Single().Blocks.Single();
        byte[] blockBytes = source.AsSpan(checked((int)block.Offset), checked((int)block.ByteCount)).ToArray();
        TiffLayoutBlockInput rebuiltBlock = new TiffLayoutBlockInput("strip-0", TiffLayoutBlockKind.Strip, blockBytes.Length, blockBytes);
        return new TiffLayoutRequest(graph.Header.BigTiff, graph.Header.ByteOrder, "IFD0", directories.ToArray(), new TiffLayoutBlockInput[] { rebuiltBlock });
    }

    static byte[] CreateXmp()
    {
        string xml = "<x:xmpmeta xmlns:x='adobe:ns:meta/'><rdf:RDF xmlns:rdf='" + Rdf + "'><rdf:Description rdf:about='' xmlns:dc='" + Dc + "' xmlns:tiff='" + Tiff + "' xmlns:photoshop='" + Photoshop + "' tiff:Make='XMP Camera' photoshop:Headline='XMP headline'><dc:creator><rdf:Seq><rdf:li>Zoë</rdf:li></rdf:Seq></dc:creator></rdf:Description></rdf:RDF></x:xmpmeta>";
        string text = "<?xpacket begin='﻿' id='W5M0MpCehiHzreSzNTczkc9d'?>\n" + xml + new string(' ', 96) + "<?xpacket end='w'?>";
        Encoding encoding = new UnicodeEncoding(false, true);
        return encoding.GetPreamble().Concat(encoding.GetBytes(text)).ToArray();
    }

    static byte[] CreateIptc()
    {
        return RawIptcRecord.Create(new RawIptcDataset[]
        {
            RawIptcDataset.CreateBinary(1, 90, new byte[] { 0x1b, 0x25, 0x47 }),
            RawIptcDataset.CreateUnsignedInteger(2, 0, 4),
            RawIptcDataset.CreateText(2, 105, "IPTC café headline", true),
            RawIptcDataset.CreateText(2, 25, "東京", true)
        }, new RawLimits()).Encode(new RawLimits());
    }

    static byte[] CreateMinimalIccProfile()
    {
        byte[] profile = new byte[132];
        WriteBigUInt32(profile, 0, (uint)profile.Length);
        WriteSignature(profile, 4, "TEST");
        profile[8] = 4;
        profile[9] = 0x40;
        WriteSignature(profile, 12, "mntr");
        WriteSignature(profile, 16, "RGB ");
        WriteSignature(profile, 20, "XYZ ");
        WriteBigUInt16(profile, 24, 2026);
        WriteBigUInt16(profile, 26, 8);
        WriteBigUInt16(profile, 28, 19);
        WriteBigUInt16(profile, 30, 15);
        WriteBigUInt16(profile, 32, 27);
        WriteBigUInt16(profile, 34, 4);
        WriteSignature(profile, 36, "acsp");
        WriteSignature(profile, 40, "APPL");
        WriteSignature(profile, 80, "TEST");
        WriteBigUInt32(profile, 128, 0);
        return profile;
    }

    static byte[] CreateNikonMakerNote()
    {
        byte[] bytes = new byte[79];
        Encoding.ASCII.GetBytes("Nikon\0").CopyTo(bytes, 0);
        bytes[6] = 2;
        bytes[10] = (byte)'I';
        bytes[11] = (byte)'I';
        EndianWriter.WriteUInt16(bytes.AsSpan(12, 2), 42, RawByteOrder.LittleEndian);
        EndianWriter.WriteUInt32(bytes.AsSpan(14, 4), 8, RawByteOrder.LittleEndian);
        EndianWriter.WriteUInt16(bytes.AsSpan(18, 2), 3, RawByteOrder.LittleEndian);
        WriteNikonEntry(bytes, 0, 0x0001, TiffFieldType.Undefined, 4, 0);
        Encoding.ASCII.GetBytes("0200").CopyTo(bytes, 28);
        WriteNikonEntry(bytes, 1, 0x001d, TiffFieldType.Ascii, 12, 50);
        WriteNikonEntry(bytes, 2, 0xbeef, TiffFieldType.Undefined, 7, 62);
        EndianWriter.WriteUInt32(bytes.AsSpan(56, 4), 0, RawByteOrder.LittleEndian);
        Encoding.ASCII.GetBytes("1234567\0").CopyTo(bytes, 60);
        new byte[] { 91, 92, 93, 94, 95, 96, 97 }.CopyTo(bytes, 72);
        return bytes;
    }

    static void WriteNikonEntry(byte[] bytes, int index, ushort tag, TiffFieldType type, uint count, uint value)
    {
        int position = 20 + index * 12;
        EndianWriter.WriteUInt16(bytes.AsSpan(position, 2), tag, RawByteOrder.LittleEndian);
        EndianWriter.WriteUInt16(bytes.AsSpan(position + 2, 2), (ushort)type, RawByteOrder.LittleEndian);
        EndianWriter.WriteUInt32(bytes.AsSpan(position + 4, 4), count, RawByteOrder.LittleEndian);
        EndianWriter.WriteUInt32(bytes.AsSpan(position + 8, 4), value, RawByteOrder.LittleEndian);
    }

    static TiffLayoutEntryInput IfdOffsetEntry(ushort tag, RawByteOrder byteOrder, string path)
    {
        TiffLayoutReference reference = new TiffLayoutReference(0, TiffLayoutReferenceKind.Ifd, path);
        return new TiffLayoutEntryInput(tag, TiffFieldType.Long, 1, UInt32Bytes(0, byteOrder), new TiffLayoutReference[] { reference });
    }

    static TiffLayoutEntryInput BlockOffsetEntry(ushort tag, RawByteOrder byteOrder)
    {
        TiffLayoutReference reference = new TiffLayoutReference(0, TiffLayoutReferenceKind.Block, "strip-0");
        return new TiffLayoutEntryInput(tag, TiffFieldType.Long, 1, UInt32Bytes(0, byteOrder), new TiffLayoutReference[] { reference });
    }

    static TiffLayoutEntryInput AsciiEntry(ushort tag, string text)
    {
        byte[] bytes = Encoding.ASCII.GetBytes(text + "\0");
        return new TiffLayoutEntryInput(tag, TiffFieldType.Ascii, (ulong)bytes.Length, bytes, null);
    }

    static TiffLayoutEntryInput ShortEntry(ushort tag, ushort value, RawByteOrder byteOrder)
    {
        byte[] bytes = new byte[2];
        EndianWriter.WriteUInt16(bytes, value, byteOrder);
        return new TiffLayoutEntryInput(tag, TiffFieldType.Short, 1, bytes, null);
    }

    static TiffLayoutEntryInput LongEntry(ushort tag, uint value, RawByteOrder byteOrder)
    {
        return new TiffLayoutEntryInput(tag, TiffFieldType.Long, 1, UInt32Bytes(value, byteOrder), null);
    }

    static byte[] UInt32Bytes(uint value, RawByteOrder byteOrder)
    {
        byte[] bytes = new byte[4];
        EndianWriter.WriteUInt32(bytes, value, byteOrder);
        return bytes;
    }

    static void WriteBigUInt16(byte[] bytes, int offset, ushort value)
    {
        bytes[offset] = (byte)(value >> 8);
        bytes[offset + 1] = (byte)value;
    }

    static void WriteBigUInt32(byte[] bytes, int offset, uint value)
    {
        bytes[offset] = (byte)(value >> 24);
        bytes[offset + 1] = (byte)(value >> 16);
        bytes[offset + 2] = (byte)(value >> 8);
        bytes[offset + 3] = (byte)value;
    }

    static void WriteSignature(byte[] bytes, int offset, string signature)
    {
        Encoding.ASCII.GetBytes(signature).CopyTo(bytes, offset);
    }

    static byte[] Write(TiffLayoutRequest request)
    {
        TiffLayoutPlan plan = TiffLayoutPlanner.Plan(request, new RawLimits(), CancellationToken.None);
        return TiffWriter.WriteToArray(plan, new RawLimits(), CancellationToken.None);
    }

    static TiffIfdGraph Parse(byte[] source)
    {
        return TiffParser.Parse(new BoundedInput(new ReadOnlyMemory<byte>(source), 0, source.Length, new RawLimits()));
    }

    static RawDocument Open(byte[] source)
    {
        return RawCodec.OpenMetadata(new ReadOnlyMemory<byte>(source), null, new RawOpenOptions());
    }

}
