using NUnit.Framework;

namespace gif.Tests;

[TestFixture]
public class GifLzwTests
{
    [Test]
    public void BitReader_ReadsCodesLeastSignificantBitFirstWithinByte()
    {
        GifLzwBitReader reader = new GifLzwBitReader(new byte[] { 0xAC });

        GifLzwCodeReadResult first = reader.ReadCode(3);
        GifLzwCodeReadResult second = reader.ReadCode(3);

        Assert.That(first.Success, Is.True);
        Assert.That(first.Code, Is.EqualTo(4));
        Assert.That(first.CodeSize, Is.EqualTo(3));
        Assert.That(second.Success, Is.True);
        Assert.That(second.Code, Is.EqualTo(5));
        Assert.That(reader.ByteOffset, Is.EqualTo(0));
        Assert.That(reader.BitOffset, Is.EqualTo(6));
        Assert.That(reader.TotalBitsRead, Is.EqualTo(6));
        Assert.That(reader.RemainingBits(), Is.EqualTo(2));
    }

    [Test]
    public void BitReader_ReadsCodeAcrossByteBoundary()
    {
        GifLzwBitReader reader = new GifLzwBitReader(new byte[] { 0xFF, 0x01 });

        GifLzwCodeReadResult result = reader.ReadCode(9);

        Assert.That(result.Success, Is.True);
        Assert.That(result.Code, Is.EqualTo(511));
        Assert.That(reader.ByteOffset, Is.EqualTo(1));
        Assert.That(reader.BitOffset, Is.EqualTo(1));
        Assert.That(reader.TotalBitsRead, Is.EqualTo(9));
    }

    [Test]
    public void BitReader_ReadsMaximumTwelveBitCode()
    {
        GifLzwBitReader reader = new GifLzwBitReader(new byte[] { 0x34, 0x12 });

        GifLzwCodeReadResult result = reader.ReadCode(12);

        Assert.That(result.Success, Is.True);
        Assert.That(result.Code, Is.EqualTo(0x234));
        Assert.That(reader.ByteOffset, Is.EqualTo(1));
        Assert.That(reader.BitOffset, Is.EqualTo(4));
        Assert.That(reader.RemainingBits(), Is.EqualTo(4));
    }

    [Test]
    public void BitReader_SupportsSequentialVariableCodeSizes()
    {
        GifLzwBitReader reader = new GifLzwBitReader(new byte[] { 0x8C, 0x2D });

        GifLzwCodeReadResult first = reader.ReadCode(3);
        GifLzwCodeReadResult second = reader.ReadCode(4);
        GifLzwCodeReadResult third = reader.ReadCode(5);

        Assert.That(first.Success, Is.True);
        Assert.That(second.Success, Is.True);
        Assert.That(third.Success, Is.True);
        Assert.That(first.Code, Is.EqualTo(4));
        Assert.That(second.Code, Is.EqualTo(1));
        Assert.That(third.Code, Is.EqualTo(27));
        Assert.That(reader.ByteOffset, Is.EqualTo(1));
        Assert.That(reader.BitOffset, Is.EqualTo(4));
    }

    [Test]
    public void BitReader_RejectsInvalidCodeSizes()
    {
        GifLzwBitReader reader = new GifLzwBitReader(new byte[] { 0 });

        GifLzwCodeReadResult zero = reader.ReadCode(0);
        GifLzwCodeReadResult tooLarge = reader.ReadCode(13);

        Assert.That(zero.Success, Is.False);
        Assert.That(zero.Error.Code, Is.EqualTo(GifErrorCode.InvalidLzwCodeStream));
        Assert.That(tooLarge.Success, Is.False);
        Assert.That(tooLarge.Error.Code, Is.EqualTo(GifErrorCode.InvalidLzwCodeStream));
        Assert.That(reader.ByteOffset, Is.EqualTo(0));
        Assert.That(reader.BitOffset, Is.EqualTo(0));
    }

    [Test]
    public void BitReader_ReportsEndOfDataWithoutAdvancingPartialCode()
    {
        GifLzwBitReader reader = new GifLzwBitReader(new byte[] { 0x03 });

        GifLzwCodeReadResult result = reader.ReadCode(9);

        Assert.That(result.Success, Is.False);
        Assert.That(result.EndOfData, Is.True);
        Assert.That(result.Error.Code, Is.EqualTo(GifErrorCode.InvalidLzwCodeStream));
        Assert.That(result.Error.Position.BlockName, Is.EqualTo("LzwBitReader"));
        Assert.That(reader.ByteOffset, Is.EqualTo(0));
        Assert.That(reader.BitOffset, Is.EqualTo(0));
        Assert.That(reader.TotalBitsRead, Is.EqualTo(0));
    }

    [Test]
    public void BitReader_NullDataBehavesAsEmptyStream()
    {
        GifLzwBitReader reader = new GifLzwBitReader(null!);

        GifLzwCodeReadResult result = reader.ReadCode(1);

        Assert.That(result.Success, Is.False);
        Assert.That(result.EndOfData, Is.True);
        Assert.That(reader.RemainingBits(), Is.EqualTo(0));
    }

    [Test]
    public void BitWriter_WritesCodesLeastSignificantBitFirstWithinByte()
    {
        GifLzwBitWriter writer = new GifLzwBitWriter(16);

        GifLzwCodeWriteResult first = writer.WriteCode(4, 3);
        GifLzwCodeWriteResult second = writer.WriteCode(5, 3);

        Assert.That(first.Success, Is.True);
        Assert.That(second.Success, Is.True);
        Assert.That(writer.ToArray(), Is.EqualTo(new byte[] { 0x2C }));
        Assert.That(writer.ByteOffset, Is.EqualTo(0));
        Assert.That(writer.BitOffset, Is.EqualTo(6));
        Assert.That(writer.TotalBitsWritten, Is.EqualTo(6));
        Assert.That(second.BytesWritten, Is.EqualTo(1));
    }

    [Test]
    public void BitWriter_WritesCodeAcrossByteBoundary()
    {
        GifLzwBitWriter writer = new GifLzwBitWriter(16);

        GifLzwCodeWriteResult result = writer.WriteCode(511, 9);

        Assert.That(result.Success, Is.True);
        Assert.That(writer.ToArray(), Is.EqualTo(new byte[] { 0xFF, 0x01 }));
        Assert.That(writer.ByteOffset, Is.EqualTo(1));
        Assert.That(writer.BitOffset, Is.EqualTo(1));
        Assert.That(writer.TotalBitsWritten, Is.EqualTo(9));
    }

    [Test]
    public void BitWriter_WritesMaximumTwelveBitCode()
    {
        GifLzwBitWriter writer = new GifLzwBitWriter(16);

        GifLzwCodeWriteResult result = writer.WriteCode(0x234, 12);

        Assert.That(result.Success, Is.True);
        Assert.That(writer.ToArray(), Is.EqualTo(new byte[] { 0x34, 0x02 }));
        Assert.That(writer.ByteOffset, Is.EqualTo(1));
        Assert.That(writer.BitOffset, Is.EqualTo(4));
        Assert.That(result.BytesWritten, Is.EqualTo(2));
    }

    [Test]
    public void BitWriter_RoundTripsSequentialVariableCodeSizesThroughReader()
    {
        GifLzwBitWriter writer = new GifLzwBitWriter(16);
        writer.WriteCode(4, 3);
        writer.WriteCode(1, 4);
        writer.WriteCode(27, 5);

        byte[] data = writer.ToArray();
        GifLzwBitReader reader = new GifLzwBitReader(data);
        GifLzwCodeReadResult first = reader.ReadCode(3);
        GifLzwCodeReadResult second = reader.ReadCode(4);
        GifLzwCodeReadResult third = reader.ReadCode(5);

        Assert.That(data, Is.EqualTo(new byte[] { 0x8C, 0x0D }));
        Assert.That(first.Code, Is.EqualTo(4));
        Assert.That(second.Code, Is.EqualTo(1));
        Assert.That(third.Code, Is.EqualTo(27));
        Assert.That(reader.TotalBitsRead, Is.EqualTo(12));
    }

    [Test]
    public void BitWriter_RejectsInvalidCodeSizesAndValuesWithoutAdvancing()
    {
        GifLzwBitWriter writer = new GifLzwBitWriter(16);

        GifLzwCodeWriteResult zeroSize = writer.WriteCode(0, 0);
        GifLzwCodeWriteResult tooLargeSize = writer.WriteCode(0, 13);
        GifLzwCodeWriteResult negativeCode = writer.WriteCode(-1, 3);
        GifLzwCodeWriteResult tooLargeCode = writer.WriteCode(8, 3);

        Assert.That(zeroSize.Success, Is.False);
        Assert.That(zeroSize.Error.Code, Is.EqualTo(GifErrorCode.InvalidLzwCodeStream));
        Assert.That(tooLargeSize.Success, Is.False);
        Assert.That(negativeCode.Success, Is.False);
        Assert.That(tooLargeCode.Success, Is.False);
        Assert.That(writer.ByteOffset, Is.EqualTo(0));
        Assert.That(writer.BitOffset, Is.EqualTo(0));
        Assert.That(writer.TotalBitsWritten, Is.EqualTo(0));
        Assert.That(writer.ToArray(), Is.Empty);
    }

    [Test]
    public void BitWriter_GrowsBufferForAdditionalCodes()
    {
        GifLzwBitWriter writer = new GifLzwBitWriter(1);

        writer.WriteCode(0x234, 12);
        writer.WriteCode(0x123, 12);

        Assert.That(writer.Buffer.Length, Is.GreaterThanOrEqualTo(3));
        Assert.That(writer.ToArray(), Is.EqualTo(new byte[] { 0x34, 0x32, 0x12 }));
        Assert.That(writer.TotalBitsWritten, Is.EqualTo(24));
        Assert.That(writer.ByteOffset, Is.EqualTo(3));
        Assert.That(writer.BitOffset, Is.EqualTo(0));
    }

    [Test]
    public void BitWriter_ClearResetsStateAndOutput()
    {
        GifLzwBitWriter writer = new GifLzwBitWriter(16);
        writer.WriteCode(511, 9);

        writer.Clear();

        Assert.That(writer.ToArray(), Is.Empty);
        Assert.That(writer.ByteOffset, Is.EqualTo(0));
        Assert.That(writer.BitOffset, Is.EqualTo(0));
        Assert.That(writer.TotalBitsWritten, Is.EqualTo(0));
    }

    [Test]
    public void EncoderDictionary_InitializeCreatesRootLookupAndControlCodes()
    {
        GifLzwEncoderDictionary dictionary = new GifLzwEncoderDictionary();

        GifLzwDictionaryResult result = dictionary.Initialize(2);

        Assert.That(result.Success, Is.True);
        Assert.That(dictionary.RootCodeCount, Is.EqualTo(4));
        Assert.That(dictionary.ClearCode, Is.EqualTo(4));
        Assert.That(dictionary.EndCode, Is.EqualTo(5));
        Assert.That(dictionary.NextCode, Is.EqualTo(6));
        Assert.That(dictionary.FindRoot(0), Is.EqualTo(0));
        Assert.That(dictionary.FindRoot(3), Is.EqualTo(3));
        Assert.That(dictionary.FindRoot(4), Is.EqualTo(-1));
        Assert.That(dictionary.ContainsDataCode(3), Is.True);
        Assert.That(dictionary.ContainsDataCode(dictionary.ClearCode), Is.False);
        Assert.That(dictionary.ContainsDataCode(dictionary.EndCode), Is.False);
    }

    [Test]
    public void EncoderDictionary_InitializeSupportsEightBitRoots()
    {
        GifLzwEncoderDictionary dictionary = new GifLzwEncoderDictionary();

        GifLzwDictionaryResult result = dictionary.Initialize(8);

        Assert.That(result.Success, Is.True);
        Assert.That(dictionary.RootCodeCount, Is.EqualTo(256));
        Assert.That(dictionary.ClearCode, Is.EqualTo(256));
        Assert.That(dictionary.EndCode, Is.EqualTo(257));
        Assert.That(dictionary.NextCode, Is.EqualTo(258));
        Assert.That(dictionary.FindRoot(255), Is.EqualTo(255));
    }

    [Test]
    public void EncoderDictionary_AddStoresAndFindsPrefixSuffixEntries()
    {
        GifLzwEncoderDictionary dictionary = new GifLzwEncoderDictionary();
        dictionary.Initialize(2);

        GifLzwDictionaryResult first = dictionary.Add(2, 9);
        GifLzwDictionaryResult second = dictionary.Add(first.Code, 8);

        Assert.That(first.Success, Is.True);
        Assert.That(first.Code, Is.EqualTo(6));
        Assert.That(dictionary.Find(2, 9), Is.EqualTo(6));
        Assert.That(dictionary.Contains(2, 9), Is.True);
        Assert.That(dictionary.PrefixCodes[6], Is.EqualTo(2));
        Assert.That(dictionary.SuffixBytes[6], Is.EqualTo((byte)9));
        Assert.That(second.Success, Is.True);
        Assert.That(second.Code, Is.EqualTo(7));
        Assert.That(dictionary.Find(6, 8), Is.EqualTo(7));
        Assert.That(dictionary.NextCode, Is.EqualTo(8));
    }

    [Test]
    public void EncoderDictionary_AddReturnsExistingCodeForDuplicateEntry()
    {
        GifLzwEncoderDictionary dictionary = new GifLzwEncoderDictionary();
        dictionary.Initialize(2);
        GifLzwDictionaryResult first = dictionary.Add(1, 2);

        GifLzwDictionaryResult duplicate = dictionary.Add(1, 2);

        Assert.That(first.Success, Is.True);
        Assert.That(duplicate.Success, Is.True);
        Assert.That(duplicate.Code, Is.EqualTo(first.Code));
        Assert.That(dictionary.NextCode, Is.EqualTo(7));
    }

    [Test]
    public void EncoderDictionary_AddRejectsControlOrUnknownPrefixCodes()
    {
        GifLzwEncoderDictionary dictionary = new GifLzwEncoderDictionary();
        dictionary.Initialize(2);

        GifLzwDictionaryResult clearPrefix = dictionary.Add(dictionary.ClearCode, 1);
        GifLzwDictionaryResult unknownPrefix = dictionary.Add(99, 1);

        Assert.That(clearPrefix.Success, Is.False);
        Assert.That(clearPrefix.Error.Code, Is.EqualTo(GifErrorCode.InvalidLzwCodeStream));
        Assert.That(unknownPrefix.Success, Is.False);
        Assert.That(unknownPrefix.Error.Code, Is.EqualTo(GifErrorCode.InvalidLzwCodeStream));
        Assert.That(dictionary.NextCode, Is.EqualTo(6));
    }

    [Test]
    public void EncoderDictionary_AddRejectsFullTable()
    {
        GifLzwEncoderDictionary dictionary = new GifLzwEncoderDictionary();
        dictionary.Initialize(2);
        dictionary.NextCode = GifConstants.MaximumLzwTableSize;

        GifLzwDictionaryResult result = dictionary.Add(0, 1);

        Assert.That(result.Success, Is.False);
        Assert.That(result.Error.Code, Is.EqualTo(GifErrorCode.MemoryLimitExceeded));
        Assert.That(dictionary.IsFull(), Is.True);
    }

    [Test]
    public void EncoderDictionary_RejectsInvalidMinimumCodeSizes()
    {
        GifLzwEncoderDictionary dictionary = new GifLzwEncoderDictionary();

        GifLzwDictionaryResult tooSmall = dictionary.Initialize(1);
        GifLzwDictionaryResult tooLarge = dictionary.Initialize(9);

        Assert.That(tooSmall.Success, Is.False);
        Assert.That(tooSmall.Error.Code, Is.EqualTo(GifErrorCode.InvalidLzwMinimumCodeSize));
        Assert.That(tooLarge.Success, Is.False);
        Assert.That(tooLarge.Error.Code, Is.EqualTo(GifErrorCode.InvalidLzwMinimumCodeSize));
        Assert.That(dictionary.NextCode, Is.EqualTo(0));
    }

    [Test]
    public void EncoderDictionary_InitializeClearsPreviousLookupEntries()
    {
        GifLzwEncoderDictionary dictionary = new GifLzwEncoderDictionary();
        dictionary.Initialize(2);
        dictionary.Add(0, 1);

        GifLzwDictionaryResult result = dictionary.Initialize(2);

        Assert.That(result.Success, Is.True);
        Assert.That(dictionary.Find(0, 1), Is.EqualTo(-1));
        Assert.That(dictionary.ContainsCode(6), Is.False);
        Assert.That(dictionary.PrefixCodes[6], Is.EqualTo(-1));
    }

    [Test]
    public void Encoder_InitializeSetsDictionaryWriterAndInitialCodeSize()
    {
        GifLzwEncoder encoder = new GifLzwEncoder();

        GifLzwCodeWriteResult result = encoder.Initialize(2);

        Assert.That(result.Success, Is.True);
        Assert.That(result.CodeSize, Is.EqualTo(3));
        Assert.That(encoder.MinimumCodeSize, Is.EqualTo(2));
        Assert.That(encoder.CurrentCodeSize, Is.EqualTo(3));
        Assert.That(encoder.Dictionary.ClearCode, Is.EqualTo(4));
        Assert.That(encoder.Dictionary.EndCode, Is.EqualTo(5));
        Assert.That(encoder.HasWrittenClearCode, Is.False);
        Assert.That(encoder.HasWrittenEndCode, Is.False);
        Assert.That(encoder.ToArray(), Is.Empty);
    }

    [Test]
    public void Encoder_WriteClearAndEndCodesUseGifBitPacking()
    {
        GifLzwEncoder encoder = new GifLzwEncoder();
        encoder.Initialize(2);

        GifLzwCodeWriteResult clear = encoder.WriteClearCode();
        GifLzwCodeWriteResult end = encoder.WriteEndCode();
        GifLzwBitReader reader = new GifLzwBitReader(encoder.ToArray());

        Assert.That(clear.Success, Is.True);
        Assert.That(end.Success, Is.True);
        Assert.That(encoder.ToArray(), Is.EqualTo(new byte[] { 0x2C }));
        Assert.That(reader.ReadCode(3).Code, Is.EqualTo(4));
        Assert.That(reader.ReadCode(3).Code, Is.EqualTo(5));
        Assert.That(encoder.HasWrittenClearCode, Is.True);
        Assert.That(encoder.HasWrittenEndCode, Is.True);
    }

    [Test]
    public void Encoder_WriteEndCodeRequiresClearCode()
    {
        GifLzwEncoder encoder = new GifLzwEncoder();
        encoder.Initialize(2);

        GifLzwCodeWriteResult result = encoder.WriteEndCode();

        Assert.That(result.Success, Is.False);
        Assert.That(result.Error.Code, Is.EqualTo(GifErrorCode.InvalidLzwCodeStream));
        Assert.That(encoder.ToArray(), Is.Empty);
        Assert.That(encoder.HasWrittenEndCode, Is.False);
    }

    [Test]
    public void Encoder_WriteClearCodeResetsDictionaryAndCodeSizeAfterGrowth()
    {
        GifLzwEncoder encoder = new GifLzwEncoder();
        encoder.Initialize(2);
        encoder.CurrentCodeSize = 4;
        encoder.Dictionary.Add(0, 1);

        GifLzwCodeWriteResult result = encoder.WriteClearCode();
        GifLzwBitReader reader = new GifLzwBitReader(encoder.ToArray());

        Assert.That(result.Success, Is.True);
        Assert.That(reader.ReadCode(4).Code, Is.EqualTo(4));
        Assert.That(encoder.CurrentCodeSize, Is.EqualTo(3));
        Assert.That(encoder.Dictionary.NextCode, Is.EqualTo(6));
        Assert.That(encoder.Dictionary.ContainsCode(6), Is.False);
        Assert.That(encoder.HasWrittenClearCode, Is.True);
    }

    [Test]
    public void Encoder_RejectsWritesBeforeInitialization()
    {
        GifLzwEncoder encoder = new GifLzwEncoder();

        GifLzwCodeWriteResult clear = encoder.WriteClearCode();
        GifLzwCodeWriteResult end = encoder.WriteEndCode();

        Assert.That(clear.Success, Is.False);
        Assert.That(clear.Error.Code, Is.EqualTo(GifErrorCode.InvalidLzwCodeStream));
        Assert.That(end.Success, Is.False);
        Assert.That(end.Error.Code, Is.EqualTo(GifErrorCode.InvalidLzwCodeStream));
        Assert.That(encoder.ToArray(), Is.Empty);
    }

    [Test]
    public void Encoder_RejectsClearAndDuplicateEndAfterEndCode()
    {
        GifLzwEncoder encoder = new GifLzwEncoder();
        encoder.Initialize(2);
        encoder.WriteClearCode();
        encoder.WriteEndCode();

        GifLzwCodeWriteResult duplicateEnd = encoder.WriteEndCode();
        GifLzwCodeWriteResult clearAfterEnd = encoder.WriteClearCode();

        Assert.That(duplicateEnd.Success, Is.False);
        Assert.That(duplicateEnd.Error.Code, Is.EqualTo(GifErrorCode.InvalidLzwCodeStream));
        Assert.That(clearAfterEnd.Success, Is.False);
        Assert.That(clearAfterEnd.Error.Code, Is.EqualTo(GifErrorCode.InvalidLzwCodeStream));
        Assert.That(encoder.ToArray(), Is.EqualTo(new byte[] { 0x2C }));
    }

    [Test]
    public void Encoder_AddDictionaryEntryGrowsCodeSizeAtThreshold()
    {
        GifLzwEncoder encoder = new GifLzwEncoder();
        encoder.Initialize(2);
        encoder.WriteClearCode();

        GifLzwEncoderDictionaryWriteResult first = encoder.AddDictionaryEntry(0, 1);
        GifLzwEncoderDictionaryWriteResult second = encoder.AddDictionaryEntry(0, 2);

        Assert.That(first.Success, Is.True);
        Assert.That(first.AddedEntry, Is.True);
        Assert.That(first.Code, Is.EqualTo(6));
        Assert.That(second.Success, Is.True);
        Assert.That(second.Code, Is.EqualTo(7));
        Assert.That(encoder.Dictionary.NextCode, Is.EqualTo(8));
        Assert.That(encoder.CurrentCodeSize, Is.EqualTo(4));
        Assert.That(second.CurrentCodeSize, Is.EqualTo(4));
    }

    [Test]
    public void Encoder_AddDictionaryEntryWritesClearWhenDictionaryIsFull()
    {
        GifLzwEncoder encoder = new GifLzwEncoder();
        encoder.Initialize(2);
        encoder.WriteClearCode();
        encoder.CurrentCodeSize = 12;
        encoder.Dictionary.NextCode = GifConstants.MaximumLzwTableSize;

        GifLzwEncoderDictionaryWriteResult result = encoder.AddDictionaryEntry(0, 1);
        GifLzwBitReader reader = new GifLzwBitReader(encoder.ToArray());

        Assert.That(result.Success, Is.True);
        Assert.That(result.WroteClearCode, Is.True);
        Assert.That(result.AddedEntry, Is.False);
        Assert.That(result.Code, Is.EqualTo(-1));
        Assert.That(reader.ReadCode(3).Code, Is.EqualTo(4));
        Assert.That(reader.ReadCode(12).Code, Is.EqualTo(4));
        Assert.That(encoder.CurrentCodeSize, Is.EqualTo(3));
        Assert.That(encoder.Dictionary.NextCode, Is.EqualTo(6));
        Assert.That(encoder.Dictionary.ContainsCode(6), Is.False);
    }

    [Test]
    public void Encoder_AddDictionaryEntryReturnsExistingEntryWhenFull()
    {
        GifLzwEncoder encoder = new GifLzwEncoder();
        encoder.Initialize(2);
        encoder.WriteClearCode();
        encoder.Dictionary.PrefixCodes[100] = 0;
        encoder.Dictionary.SuffixBytes[100] = 1;
        encoder.Dictionary.NextCode = GifConstants.MaximumLzwTableSize;

        GifLzwEncoderDictionaryWriteResult result = encoder.AddDictionaryEntry(0, 1);

        Assert.That(result.Success, Is.True);
        Assert.That(result.Code, Is.EqualTo(100));
        Assert.That(result.WroteClearCode, Is.False);
        Assert.That(result.AddedEntry, Is.False);
        Assert.That(encoder.Dictionary.NextCode, Is.EqualTo(GifConstants.MaximumLzwTableSize));
    }

    [Test]
    public void Encoder_AddDictionaryEntryRejectsInvalidOrdering()
    {
        GifLzwEncoder uninitialized = new GifLzwEncoder();
        GifLzwEncoder missingClear = new GifLzwEncoder();
        missingClear.Initialize(2);
        GifLzwEncoder afterEnd = new GifLzwEncoder();
        afterEnd.Initialize(2);
        afterEnd.WriteClearCode();
        afterEnd.WriteEndCode();

        GifLzwEncoderDictionaryWriteResult beforeInit = uninitialized.AddDictionaryEntry(0, 1);
        GifLzwEncoderDictionaryWriteResult beforeClear = missingClear.AddDictionaryEntry(0, 1);
        GifLzwEncoderDictionaryWriteResult afterEndResult = afterEnd.AddDictionaryEntry(0, 1);

        Assert.That(beforeInit.Success, Is.False);
        Assert.That(beforeInit.Error.Code, Is.EqualTo(GifErrorCode.InvalidLzwCodeStream));
        Assert.That(beforeClear.Success, Is.False);
        Assert.That(beforeClear.Error.Code, Is.EqualTo(GifErrorCode.InvalidLzwCodeStream));
        Assert.That(afterEndResult.Success, Is.False);
        Assert.That(afterEndResult.Error.Code, Is.EqualTo(GifErrorCode.InvalidLzwCodeStream));
    }

    [Test]
    public void Encoder_ConfigureCopiesClearPolicyFromOptions()
    {
        GifEncodeOptions options = new GifEncodeOptions();
        options.LzwClearPolicy = GifLzwClearPolicy.EarlyClear;
        GifLzwEncoder encoder = new GifLzwEncoder();

        encoder.Configure(options);

        Assert.That(encoder.ClearPolicy, Is.EqualTo(GifLzwClearPolicy.EarlyClear));

        encoder.Configure(null!);

        Assert.That(encoder.ClearPolicy, Is.EqualTo(GifLzwClearPolicy.ClearOnFullDictionary));
    }

    [Test]
    public void Encoder_EarlyClearPolicyWritesClearBeforeCodeSizeGrowth()
    {
        GifEncodeOptions options = new GifEncodeOptions();
        options.LzwClearPolicy = GifLzwClearPolicy.EarlyClear;
        GifLzwEncoder encoder = new GifLzwEncoder();
        encoder.Configure(options);
        encoder.Initialize(2);
        encoder.WriteClearCode();
        GifLzwEncoderDictionaryWriteResult first = encoder.AddDictionaryEntry(0, 1);

        GifLzwEncoderDictionaryWriteResult second = encoder.AddDictionaryEntry(0, 2);
        GifLzwBitReader reader = new GifLzwBitReader(encoder.ToArray());

        Assert.That(first.Success, Is.True);
        Assert.That(first.AddedEntry, Is.True);
        Assert.That(second.Success, Is.True);
        Assert.That(second.WroteClearCode, Is.True);
        Assert.That(second.WroteEarlyClearCode, Is.True);
        Assert.That(second.AddedEntry, Is.False);
        Assert.That(reader.ReadCode(3).Code, Is.EqualTo(4));
        Assert.That(reader.ReadCode(3).Code, Is.EqualTo(4));
        Assert.That(encoder.CurrentCodeSize, Is.EqualTo(3));
        Assert.That(encoder.Dictionary.NextCode, Is.EqualTo(6));
        Assert.That(encoder.Dictionary.ContainsCode(7), Is.False);
    }

    [Test]
    public void Encoder_DefaultClearPolicyGrowsInsteadOfEarlyClear()
    {
        GifLzwEncoder encoder = new GifLzwEncoder();
        encoder.Initialize(2);
        encoder.WriteClearCode();
        encoder.AddDictionaryEntry(0, 1);

        GifLzwEncoderDictionaryWriteResult result = encoder.AddDictionaryEntry(0, 2);

        Assert.That(result.Success, Is.True);
        Assert.That(result.AddedEntry, Is.True);
        Assert.That(result.WroteClearCode, Is.False);
        Assert.That(result.WroteEarlyClearCode, Is.False);
        Assert.That(result.Code, Is.EqualTo(7));
        Assert.That(encoder.CurrentCodeSize, Is.EqualTo(4));
        Assert.That(encoder.Dictionary.NextCode, Is.EqualTo(8));
    }

    [Test]
    public void Encoder_EarlyClearPolicyReturnsDuplicateBeforeClearing()
    {
        GifEncodeOptions options = new GifEncodeOptions();
        options.LzwClearPolicy = GifLzwClearPolicy.EarlyClear;
        GifLzwEncoder encoder = new GifLzwEncoder();
        encoder.Configure(options);
        encoder.Initialize(2);
        encoder.WriteClearCode();
        GifLzwEncoderDictionaryWriteResult first = encoder.AddDictionaryEntry(0, 1);
        encoder.Dictionary.NextCode = encoder.CurrentCodeSizeLimit() - 1;

        GifLzwEncoderDictionaryWriteResult duplicate = encoder.AddDictionaryEntry(0, 1);

        Assert.That(duplicate.Success, Is.True);
        Assert.That(duplicate.Code, Is.EqualTo(first.Code));
        Assert.That(duplicate.WroteClearCode, Is.False);
        Assert.That(duplicate.WroteEarlyClearCode, Is.False);
    }

    [Test]
    public void Encoder_EarlyClearPolicyClearsAtTwelveBitBoundary()
    {
        GifEncodeOptions options = new GifEncodeOptions();
        options.LzwClearPolicy = GifLzwClearPolicy.EarlyClear;
        GifLzwEncoder encoder = new GifLzwEncoder();
        encoder.Configure(options);
        encoder.Initialize(2);
        encoder.WriteClearCode();
        encoder.CurrentCodeSize = GifConstants.MaximumLzwCodeSize;
        encoder.Dictionary.NextCode = GifConstants.MaximumLzwTableSize - 1;

        GifLzwEncoderDictionaryWriteResult result = encoder.AddDictionaryEntry(0, 1);
        GifLzwBitReader reader = new GifLzwBitReader(encoder.ToArray());

        Assert.That(result.Success, Is.True);
        Assert.That(result.WroteClearCode, Is.True);
        Assert.That(result.WroteEarlyClearCode, Is.True);
        Assert.That(reader.ReadCode(3).Code, Is.EqualTo(4));
        Assert.That(reader.ReadCode(12).Code, Is.EqualTo(4));
        Assert.That(encoder.CurrentCodeSize, Is.EqualTo(3));
        Assert.That(encoder.Dictionary.NextCode, Is.EqualTo(6));
    }

    [Test]
    public void LzwRoundTrip_ClearDataAndEndCodes()
    {
        GifLzwEncoder encoder = new GifLzwEncoder();
        encoder.Initialize(2);
        encoder.WriteClearCode();
        encoder.Writer.WriteCode(0, encoder.CurrentCodeSize);
        encoder.Writer.WriteCode(1, encoder.CurrentCodeSize);
        encoder.WriteEndCode();

        GifLzwBitReader reader = new GifLzwBitReader(encoder.ToArray());
        GifLzwDecoder decoder = new GifLzwDecoder();
        decoder.Initialize(2);
        GifLzwDecodeCodeResult clear = decoder.HandleCode(reader.ReadCode(3).Code);
        GifLzwDecodeCodeResult first = decoder.HandleCode(reader.ReadCode(3).Code);
        GifLzwDecodeCodeResult second = decoder.HandleCode(reader.ReadCode(3).Code);
        GifLzwDecodeCodeResult end = decoder.HandleCode(reader.ReadCode(3).Code);

        Assert.That(encoder.ToArray(), Is.EqualTo(new byte[] { 0x44, 0x0A }));
        Assert.That(clear.IsClearCode, Is.True);
        Assert.That(first.IsDataCode, Is.True);
        Assert.That(first.Code, Is.EqualTo(0));
        Assert.That(second.IsDataCode, Is.True);
        Assert.That(second.Code, Is.EqualTo(1));
        Assert.That(end.IsEndCode, Is.True);
        Assert.That(decoder.ValidateFinalCodeStream().Success, Is.True);
    }

    [Test]
    public void LzwRoundTrip_AddedDictionaryCodeIsReadByDecoder()
    {
        GifLzwEncoder encoder = new GifLzwEncoder();
        encoder.Initialize(2);
        encoder.WriteClearCode();
        encoder.Writer.WriteCode(0, encoder.CurrentCodeSize);
        GifLzwEncoderDictionaryWriteResult encodedEntry = encoder.AddDictionaryEntry(0, 1);
        encoder.Writer.WriteCode(encodedEntry.Code, encoder.CurrentCodeSize);
        encoder.WriteEndCode();

        GifLzwBitReader reader = new GifLzwBitReader(encoder.ToArray());
        GifLzwDecoder decoder = new GifLzwDecoder();
        decoder.Initialize(2);
        decoder.HandleCode(reader.ReadCode(3).Code);
        GifLzwDecodeCodeResult root = decoder.HandleCode(reader.ReadCode(3).Code);
        GifLzwDictionaryResult decodedEntry = decoder.AddDictionaryEntry(0, 1);
        GifLzwDecodeCodeResult compound = decoder.HandleCode(reader.ReadCode(3).Code);
        GifLzwDecodeCodeResult end = decoder.HandleCode(reader.ReadCode(3).Code);

        Assert.That(encodedEntry.Code, Is.EqualTo(6));
        Assert.That(decodedEntry.Code, Is.EqualTo(encodedEntry.Code));
        Assert.That(root.Code, Is.EqualTo(0));
        Assert.That(compound.Success, Is.True);
        Assert.That(compound.Code, Is.EqualTo(6));
        Assert.That(compound.PrefixCode, Is.EqualTo(0));
        Assert.That(compound.SuffixByte, Is.EqualTo((byte)1));
        Assert.That(compound.Length, Is.EqualTo(2));
        Assert.That(end.IsEndCode, Is.True);
    }

    [Test]
    public void LzwRoundTrip_CodeWidthGrowthIsReadableByDecoder()
    {
        GifLzwEncoder encoder = new GifLzwEncoder();
        encoder.Initialize(2);
        encoder.WriteClearCode();
        encoder.Writer.WriteCode(0, encoder.CurrentCodeSize);
        GifLzwEncoderDictionaryWriteResult firstEntry = encoder.AddDictionaryEntry(0, 1);
        encoder.Writer.WriteCode(firstEntry.Code, encoder.CurrentCodeSize);
        GifLzwEncoderDictionaryWriteResult secondEntry = encoder.AddDictionaryEntry(0, 2);
        encoder.Writer.WriteCode(secondEntry.Code, encoder.CurrentCodeSize);
        encoder.WriteEndCode();

        GifLzwBitReader reader = new GifLzwBitReader(encoder.ToArray());
        GifLzwDecoder decoder = new GifLzwDecoder();
        decoder.Initialize(2);
        GifLzwDecodeCodeResult clear = decoder.HandleCode(reader.ReadCode(3).Code);
        GifLzwDecodeCodeResult root = decoder.HandleCode(reader.ReadCode(3).Code);
        GifLzwDictionaryResult firstDecodedEntry = decoder.AddDictionaryEntry(0, 1);
        GifLzwDecodeCodeResult firstCompound = decoder.HandleCode(reader.ReadCode(3).Code);
        GifLzwDictionaryResult secondDecodedEntry = decoder.AddDictionaryEntry(0, 2);
        GifLzwDecodeCodeResult secondCompound = decoder.HandleCode(reader.ReadCode(4).Code);
        GifLzwDecodeCodeResult end = decoder.HandleCode(reader.ReadCode(4).Code);

        Assert.That(clear.IsClearCode, Is.True);
        Assert.That(root.Code, Is.EqualTo(0));
        Assert.That(firstDecodedEntry.Code, Is.EqualTo(firstEntry.Code));
        Assert.That(secondDecodedEntry.Code, Is.EqualTo(secondEntry.Code));
        Assert.That(firstCompound.Code, Is.EqualTo(6));
        Assert.That(secondCompound.Code, Is.EqualTo(7));
        Assert.That(secondCompound.Length, Is.EqualTo(2));
        Assert.That(end.IsEndCode, Is.True);
        Assert.That(decoder.CurrentCodeSize, Is.EqualTo(4));
    }

    [Test]
    public void LzwRoundTrip_EarlyClearEmitsReadableClearCode()
    {
        GifEncodeOptions options = new GifEncodeOptions();
        options.LzwClearPolicy = GifLzwClearPolicy.EarlyClear;
        GifLzwEncoder encoder = new GifLzwEncoder();
        encoder.Configure(options);
        encoder.Initialize(2);
        encoder.WriteClearCode();
        encoder.AddDictionaryEntry(0, 1);
        GifLzwEncoderDictionaryWriteResult earlyClear = encoder.AddDictionaryEntry(0, 2);

        GifLzwBitReader reader = new GifLzwBitReader(encoder.ToArray());
        GifLzwDecoder decoder = new GifLzwDecoder();
        decoder.Initialize(2);
        GifLzwDecodeCodeResult firstClear = decoder.HandleCode(reader.ReadCode(3).Code);
        GifLzwDecodeCodeResult secondClear = decoder.HandleCode(reader.ReadCode(3).Code);

        Assert.That(earlyClear.WroteEarlyClearCode, Is.True);
        Assert.That(firstClear.IsClearCode, Is.True);
        Assert.That(secondClear.IsClearCode, Is.True);
        Assert.That(decoder.CurrentCodeSize, Is.EqualTo(3));
        Assert.That(decoder.Dictionary.NextCode, Is.EqualTo(6));
    }

    [Test]
    public void Dictionary_InitializeCreatesRootClearAndEndCodes()
    {
        GifLzwDictionary dictionary = new GifLzwDictionary();

        GifLzwDictionaryResult result = dictionary.Initialize(2);

        Assert.That(result.Success, Is.True);
        Assert.That(dictionary.MinimumCodeSize, Is.EqualTo(2));
        Assert.That(dictionary.RootCodeCount, Is.EqualTo(4));
        Assert.That(dictionary.ClearCode, Is.EqualTo(4));
        Assert.That(dictionary.EndCode, Is.EqualTo(5));
        Assert.That(dictionary.NextCode, Is.EqualTo(6));
        Assert.That(result.Code, Is.EqualTo(6));
        Assert.That(dictionary.PrefixCodes.Length, Is.EqualTo(GifConstants.MaximumLzwTableSize));
        Assert.That(dictionary.PrefixCodes[0], Is.EqualTo(-1));
        Assert.That(dictionary.SuffixBytes[3], Is.EqualTo((byte)3));
        Assert.That(dictionary.FirstBytes[3], Is.EqualTo((byte)3));
        Assert.That(dictionary.Lengths[3], Is.EqualTo(1));
        Assert.That(dictionary.ContainsCode(dictionary.ClearCode), Is.True);
        Assert.That(dictionary.ContainsCode(dictionary.EndCode), Is.True);
        Assert.That(dictionary.ContainsDataCode(dictionary.ClearCode), Is.False);
        Assert.That(dictionary.ContainsDataCode(dictionary.EndCode), Is.False);
    }

    [Test]
    public void Dictionary_InitializeSupportsEightBitRoots()
    {
        GifLzwDictionary dictionary = new GifLzwDictionary();

        GifLzwDictionaryResult result = dictionary.Initialize(8);

        Assert.That(result.Success, Is.True);
        Assert.That(dictionary.RootCodeCount, Is.EqualTo(256));
        Assert.That(dictionary.ClearCode, Is.EqualTo(256));
        Assert.That(dictionary.EndCode, Is.EqualTo(257));
        Assert.That(dictionary.NextCode, Is.EqualTo(258));
        Assert.That(dictionary.SuffixBytes[255], Is.EqualTo((byte)255));
        Assert.That(dictionary.FirstBytes[255], Is.EqualTo((byte)255));
        Assert.That(dictionary.Lengths[255], Is.EqualTo(1));
    }

    [Test]
    public void Dictionary_RejectsInvalidMinimumCodeSizes()
    {
        GifLzwDictionary dictionary = new GifLzwDictionary();

        GifLzwDictionaryResult tooSmall = dictionary.Initialize(1);
        GifLzwDictionaryResult tooLarge = dictionary.Initialize(9);

        Assert.That(tooSmall.Success, Is.False);
        Assert.That(tooSmall.Error.Code, Is.EqualTo(GifErrorCode.InvalidLzwMinimumCodeSize));
        Assert.That(tooLarge.Success, Is.False);
        Assert.That(tooLarge.Error.Code, Is.EqualTo(GifErrorCode.InvalidLzwMinimumCodeSize));
        Assert.That(dictionary.NextCode, Is.EqualTo(0));
    }

    [Test]
    public void Dictionary_AddStoresPrefixSuffixFirstByteAndLength()
    {
        GifLzwDictionary dictionary = new GifLzwDictionary();
        dictionary.Initialize(2);

        GifLzwDictionaryResult first = dictionary.Add(2, 9);
        GifLzwDictionaryResult second = dictionary.Add(first.Code, 8);

        Assert.That(first.Success, Is.True);
        Assert.That(first.Code, Is.EqualTo(6));
        Assert.That(dictionary.PrefixCodes[6], Is.EqualTo(2));
        Assert.That(dictionary.SuffixBytes[6], Is.EqualTo((byte)9));
        Assert.That(dictionary.FirstBytes[6], Is.EqualTo((byte)2));
        Assert.That(dictionary.Lengths[6], Is.EqualTo(2));
        Assert.That(second.Success, Is.True);
        Assert.That(second.Code, Is.EqualTo(7));
        Assert.That(dictionary.PrefixCodes[7], Is.EqualTo(6));
        Assert.That(dictionary.SuffixBytes[7], Is.EqualTo((byte)8));
        Assert.That(dictionary.FirstBytes[7], Is.EqualTo((byte)2));
        Assert.That(dictionary.Lengths[7], Is.EqualTo(3));
        Assert.That(dictionary.NextCode, Is.EqualTo(8));
    }

    [Test]
    public void Dictionary_AddRejectsControlOrUnknownPrefixCodes()
    {
        GifLzwDictionary dictionary = new GifLzwDictionary();
        dictionary.Initialize(2);

        GifLzwDictionaryResult clearPrefix = dictionary.Add(dictionary.ClearCode, 1);
        GifLzwDictionaryResult unknownPrefix = dictionary.Add(99, 1);

        Assert.That(clearPrefix.Success, Is.False);
        Assert.That(clearPrefix.Error.Code, Is.EqualTo(GifErrorCode.InvalidLzwCodeStream));
        Assert.That(unknownPrefix.Success, Is.False);
        Assert.That(unknownPrefix.Error.Code, Is.EqualTo(GifErrorCode.InvalidLzwCodeStream));
        Assert.That(dictionary.NextCode, Is.EqualTo(6));
    }

    [Test]
    public void Dictionary_AddRejectsFullTable()
    {
        GifLzwDictionary dictionary = new GifLzwDictionary();
        dictionary.Initialize(2);
        dictionary.NextCode = GifConstants.MaximumLzwTableSize;

        GifLzwDictionaryResult result = dictionary.Add(0, 1);

        Assert.That(result.Success, Is.False);
        Assert.That(result.Error.Code, Is.EqualTo(GifErrorCode.MemoryLimitExceeded));
        Assert.That(dictionary.IsFull(), Is.True);
    }

    [Test]
    public void Dictionary_InitializeClearsPreviousEntries()
    {
        GifLzwDictionary dictionary = new GifLzwDictionary();
        dictionary.Initialize(2);
        dictionary.Add(0, 1);

        GifLzwDictionaryResult result = dictionary.Initialize(2);

        Assert.That(result.Success, Is.True);
        Assert.That(dictionary.ContainsCode(6), Is.False);
        Assert.That(dictionary.Lengths[6], Is.EqualTo(0));
        Assert.That(dictionary.PrefixCodes[6], Is.EqualTo(-1));
    }

    [Test]
    public void Decoder_InitializeSetsDictionaryAndInitialCodeSize()
    {
        GifLzwDecoder decoder = new GifLzwDecoder();

        GifLzwDecodeCodeResult result = decoder.Initialize(2);

        Assert.That(result.Success, Is.True);
        Assert.That(decoder.MinimumCodeSize, Is.EqualTo(2));
        Assert.That(decoder.CurrentCodeSize, Is.EqualTo(3));
        Assert.That(decoder.Dictionary.ClearCode, Is.EqualTo(4));
        Assert.That(decoder.Dictionary.EndCode, Is.EqualTo(5));
        Assert.That(decoder.HasSeenClearCode, Is.False);
        Assert.That(decoder.HasSeenEndCode, Is.False);
        Assert.That(decoder.HasPreviousCode, Is.False);
    }

    [Test]
    public void Decoder_RequiresInitialClearCodeBeforeDataOrEnd()
    {
        GifLzwDecoder decoder = new GifLzwDecoder();
        decoder.Initialize(2);

        GifLzwDecodeCodeResult data = decoder.HandleCode(0);
        GifLzwDecodeCodeResult end = decoder.HandleCode(decoder.Dictionary.EndCode);

        Assert.That(data.Success, Is.False);
        Assert.That(data.Error.Code, Is.EqualTo(GifErrorCode.InvalidLzwCodeStream));
        Assert.That(end.Success, Is.False);
        Assert.That(end.Error.Code, Is.EqualTo(GifErrorCode.InvalidLzwCodeStream));
        Assert.That(decoder.HasSeenClearCode, Is.False);
        Assert.That(decoder.HasSeenEndCode, Is.False);
    }

    [Test]
    public void Decoder_HandleClearCodeResetsDictionaryAndPreviousCode()
    {
        GifLzwDecoder decoder = new GifLzwDecoder();
        decoder.Initialize(2);
        decoder.HandleCode(decoder.Dictionary.ClearCode);
        decoder.HandleCode(2);
        decoder.Dictionary.Add(2, 1);

        GifLzwDecodeCodeResult result = decoder.HandleCode(decoder.Dictionary.ClearCode);

        Assert.That(result.Success, Is.True);
        Assert.That(result.IsClearCode, Is.True);
        Assert.That(decoder.HasSeenClearCode, Is.True);
        Assert.That(decoder.HasPreviousCode, Is.False);
        Assert.That(decoder.PreviousCode, Is.EqualTo(-1));
        Assert.That(decoder.CurrentCodeSize, Is.EqualTo(3));
        Assert.That(decoder.Dictionary.NextCode, Is.EqualTo(6));
        Assert.That(decoder.Dictionary.ContainsCode(6), Is.False);
    }

    [Test]
    public void Decoder_AcceptsDataCodeAfterClearAndTracksPreviousCode()
    {
        GifLzwDecoder decoder = new GifLzwDecoder();
        decoder.Initialize(2);
        decoder.HandleCode(decoder.Dictionary.ClearCode);

        GifLzwDecodeCodeResult first = decoder.HandleCode(3);
        GifLzwDecodeCodeResult second = decoder.HandleCode(1);

        Assert.That(first.Success, Is.True);
        Assert.That(first.IsDataCode, Is.True);
        Assert.That(first.IsFirstDataCode, Is.True);
        Assert.That(second.Success, Is.True);
        Assert.That(second.IsDataCode, Is.True);
        Assert.That(second.IsFirstDataCode, Is.False);
        Assert.That(decoder.HasPreviousCode, Is.True);
        Assert.That(decoder.PreviousCode, Is.EqualTo(1));
    }

    [Test]
    public void Decoder_RejectsUnknownDataCodeAfterClear()
    {
        GifLzwDecoder decoder = new GifLzwDecoder();
        decoder.Initialize(2);
        decoder.HandleCode(decoder.Dictionary.ClearCode);

        GifLzwDecodeCodeResult result = decoder.HandleCode(99);

        Assert.That(result.Success, Is.False);
        Assert.That(result.Error.Code, Is.EqualTo(GifErrorCode.InvalidLzwCodeStream));
        Assert.That(decoder.HasPreviousCode, Is.False);
    }

    [Test]
    public void Decoder_HandleEndCodeMarksStreamEnded()
    {
        GifLzwDecoder decoder = new GifLzwDecoder();
        decoder.Initialize(2);
        decoder.HandleCode(decoder.Dictionary.ClearCode);
        decoder.HandleCode(0);

        GifLzwDecodeCodeResult result = decoder.HandleCode(decoder.Dictionary.EndCode);

        Assert.That(result.Success, Is.True);
        Assert.That(result.IsEndCode, Is.True);
        Assert.That(decoder.HasSeenEndCode, Is.True);
        Assert.That(decoder.HasPreviousCode, Is.False);
        Assert.That(decoder.PreviousCode, Is.EqualTo(-1));
    }

    [Test]
    public void Decoder_RejectsCodesAfterEndCode()
    {
        GifLzwDecoder decoder = new GifLzwDecoder();
        decoder.Initialize(2);
        decoder.HandleCode(decoder.Dictionary.ClearCode);
        decoder.HandleCode(decoder.Dictionary.EndCode);

        GifLzwDecodeCodeResult result = decoder.HandleCode(0);

        Assert.That(result.Success, Is.False);
        Assert.That(result.Error.Code, Is.EqualTo(GifErrorCode.InvalidLzwCodeStream));
        Assert.That(decoder.HasSeenEndCode, Is.True);
    }

    [Test]
    public void Decoder_AddDictionaryEntryGrowsCodeSizeAtThreshold()
    {
        GifLzwDecoder decoder = new GifLzwDecoder();
        decoder.Initialize(2);
        decoder.HandleCode(decoder.Dictionary.ClearCode);

        GifLzwDictionaryResult first = decoder.AddDictionaryEntry(0, 1);
        GifLzwDictionaryResult second = decoder.AddDictionaryEntry(0, 2);

        Assert.That(first.Success, Is.True);
        Assert.That(first.Code, Is.EqualTo(6));
        Assert.That(decoder.CurrentCodeSize, Is.EqualTo(4));
        Assert.That(second.Success, Is.True);
        Assert.That(second.Code, Is.EqualTo(7));
        Assert.That(decoder.Dictionary.NextCode, Is.EqualTo(8));
        Assert.That(decoder.CurrentCodeSizeLimit(), Is.EqualTo(16));
    }

    [Test]
    public void Decoder_AddDictionaryEntryUsesNineBitInitialWidthForEightBitRoots()
    {
        GifLzwDecoder decoder = new GifLzwDecoder();
        decoder.Initialize(8);
        decoder.HandleCode(decoder.Dictionary.ClearCode);
        decoder.Dictionary.NextCode = 511;

        GifLzwDictionaryResult result = decoder.AddDictionaryEntry(0, 1);

        Assert.That(result.Success, Is.True);
        Assert.That(result.Code, Is.EqualTo(511));
        Assert.That(decoder.Dictionary.NextCode, Is.EqualTo(512));
        Assert.That(decoder.CurrentCodeSize, Is.EqualTo(10));
        Assert.That(decoder.CurrentCodeSizeLimit(), Is.EqualTo(1024));
    }

    [Test]
    public void Decoder_CodeSizeGrowthStopsAtTwelveBits()
    {
        GifLzwDecoder decoder = new GifLzwDecoder();
        decoder.Initialize(2);
        decoder.HandleCode(decoder.Dictionary.ClearCode);
        decoder.CurrentCodeSize = 11;
        decoder.Dictionary.NextCode = 2047;

        GifLzwDictionaryResult growsToTwelve = decoder.AddDictionaryEntry(0, 1);
        decoder.Dictionary.NextCode = GifConstants.MaximumLzwTableSize - 1;
        GifLzwDictionaryResult staysAtTwelve = decoder.AddDictionaryEntry(0, 2);

        Assert.That(growsToTwelve.Success, Is.True);
        Assert.That(decoder.CurrentCodeSize, Is.EqualTo(12));
        Assert.That(decoder.CurrentCodeSizeLimit(), Is.EqualTo(GifConstants.MaximumLzwTableSize));
        Assert.That(staysAtTwelve.Success, Is.True);
        Assert.That(decoder.CurrentCodeSize, Is.EqualTo(12));
        Assert.That(decoder.Dictionary.NextCode, Is.EqualTo(GifConstants.MaximumLzwTableSize));
    }

    [Test]
    public void Decoder_ClearCodeResetsCodeSizeAfterGrowth()
    {
        GifLzwDecoder decoder = new GifLzwDecoder();
        decoder.Initialize(2);
        decoder.HandleCode(decoder.Dictionary.ClearCode);
        decoder.AddDictionaryEntry(0, 1);
        decoder.AddDictionaryEntry(0, 2);

        GifLzwDecodeCodeResult clear = decoder.HandleCode(decoder.Dictionary.ClearCode);

        Assert.That(clear.Success, Is.True);
        Assert.That(clear.IsClearCode, Is.True);
        Assert.That(decoder.CurrentCodeSize, Is.EqualTo(3));
        Assert.That(decoder.CurrentCodeSizeLimit(), Is.EqualTo(8));
        Assert.That(decoder.Dictionary.NextCode, Is.EqualTo(6));
    }

    [Test]
    public void Decoder_HandlesSpecialUnknownCurrentCodeWhenPreviousCodeExists()
    {
        GifLzwDecoder decoder = new GifLzwDecoder();
        decoder.Initialize(2);
        decoder.HandleCode(decoder.Dictionary.ClearCode);
        decoder.HandleCode(2);

        GifLzwDecodeCodeResult result = decoder.HandleCode(decoder.Dictionary.NextCode);

        Assert.That(result.Success, Is.True);
        Assert.That(result.IsDataCode, Is.True);
        Assert.That(result.IsSpecialCurrentCode, Is.True);
        Assert.That(result.Code, Is.EqualTo(6));
        Assert.That(result.PrefixCode, Is.EqualTo(2));
        Assert.That(result.SuffixByte, Is.EqualTo((byte)2));
        Assert.That(result.FirstByte, Is.EqualTo((byte)2));
        Assert.That(result.Length, Is.EqualTo(2));
        Assert.That(decoder.PreviousCode, Is.EqualTo(6));
    }

    [Test]
    public void Decoder_RejectsSpecialUnknownCurrentCodeWithoutPreviousCode()
    {
        GifLzwDecoder decoder = new GifLzwDecoder();
        decoder.Initialize(2);
        decoder.HandleCode(decoder.Dictionary.ClearCode);

        GifLzwDecodeCodeResult result = decoder.HandleCode(decoder.Dictionary.NextCode);

        Assert.That(result.Success, Is.False);
        Assert.That(result.Error.Code, Is.EqualTo(GifErrorCode.InvalidLzwCodeStream));
        Assert.That(result.IsSpecialCurrentCode, Is.False);
        Assert.That(decoder.HasPreviousCode, Is.False);
    }

    [Test]
    public void Decoder_RejectsUnknownCodeGreaterThanNextCode()
    {
        GifLzwDecoder decoder = new GifLzwDecoder();
        decoder.Initialize(2);
        decoder.HandleCode(decoder.Dictionary.ClearCode);
        decoder.HandleCode(2);

        GifLzwDecodeCodeResult result = decoder.HandleCode(decoder.Dictionary.NextCode + 1);

        Assert.That(result.Success, Is.False);
        Assert.That(result.Error.Code, Is.EqualTo(GifErrorCode.InvalidLzwCodeStream));
        Assert.That(result.IsSpecialCurrentCode, Is.False);
    }

    [Test]
    public void Decoder_SpecialUnknownCurrentCodeCanBeMaterializedInDictionary()
    {
        GifLzwDecoder decoder = new GifLzwDecoder();
        decoder.Initialize(2);
        decoder.HandleCode(decoder.Dictionary.ClearCode);
        decoder.HandleCode(3);

        GifLzwDecodeCodeResult special = decoder.HandleCode(decoder.Dictionary.NextCode);
        GifLzwDictionaryResult added = decoder.AddDictionaryEntry(special.PrefixCode, special.SuffixByte);

        Assert.That(special.Success, Is.True);
        Assert.That(special.IsSpecialCurrentCode, Is.True);
        Assert.That(added.Success, Is.True);
        Assert.That(added.Code, Is.EqualTo(special.Code));
        Assert.That(decoder.Dictionary.ContainsDataCode(special.Code), Is.True);
        Assert.That(decoder.Dictionary.PrefixCodes[special.Code], Is.EqualTo(3));
        Assert.That(decoder.Dictionary.SuffixBytes[special.Code], Is.EqualTo((byte)3));
        Assert.That(decoder.Dictionary.Lengths[special.Code], Is.EqualTo(2));
    }

    [Test]
    public void Decoder_OutputValidationRecordsCountsAndAcceptsExactFinalCount()
    {
        GifLzwDecoder decoder = new GifLzwDecoder();

        GifLzwOutputCountResult configured = decoder.ConfigureOutputValidation(4, 10);
        GifLzwOutputCountResult first = decoder.RecordOutputCount(2);
        GifLzwOutputCountResult second = decoder.RecordOutputCount(2);
        GifLzwOutputCountResult final = decoder.ValidateFinalOutputCount();

        Assert.That(configured.Success, Is.True);
        Assert.That(first.Success, Is.True);
        Assert.That(first.OutputCount, Is.EqualTo(2));
        Assert.That(second.Success, Is.True);
        Assert.That(second.OutputCount, Is.EqualTo(4));
        Assert.That(final.Success, Is.True);
        Assert.That(final.OutputCount, Is.EqualTo(4));
        Assert.That(decoder.OutputCount, Is.EqualTo(4));
    }

    [Test]
    public void Decoder_OutputValidationRejectsConfiguredLimitOverflow()
    {
        GifLzwDecoder decoder = new GifLzwDecoder();
        decoder.ConfigureOutputValidation(10, 3);
        decoder.RecordOutputCount(2);

        GifLzwOutputCountResult result = decoder.RecordOutputCount(2);

        Assert.That(result.Success, Is.False);
        Assert.That(result.Error.Code, Is.EqualTo(GifErrorCode.DecodedPixelLimitExceeded));
        Assert.That(result.OutputCount, Is.EqualTo(2));
        Assert.That(decoder.OutputCount, Is.EqualTo(2));
    }

    [Test]
    public void Decoder_OutputValidationRejectsExpectedCountOverflow()
    {
        GifLzwDecoder decoder = new GifLzwDecoder();
        decoder.ConfigureOutputValidation(3, 10);
        decoder.RecordOutputCount(2);

        GifLzwOutputCountResult result = decoder.RecordOutputCount(2);

        Assert.That(result.Success, Is.False);
        Assert.That(result.Error.Code, Is.EqualTo(GifErrorCode.DecodedPixelCountMismatch));
        Assert.That(result.OutputCount, Is.EqualTo(2));
        Assert.That(decoder.OutputCount, Is.EqualTo(2));
    }

    [Test]
    public void Decoder_OutputValidationRejectsShortFinalCount()
    {
        GifLzwDecoder decoder = new GifLzwDecoder();
        decoder.ConfigureOutputValidation(4, 10);
        decoder.RecordOutputCount(3);

        GifLzwOutputCountResult result = decoder.ValidateFinalOutputCount();

        Assert.That(result.Success, Is.False);
        Assert.That(result.Error.Code, Is.EqualTo(GifErrorCode.DecodedPixelCountMismatch));
        Assert.That(result.OutputCount, Is.EqualTo(3));
    }

    [Test]
    public void Decoder_OutputValidationRejectsNegativeInputs()
    {
        GifLzwDecoder decoder = new GifLzwDecoder();

        GifLzwOutputCountResult negativeExpected = decoder.ConfigureOutputValidation(-1, 10);
        GifLzwOutputCountResult negativeMaximum = decoder.ConfigureOutputValidation(1, -1);
        GifLzwOutputCountResult negativeIncrement = decoder.RecordOutputCount(-1);

        Assert.That(negativeExpected.Success, Is.False);
        Assert.That(negativeExpected.Error.Code, Is.EqualTo(GifErrorCode.DecodedPixelCountMismatch));
        Assert.That(negativeMaximum.Success, Is.False);
        Assert.That(negativeMaximum.Error.Code, Is.EqualTo(GifErrorCode.DecodedPixelLimitExceeded));
        Assert.That(negativeIncrement.Success, Is.False);
        Assert.That(negativeIncrement.Error.Code, Is.EqualTo(GifErrorCode.DecodedPixelCountMismatch));
    }

    [Test]
    public void Decoder_InitializeResetsOutputCountButKeepsConfiguredLimits()
    {
        GifLzwDecoder decoder = new GifLzwDecoder();
        decoder.ConfigureOutputValidation(4, 10);
        decoder.RecordOutputCount(2);

        GifLzwDecodeCodeResult initialized = decoder.Initialize(2);

        Assert.That(initialized.Success, Is.True);
        Assert.That(decoder.OutputCount, Is.EqualTo(0));
        Assert.That(decoder.ExpectedOutputCount, Is.EqualTo(4));
        Assert.That(decoder.MaxOutputCount, Is.EqualTo(10));
    }

    [Test]
    public void Decoder_ConfigureEnablesTolerantModeFromOptions()
    {
        GifDecodeOptions options = new GifDecodeOptions();
        options.Strictness = GifStrictness.Tolerant;
        GifLzwDecoder decoder = new GifLzwDecoder();

        decoder.Configure(options);

        Assert.That(decoder.Strictness, Is.EqualTo(GifStrictness.Tolerant));
        Assert.That(decoder.IsTolerantMode(), Is.True);

        options.Strictness = GifStrictness.Normal;
        options.TolerateMalformedStreams = true;
        decoder.Configure(options);

        Assert.That(decoder.Strictness, Is.EqualTo(GifStrictness.Normal));
        Assert.That(decoder.TolerateMalformedStreams, Is.True);
        Assert.That(decoder.IsTolerantMode(), Is.True);
    }

    [Test]
    public void Decoder_TolerantModeAcceptsDataBeforeInitialClearCodeWithWarning()
    {
        GifDecodeOptions options = new GifDecodeOptions();
        options.TolerateMalformedStreams = true;
        GifLzwDecoder decoder = new GifLzwDecoder();
        decoder.Configure(options);
        decoder.Initialize(2);

        GifLzwDecodeCodeResult result = decoder.HandleCode(2);

        Assert.That(result.Success, Is.True);
        Assert.That(result.IsDataCode, Is.True);
        Assert.That(result.Warning.Code, Is.EqualTo(GifWarningCode.ToleratedMalformedBlock));
        Assert.That(decoder.HasSeenClearCode, Is.True);
        Assert.That(decoder.HasPreviousCode, Is.True);
        Assert.That(decoder.Warnings.Length, Is.EqualTo(1));
    }

    [Test]
    public void Decoder_TolerantModeAcceptsEndBeforeInitialClearCodeWithWarning()
    {
        GifDecodeOptions options = new GifDecodeOptions();
        options.TolerateMalformedStreams = true;
        GifLzwDecoder decoder = new GifLzwDecoder();
        decoder.Configure(options);
        decoder.Initialize(2);

        GifLzwDecodeCodeResult result = decoder.HandleCode(decoder.Dictionary.EndCode);

        Assert.That(result.Success, Is.True);
        Assert.That(result.IsEndCode, Is.True);
        Assert.That(result.Warning.Code, Is.EqualTo(GifWarningCode.ToleratedMalformedBlock));
        Assert.That(decoder.HasSeenClearCode, Is.True);
        Assert.That(decoder.HasSeenEndCode, Is.True);
        Assert.That(decoder.Warnings.Length, Is.EqualTo(1));
    }

    [Test]
    public void Decoder_TolerantModeIgnoresCodesAfterEndCodeWithWarning()
    {
        GifDecodeOptions options = new GifDecodeOptions();
        options.TolerateMalformedStreams = true;
        GifLzwDecoder decoder = new GifLzwDecoder();
        decoder.Configure(options);
        decoder.Initialize(2);
        decoder.HandleCode(decoder.Dictionary.ClearCode);
        decoder.HandleCode(decoder.Dictionary.EndCode);

        GifLzwDecodeCodeResult result = decoder.HandleCode(1);

        Assert.That(result.Success, Is.True);
        Assert.That(result.IsIgnoredCode, Is.True);
        Assert.That(result.Warning.Code, Is.EqualTo(GifWarningCode.ToleratedMalformedBlock));
        Assert.That(decoder.HasSeenEndCode, Is.True);
        Assert.That(decoder.Warnings.Length, Is.EqualTo(1));
    }

    [Test]
    public void Decoder_ValidateFinalCodeStreamRequiresEndCodeUnlessTolerant()
    {
        GifLzwDecoder strictDecoder = new GifLzwDecoder();
        strictDecoder.Initialize(2);
        strictDecoder.HandleCode(strictDecoder.Dictionary.ClearCode);
        strictDecoder.HandleCode(0);
        GifLzwDecoder tolerantDecoder = new GifLzwDecoder();
        GifDecodeOptions options = new GifDecodeOptions();
        options.TolerateMalformedStreams = true;
        tolerantDecoder.Configure(options);
        tolerantDecoder.Initialize(2);
        tolerantDecoder.HandleCode(tolerantDecoder.Dictionary.ClearCode);
        tolerantDecoder.HandleCode(0);

        GifLzwDecodeCodeResult strict = strictDecoder.ValidateFinalCodeStream();
        GifLzwDecodeCodeResult tolerant = tolerantDecoder.ValidateFinalCodeStream();

        Assert.That(strict.Success, Is.False);
        Assert.That(strict.Error.Code, Is.EqualTo(GifErrorCode.InvalidLzwCodeStream));
        Assert.That(tolerant.Success, Is.True);
        Assert.That(tolerant.Warning.Code, Is.EqualTo(GifWarningCode.ToleratedMalformedBlock));
        Assert.That(tolerantDecoder.Warnings.Length, Is.EqualTo(1));
    }

    [Test]
    public void BitReader_ConsumesExactRemainingBitsAndThenReportsEnd()
    {
        GifLzwBitReader reader = new GifLzwBitReader(new byte[] { 0x5A });

        GifLzwCodeReadResult exact = reader.ReadCode(8);
        GifLzwCodeReadResult afterEnd = reader.ReadCode(1);

        Assert.That(exact.Success, Is.True);
        Assert.That(exact.Code, Is.EqualTo(0x5A));
        Assert.That(reader.ByteOffset, Is.EqualTo(1));
        Assert.That(reader.BitOffset, Is.EqualTo(0));
        Assert.That(reader.RemainingBits(), Is.EqualTo(0));
        Assert.That(afterEnd.Success, Is.False);
        Assert.That(afterEnd.EndOfData, Is.True);
        Assert.That(reader.ByteOffset, Is.EqualTo(1));
    }

    [Test]
    public void Dictionary_GettersReturnDefaultsForInvalidAndControlCodes()
    {
        GifLzwDictionary dictionary = new GifLzwDictionary();
        dictionary.Initialize(2);

        Assert.That(dictionary.GetPrefixCode(-1), Is.EqualTo(-1));
        Assert.That(dictionary.GetPrefixCode(4096), Is.EqualTo(-1));
        Assert.That(dictionary.GetSuffixByte(dictionary.ClearCode), Is.EqualTo((byte)0));
        Assert.That(dictionary.GetFirstByte(dictionary.EndCode), Is.EqualTo((byte)0));
        Assert.That(dictionary.GetLength(dictionary.ClearCode), Is.EqualTo(0));
        Assert.That(dictionary.GetLength(99), Is.EqualTo(0));
    }

    [Test]
    public void Dictionary_ControlCodePredicatesDistinguishClearEndAndData()
    {
        GifLzwDictionary dictionary = new GifLzwDictionary();
        dictionary.Initialize(3);

        Assert.That(dictionary.IsClearCode(8), Is.True);
        Assert.That(dictionary.IsEndCode(9), Is.True);
        Assert.That(dictionary.IsControlCode(8), Is.True);
        Assert.That(dictionary.IsControlCode(9), Is.True);
        Assert.That(dictionary.IsControlCode(7), Is.False);
    }

    [Test]
    public void Decoder_GrowCodeSizeIfNeededCatchesUpAcrossMultipleThresholds()
    {
        GifLzwDecoder decoder = new GifLzwDecoder();
        decoder.Initialize(2);
        decoder.CurrentCodeSize = 3;
        decoder.Dictionary.NextCode = 16;

        decoder.GrowCodeSizeIfNeeded();

        Assert.That(decoder.CurrentCodeSize, Is.EqualTo(5));
        Assert.That(decoder.CurrentCodeSizeLimit(), Is.EqualTo(32));
    }

    [Test]
    public void Decoder_CurrentCodeSizeLimitHandlesUninitializedAndMaximumState()
    {
        GifLzwDecoder decoder = new GifLzwDecoder();

        Assert.That(decoder.CurrentCodeSizeLimit(), Is.EqualTo(0));
        Assert.That(decoder.CanGrowCodeSize(), Is.False);

        decoder.Initialize(2);
        decoder.CurrentCodeSize = GifConstants.MaximumLzwCodeSize;

        Assert.That(decoder.CurrentCodeSizeLimit(), Is.EqualTo(GifConstants.MaximumLzwTableSize));
        Assert.That(decoder.CanGrowCodeSize(), Is.False);
    }

    [Test]
    public void Decoder_HandleCodeReturnsMetadataForAddedDictionaryCodes()
    {
        GifLzwDecoder decoder = new GifLzwDecoder();
        decoder.Initialize(2);
        decoder.HandleCode(decoder.Dictionary.ClearCode);
        GifLzwDictionaryResult added = decoder.AddDictionaryEntry(2, 9);

        GifLzwDecodeCodeResult result = decoder.HandleCode(added.Code);

        Assert.That(result.Success, Is.True);
        Assert.That(result.IsDataCode, Is.True);
        Assert.That(result.IsSpecialCurrentCode, Is.False);
        Assert.That(result.PrefixCode, Is.EqualTo(2));
        Assert.That(result.SuffixByte, Is.EqualTo((byte)9));
        Assert.That(result.FirstByte, Is.EqualTo((byte)2));
        Assert.That(result.Length, Is.EqualTo(2));
        Assert.That(decoder.PreviousCode, Is.EqualTo(added.Code));
    }

    [Test]
    public void Decoder_StrictModeRejectsClearCodeAfterEndCode()
    {
        GifLzwDecoder decoder = new GifLzwDecoder();
        decoder.Initialize(2);
        decoder.HandleCode(decoder.Dictionary.ClearCode);
        decoder.HandleCode(decoder.Dictionary.EndCode);

        GifLzwDecodeCodeResult result = decoder.HandleCode(decoder.Dictionary.ClearCode);

        Assert.That(result.Success, Is.False);
        Assert.That(result.Error.Code, Is.EqualTo(GifErrorCode.InvalidLzwCodeStream));
        Assert.That(decoder.HasSeenEndCode, Is.True);
    }

    [Test]
    public void Decoder_ConfigureNullRestoresStrictDefaultsWithoutClearingWarnings()
    {
        GifDecodeOptions options = new GifDecodeOptions();
        options.TolerateMalformedStreams = true;
        GifLzwDecoder decoder = new GifLzwDecoder();
        decoder.Configure(options);
        decoder.Initialize(2);
        decoder.HandleCode(1);

        decoder.Configure(null!);

        Assert.That(decoder.Strictness, Is.EqualTo(GifStrictness.Normal));
        Assert.That(decoder.TolerateMalformedStreams, Is.False);
        Assert.That(decoder.IsTolerantMode(), Is.False);
        Assert.That(decoder.Warnings.Length, Is.EqualTo(1));
    }
}
