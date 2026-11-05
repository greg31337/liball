using System.Reflection;
using icc;

namespace icc.Tests;

public class StaticDataTests
{
    [Test]
    public void EnvelopeConstantsMatchTheIccBinaryLayout()
    {
        Assert.Multiple(() =>
        {
            Assert.That(IccConstants.HeaderSize, Is.EqualTo(128));
            Assert.That(IccConstants.ProfileIdSize, Is.EqualTo(16));
            Assert.That(IccConstants.TagTableEntrySize, Is.EqualTo(12));
            Assert.That(IccConstants.TagPayloadAlignment, Is.EqualTo(4));
            Assert.That(IccConstants.MaximumIcc1ChannelCount, Is.EqualTo(15));
            Assert.That(IccConstants.MaximumIcc2ChannelCount, Is.EqualTo(65535));
            AssertFourCc(IccConstants.ProfileFileSignature, "acsp");
            Assert.That(IccConstants.EmbeddedProfileFlag, Is.EqualTo(1));
            Assert.That(IccConstants.UseIndependentOfDataFlag, Is.EqualTo(2));
        });
    }

    [Test]
    public void ProfileClassAndColorSpaceSignaturesMatchRegisteredFourCcs()
    {
        Assert.Multiple(() =>
        {
            AssertFourCc(IccProfileClassSignatures.Input, "scnr");
            AssertFourCc(IccProfileClassSignatures.Display, "mntr");
            AssertFourCc(IccProfileClassSignatures.Output, "prtr");
            AssertFourCc(IccProfileClassSignatures.ColorEncodingSpace, "cenc");
            AssertFourCc(IccProfileClassSignatures.MultiplexIdentification, "mid ");
            AssertFourCc(IccProfileClassSignatures.MultiplexLink, "mlnk");
            AssertFourCc(IccProfileClassSignatures.MultiplexVisualization, "mvis");
            AssertFourCc(IccColorSpaceSignatures.Xyz, "XYZ ");
            AssertFourCc(IccColorSpaceSignatures.Rgb, "RGB ");
            AssertFourCc(IccColorSpaceSignatures.FifteenColor, "FCLR");
            Assert.That(IccColorSpaceSignatures.ReflectanceSpectralPrefix, Is.EqualTo(0x72730000));
            Assert.That(IccColorSpaceSignatures.MultiplexPrefix, Is.EqualTo(0x6d630000));
        });
    }

    [Test]
    public void TagSignaturesCoverIcc1Icc2AndCurrentAmendments()
    {
        Assert.Multiple(() =>
        {
            AssertFourCc(IccTagSignatures.AToB0, "A2B0");
            AssertFourCc(IccTagSignatures.BToD3, "B2D3");
            AssertFourCc(IccTagSignatures.MediaWhitePoint, "wtpt");
            AssertFourCc(IccTagSignatures.ProfileSequenceIdentifier, "psid");
            AssertFourCc(IccTagSignatures.Cicp, "cicp");
            AssertFourCc(IccTagSignatures.AdaptiveGainCurve, "ADGC");
            AssertFourCc(IccTagSignatures.EmbeddedIccMaxProfile, "ICC5");
            AssertFourCc(IccTagSignatures.AToM0, "A2M0");
            AssertFourCc(IccTagSignatures.MToS3, "M2S3");
            AssertFourCc(IccTagSignatures.BrdfMToS3, "bMS3");
            AssertFourCc(IccTagSignatures.DirectionalDToB3, "dDB3");
            AssertFourCc(IccTagSignatures.HdrToSdr0, "H2S0");
            AssertFourCc(IccTagSignatures.ProfileSequenceInformation, "psin");
            AssertFourCc(IccTagSignatures.DevicePcc, "dpcc");
            AssertFourCc(IccTagSignatures.DeviceSpectralRange, "dsrn");
        });
    }

    [Test]
    public void TypeAndProcessingElementSignaturesMatchRegisteredFourCcs()
    {
        Assert.Multiple(() =>
        {
            AssertFourCc(IccTypeSignatures.Curve, "curv");
            AssertFourCc(IccTypeSignatures.MultiProcessElements, "mpet");
            AssertFourCc(IccTypeSignatures.AdaptiveGainCurve, "adgc");
            AssertFourCc(IccTypeSignatures.DeviceSettings, "devs");
            AssertFourCc(IccTypeSignatures.Float16Array, "fl16");
            AssertFourCc(IccTypeSignatures.TagArray, "tary");
            AssertFourCc(IccTypeSignatures.TagStructure, "tstr");
            AssertFourCc(IccTypeSignatures.ZippedXml, "zxml");
            AssertFourCc(IccTypeSignatures.SpectralRange, "srng");
            AssertFourCc(IccProcessingElementSignatures.CurveSet, "cvst");
            AssertFourCc(IccProcessingElementSignatures.BeginAcs, "bACS");
            AssertFourCc(IccProcessingElementSignatures.Calculator, "calc");
            AssertFourCc(IccProcessingElementSignatures.SparseMatrix, "smet");
            AssertFourCc(IccProcessingElementSignatures.ToneMap, "tmap");
            AssertFourCc(IccProcessingElementSignatures.XyzToJab, "XtoJ");
        });
    }

    [Test]
    public void HierarchyTechnologyAndImageStateSignaturesAreStable()
    {
        Assert.Multiple(() =>
        {
            AssertFourCc(IccArraySignatures.Generic, "tags");
            AssertFourCc(IccStructureSignatures.BrdfTransform, "brdf");
            AssertFourCc(IccStructureSignatures.ColorEncodingParameters, "cept");
            AssertFourCc(IccStructureSignatures.TintZero, "tnt0");
            AssertFourCc(IccStructureSignatures.ProfileConnectionConditions, "pcc ");
            AssertFourCc(IccTechnologySignatures.DigitalCamera, "dcam");
            AssertFourCc(IccTechnologySignatures.OffsetLithography, "offs");
            Assert.That(IccTechnologySignatures.DigitalCinemaProjector, Is.EqualTo(0x64636a70));
            AssertFourCc(IccImageStateSignatures.SceneColorimetryEstimates, "scoe");
            AssertFourCc(IccImageStateSignatures.ReflectionPrintOutputColorimetry, "rpoc");
        });
    }

    [Test]
    public void StandardColorimetryUsesIccReferenceValues()
    {
        Assert.Multiple(() =>
        {
            Assert.That(IccStandardColorimetry.D50X, Is.EqualTo(0.9642));
            Assert.That(IccStandardColorimetry.D50Y, Is.EqualTo(1.0));
            Assert.That(IccStandardColorimetry.D50Z, Is.EqualTo(0.8249));
            Assert.That(IccStandardColorimetry.D50EncodedX, Is.EqualTo(0x0000f6d6));
            Assert.That(IccStandardColorimetry.D50EncodedY, Is.EqualTo(0x00010000));
            Assert.That(IccStandardColorimetry.D50EncodedZ, Is.EqualTo(0x0000d32d));
            Assert.That(IccStandardColorimetry.D65ChromaticityX, Is.EqualTo(0.3127));
            Assert.That(IccStandardColorimetry.D65ChromaticityY, Is.EqualTo(0.3290));
            Assert.That(IccStandardColorimetry.Bradford00, Is.EqualTo(0.8951));
            Assert.That(IccStandardColorimetry.Bradford22, Is.EqualTo(1.0296));
        });
    }

    [Test]
    public void ValidationRuleIdsAreUniqueStableAndNamespaced()
    {
        FieldInfo[] fields = typeof(IccValidationRuleIds).GetFields(BindingFlags.Public | BindingFlags.Static);
        string[] values = Array.ConvertAll(fields, field => Convert.ToString(field.GetRawConstantValue()) ?? string.Empty);

        Assert.Multiple(() =>
        {
            Assert.That(values, Has.Length.GreaterThan(40));
            Assert.That(values, Is.Unique);
            Assert.That(values, Has.All.StartsWith("ICC."));
            Assert.That(IccValidationRuleIds.HeaderProfileSize, Is.EqualTo("ICC.HDR.SIZE"));
            Assert.That(IccValidationRuleIds.TagTableOverlap, Is.EqualTo("ICC.TABLE.OVERLAP"));
            Assert.That(IccValidationRuleIds.SecurityResourceLimit, Is.EqualTo("ICC.SECURITY.RESOURCE_LIMIT"));
        });
    }

    private static uint FourCc(string value)
    {
        return ((uint)value[0] << 24)
            | ((uint)value[1] << 16)
            | ((uint)value[2] << 8)
            | value[3];
    }

    private static void AssertFourCc(uint actual, string expected)
    {
        Assert.That(actual, Is.EqualTo(FourCc(expected)));
    }
}
