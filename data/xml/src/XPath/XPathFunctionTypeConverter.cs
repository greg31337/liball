namespace xml;

internal class XPathFunctionTypeConverter
{
    internal XdmTypeConverter Converter;
    internal XdmSequenceOperations Sequences;
    internal XdmTypeSystem XdmTypes;

    internal XPathFunctionTypeConverter()
    {
        Converter = new XdmTypeConverter();
        Sequences = new XdmSequenceOperations();
        XdmTypes = new XdmTypeSystem();
    }

    internal XdmSequence ConvertXPathFunctionTypedValue(XdmSequence value, XPathTypeSyntax? syntax, XPathStaticContext context)
    {
        if (syntax == null)
        {
            return Sequences.FreezeXdmSequenceStorage(value);
        }

        if (syntax.EmptySequence)
        {
            if (value.Count != 0)
            {
                throw CreateXPathFunctionTypeError("The function value does not match empty-sequence().");
            }

            return Sequences.CreateEmptyXdmSequence();
        }

        if (syntax.Name == "function" && !syntax.WildcardTest)
        {
            return CoerceXPathFunctionTypedValue(value, syntax, context);
        }

        if (syntax.Name == "array" || syntax.Name == "map" || syntax.Name == "function" || IsXPathNumericTypeSyntax(syntax))
        {
            if (!MatchesXPathFunctionTypedValue(value, syntax))
            {
                throw CreateXPathFunctionTypeError("The function value does not match the required sequence type.");
            }

            return Sequences.FreezeXdmSequenceStorage(value);
        }

        return Converter.ApplyXdmFunctionConversion(value, CreateXPathFunctionSequenceType(syntax), context);
    }

    internal XdmSequence CoerceXPathFunctionTypedValue(XdmSequence value, XPathTypeSyntax syntax, XPathStaticContext context)
    {
        if (!Sequences.MatchesXdmSequenceCardinality(value, ReadXPathFunctionOccurrence(syntax.Occurrence)))
        {
            throw CreateXPathFunctionTypeError("The function value does not match the required sequence cardinality.");
        }

        XdmFunctionOperations functions = new XdmFunctionOperations();
        XdmSequenceBuilder result = new XdmSequenceBuilder();
        int index = 0;
        while (index < value.Count)
        {
            XdmSequenceItem item = Sequences.ReadValidatedXdmSequenceItem(value, index);
            XdmFunctionItem? function = item.Function;
            if (function == null && item.Map != null)
            {
                function = functions.CreateXdmMapFunctionItem(item.Map);
            }
            else if (function == null && item.Array != null)
            {
                function = functions.CreateXdmArrayFunctionItem(item.Array);
            }

            if (function == null || function.Arity != syntax.ParameterTypes.Length)
            {
                throw CreateXPathFunctionTypeError("The function value does not have the required arity.");
            }

            XPathFunctionBinding binding = new XPathFunctionBinding();
            binding.Arity = function.Arity;
            binding.ParameterTypes = null;
            binding.ParameterOccurrences = null;
            binding.ResultType = XdmAtomicType.AnyAtomicType;
            binding.ResultOccurrence = XdmOccurrence.ZeroOrMore;
            binding.IsDeterministic = function.IsDeterministic;
            XPathCoercedFunctionImplementation implementation = new XPathCoercedFunctionImplementation(function, syntax, context);
            result.AddXdmFunction(functions.CreateXdmFunctionItem(binding, implementation, []));
            index = index + 1;
        }

        return result.BuildImmutableXdmSequence();
    }

    internal bool MatchesXPathFunctionTypedValue(XdmSequence value, XPathTypeSyntax syntax)
    {
        if (syntax.EmptySequence)
        {
            return value.Count == 0;
        }

        if (!Sequences.MatchesXdmSequenceCardinality(value, ReadXPathFunctionOccurrence(syntax.Occurrence)))
        {
            return false;
        }

        int index = 0;
        while (index < value.Count)
        {
            XdmSequenceItem item = Sequences.ReadValidatedXdmSequenceItem(value, index);
            if (!MatchesXPathFunctionTypedItem(item, syntax))
            {
                return false;
            }

            index = index + 1;
        }

        return true;
    }

    internal bool MatchesXPathFunctionTypedItem(XdmSequenceItem item, XPathTypeSyntax syntax)
    {
        if (syntax.Name == "item")
        {
            return true;
        }

        if (IsXPathNumericTypeSyntax(syntax))
        {
            return item.AtomicValue != null && XdmTypes.IsXdmNumericAtomicType(item.AtomicValue.Type);
        }

        if (syntax.AtomicType != null)
        {
            return item.AtomicValue != null && XdmTypes.IsXdmAtomicSubtypeOf(item.AtomicValue.Type, syntax.AtomicType.Type);
        }

        if (syntax.Name == "array")
        {
            if (item.Array == null)
            {
                return false;
            }

            if (syntax.WildcardTest || syntax.MemberType == null)
            {
                return true;
            }

            XdmSequence[] members = item.Array.Members == null ? [] : item.Array.Members;
            int memberIndex = 0;
            while (memberIndex < members.Length)
            {
                if (!MatchesXPathFunctionTypedValue(members[memberIndex], syntax.MemberType))
                {
                    return false;
                }

                memberIndex = memberIndex + 1;
            }

            return true;
        }

        if (syntax.Name == "map")
        {
            if (item.Map == null)
            {
                return false;
            }

            if (syntax.WildcardTest || syntax.KeyType == null || syntax.ValueType == null)
            {
                return true;
            }

            XdmMapEntry[] entries = item.Map.Entries == null ? [] : item.Map.Entries;
            int entryIndex = 0;
            while (entryIndex < entries.Length)
            {
                XdmMapEntry entry = entries[entryIndex];
                if (entry.Key == null || entry.Value == null || !MatchesXPathFunctionTypedValue(Sequences.CreateXdmAtomicSequence(entry.Key), syntax.KeyType) || !MatchesXPathFunctionTypedValue(entry.Value, syntax.ValueType))
                {
                    return false;
                }

                entryIndex = entryIndex + 1;
            }

            return true;
        }

        if (syntax.Name == "function")
        {
            return MatchesXPathFunctionItemSignature(item, syntax);
        }

        XdmSequenceBuilder builder = new XdmSequenceBuilder();
        AppendXPathFunctionTypedItem(builder, item);
        return Converter.MatchesXdmSequenceType(builder.BuildImmutableXdmSequence(), CreateXPathFunctionSequenceType(syntax));
    }

    internal bool MatchesXPathFunctionItemSignature(XdmSequenceItem item, XPathTypeSyntax required)
    {
        if (required.WildcardTest)
        {
            return item.Kind == XdmItemKind.Function || item.Kind == XdmItemKind.Map || item.Kind == XdmItemKind.Array;
        }

        if (item.Kind == XdmItemKind.Array)
        {
            return MatchesXPathArrayFunctionSignature(item.Array, required);
        }

        if (item.Kind == XdmItemKind.Map)
        {
            return MatchesXPathMapFunctionSignature(item.Map, required);
        }

        XdmFunctionItem? function = item.Function;
        if (function == null || function.Arity != required.ParameterTypes.Length)
        {
            return false;
        }

        XPathInlineFunctionImplementation? inline = function.Implementation as XPathInlineFunctionImplementation;
        if (inline != null)
        {
            int inlineIndex = 0;
            while (inlineIndex < required.ParameterTypes.Length)
            {
                if (!IsXPathSequenceTypeSubtype(required.ParameterTypes[inlineIndex], inline.Parameters[inlineIndex].Type))
                {
                    return false;
                }

                inlineIndex = inlineIndex + 1;
            }

            return IsXPathSequenceTypeSubtype(inline.ResultType, required.ResultType);
        }

        XPathCoercedFunctionImplementation? coerced = function.Implementation as XPathCoercedFunctionImplementation;
        if (coerced != null)
        {
            return IsXPathSequenceTypeSubtype(coerced.RequiredType, required);
        }

        if (function.Implementation as XPathRandomNextFunctionImplementation != null)
        {
            return MatchesXPathRandomNextFunctionSignature(required);
        }

        if (function.Implementation as XPathRandomPermuteFunctionImplementation != null)
        {
            return MatchesXPathRandomPermuteFunctionSignature(required);
        }

        XmlName? name = function.Name;
        if (name != null && MatchesXPathNamedBuiltinFunctionSignature(name, required))
        {
            return true;
        }

        if (name != null && name.NamespaceUri == XPathNamespaces.Functions && (name.LocalName == "abs" || name.LocalName == "ceiling" || name.LocalName == "floor" || name.LocalName == "round" || name.LocalName == "round-half-to-even"))
        {
            return required.ParameterTypes.Length == 1 && IsXPathNumericTypeSyntax(required.ParameterTypes[0]) && IsXPathNumericTypeSyntax(required.ResultType);
        }

        XdmAtomicType[]? parameterTypes = function.ParameterTypes;
        XdmOccurrence[]? parameterOccurrences = function.ParameterOccurrences;
        if (parameterTypes == null || parameterOccurrences == null)
        {
            return false;
        }

        int index = 0;
        while (index < parameterTypes.Length)
        {
            if (!IsXPathRequiredSubtypeOfAtomic(required.ParameterTypes[index], parameterTypes[index], parameterOccurrences[index]))
            {
                return false;
            }

            index = index + 1;
        }

        return IsXPathAtomicSubtypeOfRequired(function.ResultType, function.ResultOccurrence, required.ResultType);
    }

    internal bool MatchesXPathRandomNextFunctionSignature(XPathTypeSyntax required)
    {
        if (required.ParameterTypes.Length != 0 || required.ResultType == null || ReadXPathTypeOccurrence(required.ResultType) != XdmOccurrence.ExactlyOne)
        {
            return false;
        }

        XPathTypeSyntax result = required.ResultType;
        if (result.Name != "map")
        {
            return false;
        }

        if (result.WildcardTest)
        {
            return true;
        }

        return result.KeyType != null && result.KeyType.AtomicType != null && XdmTypes.IsXdmAtomicSubtypeOf(XdmAtomicType.String, result.KeyType.AtomicType.Type) && result.ValueType != null && result.ValueType.Name == "item" && ReadXPathTypeOccurrence(result.ValueType) == XdmOccurrence.ExactlyOne;
    }

    internal bool MatchesXPathRandomPermuteFunctionSignature(XPathTypeSyntax required)
    {
        if (required.ParameterTypes.Length != 1 || required.ResultType == null)
        {
            return false;
        }

        XPathTypeSyntax parameter = required.ParameterTypes[0];
        return parameter.Name == "item" && ReadXPathTypeOccurrence(parameter) == XdmOccurrence.ZeroOrMore && required.ResultType.Name == "item" && ReadXPathTypeOccurrence(required.ResultType) == XdmOccurrence.ZeroOrMore;
    }

    internal bool IsXPathSequenceTypeSubtype(XPathTypeSyntax? subtype, XPathTypeSyntax? supertype)
    {
        if (subtype != null && subtype.EmptySequence)
        {
            XdmOccurrence requiredOccurrence = ReadXPathTypeOccurrence(supertype);
            return requiredOccurrence == XdmOccurrence.ZeroOrOne || requiredOccurrence == XdmOccurrence.ZeroOrMore;
        }

        if (!IsXPathOccurrenceSubtype(ReadXPathTypeOccurrence(subtype), ReadXPathTypeOccurrence(supertype)))
        {
            return false;
        }

        if (supertype == null || supertype.Name == "item")
        {
            return true;
        }

        if (subtype == null)
        {
            return false;
        }

        if (IsXPathNumericTypeSyntax(subtype))
        {
            return IsXPathNumericTypeSyntax(supertype) || supertype.AtomicType != null && supertype.AtomicType.Type == XdmAtomicType.AnyAtomicType;
        }

        if (subtype.AtomicType != null && supertype.AtomicType != null)
        {
            return XdmTypes.IsXdmAtomicSubtypeOf(subtype.AtomicType.Type, supertype.AtomicType.Type);
        }

        if (subtype.Name == "array" && supertype.Name == "array")
        {
            return supertype.WildcardTest || !subtype.WildcardTest && subtype.MemberType != null && supertype.MemberType != null && IsXPathSequenceTypeSubtype(subtype.MemberType, supertype.MemberType);
        }

        if (subtype.Name == "map" && supertype.Name == "map")
        {
            return supertype.WildcardTest || !subtype.WildcardTest && subtype.KeyType != null && subtype.ValueType != null && supertype.KeyType != null && supertype.ValueType != null && IsXPathSequenceTypeSubtype(subtype.KeyType, supertype.KeyType) && IsXPathSequenceTypeSubtype(subtype.ValueType, supertype.ValueType);
        }

        if ((subtype.Name == "function" || subtype.Name == "map" || subtype.Name == "array") && supertype.Name == "function" && supertype.WildcardTest)
        {
            return true;
        }

        if (subtype.Name == "function" && supertype.Name == "function")
        {
            if (subtype.WildcardTest || subtype.ParameterTypes.Length != supertype.ParameterTypes.Length)
            {
                return false;
            }

            int parameterIndex = 0;
            while (parameterIndex < subtype.ParameterTypes.Length)
            {
                if (!IsXPathSequenceTypeSubtype(supertype.ParameterTypes[parameterIndex], subtype.ParameterTypes[parameterIndex]))
                {
                    return false;
                }

                parameterIndex = parameterIndex + 1;
            }

            return IsXPathSequenceTypeSubtype(subtype.ResultType, supertype.ResultType);
        }

        if (subtype.Name == "map" && supertype.Name == "function")
        {
            return IsXPathMapTypeSubtypeOfFunction(subtype, supertype);
        }

        if (subtype.Name == "array" && supertype.Name == "function")
        {
            return IsXPathArrayTypeSubtypeOfFunction(subtype, supertype);
        }

        if (supertype.Name == "node")
        {
            return IsXPathNodeTypeName(subtype.Name);
        }

        return subtype.Name == supertype.Name;
    }

    internal bool IsXPathNodeTypeName(string name)
    {
        return name == "node" || name == "document-node" || name == "element" || name == "attribute" || name == "namespace-node" || name == "processing-instruction" || name == "comment" || name == "text" || name == "schema-element" || name == "schema-attribute";
    }

    internal bool IsXPathMapTypeSubtypeOfFunction(XPathTypeSyntax mapType, XPathTypeSyntax functionType)
    {
        if (mapType.WildcardTest || mapType.ValueType == null || functionType.ParameterTypes.Length != 1 || functionType.ResultType == null)
        {
            return false;
        }

        return IsXPathRequiredSubtypeOfAtomic(functionType.ParameterTypes[0], XdmAtomicType.AnyAtomicType, XdmOccurrence.ExactlyOne) && DoesXPathTypeAllowEmpty(functionType.ResultType) && IsXPathSequenceTypeSubtype(mapType.ValueType, functionType.ResultType);
    }

    internal bool IsXPathArrayTypeSubtypeOfFunction(XPathTypeSyntax arrayType, XPathTypeSyntax functionType)
    {
        if (arrayType.WildcardTest || arrayType.MemberType == null || functionType.ParameterTypes.Length != 1 || functionType.ResultType == null)
        {
            return false;
        }

        return IsXPathRequiredSubtypeOfAtomic(functionType.ParameterTypes[0], XdmAtomicType.Integer, XdmOccurrence.ExactlyOne) && IsXPathSequenceTypeSubtype(arrayType.MemberType, functionType.ResultType);
    }

    internal bool MatchesXPathMapFunctionSignature(XdmMapValue? map, XPathTypeSyntax required)
    {
        if (map == null || required.ParameterTypes.Length != 1 || required.ResultType == null || !IsXPathRequiredSubtypeOfAtomic(required.ParameterTypes[0], XdmAtomicType.AnyAtomicType, XdmOccurrence.ExactlyOne) || !DoesXPathTypeAllowEmpty(required.ResultType))
        {
            return false;
        }

        XdmMapEntry[] entries = map.Entries == null ? [] : map.Entries;
        int index = 0;
        while (index < entries.Length)
        {
            XdmSequence? value = entries[index].Value;
            if (value == null || !MatchesXPathFunctionTypedValue(value, required.ResultType))
            {
                return false;
            }

            index = index + 1;
        }

        return true;
    }

    internal bool MatchesXPathArrayFunctionSignature(XdmArrayValue? array, XPathTypeSyntax required)
    {
        if (array == null || required.ParameterTypes.Length != 1 || required.ResultType == null || !IsXPathRequiredSubtypeOfAtomic(required.ParameterTypes[0], XdmAtomicType.Integer, XdmOccurrence.ExactlyOne))
        {
            return false;
        }

        XdmSequence[] members = array.Members == null ? [] : array.Members;
        int index = 0;
        while (index < members.Length)
        {
            if (!MatchesXPathFunctionTypedValue(members[index], required.ResultType))
            {
                return false;
            }

            index = index + 1;
        }

        return true;
    }

    internal bool DoesXPathTypeAllowEmpty(XPathTypeSyntax type)
    {
        XdmOccurrence occurrence = ReadXPathTypeOccurrence(type);
        return type.EmptySequence || occurrence == XdmOccurrence.ZeroOrOne || occurrence == XdmOccurrence.ZeroOrMore;
    }

    internal bool MatchesXPathNamedBuiltinFunctionSignature(XmlName name, XPathTypeSyntax required)
    {
        if (name.NamespaceUri != XPathNamespaces.Functions)
        {
            return false;
        }

        if (name.LocalName == "name" || name.LocalName == "local-name" || name.LocalName == "generate-id")
        {
            if (required.ParameterTypes.Length != 1 || required.ResultType == null)
            {
                return false;
            }

            XPathTypeSyntax parameter = required.ParameterTypes[0];
            XdmOccurrence parameterOccurrence = ReadXPathTypeOccurrence(parameter);
            bool acceptableNode = IsXPathNodeTypeName(parameter.Name) && (parameterOccurrence == XdmOccurrence.ExactlyOne || parameterOccurrence == XdmOccurrence.ZeroOrOne);
            return acceptableNode && IsXPathAtomicSubtypeOfRequired(XdmAtomicType.String, XdmOccurrence.ExactlyOne, required.ResultType);
        }

        if (name.LocalName == "filter")
        {
            if (required.ParameterTypes.Length != 2 || required.ResultType == null)
            {
                return false;
            }

            XPathTypeSyntax predicate = required.ParameterTypes[1];
            if (predicate.Name != "function" || predicate.WildcardTest || predicate.ParameterTypes.Length != 1 || predicate.ResultType == null)
            {
                return false;
            }

            XPathTypeSyntax predicateParameter = predicate.ParameterTypes[0];
            bool acceptsItem = predicateParameter.Name == "item";
            bool returnsBoolean = predicate.ResultType.AtomicType != null && predicate.ResultType.AtomicType.Type == XdmAtomicType.Boolean && IsXPathOccurrenceSubtype(ReadXPathTypeOccurrence(predicate.ResultType), XdmOccurrence.ExactlyOne);
            return acceptsItem && returnsBoolean && required.ResultType.Name == "item" && ReadXPathTypeOccurrence(required.ResultType) == XdmOccurrence.ZeroOrMore;
        }

        return false;
    }

    internal bool IsXPathRequiredSubtypeOfAtomic(XPathTypeSyntax required, XdmAtomicType actualType, XdmOccurrence actualOccurrence)
    {
        if (!IsXPathOccurrenceSubtype(ReadXPathTypeOccurrence(required), actualOccurrence))
        {
            return false;
        }

        if (IsXPathNumericTypeSyntax(required))
        {
            return actualType == XdmAtomicType.AnyAtomicType || XdmTypes.IsXdmNumericAtomicType(actualType);
        }

        return required.AtomicType != null && XdmTypes.IsXdmAtomicSubtypeOf(required.AtomicType.Type, actualType);
    }

    internal bool IsXPathAtomicSubtypeOfRequired(XdmAtomicType actualType, XdmOccurrence actualOccurrence, XPathTypeSyntax? required)
    {
        if (!IsXPathOccurrenceSubtype(actualOccurrence, ReadXPathTypeOccurrence(required)))
        {
            return false;
        }

        if (required == null || required.Name == "item")
        {
            return true;
        }

        if (IsXPathNumericTypeSyntax(required))
        {
            return XdmTypes.IsXdmNumericAtomicType(actualType);
        }

        return required.AtomicType != null && XdmTypes.IsXdmAtomicSubtypeOf(actualType, required.AtomicType.Type);
    }

    internal bool IsXPathDefaultItemSequenceSubtype(XPathTypeSyntax? required)
    {
        return required == null || required.Name == "item" && ReadXPathTypeOccurrence(required) == XdmOccurrence.ZeroOrMore;
    }

    internal bool IsXPathNumericTypeSyntax(XPathTypeSyntax? syntax)
    {
        XmlName? name = syntax == null ? null : syntax.ResolvedName;
        return name != null && name.NamespaceUri == XPathNamespaces.XmlSchema && name.LocalName == "numeric";
    }

    internal XdmOccurrence ReadXPathTypeOccurrence(XPathTypeSyntax? syntax)
    {
        if (syntax == null)
        {
            return XdmOccurrence.ZeroOrMore;
        }

        return syntax.EmptySequence ? XdmOccurrence.ZeroOrOne : ReadXPathFunctionOccurrence(syntax.Occurrence);
    }

    internal bool IsXPathOccurrenceSubtype(XdmOccurrence subtype, XdmOccurrence supertype)
    {
        int subtypeMinimum = subtype == XdmOccurrence.ExactlyOne || subtype == XdmOccurrence.OneOrMore ? 1 : 0;
        int subtypeMaximum = subtype == XdmOccurrence.ZeroOrMore || subtype == XdmOccurrence.OneOrMore ? int.MaxValue : 1;
        int supertypeMinimum = supertype == XdmOccurrence.ExactlyOne || supertype == XdmOccurrence.OneOrMore ? 1 : 0;
        int supertypeMaximum = supertype == XdmOccurrence.ZeroOrMore || supertype == XdmOccurrence.OneOrMore ? int.MaxValue : 1;
        return subtypeMinimum >= supertypeMinimum && subtypeMaximum <= supertypeMaximum;
    }

    internal void AppendXPathFunctionTypedItem(XdmSequenceBuilder builder, XdmSequenceItem item)
    {
        if (item.Node != null)
        {
            builder.AddXdmNode(item.Node);
        }
        else if (item.AtomicValue != null)
        {
            builder.AddXdmAtomicValue(item.AtomicValue);
        }
        else if (item.Function != null)
        {
            builder.AddXdmFunction(item.Function);
        }
        else if (item.Map != null)
        {
            builder.AddXdmMap(item.Map);
        }
        else if (item.Array != null)
        {
            builder.AddXdmArray(item.Array);
        }
    }

    internal XdmSequenceType CreateXPathFunctionSequenceType(XPathTypeSyntax syntax)
    {
        XdmSequenceType result = new XdmSequenceType();
        result.Occurrence = ReadXPathFunctionOccurrence(syntax.Occurrence);
        if (syntax.AtomicType != null)
        {
            result.ItemKind = XdmItemKind.Atomic;
            result.AtomicType = syntax.AtomicType.Type;
            return result;
        }

        if (syntax.Name == "item")
        {
            result.AnyItem = true;
            return result;
        }

        if (syntax.Name == "function")
        {
            result.AnyFunction = true;
            return result;
        }

        if (syntax.Name == "map")
        {
            result.ItemKind = XdmItemKind.Map;
            return result;
        }

        if (syntax.Name == "array")
        {
            result.ItemKind = XdmItemKind.Array;
            return result;
        }

        result.ItemKind = XdmItemKind.Node;
        result.AnyNode = syntax.Name == "node";
        result.NodeKind = ReadXPathFunctionNodeKind(syntax.Name);
        return result;
    }

    internal XdmOccurrence ReadXPathFunctionOccurrence(string? occurrence)
    {
        if (occurrence == "?")
        {
            return XdmOccurrence.ZeroOrOne;
        }

        if (occurrence == "*")
        {
            return XdmOccurrence.ZeroOrMore;
        }

        return occurrence == "+" ? XdmOccurrence.OneOrMore : XdmOccurrence.ExactlyOne;
    }

    internal XdmNodeKind ReadXPathFunctionNodeKind(string name)
    {
        if (name == "document-node")
        {
            return XdmNodeKind.Document;
        }

        if (name == "attribute" || name == "schema-attribute")
        {
            return XdmNodeKind.Attribute;
        }

        if (name == "namespace-node")
        {
            return XdmNodeKind.Namespace;
        }

        if (name == "processing-instruction")
        {
            return XdmNodeKind.ProcessingInstruction;
        }

        if (name == "comment")
        {
            return XdmNodeKind.Comment;
        }

        if (name == "text")
        {
            return XdmNodeKind.Text;
        }

        return XdmNodeKind.Element;
    }

    internal XPathTypeException CreateXPathFunctionTypeError(string message)
    {
        XmlDiagnostic diagnostic = new XmlDiagnostic();
        diagnostic.Code = XPathErrorCodes.XPTY0004;
        diagnostic.Category = XmlErrorCategory.XPathType;
        diagnostic.Severity = XmlDiagnosticSeverity.Fatal;
        diagnostic.Message = message;
        return new XPathTypeException(message, diagnostic, null);
    }
}

internal class XPathCoercedFunctionImplementation : IXPathFunction
{
    internal XdmFunctionItem Function;
    internal XPathTypeSyntax RequiredType;
    internal XPathStaticContext StaticContext;
    internal XdmFunctionOperations Functions;
    internal XPathFunctionTypeConverter Converter;

    internal XPathCoercedFunctionImplementation(XdmFunctionItem function, XPathTypeSyntax requiredType, XPathStaticContext staticContext)
    {
        Function = function;
        RequiredType = requiredType;
        StaticContext = staticContext;
        Functions = new XdmFunctionOperations();
        Converter = new XPathFunctionTypeConverter();
    }

    public XdmSequence InvokeXPathFunction(XPathFunctionBinding binding, XdmSequence[] arguments, XPathDynamicContext context)
    {
        XdmSequence value = Functions.InvokeXdmFunctionItem(Function, arguments, context, StaticContext);
        return Converter.ConvertXPathFunctionTypedValue(value, RequiredType.ResultType, StaticContext);
    }
}
