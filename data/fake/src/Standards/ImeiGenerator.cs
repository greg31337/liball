using System;

namespace fake;

internal static class ImeiGenerator
{
    internal const string Ts06Version = "28.1";
    internal const int ImeiLength = 15;
    internal const int ImeiSvLength = 16;
    internal const int TacLength = 8;
    internal const int SerialLength = 6;
    internal const int MaximumSoftwareVersion = 98;

    internal static string GenerateImei(IRandomSource random, int maximumCount, int maximumOutputSize)
    {
        ValidateGeneration(ImeiLength, 14, maximumCount, maximumOutputSize);
        char[] destination = new char[ImeiLength];
        FillDigits(random, destination, 14);
        ApplyCheckDigit(destination, maximumCount);
        return new string(destination);
    }

    internal static string GenerateImei(IRandomSource random, ReadOnlySpan<char> tac, int maximumCount, int maximumOutputSize)
    {
        RequireDigits(tac, TacLength, "tac");
        ValidateGeneration(ImeiLength, 14, maximumCount, maximumOutputSize);
        char[] destination = new char[ImeiLength];
        tac.CopyTo(destination);
        FillDigits(random, new Span<char>(destination).Slice(TacLength, SerialLength), SerialLength);
        ApplyCheckDigit(destination, maximumCount);
        return new string(destination);
    }

    internal static string ComposeImei(ReadOnlySpan<char> tac, ReadOnlySpan<char> serialNumber, int maximumCount, int maximumOutputSize)
    {
        RequireDigits(tac, TacLength, "tac");
        RequireDigits(serialNumber, SerialLength, "serialNumber");
        ValidateGeneration(ImeiLength, 14, maximumCount, maximumOutputSize);
        char[] destination = new char[ImeiLength];
        tac.CopyTo(destination);
        serialNumber.CopyTo(new Span<char>(destination).Slice(TacLength, SerialLength));
        ApplyCheckDigit(destination, maximumCount);
        return new string(destination);
    }

    internal static int WriteImei(IRandomSource random, Span<char> destination, int maximumCount, int maximumOutputSize)
    {
        ValidateGeneration(ImeiLength, 14, maximumCount, maximumOutputSize);
        RequireDestination(destination.Length, ImeiLength);
        FillDigits(random, destination.Slice(0, 14), 14);
        ApplyCheckDigit(destination.Slice(0, ImeiLength), maximumCount);
        return ImeiLength;
    }

    internal static int WriteImei(ReadOnlySpan<char> tac, ReadOnlySpan<char> serialNumber, Span<char> destination, int maximumCount, int maximumOutputSize)
    {
        RequireDigits(tac, TacLength, "tac");
        RequireDigits(serialNumber, SerialLength, "serialNumber");
        ValidateGeneration(ImeiLength, 14, maximumCount, maximumOutputSize);
        RequireDestination(destination.Length, ImeiLength);
        Span<char> target = destination.Slice(0, ImeiLength);
        tac.CopyTo(target);
        serialNumber.CopyTo(target.Slice(TacLength, SerialLength));
        ApplyCheckDigit(target, maximumCount);
        return ImeiLength;
    }

    internal static bool ValidateImei(ReadOnlySpan<char> value, int maximumCount)
    {
        if (value.Length != ImeiLength)
        {
            return false;
        }
        return ChecksumAlgorithms.IsLuhn(value, maximumCount);
    }

    internal static string GenerateImeiSv(IRandomSource random, int softwareVersion, int maximumCount, int maximumOutputSize)
    {
        ValidateSoftwareVersion(softwareVersion);
        ValidateGeneration(ImeiSvLength, ImeiSvLength, maximumCount, maximumOutputSize);
        char[] destination = new char[ImeiSvLength];
        FillDigits(random, destination, 14);
        WriteSoftwareVersion(destination, softwareVersion);
        return new string(destination);
    }

    internal static string GenerateImeiSv(IRandomSource random, int maximumCount, int maximumOutputSize)
    {
        ValidateGeneration(ImeiSvLength, ImeiSvLength, maximumCount, maximumOutputSize);
        char[] destination = new char[ImeiSvLength];
        FillDigits(random, destination, 14);
        WriteSoftwareVersion(destination, random.IntFromMaximumExclusive(MaximumSoftwareVersion + 1));
        return new string(destination);
    }

    internal static string ComposeImeiSv(ReadOnlySpan<char> tac, ReadOnlySpan<char> serialNumber, int softwareVersion, int maximumCount, int maximumOutputSize)
    {
        RequireDigits(tac, TacLength, "tac");
        RequireDigits(serialNumber, SerialLength, "serialNumber");
        ValidateSoftwareVersion(softwareVersion);
        ValidateGeneration(ImeiSvLength, ImeiSvLength, maximumCount, maximumOutputSize);
        char[] destination = new char[ImeiSvLength];
        tac.CopyTo(destination);
        serialNumber.CopyTo(new Span<char>(destination).Slice(TacLength, SerialLength));
        WriteSoftwareVersion(destination, softwareVersion);
        return new string(destination);
    }

    internal static string ImeiSvFromImei(ReadOnlySpan<char> imei, int softwareVersion, int maximumCount, int maximumOutputSize)
    {
        RequireImei(imei, maximumCount);
        ValidateSoftwareVersion(softwareVersion);
        ValidateGeneration(ImeiSvLength, ImeiSvLength, maximumCount, maximumOutputSize);
        char[] destination = new char[ImeiSvLength];
        imei.Slice(0, 14).CopyTo(destination);
        WriteSoftwareVersion(destination, softwareVersion);
        return new string(destination);
    }

    internal static int WriteImeiSv(IRandomSource random, Span<char> destination, int softwareVersion, int maximumCount, int maximumOutputSize)
    {
        ValidateSoftwareVersion(softwareVersion);
        ValidateGeneration(ImeiSvLength, ImeiSvLength, maximumCount, maximumOutputSize);
        RequireDestination(destination.Length, ImeiSvLength);
        Span<char> target = destination.Slice(0, ImeiSvLength);
        FillDigits(random, target.Slice(0, 14), 14);
        WriteSoftwareVersion(target, softwareVersion);
        return ImeiSvLength;
    }

    internal static int WriteImeiSv(IRandomSource random, Span<char> destination, int maximumCount, int maximumOutputSize)
    {
        ValidateGeneration(ImeiSvLength, ImeiSvLength, maximumCount, maximumOutputSize);
        RequireDestination(destination.Length, ImeiSvLength);
        Span<char> target = destination.Slice(0, ImeiSvLength);
        FillDigits(random, target.Slice(0, 14), 14);
        WriteSoftwareVersion(target, random.IntFromMaximumExclusive(MaximumSoftwareVersion + 1));
        return ImeiSvLength;
    }

    internal static bool ValidateImeiSv(ReadOnlySpan<char> value, int maximumCount)
    {
        if (value.Length != ImeiSvLength)
        {
            return false;
        }
        Guard.Count(value.Length, maximumCount, "value");
        if (!Digits(value))
        {
            return false;
        }
        return SoftwareVersionUnchecked(value) <= MaximumSoftwareVersion;
    }

    internal static string ImeiFromImeiSv(ReadOnlySpan<char> value, int maximumCount, int maximumOutputSize)
    {
        RequireImeiSv(value, maximumCount);
        ValidateGeneration(ImeiLength, 14, maximumCount, maximumOutputSize);
        char[] destination = new char[ImeiLength];
        value.Slice(0, 14).CopyTo(destination);
        ApplyCheckDigit(destination, maximumCount);
        return new string(destination);
    }

    internal static string Tac(ReadOnlySpan<char> value, bool imeiSv, int maximumCount, int maximumOutputSize)
    {
        RequireIdentifier(value, imeiSv, maximumCount);
        Guard.OutputSize(TacLength, maximumOutputSize, "value");
        return new string(value.Slice(0, TacLength));
    }

    internal static string SerialNumber(ReadOnlySpan<char> value, bool imeiSv, int maximumCount, int maximumOutputSize)
    {
        RequireIdentifier(value, imeiSv, maximumCount);
        Guard.OutputSize(SerialLength, maximumOutputSize, "value");
        return new string(value.Slice(TacLength, SerialLength));
    }

    internal static int SoftwareVersion(ReadOnlySpan<char> value, int maximumCount)
    {
        RequireImeiSv(value, maximumCount);
        return SoftwareVersionUnchecked(value);
    }

    internal static string GenerateInternationalTestShape(IRandomSource random, int maximumCount, int maximumOutputSize)
    {
        ValidateGeneration(ImeiLength, 14, maximumCount, maximumOutputSize);
        char[] destination = new char[ImeiLength];
        "0044".AsSpan().CopyTo(destination);
        FillDigits(random, new Span<char>(destination).Slice(4, 10), 10);
        ApplyCheckDigit(destination, maximumCount);
        return new string(destination);
    }

    internal static string GenerateNorthAmericanTestShape(IRandomSource random, int maximumCount, int maximumOutputSize)
    {
        ValidateGeneration(ImeiLength, 14, maximumCount, maximumOutputSize);
        char[] destination = new char[ImeiLength];
        "001".AsSpan().CopyTo(destination);
        FillDigits(random, new Span<char>(destination).Slice(3, 3), 3);
        destination[6] = '0';
        destination[7] = '0';
        FillDigits(random, new Span<char>(destination).Slice(8, 6), 6);
        ApplyCheckDigit(destination, maximumCount);
        return new string(destination);
    }

    internal static bool InternationalTestShape(ReadOnlySpan<char> value, int maximumCount)
    {
        if (!ValidateImei(value, maximumCount))
        {
            return false;
        }
        return value[0] == '0' && value[1] == '0' && value[2] == '4' && value[3] == '4';
    }

    internal static bool NorthAmericanTestShape(ReadOnlySpan<char> value, int maximumCount)
    {
        if (!ValidateImei(value, maximumCount))
        {
            return false;
        }
        return value[0] == '0' && value[1] == '0' && value[2] == '1' && value[6] == '0' && value[7] == '0';
    }

    static void ApplyCheckDigit(Span<char> value, int maximumCount)
    {
        int check = ChecksumAlgorithms.Luhn(value.Slice(0, 14), maximumCount);
        value[14] = (char)('0' + check);
    }

    static void WriteSoftwareVersion(Span<char> value, int softwareVersion)
    {
        value[14] = (char)('0' + (softwareVersion / 10));
        value[15] = (char)('0' + (softwareVersion % 10));
    }

    static int SoftwareVersionUnchecked(ReadOnlySpan<char> value)
    {
        return ((value[14] - '0') * 10) + value[15] - '0';
    }

    static void FillDigits(IRandomSource random, Span<char> destination, int count)
    {
        int index = 0;
        while (index < count)
        {
            destination[index] = (char)('0' + random.IntFromMaximumExclusive(10));
            index++;
        }
    }

    static void ValidateGeneration(int outputLength, int digitCount, int maximumCount, int maximumOutputSize)
    {
        Guard.Count(digitCount, maximumCount, "value");
        Guard.OutputSize(outputLength, maximumOutputSize, "value");
    }

    static void ValidateSoftwareVersion(int softwareVersion)
    {
        if (softwareVersion < 0 || softwareVersion > MaximumSoftwareVersion)
        {
            throw new ArgumentOutOfRangeException("softwareVersion", softwareVersion, "softwareVersion must be between 0 and 98; 99 is reserved.");
        }
    }

    static void RequireDigits(ReadOnlySpan<char> value, int length, string parameterName)
    {
        if (value.Length != length || !Digits(value))
        {
            throw new ArgumentException($"{parameterName} must contain exactly {length} ASCII decimal digits.", parameterName);
        }
    }

    static bool Digits(ReadOnlySpan<char> value)
    {
        int index = 0;
        while (index < value.Length)
        {
            if (value[index] < '0' || value[index] > '9')
            {
                return false;
            }
            index++;
        }
        return true;
    }

    static void RequireImei(ReadOnlySpan<char> value, int maximumCount)
    {
        if (!ValidateImei(value, maximumCount))
        {
            throw new ArgumentException("value must be a 15-digit IMEI shape with a valid Luhn check digit.", "value");
        }
    }

    static void RequireImeiSv(ReadOnlySpan<char> value, int maximumCount)
    {
        if (!ValidateImeiSv(value, maximumCount))
        {
            throw new ArgumentException("value must be a 16-digit IMEISV shape with software version 00 through 98.", "value");
        }
    }

    static void RequireIdentifier(ReadOnlySpan<char> value, bool imeiSv, int maximumCount)
    {
        if (imeiSv)
        {
            RequireImeiSv(value, maximumCount);
        }
        else
        {
            RequireImei(value, maximumCount);
        }
    }

    static void RequireDestination(int actualLength, int requiredLength)
    {
        if (actualLength < requiredLength)
        {
            throw new ArgumentException($"destination must contain at least {requiredLength} elements.", "destination");
        }
    }
}
