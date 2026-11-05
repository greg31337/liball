namespace barcode.Tests;

public class EanUpcAddonScanlineDecoderTests
{
    [TestCase("12", BarcodeFormat.EanUpcAddon2, 20, 0, "LL")]
    [TestCase("86104", BarcodeFormat.EanUpcAddon5, 47, 3, "GLLLG")]
    public void ForwardStandaloneDecodeRecoversParityChecksumAndMetadata(
        string text,
        BarcodeFormat format,
        int moduleCount,
        int checksum,
        string parity)
    {
        EanUpcEncodedData encoded = EanUpcAddon.Encode(text);
        BitRow row = CreateStandaloneScanline(encoded.Modules, 7, 5, 1);

        EanUpcAddonDecodedData decoded = EanUpcAddonScanlineDecoder.DecodeRow(row, null);

        Assert.Multiple(() =>
        {
            Assert.That(decoded.Format, Is.EqualTo(format));
            Assert.That(decoded.Text, Is.EqualTo(text));
            Assert.That(decoded.Digits, Is.EqualTo(ToDigits(text)));
            Assert.That(ToParityString(decoded.Parity), Is.EqualTo(parity));
            Assert.That(decoded.ChecksumValue, Is.EqualTo(checksum));
            Assert.That(decoded.IsReversed, Is.False);
            Assert.That(decoded.StartPosition, Is.EqualTo(7));
            Assert.That(decoded.EndPosition, Is.EqualTo(7 + moduleCount));
            Assert.That(decoded.ModuleSize, Is.EqualTo(1.0));
            Assert.That(decoded.AverageVariance, Is.EqualTo(0.0));
            Assert.That(decoded.Result.Format, Is.EqualTo(format));
            Assert.That(decoded.Result.Payload!.Text, Is.EqualTo(text));
            Assert.That(decoded.Result.Payload.RawBytes, Is.EqualTo(ToAsciiBytes(text)));
            Assert.That(decoded.Result.Metadata!.ChecksumValid, Is.True);
            Assert.That(decoded.Result.Metadata.ChecksumValue, Is.EqualTo(checksum));
            Assert.That(decoded.Result.Metadata.SymbologyIdentifier, Is.EqualTo("]E0"));
            Assert.That(decoded.Result.Metadata.Confidence, Is.EqualTo(1.0));
        });
    }

    [TestCase("12", 20)]
    [TestCase("86104", 47)]
    public void ReversedStandaloneDecodePreservesPayloadAndCoordinates(string text, int moduleCount)
    {
        EanUpcEncodedData encoded = EanUpcAddon.Encode(text);
        BitRow row = CreateStandaloneScanline(encoded.Modules, 7, 5, 3);
        row.ReverseBits();

        EanUpcAddonDecodedData decoded = EanUpcAddonScanlineDecoder.DecodeRow(row, null);

        Assert.Multiple(() =>
        {
            Assert.That(decoded.Text, Is.EqualTo(text));
            Assert.That(decoded.IsReversed, Is.True);
            Assert.That(decoded.StartPosition, Is.EqualTo(15));
            Assert.That(decoded.EndPosition, Is.EqualTo((5 + moduleCount) * 3));
            Assert.That(decoded.ModuleSize, Is.EqualTo(3.0));
            Assert.That(decoded.Result.Rotation, Is.EqualTo(BarcodeRotation.Clockwise180));
            Assert.That(decoded.Result.OrientationDegrees, Is.EqualTo(180.0));
        });
    }

    [Test]
    public void StandaloneAllowedLengthPolicySelectsOnlyPermittedSupplements()
    {
        EanUpcOptions allowTwo = CreateOptions(new int[] { 2 }, false);
        EanUpcOptions allowFive = CreateOptions(new int[] { 5 }, false);
        BitRow two = CreateStandaloneScanline(EanUpcAddon.Encode("12").Modules, 7, 5, 1);
        BitRow five = CreateStandaloneScanline(EanUpcAddon.Encode("86104").Modules, 7, 5, 1);

        Assert.Multiple(() =>
        {
            Assert.That(EanUpcAddonScanlineDecoder.TryDecodeRow(two, allowTwo)!.Text, Is.EqualTo("12"));
            Assert.That(EanUpcAddonScanlineDecoder.TryDecodeRow(five, allowFive)!.Text, Is.EqualTo("86104"));
            Assert.That(EanUpcAddonScanlineDecoder.TryDecodeRow(two, allowFive), Is.Null);
            Assert.That(EanUpcAddonScanlineDecoder.TryDecodeRow(five, allowTwo), Is.Null);
            Assert.That(EanUpcAddonScanlineDecoder.TryDecodeRow(two, CreateOptions(new int[0], false)), Is.Null);
        });
    }

    [Test]
    public void StandaloneDecoderRequiresQuietZonesAndValidParity()
    {
        EanUpcEncodedData encoded = EanUpcAddon.Encode("12");
        byte[] wrongParity = CopyModules(encoded.Modules);
        CopyModulesAt(EanUpcPatterns.GetDigitModules(2, EanUpcDigitSet.L), wrongParity, 4);
        byte[] damagedStart = CopyModules(encoded.Modules);
        damagedStart[3] = 0;

        Assert.Multiple(() =>
        {
            Assert.That(EanUpcAddonScanlineDecoder.TryDecodeRow(
                CreateStandaloneScanline(encoded.Modules, 6, 5, 2), null), Is.Null);
            Assert.That(EanUpcAddonScanlineDecoder.TryDecodeRow(
                CreateStandaloneScanline(encoded.Modules, 7, 4, 2), null), Is.Null);
            Assert.That(EanUpcAddonScanlineDecoder.TryDecodeRow(
                CreateStandaloneScanline(wrongParity, 7, 5, 1), null), Is.Null);
            Assert.That(EanUpcAddonScanlineDecoder.TryDecodeRow(
                CreateStandaloneScanline(damagedStart, 7, 5, 1), null), Is.Null);
        });
    }

    [TestCase(BarcodeFormat.Ean13, "4006381333931", "12", 7)]
    [TestCase(BarcodeFormat.Ean13, "4006381333931", "86104", 7)]
    [TestCase(BarcodeFormat.UpcA, "036000291452", "12", 9)]
    [TestCase(BarcodeFormat.UpcA, "036000291452", "86104", 9)]
    [TestCase(BarcodeFormat.UpcE, "01234558", "12", 7)]
    [TestCase(BarcodeFormat.UpcE, "01234558", "86104", 7)]
    public void ForwardAssociationRetainsMainAndSupplementIdentity(
        BarcodeFormat mainFormat,
        string mainText,
        string addOnText,
        int separation)
    {
        EanUpcEncodedData main = CreateMain(mainFormat, mainText);
        EanUpcEncodedData addOn = EanUpcAddon.Encode(addOnText);
        int leftQuietZone = GetLeftQuietZone(mainFormat);
        BitRow row = CreateAssociationScanline(
            main.Modules,
            addOn.Modules,
            leftQuietZone,
            separation,
            1);

        EanUpcDecodedAssociation association = EanUpcAddonScanlineDecoder.DecodeAssociation(row, null);

        Assert.Multiple(() =>
        {
            Assert.That(association.Main.Format, Is.EqualTo(mainFormat));
            Assert.That(association.Main.Text, Is.EqualTo(mainText));
            Assert.That(association.Main.IsReversed, Is.False);
            Assert.That(association.AddOn, Is.Not.Null);
            Assert.That(association.AddOn!.Format, Is.EqualTo(addOn.Format));
            Assert.That(association.AddOn.Text, Is.EqualTo(addOnText));
            Assert.That(association.AddOn.IsReversed, Is.False);
            Assert.That(association.AddOn.StartPosition,
                Is.EqualTo(leftQuietZone + main.Modules.Length + separation));
            Assert.That(association.SeparationModules, Is.EqualTo(separation));
        });
    }

    [Test]
    public void ReversedAssociationMapsBothCarriersToOriginalCoordinates()
    {
        EanUpcEncodedData main = CreateMain(BarcodeFormat.Ean13, "4006381333931");
        EanUpcEncodedData addOn = EanUpcAddon.Encode("86104");
        BitRow row = CreateAssociationScanline(main.Modules, addOn.Modules, 11, 7, 2);
        row.ReverseBits();

        EanUpcDecodedAssociation association = EanUpcAddonScanlineDecoder.DecodeAssociation(row, null);

        Assert.Multiple(() =>
        {
            Assert.That(association.Main.Text, Is.EqualTo("4006381333931"));
            Assert.That(association.Main.IsReversed, Is.True);
            Assert.That(association.Main.StartPosition, Is.EqualTo((5 + 47 + 7) * 2));
            Assert.That(association.Main.EndPosition, Is.EqualTo((5 + 47 + 7 + 95) * 2));
            Assert.That(association.AddOn!.Text, Is.EqualTo("86104"));
            Assert.That(association.AddOn.IsReversed, Is.True);
            Assert.That(association.AddOn.StartPosition, Is.EqualTo(10));
            Assert.That(association.AddOn.EndPosition, Is.EqualTo((5 + 47) * 2));
            Assert.That(association.AddOn.Result.Rotation, Is.EqualTo(BarcodeRotation.Clockwise180));
            Assert.That(association.SeparationModules, Is.EqualTo(7));
        });
    }

    [Test]
    public void OptionalAndRequiredPoliciesHandleMissingSupplement()
    {
        EanUpcEncodedData main = CreateMain(BarcodeFormat.Ean13, "4006381333931");
        BitRow row = CreateStandaloneScanline(main.Modules, 11, 7, 1);
        EanUpcOptions optional = CreateOptions(null, false);
        EanUpcOptions required = CreateOptions(null, true);

        EanUpcDecodedAssociation withoutOptions =
            EanUpcAddonScanlineDecoder.DecodeAssociation(row, null);
        EanUpcDecodedAssociation withoutAddOn =
            EanUpcAddonScanlineDecoder.DecodeAssociation(row, optional);

        Assert.Multiple(() =>
        {
            Assert.That(withoutOptions.Main.Text, Is.EqualTo("4006381333931"));
            Assert.That(withoutOptions.AddOn, Is.Null);
            Assert.That(withoutOptions.SeparationModules, Is.Zero);
            Assert.That(withoutAddOn.AddOn, Is.Null);
            Assert.That(EanUpcAddonScanlineDecoder.TryDecodeAssociation(row, required), Is.Null);
            Assert.That(
                () => EanUpcAddonScanlineDecoder.DecodeAssociation(row, required),
                Throws.TypeOf<BarcodeDecodingException>());
        });
    }

    [Test]
    public void AssociationAllowedLengthPolicyAcceptsOrIgnoresObservedSupplement()
    {
        EanUpcEncodedData main = CreateMain(BarcodeFormat.UpcA, "036000291452");
        EanUpcEncodedData addOn = EanUpcAddon.Encode("12");
        BitRow row = CreateAssociationScanline(main.Modules, addOn.Modules, 9, 9, 1);
        EanUpcOptions allowTwo = CreateOptions(new int[] { 2 }, true);
        EanUpcOptions allowFiveOptional = CreateOptions(new int[] { 5 }, false);
        EanUpcOptions allowFiveRequired = CreateOptions(new int[] { 5 }, true);

        EanUpcDecodedAssociation accepted =
            EanUpcAddonScanlineDecoder.DecodeAssociation(row, allowTwo);
        EanUpcDecodedAssociation ignored =
            EanUpcAddonScanlineDecoder.DecodeAssociation(row, allowFiveOptional);

        Assert.Multiple(() =>
        {
            Assert.That(accepted.AddOn!.Text, Is.EqualTo("12"));
            Assert.That(ignored.Main.Text, Is.EqualTo("036000291452"));
            Assert.That(ignored.AddOn, Is.Null);
            Assert.That(EanUpcAddonScanlineDecoder.TryDecodeAssociation(row, allowFiveRequired), Is.Null);
        });
    }

    [Test]
    public void AssociationEnforcesSeparationWindowAndCompatibleModuleSize()
    {
        EanUpcEncodedData ean13 = CreateMain(BarcodeFormat.Ean13, "4006381333931");
        EanUpcEncodedData addOn = EanUpcAddon.Encode("12");
        EanUpcOptions required = CreateOptions(null, true);
        BitRow minimum = CreateAssociationScanline(ean13.Modules, addOn.Modules, 11, 7, 2);
        BitRow maximum = CreateAssociationScanline(ean13.Modules, addOn.Modules, 11, 12, 2);
        BitRow tooSmall = CreateAssociationScanline(ean13.Modules, addOn.Modules, 11, 6, 2);
        BitRow tooLarge = CreateAssociationScanline(ean13.Modules, addOn.Modules, 11, 13, 2);
        BitRow incompatible = CreateMixedScaleAssociationScanline(ean13.Modules, addOn.Modules);

        Assert.Multiple(() =>
        {
            Assert.That(EanUpcAddonScanlineDecoder.DecodeAssociation(minimum, required).SeparationModules, Is.EqualTo(7));
            Assert.That(EanUpcAddonScanlineDecoder.DecodeAssociation(maximum, required).SeparationModules, Is.EqualTo(12));
            Assert.That(EanUpcAddonScanlineDecoder.TryDecodeAssociation(tooSmall, required), Is.Null);
            Assert.That(EanUpcAddonScanlineDecoder.TryDecodeAssociation(tooLarge, required), Is.Null);
            Assert.That(EanUpcAddonScanlineDecoder.TryDecodeAssociation(incompatible, required), Is.Null);
        });
    }

    [Test]
    public void MalformedOptionalSupplementDoesNotDiscardValidMain()
    {
        EanUpcEncodedData main = CreateMain(BarcodeFormat.Ean13, "4006381333931");
        EanUpcEncodedData addOn = EanUpcAddon.Encode("12");
        byte[] damaged = CopyModules(addOn.Modules);
        damaged[0] = 0;
        BitRow row = CreateAssociationScanline(main.Modules, damaged, 11, 7, 1);
        EanUpcOptions optional = CreateOptions(null, false);
        EanUpcOptions required = CreateOptions(null, true);

        EanUpcDecodedAssociation association =
            EanUpcAddonScanlineDecoder.DecodeAssociation(row, optional);

        Assert.Multiple(() =>
        {
            Assert.That(association.Main.Text, Is.EqualTo("4006381333931"));
            Assert.That(association.AddOn, Is.Null);
            Assert.That(EanUpcAddonScanlineDecoder.TryDecodeAssociation(row, required), Is.Null);
        });
    }

    [Test]
    public void Ean8NeverAssociatesNonstandardSupplement()
    {
        EanUpcEncodedData main = CreateMain(BarcodeFormat.Ean8, "55123457");
        EanUpcEncodedData addOn = EanUpcAddon.Encode("12");
        BitRow row = CreateAssociationScanline(main.Modules, addOn.Modules, 7, 7, 1);
        EanUpcOptions required = CreateOptions(null, true);

        EanUpcDecodedAssociation optional =
            EanUpcAddonScanlineDecoder.DecodeAssociation(row, null);

        Assert.Multiple(() =>
        {
            Assert.That(optional.Main.Format, Is.EqualTo(BarcodeFormat.Ean8));
            Assert.That(optional.AddOn, Is.Null);
            Assert.That(EanUpcAddonScanlineDecoder.TryDecodeAssociation(row, required), Is.Null);
        });
    }

    [Test]
    public void DecoderValidatesConfigurationAndInvalidRows()
    {
        EanUpcOptions invalid = CreateOptions(new int[] { 3 }, false);
        BitRow empty = new BitRow(40);

        Assert.Multiple(() =>
        {
            Assert.That(
                () => EanUpcAddonScanlineDecoder.TryDecodeRow(null!, null),
                Throws.TypeOf<BarcodeValidationException>());
            Assert.That(
                () => EanUpcAddonScanlineDecoder.TryDecodeAssociation(null!, null),
                Throws.TypeOf<BarcodeValidationException>());
            Assert.That(
                () => EanUpcAddonScanlineDecoder.TryDecodeRow(empty, invalid),
                Throws.TypeOf<BarcodeValidationException>());
            Assert.That(
                () => EanUpcAddonScanlineDecoder.TryDecodeAssociation(empty, invalid),
                Throws.TypeOf<BarcodeValidationException>());
            Assert.That(EanUpcAddonScanlineDecoder.TryDecodeRow(empty, null), Is.Null);
            Assert.That(
                () => EanUpcAddonScanlineDecoder.DecodeRow(empty, null),
                Throws.TypeOf<BarcodeDecodingException>()
                    .With.Message.EqualTo("The scanline does not contain a valid EAN/UPC add-on symbol."));
        });
    }

    static EanUpcEncodedData CreateMain(BarcodeFormat format, string text)
    {
        if (format == BarcodeFormat.UpcE)
        {
            return EanUpcEncoder.EncodeUpcE(text, text[0] - '0', BarcodeChecksumMode.Require);
        }

        return EanUpcEncoder.Encode(text, format, BarcodeChecksumMode.Require);
    }

    static EanUpcOptions CreateOptions(int[]? allowedLengths, bool requireAddOn)
    {
        EanUpcOptions options = new EanUpcOptions();
        options.AllowedAddOnLengths = allowedLengths;
        options.RequireAddOn = requireAddOn;
        return options;
    }

    static int GetLeftQuietZone(BarcodeFormat format)
    {
        if (format == BarcodeFormat.Ean13)
        {
            return 11;
        }

        return 9;
    }

    static BitRow CreateStandaloneScanline(
        byte[] modules,
        int leftQuietModules,
        int rightQuietModules,
        int scale)
    {
        int length = (leftQuietModules + modules.Length + rightQuietModules) * scale;
        BitRow row = new BitRow(length);
        WriteScaledModules(row, modules, leftQuietModules * scale, scale);
        return row;
    }

    static BitRow CreateAssociationScanline(
        byte[] mainModules,
        byte[] addOnModules,
        int leftQuietModules,
        int separationModules,
        int scale)
    {
        int addOnOffset = leftQuietModules + mainModules.Length + separationModules;
        int length = (addOnOffset + addOnModules.Length + 5) * scale;
        BitRow row = new BitRow(length);
        WriteScaledModules(row, mainModules, leftQuietModules * scale, scale);
        WriteScaledModules(row, addOnModules, addOnOffset * scale, scale);
        return row;
    }

    static BitRow CreateMixedScaleAssociationScanline(byte[] mainModules, byte[] addOnModules)
    {
        int mainScale = 2;
        int addOnScale = 3;
        int mainStart = 11 * mainScale;
        int addOnStart = mainStart + (mainModules.Length * mainScale) + (7 * mainScale);
        int length = addOnStart + (addOnModules.Length * addOnScale) + (5 * addOnScale);
        BitRow row = new BitRow(length);
        WriteScaledModules(row, mainModules, mainStart, mainScale);
        WriteScaledModules(row, addOnModules, addOnStart, addOnScale);
        return row;
    }

    static void WriteScaledModules(BitRow row, byte[] modules, int offset, int scale)
    {
        int moduleIndex = 0;
        while (moduleIndex < modules.Length)
        {
            if (modules[moduleIndex] != 0)
            {
                int pixel = 0;
                while (pixel < scale)
                {
                    row.SetBit(offset + (moduleIndex * scale) + pixel, true);
                    pixel++;
                }
            }

            moduleIndex++;
        }
    }

    static int[] ToDigits(string text)
    {
        return text.Select(character => character - '0').ToArray();
    }

    static byte[] ToAsciiBytes(string text)
    {
        return text.Select(character => (byte)character).ToArray();
    }

    static string ToParityString(EanUpcDigitSet[] parity)
    {
        return new string(parity.Select(value => value == EanUpcDigitSet.L ? 'L' : 'G').ToArray());
    }

    static byte[] CopyModules(byte[] modules)
    {
        byte[] copy = new byte[modules.Length];
        global::System.Array.Copy(modules, copy, modules.Length);
        return copy;
    }

    static void CopyModulesAt(byte[] source, byte[] destination, int offset)
    {
        global::System.Array.Copy(source, 0, destination, offset, source.Length);
    }
}
