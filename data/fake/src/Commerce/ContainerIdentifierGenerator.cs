using System;
using System.Globalization;

namespace fake;

internal static class ContainerIdentifierGenerator
{
    const string Marker = "fake.synthetic-container.v1";
    static readonly string[] OwnerCodes = new string[]
    {
        "QXZ", "VQX", "XQZ", "ZXQ"
    };
    static readonly string[] EquipmentCategories = new string[]
    {
        "U", "J", "Z"
    };

    internal static string Generate(
        GenerationContext context,
        FakeRandom random)
    {
        ValidateLimits(context);
        string owner = OwnerCodes[random.IntFromMaximumExclusive(OwnerCodes.Length)];
        string category = EquipmentCategories[
            random.IntFromMaximumExclusive(EquipmentCategories.Length)];
        string serial = NumericStringGenerator.Digits(
            random,
            6,
            FakeLimits.DefaultMaximumOutputSize);
        string body = owner + category + serial;
        return body + CalculateCheckDigit(body).ToString(CultureInfo.InvariantCulture);
    }

    internal static bool IsValid(
        GenerationContext context,
        string value)
    {
        Guard.NotNull(value, "value");
        Guard.Count(value.Length, context.CapturedOptions.MaximumCount, "value");
        if (!HasStructure(value))
        {
            return false;
        }
        return value[10] - '0' == CalculateCheckDigit(value.Substring(0, 10));
    }

    internal static ContainerIdentifierRecord Record(
        GenerationContext context,
        FakeRandom random)
    {
        string identifier = Generate(context, random);
        return CreateRecord(context, identifier);
    }

    internal static ContainerIdentifierRecord RecordFromSource(
        GenerationContext context,
        string source)
    {
        Guard.NotNullOrWhiteSpace(source, "source");
        ValidateLimits(context);
        ulong hash = 14695981039346656037UL;
        int index = 0;
        while (index < source.Length)
        {
            hash = hash ^ source[index];
            hash = WrappingArithmetic.Multiply(hash, 1099511628211UL);
            index = index + 1;
        }
        string owner = OwnerCodes[(int)(hash % (ulong)OwnerCodes.Length)];
        string category = EquipmentCategories[
            (int)((hash >> 8) % (ulong)EquipmentCategories.Length)];
        string serial = ((int)((hash >> 16) % 1000000UL)).ToString(
            "D6",
            CultureInfo.InvariantCulture);
        string body = owner + category + serial;
        string identifier = body + CalculateCheckDigit(body).ToString(
            CultureInfo.InvariantCulture);
        return CreateRecord(context, identifier);
    }

    static ContainerIdentifierRecord CreateRecord(
        GenerationContext context,
        string identifier)
    {
        ContainerIdentifierRecord result = new ContainerIdentifierRecord();
        result.Identifier = identifier;
        result.OwnerCode = identifier.Substring(0, 3);
        result.EquipmentCategory = identifier.Substring(3, 1);
        result.SerialNumber = identifier.Substring(4, 6);
        result.CheckDigit = identifier[10] - '0';
        result.IsCheckDigitValid = true;
        result.IsSynthetic = true;
        result.TestMarker = Marker;
        result.RepresentsRegisteredContainer = false;
        Guard.OutputSize(
            identifier.Length + result.OwnerCode.Length +
                result.EquipmentCategory.Length + result.SerialNumber.Length +
                result.TestMarker.Length,
            context.CapturedOptions.MaximumOutputSize,
            "containerIdentifierRecord");
        return result;
    }

    static void ValidateLimits(GenerationContext context)
    {
        Guard.Count(11, context.CapturedOptions.MaximumCount, "containerIdentifier");
        Guard.OutputSize(
            11,
            context.CapturedOptions.MaximumOutputSize,
            "containerIdentifier");
    }

    static bool HasStructure(string value)
    {
        if (value.Length != 11)
        {
            return false;
        }
        int index = 0;
        while (index < 3)
        {
            if (value[index] < 'A' || value[index] > 'Z')
            {
                return false;
            }
            index = index + 1;
        }
        if (value[3] != 'U' && value[3] != 'J' && value[3] != 'Z')
        {
            return false;
        }
        index = 4;
        while (index < 11)
        {
            if (value[index] < '0' || value[index] > '9')
            {
                return false;
            }
            index = index + 1;
        }
        return true;
    }

    static int CalculateCheckDigit(string body)
    {
        int sum = 0;
        int weight = 1;
        int index = 0;
        while (index < body.Length)
        {
            int value = body[index] >= '0' && body[index] <= '9'
                ? body[index] - '0'
                : LetterValue(body[index]);
            sum = sum + value * weight;
            weight = weight * 2;
            index = index + 1;
        }
        int remainder = sum % 11;
        return remainder == 10 ? 0 : remainder;
    }

    static int LetterValue(char value)
    {
        if (value < 'A' || value > 'Z')
        {
            throw new ArgumentException(
                "Container owner and category characters must be uppercase ASCII letters.",
                "value");
        }
        int result = value - 'A' + 10;
        if (result >= 11)
        {
            result = result + 1;
        }
        if (result >= 22)
        {
            result = result + 1;
        }
        if (result >= 33)
        {
            result = result + 1;
        }
        return result;
    }
}
