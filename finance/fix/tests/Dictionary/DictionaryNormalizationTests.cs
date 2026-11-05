using System.Linq;
using fix;

namespace fix.Tests;

[TestFixture]
public class DictionaryNormalizationTests
{
    [Test]
    public void NormalizerBuildsAllCanonicalIndexesAndCompletesLengthPairs()
    {
        FixDatatypeDefinition textAlias = new FixDatatypeDefinition { Name = " VenueText ", BaseTypeName = " string " };
        FixDatatypeDefinition dataAlias = new FixDatatypeDefinition { Name = " VenueBlob ", BaseTypeName = " data " };
        FixFieldDefinition orderId = new FixFieldDefinition { Tag = 11, Name = " ClOrdID ", Abbreviation = " ClOrd ", DatatypeName = " venuetext " };
        FixFieldDefinition length = new FixFieldDefinition { Tag = 95, Name = " RawDataLength ", FieldType = FixFieldType.Length, HasDataTag = true, DataTag = 96 };
        FixFieldDefinition data = new FixFieldDefinition { Tag = 96, Name = " RawData ", DatatypeName = " venueblob " };
        FixCodeDefinition buy = new FixCodeDefinition { Id = 1, Name = " Buy ", Value = "1" };
        FixCodeSetDefinition sideCodes = new FixCodeSetDefinition { Id = 54, Name = " SideCodeSet ", FieldType = FixFieldType.Char, Codes = [buy] };
        FixComponentDefinition component = new FixComponentDefinition
        {
            Id = 100,
            Name = " OrderCore ",
            Abbreviation = " OrdCore ",
            Members = [new FixMemberDefinition { Kind = FixMemberKind.Field, ReferenceId = 11, Position = 99 }]
        };
        FixGroupDefinition group = new FixGroupDefinition
        {
            Id = 200,
            Name = " Parties ",
            CountTag = 453,
            DelimiterTag = 448,
            Members = [new FixMemberDefinition { Kind = FixMemberKind.Field, ReferenceId = 448, Position = 99 }]
        };
        FixMessageDefinition message = new FixMessageDefinition
        {
            Id = 300,
            Name = " NewOrderSingle ",
            MsgType = "D",
            Scenario = " BASE ",
            Members =
            [
                new FixMemberDefinition { Kind = FixMemberKind.Component, ReferenceId = 100, Position = 10 },
                new FixMemberDefinition { Kind = FixMemberKind.Group, ReferenceId = 200, Position = 20 }
            ]
        };
        FixScenarioDefinition scenario = new FixScenarioDefinition { Name = " BASE " };
        FixDictionary dictionary = new FixDictionary
        {
            Identity = new FixDictionaryIdentity { Name = " Venue Dictionary ", Scenario = " BASE " },
            Datatypes = [textAlias, dataAlias],
            CodeSets = [sideCodes],
            Fields =
            [
                orderId,
                length,
                data,
                new FixFieldDefinition { Tag = 448, Name = "PartyID", FieldType = FixFieldType.String },
                new FixFieldDefinition { Tag = 453, Name = "NoPartyIDs", FieldType = FixFieldType.NumInGroup }
            ],
            Components = [component],
            Groups = [group],
            Messages = [message],
            Scenarios = [scenario]
        };

        DictionaryIndex index = DictionaryNormalizer.NormalizeDictionary(dictionary, new FixDictionaryOptions());

        Assert.Multiple(() =>
        {
            Assert.That(index.Success, Is.True, string.Join(",", index.Diagnostics.ConvertAll(diagnostic => diagnostic.Code)));
            Assert.That(dictionary.Options.NormalizeNames, Is.True);
            Assert.That(dictionary.Identity.Name, Is.EqualTo("Venue Dictionary"));
            Assert.That(dictionary.Identity.Scenario, Is.EqualTo("BASE"));
            Assert.That(textAlias.Name, Is.EqualTo("VenueText"));
            Assert.That(textAlias.FieldType, Is.EqualTo(FixFieldType.String));
            Assert.That(dataAlias.FieldType, Is.EqualTo(FixFieldType.Data));
            Assert.That(orderId.FieldType, Is.EqualTo(FixFieldType.String));
            Assert.That(data.FieldType, Is.EqualTo(FixFieldType.Data));
            Assert.That(data.HasLengthTag, Is.True);
            Assert.That(data.LengthTag, Is.EqualTo(95));
            Assert.That(index.FindDatatypeByName("venuetext"), Is.SameAs(textAlias));
            Assert.That(index.FindCodeSetById(54), Is.SameAs(sideCodes));
            Assert.That(index.FindCodeSetByName(" sidecodeset "), Is.SameAs(sideCodes));
            Assert.That(index.FindCodeByValue("SIDECODESET", "1"), Is.SameAs(buy));
            Assert.That(index.FindCodeByName("sidecodeset", "buy"), Is.SameAs(buy));
            Assert.That(index.FindFieldByTag(11), Is.SameAs(orderId));
            Assert.That(index.FindFieldByName("clordid"), Is.SameAs(orderId));
            Assert.That(index.FindFieldByName("CLORD"), Is.SameAs(orderId));
            Assert.That(index.FindComponentById(100), Is.SameAs(component));
            Assert.That(index.FindComponentByName("ordcore"), Is.SameAs(component));
            Assert.That(index.FindGroupById(200), Is.SameAs(group));
            Assert.That(index.FindGroupByCountTag(453), Is.SameAs(group));
            Assert.That(index.FindGroupByName("parties"), Is.SameAs(group));
            Assert.That(index.FindMessageById(300), Is.SameAs(message));
            Assert.That(index.FindMessageByType("D", "base"), Is.SameAs(message));
            Assert.That(index.FindMessageByName("newordersingle", "BASE"), Is.SameAs(message));
            Assert.That(index.FindMessageByType("d", "base"), Is.Null);
            Assert.That(index.FindScenarioByName("base"), Is.SameAs(scenario));
            Assert.That(index.FindLengthFieldByDataTag(96), Is.SameAs(length));
            Assert.That(index.FindDataFieldByLengthTag(95), Is.SameAs(data));
            Assert.That(component.Members[0].Position, Is.Zero);
            Assert.That(message.Members.ConvertAll(member => member.Position), Is.EqualTo(new[] { 0, 1 }));
            Assert.That(dictionary.Diagnostics, Is.Empty);
        });
    }

    [Test]
    public void DuplicateIndexesKeepTheFirstDefinitionAndReportEveryCollision()
    {
        FixFieldDefinition first = new FixFieldDefinition { Tag = 11, Name = "ClOrdID", FieldType = FixFieldType.String };
        FixFieldDefinition duplicateTag = new FixFieldDefinition { Tag = 11, Name = "VenueOrderID", FieldType = FixFieldType.String };
        FixFieldDefinition duplicateName = new FixFieldDefinition { Tag = 9001, Name = " clordid ", FieldType = FixFieldType.String };
        FixMessageDefinition firstMessage = new FixMessageDefinition { Id = 1, Name = "Order", MsgType = "D", Scenario = "base" };
        FixMessageDefinition duplicateMessage = new FixMessageDefinition { Id = 2, Name = " order ", MsgType = "D", Scenario = " BASE " };
        FixCodeDefinition firstCode = new FixCodeDefinition { Name = "Buy", Value = "1" };
        FixCodeDefinition duplicateCode = new FixCodeDefinition { Name = "Bid", Value = "1" };
        FixScenarioDefinition firstScenario = new FixScenarioDefinition { Name = "base" };
        FixScenarioDefinition duplicateScenario = new FixScenarioDefinition { Name = " BASE " };
        FixDictionary dictionary = new FixDictionary
        {
            Fields = [first, duplicateTag, duplicateName],
            CodeSets = [new FixCodeSetDefinition { Id = 54, Name = "Side", Codes = [firstCode, duplicateCode] }],
            Messages = [firstMessage, duplicateMessage],
            Scenarios = [firstScenario, duplicateScenario]
        };

        DictionaryIndex index = DictionaryNormalizer.NormalizeDictionary(dictionary, new FixDictionaryOptions());

        Assert.Multiple(() =>
        {
            Assert.That(index.Success, Is.False);
            Assert.That(index.FindFieldByTag(11), Is.SameAs(first));
            Assert.That(index.FindFieldByName("clordid"), Is.SameAs(first));
            Assert.That(index.FindMessageByType("D", "base"), Is.SameAs(firstMessage));
            Assert.That(index.FindMessageByName("order", "base"), Is.SameAs(firstMessage));
            Assert.That(index.FindCodeByValue("side", "1"), Is.SameAs(firstCode));
            Assert.That(index.FindScenarioByName("base"), Is.SameAs(firstScenario));
            Assert.That(index.Diagnostics.Exists(diagnostic => diagnostic.Code == "duplicate-field-tag"), Is.True);
            Assert.That(index.Diagnostics.Exists(diagnostic => diagnostic.Code == "duplicate-field-name"), Is.True);
            Assert.That(index.Diagnostics.Exists(diagnostic => diagnostic.Code == "duplicate-message-type"), Is.True);
            Assert.That(index.Diagnostics.Exists(diagnostic => diagnostic.Code == "duplicate-message-name"), Is.True);
            Assert.That(index.Diagnostics.Exists(diagnostic => diagnostic.Code == "duplicate-code-value"), Is.True);
            Assert.That(index.Diagnostics.Exists(diagnostic => diagnostic.Code == "duplicate-scenario-name"), Is.True);
            Assert.That(dictionary.Diagnostics, Is.EqualTo(index.Diagnostics));
            Assert.That(index.Diagnostics, Is.All.Matches<DictionaryDiagnostic>(diagnostic => diagnostic.Severity == ValidationSeverity.Error));
        });
    }

    [Test]
    public void DuplicateAndUnresolvedPoliciesCanRetainAUsableIndexWithWarnings()
    {
        FixFieldDefinition first = new FixFieldDefinition { Tag = 11, Name = "ClOrdID", FieldType = FixFieldType.String };
        FixFieldDefinition duplicate = new FixFieldDefinition { Tag = 11, Name = "VenueOrderID", FieldType = FixFieldType.String };
        FixMessageDefinition message = new FixMessageDefinition
        {
            Id = 1,
            Name = "Order",
            MsgType = "D",
            Members = [new FixMemberDefinition { Kind = FixMemberKind.Component, ReferenceId = 999 }]
        };
        FixDictionary dictionary = new FixDictionary { Fields = [first, duplicate], Messages = [message] };
        FixDictionaryOptions options = new FixDictionaryOptions
        {
            AllowDuplicateDefinitions = true,
            RequireResolvedReferences = false
        };

        DictionaryIndex index = DictionaryNormalizer.NormalizeDictionary(dictionary, options);

        Assert.Multiple(() =>
        {
            Assert.That(index.Success, Is.True);
            Assert.That(index.FindFieldByTag(11), Is.SameAs(first));
            Assert.That(index.Diagnostics.ConvertAll(diagnostic => diagnostic.Code), Does.Contain("duplicate-field-tag"));
            Assert.That(index.Diagnostics.ConvertAll(diagnostic => diagnostic.Code), Does.Contain("unresolved-member-reference"));
            Assert.That(index.Diagnostics, Is.All.Matches<DictionaryDiagnostic>(diagnostic => diagnostic.Severity == ValidationSeverity.Warning));
        });
    }

    [Test]
    public void RecursiveComponentsInvalidPairsAndDefinitionLimitsAreDiagnosed()
    {
        FixComponentDefinition first = new FixComponentDefinition
        {
            Id = 1,
            Name = "First",
            Members = [new FixMemberDefinition { Kind = FixMemberKind.Component, ReferenceId = 2 }]
        };
        FixComponentDefinition second = new FixComponentDefinition
        {
            Id = 2,
            Name = "Second",
            Members = [new FixMemberDefinition { Kind = FixMemberKind.Component, ReferenceId = 1 }]
        };
        FixFieldDefinition invalidLength = new FixFieldDefinition
        {
            Tag = 95,
            Name = "RawDataLength",
            FieldType = FixFieldType.Length,
            HasDataTag = true,
            DataTag = 96
        };
        FixFieldDefinition notData = new FixFieldDefinition { Tag = 96, Name = "RawData", FieldType = FixFieldType.String };
        FixDictionary dictionary = new FixDictionary
        {
            Fields = [invalidLength, notData],
            Components = [first, second]
        };
        FixDictionaryOptions options = new FixDictionaryOptions { MaxDefinitions = 3 };

        DictionaryIndex index = DictionaryNormalizer.NormalizeDictionary(dictionary, options);

        Assert.Multiple(() =>
        {
            Assert.That(index.Success, Is.False);
            Assert.That(index.DefinitionCount, Is.EqualTo(4));
            Assert.That(index.Diagnostics.Count(diagnostic => diagnostic.Code == "recursive-component-reference"), Is.EqualTo(1));
            Assert.That(index.Diagnostics.Exists(diagnostic => diagnostic.Code == "invalid-length-data-pair" && diagnostic.Tag == 95), Is.True);
            Assert.That(index.Diagnostics.Exists(diagnostic => diagnostic.Code == "definition-limit-exceeded" && diagnostic.Severity == ValidationSeverity.Fatal), Is.True);
            Assert.That(index.FindLengthFieldByDataTag(96), Is.Null);
            Assert.That(index.FindDataFieldByLengthTag(95), Is.Null);
        });
    }
}
