namespace barcode.Tests;

public class Gs1128CodecTests
{
    [Test]
    public void EncodeElementsAddsLeadingFnc1AndGs1Metadata()
    {
        Gs1Element[] elements = new Gs1Element[]
        {
            CreateElement("01", "09506000134352", false),
            CreateElement("17", "260101", false)
        };
        Code128Options code128Options = new Code128Options();
        BarcodeEncodeOptions encodeOptions = new BarcodeEncodeOptions();
        encodeOptions.TextMode = BarcodeTextMode.Standard;

        Gs1128EncodedData encoded = Gs1128Codec.EncodeElements(elements, code128Options, encodeOptions);

        Assert.Multiple(() =>
        {
            Assert.That(encoded.Tokens[0], Is.EqualTo(Code128Planner.Function1Token));
            Assert.That(encoded.Tokens.Count(token => token == Code128Planner.Function1Token), Is.EqualTo(1));
            Assert.That(encoded.Symbol.Codewords![1], Is.EqualTo(Code128Patterns.Function1Codeword));
            Assert.That(encoded.DataCharacterCount, Is.EqualTo(24));
            Assert.That(encoded.ElementString, Is.EqualTo("010950600013435217260101"));
            Assert.That(encoded.HumanReadableText, Is.EqualTo("(01)09506000134352(17)260101"));
            Assert.That(encoded.ElementStringBytes, Is.EqualTo(global::System.Text.Encoding.ASCII.GetBytes("010950600013435217260101")));
            Assert.That(encoded.Symbol.Format, Is.EqualTo(BarcodeFormat.Gs1128));
            Assert.That(encoded.Symbol.SymbologyIdentifier, Is.EqualTo("]C1"));
            Assert.That(encoded.Symbol.Payload!.Text, Is.EqualTo(encoded.ElementString));
            Assert.That(encoded.Symbol.Payload.RawBytes, Is.EqualTo(encoded.ElementStringBytes));
            Assert.That(encoded.Symbol.Payload.IsGs1, Is.True);
            Assert.That(encoded.Symbol.TextLayout!.Runs![0].Text, Is.EqualTo(encoded.HumanReadableText));
            Assert.That(encoded.Tokens, Does.Not.Contain((int)'('));
            Assert.That(encoded.Tokens, Does.Not.Contain((int)')'));
            Assert.That(code128Options.AllowFnc1, Is.False);
        });
    }

    [Test]
    public void NonTerminalVariableElementGetsFnc1SeparatorButTerminalElementDoesNot()
    {
        Gs1128EncodedData nonTerminal = Gs1128Codec.EncodeElements(
            new Gs1Element[]
            {
                CreateElement("10", "LOT123", true),
                CreateElement("21", "SERIAL", true)
            },
            null,
            null);
        Gs1128EncodedData terminal = Gs1128Codec.EncodeElements(
            new Gs1Element[] { CreateElement("10", "LOT123", true) },
            null,
            null);
        int separatorIndex = "10LOT123".Length;

        Assert.Multiple(() =>
        {
            Assert.That(nonTerminal.Tokens[separatorIndex + 1], Is.EqualTo(Code128Planner.Function1Token));
            Assert.That(nonTerminal.ElementStringBytes[separatorIndex], Is.EqualTo(Gs1128Codec.GroupSeparator));
            Assert.That(nonTerminal.ElementString, Is.EqualTo("10LOT123\u001d21SERIAL"));
            Assert.That(nonTerminal.HumanReadableText, Is.EqualTo("(10)LOT123(21)SERIAL"));
            Assert.That(nonTerminal.Tokens.Count(token => token == Code128Planner.Function1Token), Is.EqualTo(2));
            Assert.That(terminal.Tokens.Count(token => token == Code128Planner.Function1Token), Is.EqualTo(1));
            Assert.That(terminal.ElementString, Is.EqualTo("10LOT123"));
        });
    }

    [Test]
    public void EncodeElementsOrdersPrimaryFixedAndVariableElementsStably()
    {
        Gs1Element[] elements = new Gs1Element[]
        {
            CreateElement("10", "LOT", true),
            CreateElement("21", "SERIAL", true),
            CreateElement("17", "260101", false),
            CreateElement("01", "09506000134352", false)
        };

        Gs1128EncodedData encoded = Gs1128Codec.EncodeElements(
            elements,
            null,
            null);

        Assert.Multiple(() =>
        {
            Assert.That(encoded.ElementString, Is.EqualTo(
                "01095060001343521726010110LOT\u001d21SERIAL"));
            Assert.That(encoded.HumanReadableText, Is.EqualTo(
                "(01)09506000134352(17)260101(10)LOT(21)SERIAL"));
            Assert.That(encoded.Tokens.Count(
                token => token == Code128Planner.Function1Token),
                Is.EqualTo(2));
            Assert.That(elements[0].ApplicationIdentifier, Is.EqualTo("10"));
            Assert.That(elements[1].ApplicationIdentifier, Is.EqualTo("21"));
        });
    }

    [Test]
    public void StandardRawAndCustomTextModesKeepEncodingSeparateFromDisplay()
    {
        Gs1Element[] elements = new Gs1Element[]
        {
            CreateElement("10", "LOT", true),
            CreateElement("17", "260101", false)
        };
        BarcodeEncodeOptions standardOptions = CreateTextOptions(BarcodeTextMode.Standard, null);
        BarcodeEncodeOptions rawOptions = CreateTextOptions(BarcodeTextMode.RawPayload, null);
        BarcodeEncodeOptions customOptions = CreateTextOptions(BarcodeTextMode.Custom, "custom label");

        Gs1128EncodedData standard = Gs1128Codec.EncodeElements(elements, null, standardOptions);
        Gs1128EncodedData raw = Gs1128Codec.EncodeElements(elements, null, rawOptions);
        Gs1128EncodedData custom = Gs1128Codec.EncodeElements(elements, null, customOptions);

        Assert.Multiple(() =>
        {
            Assert.That(standard.Symbol.TextLayout!.Runs![0].Text, Is.EqualTo("(17)260101(10)LOT"));
            Assert.That(raw.Symbol.TextLayout!.Runs![0].Text, Is.EqualTo("1726010110LOT"));
            Assert.That(custom.Symbol.TextLayout!.Runs![0].Text, Is.EqualTo("custom label"));
            Assert.That(standard.ElementStringBytes, Does.Not.Contain((byte)'('));
            Assert.That(standard.ElementStringBytes, Does.Not.Contain((byte)')'));
        });
    }

    [Test]
    public void EncodeElementsAcceptsFortyEightDataCharactersAndRejectsOneMore()
    {
        Gs1Element maximum = CreateElement("10", new string('A', 46), true);
        Gs1Element excessive = CreateElement("10", new string('A', 47), true);

        Gs1128EncodedData encoded = Gs1128Codec.EncodeElements(new Gs1Element[] { maximum }, null, null);

        Assert.Multiple(() =>
        {
            Assert.That(encoded.DataCharacterCount, Is.EqualTo(48));
            Assert.That(encoded.Tokens, Has.Length.EqualTo(49));
            Assert.That(
                () => Gs1128Codec.EncodeElements(new Gs1Element[] { excessive }, null, null),
                Throws.TypeOf<BarcodeEncodingException>()
                    .With.Message.EqualTo("GS1-128 element data exceeds the 48-character limit."));
        });
    }

    [Test]
    public void CapacityIncludesARequiredSeparatorBetweenElements()
    {
        Gs1Element terminalVariable = CreateElement("21", "B", true);
        Gs1Element maximumVariable = CreateElement(
            "10",
            new string('A', 42),
            true);
        Gs1Element excessiveVariable = CreateElement(
            "10",
            new string('A', 43),
            true);

        Gs1128EncodedData encoded = Gs1128Codec.EncodeElements(
            new Gs1Element[] { maximumVariable, terminalVariable },
            null,
            null);

        Assert.Multiple(() =>
        {
            Assert.That(encoded.DataCharacterCount, Is.EqualTo(48));
            Assert.That(encoded.ElementString[44], Is.EqualTo((char)29));
            Assert.That(
                () => Gs1128Codec.EncodeElements(
                    new Gs1Element[] { excessiveVariable, terminalVariable },
                    null,
                    null),
                Throws.TypeOf<BarcodeEncodingException>()
                    .With.Message.EqualTo(
                        "GS1-128 element data exceeds the 48-character limit."));
        });
    }

    [Test]
    public void EncodeElementsEnforcesMaximumPhysicalWidthIncludingQuietZones()
    {
        BarcodeEncodeOptions maximumOptions = CreateWidthOptions(165.10);
        BarcodeEncodeOptions excessiveOptions = CreateWidthOptions(165.11);
        Gs1Element[] elements = new Gs1Element[] { CreateElement("01", "09506000134352", false) };

        Gs1128EncodedData maximum = Gs1128Codec.EncodeElements(elements, null, maximumOptions);

        Assert.Multiple(() =>
        {
            Assert.That(maximum.Symbol.Size!.WidthMillimetres, Is.EqualTo(165.10));
            Assert.That(
                () => Gs1128Codec.EncodeElements(elements, null, excessiveOptions),
                Throws.TypeOf<BarcodeEncodingException>()
                    .With.Message.EqualTo("GS1-128 physical width including quiet zones must not exceed 165.10 millimetres."));
        });
    }

    [Test]
    public void ScanlineRoundTripRestoresSeparatorAndGs1DecodeMetadata()
    {
        Gs1128EncodedData encoded = Gs1128Codec.EncodeElements(
            new Gs1Element[]
            {
                CreateElement("10", "LOT123", true),
                CreateElement("17", "260101", false)
            },
            null,
            null);
        BitRow row = CreateScanline(encoded.Symbol.Modules!, 10, 10);

        Gs1128DecodedData? decoded = Gs1128Codec.TryDecodeScanline(row);

        Assert.Multiple(() =>
        {
            Assert.That(decoded, Is.Not.Null);
            Assert.That(decoded!.Result.Format, Is.EqualTo(BarcodeFormat.Gs1128));
            Assert.That(decoded.Result.Dimension, Is.EqualTo(BarcodeDimension.Linear));
            Assert.That(decoded.Result.Payload!.Text, Is.EqualTo("1726010110LOT123"));
            Assert.That(decoded.Result.Payload.RawBytes, Is.EqualTo(encoded.ElementStringBytes));
            Assert.That(decoded.Result.Payload.IsGs1, Is.True);
            Assert.That(decoded.Result.Metadata!.SymbologyIdentifier, Is.EqualTo("]C1"));
            Assert.That(decoded.Result.Metadata.HasChecksum, Is.True);
            Assert.That(decoded.Result.Metadata.ChecksumValid, Is.True);
            Assert.That(decoded.Result.Metadata.IsGs1, Is.True);
            Assert.That(decoded.Result.Metadata.Gs1ElementString, Is.EqualTo(encoded.ElementString));
            Assert.That(decoded.Result.Metadata.Codewords, Is.EqualTo(encoded.Symbol.Codewords));
            Assert.That(decoded.Result.Metadata.Confidence, Is.EqualTo(1.0));
            Assert.That(decoded.Tokens, Is.EqualTo(encoded.Tokens));
            Assert.That(decoded.StartPosition, Is.EqualTo(10));
            Assert.That(decoded.EndPosition, Is.EqualTo(10 + encoded.Symbol.Modules!.Length));
        });
    }

    [Test]
    public void ReversedScanlineRoundTripPreservesSeparatorAndCoordinates()
    {
        Gs1128EncodedData encoded = Gs1128Codec.EncodeElements(
            new Gs1Element[]
            {
                CreateElement("10", "A", true),
                CreateElement("21", "B", true)
            },
            null,
            null);
        BitRow reversed = CreateScanline(encoded.Symbol.Modules!, 12, 10);
        Gs1128DecodedData? forwardDecoded = Gs1128Codec.TryDecodeScanline(
            reversed);
        reversed.ReverseBits();

        Gs1128DecodedData? decoded = Gs1128Codec.TryDecodeScanline(reversed);

        Assert.Multiple(() =>
        {
            Assert.That(forwardDecoded, Is.Not.Null);
            Assert.That(decoded, Is.Not.Null);
            Assert.That(decoded!.IsReversed, Is.True);
            Assert.That(decoded.StartPosition, Is.EqualTo(10));
            Assert.That(decoded.EndPosition,
                Is.EqualTo(10 + encoded.Symbol.Modules!.Length));
            Assert.That(decoded.Result.Payload!.Text,
                Is.EqualTo("10A\u001d21B"));
            Assert.That(decoded.Result.Metadata!.Gs1ElementString,
                Is.EqualTo("10A\u001d21B"));
        });
    }

    [Test]
    public void ScanlineProfileRejectsPlainCode128AndInvalidFunctionPlacement()
    {
        Code128EncodedData plain = Code128Encoder.EncodeBytes(new byte[] { 65, 66, 67 }, null);
        Code128Options functionOptions = new Code128Options();
        functionOptions.AllowFnc1 = true;
        functionOptions.AllowFnc2 = true;
        Code128EncodedData invalidFunction = Code128Encoder.EncodeTokens(
            new int[] { Code128Planner.Function1Token, Code128Planner.Function2Token, 'A' },
            functionOptions);
        Code128EncodedData terminalSeparator = Code128Encoder.EncodeTokens(
            new int[] { Code128Planner.Function1Token, '1', Code128Planner.Function1Token },
            functionOptions);

        Assert.Multiple(() =>
        {
            Assert.That(Gs1128Codec.TryDecodeScanline(CreateScanline(plain.Modules, 10, 10)), Is.Null);
            Assert.That(Gs1128Codec.TryDecodeScanline(CreateScanline(invalidFunction.Modules, 10, 10)), Is.Null);
            Assert.That(Gs1128Codec.TryDecodeScanline(CreateScanline(terminalSeparator.Modules, 10, 10)), Is.Null);
        });
    }

    [Test]
    public void ScanlineProfileRejectsLeadingAdjacentAndOverCapacityData()
    {
        Code128Options options = new Code128Options();
        options.AllowFnc1 = true;
        Code128EncodedData leadingSeparator = Code128Encoder.EncodeTokens(
            new int[]
            {
                Code128Planner.Function1Token,
                Code128Planner.Function1Token,
                'A'
            },
            options);
        Code128EncodedData adjacentSeparators = Code128Encoder.EncodeTokens(
            new int[]
            {
                Code128Planner.Function1Token,
                'A',
                Code128Planner.Function1Token,
                Code128Planner.Function1Token,
                'B'
            },
            options);
        int[] excessiveTokens = new int[50];
        excessiveTokens[0] = Code128Planner.Function1Token;
        int index = 1;
        while (index < excessiveTokens.Length)
        {
            excessiveTokens[index] = 'A';
            index = index + 1;
        }
        Code128EncodedData excessive = Code128Encoder.EncodeTokens(
            excessiveTokens,
            options);

        Assert.Multiple(() =>
        {
            Assert.That(Gs1128Codec.TryDecodeScanline(CreateScanline(
                leadingSeparator.Modules, 10, 10)), Is.Null);
            Assert.That(Gs1128Codec.TryDecodeScanline(CreateScanline(
                adjacentSeparators.Modules, 10, 10)), Is.Null);
            Assert.That(Gs1128Codec.TryDecodeScanline(CreateScanline(
                excessive.Modules, 10, 10)), Is.Null);
        });
    }

    [Test]
    public void EncodeElementsRejectsMalformedNormalizedElements()
    {
        Gs1Element invalidAiLength = CreateElement("1", "A", false);
        Gs1Element invalidAiCharacter = CreateElement("1A", "A", false);
        Gs1Element missingAi = CreateElement(null, "A", false);
        Gs1Element missingValue = CreateElement("10", null, true);
        Gs1Element emptyValue = CreateElement("10", "", true);
        Gs1Element controlValue = CreateElement("10", "A\nB", true);
        Gs1Element nonAsciiValue = CreateElement("10", "caf\u00e9", true);

        Assert.Multiple(() =>
        {
            Assert.That(() => Gs1128Codec.EncodeElements(null!, null, null), Throws.TypeOf<BarcodeEncodingException>());
            Assert.That(() => Gs1128Codec.EncodeElements(new Gs1Element[0], null, null), Throws.TypeOf<BarcodeEncodingException>());
            Assert.That(() => Gs1128Codec.EncodeElements(new Gs1Element[] { null! }, null, null), Throws.TypeOf<BarcodeEncodingException>());
            Assert.That(() => EncodeSingle(invalidAiLength), Throws.TypeOf<BarcodeEncodingException>());
            Assert.That(() => EncodeSingle(invalidAiCharacter), Throws.TypeOf<BarcodeEncodingException>());
            Assert.That(() => EncodeSingle(missingAi), Throws.TypeOf<BarcodeEncodingException>());
            Assert.That(() => EncodeSingle(missingValue), Throws.TypeOf<BarcodeEncodingException>());
            Assert.That(() => EncodeSingle(emptyValue), Throws.TypeOf<BarcodeEncodingException>());
            Assert.That(() => EncodeSingle(controlValue), Throws.TypeOf<BarcodeEncodingException>());
            Assert.That(() => EncodeSingle(nonAsciiValue), Throws.TypeOf<BarcodeEncodingException>());
        });
    }

    static Gs1Element CreateElement(string? applicationIdentifier, string? value, bool variableLength)
    {
        Gs1Element element = new Gs1Element();
        element.ApplicationIdentifier = applicationIdentifier;
        element.Value = value;
        element.VariableLength = variableLength;
        return element;
    }

    static BarcodeEncodeOptions CreateTextOptions(BarcodeTextMode mode, string? customText)
    {
        BarcodeEncodeOptions options = new BarcodeEncodeOptions();
        options.TextMode = mode;
        options.CustomText = customText;
        return options;
    }

    static BarcodeEncodeOptions CreateWidthOptions(double widthMillimetres)
    {
        BarcodeSizing sizing = new BarcodeSizing();
        sizing.RequestedWidthMillimetres = widthMillimetres;
        BarcodeEncodeOptions options = new BarcodeEncodeOptions();
        options.Sizing = sizing;
        return options;
    }

    static BitRow CreateScanline(byte[] modules, int leftQuietModules, int rightQuietModules)
    {
        BitRow row = new BitRow(leftQuietModules + modules.Length + rightQuietModules);
        int index = 0;
        while (index < modules.Length)
        {
            if (modules[index] != 0)
            {
                row.SetBit(leftQuietModules + index, true);
            }

            index = index + 1;
        }

        return row;
    }

    static void EncodeSingle(Gs1Element element)
    {
        Gs1128Codec.EncodeElements(new Gs1Element[] { element }, null, null);
    }
}
