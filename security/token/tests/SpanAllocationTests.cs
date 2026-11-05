using token;

namespace token.Tests;

public class SpanAllocationTests
{
    const int Iterations = 128;
    const int PayloadLength = 1024;

    [TestCase(ByteSpanOperation.Base64UrlEncode)]
    [TestCase(ByteSpanOperation.Base64Encode)]
    [TestCase(ByteSpanOperation.HexEncode)]
    [TestCase(ByteSpanOperation.Utf8Decode)]
    public void ByteSpanOperationsAvoidCallerSliceCopies(ByteSpanOperation operation)
    {
        byte[] framed = CreateFramedBytes();

        long spanAllocations = MeasureAllocatedBytes(
            () => InvokeByteSpanOperation(operation, framed, 1, PayloadLength));
        long copiedAllocations = MeasureAllocatedBytes(
            () => InvokeByteArrayOperationWithCopy(operation, framed, 1, PayloadLength));

        Assert.That(
            copiedAllocations - spanAllocations,
            Is.GreaterThanOrEqualTo((long)Iterations * PayloadLength));
    }

    [TestCase(CharacterSpanOperation.Base64UrlDecode)]
    [TestCase(CharacterSpanOperation.Base64Decode)]
    [TestCase(CharacterSpanOperation.HexDecode)]
    [TestCase(CharacterSpanOperation.Utf8Encode)]
    public void CharacterSpanOperationsAvoidCallerSubstringAllocations(CharacterSpanOperation operation)
    {
        string framed = "!" + new string('A', PayloadLength) + "!";

        long spanAllocations = MeasureAllocatedBytes(
            () => InvokeCharacterSpanOperation(operation, framed, 1, PayloadLength));
        long copiedAllocations = MeasureAllocatedBytes(
            () => InvokeStringOperationWithCopy(operation, framed, 1, PayloadLength));

        Assert.That(
            copiedAllocations - spanAllocations,
            Is.GreaterThanOrEqualTo((long)Iterations * PayloadLength * sizeof(char)));
    }

    static byte[] CreateFramedBytes()
    {
        byte[] framed = new byte[PayloadLength + 2];
        framed[0] = 0xFF;
        framed[framed.Length - 1] = 0xFF;

        int index = 0;
        while (index < PayloadLength)
        {
            framed[index + 1] = (byte)('A' + index % 26);
            index += 1;
        }

        return framed;
    }

    static long MeasureAllocatedBytes(Func<int> operation)
    {
        int checksum = 0;
        int index = 0;

        while (index < Iterations)
        {
            checksum ^= operation();
            index += 1;
        }

        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();

        long before = GC.GetAllocatedBytesForCurrentThread();
        index = 0;

        while (index < Iterations)
        {
            checksum ^= operation();
            index += 1;
        }

        long allocated = GC.GetAllocatedBytesForCurrentThread() - before;
        GC.KeepAlive(checksum);
        return allocated;
    }

    static int InvokeByteSpanOperation(
        ByteSpanOperation operation,
        byte[] framed,
        int start,
        int length)
    {
        ReadOnlySpan<byte> input = framed.AsSpan(start, length);

        switch (operation)
        {
            case ByteSpanOperation.Base64UrlEncode:
                return Base64Url.Encode(input).Text.Length;
            case ByteSpanOperation.Base64Encode:
                return Base64Codec.Encode(input).Text.Length;
            case ByteSpanOperation.HexEncode:
                return HexCodec.Encode(input).Text.Length;
            case ByteSpanOperation.Utf8Decode:
                return Utf8Codec.Decode(input).Text.Length;
            default:
                throw new ArgumentOutOfRangeException(nameof(operation));
        }
    }

    static int InvokeByteArrayOperationWithCopy(
        ByteSpanOperation operation,
        byte[] framed,
        int start,
        int length)
    {
        byte[] input = framed.AsSpan(start, length).ToArray();

        switch (operation)
        {
            case ByteSpanOperation.Base64UrlEncode:
                return Base64Url.Encode(input).Text.Length;
            case ByteSpanOperation.Base64Encode:
                return Base64Codec.Encode(input).Text.Length;
            case ByteSpanOperation.HexEncode:
                return HexCodec.Encode(input).Text.Length;
            case ByteSpanOperation.Utf8Decode:
                return Utf8Codec.Decode(input).Text.Length;
            default:
                throw new ArgumentOutOfRangeException(nameof(operation));
        }
    }

    static int InvokeCharacterSpanOperation(
        CharacterSpanOperation operation,
        string framed,
        int start,
        int length)
    {
        ReadOnlySpan<char> input = framed.AsSpan(start, length);

        switch (operation)
        {
            case CharacterSpanOperation.Base64UrlDecode:
                return Base64Url.Decode(input).Bytes.Length;
            case CharacterSpanOperation.Base64Decode:
                return Base64Codec.Decode(input).Bytes.Length;
            case CharacterSpanOperation.HexDecode:
                return HexCodec.Decode(input).Bytes.Length;
            case CharacterSpanOperation.Utf8Encode:
                return Utf8Codec.Encode(input).Bytes.Length;
            default:
                throw new ArgumentOutOfRangeException(nameof(operation));
        }
    }

    static int InvokeStringOperationWithCopy(
        CharacterSpanOperation operation,
        string framed,
        int start,
        int length)
    {
        string input = framed.Substring(start, length);

        switch (operation)
        {
            case CharacterSpanOperation.Base64UrlDecode:
                return Base64Url.Decode(input).Bytes.Length;
            case CharacterSpanOperation.Base64Decode:
                return Base64Codec.Decode(input).Bytes.Length;
            case CharacterSpanOperation.HexDecode:
                return HexCodec.Decode(input).Bytes.Length;
            case CharacterSpanOperation.Utf8Encode:
                return Utf8Codec.Encode(input).Bytes.Length;
            default:
                throw new ArgumentOutOfRangeException(nameof(operation));
        }
    }

    public enum ByteSpanOperation
    {
        Base64UrlEncode,
        Base64Encode,
        HexEncode,
        Utf8Decode
    }

    public enum CharacterSpanOperation
    {
        Base64UrlDecode,
        Base64Decode,
        HexDecode,
        Utf8Encode
    }
}
