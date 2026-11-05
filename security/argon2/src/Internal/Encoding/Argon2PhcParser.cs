using System;

namespace argon2;

internal static class Argon2PhcParser
{
    const long InvalidDecimal = -1L;
    const long DecimalOverflow = -2L;

    internal static Argon2DecodeResult Parse(ReadOnlySpan<char> source)
    {
        Argon2DecodeResult result = new Argon2DecodeResult();
        if (source.Length == 0 || source[0] != '$')
        {
            return result;
        }

        int variantEnd = FindCharacter(source, 1, source.Length, '$');
        if (variantEnd <= 1)
        {
            return result;
        }

        Argon2Variant variant;
        if (MatchesText(source, 1, variantEnd, "argon2d"))
        {
            variant = Argon2Variant.D;
        }
        else if (MatchesText(source, 1, variantEnd, "argon2i"))
        {
            variant = Argon2Variant.I;
        }
        else if (MatchesText(source, 1, variantEnd, "argon2id"))
        {
            variant = Argon2Variant.Id;
        }
        else
        {
            result.Status = Argon2DecodeStatus.UnsupportedVariant;
            return result;
        }

        int firstFieldStart = variantEnd + 1;
        int firstFieldEnd = FindCharacter(source, firstFieldStart, source.Length, '$');
        if (firstFieldEnd < 0)
        {
            return result;
        }

        Argon2Version version = Argon2Version.Version10;
        int parameterStart = firstFieldStart;
        int parameterEnd = firstFieldEnd;
        if (firstFieldEnd - firstFieldStart >= 2
            && source[firstFieldStart] == 'v'
            && source[firstFieldStart + 1] == '=')
        {
            long versionValue = ParseDecimal(source, firstFieldStart + 2, firstFieldEnd);
            if (versionValue == InvalidDecimal)
            {
                return result;
            }

            if (versionValue == 16L)
            {
                version = Argon2Version.Version10;
            }
            else if (versionValue == 19L)
            {
                version = Argon2Version.Version13;
            }
            else
            {
                result.Status = Argon2DecodeStatus.UnsupportedVersion;
                return result;
            }

            parameterStart = firstFieldEnd + 1;
            parameterEnd = FindCharacter(source, parameterStart, source.Length, '$');
            if (parameterEnd < 0)
            {
                result.Status = Argon2DecodeStatus.InvalidFormat;
                return result;
            }
        }

        if (parameterEnd - parameterStart < 11
            || source[parameterStart] != 'm'
            || source[parameterStart + 1] != '=')
        {
            return result;
        }

        int memoryEnd = FindCharacter(source, parameterStart + 2, parameterEnd, ',');
        if (memoryEnd < 0
            || memoryEnd + 3 >= parameterEnd
            || source[memoryEnd + 1] != 't'
            || source[memoryEnd + 2] != '=')
        {
            return result;
        }

        int iterationsEnd = FindCharacter(source, memoryEnd + 3, parameterEnd, ',');
        if (iterationsEnd < 0
            || iterationsEnd + 3 >= parameterEnd
            || source[iterationsEnd + 1] != 'p'
            || source[iterationsEnd + 2] != '=')
        {
            return result;
        }

        if (FindCharacter(source, iterationsEnd + 3, parameterEnd, ',') >= 0)
        {
            return result;
        }

        long memorySizeKiB = ParseDecimal(source, parameterStart + 2, memoryEnd);
        long iterations = ParseDecimal(source, memoryEnd + 3, iterationsEnd);
        long parallelism = ParseDecimal(source, iterationsEnd + 3, parameterEnd);
        if (memorySizeKiB == InvalidDecimal
            || iterations == InvalidDecimal
            || parallelism == InvalidDecimal)
        {
            return result;
        }

        if (memorySizeKiB == DecimalOverflow
            || iterations == DecimalOverflow
            || parallelism == DecimalOverflow
            || iterations < Argon2Limits.MinimumIterations
            || iterations > Argon2Limits.MaximumIterations
            || parallelism < Argon2Limits.MinimumDegreeOfParallelism
            || parallelism > Argon2Limits.MaximumDegreeOfParallelism
            || memorySizeKiB < Argon2Limits.MinimumMemorySizePerLaneKiB * parallelism
            || memorySizeKiB > Argon2Limits.MaximumMemorySizeKiB)
        {
            result.Status = Argon2DecodeStatus.InvalidParameters;
            return result;
        }

        int saltStart = parameterEnd + 1;
        int saltEnd = FindCharacter(source, saltStart, source.Length, '$');
        if (saltEnd < 0)
        {
            result.Status = Argon2DecodeStatus.InvalidFormat;
            return result;
        }

        int tagStart = saltEnd + 1;
        if (FindCharacter(source, tagStart, source.Length, '$') >= 0)
        {
            result.Status = Argon2DecodeStatus.InvalidFormat;
            return result;
        }

        int saltLength = Argon2Base64.GetDecodedLengthWithoutException(saltEnd - saltStart);
        int tagLength = Argon2Base64.GetDecodedLengthWithoutException(source.Length - tagStart);
        if (saltLength < 0 || tagLength < 0)
        {
            result.Status = Argon2DecodeStatus.InvalidBase64;
            return result;
        }

        ReadOnlySpan<char> encodedSalt = source.Slice(saltStart, saltEnd - saltStart);
        ReadOnlySpan<char> encodedTag = source.Slice(tagStart);
        if (!Argon2Base64.IsCanonical(encodedSalt)
            || !Argon2Base64.IsCanonical(encodedTag))
        {
            result.Status = Argon2DecodeStatus.InvalidBase64;
            return result;
        }

        if (saltLength < Argon2Limits.MinimumSaltLength
            || tagLength < Argon2Limits.MinimumTagLength)
        {
            result.Status = Argon2DecodeStatus.InvalidParameters;
            return result;
        }

        byte[] salt = new byte[saltLength];
        byte[] tag = new byte[tagLength];
        if (!Argon2Base64.DecodeWithoutException(encodedSalt, salt)
            || !Argon2Base64.DecodeWithoutException(encodedTag, tag))
        {
            result.Status = Argon2DecodeStatus.InvalidBase64;
            return result;
        }

        Argon2Parameters parameters = new Argon2Parameters();
        parameters.Variant = variant;
        parameters.Version = version;
        parameters.Iterations = iterations;
        parameters.MemorySizeKiB = memorySizeKiB;
        parameters.DegreeOfParallelism = (int)parallelism;

        Argon2EncodedHash encodedHash = new Argon2EncodedHash();
        encodedHash.Parameters = parameters;
        encodedHash.Salt = salt;
        encodedHash.Tag = tag;
        result.Status = Argon2DecodeStatus.Success;
        result.EncodedHash = encodedHash;
        return result;
    }

    static int FindCharacter(
        ReadOnlySpan<char> source,
        int startIndex,
        int endIndex,
        char character)
    {
        int index = startIndex;
        while (index < endIndex)
        {
            if (source[index] == character)
            {
                return index;
            }

            index = index + 1;
        }

        return -1;
    }

    static bool MatchesText(
        ReadOnlySpan<char> source,
        int startIndex,
        int endIndex,
        string expected)
    {
        if (endIndex - startIndex != expected.Length)
        {
            return false;
        }

        int index = 0;
        while (index < expected.Length)
        {
            if (source[startIndex + index] != expected[index])
            {
                return false;
            }

            index = index + 1;
        }

        return true;
    }

    static long ParseDecimal(
        ReadOnlySpan<char> source,
        int startIndex,
        int endIndex)
    {
        if (startIndex >= endIndex)
        {
            return InvalidDecimal;
        }

        if (source[startIndex] == '0' && endIndex - startIndex != 1)
        {
            return InvalidDecimal;
        }

        long value = 0L;
        int index = startIndex;
        while (index < endIndex)
        {
            char character = source[index];
            if (character < '0' || character > '9')
            {
                return InvalidDecimal;
            }

            int digit = character - '0';
            if (value > (Argon2Limits.MaximumIterations - digit) / 10L)
            {
                return DecimalOverflow;
            }

            value = value * 10L + digit;
            index = index + 1;
        }

        return value;
    }
}
