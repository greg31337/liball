using System;
using System.Collections.Generic;
using System.Globalization;
using System.Numerics;

namespace xml;

internal class XPathArraySortEntry
{
    internal XdmSequence Member;
    internal XdmSequence Key;

    internal XPathArraySortEntry(XdmSequence member, XdmSequence key)
    {
        Member = member;
        Key = key;
    }
}

internal class XPathArrayFunctionImplementation : IXPathFunction
{
    internal string LocalName;
    internal SourceRange Range;
    internal XPathStaticContext StaticContext;
    internal XdmSequenceOperations Sequences;
    internal XdmArrayOperations Arrays;
    internal XdmFunctionOperations Functions;
    internal XdmTypeConverter Converter;
    internal XdmFactory Factory;

    internal XPathArrayFunctionImplementation(string localName, SourceRange range, XPathStaticContext staticContext)
    {
        LocalName = localName;
        Range = range;
        StaticContext = staticContext;
        Sequences = new XdmSequenceOperations();
        Arrays = new XdmArrayOperations();
        Functions = new XdmFunctionOperations();
        Converter = new XdmTypeConverter();
        Factory = new XdmFactory();
    }

    public XdmSequence InvokeXPathFunction(XPathFunctionBinding binding, XdmSequence[] arguments, XPathDynamicContext context)
    {
        if (LocalName == "join")
        {
            return EvaluateXPathArrayJoin(arguments[0]);
        }

        if (LocalName == "flatten")
        {
            XdmSequenceBuilder flattened = new XdmSequenceBuilder();
            FlattenXPathArraySequence(arguments[0], flattened);
            return flattened.BuildImmutableXdmSequence();
        }

        XdmArrayValue array = ReadXPathRequiredArray(arguments[0]);
        if (LocalName == "size")
        {
            return CreateXPathArrayIntegerResult(array.Count);
        }

        if (LocalName == "get")
        {
            return Arrays.GetXdmArrayMember(array, ReadXPathArrayPosition(arguments[1], array.Count, false));
        }

        if (LocalName == "put")
        {
            int position = ReadXPathArrayPosition(arguments[1], array.Count, false);
            return Sequences.CreateXdmArraySequence(Arrays.PutXdmArrayMember(array, position, arguments[2]));
        }

        if (LocalName == "append")
        {
            return Sequences.CreateXdmArraySequence(Arrays.AppendXdmArrayMember(array, arguments[1]));
        }

        if (LocalName == "subarray")
        {
            return EvaluateXPathSubarray(array, arguments);
        }

        if (LocalName == "remove")
        {
            int[] positions = ReadXPathArrayPositions(arguments[1], array.Count);
            return Sequences.CreateXdmArraySequence(Arrays.RemoveXdmArrayMembers(array, positions));
        }

        if (LocalName == "insert-before")
        {
            int position = ReadXPathArrayPosition(arguments[1], array.Count, true);
            return Sequences.CreateXdmArraySequence(Arrays.InsertBeforeXdmArrayMember(array, position, arguments[2]));
        }

        if (LocalName == "head")
        {
            return Arrays.HeadXdmArray(array);
        }

        if (LocalName == "tail")
        {
            return Sequences.CreateXdmArraySequence(Arrays.TailXdmArray(array));
        }

        if (LocalName == "reverse")
        {
            return Sequences.CreateXdmArraySequence(Arrays.ReverseXdmArray(array));
        }

        if (LocalName == "for-each")
        {
            return EvaluateXPathArrayForEach(array, arguments[1], context);
        }

        if (LocalName == "filter")
        {
            return EvaluateXPathArrayFilter(array, arguments[1], context);
        }

        if (LocalName == "fold-left" || LocalName == "fold-right")
        {
            return EvaluateXPathArrayFold(array, arguments[1], arguments[2], context);
        }

        if (LocalName == "for-each-pair")
        {
            return EvaluateXPathArrayForEachPair(array, ReadXPathRequiredArray(arguments[1]), arguments[2], context);
        }

        if (LocalName == "sort")
        {
            return EvaluateXPathArraySort(array, arguments, context);
        }

        throw new InvalidOperationException("The XPath array function is not implemented.");
    }

    internal XdmSequence EvaluateXPathSubarray(XdmArrayValue array, XdmSequence[] arguments)
    {
        BigInteger startValue = ReadXPathArrayInteger(arguments[1]);
        if (startValue < BigInteger.One || startValue > new BigInteger(array.Count) + BigInteger.One)
        {
            throw Arrays.CreateXdmArrayDynamicError("The array start position is out of bounds.", XPathErrorCodes.FOAY0001);
        }

        int start = (int)startValue;
        BigInteger lengthValue = arguments.Length == 2 ? new BigInteger(array.Count - start + 1) : ReadXPathArrayInteger(arguments[2]);
        if (lengthValue < BigInteger.Zero)
        {
            throw Arrays.CreateXdmArrayDynamicError("The array subarray length cannot be negative.", XPathErrorCodes.FOAY0002);
        }

        if (lengthValue > new BigInteger(array.Count - start + 1))
        {
            throw Arrays.CreateXdmArrayDynamicError("The requested subarray extends beyond the array.", XPathErrorCodes.FOAY0001);
        }

        return Sequences.CreateXdmArraySequence(Arrays.SubarrayXdmArray(array, start, (int)lengthValue));
    }

    internal XdmSequence EvaluateXPathArrayJoin(XdmSequence supplied)
    {
        XdmArrayValue[] values = new XdmArrayValue[supplied.Count];
        int index = 0;
        while (index < supplied.Count)
        {
            XdmSequenceItem item = Sequences.ReadXdmSequenceItem(supplied, index);
            if (item.Kind != XdmItemKind.Array || item.Array == null)
            {
                throw CreateXPathArrayTypeError("array:join requires a sequence containing only arrays.");
            }

            values[index] = item.Array;
            index = index + 1;
        }

        return Sequences.CreateXdmArraySequence(Arrays.JoinXdmArrays(values));
    }

    internal XdmSequence EvaluateXPathArrayForEach(XdmArrayValue array, XdmSequence suppliedAction, XPathDynamicContext context)
    {
        XdmFunctionItem action = ReadXPathArrayCallable(suppliedAction, 1);
        XdmSequence[] source = ReadXPathArrayMembers(array);
        XdmSequence[] result = new XdmSequence[source.Length];
        int index = 0;
        while (index < source.Length)
        {
            result[index] = Functions.InvokeXdmFunctionItem(action, [source[index]], context, StaticContext);
            index = index + 1;
        }

        return Sequences.CreateXdmArraySequence(Arrays.CreateXdmArray(result));
    }

    internal XdmSequence EvaluateXPathArrayFilter(XdmArrayValue array, XdmSequence suppliedFunction, XPathDynamicContext context)
    {
        XdmFunctionItem function = ReadXPathArrayCallable(suppliedFunction, 1);
        XdmSequence[] source = ReadXPathArrayMembers(array);
        XdmSequence[] retained = new XdmSequence[source.Length];
        int count = 0;
        int index = 0;
        while (index < source.Length)
        {
            XdmSequence predicate = Functions.InvokeXdmFunctionItem(function, [source[index]], context, StaticContext);
            XdmSequenceType expected = CreateXPathArrayAtomicType(XdmAtomicType.Boolean, XdmOccurrence.ExactlyOne);
            XdmSequence converted = Converter.ApplyXdmFunctionConversion(predicate, expected, StaticContext);
            XdmAtomicValue? value = Sequences.ReadXdmSequenceItem(converted, 0).AtomicValue;
            if (value != null && value.BooleanValue)
            {
                retained[count] = source[index];
                count = count + 1;
            }

            index = index + 1;
        }

        XdmSequence[] result = new XdmSequence[count];
        Array.Copy(retained, result, count);
        return Sequences.CreateXdmArraySequence(Arrays.CreateXdmArray(result));
    }

    internal XdmSequence EvaluateXPathArrayFold(XdmArrayValue array, XdmSequence zero, XdmSequence suppliedFunction, XPathDynamicContext context)
    {
        XdmFunctionItem function = ReadXPathArrayCallable(suppliedFunction, 2);
        XdmSequence[] members = ReadXPathArrayMembers(array);
        XdmSequence result = zero;
        if (LocalName == "fold-left")
        {
            int index = 0;
            while (index < members.Length)
            {
                result = Functions.InvokeXdmFunctionItem(function, [result, members[index]], context, StaticContext);
                index = index + 1;
            }
        }
        else
        {
            int index = members.Length - 1;
            while (index >= 0)
            {
                result = Functions.InvokeXdmFunctionItem(function, [members[index], result], context, StaticContext);
                index = index - 1;
            }
        }

        return result;
    }

    internal XdmSequence EvaluateXPathArrayForEachPair(XdmArrayValue left, XdmArrayValue right, XdmSequence suppliedFunction, XPathDynamicContext context)
    {
        XdmFunctionItem function = ReadXPathArrayCallable(suppliedFunction, 2);
        XdmSequence[] leftMembers = ReadXPathArrayMembers(left);
        XdmSequence[] rightMembers = ReadXPathArrayMembers(right);
        int count = leftMembers.Length < rightMembers.Length ? leftMembers.Length : rightMembers.Length;
        XdmSequence[] result = new XdmSequence[count];
        int index = 0;
        while (index < count)
        {
            result[index] = Functions.InvokeXdmFunctionItem(function, [leftMembers[index], rightMembers[index]], context, StaticContext);
            index = index + 1;
        }

        return Sequences.CreateXdmArraySequence(Arrays.CreateXdmArray(result));
    }

    internal XdmSequence EvaluateXPathArraySort(XdmArrayValue array, XdmSequence[] arguments, XPathDynamicContext context)
    {
        XPathSequenceFunctionImplementation sequenceFunctions = new XPathSequenceFunctionImplementation("sort", Range, StaticContext);
        IXPathCollation collation = sequenceFunctions.ResolveXPathSequenceCollation(arguments, 1, true);
        XdmFunctionItem? keyFunction = arguments.Length == 3 ? ReadXPathArrayCallable(arguments[2], 1) : null;
        XdmSequence[] members = ReadXPathArrayMembers(array);
        XPathArraySortEntry[] entries = new XPathArraySortEntry[members.Length];
        int index = 0;
        while (index < members.Length)
        {
            XdmSequence key = keyFunction == null ? Converter.AtomizeXdmSequence(members[index]) : Converter.AtomizeXdmSequence(Functions.InvokeXdmFunctionItem(keyFunction, [members[index]], context, StaticContext));
            entries[index] = new XPathArraySortEntry(members[index], key);
            index = index + 1;
        }

        index = 1;
        while (index < entries.Length)
        {
            XPathArraySortEntry entry = entries[index];
            int insertion = index;
            while (insertion > 0 && sequenceFunctions.CompareXPathSortKeys(entry.Key, entries[insertion - 1].Key, collation, context) < 0)
            {
                entries[insertion] = entries[insertion - 1];
                insertion = insertion - 1;
            }

            entries[insertion] = entry;
            index = index + 1;
        }

        XdmSequence[] sorted = new XdmSequence[entries.Length];
        index = 0;
        while (index < entries.Length)
        {
            sorted[index] = entries[index].Member;
            index = index + 1;
        }

        return Sequences.CreateXdmArraySequence(Arrays.CreateXdmArray(sorted));
    }

    internal void FlattenXPathArraySequence(XdmSequence sequence, XdmSequenceBuilder result)
    {
        XdmSequenceCursor cursor = new XdmSequenceCursor(sequence);
        while (cursor.MoveNextXdmSequenceItem())
        {
            XdmSequenceItem item = cursor.ReadCurrentXdmSequenceItem();
            if (item.Kind == XdmItemKind.Array && item.Array != null)
            {
                XdmSequence[] members = ReadXPathArrayMembers(item.Array);
                int index = 0;
                while (index < members.Length)
                {
                    FlattenXPathArraySequence(members[index], result);
                    index = index + 1;
                }
            }
            else
            {
                Arrays.AddXdmArraySequenceItem(result, item);
            }
        }
    }

    internal XdmArrayValue ReadXPathRequiredArray(XdmSequence supplied)
    {
        Sequences.ValidateXdmSequenceStorage(supplied);
        if (supplied.Count != 1)
        {
            throw CreateXPathArrayTypeError("An array function argument must contain exactly one array.");
        }

        XdmSequenceItem item = Sequences.ReadXdmSequenceItem(supplied, 0);
        if (item.Kind != XdmItemKind.Array || item.Array == null)
        {
            throw CreateXPathArrayTypeError("An array function argument must be an array.");
        }

        Arrays.ValidateXdmArrayStorage(item.Array);
        return item.Array;
    }

    internal XdmSequence[] ReadXPathArrayMembers(XdmArrayValue array)
    {
        Arrays.ValidateXdmArrayStorage(array);
        return array.Members == null ? [] : array.Members;
    }

    internal BigInteger ReadXPathArrayInteger(XdmSequence supplied)
    {
        XdmSequenceType expected = CreateXPathArrayAtomicType(XdmAtomicType.Integer, XdmOccurrence.ExactlyOne);
        XdmSequence converted = Converter.ApplyXdmFunctionConversion(supplied, expected, StaticContext);
        XdmAtomicValue? value = Sequences.ReadXdmSequenceItem(converted, 0).AtomicValue;
        return value == null ? BigInteger.Zero : new XdmNumericDatatype().ReadXdmIntegerValue(value);
    }

    internal int ReadXPathArrayPosition(XdmSequence supplied, int count, bool allowAfterLast)
    {
        BigInteger value = ReadXPathArrayInteger(supplied);
        BigInteger maximum = new BigInteger(count + (allowAfterLast ? 1 : 0));
        if (value < BigInteger.One || value > maximum)
        {
            throw Arrays.CreateXdmArrayDynamicError("The array position is out of bounds.", XPathErrorCodes.FOAY0001);
        }

        return (int)value;
    }

    internal int[] ReadXPathArrayPositions(XdmSequence supplied, int count)
    {
        XdmSequenceType expected = CreateXPathArrayAtomicType(XdmAtomicType.Integer, XdmOccurrence.ZeroOrMore);
        XdmSequence converted = Converter.ApplyXdmFunctionConversion(supplied, expected, StaticContext);
        int[] result = new int[converted.Count];
        int index = 0;
        while (index < converted.Count)
        {
            XdmAtomicValue? atomic = Sequences.ReadXdmSequenceItem(converted, index).AtomicValue;
            BigInteger value = atomic == null ? BigInteger.Zero : new XdmNumericDatatype().ReadXdmIntegerValue(atomic);
            if (value < BigInteger.One || value > new BigInteger(count))
            {
                throw Arrays.CreateXdmArrayDynamicError("The array position is out of bounds.", XPathErrorCodes.FOAY0001);
            }

            result[index] = (int)value;
            index = index + 1;
        }

        return result;
    }

    internal XdmFunctionItem ReadXPathArrayCallable(XdmSequence supplied, int arity)
    {
        if (supplied.Count != 1)
        {
            throw CreateXPathArrayTypeError("An array callback must contain exactly one function item.");
        }

        XdmSequenceItem item = Sequences.ReadXdmSequenceItem(supplied, 0);
        XdmFunctionItem? function = null;
        if (item.Kind == XdmItemKind.Function)
        {
            function = item.Function;
        }
        else if (item.Kind == XdmItemKind.Map && item.Map != null)
        {
            function = Functions.CreateXdmMapFunctionItem(item.Map);
        }
        else if (item.Kind == XdmItemKind.Array && item.Array != null)
        {
            function = Functions.CreateXdmArrayFunctionItem(item.Array);
        }

        if (function == null)
        {
            throw CreateXPathArrayTypeError("An array callback must be a function item.");
        }

        Functions.RequireXdmFunctionArity(function, arity);
        return function;
    }

    internal XdmSequenceType CreateXPathArrayAtomicType(XdmAtomicType type, XdmOccurrence occurrence)
    {
        XdmSequenceType result = new XdmSequenceType();
        result.ItemKind = XdmItemKind.Atomic;
        result.AtomicType = type;
        result.Occurrence = occurrence;
        return result;
    }

    internal XdmSequence CreateXPathArrayIntegerResult(int value)
    {
        string lexical = value.ToString(CultureInfo.InvariantCulture);
        return Sequences.CreateXdmAtomicSequence(Factory.CreateXdmIntegerAtomicValue(lexical, XdmAtomicType.Integer));
    }

    internal XPathTypeException CreateXPathArrayTypeError(string message)
    {
        XmlDiagnostic diagnostic = new XmlDiagnostic();
        diagnostic.Code = XPathErrorCodes.XPTY0004;
        diagnostic.Category = XmlErrorCategory.XPathType;
        diagnostic.Severity = XmlDiagnosticSeverity.Fatal;
        diagnostic.Message = message;
        diagnostic.Range = Range;
        return new XPathTypeException(message, diagnostic, Range.Start);
    }
}
