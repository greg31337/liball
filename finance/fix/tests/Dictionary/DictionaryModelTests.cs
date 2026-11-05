using fix;

namespace fix.Tests;

[TestFixture]
public class DictionaryModelTests
{
    [Test]
    public void DictionaryGraphRetainsDefinitionsOrderAndReferences()
    {
        FixDatatypeDefinition stringType = new FixDatatypeDefinition
        {
            Name = "String",
            FieldType = FixFieldType.String
        };
        FixCodeSetDefinition sideCodes = new FixCodeSetDefinition
        {
            Id = 54,
            Name = "SideCodeSet",
            FieldType = FixFieldType.Char,
            Codes =
            [
                new FixCodeDefinition { Id = 54001, Name = "Buy", Value = "1", SortOrder = 1 },
                new FixCodeDefinition { Id = 54002, Name = "Sell", Value = "2", SortOrder = 2 }
            ]
        };
        FixFieldDefinition encodedLength = new FixFieldDefinition
        {
            Tag = 354,
            Name = "EncodedTextLen",
            DatatypeName = "Length",
            FieldType = FixFieldType.Length,
            HasDataTag = true,
            DataTag = 355
        };
        FixFieldDefinition encodedText = new FixFieldDefinition
        {
            Tag = 355,
            Name = "EncodedText",
            DatatypeName = "data",
            FieldType = FixFieldType.Data,
            HasLengthTag = true,
            LengthTag = 354
        };
        FixComponentDefinition partiesComponent = new FixComponentDefinition
        {
            Id = 1001,
            Name = "Parties",
            Members =
            [
                new FixMemberDefinition
                {
                    Name = "NoPartyIDs",
                    Kind = FixMemberKind.Group,
                    ReferenceId = 2001,
                    Position = 0,
                    Presence = FixPresence.Optional
                }
            ]
        };
        FixGroupDefinition partiesGroup = new FixGroupDefinition
        {
            Id = 2001,
            Name = "PartiesGroup",
            CountTag = 453,
            DelimiterTag = 448,
            Members =
            [
                new FixMemberDefinition
                {
                    Name = "PartyID",
                    Kind = FixMemberKind.Field,
                    ReferenceId = 448,
                    Position = 0,
                    Presence = FixPresence.Required
                },
                new FixMemberDefinition
                {
                    Name = "PtysSubGrp",
                    Kind = FixMemberKind.Group,
                    ReferenceId = 2002,
                    Position = 1,
                    Presence = FixPresence.Optional
                }
            ]
        };
        FixMessageDefinition order = new FixMessageDefinition
        {
            Id = 14,
            Name = "NewOrderSingle",
            MsgType = "D",
            Category = "SingleGeneralOrderHandling",
            Scenario = "base",
            Members =
            [
                new FixMemberDefinition
                {
                    Name = "Side",
                    Kind = FixMemberKind.Field,
                    ReferenceId = 54,
                    Position = 0,
                    Presence = FixPresence.Required
                },
                new FixMemberDefinition
                {
                    Name = "Parties",
                    Kind = FixMemberKind.Component,
                    ReferenceId = 1001,
                    Position = 1,
                    Presence = FixPresence.Optional
                }
            ]
        };
        FixDictionary dictionary = new FixDictionary
        {
            Identity = new FixDictionaryIdentity
            {
                Name = "FIX Latest EP309",
                BeginString = FixBeginStrings.Fixt11,
                ApplicationVersion = FixApplicationVersions.FixLatest,
                HasExtensionPack = true,
                ExtensionPack = 309,
                Scenario = "base"
            },
            Datatypes = [stringType],
            CodeSets = [sideCodes],
            Fields = [encodedLength, encodedText],
            Components = [partiesComponent],
            Groups = [partiesGroup],
            Messages = [order],
            Scenarios = [new FixScenarioDefinition { Name = "base", Rules = ["exists(54)"] }]
        };

        Assert.Multiple(() =>
        {
            Assert.That(dictionary.Identity.ExtensionPack, Is.EqualTo(309));
            Assert.That(dictionary.Datatypes[0], Is.SameAs(stringType));
            Assert.That(dictionary.CodeSets[0].Codes[0].Value, Is.EqualTo("1"));
            Assert.That(dictionary.CodeSets[0].Codes[1].Value, Is.EqualTo("2"));
            Assert.That(dictionary.Fields[0].DataTag, Is.EqualTo(dictionary.Fields[1].Tag));
            Assert.That(dictionary.Fields[1].LengthTag, Is.EqualTo(dictionary.Fields[0].Tag));
            Assert.That(dictionary.Components[0].Members[0].ReferenceId, Is.EqualTo(partiesGroup.Id));
            Assert.That(dictionary.Groups[0].Members[0].ReferenceId, Is.EqualTo(448));
            Assert.That(dictionary.Groups[0].Members[1].ReferenceId, Is.EqualTo(2002));
            Assert.That(dictionary.Messages[0].Members[0].Position, Is.EqualTo(0));
            Assert.That(dictionary.Messages[0].Members[1].Position, Is.EqualTo(1));
            Assert.That(dictionary.Scenarios[0].Rules[0], Is.EqualTo("exists(54)"));
        });
    }

    [Test]
    public void LoadResultRetainsPartialDictionaryAndOrderedDiagnostics()
    {
        FixDictionary dictionary = new FixDictionary
        {
            Identity = new FixDictionaryIdentity { Name = "venue-overlay" }
        };
        DictionaryDiagnostic duplicate = new DictionaryDiagnostic
        {
            Severity = ValidationSeverity.Error,
            Code = "duplicate-field",
            Message = "Field 9001 is defined twice.",
            Source = "venue.xml",
            Line = 12,
            Column = 4,
            DefinitionName = "VenueField",
            HasTag = true,
            Tag = 9001
        };
        DictionaryDiagnostic unresolved = new DictionaryDiagnostic
        {
            Severity = ValidationSeverity.Error,
            Code = "unresolved-component",
            DefinitionName = "VenueMessage"
        };
        DictionaryLoadResult result = new DictionaryLoadResult
        {
            Success = false,
            Dictionary = dictionary,
            Diagnostics = [duplicate, unresolved]
        };

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.Dictionary, Is.SameAs(dictionary));
            Assert.That(result.Diagnostics[0], Is.SameAs(duplicate));
            Assert.That(result.Diagnostics[1], Is.SameAs(unresolved));
            Assert.That(result.Diagnostics[0].Tag, Is.EqualTo(9001));
            Assert.That(result.Diagnostics[0].Line, Is.EqualTo(12));
            Assert.That(result.Diagnostics[0].Column, Is.EqualTo(4));
        });
    }

    [Test]
    public void DictionaryDiffRetainsIdentityAndDeterministicChanges()
    {
        DictionaryChange add = new DictionaryChange
        {
            Kind = "add",
            Path = "fields/9001",
            After = "VenueField:String"
        };
        DictionaryChange requiredness = new DictionaryChange
        {
            Kind = "replace",
            Path = "messages/D/members/54/presence",
            Before = "optional",
            After = "required",
            IsBreaking = true
        };
        DictionaryDiff diff = new DictionaryDiff
        {
            BeforeIdentity = new FixDictionaryIdentity { Name = "base", ApplicationVersion = "9" },
            AfterIdentity = new FixDictionaryIdentity { Name = "overlay", ApplicationVersion = "9" },
            Changes = [add, requiredness]
        };

        Assert.Multiple(() =>
        {
            Assert.That(diff.BeforeIdentity.Name, Is.EqualTo("base"));
            Assert.That(diff.AfterIdentity.Name, Is.EqualTo("overlay"));
            Assert.That(diff.Changes[0], Is.SameAs(add));
            Assert.That(diff.Changes[1], Is.SameAs(requiredness));
            Assert.That(diff.Changes[1].Before, Is.EqualTo("optional"));
            Assert.That(diff.Changes[1].After, Is.EqualTo("required"));
            Assert.That(diff.Changes[1].IsBreaking, Is.True);
        });
    }
}
