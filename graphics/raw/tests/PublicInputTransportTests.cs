using raw;

namespace raw.Tests;

public class PublicInputTransportTests
{
    [Test]
    public void EveryTransportProbesAndOpensEveryReadMode()
    {
        IRawCodec codec = new RawCodecService();
        byte[] source = new byte[] { 17, 239 };
        RawDescriptor descriptor = RawDescriptor.CreateGray(2, 1, 8, 8, RawByteOrder.LittleEndian);
        string path = CreateRawFile(source);

        try
        {
            RawProbe bytesProbe = codec.ProbeBytes(source, "sample.raw", RawFormat.GenericRaw, true, new RawOpenOptions());
            RawProbe spanProbe = codec.ProbeSpan(source.AsSpan(), "sample.raw", RawFormat.GenericRaw, true, new RawOpenOptions());
            TrackingNonSeekableStream probeStream = CreateBoundedStream(source);
            RawProbe streamProbe = codec.ProbeStream(probeStream, 2, source.Length, "sample.raw", RawFormat.GenericRaw, true, new RawOpenOptions());
            RawProbe pathProbe = codec.ProbePath(path, RawFormat.GenericRaw, true, new RawOpenOptions());

            RawDocument bytesOpen = codec.OpenBytes(source, descriptor, new RawOpenOptions());
            RawDocument spanOpen = codec.OpenSpan(source.AsSpan(), descriptor, new RawOpenOptions());
            TrackingNonSeekableStream openStream = CreateBoundedStream(source);
            RawDocument streamOpen = codec.OpenStream(openStream, 2, source.Length, descriptor, new RawOpenOptions());
            RawDocument pathOpen = codec.OpenPath(path, descriptor, new RawOpenOptions());

            RawDocument bytesMetadata = codec.OpenMetadataBytes(source, descriptor, new RawMetadataOptions());
            RawDocument spanMetadata = codec.OpenMetadataSpan(source.AsSpan(), descriptor, new RawMetadataOptions());
            TrackingNonSeekableStream metadataStream = CreateBoundedStream(source);
            RawDocument streamMetadata = codec.OpenMetadataStream(metadataStream, 2, source.Length, descriptor, new RawMetadataOptions());
            RawDocument pathMetadata = codec.OpenMetadataPath(path, descriptor, new RawMetadataOptions());

            RawDocument bytesPreviews = codec.OpenPreviewsBytes(source, descriptor, new RawOpenOptions());
            RawDocument spanPreviews = codec.OpenPreviewsSpan(source.AsSpan(), descriptor, new RawOpenOptions());
            TrackingNonSeekableStream previewsStream = CreateBoundedStream(source);
            RawDocument streamPreviews = codec.OpenPreviewsStream(previewsStream, 2, source.Length, descriptor, new RawOpenOptions());
            RawDocument pathPreviews = codec.OpenPreviewsPath(path, descriptor, new RawOpenOptions());

            Assert.Multiple(() =>
            {
                Assert.That(bytesProbe.Format, Is.EqualTo(RawFormat.GenericRaw));
                Assert.That(spanProbe.Format, Is.EqualTo(RawFormat.GenericRaw));
                Assert.That(streamProbe.Format, Is.EqualTo(RawFormat.GenericRaw));
                Assert.That(pathProbe.Format, Is.EqualTo(RawFormat.GenericRaw));
                Assert.That(probeStream.Disposed, Is.False);

                AssertFullDocument(bytesOpen, false);
                AssertFullDocument(spanOpen, true);
                AssertFullDocument(streamOpen, true);
                AssertFullDocument(pathOpen, true);
                Assert.That(openStream.Disposed, Is.False);

                AssertMetadataDocument(bytesMetadata, false);
                AssertMetadataDocument(spanMetadata, true);
                AssertMetadataDocument(streamMetadata, true);
                AssertMetadataDocument(pathMetadata, true);
                Assert.That(metadataStream.Disposed, Is.False);

                AssertPreviewDocument(bytesPreviews, false);
                AssertPreviewDocument(spanPreviews, true);
                AssertPreviewDocument(streamPreviews, true);
                AssertPreviewDocument(pathPreviews, true);
                Assert.That(previewsStream.Disposed, Is.False);
            });
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Test]
    public void EveryTransportDecodesRendersAndValidates()
    {
        IRawCodec codec = new RawCodecService();
        byte[] source = new byte[] { 0, 255 };
        RawDescriptor descriptor = RawDescriptor.CreateGray(2, 1, 8, 8, RawByteOrder.LittleEndian);
        string path = CreateRawFile(source);
        RawRenderOptions bytesRenderOptions = CreateRenderOptions();
        RawRenderOptions spanRenderOptions = CreateRenderOptions();
        RawRenderOptions streamRenderOptions = CreateRenderOptions();
        RawRenderOptions pathRenderOptions = CreateRenderOptions();

        try
        {
            RawSensorImage bytesDecoded = codec.DecodeBytes(source, descriptor, new RawDecodeOptions());
            RawSensorImage spanDecoded = codec.DecodeSpan(source.AsSpan(), descriptor, new RawDecodeOptions());
            TrackingNonSeekableStream decodeStream = CreateBoundedStream(source);
            RawSensorImage streamDecoded = codec.DecodeStream(decodeStream, 2, source.Length, descriptor, new RawDecodeOptions());
            RawSensorImage pathDecoded = codec.DecodePath(path, descriptor, new RawDecodeOptions());

            RawRgbImage bytesRendered = codec.RenderBytes(source, descriptor, bytesRenderOptions);
            RawRgbImage spanRendered = codec.RenderSpan(source.AsSpan(), descriptor, spanRenderOptions);
            TrackingNonSeekableStream renderStream = CreateBoundedStream(source);
            RawRgbImage streamRendered = codec.RenderStream(renderStream, 2, source.Length, descriptor, streamRenderOptions);
            RawRgbImage pathRendered = codec.RenderPath(path, descriptor, pathRenderOptions);

            RawValidationReport bytesValidation = codec.ValidateBytes(source, descriptor, new RawValidationOptions());
            RawValidationReport spanValidation = codec.ValidateSpan(source.AsSpan(), descriptor, new RawValidationOptions());
            TrackingNonSeekableStream validateStream = CreateBoundedStream(source);
            RawValidationReport streamValidation = codec.ValidateStream(validateStream, 2, source.Length, descriptor, new RawValidationOptions());
            RawValidationReport pathValidation = codec.ValidatePath(path, descriptor, new RawValidationOptions());

            Assert.Multiple(() =>
            {
                Assert.That(bytesDecoded.IntegerSamples, Is.EqualTo(new ushort[] { 0, 65535 }));
                Assert.That(spanDecoded.IntegerSamples, Is.EqualTo(bytesDecoded.IntegerSamples));
                Assert.That(streamDecoded.IntegerSamples, Is.EqualTo(bytesDecoded.IntegerSamples));
                Assert.That(pathDecoded.IntegerSamples, Is.EqualTo(bytesDecoded.IntegerSamples));
                Assert.That(decodeStream.Disposed, Is.False);

                Assert.That(bytesRendered.ByteSamples, Is.EqualTo(source));
                Assert.That(spanRendered.ByteSamples, Is.EqualTo(source));
                Assert.That(streamRendered.ByteSamples, Is.EqualTo(source));
                Assert.That(pathRendered.ByteSamples, Is.EqualTo(source));
                Assert.That(renderStream.Disposed, Is.False);

                Assert.That(bytesValidation.IsValid(), Is.True);
                Assert.That(spanValidation.IsValid(), Is.True);
                Assert.That(streamValidation.IsValid(), Is.True);
                Assert.That(pathValidation.IsValid(), Is.True);
                Assert.That(validateStream.Disposed, Is.False);
            });
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Test]
    public void TransportOperationsEnforceLimitsAndCancellationBeforeWork()
    {
        IRawCodec codec = new RawCodecService();
        byte[] source = new byte[] { 0, 255 };
        RawDescriptor descriptor = RawDescriptor.CreateGray(2, 1, 8, 8, RawByteOrder.LittleEndian);

        RawOpenOptions limitedOpen = new RawOpenOptions();
        SetOneByteInputLimits(limitedOpen.Limits ?? throw new InvalidOperationException());
        RawMetadataOptions limitedMetadata = new RawMetadataOptions();
        SetOneByteInputLimits(limitedMetadata.Limits ?? throw new InvalidOperationException());
        RawDecodeOptions limitedDecode = new RawDecodeOptions();
        SetOneByteInputLimits(limitedDecode.Limits ?? throw new InvalidOperationException());
        RawRenderOptions limitedRender = CreateRenderOptions();
        SetOneByteInputLimits(limitedRender.Limits);
        RawValidationOptions limitedValidation = new RawValidationOptions();
        SetOneByteInputLimits(limitedValidation.Limits);

        Assert.Multiple(() =>
        {
            Assert.Throws<RawLimitException>(() => codec.ProbeSpan(source.AsSpan(), "sample.raw", RawFormat.GenericRaw, true, limitedOpen));
            Assert.Throws<RawLimitException>(() => codec.OpenSpan(source.AsSpan(), descriptor, limitedOpen));
            Assert.Throws<RawLimitException>(() => codec.OpenMetadataSpan(source.AsSpan(), descriptor, limitedMetadata));
            Assert.Throws<RawLimitException>(() => codec.OpenPreviewsSpan(source.AsSpan(), descriptor, limitedOpen));
            Assert.Throws<RawLimitException>(() => codec.DecodeSpan(source.AsSpan(), descriptor, limitedDecode));
            Assert.Throws<RawLimitException>(() => codec.RenderSpan(source.AsSpan(), descriptor, limitedRender));
            Assert.Throws<RawLimitException>(() => codec.ValidateSpan(source.AsSpan(), descriptor, limitedValidation));
        });

        CancellationToken canceled = new CancellationToken(true);
        RawOpenOptions canceledOpen = new RawOpenOptions();
        canceledOpen.CancellationToken = canceled;
        RawMetadataOptions canceledMetadata = new RawMetadataOptions();
        canceledMetadata.CancellationToken = canceled;
        RawDecodeOptions canceledDecode = new RawDecodeOptions();
        canceledDecode.CancellationToken = canceled;
        RawRenderOptions canceledRender = CreateRenderOptions();
        canceledRender.CancellationToken = canceled;
        RawValidationOptions canceledValidation = new RawValidationOptions();
        canceledValidation.CancellationToken = canceled;

        Assert.Multiple(() =>
        {
            Assert.Throws<OperationCanceledException>(() => codec.ProbeBytes(source, "sample.raw", RawFormat.GenericRaw, true, canceledOpen));
            Assert.Throws<OperationCanceledException>(() => codec.OpenBytes(source, descriptor, canceledOpen));
            Assert.Throws<OperationCanceledException>(() => codec.OpenMetadataBytes(source, descriptor, canceledMetadata));
            Assert.Throws<OperationCanceledException>(() => codec.OpenPreviewsBytes(source, descriptor, canceledOpen));
            Assert.Throws<OperationCanceledException>(() => codec.DecodeBytes(source, descriptor, canceledDecode));
            Assert.Throws<OperationCanceledException>(() => codec.RenderBytes(source, descriptor, canceledRender));
            Assert.Throws<OperationCanceledException>(() => codec.ValidateBytes(source, descriptor, canceledValidation));
        });

        TrackingNonSeekableStream canceledStream = CreateBoundedStream(source);
        Assert.Throws<OperationCanceledException>(() => codec.ProbeStream(canceledStream, 2, source.Length, "sample.raw", RawFormat.GenericRaw, true, canceledOpen));
        Assert.Multiple(() =>
        {
            Assert.That(canceledStream.Disposed, Is.False);
            Assert.That(canceledStream.Inner.Position, Is.EqualTo(0));
        });
    }

    [Test]
    public void DeferredMemoryUsesCallerStorageWhileEagerMemoryTakesAnOwnedSnapshot()
    {
        IRawCodec codec = new RawCodecService();
        RawDescriptor descriptor = codec.CreateGrayDescriptor(2, 1, 8, 8, RawByteOrder.LittleEndian);
        byte[] deferredSource = new byte[] { 10, 20 };
        byte[] eagerSource = new byte[] { 10, 20 };
        RawOpenOptions deferredOptions = new RawOpenOptions();
        deferredOptions.DeferPayloads = true;
        RawOpenOptions eagerOptions = new RawOpenOptions();
        eagerOptions.DeferPayloads = false;

        RawDocument deferred = codec.OpenBytes(deferredSource, descriptor, deferredOptions);
        RawDocument eager = codec.OpenBytes(eagerSource, descriptor, eagerOptions);
        deferredSource[0] = 30;
        eagerSource[0] = 30;
        RawSensorImage deferredImage = codec.Decode(deferred, new RawDecodeOptions());
        RawSensorImage eagerImage = codec.Decode(eager, new RawDecodeOptions());
        RawPayloadBlock deferredPayload = deferred.PayloadMap.Blocks.Single();
        RawPayloadBlock eagerPayload = eager.PayloadMap.Blocks.Single();

        Assert.Multiple(() =>
        {
            Assert.That(deferred.OwnsSource, Is.False);
            Assert.That(eager.OwnsSource, Is.True);
            Assert.That(deferredImage.IntegerSamples, Is.EqualTo(new ushort[] { 7710, 5140 }));
            Assert.That(eagerImage.IntegerSamples, Is.EqualTo(new ushort[] { 2570, 5140 }));
            Assert.That(codec.ExtractDocumentPayload(deferred, deferredPayload, new RawLimits(), CancellationToken.None), Is.EqualTo(deferredSource));
            Assert.That(codec.ExtractDocumentPayload(eager, eagerPayload, new RawLimits(), CancellationToken.None), Is.EqualTo(new byte[] { 10, 20 }));
        });
    }

    [Test]
    public void ContentAndExtensionMismatchesRemainDiagnosticsAndGenericRawRequiresADescriptor()
    {
        IRawCodec codec = new RawCodecService();
        byte[] nef = NefParserTests.BuildNef().Bytes;
        RawProbe mismatch = codec.ProbeBytes(nef, "misnamed.cr2", RawFormat.Tiff, false, new RawOpenOptions());
        RawOpenOptions openOptions = new RawOpenOptions();
        openOptions.FileNameHint = "misnamed.cr2";
        RawDocument opened = codec.OpenBytes(nef, null, openOptions);
        byte[] generic = new byte[] { 1, 2 };
        RawProbe missingDescriptor = codec.ProbeBytes(generic, "pixels.raw", RawFormat.GenericRaw, false, new RawOpenOptions());
        RawDescriptor descriptor = codec.CreateGrayDescriptor(2, 1, 8, 8, RawByteOrder.LittleEndian);
        RawProbe genericMismatch = codec.ProbeBytes(generic, "pixels.nef", RawFormat.Unknown, true, new RawOpenOptions());
        RawFormatException? failure = Assert.Throws<RawFormatException>(() => codec.OpenBytes(generic, null, new RawOpenOptions()));

        Assert.Multiple(() =>
        {
            Assert.That(mismatch.Format, Is.EqualTo(RawFormat.Nef));
            Assert.That(mismatch.Diagnostics.Count(item => item.Code == RawDiagnosticCode.FormatMismatch), Is.EqualTo(2));
            Assert.That(opened.Format, Is.EqualTo(RawFormat.Nef));
            Assert.That(opened.Diagnostics.Count(item => item.Code == RawDiagnosticCode.FormatMismatch), Is.EqualTo(1));
            Assert.That(missingDescriptor.Format, Is.EqualTo(RawFormat.Unknown));
            Assert.That(missingDescriptor.GenericDescriptorRequired, Is.True);
            Assert.That(missingDescriptor.Diagnostics.Any(item => item.Message.Contains("requires a descriptor")), Is.True);
            Assert.That(genericMismatch.Format, Is.EqualTo(RawFormat.GenericRaw));
            Assert.That(genericMismatch.Diagnostics.Any(item => item.Code == RawDiagnosticCode.FormatMismatch), Is.True);
            Assert.That((failure ?? throw new InvalidOperationException()).Code, Is.EqualTo(RawDiagnosticCode.UnknownFormat));
            Assert.That(codec.OpenBytes(generic, descriptor, new RawOpenOptions()).Format, Is.EqualTo(RawFormat.GenericRaw));
        });
    }

    [Test]
    public void DecodeSelectionRejectsUnavailableImagesAndPlanesThroughThePublicCodec()
    {
        IRawCodec codec = new RawCodecService();
        RawDescriptor descriptor = codec.CreateGrayDescriptor(2, 1, 8, 8, RawByteOrder.LittleEndian);
        byte[] source = new byte[] { 0, 255 };
        RawDecodeOptions missingImage = new RawDecodeOptions();
        missingImage.ImageIndex = 1;
        RawDecodeOptions missingPlane = new RawDecodeOptions();
        missingPlane.PlaneIndex = 1;
        RawDocument metadataOnly = codec.OpenMetadataBytes(source, descriptor, new RawMetadataOptions());

        Assert.Multiple(() =>
        {
            Assert.That(codec.DecodeBytes(source, descriptor, new RawDecodeOptions()).IntegerSamples, Is.EqualTo(new ushort[] { 0, 65535 }));
            Assert.That((Assert.Throws<RawFormatException>(() => codec.DecodeBytes(source, descriptor, missingImage)) ?? throw new InvalidOperationException()).Code, Is.EqualTo(RawDiagnosticCode.InvalidArgument));
            Assert.That((Assert.Throws<RawFormatException>(() => codec.DecodeBytes(source, descriptor, missingPlane)) ?? throw new InvalidOperationException()).Code, Is.EqualTo(RawDiagnosticCode.InvalidArgument));
            Assert.That((Assert.Throws<RawUnsupportedException>(() => codec.Decode(metadataOnly, new RawDecodeOptions())) ?? throw new InvalidOperationException()).Code, Is.EqualTo(RawDiagnosticCode.UnsupportedCodec));
        });
    }

    [TestCase(13, RawCompression.NikonHe)]
    [TestCase(14, RawCompression.NikonHeStar)]
    public void UnsupportedCodecsRemainInspectableWithExplicitCapabilitiesDiagnosticsAndExceptions(int makerCode, RawCompression compression)
    {
        IRawCodec codec = new RawCodecService();
        NikonHeFixture fixture = NikonHeRecognitionTests.BuildNef((ushort)makerCode);
        RawDocument document = codec.OpenBytes(fixture.Bytes, null, new RawOpenOptions());
        RawImageInfo rawImage = (document.Images ?? Array.Empty<RawImageInfo>()).Single(item => item.Kind == RawImageKind.Raw);
        RawDiagnostic diagnostic = document.Diagnostics.Single(item => item.Code == RawDiagnosticCode.UnsupportedCodec);
        RawUnsupportedException? failure = Assert.Throws<RawUnsupportedException>(() => codec.Decode(document, new RawDecodeOptions()));

        Assert.Multiple(() =>
        {
            Assert.That(document.Format, Is.EqualTo(RawFormat.Nef));
            Assert.That(document.Capabilities.Recognized, Is.True);
            Assert.That(document.Capabilities.Inspected, Is.True);
            Assert.That(document.Capabilities.Decoded, Is.False);
            Assert.That(document.Capabilities.Rendered, Is.False);
            Assert.That(rawImage.Compression, Is.EqualTo(compression));
            Assert.That((rawImage.Payload ?? throw new InvalidOperationException()).Decodable, Is.False);
            Assert.That(codec.ExtractDocumentPayload(document, rawImage.Payload, new RawLimits(), CancellationToken.None), Is.EqualTo(fixture.Raw));
            Assert.That(diagnostic.Severity, Is.EqualTo(RawSeverity.Warning));
            Assert.That((failure ?? throw new InvalidOperationException()).Code, Is.EqualTo(RawDiagnosticCode.UnsupportedCodec));
            Assert.That(failure.Message, Is.EqualTo(diagnostic.Message));
        });
    }

    private static RawRenderOptions CreateRenderOptions()
    {
        RawRenderOptions options = new RawRenderOptions();
        options.DemosaicMode = RawDemosaicMode.None;
        options.WhiteBalanceMode = RawWhiteBalanceMode.None;
        options.ColorSpace = RawColorSpace.RawCamera;
        options.TransferFunction = RawTransferFunction.Linear;
        options.OutputBitDepth = 8;
        options.ApplyOrientation = false;
        return options;
    }

    private static string CreateRawFile(byte[] source)
    {
        string path = Path.Combine(TestContext.CurrentContext.WorkDirectory, "transport-" + Guid.NewGuid().ToString("N") + ".raw");
        File.WriteAllBytes(path, source);
        return path;
    }

    private static TrackingNonSeekableStream CreateBoundedStream(byte[] source)
    {
        byte[] bounded = new byte[source.Length + 4];
        bounded[0] = 91;
        bounded[1] = 92;
        source.CopyTo(bounded, 2);
        bounded[bounded.Length - 2] = 93;
        bounded[bounded.Length - 1] = 94;
        return new TrackingNonSeekableStream(bounded);
    }

    private static void AssertFullDocument(RawDocument document, bool ownsSource)
    {
        Assert.That(document.Format, Is.EqualTo(RawFormat.GenericRaw));
        Assert.That(document.MetadataLoaded, Is.True);
        Assert.That(document.PreviewsLoaded, Is.True);
        Assert.That(document.RawPayloadAvailable, Is.True);
        Assert.That(document.SourceRetained, Is.True);
        Assert.That(document.OwnsSource, Is.EqualTo(ownsSource));
    }

    private static void AssertMetadataDocument(RawDocument document, bool ownsSource)
    {
        Assert.That(document.MetadataLoaded, Is.True);
        Assert.That(document.PreviewsLoaded, Is.False);
        Assert.That(document.RawPayloadAvailable, Is.False);
        Assert.That(document.OwnsSource, Is.EqualTo(ownsSource));
    }

    private static void AssertPreviewDocument(RawDocument document, bool ownsSource)
    {
        Assert.That(document.MetadataLoaded, Is.False);
        Assert.That(document.PreviewsLoaded, Is.True);
        Assert.That(document.RawPayloadAvailable, Is.False);
        Assert.That(document.OwnsSource, Is.EqualTo(ownsSource));
    }

    private static void SetOneByteInputLimits(RawLimits limits)
    {
        limits.MaxFileSizeBytes = 1;
        limits.MaxTagValueBytes = 1;
        limits.MaxMetadataBytes = 1;
        limits.MaxXmpBytes = 1;
        limits.MaxPreviewBytes = 1;
    }
}

internal class TrackingNonSeekableStream : Stream
{
    internal MemoryStream Inner;
    internal bool Disposed;

    internal TrackingNonSeekableStream(byte[] bytes)
    {
        Inner = new MemoryStream(bytes, false);
        Disposed = false;
    }

    public override bool CanRead => Inner.CanRead;
    public override bool CanSeek => false;
    public override bool CanWrite => false;
    public override long Length => throw new NotSupportedException();
    public override long Position
    {
        get => throw new NotSupportedException();
        set => throw new NotSupportedException();
    }

    public override void Flush()
    {
    }

    public override int Read(byte[] buffer, int offset, int count)
    {
        return Inner.Read(buffer, offset, count);
    }

    public override int Read(Span<byte> buffer)
    {
        return Inner.Read(buffer);
    }

    public override long Seek(long offset, SeekOrigin origin)
    {
        throw new NotSupportedException();
    }

    public override void SetLength(long value)
    {
        throw new NotSupportedException();
    }

    public override void Write(byte[] buffer, int offset, int count)
    {
        throw new NotSupportedException();
    }

    protected override void Dispose(bool disposing)
    {
        Disposed = true;
        base.Dispose(disposing);
    }
}
