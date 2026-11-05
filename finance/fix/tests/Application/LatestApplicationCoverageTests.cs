using System;
using System.Collections.Generic;
using System.Text;
using fix;

namespace fix.Tests;

[TestFixture]
public class LatestApplicationCoverageTests
{
    private const ValidationLayer ApplicationLayers = ValidationLayer.DictionaryGrammar | ValidationLayer.Datatype | ValidationLayer.CodeSet | ValidationLayer.Group | ValidationLayer.ConditionalRule;

    [Test]
    public void EveryLatestMessageCompletesAValidMinimalApplicationFlow()
    {
        DictionaryLoadResult loaded = new EmbeddedDictionaryProvider(null).ResolveDictionary(new FixDictionaryIdentity { Name = "FIX.Latest" });
        Assert.That(loaded.Success, Is.True, string.Join(",", loaded.Diagnostics.ConvertAll(item => item.Code)));
        Assert.That(loaded.Dictionary, Is.Not.Null);
        FixDictionary dictionary = loaded.Dictionary!;
        List<string> failures = [];
        int completed = 0;

        for (int index = 0; index < dictionary.Messages.Count; index++)
        {
            FixMessageDefinition definition = dictionary.Messages[index];
            string failure = RunApplicationMessageFlow(dictionary, definition, FixBeginStrings.Fixt11);
            if (failure.Length == 0)
            {
                completed++;
            }
            else
            {
                failures.Add(definition.Name + "(" + definition.MsgType + "):" + failure);
            }
        }

        Assert.Multiple(() =>
        {
            Assert.That(dictionary.Messages, Has.Count.EqualTo(181));
            Assert.That(completed, Is.EqualTo(dictionary.Messages.Count), string.Join(Environment.NewLine, failures));
            Assert.That(failures, Is.Empty, string.Join(Environment.NewLine, failures));
        });
    }

    [Test]
    public void ConditionallyRequiredMessagesUseExplicitTriggerAndRequiredFieldFixtures()
    {
        FixDictionary dictionary = CreateConditionalDictionary();
        ConditionalMessageFixture[] fixtures =
        [
            new ConditionalMessageFixture
            {
                Name = "ConditionalOrder",
                MsgType = "U1",
                TriggerTag = 54,
                TriggerValue = "1",
                RequiredTag = 44,
                RequiredValue = "12.5"
            },
            new ConditionalMessageFixture
            {
                Name = "ConditionalReview",
                MsgType = "U2",
                TriggerTag = 60,
                TriggerValue = "20260830-12:34:56",
                RequiredTag = 58,
                RequiredValue = "REVIEW"
            }
        ];

        for (int index = 0; index < fixtures.Length; index++)
        {
            ConditionalMessageFixture fixture = fixtures[index];
            ApplicationMessageResult created = new FixMessageOperations().CreateApplicationMessage(dictionary, fixture.MsgType, "");
            Assert.That(created.Success, Is.True, fixture.Name + ":" + created.ErrorCode);
            FixMessage message = created.Message!;
            message.BodyFields.Add(Field(11, fixture.Name));
            message.BodyFields.Add(Field(fixture.TriggerTag, fixture.TriggerValue));
            message.BodyFields.Add(Field(fixture.RequiredTag, fixture.RequiredValue));

            byte[] encoded = Encode(message, dictionary);
            DecodeResult decoded = new TagValueMessageDecoder().DecodeMessage(encoded, dictionary, new DecodeOptions());
            ApplicationMessageResult identified = decoded.Message == null ? new ApplicationMessageResult() : new FixMessageOperations().IdentifyApplicationMessage(decoded.Message, dictionary, "");
            ValidationContext context = new ValidationContext();
            ValidationResult validation = decoded.Message == null ? new ValidationResult() : new FixMessageValidator().ValidateMessage(decoded.Message, dictionary, new ValidationOptions { Layers = ValidationLayer.All }, context);

            Assert.Multiple(() =>
            {
                Assert.That(decoded.Status, Is.EqualTo(DecodeStatus.Success), fixture.Name + ":" + decoded.ErrorCode);
                Assert.That(identified.Success, Is.True, fixture.Name + ":" + identified.ErrorCode);
                Assert.That(identified.Definition!.Name, Is.EqualTo(fixture.Name));
                Assert.That(validation.IsValid, Is.True, fixture.Name + ":" + FormatIssues(validation));
                Assert.That(decoded.Message!.BodyFields.Exists(item => item.Tag == fixture.TriggerTag), Is.True, fixture.Name);
                Assert.That(decoded.Message.BodyFields.Exists(item => item.Tag == fixture.RequiredTag), Is.True, fixture.Name);
            });
        }
    }

    internal static string RunApplicationMessageFlow(FixDictionary dictionary, FixMessageDefinition definition, string beginString)
    {
        FixMessageOperations operations = new FixMessageOperations();
        ApplicationMessageResult created = operations.CreateApplicationMessage(dictionary, definition.MsgType, definition.Scenario);
        if (!created.Success || created.Message == null) return "create:" + created.ErrorCode;

        FixMessage message = created.Message;
        message.BeginString = beginString;
        List<int> componentPath = [];
        string synthesisFailure = PopulateRequiredMembers(message.BodyFields, message.BodyGroups, definition.Members, dictionary, componentPath, 0);
        if (synthesisFailure.Length != 0) return "synthesize:" + synthesisFailure;

        EncodeResult measured = new TagValueMessageEncoder().EncodeMessage(message, dictionary, Span<byte>.Empty, new EncodeOptions { PreserveFieldOrder = false });
        if (measured.Status != EncodeStatus.DestinationTooSmall) return "measure:" + measured.ErrorCode;
        byte[] destination = new byte[measured.RequiredSize];
        EncodeResult encoded = new TagValueMessageEncoder().EncodeMessage(message, dictionary, destination, new EncodeOptions { PreserveFieldOrder = false });
        if (encoded.Status != EncodeStatus.Success) return "encode:" + encoded.ErrorCode;

        DecodeResult decoded = new TagValueMessageDecoder().DecodeMessage(destination, dictionary, new DecodeOptions());
        if (decoded.Status != DecodeStatus.Success || decoded.Message == null) return "decode:" + decoded.ErrorCode + "@" + decoded.ErrorOffset + ":" + Encoding.Latin1.GetString(destination).Replace((char)FixDefaults.Soh, '|');
        ApplicationMessageResult identified = operations.IdentifyApplicationMessage(decoded.Message, dictionary, definition.Scenario);
        if (!identified.Success || identified.Definition == null) return "identify:" + identified.ErrorCode;
        if (identified.Definition.Id != definition.Id) return "identify:wrong-definition";

        operations.RemoveField(decoded.Message, FixTags.ApplVerID);
        operations.RemoveField(decoded.Message, FixTags.ApplExtID);
        operations.RemoveField(decoded.Message, FixTags.CstmApplVerID);
        ValidationOptions validationOptions = new ValidationOptions { Layers = ApplicationLayers };
        ValidationContext validationContext = new ValidationContext { Scenario = definition.Scenario };
        ValidationResult validation = new FixMessageValidator().ValidateMessage(decoded.Message, dictionary, validationOptions, validationContext);
        if (!validation.IsValid) return "validate:" + FormatIssues(validation);
        return "";
    }

    private static string PopulateRequiredMembers(List<FixField> fields, List<FixGroup> groups, List<FixMemberDefinition> members, FixDictionary dictionary, List<int> componentPath, int depth)
    {
        if (depth > FixDefaults.MaxGroupDepth) return "dictionary-depth-limit";
        for (int index = 0; index < members.Count; index++)
        {
            FixMemberDefinition member = members[index];
            bool conditional = member.Presence == FixPresence.Conditional || member.PresenceRule.Length != 0;
            bool required = !conditional && (member.Presence == FixPresence.Required || member.Presence == FixPresence.Constant);
            if (!required || member.IsRemoved) continue;

            if (member.Kind == FixMemberKind.Field)
            {
                string failure = AddSynthesizedField(fields, member.ReferenceId, dictionary);
                if (failure.Length != 0) return failure;
            }
            else if (member.Kind == FixMemberKind.Component)
            {
                FixComponentDefinition? component = dictionary.Components.Find(item => item.Id == member.ReferenceId && !item.IsRemoved);
                if (component == null) return "unresolved-component-" + member.ReferenceId;
                if (componentPath.Contains(component.Id)) return "recursive-component-" + component.Id;
                componentPath.Add(component.Id);
                string failure = PopulateRequiredMembers(fields, groups, component.Members, dictionary, componentPath, depth + 1);
                componentPath.RemoveAt(componentPath.Count - 1);
                if (failure.Length != 0) return failure;
            }
            else if (member.Kind == FixMemberKind.Group)
            {
                FixGroupDefinition? group = dictionary.Groups.Find(item => item.Id == member.ReferenceId && !item.IsRemoved);
                if (group == null) return "unresolved-group-" + member.ReferenceId;
                string failure = AddSynthesizedGroup(fields, groups, group, dictionary, componentPath, depth + 1);
                if (failure.Length != 0) return failure;
            }
        }
        return "";
    }

    private static string AddSynthesizedGroup(List<FixField> fields, List<FixGroup> groups, FixGroupDefinition definition, FixDictionary dictionary, List<int> componentPath, int depth)
    {
        FixFieldDefinition? countDefinition = dictionary.Fields.Find(item => item.Tag == definition.CountTag && !item.IsRemoved);
        if (countDefinition == null) return "unresolved-field-" + definition.CountTag;
        if (!GroupCountRequiresEntry(countDefinition, dictionary))
        {
            if (!fields.Exists(item => item.Tag == definition.CountTag)) fields.Add(new FixField { Tag = definition.CountTag, Value = "0"u8.ToArray() });
            return "";
        }
        if (groups.Exists(item => item.CountTag == definition.CountTag)) return "";

        FixGroup group = new FixGroup { CountTag = definition.CountTag, DelimiterTag = definition.DelimiterTag, DeclaredCount = 1 };
        FixGroupEntry entry = new FixGroupEntry { EntryIndex = 0 };
        string delimiterFailure = AddSynthesizedField(entry.Fields, definition.DelimiterTag, dictionary);
        if (delimiterFailure.Length != 0) return delimiterFailure;
        string memberFailure = PopulateRequiredMembers(entry.Fields, entry.Groups, definition.Members, dictionary, componentPath, depth);
        if (memberFailure.Length != 0) return memberFailure;
        group.Entries.Add(entry);
        groups.Add(group);
        return "";
    }

    private static bool GroupCountRequiresEntry(FixFieldDefinition definition, FixDictionary dictionary)
    {
        if (definition.CodeSetName.Length == 0) return false;
        FixCodeSetDefinition? codeSet = dictionary.CodeSets.Find(item => item.Name == definition.CodeSetName && !item.IsRemoved);
        if (codeSet == null) return false;
        return !codeSet.Codes.Exists(item => !item.IsRemoved && item.Value == "0");
    }

    private static string AddSynthesizedField(List<FixField> fields, int tag, FixDictionary dictionary)
    {
        if (fields.Exists(item => item.Tag == tag)) return "";
        FixFieldDefinition? definition = dictionary.Fields.Find(item => item.Tag == tag && !item.IsRemoved);
        if (definition == null) return "unresolved-field-" + tag;

        if (definition.HasDiscriminatorTag)
        {
            string discriminatorFailure = AddSynthesizedField(fields, definition.DiscriminatorTag, dictionary);
            if (discriminatorFailure.Length != 0) return discriminatorFailure;
        }
        if (definition.HasLengthTag)
        {
            string lengthFailure = AddSynthesizedField(fields, definition.LengthTag, dictionary);
            if (lengthFailure.Length != 0) return lengthFailure;
        }

        fields.Add(new FixField { Tag = tag, Value = SynthesizeValue(definition, dictionary) });
        if (definition.HasDataTag)
        {
            string dataFailure = AddSynthesizedField(fields, definition.DataTag, dictionary);
            if (dataFailure.Length != 0) return dataFailure;
        }
        return "";
    }

    private static byte[] SynthesizeValue(FixFieldDefinition definition, FixDictionary dictionary)
    {
        if (definition.FieldType == FixFieldType.NumInGroup) return "0"u8.ToArray();
        if (definition.CodeSetName.Length != 0)
        {
            FixCodeSetDefinition? codeSet = dictionary.CodeSets.Find(item => item.Name == definition.CodeSetName && !item.IsRemoved);
            FixCodeDefinition? code = codeSet == null ? null : codeSet.Codes.Find(item => !item.IsRemoved && item.Value.Length != 0);
            if (code != null) return Encoding.Latin1.GetBytes(code.Value);
        }
        if (definition.FieldType == FixFieldType.Int || definition.FieldType == FixFieldType.SeqNum || definition.FieldType == FixFieldType.TagNum) return "1"u8.ToArray();
        if (definition.FieldType == FixFieldType.Length) return "1"u8.ToArray();
        if (definition.FieldType == FixFieldType.Float || definition.FieldType == FixFieldType.Qty || definition.FieldType == FixFieldType.Price || definition.FieldType == FixFieldType.PriceOffset || definition.FieldType == FixFieldType.Amt || definition.FieldType == FixFieldType.Percentage) return "1"u8.ToArray();
        if (definition.FieldType == FixFieldType.Boolean) return "Y"u8.ToArray();
        if (definition.FieldType == FixFieldType.Char || definition.FieldType == FixFieldType.String || definition.FieldType == FixFieldType.MultipleCharValue || definition.FieldType == FixFieldType.MultipleStringValue) return "X"u8.ToArray();
        if (definition.FieldType == FixFieldType.Currency) return "USD"u8.ToArray();
        if (definition.FieldType == FixFieldType.Country) return "US"u8.ToArray();
        if (definition.FieldType == FixFieldType.Exchange) return "XNAS"u8.ToArray();
        if (definition.FieldType == FixFieldType.MonthYear) return "202608"u8.ToArray();
        if (definition.FieldType == FixFieldType.DayOfMonth) return "1"u8.ToArray();
        if (definition.FieldType == FixFieldType.LocalMktDate || definition.FieldType == FixFieldType.UtcDateOnly) return "20260830"u8.ToArray();
        if (definition.FieldType == FixFieldType.UtcTimeOnly) return "12:34:56"u8.ToArray();
        if (definition.FieldType == FixFieldType.UtcTimestamp) return "20260830-12:34:56"u8.ToArray();
        if (definition.FieldType == FixFieldType.TzTimeOnly) return "12:34Z"u8.ToArray();
        if (definition.FieldType == FixFieldType.TzTimestamp) return "20260830-12:34:56Z"u8.ToArray();
        if (definition.FieldType == FixFieldType.Data || definition.FieldType == FixFieldType.XmlData) return "X"u8.ToArray();
        return "X"u8.ToArray();
    }

    private static byte[] Encode(FixMessage message, FixDictionary dictionary)
    {
        TagValueMessageEncoder encoder = new TagValueMessageEncoder();
        EncodeResult measured = encoder.EncodeMessage(message, dictionary, Span<byte>.Empty, new EncodeOptions { PreserveFieldOrder = false });
        Assert.That(measured.Status, Is.EqualTo(EncodeStatus.DestinationTooSmall), measured.ErrorCode);
        byte[] destination = new byte[measured.RequiredSize];
        EncodeResult encoded = encoder.EncodeMessage(message, dictionary, destination, new EncodeOptions { PreserveFieldOrder = false });
        Assert.That(encoded.Status, Is.EqualTo(EncodeStatus.Success), encoded.ErrorCode);
        return destination;
    }

    private static FixDictionary CreateConditionalDictionary()
    {
        return new FixDictionary
        {
            Identity = new FixDictionaryIdentity { Name = "conditional", BeginString = FixBeginStrings.Fix44 },
            Fields =
            [
                Definition(8, FixFieldType.String),
                Definition(9, FixFieldType.Length),
                Definition(10, FixFieldType.String),
                Definition(11, FixFieldType.String),
                Definition(35, FixFieldType.String),
                Definition(44, FixFieldType.Price),
                Definition(54, FixFieldType.Char),
                Definition(58, FixFieldType.String),
                Definition(60, FixFieldType.UtcTimestamp)
            ],
            Messages =
            [
                new FixMessageDefinition
                {
                    Id = 1,
                    Name = "ConditionalOrder",
                    MsgType = "U1",
                    Members =
                    [
                        Member(11, FixPresence.Required, ""),
                        Member(54, FixPresence.Optional, ""),
                        Member(44, FixPresence.Conditional, "exists(54)")
                    ]
                },
                new FixMessageDefinition
                {
                    Id = 2,
                    Name = "ConditionalReview",
                    MsgType = "U2",
                    Members =
                    [
                        Member(11, FixPresence.Required, ""),
                        Member(60, FixPresence.Optional, ""),
                        Member(58, FixPresence.Conditional, "exists(60)")
                    ]
                }
            ]
        };
    }

    private static FixFieldDefinition Definition(int tag, FixFieldType fieldType)
    {
        return new FixFieldDefinition { Tag = tag, Name = tag.ToString(), FieldType = fieldType };
    }

    private static FixMemberDefinition Member(int tag, FixPresence presence, string rule)
    {
        return new FixMemberDefinition
        {
            Name = tag.ToString(),
            Kind = FixMemberKind.Field,
            ReferenceId = tag,
            Presence = presence,
            PresenceRule = rule,
            RulePresence = FixPresence.Required
        };
    }

    private static FixField Field(int tag, string value)
    {
        return new FixField { Tag = tag, Value = Encoding.Latin1.GetBytes(value) };
    }

    private static string FormatIssues(ValidationResult result)
    {
        return string.Join(",", result.Issues.ConvertAll(item => item.Code + ":" + item.Tag));
    }

    private class ConditionalMessageFixture
    {
        public string Name = "";
        public string MsgType = "";
        public int TriggerTag;
        public string TriggerValue = "";
        public int RequiredTag;
        public string RequiredValue = "";
    }
}
