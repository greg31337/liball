namespace png;

internal class PngTextKeywordValidator
{
    internal const int MinimumKeywordLength = 1;
    internal const int MaximumKeywordLength = 79;
    internal const int Space = 32;
    internal const int Latin1PrintableMinimum = 32;
    internal const int Latin1PrintableMaximum = 126;
    internal const int Latin1SupplementPrintableMinimum = 161;
    internal const int Latin1SupplementPrintableMaximum = 255;

    internal static bool IsValid(string keyword)
    {
        ArgumentNullException.ThrowIfNull(keyword);

        if (keyword.Length < MinimumKeywordLength || keyword.Length > MaximumKeywordLength)
        {
            return false;
        }

        int previous = -1;
        int index = 0;
        while (index < keyword.Length)
        {
            int value = keyword[index];
            if (!IsAllowedKeywordValue(value))
            {
                return false;
            }

            if (IsInvalidSpace(value, previous, index, keyword.Length))
            {
                return false;
            }

            previous = value;
            index += 1;
        }

        return true;
    }

    internal static bool IsValidBytes(byte[] data, int offset, int length)
    {
        ArgumentNullException.ThrowIfNull(data);
        ValidateRange(data, offset, length);

        if (length < MinimumKeywordLength || length > MaximumKeywordLength)
        {
            return false;
        }

        int previous = -1;
        int index = 0;
        while (index < length)
        {
            int value = data[offset + index];
            if (!IsAllowedKeywordValue(value))
            {
                return false;
            }

            if (IsInvalidSpace(value, previous, index, length))
            {
                return false;
            }

            previous = value;
            index += 1;
        }

        return true;
    }

    internal static void Validate(string keyword)
    {
        ArgumentNullException.ThrowIfNull(keyword);

        if (!IsValid(keyword))
        {
            throw new ArgumentOutOfRangeException("Keyword");
        }
    }

    internal static bool IsAllowedKeywordValue(int value)
    {
        if (value >= Latin1PrintableMinimum && value <= Latin1PrintableMaximum)
        {
            return true;
        }

        if (value >= Latin1SupplementPrintableMinimum && value <= Latin1SupplementPrintableMaximum)
        {
            return true;
        }

        return false;
    }

    internal static bool IsInvalidSpace(int value, int previous, int index, int length)
    {
        if (value != Space)
        {
            return false;
        }

        return index == 0 || index == length - 1 || previous == Space;
    }

    internal static void ValidateRange(byte[] data, int offset, int length)
    {
        if (offset < 0 || offset > data.Length)
        {
            throw new ArgumentOutOfRangeException("offset");
        }

        if (length < 0 || length > data.Length - offset)
        {
            throw new ArgumentOutOfRangeException("length");
        }
    }
}
