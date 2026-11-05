namespace barcode.Tests;

public class DataMatrixControlCodewordEncoderTests
{
    [Test]
    public void ControlConstantsAndSingleCodewordsMatchTheDataMatrixTable()
    {
        Assert.Multiple(() =>
        {
            Assert.That(DataMatrixHighLevelEncoder.Fnc1Codeword, Is.EqualTo(232));
            Assert.That(DataMatrixHighLevelEncoder.StructuredAppendCodeword, Is.EqualTo(233));
            Assert.That(DataMatrixHighLevelEncoder.ReaderProgrammingCodeword, Is.EqualTo(234));
            Assert.That(DataMatrixHighLevelEncoder.Macro05Codeword, Is.EqualTo(236));
            Assert.That(DataMatrixHighLevelEncoder.Macro06Codeword, Is.EqualTo(237));
            Assert.That(DataMatrixHighLevelEncoder.EciCodeword, Is.EqualTo(241));
            Assert.That(DataMatrixControlCodewordEncoder.EncodeFnc1(), Is.EqualTo(new int[] { 232 }));
            Assert.That(DataMatrixControlCodewordEncoder.EncodeReaderProgramming(), Is.EqualTo(new int[] { 234 }));
            Assert.That(DataMatrixControlCodewordEncoder.EncodeMacro(5), Is.EqualTo(new int[] { 236 }));
            Assert.That(DataMatrixControlCodewordEncoder.EncodeMacro(6), Is.EqualTo(new int[] { 237 }));
        });
    }

    [Test]
    public void EciAssignmentsUseEveryStandardLengthBoundary()
    {
        Assert.Multiple(() =>
        {
            Assert.That(
                DataMatrixControlCodewordEncoder.EncodeEciAssignment(0),
                Is.EqualTo(new int[] { 241, 1 }));
            Assert.That(
                DataMatrixControlCodewordEncoder.EncodeEciAssignment(126),
                Is.EqualTo(new int[] { 241, 127 }));
            Assert.That(
                DataMatrixControlCodewordEncoder.EncodeEciAssignment(127),
                Is.EqualTo(new int[] { 241, 128, 1 }));
            Assert.That(
                DataMatrixControlCodewordEncoder.EncodeEciAssignment(16382),
                Is.EqualTo(new int[] { 241, 191, 254 }));
            Assert.That(
                DataMatrixControlCodewordEncoder.EncodeEciAssignment(16383),
                Is.EqualTo(new int[] { 241, 192, 1, 1 }));
            Assert.That(
                DataMatrixControlCodewordEncoder.EncodeEciAssignment(999999),
                Is.EqualTo(new int[] { 241, 207, 63, 129 }));
        });
    }

    [Test]
    public void StructuredAppendPacksSequenceAndFileIdentifiers()
    {
        StructuredAppendInfo first = CreateAppend(0, 2, "001002");
        StructuredAppendInfo last = CreateAppend(15, 16, "64516");

        Assert.Multiple(() =>
        {
            Assert.That(
                DataMatrixControlCodewordEncoder.EncodeStructuredAppend(first),
                Is.EqualTo(new int[] { 233, 15, 1, 2 }));
            Assert.That(
                DataMatrixControlCodewordEncoder.EncodeStructuredAppend(last),
                Is.EqualTo(new int[] { 233, 241, 254, 254 }));
            Assert.That(
                DataMatrixControlCodewordEncoder.EncodeStructuredAppendFileIdentifier("254254"),
                Is.EqualTo(new int[] { 254, 254 }));
            Assert.That(
                DataMatrixControlCodewordEncoder.EncodeStructuredAppendFileIdentifier(null),
                Is.EqualTo(new int[] { 1, 1 }));
        });
    }

    [Test]
    public void PrefixOrdersStructuredAppendFnc1AndEciBeforeData()
    {
        DataMatrixOptions options = new DataMatrixOptions();
        options.EncodingMode = DataMatrixEncodingMode.Automatic;
        options.EciAssignmentNumber = 26;
        options.StructuredAppend = CreateAppend(1, 3, "64516");
        byte[] data = global::System.Text.Encoding.ASCII.GetBytes("123456");

        DataMatrixMessagePlan plan = DataMatrixControlCodewordEncoder.CreateMessagePlan(
            data,
            options,
            true);

        Assert.Multiple(() =>
        {
            Assert.That(plan.ControlCodewords, Is.EqualTo(new int[] { 233, 30, 254, 254, 232, 241, 27 }));
            Assert.That(plan.DataPlan.Codewords, Is.EqualTo(new int[] { 142, 164, 186 }));
            Assert.That(plan.Codewords, Is.EqualTo(new int[] { 233, 30, 254, 254, 232, 241, 27, 142, 164, 186 }));
            Assert.That(plan.DataPlan.RequestedMode, Is.EqualTo(DataMatrixEncodingMode.Automatic));
        });
    }

    [Test]
    public void ControlPrefixOffsetsBase256RandomizationPositions()
    {
        byte[] data = new byte[] { 171, 228, 246, 252, 233, 187 };
        DataMatrixOptions options = new DataMatrixOptions();
        options.EncodingMode = DataMatrixEncodingMode.Base256;
        options.EciAssignmentNumber = 26;

        DataMatrixMessagePlan plan = DataMatrixControlCodewordEncoder.CreateMessagePlan(
            data,
            options,
            false);
        int[] expectedDataCodewords = DataMatrixModeCodec.EncodeBase256(data, 3);

        Assert.Multiple(() =>
        {
            Assert.That(plan.ControlCodewords, Is.EqualTo(new int[] { 241, 27 }));
            Assert.That(plan.DataPlan.Codewords, Is.EqualTo(expectedDataCodewords));
            Assert.That(plan.Codewords[2], Is.EqualTo(231));
            Assert.That(plan.Codewords, Is.EqualTo(Join(new int[] { 241, 27 }, expectedDataCodewords)));
            Assert.That(
                DataMatrixModeCodec.DecodeBase256(plan.DataPlan.Codewords, 3),
                Is.EqualTo(data));
        });
    }

    [Test]
    public void ControlPrefixAndPaddingShareAbsoluteCodewordPositions()
    {
        DataMatrixOptions options = new DataMatrixOptions();
        options.ReaderProgramming = true;

        DataMatrixMessagePlan plan = DataMatrixControlCodewordEncoder.CreateMessagePlan(
            new byte[] { (byte)'A' },
            options,
            false);
        int[] padded = DataMatrixHighLevelEncoder.PadCodewords(plan.Codewords, 6);
        DataMatrixHighLevelDecodeResult decoded = DataMatrixHighLevelDecoder.Decode(padded);

        Assert.Multiple(() =>
        {
            Assert.That(plan.Codewords, Is.EqualTo(new int[] { 234, 66 }));
            Assert.That(padded, Is.EqualTo(new int[] { 234, 66, 129, 220, 115, 11 }));
            Assert.That(decoded.RawBytes, Is.EqualTo(new byte[] { (byte)'A' }));
            Assert.That(decoded.ReaderProgramming, Is.True);
        });
    }

    [Test]
    public void MacroAndReaderProgrammingPrefixesRemainFirst()
    {
        byte[] data = new byte[] { (byte)'A' };
        DataMatrixOptions macro05 = new DataMatrixOptions();
        macro05.MacroType = 5;
        macro05.EciAssignmentNumber = 26;
        DataMatrixOptions macro06 = new DataMatrixOptions();
        macro06.MacroType = 6;
        DataMatrixOptions reader = new DataMatrixOptions();
        reader.ReaderProgramming = true;
        reader.EciAssignmentNumber = 26;

        DataMatrixMessagePlan macro05Plan = DataMatrixControlCodewordEncoder.CreateMessagePlan(data, macro05, false);
        DataMatrixMessagePlan macro06Plan = DataMatrixControlCodewordEncoder.CreateMessagePlan(data, macro06, false);
        DataMatrixMessagePlan readerPlan = DataMatrixControlCodewordEncoder.CreateMessagePlan(data, reader, false);

        Assert.Multiple(() =>
        {
            Assert.That(macro05Plan.ControlCodewords, Is.EqualTo(new int[] { 236, 241, 27 }));
            Assert.That(macro06Plan.ControlCodewords, Is.EqualTo(new int[] { 237 }));
            Assert.That(readerPlan.ControlCodewords, Is.EqualTo(new int[] { 234, 241, 27 }));
            Assert.That(macro05Plan.Codewords[macro05Plan.Codewords.Length - 1], Is.EqualTo(66));
            Assert.That(macro06Plan.Codewords[macro06Plan.Codewords.Length - 1], Is.EqualTo(66));
            Assert.That(readerPlan.Codewords[readerPlan.Codewords.Length - 1], Is.EqualTo(66));
        });
    }

    [Test]
    public void InvalidControlsAndIncompatibleFirstPositionFeaturesAreRejected()
    {
        DataMatrixOptions invalidEci = new DataMatrixOptions();
        invalidEci.EciAssignmentNumber = -1;
        DataMatrixOptions invalidMacro = new DataMatrixOptions();
        invalidMacro.MacroType = 4;
        DataMatrixOptions reader = new DataMatrixOptions();
        reader.ReaderProgramming = true;
        DataMatrixOptions readerAppend = new DataMatrixOptions();
        readerAppend.ReaderProgramming = true;
        readerAppend.StructuredAppend = CreateAppend(0, 2, null);
        DataMatrixOptions readerMacro = new DataMatrixOptions();
        readerMacro.ReaderProgramming = true;
        readerMacro.MacroType = 5;
        DataMatrixOptions macroGs1 = new DataMatrixOptions();
        macroGs1.MacroType = 6;
        DataMatrixOptions macroAppend = new DataMatrixOptions();
        macroAppend.MacroType = 5;
        macroAppend.StructuredAppend = CreateAppend(0, 2, null);

        Assert.Multiple(() =>
        {
            Assert.That(
                () => DataMatrixControlCodewordEncoder.CreateMessagePlan(new byte[] { 65 }, invalidEci, false),
                Throws.TypeOf<BarcodeEncodingException>());
            Assert.That(
                () => DataMatrixControlCodewordEncoder.EncodeEciAssignment(1000000),
                Throws.TypeOf<BarcodeEncodingException>());
            Assert.That(
                () => DataMatrixControlCodewordEncoder.CreateMessagePlan(new byte[] { 65 }, invalidMacro, false),
                Throws.TypeOf<BarcodeEncodingException>());
            Assert.That(
                () => DataMatrixControlCodewordEncoder.CreateMessagePlan(new byte[] { 65 }, reader, true),
                Throws.TypeOf<BarcodeEncodingException>());
            Assert.That(
                () => DataMatrixControlCodewordEncoder.CreateMessagePlan(new byte[] { 65 }, readerAppend, false),
                Throws.TypeOf<BarcodeEncodingException>());
            Assert.That(
                () => DataMatrixControlCodewordEncoder.CreateMessagePlan(new byte[] { 65 }, readerMacro, false),
                Throws.TypeOf<BarcodeEncodingException>());
            Assert.That(
                () => DataMatrixControlCodewordEncoder.CreateMessagePlan(new byte[] { 65 }, macroGs1, true),
                Throws.TypeOf<BarcodeEncodingException>());
            Assert.That(
                () => DataMatrixControlCodewordEncoder.CreateMessagePlan(new byte[] { 65 }, macroAppend, false),
                Throws.TypeOf<BarcodeEncodingException>());
        });
    }

    [Test]
    public void InvalidStructuredAppendMetadataIsRejected()
    {
        StructuredAppendInfo tooFew = CreateAppend(0, 1, null);
        StructuredAppendInfo tooMany = CreateAppend(0, 17, null);
        StructuredAppendInfo negativeIndex = CreateAppend(-1, 2, null);
        StructuredAppendInfo highIndex = CreateAppend(2, 2, null);
        StructuredAppendInfo wrongFormat = CreateAppend(0, 2, null);
        wrongFormat.Format = BarcodeFormat.Pdf417;

        Assert.Multiple(() =>
        {
            Assert.That(
                () => DataMatrixControlCodewordEncoder.EncodeStructuredAppend(tooFew),
                Throws.TypeOf<BarcodeEncodingException>());
            Assert.That(
                () => DataMatrixControlCodewordEncoder.EncodeStructuredAppend(tooMany),
                Throws.TypeOf<BarcodeEncodingException>());
            Assert.That(
                () => DataMatrixControlCodewordEncoder.EncodeStructuredAppend(negativeIndex),
                Throws.TypeOf<BarcodeEncodingException>());
            Assert.That(
                () => DataMatrixControlCodewordEncoder.EncodeStructuredAppend(highIndex),
                Throws.TypeOf<BarcodeEncodingException>());
            Assert.That(
                () => DataMatrixControlCodewordEncoder.EncodeStructuredAppend(wrongFormat),
                Throws.TypeOf<BarcodeEncodingException>());
            Assert.That(
                () => DataMatrixControlCodewordEncoder.EncodeStructuredAppendFileIdentifier("ABC"),
                Throws.TypeOf<BarcodeEncodingException>());
            Assert.That(
                () => DataMatrixControlCodewordEncoder.EncodeStructuredAppendFileIdentifier("0"),
                Throws.TypeOf<BarcodeEncodingException>());
            Assert.That(
                () => DataMatrixControlCodewordEncoder.EncodeStructuredAppendFileIdentifier("64517"),
                Throws.TypeOf<BarcodeEncodingException>());
            Assert.That(
                () => DataMatrixControlCodewordEncoder.EncodeStructuredAppendFileIdentifier("000001"),
                Throws.TypeOf<BarcodeEncodingException>());
            Assert.That(
                () => DataMatrixControlCodewordEncoder.EncodeStructuredAppendFileIdentifier("1234567"),
                Throws.TypeOf<BarcodeEncodingException>());
        });
    }

    [Test]
    public void ControlsCountTowardCapacityAndMessagePlanOwnsItsData()
    {
        DataMatrixOptions eci = new DataMatrixOptions();
        eci.EciAssignmentNumber = 26;
        byte[] input = new byte[] { (byte)'A' };
        DataMatrixMessagePlan plan = DataMatrixControlCodewordEncoder.CreateMessagePlan(input, eci, false);
        input[0] = (byte)'Z';

        Assert.Multiple(() =>
        {
            Assert.That(plan.DataPlan.Data, Is.EqualTo(new byte[] { (byte)'A' }));
            Assert.That(
                () => DataMatrixControlCodewordEncoder.CreateMessagePlan(CreateRepeatedByte(3116, (byte)'1'), eci, false),
                Throws.TypeOf<BarcodeEncodingException>());
            Assert.That(
                () => DataMatrixControlCodewordEncoder.CreateMessagePlan(null!, null, false),
                Throws.TypeOf<BarcodeEncodingException>());
            Assert.That(
                () => DataMatrixControlCodewordEncoder.CreateMessagePlan(new byte[0], null, false),
                Throws.TypeOf<BarcodeEncodingException>());
            Assert.That(
                () => DataMatrixMinimalEncoder.PlanAtCodewordPosition(new byte[] { 65 }, DataMatrixEncodingMode.Ascii, 0),
                Throws.TypeOf<BarcodeValidationException>());
        });
    }

    static StructuredAppendInfo CreateAppend(
        int index,
        int count,
        string? fileIdentifier)
    {
        StructuredAppendInfo append = new StructuredAppendInfo();
        append.Format = BarcodeFormat.DataMatrix;
        append.Index = index;
        append.Count = count;
        append.FileIdentifier = fileIdentifier;
        append.IsLastSegment = index == count - 1;
        return append;
    }

    static int[] Join(int[] first, int[] second)
    {
        int[] joined = new int[first.Length + second.Length];
        global::System.Array.Copy(first, 0, joined, 0, first.Length);
        global::System.Array.Copy(second, 0, joined, first.Length, second.Length);
        return joined;
    }

    static byte[] CreateRepeatedByte(int count, byte value)
    {
        byte[] data = new byte[count];
        int index = 0;
        while (index < data.Length)
        {
            data[index] = value;
            index++;
        }

        return data;
    }
}
