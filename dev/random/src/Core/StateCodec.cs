using System;
using System.Security.Cryptography;

namespace random;

internal class StateCodec
{
    internal const int FormatVersion = 1;
    internal const int CurrentTransformVersion = 1;
    internal const int HeaderSize = 28;
    internal const int DigestSize = 32;
    internal const int MinimumEnvelopeSize = HeaderSize + DigestSize;
    const ulong Magic = 0x4554415453444E52UL;

    internal static RandomState Create(RandomAlgorithm algorithm, int algorithmVersion, int transformVersion, byte[] payload)
    {
        Guard.Argument(IsDeterministicAlgorithm(algorithm), "algorithm", "State is available only for a named deterministic algorithm.");
        Guard.Argument(algorithmVersion == 1, "algorithmVersion", "The algorithm version is not supported.");
        Guard.Argument(transformVersion == CurrentTransformVersion, "transformVersion", "The transform version is not supported.");
        Guard.NotNull(payload, "payload");
        Guard.Argument(payload.Length > 0, "payload", "The state payload must not be empty.");
        return new RandomState(algorithm, algorithmVersion, transformVersion, payload);
    }

    internal static byte[] Encode(RandomAlgorithm algorithm, int algorithmVersion, int transformVersion, byte[] payload)
    {
        if (payload.Length > int.MaxValue - MinimumEnvelopeSize)
        {
            throw RandomStateException.Create("The state payload is too large to encode.");
        }

        int digestOffset = HeaderSize + payload.Length;
        byte[] encoded = new byte[digestOffset + DigestSize];
        int offset = 0;
        offset = BinaryCodec.WriteUInt64(encoded, offset, Magic);
        offset = BinaryCodec.WriteUInt32(encoded, offset, (uint)FormatVersion);
        offset = BinaryCodec.WriteUInt32(encoded, offset, (uint)algorithm);
        offset = BinaryCodec.WriteUInt32(encoded, offset, (uint)algorithmVersion);
        offset = BinaryCodec.WriteUInt32(encoded, offset, (uint)transformVersion);
        offset = BinaryCodec.WriteUInt32(encoded, offset, (uint)payload.Length);
        offset = BinaryCodec.WriteBytes(encoded, offset, payload);

        byte[] digest = SHA256.HashData(encoded.AsSpan(0, digestOffset));
        BinaryCodec.WriteBytes(encoded, offset, digest);
        return encoded;
    }

    internal static RandomState Decode(byte[] encoded)
    {
        Guard.NotNull(encoded, "encoded");
        if (encoded.Length < MinimumEnvelopeSize)
        {
            throw RandomStateException.Create("The state envelope is truncated.");
        }

        ulong magic = BinaryCodec.ReadUInt64(encoded, 0);
        if (magic != Magic)
        {
            throw RandomStateException.Create("The state envelope magic is invalid.");
        }

        uint payloadLengthValue = BinaryCodec.ReadUInt32(encoded, 24);
        if (payloadLengthValue > int.MaxValue)
        {
            throw RandomStateException.Create("The state payload length is too large.");
        }

        int payloadLength = (int)payloadLengthValue;
        int availablePayloadLength = encoded.Length - MinimumEnvelopeSize;
        if (payloadLength != availablePayloadLength)
        {
            throw RandomStateException.Create("The state payload length does not match the envelope length.");
        }

        int digestOffset = HeaderSize + payloadLength;
        byte[] expectedDigest = SHA256.HashData(encoded.AsSpan(0, digestOffset));
        byte[] actualDigest = BinaryCodec.ReadBytes(encoded, digestOffset, DigestSize);
        if (!BitMath.ConstantTimeEquals(expectedDigest, actualDigest))
        {
            throw RandomStateException.Create("The state integrity digest does not match.");
        }

        uint formatVersion = BinaryCodec.ReadUInt32(encoded, 8);
        if (formatVersion != (uint)FormatVersion)
        {
            throw RandomStateException.Create("The state format version is not supported.");
        }

        uint algorithmValue = BinaryCodec.ReadUInt32(encoded, 12);
        RandomAlgorithm algorithm = (RandomAlgorithm)algorithmValue;
        if (!IsDeterministicAlgorithm(algorithm))
        {
            throw RandomStateException.Create("The state algorithm is unknown or is not deterministic.");
        }

        uint algorithmVersionValue = BinaryCodec.ReadUInt32(encoded, 16);
        if (algorithmVersionValue != 1U)
        {
            throw RandomStateException.Create("The state algorithm version is not supported.");
        }

        uint transformVersionValue = BinaryCodec.ReadUInt32(encoded, 20);
        if (transformVersionValue != (uint)CurrentTransformVersion)
        {
            throw RandomStateException.Create("The state transform version is not supported.");
        }

        if (payloadLength == 0)
        {
            throw RandomStateException.Create("The state payload is empty.");
        }

        byte[] payload = BinaryCodec.ReadBytes(encoded, HeaderSize, payloadLength);
        return new RandomState(algorithm, (int)algorithmVersionValue, (int)transformVersionValue, payload);
    }

    internal static RandomState DecodeBase64(string encoded)
    {
        Guard.NotNull(encoded, "encoded");
        ValidateBase64(encoded);
        byte[] bytes = Convert.FromBase64String(encoded);
        return Decode(bytes);
    }

    internal static bool IsDeterministicAlgorithm(RandomAlgorithm algorithm)
    {
        switch (algorithm)
        {
            case RandomAlgorithm.Pcg64DxsmV1:
            case RandomAlgorithm.Xoshiro256StarStarV1:
            case RandomAlgorithm.Philox4x64TenV1:
            case RandomAlgorithm.Mt19937V1:
            case RandomAlgorithm.Mt19937NumpyV1:
            case RandomAlgorithm.SplitMix64V1:
                return true;
            default:
                return false;
        }
    }

    internal static void ValidateBase64(string encoded)
    {
        if (encoded.Length == 0 || encoded.Length % 4 != 0)
        {
            throw RandomStateException.Create("The state Base64 text has an invalid length.");
        }

        int padding = 0;
        if (encoded[encoded.Length - 1] == '=')
        {
            padding++;
        }

        if (encoded[encoded.Length - 2] == '=')
        {
            padding++;
        }

        int dataLength = encoded.Length - padding;
        int index = 0;
        while (index < dataLength)
        {
            if (!IsBase64Character(encoded[index]))
            {
                throw RandomStateException.Create("The state Base64 text contains an invalid character.");
            }

            index++;
        }

        while (index < encoded.Length)
        {
            if (encoded[index] != '=')
            {
                throw RandomStateException.Create("The state Base64 padding is invalid.");
            }

            index++;
        }
    }

    internal static bool IsBase64Character(char value)
    {
        if (value >= 'A' && value <= 'Z')
        {
            return true;
        }

        if (value >= 'a' && value <= 'z')
        {
            return true;
        }

        if (value >= '0' && value <= '9')
        {
            return true;
        }

        return value == '+' || value == '/';
    }
}
