using System.Numerics;

namespace xml;

internal class XPathFunctionEvaluator
{
    internal XPathRuntime Runtime;
    internal XdmSequenceOperations Sequences;
    internal XdmFunctionOperations Functions;
    internal XdmTypeConverter Converter;
    internal XdmMapOperations Maps;
    internal XdmArrayOperations Arrays;
    internal XPathFunctionContextFactory ContextFactory;
    internal bool HasTailCall;
    internal XdmFunctionItem? TailFunction;
    internal XdmSequence[] TailArguments;
    internal XPathDynamicContext? TailContext;

    internal XPathFunctionEvaluator(XPathRuntime runtime)
    {
        Runtime = runtime;
        Sequences = new XdmSequenceOperations();
        Functions = new XdmFunctionOperations();
        Converter = new XdmTypeConverter();
        Maps = new XdmMapOperations();
        Arrays = new XdmArrayOperations();
        ContextFactory = new XPathFunctionContextFactory();
        HasTailCall = false;
        TailFunction = null;
        TailArguments = [];
        TailContext = null;
    }

    internal XdmSequence EvaluateXPathFunction(XPathPlanInstruction instruction)
    {
        if (instruction.Opcode == XPathPlanOpcode.StaticFunctionCall)
        {
            return EvaluateXPathStaticFunctionCall(instruction);
        }

        if (instruction.Opcode == XPathPlanOpcode.NamedFunctionReference)
        {
            return EvaluateXPathNamedFunctionReference(instruction);
        }

        if (instruction.Opcode == XPathPlanOpcode.InlineFunction)
        {
            return EvaluateXPathInlineFunction(instruction);
        }

        if (instruction.Opcode == XPathPlanOpcode.DynamicFunctionCall)
        {
            return EvaluateXPathDynamicFunctionCall(instruction);
        }

        if (instruction.Opcode == XPathPlanOpcode.MapConstructor)
        {
            return EvaluateXPathMapConstructor(instruction);
        }

        if (instruction.Opcode == XPathPlanOpcode.SquareArrayConstructor)
        {
            return EvaluateXPathSquareArrayConstructor(instruction);
        }

        if (instruction.Opcode == XPathPlanOpcode.Lookup || instruction.Opcode == XPathPlanOpcode.UnaryLookup)
        {
            return EvaluateXPathLookup(instruction);
        }

        return EvaluateXPathCurlyArrayConstructor(instruction);
    }

    internal XdmSequence EvaluateXPathLookup(XPathPlanInstruction instruction)
    {
        bool unary = instruction.Opcode == XPathPlanOpcode.UnaryLookup;
        XdmSequence? target;
        if (unary)
        {
            target = Runtime.Focus.ReadXPathFocus().ContextItem;
            if (target == null)
            {
                throw Runtime.CreateXPathRuntimeError("A unary lookup requires a context item.", XPathErrorCodes.XPDY0002, instruction.Range);
            }
        }
        else
        {
            XPathPlanInstruction? targetInstruction = instruction.Left;
            if (targetInstruction == null)
            {
                throw new System.InvalidOperationException("A postfix lookup requires a target expression.");
            }

            target = Runtime.EvaluateXPathInstruction(targetInstruction);
        }

        XPathPlanInstruction? keyInstruction = instruction.Right;
        if (keyInstruction == null)
        {
            throw new System.InvalidOperationException("An XPath lookup requires a key specifier.");
        }

        bool wildcard = keyInstruction.Opcode == XPathPlanOpcode.Wildcard;
        XdmSequenceBuilder result = new XdmSequenceBuilder();
        XdmSequenceCursor targets = new XdmSequenceCursor(target);
        while (targets.MoveNextXdmSequenceItem())
        {
            Runtime.Limiter.CountXPathWork(instruction.Range);
            XdmSequenceItem item = targets.ReadCurrentXdmSequenceItem();
            if (item.Kind != XdmItemKind.Map && item.Kind != XdmItemKind.Array)
            {
                throw Runtime.CreateXPathRuntimeError("An XPath lookup target must be a map or array.", XPathErrorCodes.XPTY0004, instruction.Range);
            }

            if (wildcard)
            {
                AppendXPathWildcardLookup(result, item, instruction.Range);
            }
            else
            {
                XdmSequence keys = EvaluateXPathLookupKeys(keyInstruction);
                AppendXPathKeyedLookup(result, item, keys, instruction.Range);
            }
        }

        return result.BuildImmutableXdmSequence();
    }

    internal XdmSequence EvaluateXPathLookupKeys(XPathPlanInstruction instruction)
    {
        if (instruction.Opcode == XPathPlanOpcode.Name)
        {
            string key = instruction.LocalName == null ? instruction.Value == null ? "" : instruction.Value : instruction.LocalName;
            XdmAtomicValue atomic = new XdmFactory().CreateXdmStringAtomicValue(key, XdmAtomicType.String);
            return Sequences.CreateXdmAtomicSequence(atomic);
        }

        XdmSequence value = Runtime.EvaluateXPathInstruction(instruction);
        return Converter.AtomizeXdmSequence(value);
    }

    internal void AppendXPathWildcardLookup(XdmSequenceBuilder result, XdmSequenceItem target, SourceRange range)
    {
        if (target.Kind == XdmItemKind.Map)
        {
            XdmMapValue? map = target.Map;
            if (map == null)
            {
                throw new System.InvalidOperationException("A map lookup target has no map storage.");
            }

            Maps.ValidateXdmMapStorage(map);
            XdmMapEntry[] entries = map.Entries == null ? [] : map.Entries;
            int index = 0;
            while (index < entries.Length)
            {
                Runtime.Limiter.CountXPathWork(range);
                XdmSequence? value = entries[index].Value;
                result.AppendXdmSequence(value == null ? throw new System.InvalidOperationException("A map entry has no value.") : value);
                index = index + 1;
            }

            return;
        }

        XdmArrayValue? array = target.Array;
        if (array == null)
        {
            throw new System.InvalidOperationException("An array lookup target has no array storage.");
        }

        Arrays.ValidateXdmArrayStorage(array);
        XdmSequence[] members = array.Members == null ? [] : array.Members;
        int memberIndex = 0;
        while (memberIndex < members.Length)
        {
            Runtime.Limiter.CountXPathWork(range);
            result.AppendXdmSequence(members[memberIndex]);
            memberIndex = memberIndex + 1;
        }
    }

    internal void AppendXPathKeyedLookup(XdmSequenceBuilder result, XdmSequenceItem target, XdmSequence keys, SourceRange range)
    {
        XdmSequenceCursor keyCursor = new XdmSequenceCursor(keys);
        while (keyCursor.MoveNextXdmSequenceItem())
        {
            Runtime.Limiter.CountXPathWork(range);
            XdmSequenceItem keyItem = keyCursor.ReadCurrentXdmSequenceItem();
            XdmAtomicValue? key = keyItem.AtomicValue;
            if (key == null)
            {
                throw Runtime.CreateXPathRuntimeError("An XPath lookup key must be atomic.", XPathErrorCodes.XPTY0004, range);
            }

            if (target.Kind == XdmItemKind.Map)
            {
                XdmMapValue? map = target.Map;
                if (map == null)
                {
                    throw new System.InvalidOperationException("A map lookup target has no map storage.");
                }

                result.AppendXdmSequence(Maps.GetXdmMapValue(map, key));
            }
            else
            {
                XdmArrayValue? array = target.Array;
                if (array == null)
                {
                    throw new System.InvalidOperationException("An array lookup target has no array storage.");
                }

                result.AppendXdmSequence(ReadXPathArrayLookup(array, key, range));
            }
        }
    }

    internal XdmSequence ReadXPathArrayLookup(XdmArrayValue array, XdmAtomicValue key, SourceRange range)
    {
        if (!Runtime.Plan.Environment.Types.IsXdmAtomicSubtypeOf(key.Type, XdmAtomicType.Integer))
        {
            throw Runtime.CreateXPathRuntimeError("An XPath array lookup key must be an integer.", XPathErrorCodes.XPTY0004, range);
        }

        BigInteger position = new XdmNumericDatatype().ReadXdmIntegerValue(key);
        if (position < new BigInteger(1) || position > new BigInteger(array.Count) || position > new BigInteger(int.MaxValue))
        {
            throw Runtime.CreateXPathRuntimeError("The XPath array lookup position is out of bounds.", XPathErrorCodes.FOAY0001, range);
        }

        return Arrays.GetXdmArrayMember(array, (int)position);
    }

    internal XdmSequence EvaluateXPathCurlyArrayConstructor(XPathPlanInstruction instruction)
    {
        XdmSequence contents = instruction.Body == null ? Sequences.CreateEmptyXdmSequence() : Runtime.EvaluateXPathInstruction(instruction.Body);
        return Sequences.CreateXdmArraySequence(Arrays.CreateXdmArrayFromSequence(contents));
    }

    internal XdmSequence EvaluateXPathStaticFunctionCall(XPathPlanInstruction instruction)
    {
        XdmFunctionItem function = CreateXPathNamedFunctionItem(instruction, instruction.Children.Length);
        return EvaluateXPathFunctionCall(function, instruction.Children, instruction.Range, instruction.TailCall, true);
    }

    internal XdmSequence EvaluateXPathNamedFunctionReference(XPathPlanInstruction instruction)
    {
        XdmFunctionItem function = CreateXPathNamedFunctionItem(instruction, instruction.Arity);
        XPathDynamicContext dynamicContext = ContextFactory.CreateXPathFunctionDynamicContext(Runtime);
        if (!IsXPathAccumulatorFunction(function) && !IsXPathStylesheetFunction(function))
        {
            dynamicContext.XsltCurrentGroupAvailable = false;
            dynamicContext.XsltCurrentGroup = null;
            dynamicContext.XsltCurrentGroupingKeyAvailable = false;
            dynamicContext.XsltCurrentGroupingKey = null;
            dynamicContext.XsltCurrentMergeGroupAvailable = false;
            dynamicContext.XsltCurrentMergeGroup = null;
            dynamicContext.XsltCurrentMergeSourceNames = null;
            dynamicContext.XsltCurrentMergeSourceGroups = null;
            dynamicContext.XsltCurrentMergeKeyAvailable = false;
            dynamicContext.XsltCurrentMergeKey = null;
            dynamicContext.XsltKeys = null;
            dynamicContext.XsltRegexGroups = null;
        }

        XPathStaticContext staticContext = ContextFactory.CreateXPathFunctionStaticContext(Runtime.Plan.Environment);
        XPathFunctionBinding binding = Functions.CreateXPathFunctionBinding(function);
        IXPathFunction implementation = new XPathContextBoundFunctionImplementation(function, dynamicContext, staticContext);
        function = Functions.CreateXdmFunctionItem(binding, implementation, []);

        return Sequences.CreateXdmFunctionSequence(function);
    }

    internal bool IsXPathStylesheetFunction(XdmFunctionItem function)
    {
        string? key = function.BindingKey;
        return key != null && (key.StartsWith("xslt-function:", global::System.StringComparison.Ordinal) || key.StartsWith("xslt-original-function:", global::System.StringComparison.Ordinal));
    }

    internal bool IsXPathAccumulatorFunction(XdmFunctionItem function)
    {
        XmlName? name = function.Name;
        return name != null && name.NamespaceUri == XPathNamespaces.Functions && (name.LocalName == "accumulator-before" || name.LocalName == "accumulator-after") && function.Arity == 1;
    }

    internal XdmSequence EvaluateXPathInlineFunction(XPathPlanInstruction instruction)
    {
        XPathPlanInstruction body = CreateXPathInlineBodyInstruction(instruction);
        XPathPlan bodyPlan = new XPathPlan(Runtime.Plan.Source, instruction.Range, body, Runtime.Plan.Environment);
        XPathStaticContext staticContext = ContextFactory.CreateXPathFunctionStaticContext(Runtime.Plan.Environment);
        XPathInlineFunctionImplementation implementation = new XPathInlineFunctionImplementation(bodyPlan, instruction.Parameters, instruction.Type, staticContext);
        XPathFunctionBinding binding = new XPathFunctionBinding();
        binding.Arity = instruction.Parameters.Length;
        binding.ResultType = XdmAtomicType.AnyAtomicType;
        binding.ResultOccurrence = XdmOccurrence.ZeroOrMore;
        XPathVariableBinding[] closure = Runtime.Variables.SnapshotXPathVariables();
        XdmFunctionItem function = Functions.CreateXdmFunctionItem(binding, implementation, closure);
        return Sequences.CreateXdmFunctionSequence(function);
    }

    internal XPathPlanInstruction CreateXPathInlineBodyInstruction(XPathPlanInstruction instruction)
    {
        if (instruction.Body != null)
        {
            return instruction.Body;
        }

        XPathStaticType type = new XPathStaticType(XPathStaticItemKind.Unknown, XPathStaticCardinality.Empty);
        XPathPlanInstruction body = new XPathPlanInstruction(XPathPlanOpcode.Constant, instruction.Range, type);
        body.Constants = [];
        return body;
    }

    internal XdmSequence EvaluateXPathDynamicFunctionCall(XPathPlanInstruction instruction)
    {
        XPathPlanInstruction? targetInstruction = instruction.Operand;
        if (targetInstruction == null)
        {
            throw new System.InvalidOperationException("A dynamic function call requires a target expression.");
        }

        XdmSequence target = Runtime.EvaluateXPathInstruction(targetInstruction);
        XdmFunctionItem function = ReadXPathCallableItem(target, targetInstruction.Range);
        return EvaluateXPathFunctionCall(function, instruction.Children, instruction.Range, instruction.TailCall, false);
    }

    internal XdmSequence EvaluateXPathFunctionCall(XdmFunctionItem function, XPathPlanInstruction[] argumentInstructions, SourceRange range, bool tailCall, bool preserveXsltKeyContext)
    {
        if (function.Arity != argumentInstructions.Length)
        {
            throw Runtime.CreateXPathRuntimeError("The supplied argument count does not match the function arity.", XPathErrorCodes.XPTY0004, range);
        }

        XdmSequence?[] values = new XdmSequence?[argumentInstructions.Length];
        bool[] placeholders = new bool[argumentInstructions.Length];
        int placeholderCount = 0;
        int index = 0;
        while (index < argumentInstructions.Length)
        {
            XPathPlanInstruction argument = argumentInstructions[index];
            if (argument.Opcode == XPathPlanOpcode.ArgumentPlaceholder)
            {
                placeholders[index] = true;
                placeholderCount = placeholderCount + 1;
            }
            else
            {
                values[index] = Runtime.EvaluateXPathInstruction(argument);
            }

            index = index + 1;
        }

        XPathDynamicContext dynamicContext = ContextFactory.CreateXPathFunctionDynamicContext(Runtime);
        if (!preserveXsltKeyContext)
        {
            dynamicContext.XsltKeys = null;
            dynamicContext.XsltRegexGroups = null;
        }

        XPathStaticContext staticContext = ContextFactory.CreateXPathFunctionStaticContext(Runtime.Plan.Environment);
        if (placeholderCount > 0)
        {
            return CreateXPathPartialFunction(function, placeholders, values, placeholderCount, dynamicContext, staticContext, range);
        }

        XdmSequence[] arguments = RequireXPathFunctionArguments(values);
        if (tailCall)
        {
            HasTailCall = true;
            TailFunction = function;
            TailArguments = arguments;
            TailContext = dynamicContext;
            return Sequences.CreateEmptyXdmSequence();
        }

        Runtime.FunctionCalls.PushXPathFunctionCall(function.Name, arguments, range);
        XdmSequence result = Functions.InvokeXdmFunctionItem(function, arguments, dynamicContext, staticContext);
        Runtime.FunctionCalls.PopXPathFunctionCall();
        return result;
    }

    internal XdmSequence[] RequireXPathFunctionArguments(XdmSequence?[] values)
    {
        XdmSequence[] result = new XdmSequence[values.Length];
        int index = 0;
        while (index < values.Length)
        {
            XdmSequence? value = values[index];
            if (value == null)
            {
                throw new System.InvalidOperationException("A complete XPath function call has an absent argument.");
            }

            result[index] = value;
            index = index + 1;
        }

        return result;
    }

    internal XdmSequence CreateXPathPartialFunction(XdmFunctionItem original, bool[] placeholders, XdmSequence?[] values, int placeholderCount, XPathDynamicContext dynamicContext, XPathStaticContext staticContext, SourceRange range)
    {
        dynamicContext.XsltCurrentGroupAvailable = false;
        dynamicContext.XsltCurrentGroup = null;
        dynamicContext.XsltCurrentGroupingKeyAvailable = false;
        dynamicContext.XsltCurrentGroupingKey = null;
        dynamicContext.XsltCurrentMergeGroupAvailable = false;
        dynamicContext.XsltCurrentMergeGroup = null;
        dynamicContext.XsltCurrentMergeSourceNames = null;
        dynamicContext.XsltCurrentMergeSourceGroups = null;
        dynamicContext.XsltCurrentMergeKeyAvailable = false;
        dynamicContext.XsltCurrentMergeKey = null;
        dynamicContext.XsltKeys = null;
        dynamicContext.XsltRegexGroups = null;
        XdmSequence?[] fixedValues = new XdmSequence?[values.Length];
        int index = 0;
        while (index < values.Length)
        {
            if (!placeholders[index])
            {
                XdmSequence? value = values[index];
                if (value == null)
                {
                    throw new System.InvalidOperationException("A fixed partial-function argument is absent.");
                }

                fixedValues[index] = ConvertXPathFixedFunctionArgument(original, index, value, staticContext);
            }

            index = index + 1;
        }

        XPathFunctionBinding binding = CreateXPathPartialFunctionBinding(original, placeholders, placeholderCount);
        XPathPartialFunctionImplementation implementation = new XPathPartialFunctionImplementation(original, placeholders, fixedValues, dynamicContext, staticContext);
        XdmFunctionItem partial = Functions.CreateXdmFunctionItem(binding, implementation, []);
        return Sequences.CreateXdmFunctionSequence(partial);
    }

    internal XdmSequence ConvertXPathFixedFunctionArgument(XdmFunctionItem function, int index, XdmSequence value, XPathStaticContext staticContext)
    {
        XPathInlineFunctionImplementation? inline = function.Implementation as XPathInlineFunctionImplementation;
        if (inline != null)
        {
            return inline.ConvertXPathInlineArgument(index, value);
        }

        if (function.ParameterTypes == null || function.ParameterOccurrences == null)
        {
            return Sequences.FreezeXdmSequenceStorage(value);
        }

        XdmSequenceType expected = new XdmSequenceType();
        expected.ItemKind = XdmItemKind.Atomic;
        expected.AtomicType = function.ParameterTypes[index];
        expected.Occurrence = function.ParameterOccurrences[index];
        return Converter.ApplyXdmFunctionConversion(value, expected, staticContext);
    }

    internal XPathFunctionBinding CreateXPathPartialFunctionBinding(XdmFunctionItem original, bool[] placeholders, int placeholderCount)
    {
        XPathFunctionBinding binding = new XPathFunctionBinding();
        binding.Arity = placeholderCount;
        binding.ResultType = original.ResultType;
        binding.ResultOccurrence = original.ResultOccurrence;
        binding.IsDeterministic = original.IsDeterministic;
        if (original.ParameterTypes != null && original.ParameterOccurrences != null)
        {
            XdmAtomicType[] types = new XdmAtomicType[placeholderCount];
            XdmOccurrence[] occurrences = new XdmOccurrence[placeholderCount];
            int resultIndex = 0;
            int index = 0;
            while (index < placeholders.Length)
            {
                if (placeholders[index])
                {
                    types[resultIndex] = original.ParameterTypes[index];
                    occurrences[resultIndex] = original.ParameterOccurrences[index];
                    resultIndex = resultIndex + 1;
                }

                index = index + 1;
            }

            binding.ParameterTypes = types;
            binding.ParameterOccurrences = occurrences;
        }

        return binding;
    }

    internal XdmFunctionItem CreateXPathNamedFunctionItem(XPathPlanInstruction instruction, int arity)
    {
        XPathFunctionBinding binding = instruction.FunctionBinding == null ? CreateXPathBuiltinFunctionBinding(instruction.ResolvedName, arity) : instruction.FunctionBinding;
        XmlName? name = binding.Name;
        string localName = name == null || name.LocalName == null ? "" : name.LocalName;
        IXPathFunction implementation;
        IXPathFunction? resolved = instruction.FunctionBinding == null || name == null ? null : Runtime.Plan.Environment.FindXPathFunctionImplementation(name, arity);
        if (resolved != null)
        {
            implementation = resolved;
        }
        else if (IsXPathArrayFunction(name, arity))
        {
            XPathStaticContext staticContext = ContextFactory.CreateXPathFunctionStaticContext(Runtime.Plan.Environment);
            implementation = new XPathArrayFunctionImplementation(localName, instruction.Range, staticContext);
        }
        else if (IsXPathMapFunction(name, arity))
        {
            XPathStaticContext staticContext = ContextFactory.CreateXPathFunctionStaticContext(Runtime.Plan.Environment);
            implementation = new XPathMapFunctionImplementation(localName, instruction.Range, staticContext);
        }
        else if (IsXPathMathFunction(name, arity))
        {
            XPathStaticContext staticContext = ContextFactory.CreateXPathFunctionStaticContext(Runtime.Plan.Environment);
            implementation = new XPathMathFunctionImplementation(localName, instruction.Range, staticContext);
        }
        else if (IsXPathIntrinsicFunction(name, arity))
        {
            if (localName == "trace")
            {
                XPathStaticContext staticContext = ContextFactory.CreateXPathFunctionStaticContext(Runtime.Plan.Environment);
                implementation = new XPathTraceFunctionImplementation(instruction.Range, staticContext);
            }
            else if (localName == "error")
            {
                XPathStaticContext staticContext = ContextFactory.CreateXPathFunctionStaticContext(Runtime.Plan.Environment);
                implementation = new XPathErrorFunctionImplementation(instruction.Range, staticContext);
            }
            else if (localName == "parse-xml" || localName == "parse-xml-fragment" || localName == "serialize")
            {
                XPathStaticContext staticContext = ContextFactory.CreateXPathFunctionStaticContext(Runtime.Plan.Environment);
                implementation = new XPathParsingSerializationFunctionImplementation(localName, instruction.Range, staticContext);
            }
            else if (localName == "random-number-generator")
            {
                XPathStaticContext staticContext = ContextFactory.CreateXPathFunctionStaticContext(Runtime.Plan.Environment);
                implementation = new XPathRandomNumberFunctionImplementation(instruction.Range, staticContext);
            }
            else if (localName == "transform" || localName == "load-xquery-module")
            {
                XPathStaticContext staticContext = ContextFactory.CreateXPathFunctionStaticContext(Runtime.Plan.Environment);
                implementation = new XPathDynamicLoadingFunctionImplementation(localName, instruction.Range, staticContext);
            }
            else if (IsXPathNumericIntrinsicFunction(name, arity))
            {
                XPathStaticContext staticContext = ContextFactory.CreateXPathFunctionStaticContext(Runtime.Plan.Environment);
                implementation = new XPathNumericFunctionImplementation(localName, instruction.Range, staticContext);
            }
            else if (IsXPathRegexIntrinsicFunction(name, arity))
            {
                XPathStaticContext staticContext = ContextFactory.CreateXPathFunctionStaticContext(Runtime.Plan.Environment);
                implementation = new XPathRegexFunctionImplementation(localName, instruction.Range, staticContext);
            }
            else if (IsXPathUriQNameIntrinsicFunction(name, arity))
            {
                XPathStaticContext staticContext = ContextFactory.CreateXPathFunctionStaticContext(Runtime.Plan.Environment);
                implementation = new XPathUriQNameFunctionImplementation(localName, instruction.Range, staticContext);
            }
            else if (IsXPathNodeIntrinsicFunction(name, arity))
            {
                XPathStaticContext staticContext = ContextFactory.CreateXPathFunctionStaticContext(Runtime.Plan.Environment);
                implementation = new XPathNodeFunctionImplementation(localName, instruction.Range, staticContext);
            }
            else if (IsXPathSequenceIntrinsicFunction(name, arity))
            {
                XPathStaticContext staticContext = ContextFactory.CreateXPathFunctionStaticContext(Runtime.Plan.Environment);
                implementation = new XPathSequenceFunctionImplementation(localName, instruction.Range, staticContext);
            }
            else if (IsXPathDateTimeIntrinsicFunction(name, arity))
            {
                XPathStaticContext staticContext = ContextFactory.CreateXPathFunctionStaticContext(Runtime.Plan.Environment);
                implementation = new XPathDateTimeFunctionImplementation(localName, instruction.Range, staticContext);
            }
            else if (IsXPathNumberFormattingIntrinsicFunction(name, arity))
            {
                XPathStaticContext staticContext = ContextFactory.CreateXPathFunctionStaticContext(Runtime.Plan.Environment);
                implementation = new XPathNumberFormattingFunctionImplementation(localName, instruction.Range, staticContext);
            }
            else if (IsXPathFunctionItemIntrinsicFunction(name, arity))
            {
                XPathStaticContext staticContext = ContextFactory.CreateXPathFunctionStaticContext(Runtime.Plan.Environment);
                implementation = new XPathFunctionItemFunctionImplementation(localName, instruction.Range, staticContext, this);
            }
            else if (IsXPathJsonIntrinsicFunction(name, arity))
            {
                XPathStaticContext staticContext = ContextFactory.CreateXPathFunctionStaticContext(Runtime.Plan.Environment);
                implementation = new XPathJsonFunctionImplementation(localName, instruction.Range, staticContext, Runtime);
            }
            else if (IsXPathResourceIntrinsicFunction(name, arity))
            {
                XPathStaticContext staticContext = ContextFactory.CreateXPathFunctionStaticContext(Runtime.Plan.Environment);
                implementation = new XPathResourceFunctionImplementation(localName, instruction.Range, staticContext, Runtime);
            }
            else if (IsXPathEnvironmentIntrinsicFunction(name, arity))
            {
                XPathStaticContext staticContext = ContextFactory.CreateXPathFunctionStaticContext(Runtime.Plan.Environment);
                implementation = new XPathEnvironmentFunctionImplementation(localName, instruction.Range, staticContext);
            }
            else if (IsXPathStringIntrinsicFunction(name, arity))
            {
                XPathStaticContext staticContext = ContextFactory.CreateXPathFunctionStaticContext(Runtime.Plan.Environment);
                implementation = new XPathStringFunctionImplementation(localName, instruction.Range, staticContext);
            }
            else
            {
                XPathStaticContext staticContext = ContextFactory.CreateXPathFunctionStaticContext(Runtime.Plan.Environment);
                implementation = new XPathBuiltinFunctionImplementation(localName, instruction.Range, staticContext);
            }
        }
        else if (instruction.FunctionBinding == null && arity == 1 && name != null && IsXPathConstructorFunctionName(name))
        {
            CompiledXmlSchemaSimpleType? schemaType = FindXPathConstructorSchemaType(name);
            bool listType = IsXPathListConstructorFunctionName(name);
            XdmAtomicType targetType;
            if (name.NamespaceUri == XPathNamespaces.XmlSchema && name.LocalName == "numeric")
            {
                targetType = XdmAtomicType.Double;
            }
            else if (listType)
            {
                targetType = ReadXPathListConstructorItemType(name);
            }
            else if (schemaType != null)
            {
                targetType = schemaType.AtomicType;
            }
            else
            {
                XdmAtomicTypeDefinition? definition = Runtime.Plan.Environment.Types.FindXdmAtomicTypeDefinition(name);
                if (definition == null)
                {
                    throw new System.InvalidOperationException("A recognized constructor function has no atomic type definition.");
                }

                targetType = definition.Type;
            }

            binding = CreateXPathConstructorFunctionBinding(name, targetType);
            XPathStaticContext staticContext = ContextFactory.CreateXPathFunctionStaticContext(Runtime.Plan.Environment);
            implementation = new XPathConstructorFunctionImplementation(targetType, schemaType, staticContext, listType);
        }
        else
        {
            string displayName = name == null || name.QualifiedName == null ? localName : name.QualifiedName;
            implementation = new XPathUnavailableFunctionImplementation(displayName);
        }

        return Functions.CreateXdmFunctionItem(binding, implementation, []);
    }

    internal XdmFunctionItem? LookupXPathNamedFunctionItem(XmlName name, int arity, SourceRange range)
    {
        XPathFunctionBinding? registered = Runtime.Plan.Environment.FindXPathFunction(name, arity);
        bool known = registered != null || IsXPathArrayFunction(name, arity) || IsXPathMapFunction(name, arity) || IsXPathMathFunction(name, arity) || IsXPathIntrinsicFunction(name, arity) || arity == 1 && IsXPathConstructorFunctionName(name);
        if (!known)
        {
            return null;
        }

        XPathStaticType type = new XPathStaticType(XPathStaticItemKind.Function, XPathStaticCardinality.ExactlyOne);
        XPathPlanInstruction instruction = new XPathPlanInstruction(XPathPlanOpcode.NamedFunctionReference, range, type);
        instruction.ResolvedName = name;
        instruction.FunctionBinding = registered;
        instruction.LocalName = name.LocalName;
        instruction.NamespaceUri = name.NamespaceUri;
        instruction.Arity = arity;
        return CreateXPathNamedFunctionItem(instruction, arity);
    }

    internal bool IsXPathConstructorFunctionName(XmlName name)
    {
        if (name.NamespaceUri == XPathNamespaces.XmlSchema && name.LocalName == "numeric")
        {
            return true;
        }

        if (IsXPathListConstructorFunctionName(name))
        {
            return true;
        }

        XdmAtomicTypeDefinition? builtIn = Runtime.Plan.Environment.Types.FindXdmAtomicTypeDefinition(name);
        if (builtIn != null)
        {
            return !builtIn.IsAbstract && builtIn.Type != XdmAtomicType.Notation;
        }

        CompiledXmlSchemaSimpleType? schemaType = FindXPathConstructorSchemaType(name);
        return schemaType != null && schemaType.Variety == XmlSchemaSimpleVariety.Atomic && schemaType.HasAtomicType && schemaType.AtomicType != XdmAtomicType.AnyAtomicType && schemaType.AtomicType != XdmAtomicType.Notation;
    }

    internal bool IsXPathListConstructorFunctionName(XmlName name)
    {
        return name.NamespaceUri == XPathNamespaces.XmlSchema && (name.LocalName == "IDREFS" || name.LocalName == "NMTOKENS" || name.LocalName == "ENTITIES");
    }

    internal XdmAtomicType ReadXPathListConstructorItemType(XmlName name)
    {
        if (name.LocalName == "IDREFS")
        {
            return XdmAtomicType.Idref;
        }

        return name.LocalName == "NMTOKENS" ? XdmAtomicType.Nmtoken : XdmAtomicType.Entity;
    }

    internal CompiledXmlSchemaSimpleType? FindXPathConstructorSchemaType(XmlName name)
    {
        XPathSchemaNodeTest? schema = Runtime.Plan.Environment.SchemaTests;
        return schema == null ? null : schema.FindXPathSchemaSimpleType(name);
    }

    internal XPathFunctionBinding CreateXPathConstructorFunctionBinding(XmlName name, XdmAtomicType targetType)
    {
        XPathFunctionBinding binding = new XPathFunctionBinding();
        binding.Name = name;
        binding.Arity = 1;
        binding.ParameterTypes = [XdmAtomicType.AnyAtomicType];
        binding.ParameterOccurrences = [XdmOccurrence.ZeroOrOne];
        binding.ResultType = targetType;
        binding.ResultOccurrence = IsXPathListConstructorFunctionName(name) ? XdmOccurrence.ZeroOrMore : XdmOccurrence.ZeroOrOne;
        binding.IsDeterministic = true;
        return binding;
    }

    internal XPathFunctionBinding CreateXPathBuiltinFunctionBinding(XmlName? name, int arity)
    {
        XPathFunctionBinding binding = new XPathFunctionBinding();
        binding.Name = name;
        binding.Arity = arity;
        binding.ResultType = XdmAtomicType.AnyAtomicType;
        binding.ResultOccurrence = XdmOccurrence.ZeroOrMore;
        binding.IsDeterministic = true;
        string localName = name == null || name.LocalName == null ? "" : name.LocalName;
        if (name != null && name.NamespaceUri == XPathNamespaces.Array)
        {
            binding.ResultType = localName == "size" ? XdmAtomicType.Integer : XdmAtomicType.AnyAtomicType;
            bool sequenceResult = localName == "get" || localName == "head" || localName == "fold-left" || localName == "fold-right" || localName == "flatten";
            binding.ResultOccurrence = sequenceResult ? XdmOccurrence.ZeroOrMore : XdmOccurrence.ExactlyOne;
            return binding;
        }

        if (name != null && name.NamespaceUri == XPathNamespaces.Map)
        {
            binding.ResultType = localName == "size" ? XdmAtomicType.Integer : localName == "contains" ? XdmAtomicType.Boolean : XdmAtomicType.AnyAtomicType;
            binding.ResultOccurrence = localName == "keys" || localName == "get" || localName == "for-each" ? XdmOccurrence.ZeroOrMore : XdmOccurrence.ExactlyOne;
            binding.IsDeterministic = localName != "keys" && localName != "for-each";
            return binding;
        }

        if (name != null && name.NamespaceUri == XPathNamespaces.Math)
        {
            binding.ResultType = XdmAtomicType.Double;
            binding.ResultOccurrence = localName == "pi" || localName == "atan2" ? XdmOccurrence.ExactlyOne : XdmOccurrence.ZeroOrOne;
            return binding;
        }

        if (localName == "unordered")
        {
            binding.IsDeterministic = false;
        }

        if (localName == "true" || localName == "false" || localName == "boolean" || localName == "not" || localName == "empty" || localName == "exists" || localName == "has-children" || localName == "lang" || localName == "deep-equal")
        {
            binding.ResultType = XdmAtomicType.Boolean;
            binding.ResultOccurrence = XdmOccurrence.ExactlyOne;
        }
        else if (localName == "position" || localName == "last" || localName == "count")
        {
            binding.ResultType = XdmAtomicType.Integer;
            binding.ResultOccurrence = XdmOccurrence.ExactlyOne;
        }
        else if (localName == "index-of")
        {
            binding.ResultType = XdmAtomicType.Integer;
            binding.ResultOccurrence = XdmOccurrence.ZeroOrMore;
        }
        else if (localName == "current-dateTime")
        {
            binding.ResultType = XdmAtomicType.DateTimeStamp;
            binding.ResultOccurrence = XdmOccurrence.ExactlyOne;
        }
        else if (localName == "current-date")
        {
            binding.ResultType = XdmAtomicType.Date;
            binding.ResultOccurrence = XdmOccurrence.ExactlyOne;
        }
        else if (localName == "current-time")
        {
            binding.ResultType = XdmAtomicType.Time;
            binding.ResultOccurrence = XdmOccurrence.ExactlyOne;
        }
        else if (localName == "implicit-timezone")
        {
            binding.ResultType = XdmAtomicType.DayTimeDuration;
            binding.ResultOccurrence = XdmOccurrence.ExactlyOne;
        }
        else if (localName == "default-collation")
        {
            binding.ResultType = XdmAtomicType.String;
            binding.ResultOccurrence = XdmOccurrence.ExactlyOne;
        }
        else if (localName == "default-language")
        {
            binding.ResultType = XdmAtomicType.Language;
            binding.ResultOccurrence = XdmOccurrence.ExactlyOne;
        }
        else if (localName == "static-base-uri")
        {
            binding.ResultType = XdmAtomicType.AnyUri;
            binding.ResultOccurrence = XdmOccurrence.ZeroOrOne;
        }
        else if (localName == "nilled")
        {
            binding.ResultType = XdmAtomicType.Boolean;
            binding.ResultOccurrence = XdmOccurrence.ZeroOrOne;
        }
        else if (localName == "node-name")
        {
            binding.ResultType = XdmAtomicType.QName;
            binding.ResultOccurrence = XdmOccurrence.ZeroOrOne;
        }
        else if (localName == "function-name")
        {
            binding.ResultType = XdmAtomicType.QName;
            binding.ResultOccurrence = XdmOccurrence.ZeroOrOne;
        }
        else if (localName == "function-arity")
        {
            binding.ResultType = XdmAtomicType.Integer;
            binding.ResultOccurrence = XdmOccurrence.ExactlyOne;
        }
        else if (localName == "function-lookup")
        {
            binding.ResultOccurrence = XdmOccurrence.ZeroOrOne;
        }
        else if (localName == "parse-json" || localName == "json-doc" || localName == "json-to-xml")
        {
            binding.ResultOccurrence = XdmOccurrence.ZeroOrOne;
            if (localName == "json-to-xml")
            {
                binding.IsDeterministic = false;
            }
        }
        else if (localName == "xml-to-json")
        {
            binding.ResultType = XdmAtomicType.String;
            binding.ResultOccurrence = XdmOccurrence.ZeroOrOne;
        }
        else if (localName == "parse-xml" || localName == "parse-xml-fragment")
        {
            binding.ResultOccurrence = XdmOccurrence.ZeroOrOne;
            binding.IsDeterministic = false;
        }
        else if (localName == "serialize")
        {
            binding.ResultType = XdmAtomicType.String;
            binding.ResultOccurrence = XdmOccurrence.ExactlyOne;
        }
        else if (localName == "parse-ietf-date")
        {
            binding.ResultType = XdmAtomicType.DateTime;
            binding.ResultOccurrence = XdmOccurrence.ZeroOrOne;
        }
        else if (localName == "random-number-generator" || localName == "transform" || localName == "load-xquery-module")
        {
            binding.ResultOccurrence = XdmOccurrence.ExactlyOne;
            if (localName == "transform")
            {
                binding.IsDeterministic = false;
            }
        }
        else if (localName == "error")
        {
            binding.IsDeterministic = false;
        }
        else if (localName == "doc")
        {
            binding.ResultOccurrence = XdmOccurrence.ZeroOrOne;
        }
        else if (localName == "doc-available" || localName == "unparsed-text-available")
        {
            binding.ResultType = XdmAtomicType.Boolean;
            binding.ResultOccurrence = XdmOccurrence.ExactlyOne;
        }
        else if (localName == "unparsed-text")
        {
            binding.ResultType = XdmAtomicType.String;
            binding.ResultOccurrence = XdmOccurrence.ZeroOrOne;
        }
        else if (localName == "unparsed-text-lines")
        {
            binding.ResultType = XdmAtomicType.String;
            binding.ResultOccurrence = XdmOccurrence.ZeroOrMore;
        }
        else if (localName == "uri-collection")
        {
            binding.ResultType = XdmAtomicType.AnyUri;
            binding.ResultOccurrence = XdmOccurrence.ZeroOrMore;
        }
        else if (localName == "environment-variable")
        {
            binding.ResultType = XdmAtomicType.String;
            binding.ResultOccurrence = XdmOccurrence.ZeroOrOne;
        }
        else if (localName == "available-environment-variables")
        {
            binding.ResultType = XdmAtomicType.String;
            binding.ResultOccurrence = XdmOccurrence.ZeroOrMore;
        }
        else if (localName == "name" || localName == "local-name" || localName == "generate-id")
        {
            binding.ResultType = XdmAtomicType.String;
            binding.ResultOccurrence = XdmOccurrence.ExactlyOne;
        }
        else if (localName == "path")
        {
            binding.ResultType = XdmAtomicType.String;
            binding.ResultOccurrence = XdmOccurrence.ZeroOrOne;
        }
        else if (localName == "namespace-uri")
        {
            binding.ResultType = XdmAtomicType.AnyUri;
            binding.ResultOccurrence = XdmOccurrence.ExactlyOne;
        }
        else if (localName == "base-uri" || localName == "document-uri")
        {
            binding.ResultType = XdmAtomicType.AnyUri;
            binding.ResultOccurrence = XdmOccurrence.ZeroOrOne;
        }
        else if (localName == "root")
        {
            binding.ResultOccurrence = arity == 0 ? XdmOccurrence.ExactlyOne : XdmOccurrence.ZeroOrOne;
        }
        else if (localName == "head" || localName == "zero-or-one")
        {
            binding.ResultOccurrence = XdmOccurrence.ZeroOrOne;
        }
        else if (localName == "one-or-more")
        {
            binding.ResultOccurrence = XdmOccurrence.OneOrMore;
        }
        else if (localName == "exactly-one")
        {
            binding.ResultOccurrence = XdmOccurrence.ExactlyOne;
        }
        else if (localName == "number")
        {
            binding.ResultType = XdmAtomicType.Double;
            binding.ResultOccurrence = XdmOccurrence.ExactlyOne;
        }
        else if (localName == "abs" || localName == "ceiling" || localName == "floor" || localName == "round" || localName == "round-half-to-even" || localName == "avg" || localName == "min" || localName == "max")
        {
            binding.ResultOccurrence = XdmOccurrence.ZeroOrOne;
        }
        else if (localName == "sum")
        {
            binding.ResultOccurrence = arity == 1 ? XdmOccurrence.ExactlyOne : XdmOccurrence.ZeroOrOne;
        }
        else if (localName == "compare" || localName == "codepoint-equal")
        {
            binding.ResultType = localName == "compare" ? XdmAtomicType.Integer : XdmAtomicType.Boolean;
            binding.ResultOccurrence = XdmOccurrence.ZeroOrOne;
        }
        else if (localName == "contains" || localName == "starts-with" || localName == "ends-with" || localName == "contains-token" || localName == "matches")
        {
            binding.ResultType = XdmAtomicType.Boolean;
            binding.ResultOccurrence = XdmOccurrence.ExactlyOne;
        }
        else if (localName == "string-length")
        {
            binding.ResultType = XdmAtomicType.Integer;
            binding.ResultOccurrence = XdmOccurrence.ExactlyOne;
        }
        else if (localName == "string-to-codepoints")
        {
            binding.ResultType = XdmAtomicType.Integer;
            binding.ResultOccurrence = XdmOccurrence.ZeroOrMore;
        }
        else if (localName == "collation-key")
        {
            binding.ResultType = XdmAtomicType.Base64Binary;
            binding.ResultOccurrence = XdmOccurrence.ExactlyOne;
        }
        else if (localName == "string" || localName == "codepoints-to-string" || localName == "concat" || localName == "string-join" || localName == "substring" || localName == "normalize-space" || localName == "normalize-unicode" || localName == "upper-case" || localName == "lower-case" || localName == "translate" || localName == "substring-before" || localName == "substring-after" || localName == "replace")
        {
            binding.ResultType = XdmAtomicType.String;
            binding.ResultOccurrence = XdmOccurrence.ExactlyOne;
        }
        else if (localName == "tokenize")
        {
            binding.ResultType = XdmAtomicType.String;
            binding.ResultOccurrence = XdmOccurrence.ZeroOrMore;
        }
        else if (localName == "analyze-string")
        {
            binding.ResultOccurrence = XdmOccurrence.ExactlyOne;
        }
        else if (localName == "resolve-uri" || localName == "namespace-uri-from-QName" || localName == "namespace-uri-for-prefix")
        {
            binding.ResultType = XdmAtomicType.AnyUri;
            binding.ResultOccurrence = XdmOccurrence.ZeroOrOne;
        }
        else if (localName == "encode-for-uri" || localName == "iri-to-uri" || localName == "escape-html-uri")
        {
            binding.ResultType = XdmAtomicType.String;
            binding.ResultOccurrence = XdmOccurrence.ExactlyOne;
        }
        else if (localName == "QName")
        {
            binding.ResultType = XdmAtomicType.QName;
            binding.ResultOccurrence = XdmOccurrence.ExactlyOne;
        }
        else if (localName == "resolve-QName")
        {
            binding.ResultType = XdmAtomicType.QName;
            binding.ResultOccurrence = XdmOccurrence.ZeroOrOne;
        }
        else if (localName == "prefix-from-QName" || localName == "local-name-from-QName")
        {
            binding.ResultType = XdmAtomicType.NcName;
            binding.ResultOccurrence = XdmOccurrence.ZeroOrOne;
        }
        else if (localName == "in-scope-prefixes")
        {
            binding.ResultType = XdmAtomicType.String;
            binding.ResultOccurrence = XdmOccurrence.ZeroOrMore;
        }
        else if (localName == "years-from-duration" || localName == "months-from-duration" || localName == "days-from-duration" || localName == "hours-from-duration" || localName == "minutes-from-duration" || localName == "year-from-dateTime" || localName == "month-from-dateTime" || localName == "day-from-dateTime" || localName == "hours-from-dateTime" || localName == "minutes-from-dateTime" || localName == "year-from-date" || localName == "month-from-date" || localName == "day-from-date" || localName == "hours-from-time" || localName == "minutes-from-time")
        {
            binding.ResultType = XdmAtomicType.Integer;
            binding.ResultOccurrence = XdmOccurrence.ZeroOrOne;
        }
        else if (localName == "seconds-from-duration" || localName == "seconds-from-dateTime" || localName == "seconds-from-time")
        {
            binding.ResultType = XdmAtomicType.Decimal;
            binding.ResultOccurrence = XdmOccurrence.ZeroOrOne;
        }
        else if (localName == "timezone-from-dateTime" || localName == "timezone-from-date" || localName == "timezone-from-time")
        {
            binding.ResultType = XdmAtomicType.DayTimeDuration;
            binding.ResultOccurrence = XdmOccurrence.ZeroOrOne;
        }
        else if (localName == "dateTime" || localName == "adjust-dateTime-to-timezone")
        {
            binding.ResultType = XdmAtomicType.DateTime;
            binding.ResultOccurrence = XdmOccurrence.ZeroOrOne;
        }
        else if (localName == "adjust-date-to-timezone")
        {
            binding.ResultType = XdmAtomicType.Date;
            binding.ResultOccurrence = XdmOccurrence.ZeroOrOne;
        }
        else if (localName == "adjust-time-to-timezone")
        {
            binding.ResultType = XdmAtomicType.Time;
            binding.ResultOccurrence = XdmOccurrence.ZeroOrOne;
        }
        else if (localName == "format-dateTime" || localName == "format-date" || localName == "format-time")
        {
            binding.ResultType = XdmAtomicType.String;
            binding.ResultOccurrence = XdmOccurrence.ZeroOrOne;
        }
        else if (localName == "format-integer" || localName == "format-number")
        {
            binding.ResultType = XdmAtomicType.String;
            binding.ResultOccurrence = XdmOccurrence.ExactlyOne;
        }

        return binding;
    }

    internal bool IsXPathArrayFunction(XmlName? name, int arity)
    {
        if (name == null || name.NamespaceUri != XPathNamespaces.Array || name.LocalName == null)
        {
            return false;
        }

        string local = name.LocalName;
        if (local == "size" || local == "head" || local == "tail" || local == "reverse" || local == "join" || local == "flatten")
        {
            return arity == 1;
        }

        if (local == "get" || local == "append" || local == "remove" || local == "for-each" || local == "filter")
        {
            return arity == 2;
        }

        if (local == "subarray")
        {
            return arity == 2 || arity == 3;
        }

        if (local == "put" || local == "insert-before" || local == "fold-left" || local == "fold-right" || local == "for-each-pair")
        {
            return arity == 3;
        }

        return local == "sort" && (arity == 1 || arity == 2 || arity == 3);
    }

    internal bool IsXPathMapFunction(XmlName? name, int arity)
    {
        if (name == null || name.NamespaceUri != XPathNamespaces.Map || name.LocalName == null)
        {
            return false;
        }

        string local = name.LocalName;
        if (local == "size" || local == "keys")
        {
            return arity == 1;
        }

        if (local == "contains" || local == "get" || local == "find" || local == "entry" || local == "remove" || local == "for-each")
        {
            return arity == 2;
        }

        return local == "put" && arity == 3 || local == "merge" && (arity == 1 || arity == 2);
    }

    internal bool IsXPathMathFunction(XmlName? name, int arity)
    {
        if (name == null || name.NamespaceUri != XPathNamespaces.Math || name.LocalName == null)
        {
            return false;
        }

        string local = name.LocalName;
        if (local == "pi")
        {
            return arity == 0;
        }

        if (local == "pow" || local == "atan2")
        {
            return arity == 2;
        }

        return arity == 1 && (local == "exp" || local == "exp10" || local == "log" || local == "log10" || local == "sqrt" || local == "sin" || local == "cos" || local == "tan" || local == "asin" || local == "acos" || local == "atan");
    }

    internal bool IsXPathIntrinsicFunction(XmlName? name, int arity)
    {
        if (name == null || name.NamespaceUri != XPathNamespaces.Functions || name.LocalName == null)
        {
            return false;
        }

        string local = name.LocalName;
        if (IsXPathNumericIntrinsicFunction(name, arity))
        {
            return true;
        }

        if (IsXPathStringIntrinsicFunction(name, arity))
        {
            return true;
        }

        if (IsXPathRegexIntrinsicFunction(name, arity))
        {
            return true;
        }

        if (IsXPathUriQNameIntrinsicFunction(name, arity))
        {
            return true;
        }

        if (IsXPathNodeIntrinsicFunction(name, arity))
        {
            return true;
        }

        if (IsXPathSequenceIntrinsicFunction(name, arity))
        {
            return true;
        }

        if (IsXPathDateTimeIntrinsicFunction(name, arity))
        {
            return true;
        }

        if (IsXPathNumberFormattingIntrinsicFunction(name, arity))
        {
            return true;
        }

        if (IsXPathFunctionItemIntrinsicFunction(name, arity))
        {
            return true;
        }

        if (IsXPathJsonIntrinsicFunction(name, arity))
        {
            return true;
        }

        if (IsXPathResourceIntrinsicFunction(name, arity))
        {
            return true;
        }

        if (IsXPathEnvironmentIntrinsicFunction(name, arity))
        {
            return true;
        }

        if (local == "parse-xml" || local == "parse-xml-fragment")
        {
            return arity == 1;
        }

        if (local == "serialize" || local == "load-xquery-module")
        {
            return arity == 1 || arity == 2;
        }

        if (local == "parse-ietf-date")
        {
            return arity == 1;
        }

        if (local == "random-number-generator")
        {
            return arity == 0 || arity == 1;
        }

        if (local == "transform")
        {
            return arity == 1;
        }

        if (local == "error")
        {
            return arity >= 0 && arity <= 3;
        }

        if (local == "trace")
        {
            return arity == 1 || arity == 2;
        }

        if (arity == 0)
        {
            return local == "true" || local == "false" || local == "position" || local == "last" || local == "current-dateTime" || local == "current-date" || local == "current-time" || local == "implicit-timezone" || local == "default-collation" || local == "default-language" || local == "static-base-uri";
        }

        if (arity == 1)
        {
            return local == "boolean" || local == "not" || local == "count" || local == "empty" || local == "exists" || local == "head" || local == "tail" || local == "exactly-one" || local == "zero-or-one" || local == "one-or-more";
        }

        return false;
    }

    internal bool IsXPathFunctionItemIntrinsicFunction(XmlName? name, int arity)
    {
        if (name == null || name.NamespaceUri != XPathNamespaces.Functions || name.LocalName == null)
        {
            return false;
        }

        string local = name.LocalName;
        if (local == "function-name" || local == "function-arity")
        {
            return arity == 1;
        }

        return (local == "function-lookup" || local == "apply") && arity == 2;
    }

    internal bool IsXPathResourceIntrinsicFunction(XmlName? name, int arity)
    {
        if (name == null || name.NamespaceUri != XPathNamespaces.Functions || name.LocalName == null)
        {
            return false;
        }

        string local = name.LocalName;
        if (local == "doc" || local == "doc-available")
        {
            return arity == 1;
        }

        if (local == "collection" || local == "uri-collection")
        {
            return arity == 0 || arity == 1;
        }

        return (local == "unparsed-text" || local == "unparsed-text-lines" || local == "unparsed-text-available") && (arity == 1 || arity == 2);
    }

    internal bool IsXPathJsonIntrinsicFunction(XmlName? name, int arity)
    {
        if (name == null || name.NamespaceUri != XPathNamespaces.Functions || name.LocalName == null)
        {
            return false;
        }

        string localName = name.LocalName;
        bool jsonFunction = localName == "parse-json" || localName == "json-doc" || localName == "json-to-xml" || localName == "xml-to-json";
        return jsonFunction && (arity == 1 || arity == 2);
    }

    internal bool IsXPathEnvironmentIntrinsicFunction(XmlName? name, int arity)
    {
        if (name == null || name.NamespaceUri != XPathNamespaces.Functions || name.LocalName == null)
        {
            return false;
        }

        return name.LocalName == "environment-variable" && arity == 1 || name.LocalName == "available-environment-variables" && arity == 0;
    }

    internal bool IsXPathNodeIntrinsicFunction(XmlName? name, int arity)
    {
        if (name == null || name.NamespaceUri != XPathNamespaces.Functions || name.LocalName == null)
        {
            return false;
        }

        string local = name.LocalName;
        if (local == "innermost" || local == "outermost")
        {
            return arity == 1;
        }

        if (local == "lang" || local == "id" || local == "element-with-id" || local == "idref")
        {
            return arity == 1 || arity == 2;
        }

        if (local == "data" || local == "string" || local == "node-name" || local == "name" || local == "local-name" || local == "namespace-uri" || local == "base-uri" || local == "document-uri" || local == "root" || local == "path" || local == "has-children" || local == "generate-id" || local == "nilled")
        {
            return arity == 0 || arity == 1;
        }

        return false;
    }

    internal bool IsXPathSequenceIntrinsicFunction(XmlName? name, int arity)
    {
        if (name == null || name.NamespaceUri != XPathNamespaces.Functions || name.LocalName == null)
        {
            return false;
        }

        string local = name.LocalName;
        if (local == "reverse" || local == "unordered")
        {
            return arity == 1;
        }

        if (local == "distinct-values")
        {
            return arity == 1 || arity == 2;
        }

        if (local == "insert-before")
        {
            return arity == 3;
        }

        if (local == "index-of" || local == "subsequence" || local == "deep-equal")
        {
            return arity == 2 || arity == 3;
        }

        if (local == "remove")
        {
            return arity == 2;
        }

        if (local == "for-each" || local == "filter")
        {
            return arity == 2;
        }

        if (local == "fold-left" || local == "fold-right" || local == "for-each-pair")
        {
            return arity == 3;
        }

        return local == "sort" && (arity == 1 || arity == 2 || arity == 3);
    }

    internal bool IsXPathDateTimeIntrinsicFunction(XmlName? name, int arity)
    {
        if (name == null || name.NamespaceUri != XPathNamespaces.Functions || name.LocalName == null)
        {
            return false;
        }

        string local = name.LocalName;
        if (local == "parse-ietf-date")
        {
            return arity == 1;
        }

        if (local == "years-from-duration" || local == "months-from-duration" || local == "days-from-duration" || local == "hours-from-duration" || local == "minutes-from-duration" || local == "seconds-from-duration" || local == "year-from-dateTime" || local == "month-from-dateTime" || local == "day-from-dateTime" || local == "hours-from-dateTime" || local == "minutes-from-dateTime" || local == "seconds-from-dateTime" || local == "timezone-from-dateTime" || local == "year-from-date" || local == "month-from-date" || local == "day-from-date" || local == "timezone-from-date" || local == "hours-from-time" || local == "minutes-from-time" || local == "seconds-from-time" || local == "timezone-from-time")
        {
            return arity == 1;
        }

        if (local == "dateTime")
        {
            return arity == 2;
        }

        if (local == "adjust-dateTime-to-timezone" || local == "adjust-date-to-timezone" || local == "adjust-time-to-timezone")
        {
            return arity == 1 || arity == 2;
        }

        return (local == "format-dateTime" || local == "format-date" || local == "format-time") && (arity == 2 || arity == 5);
    }

    internal bool IsXPathNumberFormattingIntrinsicFunction(XmlName? name, int arity)
    {
        if (name == null || name.NamespaceUri != XPathNamespaces.Functions || name.LocalName == null)
        {
            return false;
        }

        return (name.LocalName == "format-integer" || name.LocalName == "format-number") && (arity == 2 || arity == 3);
    }

    internal bool IsXPathNumericIntrinsicFunction(XmlName? name, int arity)
    {
        if (name == null || name.NamespaceUri != XPathNamespaces.Functions || name.LocalName == null)
        {
            return false;
        }

        string local = name.LocalName;
        if (local == "number")
        {
            return arity == 0 || arity == 1;
        }

        if (local == "abs" || local == "ceiling" || local == "floor" || local == "avg")
        {
            return arity == 1;
        }

        if (local == "round" || local == "round-half-to-even" || local == "sum" || local == "min" || local == "max")
        {
            return arity == 1 || arity == 2;
        }

        return false;
    }

    internal bool IsXPathRegexIntrinsicFunction(XmlName? name, int arity)
    {
        if (name == null || name.NamespaceUri != XPathNamespaces.Functions || name.LocalName == null)
        {
            return false;
        }

        string local = name.LocalName;
        if (local == "matches" || local == "analyze-string")
        {
            return arity == 2 || arity == 3;
        }

        if (local == "replace")
        {
            return arity == 3 || arity == 4;
        }

        return local == "tokenize" && (arity == 1 || arity == 2 || arity == 3);
    }

    internal bool IsXPathStringIntrinsicFunction(XmlName? name, int arity)
    {
        if (name == null || name.NamespaceUri != XPathNamespaces.Functions || name.LocalName == null)
        {
            return false;
        }

        string local = name.LocalName;
        if (local == "concat")
        {
            return arity >= 2;
        }

        if (local == "string-length" || local == "normalize-space")
        {
            return arity == 0 || arity == 1;
        }

        if (local == "string-join" || local == "normalize-unicode" || local == "collation-key")
        {
            return arity == 1 || arity == 2;
        }

        if (local == "substring" || local == "compare" || local == "contains" || local == "starts-with" || local == "ends-with" || local == "substring-before" || local == "substring-after" || local == "contains-token")
        {
            return arity == 2 || arity == 3;
        }

        if (local == "codepoint-equal")
        {
            return arity == 2;
        }

        if (local == "translate")
        {
            return arity == 3;
        }

        return arity == 1 && (local == "codepoints-to-string" || local == "string-to-codepoints" || local == "upper-case" || local == "lower-case");
    }

    internal bool IsXPathUriQNameIntrinsicFunction(XmlName? name, int arity)
    {
        if (name == null || name.NamespaceUri != XPathNamespaces.Functions || name.LocalName == null)
        {
            return false;
        }

        string local = name.LocalName;
        if (local == "resolve-uri")
        {
            return arity == 1 || arity == 2;
        }

        if (local == "QName" || local == "resolve-QName" || local == "namespace-uri-for-prefix")
        {
            return arity == 2;
        }

        if (arity != 1)
        {
            return false;
        }

        return local == "encode-for-uri" || local == "iri-to-uri" || local == "escape-html-uri" || local == "prefix-from-QName" || local == "local-name-from-QName" || local == "namespace-uri-from-QName" || local == "in-scope-prefixes";
    }

    internal XdmFunctionItem ReadXPathCallableItem(XdmSequence target, SourceRange range)
    {
        Sequences.ValidateXdmSequenceStorage(target);
        if (target.Count != 1)
        {
            throw Runtime.CreateXPathRuntimeError("A dynamic function target must contain exactly one function item.", XPathErrorCodes.XPTY0004, range);
        }

        XdmSequenceItem item = Sequences.ReadXdmSequenceItem(target, 0);
        if (item.Kind == XdmItemKind.Function && item.Function != null)
        {
            return item.Function;
        }

        if (item.Kind == XdmItemKind.Map && item.Map != null)
        {
            return Functions.CreateXdmMapFunctionItem(item.Map);
        }

        if (item.Kind == XdmItemKind.Array && item.Array != null)
        {
            return Functions.CreateXdmArrayFunctionItem(item.Array);
        }

        throw Runtime.CreateXPathRuntimeError("A dynamic function target must be a function, map, or array.", XPathErrorCodes.XPTY0004, range);
    }

    internal XdmSequence EvaluateXPathMapConstructor(XPathPlanInstruction instruction)
    {
        XdmMapEntry[] entries = new XdmMapEntry[instruction.MapEntries.Length];
        int index = 0;
        while (index < instruction.MapEntries.Length)
        {
            XPathPlanMapEntry entry = instruction.MapEntries[index];
            XdmSequence keyValue = Converter.AtomizeXdmSequence(Runtime.EvaluateXPathInstruction(entry.Key));
            if (keyValue.Count != 1)
            {
                throw Runtime.CreateXPathRuntimeError("A map constructor key must atomize to exactly one value.", XPathErrorCodes.XPTY0004, entry.Key.Range);
            }

            XdmSequenceItem keyItem = Sequences.ReadXdmSequenceItem(keyValue, 0);
            if (keyItem.Kind != XdmItemKind.Atomic || keyItem.AtomicValue == null)
            {
                throw Runtime.CreateXPathRuntimeError("A map constructor key must be atomic.", XPathErrorCodes.XPTY0004, entry.Key.Range);
            }

            int earlier = 0;
            while (earlier < index)
            {
                XdmAtomicValue? earlierKey = entries[earlier].Key;
                if (earlierKey != null && Maps.SameXdmMapKey(earlierKey, keyItem.AtomicValue))
                {
                    throw Runtime.CreateXPathRuntimeError("A map constructor cannot contain duplicate keys.", "XQDY0137", entry.Range);
                }

                earlier = earlier + 1;
            }

            XdmSequence value = Runtime.EvaluateXPathInstruction(entry.Value);
            entries[index] = Maps.CreateXdmMapEntry(keyItem.AtomicValue, value);
            index = index + 1;
        }

        return Sequences.CreateXdmMapSequence(Maps.CreateXdmMap(entries));
    }

    internal XdmSequence EvaluateXPathSquareArrayConstructor(XPathPlanInstruction instruction)
    {
        XdmSequence[] members = new XdmSequence[instruction.Children.Length];
        int index = 0;
        while (index < instruction.Children.Length)
        {
            members[index] = Runtime.EvaluateXPathInstruction(instruction.Children[index]);
            index = index + 1;
        }

        return Sequences.CreateXdmArraySequence(Arrays.CreateXdmArray(members));
    }
}
