namespace barcode.Tests;

public class Gs1DataMatrixCodecTests
{
    [Test]
    public void EncodeElementsCreatesFirstPositionFnc1IdentifierPayloadAndHri()
    {
        Gs1Element[] elements = new Gs1Element[]
        {
            CreateElement("01", "09506000134352", false),
            CreateElement("17", "260101", false)
        };
        BarcodeEncodeOptions options = new BarcodeEncodeOptions();
        options.TextMode = BarcodeTextMode.Standard;

        Gs1DataMatrixEncodedData encoded = Gs1DataMatrixCodec.EncodeElements(
            elements,
            null,
            options);

        Assert.Multiple(() =>
        {
            Assert.That(encoded.Encoding.MessagePlan.ControlCodewords, Is.EqualTo(new int[] { 232 }));
            Assert.That(encoded.Encoding.MessagePlan.Codewords[0], Is.EqualTo(232));
            Assert.That(encoded.ElementString, Is.EqualTo("010950600013435217260101"));
            Assert.That(encoded.HumanReadableText, Is.EqualTo("(01)09506000134352(17)260101"));
            Assert.That(encoded.ElementStringBytes, Is.EqualTo(global::System.Text.Encoding.ASCII.GetBytes(encoded.ElementString)));
            Assert.That(encoded.Symbol.Format, Is.EqualTo(BarcodeFormat.Gs1DataMatrix));
            Assert.That(encoded.Symbol.SymbologyIdentifier, Is.EqualTo("]d2"));
            Assert.That(encoded.Symbol.Dimension, Is.EqualTo(BarcodeDimension.Matrix));
            Assert.That(encoded.Symbol.Payload!.Text, Is.EqualTo(encoded.ElementString));
            Assert.That(encoded.Symbol.Payload.RawBytes, Is.EqualTo(encoded.ElementStringBytes));
            Assert.That(encoded.Symbol.Payload.CharacterEncoding, Is.EqualTo("ISO/IEC 646"));
            Assert.That(encoded.Symbol.Payload.IsGs1, Is.True);
            Assert.That(encoded.Symbol.TextLayout!.Runs![0].Text, Is.EqualTo(encoded.HumanReadableText));
            Assert.That(encoded.Symbol.Size!.ModuleSizeMillimetres, Is.EqualTo(0.495));
            Assert.That(options.Sizing, Is.Null);
        });
    }

    [Test]
    public void VariableElementSeparatorUsesFnc1AndPureRoundTripRestoresGs1Metadata()
    {
        Gs1DataMatrixEncodedData encoded = Gs1DataMatrixCodec.EncodeElements(
            new Gs1Element[]
            {
                CreateElement("10", "LOT123", true),
                CreateElement("21", "SERIAL", true)
            },
            null,
            null);

        Gs1DataMatrixDecodedData? decoded = Gs1DataMatrixCodec.TryDecodePure(
            encoded.Encoding.SymbolMatrix,
            null);
        int internalFnc1Count = encoded.Encoding.MessagePlan.DataPlan.Codewords.Count(
            codeword => codeword == DataMatrixHighLevelEncoder.Fnc1Codeword);

        Assert.Multiple(() =>
        {
            Assert.That(encoded.ElementString, Is.EqualTo("10LOT123\u001d21SERIAL"));
            Assert.That(encoded.HumanReadableText, Is.EqualTo("(10)LOT123(21)SERIAL"));
            Assert.That(internalFnc1Count, Is.EqualTo(1));
            Assert.That(decoded, Is.Not.Null);
            Assert.That(decoded!.ElementString, Is.EqualTo(encoded.ElementString));
            Assert.That(decoded.Result.Payload!.RawBytes, Is.EqualTo(encoded.ElementStringBytes));
            Assert.That(decoded.Result.Payload.Text, Is.EqualTo(encoded.ElementString));
            Assert.That(decoded.Result.Format, Is.EqualTo(BarcodeFormat.Gs1DataMatrix));
            Assert.That(decoded.Result.Metadata!.IsGs1, Is.True);
            Assert.That(decoded.Result.Metadata.SymbologyIdentifier, Is.EqualTo("]d2"));
            Assert.That(decoded.Result.Metadata.Gs1ElementString, Is.EqualTo(encoded.ElementString));
        });
    }

    [Test]
    public void TerminalVariableElementDoesNotAddSeparatorAndTextModesStayDisplayOnly()
    {
        Gs1Element[] elements = new Gs1Element[] { CreateElement("10", "LOT123", true) };
        BarcodeEncodeOptions rawOptions = CreateTextOptions(BarcodeTextMode.RawPayload, null);
        BarcodeEncodeOptions customOptions = CreateTextOptions(BarcodeTextMode.Custom, "custom label");
        BarcodeEncodeOptions hiddenOptions = CreateTextOptions(BarcodeTextMode.Hidden, null);

        Gs1DataMatrixEncodedData raw = Gs1DataMatrixCodec.EncodeElements(elements, null, rawOptions);
        Gs1DataMatrixEncodedData custom = Gs1DataMatrixCodec.EncodeElements(elements, null, customOptions);
        Gs1DataMatrixEncodedData hidden = Gs1DataMatrixCodec.EncodeElements(elements, null, hiddenOptions);

        Assert.Multiple(() =>
        {
            Assert.That(raw.ElementString, Is.EqualTo("10LOT123"));
            Assert.That(raw.ElementStringBytes, Does.Not.Contain((byte)Gs1DataMatrixCodec.GroupSeparator));
            Assert.That(raw.Symbol.TextLayout!.Runs![0].Text, Is.EqualTo("10LOT123"));
            Assert.That(custom.Symbol.TextLayout!.Runs![0].Text, Is.EqualTo("custom label"));
            Assert.That(custom.Symbol.Payload!.Text, Is.EqualTo("10LOT123"));
            Assert.That(hidden.Symbol.TextLayout, Is.Null);
        });
    }

    [Test]
    public void StructuredAppendStillPlacesFnc1InTheFirstDataPosition()
    {
        StructuredAppendInfo append = new StructuredAppendInfo();
        append.Format = BarcodeFormat.Gs1DataMatrix;
        append.Index = 0;
        append.Count = 2;
        append.FileIdentifier = "12";
        BarcodeEncodeOptions options = new BarcodeEncodeOptions();
        options.StructuredAppend = append;

        Gs1DataMatrixEncodedData encoded = Gs1DataMatrixCodec.EncodeElements(
            new Gs1Element[] { CreateElement("01", "09506000134352", false) },
            null,
            options);

        Assert.Multiple(() =>
        {
            Assert.That(encoded.Encoding.MessagePlan.ControlCodewords[0], Is.EqualTo(233));
            Assert.That(encoded.Encoding.MessagePlan.ControlCodewords[4], Is.EqualTo(232));
            Assert.That(encoded.Symbol.Payload!.StructuredAppend!.Format, Is.EqualTo(BarcodeFormat.Gs1DataMatrix));
            Assert.That(encoded.Symbol.Payload.StructuredAppend.Index, Is.EqualTo(0));
            Assert.That(append.Index, Is.EqualTo(0));
        });
    }

    [Test]
    public void ForcedBase256SegmentsAroundSeparatorKeepTheirAbsoluteRandomizationPositions()
    {
        DataMatrixOptions options = new DataMatrixOptions();
        options.EncodingMode = DataMatrixEncodingMode.Base256;
        Gs1DataMatrixEncodedData encoded = Gs1DataMatrixCodec.EncodeElements(
            new Gs1Element[]
            {
                CreateElement("10", "LOT", true),
                CreateElement("21", "SERIAL", true)
            },
            options,
            null);

        Gs1DataMatrixDecodedData? decoded = Gs1DataMatrixCodec.TryDecodePure(
            encoded.Encoding.SymbolMatrix,
            null);

        Assert.Multiple(() =>
        {
            Assert.That(encoded.Encoding.MessagePlan.DataPlan.SegmentModes, Is.EqualTo(new DataMatrixEncodingMode[]
            {
                DataMatrixEncodingMode.Base256,
                DataMatrixEncodingMode.Base256
            }));
            Assert.That(encoded.Encoding.MessagePlan.DataPlan.Codewords, Does.Contain(232));
            Assert.That(decoded, Is.Not.Null);
            Assert.That(decoded!.ElementString, Is.EqualTo("10LOT\u001d21SERIAL"));
        });
    }

    [Test]
    public void GeneralDistributionSizingAcceptsBoundariesAndRejectsOutOfRangeModules()
    {
        Gs1Element[] elements = new Gs1Element[]
        {
            CreateElement("01", "09506000134352", false)
        };
        BarcodeEncodeOptions minimum = CreateModuleSizeOptions(0.396);
        BarcodeEncodeOptions maximum = CreateModuleSizeOptions(0.990);
        BarcodeEncodeOptions tooSmall = CreateModuleSizeOptions(0.395);
        BarcodeEncodeOptions tooLarge = CreateModuleSizeOptions(0.991);
        BarcodeEncodeOptions missingQuietZone = CreateModuleSizeOptions(0.625);
        missingQuietZone.QuietZonePolicy = BarcodeQuietZonePolicy.ExactCustom;
        missingQuietZone.QuietZone = CreateQuietZone(0, 1, 1, 1);

        Gs1DataMatrixEncodedData minimumEncoded = Gs1DataMatrixCodec.EncodeElements(elements, null, minimum);
        Gs1DataMatrixEncodedData maximumEncoded = Gs1DataMatrixCodec.EncodeElements(elements, null, maximum);

        Assert.Multiple(() =>
        {
            Assert.That(minimumEncoded.Symbol.Size!.ModuleSizeMillimetres, Is.EqualTo(0.396));
            Assert.That(maximumEncoded.Symbol.Size!.ModuleSizeMillimetres, Is.EqualTo(0.990));
            Assert.That(() => Gs1DataMatrixCodec.EncodeElements(elements, null, tooSmall), Throws.TypeOf<BarcodeEncodingException>());
            Assert.That(() => Gs1DataMatrixCodec.EncodeElements(elements, null, tooLarge), Throws.TypeOf<BarcodeEncodingException>());
            Assert.That(() => Gs1DataMatrixCodec.EncodeElements(elements, null, missingQuietZone), Throws.TypeOf<BarcodeEncodingException>());
        });
    }

    [Test]
    public void ProfileRejectsMalformedElementsAndNonGs1OptionsOrSymbols()
    {
        Gs1Element[] valid = new Gs1Element[] { CreateElement("10", "LOT", true) };
        BarcodeEncodeOptions eci = new BarcodeEncodeOptions();
        eci.EciAssignmentNumber = 26;
        DataMatrixOptions matrixEci = new DataMatrixOptions();
        matrixEci.EciAssignmentNumber = 26;
        DataMatrixEncodingResult plain = DataMatrixEncoder.Encode(
            global::System.Text.Encoding.ASCII.GetBytes("10LOT"),
            CreatePayload("10LOT"),
            null,
            null,
            false);

        Assert.Multiple(() =>
        {
            Assert.That(() => Gs1DataMatrixCodec.EncodeElements(null!, null, null), Throws.TypeOf<BarcodeEncodingException>());
            Assert.That(() => Gs1DataMatrixCodec.EncodeElements(new Gs1Element[0], null, null), Throws.TypeOf<BarcodeEncodingException>());
            Assert.That(() => Gs1DataMatrixCodec.EncodeElements(new Gs1Element[] { null! }, null, null), Throws.TypeOf<BarcodeEncodingException>());
            Assert.That(() => EncodeSingle(CreateElement("1", "A", false)), Throws.TypeOf<BarcodeEncodingException>());
            Assert.That(() => EncodeSingle(CreateElement("1A", "A", false)), Throws.TypeOf<BarcodeEncodingException>());
            Assert.That(() => EncodeSingle(CreateElement("04", "ABC", false)), Throws.TypeOf<BarcodeEncodingException>());
            Assert.That(() => EncodeSingle(CreateElement("17", "2601", false)), Throws.TypeOf<BarcodeEncodingException>());
            Assert.That(() => EncodeSingle(CreateElement("10", "", true)), Throws.TypeOf<BarcodeEncodingException>());
            Assert.That(() => EncodeSingle(CreateElement("10", "A\nB", true)), Throws.TypeOf<BarcodeEncodingException>());
            Assert.That(() => Gs1DataMatrixCodec.EncodeElements(valid, null, eci), Throws.TypeOf<BarcodeEncodingException>());
            Assert.That(() => Gs1DataMatrixCodec.EncodeElements(valid, matrixEci, null), Throws.TypeOf<BarcodeEncodingException>());
            Assert.That(Gs1DataMatrixCodec.TryDecodePure(plain.SymbolMatrix, null), Is.Null);
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

    static BarcodeEncodeOptions CreateModuleSizeOptions(double moduleSize)
    {
        BarcodeSizing sizing = new BarcodeSizing();
        sizing.ModuleSizeMillimetres = moduleSize;
        BarcodeEncodeOptions options = new BarcodeEncodeOptions();
        options.Sizing = sizing;
        return options;
    }

    static BarcodeQuietZone CreateQuietZone(int left, int top, int right, int bottom)
    {
        BarcodeQuietZone quietZone = new BarcodeQuietZone();
        quietZone.LeftModules = left;
        quietZone.TopModules = top;
        quietZone.RightModules = right;
        quietZone.BottomModules = bottom;
        return quietZone;
    }

    static BarcodePayload CreatePayload(string text)
    {
        BarcodePayload payload = new BarcodePayload();
        payload.Text = text;
        payload.RawBytes = global::System.Text.Encoding.ASCII.GetBytes(text);
        return payload;
    }

    static void EncodeSingle(Gs1Element element)
    {
        Gs1DataMatrixCodec.EncodeElements(new Gs1Element[] { element }, null, null);
    }
}
