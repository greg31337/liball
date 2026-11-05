using System.Buffers.Binary;

namespace avif.Tests;

public class AvifContainerValidationTests
{
    [Test]
    public void StrictValidationAcceptsAConformingImageItemContainer()
    {
        byte[] source = BuildDocument(true, false, true, false, false, false, false);

        AvifContainerValidationResult result = AvifContainerValidator.Validate(
            source,
            CreateOptions(AvifValidationMode.Strict));

        Assert.Multiple(() =>
        {
            Assert.That(result.Code, Is.EqualTo(AvifResultCode.Success));
            Assert.That((result.FileType ?? throw new InvalidOperationException("Invalid internal state.")).HasBrand("avif"), Is.True);
            Assert.That((result.Meta ?? throw new InvalidOperationException("Invalid internal state.")).GetPrimaryItemId(), Is.EqualTo(1));
            Assert.That(result.Diagnostics.Diagnostics, Is.Empty);
        });
    }

    [Test]
    public void ValidationModesDistinguishMandatoryConformanceFromPermissiveAcceptance()
    {
        byte[] source = BuildDocument(false, false, true, false, false, false, false);

        AvifContainerValidationResult strict = AvifContainerValidator.Validate(
            source,
            CreateOptions(AvifValidationMode.Strict));
        AvifContainerValidationResult compatible = AvifContainerValidator.Validate(
            source,
            CreateOptions(AvifValidationMode.Compatible));
        AvifContainerValidationResult permissive = AvifContainerValidator.Validate(
            source,
            CreateOptions(AvifValidationMode.Permissive));

        Assert.Multiple(() =>
        {
            Assert.That(strict.Code, Is.EqualTo(AvifResultCode.NonConformingData));
            Assert.That(compatible.Code, Is.EqualTo(AvifResultCode.NonConformingData));
            Assert.That(permissive.Code, Is.EqualTo(AvifResultCode.Success));
            Assert.That(FindDiagnostic(strict, "miaf").Severity, Is.EqualTo(AvifDiagnosticSeverity.Error));
            Assert.That(FindDiagnostic(compatible, "miaf").Severity, Is.EqualTo(AvifDiagnosticSeverity.Error));
            Assert.That(FindDiagnostic(permissive, "miaf").Severity, Is.EqualTo(AvifDiagnosticSeverity.Warning));
        });
    }

    [Test]
    public void RequiredBoxAndPropertyRulesProduceContextualFindings()
    {
        byte[] source = BuildDocument(true, true, false, false, false, false, false);

        AvifContainerValidationResult result = AvifContainerValidator.Validate(
            source,
            CreateOptions(AvifValidationMode.Strict));
        AvifDiagnostic firstBox = FindDiagnostic(result, "first box");
        AvifDiagnostic pixels = FindDiagnostic(result, "pixi");

        Assert.Multiple(() =>
        {
            Assert.That(result.Code, Is.EqualTo(AvifResultCode.NonConformingData));
            Assert.That(firstBox.BoxPath, Is.EqualTo("/ftyp"));
            Assert.That(firstBox.ByteOffset, Is.EqualTo(8));
            Assert.That(firstBox.StandardReference, Does.Contain("9.1.1"));
            Assert.That(pixels.ItemId, Is.EqualTo(1));
            Assert.That(pixels.Category, Is.EqualTo(AvifDiagnosticCategory.Validation));
        });
    }

    [Test]
    public void CompatibleModeReportsButAcceptsLateFileTypeBox()
    {
        byte[] source = BuildDocument(true, true, true, false, false, false, false);

        AvifContainerValidationResult strict = AvifContainerValidator.Validate(
            source,
            CreateOptions(AvifValidationMode.Strict));
        AvifContainerValidationResult compatible = AvifContainerValidator.Validate(
            source,
            CreateOptions(AvifValidationMode.Compatible));

        Assert.Multiple(() =>
        {
            Assert.That(strict.Code, Is.EqualTo(AvifResultCode.NonConformingData));
            Assert.That(compatible.Code, Is.EqualTo(AvifResultCode.Success));
            Assert.That(FindDiagnostic(strict, "first box").Severity,
                Is.EqualTo(AvifDiagnosticSeverity.Error));
            Assert.That(FindDiagnostic(compatible, "first box").Severity,
                Is.EqualTo(AvifDiagnosticSeverity.Warning));
        });
    }

    [Test]
    public void UnknownEssentialPropertiesRemainHardFailuresInEveryMode()
    {
        byte[] source = BuildDocument(true, false, true, true, false, false, false);

        AvifContainerValidationResult strict = AvifContainerValidator.Validate(
            source,
            CreateOptions(AvifValidationMode.Strict));
        AvifContainerValidationResult compatible = AvifContainerValidator.Validate(
            source,
            CreateOptions(AvifValidationMode.Compatible));
        AvifContainerValidationResult permissive = AvifContainerValidator.Validate(
            source,
            CreateOptions(AvifValidationMode.Permissive));

        Assert.Multiple(() =>
        {
            Assert.That(strict.Code, Is.EqualTo(AvifResultCode.UnsupportedEssentialProperty));
            Assert.That(compatible.Code, Is.EqualTo(AvifResultCode.UnsupportedEssentialProperty));
            Assert.That(permissive.Code, Is.EqualTo(AvifResultCode.UnsupportedEssentialProperty));
            Assert.That(FindDiagnostic(permissive, "unknown essential").ItemId, Is.EqualTo(1));
        });
    }

    [Test]
    public void AlternatePropertyVersionsAreOpaqueUnlessMarkedEssential()
    {
        byte[] nonEssentialSource = BuildDocument(
            true,
            false,
            true,
            false,
            false,
            false,
            false);
        SetBoxVersion(nonEssentialSource, "ispe", 1);
        byte[] essentialSource = (byte[])nonEssentialSource.Clone();
        SetAssociationEssential(essentialSource, 1);

        AvifContainerValidationResult nonEssential = AvifContainerValidator.Validate(
            nonEssentialSource,
            CreateOptions(AvifValidationMode.Strict));
        AvifContainerValidationResult essential = AvifContainerValidator.Validate(
            essentialSource,
            CreateOptions(AvifValidationMode.Permissive));

        Assert.Multiple(() =>
        {
            Assert.That(nonEssential.Code, Is.EqualTo(AvifResultCode.Success));
            Assert.That(((nonEssential.Meta ?? throw new InvalidOperationException("Invalid internal state.")).GetProperty(1) ?? throw new InvalidOperationException("Invalid internal state.")).GetSpatialExtents(), Is.Null);
            Assert.That(essential.Code, Is.EqualTo(AvifResultCode.UnsupportedEssentialProperty));
            Assert.That(FindDiagnostic(essential, "unknown essential").ItemId, Is.EqualTo(1));
        });
    }

    [Test]
    public void EssentialFlagsPixelAspectRatioAndTransformOrderingAreValidated()
    {
        byte[] source = BuildDocument(true, false, true, false, true, true, true);

        AvifContainerValidationResult result = AvifContainerValidator.Validate(
            source,
            CreateOptions(AvifValidationMode.Strict));

        Assert.Multiple(() =>
        {
            Assert.That(result.Code, Is.EqualTo(AvifResultCode.NonConformingData));
            Assert.That(FindDiagnostic(result, "a1op").Severity, Is.EqualTo(AvifDiagnosticSeverity.Error));
            Assert.That(FindDiagnostic(result, "square pixels").Severity, Is.EqualTo(AvifDiagnosticSeverity.Error));
            Assert.That(FindDiagnostic(result, "clap, irot, imir").Category,
                Is.EqualTo(AvifDiagnosticCategory.Rendering));
        });
    }

    [Test]
    public void ProfileBrandsEnforceCodedLimitsAndSelfContainment()
    {
        byte[] source = BuildProfileDocument();

        AvifContainerValidationResult result = AvifContainerValidator.Validate(
            source,
            CreateOptions(AvifValidationMode.Strict));

        Assert.Multiple(() =>
        {
            Assert.That(result.Code, Is.EqualTo(AvifResultCode.NonConformingData));
            Assert.That(FindDiagnostic(result, "MA1B").ItemId, Is.EqualTo(1));
            Assert.That(FindDiagnostic(result, "MA1B").StandardReference, Does.Contain("8.2"));
        });
    }

    [Test]
    public void GridInputsRejectTransformsAcrossTheDependencyChain()
    {
        byte[] source = BuildGridDocument();

        AvifContainerValidationResult result = AvifContainerValidator.Validate(
            source,
            CreateOptions(AvifValidationMode.Strict));
        AvifDiagnostic diagnostic = FindDiagnostic(result, "grid input");

        Assert.Multiple(() =>
        {
            Assert.That(result.Code, Is.EqualTo(AvifResultCode.NonConformingData));
            Assert.That(diagnostic.Category, Is.EqualTo(AvifDiagnosticCategory.Rendering));
            Assert.That(diagnostic.ItemId, Is.EqualTo(1));
            Assert.That(diagnostic.StandardReference, Does.Contain("section 7"));
        });
    }

    [Test]
    public void PayloadValidationReconcilesAv1ConfigurationAndBitstream()
    {
        byte[] source = BuildDocument(true, false, true, false, false, false, false);
        AvifValidationOptions options = CreateOptions(AvifValidationMode.Strict);
        options.ValidateEncodedPayload = true;

        AvifContainerValidationResult result = AvifContainerValidator.Validate(source, options);

        Assert.Multiple(() =>
        {
            Assert.That(result.Code, Is.EqualTo(AvifResultCode.InvalidAv1Bitstream));
            Assert.That(FindDiagnostic(result, "payload").Category, Is.EqualTo(AvifDiagnosticCategory.Av1));
            Assert.That(FindDiagnostic(result, "payload").ItemId, Is.EqualTo(1));
        });
    }

    private static byte[] BuildDocument(
        bool includeMiaf,
        bool freeBeforeFileType,
        bool includePixelInformation,
        bool unknownEssential,
        bool invalidOperatingPoint,
        bool invalidAspectRatio,
        bool invalidTransformOrder)
    {
        IsoBoxWriter writer = new IsoBoxWriter(0, 65536);
        if (freeBeforeFileType)
        {
            writer.BeginBox("free");
            writer.EndBox();
        }

        string[] brands = includeMiaf
            ? new string[] { "mif1", "miaf" }
            : new string[] { "mif1" };
        FileTypeBox.Write(writer, "avif", 0, brands);
        writer.BeginBox("mdat");
        writer.Write(new byte[] { 0, 0, 0, 0 });
        writer.EndBox();
        int payloadOffset = freeBeforeFileType ? 40 : 32;
        WriteMeta(
            writer,
            payloadOffset,
            includePixelInformation,
            unknownEssential,
            invalidOperatingPoint,
            invalidAspectRatio,
            invalidTransformOrder,
            false);
        IsoBoxWriteResult result = writer.Finish();
        Assert.That(result.Code, Is.EqualTo(AvifResultCode.Success));
        return result.Data;
    }

    private static byte[] BuildProfileDocument()
    {
        IsoBoxWriter writer = new IsoBoxWriter(0, 65536);
        FileTypeBox.Write(
            writer,
            "avif",
            0,
            new string[] { "mif1", "miaf", "MA1B" });
        writer.BeginBox("mdat");
        writer.Write(new byte[] { 0, 0, 0, 0 });
        writer.EndBox();
        WriteMeta(writer, 36, true, false, false, false, false, true);
        IsoBoxWriteResult result = writer.Finish();
        Assert.That(result.Code, Is.EqualTo(AvifResultCode.Success));
        return result.Data;
    }

    private static byte[] BuildGridDocument()
    {
        IsoBoxWriter writer = new IsoBoxWriter(0, 65536);
        FileTypeBox.Write(writer, "avif", 0, new string[] { "mif1", "miaf" });
        writer.BeginBox("mdat");
        writer.Write(new byte[] { 0, 0, 0, 0 });
        writer.EndBox();
        writer.BeginFullBox("meta", 0, 0);
        WriteHandler(writer);
        writer.BeginFullBox("pitm", 0, 0);
        WriteUInt16(writer, 2);
        writer.EndBox();
        writer.BeginFullBox("iloc", 1, 0);
        writer.Write(new byte[] { 68, 0 });
        WriteUInt16(writer, 2);
        int locationIndex = 0;
        while (locationIndex < 2)
        {
            WriteUInt16(writer, (ushort)(locationIndex + 1));
            WriteUInt16(writer, 0);
            WriteUInt16(writer, 0);
            WriteUInt16(writer, 1);
            WriteUInt32(writer, 32);
            WriteUInt32(writer, 4);
            locationIndex++;
        }

        writer.EndBox();
        writer.BeginFullBox("iinf", 1, 0);
        WriteUInt32(writer, 2);
        WriteItemInfoEntry(writer, 1, "av01");
        WriteItemInfoEntry(writer, 2, "grid");
        writer.EndBox();
        writer.BeginFullBox("iref", 0, 0);
        writer.BeginBox("dimg");
        WriteUInt16(writer, 2);
        WriteUInt16(writer, 1);
        WriteUInt16(writer, 1);
        writer.EndBox();
        writer.EndBox();
        writer.BeginBox("iprp");
        writer.BeginBox("ipco");
        WriteAv1Configuration(writer, false);
        WriteSpatialExtents(writer, 64, 32);
        WritePixelInformation(writer);
        writer.BeginBox("irot");
        writer.Write(new byte[] { 1 });
        writer.EndBox();
        writer.EndBox();
        writer.BeginFullBox("ipma", 0, 0);
        WriteUInt32(writer, 2);
        WriteUInt16(writer, 1);
        writer.Write(new byte[] { 4, 0x81, 2, 3, 4 });
        WriteUInt16(writer, 2);
        writer.Write(new byte[] { 1, 2 });
        writer.EndBox();
        writer.EndBox();
        writer.EndBox();
        IsoBoxWriteResult result = writer.Finish();
        Assert.That(result.Code, Is.EqualTo(AvifResultCode.Success));
        return result.Data;
    }

    private static void WriteMeta(
        IsoBoxWriter writer,
        int payloadOffset,
        bool includePixelInformation,
        bool unknownEssential,
        bool invalidOperatingPoint,
        bool invalidAspectRatio,
        bool invalidTransformOrder,
        bool highProfile)
    {
        writer.BeginFullBox("meta", 0, 0);
        WriteHandler(writer);
        writer.BeginFullBox("pitm", 0, 0);
        WriteUInt16(writer, 1);
        writer.EndBox();
        WriteLocation(writer, payloadOffset);
        WriteItemInfo(writer);
        writer.BeginBox("iprp");
        writer.BeginBox("ipco");
        WriteAv1Configuration(writer, highProfile);
        WriteSpatialExtents(writer, 64, 32);
        int propertyCount = 2;
        if (includePixelInformation)
        {
            WritePixelInformation(writer);
            propertyCount++;
        }

        if (unknownEssential)
        {
            writer.BeginBox("zzzz");
            writer.Write(new byte[] { 1 });
            writer.EndBox();
            propertyCount++;
        }

        if (invalidOperatingPoint)
        {
            writer.BeginBox("a1op");
            writer.Write(new byte[] { 0 });
            writer.EndBox();
            propertyCount++;
        }

        if (invalidAspectRatio)
        {
            writer.BeginBox("pasp");
            WriteUInt32(writer, 4);
            WriteUInt32(writer, 3);
            writer.EndBox();
            propertyCount++;
        }

        if (invalidTransformOrder)
        {
            writer.BeginBox("irot");
            writer.Write(new byte[] { 1 });
            writer.EndBox();
            writer.BeginBox("clap");
            WriteUInt32(writer, 32);
            WriteUInt32(writer, 1);
            WriteUInt32(writer, 16);
            WriteUInt32(writer, 1);
            WriteInt32(writer, -16);
            WriteUInt32(writer, 1);
            WriteInt32(writer, -8);
            WriteUInt32(writer, 1);
            writer.EndBox();
            propertyCount += 2;
        }

        writer.EndBox();
        writer.BeginFullBox("ipma", 0, 0);
        WriteUInt32(writer, 1);
        WriteUInt16(writer, 1);
        writer.Write(new byte[] { (byte)propertyCount });
        writer.Write(new byte[] { 0x81, 2 });
        int associationIndex = 3;
        if (includePixelInformation)
        {
            writer.Write(new byte[] { (byte)associationIndex });
            associationIndex++;
        }

        if (unknownEssential)
        {
            writer.Write(new byte[] { (byte)(0x80 | associationIndex) });
            associationIndex++;
        }

        if (invalidOperatingPoint)
        {
            writer.Write(new byte[] { (byte)associationIndex });
            associationIndex++;
        }

        if (invalidAspectRatio)
        {
            writer.Write(new byte[] { (byte)associationIndex });
            associationIndex++;
        }

        if (invalidTransformOrder)
        {
            writer.Write(new byte[] { (byte)associationIndex, (byte)(associationIndex + 1) });
        }

        writer.EndBox();
        writer.EndBox();
        writer.EndBox();
    }

    private static void WriteHandler(IsoBoxWriter writer)
    {
        writer.BeginFullBox("hdlr", 0, 0);
        byte[] fields = new byte[21];
        WriteFourCc(fields.AsSpan(4, 4), "pict");
        writer.Write(fields);
        writer.EndBox();
    }

    private static void WriteLocation(IsoBoxWriter writer, int payloadOffset)
    {
        writer.BeginFullBox("iloc", 1, 0);
        writer.Write(new byte[] { 68, 0 });
        WriteUInt16(writer, 1);
        WriteUInt16(writer, 1);
        WriteUInt16(writer, 0);
        WriteUInt16(writer, 0);
        WriteUInt16(writer, 1);
        WriteUInt32(writer, (uint)payloadOffset);
        WriteUInt32(writer, 4);
        writer.EndBox();
    }

    private static void WriteItemInfo(IsoBoxWriter writer)
    {
        writer.BeginFullBox("iinf", 1, 0);
        WriteUInt32(writer, 1);
        writer.BeginFullBox("infe", 3, 0);
        byte[] entry = new byte[11];
        BinaryPrimitives.WriteUInt32BigEndian(entry.AsSpan(0, 4), 1);
        WriteFourCc(entry.AsSpan(6, 4), "av01");
        writer.Write(entry);
        writer.EndBox();
        writer.EndBox();
    }

    private static void WriteItemInfoEntry(
        IsoBoxWriter writer,
        uint itemId,
        string itemType)
    {
        writer.BeginFullBox("infe", 3, 0);
        byte[] entry = new byte[11];
        BinaryPrimitives.WriteUInt32BigEndian(entry.AsSpan(0, 4), itemId);
        WriteFourCc(entry.AsSpan(6, 4), itemType);
        writer.Write(entry);
        writer.EndBox();
    }

    private static void WriteAv1Configuration(IsoBoxWriter writer, bool highProfile)
    {
        writer.BeginBox("av1C");
        writer.Write(new byte[]
        {
            0x81,
            (byte)((highProfile ? 1 : 0) << 5 | 4),
            (byte)(highProfile ? 0 : 0x0c),
            0
        });
        writer.EndBox();
    }

    private static void WriteSpatialExtents(IsoBoxWriter writer, uint width, uint height)
    {
        writer.BeginFullBox("ispe", 0, 0);
        WriteUInt32(writer, width);
        WriteUInt32(writer, height);
        writer.EndBox();
    }

    private static void WritePixelInformation(IsoBoxWriter writer)
    {
        writer.BeginFullBox("pixi", 0, 0);
        writer.Write(new byte[] { 3, 8, 8, 8 });
        writer.EndBox();
    }

    private static AvifValidationOptions CreateOptions(AvifValidationMode mode)
    {
        AvifValidationOptions options = new AvifValidationOptions();
        options.Mode = mode;
        options.ValidateEncodedPayload = false;
        options.Limits.MaxSourceBytes = 131072;
        options.Limits.MaxBoxDepth = 16;
        options.Limits.MaxBoxCount = 512;
        options.Limits.MaxItemCount = 64;
        options.Limits.MaxPropertyCount = 256;
        options.Limits.MaxReferenceCount = 256;
        options.Limits.MaxTemporaryBytes = 65536;
        return options;
    }

    private static AvifDiagnostic FindDiagnostic(
        AvifContainerValidationResult result,
        string text)
    {
        return result.Diagnostics.Diagnostics.Single(diagnostic =>
            diagnostic.Message.Contains(text, StringComparison.OrdinalIgnoreCase));
    }

    private static void SetBoxVersion(byte[] source, string type, byte version)
    {
        int index = FindFourCc(source, type);
        Assert.That(index, Is.GreaterThanOrEqualTo(0));
        source[index + 4] = version;
    }

    private static void SetAssociationEssential(byte[] source, int associationIndex)
    {
        int index = FindFourCc(source, "ipma");
        Assert.That(index, Is.GreaterThanOrEqualTo(0));
        source[index + 15 + associationIndex] |= 0x80;
    }

    private static int FindFourCc(byte[] source, string type)
    {
        int index = 0;
        while (index <= source.Length - 4)
        {
            if (source[index] == type[0]
                && source[index + 1] == type[1]
                && source[index + 2] == type[2]
                && source[index + 3] == type[3])
            {
                return index;
            }

            index++;
        }

        return -1;
    }

    private static void WriteUInt16(IsoBoxWriter writer, ushort value)
    {
        byte[] bytes = new byte[2];
        BinaryPrimitives.WriteUInt16BigEndian(bytes, value);
        writer.Write(bytes);
    }

    private static void WriteUInt32(IsoBoxWriter writer, uint value)
    {
        byte[] bytes = new byte[4];
        BinaryPrimitives.WriteUInt32BigEndian(bytes, value);
        writer.Write(bytes);
    }

    private static void WriteInt32(IsoBoxWriter writer, int value)
    {
        byte[] bytes = new byte[4];
        BinaryPrimitives.WriteInt32BigEndian(bytes, value);
        writer.Write(bytes);
    }

    private static void WriteFourCc(Span<byte> destination, string value)
    {
        destination[0] = (byte)value[0];
        destination[1] = (byte)value[1];
        destination[2] = (byte)value[2];
        destination[3] = (byte)value[3];
    }
}
