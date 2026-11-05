using icc;

namespace icc.Tests;

public class SignatureDescriberTests
{
    [Test]
    public void FourCcConversionsPreserveAsciiAndHexValues()
    {
        IIccSignatureDescriber describer = new IccSignatureDescriber();

        Assert.Multiple(() =>
        {
            Assert.That(describer.ParseFourCc("RGB "), Is.EqualTo(IccColorSpaceSignatures.Rgb));
            Assert.That(describer.ParseFourCc("0x52474220"), Is.EqualTo(IccColorSpaceSignatures.Rgb));
            Assert.That(describer.ParseFourCc("0x00aB12fF"), Is.EqualTo(0x00ab12ff));
            Assert.That(describer.FormatFourCc(IccConstants.ProfileFileSignature), Is.EqualTo("acsp"));
            Assert.That(describer.FormatFourCc(0x00637370), Is.EqualTo("0x00637370"));
            Assert.That(describer.FormatFourCcHex(IccColorSpaceSignatures.Rgb), Is.EqualTo("0x52474220"));
        });
    }

    [Test]
    public void FourCcParsingRejectsMalformedOrNonAsciiInput()
    {
        IIccSignatureDescriber describer = new IccSignatureDescriber();

        Assert.Multiple(() =>
        {
            Assert.Throws<IccProfileFormatException>(() => describer.ParseFourCc("RGB"));
            Assert.Throws<IccProfileFormatException>(() => describer.ParseFourCc("RGB12"));
            Assert.Throws<IccProfileFormatException>(() => describer.ParseFourCc("0x1234567G"));
            Assert.Throws<IccProfileFormatException>(() => describer.ParseFourCc("éRGB"));
        });
    }

    [Test]
    public void RegisteredIccOneAndIccTwoSignaturesHaveStableDescriptions()
    {
        IIccSignatureDescriber describer = new IccSignatureDescriber();

        Assert.Multiple(() =>
        {
            Assert.That(describer.DescribeProfileClassSignature(IccProfileClassSignatures.Display), Is.EqualTo("Display ('mntr', 0x6D6E7472)"));
            Assert.That(describer.DescribeColorSpaceSignature(IccColorSpaceSignatures.Cmyk), Is.EqualTo("Cmyk ('CMYK', 0x434D594B)"));
            Assert.That(describer.DescribeTagSignature(IccTagSignatures.ProfileDescription), Is.EqualTo("Profile Description ('desc', 0x64657363)"));
            Assert.That(describer.DescribeTypeSignature(IccTypeSignatures.TagStructure), Is.EqualTo("Tag Structure ('tstr', 0x74737472)"));
            Assert.That(describer.DescribeTypeSignature(IccTypeSignatures.DeviceSettings), Is.EqualTo("Device Settings ('devs', 0x64657673)"));
            Assert.That(describer.DescribeProcessingElementSignature(IccProcessingElementSignatures.Calculator), Is.EqualTo("Calculator ('calc', 0x63616C63)"));
            Assert.That(describer.DescribeArraySignature(IccArraySignatures.ColorantInformation), Is.EqualTo("Colorant Information ('cinf', 0x63696E66)"));
            Assert.That(describer.DescribeArraySignature(IccArraySignatures.ProfileInformation), Is.EqualTo("Profile Information ('pinf', 0x70696E66)"));
            Assert.That(describer.DescribeStructureSignature(IccStructureSignatures.ProfileConnectionConditions), Is.EqualTo("Profile Connection Conditions ('pcc ', 0x70636320)"));
            Assert.That(describer.DescribeTechnologySignature(IccTechnologySignatures.DigitalCamera), Is.EqualTo("Digital Camera ('dcam', 0x6463616D)"));
            Assert.That(describer.DescribeImageStateSignature(IccImageStateSignatures.SceneAppearanceEstimates), Is.EqualTo("Scene Appearance Estimates ('sape', 0x73617065)"));
        });
    }

    [Test]
    public void DynamicAndUnknownSignaturesRemainInformativeAndLossless()
    {
        IIccSignatureDescriber describer = new IccSignatureDescriber();
        uint extendedReflectance = describer.ParseFourCc("rs16");
        uint privateTag = describer.ParseFourCc("zzzz");

        Assert.Multiple(() =>
        {
            Assert.That(describer.DescribeColorSpaceSignature(extendedReflectance), Is.EqualTo("Reflectance Spectral ('rs16', 0x72733136)"));
            Assert.That(describer.DescribeTagSignature(privateTag), Is.EqualTo("Unknown/private tag signature ('zzzz', 0x7A7A7A7A)"));
            Assert.That(describer.DescribeVersionKind(IccVersionKind.Version5), Is.EqualTo("ICC.2 v5"));
        });
    }
}
