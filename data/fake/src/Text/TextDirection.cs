using System.Globalization;
using System.Text;

namespace fake;

internal static class TextDirection
{
    internal static WritingDirection Direction(string value)
    {
        UnicodeText.Validate(value);
        bool left = false;
        bool right = false;
        int index = 0;
        while (index < value.Length)
        {
            int scalar = char.ConvertToUtf32(value, index);
            UnicodeCategory category = CharUnicodeInfo.GetUnicodeCategory(value, index);
            if (RightToLeft(scalar))
            {
                if (Letter(category))
                {
                    right = true;
                }
            }
            else if (Letter(category))
            {
                left = true;
            }
            index = index + (scalar > 65535 ? 2 : 1);
        }
        if (left && right)
        {
            return WritingDirection.Mixed;
        }
        if (right)
        {
            return WritingDirection.RightToLeft;
        }
        if (left)
        {
            return WritingDirection.LeftToRight;
        }
        return WritingDirection.Neutral;
    }

    internal static bool HasMixedScripts(string value)
    {
        UnicodeText.Validate(value);
        int found = 0;
        int index = 0;
        while (index < value.Length)
        {
            int scalar = char.ConvertToUtf32(value, index);
            UnicodeCategory category = CharUnicodeInfo.GetUnicodeCategory(value, index);
            int script = Script(scalar, category);
            if (script != 0)
            {
                if (found != 0 && found != script)
                {
                    return true;
                }
                found = script;
            }
            index = index + (scalar > 65535 ? 2 : 1);
        }
        return false;
    }

    internal static string Isolate(string value, int maximumOutputSize)
    {
        WritingDirection direction = Direction(value);
        if (value.Length == 0)
        {
            return value;
        }
        Guard.OutputSize((long)value.Length + 2L, maximumOutputSize, "value");
        char opening = '\u2068';
        if (direction == WritingDirection.LeftToRight)
        {
            opening = '\u2066';
        }
        else if (direction == WritingDirection.RightToLeft)
        {
            opening = '\u2067';
        }
        return opening + value + '\u2069';
    }

    internal static string StripControls(string value, int maximumOutputSize)
    {
        UnicodeText.Validate(value);
        StringBuilder builder = new StringBuilder(value.Length);
        int index = 0;
        while (index < value.Length)
        {
            int scalar = char.ConvertToUtf32(value, index);
            if (!DirectionalControl(scalar))
            {
                builder.Append(char.ConvertFromUtf32(scalar));
            }
            index = index + (scalar > 65535 ? 2 : 1);
        }
        Guard.OutputSize(builder.Length, maximumOutputSize, "value");
        return builder.ToString();
    }

    static bool Letter(UnicodeCategory category)
    {
        return category == UnicodeCategory.UppercaseLetter
            || category == UnicodeCategory.LowercaseLetter
            || category == UnicodeCategory.TitlecaseLetter
            || category == UnicodeCategory.ModifierLetter
            || category == UnicodeCategory.OtherLetter;
    }

    static bool RightToLeft(int scalar)
    {
        return (scalar >= 1424 && scalar <= 2303)
            || (scalar >= 64285 && scalar <= 65023)
            || (scalar >= 65136 && scalar <= 65279)
            || (scalar >= 67584 && scalar <= 67647)
            || (scalar >= 69216 && scalar <= 69247)
            || (scalar >= 125184 && scalar <= 125279)
            || (scalar >= 126464 && scalar <= 126719);
    }

    static bool DirectionalControl(int scalar)
    {
        return scalar == 1564
            || scalar == 8206
            || scalar == 8207
            || (scalar >= 8234 && scalar <= 8238)
            || (scalar >= 8294 && scalar <= 8297);
    }

    static int Script(int scalar, UnicodeCategory category)
    {
        if (!Letter(category))
        {
            return 0;
        }
        if ((scalar >= 65 && scalar <= 90)
            || (scalar >= 97 && scalar <= 122)
            || (scalar >= 192 && scalar <= 591)
            || (scalar >= 7680 && scalar <= 7935)
            || (scalar >= 42786 && scalar <= 42991)
            || (scalar >= 65313 && scalar <= 65370))
        {
            return 1;
        }
        if ((scalar >= 880 && scalar <= 1023) || (scalar >= 7936 && scalar <= 8191))
        {
            return 2;
        }
        if ((scalar >= 1024 && scalar <= 1327)
            || (scalar >= 11744 && scalar <= 11775)
            || (scalar >= 42560 && scalar <= 42655))
        {
            return 3;
        }
        if (scalar >= 1424 && scalar <= 1535)
        {
            return 4;
        }
        if ((scalar >= 1536 && scalar <= 2303)
            || (scalar >= 64336 && scalar <= 65023)
            || (scalar >= 65136 && scalar <= 65279)
            || (scalar >= 125184 && scalar <= 125279)
            || (scalar >= 126464 && scalar <= 126719))
        {
            return 5;
        }
        if (scalar >= 2304 && scalar <= 2431)
        {
            return 6;
        }
        if (scalar >= 2432 && scalar <= 2559)
        {
            return 7;
        }
        if (scalar >= 3584 && scalar <= 3711)
        {
            return 8;
        }
        if ((scalar >= 12352 && scalar <= 12543)
            || (scalar >= 12784 && scalar <= 12799)
            || (scalar >= 13312 && scalar <= 40959)
            || (scalar >= 63744 && scalar <= 64255)
            || (scalar >= 131072 && scalar <= 201551))
        {
            return 9;
        }
        if ((scalar >= 4352 && scalar <= 4607)
            || (scalar >= 12592 && scalar <= 12687)
            || (scalar >= 44032 && scalar <= 55215))
        {
            return 10;
        }
        return 100;
    }
}
