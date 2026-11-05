using System;
using System.Text;
using fix;

namespace fix.Tests;

[TestFixture]
public class FixMessageOperationsTests
{
    [Test]
    public void SetFieldRoutesAddsAndMutatesTheFirstExistingOccurrence()
    {
        FixMessage message = new FixMessage { BeginString = "FIX.4.2", MsgType = "0", OriginalBytes = [1, 2, 3] };
        FixField firstSender = FieldAt(49, "OLD", 3);
        firstSender.SourceOffset = 20;
        firstSender.SourceLength = 7;
        message.HeaderFields = [firstSender, FieldAt(49, "DUPLICATE", 4)];
        FixMessageOperations operations = new FixMessageOperations();

        MessageEditResult sender = operations.SetField(message, Field(49, "NEW"));
        MessageEditResult begin = operations.SetField(message, Field(8, "FIX.4.4"));
        MessageEditResult type = operations.SetField(message, Field(35, "D"));
        MessageEditResult body = operations.SetField(message, Field(11, "ORDER"));
        MessageEditResult trailer = operations.SetField(message, Field(10, "000"));

        Assert.Multiple(() =>
        {
            Assert.That(sender.Success, Is.True, sender.ErrorCode);
            Assert.That(sender.Message, Is.SameAs(message));
            Assert.That(sender.AffectedTag, Is.EqualTo(49));
            Assert.That(message.HeaderFields, Has.Count.EqualTo(4));
            Assert.That(message.HeaderFields[0], Is.SameAs(firstSender));
            Assert.That(Text(message.HeaderFields[0]), Is.EqualTo("NEW"));
            Assert.That(message.HeaderFields[0].WireIndex, Is.EqualTo(3));
            Assert.That(message.HeaderFields[0].SourceOffset, Is.EqualTo(-1));
            Assert.That(message.HeaderFields[0].SourceLength, Is.Zero);
            Assert.That(Text(message.HeaderFields[1]), Is.EqualTo("DUPLICATE"));
            Assert.That(message.HeaderFields.ConvertAll(field => field.Tag), Does.Contain(8));
            Assert.That(message.HeaderFields.ConvertAll(field => field.Tag), Does.Contain(35));
            Assert.That(message.BodyFields.ConvertAll(field => field.Tag), Is.EqualTo(new[] { 11 }));
            Assert.That(message.TrailerFields.ConvertAll(field => field.Tag), Is.EqualTo(new[] { 10 }));
            Assert.That(message.BeginString, Is.EqualTo("FIX.4.4"));
            Assert.That(message.MsgType, Is.EqualTo("D"));
            Assert.That(message.OriginalBytes, Is.Empty);
            Assert.That(begin.Success && type.Success && body.Success && trailer.Success, Is.True);
        });
    }

    [Test]
    public void ExistingNonstandardFieldsRemainInTheirDecodedSections()
    {
        FixMessage message = new FixMessage();
        message.HeaderFields.Add(Field(5000, "HEADER"));
        message.TrailerFields.Add(Field(5001, "TRAILER"));
        FixMessageOperations operations = new FixMessageOperations();

        MessageEditResult header = operations.SetField(message, Field(5000, "NEW-HEADER"));
        MessageEditResult trailer = operations.SetField(message, Field(5001, "NEW-TRAILER"));

        Assert.Multiple(() =>
        {
            Assert.That(header.Success, Is.True);
            Assert.That(trailer.Success, Is.True);
            Assert.That(Text(message.HeaderFields.Single()), Is.EqualTo("NEW-HEADER"));
            Assert.That(Text(message.TrailerFields.Single()), Is.EqualTo("NEW-TRAILER"));
            Assert.That(message.BodyFields, Is.Empty);
        });
    }

    [Test]
    public void SetAndRemoveSynchronizeSharedStructuredFieldsAndCounts()
    {
        FixMessage message = new FixMessage { MsgType = "D", OriginalBytes = [9] };
        FixField count = FieldAt(453, "99", 0);
        FixField party = FieldAt(448, "OLD", 1);
        FixGroup group = new FixGroup { CountTag = 453, DelimiterTag = 448, DeclaredCount = 99, WireIndex = 0 };
        group.Entries.Add(new FixGroupEntry { EntryIndex = 8, Fields = [party] });
        message.BodyFields = [count, party, FieldAt(58, "A", 2), FieldAt(58, "B", 3)];
        message.BodyGroups.Add(group);
        FixMessageOperations operations = new FixMessageOperations();

        MessageEditResult set = operations.SetField(message, Field(448, "NEW"));
        MessageEditResult remove = operations.RemoveField(message, 58);

        Assert.Multiple(() =>
        {
            Assert.That(set.Success, Is.True, set.ErrorCode);
            Assert.That(remove.Success, Is.True, remove.ErrorCode);
            Assert.That(message.BodyFields[1], Is.SameAs(group.Entries[0].Fields[0]));
            Assert.That(Text(group.Entries[0].Fields[0]), Is.EqualTo("NEW"));
            Assert.That(group.DeclaredCount, Is.EqualTo(1));
            Assert.That(group.Entries[0].EntryIndex, Is.Zero);
            Assert.That(Text(count), Is.EqualTo("1"));
            Assert.That(message.BodyFields.Exists(field => field.Tag == 58), Is.False);
            Assert.That(message.OriginalBytes, Is.Empty);
        });
    }

    [Test]
    public void SetFieldFindsStructuredOnlyFieldsAtAnyGroupDepth()
    {
        FixField count = FieldAt(453, "99", -1);
        FixField party = FieldAt(448, "OLD", 1);
        FixField sub = FieldAt(523, "OLD-SUB", 2);
        FixGroup nested = new FixGroup { CountTag = 802, DelimiterTag = 523 };
        nested.Entries.Add(new FixGroupEntry { Fields = [sub] });
        FixGroup group = new FixGroup { CountTag = 453, DelimiterTag = 448 };
        group.Entries.Add(new FixGroupEntry { Fields = [party], Groups = [nested] });
        FixMessage message = new FixMessage { BodyFields = [count], BodyGroups = [group] };
        FixMessageOperations operations = new FixMessageOperations();

        MessageEditResult direct = operations.SetField(message, Field(448, "NEW"));
        MessageEditResult nestedResult = operations.SetField(message, Field(523, "NEW-SUB"));

        Assert.Multiple(() =>
        {
            Assert.That(direct.Success, Is.True);
            Assert.That(nestedResult.Success, Is.True);
            Assert.That(Text(party), Is.EqualTo("NEW"));
            Assert.That(Text(sub), Is.EqualTo("NEW-SUB"));
            Assert.That(Text(count), Is.EqualTo("1"));
        });
    }

    [Test]
    public void AddGroupCreatesSynchronizedFlatAndNestedStructuredViewsThatEncode()
    {
        FixMessage message = new FixMessage { BeginString = "FIX.4.4", MsgType = "D", OriginalBytes = [7] };
        message.BodyFields.Add(FieldAt(11, "ORDER", 0));
        FixGroup group = CreateNestedGroup();
        FixMessageOperations operations = new FixMessageOperations();

        MessageEditResult added = operations.AddGroup(message, group);
        FixDictionary dictionary = CreateDictionary();
        TagValueMessageEncoder encoder = new TagValueMessageEncoder();
        EncodeResult measured = encoder.EncodeMessage(message, dictionary, Span<byte>.Empty, new EncodeOptions());
        byte[] destination = new byte[measured.RequiredSize];
        EncodeResult encoded = encoder.EncodeMessage(message, dictionary, destination, new EncodeOptions());
        DecodeResult decoded = new TagValueMessageDecoder().DecodeMessage(destination, dictionary, new DecodeOptions());

        Assert.Multiple(() =>
        {
            Assert.That(added.Success, Is.True, added.ErrorCode);
            Assert.That(added.Message, Is.SameAs(message));
            Assert.That(message.BodyGroups, Has.Count.EqualTo(1));
            Assert.That(message.BodyGroups[0], Is.SameAs(group));
            Assert.That(group.DeclaredCount, Is.EqualTo(2));
            Assert.That(group.Entries[0].EntryIndex, Is.Zero);
            Assert.That(group.Entries[1].EntryIndex, Is.EqualTo(1));
            Assert.That(group.Entries[0].Groups[0].DeclaredCount, Is.EqualTo(1));
            Assert.That(message.BodyFields.ConvertAll(field => field.Tag), Is.EqualTo(new[] { 11, 453, 448, 802, 523, 452, 448, 452 }));
            Assert.That(Text(message.BodyFields[1]), Is.EqualTo("2"));
            Assert.That(Text(message.BodyFields[3]), Is.EqualTo("1"));
            Assert.That(message.BodyFields[2], Is.SameAs(group.Entries[0].Fields[0]));
            Assert.That(message.OriginalBytes, Is.Empty);
            Assert.That(encoded.Status, Is.EqualTo(EncodeStatus.Success), encoded.ErrorCode);
            Assert.That(decoded.Status, Is.EqualTo(DecodeStatus.Success), decoded.ErrorCode);
            Assert.That(decoded.Message!.BodyGroups[0].Entries, Has.Count.EqualTo(2));
            Assert.That(decoded.Message.BodyGroups[0].Entries[0].Groups[0].Entries, Has.Count.EqualTo(1));
        });
    }

    [Test]
    public void RemoveGroupRemovesNestedAndTopLevelFlatRepresentations()
    {
        FixMessage message = new FixMessage { BeginString = "FIX.4.4", MsgType = "D" };
        message.BodyFields.Add(FieldAt(11, "ORDER", 0));
        FixMessageOperations operations = new FixMessageOperations();
        operations.AddGroup(message, CreateNestedGroup());

        MessageEditResult nested = operations.RemoveGroup(message, 802);

        Assert.Multiple(() =>
        {
            Assert.That(nested.Success, Is.True, nested.ErrorCode);
            Assert.That(message.BodyGroups[0].Entries[0].Groups, Is.Empty);
            Assert.That(message.BodyFields.ConvertAll(field => field.Tag), Is.EqualTo(new[] { 11, 453, 448, 452, 448, 452 }));
            Assert.That(Text(message.BodyFields[1]), Is.EqualTo("2"));
        });

        MessageEditResult top = operations.RemoveGroup(message, 453);

        Assert.Multiple(() =>
        {
            Assert.That(top.Success, Is.True, top.ErrorCode);
            Assert.That(top.AffectedTag, Is.EqualTo(453));
            Assert.That(message.BodyGroups, Is.Empty);
            Assert.That(message.BodyFields.ConvertAll(field => field.Tag), Is.EqualTo(new[] { 11 }));
        });
    }

    [Test]
    public void RemoveFieldRemovesCountTaggedGroupsAndEnvelopeMetadata()
    {
        FixMessage message = new FixMessage { BeginString = "FIX.4.4", MsgType = "D" };
        message.HeaderFields = [Field(8, "FIX.4.4"), Field(35, "D")];
        FixMessageOperations operations = new FixMessageOperations();
        operations.AddGroup(message, CreateNestedGroup());

        MessageEditResult group = operations.RemoveField(message, 453);
        MessageEditResult begin = operations.RemoveField(message, 8);
        MessageEditResult type = operations.RemoveField(message, 35);

        Assert.Multiple(() =>
        {
            Assert.That(group.Success && begin.Success && type.Success, Is.True);
            Assert.That(message.BodyGroups, Is.Empty);
            Assert.That(message.BodyFields.Exists(field => field.Tag == 453 || field.Tag == 448 || field.Tag == 802 || field.Tag == 523 || field.Tag == 452), Is.False);
            Assert.That(message.HeaderFields, Is.Empty);
            Assert.That(message.BeginString, Is.Empty);
            Assert.That(message.MsgType, Is.Empty);
        });
    }

    [Test]
    public void CloneIsIndependentAndPreservesAliasesWithinTheCopy()
    {
        FixMessage original = new FixMessage { BeginString = "FIX.4.4", MsgType = "D", OriginalBytes = [1, 2, 3] };
        FixField count = FieldAt(453, "1", 0);
        FixField party = FieldAt(448, "P1", 1);
        FixGroup group = new FixGroup { CountTag = 453, DelimiterTag = 448, DeclaredCount = 1, WireIndex = 0 };
        group.Entries.Add(new FixGroupEntry { EntryIndex = 0, Fields = [party] });
        original.BodyFields = [count, party];
        original.BodyGroups.Add(group);
        FixMessageOperations operations = new FixMessageOperations();

        MessageEditResult result = operations.CloneMessage(original);
        FixMessage clone = result.Message!;
        clone.OriginalBytes[0] = 9;
        MessageEditResult changed = operations.SetField(clone, Field(448, "COPY"));

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True, result.ErrorCode);
            Assert.That(result.AffectedTag, Is.Zero);
            Assert.That(clone, Is.Not.SameAs(original));
            Assert.That(clone.BodyFields, Is.Not.SameAs(original.BodyFields));
            Assert.That(clone.BodyFields[1], Is.Not.SameAs(original.BodyFields[1]));
            Assert.That(clone.BodyGroups[0], Is.Not.SameAs(original.BodyGroups[0]));
            Assert.That(clone.BodyGroups[0].Entries[0].Fields[0], Is.SameAs(clone.BodyFields[1]));
            Assert.That(changed.Success, Is.True, changed.ErrorCode);
            Assert.That(Text(clone.BodyFields[1]), Is.EqualTo("COPY"));
            Assert.That(Text(original.BodyFields[1]), Is.EqualTo("P1"));
            Assert.That(original.OriginalBytes, Is.EqualTo(new byte[] { 1, 2, 3 }));
        });
    }

    [Test]
    public void InvalidTagsAndMalformedGroupsReturnErrorsWithoutEditingTheMessage()
    {
        FixMessage message = new FixMessage { OriginalBytes = [1] };
        FixMessageOperations operations = new FixMessageOperations();
        FixGroup missingDelimiter = new FixGroup { CountTag = 453, DelimiterTag = 448 };
        missingDelimiter.Entries.Add(new FixGroupEntry { Fields = [Field(452, "1")] });
        FixGroup cyclic = new FixGroup { CountTag = 453, DelimiterTag = 448 };
        FixGroupEntry cyclicEntry = new FixGroupEntry { Fields = [Field(448, "P1")] };
        cyclicEntry.Groups.Add(cyclic);
        cyclic.Entries.Add(cyclicEntry);
        FixGroup invalidDefinition = new FixGroup { CountTag = 453, DelimiterTag = 453 };

        MessageEditResult set = operations.SetField(message, Field(0, "X"));
        MessageEditResult missingValue = operations.SetField(message, new FixField { Tag = 1, Value = null! });
        MessageEditResult remove = operations.RemoveField(message, -1);
        MessageEditResult malformed = operations.AddGroup(message, missingDelimiter);
        MessageEditResult invalidGroup = operations.AddGroup(message, invalidDefinition);
        MessageEditResult depth = operations.AddGroup(message, cyclic);
        MessageEditResult absentField = operations.RemoveField(message, 999);
        MessageEditResult absentGroup = operations.RemoveGroup(message, 453);
        MessageEditResult invalidRemoveGroup = operations.RemoveGroup(message, 0);

        Assert.Multiple(() =>
        {
            Assert.That(set.ErrorCode, Is.EqualTo("invalid-tag"));
            Assert.That(missingValue.ErrorCode, Is.EqualTo("missing-field-value"));
            Assert.That(remove.ErrorCode, Is.EqualTo("invalid-tag"));
            Assert.That(malformed.ErrorCode, Is.EqualTo("missing-group-delimiter"));
            Assert.That(invalidGroup.ErrorCode, Is.EqualTo("invalid-group-definition"));
            Assert.That(depth.ErrorCode, Is.EqualTo("group-depth-limit-exceeded"));
            Assert.That(absentField.Success, Is.True);
            Assert.That(absentGroup.Success, Is.True);
            Assert.That(invalidRemoveGroup.ErrorCode, Is.EqualTo("invalid-tag"));
            Assert.That(message.HeaderFields, Is.Empty);
            Assert.That(message.BodyFields, Is.Empty);
            Assert.That(message.TrailerFields, Is.Empty);
            Assert.That(message.BodyGroups, Is.Empty);
            Assert.That(message.OriginalBytes, Is.EqualTo(new byte[] { 1 }));
        });
    }

    private static FixGroup CreateNestedGroup()
    {
        FixGroup parties = new FixGroup { CountTag = 453, DelimiterTag = 448, DeclaredCount = 99 };
        FixGroupEntry first = new FixGroupEntry
        {
            EntryIndex = 5,
            Fields = [FieldAt(448, "P1", 1), FieldAt(452, "1", 5)]
        };
        FixGroup nested = new FixGroup { CountTag = 802, DelimiterTag = 523, DeclaredCount = 10, WireIndex = 3 };
        nested.Entries.Add(new FixGroupEntry { EntryIndex = 4, Fields = [FieldAt(523, "S1", 4)] });
        first.Groups.Add(nested);
        parties.Entries.Add(first);
        parties.Entries.Add(new FixGroupEntry
        {
            EntryIndex = 7,
            Fields = [FieldAt(448, "P2", 8), FieldAt(452, "2", 9)]
        });
        return parties;
    }

    private static FixDictionary CreateDictionary()
    {
        FixDictionary dictionary = new FixDictionary();
        dictionary.Identity.BeginString = "FIX.4.4";
        dictionary.Fields =
        [
            Definition(8, FixFieldType.String),
            Definition(9, FixFieldType.Length),
            Definition(10, FixFieldType.String),
            Definition(11, FixFieldType.String),
            Definition(35, FixFieldType.String),
            Definition(448, FixFieldType.String),
            Definition(452, FixFieldType.Int),
            Definition(453, FixFieldType.NumInGroup),
            Definition(523, FixFieldType.String),
            Definition(802, FixFieldType.NumInGroup)
        ];
        dictionary.Trailer = [Member(FixMemberKind.Field, 10)];
        dictionary.Groups =
        [
            new FixGroupDefinition
            {
                Id = 1,
                CountTag = 453,
                DelimiterTag = 448,
                Members = [Member(FixMemberKind.Field, 448), Member(FixMemberKind.Group, 2), Member(FixMemberKind.Field, 452)]
            },
            new FixGroupDefinition
            {
                Id = 2,
                CountTag = 802,
                DelimiterTag = 523,
                Members = [Member(FixMemberKind.Field, 523)]
            }
        ];
        dictionary.Messages =
        [
            new FixMessageDefinition
            {
                Id = 100,
                MsgType = "D",
                Members = [Member(FixMemberKind.Field, 11), Member(FixMemberKind.Group, 1)]
            }
        ];
        return dictionary;
    }

    private static FixFieldDefinition Definition(int tag, FixFieldType type)
    {
        return new FixFieldDefinition { Tag = tag, FieldType = type };
    }

    private static FixMemberDefinition Member(FixMemberKind kind, int referenceId)
    {
        return new FixMemberDefinition { Kind = kind, ReferenceId = referenceId };
    }

    private static FixField Field(int tag, string value)
    {
        return new FixField { Tag = tag, Value = Encoding.Latin1.GetBytes(value) };
    }

    private static FixField FieldAt(int tag, string value, int wireIndex)
    {
        return new FixField { Tag = tag, Value = Encoding.Latin1.GetBytes(value), WireIndex = wireIndex };
    }

    private static string Text(FixField field)
    {
        return Encoding.Latin1.GetString(field.Value);
    }
}
