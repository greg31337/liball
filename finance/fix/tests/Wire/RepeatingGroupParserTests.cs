using System.Text;
using fix;

namespace fix.Tests;

[TestFixture]
public class RepeatingGroupParserTests
{
    [Test]
    public void ParserBuildsIndependentEntriesAndNestedGroupsWithoutRemovingFlatFields()
    {
        FixDictionary dictionary = CreateDictionary();
        FixMessage message = CreateBodyMessage(
            Field(453, "2", 0),
            Field(448, "P1", 1),
            Field(447, "D", 2),
            Field(802, "2", 3),
            Field(523, "S1", 4),
            Field(803, "1", 5),
            Field(523, "S2", 6),
            Field(803, "2", 7),
            Field(452, "1", 8),
            Field(448, "P2", 9),
            Field(452, "3", 10),
            Field(11, "AFTER", 11));

        GroupStructureResult result = GroupParser.BuildGroups(message, dictionary, new DecodeOptions());

        Assert.That(result.Success, Is.True, result.ErrorCode);
        Assert.That(message.BodyGroups, Has.Count.EqualTo(1));
        FixGroup parties = message.BodyGroups[0];
        Assert.Multiple(() =>
        {
            Assert.That(parties.CountTag, Is.EqualTo(453));
            Assert.That(parties.DelimiterTag, Is.EqualTo(448));
            Assert.That(parties.DeclaredCount, Is.EqualTo(2));
            Assert.That(parties.WireIndex, Is.Zero);
            Assert.That(parties.Entries, Has.Count.EqualTo(2));
            Assert.That(parties.Entries[0], Is.Not.SameAs(parties.Entries[1]));
            Assert.That(parties.Entries[0].EntryIndex, Is.Zero);
            Assert.That(parties.Entries[1].EntryIndex, Is.EqualTo(1));
            Assert.That(parties.Entries[0].Fields.ConvertAll(field => field.Tag), Is.EqualTo(new[] { 448, 447, 452 }));
            Assert.That(parties.Entries[1].Fields.ConvertAll(field => field.Tag), Is.EqualTo(new[] { 448, 452 }));
            Assert.That(parties.Entries[0].Groups, Has.Count.EqualTo(1));
            Assert.That(parties.Entries[0].Groups[0].CountTag, Is.EqualTo(802));
            Assert.That(parties.Entries[0].Groups[0].Entries, Has.Count.EqualTo(2));
            Assert.That(Text(parties.Entries[0].Groups[0].Entries[0].Fields[0]), Is.EqualTo("S1"));
            Assert.That(Text(parties.Entries[0].Groups[0].Entries[1].Fields[0]), Is.EqualTo("S2"));
            Assert.That(message.BodyFields, Has.Count.EqualTo(12));
            Assert.That(message.BodyFields[3].Tag, Is.EqualTo(802));
            Assert.That(message.BodyFields[11].Tag, Is.EqualTo(11));
            Assert.That(parties.Entries[0].Fields[0], Is.SameAs(message.BodyFields[1]));
        });

        parties.Entries[0].Fields[0].Value[0] = 88;
        Assert.That(Text(parties.Entries[1].Fields[0]), Is.EqualTo("P2"));
    }

    [Test]
    public void ZeroEntryGroupUsesDelimiterDiscoveredThroughAComponent()
    {
        FixDictionary dictionary = CreateDictionary();
        FixGroupDefinition parties = dictionary.Groups[0];
        parties.DelimiterTag = 0;
        FixComponentDefinition identity = new FixComponentDefinition
        {
            Id = 300,
            Members =
            [
                new FixMemberDefinition { Kind = FixMemberKind.Field, ReferenceId = 448 },
                new FixMemberDefinition { Kind = FixMemberKind.Field, ReferenceId = 447 }
            ]
        };
        dictionary.Components.Add(identity);
        parties.Members =
        [
            new FixMemberDefinition { Kind = FixMemberKind.Component, ReferenceId = 300 },
            new FixMemberDefinition { Kind = FixMemberKind.Group, ReferenceId = 2 },
            new FixMemberDefinition { Kind = FixMemberKind.Field, ReferenceId = 452 }
        ];
        FixMessage message = CreateBodyMessage(Field(453, "0", 0), Field(11, "AFTER", 1));

        GroupStructureResult result = GroupParser.BuildGroups(message, dictionary, new DecodeOptions());

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True, result.ErrorCode);
            Assert.That(message.BodyGroups, Has.Count.EqualTo(1));
            Assert.That(message.BodyGroups[0].DeclaredCount, Is.Zero);
            Assert.That(message.BodyGroups[0].DelimiterTag, Is.EqualTo(448));
            Assert.That(message.BodyGroups[0].Entries, Is.Empty);
        });
    }

    [Test]
    public void ParserStructuresGroupsInHeaderBodyAndTrailerSections()
    {
        FixDictionary dictionary = CreateDictionary();
        FixGroupDefinition hops = new FixGroupDefinition
        {
            Id = 3,
            CountTag = 627,
            DelimiterTag = 628,
            Members =
            [
                new FixMemberDefinition { Kind = FixMemberKind.Field, ReferenceId = 628 },
                new FixMemberDefinition { Kind = FixMemberKind.Field, ReferenceId = 629 }
            ]
        };
        FixGroupDefinition trailerItems = new FixGroupDefinition
        {
            Id = 4,
            CountTag = 900,
            DelimiterTag = 901,
            Members = [new FixMemberDefinition { Kind = FixMemberKind.Field, ReferenceId = 901 }]
        };
        dictionary.Groups.Add(hops);
        dictionary.Groups.Add(trailerItems);
        dictionary.Header.Add(new FixMemberDefinition { Kind = FixMemberKind.Group, ReferenceId = 3 });
        dictionary.Trailer.Add(new FixMemberDefinition { Kind = FixMemberKind.Group, ReferenceId = 4 });
        FixMessage message = new FixMessage { MsgType = "D" };
        message.HeaderFields = [Field(8, "FIX.4.4", 0), Field(627, "1", 1), Field(628, "H", 2), Field(629, "T", 3)];
        message.BodyFields = [Field(453, "0", 4)];
        message.TrailerFields = [Field(900, "1", 5), Field(901, "X", 6), Field(10, "000", 7)];

        GroupStructureResult result = GroupParser.BuildGroups(message, dictionary, new DecodeOptions());

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True, result.ErrorCode);
            Assert.That(message.HeaderGroups, Has.Count.EqualTo(1));
            Assert.That(message.HeaderGroups[0].Entries[0].Fields.ConvertAll(field => field.Tag), Is.EqualTo(new[] { 628, 629 }));
            Assert.That(message.BodyGroups, Has.Count.EqualTo(1));
            Assert.That(message.TrailerGroups, Has.Count.EqualTo(1));
            Assert.That(message.TrailerGroups[0].Entries[0].Fields[0].Tag, Is.EqualTo(901));
        });
    }

    [Test]
    public void InvalidCountsAndConfiguredLimitsReturnExplicitDiagnostics()
    {
        FixDictionary dictionary = CreateDictionary();
        GroupStructureResult malformed = ParseBody(dictionary, new DecodeOptions(), Field(453, "X", 0));
        GroupStructureResult negative = ParseBody(dictionary, new DecodeOptions(), Field(453, "-1", 0));
        GroupStructureResult entryLimit = ParseBody(dictionary, new DecodeOptions { MaxGroupEntries = 1 }, Field(453, "2", 0));
        GroupStructureResult depthLimit = ParseBody(
            dictionary,
            new DecodeOptions { MaxGroupDepth = 1 },
            Field(453, "1", 0),
            Field(448, "P1", 1),
            Field(802, "1", 2),
            Field(523, "S1", 3));
        GroupStructureResult invalidLimit = ParseBody(dictionary, new DecodeOptions { MaxGroupDepth = -1 }, Field(453, "0", 0));

        Assert.Multiple(() =>
        {
            Assert.That(malformed.ErrorCode, Is.EqualTo("invalid-group-count"));
            Assert.That(malformed.ErrorWireIndex, Is.Zero);
            Assert.That(malformed.ErrorOffset, Is.EqualTo(0));
            Assert.That(negative.ErrorCode, Is.EqualTo("invalid-group-count"));
            Assert.That(entryLimit.Status, Is.EqualTo(DecodeStatus.LimitExceeded));
            Assert.That(entryLimit.ErrorCode, Is.EqualTo("group-entry-limit-exceeded"));
            Assert.That(depthLimit.Status, Is.EqualTo(DecodeStatus.LimitExceeded));
            Assert.That(depthLimit.ErrorCode, Is.EqualTo("group-depth-limit-exceeded"));
            Assert.That(depthLimit.ErrorWireIndex, Is.EqualTo(2));
            Assert.That(invalidLimit.ErrorCode, Is.EqualTo("invalid-group-limit"));
        });
    }

    [Test]
    public void MissingDelimiterPrematureTerminationAndCountMismatchAreDistinct()
    {
        FixDictionary dictionary = CreateDictionary();
        GroupStructureResult delimiter = ParseBody(dictionary, new DecodeOptions(), Field(453, "1", 0), Field(447, "D", 1));
        GroupStructureResult premature = ParseBody(
            dictionary,
            new DecodeOptions(),
            Field(453, "2", 0),
            Field(448, "P1", 1),
            Field(452, "1", 2),
            Field(11, "AFTER", 3));
        GroupStructureResult extra = ParseBody(
            dictionary,
            new DecodeOptions(),
            Field(453, "1", 0),
            Field(448, "P1", 1),
            Field(448, "P2", 2));
        GroupStructureResult zeroWithEntry = ParseBody(dictionary, new DecodeOptions(), Field(453, "0", 0), Field(448, "P1", 1));

        Assert.Multiple(() =>
        {
            Assert.That(delimiter.ErrorCode, Is.EqualTo("missing-group-delimiter"));
            Assert.That(delimiter.ErrorWireIndex, Is.EqualTo(1));
            Assert.That(premature.ErrorCode, Is.EqualTo("premature-group-termination"));
            Assert.That(premature.ErrorWireIndex, Is.EqualTo(3));
            Assert.That(extra.ErrorCode, Is.EqualTo("group-count-mismatch"));
            Assert.That(extra.ErrorWireIndex, Is.EqualTo(2));
            Assert.That(zeroWithEntry.ErrorCode, Is.EqualTo("group-count-mismatch"));
        });
    }

    [Test]
    public void MemberOrderingDuplicatesAndNestedDelimiterFailuresAreDiagnosed()
    {
        FixDictionary dictionary = CreateDictionary();
        GroupStructureResult order = ParseBody(
            dictionary,
            new DecodeOptions(),
            Field(453, "1", 0),
            Field(448, "P1", 1),
            Field(452, "1", 2),
            Field(447, "D", 3));
        GroupStructureResult duplicate = ParseBody(
            dictionary,
            new DecodeOptions(),
            Field(453, "1", 0),
            Field(448, "P1", 1),
            Field(447, "D", 2),
            Field(447, "C", 3));
        GroupStructureResult nestedDelimiter = ParseBody(
            dictionary,
            new DecodeOptions(),
            Field(453, "1", 0),
            Field(448, "P1", 1),
            Field(802, "1", 2),
            Field(803, "1", 3));

        Assert.Multiple(() =>
        {
            Assert.That(order.ErrorCode, Is.EqualTo("group-member-out-of-order"));
            Assert.That(order.ErrorWireIndex, Is.EqualTo(3));
            Assert.That(duplicate.ErrorCode, Is.EqualTo("duplicate-group-member"));
            Assert.That(duplicate.ErrorWireIndex, Is.EqualTo(3));
            Assert.That(nestedDelimiter.ErrorCode, Is.EqualTo("missing-group-delimiter"));
            Assert.That(nestedDelimiter.ErrorWireIndex, Is.EqualTo(3));
        });
    }

    private static GroupStructureResult ParseBody(FixDictionary dictionary, DecodeOptions options, params FixField[] fields)
    {
        FixMessage message = CreateBodyMessage(fields);
        return GroupParser.BuildGroups(message, dictionary, options);
    }

    private static FixMessage CreateBodyMessage(params FixField[] fields)
    {
        FixMessage message = new FixMessage { MsgType = "D" };
        message.BodyFields.AddRange(fields);
        return message;
    }

    private static FixField Field(int tag, string value, int wireIndex)
    {
        return new FixField
        {
            Tag = tag,
            Value = Encoding.ASCII.GetBytes(value),
            WireIndex = wireIndex,
            SourceOffset = wireIndex * 10,
            SourceLength = value.Length + 4
        };
    }

    private static string Text(FixField field)
    {
        return Encoding.ASCII.GetString(field.Value);
    }

    private static FixDictionary CreateDictionary()
    {
        FixGroupDefinition nested = new FixGroupDefinition
        {
            Id = 2,
            Name = "PtysSubGrp",
            CountTag = 802,
            DelimiterTag = 523,
            Members =
            [
                new FixMemberDefinition { Kind = FixMemberKind.Field, ReferenceId = 523, Position = 0 },
                new FixMemberDefinition { Kind = FixMemberKind.Field, ReferenceId = 803, Position = 1 }
            ]
        };
        FixGroupDefinition parties = new FixGroupDefinition
        {
            Id = 1,
            Name = "Parties",
            CountTag = 453,
            DelimiterTag = 448,
            Members =
            [
                new FixMemberDefinition { Kind = FixMemberKind.Field, ReferenceId = 448, Position = 0 },
                new FixMemberDefinition { Kind = FixMemberKind.Field, ReferenceId = 447, Position = 1 },
                new FixMemberDefinition { Kind = FixMemberKind.Group, ReferenceId = 2, Position = 2 },
                new FixMemberDefinition { Kind = FixMemberKind.Field, ReferenceId = 452, Position = 3 }
            ]
        };
        FixMessageDefinition message = new FixMessageDefinition
        {
            Id = 10,
            MsgType = "D",
            Members =
            [
                new FixMemberDefinition { Kind = FixMemberKind.Group, ReferenceId = 1, Position = 0 },
                new FixMemberDefinition { Kind = FixMemberKind.Field, ReferenceId = 11, Position = 1 }
            ]
        };
        return new FixDictionary
        {
            Groups = [parties, nested],
            Messages = [message]
        };
    }
}
