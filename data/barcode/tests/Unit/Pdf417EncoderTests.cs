namespace barcode.Tests;

public class Pdf417EncoderTests
{
    [Test]
    public void CompleteDefaultPipelineCompactsPlansProtectsAndLaysOutTheSymbol()
    {
        byte[] data = Bytes("PDF417");
        BarcodePayload payload = CreatePayload(data, "PDF417");

        Pdf417EncodingResult result = Pdf417Encoder.Encode(
            data,
            payload,
            null,
            null);

        Assert.Multiple(() =>
        {
            Assert.That(result.CompactionPlan.Codewords, Is.EqualTo(new int[]
            {
                453, 178, 121, 239
            }));
            Assert.That(result.MacroCodewords, Is.Empty);
            Assert.That(result.HighLevelCodewords, Is.EqualTo(result.CompactionPlan.Codewords));
            Assert.That(result.SymbolPlan.Columns, Is.EqualTo(2));
            Assert.That(result.SymbolPlan.Rows, Is.EqualTo(7));
            Assert.That(result.SymbolPlan.ErrorCorrectionLevel, Is.EqualTo(2));
            Assert.That(result.SymbolPlan.ErrorCorrectionCodewordCount, Is.EqualTo(8));
            Assert.That(result.DataCodewords, Is.EqualTo(new int[]
            {
                6, 453, 178, 121, 239, 900
            }));
            Assert.That(result.ErrorCorrectionCodewords, Has.Length.EqualTo(8));
            Assert.That(result.SymbolCodewords, Is.EqualTo(
                result.DataCodewords.Concat(result.ErrorCorrectionCodewords)));
            Assert.That(Pdf417ReedSolomon.VerifyCodewords(result.SymbolCodewords, 2), Is.True);
            Assert.That(result.Symbol.Format, Is.EqualTo(BarcodeFormat.Pdf417));
            Assert.That(result.Symbol.Dimension, Is.EqualTo(BarcodeDimension.StackedLinear));
            Assert.That(result.Symbol.WidthModules, Is.EqualTo(103));
            Assert.That(result.Symbol.HeightModules, Is.EqualTo(21));
            Assert.That(result.Symbol.Modules, Has.Length.EqualTo(103 * 21));
            Assert.That(result.Symbol.Codewords, Is.EqualTo(result.SymbolCodewords));
            Assert.That(result.Symbol.Payload?.RawBytes, Is.EqualTo(data));
            Assert.That(result.Symbol.Payload?.Text, Is.EqualTo("PDF417"));
            Assert.That(result.Symbol.SymbologyIdentifier, Is.EqualTo("]L0"));
        });
    }

    [TestCase(0, 2)]
    [TestCase(1, 4)]
    [TestCase(2, 8)]
    [TestCase(3, 16)]
    [TestCase(4, 32)]
    [TestCase(5, 64)]
    [TestCase(6, 128)]
    [TestCase(7, 256)]
    [TestCase(8, 512)]
    public void EveryErrorCorrectionLevelIsAssembledIntoThePlannedGrid(
        int level,
        int expectedErrorCorrectionCount)
    {
        byte[] data = Bytes("A");
        Pdf417Options options = new Pdf417Options();
        options.ErrorCorrectionLevel = level;

        Pdf417EncodingResult result = Pdf417Encoder.Encode(
            data,
            CreatePayload(data, "A"),
            options,
            null);
        int[] expectedParity = Pdf417ReedSolomon.EncodeParity(
            result.DataCodewords,
            level);

        Assert.Multiple(() =>
        {
            Assert.That(result.SymbolPlan.ErrorCorrectionLevel, Is.EqualTo(level));
            Assert.That(result.SymbolPlan.ErrorCorrectionCodewordCount, Is.EqualTo(expectedErrorCorrectionCount));
            Assert.That(result.ErrorCorrectionCodewords, Is.EqualTo(expectedParity));
            Assert.That(result.SymbolCodewords.Length, Is.EqualTo(result.SymbolPlan.Rows * result.SymbolPlan.Columns));
            Assert.That(result.SymbolCodewords.Length, Is.EqualTo(result.DataCodewords.Length + expectedErrorCorrectionCount));
            Assert.That(Pdf417ReedSolomon.VerifyCodewords(result.SymbolCodewords, level), Is.True);
            Assert.That(result.Symbol.Codewords, Is.EqualTo(result.SymbolCodewords));
            Assert.That(result.Symbol.Modules, Has.Length.EqualTo(
                result.Symbol.WidthModules * result.Symbol.HeightModules));
        });
    }

    [Test]
    public void MacroControlBlockFollowsPayloadAndProducesMacroMetadata()
    {
        byte[] data = Bytes("AB");
        StructuredAppendInfo macro = new StructuredAppendInfo();
        macro.Format = BarcodeFormat.MacroPdf417;
        macro.Index = 1;
        macro.Count = 2;
        macro.FileIdentifier = "017053";
        macro.IsLastSegment = true;
        Pdf417Options options = new Pdf417Options();
        options.CompactionMode = Pdf417CompactionMode.Text;
        options.ErrorCorrectionLevel = 1;
        options.Macro = macro;
        options.Sender = "CEN BE";
        options.Addressee = "ISO CH";

        Pdf417EncodingResult result = Pdf417Encoder.Encode(
            data,
            CreatePayload(data, "AB"),
            options,
            null);

        Assert.Multiple(() =>
        {
            Assert.That(result.CompactionPlan.Codewords, Is.EqualTo(new int[] { 1 }));
            Assert.That(result.MacroCodewords, Is.EqualTo(new int[]
            {
                928, 111, 101, 17, 53,
                923, 1, 111, 102,
                923, 3, 64, 416, 34,
                923, 4, 258, 446, 67,
                922
            }));
            Assert.That(result.HighLevelCodewords, Is.EqualTo(
                result.CompactionPlan.Codewords.Concat(result.MacroCodewords)));
            Assert.That(result.SymbolPlan.DataCodewords.Skip(1).Take(result.HighLevelCodewords.Length), Is.EqualTo(result.HighLevelCodewords));
            Assert.That(result.Symbol.Format, Is.EqualTo(BarcodeFormat.MacroPdf417));
            Assert.That(result.Symbol.Payload?.StructuredAppend?.Format, Is.EqualTo(BarcodeFormat.MacroPdf417));
            Assert.That(result.Symbol.Payload?.StructuredAppend?.Index, Is.EqualTo(1));
            Assert.That(result.Symbol.Payload?.StructuredAppend?.Count, Is.EqualTo(2));
            Assert.That(result.Symbol.Payload?.StructuredAppend?.FileIdentifier, Is.EqualTo("017053"));
            Assert.That(result.Symbol.Payload?.StructuredAppend?.IsLastSegment, Is.True);
            Assert.That(Pdf417ReedSolomon.VerifyCodewords(result.SymbolCodewords, 1), Is.True);
        });

        macro.Index = 0;
        macro.FileIdentifier = "changed";
        Assert.Multiple(() =>
        {
            Assert.That(result.Symbol.Payload?.StructuredAppend?.Index, Is.EqualTo(1));
            Assert.That(result.Symbol.Payload?.StructuredAppend?.FileIdentifier, Is.EqualTo("017053"));
        });
    }

    [Test]
    public void CommonMacroEciCharacterEncodingAndCompactLayoutAreResolved()
    {
        byte[] data = new byte[] { 0, 127, 255 };
        StructuredAppendInfo macro = new StructuredAppendInfo();
        macro.Index = 0;
        macro.Count = 1;
        macro.FileIdentifier = "129";
        macro.IsLastSegment = true;
        BarcodeEncodeOptions common = new BarcodeEncodeOptions();
        common.EciAssignmentNumber = 26;
        common.CharacterEncoding = "UTF-8";
        common.StructuredAppend = macro;
        Pdf417Options options = new Pdf417Options();
        options.Variant = Pdf417Variant.Compact;
        options.CompactionMode = Pdf417CompactionMode.Byte;
        options.ErrorCorrectionLevel = 0;

        Pdf417EncodingResult result = Pdf417Encoder.Encode(
            data,
            CreatePayload(data, null),
            options,
            common);

        Assert.Multiple(() =>
        {
            Assert.That(result.CompactionPlan.EciAssignmentNumber, Is.EqualTo(26));
            Assert.That(result.CompactionPlan.Codewords.Take(2), Is.EqualTo(new int[] { 927, 26 }));
            Assert.That(result.CompactionPlan.Codewords.Skip(2), Is.EqualTo(new int[] { 901, 0, 127, 255 }));
            Assert.That(result.SymbolPlan.Variant, Is.EqualTo(Pdf417Variant.Compact));
            Assert.That(result.Symbol.Format, Is.EqualTo(BarcodeFormat.MacroPdf417));
            Assert.That(result.Symbol.WidthModules, Is.EqualTo(result.SymbolPlan.RowWidthModules));
            Assert.That(result.Symbol.Payload?.CharacterEncoding, Is.EqualTo("UTF-8"));
            Assert.That(result.Symbol.Payload?.EciSegments, Has.Length.EqualTo(1));
            Assert.That(result.Symbol.Payload?.EciSegments?[0].AssignmentNumber, Is.EqualTo(26));
            Assert.That(result.Symbol.Payload?.EciSegments?[0].ByteOffset, Is.EqualTo(0));
            Assert.That(result.Symbol.Payload?.EciSegments?[0].ByteCount, Is.EqualTo(3));
            Assert.That(result.Symbol.Payload?.StructuredAppend?.FileIdentifier, Is.EqualTo("129"));
        });
    }

    [Test]
    public void FormatSpecificEciOverridesCommonEciAndResultBuffersAreIndependent()
    {
        byte[] data = Bytes("ABC123");
        BarcodePayload payload = CreatePayload(data, "ABC123");
        BarcodeEncodeOptions common = new BarcodeEncodeOptions();
        common.EciAssignmentNumber = 26;
        Pdf417Options options = new Pdf417Options();
        options.EciAssignmentNumber = 3;
        options.ErrorCorrectionLevel = 0;

        Pdf417EncodingResult result = Pdf417Encoder.Encode(
            data,
            payload,
            options,
            common);
        int symbolCodeword = result.Symbol.Codewords![0];
        int plannedHighLevel = result.SymbolPlan.HighLevelCodewords[0];
        int plannedData = result.SymbolPlan.DataCodewords[0];
        byte symbolByte = result.Symbol.Payload!.RawBytes![0];
        data[0] = 0;
        payload.RawBytes![0] = 0;
        result.HighLevelCodewords[0] = 0;
        result.DataCodewords[0] = 0;
        result.SymbolCodewords[0] = 0;

        Assert.Multiple(() =>
        {
            Assert.That(result.CompactionPlan.EciAssignmentNumber, Is.EqualTo(3));
            Assert.That(result.CompactionPlan.Codewords.Take(2), Is.EqualTo(new int[] { 927, 3 }));
            Assert.That(result.Symbol.Payload?.EciSegments?[0].AssignmentNumber, Is.EqualTo(3));
            Assert.That(result.Symbol.Payload?.RawBytes?[0], Is.EqualTo(symbolByte));
            Assert.That(result.SymbolPlan.HighLevelCodewords[0], Is.EqualTo(plannedHighLevel));
            Assert.That(result.SymbolPlan.DataCodewords[0], Is.EqualTo(plannedData));
            Assert.That(result.Symbol.Codewords?[0], Is.EqualTo(symbolCodeword));
        });
    }

    [Test]
    public void InvalidPayloadOptionsMacroAndCapacityRequestsAreRejected()
    {
        byte[] data = Bytes("A");
        BarcodePayload payload = CreatePayload(data, "A");
        Pdf417Options invalidEci = new Pdf417Options();
        invalidEci.EciAssignmentNumber = -1;
        BarcodeEncodeOptions invalidCommonEci = new BarcodeEncodeOptions();
        invalidCommonEci.EciAssignmentNumber = -1;
        Pdf417Options invalidMode = new Pdf417Options();
        invalidMode.CompactionMode = (Pdf417CompactionMode)99;
        Pdf417Options invalidLevel = new Pdf417Options();
        invalidLevel.ErrorCorrectionLevel = 9;
        Pdf417Options invalidMacro = new Pdf417Options();
        invalidMacro.Macro = new StructuredAppendInfo();
        Pdf417Options orphanMacroField = new Pdf417Options();
        orphanMacroField.FileName = "manifest.txt";
        Pdf417Options excessiveEcc = new Pdf417Options();
        excessiveEcc.CompactionMode = Pdf417CompactionMode.Byte;
        excessiveEcc.ErrorCorrectionLevel = 8;

        Assert.Multiple(() =>
        {
            Assert.That(
                () => Pdf417Encoder.Encode(null!, payload, null, null),
                Throws.TypeOf<BarcodeEncodingException>());
            Assert.That(
                () => Pdf417Encoder.Encode(data, null!, null, null),
                Throws.TypeOf<BarcodeEncodingException>());
            Assert.That(
                () => Pdf417Encoder.Encode(new byte[0], payload, null, null),
                Throws.TypeOf<BarcodeEncodingException>());
            Assert.That(
                () => Pdf417Encoder.Encode(data, payload, invalidEci, null),
                Throws.TypeOf<BarcodeEncodingException>());
            Assert.That(
                () => Pdf417Encoder.Encode(data, payload, null, invalidCommonEci),
                Throws.TypeOf<BarcodeEncodingException>());
            Assert.That(
                () => Pdf417Encoder.Encode(data, payload, invalidMode, null),
                Throws.TypeOf<BarcodeEncodingException>());
            Assert.That(
                () => Pdf417Encoder.Encode(data, payload, invalidLevel, null),
                Throws.TypeOf<BarcodeEncodingException>());
            Assert.That(
                () => Pdf417Encoder.Encode(data, payload, invalidMacro, null),
                Throws.TypeOf<BarcodeEncodingException>());
            Assert.That(
                () => Pdf417Encoder.Encode(data, payload, orphanMacroField, null),
                Throws.TypeOf<BarcodeEncodingException>());
            Assert.That(
                () => Pdf417Encoder.Encode(new byte[600], payload, excessiveEcc, null),
                Throws.TypeOf<BarcodeEncodingException>());
        });
    }

    static BarcodePayload CreatePayload(byte[] data, string? text)
    {
        BarcodePayload payload = new BarcodePayload();
        payload.Text = text;
        payload.RawBytes = CopyBytes(data);
        return payload;
    }

    static byte[] Bytes(string text)
    {
        return global::System.Text.Encoding.Latin1.GetBytes(text);
    }

    static byte[] CopyBytes(byte[] source)
    {
        byte[] copy = new byte[source.Length];
        global::System.Array.Copy(source, copy, source.Length);
        return copy;
    }
}
