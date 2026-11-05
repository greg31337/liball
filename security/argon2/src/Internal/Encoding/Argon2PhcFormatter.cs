using System;

namespace argon2;

internal static class Argon2PhcFormatter
{
    internal static int GetEncodedLength(Argon2EncodedHash hash)
    {
        Argon2ParameterValidator.ValidateEncodedHash(hash);

        return GetEncodedLengthForComponents(
            hash.Parameters,
            hash.Salt.Length,
            hash.Tag.Length);
    }

    internal static int GetEncodedLengthForComponents(
        Argon2Parameters parameters,
        int saltLength,
        int tagLength)
    {
        Argon2ParameterValidator.ValidateParameters(parameters);
        if (saltLength < Argon2Limits.MinimumSaltLength || saltLength > Array.MaxLength)
        {
            throw new Argon2ParameterException("PHC salt length is outside the supported range.");
        }

        if (tagLength < Argon2Limits.MinimumTagLength || tagLength > Array.MaxLength)
        {
            throw new Argon2ParameterException("PHC tag length is outside the supported range.");
        }

        long encodedLength = 15L;
        encodedLength = encodedLength + GetVariantLength(parameters.Variant);
        encodedLength = encodedLength + GetDecimalLength(GetVersionNumber(parameters.Version));
        encodedLength = encodedLength + GetDecimalLength(parameters.MemorySizeKiB);
        encodedLength = encodedLength + GetDecimalLength(parameters.Iterations);
        encodedLength = encodedLength + GetDecimalLength(parameters.DegreeOfParallelism);
        encodedLength = encodedLength + Argon2Base64.GetEncodedLength(saltLength);
        encodedLength = encodedLength + Argon2Base64.GetEncodedLength(tagLength);
        return Argon2ParameterValidator.GetManagedArrayLength(encodedLength);
    }

    internal static string Format(Argon2EncodedHash hash)
    {
        int encodedLength = GetEncodedLength(hash);
        Argon2Parameters parameters = hash.Parameters;
        char[] destination = new char[encodedLength];
        int index = 0;

        destination[index] = '$';
        index = WriteVariant(destination, index + 1, parameters.Variant);
        index = WriteText(destination, index, "$v=");
        index = WriteDecimal(destination, index, GetVersionNumber(parameters.Version));
        index = WriteText(destination, index, "$m=");
        index = WriteDecimal(destination, index, parameters.MemorySizeKiB);
        index = WriteText(destination, index, ",t=");
        index = WriteDecimal(destination, index, parameters.Iterations);
        index = WriteText(destination, index, ",p=");
        index = WriteDecimal(destination, index, parameters.DegreeOfParallelism);
        destination[index] = '$';
        index = index + 1;

        int encodedSaltLength = Argon2Base64.GetEncodedLength(hash.Salt.Length);
        Argon2Base64.Encode(hash.Salt, destination.AsSpan(index, encodedSaltLength));
        index = index + encodedSaltLength;
        destination[index] = '$';
        index = index + 1;

        int encodedTagLength = Argon2Base64.GetEncodedLength(hash.Tag.Length);
        Argon2Base64.Encode(hash.Tag, destination.AsSpan(index, encodedTagLength));
        return new string(destination);
    }

    static int GetVariantLength(Argon2Variant variant)
    {
        if (variant == Argon2Variant.Id)
        {
            return 8;
        }

        return 7;
    }

    static long GetVersionNumber(Argon2Version version)
    {
        if (version == Argon2Version.Version10)
        {
            return 16L;
        }

        return 19L;
    }

    static int GetDecimalLength(long value)
    {
        int length = 1;
        while (value >= 10L)
        {
            value = value / 10L;
            length = length + 1;
        }

        return length;
    }

    static int WriteVariant(
        Span<char> destination,
        int startIndex,
        Argon2Variant variant)
    {
        if (variant == Argon2Variant.D)
        {
            return WriteText(destination, startIndex, "argon2d");
        }

        if (variant == Argon2Variant.I)
        {
            return WriteText(destination, startIndex, "argon2i");
        }

        return WriteText(destination, startIndex, "argon2id");
    }

    static int WriteText(
        Span<char> destination,
        int startIndex,
        string text)
    {
        int index = 0;
        while (index < text.Length)
        {
            destination[startIndex + index] = text[index];
            index = index + 1;
        }

        return startIndex + text.Length;
    }

    static int WriteDecimal(
        Span<char> destination,
        int startIndex,
        long value)
    {
        int length = GetDecimalLength(value);
        int index = startIndex + length - 1;
        while (index >= startIndex)
        {
            destination[index] = (char)('0' + value % 10L);
            value = value / 10L;
            index = index - 1;
        }

        return startIndex + length;
    }
}
