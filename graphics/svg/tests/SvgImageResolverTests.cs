namespace svg.Tests;

public class SvgImageResolverTests
{
    [Test]
    public void PngProbeReadsOnlyHeaderAndReturnsIntrinsicFormatFacts()
    {
        byte[] header = PngHeader(640, 480, 8, 6, 1);

        SvgPngProbeResult result = new SvgPngCodec().ProbeWithBytes(header);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.Error, Is.EqualTo(SvgPngProbeError.None));
            Assert.That(result.Width, Is.EqualTo(640));
            Assert.That(result.Height, Is.EqualTo(480));
            Assert.That(result.BitDepth, Is.EqualTo(8));
            Assert.That(result.ColorType, Is.EqualTo(6));
            Assert.That(result.InterlaceMethod, Is.EqualTo(1));
            Assert.That(result.HasAlpha, Is.True);
            Assert.That(result.IntrinsicSize.Width, Is.EqualTo(640));
            Assert.That(result.IntrinsicSize.AspectRatio, Is.EqualTo(4d / 3d));
        });
    }

    [Test]
    public void PngProbeRejectsTruncatedInvalidAndOversizedHeadersWithoutDecoding()
    {
        byte[] truncated = new byte[8];
        byte[] invalid = PngHeader(10, 10, 8, 6, 0);
        invalid[25] = 1;
        SvgRasterLimits limits = new SvgRasterLimits();
        limits.MaxWidth = 20;

        SvgPngProbeResult truncatedResult = new SvgPngCodec().ProbeWithBytesAndLimits(truncated, limits);
        SvgPngProbeResult invalidResult = new SvgPngCodec().ProbeWithBytesAndLimits(invalid, limits);
        SvgPngProbeResult oversizedResult = new SvgPngCodec().ProbeWithBytesAndLimits(PngHeader(21, 10, 8, 2, 0), limits);

        Assert.Multiple(() =>
        {
            Assert.That(truncatedResult.Error, Is.EqualTo(SvgPngProbeError.TruncatedHeader));
            Assert.That(invalidResult.Error, Is.EqualTo(SvgPngProbeError.InvalidHeader));
            Assert.That(oversizedResult.Error, Is.EqualTo(SvgPngProbeError.DimensionLimitExceeded));
        });
    }

    [Test]
    public void CallbackMetadataSizesImageWithoutDecodingBytes()
    {
        SvgNode image = Load("<svg xmlns='http://www.w3.org/2000/svg'><image href='photo.jpg'/></svg>", "https://example.test/root.svg").DocumentElement.Children[0];
        SvgResourceRequest observed = null;
        SvgEmbeddedContentResolveOptions options = EmbeddedOptions(request =>
        {
            observed = request;
            SvgResourceResponse response = ImageResponse(new byte[] { 255, 216, 255 }, request.NormalizedUri, SvgMimeTypes.Jpeg);
            response.ImageMetadata = new SvgImageMetadata();
            response.ImageMetadata.IntrinsicSize = SvgIntrinsicSize.Create(320, 180);
            return response;
        });

        SvgEmbeddedContentResult result = new SvgEmbeddedContentResolver().ResolveWithNodeAndOptions(image, options);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.Width, Is.EqualTo(320));
            Assert.That(result.Height, Is.EqualTo(180));
            Assert.That(result.SourceWidth, Is.EqualTo(320));
            Assert.That(result.SourceHeight, Is.EqualTo(180));
            Assert.That(result.ContentAvailable, Is.False);
            Assert.That(result.Renderable, Is.False);
            Assert.That(result.ImageResource.UsedCallbackMetadata, Is.True);
            Assert.That(result.ImageResource.UsedPngHeader, Is.False);
            Assert.That(result.HostContent.ImageResource, Is.SameAs(result.ImageResource));
            Assert.That(observed.ResourceKind, Is.EqualTo(SvgResourceKind.Image));
            Assert.That(observed.MediaKind, Is.EqualTo(SvgMimeTypes.Jpeg));
        });
    }

    [Test]
    public void PngHeaderSizesImageWhenCallbackDoesNotSupplyMetadata()
    {
        SvgNode image = Load("<svg xmlns='http://www.w3.org/2000/svg'><image href='photo.png' width='240'/></svg>", "https://example.test/root.svg").DocumentElement.Children[0];
        SvgEmbeddedContentResolveOptions options = EmbeddedOptions(request => ImageResponse(PngHeader(800, 600, 8, 2, 0), request.NormalizedUri, SvgMimeTypes.Png));

        SvgEmbeddedContentResult result = new SvgEmbeddedContentResolver().ResolveWithNodeAndOptions(image, options);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.Width, Is.EqualTo(240));
            Assert.That(result.Height, Is.EqualTo(180));
            Assert.That(result.IntrinsicAspectRatio, Is.EqualTo(4d / 3d));
            Assert.That(result.ImageResource.UsedPngHeader, Is.True);
            Assert.That(result.ImageResource.Acquisition.Bytes, Has.Length.EqualTo(33));
        });
    }

    [Test]
    public void ResolvedDocumentAcquiresImagesAsImagesAndAccountsForEncodedHeaderBytes()
    {
        SvgDocument document = Load("<svg xmlns='http://www.w3.org/2000/svg'><image href='asset.png'/></svg>", "https://example.test/root.svg");
        SvgResourceRequest observed = null;
        byte[] header = PngHeader(48, 32, 8, 6, 0);
        SvgResolveOptions options = new SvgResolveOptions();
        options.ResourceProvider = new SvgResourceProvider(request =>
        {
            observed = request;
            return ImageResponse(header, request.NormalizedUri, SvgMimeTypes.Png);
        });

        SvgResolvedDocument result = new SvgDocumentResolver().ResolveWithOptions(document, options);
        SvgEmbeddedContentResult image = result.Root.Children[0].EmbeddedContent;

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(observed.ResourceKind, Is.EqualTo(SvgResourceKind.Image));
            Assert.That(image.Width, Is.EqualTo(48));
            Assert.That(image.Height, Is.EqualTo(32));
            Assert.That(image.ImageResource.UsedPngHeader, Is.True);
            Assert.That(result.Resources.RequestCount, Is.EqualTo(1));
            Assert.That(result.Resources.LoadedCount, Is.EqualTo(1));
            Assert.That(result.Resources.EncodedBytes, Is.EqualTo(header.Length));
        });
    }

    [Test]
    public void CachedImageAcquisitionsRetainIndependentCallbackMetadata()
    {
        SvgDocument document = Load("<svg xmlns='http://www.w3.org/2000/svg'><image href='asset.jpg'/><image href='asset.jpg'/></svg>", "https://example.test/root.svg");
        int callbackCount = 0;
        SvgResolveOptions options = new SvgResolveOptions();
        options.ResourceProvider = new SvgResourceProvider(request =>
        {
            callbackCount = callbackCount + 1;
            SvgResourceResponse response = ImageResponse(new byte[] { 255, 216, 255 }, request.NormalizedUri, SvgMimeTypes.Jpeg);
            response.ImageMetadata = new SvgImageMetadata();
            response.ImageMetadata.IntrinsicSize = SvgIntrinsicSize.Create(12, 7);
            return response;
        });

        SvgResolvedDocument result = new SvgDocumentResolver().ResolveWithOptions(document, options);
        SvgEmbeddedContentResult first = result.Root.Children[0].EmbeddedContent;
        SvgEmbeddedContentResult second = result.Root.Children[1].EmbeddedContent;

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(callbackCount, Is.EqualTo(1));
            Assert.That(first.Width, Is.EqualTo(12));
            Assert.That(second.Width, Is.EqualTo(12));
            Assert.That(second.ImageResource.Acquisition.CacheHit, Is.True);
            Assert.That(second.ImageResource.UsedCallbackMetadata, Is.True);
            Assert.That(result.Resources.RequestCount, Is.EqualTo(1));
            Assert.That(result.Resources.LoadedCount, Is.EqualTo(1));
        });
    }

    [Test]
    public void DataPngIntrinsicSizeIsProbedWithoutAResourceProvider()
    {
        byte[] header = PngHeader(9, 5, 8, 2, 0);
        string uri = "data:image/png;base64," + System.Convert.ToBase64String(header);
        SvgDocument document = Load("<svg xmlns='http://www.w3.org/2000/svg'><image href='" + uri + "'/></svg>", "https://example.test/root.svg");

        SvgResolvedDocument result = new SvgDocumentResolver().ResolveWithOptions(document, new SvgResolveOptions());
        SvgEmbeddedContentResult image = result.Root.Children[0].EmbeddedContent;

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(image.Width, Is.EqualTo(9));
            Assert.That(image.Height, Is.EqualTo(5));
            Assert.That(image.ImageResource.Acquisition.IsDataUrl, Is.True);
            Assert.That(image.ImageResource.UsedPngHeader, Is.True);
            Assert.That(result.Resources.RequestCount, Is.Zero);
            Assert.That(result.Resources.LoadedCount, Is.EqualTo(1));
        });
    }

    [Test]
    public void NullReferenceReturnsAStableInvalidResult()
    {
        SvgImageResolveResult result = new SvgImageResolver().Resolve(null, new SvgImageResolveOptions());

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.Reference.Success, Is.False);
            Assert.That(result.Reference.Node, Is.Null);
            Assert.That(result.Diagnostics, Has.Count.EqualTo(1));
            Assert.That(result.Diagnostics[0].Code, Is.EqualTo(SvgDiagnosticCode.InvalidArgument));
        });
    }

    static SvgEmbeddedContentResolveOptions EmbeddedOptions(System.Func<SvgResourceRequest, SvgResourceResponse> callback)
    {
        SvgLoadOptions load = new SvgLoadOptions();
        load.ResourceProvider = new SvgResourceProvider(callback);
        SvgEmbeddedContentResolveOptions result = new SvgEmbeddedContentResolveOptions();
        result.ImageOptions = new SvgImageResolveOptions();
        result.ImageOptions.LoadOptions = load;
        result.ImageOptions.Context = new SvgNestedDocumentContext(load.Limits.Resources);
        return result;
    }

    static SvgResourceResponse ImageResponse(byte[] bytes, string finalUri, string mediaType)
    {
        SvgResourceResponse response = new SvgResourceResponse();
        response.Bytes = bytes;
        response.ActualMediaType = mediaType;
        response.FinalUri = finalUri;
        response.Cacheable = true;
        return response;
    }

    static SvgDocument Load(string source, string baseUri)
    {
        SvgLoadOptions options = new SvgLoadOptions();
        options.BaseUri = baseUri;
        SvgLoadResult result = new SvgLoader().LoadWithStringAndSvgLoadOptions(source, options);
        Assert.That(result.Success, Is.True);
        return result.Document;
    }

    static byte[] PngHeader(int width, int height, int bitDepth, int colorType, int interlace)
    {
        byte[] result = new byte[33];
        byte[] signature = new byte[] { 137, 80, 78, 71, 13, 10, 26, 10 };
        System.Array.Copy(signature, 0, result, 0, signature.Length);
        WriteUInt32(result, 8, 13);
        result[12] = 73;
        result[13] = 72;
        result[14] = 68;
        result[15] = 82;
        WriteUInt32(result, 16, (uint)width);
        WriteUInt32(result, 20, (uint)height);
        result[24] = (byte)bitDepth;
        result[25] = (byte)colorType;
        result[26] = 0;
        result[27] = 0;
        result[28] = (byte)interlace;
        WriteUInt32(result, 29, Crc(result, 12, 17));
        return result;
    }

    static uint Crc(byte[] bytes, int offset, int count)
    {
        uint crc = 4294967295;
        int index = offset;
        while (index < offset + count)
        {
            crc = crc ^ bytes[index];
            int bit = 0;
            while (bit < 8)
            {
                crc = (crc & 1) == 0 ? crc >> 1 : 3988292384 ^ crc >> 1;
                bit = bit + 1;
            }
            index = index + 1;
        }
        return crc ^ 4294967295;
    }

    static void WriteUInt32(byte[] bytes, int offset, uint value)
    {
        bytes[offset] = (byte)(value >> 24);
        bytes[offset + 1] = (byte)(value >> 16);
        bytes[offset + 2] = (byte)(value >> 8);
        bytes[offset + 3] = (byte)value;
    }
}
