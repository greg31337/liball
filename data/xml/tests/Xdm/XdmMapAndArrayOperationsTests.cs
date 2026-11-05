using xml;

namespace xml.Tests;

[TestFixture]
public class XdmMapAndArrayOperationsTests
{
    [Test]
    public void MapConstructionCreatesDetachedImmutableEntryKeyAndSequenceStorage()
    {
        XdmMapOperations maps = new XdmMapOperations();
        XdmAtomicValue sourceKey = Atomic("name", XdmAtomicType.String);
        XdmSequence sourceValue = Sequence(Atomic("one", XdmAtomicType.String));
        XdmMapEntry sourceEntry = new XdmMapEntry { Key = sourceKey, Value = sourceValue };
        XdmMapEntry[] sourceEntries = [sourceEntry];

        XdmMapValue map = maps.CreateXdmMap(sourceEntries);

        sourceKey.StringValue = "changed";
        sourceValue.ItemKinds![0] = XdmItemKind.Node;
        sourceEntry.Value = Sequence(Atomic("changed", XdmAtomicType.String));
        sourceEntries[0] = new XdmMapEntry();

        Assert.Multiple(() =>
        {
            Assert.That(map.Count, Is.EqualTo(1));
            Assert.That(map.IsImmutable, Is.True);
            Assert.That(map.Entries, Is.Not.SameAs(sourceEntries));
            Assert.That(map.Entries![0], Is.Not.SameAs(sourceEntry));
            Assert.That(map.Entries[0].Key, Is.Not.SameAs(sourceKey));
            Assert.That(map.Entries[0].Key!.StringValue, Is.EqualTo("name"));
            Assert.That(map.Entries[0].Key!.IsImmutable, Is.True);
            Assert.That(map.Entries[0].Value, Is.Not.SameAs(sourceValue));
            Assert.That(map.Entries[0].Value!.ItemKinds, Is.EqualTo(new[] { XdmItemKind.Atomic }));
            Assert.That(map.Entries[0].Value!.IsImmutable, Is.True);
        });
    }

    [Test]
    public void SameKeyUsesCodepointsAndExactTransitiveNumericEquality()
    {
        XdmMapOperations maps = new XdmMapOperations();
        XdmAtomicValue token = Atomic("alpha", XdmAtomicType.Token);
        XdmAtomicValue uri = new XdmAnyUriDatatype().ParseXdmAnyUri("alpha");
        XdmAtomicValue integer = Atomic("9007199254740993", XdmAtomicType.Integer);
        XdmAtomicValue roundedDouble = Atomic("9007199254740992", XdmAtomicType.Double);
        XdmAtomicValue exactDouble = Atomic("9007199254740992", XdmAtomicType.Double);
        XdmAtomicValue decimalValue = Atomic("9007199254740992", XdmAtomicType.Decimal);

        Assert.Multiple(() =>
        {
            Assert.That(maps.SameXdmMapKey(token, uri), Is.True);
            Assert.That(maps.SameXdmMapKey(token, Atomic("Alpha", XdmAtomicType.String)), Is.False);
            Assert.That(maps.SameXdmMapKey(integer, roundedDouble), Is.False);
            Assert.That(maps.SameXdmMapKey(exactDouble, decimalValue), Is.True);
            Assert.That(maps.SameXdmMapKey(Atomic("NaN", XdmAtomicType.Float), Atomic("NaN", XdmAtomicType.Double)), Is.True);
            Assert.That(maps.SameXdmMapKey(Atomic("INF", XdmAtomicType.Float), Atomic("INF", XdmAtomicType.Double)), Is.True);
            Assert.That(maps.SameXdmMapKey(Atomic("0", XdmAtomicType.Float), Atomic("-0", XdmAtomicType.Double)), Is.True);
            Assert.That(maps.SameXdmMapKey(Atomic("INF", XdmAtomicType.Float), Atomic("-INF", XdmAtomicType.Double)), Is.False);
        });
    }

    [Test]
    public void SameKeyHandlesTemporalDurationBinaryAndNamespaceSensitiveValues()
    {
        XdmMapOperations maps = new XdmMapOperations();
        XdmDateTimeDatatype dates = new XdmDateTimeDatatype();
        XdmQNameDatatype names = new XdmQNameDatatype();
        XdmAtomicValue eastern = dates.ParseXdmDateTime("2002-10-10T12:00:00-05:00", XdmAtomicType.DateTime);
        XdmAtomicValue utc = dates.ParseXdmDateTime("2002-10-10T17:00:00Z", XdmAtomicType.DateTimeStamp);
        XdmAtomicValue local = dates.ParseXdmDateTime("2002-10-10T17:00:00", XdmAtomicType.DateTime);
        XdmAtomicValue qname1 = names.CreateXdmQNameValue("urn:items", "a:item");
        XdmAtomicValue qname2 = names.CreateXdmQNameValue("urn:items", "b:item");

        Assert.Multiple(() =>
        {
            Assert.That(maps.SameXdmMapKey(eastern, utc), Is.True);
            Assert.That(maps.SameXdmMapKey(utc, local), Is.False);
            Assert.That(maps.SameXdmMapKey(dates.ParseXdmDateTime("2002-10-10Z", XdmAtomicType.Date), utc), Is.False);
            Assert.That(maps.SameXdmMapKey(Atomic("P1M", XdmAtomicType.Duration), Atomic("P1M", XdmAtomicType.YearMonthDuration)), Is.True);
            Assert.That(maps.SameXdmMapKey(Atomic("0A", XdmAtomicType.HexBinary), Atomic("Cg==", XdmAtomicType.Base64Binary)), Is.False);
            Assert.That(maps.SameXdmMapKey(qname1, qname2), Is.True);
        });
    }

    [Test]
    public void MapConstructionRejectsDuplicateSameKeysAndAllowsDifferentTimezoneKinds()
    {
        XdmMapOperations maps = new XdmMapOperations();
        XdmSequence value = Sequence(Atomic("value", XdmAtomicType.String));
        XdmMapEntry integer = maps.CreateXdmMapEntry(Atomic("1", XdmAtomicType.Integer), value);
        XdmMapEntry decimalValue = maps.CreateXdmMapEntry(Atomic("1.0", XdmAtomicType.Decimal), value);
        XPathDynamicException duplicate = Assert.Throws<XPathDynamicException>(() => maps.CreateXdmMap([integer, decimalValue]))!;
        XdmDateTimeDatatype dates = new XdmDateTimeDatatype();

        XdmMapValue timezoneMap = maps.CreateXdmMap(
        [
            maps.CreateXdmMapEntry(dates.ParseXdmDateTime("2020-01-01Z", XdmAtomicType.Date), value),
            maps.CreateXdmMapEntry(dates.ParseXdmDateTime("2020-01-01", XdmAtomicType.Date), value)
        ]);

        Assert.Multiple(() =>
        {
            Assert.That(duplicate.Diagnostic!.Code, Is.EqualTo("XQDY0137"));
            Assert.That(timezoneMap.Count, Is.EqualTo(2));
            Assert.Throws<ArgumentException>(() => maps.ValidateXdmMapStorage(new XdmMapValue { Count = 1, Entries = [] }));
            Assert.Throws<ArgumentException>(() => maps.ValidateXdmMapStorage(new XdmMapValue { Count = 1, Entries = [new XdmMapEntry()] }));
        });
    }

    [Test]
    public void MapLookupPutAndRemoveUseSameKeyWithoutMutatingTheInput()
    {
        XdmMapOperations maps = new XdmMapOperations();
        XdmMapValue original = maps.CreateXdmMap(
        [
            maps.CreateXdmMapEntry(Atomic("a", XdmAtomicType.String), Sequence(Atomic("old", XdmAtomicType.String))),
            maps.CreateXdmMapEntry(Atomic("2", XdmAtomicType.Integer), Sequence(Atomic("two", XdmAtomicType.String)))
        ]);

        XdmMapValue replaced = maps.PutXdmMapEntry(original, new XdmAnyUriDatatype().ParseXdmAnyUri("a"), Sequence(Atomic("new", XdmAtomicType.String), Atomic("value", XdmAtomicType.String)));
        XdmMapValue appended = maps.PutXdmMapEntry(replaced, Atomic("3", XdmAtomicType.Integer), Sequence());
        XdmMapValue removed = maps.RemoveXdmMapKeys(appended, [Atomic("2.0", XdmAtomicType.Decimal), Atomic("missing", XdmAtomicType.String)]);

        Assert.Multiple(() =>
        {
            Assert.That(maps.ContainsXdmMapKey(original, Atomic("a", XdmAtomicType.UntypedAtomic)), Is.True);
            Assert.That(maps.GetXdmMapValue(original, Atomic("a", XdmAtomicType.String)).AtomicValues![0].StringValue, Is.EqualTo("old"));
            Assert.That(maps.GetXdmMapValue(replaced, Atomic("a", XdmAtomicType.String)).Count, Is.EqualTo(2));
            Assert.That(maps.GetXdmMapKeys(replaced).AtomicValues![0].Type, Is.EqualTo(XdmAtomicType.AnyUri));
            Assert.That(appended.Count, Is.EqualTo(3));
            Assert.That(maps.GetXdmMapValue(appended, Atomic("3", XdmAtomicType.Integer)).Count, Is.Zero);
            Assert.That(removed.Count, Is.EqualTo(2));
            Assert.That(maps.ContainsXdmMapKey(removed, Atomic("2", XdmAtomicType.Integer)), Is.False);
            Assert.That(maps.GetXdmMapValue(removed, Atomic("absent", XdmAtomicType.String)).Count, Is.Zero);
            Assert.That(original.Count, Is.EqualTo(2));
            Assert.That(original.Entries![0].Key!.Type, Is.EqualTo(XdmAtomicType.String));
            Assert.That(replaced.IsImmutable, Is.True);
            Assert.That(removed.IsImmutable, Is.True);
        });
    }

    [Test]
    public void ArrayConstructionPreservesSequenceMembersAndDetachesTheirStorage()
    {
        XdmArrayOperations arrays = new XdmArrayOperations();
        XdmSequence empty = Sequence();
        XdmSequence multiple = Sequence(Atomic("a", XdmAtomicType.String), Atomic("b", XdmAtomicType.String));
        XdmSequence[] source = [empty, multiple];

        XdmArrayValue array = arrays.CreateXdmArray(source);

        multiple.ItemKinds![0] = XdmItemKind.Node;
        source[0] = multiple;

        Assert.Multiple(() =>
        {
            Assert.That(array.Count, Is.EqualTo(2));
            Assert.That(array.IsImmutable, Is.True);
            Assert.That(array.Members, Is.Not.SameAs(source));
            Assert.That(array.Members![0].Count, Is.Zero);
            Assert.That(array.Members[1], Is.Not.SameAs(multiple));
            Assert.That(array.Members[1].Count, Is.EqualTo(2));
            Assert.That(array.Members[1].ItemKinds, Is.EqualTo(new[] { XdmItemKind.Atomic, XdmItemKind.Atomic }));
            Assert.That(arrays.GetXdmArrayMember(array, 2).Count, Is.EqualTo(2));
            Assert.That(arrays.GetXdmArraySize(array), Is.EqualTo(2));
        });
    }

    [Test]
    public void ArrayUpdatesAreOneBasedPersistentAndDoNotFlattenMembers()
    {
        XdmArrayOperations arrays = new XdmArrayOperations();
        XdmArrayValue original = arrays.CreateXdmArray([Sequence(Atomic("a", XdmAtomicType.String)), Sequence(Atomic("b", XdmAtomicType.String))]);
        XdmSequence pair = Sequence(Atomic("x", XdmAtomicType.String), Atomic("y", XdmAtomicType.String));

        XdmArrayValue replaced = arrays.PutXdmArrayMember(original, 1, pair);
        XdmArrayValue inserted = arrays.InsertBeforeXdmArrayMember(replaced, 2, Sequence());
        XdmArrayValue appended = arrays.AppendXdmArrayMember(inserted, Sequence(Atomic("z", XdmAtomicType.String)));

        Assert.Multiple(() =>
        {
            Assert.That(original.Count, Is.EqualTo(2));
            Assert.That(arrays.GetXdmArrayMember(original, 1).AtomicValues![0].StringValue, Is.EqualTo("a"));
            Assert.That(arrays.GetXdmArrayMember(replaced, 1).Count, Is.EqualTo(2));
            Assert.That(arrays.GetXdmArrayMember(inserted, 2).Count, Is.Zero);
            Assert.That(appended.Count, Is.EqualTo(4));
            Assert.That(arrays.GetXdmArrayMember(appended, 4).AtomicValues![0].StringValue, Is.EqualTo("z"));
            Assert.That(replaced.IsImmutable, Is.True);
            Assert.That(inserted.IsImmutable, Is.True);
            Assert.That(appended.IsImmutable, Is.True);
        });
    }

    [Test]
    public void ArraySelectionRemovalReversalAndJoinReturnIndependentArrays()
    {
        XdmArrayOperations arrays = new XdmArrayOperations();
        XdmArrayValue source = arrays.CreateXdmArray(
        [
            Sequence(Atomic("a", XdmAtomicType.String)),
            Sequence(Atomic("b", XdmAtomicType.String)),
            Sequence(Atomic("c", XdmAtomicType.String)),
            Sequence(Atomic("d", XdmAtomicType.String))
        ]);

        XdmArrayValue selected = arrays.SubarrayXdmArray(source, 2, 2);
        XdmArrayValue removed = arrays.RemoveXdmArrayMembers(source, [2, 2, 4]);
        XdmArrayValue reversed = arrays.ReverseXdmArray(selected);
        XdmArrayValue joined = arrays.JoinXdmArrays([arrays.CreateEmptyXdmArray(), removed, reversed]);

        Assert.Multiple(() =>
        {
            Assert.That(MemberText(arrays.HeadXdmArray(source)), Is.EqualTo("a"));
            Assert.That(MemberTexts(arrays.TailXdmArray(source)), Is.EqualTo(new[] { "b", "c", "d" }));
            Assert.That(MemberTexts(selected), Is.EqualTo(new[] { "b", "c" }));
            Assert.That(MemberTexts(removed), Is.EqualTo(new[] { "a", "c" }));
            Assert.That(MemberTexts(reversed), Is.EqualTo(new[] { "c", "b" }));
            Assert.That(MemberTexts(joined), Is.EqualTo(new[] { "a", "c", "c", "b" }));
            Assert.That(source.Count, Is.EqualTo(4));
            Assert.That(joined.IsImmutable, Is.True);
        });
    }

    [Test]
    public void ArrayConstructionFromSequenceCreatesSingletonMembersForEveryItemKind()
    {
        XdmArrayOperations arrays = new XdmArrayOperations();
        XmlNode node = new XmlNode { Kind = XmlNodeKind.Element };
        XdmMapValue map = new XdmMapOperations().CreateEmptyXdmMap();
        XdmSequenceBuilder builder = new XdmSequenceBuilder();
        builder.AddXdmNode(node);
        builder.AddXdmAtomicValue(Atomic("value", XdmAtomicType.String));
        builder.AddXdmMap(map);

        XdmArrayValue result = arrays.CreateXdmArrayFromSequence(builder.BuildImmutableXdmSequence());

        Assert.Multiple(() =>
        {
            Assert.That(result.Count, Is.EqualTo(3));
            Assert.That(result.Members![0].ItemKinds, Is.EqualTo(new[] { XdmItemKind.Node }));
            Assert.That(result.Members[0].Nodes![0], Is.SameAs(node));
            Assert.That(result.Members[1].ItemKinds, Is.EqualTo(new[] { XdmItemKind.Atomic }));
            Assert.That(result.Members[2].ItemKinds, Is.EqualTo(new[] { XdmItemKind.Map }));
            Assert.That(result.Members[2].Maps![0], Is.SameAs(map));
        });
    }

    [Test]
    public void ArrayOperationsReportStandardBoundsAndLengthErrors()
    {
        XdmArrayOperations arrays = new XdmArrayOperations();
        XdmArrayValue array = arrays.CreateXdmArray([Sequence(Atomic("a", XdmAtomicType.String))]);

        XPathDynamicException get = Assert.Throws<XPathDynamicException>(() => arrays.GetXdmArrayMember(array, 0))!;
        XPathDynamicException put = Assert.Throws<XPathDynamicException>(() => arrays.PutXdmArrayMember(array, 2, Sequence()))!;
        XPathDynamicException insert = Assert.Throws<XPathDynamicException>(() => arrays.InsertBeforeXdmArrayMember(array, 3, Sequence()))!;
        XPathDynamicException start = Assert.Throws<XPathDynamicException>(() => arrays.SubarrayXdmArray(array, 3, 0))!;
        XPathDynamicException range = Assert.Throws<XPathDynamicException>(() => arrays.SubarrayXdmArray(array, 1, 2))!;
        XPathDynamicException length = Assert.Throws<XPathDynamicException>(() => arrays.SubarrayXdmArray(array, 1, -1))!;
        XPathDynamicException head = Assert.Throws<XPathDynamicException>(() => arrays.HeadXdmArray(arrays.CreateEmptyXdmArray()))!;

        Assert.Multiple(() =>
        {
            Assert.That(get.Diagnostic!.Code, Is.EqualTo(XPathErrorCodes.FOAY0001));
            Assert.That(put.Diagnostic!.Code, Is.EqualTo(XPathErrorCodes.FOAY0001));
            Assert.That(insert.Diagnostic!.Code, Is.EqualTo(XPathErrorCodes.FOAY0001));
            Assert.That(start.Diagnostic!.Code, Is.EqualTo(XPathErrorCodes.FOAY0001));
            Assert.That(range.Diagnostic!.Code, Is.EqualTo(XPathErrorCodes.FOAY0001));
            Assert.That(length.Diagnostic!.Code, Is.EqualTo(XPathErrorCodes.FOAY0002));
            Assert.That(head.Diagnostic!.Code, Is.EqualTo(XPathErrorCodes.FOAY0001));
            Assert.Throws<ArgumentException>(() => arrays.ValidateXdmArrayStorage(new XdmArrayValue { Count = 1, Members = [] }));
        });
    }

    private static XdmAtomicValue Atomic(string lexical, XdmAtomicType type)
    {
        if (type == XdmAtomicType.UntypedAtomic)
        {
            return new XdmTypeConverter().CreateXdmUntypedAtomicValue(lexical);
        }

        if (type == XdmAtomicType.Boolean)
        {
            return new XdmNumericDatatype().ParseXdmBoolean(lexical);
        }

        if (type == XdmAtomicType.Decimal)
        {
            return new XdmNumericDatatype().ParseXdmDecimal(lexical);
        }

        if (new XdmTypeSystem().IsXdmAtomicSubtypeOf(type, XdmAtomicType.Integer))
        {
            return new XdmNumericDatatype().ParseXdmInteger(lexical, type);
        }

        if (type == XdmAtomicType.Float || type == XdmAtomicType.Double)
        {
            return new XdmFloatingDatatype().ParseXdmFloating(lexical, type);
        }

        if (new XdmTypeSystem().IsXdmDurationDerivedAtomicType(type))
        {
            return new XdmDurationDatatype().ParseXdmDuration(lexical, type);
        }

        if (type == XdmAtomicType.HexBinary || type == XdmAtomicType.Base64Binary)
        {
            return new XdmBinaryDatatype().ParseXdmBinary(lexical, type);
        }

        return new XdmStringDatatype().ParseXdmStringAtomic(lexical, type);
    }

    private static XdmSequence Sequence(params XdmAtomicValue[] values)
    {
        XdmSequenceBuilder builder = new XdmSequenceBuilder();
        foreach (XdmAtomicValue value in values)
        {
            builder.AddXdmAtomicValue(value);
        }

        return builder.BuildImmutableXdmSequence();
    }

    private static string MemberText(XdmSequence member)
    {
        return member.AtomicValues![0].StringValue!;
    }

    private static string[] MemberTexts(XdmArrayValue array)
    {
        return array.Members!.Select(MemberText).ToArray();
    }
}
