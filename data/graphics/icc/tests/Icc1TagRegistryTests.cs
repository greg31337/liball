using icc;

namespace icc.Tests;

public class Icc1TagRegistryTests
{
    [Test]
    public void CoversEveryVersion2AndVersion4PublicOrLegacyTagExactlyOnce()
    {
        uint[] version2 = GetAvailableSignatures(IccVersionKind.Version2);
        uint[] version4 = GetAvailableSignatures(IccVersionKind.Version4);

        Assert.Multiple(() =>
        {
            Assert.That(Icc1TagRegistry.Definitions.Select(item => item.TagSignature), Is.Unique);
            Assert.That(Icc1TagRegistry.Definitions, Has.Length.EqualTo(66));
            Assert.That(version2, Is.EquivalentTo(new uint[]
            {
                IccTagSignatures.AToB0, IccTagSignatures.AToB1, IccTagSignatures.AToB2,
                IccTagSignatures.BToA0, IccTagSignatures.BToA1, IccTagSignatures.BToA2,
                IccTagSignatures.BlueMatrixColumn, IccTagSignatures.GreenMatrixColumn, IccTagSignatures.RedMatrixColumn,
                IccTagSignatures.BlueTrc, IccTagSignatures.GreenTrc, IccTagSignatures.RedTrc, IccTagSignatures.GrayTrc,
                IccTagSignatures.CalibrationDateTime, IccTagSignatures.CharacterizationTarget,
                IccTagSignatures.ChromaticAdaptation, IccTagSignatures.Chromaticity,
                IccTagSignatures.ColorantOrder, IccTagSignatures.ColorantTable,
                IccTagSignatures.Copyright, IccTagSignatures.CrdInfo,
                IccTagSignatures.DeviceManufacturerDescription, IccTagSignatures.DeviceModelDescription,
                IccTagSignatures.DeviceSettings, IccTagSignatures.Gamut, IccTagSignatures.Luminance,
                IccTagSignatures.Measurement, IccTagSignatures.MediaBlackPoint, IccTagSignatures.MediaWhitePoint,
                IccTagSignatures.NamedColor, IccTagSignatures.NamedColor2, IccTagSignatures.OutputResponse,
                IccTagSignatures.Preview0, IccTagSignatures.Preview1, IccTagSignatures.Preview2,
                IccTagSignatures.ProfileDescription, IccTagSignatures.ProfileSequenceDescription,
                IccTagSignatures.PostScriptCrd0, IccTagSignatures.PostScriptCrd1,
                IccTagSignatures.PostScriptCrd2, IccTagSignatures.PostScriptCrd3,
                IccTagSignatures.PostScriptCsa, IccTagSignatures.PostScriptRenderingIntent,
                IccTagSignatures.ScreeningDescription, IccTagSignatures.Screening,
                IccTagSignatures.Technology, IccTagSignatures.UcrBg,
                IccTagSignatures.ViewingConditionDescription, IccTagSignatures.ViewingConditions,
                IccTagSignatures.EmbeddedIccMaxProfile
            }));
            Assert.That(version4, Is.EquivalentTo(new uint[]
            {
                IccTagSignatures.AToB0, IccTagSignatures.AToB1, IccTagSignatures.AToB2,
                IccTagSignatures.BToA0, IccTagSignatures.BToA1, IccTagSignatures.BToA2,
                IccTagSignatures.BToD0, IccTagSignatures.BToD1, IccTagSignatures.BToD2, IccTagSignatures.BToD3,
                IccTagSignatures.DToB0, IccTagSignatures.DToB1, IccTagSignatures.DToB2, IccTagSignatures.DToB3,
                IccTagSignatures.BlueMatrixColumn, IccTagSignatures.GreenMatrixColumn, IccTagSignatures.RedMatrixColumn,
                IccTagSignatures.BlueTrc, IccTagSignatures.GreenTrc, IccTagSignatures.RedTrc, IccTagSignatures.GrayTrc,
                IccTagSignatures.CalibrationDateTime, IccTagSignatures.CharacterizationTarget,
                IccTagSignatures.ChromaticAdaptation, IccTagSignatures.Chromaticity, IccTagSignatures.Cicp,
                IccTagSignatures.ColorantOrder, IccTagSignatures.ColorantTable, IccTagSignatures.ColorantTableOut,
                IccTagSignatures.ColorimetricIntentImageState, IccTagSignatures.Copyright,
                IccTagSignatures.DeviceManufacturerDescription, IccTagSignatures.DeviceModelDescription,
                IccTagSignatures.Gamut, IccTagSignatures.Luminance, IccTagSignatures.Measurement,
                IccTagSignatures.Metadata, IccTagSignatures.MediaWhitePoint, IccTagSignatures.NamedColor2,
                IccTagSignatures.OutputResponse, IccTagSignatures.PerceptualRenderingIntentGamut,
                IccTagSignatures.SaturationRenderingIntentGamut,
                IccTagSignatures.Preview0, IccTagSignatures.Preview1, IccTagSignatures.Preview2,
                IccTagSignatures.ProfileDescription, IccTagSignatures.ProfileSequenceDescription,
                IccTagSignatures.ProfileSequenceIdentifier, IccTagSignatures.Technology,
                IccTagSignatures.ViewingConditionDescription, IccTagSignatures.ViewingConditions,
                IccTagSignatures.EmbeddedIccMaxProfile, IccTagSignatures.AdaptiveGainCurve
            }));
        });
    }

    [Test]
    public void MapsGenerationSpecificPermittedTypes()
    {
        Icc1TagDefinition forward = Find(IccTagSignatures.AToB0);
        Icc1TagDefinition reverse = Find(IccTagSignatures.BToA0);
        Icc1TagDefinition copyright = Find(IccTagSignatures.Copyright);
        Icc1TagDefinition toneCurve = Find(IccTagSignatures.RedTrc);

        Assert.Multiple(() =>
        {
            Assert.That(forward.Version2Types, Is.EqualTo(new[] { IccTypeSignatures.Lut8, IccTypeSignatures.Lut16 }));
            Assert.That(forward.Version4Types, Is.EqualTo(new[] { IccTypeSignatures.Lut8, IccTypeSignatures.Lut16, IccTypeSignatures.LutAToB }));
            Assert.That(reverse.Version4Types, Does.Contain(IccTypeSignatures.LutBToA).And.Not.Contain(IccTypeSignatures.LutAToB));
            Assert.That(copyright.Version2Types, Is.EqualTo(new[] { IccTypeSignatures.Text }));
            Assert.That(copyright.Version4Types, Is.EqualTo(new[] { IccTypeSignatures.MultiLocalizedUnicode }));
            Assert.That(toneCurve.Version2Types, Is.EqualTo(new[] { IccTypeSignatures.Curve }));
            Assert.That(toneCurve.Version4Types, Is.EqualTo(new[] { IccTypeSignatures.Curve, IccTypeSignatures.ParametricCurve }));
            Assert.That(Find(IccTagSignatures.MediaBlackPoint).Version4Types, Is.Empty);
            Assert.That(Find(IccTagSignatures.Metadata).Version2Types, Is.Empty);
            Assert.That(Find(IccTagSignatures.CrdInfo).Version4Types, Is.Empty);
            Assert.That(TagCodecDispatcher.IsTypeSupportedByGeneration(IccTypeSignatures.ResponseCurveSet16, IccVersionKind.Version2), Is.True);
            Assert.That(TagCodecDispatcher.IsTypeSupportedByGeneration(IccTypeSignatures.EmbeddedProfile, IccVersionKind.Version2), Is.True);
        });
    }

    [Test]
    public void RecordsRolesRenderingIntentsAndProfileClassSemantics()
    {
        Icc1TagDefinition aToB2 = Find(IccTagSignatures.AToB2);
        Icc1TagDefinition bToD3 = Find(IccTagSignatures.BToD3);
        Icc1TagDefinition matrix = Find(IccTagSignatures.RedMatrixColumn);
        Icc1TagDefinition colorantOut = Find(IccTagSignatures.ColorantTableOut);
        Icc1TagDefinition cicp = Find(IccTagSignatures.Cicp);

        Assert.Multiple(() =>
        {
            Assert.That(aToB2.Role, Is.EqualTo(Icc1TagRole.ForwardTransform));
            Assert.That(aToB2.RenderingIntent, Is.EqualTo(2));
            Assert.That(aToB2.InputChannels, Is.EqualTo(Icc1ChannelSource.DataColorSpace));
            Assert.That(aToB2.OutputChannels, Is.EqualTo(Icc1ChannelSource.ProfileConnectionSpace));
            Assert.That(bToD3.Role, Is.EqualTo(Icc1TagRole.ReverseTransform));
            Assert.That(bToD3.RenderingIntent, Is.EqualTo(3));
            Assert.That(matrix.Role, Is.EqualTo(Icc1TagRole.MatrixColumn));
            Assert.That(matrix.RequiredDataChannels, Is.EqualTo(3));
            Assert.That(matrix.ExpectedValueCount, Is.EqualTo(1));
            Assert.That(matrix.AssociatedChannels, Is.EqualTo(Icc1ChannelSource.DataColorSpace));
            Assert.That(Icc1TagRegistry.IsProfileClassAllowed(aToB2, IccProfileClass.ColorSpace), Is.True);
            Assert.That(Icc1TagRegistry.IsProfileClassAllowed(aToB2, IccProfileClass.Abstract), Is.False);
            Assert.That(Icc1TagRegistry.IsProfileClassAllowed(colorantOut, IccProfileClass.DeviceLink), Is.True);
            Assert.That(Icc1TagRegistry.IsProfileClassAllowed(colorantOut, IccProfileClass.Output), Is.False);
            Assert.That(Icc1TagRegistry.IsProfileClassAllowed(cicp, IccProfileClass.Input), Is.True);
            Assert.That(Icc1TagRegistry.IsProfileClassAllowed(cicp, IccProfileClass.Output), Is.False);
        });
    }

    [Test]
    public void ResolvesTransformAndAssociatedChannelExpectationsFromProfileContext()
    {
        TagDecodeState display = CreateState(IccVersionKind.Version4, IccTagSignatures.ColorantOrder, IccProfileClass.Display, IccColorSpaceSignatures.Rgb, IccColorSpaceSignatures.Xyz, true);
        TagDecodeState deviceLink = CreateState(IccVersionKind.Version4, IccTagSignatures.ColorantOrder, IccProfileClass.DeviceLink, IccColorSpaceSignatures.Rgb, IccColorSpaceSignatures.Cmyk, true);
        Icc1TagDefinition colorantOrder = Find(IccTagSignatures.ColorantOrder);
        Icc1TagDefinition colorantTable = Find(IccTagSignatures.ColorantTable);
        Icc1TagDefinition colorantTableOut = Find(IccTagSignatures.ColorantTableOut);
        Icc1TagDefinition namedColor = Find(IccTagSignatures.NamedColor2);

        Assert.Multiple(() =>
        {
            Assert.That(colorantOrder.AssociatedChannels, Is.EqualTo(Icc1ChannelSource.DataOrDeviceLinkOutput));
            Assert.That(Icc1TagRegistry.ResolveChannelCount(colorantOrder.AssociatedChannels, display), Is.EqualTo(3));
            Assert.That(Icc1TagRegistry.ResolveChannelCount(colorantOrder.AssociatedChannels, deviceLink), Is.EqualTo(4));
            Assert.That(colorantTable.AssociatedChannels, Is.EqualTo(Icc1ChannelSource.DataColorSpace));
            Assert.That(colorantTableOut.AssociatedChannels, Is.EqualTo(Icc1ChannelSource.ProfileConnectionSpace));
            Assert.That(namedColor.AssociatedChannels, Is.EqualTo(Icc1ChannelSource.DataColorSpace));
            Assert.That(TagTableCodec.UsesContextualColorantSemantics(IccTagSignatures.NamedColor2), Is.True);
            Assert.That(TagTableCodec.UsesContextualColorantSemantics(IccTagSignatures.OutputResponse), Is.True);
        });
    }

    [Test]
    public void ValidationReportsWrongTypesUnavailableTagsAndTransformChannels()
    {
        byte[] xyzPayload = new byte[20];
        WriteUInt32(xyzPayload, 0, IccTypeSignatures.Xyz);
        TagDecodeState wrongType = CreateState(IccVersionKind.Version4, IccTagSignatures.Copyright, IccProfileClass.Display, IccColorSpaceSignatures.Rgb, IccColorSpaceSignatures.Xyz, true);
        bool wrongTypeValid = Icc1TagRegistry.ValidateTagContext(wrongType, xyzPayload, IccTypeSignatures.Xyz);

        TagDecodeState unavailable = CreateState(IccVersionKind.Version4, IccTagSignatures.MediaBlackPoint, IccProfileClass.Display, IccColorSpaceSignatures.Rgb, IccColorSpaceSignatures.Xyz, false);
        bool unavailableValid = Icc1TagRegistry.ValidateTagContext(unavailable, xyzPayload, IccTypeSignatures.Xyz);

        byte[] lutHeader = new byte[10];
        WriteUInt32(lutHeader, 0, IccTypeSignatures.Lut8);
        lutHeader[8] = 3;
        lutHeader[9] = 3;
        TagDecodeState wrongChannels = CreateState(IccVersionKind.Version4, IccTagSignatures.AToB0, IccProfileClass.Output, IccColorSpaceSignatures.Cmyk, IccColorSpaceSignatures.Lab, true);
        bool channelValid = Icc1TagRegistry.ValidateTagContext(wrongChannels, lutHeader, IccTypeSignatures.Lut8);

        byte[] matrixPayload = new byte[40];
        WriteUInt32(matrixPayload, 0, IccTypeSignatures.S15Fixed16Array);
        TagDecodeState wrongMatrix = CreateState(IccVersionKind.Version4, IccTagSignatures.ChromaticAdaptation, IccProfileClass.Display, IccColorSpaceSignatures.Rgb, IccColorSpaceSignatures.Xyz, true);
        bool matrixValid = Icc1TagRegistry.ValidateTagContext(wrongMatrix, matrixPayload, IccTypeSignatures.S15Fixed16Array);

        Assert.Multiple(() =>
        {
            Assert.That(wrongTypeValid, Is.False);
            Assert.That(wrongType.Result.Status, Is.EqualTo(IccOperationStatus.InvalidData));
            Assert.That(wrongType.Result.Message, Does.Contain("permits 'mluc'").And.Contain("not 'XYZ '"));
            Assert.That(unavailableValid, Is.False);
            Assert.That(unavailable.Result.Status, Is.EqualTo(IccOperationStatus.UnsupportedFeature));
            Assert.That(unavailable.Data.RawData.Bytes, Is.EqualTo(xyzPayload));
            Assert.That(channelValid, Is.False);
            Assert.That(wrongChannels.Result.Message, Does.Contain("3 input channels").And.Contain("require 4"));
            Assert.That(matrixValid, Is.False);
            Assert.That(wrongMatrix.Result.Message, Does.Contain("exactly 9 encoded values"));
        });
    }

    private static Icc1TagDefinition Find(uint signature)
    {
        Icc1TagDefinition? definition = Icc1TagRegistry.FindDefinition(signature);
        Assert.That(definition, Is.Not.Null, $"Missing definition for 0x{signature:X8}");
        return definition!;
    }

    private static uint[] GetAvailableSignatures(IccVersionKind version)
    {
        return Icc1TagRegistry.Definitions
            .Where(item => Icc1TagRegistry.GetPermittedTypes(item, version).Length > 0)
            .Select(item => item.TagSignature)
            .ToArray();
    }

    private static TagDecodeState CreateState(IccVersionKind version, uint tagSignature, IccProfileClass profileClass, uint dataColorSpace, uint connectionSpace, bool preserveOriginalBytes)
    {
        byte[] sourceBytes = new byte[64];
        TagDecodeState state = new TagDecodeState(new TagDecodeSource(sourceBytes), 0, sourceBytes.Length, 100, version, preserveOriginalBytes);
        state.HasProfileContext = true;
        state.TagSignature = tagSignature;
        state.ProfileClass = profileClass;
        state.DataColorSpaceSignature = dataColorSpace;
        state.ProfileConnectionSpaceSignature = connectionSpace;
        return state;
    }

    private static void WriteUInt32(byte[] bytes, int offset, uint value)
    {
        bytes[offset] = (byte)(value >> 24);
        bytes[offset + 1] = (byte)(value >> 16);
        bytes[offset + 2] = (byte)(value >> 8);
        bytes[offset + 3] = (byte)value;
    }
}
