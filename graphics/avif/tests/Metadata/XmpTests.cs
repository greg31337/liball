using System.Text;

namespace avif.Tests;

public class XmpTests
{
    [Test]
    public void ParsesStandardAttributesElementsResourcesAndLanguageAlternatives()
    {
        byte[] data = Encoding.UTF8.GetBytes(CreatePacket(
            "tiff:Orientation=\"6\" "
                + "xmp:CreatorTool=\"Tool &amp; Co\" custom:Rating=\"5\"",
            "<dc:title><rdf:Alt>"
                + "<rdf:li xml:lang=\"fr\">Titre</rdf:li>"
                + "<rdf:li xml:lang=\"x-default\">Title &#x1F642;</rdf:li>"
                + "</rdf:Alt></dc:title>"
                + "<dc:description><![CDATA[A <safe> description]]>"
                + "</dc:description>"
                + "<dc:format rdf:resource=\"image/avif\"/>"));
        AvifXmp xmp = AvifXmp.Create(data);

        Assert.Multiple(() =>
        {
            Assert.That(xmp.GetCode(), Is.EqualTo(AvifResultCode.Success));
            Assert.That(xmp.GetFieldCount(), Is.EqualTo(6));
            Assert.That(xmp.GetOrientation(), Is.EqualTo(6));
            Assert.That(xmp.GetCreatorTool(), Is.EqualTo("Tool & Co"));
            Assert.That(xmp.GetTitle(), Is.EqualTo("Title 🙂"));
            Assert.That(xmp.GetDescription(),
                Is.EqualTo("A <safe> description"));
            Assert.That(xmp.GetField(
                    AvifXmp.DublinCoreNamespace, "format"),
                Is.EqualTo("image/avif"));
            Assert.That(xmp.GetField("urn:custom", "Rating"),
                Is.EqualTo("5"));
            Assert.That(xmp.GetData(), Is.EqualTo(data));
            Assert.That(xmp.GetFieldNamespace(-1), Is.Empty);
            Assert.That(xmp.GetFieldName(99), Is.Empty);
        });
    }

    [Test]
    public void NamespaceAliasesAndSequenceValuesRemainNamespaceAware()
    {
        string packet = "<meta xmlns:r=\"" + AvifXmp.RdfNamespace
            + "\" xmlns:d=\"" + AvifXmp.DublinCoreNamespace
            + "\"><r:RDF><r:Description d:format=\"image/avif\">"
            + "<d:creator><r:Seq><r:li>Alice</r:li>"
            + "<r:li>Bob</r:li></r:Seq></d:creator>"
            + "</r:Description></r:RDF></meta>";
        AvifXmp xmp = AvifXmp.Create(Encoding.UTF8.GetBytes(packet));

        Assert.Multiple(() =>
        {
            Assert.That(xmp.GetCode(), Is.EqualTo(AvifResultCode.Success));
            Assert.That(xmp.GetField(
                    AvifXmp.DublinCoreNamespace, "format"),
                Is.EqualTo("image/avif"));
            Assert.That(xmp.GetField(
                    AvifXmp.DublinCoreNamespace, "creator"),
                Is.EqualTo("Alice"));
        });
    }

    [Test]
    public void ReconcilesMappedExifFieldsAndCanApplyXmpOrientation()
    {
        AvifXmp xmp = AvifXmp.Create(Encoding.UTF8.GetBytes(CreatePacket(
            "tiff:Orientation=\"7\" tiff:Make=\"Nikon\"",
            string.Empty)));
        AvifExif exif = AvifExif.Create(BuildExif(6, "Canon"));

        Assert.Multiple(() =>
        {
            Assert.That(xmp.HasExifConflict(
                    exif, AvifXmp.TiffNamespace, "Orientation"),
                Is.True);
            Assert.That(xmp.HasExifConflict(
                    exif, AvifXmp.TiffNamespace, "Make"),
                Is.True);
            Assert.That(xmp.GetReconciledField(
                    exif,
                    AvifXmp.TiffNamespace,
                    "Orientation",
                    true),
                Is.EqualTo("6"));
            Assert.That(xmp.GetReconciledField(
                    exif,
                    AvifXmp.TiffNamespace,
                    "Orientation",
                    false),
                Is.EqualTo("7"));
            Assert.That(xmp.GetReconciledField(
                    exif, AvifXmp.TiffNamespace, "Make", true),
                Is.EqualTo("Canon"));
            Assert.That(xmp.ApplyOrientationToExif(exif),
                Is.EqualTo(AvifResultCode.Success));
            Assert.That(exif.GetOrientation(), Is.EqualTo(7));
            Assert.That(xmp.HasExifConflict(
                    exif, AvifXmp.TiffNamespace, "Orientation"),
                Is.False);
        });
    }

    [Test]
    public void ReconciliationFallsBackAndReportsMissingOrientation()
    {
        AvifXmp xmp = AvifXmp.Create(Encoding.UTF8.GetBytes(CreatePacket(
            "xmp:CreatorTool=\"Library\"", string.Empty)));
        AvifExif exif = AvifExif.Create(BuildExif(4, "Camera"));

        Assert.Multiple(() =>
        {
            Assert.That(xmp.GetReconciledField(
                    exif, AvifXmp.TiffNamespace, "Make", false),
                Is.EqualTo("Camera"));
            Assert.That(xmp.GetReconciledField(
                    exif, AvifXmp.XmpNamespace, "CreatorTool", true),
                Is.EqualTo("Library"));
            Assert.That(xmp.ApplyOrientationToExif(exif),
                Is.EqualTo(AvifResultCode.NotFound));
            Assert.That(xmp.ApplyOrientationToExif(null),
                Is.EqualTo(AvifResultCode.InvalidArgument));
        });
    }

    [TestCaseSource(nameof(InvalidPackets))]
    public void RejectsMalformedOrUnsafeXml(string packet)
    {
        AvifXmp xmp = AvifXmp.Create(Encoding.UTF8.GetBytes(packet));
        Assert.That(xmp.GetCode(), Is.EqualTo(AvifResultCode.InvalidMetadata));
    }

    static IEnumerable<string> InvalidPackets()
    {
        yield return string.Empty;
        yield return "<!DOCTYPE x SYSTEM 'file:///etc/passwd'><x/>";
        yield return "<x>&external;</x>";
        yield return "<x><y></x>";
        yield return "<x:meta/>";
        yield return "<x/><y/>";
        yield return "<root/>";
        yield return "<root xmlns:r='" + AvifXmp.RdfNamespace
            + "'><r:RDF/></root>";
        yield return "<root xmlns:r='" + AvifXmp.RdfNamespace
            + "'><r:RDF><r:Description/></r:RDF>"
            + "<r:RDF><r:Description/></r:RDF></root>";
        yield return "<root><!-- bad -- comment --><x/></root>";
        yield return "<![CDATA[outside]]><root/>";
        yield return "<root>&#xD800;</root>";
        yield return "<?xml version='1.0' encoding='utf-16'?><root/>";
        yield return "<?xml?><root/>";
        yield return "<?xml encoding='utf-8' version='1.0'?><root/>";
        yield return "<root>forbidden ]]></root>";
        yield return "<root xmlns:a='urn:same' xmlns:b='urn:same' "
            + "a:value='1' b:value='2'/>";
        yield return "<root xmlns:xml='urn:wrong'/>";
    }

    [Test]
    public void RejectsInvalidUtf8AndXmlControlCharacters()
    {
        byte[] invalidUtf8 = new byte[] { 60, 120, 62, 192, 128, 60, 47, 120, 62 };
        byte[] control = Encoding.UTF8.GetBytes("<x>\u0001</x>");

        Assert.Multiple(() =>
        {
            Assert.That(AvifXmp.Create(invalidUtf8).GetCode(),
                Is.EqualTo(AvifResultCode.InvalidMetadata));
            Assert.That(AvifXmp.Create(control).GetCode(),
                Is.EqualTo(AvifResultCode.InvalidMetadata));
        });
    }

    [Test]
    public void EnforcesXmpMetadataTemporaryAndRecursionLimits()
    {
        byte[] data = Encoding.UTF8.GetBytes(CreatePacket(
            "tiff:Orientation=\"1\"", string.Empty));
        AvifSecurityLimits xmpLimit = new AvifSecurityLimits();
        xmpLimit.MaxXmpBytes = data.Length - 1;
        AvifSecurityLimits metadataLimit = new AvifSecurityLimits();
        metadataLimit.MaxMetadataBytes = data.Length - 1;
        AvifSecurityLimits temporaryLimit = new AvifSecurityLimits();
        temporaryLimit.MaxTemporaryBytes = 1;
        AvifSecurityLimits recursionLimit = new AvifSecurityLimits();
        recursionLimit.MaxRecursionDepth = 2;

        Assert.Multiple(() =>
        {
            Assert.That(AvifXmp.Create(data, xmpLimit).GetCode(),
                Is.EqualTo(AvifResultCode.LimitExceeded));
            Assert.That(AvifXmp.Create(data, metadataLimit).GetCode(),
                Is.EqualTo(AvifResultCode.LimitExceeded));
            Assert.That(AvifXmp.Create(data, temporaryLimit).GetCode(),
                Is.EqualTo(AvifResultCode.LimitExceeded));
            Assert.That(AvifXmp.Create(data, recursionLimit).GetCode(),
                Is.EqualTo(AvifResultCode.LimitExceeded));
        });
    }

    [Test]
    public void InvalidXmpStateExposesNoPartialFieldsAndCannotUpdateExif()
    {
        AvifXmp xmp = AvifXmp.Create(Encoding.UTF8.GetBytes("<invalid/>"));
        AvifExif exif = AvifExif.Create(BuildExif(2, "Camera"));

        Assert.Multiple(() =>
        {
            Assert.That(xmp.GetCode(),
                Is.EqualTo(AvifResultCode.InvalidMetadata));
            Assert.That(xmp.GetData(), Is.Empty);
            Assert.That(xmp.GetFieldCount(), Is.Zero);
            Assert.That(xmp.GetFieldNamespace(0), Is.Empty);
            Assert.That(xmp.GetFieldName(0), Is.Empty);
            Assert.That(xmp.GetFieldValue(0), Is.Empty);
            Assert.That(xmp.GetField(null, null), Is.Empty);
            Assert.That(xmp.GetOrientation(), Is.Zero);
            Assert.That(xmp.ApplyOrientationToExif(exif),
                Is.EqualTo(AvifResultCode.InvalidMetadata));
            Assert.That(exif.GetOrientation(), Is.EqualTo(2));
        });
    }

    [Test]
    public void InvalidOrientationIsInspectableButNotApplied()
    {
        AvifXmp xmp = AvifXmp.Create(Encoding.UTF8.GetBytes(CreatePacket(
            "tiff:Orientation=\"9\"", string.Empty)));
        AvifExif exif = AvifExif.Create(BuildExif(1, "Camera"));

        Assert.Multiple(() =>
        {
            Assert.That(xmp.GetCode(), Is.EqualTo(AvifResultCode.Success));
            Assert.That(xmp.GetOrientation(), Is.Zero);
            Assert.That(xmp.ApplyOrientationToExif(exif),
                Is.EqualTo(AvifResultCode.NotFound));
            Assert.That(exif.GetOrientation(), Is.EqualTo(1));
        });
    }

    static string CreatePacket(string attributes, string elements)
    {
        return "<?xpacket begin='﻿' id='W5M0MpCehiHzreSzNTczkc9d'?>"
            + "<x:xmpmeta xmlns:x='adobe:ns:meta/' "
            + "xmlns:rdf='" + AvifXmp.RdfNamespace + "' "
            + "xmlns:dc='" + AvifXmp.DublinCoreNamespace + "' "
            + "xmlns:xmp='" + AvifXmp.XmpNamespace + "' "
            + "xmlns:tiff='" + AvifXmp.TiffNamespace + "' "
            + "xmlns:custom='urn:custom'>"
            + "<rdf:RDF><rdf:Description rdf:about='' "
            + attributes + ">" + elements
            + "</rdf:Description></rdf:RDF></x:xmpmeta>"
            + "<?xpacket end='w'?>";
    }

    static byte[] BuildExif(int orientation, string make)
    {
        byte[] makeBytes = Encoding.ASCII.GetBytes(make + "\0");
        int tiff = 4;
        int valueRelative = 38;
        byte[] data = new byte[tiff + valueRelative + makeBytes.Length];
        WriteUInt32BigEndian(data, 0, 0);
        data[tiff] = 73;
        data[tiff + 1] = 73;
        WriteUInt16(data, tiff + 2, 42);
        WriteUInt32(data, tiff + 4, 8);
        int root = tiff + 8;
        WriteUInt16(data, root, 2);
        WriteEntry(data, root + 2, 274, 3, 1, (uint)orientation);
        WriteEntry(data, root + 14, 271, 2,
            (uint)makeBytes.Length, (uint)valueRelative);
        WriteUInt32(data, root + 26, 0);
        makeBytes.CopyTo(data, tiff + valueRelative);
        return data;
    }

    static void WriteEntry(
        byte[] data,
        int offset,
        ushort identifier,
        ushort type,
        uint count,
        uint value)
    {
        WriteUInt16(data, offset, identifier);
        WriteUInt16(data, offset + 2, type);
        WriteUInt32(data, offset + 4, count);
        if (type == 3 && count == 1)
        {
            WriteUInt16(data, offset + 8, (ushort)value);
        }
        else
        {
            WriteUInt32(data, offset + 8, value);
        }
    }

    static void WriteUInt16(byte[] data, int offset, ushort value)
    {
        data[offset] = (byte)value;
        data[offset + 1] = (byte)(value >> 8);
    }

    static void WriteUInt32(byte[] data, int offset, uint value)
    {
        data[offset] = (byte)value;
        data[offset + 1] = (byte)(value >> 8);
        data[offset + 2] = (byte)(value >> 16);
        data[offset + 3] = (byte)(value >> 24);
    }

    static void WriteUInt32BigEndian(byte[] data, int offset, uint value)
    {
        data[offset] = (byte)(value >> 24);
        data[offset + 1] = (byte)(value >> 16);
        data[offset + 2] = (byte)(value >> 8);
        data[offset + 3] = (byte)value;
    }
}
