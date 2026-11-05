using System;
using System.Collections;

namespace xml;

internal class XsltSortComponent
{
    internal XsltSequenceItemPlan Plan;
    internal string Order;
    internal string DataType;
    internal string CaseOrder;
    internal string Language;
    internal IXPathCollation Collation;
    internal bool ExplicitCollation;
    internal string CollationValue;
    internal bool Backwards;
    internal bool Merge;

    internal XsltSortComponent(XsltSequenceItemPlan plan)
    {
        Plan = plan;
        Order = "ascending";
        DataType = "";
        CaseOrder = "";
        Language = "";
        Collation = new XPathCodepointCollation();
        ExplicitCollation = false;
        CollationValue = "";
        Backwards = plan.Instruction != null && plan.Instruction.StaticContext.CompatibilityBehavior == XsltCompatibilityBehavior.Backwards;
        Merge = false;
    }
}

internal class XsltSortKey
{
    internal XdmAtomicValue? Value;
    internal XdmAtomicValue? OriginalValue;
    internal bool Empty;
    internal bool NaN;

    internal XsltSortKey()
    {
        Value = null;
        OriginalValue = null;
        Empty = false;
        NaN = false;
    }
}

internal class XsltSortRecord
{
    internal XdmSequence Item;
    internal XsltSortKey[] Keys;
    internal object? Payload;

    internal XsltSortRecord(XdmSequence item, XsltSortKey[] keys)
    {
        Item = item;
        Keys = keys;
        Payload = null;
    }
}

internal class XsltSortComparer : IComparer
{
    internal XsltSortRuntime Runtime;
    internal XsltSortComponent[] Components;

    internal XsltSortComparer(XsltSortRuntime runtime, XsltSortComponent[] components)
    {
        Runtime = runtime;
        Components = components;
    }

    public int Compare(object? left, object? right)
    {
        XsltSortRecord? leftRecord = left as XsltSortRecord;
        XsltSortRecord? rightRecord = right as XsltSortRecord;
        if (leftRecord == null || rightRecord == null)
        {
            throw new ArgumentException("Sort records are required.");
        }

        int index = 0;
        while (index < Components.Length)
        {
            int comparison = Runtime.CompareXsltSortKeys(leftRecord.Keys[index], rightRecord.Keys[index], Components[index]);
            if (comparison != 0)
            {
                return Components[index].Order == "descending" ? -comparison : comparison;
            }

            index = index + 1;
        }

        return 0;
    }
}

internal class XsltSortRuntime
{
    internal XsltTemplateRuntime Templates;
    internal XsltSequenceConstructorRuntime SequencesRuntime;
    internal XdmSequenceOperations Sequences;
    internal XdmTypeConverter Converter;
    internal XdmTypeSystem Types;

    internal XsltSortRuntime(XsltTemplateRuntime templates)
    {
        Templates = templates;
        SequencesRuntime = templates.SequenceRuntime;
        Sequences = new XdmSequenceOperations();
        Converter = new XdmTypeConverter();
        Types = new XdmTypeSystem();
    }

    internal XdmSequence SortXsltSequence(XdmSequence input, XsltSequenceItemPlan owner, XsltTemplateInvocation invocation)
    {
        Sequences.ValidateXdmSequenceStorage(input);
        XsltSequenceItemPlan[] sortPlans = FindXsltSortPlans(owner);
        if (sortPlans.Length == 0)
        {
            return input;
        }

        XsltSortComponent[] components = CreateXsltSortComponents(sortPlans, invocation);
        object[] records = new object[input.Count];
        int index = 0;
        while (index < input.Count)
        {
            XdmSequence candidate = Templates.CreateXsltTemplateCandidate(input, index);
            XsltSortKey[] keys = new XsltSortKey[components.Length];
            int keyIndex = 0;
            while (keyIndex < components.Length)
            {
                keys[keyIndex] = EvaluateXsltSortKey(components[keyIndex], candidate, invocation, index + 1, input.Count);
                keyIndex = keyIndex + 1;
            }

            records[index] = new XsltSortRecord(candidate, keys);
            index = index + 1;
        }

        NormalizeXsltNumericSortKeys(records, components);
        StableSort.SortObjects(records, new XsltSortComparer(this, components));
        XdmSequenceBuilder result = new XdmSequenceBuilder();
        index = 0;
        while (index < records.Length)
        {
            XsltSortRecord? record = records[index] as XsltSortRecord;
            if (record != null)
            {
                result.AppendXdmSequence(record.Item);
            }

            index = index + 1;
        }

        return result.BuildImmutableXdmSequence();
    }

    internal void NormalizeXsltNumericSortKeys(object[] records, XsltSortComponent[] components)
    {
        int componentIndex = 0;
        while (componentIndex < components.Length)
        {
            bool numeric = records.Length != 0;
            XdmAtomicType target = XdmAtomicType.Decimal;
            int recordIndex = 0;
            while (recordIndex < records.Length)
            {
                XsltSortRecord? record = records[recordIndex] as XsltSortRecord;
                XsltSortKey? key = record == null ? null : record.Keys[componentIndex];
                if (key != null && !key.Empty && key.Value != null)
                {
                    if (!Types.IsXdmNumericAtomicType(key.Value.Type))
                    {
                        numeric = false;
                        break;
                    }

                    if (key.Value.Type == XdmAtomicType.Double)
                    {
                        target = XdmAtomicType.Double;
                    }
                    else if (key.Value.Type == XdmAtomicType.Float && target != XdmAtomicType.Double)
                    {
                        target = XdmAtomicType.Float;
                    }
                }

                recordIndex = recordIndex + 1;
            }

            if (numeric && (target == XdmAtomicType.Float || target == XdmAtomicType.Double))
            {
                recordIndex = 0;
                while (recordIndex < records.Length)
                {
                    XsltSortRecord? record = records[recordIndex] as XsltSortRecord;
                    XsltSortKey? key = record == null ? null : record.Keys[componentIndex];
                    if (key != null && !key.Empty && key.Value != null)
                    {
                        key.Value = new XdmNumericDatatype().PromoteXdmNumeric(key.Value, target);
                        key.NaN = IsXsltSortNaN(key.Value);
                    }

                    recordIndex = recordIndex + 1;
                }
            }

            componentIndex = componentIndex + 1;
        }
    }

    internal XsltSequenceItemPlan[] FindXsltSortPlans(XsltSequenceItemPlan owner)
    {
        XsltSequenceItemPlan[] content = owner.Content == null ? [] : owner.Content.Items;
        int count = 0;
        while (count < content.Length)
        {
            XsltInstructionPlan? instruction = content[count].Instruction;
            if (instruction == null || instruction.Kind != XsltInstructionKind.Sort)
            {
                break;
            }

            count = count + 1;
        }

        XsltSequenceItemPlan[] result = new XsltSequenceItemPlan[count];
        int index = 0;
        while (index < count)
        {
            result[index] = content[index];
            index = index + 1;
        }

        return result;
    }

    internal XsltSortComponent[] CreateXsltSortComponents(XsltSequenceItemPlan[] plans, XsltTemplateInvocation invocation)
    {
        XsltSortComponent[] result = new XsltSortComponent[plans.Length];
        int index = 0;
        while (index < plans.Length)
        {
            XsltSortComponent component = new XsltSortComponent(plans[index]);
            component.Order = ReadXsltSortAttribute(component, "order", invocation, "ascending");
            component.DataType = ReadXsltSortAttribute(component, "data-type", invocation, "");
            component.CaseOrder = ReadXsltSortAttribute(component, "case-order", invocation, "");
            component.Language = ReadXsltSortAttribute(component, "lang", invocation, "");
            ValidateXsltSortComponent(component, invocation);
            result[index] = component;
            index = index + 1;
        }

        return result;
    }

    internal string ReadXsltSortAttribute(XsltSortComponent component, string name, XsltTemplateInvocation invocation, string fallback)
    {
        XsltInstructionAttributePlan? attribute = SequencesRuntime.FindXsltAttribute(component.Plan.Instruction, name);
        return SequencesRuntime.EvaluateXsltAttributeTemplate(attribute, invocation, fallback).Trim();
    }

    internal void ValidateXsltSortComponent(XsltSortComponent component, XsltTemplateInvocation invocation)
    {
        if (component.Order != "ascending" && component.Order != "descending")
        {
            throw CreateXsltSortError("The xsl:sort order must be ascending or descending.", XsltErrorCodes.XTDE0030, component.Plan.Node);
        }

        if (component.DataType != "" && component.DataType != "text" && component.DataType != "number")
        {
            throw CreateXsltSortError("The requested xsl:sort data type is not supported.", XsltErrorCodes.XTDE0030, component.Plan.Node);
        }

        if (component.CaseOrder != "" && component.CaseOrder != "upper-first" && component.CaseOrder != "lower-first")
        {
            throw CreateXsltSortError("The xsl:sort case-order must be upper-first or lower-first.", XsltErrorCodes.XTDE0030, component.Plan.Node);
        }

        if (component.Language.Length != 0 && !new XdmStringDatatype().IsValidXdmStringValue(component.Language, XdmAtomicType.Language))
        {
            throw CreateXsltSortError("The xsl:sort language is not a valid xs:language value.", XsltErrorCodes.XTDE0030, component.Plan.Node);
        }

        string collation = ReadXsltSortAttribute(component, "collation", invocation, "");
        if (collation.Length != 0)
        {
            component.ExplicitCollation = true;
            component.CollationValue = ResolveXsltSortCollationUri(collation, component);
            component.Collation = ResolveXsltSortCollation(collation, component);
        }

        string stable = ReadXsltSortAttribute(component, "stable", invocation, "yes");
        if (stable != "yes" && stable != "no" && stable != "true" && stable != "false" && stable != "1" && stable != "0")
        {
            throw CreateXsltSortError("The xsl:sort stable value is not boolean.", XsltErrorCodes.XTDE0030, component.Plan.Node);
        }
    }

    internal IXPathCollation ResolveXsltSortCollation(string value, XsltSortComponent component)
    {
        string uri = ResolveXsltSortCollationUri(value, component);
        if (uri == XPathNamespaces.CodepointCollation)
        {
            return new XPathCodepointCollation();
        }

        if (uri == XPathAsciiCaseInsensitiveCollation.Uri)
        {
            return new XPathAsciiCaseInsensitiveCollation();
        }

        XsltStaticContext? context = component.Plan.Instruction == null ? null : component.Plan.Instruction.StaticContext;
        XPathCollationBinding[] bindings = context == null ? [] : context.CollationBindings;
        int index = 0;
        while (index < bindings.Length)
        {
            IXPathCollation? implementation = bindings[index].Implementation;
            if (bindings[index].Uri == uri && implementation != null)
            {
                return implementation;
            }

            index = index + 1;
        }

        throw CreateXsltSortError("The xsl:sort collation is not recognized.", XsltErrorCodes.XTDE1035, component.Plan.Node);
    }

    internal string ResolveXsltSortCollationUri(string value, XsltSortComponent component)
    {
        string? baseUri = component.Plan.Instruction == null ? null : component.Plan.Instruction.StaticContext.BaseUri;
        string uri = value;
        if (!Uri.IsWellFormedUriString(uri, UriKind.Absolute))
        {
            if (baseUri == null || !Uri.IsWellFormedUriString(uri, UriKind.Relative))
            {
                throw CreateXsltSortError("The xsl:sort collation URI cannot be resolved.", XsltErrorCodes.XTDE1035, component.Plan.Node);
            }

            uri = new Uri(new Uri(baseUri, UriKind.Absolute), uri).AbsoluteUri;
        }

        return UriTools.CanonicalAbsoluteUri(UriTools.CreateAbsolute(uri));
    }

    internal XsltSortKey EvaluateXsltSortKey(XsltSortComponent component, XdmSequence candidate, XsltTemplateInvocation invocation, int position, int size)
    {
        XPathDynamicContext outer = invocation.Frame == null ? Templates.State.DynamicContext : invocation.Frame.Context;
        XPathDynamicContext context = Templates.CreateXsltTemplateFocusContext(outer, candidate, position, size);
        XsltVariableFrame frame = new XsltVariableFrame(invocation.Frame, invocation.Frame == null ? [] : invocation.Frame.TunnelParameters, context);
        XsltTemplateInvocation focused = new XsltTemplateInvocation(invocation.Kind, invocation.Template, invocation.Rule, invocation.Mode, candidate, position, size, invocation.Depth);
        focused.Frame = frame;
        XdmSequence value;
        XsltInstructionAttributePlan? select = SequencesRuntime.FindXsltAttribute(component.Plan.Instruction, "select");
        if (select != null && select.Expression != null)
        {
            value = Templates.EvaluateXsltInstructionExpression(select.Expression, frame);
        }
        else if (component.Plan.Content != null && component.Plan.Content.Items.Length != 0)
        {
            value = SequencesRuntime.EvaluateXsltSequenceConstructor(component.Plan.Content, focused);
        }
        else if (component.Merge)
        {
            value = Sequences.CreateEmptyXdmSequence();
        }
        else
        {
            value = candidate;
        }

        XdmSequence atomized = Converter.AtomizeXdmSequence(value);
        if (atomized.Count > 1 && !component.Backwards)
        {
            throw CreateXsltSortTypeError("An xsl:sort key must atomize to zero or one value.", component.Plan.Node);
        }

        XsltSortKey key = new XsltSortKey();
        if (atomized.Count == 0)
        {
            key.Empty = true;
            return key;
        }

        XdmSequenceItem item = Sequences.ReadXdmSequenceItem(atomized, 0);
        XdmAtomicValue atomic = item.AtomicValue == null ? throw new InvalidOperationException("Atomization did not produce an atomic sort key.") : item.AtomicValue;
        key.Value = ConvertXsltSortKey(atomic, component);
        key.OriginalValue = key.Value;
        key.NaN = IsXsltSortNaN(key.Value);
        return key;
    }

    internal XdmAtomicValue ConvertXsltSortKey(XdmAtomicValue value, XsltSortComponent component)
    {
        if (component.DataType == "text")
        {
            return new XdmFactory().CreateXdmStringAtomicValue(Converter.ReadXdmAtomicStringValue(value), XdmAtomicType.String);
        }

        if (component.DataType == "number")
        {
            XdmConversionResult converted = Converter.TryCastXdmAtomic(value, XdmAtomicType.Double, Templates.CreateXsltInstructionConversionContext(component.Plan.Instruction == null ? throw new InvalidOperationException("A sort key has no instruction plan.") : component.Plan.Instruction));
            return converted.Success && converted.Value != null ? converted.Value : new XdmFactory().CreateXdmFloatingAtomicValue("NaN", XdmAtomicType.Double);
        }

        if (value.Type == XdmAtomicType.UntypedAtomic)
        {
            return new XdmFactory().CreateXdmStringAtomicValue(Converter.ReadXdmAtomicStringValue(value), XdmAtomicType.String);
        }

        return value;
    }

    internal bool IsXsltSortNaN(XdmAtomicValue value)
    {
        return (value.Type == XdmAtomicType.Float || value.Type == XdmAtomicType.Double) && double.IsNaN(value.FloatingValue);
    }

    internal int CompareXsltSortKeys(XsltSortKey left, XsltSortKey right, XsltSortComponent component)
    {
        if (left.Empty || right.Empty)
        {
            return left.Empty == right.Empty ? 0 : left.Empty ? -1 : 1;
        }

        if (left.NaN || right.NaN)
        {
            return left.NaN == right.NaN ? 0 : left.NaN ? -1 : 1;
        }

        XdmAtomicValue leftValue = left.Value == null ? throw new InvalidOperationException("A sort key has no atomic value.") : left.Value;
        XdmAtomicValue rightValue = right.Value == null ? throw new InvalidOperationException("A sort key has no atomic value.") : right.Value;
        bool leftString = Types.IsXdmStringDerivedAtomicType(leftValue.Type) || leftValue.Type == XdmAtomicType.AnyUri;
        bool rightString = Types.IsXdmStringDerivedAtomicType(rightValue.Type) || rightValue.Type == XdmAtomicType.AnyUri;
        if (leftString && rightString)
        {
            return CompareXsltSortStrings(Converter.ReadXdmAtomicStringValue(leftValue), Converter.ReadXdmAtomicStringValue(rightValue), component);
        }

        if (Types.IsXdmNumericAtomicType(leftValue.Type) && Types.IsXdmNumericAtomicType(rightValue.Type))
        {
            return new XdmNumericDatatype().CompareXdmNumeric(leftValue, rightValue);
        }

        XdmAtomicType leftPrimitive = Types.GetXdmAtomicTypeDefinition(leftValue.Type).PrimitiveType;
        XdmAtomicType rightPrimitive = Types.GetXdmAtomicTypeDefinition(rightValue.Type).PrimitiveType;
        if (leftPrimitive != rightPrimitive)
        {
            if (component.Merge)
            {
                throw Templates.CreateXsltTemplateTypeError("The merge key values do not have comparable types.", XsltErrorCodes.XTTE2230, component.Plan.Node);
            }

            throw CreateXsltSortError("The sort key values do not have comparable types.", XsltErrorCodes.XTDE1030, component.Plan.Node);
        }

        if (leftPrimitive == XdmAtomicType.Boolean)
        {
            return new XdmNumericDatatype().CompareXdmBoolean(leftValue, rightValue);
        }

        if (leftPrimitive == XdmAtomicType.DateTime || leftPrimitive == XdmAtomicType.Date || leftPrimitive == XdmAtomicType.Time)
        {
            return new XdmDateTimeDatatype().CompareXdmDateTime(leftValue, rightValue, Templates.State.DynamicContext.ImplicitTimezoneMinutes);
        }

        if (leftValue.Type == rightValue.Type && (leftValue.Type == XdmAtomicType.YearMonthDuration || leftValue.Type == XdmAtomicType.DayTimeDuration))
        {
            return new XdmDurationDatatype().CompareXdmDuration(leftValue, rightValue);
        }

        if (component.Merge)
        {
            throw Templates.CreateXsltTemplateTypeError("The merge key values do not support ordering.", XsltErrorCodes.XTTE2230, component.Plan.Node);
        }

        throw CreateXsltSortError("The sort key values do not support ordering.", XsltErrorCodes.XTDE1030, component.Plan.Node);
    }

    internal int CompareXsltSortStrings(string left, string right, XsltSortComponent component)
    {
        if (component.ExplicitCollation || component.CaseOrder.Length == 0)
        {
            return component.Collation.CompareXPathCollation(left, right);
        }

        int primary = new XPathAsciiCaseInsensitiveCollation().CompareXPathCollation(left, right);
        if (primary != 0)
        {
            return primary;
        }

        int limit = left.Length < right.Length ? left.Length : right.Length;
        int index = 0;
        while (index < limit)
        {
            char leftCharacter = left[index];
            char rightCharacter = right[index];
            if (leftCharacter != rightCharacter && char.ToUpperInvariant(leftCharacter) == char.ToUpperInvariant(rightCharacter))
            {
                bool leftUpper = char.IsUpper(leftCharacter);
                bool rightUpper = char.IsUpper(rightCharacter);
                if (leftUpper != rightUpper)
                {
                    bool upperFirst = component.CaseOrder == "upper-first";
                    return leftUpper == upperFirst ? -1 : 1;
                }
            }

            index = index + 1;
        }

        return component.Collation.CompareXPathCollation(left, right);
    }

    internal XsltDynamicException CreateXsltSortError(string message, string code, XmlNode node)
    {
        return Templates.CreateXsltTemplateDynamicError(message, code, node);
    }

    internal XsltTypeException CreateXsltSortTypeError(string message, XmlNode node)
    {
        return Templates.CreateXsltTemplateTypeError(message, XsltErrorCodes.XTTE1020, node);
    }
}
