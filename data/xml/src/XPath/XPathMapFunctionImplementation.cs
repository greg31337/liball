using System;
using System.Collections.Generic;
using System.Globalization;

namespace xml;

internal class XPathMapFunctionImplementation : IXPathFunction
{
    internal string LocalName;
    internal SourceRange Range;
    internal XPathStaticContext StaticContext;
    internal XdmSequenceOperations Sequences;
    internal XdmMapOperations Maps;
    internal XdmArrayOperations Arrays;
    internal XdmFunctionOperations Functions;
    internal XdmTypeConverter Converter;
    internal XdmFactory Factory;

    internal XPathMapFunctionImplementation(string localName, SourceRange range, XPathStaticContext staticContext)
    {
        LocalName = localName;
        Range = range;
        StaticContext = staticContext;
        Sequences = new XdmSequenceOperations();
        Maps = new XdmMapOperations();
        Arrays = new XdmArrayOperations();
        Functions = new XdmFunctionOperations();
        Converter = new XdmTypeConverter();
        Factory = new XdmFactory();
    }

    public XdmSequence InvokeXPathFunction(XPathFunctionBinding binding, XdmSequence[] arguments, XPathDynamicContext context)
    {
        if (LocalName == "merge")
        {
            return EvaluateXPathMapMerge(arguments);
        }

        if (LocalName == "find")
        {
            return EvaluateXPathMapFind(arguments);
        }

        if (LocalName == "entry")
        {
            XdmAtomicValue key = ReadXPathMapAtomic(arguments[0]);
            XdmMapEntry entry = Maps.CreateXdmMapEntry(key, arguments[1]);
            return Sequences.CreateXdmMapSequence(Maps.CreateXdmMap([entry]));
        }

        XdmMapValue map = ReadXPathRequiredMap(arguments[0]);
        if (LocalName == "size")
        {
            return CreateXPathMapIntegerResult(map.Count);
        }

        if (LocalName == "keys")
        {
            return Maps.GetXdmMapKeys(map);
        }

        if (LocalName == "contains")
        {
            return CreateXPathMapBooleanResult(Maps.ContainsXdmMapKey(map, ReadXPathMapAtomic(arguments[1])));
        }

        if (LocalName == "get")
        {
            return Maps.GetXdmMapValue(map, ReadXPathMapAtomic(arguments[1]));
        }

        if (LocalName == "put")
        {
            XdmMapValue result = Maps.PutXdmMapEntry(map, ReadXPathMapAtomic(arguments[1]), arguments[2]);
            return Sequences.CreateXdmMapSequence(result);
        }

        if (LocalName == "remove")
        {
            XdmAtomicValue[] keys = ReadXPathMapAtomicSequence(arguments[1]);
            return Sequences.CreateXdmMapSequence(Maps.RemoveXdmMapKeys(map, keys));
        }

        if (LocalName == "for-each")
        {
            return EvaluateXPathMapForEach(map, arguments[1], context);
        }

        throw new InvalidOperationException("The XPath map function is not implemented.");
    }

    internal XdmSequence EvaluateXPathMapMerge(XdmSequence[] arguments)
    {
        XdmMapValue options = arguments.Length == 2 ? ReadXPathRequiredMap(arguments[1]) : Maps.CreateEmptyXdmMap();
        string policy = ReadXPathMapDuplicatePolicy(options);
        List<XdmMapEntry> resultEntries = new List<XdmMapEntry>();
        Dictionary<string, List<int>> resultIndexes = new Dictionary<string, List<int>>(StringComparer.Ordinal);
        XdmSequenceCursor cursor = new XdmSequenceCursor(arguments[0]);
        while (cursor.MoveNextXdmSequenceItem())
        {
            XdmSequenceItem item = cursor.ReadCurrentXdmSequenceItem();
            if (item.Kind != XdmItemKind.Map || item.Map == null)
            {
                throw CreateXPathMapTypeError("map:merge requires a sequence containing only maps.");
            }

            XdmMapValue input = item.Map;
            Maps.ValidateXdmMapStorage(input);
            XdmMapEntry[] entries = input.Entries == null ? [] : input.Entries;
            int index = 0;
            while (index < entries.Length)
            {
                XdmAtomicValue key = ReadXPathMapEntryKey(entries[index]);
                XdmSequence value = ReadXPathMapEntryValue(entries[index]);
                string identity = Maps.CreateXdmMapKeyIdentity(key);
                List<int>? candidates = resultIndexes.ContainsKey(identity) ? resultIndexes[identity] : null;
                int existing = -1;
                int resultIndex = 0;
                while (candidates != null && resultIndex < candidates.Count && existing < 0)
                {
                    int candidateIndex = candidates[resultIndex];
                    XdmAtomicValue existingKey = ReadXPathMapEntryKey(resultEntries[candidateIndex]);
                    if (Maps.SameXdmMapKey(existingKey, key))
                    {
                        existing = candidateIndex;
                    }

                    resultIndex = resultIndex + 1;
                }

                if (existing < 0)
                {
                    resultEntries.Add(Maps.CreateXdmMapEntry(key, value));
                    if (candidates == null)
                    {
                        candidates = new List<int>();
                        resultIndexes[identity] = candidates;
                    }

                    candidates.Add(resultEntries.Count - 1);
                }
                else if (policy == "use-last")
                {
                    resultEntries[existing] = Maps.CreateXdmMapEntry(key, value);
                }
                else if (policy == "combine")
                {
                    XdmSequence combined = Sequences.ConcatenateXdmSequences([ReadXPathMapEntryValue(resultEntries[existing]), value]);
                    resultEntries[existing] = Maps.CreateXdmMapEntry(key, combined);
                }
                else if (policy == "reject")
                {
                    throw CreateXPathMapDynamicError("map:merge encountered a duplicate key.", XPathErrorCodes.FOJS0003);
                }

                index = index + 1;
            }
        }

        return Sequences.CreateXdmMapSequence(Maps.CreateXdmMap(resultEntries.ToArray()));
    }

    internal string ReadXPathMapDuplicatePolicy(XdmMapValue options)
    {
        XdmAtomicValue key = Factory.CreateXdmStringAtomicValue("duplicates", XdmAtomicType.String);
        if (!Maps.ContainsXdmMapKey(options, key))
        {
            return "use-first";
        }

        XdmSequence supplied = Maps.GetXdmMapValue(options, key);
        XdmSequenceType expected = new XdmSequenceType();
        expected.ItemKind = XdmItemKind.Atomic;
        expected.AtomicType = XdmAtomicType.String;
        expected.Occurrence = XdmOccurrence.ExactlyOne;
        XdmSequence converted = Converter.ApplyXdmFunctionConversion(supplied, expected, StaticContext);
        XdmAtomicValue? value = Sequences.ReadXdmSequenceItem(converted, 0).AtomicValue;
        string policy = value == null ? "" : Converter.ReadXdmAtomicStringValue(value);
        if (policy != "reject" && policy != "use-first" && policy != "use-last" && policy != "use-any" && policy != "combine")
        {
            throw CreateXPathMapDynamicError("The map duplicate policy is not permitted.", XPathErrorCodes.FOJS0005);
        }

        return policy;
    }

    internal XdmSequence EvaluateXPathMapFind(XdmSequence[] arguments)
    {
        XdmAtomicValue key = ReadXPathMapAtomic(arguments[1]);
        List<XdmSequence> members = new List<XdmSequence>();
        FindXPathMapValues(arguments[0], key, members);
        return Sequences.CreateXdmArraySequence(Arrays.CreateXdmArray(members.ToArray()));
    }

    internal void FindXPathMapValues(XdmSequence sequence, XdmAtomicValue key, List<XdmSequence> members)
    {
        XdmSequenceCursor cursor = new XdmSequenceCursor(sequence);
        while (cursor.MoveNextXdmSequenceItem())
        {
            XdmSequenceItem item = cursor.ReadCurrentXdmSequenceItem();
            if (item.Kind == XdmItemKind.Array && item.Array != null)
            {
                Arrays.ValidateXdmArrayStorage(item.Array);
                XdmSequence[] arrayMembers = item.Array.Members == null ? [] : item.Array.Members;
                int memberIndex = 0;
                while (memberIndex < arrayMembers.Length)
                {
                    FindXPathMapValues(arrayMembers[memberIndex], key, members);
                    memberIndex = memberIndex + 1;
                }
            }
            else if (item.Kind == XdmItemKind.Map && item.Map != null)
            {
                Maps.ValidateXdmMapStorage(item.Map);
                XdmMapEntry[] entries = item.Map.Entries == null ? [] : item.Map.Entries;
                int entryIndex = 0;
                while (entryIndex < entries.Length)
                {
                    XdmAtomicValue entryKey = ReadXPathMapEntryKey(entries[entryIndex]);
                    XdmSequence value = ReadXPathMapEntryValue(entries[entryIndex]);
                    if (Maps.SameXdmMapKey(entryKey, key))
                    {
                        members.Add(value);
                    }

                    FindXPathMapValues(value, key, members);
                    entryIndex = entryIndex + 1;
                }
            }
        }
    }

    internal XdmSequence EvaluateXPathMapForEach(XdmMapValue map, XdmSequence actionSequence, XPathDynamicContext context)
    {
        XdmFunctionItem action = ReadXPathMapCallable(actionSequence);
        Functions.RequireXdmFunctionArity(action, 2);
        XdmSequenceBuilder result = new XdmSequenceBuilder();
        XdmMapEntry[] entries = map.Entries == null ? [] : map.Entries;
        int index = 0;
        while (index < entries.Length)
        {
            XdmAtomicValue key = ReadXPathMapEntryKey(entries[index]);
            XdmSequence value = ReadXPathMapEntryValue(entries[index]);
            XdmSequence keySequence = Sequences.CreateXdmAtomicSequence(key);
            XdmSequence invocation = Functions.InvokeXdmFunctionItem(action, [keySequence, value], context, StaticContext);
            result.AppendXdmSequence(invocation);
            index = index + 1;
        }

        return result.BuildImmutableXdmSequence();
    }

    internal XdmAtomicValue ReadXPathMapEntryKey(XdmMapEntry entry)
    {
        XdmAtomicValue? key = entry.Key;
        if (key == null)
        {
            throw new InvalidOperationException("A map entry has no key.");
        }

        return key;
    }

    internal XdmSequence ReadXPathMapEntryValue(XdmMapEntry entry)
    {
        XdmSequence? value = entry.Value;
        if (value == null)
        {
            throw new InvalidOperationException("A map entry has no value.");
        }

        return value;
    }

    internal XdmMapValue ReadXPathRequiredMap(XdmSequence supplied)
    {
        Sequences.ValidateXdmSequenceStorage(supplied);
        if (supplied.Count != 1)
        {
            throw CreateXPathMapTypeError("A map function argument must contain exactly one map.");
        }

        XdmSequenceItem item = Sequences.ReadXdmSequenceItem(supplied, 0);
        if (item.Kind != XdmItemKind.Map || item.Map == null)
        {
            throw CreateXPathMapTypeError("A map function argument must be a map.");
        }

        Maps.ValidateXdmMapStorage(item.Map);
        return item.Map;
    }

    internal XdmAtomicValue ReadXPathMapAtomic(XdmSequence supplied)
    {
        XdmSequence atomized = Converter.AtomizeXdmSequence(supplied);
        if (atomized.Count != 1)
        {
            throw CreateXPathMapTypeError("A map key argument must contain exactly one atomic value.");
        }

        XdmAtomicValue? value = Sequences.ReadXdmSequenceItem(atomized, 0).AtomicValue;
        return value == null ? throw CreateXPathMapTypeError("A map key must be atomic.") : value;
    }

    internal XdmAtomicValue[] ReadXPathMapAtomicSequence(XdmSequence supplied)
    {
        XdmSequence atomized = Converter.AtomizeXdmSequence(supplied);
        XdmAtomicValue[] result = new XdmAtomicValue[atomized.Count];
        int index = 0;
        while (index < atomized.Count)
        {
            XdmAtomicValue? value = Sequences.ReadXdmSequenceItem(atomized, index).AtomicValue;
            result[index] = value == null ? throw CreateXPathMapTypeError("A map key must be atomic.") : value;
            index = index + 1;
        }

        return result;
    }

    internal XdmFunctionItem ReadXPathMapCallable(XdmSequence supplied)
    {
        Sequences.ValidateXdmSequenceStorage(supplied);
        if (supplied.Count != 1)
        {
            throw CreateXPathMapTypeError("A map callback must contain exactly one function item.");
        }

        XdmSequenceItem item = Sequences.ReadXdmSequenceItem(supplied, 0);
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

        throw CreateXPathMapTypeError("A map callback must be a function item.");
    }

    internal XdmSequence CreateXPathMapIntegerResult(int value)
    {
        string lexical = value.ToString(CultureInfo.InvariantCulture);
        return Sequences.CreateXdmAtomicSequence(Factory.CreateXdmIntegerAtomicValue(lexical, XdmAtomicType.Integer));
    }

    internal XdmSequence CreateXPathMapBooleanResult(bool value)
    {
        return Sequences.CreateXdmAtomicSequence(Factory.CreateXdmBooleanAtomicValue(value ? "true" : "false"));
    }

    internal XPathTypeException CreateXPathMapTypeError(string message)
    {
        XmlDiagnostic diagnostic = new XmlDiagnostic();
        diagnostic.Code = XPathErrorCodes.XPTY0004;
        diagnostic.Category = XmlErrorCategory.XPathType;
        diagnostic.Severity = XmlDiagnosticSeverity.Fatal;
        diagnostic.Message = message;
        diagnostic.Range = Range;
        return new XPathTypeException(message, diagnostic, Range.Start);
    }

    internal XPathDynamicException CreateXPathMapDynamicError(string message, string code)
    {
        XmlDiagnostic diagnostic = new XmlDiagnostic();
        diagnostic.Code = code;
        diagnostic.Category = XmlErrorCategory.XPathDynamic;
        diagnostic.Severity = XmlDiagnosticSeverity.Fatal;
        diagnostic.Message = message;
        diagnostic.Range = Range;
        return new XPathDynamicException(message, diagnostic, Range.Start);
    }
}
