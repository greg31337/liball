using System;
using System.Collections.Generic;

namespace xml;

internal class XsltMapArrayRuntime
{
    internal XsltSequenceConstructorRuntime Runtime;
    internal XsltTemplateRuntime Templates;
    internal XdmSequenceOperations Sequences;
    internal XdmTypeConverter Converter;
    internal XdmMapOperations Maps;
    internal XdmArrayOperations Arrays;
    internal XdmFactory Factory;

    internal XsltMapArrayRuntime(XsltSequenceConstructorRuntime runtime)
    {
        Runtime = runtime;
        Templates = runtime.Templates;
        Sequences = new XdmSequenceOperations();
        Converter = new XdmTypeConverter();
        Maps = new XdmMapOperations();
        Arrays = new XdmArrayOperations();
        Factory = new XdmFactory();
    }

    internal XdmSequence EvaluateXsltMapArrayInstruction(XsltSequenceItemPlan item, XsltTemplateInvocation invocation)
    {
        XsltInstructionKind kind = item.Instruction == null ? XsltInstructionKind.Unknown : item.Instruction.Kind;
        if (kind == XsltInstructionKind.Map)
        {
            return EvaluateXsltMap(item, invocation);
        }

        if (kind == XsltInstructionKind.MapEntry)
        {
            return EvaluateXsltMapEntry(item, invocation);
        }

        if (kind == XsltInstructionKind.Array)
        {
            return EvaluateXsltArray(item, invocation);
        }

        if (kind == XsltInstructionKind.ArrayMember)
        {
            return Runtime.EvaluateXsltSequenceInstruction(item, invocation);
        }

        return Sequences.CreateEmptyXdmSequence();
    }

    internal XdmSequence EvaluateXsltMap(XsltSequenceItemPlan item, XsltTemplateInvocation invocation)
    {
        XdmSequence maps = item.Content == null ? Sequences.CreateEmptyXdmSequence() : Runtime.EvaluateXsltSequenceConstructor(item.Content, invocation);
        List<XdmMapEntry> entries = new List<XdmMapEntry>();
        int index = 0;
        while (index < maps.Count)
        {
            XdmSequenceItem candidate = Sequences.ReadXdmSequenceItem(maps, index);
            if (candidate.Kind != XdmItemKind.Map || candidate.Map == null)
            {
                throw CreateXsltMapTypeError("The sequence constructor of xsl:map must return only maps.", XsltErrorCodes.XTTE3375, item.Node);
            }

            XdmMapEntry[] supplied = candidate.Map.Entries == null ? [] : candidate.Map.Entries;
            int entryIndex = 0;
            while (entryIndex < supplied.Length)
            {
                XdmMapEntry entry = supplied[entryIndex];
                XdmAtomicValue key = entry.Key == null ? throw new InvalidOperationException("A map entry has no key.") : entry.Key;
                if (ContainsXsltMapKey(entries, key))
                {
                    throw CreateXsltMapDynamicError("The maps supplied to xsl:map contain duplicate keys.", XsltErrorCodes.XTDE3365, item.Node);
                }

                entries.Add(entry);
                entryIndex = entryIndex + 1;
            }

            index = index + 1;
        }

        return Sequences.CreateXdmMapSequence(Maps.CreateXdmMap(entries.ToArray()));
    }

    internal bool ContainsXsltMapKey(List<XdmMapEntry> entries, XdmAtomicValue key)
    {
        int index = 0;
        while (index < entries.Count)
        {
            XdmAtomicValue? candidate = entries[index].Key;
            if (candidate != null && Maps.SameXdmMapKey(candidate, key))
            {
                return true;
            }

            index = index + 1;
        }

        return false;
    }

    internal XdmSequence EvaluateXsltMapEntry(XsltSequenceItemPlan item, XsltTemplateInvocation invocation)
    {
        if (item.Instruction == null || invocation.Frame == null)
        {
            throw new InvalidOperationException("A map-entry instruction has no plan or variable frame.");
        }

        XsltInstructionAttributePlan? keyPlan = Runtime.FindXsltAttribute(item.Instruction, "key");
        if (keyPlan == null || keyPlan.Expression == null)
        {
            throw new InvalidOperationException("A map-entry instruction has no key expression.");
        }

        XdmSequence atomized = Converter.AtomizeXdmSequence(Templates.EvaluateXsltInstructionExpression(keyPlan.Expression, invocation.Frame));
        if (atomized.Count != 1)
        {
            throw CreateXsltMapTypeError("The key of xsl:map-entry must be one atomic value.", XPathErrorCodes.XPTY0004, item.Node);
        }

        XdmSequenceItem keyItem = Sequences.ReadXdmSequenceItem(atomized, 0);
        XdmAtomicValue? key = keyItem.AtomicValue;
        if (key == null)
        {
            throw CreateXsltMapTypeError("The key of xsl:map-entry must be atomic.", XPathErrorCodes.XPTY0004, item.Node);
        }

        if (key.Type == XdmAtomicType.UntypedAtomic)
        {
            key = Factory.CreateXdmStringAtomicValue(Converter.ReadXdmAtomicStringValue(key), XdmAtomicType.String);
        }

        XdmSequence value = Runtime.EvaluateXsltSequenceInstruction(item, invocation);
        XdmMapEntry entry = Maps.CreateXdmMapEntry(key, value);
        return Sequences.CreateXdmMapSequence(Maps.CreateXdmMap([entry]));
    }

    internal XdmSequence EvaluateXsltArray(XsltSequenceItemPlan item, XsltTemplateInvocation invocation)
    {
        List<XdmSequence> members = new List<XdmSequence>();
        XsltSequenceItemPlan[] content = item.Content == null ? [] : item.Content.Items;
        int index = 0;
        while (index < content.Length)
        {
            XsltSequenceItemPlan child = content[index];
            XsltInstructionKind kind = child.Instruction == null ? XsltInstructionKind.Unknown : child.Instruction.Kind;
            XdmSequence value = kind == XsltInstructionKind.ArrayMember ? Runtime.EvaluateXsltSequenceInstruction(child, invocation) : Runtime.EvaluateXsltSequenceItem(child, invocation);
            if (kind == XsltInstructionKind.ArrayMember)
            {
                members.Add(value);
            }
            else
            {
                XdmArrayValue expanded = Arrays.CreateXdmArrayFromSequence(value);
                XdmSequence[] expandedMembers = expanded.Members == null ? [] : expanded.Members;
                int memberIndex = 0;
                while (memberIndex < expandedMembers.Length)
                {
                    members.Add(expandedMembers[memberIndex]);
                    memberIndex = memberIndex + 1;
                }
            }

            index = index + 1;
        }

        return Sequences.CreateXdmArraySequence(Arrays.CreateXdmArray(members.ToArray()));
    }

    internal XsltDynamicException CreateXsltMapDynamicError(string message, string code, XmlNode node)
    {
        return Templates.CreateXsltTemplateDynamicError(message, code, node);
    }

    internal XsltTypeException CreateXsltMapTypeError(string message, string code, XmlNode node)
    {
        return Templates.CreateXsltTemplateTypeError(message, code, node);
    }
}
