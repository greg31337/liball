using System;
using System.Globalization;
using System.Numerics;
using System.Text;

namespace xml;

internal class XPathRegexFunctionImplementation : IXPathFunction
{
    internal string LocalName;
    internal SourceRange Range;
    internal XdmSequenceOperations Sequences;
    internal XdmTypeConverter Converter;
    internal XdmTypeSystem Types;
    internal XdmFactory Factory;

    internal XPathRegexFunctionImplementation(string localName, SourceRange range, XPathStaticContext staticContext)
    {
        LocalName = localName;
        Range = CopyXPathRegexRange(range);
        Sequences = new XdmSequenceOperations();
        Converter = new XdmTypeConverter();
        Types = new XdmTypeSystem();
        Factory = new XdmFactory();
    }

    public XdmSequence InvokeXPathFunction(XPathFunctionBinding binding, XdmSequence[] arguments, XPathDynamicContext context)
    {
        if (LocalName == "matches")
        {
            return EvaluateXPathRegexMatches(arguments);
        }

        if (LocalName == "replace")
        {
            return EvaluateXPathRegexReplace(arguments);
        }

        if (LocalName == "tokenize")
        {
            return EvaluateXPathRegexTokenize(arguments);
        }

        if (LocalName == "analyze-string")
        {
            return EvaluateXPathRegexAnalyzeString(arguments);
        }

        throw new InvalidOperationException("The XPath regular expression function is not implemented.");
    }

    internal XdmSequence EvaluateXPathRegexMatches(XdmSequence[] arguments)
    {
        string input = ReadXPathRegexOptionalString(arguments[0]) ?? "";
        string pattern = ReadXPathRegexRequiredString(arguments[1]);
        string flags = arguments.Length == 3 ? ReadXPathRegexRequiredString(arguments[2]) : "";
        XmlSchemaRegexProgram program = CompileXPathRegex(pattern, flags);
        bool result = new XmlSchemaRegexMatcher().IsXmlSchemaRegexMatch(program, input);
        return Sequences.CreateXdmAtomicSequence(Factory.CreateXdmBooleanAtomicValue(result ? "true" : "false"));
    }

    internal XdmSequence EvaluateXPathRegexReplace(XdmSequence[] arguments)
    {
        string input = ReadXPathRegexOptionalString(arguments[0]) ?? "";
        string pattern = ReadXPathRegexRequiredString(arguments[1]);
        string replacement = ReadXPathRegexRequiredString(arguments[2]);
        string flags = arguments.Length == 4 ? ReadXPathRegexRequiredString(arguments[3]) : "";
        XmlSchemaRegexProgram program = CompileXPathRegex(pattern, flags);
        RequireXPathRegexNonEmptyMatch(program);
        bool quoted = ContainsXPathRegexFlag(flags, 'q');
        if (!quoted)
        {
            ValidateXPathRegexReplacement(replacement);
        }

        StringBuilder result = new StringBuilder();
        XmlSchemaRegexMatcher matcher = new XmlSchemaRegexMatcher();
        int offset = 0;
        while (offset <= input.Length)
        {
            XmlSchemaRegexMatch match = matcher.FindXmlSchemaRegexMatch(program, input, offset);
            if (!match.Success)
            {
                break;
            }

            if (match.Length == 0)
            {
                throw CreateXPathRegexError("The regular expression used by fn:replace must not match a zero-length string.", XPathErrorCodes.FORX0003);
            }

            result.Append(input, offset, match.Index - offset);
            if (quoted)
            {
                result.Append(replacement);
            }
            else
            {
                AppendXPathRegexReplacement(result, replacement, match, program.CaptureCount);
            }

            offset = match.Index + match.Length;
        }

        result.Append(input, offset, input.Length - offset);
        return CreateXPathRegexStringResult(result.ToString());
    }

    internal XdmSequence EvaluateXPathRegexTokenize(XdmSequence[] arguments)
    {
        string input = ReadXPathRegexOptionalString(arguments[0]) ?? "";
        string pattern;
        string flags;
        if (arguments.Length == 1)
        {
            input = new XdmStringDatatype().CollapseXdmStringWhitespace(input);
            pattern = " ";
            flags = "";
        }
        else
        {
            pattern = ReadXPathRegexRequiredString(arguments[1]);
            flags = arguments.Length == 3 ? ReadXPathRegexRequiredString(arguments[2]) : "";
        }

        XmlSchemaRegexProgram program = CompileXPathRegex(pattern, flags);
        RequireXPathRegexNonEmptyMatch(program);
        if (input.Length == 0)
        {
            return Sequences.CreateEmptyXdmSequence();
        }

        XdmSequenceBuilder result = new XdmSequenceBuilder();
        XmlSchemaRegexMatcher matcher = new XmlSchemaRegexMatcher();
        int offset = 0;
        while (offset <= input.Length)
        {
            XmlSchemaRegexMatch match = matcher.FindXmlSchemaRegexMatch(program, input, offset);
            if (!match.Success)
            {
                break;
            }

            if (match.Length == 0)
            {
                throw CreateXPathRegexError("The regular expression used by fn:tokenize must not match a zero-length string.", XPathErrorCodes.FORX0003);
            }

            result.AddXdmAtomicValue(Factory.CreateXdmStringAtomicValue(input.Substring(offset, match.Index - offset), XdmAtomicType.String));
            offset = match.Index + match.Length;
        }

        result.AddXdmAtomicValue(Factory.CreateXdmStringAtomicValue(input.Substring(offset), XdmAtomicType.String));
        return result.BuildImmutableXdmSequence();
    }

    internal XdmSequence EvaluateXPathRegexAnalyzeString(XdmSequence[] arguments)
    {
        string input = ReadXPathRegexOptionalString(arguments[0]) ?? "";
        string pattern = ReadXPathRegexRequiredString(arguments[1]);
        string flags = arguments.Length == 3 ? ReadXPathRegexRequiredString(arguments[2]) : "";
        XmlSchemaRegexProgram program = CompileXPathRegex(pattern, flags);
        RequireXPathRegexNonEmptyMatch(program);

        XmlDocument document = new XmlDocument();
        document.Version = XmlVersion.Xml10;
        TreeMutation mutation = new TreeMutation();
        XmlNode root = CreateXPathRegexElement(document, "analyze-string-result", mutation);
        XmlNamespaceBinding binding = new XmlNamespaceBinding();
        binding.Prefix = "";
        binding.NamespaceUri = XPathNamespaces.Functions;
        binding.IsUndeclaration = false;
        root.NamespaceBindings = [binding];

        int[] captureParents = new int[program.CaptureCount + 1];
        CollectXPathRegexCaptureParents(program.Root, 0, captureParents);
        XmlSchemaRegexMatcher matcher = new XmlSchemaRegexMatcher();
        int offset = 0;
        while (offset < input.Length)
        {
            XmlSchemaRegexMatch match = matcher.FindXmlSchemaRegexMatch(program, input, offset);
            if (!match.Success)
            {
                AppendXPathRegexNonMatch(root, input, offset, input.Length, document, mutation);
                offset = input.Length;
                continue;
            }

            if (match.Index > offset)
            {
                AppendXPathRegexNonMatch(root, input, offset, match.Index, document, mutation);
            }

            XmlNode matchElement = CreateXPathRegexElement(document, "match", mutation);
            mutation.AppendXmlTreeChildOperation(root, matchElement);
            bool[] emitted = new bool[program.CaptureCount + 1];
            AppendXPathRegexGroupedContent(matchElement, input, match.Index, match.Index + match.Length, 0, match, captureParents, emitted, document, mutation);
            offset = match.Index + match.Length;
        }

        return Sequences.CreateXdmNodeSequence(root);
    }

    internal XmlSchemaRegexProgram CompileXPathRegex(string pattern, string flags)
    {
        ValidateXPathRegexFlags(flags);
        XmlSchemaRegexCompilationResult compilation = new XmlSchemaRegexCompiler().CompileXmlSchemaRegex(pattern, flags, true);
        if (!compilation.Success || compilation.Program == null)
        {
            string message = compilation.ErrorMessage == null ? "The regular expression is invalid." : compilation.ErrorMessage;
            throw CreateXPathRegexError(message, XPathErrorCodes.FORX0002);
        }

        return compilation.Program;
    }

    internal void ValidateXPathRegexFlags(string flags)
    {
        int index = 0;
        while (index < flags.Length)
        {
            char flag = flags[index];
            if (flag != 's' && flag != 'm' && flag != 'i' && flag != 'x' && flag != 'q')
            {
                throw CreateXPathRegexError("The regular expression flags contain an invalid flag.", XPathErrorCodes.FORX0001);
            }

            index = index + 1;
        }
    }

    internal bool ContainsXPathRegexFlag(string flags, char sought)
    {
        return flags.IndexOf(sought) >= 0;
    }

    internal void RequireXPathRegexNonEmptyMatch(XmlSchemaRegexProgram program)
    {
        if (new XmlSchemaRegexMatcher().IsXmlSchemaRegexMatch(program, ""))
        {
            throw CreateXPathRegexError("The regular expression must not match a zero-length string.", XPathErrorCodes.FORX0003);
        }
    }

    internal void ValidateXPathRegexReplacement(string replacement)
    {
        int index = 0;
        while (index < replacement.Length)
        {
            char current = replacement[index];
            if (current == '$')
            {
                if (index + 1 >= replacement.Length || replacement[index + 1] < '0' || replacement[index + 1] > '9')
                {
                    throw CreateXPathRegexError("A replacement dollar sign must be followed by a decimal digit.", XPathErrorCodes.FORX0004);
                }

                index = index + 2;
                while (index < replacement.Length && replacement[index] >= '0' && replacement[index] <= '9')
                {
                    index = index + 1;
                }

                continue;
            }

            if (current == '\\')
            {
                if (index + 1 >= replacement.Length || replacement[index + 1] != '\\' && replacement[index + 1] != '$')
                {
                    throw CreateXPathRegexError("A replacement backslash must escape a backslash or dollar sign.", XPathErrorCodes.FORX0004);
                }

                index = index + 2;
                continue;
            }

            index = index + 1;
        }
    }

    internal void AppendXPathRegexReplacement(StringBuilder result, string replacement, XmlSchemaRegexMatch match, int captureCount)
    {
        int index = 0;
        while (index < replacement.Length)
        {
            char current = replacement[index];
            if (current == '\\')
            {
                result.Append(replacement[index + 1]);
                index = index + 2;
                continue;
            }

            if (current != '$')
            {
                result.Append(current);
                index = index + 1;
                continue;
            }

            int digitStart = index + 1;
            int digitEnd = digitStart;
            while (digitEnd < replacement.Length && replacement[digitEnd] >= '0' && replacement[digitEnd] <= '9')
            {
                digitEnd = digitEnd + 1;
            }

            BigInteger number = BigInteger.Parse(replacement.Substring(digitStart, digitEnd - digitStart), CultureInfo.InvariantCulture);
            int suffixStart = digitEnd;
            while (number > new BigInteger(captureCount) && number > new BigInteger(9))
            {
                suffixStart = suffixStart - 1;
                number = number / new BigInteger(10);
            }

            if (number <= new BigInteger(captureCount))
            {
                string? captured = match.Captures[(int)number];
                if (captured != null)
                {
                    result.Append(captured);
                }
            }

            if (suffixStart < digitEnd)
            {
                result.Append(replacement, suffixStart, digitEnd - suffixStart);
            }

            index = digitEnd;
        }
    }

    internal void CollectXPathRegexCaptureParents(XmlSchemaRegexNode node, int parent, int[] parents)
    {
        int childParent = parent;
        if (node.Kind == XmlSchemaRegexNodeKind.Capture)
        {
            parents[node.CaptureNumber] = parent;
            childParent = node.CaptureNumber;
        }

        int index = 0;
        while (index < node.Children.Length)
        {
            CollectXPathRegexCaptureParents(node.Children[index], childParent, parents);
            index = index + 1;
        }
    }

    internal void AppendXPathRegexNonMatch(XmlNode root, string input, int start, int end, XmlDocument document, TreeMutation mutation)
    {
        if (end <= start)
        {
            return;
        }

        XmlNode nonMatch = CreateXPathRegexElement(document, "non-match", mutation);
        mutation.AppendXmlTreeChildOperation(root, nonMatch);
        AppendXPathRegexText(nonMatch, input.Substring(start, end - start), document, mutation);
    }

    internal void AppendXPathRegexGroupedContent(XmlNode container, string input, int start, int end, int parent, XmlSchemaRegexMatch match, int[] parents, bool[] emitted, XmlDocument document, TreeMutation mutation)
    {
        int cursor = start;
        while (true)
        {
            int selected = -1;
            int selectedStart = end + 1;
            int capture = 1;
            while (capture < match.CaptureIndexes.Length)
            {
                int captureStart = match.CaptureIndexes[capture];
                int captureEnd = captureStart + match.CaptureLengths[capture];
                bool contained = captureStart >= cursor && captureEnd >= captureStart && captureEnd <= end;
                if (!emitted[capture] && contained && ReadXPathRegexEffectiveParent(capture, parents, match) == parent && (captureStart < selectedStart || captureStart == selectedStart && capture < selected))
                {
                    selected = capture;
                    selectedStart = captureStart;
                }

                capture = capture + 1;
            }

            if (selected < 0)
            {
                break;
            }

            if (selectedStart > cursor)
            {
                AppendXPathRegexText(container, input.Substring(cursor, selectedStart - cursor), document, mutation);
            }

            int selectedEnd = selectedStart + match.CaptureLengths[selected];
            XmlNode group = CreateXPathRegexElement(document, "group", mutation);
            XmlName attributeName = CreateXPathRegexName("nr", null);
            XmlNode attribute = mutation.CreateXmlTreeNodeOperation(document, XmlNodeKind.Attribute, attributeName, selected.ToString(CultureInfo.InvariantCulture));
            new TreeContentMutation(mutation).SetXmlTreeAttributeOperation(group, attribute);
            mutation.AppendXmlTreeChildOperation(container, group);
            emitted[selected] = true;
            AppendXPathRegexGroupedContent(group, input, selectedStart, selectedEnd, selected, match, parents, emitted, document, mutation);
            cursor = selectedEnd > cursor ? selectedEnd : cursor;
        }

        if (cursor < end)
        {
            AppendXPathRegexText(container, input.Substring(cursor, end - cursor), document, mutation);
        }
    }

    internal int ReadXPathRegexEffectiveParent(int capture, int[] parents, XmlSchemaRegexMatch match)
    {
        int parent = parents[capture];
        while (parent > 0 && match.CaptureIndexes[parent] < 0)
        {
            parent = parents[parent];
        }

        return parent;
    }

    internal XmlNode CreateXPathRegexElement(XmlDocument document, string localName, TreeMutation mutation)
    {
        return mutation.CreateXmlTreeNodeOperation(document, XmlNodeKind.Element, CreateXPathRegexName(localName, XPathNamespaces.Functions), null);
    }

    internal XmlName CreateXPathRegexName(string localName, string? namespaceUri)
    {
        XmlName result = new XmlName();
        result.Prefix = null;
        result.LocalName = localName;
        result.NamespaceUri = namespaceUri;
        result.QualifiedName = localName;
        return result;
    }

    internal void AppendXPathRegexText(XmlNode parent, string value, XmlDocument document, TreeMutation mutation)
    {
        if (value.Length == 0)
        {
            return;
        }

        XmlNode text = mutation.CreateXmlTreeNodeOperation(document, XmlNodeKind.Text, null, value);
        mutation.AppendXmlTreeChildOperation(parent, text);
    }

    internal string? ReadXPathRegexOptionalString(XdmSequence supplied)
    {
        XdmSequence atomized = Converter.AtomizeXdmSequence(supplied);
        if (atomized.Count == 0)
        {
            return null;
        }

        if (atomized.Count != 1)
        {
            throw CreateXPathRegexError("A string argument must atomize to at most one value.", XPathErrorCodes.XPTY0004);
        }

        XdmAtomicValue? value = Sequences.ReadValidatedXdmSequenceItem(atomized, 0).AtomicValue;
        if (value == null)
        {
            throw new InvalidOperationException("Atomization did not produce an atomic value.");
        }

        bool supported = Types.IsXdmStringDerivedAtomicType(value.Type) || value.Type == XdmAtomicType.UntypedAtomic || value.Type == XdmAtomicType.AnyUri;
        if (!supported)
        {
            throw CreateXPathRegexError("The function argument must be a string.", XPathErrorCodes.XPTY0004);
        }

        return Converter.ReadXdmAtomicStringValue(value);
    }

    internal string ReadXPathRegexRequiredString(XdmSequence supplied)
    {
        string? result = ReadXPathRegexOptionalString(supplied);
        if (result == null)
        {
            throw CreateXPathRegexError("A required string argument is empty.", XPathErrorCodes.XPTY0004);
        }

        return result;
    }

    internal XdmSequence CreateXPathRegexStringResult(string value)
    {
        return Sequences.CreateXdmAtomicSequence(Factory.CreateXdmStringAtomicValue(value, XdmAtomicType.String));
    }

    internal XPathDynamicException CreateXPathRegexError(string message, string code)
    {
        XmlDiagnostic diagnostic = new XmlDiagnostic();
        diagnostic.Code = code;
        diagnostic.Category = XmlErrorCategory.XPathDynamic;
        diagnostic.Severity = XmlDiagnosticSeverity.Fatal;
        diagnostic.Message = message;
        diagnostic.Range = CopyXPathRegexRange(Range);
        return new XPathDynamicException(message, diagnostic, diagnostic.Range.Start);
    }

    internal SourceRange CopyXPathRegexRange(SourceRange source)
    {
        SourceRange result = new SourceRange();
        result.Start = CopyXPathRegexLocation(source.Start);
        result.End = CopyXPathRegexLocation(source.End);
        return result;
    }

    internal SourceLocation? CopyXPathRegexLocation(SourceLocation? source)
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
