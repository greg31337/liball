using System;
using System.Collections.Generic;

namespace xml;

internal class XsltGroup
{
    internal List<XdmSequence> Members;
    internal XdmSequence Key;
    internal bool HasKey;

    internal XsltGroup(XdmSequence first, XdmSequence key, bool hasKey)
    {
        Members = new List<XdmSequence>();
        Members.Add(first);
        Key = key;
        HasKey = hasKey;
    }
}

internal class XsltGroupingRuntime
{
    internal XsltTemplateRuntime Templates;
    internal XsltSequenceConstructorRuntime SequenceRuntime;
    internal XdmSequenceOperations Sequences;
    internal XdmTypeConverter Converter;
    internal XdmFactory Factory;

    internal XsltGroupingRuntime(XsltTemplateRuntime templates)
    {
        Templates = templates;
        SequenceRuntime = templates.SequenceRuntime;
        Sequences = new XdmSequenceOperations();
        Converter = new XdmTypeConverter();
        Factory = new XdmFactory();
    }

    internal XdmSequence EvaluateXsltForEachGroup(XsltSequenceItemPlan item, XsltTemplateInvocation invocation)
    {
        XsltInstructionPlan instruction = item.Instruction == null ? throw new InvalidOperationException("A grouping instruction has no plan.") : item.Instruction;
        XsltInstructionAttributePlan? select = SequenceRuntime.FindXsltAttribute(instruction, "select");
        XdmSequence population = select == null || select.Expression == null || invocation.Frame == null ? Sequences.CreateEmptyXdmSequence() : Templates.EvaluateXsltInstructionExpression(select.Expression, invocation.Frame);
        bool composite = ReadXsltGroupingBoolean(instruction, "composite");
        IXPathCollation collation = ResolveXsltGroupingCollation(instruction, invocation);
        XsltGroup[] groups;
        if (SequenceRuntime.FindXsltAttribute(instruction, "group-by") != null)
        {
            groups = CreateXsltGroupByGroups(population, instruction, invocation, composite, collation);
        }
        else if (SequenceRuntime.FindXsltAttribute(instruction, "group-adjacent") != null)
        {
            groups = CreateXsltAdjacentGroups(population, instruction, invocation, composite, collation);
        }
        else
        {
            groups = CreateXsltPatternGroups(population, instruction, invocation);
        }

        groups = SortXsltGroups(groups, item, invocation);
        XdmSequenceBuilder result = new XdmSequenceBuilder();
        int index = 0;
        while (index < groups.Length)
        {
            XsltTemplateInvocation grouped = CreateXsltGroupInvocation(invocation, groups[index], index + 1, groups.Length);
            if (item.Content != null)
            {
                result.AppendXdmSequence(SequenceRuntime.EvaluateXsltSequenceConstructor(item.Content, grouped));
            }

            index = index + 1;
        }

        return result.BuildImmutableXdmSequence();
    }

    internal XsltGroup[] CreateXsltGroupByGroups(XdmSequence population, XsltInstructionPlan instruction, XsltTemplateInvocation invocation, bool composite, IXPathCollation collation)
    {
        List<XsltGroup> groups = new List<XsltGroup>();
        XsltInstructionAttributePlan attribute = SequenceRuntime.FindXsltAttribute(instruction, "group-by") ?? throw new InvalidOperationException("A group-by instruction has no expression.");
        int index = 0;
        while (index < population.Count)
        {
            XdmSequence candidate = Templates.CreateXsltTemplateCandidate(population, index);
            XdmSequence[] keys = EvaluateXsltGroupingKeys(attribute, candidate, invocation, index + 1, population.Count, composite);
            List<XdmSequence> distinct = new List<XdmSequence>();
            int keyIndex = 0;
            while (keyIndex < keys.Length)
            {
                if (!ContainsXsltGroupingKey(distinct, keys[keyIndex], collation, invocation))
                {
                    distinct.Add(keys[keyIndex]);
                    XsltGroup? group = FindXsltGroupingKey(groups, keys[keyIndex], collation, invocation);
                    if (group == null)
                    {
                        groups.Add(new XsltGroup(candidate, keys[keyIndex], true));
                    }
                    else
                    {
                        group.Members.Add(candidate);
                    }
                }

                keyIndex = keyIndex + 1;
            }

            index = index + 1;
        }

        return groups.ToArray();
    }

    internal XsltGroup[] CreateXsltAdjacentGroups(XdmSequence population, XsltInstructionPlan instruction, XsltTemplateInvocation invocation, bool composite, IXPathCollation collation)
    {
        List<XsltGroup> groups = new List<XsltGroup>();
        XsltInstructionAttributePlan attribute = SequenceRuntime.FindXsltAttribute(instruction, "group-adjacent") ?? throw new InvalidOperationException("A group-adjacent instruction has no expression.");
        XsltGroup? current = null;
        int index = 0;
        while (index < population.Count)
        {
            XdmSequence candidate = Templates.CreateXsltTemplateCandidate(population, index);
            XdmSequence[] keys = EvaluateXsltGroupingKeys(attribute, candidate, invocation, index + 1, population.Count, composite);
            if (!composite && keys.Length != 1)
            {
                throw CreateXsltGroupingTypeError("A non-composite group-adjacent key must contain exactly one value.", XsltErrorCodes.XTTE1100, instruction.Instruction);
            }

            XdmSequence key = keys[0];
            if (current == null || !SameXsltGroupingKey(current.Key, key, collation, invocation))
            {
                current = new XsltGroup(candidate, key, true);
                groups.Add(current);
            }
            else
            {
                current.Members.Add(candidate);
            }

            index = index + 1;
        }

        return groups.ToArray();
    }

    internal XsltGroup[] CreateXsltPatternGroups(XdmSequence population, XsltInstructionPlan instruction, XsltTemplateInvocation invocation)
    {
        List<XsltGroup> groups = new List<XsltGroup>();
        XsltInstructionAttributePlan? starting = SequenceRuntime.FindXsltAttribute(instruction, "group-starting-with");
        XsltInstructionAttributePlan? ending = SequenceRuntime.FindXsltAttribute(instruction, "group-ending-with");
        XsltMatchPatternPlan pattern = starting != null && starting.Pattern != null ? starting.Pattern : ending != null && ending.Pattern != null ? ending.Pattern : throw new InvalidOperationException("A pattern grouping instruction has no pattern.");
        XdmSequence empty = Sequences.CreateEmptyXdmSequence();
        XsltGroup? current = null;
        int index = 0;
        while (index < population.Count)
        {
            XdmSequence candidate = Templates.CreateXsltTemplateCandidate(population, index);
            bool matches = MatchesXsltGroupingPattern(pattern, candidate, invocation, index + 1, population.Count);
            if (current == null || starting != null && matches)
            {
                current = new XsltGroup(candidate, empty, false);
                groups.Add(current);
            }
            else
            {
                current.Members.Add(candidate);
            }

            if (ending != null && matches)
            {
                current = null;
            }

            index = index + 1;
        }

        return groups.ToArray();
    }

    internal bool MatchesXsltGroupingPattern(XsltMatchPatternPlan pattern, XdmSequence candidate, XsltTemplateInvocation invocation, int position, int size)
    {
        XPathDynamicContext outer = invocation.Frame == null ? Templates.State.DynamicContext : invocation.Frame.Context;
        XPathDynamicContext focused = Templates.CreateXsltTemplateFocusContext(outer, candidate, position, size);
        XsltVariableFrame frame = new XsltVariableFrame(invocation.Frame, invocation.Frame == null ? [] : invocation.Frame.TunnelParameters, focused);
        XPathDynamicContext dynamic = Templates.Variables.CreateXsltVariableDynamicContext(frame);
        return new XsltPatternMatcher().MatchesXsltPattern(pattern, candidate, dynamic);
    }

    internal XdmSequence[] EvaluateXsltGroupingKeys(XsltInstructionAttributePlan attribute, XdmSequence candidate, XsltTemplateInvocation invocation, int position, int size, bool composite)
    {
        if (attribute.Expression == null)
        {
            return [];
        }

        XPathDynamicContext outer = invocation.Frame == null ? Templates.State.DynamicContext : invocation.Frame.Context;
        XPathDynamicContext context = Templates.CreateXsltTemplateFocusContext(outer, candidate, position, size);
        XsltVariableFrame frame = new XsltVariableFrame(invocation.Frame, invocation.Frame == null ? [] : invocation.Frame.TunnelParameters, context);
        XdmSequence atomized = Converter.AtomizeXdmSequence(Templates.EvaluateXsltInstructionExpression(attribute.Expression, frame));
        XdmSequence converted = ConvertXsltGroupingKey(atomized);
        if (composite)
        {
            return [converted];
        }

        XdmSequence[] result = new XdmSequence[converted.Count];
        int index = 0;
        while (index < converted.Count)
        {
            result[index] = Templates.CreateXsltTemplateCandidate(converted, index);
            index = index + 1;
        }

        return result;
    }

    internal XdmSequence ConvertXsltGroupingKey(XdmSequence key)
    {
        XdmSequenceBuilder result = new XdmSequenceBuilder();
        int index = 0;
        while (index < key.Count)
        {
            XdmSequenceItem item = Sequences.ReadXdmSequenceItem(key, index);
            XdmAtomicValue value = item.AtomicValue == null ? throw new InvalidOperationException("Atomization did not produce an atomic grouping key.") : item.AtomicValue;
            if (value.Type == XdmAtomicType.UntypedAtomic)
            {
                value = Factory.CreateXdmStringAtomicValue(Converter.ReadXdmAtomicStringValue(value), XdmAtomicType.String);
            }

            result.AddXdmAtomicValue(value);
            index = index + 1;
        }

        return result.BuildImmutableXdmSequence();
    }

    internal bool ContainsXsltGroupingKey(List<XdmSequence> keys, XdmSequence sought, IXPathCollation collation, XsltTemplateInvocation invocation)
    {
        int index = 0;
        while (index < keys.Count)
        {
            if (SameXsltGroupingKey(keys[index], sought, collation, invocation))
            {
                return true;
            }

            index = index + 1;
        }

        return false;
    }

    internal XsltGroup? FindXsltGroupingKey(List<XsltGroup> groups, XdmSequence key, IXPathCollation collation, XsltTemplateInvocation invocation)
    {
        int index = 0;
        while (index < groups.Count)
        {
            if (SameXsltGroupingKey(groups[index].Key, key, collation, invocation))
            {
                return groups[index];
            }

            index = index + 1;
        }

        return null;
    }

    internal bool SameXsltGroupingKey(XdmSequence left, XdmSequence right, IXPathCollation collation, XsltTemplateInvocation invocation)
    {
        if (left.Count != right.Count)
        {
            return false;
        }

        XPathStaticContext staticContext = new XPathStaticContext();
        XPathSequenceFunctionImplementation equality = new XPathSequenceFunctionImplementation("deep-equal", new SourceRange(), staticContext);
        XPathDynamicContext context = invocation.Frame == null ? Templates.State.DynamicContext : invocation.Frame.Context;
        int index = 0;
        while (index < left.Count)
        {
            XdmSequenceItem leftItem = Sequences.ReadXdmSequenceItem(left, index);
            XdmSequenceItem rightItem = Sequences.ReadXdmSequenceItem(right, index);
            if (leftItem.AtomicValue == null || rightItem.AtomicValue == null || !equality.EqualXPathAtomicValues(leftItem.AtomicValue, rightItem.AtomicValue, collation, context.ImplicitTimezoneMinutes, true))
            {
                return false;
            }

            index = index + 1;
        }

        return true;
    }

    internal bool ReadXsltGroupingBoolean(XsltInstructionPlan instruction, string name)
    {
        XsltInstructionAttributePlan? attribute = SequenceRuntime.FindXsltAttribute(instruction, name);
        if (attribute == null)
        {
            return false;
        }

        string value = attribute.Source.Trim();
        return value == "yes" || value == "true" || value == "1";
    }

    internal IXPathCollation ResolveXsltGroupingCollation(XsltInstructionPlan instruction, XsltTemplateInvocation invocation)
    {
        XsltInstructionAttributePlan? attribute = SequenceRuntime.FindXsltAttribute(instruction, "collation");
        string value = SequenceRuntime.EvaluateXsltAttributeTemplate(attribute, invocation, instruction.StaticContext.DefaultCollation).Trim();
        string uri = value;
        if (!Uri.IsWellFormedUriString(uri, UriKind.Absolute))
        {
            string? baseUri = instruction.StaticContext.BaseUri;
            if (baseUri == null || !Uri.IsWellFormedUriString(uri, UriKind.Relative))
            {
                throw CreateXsltGroupingError("The grouping collation URI cannot be resolved.", XsltErrorCodes.XTDE1110, instruction.Instruction);
            }

            uri = new Uri(new Uri(baseUri, UriKind.Absolute), uri).AbsoluteUri;
        }

        if (uri == XPathNamespaces.CodepointCollation)
        {
            return new XPathCodepointCollation();
        }

        if (uri == XPathAsciiCaseInsensitiveCollation.Uri)
        {
            return new XPathAsciiCaseInsensitiveCollation();
        }

        throw CreateXsltGroupingError("The grouping collation is not recognized.", XsltErrorCodes.XTDE1110, instruction.Instruction);
    }

    internal XdmSequence BuildXsltGroupSequence(XsltGroup group)
    {
        XdmSequenceBuilder result = new XdmSequenceBuilder();
        int index = 0;
        while (index < group.Members.Count)
        {
            result.AppendXdmSequence(group.Members[index]);
            index = index + 1;
        }

        return result.BuildImmutableXdmSequence();
    }

    internal XsltTemplateInvocation CreateXsltGroupInvocation(XsltTemplateInvocation invocation, XsltGroup group, int position, int size)
    {
        XdmSequence initial = group.Members[0];
        XPathDynamicContext outer = invocation.Frame == null ? Templates.State.DynamicContext : invocation.Frame.Context;
        XPathDynamicContext context = Templates.CreateXsltTemplateFocusContext(outer, initial, position, size);
        context.XsltCurrentGroupAvailable = true;
        context.XsltCurrentGroup = BuildXsltGroupSequence(group);
        context.XsltCurrentGroupingKeyAvailable = group.HasKey;
        context.XsltCurrentGroupingKey = group.Key;
        XsltVariableFrame frame = new XsltVariableFrame(invocation.Frame, invocation.Frame == null ? [] : invocation.Frame.TunnelParameters, context);
        return SequenceRuntime.CreateXsltFocusedInvocation(invocation, initial, frame, position, size);
    }

    internal XsltGroup[] SortXsltGroups(XsltGroup[] groups, XsltSequenceItemPlan owner, XsltTemplateInvocation invocation)
    {
        XsltSortRuntime sorting = new XsltSortRuntime(Templates);
        XsltSequenceItemPlan[] plans = sorting.FindXsltSortPlans(owner);
        if (plans.Length == 0)
        {
            return groups;
        }

        XsltSortComponent[] components = sorting.CreateXsltSortComponents(plans, invocation);
        object[] records = new object[groups.Length];
        int index = 0;
        while (index < groups.Length)
        {
            XsltTemplateInvocation grouped = CreateXsltGroupInvocation(invocation, groups[index], index + 1, groups.Length);
            XsltSortKey[] keys = new XsltSortKey[components.Length];
            int keyIndex = 0;
            while (keyIndex < components.Length)
            {
                keys[keyIndex] = sorting.EvaluateXsltSortKey(components[keyIndex], groups[index].Members[0], grouped, index + 1, groups.Length);
                keyIndex = keyIndex + 1;
            }

            XsltSortRecord record = new XsltSortRecord(groups[index].Members[0], keys);
            record.Payload = groups[index];
            records[index] = record;
            index = index + 1;
        }

        sorting.NormalizeXsltNumericSortKeys(records, components);
        StableSort.SortObjects(records, new XsltSortComparer(sorting, components));
        XsltGroup[] result = new XsltGroup[groups.Length];
        index = 0;
        while (index < records.Length)
        {
            XsltSortRecord? record = records[index] as XsltSortRecord;
            XsltGroup? group = record == null ? null : record.Payload as XsltGroup;
            result[index] = group == null ? throw new InvalidOperationException("A sorted group record has no group.") : group;
            index = index + 1;
        }

        return result;
    }

    internal XsltDynamicException CreateXsltGroupingError(string message, string code, XmlNode node)
    {
        return Templates.CreateXsltTemplateDynamicError(message, code, node);
    }

    internal XsltTypeException CreateXsltGroupingTypeError(string message, string code, XmlNode node)
    {
        return Templates.CreateXsltTemplateTypeError(message, code, node);
    }
}
