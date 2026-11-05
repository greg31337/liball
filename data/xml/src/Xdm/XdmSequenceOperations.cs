namespace xml;

internal class XdmSequenceOperations
{
    internal XdmSequence CreateEmptyXdmSequence()
    {
        return new XdmSequenceBuilder().BuildImmutableXdmSequence();
    }

    internal XdmSequence CreateXdmNodeSequence(XmlNode node)
    {
        XdmSequenceBuilder builder = new XdmSequenceBuilder();
        builder.AddXdmNode(node);
        return builder.BuildImmutableXdmSequence();
    }

    internal XdmSequence CreateXdmAtomicSequence(XdmAtomicValue value)
    {
        XdmSequenceBuilder builder = new XdmSequenceBuilder();
        builder.AddXdmAtomicValue(value);
        return builder.BuildImmutableXdmSequence();
    }

    internal XdmSequence CreateXdmFunctionSequence(XdmFunctionItem function)
    {
        XdmSequenceBuilder builder = new XdmSequenceBuilder();
        builder.AddXdmFunction(function);
        return builder.BuildImmutableXdmSequence();
    }

    internal XdmSequence CreateXdmMapSequence(XdmMapValue map)
    {
        XdmSequenceBuilder builder = new XdmSequenceBuilder();
        builder.AddXdmMap(map);
        return builder.BuildImmutableXdmSequence();
    }

    internal XdmSequence CreateXdmArraySequence(XdmArrayValue array)
    {
        XdmSequenceBuilder builder = new XdmSequenceBuilder();
        builder.AddXdmArray(array);
        return builder.BuildImmutableXdmSequence();
    }

    internal XdmSequence ConcatenateXdmSequences(XdmSequence[] sequences)
    {
        Guard.NotNull(sequences, "sequences");
        XdmSequenceBuilder builder = new XdmSequenceBuilder();
        int index = 0;
        while (index < sequences.Length)
        {
            XdmSequence? sequence = sequences[index];
            if (sequence == null)
            {
                throw new System.ArgumentException("The sequence collection contains a null entry.", "sequences");
            }

            builder.AppendXdmSequence(sequence);
            index = index + 1;
        }

        return builder.BuildImmutableXdmSequence();
    }

    internal XdmSequence FreezeXdmSequenceStorage(XdmSequence sequence)
    {
        Guard.NotNull(sequence, "sequence");
        XdmSequenceBuilder builder = new XdmSequenceBuilder();
        builder.AppendXdmSequence(sequence);
        return builder.BuildImmutableXdmSequence();
    }

    internal bool MatchesXdmSequenceCardinality(XdmSequence sequence, XdmOccurrence occurrence)
    {
        Guard.NotNull(sequence, "sequence");
        ValidateXdmSequenceOccurrence(occurrence);
        ValidateXdmSequenceStorage(sequence);
        if (occurrence == XdmOccurrence.ExactlyOne)
        {
            return sequence.Count == 1;
        }

        if (occurrence == XdmOccurrence.ZeroOrOne)
        {
            return sequence.Count <= 1;
        }

        if (occurrence == XdmOccurrence.OneOrMore)
        {
            return sequence.Count >= 1;
        }

        return true;
    }

    internal XdmSequence RequireXdmSequenceCardinality(XdmSequence sequence, XdmOccurrence occurrence)
    {
        if (!MatchesXdmSequenceCardinality(sequence, occurrence))
        {
            throw CreateXdmSequenceCardinalityError("The XDM sequence does not satisfy the required cardinality.");
        }

        return sequence;
    }

    internal XdmSequenceItem ReadXdmSequenceItem(XdmSequence sequence, int position)
    {
        Guard.NotNull(sequence, "sequence");
        ValidateXdmSequenceStorage(sequence);
        if (position < 0 || position >= sequence.Count)
        {
            throw new System.ArgumentOutOfRangeException("position");
        }

        return ReadValidatedXdmSequenceItem(sequence, position);
    }

    internal XdmSequenceItem ReadValidatedXdmSequenceItem(XdmSequence sequence, int position)
    {
        XdmItemKind[] kinds = sequence.ItemKinds == null ? [] : sequence.ItemKinds;
        int[] indexes = sequence.ItemIndexes == null ? [] : sequence.ItemIndexes;
        XdmItemKind kind = kinds[position];
        int index = indexes[position];
        if (kind == XdmItemKind.Node)
        {
            XmlNode[] values = sequence.Nodes == null ? [] : sequence.Nodes;
            return new XdmSequenceItem(kind, values[index], null, null, null, null);
        }

        if (kind == XdmItemKind.Atomic)
        {
            XdmAtomicValue[] values = sequence.AtomicValues == null ? [] : sequence.AtomicValues;
            return new XdmSequenceItem(kind, null, values[index], null, null, null);
        }

        if (kind == XdmItemKind.Function)
        {
            XdmFunctionItem[] values = sequence.Functions == null ? [] : sequence.Functions;
            return new XdmSequenceItem(kind, null, null, values[index], null, null);
        }

        if (kind == XdmItemKind.Map)
        {
            XdmMapValue[] values = sequence.Maps == null ? [] : sequence.Maps;
            return new XdmSequenceItem(kind, null, null, null, values[index], null);
        }

        XdmArrayValue[] arrays = sequence.Arrays == null ? [] : sequence.Arrays;
        return new XdmSequenceItem(kind, null, null, null, null, arrays[index]);
    }

    internal void ValidateXdmSequenceStorage(XdmSequence sequence)
    {
        Guard.NotNull(sequence, "sequence");
        if (sequence.Count < 0)
        {
            throw new System.ArgumentException("An XDM sequence cannot have a negative count.", "sequence");
        }

        XdmItemKind[] kinds = sequence.ItemKinds == null ? [] : sequence.ItemKinds;
        int[] indexes = sequence.ItemIndexes == null ? [] : sequence.ItemIndexes;
        if (kinds.Length != sequence.Count || indexes.Length != sequence.Count)
        {
            throw new System.ArgumentException("The ordered XDM sequence storage is inconsistent.", "sequence");
        }

        int position = 0;
        while (position < sequence.Count)
        {
            ValidateXdmSequenceItemReference(sequence, kinds[position], indexes[position]);
            position = position + 1;
        }
    }

    internal void ValidateXdmSequenceItemReference(XdmSequence sequence, XdmItemKind kind, int index)
    {
        bool valid = false;
        if (kind == XdmItemKind.Node)
        {
            XmlNode[] values = sequence.Nodes == null ? [] : sequence.Nodes;
            valid = index >= 0 && index < values.Length && values[index] != null && IsXdmSequenceNodeKind(values[index].Kind);
        }
        else if (kind == XdmItemKind.Atomic)
        {
            XdmAtomicValue[] values = sequence.AtomicValues == null ? [] : sequence.AtomicValues;
            valid = index >= 0 && index < values.Length && values[index] != null;
        }
        else if (kind == XdmItemKind.Function)
        {
            XdmFunctionItem[] values = sequence.Functions == null ? [] : sequence.Functions;
            valid = index >= 0 && index < values.Length && values[index] != null;
        }
        else if (kind == XdmItemKind.Map)
        {
            XdmMapValue[] values = sequence.Maps == null ? [] : sequence.Maps;
            valid = index >= 0 && index < values.Length && values[index] != null;
        }
        else if (kind == XdmItemKind.Array)
        {
            XdmArrayValue[] values = sequence.Arrays == null ? [] : sequence.Arrays;
            valid = index >= 0 && index < values.Length && values[index] != null;
        }

        if (!valid)
        {
            throw new System.ArgumentException("An XDM sequence item reference is invalid.", "sequence");
        }
    }

    internal bool IsXdmSequenceNodeKind(XmlNodeKind kind)
    {
        return kind == XmlNodeKind.Document || kind == XmlNodeKind.DocumentFragment || kind == XmlNodeKind.Element || kind == XmlNodeKind.Attribute || kind == XmlNodeKind.Namespace || kind == XmlNodeKind.Text || kind == XmlNodeKind.CData || kind == XmlNodeKind.ProcessingInstruction || kind == XmlNodeKind.Comment;
    }

    internal void ValidateXdmSequenceOccurrence(XdmOccurrence occurrence)
    {
        if (occurrence < XdmOccurrence.ExactlyOne || occurrence > XdmOccurrence.OneOrMore)
        {
            throw new System.ArgumentOutOfRangeException("occurrence");
        }
    }

    internal XPathTypeException CreateXdmSequenceCardinalityError(string message)
    {
        XmlDiagnostic diagnostic = new XmlDiagnostic();
        diagnostic.Code = XPathErrorCodes.XPTY0004;
        diagnostic.Category = XmlErrorCategory.XPathType;
        diagnostic.Severity = XmlDiagnosticSeverity.Fatal;
        diagnostic.Message = message;
        return new XPathTypeException(message, diagnostic, null);
    }
}
