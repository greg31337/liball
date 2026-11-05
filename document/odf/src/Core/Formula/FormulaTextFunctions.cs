using System.Globalization;
using System.Text;

namespace odf;

internal static class FormulaTextFunctions
{
    internal static OdfFormulaResult Evaluate(
        string name,
        OdfFormulaResult[] arguments,
        FormulaFunctionContext context)
    {
        if (name == OdfFormulaNames.T)
        {
            return TextIdentity(arguments[0]);
        }
        if (name == OdfFormulaNames.CONCATENATE)
        {
            return Concatenate(arguments);
        }
        if (name == OdfFormulaNames.CHAR
            || name == OdfFormulaNames.UNICHAR)
        {
            return CharacterFromNumber(name, arguments[0]);
        }
        if (name == OdfFormulaNames.CODE
            || name == OdfFormulaNames.UNICODE)
        {
            return CharacterCode(name, arguments[0]);
        }
        if (name == OdfFormulaNames.ASC || name == OdfFormulaNames.JIS)
        {
            return WidthConversion(name, arguments[0]);
        }
        if (name == OdfFormulaNames.CLEAN)
        {
            return Clean(arguments[0]);
        }
        if (name == OdfFormulaNames.EXACT)
        {
            return Exact(arguments);
        }
        if (name == OdfFormulaNames.LOWER
            || name == OdfFormulaNames.UPPER
            || name == OdfFormulaNames.PROPER)
        {
            return ChangeCase(name, arguments[0]);
        }
        if (name == OdfFormulaNames.TRIM)
        {
            return Trim(arguments[0]);
        }
        if (name == OdfFormulaNames.LEN || name == OdfFormulaNames.LENB)
        {
            return Length(name, arguments[0]);
        }
        if (name == OdfFormulaNames.FIND
            || name == OdfFormulaNames.FINDB
            || name == OdfFormulaNames.SEARCH
            || name == OdfFormulaNames.SEARCHB)
        {
            return Find(name, arguments, context);
        }
        if (name == OdfFormulaNames.LEFT
            || name == OdfFormulaNames.LEFTB
            || name == OdfFormulaNames.RIGHT
            || name == OdfFormulaNames.RIGHTB
            || name == OdfFormulaNames.MID
            || name == OdfFormulaNames.MIDB)
        {
            return Extract(name, arguments);
        }
        if (name == OdfFormulaNames.REPLACE
            || name == OdfFormulaNames.REPLACEB)
        {
            return Replace(name, arguments);
        }
        if (name == OdfFormulaNames.SUBSTITUTE)
        {
            return Substitute(arguments);
        }
        if (name == OdfFormulaNames.REPT)
        {
            return Repeat(arguments);
        }
        if (name == OdfFormulaNames.DOLLAR
            || name == OdfFormulaNames.FIXED
            || name == OdfFormulaNames.TEXT)
        {
            return Format(name, arguments, context);
        }
        return FormulaValueModel.CreateError(
            OdfFormulaError.UnsupportedFunction);
    }

    static OdfFormulaResult TextIdentity(OdfFormulaResult argument)
    {
        OdfFormulaResult value = ScalarArgument(argument);
        if (value.Error != OdfFormulaError.None) return value;
        return FormulaValueModel.CreateText(
            value.Kind == OdfValueKind.String ? value.StringValue : "");
    }

    static OdfFormulaResult Concatenate(OdfFormulaResult[] arguments)
    {
        StringBuilder result = new StringBuilder();
        int index = 0;
        while (index < arguments.Length)
        {
            OdfFormulaResult text = Text(arguments[index]);
            if (text.Error != OdfFormulaError.None) return text;
            result.Append(text.StringValue);
            index = index + 1;
        }
        return FormulaValueModel.CreateText(result.ToString());
    }

    static OdfFormulaResult CharacterFromNumber(
        string name,
        OdfFormulaResult argument)
    {
        OdfFormulaResult number = Number(argument);
        if (number.Error != OdfFormulaError.None) return number;
        int value = (int)global::System.Math.Truncate(number.NumericValue);
        if (name == OdfFormulaNames.CHAR)
        {
            if (value < 1 || value > 255)
                return FormulaValueModel.CreateError(OdfFormulaError.Value);
        }
        else if (value < 0 || value > 0x10ffff
            || (value >= 0xd800 && value <= 0xdfff))
        {
            return FormulaValueModel.CreateError(OdfFormulaError.Value);
        }
        return FormulaValueModel.CreateText(
            global::System.Char.ConvertFromUtf32(value));
    }

    static OdfFormulaResult CharacterCode(
        string name,
        OdfFormulaResult argument)
    {
        OdfFormulaResult text = Text(argument);
        if (text.Error != OdfFormulaError.None) return text;
        if (text.StringValue.Length == 0)
            return FormulaValueModel.CreateError(OdfFormulaError.Value);
        int value = CodePointAt(text.StringValue, 0);
        if (name == OdfFormulaNames.CODE && value > 255)
            return FormulaValueModel.CreateError(OdfFormulaError.Value);
        return FormulaValueModel.CreateNumber(value);
    }

    static OdfFormulaResult WidthConversion(
        string name,
        OdfFormulaResult argument)
    {
        OdfFormulaResult text = Text(argument);
        if (text.Error != OdfFormulaError.None) return text;
        return FormulaValueModel.CreateText(name == OdfFormulaNames.ASC
            ? ToHalfWidth(text.StringValue)
            : ToFullWidth(text.StringValue));
    }

    static OdfFormulaResult Clean(OdfFormulaResult argument)
    {
        OdfFormulaResult text = Text(argument);
        if (text.Error != OdfFormulaError.None) return text;
        StringBuilder result = new StringBuilder();
        int index = 0;
        while (index < text.StringValue.Length)
        {
            UnicodeCategory category = CharUnicodeInfo.GetUnicodeCategory(
                text.StringValue,
                index);
            int width = CodeUnitWidth(text.StringValue, index);
            if (category != UnicodeCategory.Control
                && category != UnicodeCategory.OtherNotAssigned)
            {
                result.Append(text.StringValue, index, width);
            }
            index = index + width;
        }
        return FormulaValueModel.CreateText(result.ToString());
    }

    static OdfFormulaResult Exact(OdfFormulaResult[] arguments)
    {
        OdfFormulaResult first = Text(arguments[0]);
        OdfFormulaResult second = Text(arguments[1]);
        if (first.Error != OdfFormulaError.None) return first;
        if (second.Error != OdfFormulaError.None) return second;
        return FormulaValueModel.CreateBoolean(global::System.String.Equals(
            first.StringValue,
            second.StringValue,
            global::System.StringComparison.Ordinal));
    }

    static OdfFormulaResult ChangeCase(
        string name,
        OdfFormulaResult argument)
    {
        OdfFormulaResult text = Text(argument);
        if (text.Error != OdfFormulaError.None) return text;
        if (name == OdfFormulaNames.LOWER)
            return FormulaValueModel.CreateText(
                text.StringValue.ToLowerInvariant());
        if (name == OdfFormulaNames.UPPER)
            return FormulaValueModel.CreateText(
                text.StringValue.ToUpperInvariant());
        StringBuilder result = new StringBuilder();
        bool previousLetter = false;
        int index = 0;
        while (index < text.StringValue.Length)
        {
            int width = CodeUnitWidth(text.StringValue, index);
            string character = text.StringValue.Substring(index, width);
            bool letter = global::System.Char.IsLetter(text.StringValue, index);
            result.Append(letter
                ? previousLetter
                    ? character.ToLowerInvariant()
                    : character.ToUpperInvariant()
                : character);
            previousLetter = letter;
            index = index + width;
        }
        return FormulaValueModel.CreateText(result.ToString());
    }

    static OdfFormulaResult Trim(OdfFormulaResult argument)
    {
        OdfFormulaResult text = Text(argument);
        if (text.Error != OdfFormulaError.None) return text;
        StringBuilder result = new StringBuilder();
        bool pendingSpace = false;
        int index = 0;
        while (index < text.StringValue.Length)
        {
            char character = text.StringValue[index];
            if (character == '\t' || character == '\n'
                || character == '\r' || character == ' ')
            {
                pendingSpace = result.Length > 0;
            }
            else
            {
                if (pendingSpace) result.Append(' ');
                result.Append(character);
                pendingSpace = false;
            }
            index = index + 1;
        }
        return FormulaValueModel.CreateText(result.ToString());
    }

    static OdfFormulaResult Length(
        string name,
        OdfFormulaResult argument)
    {
        OdfFormulaResult text = Text(argument);
        if (text.Error != OdfFormulaError.None) return text;
        return FormulaValueModel.CreateNumber(name == OdfFormulaNames.LENB
            ? Encoding.UTF8.GetByteCount(text.StringValue)
            : CodePointCount(text.StringValue));
    }

    static OdfFormulaResult Find(
        string name,
        OdfFormulaResult[] arguments,
        FormulaFunctionContext context)
    {
        OdfFormulaResult sought = Text(arguments[0]);
        OdfFormulaResult source = Text(arguments[1]);
        if (sought.Error != OdfFormulaError.None) return sought;
        if (source.Error != OdfFormulaError.None) return source;
        bool byteMode = name == OdfFormulaNames.FINDB
            || name == OdfFormulaNames.SEARCHB;
        bool insensitive = name == OdfFormulaNames.SEARCH
            || name == OdfFormulaNames.SEARCHB;
        int start = 1;
        if (arguments.Length > 2)
        {
            OdfFormulaResult startValue = Number(arguments[2]);
            if (startValue.Error != OdfFormulaError.None) return startValue;
            start = (int)global::System.Math.Truncate(startValue.NumericValue);
        }
        if (start < 1)
            return FormulaValueModel.CreateError(OdfFormulaError.Value);
        int codeUnitStart;
        if (byteMode)
        {
            codeUnitStart = CodeUnitAtByteOffset(
                source.StringValue,
                start - 1);
        }
        else
        {
            if (start > CodePointCount(source.StringValue) + 1)
                return FormulaValueModel.CreateError(OdfFormulaError.Value);
            codeUnitStart = CodeUnitAtCodePoint(source.StringValue, start - 1);
        }
        if (codeUnitStart < 0)
            return FormulaValueModel.CreateError(OdfFormulaError.Value);
        int found = insensitive
            ? SearchText(
                source.StringValue,
                sought.StringValue,
                codeUnitStart,
                context)
            : source.StringValue.IndexOf(
                sought.StringValue,
                codeUnitStart,
                global::System.StringComparison.Ordinal);
        if (found < 0)
            return FormulaValueModel.CreateError(OdfFormulaError.Value);
        int position = byteMode
            ? Encoding.UTF8.GetByteCount(
                source.StringValue.Substring(0, found)) + 1
            : CodePointCount(source.StringValue.Substring(0, found)) + 1;
        return FormulaValueModel.CreateNumber(position);
    }

    static OdfFormulaResult Extract(
        string name,
        OdfFormulaResult[] arguments)
    {
        OdfFormulaResult text = Text(arguments[0]);
        if (text.Error != OdfFormulaError.None) return text;
        bool byteMode = name == OdfFormulaNames.LEFTB
            || name == OdfFormulaNames.RIGHTB
            || name == OdfFormulaNames.MIDB;
        bool middle = name == OdfFormulaNames.MID
            || name == OdfFormulaNames.MIDB;
        int start = 1;
        int length = 1;
        if (middle)
        {
            OdfFormulaResult startValue = Number(arguments[1]);
            OdfFormulaResult lengthValue = Number(arguments[2]);
            if (startValue.Error != OdfFormulaError.None) return startValue;
            if (lengthValue.Error != OdfFormulaError.None) return lengthValue;
            start = (int)global::System.Math.Truncate(startValue.NumericValue);
            length = (int)global::System.Math.Truncate(lengthValue.NumericValue);
        }
        else if (arguments.Length > 1)
        {
            OdfFormulaResult lengthValue = Number(arguments[1]);
            if (lengthValue.Error != OdfFormulaError.None) return lengthValue;
            length = (int)global::System.Math.Truncate(lengthValue.NumericValue);
        }
        if (start < 1 || length < 0)
            return FormulaValueModel.CreateError(OdfFormulaError.Value);
        if (byteMode)
            return ExtractBytes(name, text.StringValue, start, length);
        int count = CodePointCount(text.StringValue);
        int position = middle ? start - 1 : 0;
        if (name == OdfFormulaNames.RIGHT)
            position = global::System.Math.Max(0, count - length);
        if (position >= count || length == 0)
            return FormulaValueModel.CreateText("");
        int actual = global::System.Math.Min(length, count - position);
        int codeUnitStart = CodeUnitAtCodePoint(text.StringValue, position);
        int codeUnitEnd = CodeUnitAtCodePoint(
            text.StringValue,
            position + actual);
        return FormulaValueModel.CreateText(text.StringValue.Substring(
            codeUnitStart,
            codeUnitEnd - codeUnitStart));
    }

    static OdfFormulaResult ExtractBytes(
        string name,
        string text,
        int start,
        int length)
    {
        int total = Encoding.UTF8.GetByteCount(text);
        int byteStart = name == OdfFormulaNames.RIGHTB
            ? global::System.Math.Max(0, total - length)
            : name == OdfFormulaNames.MIDB ? start - 1 : 0;
        if (byteStart >= total || length == 0)
            return FormulaValueModel.CreateText("");
        int byteEnd = global::System.Math.Min(total, byteStart + length);
        int codeUnitStart = CodeUnitAtByteOffset(text, byteStart);
        int codeUnitEnd = CodeUnitAtByteOffset(text, byteEnd);
        if (codeUnitStart < 0 || codeUnitEnd < 0)
            return FormulaValueModel.CreateError(OdfFormulaError.Value);
        return FormulaValueModel.CreateText(text.Substring(
            codeUnitStart,
            codeUnitEnd - codeUnitStart));
    }

    static OdfFormulaResult Replace(
        string name,
        OdfFormulaResult[] arguments)
    {
        OdfFormulaResult source = Text(arguments[0]);
        OdfFormulaResult startValue = Number(arguments[1]);
        OdfFormulaResult countValue = Number(arguments[2]);
        OdfFormulaResult replacement = Text(arguments[3]);
        if (source.Error != OdfFormulaError.None) return source;
        if (startValue.Error != OdfFormulaError.None) return startValue;
        if (countValue.Error != OdfFormulaError.None) return countValue;
        if (replacement.Error != OdfFormulaError.None) return replacement;
        int start = (int)global::System.Math.Truncate(startValue.NumericValue);
        int count = (int)global::System.Math.Truncate(countValue.NumericValue);
        if (start < 1 || count < 0)
            return FormulaValueModel.CreateError(OdfFormulaError.Value);
        if (name == OdfFormulaNames.REPLACEB)
            return ReplaceBytes(source.StringValue, start, count,
                replacement.StringValue);
        int length = CodePointCount(source.StringValue);
        int adjustedStart = start > length ? length : start;
        int position = length == 0 ? 0 : adjustedStart - 1;
        int removed = global::System.Math.Min(count, length - position);
        int codeUnitStart = CodeUnitAtCodePoint(source.StringValue, position);
        int codeUnitEnd = CodeUnitAtCodePoint(
            source.StringValue,
            position + removed);
        return FormulaValueModel.CreateText(
            source.StringValue.Substring(0, codeUnitStart)
                + replacement.StringValue
                + source.StringValue.Substring(codeUnitEnd));
    }

    static OdfFormulaResult ReplaceBytes(
        string source,
        int start,
        int count,
        string replacement)
    {
        int total = Encoding.UTF8.GetByteCount(source);
        int byteStart = global::System.Math.Min(start - 1, total);
        int byteEnd = global::System.Math.Min(total, byteStart + count);
        int codeUnitStart = CodeUnitAtByteOffset(source, byteStart);
        int codeUnitEnd = CodeUnitAtByteOffset(source, byteEnd);
        if (codeUnitStart < 0 || codeUnitEnd < 0)
            return FormulaValueModel.CreateError(OdfFormulaError.Value);
        return FormulaValueModel.CreateText(
            source.Substring(0, codeUnitStart)
                + replacement
                + source.Substring(codeUnitEnd));
    }

    static OdfFormulaResult Substitute(OdfFormulaResult[] arguments)
    {
        OdfFormulaResult source = Text(arguments[0]);
        OdfFormulaResult oldText = Text(arguments[1]);
        OdfFormulaResult newText = Text(arguments[2]);
        if (source.Error != OdfFormulaError.None) return source;
        if (oldText.Error != OdfFormulaError.None) return oldText;
        if (newText.Error != OdfFormulaError.None) return newText;
        if (oldText.StringValue.Length == 0) return source;
        int which = 0;
        if (arguments.Length > 3)
        {
            OdfFormulaResult whichValue = Number(arguments[3]);
            if (whichValue.Error != OdfFormulaError.None) return whichValue;
            which = (int)global::System.Math.Truncate(whichValue.NumericValue);
            if (which < 1)
                return FormulaValueModel.CreateError(OdfFormulaError.Value);
        }
        if (which == 0)
        {
            return FormulaValueModel.CreateText(source.StringValue.Replace(
                oldText.StringValue,
                newText.StringValue));
        }
        int found = -1;
        int occurrence = 0;
        int searchStart = 0;
        while (occurrence < which)
        {
            found = source.StringValue.IndexOf(
                oldText.StringValue,
                searchStart,
                global::System.StringComparison.Ordinal);
            if (found < 0) return source;
            occurrence = occurrence + 1;
            searchStart = found + oldText.StringValue.Length;
        }
        return FormulaValueModel.CreateText(
            source.StringValue.Substring(0, found)
                + newText.StringValue
                + source.StringValue.Substring(
                    found + oldText.StringValue.Length));
    }

    static OdfFormulaResult Repeat(OdfFormulaResult[] arguments)
    {
        OdfFormulaResult text = Text(arguments[0]);
        OdfFormulaResult countValue = Number(arguments[1]);
        if (text.Error != OdfFormulaError.None) return text;
        if (countValue.Error != OdfFormulaError.None) return countValue;
        int count = (int)global::System.Math.Truncate(countValue.NumericValue);
        if (count < 0)
            return FormulaValueModel.CreateError(OdfFormulaError.Value);
        StringBuilder result = new StringBuilder();
        int index = 0;
        while (index < count)
        {
            result.Append(text.StringValue);
            index = index + 1;
        }
        return FormulaValueModel.CreateText(result.ToString());
    }

    static OdfFormulaResult Format(
        string name,
        OdfFormulaResult[] arguments,
        FormulaFunctionContext context)
    {
        if (name == OdfFormulaNames.TEXT)
        {
            OdfFormulaResult format = Text(arguments[1]);
            if (format.Error != OdfFormulaError.None) return format;
            return FormatWithCode(arguments[0], format.StringValue, context);
        }
        OdfFormulaResult number = Number(arguments[0]);
        if (number.Error != OdfFormulaError.None) return number;
        int decimals = 2;
        if (arguments.Length > 1)
        {
            OdfFormulaResult decimalsValue = Number(arguments[1]);
            if (decimalsValue.Error != OdfFormulaError.None)
                return decimalsValue;
            decimals = (int)global::System.Math.Truncate(
                decimalsValue.NumericValue);
        }
        bool separators = true;
        if (name == OdfFormulaNames.FIXED && arguments.Length > 2)
        {
            OdfFormulaResult omit =
                FormulaValueModel.ConvertToBoolean(arguments[2]);
            if (omit.Error != OdfFormulaError.None) return omit;
            separators = !omit.BooleanValue;
        }
        string prefix = name == OdfFormulaNames.DOLLAR
            ? context.CellPrefix.Length > 0 ? context.CellPrefix : "$"
            : "";
        return FormulaValueModel.CreateText(FormatFixedNumber(
            number.NumericValue,
            decimals,
            separators,
            prefix,
            "",
            context));
    }

    static OdfFormulaResult FormatWithCode(
        OdfFormulaResult argument,
        string format,
        FormulaFunctionContext context)
    {
        OdfFormulaResult scalar = ScalarArgument(argument);
        if (scalar.Error != OdfFormulaError.None) return scalar;
        if (scalar.Kind == OdfValueKind.String)
            return FormulaValueModel.CreateText(scalar.StringValue);
        OdfFormulaResult number = Number(scalar);
        if (number.Error != OdfFormulaError.None)
            return FormulaValueModel.ConvertToText(scalar);
        int decimalPoint = format.IndexOf('.');
        int decimals = 0;
        if (decimalPoint >= 0)
        {
            int index = decimalPoint + 1;
            while (index < format.Length
                && (format[index] == '0' || format[index] == '#'))
            {
                decimals = decimals + 1;
                index = index + 1;
            }
        }
        bool percent = format.IndexOf('%') >= 0;
        bool separators = format.IndexOf(',') >= 0;
        string prefix = format.IndexOf('$') >= 0 ? "$" : "";
        double value = percent ? number.NumericValue * 100 : number.NumericValue;
        return FormulaValueModel.CreateText(FormatFixedNumber(
            value,
            decimals,
            separators,
            prefix,
            percent ? "%" : "",
            context));
    }

    static string FormatFixedNumber(
        double value,
        int decimals,
        bool separators,
        string prefix,
        string suffix,
        FormulaFunctionContext context)
    {
        int shownDecimals = global::System.Math.Max(0,
            global::System.Math.Min(15, decimals));
        double rounded;
        if (decimals < 0)
        {
            double scale = global::System.Math.Pow(10, -decimals);
            rounded = global::System.Math.Round(
                value / scale,
                0,
                global::System.MidpointRounding.AwayFromZero) * scale;
        }
        else
        {
            rounded = global::System.Math.Round(
                value,
                shownDecimals,
                global::System.MidpointRounding.AwayFromZero);
        }
        bool negative = rounded < 0;
        string raw = global::System.Math.Abs(rounded).ToString(
            "F" + shownDecimals.ToString(CultureInfo.InvariantCulture),
            CultureInfo.InvariantCulture);
        int decimalPoint = raw.IndexOf('.');
        string integer = decimalPoint >= 0
            ? raw.Substring(0, decimalPoint)
            : raw;
        string fraction = decimalPoint >= 0
            ? raw.Substring(decimalPoint + 1)
            : "";
        if (separators) integer = AddGroupSeparators(
            integer,
            context.GroupSeparator);
        string decimalSeparator = context.DecimalSeparator.Length > 0
            ? context.DecimalSeparator
            : ".";
        return (negative ? "-" : "") + prefix + integer
            + (shownDecimals > 0 ? decimalSeparator + fraction : "")
            + suffix;
    }

    static string AddGroupSeparators(string value, string separator)
    {
        if (separator.Length == 0) return value;
        StringBuilder result = new StringBuilder();
        int index = 0;
        while (index < value.Length)
        {
            if (index > 0 && (value.Length - index) % 3 == 0)
                result.Append(separator);
            result.Append(value[index]);
            index = index + 1;
        }
        return result.ToString();
    }

    static int SearchText(
        string source,
        string sought,
        int start,
        FormulaFunctionContext context)
    {
        if (context.SpreadsheetSettings.UseWildcards)
        {
            int index = start;
            while (index <= source.Length)
            {
                if (WildcardMatchesAt(source, index, sought, 0)) return index;
                if (index == source.Length) index = index + 1;
                else index = index + CodeUnitWidth(source, index);
            }
            return -1;
        }
        if (context.SpreadsheetSettings.UseRegularExpressions)
        {
            bool anchored = sought.Length > 0 && sought[0] == '^';
            int patternStart = anchored ? 1 : 0;
            int index = anchored ? 0 : start;
            if (index < start) return -1;
            while (index <= source.Length)
            {
                if (RegularExpressionMatchesAt(
                    source,
                    index,
                    sought,
                    patternStart)) return index;
                if (anchored || index == source.Length) return -1;
                index = index + CodeUnitWidth(source, index);
            }
            return -1;
        }
        return source.IndexOf(
            sought,
            start,
            global::System.StringComparison.OrdinalIgnoreCase);
    }

    static bool RegularExpressionMatchesAt(
        string source,
        int sourceIndex,
        string pattern,
        int patternIndex)
    {
        if (patternIndex >= pattern.Length) return true;
        if (pattern[patternIndex] == '$'
            && patternIndex + 1 == pattern.Length)
            return sourceIndex == source.Length;
        int tokenIndex = patternIndex;
        bool escaped = pattern[tokenIndex] == '\\'
            && tokenIndex + 1 < pattern.Length;
        if (escaped
            && tokenIndex + 1 < pattern.Length)
        {
            tokenIndex = tokenIndex + 1;
        }
        int tokenWidth = CodeUnitWidth(pattern, tokenIndex);
        int nextPattern = tokenIndex + tokenWidth;
        char quantifier = nextPattern < pattern.Length
            ? pattern[nextPattern]
            : '\0';
        if (quantifier == '*' || quantifier == '?')
        {
            if (RegularExpressionMatchesAt(
                source,
                sourceIndex,
                pattern,
                nextPattern + 1)) return true;
        }
        bool matched = RegexTokenMatches(
            source,
            sourceIndex,
            pattern,
            tokenIndex,
            tokenWidth,
            escaped);
        if (!matched) return false;
        int nextSource = sourceIndex + CodeUnitWidth(source, sourceIndex);
        if (quantifier == '*' || quantifier == '+')
        {
            int candidate = nextSource;
            while (true)
            {
                if (RegularExpressionMatchesAt(
                    source,
                    candidate,
                    pattern,
                    nextPattern + 1)) return true;
                if (!RegexTokenMatches(
                    source,
                    candidate,
                    pattern,
                    tokenIndex,
                    tokenWidth,
                    escaped)) return false;
                candidate = candidate + CodeUnitWidth(source, candidate);
            }
        }
        return RegularExpressionMatchesAt(
            source,
            nextSource,
            pattern,
            quantifier == '?' ? nextPattern + 1 : nextPattern);
    }

    static bool RegexTokenMatches(
        string source,
        int sourceIndex,
        string pattern,
        int tokenIndex,
        int tokenWidth,
        bool escaped)
    {
        if (sourceIndex >= source.Length) return false;
        if (!escaped && pattern[tokenIndex] == '.') return true;
        int sourceWidth = CodeUnitWidth(source, sourceIndex);
        return sourceWidth == tokenWidth
            && global::System.String.Compare(
                source,
                sourceIndex,
                pattern,
                tokenIndex,
                tokenWidth,
                global::System.StringComparison.OrdinalIgnoreCase) == 0;
    }

    static bool WildcardMatchesAt(
        string source,
        int sourceIndex,
        string pattern,
        int patternIndex)
    {
        if (patternIndex >= pattern.Length) return true;
        char token = pattern[patternIndex];
        if (token == '*')
        {
            int candidate = sourceIndex;
            while (candidate <= source.Length)
            {
                if (WildcardMatchesAt(
                    source,
                    candidate,
                    pattern,
                    patternIndex + 1)) return true;
                if (candidate == source.Length) candidate = candidate + 1;
                else candidate = candidate + CodeUnitWidth(source, candidate);
            }
            return false;
        }
        if (sourceIndex >= source.Length) return false;
        if (token == '?')
        {
            return WildcardMatchesAt(
                source,
                sourceIndex + CodeUnitWidth(source, sourceIndex),
                pattern,
                patternIndex + 1);
        }
        int literalIndex = patternIndex;
        if (token == '~' && patternIndex + 1 < pattern.Length)
            literalIndex = patternIndex + 1;
        int patternWidth = CodeUnitWidth(pattern, literalIndex);
        int sourceWidth = CodeUnitWidth(source, sourceIndex);
        if (patternWidth != sourceWidth
            || global::System.String.Compare(
                source,
                sourceIndex,
                pattern,
                literalIndex,
                patternWidth,
                global::System.StringComparison.OrdinalIgnoreCase) != 0)
            return false;
        return WildcardMatchesAt(
            source,
            sourceIndex + sourceWidth,
            pattern,
            literalIndex + patternWidth);
    }

    static int CodePointCount(string value)
    {
        int count = 0;
        int index = 0;
        while (index < value.Length)
        {
            index = index + CodeUnitWidth(value, index);
            count = count + 1;
        }
        return count;
    }

    static int CodeUnitAtCodePoint(string value, int position)
    {
        int count = 0;
        int index = 0;
        while (index < value.Length && count < position)
        {
            index = index + CodeUnitWidth(value, index);
            count = count + 1;
        }
        return index;
    }

    static int CodeUnitAtByteOffset(string value, int byteOffset)
    {
        if (byteOffset < 0) return -1;
        int bytes = 0;
        int index = 0;
        while (index < value.Length)
        {
            if (bytes == byteOffset) return index;
            int width = CodeUnitWidth(value, index);
            bytes = bytes + Encoding.UTF8.GetByteCount(
                value.Substring(index, width));
            if (bytes > byteOffset) return -1;
            index = index + width;
        }
        return bytes == byteOffset ? value.Length : -1;
    }

    static int CodeUnitWidth(string value, int index)
    {
        return index + 1 < value.Length
            && global::System.Char.IsSurrogatePair(
                value[index],
                value[index + 1]) ? 2 : 1;
    }

    static int CodePointAt(string value, int index)
    {
        return CodeUnitWidth(value, index) == 2
            ? global::System.Char.ConvertToUtf32(value, index)
            : value[index];
    }

    static string ToHalfWidth(string value)
    {
        StringBuilder result = new StringBuilder();
        int index = 0;
        while (index < value.Length)
        {
            int codePoint = CodePointAt(value, index);
            AppendHalfWidth(result, codePoint);
            index = index + CodeUnitWidth(value, index);
        }
        return result.ToString();
    }

    static void AppendHalfWidth(StringBuilder result, int value)
    {
        if (value >= 0xff01 && value <= 0xff5e)
        {
            AppendCodePoint(result, value - 0xff01 + 0x21);
            return;
        }
        if (value >= 0x30a1 && value <= 0x30aa)
        {
            AppendCodePoint(result, value % 2 == 0
                ? (value - 0x30a2) / 2 + 0xff71
                : (value - 0x30a1) / 2 + 0xff67);
            return;
        }
        if (value >= 0x30ab && value <= 0x30c2)
        {
            AppendCodePoint(result, value % 2 == 1
                ? (value - 0x30ab) / 2 + 0xff76
                : (value - 0x30ac) / 2 + 0xff76);
            if (value % 2 == 0) AppendCodePoint(result, 0xff9e);
            return;
        }
        if (value == 0x30c3)
        {
            AppendCodePoint(result, 0xff6f);
            return;
        }
        if (value >= 0x30c4 && value <= 0x30c9)
        {
            AppendCodePoint(result, value % 2 == 0
                ? (value - 0x30c4) / 2 + 0xff82
                : (value - 0x30c5) / 2 + 0xff82);
            if (value % 2 == 1) AppendCodePoint(result, 0xff9e);
            return;
        }
        if (value >= 0x30ca && value <= 0x30ce)
        {
            AppendCodePoint(result, value - 0x30ca + 0xff85);
            return;
        }
        if (value >= 0x30cf && value <= 0x30dd)
        {
            AppendCodePoint(result, (value - 0x30cf) / 3 + 0xff8a);
            if (value % 3 == 1) AppendCodePoint(result, 0xff9e);
            if (value % 3 == 2) AppendCodePoint(result, 0xff9f);
            return;
        }
        if (value >= 0x30de && value <= 0x30e2)
        {
            AppendCodePoint(result, value - 0x30de + 0xff8f);
            return;
        }
        if (value >= 0x30e3 && value <= 0x30e8)
        {
            AppendCodePoint(result, value % 2 == 0
                ? (value - 0x30e4) / 2 + 0xff94
                : (value - 0x30e3) / 2 + 0xff6c);
            return;
        }
        if (value >= 0x30e9 && value <= 0x30ed)
        {
            AppendCodePoint(result, value - 0x30e9 + 0xff97);
            return;
        }
        if (value == 0x30ef) value = 0xff9c;
        else if (value == 0x30f2) value = 0xff66;
        else if (value == 0x30f3) value = 0xff9d;
        else if (value == 0x2015 || value == 0x30fc) value = 0xff70;
        else if (value == 0x2018) value = 0x60;
        else if (value == 0x2019) value = 0x27;
        else if (value == 0x201d) value = 0x22;
        else if (value == 0x3001) value = 0xff64;
        else if (value == 0x3002) value = 0xff61;
        else if (value == 0x300c) value = 0xff62;
        else if (value == 0x300d) value = 0xff63;
        else if (value == 0x309b) value = 0xff9e;
        else if (value == 0x309c) value = 0xff9f;
        else if (value == 0x30fb) value = 0xff65;
        else if (value == 0xffe5) value = 0x5c;
        AppendCodePoint(result, value);
    }

    static string ToFullWidth(string value)
    {
        StringBuilder result = new StringBuilder();
        int index = 0;
        while (index < value.Length)
        {
            int codePoint = CodePointAt(value, index);
            int next = index + CodeUnitWidth(value, index);
            int mark = next < value.Length ? CodePointAt(value, next) : -1;
            bool consumed = AppendFullWidth(result, codePoint, mark);
            index = consumed
                ? next + CodeUnitWidth(value, next)
                : next;
        }
        return result.ToString();
    }

    static bool AppendFullWidth(
        StringBuilder result,
        int value,
        int mark)
    {
        if (value == 0x22) value = 0x201d;
        else if (value == 0x5c) value = 0xffe5;
        else if (value == 0x60) value = 0x2018;
        else if (value == 0x27) value = 0x2019;
        else if (value >= 0x21 && value <= 0x7e)
            value = value - 0x21 + 0xff01;
        else if (value == 0xff66) value = 0x30f2;
        else if (value >= 0xff67 && value <= 0xff6b)
            value = (value - 0xff67) * 2 + 0x30a1;
        else if (value >= 0xff6c && value <= 0xff6e)
            value = (value - 0xff6c) * 2 + 0x30e3;
        else if (value == 0xff6f) value = 0x30c3;
        else if (value >= 0xff71 && value <= 0xff75)
            value = (value - 0xff71) * 2 + 0x30a2;
        else if (value >= 0xff76 && value <= 0xff81)
        {
            value = (value - 0xff76) * 2
                + (mark == 0xff9e ? 0x30ac : 0x30ab);
            AppendCodePoint(result, value);
            return mark == 0xff9e;
        }
        else if (value >= 0xff82 && value <= 0xff84)
        {
            value = (value - 0xff82) * 2
                + (mark == 0xff9e ? 0x30c5 : 0x30c4);
            AppendCodePoint(result, value);
            return mark == 0xff9e;
        }
        else if (value >= 0xff85 && value <= 0xff89)
            value = value - 0xff85 + 0x30ca;
        else if (value >= 0xff8a && value <= 0xff8e)
        {
            int addition = mark == 0xff9e ? 1 : mark == 0xff9f ? 2 : 0;
            value = (value - 0xff8a) * 3 + 0x30cf + addition;
            AppendCodePoint(result, value);
            return mark == 0xff9e || mark == 0xff9f;
        }
        else if (value >= 0xff8f && value <= 0xff93)
            value = value - 0xff8f + 0x30de;
        else if (value >= 0xff94 && value <= 0xff96)
            value = (value - 0xff94) * 2 + 0x30e4;
        else if (value >= 0xff97 && value <= 0xff9b)
            value = value - 0xff97 + 0x30e9;
        else if (value == 0xff9c) value = 0x30ef;
        else if (value == 0xff9d) value = 0x30f3;
        else if (value == 0xff9e) value = 0x309b;
        else if (value == 0xff9f) value = 0x309c;
        else if (value == 0xff70) value = 0x30fc;
        else if (value == 0xff61) value = 0x3002;
        else if (value == 0xff62) value = 0x300c;
        else if (value == 0xff63) value = 0x300d;
        else if (value == 0xff64) value = 0x3001;
        else if (value == 0xff65) value = 0x30fb;
        AppendCodePoint(result, value);
        return false;
    }

    static void AppendCodePoint(StringBuilder result, int value)
    {
        result.Append(global::System.Char.ConvertFromUtf32(value));
    }

    static OdfFormulaResult Text(OdfFormulaResult value)
    {
        return FormulaValueModel.ConvertToText(value);
    }

    static OdfFormulaResult Number(OdfFormulaResult value)
    {
        return FormulaValueModel.ConvertToNumber(value);
    }

    static OdfFormulaResult ScalarArgument(OdfFormulaResult value)
    {
        if (value.Kind != OdfValueKind.Array
            && value.Kind != OdfValueKind.Reference)
        {
            return value;
        }
        return value.Values.Length == 1
            ? value.Values[0]
            : FormulaValueModel.CreateError(OdfFormulaError.Value);
    }
}
