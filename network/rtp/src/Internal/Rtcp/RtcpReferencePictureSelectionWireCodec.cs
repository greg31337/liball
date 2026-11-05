using System;

namespace rtp;

internal static class RtcpReferencePictureSelectionWireCodec
{
    const int HeaderSize = 2;
    const int HeaderBitCount = 16;
    const int AlignmentBitCount = 32;
    const int PaddingBitCountMaximum = 31;
    const byte PayloadTypeMaximum = 127;

    internal static string? GetReferencePictureSelectionValidationError(
        RtcpReferencePictureSelectionIndication? indication)
    {
        if (indication == null)
        {
            return "The reference picture selection indication body is required.";
        }

        if (indication.PaddingBitCount > PaddingBitCountMaximum)
        {
            return "The RPSI padding-bit count must be between 0 and 31.";
        }

        if (indication.PayloadType > PayloadTypeMaximum)
        {
            return "The RPSI payload type must fit seven bits.";
        }

        if (indication.BitLength < 0)
        {
            return "The RPSI native bit-string length cannot be negative.";
        }

        if (indication.NativeBitString == null)
        {
            return "The RPSI native bit string is required.";
        }

        int nativeByteLength = GetNativeByteLength(indication.BitLength);
        if (indication.NativeBitString.Length != nativeByteLength)
        {
            return "The RPSI native byte length does not match its bit length.";
        }

        int requiredPaddingBitCount = GetRequiredPaddingBitCount(
            indication.BitLength);
        if (indication.PaddingBitCount != requiredPaddingBitCount)
        {
            return "The RPSI padding-bit count does not align the FCI to 32 bits.";
        }

        if (!AreUnusedNativeBitsZero(
            indication.NativeBitString,
            indication.BitLength))
        {
            return "Unused bits in the final RPSI native byte must be zero.";
        }

        return null;
    }

    internal static string? GetReferencePictureSelectionDecodeError(
        ReadOnlySpan<byte> source)
    {
        if (source.Length < RtcpProtocolConstants.LengthWordSize ||
            source.Length % RtcpProtocolConstants.LengthWordSize != 0)
        {
            return "An RPSI FCI must contain at least one complete 32-bit word.";
        }

        int paddingBitCount = source[0];
        if (paddingBitCount > PaddingBitCountMaximum)
        {
            return "The RPSI padding-bit count must be between 0 and 31.";
        }

        int bitLength = source.Length * 8 - HeaderBitCount - paddingBitCount;
        if (bitLength < 0)
        {
            return "The RPSI padding-bit count exceeds the available bit string.";
        }

        if (!AreWirePaddingBitsZero(source, paddingBitCount))
        {
            return "RPSI padding bits must be zero.";
        }

        return null;
    }

    internal static long CalculateReferencePictureSelectionSize(
        RtcpReferencePictureSelectionIndication indication)
    {
        return ((long)HeaderBitCount + indication.BitLength +
            indication.PaddingBitCount) / 8;
    }

    internal static RtcpReferencePictureSelectionIndication
        DecodeReferencePictureSelection(ReadOnlySpan<byte> source)
    {
        RtcpReferencePictureSelectionIndication indication =
            new RtcpReferencePictureSelectionIndication();
        indication.PaddingBitCount = source[0];
        indication.PayloadType = (byte)(source[1] & PayloadTypeMaximum);
        indication.BitLength = source.Length * 8 - HeaderBitCount -
            indication.PaddingBitCount;
        int nativeByteLength = GetNativeByteLength(indication.BitLength);
        indication.NativeBitString = source.Slice(
            HeaderSize,
            nativeByteLength).ToArray();
        return indication;
    }

    internal static void EncodeReferencePictureSelection(
        RtcpReferencePictureSelectionIndication indication,
        Span<byte> destination)
    {
        destination.Clear();
        destination[0] = indication.PaddingBitCount;
        destination[1] = indication.PayloadType;
        NetworkBinaryWriter.WriteBytes(
            destination,
            HeaderSize,
            indication.NativeBitString);
    }

    internal static int GetNativeByteLength(int bitLength)
    {
        int byteLength = bitLength / 8;
        if (bitLength % 8 != 0)
        {
            byteLength = byteLength + 1;
        }

        return byteLength;
    }

    internal static int GetRequiredPaddingBitCount(int bitLength)
    {
        int bitRemainder = (HeaderBitCount + bitLength % AlignmentBitCount) %
            AlignmentBitCount;
        if (bitRemainder == 0)
        {
            return 0;
        }

        return AlignmentBitCount - bitRemainder;
    }

    internal static bool AreUnusedNativeBitsZero(
        byte[] nativeBitString,
        int bitLength)
    {
        int unusedBitCount = nativeBitString.Length * 8 - bitLength;
        if (unusedBitCount == 0 || nativeBitString.Length == 0)
        {
            return true;
        }

        int unusedMask = (1 << unusedBitCount) - 1;
        return (nativeBitString[nativeBitString.Length - 1] & unusedMask) == 0;
    }

    internal static bool AreWirePaddingBitsZero(
        ReadOnlySpan<byte> source,
        int paddingBitCount)
    {
        int bitIndex = source.Length * 8 - paddingBitCount;
        int totalBitCount = source.Length * 8;
        while (bitIndex < totalBitCount)
        {
            int byteIndex = bitIndex / 8;
            int bitMask = 1 << (7 - bitIndex % 8);
            if ((source[byteIndex] & bitMask) != 0)
            {
                return false;
            }

            bitIndex = bitIndex + 1;
        }

        return true;
    }
}
