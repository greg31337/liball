using System.Buffers.Binary;

namespace avif.Tests;

public class AvifRemuxerTests
{
    [Test]
    public void DefaultPreservationRewriteReturnsSourceBytesExactly()
    {
        byte[] source = AppendBox(CreateOpaqueDocument(), "junk", new byte[] { 9, 8, 7 });

        AvifRemuxResult result = AvifRemuxer.Rewrite(source, new AvifRewriteOptions());

        Assert.Multiple(() =>
        {
            Assert.That(result.Code, Is.EqualTo(AvifResultCode.Success));
            Assert.That(result.PreservedSourceBytes, Is.True);
            Assert.That(result.Data, Is.EqualTo(source));
            Assert.That((result.InputValidation ?? throw new InvalidOperationException("Invalid internal state.")).Code, Is.EqualTo(AvifResultCode.Success));
        });
    }

    [Test]
    public void CanonicalRemuxPreservesOpaqueBoxesPropertiesAndMetadataBytes()
    {
        byte[] opaqueBox = CreateBox("junk", new byte[] { 9, 8, 7 });
        byte[] opaqueProperty = BuildRawProperty("zzzz", new byte[] { 6, 5, 4 });
        byte[] metadata = new byte[] { 3, 2, 1 };
        byte[] source = AppendBytes(CreateOpaqueDocument(), opaqueBox);
        AvifRewriteOptions options = CreateRewriteOptions();

        AvifRemuxResult result = AvifRemuxer.Rewrite(source, options);
        MetaParseResult parsed = MetaBoxParser.Parse(result.Data, CreateLimits());
        IsoBoxParseResult boxes = IsoBoxReader.Parse(result.Data, CreateLimits(), true);
        ParsedItem? primary = (parsed.Meta ?? throw new InvalidOperationException("Invalid internal state.")).GetPrimaryItem();
        EncodedItemExtractionResult extracted = parsed.Meta.ExtractEncodedItem(result.Data, 9);

        Assert.Multiple(() =>
        {
            Assert.That(result.Code, Is.EqualTo(AvifResultCode.Success));
            Assert.That(result.PreservedSourceBytes, Is.False);
            Assert.That(result.PreservedOpaqueBoxCount, Is.EqualTo(1));
            Assert.That(result.PreservedOpaquePropertyCount, Is.EqualTo(1));
            Assert.That(result.PreservedOpaqueMetadataCount, Is.EqualTo(1));
            Assert.That(boxes.Boxes[boxes.Boxes.Count - 1].RawData, Is.EqualTo(opaqueBox));
            Assert.That(((primary ?? throw new InvalidOperationException("Invalid internal state.")).GetPropertyAssociation(4) ?? throw new InvalidOperationException("Invalid internal state.")).GetProperty().GetRawData(),
                Is.EqualTo(opaqueProperty));
            Assert.That((extracted.Data ?? throw new InvalidOperationException("Invalid internal state.")).GetSpan().ToArray(), Is.EqualTo(metadata));
            Assert.That((result.OutputValidation ?? throw new InvalidOperationException("Invalid internal state.")).Code, Is.EqualTo(AvifResultCode.Success));
        });

        (extracted.Data ?? throw new InvalidOperationException("Invalid internal state.")).Close();
    }

    [Test]
    public void PreservationControlsRemoveOpaqueNonEssentialData()
    {
        byte[] source = AppendBox(CreateOpaqueDocument(), "junk", new byte[] { 9, 8, 7 });
        AvifRewriteOptions options = CreateRewriteOptions();
        options.PreserveUnknownBoxes = false;
        options.PreserveUnknownProperties = false;
        options.PreserveUnknownMetadata = false;

        AvifRemuxResult result = AvifRemuxer.Rewrite(source, options);
        MetaParseResult parsed = MetaBoxParser.Parse(result.Data, CreateLimits());

        Assert.Multiple(() =>
        {
            Assert.That(result.Code, Is.EqualTo(AvifResultCode.Success));
            Assert.That(result.PreservedOpaqueBoxCount, Is.Zero);
            Assert.That(result.PreservedOpaquePropertyCount, Is.Zero);
            Assert.That(result.PreservedOpaqueMetadataCount, Is.Zero);
            Assert.That((parsed.Boxes ?? throw new InvalidOperationException("Invalid internal state.")).Boxes.Select(box => box.Type),
                Is.EqualTo(new string[] { "ftyp", "meta", "mdat" }));
            Assert.That((parsed.Meta ?? throw new InvalidOperationException("Invalid internal state.")).GetItemCount(), Is.EqualTo(1));
            Assert.That((parsed.Meta.GetPrimaryItem() ?? throw new InvalidOperationException("Invalid internal state.")).GetPropertyCount(), Is.EqualTo(4));
        });
    }

    [Test]
    public void RemuxPreservesMimeMetadataFieldsAndPayload()
    {
        CanonicalItemContainerRequest request = CreateBaseRequest(false);
        CanonicalItemPayload mime = CanonicalItemPayload.Create(
            7,
            "mime",
            "xmp",
            true,
            new byte[] { 60, 120, 109, 112, 62 });
        mime.ContentType = "application/rdf+xml";
        mime.ContentEncoding = "identity";
        (request.Items ?? throw new InvalidOperationException("Invalid internal state.")).Add(mime);
        (request.References ?? throw new InvalidOperationException("Invalid internal state.")).Add(new CanonicalItemReference(
            "cdsc",
            7,
            new List<uint> { 1 }));
        byte[] source = CanonicalItemContainerWriter.Write(request).Data;

        AvifRemuxResult result = AvifRemuxer.Rewrite(source, CreateRewriteOptions());
        MetaParseResult parsed = MetaBoxParser.Parse(result.Data, CreateLimits());
        ParsedItem? parsedMime = (parsed.Meta ?? throw new InvalidOperationException("Invalid internal state.")).FindItem(7);
        EncodedItemExtractionResult extracted = parsed.Meta.ExtractEncodedItem(result.Data, 7);

        Assert.Multiple(() =>
        {
            Assert.That(result.Code, Is.EqualTo(AvifResultCode.Success));
            Assert.That((parsedMime ?? throw new InvalidOperationException("Invalid internal state.")).GetContentType(), Is.EqualTo("application/rdf+xml"));
            Assert.That(parsedMime.GetContentEncoding(), Is.EqualTo("identity"));
            Assert.That((extracted.Data ?? throw new InvalidOperationException("Invalid internal state.")).GetSpan().ToArray(),
                Is.EqualTo(new byte[] { 60, 120, 109, 112, 62 }));
        });

        (extracted.Data ?? throw new InvalidOperationException("Invalid internal state.")).Close();
    }

    [Test]
    public void RemuxExpandsMiniIntoRegularContainerWithoutReencodingPayload()
    {
        CanonicalItemContainerRequest request = CreateBaseRequest(false);
        (request.Items ?? throw new InvalidOperationException("Invalid internal state."))[0].Name = string.Empty;
        request.Options.HeaderFormat = AvifHeaderFormat.Minimized;
        CanonicalItemContainerWriteResult minimized = CanonicalItemContainerWriter.Write(request);
        AvifRewriteOptions options = CreateRewriteOptions();
        options.HeaderFormat = AvifHeaderFormat.Full;

        AvifRemuxResult result = AvifRemuxer.Rewrite(minimized.Data, options);
        Assert.That(
            result.Code,
            Is.EqualTo(AvifResultCode.Success),
            "input=" + (result.InputValidation ?? throw new InvalidOperationException("Invalid internal state.")).Code
                + " output=" + (result.OutputValidation == null
                    ? "null"
                    : result.OutputValidation.Code.ToString()
                        + " diagnostic=" + (result.OutputValidation.Diagnostics.Diagnostics.Count == 0
                            ? "none"
                            : (result.OutputValidation.Diagnostics.Get(0) ?? throw new InvalidOperationException("Invalid internal state.")).Message)));
        MetaParseResult parsed = MetaBoxParser.Parse(result.Data, CreateLimits());
        EncodedItemExtractionResult extracted = (parsed.Meta ?? throw new InvalidOperationException("Invalid internal state.")).ExtractEncodedItem(result.Data, 1);

        Assert.Multiple(() =>
        {
            Assert.That(minimized.Layout.UsesMinimizedFormat, Is.True);
            Assert.That(result.Code, Is.EqualTo(AvifResultCode.Success));
            Assert.That((parsed.Boxes ?? throw new InvalidOperationException("Invalid internal state.")).Boxes.Select(box => box.Type),
                Is.EqualTo(new string[] { "ftyp", "meta", "mdat" }));
            Assert.That((parsed.Meta.GetPrimaryItem() ?? throw new InvalidOperationException("Invalid internal state.")).GetPropertyCount(), Is.EqualTo(4));
            Assert.That((extracted.Data ?? throw new InvalidOperationException("Invalid internal state.")).GetSpan().ToArray(), Is.EqualTo(new byte[] { 1, 2, 3 }));
        });

        (extracted.Data ?? throw new InvalidOperationException("Invalid internal state.")).Close();
    }

    static byte[] CreateOpaqueDocument()
    {
        CanonicalItemContainerRequest request = CreateBaseRequest(true);
        CanonicalItemPayload metadata = CanonicalItemPayload.Create(
            9,
            "zzzz",
            "opaque",
            true,
            new byte[] { 3, 2, 1 });
        (request.Items ?? throw new InvalidOperationException("Invalid internal state.")).Add(metadata);
        (request.References ?? throw new InvalidOperationException("Invalid internal state.")).Add(new CanonicalItemReference(
            "cdsc",
            9,
            new List<uint> { 1 }));
        CanonicalItemContainerWriteResult written = CanonicalItemContainerWriter.Write(request);
        Assert.That(written.Code, Is.EqualTo(AvifResultCode.Success));
        return written.Data;
    }

    static CanonicalItemContainerRequest CreateBaseRequest(bool opaqueProperty)
    {
        CanonicalItemContainerOptions options = new CanonicalItemContainerOptions();
        options.MaximumOutputBytes = 131072;
        CanonicalItemPayload main = CanonicalItemPayload.Create(
            1,
            "av01",
            "main",
            false,
            new byte[] { 1, 2, 3 });
        AddRequiredProperties(main);
        if (opaqueProperty)
        {
            main.Properties.Add(new CanonicalItemPropertyAssociation(
                BuildRawProperty("zzzz", new byte[] { 6, 5, 4 }),
                false));
        }

        return CanonicalItemContainerRequest.Create(
            "avif",
            0,
            new List<string> { "mif1", "miaf" },
            1,
            new List<CanonicalItemPayload> { main },
            options);
    }

    static void AddRequiredProperties(CanonicalItemPayload item)
    {
        item.Properties.Add(new CanonicalItemPropertyAssociation(
            BuildRawProperty("av1C", new byte[] { 129, 13, 12, 0 }),
            true));

        byte[] spatial = new byte[12];
        BinaryPrimitives.WriteUInt32BigEndian(spatial.AsSpan(4, 4), 320);
        BinaryPrimitives.WriteUInt32BigEndian(spatial.AsSpan(8, 4), 240);
        item.Properties.Add(new CanonicalItemPropertyAssociation(
            BuildFullProperty("ispe", spatial),
            false));

        byte[] pixels = new byte[] { 0, 0, 0, 0, 3, 8, 8, 8 };
        item.Properties.Add(new CanonicalItemPropertyAssociation(
            BuildRawProperty("pixi", pixels),
            false));

        byte[] colour = new byte[11];
        colour[0] = (byte)'n';
        colour[1] = (byte)'c';
        colour[2] = (byte)'l';
        colour[3] = (byte)'x';
        BinaryPrimitives.WriteUInt16BigEndian(colour.AsSpan(4, 2), 1);
        BinaryPrimitives.WriteUInt16BigEndian(colour.AsSpan(6, 2), 13);
        BinaryPrimitives.WriteUInt16BigEndian(colour.AsSpan(8, 2), 6);
        colour[10] = 128;
        item.Properties.Add(new CanonicalItemPropertyAssociation(
            BuildRawProperty("colr", colour),
            true));
    }

    static byte[] BuildFullProperty(string type, byte[] payload)
    {
        return BuildRawProperty(type, payload);
    }

    static byte[] BuildRawProperty(string type, byte[] payload)
    {
        byte[] raw = new byte[8 + payload.Length];
        BinaryPrimitives.WriteUInt32BigEndian(raw, (uint)raw.Length);
        raw[4] = (byte)type[0];
        raw[5] = (byte)type[1];
        raw[6] = (byte)type[2];
        raw[7] = (byte)type[3];
        payload.CopyTo(raw, 8);
        return raw;
    }

    static byte[] AppendBox(byte[] source, string type, byte[] payload)
    {
        return AppendBytes(source, CreateBox(type, payload));
    }

    static byte[] CreateBox(string type, byte[] payload)
    {
        return BuildRawProperty(type, payload);
    }

    static byte[] AppendBytes(byte[] source, byte[] suffix)
    {
        byte[] result = new byte[source.Length + suffix.Length];
        source.CopyTo(result, 0);
        suffix.CopyTo(result, source.Length);
        return result;
    }

    static AvifRewriteOptions CreateRewriteOptions()
    {
        AvifRewriteOptions options = new AvifRewriteOptions();
        options.HeaderFormat = AvifHeaderFormat.Full;
        options.Canonicalize = true;
        options.Limits = CreateLimits();
        return options;
    }

    static AvifSecurityLimits CreateLimits()
    {
        AvifSecurityLimits limits = new AvifSecurityLimits();
        limits.MaxSourceBytes = 131072;
        limits.MaxTemporaryBytes = 131072;
        limits.MaxBoxCount = 512;
        limits.MaxItemCount = 32;
        limits.MaxPropertyCount = 64;
        limits.MaxReferenceCount = 64;
        return limits;
    }
}
