using System;
using System.Collections;
using System.Collections.Generic;

namespace xml;

internal class XsltMergeSourceState
{
    internal XsltSequenceItemPlan Plan;
    internal string Name;
    internal bool HasName;
    internal bool SortBeforeMerge;
    internal bool Streamable;
    internal XsltSequenceItemPlan[] KeyPlans;
    internal XsltSortComponent[] Components;

    internal XsltMergeSourceState(XsltSequenceItemPlan plan)
    {
        Plan = plan;
        Name = "";
        HasName = false;
        SortBeforeMerge = false;
        Streamable = false;
        KeyPlans = [];
        Components = [];
    }
}

internal class XsltMergeRecordState
{
    internal int SourceIndex;
    internal int AnchorIndex;
    internal int ItemIndex;

    internal XsltMergeRecordState(int sourceIndex, int anchorIndex, int itemIndex)
    {
        SourceIndex = sourceIndex;
        AnchorIndex = anchorIndex;
        ItemIndex = itemIndex;
    }
}

internal class XsltMergeRuntime
{
    internal XsltTemplateRuntime Templates;
    internal XsltSequenceConstructorRuntime Runtime;
    internal XsltTransformationState State;
    internal XdmSequenceOperations Sequences;
    internal XdmTypeConverter Converter;
    internal XsltSortRuntime Sorting;

    internal XsltMergeRuntime(XsltSequenceConstructorRuntime runtime)
    {
        Runtime = runtime;
        Templates = runtime.Templates;
        State = runtime.State;
        Sequences = new XdmSequenceOperations();
        Converter = new XdmTypeConverter();
        Sorting = new XsltSortRuntime(Templates);
    }

    internal XdmSequence EvaluateXsltMerge(XsltSequenceItemPlan item, XsltTemplateInvocation invocation)
    {
        XsltSequenceItemPlan[] content = item.Content == null ? [] : item.Content.Items;
        List<XsltMergeSourceState> sources = new List<XsltMergeSourceState>();
        XsltSequenceItemPlan? action = null;
        int index = 0;
        while (index < content.Length)
        {
            XsltInstructionPlan? optionalInstruction = content[index].Instruction;
            XsltInstructionKind kind = optionalInstruction == null ? XsltInstructionKind.Unknown : optionalInstruction.Kind;
            if (kind == XsltInstructionKind.MergeSource)
            {
                sources.Add(CreateXsltMergeSource(content[index], invocation));
            }
            else if (kind == XsltInstructionKind.MergeAction)
            {
                action = content[index];
            }

            index = index + 1;
        }

        if (sources.Count == 0 || action == null)
        {
            return Sequences.CreateEmptyXdmSequence();
        }

        ValidateXsltMergeComponents(sources, item.Node);
        List<object> records = new List<object>();
        index = 0;
        while (index < sources.Count)
        {
            AppendXsltMergeSourceRecords(records, sources[index], index, invocation);
            index = index + 1;
        }

        object[] sorted = records.ToArray();
        if (sorted.Length == 0)
        {
            return Sequences.CreateEmptyXdmSequence();
        }

        XsltSortComponent[] components = sources[0].Components;
        Sorting.NormalizeXsltNumericSortKeys(sorted, components);
        StableSort.SortObjects(sorted, new XsltSortComparer(Sorting, components));
        return EvaluateXsltMergeGroups(sorted, sources, action, invocation);
    }

    internal XsltMergeSourceState CreateXsltMergeSource(XsltSequenceItemPlan plan, XsltTemplateInvocation invocation)
    {
        XsltInstructionPlan instruction = plan.Instruction == null ? throw new InvalidOperationException("A merge source instruction plan is required.") : plan.Instruction;
        XsltMergeSourceState result = new XsltMergeSourceState(plan);
        XsltInstructionAttributePlan? name = Runtime.FindXsltAttribute(instruction, "name");
        result.HasName = name != null;
        result.Name = name == null ? "" : name.Source.Trim();
        result.SortBeforeMerge = Runtime.ReadXsltBooleanAttribute(instruction, "sort-before-merge", false);
        result.Streamable = Runtime.ReadXsltBooleanAttribute(instruction, "streamable", false);
        List<XsltSequenceItemPlan> keys = new List<XsltSequenceItemPlan>();
        XsltSequenceItemPlan[] content = plan.Content == null ? [] : plan.Content.Items;
        int index = 0;
        while (index < content.Length)
        {
            XsltInstructionPlan? optionalInstruction = content[index].Instruction;
            if (optionalInstruction != null && optionalInstruction.Kind == XsltInstructionKind.MergeKey)
            {
                keys.Add(content[index]);
            }

            index = index + 1;
        }

        result.KeyPlans = keys.ToArray();
        result.Components = Sorting.CreateXsltSortComponents(result.KeyPlans, invocation);
        index = 0;
        while (index < result.Components.Length)
        {
            result.Components[index].Merge = true;
            index = index + 1;
        }

        return result;
    }

    internal void ValidateXsltMergeComponents(List<XsltMergeSourceState> sources, XmlNode node)
    {
        XsltMergeSourceState first = sources[0];
        int sourceIndex = 1;
        while (sourceIndex < sources.Count)
        {
            XsltMergeSourceState current = sources[sourceIndex];
            if (current.Components.Length != first.Components.Length)
            {
                throw Runtime.CreateXsltConstructionError("All merge sources must declare the same number of merge keys.", XsltErrorCodes.XTSE2200, node);
            }

            int keyIndex = 0;
            while (keyIndex < first.Components.Length)
            {
                if (!HaveEqualXsltMergeComponent(first.Components[keyIndex], current.Components[keyIndex]))
                {
                    throw Runtime.CreateXsltConstructionError("Corresponding merge keys have different effective sort attributes.", XsltErrorCodes.XTDE2210, current.Plan.Node);
                }

                keyIndex = keyIndex + 1;
            }

            sourceIndex = sourceIndex + 1;
        }
    }

    internal bool HaveEqualXsltMergeComponent(XsltSortComponent left, XsltSortComponent right)
    {
        string[] names = ["lang", "order", "collation", "case-order", "data-type"];
        int index = 0;
        while (index < names.Length)
        {
            XsltInstructionAttributePlan? leftAttribute = Runtime.FindXsltAttribute(left.Plan.Instruction, names[index]);
            XsltInstructionAttributePlan? rightAttribute = Runtime.FindXsltAttribute(right.Plan.Instruction, names[index]);
            if (leftAttribute == null != (rightAttribute == null))
            {
                return false;
            }

            index = index + 1;
        }

        return left.Language == right.Language && left.Order == right.Order && left.CollationValue == right.CollationValue && left.CaseOrder == right.CaseOrder && left.DataType == right.DataType;
    }

    internal void AppendXsltMergeSourceRecords(List<object> destination, XsltMergeSourceState source, int sourceIndex, XsltTemplateInvocation invocation)
    {
        XdmSequence[] inputs = SelectXsltMergeInputSequences(source, invocation);
        int anchorIndex = 0;
        while (anchorIndex < inputs.Length)
        {
            object[] records = CreateXsltMergeInputRecords(inputs[anchorIndex], source, sourceIndex, anchorIndex, invocation);
            Sorting.NormalizeXsltNumericSortKeys(records, source.Components);
            XsltSortComparer comparer = new XsltSortComparer(Sorting, source.Components);
            if (source.SortBeforeMerge)
            {
                StableSort.SortObjects(records, comparer);
            }
            else
            {
                ValidateXsltMergeInputOrder(records, comparer, source.Plan.Node);
            }

            int recordIndex = 0;
            while (recordIndex < records.Length)
            {
                destination.Add(records[recordIndex]);
                recordIndex = recordIndex + 1;
            }

            anchorIndex = anchorIndex + 1;
        }
    }

    internal XdmSequence[] SelectXsltMergeInputSequences(XsltMergeSourceState source, XsltTemplateInvocation invocation)
    {
        XsltInstructionPlan instruction = source.Plan.Instruction == null ? throw new InvalidOperationException("A merge source instruction plan is required.") : source.Plan.Instruction;
        XsltInstructionAttributePlan? forEachItem = Runtime.FindXsltAttribute(instruction, "for-each-item");
        XsltInstructionAttributePlan? forEachSource = Runtime.FindXsltAttribute(instruction, "for-each-source");
        if (forEachItem == null && forEachSource == null)
        {
            return [EvaluateXsltMergeExpression(Runtime.FindXsltAttribute(instruction, "select"), invocation)];
        }

        XdmSequence anchors = EvaluateXsltMergeExpression(forEachItem == null ? forEachSource : forEachItem, invocation);
        if (forEachSource != null)
        {
            anchors = ResolveXsltMergeSourceDocuments(anchors, instruction);
        }

        XdmSequence[] result = new XdmSequence[anchors.Count];
        int index = 0;
        while (index < anchors.Count)
        {
            XdmSequence candidate = Templates.CreateXsltTemplateCandidate(anchors, index);
            XPathDynamicContext basis = invocation.Frame == null ? State.DynamicContext : invocation.Frame.Context;
            XPathDynamicContext context = Templates.CreateXsltTemplateFocusContext(basis, candidate, index + 1, anchors.Count);
            XsltVariableFrame frame = new XsltVariableFrame(invocation.Frame, invocation.Frame == null ? [] : invocation.Frame.TunnelParameters, context);
            XsltTemplateInvocation focused = Runtime.CreateXsltFocusedInvocation(invocation, candidate, frame, index + 1, anchors.Count);
            result[index] = EvaluateXsltMergeExpression(Runtime.FindXsltAttribute(instruction, "select"), focused);
            index = index + 1;
        }

        return result;
    }

    internal XdmSequence EvaluateXsltMergeExpression(XsltInstructionAttributePlan? attribute, XsltTemplateInvocation invocation)
    {
        if (attribute == null || attribute.Expression == null || invocation.Frame == null)
        {
            return Sequences.CreateEmptyXdmSequence();
        }

        return Templates.EvaluateXsltInstructionExpression(attribute.Expression, invocation.Frame);
    }

    internal XdmSequence ResolveXsltMergeSourceDocuments(XdmSequence supplied, XsltInstructionPlan instruction)
    {
        XdmSequence atomized = Converter.AtomizeXdmSequence(supplied);
        XdmSequenceBuilder result = new XdmSequenceBuilder();
        XsltDocumentRuntime documents = new XsltDocumentRuntime(Runtime);
        int index = 0;
        while (index < atomized.Count)
        {
            XdmSequenceItem item = Sequences.ReadXdmSequenceItem(atomized, index);
            string reference = item.AtomicValue == null ? "" : Converter.ReadXdmAtomicStringValue(item.AtomicValue);
            string uri = documents.ResolveXsltDocumentUri(reference, instruction);
            XmlDocument document = documents.LoadXsltDocument(uri, instruction);
            if (document.DocumentNode == null)
            {
                throw Runtime.CreateXsltConstructionError("A merge source document has no document node.", XPathErrorCodes.FODC0002, instruction.Instruction);
            }

            result.AddXdmNode(document.DocumentNode);
            index = index + 1;
        }

        return result.BuildImmutableXdmSequence();
    }

    internal object[] CreateXsltMergeInputRecords(XdmSequence input, XsltMergeSourceState source, int sourceIndex, int anchorIndex, XsltTemplateInvocation invocation)
    {
        object[] result = new object[input.Count];
        int index = 0;
        while (index < input.Count)
        {
            XdmSequence candidate = Templates.CreateXsltTemplateCandidate(input, index);
            XsltSortKey[] keys = new XsltSortKey[source.Components.Length];
            int keyIndex = 0;
            while (keyIndex < source.Components.Length)
            {
                keys[keyIndex] = Sorting.EvaluateXsltSortKey(source.Components[keyIndex], candidate, invocation, 1, 1);
                keyIndex = keyIndex + 1;
            }

            XsltSortRecord record = new XsltSortRecord(candidate, keys);
            record.Payload = new XsltMergeRecordState(sourceIndex, anchorIndex, index);
            result[index] = record;
            index = index + 1;
        }

        return result;
    }

    internal void ValidateXsltMergeInputOrder(object[] records, IComparer comparer, XmlNode node)
    {
        int index = 1;
        while (index < records.Length)
        {
            if (comparer.Compare(records[index - 1], records[index]) > 0)
            {
                throw Runtime.CreateXsltConstructionError("A merge input sequence is not sorted according to its merge keys.", XsltErrorCodes.XTDE2220, node);
            }

            index = index + 1;
        }
    }

    internal XdmSequence EvaluateXsltMergeGroups(object[] records, List<XsltMergeSourceState> sources, XsltSequenceItemPlan action, XsltTemplateInvocation invocation)
    {
        XsltSortComparer comparer = new XsltSortComparer(Sorting, sources[0].Components);
        List<int> starts = new List<int>();
        starts.Add(0);
        int index = 1;
        while (index < records.Length)
        {
            if (comparer.Compare(records[index - 1], records[index]) != 0)
            {
                starts.Add(index);
            }

            index = index + 1;
        }

        starts.Add(records.Length);
        XdmSequenceBuilder result = new XdmSequenceBuilder();
        index = 0;
        while (index + 1 < starts.Count)
        {
            result.AppendXdmSequence(EvaluateXsltMergeAction(records, starts[index], starts[index + 1], sources, action, invocation, index + 1, starts.Count - 1));
            index = index + 1;
        }

        return result.BuildImmutableXdmSequence();
    }

    internal XdmSequence EvaluateXsltMergeAction(object[] records, int start, int end, List<XsltMergeSourceState> sources, XsltSequenceItemPlan action, XsltTemplateInvocation invocation, int position, int size)
    {
        XdmSequence group = BuildXsltMergeGroup(records, start, end, -1);
        XsltSortRecord first = records[start] as XsltSortRecord ?? throw new InvalidOperationException("A merge record is required.");
        XPathDynamicContext basis = invocation.Frame == null ? State.DynamicContext : invocation.Frame.Context;
        XPathDynamicContext context = Templates.CreateXsltTemplateFocusContext(basis, first.Item, position, HasXsltStreamableMergeSource(sources) ? 0 : size);
        context.XsltCurrentMergeGroupAvailable = true;
        context.XsltCurrentMergeGroup = group;
        context.XsltCurrentMergeKeyAvailable = true;
        context.XsltCurrentMergeKey = BuildXsltCurrentMergeKey(first.Keys);
        SetXsltNamedMergeGroups(context, records, start, end, sources);
        XsltVariableFrame frame = new XsltVariableFrame(invocation.Frame, invocation.Frame == null ? [] : invocation.Frame.TunnelParameters, context);
        XsltTemplateInvocation focused = Runtime.CreateXsltFocusedInvocation(invocation, first.Item, frame, position, context.Size);
        return action.Content == null ? Sequences.CreateEmptyXdmSequence() : Runtime.EvaluateXsltSequenceConstructor(action.Content, focused);
    }

    internal XdmSequence BuildXsltMergeGroup(object[] records, int start, int end, int sourceIndex)
    {
        XdmSequenceBuilder result = new XdmSequenceBuilder();
        int index = start;
        while (index < end)
        {
            XsltSortRecord? record = records[index] as XsltSortRecord;
            XsltMergeRecordState? details = record == null ? null : record.Payload as XsltMergeRecordState;
            if (record != null && (sourceIndex < 0 || details != null && details.SourceIndex == sourceIndex))
            {
                result.AppendXdmSequence(record.Item);
            }

            index = index + 1;
        }

        return result.BuildImmutableXdmSequence();
    }

    internal XdmSequence BuildXsltCurrentMergeKey(XsltSortKey[] keys)
    {
        XdmSequenceBuilder result = new XdmSequenceBuilder();
        int index = 0;
        while (index < keys.Length)
        {
            XdmAtomicValue? value = keys[index].OriginalValue;
            if (!keys[index].Empty && value != null)
            {
                result.AddXdmAtomicValue(value);
            }

            index = index + 1;
        }

        return result.BuildImmutableXdmSequence();
    }

    internal void SetXsltNamedMergeGroups(XPathDynamicContext context, object[] records, int start, int end, List<XsltMergeSourceState> sources)
    {
        int namedCount = 0;
        int index = 0;
        while (index < sources.Count)
        {
            if (sources[index].HasName) namedCount = namedCount + 1;
            index = index + 1;
        }

        string[] names = new string[namedCount];
        XdmSequence[] groups = new XdmSequence[namedCount];
        int namedIndex = 0;
        index = 0;
        while (index < sources.Count)
        {
            if (sources[index].HasName)
            {
                names[namedIndex] = sources[index].Name;
                groups[namedIndex] = BuildXsltMergeGroup(records, start, end, index);
                namedIndex = namedIndex + 1;
            }

            index = index + 1;
        }

        context.XsltCurrentMergeSourceNames = names;
        context.XsltCurrentMergeSourceGroups = groups;
    }

    internal bool HasXsltStreamableMergeSource(List<XsltMergeSourceState> sources)
    {
        int index = 0;
        while (index < sources.Count)
        {
            if (sources[index].Streamable)
            {
                return true;
            }

            index = index + 1;
        }

        return false;
    }
}
