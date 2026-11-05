using System;
using System.Collections.Generic;
using System.Globalization;
using System.Numerics;
using System.Text;

namespace xml;

internal class XPathNumberPicture
{
    internal string Prefix;
    internal string Suffix;
    internal int MinimumIntegerSize;
    internal int MinimumFractionSize;
    internal int MaximumFractionSize;
    internal int ScalingFactor;
    internal int MinimumExponentSize;
    internal int Scale;
    internal int RegularIntegerGroupingSize;
    internal bool HasDecimalSeparator;
    internal int[] IntegerGroupingPositions;
    internal int[] FractionGroupingPositions;

    internal XPathNumberPicture()
    {
        Prefix = "";
        Suffix = "";
        IntegerGroupingPositions = [];
        FractionGroupingPositions = [];
    }
}

internal class XPathNumberFormattingValue
{
    internal BigInteger Coefficient;
    internal int Scale;
    internal bool Negative;
    internal bool NotANumber;
    internal bool Infinite;
    internal bool Floating;
    internal double FloatingValue;

    internal XPathNumberFormattingValue()
    {
        Coefficient = BigInteger.Zero;
    }
}

internal class XPathNumberFormattingFunctionImplementation : IXPathFunction
{
    internal string LocalName;
    internal SourceRange Range;
    internal XPathStaticContext StaticContext;
    internal XdmSequenceOperations Sequences;
    internal XdmTypeConverter Converter;
    internal XdmTypeSystem Types;
    internal XPathDecimalFormat ActiveDecimalFormat;
    internal XdmNumericDatatype Numerics;
    internal XdmFactory Factory;

    internal XPathNumberFormattingFunctionImplementation(string localName, SourceRange range, XPathStaticContext staticContext)
    {
        LocalName = localName;
        Range = range;
        StaticContext = staticContext;
        Sequences = new XdmSequenceOperations();
        Converter = new XdmTypeConverter();
        Types = new XdmTypeSystem();
        ActiveDecimalFormat = new XPathDecimalFormat();
        Numerics = new XdmNumericDatatype();
        Factory = new XdmFactory();
    }

    public XdmSequence InvokeXPathFunction(XPathFunctionBinding binding, XdmSequence[] arguments, XPathDynamicContext context)
    {
        string result = LocalName == "format-integer" ? EvaluateXPathFormatInteger(arguments) : EvaluateXPathFormatNumber(arguments);
        return Sequences.CreateXdmAtomicSequence(Factory.CreateXdmStringAtomicValue(result, XdmAtomicType.String));
    }

    internal string EvaluateXPathFormatInteger(XdmSequence[] arguments)
    {
        XdmAtomicValue? value = ReadXPathFormattingOptionalInteger(arguments[0]);
        string picture = ReadXPathFormattingRequiredString(arguments[1]);
        if (arguments.Length == 3)
        {
            ReadXPathFormattingOptionalString(arguments[2]);
        }

        if (value == null)
        {
            return "";
        }

        BigInteger integer = Numerics.ReadXdmIntegerValue(value);
        bool negative = integer < BigInteger.Zero;
        string formatted = FormatXPathIntegerValue(BigInteger.Abs(integer), picture);
        return negative ? "-" + formatted : formatted;
    }

    internal string FormatXPathIntegerValue(BigInteger value, string picture)
    {
        if (picture.EndsWith(";", StringComparison.Ordinal))
        {
            picture = picture.Substring(0, picture.Length - 1);
        }

        int separator = picture.LastIndexOf(';');
        if (separator >= 0)
        {
            string candidate = picture.Substring(separator + 1);
            if (candidate.Length == 0 || candidate[0] != 'c' && candidate[0] != 'o' && candidate[0] != 'a' && candidate[0] != 't')
            {
                separator = -1;
            }
        }
        string primary = separator < 0 ? picture : picture.Substring(0, separator);
        string modifier = separator < 0 ? "" : picture.Substring(separator + 1);
        bool ordinal = ValidateXPathIntegerModifier(modifier);
        if (primary.Length == 0)
        {
            throw CreateXPathFormattingError("An integer picture requires a primary format token.", XPathErrorCodes.FODF1310);
        }

        bool hasDigit = false;
        int index = 0;
        while (index < primary.Length)
        {
            if (ReadXPathDigitValueAt(primary, index) >= 0)
            {
                hasDigit = true;
            }

            index = index + ReadXPathCodepointWidth(primary, index);
        }

        string result;
        if (hasDigit)
        {
            result = FormatXPathIntegerDecimal(value, primary);
            if (ordinal)
            {
                result = result + ReadXPathFormattingOrdinalSuffix(value);
            }

            return result;
        }

        if (primary == "A" || primary == "a")
        {
            result = FormatXPathFormattingAlphabetic(value);
            return primary == "a" ? result.ToLowerInvariant() : result;
        }

        if (primary == "I" || primary == "i")
        {
            result = FormatXPathFormattingRoman(value);
            return primary == "i" ? result.ToLowerInvariant() : result;
        }

        if (primary == "W" || primary == "w" || primary == "Ww")
        {
            result = ordinal ? FormatXPathFormattingOrdinalWords(value) : FormatXPathFormattingWords(value);
            if (primary == "W")
            {
                return result.ToUpperInvariant();
            }

            return primary == "w" ? result.ToLowerInvariant() : result;
        }

        result = value.ToString(CultureInfo.InvariantCulture);
        return ordinal ? result + ReadXPathFormattingOrdinalSuffix(value) : result;
    }

    internal bool ValidateXPathIntegerModifier(string modifier)
    {
        if (modifier.Length == 0)
        {
            return false;
        }

        int index = 0;
        bool ordinal = false;
        if (modifier[index] == 'c' || modifier[index] == 'o')
        {
            ordinal = modifier[index] == 'o';
            index = index + 1;
            if (index < modifier.Length && modifier[index] == '(')
            {
                int closing = modifier.IndexOf(')', index + 1);
                if (closing <= index + 1 || closing != modifier.LastIndexOf(')'))
                {
                    throw CreateXPathFormattingError("The integer picture has an invalid format modifier.", XPathErrorCodes.FODF1310);
                }

                index = closing + 1;
            }
        }

        if (index < modifier.Length && (modifier[index] == 'a' || modifier[index] == 't'))
        {
            index = index + 1;
        }

        if (index != modifier.Length)
        {
            throw CreateXPathFormattingError("The integer picture has an invalid format modifier.", XPathErrorCodes.FODF1310);
        }

        return ordinal;
    }

    internal string FormatXPathIntegerDecimal(BigInteger value, string primary)
    {
        int mandatory = 0;
        int totalDigits = 0;
        int zero = -1;
        bool foundMandatory = false;
        List<int> groupIndexes = new List<int>();
        List<string> groupCharacters = new List<string>();
        int index = 0;
        while (index < primary.Length)
        {
            char character = primary[index];
            int width = ReadXPathCodepointWidth(primary, index);
            int digit = ReadXPathDigitValueAt(primary, index);
            if (digit >= 0)
            {
                int candidate = char.ConvertToUtf32(primary, index) - digit;
                if (zero >= 0 && zero != candidate)
                {
                    throw CreateXPathFormattingError("All mandatory digits in an integer picture must use one digit family.", XPathErrorCodes.FODF1310);
                }

                zero = candidate;
                mandatory = mandatory + 1;
                totalDigits = totalDigits + 1;
                foundMandatory = true;
            }
            else if (character == '#')
            {
                if (foundMandatory)
                {
                    throw CreateXPathFormattingError("Optional digits must precede mandatory digits in an integer picture.", XPathErrorCodes.FODF1310);
                }

                totalDigits = totalDigits + 1;
            }
            else
            {
                UnicodeCategory category = CharUnicodeInfo.GetUnicodeCategory(primary, index);
                bool alphanumeric = category == UnicodeCategory.DecimalDigitNumber || category == UnicodeCategory.LetterNumber || category == UnicodeCategory.OtherNumber || category == UnicodeCategory.UppercaseLetter || category == UnicodeCategory.LowercaseLetter || category == UnicodeCategory.TitlecaseLetter || category == UnicodeCategory.ModifierLetter || category == UnicodeCategory.OtherLetter;
                int previous = index > 0 && char.IsLowSurrogate(primary[index - 1]) ? index - 2 : index - 1;
                if (alphanumeric || index == 0 || index + width == primary.Length || previous >= 0 && IsXPathIntegerGroupingCharacterAt(primary, previous))
                {
                    throw CreateXPathFormattingError("The integer picture contains an invalid grouping separator.", XPathErrorCodes.FODF1310);
                }

                groupIndexes.Add(index);
                groupCharacters.Add(primary.Substring(index, width));
            }

            index = index + width;
        }

        if (!foundMandatory)
        {
            throw CreateXPathFormattingError("An integer decimal picture requires a mandatory digit.", XPathErrorCodes.FODF1310);
        }

        string digits = value.ToString(CultureInfo.InvariantCulture).PadLeft(mandatory, '0');
        List<int> positions = new List<int>();
        index = 0;
        while (index < groupIndexes.Count)
        {
            positions.Add(CountXPathIntegerPictureDigits(primary, groupIndexes[index] + ReadXPathCodepointWidth(primary, groupIndexes[index])));
            index = index + 1;
        }

        bool regular = IsXPathRegularIntegerGrouping(positions, groupCharacters, totalDigits);
        if (regular)
        {
            int groupSize = positions[positions.Count - 1];
            string regularSeparator = groupCharacters[groupCharacters.Count - 1];
            positions.Clear();
            int position = groupSize;
            while (position < digits.Length)
            {
                positions.Add(position);
                position = position + groupSize;
            }

            groupCharacters.Clear();
            index = 0;
            while (index < positions.Count)
            {
                groupCharacters.Add(regularSeparator);
                index = index + 1;
            }
        }

        string grouped = InsertXPathIntegerGrouping(digits, positions, groupCharacters);
        return TranslateXPathFormattingDigits(grouped, zero);
    }

    internal bool IsXPathIntegerGroupingCharacterAt(string source, int index)
    {
        return source[index] != '#' && ReadXPathDigitValueAt(source, index) < 0;
    }

    internal int CountXPathIntegerPictureDigits(string picture, int start)
    {
        int count = 0;
        int index = start;
        while (index < picture.Length)
        {
            if (picture[index] == '#' || ReadXPathDigitValueAt(picture, index) >= 0)
            {
                count = count + 1;
            }

            index = index + ReadXPathCodepointWidth(picture, index);
        }

        return count;
    }

    internal bool IsXPathRegularIntegerGrouping(List<int> positions, List<string> characters, int totalDigits)
    {
        if (positions.Count == 0)
        {
            return false;
        }

        int groupSize = positions[positions.Count - 1];
        if (groupSize <= 0)
        {
            return false;
        }

        string separator = characters[0];
        int index = 0;
        while (index < positions.Count)
        {
            if (characters[index] != separator || positions[index] % groupSize != 0)
            {
                return false;
            }

            index = index + 1;
        }

        int expected = groupSize;
        while (expected < totalDigits)
        {
            bool found = false;
            index = 0;
            while (index < positions.Count)
            {
                if (positions[index] == expected)
                {
                    found = true;
                }

                index = index + 1;
            }

            if (!found)
            {
                return false;
            }

            expected = expected + groupSize;
        }

        return true;
    }

    internal string InsertXPathIntegerGrouping(string digits, List<int> positions, List<string> characters)
    {
        StringBuilder result = new StringBuilder();
        int digitIndex = 0;
        while (digitIndex < digits.Length)
        {
            int position = digits.Length - digitIndex;
            int groupIndex = 0;
            while (groupIndex < positions.Count)
            {
                if (positions[groupIndex] == position && position < digits.Length)
                {
                    result.Append(characters[groupIndex]);
                }

                groupIndex = groupIndex + 1;
            }

            result.Append(digits[digitIndex]);
            digitIndex = digitIndex + 1;
        }

        return result.ToString();
    }

    internal string EvaluateXPathFormatNumber(XdmSequence[] arguments)
    {
        XdmAtomicValue? atomic = ReadXPathFormattingOptionalAtomic(arguments[0]);
        string picture = ReadXPathFormattingRequiredString(arguments[1]);
        string? decimalFormatName = null;
        if (arguments.Length == 3)
        {
            decimalFormatName = ReadXPathFormattingOptionalString(arguments[2]);
            if (decimalFormatName != null && new XdmStringDatatype().CollapseXdmStringWhitespace(decimalFormatName).Length > 0)
            {
                ValidateXPathDecimalFormatName(decimalFormatName);
            }
        }

        ActiveDecimalFormat = SelectXPathDecimalFormat(decimalFormatName);
        picture = NormalizeXPathDecimalFormatPicture(picture, ActiveDecimalFormat);
        XPathNumberFormattingValue value = atomic == null ? CreateXPathNotANumberValue() : ReadXPathNumberFormattingValue(atomic);
        XPathNumberPicture[] pictures = ParseXPathNumberPictures(picture);
        if (value.NotANumber)
        {
            return ActiveDecimalFormat.NaN == null ? "NaN" : ActiveDecimalFormat.NaN;
        }

        XPathNumberPicture selected = value.Negative && pictures.Length == 2 ? pictures[1] : pictures[0];
        string minus = ActiveDecimalFormat.MinusSign == null ? "-" : ActiveDecimalFormat.MinusSign;
        string prefix = value.Negative && pictures.Length == 1 ? minus + selected.Prefix : selected.Prefix;
        if (value.Infinite)
        {
            string infinity = ActiveDecimalFormat.Infinity == null ? "Infinity" : ActiveDecimalFormat.Infinity;
            return TranslateXPathDecimalFormatAffix(prefix, ActiveDecimalFormat) + infinity + TranslateXPathDecimalFormatAffix(selected.Suffix, ActiveDecimalFormat);
        }

        if (value.Floating && selected.Scale > 0 && double.IsInfinity(value.FloatingValue * Math.Pow(10, selected.Scale)))
        {
            string infinity = ActiveDecimalFormat.Infinity == null ? "Infinity" : ActiveDecimalFormat.Infinity;
            return TranslateXPathDecimalFormatAffix(prefix, ActiveDecimalFormat) + infinity + TranslateXPathDecimalFormatAffix(selected.Suffix, ActiveDecimalFormat);
        }

        if (selected.MinimumExponentSize > 0 && selected.MinimumIntegerSize == 0 && selected.MaximumFractionSize == 0)
        {
            selected.MinimumFractionSize = 1;
            selected.MaximumFractionSize = 1;
        }

        XdmDecimalStorage storage;
        if (value.Floating && selected.Scale > 0)
        {
            XPathNumberFormattingValue scaled = new XPathNumberFormattingValue();
            double floating = value.FloatingValue * Math.Pow(10, selected.Scale);
            ParseXPathFloatingDecimal(new XdmFloatingDatatype().CanonicalizeXdmFloating(floating, XdmAtomicType.Double), scaled);
            storage = new XdmDecimalStorage(scaled.Coefficient, scaled.Scale);
        }
        else
        {
            storage = new XdmDecimalStorage(value.Coefficient, value.Scale - selected.Scale);
        }
        int exponent = 0;
        if (selected.MinimumExponentSize > 0 && storage.Coefficient != BigInteger.Zero)
        {
            int magnitude = BigInteger.Abs(storage.Coefficient).ToString(CultureInfo.InvariantCulture).Length - storage.Scale - 1;
            exponent = magnitude - selected.ScalingFactor + 1;
            storage.Scale = storage.Scale + exponent;
        }

        string number = FormatXPathDecimalNumber(storage, selected);
        if (selected.MinimumExponentSize > 0)
        {
            string exponentDigits = BigInteger.Abs(new BigInteger(exponent)).ToString(CultureInfo.InvariantCulture).PadLeft(selected.MinimumExponentSize, '0');
            number = number + "e" + (exponent < 0 ? "-" : "") + exponentDigits;
        }

        return TranslateXPathDecimalFormatAffix(prefix, ActiveDecimalFormat) + TranslateXPathDecimalFormatNumber(number, ActiveDecimalFormat) + TranslateXPathDecimalFormatAffix(selected.Suffix, ActiveDecimalFormat);
    }

    internal XPathDecimalFormat SelectXPathDecimalFormat(string? suppliedName)
    {
        XPathDecimalFormat[] declarations = StaticContext.DecimalFormatDeclarations == null ? [] : StaticContext.DecimalFormatDeclarations;
        XmlName? requested = suppliedName == null || new XdmStringDatatype().CollapseXdmStringWhitespace(suppliedName).Length == 0 ? null : ParseXPathDecimalFormatName(suppliedName);
        int index = 0;
        while (index < declarations.Length)
        {
            XPathDecimalFormat declaration = declarations[index];
            if (requested == null && declaration.Name == null || requested != null && declaration.Name != null && declaration.Name.LocalName == requested.LocalName && (declaration.Name.NamespaceUri == null ? "" : declaration.Name.NamespaceUri) == (requested.NamespaceUri == null ? "" : requested.NamespaceUri))
            {
                return declaration;
            }

            index = index + 1;
        }

        return new XPathDecimalFormat();
    }

    internal XmlName? ParseXPathDecimalFormatName(string supplied)
    {
        string name = new XdmStringDatatype().CollapseXdmStringWhitespace(supplied);
        if (name.StartsWith("Q{", StringComparison.Ordinal))
        {
            int closing = name.IndexOf('}');
            if (closing >= 2)
            {
                XmlName result = new XmlName();
                result.NamespaceUri = name.Substring(2, closing - 2);
                result.LocalName = name.Substring(closing + 1);
                return result;
            }

            return null;
        }

        XmlNamespaceBinding[] namespaces = StaticContext.Namespaces == null ? [] : StaticContext.Namespaces;
        XdmNumericParseResult parsed = new XdmQNameDatatype().TryParseXdmQNameWithNamespaces(name, XdmAtomicType.QName, namespaces, null);
        return parsed.Success && parsed.Value != null ? parsed.Value.NameValue : null;
    }

    internal string NormalizeXPathDecimalFormatPicture(string picture, XPathDecimalFormat format)
    {
        StringBuilder result = new StringBuilder();
        int zero = format.ZeroDigit == null ? -1 : char.ConvertToUtf32(format.ZeroDigit, 0);
        int index = 0;
        while (index < picture.Length)
        {
            int width = ReadXPathCodepointWidth(picture, index);
            string character = picture.Substring(index, width);
            int codepoint = char.ConvertToUtf32(picture, index);
            if (zero >= 0 && codepoint >= zero && codepoint <= zero + 9)
            {
                result.Append((char)('0' + codepoint - zero));
            }
            else if (format.Digit != null && character == "#")
            {
                result.Append('\uE000');
            }
            else if (format.ZeroDigit != null && character == "0")
            {
                result.Append('\uE001');
            }
            else if (format.DecimalSeparator != null && character == format.DecimalSeparator)
            {
                result.Append('.');
            }
            else if (format.GroupingSeparator != null && character == format.GroupingSeparator)
            {
                result.Append(',');
            }
            else if (format.Digit != null && character == format.Digit)
            {
                result.Append('#');
            }
            else if (format.PatternSeparator != null && character == format.PatternSeparator)
            {
                result.Append(';');
            }
            else if (format.Percent != null && character == format.Percent)
            {
                result.Append('%');
            }
            else if (format.PerMille != null && character == format.PerMille)
            {
                result.Append('‰');
            }
            else if (format.ExponentSeparator != null && character == format.ExponentSeparator)
            {
                result.Append('e');
            }
            else
            {
                result.Append(character);
            }

            index = index + width;
        }

        return result.ToString();
    }

    internal string TranslateXPathDecimalFormatNumber(string source, XPathDecimalFormat format)
    {
        StringBuilder result = new StringBuilder();
        int zero = format.ZeroDigit == null ? -1 : char.ConvertToUtf32(format.ZeroDigit, 0);
        int index = 0;
        while (index < source.Length)
        {
            char character = source[index];
            if (zero >= 0 && character >= '0' && character <= '9')
            {
                result.Append(char.ConvertFromUtf32(zero + character - '0'));
            }
            else if (character == '.' && format.DecimalSeparator != null)
            {
                result.Append(format.DecimalSeparator);
            }
            else if (character == ',' && format.GroupingSeparator != null)
            {
                result.Append(format.GroupingSeparator);
            }
            else if (character == 'e' && format.ExponentSeparator != null)
            {
                result.Append(format.ExponentSeparator);
            }
            else
            {
                result.Append(character);
            }

            index = index + 1;
        }

        return result.ToString();
    }

    internal string TranslateXPathDecimalFormatAffix(string source, XPathDecimalFormat format)
    {
        string result = source;
        result = format.Percent == null ? result : result.Replace("%", format.Percent, StringComparison.Ordinal);
        result = format.PerMille == null ? result : result.Replace("‰", format.PerMille, StringComparison.Ordinal);
        result = result.Replace("\uE000", "#", StringComparison.Ordinal);
        result = result.Replace("\uE001", "0", StringComparison.Ordinal);
        return result;
    }

    internal XPathNumberPicture[] ParseXPathNumberPictures(string picture)
    {
        int separator = picture.IndexOf(';');
        if (separator >= 0 && picture.IndexOf(';', separator + 1) >= 0)
        {
            throw CreateXPathFormattingError("A number picture may contain at most two sub-pictures.", XPathErrorCodes.FODF1310);
        }

        if (separator < 0)
        {
            return [ParseXPathNumberSubPicture(picture)];
        }

        return [ParseXPathNumberSubPicture(picture.Substring(0, separator)), ParseXPathNumberSubPicture(picture.Substring(separator + 1))];
    }

    internal XPathNumberPicture ParseXPathNumberSubPicture(string source)
    {
        XPathNumberPicture result = new XPathNumberPicture();
        int percent = CountXPathPictureCharacter(source, '%');
        int perMille = CountXPathPictureCharacter(source, '‰');
        if (percent > 1 || perMille > 1 || percent > 0 && perMille > 0)
        {
            throw CreateXPathFormattingError("A number sub-picture has invalid percent or per-mille signs.", XPathErrorCodes.FODF1310);
        }

        result.Scale = percent == 1 ? 2 : perMille == 1 ? 3 : 0;
        int exponent = FindXPathPictureExponent(source);
        if (exponent >= 0 && result.Scale != 0)
        {
            throw CreateXPathFormattingError("A number sub-picture cannot combine an exponent with percent or per-mille.", XPathErrorCodes.FODF1310);
        }

        int firstActive = FindXPathPictureFirstActive(source, exponent);
        int lastActive = FindXPathPictureLastActive(source, exponent);
        if (firstActive < 0)
        {
            throw CreateXPathFormattingError("A number sub-picture requires a digit sign.", XPathErrorCodes.FODF1310);
        }

        result.Prefix = source.Substring(0, firstActive);
        result.Suffix = source.Substring(lastActive + 1);
        int index = firstActive;
        while (index <= lastActive)
        {
            if (!IsXPathNumberActiveCharacter(source[index], exponent == index))
            {
                throw CreateXPathFormattingError("A passive character cannot occur between active picture characters.", XPathErrorCodes.FODF1310);
            }

            index = index + 1;
        }

        string mantissa = exponent < 0 ? source.Substring(firstActive, lastActive - firstActive + 1) : source.Substring(firstActive, exponent - firstActive);
        string exponentPart = exponent < 0 ? "" : source.Substring(exponent + 1, lastActive - exponent);
        if (exponent >= 0)
        {
            if (exponentPart.Length == 0)
            {
                throw CreateXPathFormattingError("An exponent requires one or more mandatory digits.", XPathErrorCodes.FODF1310);
            }

            index = 0;
            while (index < exponentPart.Length)
            {
                if (!IsXPathAsciiDigit(exponentPart[index]))
                {
                    throw CreateXPathFormattingError("An exponent may contain only mandatory digits.", XPathErrorCodes.FODF1310);
                }

                index = index + 1;
            }

            result.MinimumExponentSize = exponentPart.Length;
        }

        int decimalSeparator = mantissa.IndexOf('.');
        if (decimalSeparator >= 0 && mantissa.IndexOf('.', decimalSeparator + 1) >= 0)
        {
            throw CreateXPathFormattingError("A number sub-picture may contain only one decimal separator.", XPathErrorCodes.FODF1310);
        }

        result.HasDecimalSeparator = decimalSeparator >= 0;
        string integerPart = decimalSeparator < 0 ? mantissa : mantissa.Substring(0, decimalSeparator);
        string fractionPart = decimalSeparator < 0 ? "" : mantissa.Substring(decimalSeparator + 1);
        ValidateXPathNumberPictureParts(integerPart, fractionPart, decimalSeparator >= 0);
        result.MinimumIntegerSize = CountXPathMandatoryPictureDigits(integerPart);
        result.ScalingFactor = result.MinimumIntegerSize;
        result.MinimumFractionSize = CountXPathMandatoryPictureDigits(fractionPart);
        result.MaximumFractionSize = CountXPathAllPictureDigits(fractionPart);
        int allMantissaDigits = CountXPathAllPictureDigits(integerPart) + result.MaximumFractionSize;
        if (allMantissaDigits == 0)
        {
            throw CreateXPathFormattingError("A number sub-picture requires a digit sign in its mantissa.", XPathErrorCodes.FODF1310);
        }

        if (result.MinimumIntegerSize == 0 && result.MaximumFractionSize == 0)
        {
            if (result.MinimumExponentSize > 0)
            {
                result.MinimumFractionSize = 1;
                result.MaximumFractionSize = 1;
                result.HasDecimalSeparator = true;
            }
            else
            {
                result.MinimumIntegerSize = 1;
            }
        }

        if (result.MinimumExponentSize > 0 && result.MinimumIntegerSize == 0 && integerPart.IndexOf('#') >= 0)
        {
            result.MinimumIntegerSize = 1;
        }

        if (result.MinimumIntegerSize == 0 && result.MinimumFractionSize == 0)
        {
            result.MinimumFractionSize = 1;
        }

        result.IntegerGroupingPositions = ReadXPathIntegerGroupingPositions(integerPart);
        result.RegularIntegerGroupingSize = ReadXPathRegularNumberGroupingSize(integerPart, result.IntegerGroupingPositions);
        result.FractionGroupingPositions = ReadXPathFractionGroupingPositions(fractionPart);
        return result;
    }

    internal int FindXPathPictureExponent(string source)
    {
        int found = -1;
        int index = 0;
        while (index < source.Length)
        {
            if (source[index] == 'e' && HasXPathPictureActiveBefore(source, index) && HasXPathPictureActiveAfter(source, index))
            {
                if (found >= 0)
                {
                    throw CreateXPathFormattingError("A number sub-picture may contain only one exponent separator.", XPathErrorCodes.FODF1310);
                }

                found = index;
            }

            index = index + 1;
        }

        return found;
    }

    internal bool HasXPathPictureActiveBefore(string source, int position)
    {
        int index = position - 1;
        while (index >= 0)
        {
            if (source[index] == '#' || IsXPathAsciiDigit(source[index]) || source[index] == '.' || source[index] == ',')
            {
                return true;
            }

            index = index - 1;
        }

        return false;
    }

    internal bool HasXPathPictureActiveAfter(string source, int position)
    {
        int index = position + 1;
        while (index < source.Length)
        {
            if (source[index] == '#' || IsXPathAsciiDigit(source[index]) || source[index] == '.' || source[index] == ',')
            {
                return true;
            }

            index = index + 1;
        }

        return false;
    }

    internal int FindXPathPictureFirstActive(string source, int exponent)
    {
        int index = 0;
        while (index < source.Length)
        {
            if (IsXPathNumberActiveCharacter(source[index], exponent == index))
            {
                return index;
            }

            index = index + 1;
        }

        return -1;
    }

    internal int FindXPathPictureLastActive(string source, int exponent)
    {
        int index = source.Length - 1;
        while (index >= 0)
        {
            if (IsXPathNumberActiveCharacter(source[index], exponent == index))
            {
                return index;
            }

            index = index - 1;
        }

        return -1;
    }

    internal bool IsXPathNumberActiveCharacter(char character, bool exponent)
    {
        return character == '#' || IsXPathAsciiDigit(character) || character == '.' || character == ',' || exponent;
    }

    internal void ValidateXPathNumberPictureParts(string integerPart, string fractionPart, bool hasDecimal)
    {
        if (integerPart.Length > 0 && integerPart[integerPart.Length - 1] == ',' || fractionPart.Length > 0 && fractionPart[0] == ',')
        {
            throw CreateXPathFormattingError("A grouping separator cannot be adjacent to a decimal separator or end the integer part.", XPathErrorCodes.FODF1310);
        }

        bool mandatorySeen = false;
        int index = 0;
        while (index < integerPart.Length)
        {
            char character = integerPart[index];
            if (character == ',' && (index + 1 == integerPart.Length && !hasDecimal || index > 0 && integerPart[index - 1] == ',' || index + 1 < integerPart.Length && integerPart[index + 1] == ','))
            {
                throw CreateXPathFormattingError("A number picture contains adjacent or misplaced grouping separators.", XPathErrorCodes.FODF1310);
            }

            if (IsXPathAsciiDigit(character))
            {
                mandatorySeen = true;
            }
            else if (character == '#' && mandatorySeen)
            {
                throw CreateXPathFormattingError("An optional integer digit cannot follow a mandatory digit.", XPathErrorCodes.FODF1310);
            }

            index = index + 1;
        }

        bool optionalSeen = false;
        index = 0;
        while (index < fractionPart.Length)
        {
            char character = fractionPart[index];
            if (character == ',' && (index > 0 && fractionPart[index - 1] == ',' || index + 1 < fractionPart.Length && fractionPart[index + 1] == ','))
            {
                throw CreateXPathFormattingError("A number picture contains adjacent or misplaced grouping separators.", XPathErrorCodes.FODF1310);
            }

            if (character == '#')
            {
                optionalSeen = true;
            }
            else if (IsXPathAsciiDigit(character) && optionalSeen)
            {
                throw CreateXPathFormattingError("A mandatory fractional digit cannot follow an optional digit.", XPathErrorCodes.FODF1310);
            }

            index = index + 1;
        }
    }

    internal int[] ReadXPathIntegerGroupingPositions(string part)
    {
        List<int> positions = new List<int>();
        int index = 0;
        while (index < part.Length)
        {
            if (part[index] == ',')
            {
                positions.Add(CountXPathAllPictureDigits(part.Substring(index + 1)));
            }

            index = index + 1;
        }

        return positions.ToArray();
    }

    internal int ReadXPathRegularNumberGroupingSize(string part, int[] positions)
    {
        if (positions.Length == 0)
        {
            return 0;
        }

        int groupSize = positions[positions.Length - 1];
        bool regular = groupSize > 0;
        int index = 0;
        while (index < positions.Length)
        {
            if (positions[index] % groupSize != 0)
            {
                regular = false;
            }

            index = index + 1;
        }

        int total = CountXPathAllPictureDigits(part);
        int expected = groupSize;
        while (regular && expected < total)
        {
            bool found = false;
            index = 0;
            while (index < positions.Length)
            {
                if (positions[index] == expected)
                {
                    found = true;
                }

                index = index + 1;
            }

            regular = found;
            expected = expected + groupSize;
        }

        return regular ? groupSize : 0;
    }

    internal int[] ReadXPathFractionGroupingPositions(string part)
    {
        List<int> positions = new List<int>();
        int digits = 0;
        int index = 0;
        while (index < part.Length)
        {
            if (part[index] == ',')
            {
                positions.Add(digits);
            }
            else
            {
                digits = digits + 1;
            }

            index = index + 1;
        }

        return positions.ToArray();
    }

    internal string FormatXPathDecimalNumber(XdmDecimalStorage supplied, XPathNumberPicture picture)
    {
        BigInteger coefficient = BigInteger.Abs(supplied.Coefficient);
        int scale = supplied.Scale;
        if (scale > picture.MaximumFractionSize)
        {
            coefficient = Numerics.DivideXdmHalfToEven(coefficient, Numerics.PowerOfTen(scale - picture.MaximumFractionSize));
            scale = picture.MaximumFractionSize;
        }

        if (scale < 0)
        {
            coefficient = coefficient * Numerics.PowerOfTen(-scale);
            scale = 0;
        }

        string digits = coefficient.ToString(CultureInfo.InvariantCulture);
        string integerPart;
        string fractionPart;
        if (scale == 0)
        {
            integerPart = digits;
            fractionPart = "";
        }
        else if (digits.Length <= scale)
        {
            integerPart = "";
            fractionPart = new string('0', scale - digits.Length) + digits;
        }
        else
        {
            integerPart = digits.Substring(0, digits.Length - scale);
            fractionPart = digits.Substring(digits.Length - scale);
        }

        integerPart = integerPart.TrimStart('0');
        fractionPart = fractionPart.TrimEnd('0');
        integerPart = integerPart.PadLeft(picture.MinimumIntegerSize, '0');
        fractionPart = fractionPart.PadRight(picture.MinimumFractionSize, '0');
        integerPart = InsertXPathNumberIntegerGrouping(integerPart, picture.IntegerGroupingPositions, picture.RegularIntegerGroupingSize);
        fractionPart = InsertXPathNumberFractionGrouping(fractionPart, picture.FractionGroupingPositions);
        if (picture.HasDecimalSeparator && fractionPart.Length > 0)
        {
            return integerPart + "." + fractionPart;
        }

        return integerPart;
    }

    internal string InsertXPathNumberIntegerGrouping(string digits, int[] positions, int regularSize)
    {
        StringBuilder result = new StringBuilder();
        int digitIndex = 0;
        while (digitIndex < digits.Length)
        {
            int position = digits.Length - digitIndex;
            bool grouping = regularSize > 0 && position < digits.Length && position % regularSize == 0;
            int index = 0;
            while (!grouping && index < positions.Length)
            {
                if (position < digits.Length && positions[index] == position)
                {
                    grouping = true;
                }

                index = index + 1;
            }

            if (grouping)
            {
                result.Append(',');
            }

            result.Append(digits[digitIndex]);
            digitIndex = digitIndex + 1;
        }

        return result.ToString();
    }

    internal string InsertXPathNumberFractionGrouping(string digits, int[] positions)
    {
        StringBuilder result = new StringBuilder(digits);
        int inserted = 0;
        int index = 0;
        while (index < positions.Length)
        {
            int position = positions[index];
            if (position > 0 && position < digits.Length)
            {
                result.Insert(position + inserted, ',');
                inserted = inserted + 1;
            }

            index = index + 1;
        }

        return result.ToString();
    }

    internal XPathNumberFormattingValue ReadXPathNumberFormattingValue(XdmAtomicValue atomic)
    {
        if (StaticContext.CompatibilityMode == XPathCompatibilityMode.BackwardsCompatible && (Types.IsXdmStringDerivedAtomicType(atomic.Type) || atomic.Type == XdmAtomicType.UntypedAtomic))
        {
            XdmConversionResult backwardsConversion = Converter.TryCastXdmAtomic(atomic, XdmAtomicType.Double, StaticContext);
            if (!backwardsConversion.Success || backwardsConversion.Value == null)
            {
                return CreateXPathNotANumberValue();
            }

            atomic = backwardsConversion.Value;
        }

        if (atomic.Type == XdmAtomicType.UntypedAtomic)
        {
            XdmConversionResult conversion = Converter.TryCastXdmAtomic(atomic, XdmAtomicType.Double, StaticContext);
            if (!conversion.Success || conversion.Value == null)
            {
                throw CreateXPathFormattingError("The untyped value cannot be converted to a number.", XPathErrorCodes.FORG0001);
            }

            atomic = conversion.Value;
        }

        if (!Types.IsXdmNumericAtomicType(atomic.Type))
        {
            throw CreateXPathFormattingError("fn:format-number requires a numeric value.", XPathErrorCodes.XPTY0004);
        }

        XPathNumberFormattingValue result = new XPathNumberFormattingValue();
        if (atomic.Type == XdmAtomicType.Float || atomic.Type == XdmAtomicType.Double)
        {
            double floating = atomic.Type == XdmAtomicType.Float ? (float)atomic.FloatingValue : atomic.FloatingValue;
            result.Floating = true;
            result.FloatingValue = floating;
            result.NotANumber = double.IsNaN(floating);
            result.Infinite = double.IsInfinity(floating);
            result.Negative = floating < 0 || floating == 0 && BitConverter.DoubleToInt64Bits(floating) < 0;
            if (!result.NotANumber && !result.Infinite)
            {
                ParseXPathFloatingDecimal(new XdmFloatingDatatype().CanonicalizeXdmFloating(floating, atomic.Type), result);
            }

            return result;
        }

        XdmDecimalStorage storage = Numerics.ReadXdmDecimalStorage(atomic);
        result.Negative = storage.Coefficient < BigInteger.Zero;
        result.Coefficient = BigInteger.Abs(storage.Coefficient);
        result.Scale = storage.Scale;
        return result;
    }

    internal void ParseXPathFloatingDecimal(string canonical, XPathNumberFormattingValue result)
    {
        string source = canonical;
        if (source.Length > 0 && source[0] == '-')
        {
            source = source.Substring(1);
        }

        int exponentIndex = source.IndexOf('E');
        int exponent = exponentIndex < 0 ? 0 : int.Parse(source.Substring(exponentIndex + 1), CultureInfo.InvariantCulture);
        string mantissa = exponentIndex < 0 ? source : source.Substring(0, exponentIndex);
        int decimalIndex = mantissa.IndexOf('.');
        string digits = decimalIndex < 0 ? mantissa : mantissa.Substring(0, decimalIndex) + mantissa.Substring(decimalIndex + 1);
        result.Coefficient = BigInteger.Parse(digits, CultureInfo.InvariantCulture);
        result.Scale = (decimalIndex < 0 ? 0 : mantissa.Length - decimalIndex - 1) - exponent;
    }

    internal XPathNumberFormattingValue CreateXPathNotANumberValue()
    {
        XPathNumberFormattingValue result = new XPathNumberFormattingValue();
        result.NotANumber = true;
        return result;
    }

    internal void ValidateXPathDecimalFormatName(string supplied)
    {
        string name = new XdmStringDatatype().CollapseXdmStringWhitespace(supplied);
        XmlName? expanded = null;
        if (name.StartsWith("Q{", StringComparison.Ordinal))
        {
            int closing = name.IndexOf('}');
            string local = closing < 0 ? "" : name.Substring(closing + 1);
            if (closing >= 2 && name.IndexOf('{', 2) < 0 && name.IndexOf('}', closing + 1) < 0 && new XdmQNameDatatype().ParseXdmQNameLexical(local) != null)
            {
                expanded = new XmlName();
                expanded.NamespaceUri = name.Substring(2, closing - 2);
                expanded.LocalName = local;
            }
        }
        else
        {
            XmlNamespaceBinding[] namespaces = StaticContext.Namespaces == null ? [] : StaticContext.Namespaces;
            XdmNumericParseResult parsed = new XdmQNameDatatype().TryParseXdmQNameWithNamespaces(name, XdmAtomicType.QName, namespaces, null);
            if (parsed.Success && parsed.Value != null)
            {
                expanded = parsed.Value.NameValue;
            }
        }

        if (expanded == null || !HasXPathNamedDecimalFormat(expanded))
        {
            throw CreateXPathFormattingError("The decimal format name is invalid or is not declared.", XPathErrorCodes.FODF1280);
        }
    }

    internal bool HasXPathNamedDecimalFormat(XmlName name)
    {
        XmlName[] formats = StaticContext.DecimalFormats == null ? [] : StaticContext.DecimalFormats;
        int index = 0;
        while (index < formats.Length)
        {
            XmlName? candidate = formats[index];
            if (candidate != null && candidate.LocalName == name.LocalName && (candidate.NamespaceUri == null ? "" : candidate.NamespaceUri) == (name.NamespaceUri == null ? "" : name.NamespaceUri))
            {
                return true;
            }

            index = index + 1;
        }

        return false;
    }

    internal int CountXPathPictureCharacter(string source, char character)
    {
        int count = 0;
        int index = 0;
        while (index < source.Length)
        {
            if (source[index] == character)
            {
                count = count + 1;
            }

            index = index + 1;
        }

        return count;
    }

    internal int CountXPathMandatoryPictureDigits(string source)
    {
        int count = 0;
        int index = 0;
        while (index < source.Length)
        {
            if (IsXPathAsciiDigit(source[index]))
            {
                count = count + 1;
            }

            index = index + 1;
        }

        return count;
    }

    internal int CountXPathAllPictureDigits(string source)
    {
        int count = 0;
        int index = 0;
        while (index < source.Length)
        {
            if (source[index] == '#' || IsXPathAsciiDigit(source[index]))
            {
                count = count + 1;
            }

            index = index + 1;
        }

        return count;
    }

    internal bool IsXPathAsciiDigit(char character)
    {
        return character >= '0' && character <= '9';
    }

    internal int ReadXPathDigitValue(char character)
    {
        if (char.GetUnicodeCategory(character) != UnicodeCategory.DecimalDigitNumber)
        {
            return -1;
        }

        double value = char.GetNumericValue(character);
        int digit = (int)value;
        return digit >= 0 && digit <= 9 && value == digit ? digit : -1;
    }

    internal int ReadXPathDigitValueAt(string source, int index)
    {
        return CharUnicodeInfo.GetUnicodeCategory(source, index) == UnicodeCategory.DecimalDigitNumber ? CharUnicodeInfo.GetDecimalDigitValue(source, index) : -1;
    }

    internal int ReadXPathCodepointWidth(string source, int index)
    {
        return char.IsHighSurrogate(source[index]) && index + 1 < source.Length && char.IsLowSurrogate(source[index + 1]) ? 2 : 1;
    }

    internal string TranslateXPathFormattingDigits(string source, int zero)
    {
        if (zero == '0')
        {
            return source;
        }

        StringBuilder result = new StringBuilder();
        int index = 0;
        while (index < source.Length)
        {
            char character = source[index];
            result.Append(character >= '0' && character <= '9' ? char.ConvertFromUtf32(zero + character - '0') : character.ToString());
            index = index + 1;
        }

        return result.ToString();
    }

    internal string FormatXPathFormattingRoman(BigInteger value)
    {
        if (value <= BigInteger.Zero || value > new BigInteger(3999))
        {
            return value.ToString(CultureInfo.InvariantCulture);
        }

        int remaining = (int)value;
        int[] values = [1000, 900, 500, 400, 100, 90, 50, 40, 10, 9, 5, 4, 1];
        string[] symbols = ["M", "CM", "D", "CD", "C", "XC", "L", "XL", "X", "IX", "V", "IV", "I"];
        StringBuilder result = new StringBuilder();
        int index = 0;
        while (index < values.Length)
        {
            while (remaining >= values[index])
            {
                result.Append(symbols[index]);
                remaining = remaining - values[index];
            }

            index = index + 1;
        }

        return result.ToString();
    }

    internal string FormatXPathFormattingAlphabetic(BigInteger value)
    {
        if (value <= BigInteger.Zero)
        {
            return value.ToString(CultureInfo.InvariantCulture);
        }

        StringBuilder reversed = new StringBuilder();
        BigInteger remaining = value;
        while (remaining > BigInteger.Zero)
        {
            remaining = remaining - BigInteger.One;
            reversed.Append((char)('A' + (int)(remaining % 26)));
            remaining = remaining / 26;
        }

        StringBuilder result = new StringBuilder();
        int index = reversed.Length - 1;
        while (index >= 0)
        {
            result.Append(reversed[index]);
            index = index - 1;
        }

        return result.ToString();
    }

    internal string FormatXPathFormattingWords(BigInteger value)
    {
        if (value < BigInteger.Zero || value > new BigInteger(999999))
        {
            return value.ToString(CultureInfo.InvariantCulture);
        }

        int number = (int)value;
        if (number == 0)
        {
            return "Zero";
        }

        string result = "";
        if (number >= 1000)
        {
            result = FormatXPathFormattingWordsUnderThousand(number / 1000) + " Thousand";
            number = number % 1000;
        }

        if (number > 0)
        {
            result = result + (result.Length == 0 ? "" : number < 100 ? " and " : " ") + FormatXPathFormattingWordsUnderThousand(number);
        }

        return result;
    }

    internal string FormatXPathFormattingOrdinalWords(BigInteger value)
    {
        string cardinal = FormatXPathFormattingWords(value);
        string[] cardinals = ["One", "Two", "Three", "Four", "Five", "Six", "Seven", "Eight", "Nine", "Ten", "Eleven", "Twelve", "Thirteen", "Fourteen", "Fifteen", "Sixteen", "Seventeen", "Eighteen", "Nineteen", "Twenty", "Thirty", "Forty", "Fifty", "Sixty", "Seventy", "Eighty", "Ninety", "Hundred", "Thousand"];
        string[] ordinals = ["First", "Second", "Third", "Fourth", "Fifth", "Sixth", "Seventh", "Eighth", "Ninth", "Tenth", "Eleventh", "Twelfth", "Thirteenth", "Fourteenth", "Fifteenth", "Sixteenth", "Seventeenth", "Eighteenth", "Nineteenth", "Twentieth", "Thirtieth", "Fortieth", "Fiftieth", "Sixtieth", "Seventieth", "Eightieth", "Ninetieth", "Hundredth", "Thousandth"];
        int index = 0;
        while (index < cardinals.Length)
        {
            if (cardinal.EndsWith(cardinals[index], StringComparison.Ordinal))
            {
                return cardinal.Substring(0, cardinal.Length - cardinals[index].Length) + ordinals[index];
            }

            index = index + 1;
        }

        return cardinal + ReadXPathFormattingOrdinalSuffix(value);
    }

    internal string FormatXPathFormattingWordsUnderThousand(int value)
    {
        string[] small = ["Zero", "One", "Two", "Three", "Four", "Five", "Six", "Seven", "Eight", "Nine", "Ten", "Eleven", "Twelve", "Thirteen", "Fourteen", "Fifteen", "Sixteen", "Seventeen", "Eighteen", "Nineteen"];
        string[] tens = ["", "", "Twenty", "Thirty", "Forty", "Fifty", "Sixty", "Seventy", "Eighty", "Ninety"];
        string result = "";
        if (value >= 100)
        {
            result = small[value / 100] + " Hundred";
            value = value % 100;
            if (value > 0)
            {
                result = result + " and ";
            }
        }

        if (value >= 20)
        {
            result = result + tens[value / 10];
            if (value % 10 > 0)
            {
                result = result + "-" + small[value % 10];
            }
        }
        else if (value > 0)
        {
            result = result + small[value];
        }

        return result;
    }

    internal string ReadXPathFormattingOrdinalSuffix(BigInteger value)
    {
        int lastTwo = (int)(BigInteger.Abs(value) % 100);
        if (lastTwo >= 11 && lastTwo <= 13)
        {
            return "th";
        }

        int last = (int)(BigInteger.Abs(value) % 10);
        if (last == 1)
        {
            return "st";
        }

        if (last == 2)
        {
            return "nd";
        }

        return last == 3 ? "rd" : "th";
    }

    internal XdmAtomicValue? ReadXPathFormattingOptionalAtomic(XdmSequence supplied)
    {
        XdmSequence atomized = Converter.AtomizeXdmSequence(supplied);
        if (atomized.Count > 1)
        {
            throw CreateXPathFormattingError("A formatting function argument must contain at most one atomic value.", XPathErrorCodes.XPTY0004);
        }

        return atomized.Count == 0 ? null : Sequences.ReadXdmSequenceItem(atomized, 0).AtomicValue;
    }

    internal XdmAtomicValue? ReadXPathFormattingOptionalInteger(XdmSequence supplied)
    {
        XdmSequenceType expected = new XdmSequenceType();
        expected.ItemKind = XdmItemKind.Atomic;
        expected.AtomicType = XdmAtomicType.Integer;
        expected.Occurrence = XdmOccurrence.ZeroOrOne;
        XdmSequence converted = Converter.ApplyXdmFunctionConversion(supplied, expected, StaticContext);
        return converted.Count == 0 ? null : Sequences.ReadXdmSequenceItem(converted, 0).AtomicValue;
    }

    internal string ReadXPathFormattingRequiredString(XdmSequence supplied)
    {
        XdmSequenceType expected = new XdmSequenceType();
        expected.ItemKind = XdmItemKind.Atomic;
        expected.AtomicType = XdmAtomicType.String;
        expected.Occurrence = XdmOccurrence.ExactlyOne;
        XdmSequence converted = Converter.ApplyXdmFunctionConversion(supplied, expected, StaticContext);
        XdmAtomicValue? value = Sequences.ReadXdmSequenceItem(converted, 0).AtomicValue;
        return value == null ? "" : Converter.ReadXdmAtomicStringValue(value);
    }

    internal string? ReadXPathFormattingOptionalString(XdmSequence supplied)
    {
        XdmSequenceType expected = new XdmSequenceType();
        expected.ItemKind = XdmItemKind.Atomic;
        expected.AtomicType = XdmAtomicType.String;
        expected.Occurrence = XdmOccurrence.ZeroOrOne;
        XdmSequence converted = Converter.ApplyXdmFunctionConversion(supplied, expected, StaticContext);
        if (converted.Count == 0)
        {
            return null;
        }

        XdmAtomicValue? value = Sequences.ReadXdmSequenceItem(converted, 0).AtomicValue;
        return value == null ? null : Converter.ReadXdmAtomicStringValue(value);
    }

    internal XPathDynamicException CreateXPathFormattingError(string message, string code)
    {
        XmlDiagnostic diagnostic = new XmlDiagnostic();
        diagnostic.Code = code;
        diagnostic.Category = XmlErrorCategory.XPathDynamic;
        diagnostic.Severity = XmlDiagnosticSeverity.Fatal;
        diagnostic.Message = message;
        diagnostic.Range = Range;
        return new XPathDynamicException(message, diagnostic, Range.Start);
    }
}
