namespace xml;

internal class XPathTypeEvaluator
{
    internal XPathRuntime Runtime;
    internal XdmTypeConverter Converter;
    internal XdmSequenceOperations Sequences;
    internal XdmFactory Factory;
    internal XPathFunctionTypeConverter Types;
    internal XPathAtomicTypeCaster Caster;
    internal XPathStaticContext StaticContext;

    internal XPathTypeEvaluator(XPathRuntime runtime)
    {
        Runtime = runtime;
        Converter = new XdmTypeConverter();
        Sequences = new XdmSequenceOperations();
        Factory = new XdmFactory();
        Types = new XPathFunctionTypeConverter();
        Caster = new XPathAtomicTypeCaster();
        StaticContext = new XPathFunctionContextFactory().CreateXPathFunctionStaticContext(runtime.Plan.Environment);
    }

    internal XdmSequence EvaluateXPathTypeOperation(XPathPlanInstruction instruction)
    {
        XPathPlanInstruction? operand = instruction.Operand;
        XPathTypeSyntax? type = instruction.Type;
        if (operand == null || type == null)
        {
            throw new System.InvalidOperationException("An XPath type operation requires an operand and target type.");
        }

        XdmSequence value = Runtime.EvaluateXPathInstruction(operand);
        if (instruction.Opcode == XPathPlanOpcode.InstanceOf)
        {
            return CreateXPathTypeBoolean(MatchesXPathType(value, type));
        }

        if (instruction.Opcode == XPathPlanOpcode.TreatAs)
        {
            if (!MatchesXPathType(value, type))
            {
                throw Runtime.CreateXPathRuntimeError("The value does not match the type required by treat as.", XPathErrorCodes.XPDY0050, instruction.Range);
            }

            return Sequences.FreezeXdmSequenceStorage(value);
        }

        XdmSequence atomized = Converter.AtomizeXdmSequence(value);
        if (instruction.Opcode == XPathPlanOpcode.CastableAs)
        {
            return CreateXPathTypeBoolean(IsXPathCastable(atomized, type));
        }

        return CastXPathSequence(atomized, type, instruction.Range);
    }

    internal bool MatchesXPathType(XdmSequence value, XPathTypeSyntax type)
    {
        if (type.EmptySequence)
        {
            return value.Count == 0;
        }

        if (!type.SchemaType)
        {
            if (type.ParenthesizedTest && (type.Name == "function" || type.Name == "map" || type.Name == "array") || Types.IsXPathNumericTypeSyntax(type))
            {
                return Types.MatchesXPathFunctionTypedValue(value, type);
            }

            return Converter.InstanceOfXdmSequence(value, Types.CreateXPathFunctionSequenceType(type));
        }

        XdmOccurrence occurrence = Types.ReadXPathFunctionOccurrence(type.Occurrence);
        if (!Sequences.MatchesXdmSequenceCardinality(value, occurrence))
        {
            return false;
        }

        XmlName? name = type.ResolvedName;
        XPathSchemaNodeTest? schema = Runtime.Plan.Environment.SchemaTests;
        if (name == null || schema == null)
        {
            return false;
        }

        int index = 0;
        while (index < value.Count)
        {
            XdmSequenceItem item = Sequences.ReadXdmSequenceItem(value, index);
            if (item.AtomicValue == null || !schema.MatchesXPathSchemaAtomicType(item.AtomicValue, name))
            {
                return false;
            }

            index = index + 1;
        }

        return true;
    }

    internal bool IsXPathCastable(XdmSequence value, XPathTypeSyntax type)
    {
        if (value.Count == 0)
        {
            return type.Occurrence == "?";
        }

        if (value.Count != 1)
        {
            return false;
        }

        XdmSequenceItem item = Sequences.ReadXdmSequenceItem(value, 0);
        if (item.AtomicValue == null)
        {
            return false;
        }

        return TryCastXPathItem(item.AtomicValue, type).Success;
    }

    internal XdmSequence CastXPathSequence(XdmSequence value, XPathTypeSyntax type, SourceRange range)
    {
        if (value.Count == 0)
        {
            if (type.Occurrence == "?")
            {
                return Sequences.CreateEmptyXdmSequence();
            }

            throw Runtime.CreateXPathRuntimeError("A non-optional cast target requires one atomic value.", XPathErrorCodes.XPTY0004, range);
        }

        if (value.Count != 1)
        {
            throw Runtime.CreateXPathRuntimeError("A cast operand must atomize to at most one atomic value.", XPathErrorCodes.XPTY0004, range);
        }

        XdmSequenceItem item = Sequences.ReadXdmSequenceItem(value, 0);
        if (item.AtomicValue == null)
        {
            throw Runtime.CreateXPathRuntimeError("A cast operand must atomize to an atomic value.", XPathErrorCodes.XPTY0004, range);
        }

        XdmConversionResult converted = TryCastXPathItem(item.AtomicValue, type);
        if (!converted.Success || converted.Value == null)
        {
            string code = converted.ErrorCode == null ? XPathErrorCodes.FORG0001 : converted.ErrorCode;
            string message = converted.ErrorMessage == null ? "The requested atomic conversion failed." : converted.ErrorMessage;
            throw Runtime.CreateXPathRuntimeError(message, code, range);
        }

        return Sequences.CreateXdmAtomicSequence(converted.Value);
    }

    internal XdmConversionResult TryCastXPathItem(XdmAtomicValue value, XPathTypeSyntax type)
    {
        if (Types.IsXPathNumericTypeSyntax(type))
        {
            if (new XdmTypeSystem().IsXdmNumericAtomicType(value.Type))
            {
                return new XdmConversionResult(true, false, value, null, null);
            }

            return Caster.TryCastXPathBuiltInAtomic(value, XdmAtomicType.Double, StaticContext);
        }

        if (type.AtomicType != null)
        {
            return Caster.TryCastXPathBuiltInAtomic(value, type.AtomicType.Type, StaticContext);
        }

        XmlName? name = type.ResolvedName;
        XPathSchemaNodeTest? schema = Runtime.Plan.Environment.SchemaTests;
        CompiledXmlSchemaSimpleType? target = name == null || schema == null ? null : schema.FindXPathSchemaSimpleType(name);
        if (target == null)
        {
            return new XdmConversionResult(false, true, null, XPathErrorCodes.XPTY0004, "The schema cast target is unavailable.");
        }

        return Caster.TryCastXPathSchemaAtomic(value, target, StaticContext);
    }

    internal XdmSequence CreateXPathTypeBoolean(bool value)
    {
        XdmAtomicValue atomic = Factory.CreateXdmBooleanAtomicValue(value ? "true" : "false");
        return Sequences.CreateXdmAtomicSequence(atomic);
    }
}
