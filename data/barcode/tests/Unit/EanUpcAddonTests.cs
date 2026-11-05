namespace barcode.Tests;

public class EanUpcAddonTests
{
    [Test]
    public void TwoDigitAddonMatchesIndependentParityAndModuleOracle()
    {
        string expectedModules =
            "1011" +
            "0011001" +
            "01" +
            "0010011";

        EanUpcEncodedData encoded = EanUpcAddon.Encode("12");

        Assert.Multiple(() =>
        {
            Assert.That(encoded.Format, Is.EqualTo(BarcodeFormat.EanUpcAddon2));
            Assert.That(encoded.Text, Is.EqualTo("12"));
            Assert.That(encoded.Digits, Is.EqualTo(new int[] { 1, 2 }));
            Assert.That(ToParityString(encoded.LeftParity), Is.EqualTo("LL"));
            Assert.That(encoded.Modules, Has.Length.EqualTo(EanUpcAddon.TwoDigitModuleCount));
            Assert.That(ToModuleString(encoded.Modules), Is.EqualTo(expectedModules));
            Assert.That(encoded.ChecksumValue, Is.EqualTo(0));
            Assert.That(encoded.ChecksumCalculated, Is.True);
        });
    }

    [TestCase("12", 0, "LL")]
    [TestCase("13", 1, "LG")]
    [TestCase("14", 2, "GL")]
    [TestCase("15", 3, "GG")]
    public void TwoDigitValueModuloFourSelectsParity(
        string digits,
        int expectedRemainder,
        string expectedParity)
    {
        EanUpcEncodedData encoded = EanUpcAddon.Encode(digits);

        Assert.Multiple(() =>
        {
            Assert.That(encoded.ChecksumValue, Is.EqualTo(expectedRemainder));
            Assert.That(ToParityString(encoded.LeftParity), Is.EqualTo(expectedParity));
        });
    }

    [Test]
    public void FiveDigitAddonMatchesIndependentChecksumParityAndModuleOracle()
    {
        string expectedModules =
            "1011" +
            "0001001" +
            "01" +
            "0101111" +
            "01" +
            "0011001" +
            "01" +
            "0001101" +
            "01" +
            "0011101";

        EanUpcEncodedData encoded = EanUpcAddon.Encode("86104");

        Assert.Multiple(() =>
        {
            Assert.That(encoded.Format, Is.EqualTo(BarcodeFormat.EanUpcAddon5));
            Assert.That(encoded.Text, Is.EqualTo("86104"));
            Assert.That(encoded.Digits, Is.EqualTo(new int[] { 8, 6, 1, 0, 4 }));
            Assert.That(ToParityString(encoded.LeftParity), Is.EqualTo("GLLLG"));
            Assert.That(encoded.Modules, Has.Length.EqualTo(EanUpcAddon.FiveDigitModuleCount));
            Assert.That(ToModuleString(encoded.Modules), Is.EqualTo(expectedModules));
            Assert.That(encoded.ChecksumValue, Is.EqualTo(3));
            Assert.That(encoded.ChecksumCalculated, Is.True);
        });
    }

    [TestCase("00000", 0, "GGLLL")]
    [TestCase("00001", 3, "GLLLG")]
    [TestCase("00010", 9, "LLGLG")]
    [TestCase("00100", 3, "GLLLG")]
    [TestCase("01000", 9, "LLGLG")]
    [TestCase("10000", 3, "GLLLG")]
    [TestCase("51234", 9, "LLGLG")]
    public void FiveDigitWeightedChecksumSelectsParity(
        string digits,
        int expectedChecksum,
        string expectedParity)
    {
        EanUpcEncodedData encoded = EanUpcAddon.Encode(digits);

        Assert.Multiple(() =>
        {
            Assert.That(encoded.ChecksumValue, Is.EqualTo(expectedChecksum));
            Assert.That(ToParityString(encoded.LeftParity), Is.EqualTo(expectedParity));
        });
    }

    [Test]
    public void AddonRejectsMissingWrongLengthAndNonAsciiInput()
    {
        Assert.Multiple(() =>
        {
            Assert.That(() => EanUpcAddon.Encode(null!), Throws.TypeOf<BarcodeEncodingException>());
            Assert.That(() => EanUpcAddon.Encode(""), Throws.TypeOf<BarcodeEncodingException>());
            Assert.That(() => EanUpcAddon.Encode("1"), Throws.TypeOf<BarcodeEncodingException>());
            Assert.That(() => EanUpcAddon.Encode("123"), Throws.TypeOf<BarcodeEncodingException>());
            Assert.That(() => EanUpcAddon.Encode("1234"), Throws.TypeOf<BarcodeEncodingException>());
            Assert.That(() => EanUpcAddon.Encode("123456"), Throws.TypeOf<BarcodeEncodingException>());
            Assert.That(() => EanUpcAddon.Encode("1A"), Throws.TypeOf<BarcodeEncodingException>());
            Assert.That(() => EanUpcAddon.Encode("12 45"), Throws.TypeOf<BarcodeEncodingException>());
            Assert.That(() => EanUpcAddon.Encode("１２"), Throws.TypeOf<BarcodeEncodingException>());
        });
    }

    [TestCase(BarcodeFormat.Ean13)]
    [TestCase(BarcodeFormat.UpcA)]
    [TestCase(BarcodeFormat.UpcE)]
    public void AssociationRetainsSupportedMainIdentityAndSeparateAddon(BarcodeFormat mainFormat)
    {
        EanUpcEncodedData main = CreateMain(mainFormat);
        EanUpcOptions options = new EanUpcOptions();
        options.AddOn = "12";

        EanUpcAddonAssociation association = EanUpcAddon.Associate(main, options);

        Assert.Multiple(() =>
        {
            Assert.That(association.Main, Is.SameAs(main));
            Assert.That(association.Main.Format, Is.EqualTo(mainFormat));
            Assert.That(association.AddOn, Is.Not.Null);
            Assert.That(association.AddOn!.Format, Is.EqualTo(BarcodeFormat.EanUpcAddon2));
            Assert.That(association.AddOn.Text, Is.EqualTo("12"));
            Assert.That(association.AddOn.Modules, Has.Length.EqualTo(20));
            Assert.That(association.Main.Modules, Is.Not.SameAs(association.AddOn.Modules));
        });
    }

    [Test]
    public void OptionalAndRequiredAssociationPoliciesHandleMissingAddon()
    {
        EanUpcEncodedData main = CreateMain(BarcodeFormat.Ean13);
        EanUpcOptions optional = new EanUpcOptions();
        EanUpcOptions required = new EanUpcOptions();
        required.RequireAddOn = true;

        EanUpcAddonAssociation withoutOptions = EanUpcAddon.Associate(main, null);
        EanUpcAddonAssociation withoutAddon = EanUpcAddon.Associate(main, optional);

        Assert.Multiple(() =>
        {
            Assert.That(withoutOptions.Main, Is.SameAs(main));
            Assert.That(withoutOptions.AddOn, Is.Null);
            Assert.That(withoutAddon.Main, Is.SameAs(main));
            Assert.That(withoutAddon.AddOn, Is.Null);
            Assert.That(
                () => EanUpcAddon.Associate(main, required),
                Throws.TypeOf<BarcodeEncodingException>()
                    .With.Message.EqualTo("An EAN/UPC add-on is required."));
        });
    }

    [Test]
    public void AllowedLengthPolicyAcceptsConfiguredLengthAndRejectsOthers()
    {
        EanUpcEncodedData main = CreateMain(BarcodeFormat.UpcA);
        EanUpcOptions allowTwo = CreateOptions("12", new int[] { 2 });
        EanUpcOptions allowFive = CreateOptions("51234", new int[] { 5 });
        EanUpcOptions rejectTwo = CreateOptions("12", new int[] { 5 });
        EanUpcOptions rejectAll = CreateOptions("51234", new int[0]);
        EanUpcOptions invalid = CreateOptions(null, new int[] { 3 });

        EanUpcAddonAssociation two = EanUpcAddon.Associate(main, allowTwo);
        EanUpcAddonAssociation five = EanUpcAddon.Associate(main, allowFive);

        Assert.Multiple(() =>
        {
            Assert.That(two.AddOn!.Format, Is.EqualTo(BarcodeFormat.EanUpcAddon2));
            Assert.That(five.AddOn!.Format, Is.EqualTo(BarcodeFormat.EanUpcAddon5));
            Assert.That(() => EanUpcAddon.Associate(main, rejectTwo), Throws.TypeOf<BarcodeEncodingException>());
            Assert.That(() => EanUpcAddon.Associate(main, rejectAll), Throws.TypeOf<BarcodeEncodingException>());
            Assert.That(() => EanUpcAddon.Associate(main, invalid), Throws.TypeOf<BarcodeEncodingException>());
        });
    }

    [Test]
    public void AssociationRejectsUnsupportedAndIncompleteMainSymbols()
    {
        EanUpcOptions options = new EanUpcOptions();
        options.AddOn = "12";
        EanUpcEncodedData ean8 = CreateMain(BarcodeFormat.Ean8);
        EanUpcEncodedData addon = EanUpcAddon.Encode("12");
        EanUpcEncodedData incomplete = new EanUpcEncodedData();
        incomplete.Format = BarcodeFormat.Ean13;
        incomplete.Text = "4006381333931";
        incomplete.Modules = new byte[1];

        Assert.Multiple(() =>
        {
            Assert.That(() => EanUpcAddon.Associate(null!, options), Throws.TypeOf<BarcodeEncodingException>());
            Assert.That(() => EanUpcAddon.Associate(ean8, options), Throws.TypeOf<BarcodeEncodingException>());
            Assert.That(() => EanUpcAddon.Associate(addon, options), Throws.TypeOf<BarcodeEncodingException>());
            Assert.That(() => EanUpcAddon.Associate(incomplete, options), Throws.TypeOf<BarcodeEncodingException>());
        });
    }

    [Test]
    public void EncodedAddonsDoNotAliasCanonicalOrLaterResults()
    {
        EanUpcEncodedData first = EanUpcAddon.Encode("86104");
        first.Modules[0] = 0;
        first.Digits[0] = 0;
        first.LeftParity[0] = EanUpcDigitSet.L;

        EanUpcEncodedData second = EanUpcAddon.Encode("86104");

        Assert.Multiple(() =>
        {
            Assert.That(second.Modules[0], Is.EqualTo(1));
            Assert.That(second.Digits[0], Is.EqualTo(8));
            Assert.That(ToParityString(second.LeftParity), Is.EqualTo("GLLLG"));
        });
    }

    static EanUpcEncodedData CreateMain(BarcodeFormat format)
    {
        switch (format)
        {
            case BarcodeFormat.Ean13:
                return EanUpcEncoder.Encode("4006381333931", format, BarcodeChecksumMode.Require);
            case BarcodeFormat.Ean8:
                return EanUpcEncoder.Encode("55123457", format, BarcodeChecksumMode.Require);
            case BarcodeFormat.UpcA:
                return EanUpcEncoder.Encode("036000291452", format, BarcodeChecksumMode.Require);
            case BarcodeFormat.UpcE:
                return EanUpcEncoder.EncodeUpcE("01234558", 0, BarcodeChecksumMode.Require);
            default:
                throw new global::System.InvalidOperationException();
        }
    }

    static EanUpcOptions CreateOptions(string? addOn, int[] allowedLengths)
    {
        EanUpcOptions options = new EanUpcOptions();
        options.AddOn = addOn;
        options.AllowedAddOnLengths = allowedLengths;
        return options;
    }

    static string ToModuleString(byte[] modules)
    {
        char[] characters = new char[modules.Length];
        int index = 0;
        while (index < modules.Length)
        {
            characters[index] = modules[index] == 0 ? '0' : '1';
            index = index + 1;
        }

        return new string(characters);
    }

    static string ToParityString(EanUpcDigitSet[] parity)
    {
        char[] characters = new char[parity.Length];
        int index = 0;
        while (index < parity.Length)
        {
            characters[index] = parity[index] == EanUpcDigitSet.L ? 'L' : 'G';
            index = index + 1;
        }

        return new string(characters);
    }
}
