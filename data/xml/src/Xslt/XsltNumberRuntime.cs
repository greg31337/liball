using System;
using System.Collections.Generic;
using System.Globalization;
using System.Numerics;
using System.Text;

namespace xml;

internal class XsltNumberFormatTokens
{
    internal string Prefix;
    internal string Suffix;
    internal string[] Formats;
    internal string[] Separators;

    internal XsltNumberFormatTokens()
    {
        Prefix = "";
        Suffix = "";
        Formats = [];
        Separators = [];
    }
}

internal class XsltNumberRuntime
{
    internal XsltSequenceConstructorRuntime Runtime;
    internal XsltTemplateRuntime Templates;
    internal XdmSequenceOperations Sequences;
    internal XdmTypeConverter Converter;
    internal XdmNumericDatatype Numerics;
    internal XsltPatternMatcher Patterns;

    internal XsltNumberRuntime(XsltSequenceConstructorRuntime runtime)
    {
        Runtime = runtime;
        Templates = runtime.Templates;
        Sequences = runtime.Sequences;
        Converter = runtime.Converter;
        Numerics = new XdmNumericDatatype();
        Patterns = new XsltPatternMatcher();
    }

    internal XdmSequence EvaluateXsltNumber(XsltSequenceItemPlan item, XsltTemplateInvocation invocation)
    {
        XsltInstructionPlan instruction = item.Instruction == null ? throw new InvalidOperationException("A number instruction has no plan.") : item.Instruction;
        XsltInstructionAttributePlan? value = Runtime.FindXsltAttribute(instruction, "value");
        List<BigInteger> numbers = value == null ? SelectXsltNumberPlaceMarker(instruction, invocation) : EvaluateXsltNumberValue(value, instruction, invocation);
        ApplyXsltNumberStartAt(numbers, instruction, invocation);
        string formatted = FormatXsltNumberSequence(numbers, instruction, invocation);
        return Runtime.CreateXsltTextNodeSequence(formatted, item.Node);
    }

    internal List<BigInteger> EvaluateXsltNumberValue(XsltInstructionAttributePlan value, XsltInstructionPlan instruction, XsltTemplateInvocation invocation)
    {
        XdmSequence supplied = value.Expression == null || invocation.Frame == null ? Sequences.CreateEmptyXdmSequence() : Templates.EvaluateXsltInstructionExpression(value.Expression, invocation.Frame);
        XdmSequence atomized = Converter.AtomizeXdmSequence(supplied);
        List<BigInteger> result = new List<BigInteger>();
        int index = 0;
        while (index < atomized.Count)
        {
            XdmAtomicValue? atomic = Sequences.ReadXdmSequenceItem(atomized, index).AtomicValue;
            XdmConversionResult converted = atomic == null ? new XdmConversionResult(false, false, null, null, null) : Converter.TryCastXdmAtomic(atomic, XdmAtomicType.Double, new XPathStaticContext());
            if (!converted.Success || converted.Value == null)
            {
                throw Runtime.CreateXsltConstructionError("A value supplied to xsl:number cannot be converted to an integer.", XsltErrorCodes.XTDE0980, instruction.Instruction);
            }

            double number = new XdmFloatingDatatype().ReadXdmFloatingValue(converted.Value, XdmAtomicType.Double);
            if (double.IsNaN(number) || double.IsInfinity(number))
            {
                throw Runtime.CreateXsltConstructionError("A value supplied to xsl:number must be finite.", XsltErrorCodes.XTDE0980, instruction.Instruction);
            }

            double rounded = Math.Round(number, MidpointRounding.ToPositiveInfinity);
            if (rounded < 0d)
            {
                throw Runtime.CreateXsltConstructionError("A value supplied to xsl:number must not be negative after rounding.", XsltErrorCodes.XTDE0980, instruction.Instruction);
            }

            result.Add(new BigInteger(rounded));
            index = index + 1;
        }

        return result;
    }

    internal List<BigInteger> SelectXsltNumberPlaceMarker(XsltInstructionPlan instruction, XsltTemplateInvocation invocation)
    {
        XmlNode selected = SelectXsltNumberNode(instruction, invocation);
        string level = ReadXsltNumberLiteralAttribute(instruction, "level", "single");
        if (level == "multiple")
        {
            return SelectXsltMultipleNumber(selected, instruction, invocation);
        }

        if (level == "any")
        {
            return SelectXsltAnyNumber(selected, instruction, invocation);
        }

        return SelectXsltSingleNumber(selected, instruction, invocation);
    }

    internal XmlNode SelectXsltNumberNode(XsltInstructionPlan instruction, XsltTemplateInvocation invocation)
    {
        XsltInstructionAttributePlan? select = Runtime.FindXsltAttribute(instruction, "select");
        XdmSequence selected = select == null ? invocation.ContextItem : select.Expression == null || invocation.Frame == null ? Sequences.CreateEmptyXdmSequence() : Templates.EvaluateXsltInstructionExpression(select.Expression, invocation.Frame);
        if (selected.Count != 1)
        {
            throw Runtime.CreateXsltConstructionTypeError(select == null ? "xsl:number requires a context node when select is absent." : "The select expression of xsl:number must return exactly one node.", select == null ? XsltErrorCodes.XTTE0990 : XsltErrorCodes.XTTE1000, instruction.Instruction);
        }

        XdmSequenceItem selectedItem = Sequences.ReadXdmSequenceItem(selected, 0);
        if (selectedItem.Node == null)
        {
            throw Runtime.CreateXsltConstructionTypeError(select == null ? "The context item of xsl:number must be a node." : "The select expression of xsl:number must return a node.", select == null ? XsltErrorCodes.XTTE0990 : XsltErrorCodes.XTTE1000, instruction.Instruction);
        }

        return selectedItem.Node;
    }

    internal List<BigInteger> SelectXsltSingleNumber(XmlNode selected, XsltInstructionPlan instruction, XsltTemplateInvocation invocation)
    {
        XmlNode? counted = selected;
        while (counted != null && !MatchesXsltCount(counted, selected, instruction, invocation))
        {
            counted = counted.Parent;
        }

        XmlNode? boundary = FindXsltNumberBoundary(selected, instruction, invocation);
        List<BigInteger> result = new List<BigInteger>();
        if (counted == null || boundary == null || !IsXsltNumberDescendantOrSelf(counted, boundary))
        {
            return result;
        }

        result.Add(new BigInteger(CountXsltNumberPrecedingSiblings(counted, selected, instruction, invocation) + 1));
        return result;
    }

    internal List<BigInteger> SelectXsltMultipleNumber(XmlNode selected, XsltInstructionPlan instruction, XsltTemplateInvocation invocation)
    {
        XmlNode? boundary = FindXsltNumberBoundary(selected, instruction, invocation);
        List<XmlNode> ancestors = new List<XmlNode>();
        XmlNode? current = selected;
        while (current != null)
        {
            if (MatchesXsltCount(current, selected, instruction, invocation) && boundary != null && IsXsltNumberDescendantOrSelf(current, boundary))
            {
                ancestors.Insert(0, current);
            }

            current = current.Parent;
        }

        List<BigInteger> result = new List<BigInteger>();
        int index = 0;
        while (index < ancestors.Count)
        {
            result.Add(new BigInteger(CountXsltNumberPrecedingSiblings(ancestors[index], selected, instruction, invocation) + 1));
            index = index + 1;
        }

        return result;
    }

    internal List<BigInteger> SelectXsltAnyNumber(XmlNode selected, XsltInstructionPlan instruction, XsltTemplateInvocation invocation)
    {
        XmlNode root = selected;
        while (root.Parent != null)
        {
            root = root.Parent;
        }

        List<XmlNode> order = new List<XmlNode>();
        XmlNode stop = (selected.Kind == XmlNodeKind.Attribute || selected.Kind == XmlNodeKind.Namespace) && selected.Parent != null ? selected.Parent : selected;
        AddXsltNumberDocumentOrder(order, root, stop);
        if (!object.ReferenceEquals(stop, selected))
        {
            order.Add(selected);
        }
        int boundaryIndex = 0;
        XsltInstructionAttributePlan? from = Runtime.FindXsltAttribute(instruction, "from");
        bool boundaryFound = from == null;
        int index = 0;
        while (index < order.Count)
        {
            if (from == null ? object.ReferenceEquals(order[index], root) : MatchesXsltNumberPattern(order[index], from, invocation))
            {
                boundaryIndex = index;
                boundaryFound = true;
            }

            index = index + 1;
        }

        int count = 0;
        index = boundaryIndex;
        while (index < order.Count)
        {
            if (MatchesXsltCount(order[index], selected, instruction, invocation))
            {
                count = count + 1;
            }

            index = index + 1;
        }

        List<BigInteger> result = new List<BigInteger>();
        if (!boundaryFound)
        {
            return result;
        }

        if (count > 0)
        {
            result.Add(new BigInteger(count));
        }

        return result;
    }

    internal bool AddXsltNumberDocumentOrder(List<XmlNode> result, XmlNode node, XmlNode stop)
    {
        if (node.Kind != XmlNodeKind.Attribute && node.Kind != XmlNodeKind.Namespace)
        {
            result.Add(node);
        }

        if (object.ReferenceEquals(node, stop))
        {
            return true;
        }

        XmlNode[] children = node.Children == null ? [] : node.Children;
        int index = 0;
        while (index < children.Length)
        {
            if (AddXsltNumberDocumentOrder(result, children[index], stop))
            {
                return true;
            }

            index = index + 1;
        }

        return false;
    }

    internal XmlNode? FindXsltNumberBoundary(XmlNode selected, XsltInstructionPlan instruction, XsltTemplateInvocation invocation)
    {
        XsltInstructionAttributePlan? from = Runtime.FindXsltAttribute(instruction, "from");
        XmlNode? current = selected;
        while (current != null)
        {
            if (from == null ? current.Parent == null : MatchesXsltNumberPattern(current, from, invocation))
            {
                return current;
            }

            current = current.Parent;
        }

        return null;
    }

    internal int CountXsltNumberPrecedingSiblings(XmlNode node, XmlNode selected, XsltInstructionPlan instruction, XsltTemplateInvocation invocation)
    {
        XmlNode? parent = node.Parent;
        XmlNode[] siblings = parent == null || parent.Children == null ? [] : parent.Children;
        int count = 0;
        int index = 0;
        while (index < siblings.Length && !object.ReferenceEquals(siblings[index], node))
        {
            if (MatchesXsltCount(siblings[index], selected, instruction, invocation))
            {
                count = count + 1;
            }

            index = index + 1;
        }

        return count;
    }

    internal bool MatchesXsltCount(XmlNode node, XmlNode selected, XsltInstructionPlan instruction, XsltTemplateInvocation invocation)
    {
        XsltInstructionAttributePlan? count = Runtime.FindXsltAttribute(instruction, "count");
        if (count != null)
        {
            return MatchesXsltNumberPattern(node, count, invocation);
        }

        if (node.Kind != selected.Kind)
        {
            return false;
        }

        if (selected.Name == null)
        {
            return node.Name == null;
        }

        return node.Name != null && node.Name.LocalName == selected.Name.LocalName && node.Name.NamespaceUri == selected.Name.NamespaceUri;
    }

    internal bool MatchesXsltNumberPattern(XmlNode node, XsltInstructionAttributePlan attribute, XsltTemplateInvocation invocation)
    {
        if (attribute.Pattern == null)
        {
            return false;
        }

        XPathDynamicContext context = invocation.Frame == null ? Templates.State.DynamicContext : invocation.Frame.Context;
        return Patterns.MatchesXsltPattern(attribute.Pattern, Sequences.CreateXdmNodeSequence(node), context);
    }

    internal bool IsXsltNumberDescendantOrSelf(XmlNode node, XmlNode ancestor)
    {
        XmlNode? current = node;
        while (current != null)
        {
            if (object.ReferenceEquals(current, ancestor))
            {
                return true;
            }

            current = current.Parent;
        }

        return false;
    }

    internal void ApplyXsltNumberStartAt(List<BigInteger> numbers, XsltInstructionPlan instruction, XsltTemplateInvocation invocation)
    {
        XsltInstructionAttributePlan? attribute = Runtime.FindXsltAttribute(instruction, "start-at");
        if (attribute == null)
        {
            return;
        }

        string source = Runtime.EvaluateXsltAttributeTemplate(attribute, invocation, "").Trim();
        string[] parts = source.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length == 0)
        {
            throw Runtime.CreateXsltConstructionError("The start-at attribute must contain one or more integers.", XsltErrorCodes.XTDE0030, instruction.Instruction);
        }

        BigInteger[] starts = new BigInteger[parts.Length];
        int index = 0;
        while (index < parts.Length)
        {
            if (!IsXsltNumberIntegerLexical(parts[index]))
            {
                throw Runtime.CreateXsltConstructionError("The start-at attribute must be a whitespace-separated sequence of integers.", XsltErrorCodes.XTDE0030, instruction.Instruction);
            }

            starts[index] = BigInteger.Parse(parts[index], CultureInfo.InvariantCulture);
            index = index + 1;
        }

        index = 0;
        while (index < numbers.Count)
        {
            int startIndex = index < starts.Length ? index : starts.Length - 1;
            numbers[index] = numbers[index] + starts[startIndex] - BigInteger.One;
            index = index + 1;
        }
    }

    internal string FormatXsltNumberSequence(List<BigInteger> numbers, XsltInstructionPlan instruction, XsltTemplateInvocation invocation)
    {
        string picture = Runtime.EvaluateXsltAttributeTemplate(Runtime.FindXsltAttribute(instruction, "format"), invocation, "1");
        string language = Runtime.EvaluateXsltAttributeTemplate(Runtime.FindXsltAttribute(instruction, "lang"), invocation, "").Trim();
        string letterValue = Runtime.EvaluateXsltAttributeTemplate(Runtime.FindXsltAttribute(instruction, "letter-value"), invocation, "").Trim();
        string ordinal = Runtime.EvaluateXsltAttributeTemplate(Runtime.FindXsltAttribute(instruction, "ordinal"), invocation, "").Trim();
        string groupingSeparator = Runtime.EvaluateXsltAttributeTemplate(Runtime.FindXsltAttribute(instruction, "grouping-separator"), invocation, "");
        string groupingSizeSource = Runtime.EvaluateXsltAttributeTemplate(Runtime.FindXsltAttribute(instruction, "grouping-size"), invocation, "").Trim();
        ValidateXsltNumberFormattingAttributes(language, letterValue, groupingSizeSource, instruction);
        if (numbers.Count == 0)
        {
            return "";
        }

        int groupingSize = ReadXsltNumberGroupingSize(groupingSizeSource);
        bool grouping = Runtime.FindXsltAttribute(instruction, "grouping-separator") != null && Runtime.FindXsltAttribute(instruction, "grouping-size") != null && groupingSize > 0;
        XsltNumberFormatTokens tokens = TokenizeXsltNumberFormat(picture);
        StringBuilder result = new StringBuilder(tokens.Prefix);
        int index = 0;
        while (index < numbers.Count)
        {
            int formatIndex = index < tokens.Formats.Length ? index : tokens.Formats.Length - 1;
            if (index > 0)
            {
                int separatorIndex = formatIndex - 1;
                result.Append(separatorIndex >= 0 && separatorIndex < tokens.Separators.Length ? tokens.Separators[separatorIndex] : ".");
            }

            result.Append(FormatXsltNumberValue(numbers[index], tokens.Formats[formatIndex], language, letterValue, ordinal, grouping ? groupingSeparator : "", grouping ? groupingSize : 0));
            index = index + 1;
        }

        result.Append(tokens.Suffix);
        return result.ToString();
    }

    internal void ValidateXsltNumberFormattingAttributes(string language, string letterValue, string groupingSize, XsltInstructionPlan instruction)
    {
        if (language.Length != 0 && !IsXsltNumberLanguage(language))
        {
            throw Runtime.CreateXsltConstructionError("The lang attribute of xsl:number must be an xs:language value.", XsltErrorCodes.XTDE0030, instruction.Instruction);
        }

        if (letterValue.Length != 0 && letterValue != "alphabetic" && letterValue != "traditional")
        {
            throw Runtime.CreateXsltConstructionError("The letter-value attribute of xsl:number must be alphabetic or traditional.", XsltErrorCodes.XTDE0030, instruction.Instruction);
        }

        if (groupingSize.Length != 0 && !IsXsltNumberGroupingIntegerLexical(groupingSize))
        {
            throw Runtime.CreateXsltConstructionError("The grouping-size attribute of xsl:number must be an integer.", XsltErrorCodes.XTDE0030, instruction.Instruction);
        }
    }

    internal int ReadXsltNumberGroupingSize(string source)
    {
        if (source.Length == 0)
        {
            return 0;
        }

        BigInteger value = BigInteger.Parse(source, CultureInfo.InvariantCulture);
        return value <= BigInteger.Zero || value > new BigInteger(1000000) ? 0 : (int)value;
    }

    internal XsltNumberFormatTokens TokenizeXsltNumberFormat(string picture)
    {
        List<string> parts = new List<string>();
        List<bool> kinds = new List<bool>();
        int start = 0;
        while (start < picture.Length)
        {
            bool alphanumeric = IsXsltNumberAlphanumeric(picture, start);
            int end = start + Rune.GetRuneAt(picture, start).Utf16SequenceLength;
            while (end < picture.Length && IsXsltNumberAlphanumeric(picture, end) == alphanumeric)
            {
                end = end + Rune.GetRuneAt(picture, end).Utf16SequenceLength;
            }

            parts.Add(picture.Substring(start, end - start));
            kinds.Add(alphanumeric);
            start = end;
        }

        XsltNumberFormatTokens result = new XsltNumberFormatTokens();
        List<string> formats = new List<string>();
        List<string> separators = new List<string>();
        int firstFormat = -1;
        int lastFormat = -1;
        int index = 0;
        while (index < parts.Count)
        {
            if (kinds[index])
            {
                if (firstFormat < 0)
                {
                    firstFormat = index;
                }

                lastFormat = index;
                formats.Add(parts[index]);
            }

            index = index + 1;
        }

        if (formats.Count == 0)
        {
            result.Prefix = picture;
            result.Suffix = picture;
            result.Formats = ["1"];
            return result;
        }

        result.Prefix = firstFormat > 0 ? parts[0] : "";
        result.Suffix = lastFormat + 1 < parts.Count ? parts[parts.Count - 1] : "";
        index = firstFormat + 1;
        while (index < lastFormat)
        {
            if (!kinds[index])
            {
                separators.Add(parts[index]);
            }

            index = index + 1;
        }

        result.Formats = formats.ToArray();
        result.Separators = separators.ToArray();
        return result;
    }

    internal string FormatXsltNumberValue(BigInteger value, string token, string language, string letterValue, string ordinal, string groupingSeparator, int groupingSize)
    {
        if (IsXsltNumberDecimalToken(token))
        {
            int zero = ReadXsltNumberDecimalZero(token);
            int minimum = CountXsltNumberCodepoints(token);
            string result = FormatXsltNumberDecimal(value, zero, minimum);
            if (groupingSize > 0)
            {
                result = GroupXsltNumberDecimal(result, groupingSeparator, groupingSize);
            }

            return result + ReadXsltNumberOrdinalSuffix(value, ordinal, language);
        }

        XPathNumberFormattingFunctionImplementation formatter = new XPathNumberFormattingFunctionImplementation("format-integer", new SourceRange(), new XPathStaticContext());
        bool ordinalWords = IsXsltNumberOrdinal(ordinal);
        if (token == "A" || token == "a" || token == "I" || token == "i" || token == "W" || token == "w" || token == "Ww")
        {
            return formatter.FormatXPathIntegerValue(value, token + (ordinalWords ? ";o" : ""));
        }

        if (letterValue == "alphabetic" && token.Length == 1 && char.IsLetter(token[0]))
        {
            string alphabetic = formatter.FormatXPathFormattingAlphabetic(value);
            return char.IsLower(token[0]) ? alphabetic.ToLowerInvariant() : alphabetic;
        }

        return FormatXsltNumberDecimal(value, (int)'0', 1) + ReadXsltNumberOrdinalSuffix(value, ordinal, language);
    }

    internal bool IsXsltNumberDecimalToken(string token)
    {
        if (token.Length == 0)
        {
            return false;
        }

        int lastIndex = FindXsltNumberLastCodepointIndex(token);
        if (CharUnicodeInfo.GetNumericValue(token, lastIndex) != 1d)
        {
            return false;
        }

        int zero = Rune.GetRuneAt(token, lastIndex).Value - 1;
        int index = 0;
        while (index < lastIndex)
        {
            Rune current = Rune.GetRuneAt(token, index);
            if (current.Value != zero)
            {
                return false;
            }

            index = index + current.Utf16SequenceLength;
        }

        return true;
    }

    internal int ReadXsltNumberDecimalZero(string token)
    {
        return Rune.GetRuneAt(token, FindXsltNumberLastCodepointIndex(token)).Value - 1;
    }

    internal int FindXsltNumberLastCodepointIndex(string source)
    {
        int index = 0;
        int last = 0;
        while (index < source.Length)
        {
            last = index;
            index = index + Rune.GetRuneAt(source, index).Utf16SequenceLength;
        }

        return last;
    }

    internal int CountXsltNumberCodepoints(string source)
    {
        int count = 0;
        int index = 0;
        while (index < source.Length)
        {
            index = index + Rune.GetRuneAt(source, index).Utf16SequenceLength;
            count = count + 1;
        }

        return count;
    }

    internal string FormatXsltNumberDecimal(BigInteger value, int zero, int minimum)
    {
        bool negative = value < BigInteger.Zero;
        string ascii = BigInteger.Abs(value).ToString(CultureInfo.InvariantCulture);
        if (ascii.Length < minimum)
        {
            ascii = new string('0', minimum - ascii.Length) + ascii;
        }

        if (zero == (int)'0')
        {
            return negative ? "-" + ascii : ascii;
        }

        StringBuilder result = new StringBuilder();
        int index = 0;
        while (index < ascii.Length)
        {
            result.Append(char.ConvertFromUtf32(zero + ascii[index] - '0'));
            index = index + 1;
        }

        return negative ? "-" + result.ToString() : result.ToString();
    }

    internal string GroupXsltNumberDecimal(string source, string separator, int size)
    {
        string sign = source.Length > 0 && source[0] == '-' ? "-" : "";
        string digits = sign.Length == 0 ? source : source.Substring(1);
        StringBuilder result = new StringBuilder();
        int first = digits.Length % size;
        int index = 0;
        while (index < digits.Length)
        {
            if (index > 0 && (index - first) % size == 0)
            {
                result.Append(separator);
            }

            result.Append(digits[index]);
            index = index + 1;
        }

        return sign + result.ToString();
    }

    internal string ReadXsltNumberOrdinalSuffix(BigInteger value, string ordinal, string language)
    {
        if (IsXsltNumberOrdinal(ordinal))
        {
            return new XPathNumberFormattingFunctionImplementation("format-integer", new SourceRange(), new XPathStaticContext()).ReadXPathFormattingOrdinalSuffix(value);
        }

        return ordinal.Length > 1 && ordinal[0] == '-' ? ordinal.Substring(1) : "";
    }

    internal bool IsXsltNumberOrdinal(string ordinal)
    {
        return ordinal == "yes" || ordinal == "true" || ordinal == "1";
    }

    internal bool IsXsltNumberIntegerLexical(string source)
    {
        if (source.Length == 0)
        {
            return false;
        }

        int index = source[0] == '-' ? 1 : 0;
        if (index == source.Length)
        {
            return false;
        }

        while (index < source.Length)
        {
            if (source[index] < '0' || source[index] > '9')
            {
                return false;
            }

            index = index + 1;
        }

        return true;
    }

    internal bool IsXsltNumberGroupingIntegerLexical(string source)
    {
        if (source.Length == 0)
        {
            return false;
        }

        int index = source[0] == '-' || source[0] == '+' ? 1 : 0;
        if (index == source.Length)
        {
            return false;
        }

        while (index < source.Length)
        {
            if (source[index] < '0' || source[index] > '9')
            {
                return false;
            }

            index = index + 1;
        }

        return true;
    }

    internal bool IsXsltNumberLanguage(string source)
    {
        int partLength = 0;
        int index = 0;
        while (index < source.Length)
        {
            char character = source[index];
            if (character == '-')
            {
                if (partLength == 0 || partLength > 8)
                {
                    return false;
                }

                partLength = 0;
            }
            else if (character >= 'A' && character <= 'Z' || character >= 'a' && character <= 'z' || partLength > 0 && character >= '0' && character <= '9')
            {
                partLength = partLength + 1;
            }
            else
            {
                return false;
            }

            index = index + 1;
        }

        return partLength > 0 && partLength <= 8;
    }

    internal bool IsXsltNumberAlphanumeric(string source, int index)
    {
        UnicodeCategory category = Rune.GetUnicodeCategory(Rune.GetRuneAt(source, index));
        return category == UnicodeCategory.DecimalDigitNumber || category == UnicodeCategory.LetterNumber || category == UnicodeCategory.OtherNumber || category == UnicodeCategory.UppercaseLetter || category == UnicodeCategory.LowercaseLetter || category == UnicodeCategory.TitlecaseLetter || category == UnicodeCategory.ModifierLetter || category == UnicodeCategory.OtherLetter;
    }

    internal string ReadXsltNumberLiteralAttribute(XsltInstructionPlan instruction, string name, string fallback)
    {
        XsltInstructionAttributePlan? attribute = Runtime.FindXsltAttribute(instruction, name);
        return attribute == null ? fallback : attribute.Source;
    }
}
