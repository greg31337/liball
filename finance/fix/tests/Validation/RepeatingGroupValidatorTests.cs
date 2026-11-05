using System.Collections.Generic;
using System.Text;
using fix;
using NUnit.Framework;

namespace fix.Tests;

public class RepeatingGroupValidatorTests
{
    [Test]
    public void ValidNestedGroupsAndComponentMembersProduceNoIssues()
    {
        FixDictionary dictionary = Dictionary();
        FixMessage message = new FixMessage { MsgType = "D" };
        FixGroup parties = Group(453, 448, 2, 0);
        FixGroupEntry first = Entry(
            Field(448, "P1", 1),
            Field(447, "D", 2),
            Field(452, "1", 3));
        FixGroup subIds = Group(802, 523, 1, 4);
        subIds.Entries.Add(Entry(Field(523, "SUB", 5), Field(803, "1", 6)));
        first.Groups.Add(subIds);
        parties.Entries.Add(first);
        parties.Entries.Add(Entry(Field(448, "P2", 7), Field(452, "3", 8)));
        message.BodyGroups.Add(parties);
        message.BodyFields =
        [
            Field(453, "2", 0),
            first.Fields[0],
            first.Fields[1],
            first.Fields[2],
            Field(802, "1", 4),
            subIds.Entries[0].Fields[0],
            subIds.Entries[0].Fields[1],
            parties.Entries[1].Fields[0],
            parties.Entries[1].Fields[1]
        ];

        ValidationResult result = Validate(message, dictionary, new ValidationOptions(), new ValidationContext());

        Assert.Multiple(() =>
        {
            Assert.That(result.IsValid, Is.True, string.Join(",", result.Issues.ConvertAll(issue => issue.Code + ":" + issue.GroupPath)));
            Assert.That(result.Issues, Is.Empty);
        });
    }

    [Test]
    public void RawAndStructuredCountsMustAgreeWithTheNumberOfEntries()
    {
        FixDictionary dictionary = Dictionary();
        FixMessage mismatchMessage = new FixMessage { MsgType = "D" };
        FixGroup mismatch = Group(453, 448, 2, 0);
        mismatch.Entries.Add(Entry(Field(448, "P1", 1), Field(452, "1", 2)));
        mismatchMessage.BodyGroups.Add(mismatch);
        mismatchMessage.BodyFields = [Field(453, "3", 0)];
        FixMessage invalidMessage = new FixMessage { MsgType = "D" };
        FixGroup invalid = Group(453, 448, 1, 0);
        invalid.Entries.Add(Entry(Field(448, "P1", 1), Field(452, "1", 2)));
        invalidMessage.BodyGroups.Add(invalid);
        invalidMessage.BodyFields = [Field(453, "X", 0)];

        ValidationResult mismatchResult = Validate(mismatchMessage, dictionary, new ValidationOptions(), new ValidationContext());
        ValidationResult invalidResult = Validate(invalidMessage, dictionary, new ValidationOptions(), new ValidationContext());

        Assert.Multiple(() =>
        {
            Assert.That(mismatchResult.Issues.ConvertAll(issue => issue.Code), Is.EqualTo(new[] { "group-count-mismatch" }));
            Assert.That(mismatchResult.Issues[0].GroupPath, Is.EqualTo("453"));
            Assert.That(mismatchResult.Issues[0].Tag, Is.EqualTo(453));
            Assert.That(mismatchResult.Issues[0].SourceOffset, Is.Zero);
            Assert.That(mismatchResult.Issues[0].SessionRejectReason, Is.EqualTo(16));
            Assert.That(invalidResult.Issues.ConvertAll(issue => issue.Code), Is.EqualTo(new[] { "invalid-group-count" }));
            Assert.That(invalidResult.Issues[0].SessionRejectReason, Is.EqualTo(6));
        });
    }

    [Test]
    public void DelimiterAllowedOrderAndDuplicateRulesCollectIndependentIssues()
    {
        FixDictionary dictionary = Dictionary();
        FixMessage message = new FixMessage { MsgType = "D" };
        FixGroup parties = Group(453, 447, 1, 0);
        parties.Entries.Add(Entry(
            Field(452, "1", 1),
            Field(448, "P1", 2),
            Field(447, "D", 3),
            Field(999, "UNKNOWN", 4),
            Field(447, "C", 5)));
        message.BodyGroups.Add(parties);
        message.BodyFields = [Field(453, "1", 0)];

        ValidationResult strict = Validate(message, dictionary, new ValidationOptions(), new ValidationContext());
        ValidationResult relaxed = Validate(message, dictionary, new ValidationOptions { CheckFieldOrder = false, RejectDuplicateFields = false }, new ValidationContext());

        Assert.Multiple(() =>
        {
            Assert.That(strict.Issues.ConvertAll(issue => issue.Code), Is.EqualTo(new[]
            {
                "invalid-group-delimiter",
                "missing-group-delimiter",
                "group-member-out-of-order",
                "group-member-out-of-order",
                "group-member-not-allowed",
                "group-member-out-of-order",
                "duplicate-group-member"
            }));
            Assert.That(strict.Issues.TrueForAll(issue => issue.GroupPath == "453" || issue.GroupPath == "453[0]"), Is.True);
            Assert.That(strict.Issues.ConvertAll(issue => issue.SessionRejectReason), Is.EqualTo(new[] { 15, 1, 15, 15, 2, 15, 13 }));
            Assert.That(relaxed.Issues.ConvertAll(issue => issue.Code), Is.EqualTo(new[]
            {
                "invalid-group-delimiter",
                "missing-group-delimiter",
                "group-member-not-allowed"
            }));
        });
    }

    [Test]
    public void ActivatedComponentsEnforceRequiredMembersAndForbiddenMembersRemainInvalid()
    {
        FixDictionary dictionary = Dictionary();
        FixMessage message = new FixMessage { MsgType = "D" };
        FixGroup parties = Group(453, 448, 1, 0);
        parties.Entries.Add(Entry(
            Field(448, "P1", 1),
            Field(900, "ACTIVATES-COMPONENT", 2),
            Field(901, "FORBIDDEN", 3)));
        message.BodyGroups.Add(parties);
        message.BodyFields = [Field(453, "1", 0)];

        ValidationResult strict = Validate(message, dictionary, new ValidationOptions(), new ValidationContext());
        ValidationResult noRequired = Validate(message, dictionary, new ValidationOptions { CheckRequiredFields = false }, new ValidationContext());

        Assert.Multiple(() =>
        {
            Assert.That(strict.Issues.ConvertAll(issue => issue.Code + ":" + issue.Tag), Is.EqualTo(new[]
            {
                "required-group-member-missing:452",
                "forbidden-group-member-present:901"
            }));
            Assert.That(strict.Issues[0].GroupPath, Is.EqualTo("453[0]"));
            Assert.That(strict.Issues.ConvertAll(issue => issue.SessionRejectReason), Is.EqualTo(new[] { 1, 2 }));
            Assert.That(noRequired.Issues.ConvertAll(issue => issue.Code + ":" + issue.Tag), Is.EqualTo(new[]
            {
                "forbidden-group-member-present:901"
            }));
        });
    }

    [Test]
    public void NestedFailuresCarryPreciseRecursiveGroupPaths()
    {
        FixDictionary dictionary = Dictionary();
        FixMessage message = new FixMessage { MsgType = "D" };
        FixGroup parties = Group(453, 448, 1, 0);
        FixGroupEntry party = Entry(Field(448, "P1", 1), Field(452, "1", 2));
        FixGroup subIds = Group(802, 523, 2, 3);
        subIds.Entries.Add(Entry(Field(523, "SUB", 4)));
        party.Groups.Add(subIds);
        parties.Entries.Add(party);
        message.BodyGroups.Add(parties);
        message.BodyFields = [Field(453, "1", 0), Field(802, "2", 3)];

        ValidationResult result = Validate(message, dictionary, new ValidationOptions(), new ValidationContext());

        Assert.Multiple(() =>
        {
            Assert.That(result.Issues.ConvertAll(issue => issue.Code), Is.EqualTo(new[]
            {
                "group-count-mismatch",
                "required-group-member-missing"
            }));
            Assert.That(result.Issues[0].GroupPath, Is.EqualTo("453[0]/802"));
            Assert.That(result.Issues[0].HasSourceOffset, Is.True);
            Assert.That(result.Issues[1].GroupPath, Is.EqualTo("453[0]/802[0]"));
            Assert.That(result.Issues[1].Tag, Is.EqualTo(803));
            Assert.That(result.Issues.ConvertAll(issue => issue.SessionRejectReason), Is.EqualTo(new[] { 16, 1 }));
        });
    }

    [Test]
    public void MultipleEntriesRetainZeroBasedPathsForIndependentFailures()
    {
        FixDictionary dictionary = Dictionary();
        FixMessage message = new FixMessage { MsgType = "D" };
        FixGroup parties = Group(453, 448, 2, 0);
        parties.Entries.Add(Entry(Field(448, "P1", 1), Field(900, "ACTIVE", 2)));
        parties.Entries.Add(Entry(Field(447, "D", 3), Field(448, "P2", 4), Field(900, "ACTIVE", 5), Field(999, "UNKNOWN", 6)));
        message.BodyGroups.Add(parties);
        message.BodyFields = [Field(453, "2", 0)];

        ValidationResult result = Validate(message, dictionary, new ValidationOptions(), new ValidationContext());

        Assert.Multiple(() =>
        {
            Assert.That(result.Issues.ConvertAll(issue => issue.Code + ":" + issue.GroupPath), Is.EqualTo(new[]
            {
                "required-group-member-missing:453[0]",
                "missing-group-delimiter:453[1]",
                "group-member-out-of-order:453[1]",
                "group-member-not-allowed:453[1]",
                "required-group-member-missing:453[1]"
            }));
            Assert.That(result.Issues.ConvertAll(issue => issue.SessionRejectReason), Is.EqualTo(new[] { 1, 1, 15, 2, 1 }));
            Assert.That(result.Issues[1].SourceOffset, Is.EqualTo(30));
            Assert.That(result.Issues[2].SourceOffset, Is.EqualTo(40));
        });
    }

    [Test]
    public void InvalidDelimiterDefinitionsRemainDictionaryFailures()
    {
        FixDictionary dictionary = Dictionary();
        dictionary.Groups[0].DelimiterTag = 999;
        FixMessage message = new FixMessage { MsgType = "D" };
        FixGroup parties = Group(453, 999, 1, 0);
        parties.Entries.Add(Entry(Field(448, "P1", 1), Field(452, "1", 2)));
        message.BodyGroups.Add(parties);
        message.BodyFields = [Field(453, "1", 0)];
        ValidationOptions options = new ValidationOptions { CheckRequiredFields = false };

        ValidationResult result = Validate(message, dictionary, options, new ValidationContext());

        Assert.Multiple(() =>
        {
            Assert.That(result.Issues.ConvertAll(issue => issue.Code), Is.EqualTo(new[] { "invalid-group-delimiter-definition" }));
            Assert.That(result.Issues[0].GroupPath, Is.EqualTo("453"));
            Assert.That(result.Issues[0].Tag, Is.EqualTo(453));
            Assert.That(result.Issues[0].HasSessionRejectReason, Is.False);
        });
    }

    [Test]
    public void NestedUnknownAndDuplicateGroupsIdentifyTheirParentEntry()
    {
        FixDictionary dictionary = Dictionary();
        FixMessage message = new FixMessage { MsgType = "D" };
        FixGroup parties = Group(453, 448, 1, 0);
        FixGroupEntry party = Entry(Field(448, "P1", 1), Field(452, "1", 2));
        FixGroup first = Group(802, 523, 0, 3);
        FixGroup duplicate = Group(802, 523, 0, 4);
        FixGroup unknown = Group(999, 998, 0, 5);
        party.Groups = [first, duplicate, unknown];
        parties.Entries.Add(party);
        message.BodyGroups.Add(parties);
        message.BodyFields = [Field(453, "1", 0), Field(802, "0", 3), Field(802, "0", 4), Field(999, "0", 5)];

        ValidationResult result = Validate(message, dictionary, new ValidationOptions(), new ValidationContext());

        Assert.Multiple(() =>
        {
            Assert.That(result.Issues.ConvertAll(issue => issue.Code + ":" + issue.Tag + ":" + issue.GroupPath), Is.EqualTo(new[]
            {
                "duplicate-group-member:802:453[0]",
                "group-member-not-allowed:999:453[0]"
            }));
            Assert.That(result.Issues.ConvertAll(issue => issue.SessionRejectReason), Is.EqualTo(new[] { 13, 2 }));
            Assert.That(result.Issues.ConvertAll(issue => issue.SourceOffset), Is.EqualTo(new[] { 40, 50 }));
        });
    }

    [Test]
    public void RecursiveDepthLimitIsFatalAndCarriesTheCompleteGroupPath()
    {
        FixDictionary dictionary = RecursiveDictionary();
        FixGroup root = Group(453, 448, 1, -1);
        FixGroup current = root;
        string expectedPath = "453";
        for (int depth = 1; depth <= FixDefaults.MaxGroupDepth; depth++)
        {
            FixGroupEntry entry = Entry(Field(448, "P" + depth, -1));
            FixGroup nested = Group(453, 448, 1, -1);
            entry.Groups.Add(nested);
            current.Entries.Add(entry);
            current = nested;
            expectedPath = expectedPath + "[0]/453";
        }
        current.Entries.Add(Entry(Field(448, "LAST", -1)));
        FixMessage message = new FixMessage { MsgType = "D", BodyGroups = [root] };

        ValidationResult result = Validate(message, dictionary, new ValidationOptions(), new ValidationContext());

        Assert.Multiple(() =>
        {
            Assert.That(result.Issues.ConvertAll(issue => issue.Code), Is.EqualTo(new[] { "group-depth-limit-exceeded" }));
            Assert.That(result.Issues[0].Severity, Is.EqualTo(ValidationSeverity.Fatal));
            Assert.That(result.Issues[0].GroupPath, Is.EqualTo(expectedPath));
            Assert.That(result.Issues[0].SessionRejectReason, Is.EqualTo(99));
        });
    }

    [Test]
    public void EntryLimitIsFatalAtTheAffectedGroupPath()
    {
        FixDictionary dictionary = Dictionary();
        FixGroup parties = Group(453, 448, FixDefaults.MaxGroupEntries + 1, -1);
        for (int index = 0; index <= FixDefaults.MaxGroupEntries; index++)
        {
            parties.Entries.Add(new FixGroupEntry());
        }
        FixMessage message = new FixMessage { MsgType = "D", BodyGroups = [parties] };

        ValidationResult result = Validate(message, dictionary, new ValidationOptions(), new ValidationContext());

        Assert.Multiple(() =>
        {
            Assert.That(result.Issues.ConvertAll(issue => issue.Code), Is.EqualTo(new[] { "group-entry-limit-exceeded" }));
            Assert.That(result.Issues[0].Severity, Is.EqualTo(ValidationSeverity.Fatal));
            Assert.That(result.Issues[0].GroupPath, Is.EqualTo("453"));
            Assert.That(result.Issues[0].SessionRejectReason, Is.EqualTo(99));
        });
    }

    [Test]
    public void SectionMembershipDuplicatesAndGroupLayerSelectionAreHonored()
    {
        FixDictionary dictionary = Dictionary();
        FixMessage message = new FixMessage { MsgType = "D" };
        FixGroup first = Group(453, 448, 0, 0);
        FixGroup second = Group(453, 448, 0, 10);
        message.BodyGroups = [first, second];
        message.HeaderGroups.Add(Group(453, 448, 0, 20));
        message.BodyFields = [Field(453, "0", 0), Field(453, "0", 10)];
        message.HeaderFields = [Field(453, "0", 20)];

        ValidationResult strict = Validate(message, dictionary, new ValidationOptions(), new ValidationContext());
        ValidationResult relaxedDuplicates = Validate(message, dictionary, new ValidationOptions { RejectDuplicateFields = false }, new ValidationContext());
        ValidationResult disabled = Validate(message, dictionary, new ValidationOptions { CheckGroups = false }, new ValidationContext());

        Assert.Multiple(() =>
        {
            Assert.That(strict.Issues.ConvertAll(issue => issue.Code), Is.EqualTo(new[]
            {
                "group-not-allowed",
                "duplicate-group"
            }));
            Assert.That(strict.Issues[0].GroupPath, Is.EqualTo("453"));
            Assert.That(strict.Issues.ConvertAll(issue => issue.SessionRejectReason), Is.EqualTo(new[] { 2, 13 }));
            Assert.That(relaxedDuplicates.Issues.ConvertAll(issue => issue.Code), Is.EqualTo(new[] { "group-not-allowed" }));
            Assert.That(disabled.IsValid, Is.True);
            Assert.That(disabled.Issues, Is.Empty);
        });
    }

    private static ValidationResult Validate(FixMessage message, FixDictionary dictionary, ValidationOptions options, ValidationContext context)
    {
        options.Layers = ValidationLayer.Group;
        List<ValidationIssue> candidates = RepeatingGroupValidator.ValidateRepeatingGroups(message, dictionary, options, context);
        RejectMapper.MapValidationIssues(candidates, message);
        return ValidationCoordinator.CoordinateValidation(candidates, options);
    }

    private static FixGroup Group(int countTag, int delimiterTag, int declaredCount, int wireIndex)
    {
        return new FixGroup { CountTag = countTag, DelimiterTag = delimiterTag, DeclaredCount = declaredCount, WireIndex = wireIndex };
    }

    private static FixGroupEntry Entry(params FixField[] fields)
    {
        FixGroupEntry entry = new FixGroupEntry();
        entry.Fields.AddRange(fields);
        return entry;
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

    private static FixDictionary Dictionary()
    {
        FixComponentDefinition partyDetails = new FixComponentDefinition
        {
            Id = 10,
            Members =
            [
                Member(FixMemberKind.Field, 452, FixPresence.Required),
                Member(FixMemberKind.Field, 900, FixPresence.Optional)
            ]
        };
        FixGroupDefinition subIds = new FixGroupDefinition
        {
            Id = 2,
            Name = "PtysSubGrp",
            CountTag = 802,
            DelimiterTag = 523,
            Members =
            [
                Member(FixMemberKind.Field, 523, FixPresence.Required),
                Member(FixMemberKind.Field, 803, FixPresence.Required)
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
                Member(FixMemberKind.Field, 448, FixPresence.Required),
                Member(FixMemberKind.Field, 447, FixPresence.Optional),
                Member(FixMemberKind.Component, 10, FixPresence.Optional),
                Member(FixMemberKind.Group, 2, FixPresence.Optional),
                Member(FixMemberKind.Field, 901, FixPresence.Forbidden)
            ]
        };
        return new FixDictionary
        {
            Components = [partyDetails],
            Groups = [parties, subIds],
            Messages =
            [
                new FixMessageDefinition
                {
                    MsgType = "D",
                    Members = [Member(FixMemberKind.Group, 1, FixPresence.Optional)]
                }
            ]
        };
    }

    private static FixDictionary RecursiveDictionary()
    {
        FixGroupDefinition recursive = new FixGroupDefinition
        {
            Id = 1,
            Name = "RecursiveParties",
            CountTag = 453,
            DelimiterTag = 448,
            Members =
            [
                Member(FixMemberKind.Field, 448, FixPresence.Required),
                Member(FixMemberKind.Group, 1, FixPresence.Optional)
            ]
        };
        return new FixDictionary
        {
            Groups = [recursive],
            Messages = [new FixMessageDefinition { MsgType = "D", Members = [Member(FixMemberKind.Group, 1, FixPresence.Optional)] }]
        };
    }

    private static FixMemberDefinition Member(FixMemberKind kind, int referenceId, FixPresence presence)
    {
        return new FixMemberDefinition { Kind = kind, ReferenceId = referenceId, Presence = presence };
    }
}
