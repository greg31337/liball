using qrcode;

namespace qrcode.Tests;

[TestFixture]
public class PublicStaticDataTests
{
    [Test]
    public void ProfileLimitsMatchStandardizedVersionAndDimensionRanges()
    {
        Assert.Multiple(() =>
        {
            Assert.That(QrLimits.Model2MinimumVersion, Is.EqualTo(1));
            Assert.That(QrLimits.Model2MaximumVersion, Is.EqualTo(40));
            Assert.That(QrLimits.Model2MinimumDimension, Is.EqualTo(21));
            Assert.That(QrLimits.Model2MaximumDimension, Is.EqualTo(177));
            Assert.That(QrLimits.Model2QuietZoneModules, Is.EqualTo(4));
            Assert.That(QrLimits.Model2MinimumMask, Is.EqualTo(0));
            Assert.That(QrLimits.Model2MaximumMask, Is.EqualTo(7));
            Assert.That(QrLimits.MicroMinimumVersion, Is.EqualTo(1));
            Assert.That(QrLimits.MicroMaximumVersion, Is.EqualTo(4));
            Assert.That(QrLimits.MicroMinimumDimension, Is.EqualTo(11));
            Assert.That(QrLimits.MicroMaximumDimension, Is.EqualTo(17));
            Assert.That(QrLimits.MicroQuietZoneModules, Is.EqualTo(2));
            Assert.That(QrLimits.MicroMinimumMask, Is.EqualTo(0));
            Assert.That(QrLimits.MicroMaximumMask, Is.EqualTo(3));
            Assert.That(QrLimits.RectangularMicroSizeCount, Is.EqualTo(32));
            Assert.That(QrLimits.RectangularMicroMinimumHeight, Is.EqualTo(7));
            Assert.That(QrLimits.RectangularMicroMaximumHeight, Is.EqualTo(17));
            Assert.That(QrLimits.RectangularMicroMinimumWidth, Is.EqualTo(27));
            Assert.That(QrLimits.RectangularMicroMaximumWidth, Is.EqualTo(139));
            Assert.That(QrLimits.RectangularMicroQuietZoneModules, Is.EqualTo(2));
            Assert.That(QrLimits.RectangularMicroMask, Is.EqualTo(1));
            Assert.That(QrLimits.LegacyModel1MinimumVersion, Is.EqualTo(1));
            Assert.That(QrLimits.LegacyModel1MaximumVersion, Is.EqualTo(14));
            Assert.That(QrLimits.LegacyModel1MinimumDimension, Is.EqualTo(21));
            Assert.That(QrLimits.LegacyModel1MaximumDimension, Is.EqualTo(73));
            Assert.That(QrLimits.LegacyModel1QuietZoneModules, Is.EqualTo(4));
        });
    }

    [Test]
    public void ProtocolLimitsExposeStructuredAppendAndEveryEciDesignatorWidth()
    {
        Assert.Multiple(() =>
        {
            Assert.That(QrLimits.StructuredAppendMinimumSymbols, Is.EqualTo(2));
            Assert.That(QrLimits.StructuredAppendMaximumSymbols, Is.EqualTo(16));
            Assert.That(QrLimits.StructuredAppendMaximumIndex, Is.EqualTo(15));
            Assert.That(QrLimits.StructuredAppendMaximumParity, Is.EqualTo(255));
            Assert.That(QrLimits.EciMinimumAssignment, Is.EqualTo(EciRegistry.MinimumAssignment));
            Assert.That(QrLimits.EciOneByteMaximumAssignment, Is.EqualTo(EciRegistry.OneByteMaximumAssignment));
            Assert.That(QrLimits.EciTwoByteMaximumAssignment, Is.EqualTo(EciRegistry.TwoByteMaximumAssignment));
            Assert.That(QrLimits.EciMaximumQrAssignment, Is.EqualTo(EciRegistry.MaximumQrAssignment));
            Assert.That(QrLimits.EciMaximumProtocolAssignment, Is.EqualTo(EciRegistry.MaximumProtocolAssignment));
        });
    }

    [Test]
    public void DefaultSafetyLimitsAgreeWithPublicOptionDefaults()
    {
        QrEncodeOptions encode = new QrEncodeOptions();
        QrDecodeOptions decode = new QrDecodeOptions();
        QrDetectionOptions detection = new QrDetectionOptions();
        QrQualityOptions quality = new QrQualityOptions();

        Assert.Multiple(() =>
        {
            Assert.That(encode.MaximumInputBytes, Is.EqualTo(QrLimits.DefaultMaximumInputBytes));
            Assert.That(encode.MaximumSegments, Is.EqualTo(QrLimits.DefaultMaximumSegments));
            Assert.That(encode.MaximumStructuredAppendSymbols, Is.EqualTo(QrLimits.StructuredAppendMaximumSymbols));
            Assert.That(decode.MaximumSegments, Is.EqualTo(QrLimits.DefaultMaximumSegments));
            Assert.That(decode.MaximumRecoveryAttempts, Is.EqualTo(QrLimits.DefaultMaximumRecoveryAttempts));
            Assert.That(decode.MaximumAllocationBytes, Is.EqualTo(QrLimits.DefaultMaximumAllocationBytes));
            Assert.That(detection.MaximumCandidates, Is.EqualTo(QrLimits.DefaultMaximumDetectionCandidates));
            Assert.That(detection.MaximumImageWidth, Is.EqualTo(QrLimits.DefaultMaximumImageDimension));
            Assert.That(detection.MaximumImageHeight, Is.EqualTo(QrLimits.DefaultMaximumImageDimension));
            Assert.That(detection.MaximumImagePixels, Is.EqualTo(QrLimits.DefaultMaximumImagePixels));
            Assert.That(quality.MaximumModuleSamples, Is.EqualTo(QrLimits.DefaultMaximumQualitySamples));
        });
    }

    [Test]
    public void CommonEciAssignmentsResolveToTheirCanonicalRegistryNames()
    {
        Assert.Multiple(() =>
        {
            Assert.That(EciRegistry.GetCanonicalName(QrEciAssignments.Cp437), Is.EqualTo("CP437"));
            Assert.That(EciRegistry.GetCanonicalName(QrEciAssignments.Iso8859_1), Is.EqualTo("ISO-8859-1"));
            Assert.That(EciRegistry.GetCanonicalName(QrEciAssignments.ShiftJis), Is.EqualTo("Shift_JIS"));
            Assert.That(EciRegistry.GetCanonicalName(QrEciAssignments.Windows1252), Is.EqualTo("windows-1252"));
            Assert.That(EciRegistry.GetCanonicalName(QrEciAssignments.Utf16BigEndian), Is.EqualTo("UTF-16BE"));
            Assert.That(EciRegistry.GetCanonicalName(QrEciAssignments.Utf8), Is.EqualTo("UTF-8"));
            Assert.That(EciRegistry.GetCanonicalName(QrEciAssignments.UsAscii), Is.EqualTo("US-ASCII"));
            Assert.That(EciRegistry.GetCanonicalName(QrEciAssignments.Big5), Is.EqualTo("Big5"));
            Assert.That(EciRegistry.GetCanonicalName(QrEciAssignments.Gb2312), Is.EqualTo("GB2312"));
            Assert.That(EciRegistry.GetCanonicalName(QrEciAssignments.KsX1001), Is.EqualTo("KS X 1001"));
            Assert.That(EciRegistry.GetCanonicalName(QrEciAssignments.Gbk), Is.EqualTo("GBK"));
            Assert.That(EciRegistry.GetCanonicalName(QrEciAssignments.Gb18030), Is.EqualTo("GB18030"));
            Assert.That(EciRegistry.GetCanonicalName(QrEciAssignments.Utf16LittleEndian), Is.EqualTo("UTF-16LE"));
            Assert.That(EciRegistry.GetCanonicalName(QrEciAssignments.Utf32BigEndian), Is.EqualTo("UTF-32BE"));
            Assert.That(EciRegistry.GetCanonicalName(QrEciAssignments.Utf32LittleEndian), Is.EqualTo("UTF-32LE"));
            Assert.That(EciRegistry.GetCanonicalName(QrEciAssignments.Iso646Invariant), Is.EqualTo("ISO/IEC 646 invariant"));
            Assert.That(EciRegistry.GetCanonicalName(QrEciAssignments.Binary), Is.EqualTo("8-bit binary data"));
        });
    }

    [Test]
    public void CharacterSetNamesMatchTheEciRegistryAndAlphanumericValueOrder()
    {
        Assert.Multiple(() =>
        {
            Assert.That(QrCharacterSets.Alphanumeric, Is.EqualTo("0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ $%*+-./:"));
            Assert.That(QrCharacterSets.Alphanumeric, Has.Length.EqualTo(45));
            Assert.That(EciRegistry.GetCanonicalName(QrEciAssignments.Cp437), Is.EqualTo(QrCharacterSets.Cp437));
            Assert.That(EciRegistry.GetCanonicalName(QrEciAssignments.Iso8859_1), Is.EqualTo(QrCharacterSets.Iso8859_1));
            Assert.That(EciRegistry.GetCanonicalName(QrEciAssignments.ShiftJis), Is.EqualTo(QrCharacterSets.ShiftJis));
            Assert.That(EciRegistry.GetCanonicalName(QrEciAssignments.Windows1252), Is.EqualTo(QrCharacterSets.Windows1252));
            Assert.That(EciRegistry.GetCanonicalName(QrEciAssignments.Utf16BigEndian), Is.EqualTo(QrCharacterSets.Utf16BigEndian));
            Assert.That(EciRegistry.GetCanonicalName(QrEciAssignments.Utf8), Is.EqualTo(QrCharacterSets.Utf8));
            Assert.That(EciRegistry.GetCanonicalName(QrEciAssignments.UsAscii), Is.EqualTo(QrCharacterSets.UsAscii));
            Assert.That(EciRegistry.GetCanonicalName(QrEciAssignments.Gb2312), Is.EqualTo(QrCharacterSets.Gb2312));
            Assert.That(EciRegistry.GetCanonicalName(QrEciAssignments.Utf16LittleEndian), Is.EqualTo(QrCharacterSets.Utf16LittleEndian));
        });
    }

    [Test]
    public void SymbologyIdentifiersExposeQrModifierValuesWithoutPayloadInjection()
    {
        Assert.Multiple(() =>
        {
            Assert.That(QrSymbologyIdentifiers.Prefix, Is.EqualTo("]Q"));
            Assert.That(QrSymbologyIdentifiers.LegacyModel1, Is.EqualTo("]Q0"));
            Assert.That(QrSymbologyIdentifiers.Model2, Is.EqualTo("]Q1"));
            Assert.That(QrSymbologyIdentifiers.Model2WithEci, Is.EqualTo("]Q2"));
            Assert.That(QrSymbologyIdentifiers.Model2WithFnc1First, Is.EqualTo("]Q3"));
            Assert.That(QrSymbologyIdentifiers.Model2WithEciAndFnc1First, Is.EqualTo("]Q4"));
            Assert.That(QrSymbologyIdentifiers.Model2WithFnc1Second, Is.EqualTo("]Q5"));
            Assert.That(QrSymbologyIdentifiers.Model2WithEciAndFnc1Second, Is.EqualTo("]Q6"));
        });
    }
}
