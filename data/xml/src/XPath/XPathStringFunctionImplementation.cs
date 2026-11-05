using System;
using System.Collections.Generic;
using System.Globalization;
using System.Numerics;
using System.Text;

namespace xml;

internal class XPathStringFunctionImplementation : IXPathFunction
{
    internal string LocalName;
    internal SourceRange Range;
    internal XPathStaticContext StaticContext;
    internal XdmSequenceOperations Sequences;
    internal XdmTypeConverter Converter;
    internal XdmTypeSystem Types;
    internal XdmNumericDatatype Numerics;
    internal XdmFactory Factory;
    internal XdmStringDatatype Strings;

    internal XPathStringFunctionImplementation(string localName, SourceRange range, XPathStaticContext staticContext)
    {
        LocalName = localName;
        Range = CopyXPathStringRange(range);
        StaticContext = staticContext;
        Sequences = new XdmSequenceOperations();
        Converter = new XdmTypeConverter();
        Types = new XdmTypeSystem();
        Numerics = new XdmNumericDatatype();
        Factory = new XdmFactory();
        Strings = new XdmStringDatatype();
    }

    public XdmSequence InvokeXPathFunction(XPathFunctionBinding binding, XdmSequence[] arguments, XPathDynamicContext context)
    {
        if (LocalName == "codepoints-to-string")
        {
            return EvaluateXPathCodepointsToString(arguments[0]);
        }

        if (LocalName == "string-to-codepoints")
        {
            return EvaluateXPathStringToCodepoints(arguments[0]);
        }

        if (LocalName == "compare" || LocalName == "codepoint-equal")
        {
            return EvaluateXPathStringComparison(arguments);
        }

        if (LocalName == "collation-key")
        {
            return EvaluateXPathCollationKey(arguments);
        }

        if (LocalName == "contains-token")
        {
            return EvaluateXPathContainsToken(arguments);
        }

        if (LocalName == "concat")
        {
            return EvaluateXPathConcat(arguments);
        }

        if (LocalName == "string-join")
        {
            return EvaluateXPathStringJoin(arguments);
        }

        if (LocalName == "substring")
        {
            return EvaluateXPathSubstring(arguments);
        }

        if (LocalName == "string-length" || LocalName == "normalize-space")
        {
            return EvaluateXPathContextStringFunction(arguments, context);
        }

        if (LocalName == "normalize-unicode")
        {
            return EvaluateXPathNormalizeUnicode(arguments);
        }

        if (LocalName == "upper-case" || LocalName == "lower-case")
        {
            return EvaluateXPathCaseMapping(arguments[0]);
        }

        if (LocalName == "translate")
        {
            return EvaluateXPathTranslate(arguments);
        }

        if (LocalName == "contains" || LocalName == "starts-with" || LocalName == "ends-with" || LocalName == "substring-before" || LocalName == "substring-after")
        {
            return EvaluateXPathSubstringMatch(arguments);
        }

        throw new InvalidOperationException("The XPath string function is not implemented.");
    }

    internal XdmSequence EvaluateXPathCodepointsToString(XdmSequence supplied)
    {
        XdmSequence atomized = Converter.AtomizeXdmSequence(supplied);
        StringBuilder result = new StringBuilder();
        int index = 0;
        while (index < atomized.Count)
        {
            XdmAtomicValue value = ReadXPathAtomicValue(atomized, index);
            if (value.Type == XdmAtomicType.UntypedAtomic)
            {
                value = RequireXPathStringConversion(Converter.TryCastXdmAtomic(value, XdmAtomicType.Integer, StaticContext));
            }

            if (!Types.IsXdmAtomicSubtypeOf(value.Type, XdmAtomicType.Integer))
            {
                throw CreateXPathStringError("fn:codepoints-to-string requires integer values.", XPathErrorCodes.XPTY0004);
            }

            BigInteger integer = Numerics.ReadXdmIntegerValue(value);
            if (integer < BigInteger.Zero || integer > new BigInteger(1114111))
            {
                throw CreateXPathStringError("The supplied codepoint is not a permitted XML character.", XPathErrorCodes.FOCH0001);
            }

            int codepoint = (int)integer;
            if (!Xml10CharacterRules.IsLegalXml10Character(codepoint))
            {
                throw CreateXPathStringError("The supplied codepoint is not a permitted XML character.", XPathErrorCodes.FOCH0001);
            }

            result.Append(char.ConvertFromUtf32(codepoint));
            index = index + 1;
        }

        return CreateXPathStringResult(result.ToString());
    }

    internal XdmSequence EvaluateXPathStringToCodepoints(XdmSequence supplied)
    {
        string? optional = ReadXPathOptionalString(supplied);
        if (optional == null || optional.Length == 0)
        {
            return Sequences.CreateEmptyXdmSequence();
        }

        XdmSequenceBuilder result = new XdmSequenceBuilder();
        UnicodeScalarScanner scanner = new UnicodeScalarScanner(optional.AsMemory());
        while (!scanner.IsUnicodeScalarEnd())
        {
            UnicodeScalar scalar = scanner.ReadUnicodeScalar();
            XdmAtomicValue value = Factory.CreateXdmIntegerAtomicValue(scalar.Value.ToString(CultureInfo.InvariantCulture), XdmAtomicType.Integer);
            result.AddXdmAtomicValue(value);
        }

        return result.BuildImmutableXdmSequence();
    }

    internal XdmSequence EvaluateXPathStringComparison(XdmSequence[] arguments)
    {
        string? left = ReadXPathOptionalString(arguments[0]);
        string? right = ReadXPathOptionalString(arguments[1]);
        if (left == null || right == null)
        {
            return Sequences.CreateEmptyXdmSequence();
        }

        IXPathCollation collation = LocalName == "codepoint-equal" ? new XPathCodepointCollation() : ResolveXPathCollation(arguments, 2);
        int comparison = collation.CompareXPathCollation(left, right);
        if (LocalName == "codepoint-equal")
        {
            return CreateXPathBooleanResult(comparison == 0);
        }

        int normalized = comparison < 0 ? -1 : comparison > 0 ? 1 : 0;
        return CreateXPathIntegerResult(normalized);
    }

    internal XdmSequence EvaluateXPathCollationKey(XdmSequence[] arguments)
    {
        string value = ReadXPathRequiredString(arguments[0]);
        IXPathCollation collation = ResolveXPathCollation(arguments, 1);
        byte[] key = collation.CreateXPathCollationKey(value);
        string lexical = Convert.ToBase64String(key);
        return CreateXPathAtomicResult(Factory.CreateXdmBinaryAtomicValue(lexical, XdmAtomicType.Base64Binary));
    }

    internal XdmSequence EvaluateXPathContainsToken(XdmSequence[] arguments)
    {
        string token = Strings.CollapseXdmStringWhitespace(ReadXPathRequiredString(arguments[1]));
        if (token.Length == 0)
        {
            return CreateXPathBooleanResult(false);
        }

        IXPathCollation collation = ResolveXPathCollation(arguments, 2);
        XdmSequence atomized = Converter.AtomizeXdmSequence(arguments[0]);
        int index = 0;
        while (index < atomized.Count)
        {
            string input = ReadXPathStringValue(ReadXPathAtomicValue(atomized, index));
            if (ContainsXPathToken(input, token, collation))
            {
                return CreateXPathBooleanResult(true);
            }

            index = index + 1;
        }

        return CreateXPathBooleanResult(false);
    }

    internal bool ContainsXPathToken(string input, string token, IXPathCollation collation)
    {
        int index = 0;
        while (index < input.Length)
        {
            while (index < input.Length && IsXPathWhitespace(input[index]))
            {
                index = index + 1;
            }

            int start = index;
            while (index < input.Length && !IsXPathWhitespace(input[index]))
            {
                index = index + 1;
            }

            if (index > start && collation.CompareXPathCollation(input.Substring(start, index - start), token) == 0)
            {
                return true;
            }
        }

        return false;
    }

    internal XdmSequence EvaluateXPathConcat(XdmSequence[] arguments)
    {
        StringBuilder result = new StringBuilder();
        int index = 0;
        while (index < arguments.Length)
        {
            result.Append(ReadXPathOptionalAtomicString(arguments[index]));
            index = index + 1;
        }

        return CreateXPathStringResult(result.ToString());
    }

    internal XdmSequence EvaluateXPathStringJoin(XdmSequence[] arguments)
    {
        string separator = arguments.Length == 1 ? "" : ReadXPathRequiredString(arguments[1]);
        XdmSequence atomized = Converter.AtomizeXdmSequence(arguments[0]);
        StringBuilder result = new StringBuilder();
        int index = 0;
        while (index < atomized.Count)
        {
            if (index > 0)
            {
                result.Append(separator);
            }

            result.Append(Converter.ReadXdmAtomicStringValue(ReadXPathAtomicValue(atomized, index)));
            index = index + 1;
        }

        return CreateXPathStringResult(result.ToString());
    }

    internal XdmSequence EvaluateXPathSubstring(XdmSequence[] arguments)
    {
        string source = ReadXPathOptionalString(arguments[0]) ?? "";
        double start = RoundXPathSubstringNumber(ReadXPathDouble(arguments[1]));
        double end = double.PositiveInfinity;
        if (arguments.Length == 3)
        {
            end = start + RoundXPathSubstringNumber(ReadXPathDouble(arguments[2]));
        }

        StringBuilder result = new StringBuilder();
        UnicodeScalarScanner scanner = new UnicodeScalarScanner(source.AsMemory());
        int position = 1;
        while (!scanner.IsUnicodeScalarEnd())
        {
            UnicodeScalar scalar = scanner.ReadUnicodeScalar();
            if (start <= position && position < end)
            {
                result.Append(source, scalar.Utf16Offset, scalar.Utf16CodeUnitCount);
            }

            position = position + 1;
        }

        return CreateXPathStringResult(result.ToString());
    }

    internal double RoundXPathSubstringNumber(double value)
    {
        if (double.IsNaN(value) || double.IsInfinity(value) || value == 0)
        {
            return value;
        }

        return Math.Floor(value + 0.5);
    }

    internal XdmSequence EvaluateXPathContextStringFunction(XdmSequence[] arguments, XPathDynamicContext context)
    {
        string value = arguments.Length == 0 ? ReadXPathContextString(context) : ReadXPathOptionalString(arguments[0]) ?? "";
        if (LocalName == "normalize-space")
        {
            return CreateXPathStringResult(Strings.CollapseXdmStringWhitespace(value));
        }

        int length = CountXPathCodepoints(value);
        return CreateXPathIntegerResult(length);
    }

    internal string ReadXPathContextString(XPathDynamicContext context)
    {
        XdmSequence? sequence = context.ContextItem;
        if (sequence == null || sequence.Count == 0)
        {
            throw CreateXPathStringError("The context item is absent.", XPathErrorCodes.XPDY0002);
        }

        if (sequence.Count != 1)
        {
            throw CreateXPathStringError("The context item must be a single item.", XPathErrorCodes.XPTY0004);
        }

        XdmSequenceItem item = Sequences.ReadValidatedXdmSequenceItem(sequence, 0);
        if (item.Kind == XdmItemKind.Node && item.Node != null)
        {
            XdmNodeAccessor accessor = new XdmNodeAccessor();
            return accessor.GetXdmNodeStringValue(accessor.WrapXmlNode(item.Node));
        }

        if (item.Kind == XdmItemKind.Atomic && item.AtomicValue != null)
        {
            return Converter.ReadXdmAtomicStringValue(item.AtomicValue);
        }

        throw CreateXPathStringError("The context item has no string value.", XPathErrorCodes.XPTY0004);
    }

    internal XdmSequence EvaluateXPathNormalizeUnicode(XdmSequence[] arguments)
    {
        string value = ReadXPathOptionalString(arguments[0]) ?? "";
        string form = arguments.Length == 1 ? "NFC" : Strings.CollapseXdmStringWhitespace(ReadXPathRequiredString(arguments[1])).ToUpperInvariant();
        if (form.Length == 0)
        {
            return CreateXPathStringResult(value);
        }

        if (form == "NFC")
        {
            return CreateXPathStringResult(value.Normalize(NormalizationForm.FormC));
        }

        if (form == "NFD")
        {
            return CreateXPathStringResult(value.Normalize(NormalizationForm.FormD));
        }

        if (form == "NFKC")
        {
            return CreateXPathStringResult(value.Normalize(NormalizationForm.FormKC));
        }

        if (form == "NFKD")
        {
            return CreateXPathStringResult(value.Normalize(NormalizationForm.FormKD));
        }

        throw CreateXPathStringError("The requested Unicode normalization form is not supported.", XPathErrorCodes.FOCH0003);
    }

    internal XdmSequence EvaluateXPathCaseMapping(XdmSequence supplied)
    {
        string value = ReadXPathOptionalString(supplied) ?? "";
        string mapped = MapXPathUnicodeCase(value, LocalName == "upper-case");
        return CreateXPathStringResult(mapped);
    }

    internal string MapXPathUnicodeCase(string value, bool upper)
    {
        List<int> codepoints = ReadXPathCodepointList(value);
        StringBuilder result = new StringBuilder(value.Length);
        int index = 0;
        while (index < codepoints.Count)
        {
            int codepoint = codepoints[index];
            string? special = upper ? ReadXPathSpecialUppercase(codepoint) : ReadXPathSpecialLowercase(codepoints, index);
            if (special != null)
            {
                result.Append(special);
            }
            else
            {
                string character = char.ConvertFromUtf32(codepoint);
                result.Append(upper ? character.ToUpperInvariant() : character.ToLowerInvariant());
            }

            index = index + 1;
        }

        return result.ToString();
    }

    internal string? ReadXPathSpecialLowercase(List<int> codepoints, int index)
    {
        int codepoint = codepoints[index];
        if (codepoint == 0x0130)
        {
            return "i\u0307";
        }

        if (codepoint == 0x03A3 && IsXPathFinalSigma(codepoints, index))
        {
            return "\u03C2";
        }

        return null;
    }

    internal bool IsXPathFinalSigma(List<int> codepoints, int index)
    {
        bool preceded = false;
        int previous = index - 1;
        while (previous >= 0)
        {
            int codepoint = codepoints[previous];
            if (!IsXPathCaseIgnorable(codepoint))
            {
                preceded = IsXPathCasedCodepoint(codepoint);
                break;
            }

            previous = previous - 1;
        }

        if (!preceded)
        {
            return false;
        }

        int following = index + 1;
        while (following < codepoints.Count)
        {
            int codepoint = codepoints[following];
            if (!IsXPathCaseIgnorable(codepoint))
            {
                return !IsXPathCasedCodepoint(codepoint);
            }

            following = following + 1;
        }

        return true;
    }

    internal bool IsXPathCasedCodepoint(int codepoint)
    {
        UnicodeCategory category = CharUnicodeInfo.GetUnicodeCategory(char.ConvertFromUtf32(codepoint), 0);
        return category == UnicodeCategory.UppercaseLetter || category == UnicodeCategory.LowercaseLetter || category == UnicodeCategory.TitlecaseLetter;
    }

    internal bool IsXPathCaseIgnorable(int codepoint)
    {
        UnicodeCategory category = CharUnicodeInfo.GetUnicodeCategory(char.ConvertFromUtf32(codepoint), 0);
        if (category == UnicodeCategory.NonSpacingMark || category == UnicodeCategory.EnclosingMark || category == UnicodeCategory.Format || category == UnicodeCategory.ModifierLetter || category == UnicodeCategory.ModifierSymbol)
        {
            return true;
        }

        return codepoint == 0x0027 || codepoint == 0x00AD || codepoint == 0x2019;
    }

    internal string? ReadXPathSpecialUppercase(int codepoint)
    {
        if (codepoint >= 0x1F80 && codepoint <= 0x1F8F)
        {
            return char.ConvertFromUtf32(0x1F08 + (codepoint & 7)) + "\u0399";
        }

        if (codepoint >= 0x1F90 && codepoint <= 0x1F9F)
        {
            return char.ConvertFromUtf32(0x1F28 + (codepoint & 7)) + "\u0399";
        }

        if (codepoint >= 0x1FA0 && codepoint <= 0x1FAF)
        {
            return char.ConvertFromUtf32(0x1F68 + (codepoint & 7)) + "\u0399";
        }

        switch (codepoint)
        {
            case 0x00DF:
                return "SS";
            case 0x0149:
                return "\u02BCN";
            case 0x01F0:
                return "J\u030C";
            case 0x0390:
                return "\u0399\u0308\u0301";
            case 0x03B0:
                return "\u03A5\u0308\u0301";
            case 0x0587:
                return "\u0535\u0552";
            case 0x1E96:
                return "H\u0331";
            case 0x1E97:
                return "T\u0308";
            case 0x1E98:
                return "W\u030A";
            case 0x1E99:
                return "Y\u030A";
            case 0x1E9A:
                return "A\u02BE";
            case 0x1F50:
                return "\u03A5\u0313";
            case 0x1F52:
                return "\u03A5\u0313\u0300";
            case 0x1F54:
                return "\u03A5\u0313\u0301";
            case 0x1F56:
                return "\u03A5\u0313\u0342";
            case 0x1FB2:
                return "\u1FBA\u0399";
            case 0x1FB3:
            case 0x1FBC:
                return "\u0391\u0399";
            case 0x1FB4:
                return "\u0386\u0399";
            case 0x1FB6:
                return "\u0391\u0342";
            case 0x1FB7:
                return "\u0391\u0342\u0399";
            case 0x1FC2:
                return "\u1FCA\u0399";
            case 0x1FC3:
            case 0x1FCC:
                return "\u0397\u0399";
            case 0x1FC4:
                return "\u0389\u0399";
            case 0x1FC6:
                return "\u0397\u0342";
            case 0x1FC7:
                return "\u0397\u0342\u0399";
            case 0x1FD2:
                return "\u0399\u0308\u0300";
            case 0x1FD3:
                return "\u0399\u0308\u0301";
            case 0x1FD6:
                return "\u0399\u0342";
            case 0x1FD7:
                return "\u0399\u0308\u0342";
            case 0x1FE2:
                return "\u03A5\u0308\u0300";
            case 0x1FE3:
                return "\u03A5\u0308\u0301";
            case 0x1FE4:
                return "\u03A1\u0313";
            case 0x1FE6:
                return "\u03A5\u0342";
            case 0x1FE7:
                return "\u03A5\u0308\u0342";
            case 0x1FF2:
                return "\u1FFA\u0399";
            case 0x1FF3:
            case 0x1FFC:
                return "\u03A9\u0399";
            case 0x1FF4:
                return "\u038F\u0399";
            case 0x1FF6:
                return "\u03A9\u0342";
            case 0x1FF7:
                return "\u03A9\u0342\u0399";
            case 0xFB00:
                return "FF";
            case 0xFB01:
                return "FI";
            case 0xFB02:
                return "FL";
            case 0xFB03:
                return "FFI";
            case 0xFB04:
                return "FFL";
            case 0xFB05:
            case 0xFB06:
                return "ST";
            case 0xFB13:
                return "\u0544\u0546";
            case 0xFB14:
                return "\u0544\u0535";
            case 0xFB15:
                return "\u0544\u053B";
            case 0xFB16:
                return "\u054E\u0546";
            case 0xFB17:
                return "\u0544\u053D";
            default:
                return null;
        }
    }

    internal XdmSequence EvaluateXPathTranslate(XdmSequence[] arguments)
    {
        string value = ReadXPathOptionalString(arguments[0]) ?? "";
        string map = ReadXPathRequiredString(arguments[1]);
        string replacement = ReadXPathRequiredString(arguments[2]);
        List<int> mapCodepoints = ReadXPathCodepointList(map);
        List<int> replacementCodepoints = ReadXPathCodepointList(replacement);
        StringBuilder result = new StringBuilder();
        UnicodeScalarScanner scanner = new UnicodeScalarScanner(value.AsMemory());
        while (!scanner.IsUnicodeScalarEnd())
        {
            UnicodeScalar scalar = scanner.ReadUnicodeScalar();
            int position = FindXPathCodepoint(mapCodepoints, scalar.Value);
            if (position < 0)
            {
                result.Append(char.ConvertFromUtf32(scalar.Value));
            }
            else if (position < replacementCodepoints.Count)
            {
                result.Append(char.ConvertFromUtf32(replacementCodepoints[position]));
            }
        }

        return CreateXPathStringResult(result.ToString());
    }

    internal XdmSequence EvaluateXPathSubstringMatch(XdmSequence[] arguments)
    {
        string source = ReadXPathOptionalString(arguments[0]) ?? "";
        string sought = ReadXPathOptionalString(arguments[1]) ?? "";
        IXPathCollation collation = ResolveXPathCollation(arguments, 2);
        int[] match = FindXPathCollationMatch(source, sought, collation, LocalName == "starts-with", LocalName == "ends-with");
        bool found = match[0] >= 0;
        if (LocalName == "contains" || LocalName == "starts-with" || LocalName == "ends-with")
        {
            return CreateXPathBooleanResult(found);
        }

        if (!found)
        {
            return CreateXPathStringResult("");
        }

        string result = LocalName == "substring-before" ? source.Substring(0, match[0]) : source.Substring(match[1]);
        return CreateXPathStringResult(result);
    }

    internal int[] FindXPathCollationMatch(string source, string sought, IXPathCollation collation, bool requireStart, bool requireEnd)
    {
        List<int> boundaries = ReadXPathStringBoundaries(source);
        int startIndex = 0;
        while (startIndex < boundaries.Count)
        {
            int start = boundaries[startIndex];
            if (requireStart && start != 0)
            {
                break;
            }

            int endIndex = startIndex;
            while (endIndex < boundaries.Count)
            {
                int end = boundaries[endIndex];
                if ((!requireEnd || end == source.Length) && collation.CompareXPathCollation(source.Substring(start, end - start), sought) == 0)
                {
                    return [start, end];
                }

                endIndex = endIndex + 1;
            }

            startIndex = startIndex + 1;
        }

        return [-1, -1];
    }

    internal IXPathCollation ResolveXPathCollation(XdmSequence[] arguments, int collationIndex)
    {
        string uri = arguments.Length > collationIndex ? ReadXPathRequiredString(arguments[collationIndex]) : StaticContext.DefaultCollation ?? XPathNamespaces.CodepointCollation;
        uri = ResolveXPathCollationUri(uri);
        XPathCollationBinding[] bindings = StaticContext.CollationBindings == null ? [] : StaticContext.CollationBindings;
        int index = 0;
        while (index < bindings.Length)
        {
            XPathCollationBinding? binding = bindings[index];
            if (binding != null && binding.Uri == uri && binding.Implementation != null)
            {
                return binding.Implementation;
            }

            index = index + 1;
        }

        if (uri == XPathNamespaces.CodepointCollation)
        {
            return new XPathCodepointCollation();
        }

        if (uri == XPathAsciiCaseInsensitiveCollation.Uri)
        {
            return new XPathAsciiCaseInsensitiveCollation();
        }

        throw CreateXPathStringError("The requested XPath collation is not supported.", XPathErrorCodes.FOCH0002);
    }

    internal string ResolveXPathCollationUri(string value)
    {
        if (Uri.IsWellFormedUriString(value, UriKind.Absolute))
        {
            return new Uri(value, UriKind.Absolute).AbsoluteUri;
        }

        string? baseUri = StaticContext.BaseUri;
        if (baseUri == null || !Uri.IsWellFormedUriString(value, UriKind.Relative))
        {
            throw CreateXPathStringError("The collation URI cannot be resolved.", XPathErrorCodes.FOCH0002);
        }

        return new Uri(new Uri(baseUri, UriKind.Absolute), value).AbsoluteUri;
    }

    internal string? ReadXPathOptionalString(XdmSequence supplied)
    {
        XdmSequence atomized = Converter.AtomizeXdmSequence(supplied);
        if (atomized.Count == 0)
        {
            return null;
        }

        if (atomized.Count != 1)
        {
            throw CreateXPathStringError("A string argument must atomize to at most one value.", XPathErrorCodes.XPTY0004);
        }

        return ReadXPathStringValue(ReadXPathAtomicValue(atomized, 0));
    }

    internal string ReadXPathRequiredString(XdmSequence supplied)
    {
        string? result = ReadXPathOptionalString(supplied);
        if (result == null)
        {
            throw CreateXPathStringError("A required string argument is empty.", XPathErrorCodes.XPTY0004);
        }

        return result;
    }

    internal string ReadXPathStringValue(XdmAtomicValue value)
    {
        bool supported = Types.IsXdmStringDerivedAtomicType(value.Type) || value.Type == XdmAtomicType.UntypedAtomic || value.Type == XdmAtomicType.AnyUri;
        if (!supported)
        {
            throw CreateXPathStringError("The function argument must be a string.", XPathErrorCodes.XPTY0004);
        }

        return Converter.ReadXdmAtomicStringValue(value);
    }

    internal string ReadXPathOptionalAtomicString(XdmSequence supplied)
    {
        XdmSequence atomized = Converter.AtomizeXdmSequence(supplied);
        if (atomized.Count == 0)
        {
            return "";
        }

        if (atomized.Count != 1)
        {
            throw CreateXPathStringError("A concat argument must atomize to at most one atomic value.", XPathErrorCodes.XPTY0004);
        }

        return Converter.ReadXdmAtomicStringValue(ReadXPathAtomicValue(atomized, 0));
    }

    internal double ReadXPathDouble(XdmSequence supplied)
    {
        XdmSequence atomized = Converter.AtomizeXdmSequence(supplied);
        if (atomized.Count != 1)
        {
            throw CreateXPathStringError("A substring numeric argument must contain exactly one value.", XPathErrorCodes.XPTY0004);
        }

        XdmAtomicValue value = ReadXPathAtomicValue(atomized, 0);
        if (value.Type == XdmAtomicType.UntypedAtomic)
        {
            value = RequireXPathStringConversion(Converter.TryCastXdmAtomic(value, XdmAtomicType.Double, StaticContext));
        }
        else if (Types.IsXdmNumericAtomicType(value.Type) && value.Type != XdmAtomicType.Double)
        {
            value = Numerics.PromoteXdmNumeric(value, XdmAtomicType.Double);
        }

        if (value.Type != XdmAtomicType.Double)
        {
            throw CreateXPathStringError("A substring numeric argument must be convertible to xs:double.", XPathErrorCodes.XPTY0004);
        }

        return value.FloatingValue;
    }

    internal XdmAtomicValue ReadXPathAtomicValue(XdmSequence sequence, int index)
    {
        XdmAtomicValue? value = Sequences.ReadValidatedXdmSequenceItem(sequence, index).AtomicValue;
        if (value == null)
        {
            throw new InvalidOperationException("Atomization did not produce an atomic value.");
        }

        return value;
    }

    internal XdmAtomicValue RequireXPathStringConversion(XdmConversionResult conversion)
    {
        if (conversion.Success && conversion.Value != null)
        {
            return conversion.Value;
        }

        string code = conversion.ErrorCode == null ? XPathErrorCodes.FORG0001 : conversion.ErrorCode;
        string message = conversion.ErrorMessage == null ? "The value cannot be converted to the required type." : conversion.ErrorMessage;
        throw CreateXPathStringError(message, code);
    }

    internal List<int> ReadXPathCodepointList(string value)
    {
        List<int> result = new List<int>();
        UnicodeScalarScanner scanner = new UnicodeScalarScanner(value.AsMemory());
        while (!scanner.IsUnicodeScalarEnd())
        {
            result.Add(scanner.ReadUnicodeScalar().Value);
        }

        return result;
    }

    internal List<int> ReadXPathStringBoundaries(string value)
    {
        List<int> result = new List<int>();
        result.Add(0);
        UnicodeScalarScanner scanner = new UnicodeScalarScanner(value.AsMemory());
        while (!scanner.IsUnicodeScalarEnd())
        {
            UnicodeScalar scalar = scanner.ReadUnicodeScalar();
            result.Add(scalar.Utf16Offset + scalar.Utf16CodeUnitCount);
        }

        return result;
    }

    internal int FindXPathCodepoint(List<int> values, int sought)
    {
        int index = 0;
        while (index < values.Count)
        {
            if (values[index] == sought)
            {
                return index;
            }

            index = index + 1;
        }

        return -1;
    }

    internal int CountXPathCodepoints(string value)
    {
        int count = 0;
        UnicodeScalarScanner scanner = new UnicodeScalarScanner(value.AsMemory());
        while (!scanner.IsUnicodeScalarEnd())
        {
            scanner.ReadUnicodeScalar();
            count = count + 1;
        }

        return count;
    }

    internal bool IsXPathWhitespace(char value)
    {
        return value == ' ' || value == '\t' || value == '\n' || value == '\r';
    }

    internal XdmSequence CreateXPathStringResult(string value)
    {
        return CreateXPathAtomicResult(Factory.CreateXdmStringAtomicValue(value, XdmAtomicType.String));
    }

    internal XdmSequence CreateXPathBooleanResult(bool value)
    {
        return CreateXPathAtomicResult(Factory.CreateXdmBooleanAtomicValue(value ? "true" : "false"));
    }

    internal XdmSequence CreateXPathIntegerResult(int value)
    {
        return CreateXPathAtomicResult(Factory.CreateXdmIntegerAtomicValue(value.ToString(CultureInfo.InvariantCulture), XdmAtomicType.Integer));
    }

    internal XdmSequence CreateXPathAtomicResult(XdmAtomicValue value)
    {
        return Sequences.CreateXdmAtomicSequence(value);
    }

    internal XPathDynamicException CreateXPathStringError(string message, string code)
    {
        XmlDiagnostic diagnostic = new XmlDiagnostic();
        diagnostic.Code = code;
        diagnostic.Category = XmlErrorCategory.XPathDynamic;
        diagnostic.Severity = XmlDiagnosticSeverity.Fatal;
        diagnostic.Message = message;
        diagnostic.Range = CopyXPathStringRange(Range);
        return new XPathDynamicException(message, diagnostic, diagnostic.Range.Start);
    }

    internal SourceRange CopyXPathStringRange(SourceRange source)
    {
        SourceRange result = new SourceRange();
        result.Start = CopyXPathStringLocation(source.Start);
        result.End = CopyXPathStringLocation(source.End);
        return result;
    }

    internal SourceLocation? CopyXPathStringLocation(SourceLocation? source)
    {
        if (source == null)
        {
            return null;
        }

        SourceLocation result = new SourceLocation();
        result.SourceUri = source.SourceUri;
        result.CharacterOffset = source.CharacterOffset;
        result.ByteOffset = source.ByteOffset;
        result.Line = source.Line;
        result.Column = source.Column;
        return result;
    }
}
