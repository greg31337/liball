using System;

namespace xml;

internal class XdmArrayOperations
{
    internal XdmSequenceOperations Sequences;

    internal XdmArrayOperations()
    {
        Sequences = new XdmSequenceOperations();
    }

    internal XdmArrayValue CreateEmptyXdmArray()
    {
        return CreateXdmArray([]);
    }

    internal XdmArrayValue CreateXdmArray(XdmSequence[] members)
    {
        Guard.NotNull(members, "members");
        XdmSequence[] copiedMembers = new XdmSequence[members.Length];
        int index = 0;
        while (index < members.Length)
        {
            XdmSequence? member = members[index];
            if (member == null)
            {
                throw new ArgumentException("An XDM array cannot contain a null member.", "members");
            }

            copiedMembers[index] = Sequences.FreezeXdmSequenceStorage(member);
            index = index + 1;
        }

        XdmArrayValue result = new XdmArrayValue();
        result.Members = copiedMembers;
        result.Count = copiedMembers.Length;
        result.IsImmutable = true;
        return result;
    }

    internal XdmArrayValue CreateXdmArrayFromSequence(XdmSequence sequence)
    {
        Sequences.ValidateXdmSequenceStorage(sequence);
        XdmSequence[] members = new XdmSequence[sequence.Count];
        int index = 0;
        while (index < sequence.Count)
        {
            XdmSequenceBuilder builder = new XdmSequenceBuilder();
            AddXdmArraySequenceItem(builder, Sequences.ReadValidatedXdmSequenceItem(sequence, index));
            members[index] = builder.BuildImmutableXdmSequence();
            index = index + 1;
        }

        return CreateXdmArray(members);
    }

    internal void ValidateXdmArrayStorage(XdmArrayValue array)
    {
        Guard.NotNull(array, "array");
        if (array.Count < 0)
        {
            throw new ArgumentException("An XDM array cannot have a negative count.", "array");
        }

        XdmSequence[] members = array.Members == null ? [] : array.Members;
        if (members.Length != array.Count)
        {
            throw new ArgumentException("The XDM array storage is inconsistent.", "array");
        }

        int index = 0;
        while (index < members.Length)
        {
            if (members[index] == null)
            {
                throw new ArgumentException("An XDM array cannot contain a null member.", "array");
            }

            Sequences.ValidateXdmSequenceStorage(members[index]);
            index = index + 1;
        }
    }

    internal int GetXdmArraySize(XdmArrayValue array)
    {
        ValidateXdmArrayStorage(array);
        return array.Count;
    }

    internal XdmSequence GetXdmArrayMember(XdmArrayValue array, int position)
    {
        ValidateXdmArrayStorage(array);
        RequireXdmArrayPosition(array, position, false);
        XdmSequence[] members = array.Members == null ? [] : array.Members;
        return members[position - 1];
    }

    internal XdmArrayValue PutXdmArrayMember(XdmArrayValue array, int position, XdmSequence member)
    {
        ValidateXdmArrayStorage(array);
        RequireXdmArrayPosition(array, position, false);
        Sequences.ValidateXdmSequenceStorage(member);
        XdmSequence[] source = array.Members == null ? [] : array.Members;
        XdmSequence[] updated = new XdmSequence[source.Length];
        Array.Copy(source, updated, source.Length);
        updated[position - 1] = member;
        return CreateXdmArray(updated);
    }

    internal XdmArrayValue AppendXdmArrayMember(XdmArrayValue array, XdmSequence member)
    {
        ValidateXdmArrayStorage(array);
        Sequences.ValidateXdmSequenceStorage(member);
        XdmSequence[] source = array.Members == null ? [] : array.Members;
        XdmSequence[] updated = new XdmSequence[source.Length + 1];
        Array.Copy(source, updated, source.Length);
        updated[source.Length] = member;
        return CreateXdmArray(updated);
    }

    internal XdmArrayValue InsertBeforeXdmArrayMember(XdmArrayValue array, int position, XdmSequence member)
    {
        ValidateXdmArrayStorage(array);
        RequireXdmArrayPosition(array, position, true);
        Sequences.ValidateXdmSequenceStorage(member);
        XdmSequence[] source = array.Members == null ? [] : array.Members;
        XdmSequence[] updated = new XdmSequence[source.Length + 1];
        int sourceIndex = 0;
        int resultIndex = 0;
        while (resultIndex < updated.Length)
        {
            if (resultIndex == position - 1)
            {
                updated[resultIndex] = member;
            }
            else
            {
                updated[resultIndex] = source[sourceIndex];
                sourceIndex = sourceIndex + 1;
            }

            resultIndex = resultIndex + 1;
        }

        return CreateXdmArray(updated);
    }

    internal XdmArrayValue SubarrayXdmArray(XdmArrayValue array, int start, int length)
    {
        ValidateXdmArrayStorage(array);
        if (start < 1 || start > array.Count + 1)
        {
            throw CreateXdmArrayDynamicError("The array start position is out of bounds.", XPathErrorCodes.FOAY0001);
        }

        if (length < 0)
        {
            throw CreateXdmArrayDynamicError("The array subarray length cannot be negative.", XPathErrorCodes.FOAY0002);
        }

        if (length > array.Count - start + 1)
        {
            throw CreateXdmArrayDynamicError("The requested subarray extends beyond the array.", XPathErrorCodes.FOAY0001);
        }

        XdmSequence[] source = array.Members == null ? [] : array.Members;
        XdmSequence[] selected = new XdmSequence[length];
        Array.Copy(source, start - 1, selected, 0, length);
        return CreateXdmArray(selected);
    }

    internal XdmArrayValue RemoveXdmArrayMembers(XdmArrayValue array, int[] positions)
    {
        ValidateXdmArrayStorage(array);
        Guard.NotNull(positions, "positions");
        bool[] removed = new bool[array.Count];
        int index = 0;
        while (index < positions.Length)
        {
            RequireXdmArrayPosition(array, positions[index], false);
            removed[positions[index] - 1] = true;
            index = index + 1;
        }

        int retainedCount = 0;
        index = 0;
        while (index < removed.Length)
        {
            if (!removed[index])
            {
                retainedCount = retainedCount + 1;
            }

            index = index + 1;
        }

        XdmSequence[] source = array.Members == null ? [] : array.Members;
        XdmSequence[] retained = new XdmSequence[retainedCount];
        int resultIndex = 0;
        index = 0;
        while (index < source.Length)
        {
            if (!removed[index])
            {
                retained[resultIndex] = source[index];
                resultIndex = resultIndex + 1;
            }

            index = index + 1;
        }

        return CreateXdmArray(retained);
    }

    internal XdmSequence HeadXdmArray(XdmArrayValue array)
    {
        return GetXdmArrayMember(array, 1);
    }

    internal XdmArrayValue TailXdmArray(XdmArrayValue array)
    {
        ValidateXdmArrayStorage(array);
        if (array.Count == 0)
        {
            throw CreateXdmArrayDynamicError("An empty array has no tail.", XPathErrorCodes.FOAY0001);
        }

        return SubarrayXdmArray(array, 2, array.Count - 1);
    }

    internal XdmArrayValue ReverseXdmArray(XdmArrayValue array)
    {
        ValidateXdmArrayStorage(array);
        XdmSequence[] source = array.Members == null ? [] : array.Members;
        XdmSequence[] reversed = new XdmSequence[source.Length];
        int index = 0;
        while (index < source.Length)
        {
            reversed[index] = source[source.Length - index - 1];
            index = index + 1;
        }

        return CreateXdmArray(reversed);
    }

    internal XdmArrayValue JoinXdmArrays(XdmArrayValue[] arrays)
    {
        Guard.NotNull(arrays, "arrays");
        int count = 0;
        int index = 0;
        while (index < arrays.Length)
        {
            XdmArrayValue? array = arrays[index];
            if (array == null)
            {
                throw new ArgumentException("The array collection contains a null entry.", "arrays");
            }

            ValidateXdmArrayStorage(array);
            if (count > int.MaxValue - array.Count)
            {
                throw new ArgumentException("The joined array is too large.", "arrays");
            }

            count = count + array.Count;
            index = index + 1;
        }

        XdmSequence[] joined = new XdmSequence[count];
        int resultIndex = 0;
        index = 0;
        while (index < arrays.Length)
        {
            XdmArrayValue array = arrays[index];
            XdmSequence[] members = array.Members == null ? [] : array.Members;
            Array.Copy(members, 0, joined, resultIndex, members.Length);
            resultIndex = resultIndex + members.Length;
            index = index + 1;
        }

        return CreateXdmArray(joined);
    }

    internal void RequireXdmArrayPosition(XdmArrayValue array, int position, bool allowAfterLast)
    {
        int maximum = allowAfterLast ? array.Count + 1 : array.Count;
        if (position < 1 || position > maximum)
        {
            throw CreateXdmArrayDynamicError("The array position is out of bounds.", XPathErrorCodes.FOAY0001);
        }
    }

    internal void AddXdmArraySequenceItem(XdmSequenceBuilder builder, XdmSequenceItem item)
    {
        if (item.Kind == XdmItemKind.Node && item.Node != null)
        {
            builder.AddXdmNode(item.Node);
        }
        else if (item.Kind == XdmItemKind.Atomic && item.AtomicValue != null)
        {
            builder.AddXdmAtomicValue(item.AtomicValue);
        }
        else if (item.Kind == XdmItemKind.Function && item.Function != null)
        {
            builder.AddXdmFunction(item.Function);
        }
        else if (item.Kind == XdmItemKind.Map && item.Map != null)
        {
            builder.AddXdmMap(item.Map);
        }
        else if (item.Kind == XdmItemKind.Array && item.Array != null)
        {
            builder.AddXdmArray(item.Array);
        }
        else
        {
            throw new ArgumentException("The XDM sequence item is incomplete.", "item");
        }
    }

    internal XPathDynamicException CreateXdmArrayDynamicError(string message, string code)
    {
        XmlDiagnostic diagnostic = new XmlDiagnostic();
        diagnostic.Code = code;
        diagnostic.Category = XmlErrorCategory.XPathDynamic;
        diagnostic.Severity = XmlDiagnosticSeverity.Fatal;
        diagnostic.Message = message;
        return new XPathDynamicException(message, diagnostic, null);
    }
}
