using System.Collections.Generic;

namespace xml;

internal class XsltStreamingRuntime
{
    internal XsltTransformationState State;
    internal XmlSerializationEventSink? ResultSink;
    internal int SelectionDepth;
    internal int DeliveredItemCount;
    internal bool DeliveredResult;
    internal bool PreviousItemWasAtomic;
    internal List<XdmArrayValue> ActiveArrays;

    internal XsltStreamingRuntime(XsltTransformationState state)
    {
        State = state;
        ResultSink = CreateXsltStreamingResultSink(state);
        SelectionDepth = 0;
        DeliveredItemCount = 0;
        DeliveredResult = false;
        PreviousItemWasAtomic = false;
        ActiveArrays = new List<XdmArrayValue>();
    }

    internal XmlSerializationEventSink? CreateXsltStreamingResultSink(XsltTransformationState state)
    {
        XmlOutput? output = state.PrincipalDestination.Output;
        XmlSerializationOptions options = state.PrincipalDestination.SerializationOptions;
        if (output == null || options.Method != XmlSerializationMethod.Xml && options.Method != XmlSerializationMethod.Text || options.Indent || options.DoctypePublic != null || options.DoctypeSystem != null)
        {
            return null;
        }

        string normalization = options.NormalizationForm == null ? "none" : options.NormalizationForm.Trim().ToLowerInvariant();
        if (normalization.Length != 0 && normalization != "none")
        {
            return null;
        }

        return new XmlSerializationEventSink(output, options);
    }

    internal bool IsXsltStreamingMode(string name)
    {
        int index = 0;
        while (index < State.Stylesheet.Modes.Length)
        {
            XsltModePlan mode = State.Stylesheet.Modes[index];
            if (mode.Name == name)
            {
                return mode.Streamable;
            }

            index = index + 1;
        }

        return false;
    }

    internal void EnterXsltStreamingSelection(string mode)
    {
        if (IsXsltStreamingMode(mode))
        {
            SelectionDepth = SelectionDepth + 1;
        }
    }

    internal void LeaveXsltStreamingSelection(string mode)
    {
        if (IsXsltStreamingMode(mode))
        {
            SelectionDepth = SelectionDepth - 1;
        }
    }

    internal void BeginXsltStreamingCandidate(XdmSequence candidate, string mode)
    {
        if (!IsXsltStreamingMode(mode) || candidate.Count != 1)
        {
            return;
        }

        XdmSequenceItem item = new XdmSequenceOperations().ReadXdmSequenceItem(candidate, 0);
        if (item.Node == null)
        {
            return;
        }

        XsltStreamedNodeRuntime? streamed = State.StreamedNodeRuntime;
        if (streamed != null)
        {
            streamed.RegisterXsltStreamFocus(item.Node);
        }

        XsltModePlan? modePlan = FindXsltStreamingModePlan(mode);
        if (modePlan != null && State.AccumulatorRuntime != null)
        {
            State.AccumulatorRuntime.BeginXsltStreamingAccumulatorNode(modePlan, item.Node);
        }
    }

    internal void EndXsltStreamingCandidate(XdmSequence candidate, string mode)
    {
        if (!IsXsltStreamingMode(mode) || candidate.Count != 1)
        {
            return;
        }

        XdmSequenceItem item = new XdmSequenceOperations().ReadXdmSequenceItem(candidate, 0);
        XsltModePlan? modePlan = FindXsltStreamingModePlan(mode);
        if (item.Node != null && modePlan != null && State.AccumulatorRuntime != null)
        {
            State.AccumulatorRuntime.EndXsltStreamingAccumulatorNode(modePlan, item.Node);
        }
    }

    internal XsltModePlan? FindXsltStreamingModePlan(string name)
    {
        int index = 0;
        while (index < State.Stylesheet.Modes.Length)
        {
            if (State.Stylesheet.Modes[index].Name == name)
            {
                return State.Stylesheet.Modes[index];
            }

            index = index + 1;
        }

        return null;
    }

    internal void DeliverXsltStreamingCandidateResult(XdmSequence value, string mode)
    {
        if (!IsXsltStreamingMode(mode) || SelectionDepth != 1 || ResultSink == null)
        {
            return;
        }

        DeliverXsltStreamingSequence(value);
        DeliveredResult = true;
    }

    internal bool CompleteXsltStreamingResult(XdmSequence value)
    {
        XmlSerializationEventSink? sink = ResultSink;
        if (sink == null)
        {
            return false;
        }

        if (!DeliveredResult)
        {
            DeliverXsltStreamingSequence(value);
        }

        sink.CompleteXmlEventSink();
        return true;
    }

    internal void DeliverXsltStreamingSequence(XdmSequence value)
    {
        XdmSequenceOperations sequences = new XdmSequenceOperations();
        int index = 0;
        while (index < value.Count)
        {
            DeliverXsltStreamingItem(sequences.ReadXdmSequenceItem(value, index));
            index = index + 1;
        }
    }

    internal void DeliverXsltStreamingItem(XdmSequenceItem item)
    {
        if (item.Array != null)
        {
            DeliverXsltStreamingArray(item.Array);
            return;
        }

        if (item.Node != null)
        {
            WriteXsltStreamingItemSeparator(false);
            DeliverXsltStreamingNode(item.Node);
            DeliveredItemCount = DeliveredItemCount + 1;
            PreviousItemWasAtomic = false;
            return;
        }

        if (item.AtomicValue != null)
        {
            WriteXsltStreamingItemSeparator(true);
            string value = new XdmTypeConverter().ReadXdmAtomicStringValue(item.AtomicValue);
            FindXsltStreamingResultSink().HandleXmlEvent(new XmlEvent { Kind = XmlEventKind.Text, Value = value });
            DeliveredItemCount = DeliveredItemCount + 1;
            PreviousItemWasAtomic = true;
            return;
        }

        throw new System.InvalidOperationException("A streaming XML result cannot contain a function or map item.");
    }

    internal void DeliverXsltStreamingArray(XdmArrayValue array)
    {
        int activeIndex = 0;
        while (activeIndex < ActiveArrays.Count)
        {
            if (object.ReferenceEquals(ActiveArrays[activeIndex], array))
            {
                throw new System.InvalidOperationException("A streaming XML result cannot contain a recursive array.");
            }

            activeIndex = activeIndex + 1;
        }

        ActiveArrays.Add(array);
        XdmSequence[] members = array.Members == null ? [] : array.Members;
        int index = 0;
        while (index < members.Length)
        {
            DeliverXsltStreamingSequence(members[index]);
            index = index + 1;
        }

        ActiveArrays.RemoveAt(ActiveArrays.Count - 1);
    }

    internal void WriteXsltStreamingItemSeparator(bool atomic)
    {
        if (DeliveredItemCount == 0)
        {
            return;
        }

        string? separator = State.PrincipalDestination.SerializationOptions.ItemSeparator;
        if (separator != null || atomic && PreviousItemWasAtomic)
        {
            FindXsltStreamingResultSink().HandleXmlEvent(new XmlEvent { Kind = XmlEventKind.Text, Value = separator == null ? " " : separator });
        }
    }

    internal void DeliverXsltStreamingNode(XmlNode node)
    {
        if (node.Kind == XmlNodeKind.Document || node.Kind == XmlNodeKind.DocumentFragment)
        {
            XmlNode[] children = node.Children == null ? [] : node.Children;
            int childIndex = 0;
            while (childIndex < children.Length)
            {
                DeliverXsltStreamingNodeEvents(children[childIndex]);
                childIndex = childIndex + 1;
            }

            return;
        }

        DeliverXsltStreamingNodeEvents(node);
    }

    internal void DeliverXsltStreamingNodeEvents(XmlNode node)
    {
        XmlEventBatch events = new XmlEventEmitter().EmitXmlNodeEvents(node, CreateXsltStreamingParserOptions());
        XmlEvent[] batch = events.Events == null ? [] : events.Events;
        int eventIndex = events.Offset;
        int end = events.Offset + events.Count;
        while (eventIndex < end)
        {
            FindXsltStreamingResultSink().HandleXmlEvent(batch[eventIndex]);
            eventIndex = eventIndex + 1;
        }
    }

    internal XmlSerializationEventSink FindXsltStreamingResultSink()
    {
        return ResultSink == null ? throw new System.InvalidOperationException("The streaming result sink is unavailable.") : ResultSink;
    }

    internal XmlParserOptions CreateXsltStreamingParserOptions()
    {
        XmlParserOptions result = new XmlParserOptions();
        result.Version = XmlVersion.Xml10;
        result.NamespaceHandling = XmlNamespaceHandling.Namespaces10;
        result.DtdProcessing = XmlDtdProcessing.Prohibit;
        result.EntityHandling = XmlEntityHandling.Expand;
        result.PreserveComments = true;
        result.PreserveProcessingInstructions = true;
        result.PreservePrefixes = true;
        result.RetainNamespaceDeclarations = true;
        return result;
    }
}
