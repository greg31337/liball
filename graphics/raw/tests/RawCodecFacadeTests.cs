using raw;

namespace raw.Tests;

public class RawCodecFacadeTests
{
    [Test]
    public void GenericOpenBuildsPublicDocumentPayloadAndIndependentCapabilities()
    {
        RawDescriptor descriptor = RawDescriptor.CreateRgb(2, 1, 8, 8, RawByteOrder.LittleEndian);
        byte[] source = new byte[] { 1, 2, 3, 4, 5, 6 };
        RawDocument document = RawCodec.Open(source, descriptor, new RawOpenOptions());
        RawCapabilities capabilities = document.GetCapabilities();

        Assert.Multiple(() =>
        {
            Assert.That(document.Format, Is.EqualTo(RawFormat.GenericRaw));
            Assert.That(((document.Images ?? throw new System.InvalidOperationException("A required value is unavailable.")).Single().Kind), Is.EqualTo(RawImageKind.Raw));
            Assert.That(document.Images.Single().Channels.Select(channel => channel.Name), Is.EqualTo(new string[] { "R", "G", "B" }));
            Assert.That(document.PayloadMap.Blocks.Single().Path, Is.EqualTo("GenericRaw/Data"));
            Assert.That(document.ExtractPayload(document.PayloadMap.Blocks.Single()), Is.EqualTo(source));
            Assert.That(document.MetadataLoaded, Is.True);
            Assert.That(document.PreviewsLoaded, Is.True);
            Assert.That(document.RawPayloadAvailable, Is.True);
            Assert.That(document.SourceRetained, Is.True);
            Assert.That(document.OwnsSource, Is.False);
            Assert.That(document.FormatDetails, Is.Not.Null);
            Assert.That((document.FormatDetails ?? throw new System.InvalidOperationException("A required value is unavailable.")).Cr2Header, Is.Null);
            Assert.That(document.FormatDetails.Canon, Is.Null);
            Assert.That(document.FormatDetails.Nikon, Is.Null);
            Assert.That(capabilities.Decoded, Is.True);
            Assert.That(capabilities.Rendered, Is.True);
            Assert.That(capabilities.PreservingRewrite, Is.False);
            Assert.That(capabilities.Encoded, Is.True);
            Assert.That(capabilities, Is.Not.SameAs(document.Capabilities));
            Assert.That(document.Validate(new RawValidationOptions()).IsValid(), Is.True);
        });

        capabilities.Decoded = false;
        Assert.That(document.Capabilities.Decoded, Is.True);
    }

    [Test]
    public void DocumentValidationRejectsMissingFormatDetailsWithoutAParserFailure()
    {
        RawDescriptor descriptor = RawDescriptor.CreateGray(1, 1, 8, 8, RawByteOrder.LittleEndian);
        RawDocument document = RawCodec.Open(new byte[] { 42 }, descriptor, new RawOpenOptions());
        document.FormatDetails = null;

        RawValidationReport report = document.Validate(new RawValidationOptions());
        RawSensorImage decoded = document.Decode(new RawDecodeOptions());

        Assert.Multiple(() =>
        {
            Assert.That(report.IsValid(), Is.False);
            Assert.That(((report.Messages ?? throw new System.InvalidOperationException("A required value is unavailable.")).Any(message => message.Context == "Document.FormatDetails")), Is.True);
            Assert.That(decoded.IntegerSamples, Is.EqualTo(new ushort[] { 10794 }));
        });
    }

    [Test]
    public void IndependentlyOpenedDocumentsOwnDescriptorModelsAndDecodeDeterministicallyInParallel()
    {
        RawDescriptor descriptor = RawDescriptor.CreateBayer(2, 2, 8, 8, RawByteOrder.LittleEndian, RawCfaLayout.Rggb);
        byte[] source = new byte[] { 0, 85, 170, 255 };
        RawDocument first = RawCodec.Open(source, descriptor, new RawOpenOptions());
        RawDocument second = RawCodec.Open(source, descriptor, new RawOpenOptions());
        Task<ushort[]>[] tasks = new Task<ushort[]>[16];
        for (int index = 0; index < tasks.Length; index++)
        {
            RawDocument selected = index % 2 == 0 ? first : second;
            tasks[index] = Task.Run(() => selected.Decode(new RawDecodeOptions()).IntegerSamples);
        }

        Task.WaitAll(tasks);
        descriptor.Channels[0].Name = "Caller mutation";
        (descriptor.Cfa ?? throw new System.InvalidOperationException("A required value is unavailable.")).Colors[0] = RawCfaColor.White;
        (first.GenericDescriptor ?? throw new System.InvalidOperationException("A required value is unavailable.")).Channels[0].Name = "First mutation";
        (first.GenericDescriptor.Cfa ?? throw new System.InvalidOperationException("A required value is unavailable.")).Colors[0] = RawCfaColor.Blue;

        Assert.Multiple(() =>
        {
            Assert.That(first.GenericDescriptor, Is.Not.SameAs(descriptor));
            Assert.That(second.GenericDescriptor, Is.Not.SameAs(descriptor));
            Assert.That(first.GenericDescriptor, Is.Not.SameAs(second.GenericDescriptor));
            Assert.That((second.GenericDescriptor ?? throw new System.InvalidOperationException("A required value is unavailable.")).Channels[0].Name, Is.EqualTo("CFA"));
            Assert.That((second.GenericDescriptor.Cfa ?? throw new System.InvalidOperationException("A required value is unavailable.")).Colors[0], Is.EqualTo(RawCfaColor.Red));
            Assert.That(tasks.Select(task => task.Result), Is.All.EqualTo(new ushort[] { 0, 21845, 43690, 65535 }));
        });
    }

    [Test]
    public void MetadataAndPreviewOnlySelectionsNeverEnableRawDecode()
    {
        RawDescriptor descriptor = RawDescriptor.CreateGray(2, 1, 8, 8, RawByteOrder.LittleEndian);
        byte[] source = new byte[] { 10, 20 };
        RawDocument metadata = RawCodec.OpenMetadata(source, descriptor, new RawOpenOptions());
        RawDocument previews = RawCodec.OpenPreviews(source, descriptor, new RawOpenOptions());

        Assert.Multiple(() =>
        {
            Assert.That(metadata.MetadataLoaded, Is.True);
            Assert.That(metadata.PreviewsLoaded, Is.False);
            Assert.That(metadata.RawPayloadAvailable, Is.False);
            Assert.That(metadata.Capabilities.Decoded, Is.False);
            Assert.That(metadata.Capabilities.Rendered, Is.False);
            Assert.That(previews.MetadataLoaded, Is.False);
            Assert.That(previews.PreviewsLoaded, Is.True);
            Assert.That(previews.RawPayloadAvailable, Is.False);
            Assert.That(previews.Thumbnails, Is.Empty);
            Assert.That(previews.Capabilities.Decoded, Is.False);
            Assert.That(previews.Capabilities.Rendered, Is.False);
            Assert.Throws<RawUnsupportedException>(() => metadata.Decode(new RawDecodeOptions()));
            Assert.Throws<RawUnsupportedException>(() => previews.Decode(new RawDecodeOptions()));
        });
    }

    [Test]
    public void MetadataOnlyOpenDoesNotDecodeSamplesAndCodecValidationReportsInvalidStoredValues()
    {
        RawDescriptor descriptor = RawDescriptor.CreateGray(1, 1, 4, 8, RawByteOrder.LittleEndian);
        byte[] source = new byte[] { 0xFF };

        RawDocument metadata = RawCodec.OpenMetadata(source, descriptor, new RawOpenOptions());
        RawValidationReport validation = RawCodec.Validate(source, descriptor, new RawValidationOptions());

        Assert.Multiple(() =>
        {
            Assert.That(((metadata.Images ?? throw new System.InvalidOperationException("A required value is unavailable.")).Single().BitDepth), Is.EqualTo(4));
            Assert.That(metadata.RawPayloadAvailable, Is.False);
            Assert.That(((validation.Messages ?? throw new System.InvalidOperationException("A required value is unavailable.")).Single(message => message.Category == RawValidationCategory.Codec).Code), Is.EqualTo(RawDiagnosticCode.InvalidCodecData));
            Assert.Throws<RawFormatException>(() => RawCodec.Decode(source, descriptor, new RawDecodeOptions()));
        });
    }

    [Test]
    public void GenericDecodeNormalizesAndOptionallyPreservesExactStoredSamples()
    {
        RawDescriptor descriptor = RawDescriptor.CreateGray(3, 1, 4, 8, RawByteOrder.LittleEndian);
        descriptor.SampleKind = RawSampleKind.SignedInteger;
        int[] stored = new int[] { -8, 0, 7 };
        byte[] source = GenericRawEncoder.EncodeStored(descriptor, stored, 0, 3, new RawGenericEncodeOptions());
        RawDecodeOptions options = new RawDecodeOptions();
        options.PreserveStoredSamples = true;

        RawSensorImage decoded = RawCodec.Decode(source, descriptor, options);

        Assert.Multiple(() =>
        {
            Assert.That(decoded.IntegerSamples[0], Is.Zero);
            Assert.That(decoded.IntegerSamples[1], Is.EqualTo(34952));
            Assert.That(decoded.IntegerSamples[2], Is.EqualTo(65535));
            Assert.That((decoded.StoredImage ?? throw new System.InvalidOperationException("A required value is unavailable.")).SignedSamples, Is.EqualTo(stored));
            Assert.That(decoded.StoredImage.OwnsSamples, Is.True);
            Assert.That((decoded.BlackLevel ?? throw new System.InvalidOperationException("A required value is unavailable.")).GetValue(2, 0, 0), Is.Zero);
            Assert.That(decoded.WhiteLevels.Single(), Is.EqualTo(15));
            Assert.That(decoded.Validate(new RawLimits()).IsValid(), Is.True);
        });
    }

    [Test]
    public void CompleteRenderPipelineProducesEveryOutputDepthAndHonorsWhiteIsZero()
    {
        RawDescriptor rgbDescriptor = RawDescriptor.CreateRgb(2, 1, 8, 8, RawByteOrder.LittleEndian);
        byte[] rgbSource = new byte[] { 0, 128, 255, 255, 64, 0 };
        RawRenderOptions options = RawCameraRenderOptions(8);

        RawRgbImage bytes = RawCodec.Render(rgbSource, rgbDescriptor, options);
        options.OutputBitDepth = 16;
        RawRgbImage words = RawCodec.Render(rgbSource, rgbDescriptor, options);
        options.OutputBitDepth = 32;
        RawRgbImage floating = RawCodec.Render(rgbSource, rgbDescriptor, options);

        RawDescriptor whiteIsZero = RawDescriptor.CreateGray(2, 1, 8, 8, RawByteOrder.LittleEndian);
        whiteIsZero.Photometric = RawPhotometricInterpretation.WhiteIsZero;
        options.OutputBitDepth = 8;
        RawRgbImage inverted = RawCodec.Render(new byte[] { 0, 255 }, whiteIsZero, options);
        RawRenderOptions processedOptions = RawCameraRenderOptions(8);
        processedOptions.ExposureCompensation = 1.0;
        processedOptions.ShadowScale = 1.25;
        processedOptions.SaturationProtection = 0.5;
        processedOptions.CaptureSharpening = 0.25;
        processedOptions.ToneCurve = new double[] { 0.0, 0.35, 1.0 };
        processedOptions.Contrast = 1.25;
        processedOptions.GamutMode = RawGamutMode.Clip;
        processedOptions.OrderedDithering = true;
        processedOptions.OutputWidth = 1;
        processedOptions.OutputHeight = 1;
        RawRgbImage processed = RawCodec.Render(rgbSource, rgbDescriptor, processedOptions);

        Assert.Multiple(() =>
        {
            Assert.That(bytes.ByteSamples, Is.EqualTo(rgbSource));
            Assert.That(words.UShortSamples, Is.EqualTo(rgbSource.Select(value => (ushort)(value * 257))));
            Assert.That(floating.FloatingSamples[0], Is.Zero);
            Assert.That(floating.FloatingSamples[1], Is.EqualTo(128.0F / 255.0F).Within(0.000001F));
            Assert.That(floating.FloatingSamples[2], Is.EqualTo(1.0F));
            Assert.That(inverted.ByteSamples, Is.EqualTo(new byte[] { 255, 0 }));
            Assert.That(bytes.ColorSpace, Is.EqualTo(RawColorSpace.RawCamera));
            Assert.That(bytes.TransferFunction, Is.EqualTo(RawTransferFunction.Linear));
            Assert.That(bytes.Validate(new RawLimits()).IsValid(), Is.True);
            Assert.That(processed.Width, Is.EqualTo(1));
            Assert.That(processed.Height, Is.EqualTo(1));
            Assert.That(processed.ByteSamples, Is.Not.EqualTo(bytes.ByteSamples));
            Assert.Throws<RawProfileException>(() => RawCodec.Render(rgbSource, rgbDescriptor, new RawRenderOptions()));
        });
    }

    [Test]
    public void TiffPreviewOnlyOpenDiscoversDecodesAndExtractsReducedImagePayload()
    {
        byte[] pixels = new byte[] { 10, 20, 30, 40, 50, 60 };
        byte[] source = CreateReducedRgbTiff(pixels);

        RawDocument document = RawCodec.OpenPreviews(source, null, new RawOpenOptions());
        RawThumbnail thumbnail = document.GetThumbnail(0);
        RawRgbImage decoded = thumbnail.Decode(new RawLimits(), CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(document.Format, Is.EqualTo(RawFormat.Tiff));
            Assert.That(((document.Images ?? throw new System.InvalidOperationException("A required value is unavailable.")).Single().Kind), Is.EqualTo(RawImageKind.Thumbnail));
            Assert.That(document.Images.Single().Compression, Is.EqualTo(RawCompression.None));
            Assert.That(document.MetadataLoaded, Is.False);
            Assert.That(document.PreviewsLoaded, Is.True);
            Assert.That(document.RawPayloadAvailable, Is.False);
            Assert.That(document.Thumbnails.Length, Is.EqualTo(1));
            Assert.That(thumbnail.Kind, Is.EqualTo(RawThumbnailKind.Rgb8));
            Assert.That((thumbnail.Payload ?? throw new System.InvalidOperationException("A required value is unavailable.")).Kind, Is.EqualTo(RawPayloadKind.Thumbnail));
            Assert.That(decoded.ByteSamples, Is.EqualTo(pixels));
            Assert.That(document.ExtractPayload(thumbnail.Payload), Is.EqualTo(pixels));
            Assert.That(document.Capabilities.Inspected, Is.True);
            Assert.That(document.Capabilities.Decoded, Is.False);
            Assert.That(document.Capabilities.Rendered, Is.False);
            Assert.That(document.Capabilities.PreservingRewrite, Is.False);
            Assert.That(document.Capabilities.Encoded, Is.False);
            Assert.That(document.Capabilities.VendorConfirmed, Is.False);
            Assert.Throws<RawUnsupportedException>(() => document.Decode(new RawDecodeOptions()));
            Assert.That(document.Validate(new RawValidationOptions()).IsValid(), Is.True);
        });
    }

    [Test]
    public void StreamAndPathFacadesRetainOwnershipBoundariesAndUseExactWindows()
    {
        RawDescriptor descriptor = RawDescriptor.CreateGray(2, 1, 8, 8, RawByteOrder.LittleEndian);
        byte[] window = new byte[] { 11, 22 };
        ShortReadStream stream = new ShortReadStream(new byte[] { 90, 91, 11, 22, 92 }, 1);

        RawSensorImage streamed = RawCodec.Decode(stream, 2, 2, descriptor, new RawDecodeOptions());
        int trailing = stream.ReadByte();
        string path = Path.Combine(Path.GetTempPath(), "raw-codec-facade-" + Guid.NewGuid().ToString("N") + ".raw");
        try
        {
            File.WriteAllBytes(path, window);
            RawProbe probe = RawCodec.Probe(path, RawFormat.GenericRaw, true, new RawOpenOptions());
            RawDocument metadata = RawCodec.OpenMetadata(path, descriptor, new RawOpenOptions());
            RawValidationReport validation = RawCodec.Validate(path, descriptor, new RawValidationOptions());

            Assert.Multiple(() =>
            {
                Assert.That(streamed.IntegerSamples, Is.EqualTo(new ushort[] { 2827, 5654 }));
                Assert.That(stream.WasDisposed, Is.False);
                Assert.That(trailing, Is.EqualTo(92));
                Assert.That(probe.Format, Is.EqualTo(RawFormat.GenericRaw));
                Assert.That(metadata.OwnsSource, Is.True);
                Assert.That(metadata.RawPayloadAvailable, Is.False);
                Assert.That(validation.IsValid(), Is.True);
            });
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Test]
    public void InvalidSelectionsCancellationConflictsAndMutatedDocumentsFailPrecisely()
    {
        RawDescriptor descriptor = RawDescriptor.CreateGray(2, 1, 8, 8, RawByteOrder.LittleEndian);
        byte[] source = new byte[] { 1, 2 };
        RawOpenOptions noSelection = new RawOpenOptions();
        noSelection.ReadMetadata = false;
        noSelection.ReadPreviews = false;
        noSelection.ReadRawPayload = false;
        CancellationTokenSource cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        RawOpenOptions cancelled = new RawOpenOptions();
        cancelled.CancellationToken = cancellation.Token;
        RawDocument document = RawCodec.Open(source, descriptor, new RawOpenOptions());
        document.Images = null;
        RawValidationOptions semantic = new RawValidationOptions();
        RawDecodeOptions storedOnly = new RawDecodeOptions();
        storedOnly.NormalizeSamples = false;

        Assert.Multiple(() =>
        {
            Assert.Throws<RawFormatException>(() => RawCodec.Open(source, descriptor, noSelection));
            Assert.Throws<OperationCanceledException>(() => RawCodec.Open(source, descriptor, cancelled));
            Assert.Throws<RawUnsupportedException>(() => RawCodec.Decode(source, descriptor, storedOnly));
            Assert.That(RawCodec.GetCapabilities(RawFormat.Unknown, null).Recognized, Is.False);
            Assert.That(RawCodec.GetCapabilities(RawFormat.Tiff, null).Inspected, Is.True);
            Assert.That(((document.Validate(semantic).Messages ?? throw new System.InvalidOperationException("A required value is unavailable.")).Any(message => message.Context == "Document.Images")), Is.True);
            Assert.Throws<RawFormatException>(() => document.ExtractPayload(new RawPayloadBlock(RawPayloadKind.RawImage, "foreign", 0, 1, 0, 0, 0, RawCompression.None, true, true)));
            Assert.Throws<RawFormatException>(() => RawCodec.Open(CreateReducedRgbTiff(new byte[6]), descriptor, new RawOpenOptions()));
        });
    }

    private static RawRenderOptions RawCameraRenderOptions(int bitDepth)
    {
        RawRenderOptions options = new RawRenderOptions();
        options.DemosaicMode = RawDemosaicMode.None;
        options.WhiteBalanceMode = RawWhiteBalanceMode.None;
        options.HighlightMode = RawHighlightMode.Clip;
        options.ColorSpace = RawColorSpace.RawCamera;
        options.TransferFunction = RawTransferFunction.Linear;
        options.OutputBitDepth = bitDepth;
        options.ApplyOrientation = false;
        return options;
    }

    private static byte[] CreateReducedRgbTiff(byte[] pixels)
    {
        RawByteOrder order = RawByteOrder.LittleEndian;
        TiffLayoutReference[] reference = new TiffLayoutReference[] { new TiffLayoutReference(0, TiffLayoutReferenceKind.Block, "preview") };
        TiffLayoutIfdInput root = new TiffLayoutIfdInput("IFD0", new TiffLayoutEntryInput[]
        {
            LongEntry(254, 1, order, null),
            LongEntry(256, 2, order, null),
            LongEntry(257, 1, order, null),
            ShortEntry(258, new ushort[] { 8, 8, 8 }, order),
            ShortEntry(259, new ushort[] { 1 }, order),
            ShortEntry(262, new ushort[] { 2 }, order),
            LongEntry(273, 0, order, reference),
            ShortEntry(274, new ushort[] { 1 }, order),
            ShortEntry(277, new ushort[] { 3 }, order),
            LongEntry(278, 1, order, null),
            LongEntry(279, (uint)pixels.Length, order, null),
            ShortEntry(284, new ushort[] { 1 }, order),
            ShortEntry(339, new ushort[] { 1, 1, 1 }, order)
        }, null);
        TiffLayoutRequest request = new TiffLayoutRequest(false, order, "IFD0", new TiffLayoutIfdInput[] { root }, new TiffLayoutBlockInput[]
        {
            new TiffLayoutBlockInput("preview", TiffLayoutBlockKind.Strip, pixels.Length, pixels)
        });
        TiffLayoutPlan plan = TiffLayoutPlanner.Plan(request, new RawLimits(), CancellationToken.None);
        return TiffWriter.WriteToArray(plan, new RawLimits(), CancellationToken.None);
    }

    private static TiffLayoutEntryInput ShortEntry(ushort tag, ushort[] values, RawByteOrder order)
    {
        byte[] bytes = new byte[values.Length * 2];
        int index = 0;
        while (index < values.Length)
        {
            EndianWriter.WriteUInt16(bytes.AsSpan(index * 2, 2), values[index], order);
            index++;
        }
        return new TiffLayoutEntryInput(tag, TiffFieldType.Short, (ulong)values.Length, bytes, null);
    }

    private static TiffLayoutEntryInput LongEntry(ushort tag, uint value, RawByteOrder order, TiffLayoutReference[]? references)
    {
        byte[] bytes = new byte[4];
        EndianWriter.WriteUInt32(bytes, value, order);
        return new TiffLayoutEntryInput(tag, TiffFieldType.Long, 1, bytes, references);
    }

    private class ShortReadStream : Stream
    {
        private readonly MemoryStream Inner;
        private readonly int MaximumRead;
        public bool WasDisposed;

        public ShortReadStream(byte[] bytes, int maximumRead)
        {
            Inner = new MemoryStream(bytes, false);
            MaximumRead = maximumRead;
            WasDisposed = false;
        }

        public override bool CanRead => true;
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
            return Inner.Read(buffer, offset, Math.Min(count, MaximumRead));
        }

        public override int Read(Span<byte> buffer)
        {
            return Inner.Read(buffer.Slice(0, Math.Min(buffer.Length, MaximumRead)));
        }

        public override int ReadByte()
        {
            return Inner.ReadByte();
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
            WasDisposed = true;
            Inner.Dispose();
            base.Dispose(disposing);
        }
    }
}
