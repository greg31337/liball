using System.Buffers.Binary;
using System.Collections.Generic;

namespace avif.Tests;

public class AvifCapabilityFeatureTests
{
    [Test]
    public void ProbeAndInspectionReportFeaturesWithIndependentCapabilities()
    {
        AvifImage image = CreateImage(3, 2, 31);
        AvifEncodeResult encoded = Avif.Encode(image);
        image.Close();
        AvifProbeResult probe = Avif.Probe(encoded.GetData());
        AvifReadResult inspection = new AvifCodec().InspectBytes(
            encoded.GetData(),
            new AvifValidationOptions());
        AvifDocument? document = inspection.GetDocument();
        AvifCapabilityReport capabilities = inspection.GetCapabilities();

        Assert.Multiple(() =>
        {
            Assert.That(encoded.GetCode(), Is.EqualTo(AvifResultCode.Success));
            Assert.That(probe.GetCode(), Is.EqualTo(AvifResultCode.Success));
            Assert.That(inspection.GetCode(), Is.EqualTo(AvifResultCode.Success));
            Assert.That(capabilities.IsAvif(), Is.True);
            Assert.That(capabilities.HasImageItems(), Is.True);
            Assert.That(capabilities.HasImageSequence(), Is.False);
            Assert.That(capabilities.IsIntraOnly(), Is.False);
            Assert.That(capabilities.HasBaselineProfile(), Is.True);
            Assert.That(capabilities.HasAdvancedProfile(), Is.False);
            AssertSupported(probe.GetCapabilities(), AvifFeature.ImageItem);
            AssertSupported(inspection.GetCapabilities(), AvifFeature.ImageItem);
            AssertSupported(
                (document ?? throw new InvalidOperationException("Invalid internal state.")).GetCapabilities(),
                AvifFeature.ImageItem);
            Assert.That(
                inspection.GetCapabilities().HasFeature(AvifFeature.Grid),
                Is.False);
            Assert.That(inspection.GetCapabilities().GetFeature(-1), Is.Null);
            Assert.That(
                inspection.GetCapabilities().GetFeature(
                    inspection.GetCapabilities().GetFeatureCount()),
                Is.Null);
        });

        AvifFeatureCapability? first = inspection.GetCapabilities()
            .GetFeatureCapability(AvifFeature.ImageItem);
        (first ?? throw new InvalidOperationException("Invalid internal state."))
            .Writable = false;
        AvifFeatureCapability? second = inspection.GetCapabilities()
            .GetFeatureCapability(AvifFeature.ImageItem);
        Assert.That(
            (second ?? throw new InvalidOperationException("Invalid internal state.")).Writable,
            Is.True);
        AssertUniqueFeatures(inspection.GetCapabilities());
    }

    [Test]
    public void InspectionReportsCollectionsGridsLayersAndAuxiliaries()
    {
        AvifCapabilityReport collection = Inspect(CreateCollection());
        AvifCapabilityReport grid = Inspect(CreateGrid());
        AvifCapabilityReport layered = Inspect(CreateLayeredImage());
        AvifCapabilityReport auxiliary = Inspect(CreateAuxiliaryImage());

        Assert.Multiple(() =>
        {
            AssertSupported(collection, AvifFeature.ImageItem);
            AssertSupported(collection, AvifFeature.ImageCollection);
            AssertSupported(grid, AvifFeature.Grid);
            Assert.That(grid.HasFeature(AvifFeature.ImageCollection), Is.False);
            AssertSupported(layered, AvifFeature.LayeredImage);
            AssertSupported(layered, AvifFeature.ProgressiveImage);
            AssertSupported(auxiliary, AvifFeature.AuxiliaryImage);
        });
    }

    [Test]
    public void InspectionReportsDerivedTimedAndLowOverheadFeatures()
    {
        AvifCapabilityReport gainMap = Inspect(CreateGainMap());
        AvifCapabilityReport sampleTransform = Inspect(CreateSampleTransform());
        byte[] sequenceSource = CreateSequence();
        AvifCapabilityReport sequence = Inspect(sequenceSource);
        AvifCapabilityReport sequenceProbe = Avif.Probe(sequenceSource)
            .GetCapabilities();
        byte[] lowOverheadSource = CreateLowOverheadImage();
        AvifCapabilityReport lowOverhead = Inspect(lowOverheadSource);
        AvifCapabilityReport lowOverheadProbe = Avif.Probe(lowOverheadSource)
            .GetCapabilities();

        Assert.Multiple(() =>
        {
            AssertSupported(gainMap, AvifFeature.GainMap);
            AssertSupported(sampleTransform, AvifFeature.SampleTransform);
            AssertSupported(sequence, AvifFeature.ImageSequence);
            AssertSupported(sequenceProbe, AvifFeature.ImageSequence);
            AssertSupported(lowOverhead, AvifFeature.LowOverhead);
            AssertSupported(lowOverheadProbe, AvifFeature.LowOverhead);
        });
    }

    [Test]
    public void InspectionReportsFragmentedAndProtectedMediaAsPreservableOnly()
    {
        AvifImage image = CreateImage(2, 2, 59);
        AvifEncodeResult encoded = Avif.Encode(image);
        image.Close();
        byte[] fragmented = AppendEmptyBox(encoded.GetData(), "moof");
        byte[] protectedMedia = AppendEmptyBox(fragmented, "sinf");
        AvifCapabilityReport report = Avif.Read(protectedMedia)
            .GetCapabilities();

        Assert.Multiple(() =>
        {
            AssertPreservableOnly(report, AvifFeature.FragmentedMedia);
            AssertPreservableOnly(report, AvifFeature.ProtectedMedia);
        });
        AssertUniqueFeatures(report);
    }

    static AvifCapabilityReport Inspect(byte[] source)
    {
        AvifReadResult result = Avif.Read(source);
        Assert.That(result.GetCode(), Is.EqualTo(AvifResultCode.Success));
        return result.GetCapabilities();
    }

    static void AssertSupported(
        AvifCapabilityReport report,
        AvifFeature feature)
    {
        AvifFeatureCapability? capability = report.GetFeatureCapability(feature);
        Assert.That(report.HasFeature(feature), Is.True);
        Assert.That(capability, Is.Not.Null);
        Assert.Multiple(() =>
        {
            Assert.That(
                (capability ?? throw new InvalidOperationException("Invalid internal state.")).Feature,
                Is.EqualTo(feature));
            Assert.That(capability.Decodable, Is.True);
            Assert.That(capability.Renderable, Is.True);
            Assert.That(capability.Writable, Is.True);
            Assert.That(capability.Rewritable, Is.True);
            Assert.That(capability.PreservableOnly, Is.False);
        });
    }

    static void AssertPreservableOnly(
        AvifCapabilityReport report,
        AvifFeature feature)
    {
        AvifFeatureCapability? capability = report.GetFeatureCapability(feature);
        Assert.That(report.HasFeature(feature), Is.True);
        Assert.That(capability, Is.Not.Null);
        Assert.Multiple(() =>
        {
            Assert.That(
                (capability ?? throw new InvalidOperationException("Invalid internal state.")).Feature,
                Is.EqualTo(feature));
            Assert.That(capability.Decodable, Is.False);
            Assert.That(capability.Renderable, Is.False);
            Assert.That(capability.Writable, Is.False);
            Assert.That(capability.Rewritable, Is.False);
            Assert.That(capability.PreservableOnly, Is.True);
        });
    }

    static void AssertUniqueFeatures(AvifCapabilityReport report)
    {
        HashSet<AvifFeature> found = new HashSet<AvifFeature>();
        int index = 0;
        while (index < report.GetFeatureCount())
        {
            AvifFeatureCapability? capability = report.GetFeature(index);
            Assert.That(capability, Is.Not.Null);
            Assert.That(
                found.Add((capability ?? throw new InvalidOperationException("Invalid internal state.")).Feature),
                Is.True);
            index++;
        }
        Assert.That(found.Count, Is.EqualTo(report.GetFeatureCount()));
    }

    static byte[] CreateCollection()
    {
        AvifImage first = CreateImage(2, 2, 11);
        AvifImage second = CreateImage(2, 2, 23);
        AvifWriter writer = AvifWriter.Create();
        writer.AddImage(first);
        writer.AddImage(second);
        first.Close();
        second.Close();
        AvifEncodeResult result = writer.Finish();
        writer.Close();
        Assert.That(result.GetCode(), Is.EqualTo(AvifResultCode.Success));
        return result.GetData();
    }

    static byte[] CreateGrid()
    {
        AvifImage left = CreateImage(2, 2, 37);
        AvifImage right = CreateImage(2, 2, 41);
        AvifGrid grid = new AvifGrid(
            1,
            2,
            4,
            2,
            new AvifSecurityLimits());
        Assert.That(grid.SetCell(0, left), Is.EqualTo(AvifResultCode.Success));
        Assert.That(grid.SetCell(1, right), Is.EqualTo(AvifResultCode.Success));
        AvifWriter writer = AvifWriter.Create();
        writer.AddGrid(grid);
        left.Close();
        right.Close();
        AvifEncodeResult result = writer.Finish();
        writer.Close();
        Assert.That(result.GetCode(), Is.EqualTo(AvifResultCode.Success));
        return result.GetData();
    }

    static byte[] CreateLayeredImage()
    {
        AvifImage preview = CreateImage(2, 2, 43);
        AvifImage finalImage = CreateImage(3, 3, 47);
        AvifLayeredImage layered = new AvifLayeredImage(0, true);
        Assert.That(
            layered.AddLayer(preview),
            Is.EqualTo(AvifResultCode.Success));
        Assert.That(
            layered.AddLayer(finalImage),
            Is.EqualTo(AvifResultCode.Success));
        AvifWriter writer = AvifWriter.Create();
        writer.AddLayeredImage(layered);
        preview.Close();
        finalImage.Close();
        AvifEncodeResult result = writer.Finish();
        writer.Close();
        Assert.That(result.GetCode(), Is.EqualTo(AvifResultCode.Success));
        return result.GetData();
    }

    static byte[] CreateAuxiliaryImage()
    {
        AvifImage master = CreateImage(2, 2, 53);
        AvifImage alpha = CreateImage(2, 2, 211);
        AvifWriter writer = AvifWriter.Create();
        uint masterId = writer.AddImage(master);
        AvifAuxiliaryImage auxiliary = new AvifAuxiliaryImage(
            alpha,
            AvifAuxiliaryKind.Alpha,
            false);
        writer.AddAuxiliary(masterId, auxiliary);
        master.Close();
        alpha.Close();
        AvifEncodeResult result = writer.Finish();
        writer.Close();
        Assert.That(result.GetCode(), Is.EqualTo(AvifResultCode.Success));
        return result.GetData();
    }

    static byte[] CreateGainMap()
    {
        AvifImage baseImage = CreateImage(3, 2, 61);
        AvifImage gainImage = CreateImage(2, 1, 67);
        AvifGainMap gainMap = new AvifGainMap(
            gainImage,
            CreateGainMapMetadata(),
            null);
        AvifWriter writer = AvifWriter.Create();
        uint baseId = writer.AddImage(baseImage);
        writer.AddGainMap(baseId, gainMap);
        baseImage.Close();
        gainImage.Close();
        AvifEncodeResult result = writer.Finish();
        writer.Close();
        Assert.That(result.GetCode(), Is.EqualTo(AvifResultCode.Success));
        return result.GetData();
    }

    static byte[] CreateSampleTransform()
    {
        AvifImage first = CreateImage(2, 2, 71);
        AvifImage second = CreateImage(2, 2, 73);
        AvifSampleTransform transform = new AvifSampleTransform(
            new byte[] { 2, 3, 1, 2, 128 },
            new AvifImage[] { first, second },
            12);
        AvifWriter writer = AvifWriter.Create();
        writer.AddSampleTransform(transform);
        first.Close();
        second.Close();
        AvifEncodeResult result = writer.Finish();
        writer.Close();
        Assert.That(result.GetCode(), Is.EqualTo(AvifResultCode.Success));
        return result.GetData();
    }

    static byte[] CreateSequence()
    {
        AvifImage frameImage = CreateImage(2, 2, 79);
        AvifSequence sequence = new AvifSequence(1000);
        sequence.AddFrame(new AvifFrame(frameImage, null, 100, true));
        AvifWriter writer = AvifWriter.Create();
        writer.AddSequence(sequence);
        frameImage.Close();
        AvifEncodeResult result = writer.Finish();
        writer.Close();
        Assert.That(result.GetCode(), Is.EqualTo(AvifResultCode.Success));
        return result.GetData();
    }

    static byte[] CreateLowOverheadImage()
    {
        AvifImage image = CreateImage(2, 2, 83);
        AvifEncoderOptions options = new AvifEncoderOptions();
        options.HeaderFormat = AvifHeaderFormat.Minimized;
        AvifEncodeResult result = Avif.Encode(image, options);
        image.Close();
        Assert.That(result.GetCode(), Is.EqualTo(AvifResultCode.Success));
        return result.GetData();
    }

    static AvifGainMapMetadata CreateGainMapMetadata()
    {
        byte[] payload = new byte[62];
        payload[5] = 64;
        int offset = 6;
        WriteUnsigned(payload, offset, 0, 1);
        offset += 8;
        WriteUnsigned(payload, offset, 1, 1);
        offset += 8;
        WriteSigned(payload, offset, 0, 1);
        offset += 8;
        WriteSigned(payload, offset, 1, 1);
        offset += 8;
        WriteUnsigned(payload, offset, 1, 1);
        offset += 8;
        WriteSigned(payload, offset, 0, 1);
        offset += 8;
        WriteSigned(payload, offset, 0, 1);
        return new AvifGainMapMetadata(payload);
    }

    static void WriteSigned(
        byte[] payload,
        int offset,
        int numerator,
        uint denominator)
    {
        BinaryPrimitives.WriteInt32BigEndian(
            payload.AsSpan(offset, 4),
            numerator);
        BinaryPrimitives.WriteUInt32BigEndian(
            payload.AsSpan(offset + 4, 4),
            denominator);
    }

    static void WriteUnsigned(
        byte[] payload,
        int offset,
        uint numerator,
        uint denominator)
    {
        BinaryPrimitives.WriteUInt32BigEndian(
            payload.AsSpan(offset, 4),
            numerator);
        BinaryPrimitives.WriteUInt32BigEndian(
            payload.AsSpan(offset + 4, 4),
            denominator);
    }

    static byte[] AppendEmptyBox(byte[] source, string type)
    {
        byte[] result = new byte[source.Length + 8];
        source.CopyTo(result, 0);
        BinaryPrimitives.WriteUInt32BigEndian(
            result.AsSpan(source.Length, 4),
            8);
        int index = 0;
        while (index < 4)
        {
            result[source.Length + 4 + index] = (byte)type[index];
            index++;
        }
        return result;
    }

    static AvifImage CreateImage(int width, int height, byte value)
    {
        AvifImage image = new AvifImage(
            width,
            height,
            8,
            AvifChromaFormat.Monochrome,
            AvifRange.Full,
            AvifChromaSamplePosition.Unknown,
            AvifAlphaMode.None,
            1,
            new AvifSecurityLimits());
        AvifPlane? plane = image.GetPlane(AvifPlaneKind.Y);
        int row = 0;
        while (row < (plane ?? throw new InvalidOperationException("Invalid internal state.")).GetHeight())
        {
            plane.GetRowSpan(row).Fill(value);
            row++;
        }
        return image;
    }
}
