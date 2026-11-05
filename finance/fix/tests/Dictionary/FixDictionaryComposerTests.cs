using fix;

namespace fix.Tests;

[TestFixture]
public class FixDictionaryComposerTests
{
    [Test]
    public void ComposeAddsReplacesRemovesMergesNestedDefinitionsAndPreservesProvenance()
    {
        FixDictionary baseDictionary = CreateBaseDictionary();
        FixDictionary overlay = CreateOverlayDictionary();

        DictionaryLoadResult result = new FixDictionaryComposer().ComposeDictionary(baseDictionary, overlay, new FixDictionaryOptions());

        Assert.That(result.Dictionary, Is.Not.Null);
        FixDictionary composed = result.Dictionary!;
        FixCodeSetDefinition sideCodes = composed.CodeSets.Single(codeSet => codeSet.Name == "SideCodeSet");
        FixComponentDefinition component = composed.Components.Single(component => component.Id == 100);
        FixGroupDefinition group = composed.Groups.Single(group => group.Id == 200);
        FixMessageDefinition message = composed.Messages.Single(message => message.MsgType == "D");
        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True, string.Join(",", result.Diagnostics.ConvertAll(diagnostic => diagnostic.Code)));
            Assert.That(result.Dictionary, Is.SameAs(composed));
            Assert.That(result.Diagnostics.ConvertAll(diagnostic => diagnostic.Code), Is.EqualTo(new[] { "base-note", "overlay-note" }));
            Assert.That(composed.Identity.Name, Is.EqualTo("VenueFIX44"));
            Assert.That(composed.Identity.ApplicationVersion, Is.EqualTo("FIX.4.4"));
            Assert.That(composed.Identity.CustomVersion, Is.EqualTo("venue-1"));
            Assert.That(composed.Identity.Source, Is.EqualTo("venue.xml"));
            Assert.That(composed.Datatypes.ConvertAll(datatype => datatype.Name), Is.EqualTo(new[] { "String", "VenueText" }));
            Assert.That(composed.CodeSets.ConvertAll(codeSet => codeSet.Name), Is.EqualTo(new[] { "SideCodeSet" }));
            Assert.That(sideCodes.Codes.ConvertAll(code => code.Value), Is.EqualTo(new[] { "1", "4", "3" }));
            Assert.That(sideCodes.Codes.ConvertAll(code => code.Name), Is.EqualTo(new[] { "Bid", "Cross", "Auction" }));
            Assert.That(sideCodes.Codes.ConvertAll(code => code.Source), Is.EqualTo(new[] { "venue.xml", "base.xml", "venue.xml" }));
            Assert.That(composed.Fields.ConvertAll(field => field.Tag), Is.EqualTo(new[] { 8, 9, 10, 11, 35, 54, 448, 453, 9001 }));
            Assert.That(composed.Fields.Single(field => field.Tag == 11).Description, Is.EqualTo("Venue order identifier"));
            Assert.That(composed.Fields.Single(field => field.Tag == 11).Source, Is.EqualTo("venue-fields.xml"));
            Assert.That(composed.Fields.Single(field => field.Tag == 54).Source, Is.EqualTo("base.xml"));
            Assert.That(composed.Fields.Single(field => field.Tag == 9001).Source, Is.EqualTo("venue.xml"));
            Assert.That(component.Members.ConvertAll(member => member.ReferenceId), Is.EqualTo(new[] { 11, 9001 }));
            Assert.That(component.Members.ConvertAll(member => member.Source), Is.EqualTo(new[] { "venue.xml", "venue.xml" }));
            Assert.That(group.Members.ConvertAll(member => member.ReferenceId), Is.EqualTo(new[] { 448, 9001 }));
            Assert.That(message.Members.ConvertAll(member => member.ReferenceId), Is.EqualTo(new[] { 100, 200, 54 }));
            Assert.That(message.Description, Is.EqualTo("Venue order"));
            Assert.That(composed.Messages.Exists(definition => definition.MsgType == "F"), Is.False);
            Assert.That(composed.Components.Exists(definition => definition.Id == 101), Is.False);
            Assert.That(composed.Groups.Exists(definition => definition.Id == 201), Is.False);
            Assert.That(composed.Scenarios.ConvertAll(scenario => scenario.Name), Is.EqualTo(new[] { "base", "auction" }));
            Assert.That(composed.Header.Single(member => member.ReferenceId == 35).Presence, Is.EqualTo(FixPresence.Optional));
            Assert.That(baseDictionary.Fields.Exists(field => field.Tag == 55), Is.True);
            Assert.That(baseDictionary.CodeSets[0].Codes.ConvertAll(code => code.Value), Is.EqualTo(new[] { "1", "2", "4" }));
            Assert.That(overlay.Fields.Exists(field => field.IsRemoved), Is.True);
            Assert.That(composed, Is.Not.SameAs(baseDictionary));
            Assert.That(composed.Fields[0], Is.Not.SameAs(baseDictionary.Fields[0]));
            Assert.That(composed.CodeSets[0].Codes[0], Is.Not.SameAs(baseDictionary.CodeSets[0].Codes[0]));
        });

        composed.Fields[0].Name = "Changed";
        composed.CodeSets[0].Codes[0].Name = "Changed";
        Assert.Multiple(() =>
        {
            Assert.That(baseDictionary.Fields[0].Name, Is.EqualTo("BeginString"));
            Assert.That(baseDictionary.CodeSets[0].Codes[0].Name, Is.EqualTo("Buy"));
        });
    }

    [Test]
    public void ComposeReportsCollisionsAndEnforcesVenueExtensionTagRange()
    {
        FixDictionary baseDictionary = new FixDictionary
        {
            Identity = new FixDictionaryIdentity { Name = "Base", Source = "base.xml" },
            Datatypes = [new FixDatatypeDefinition { Name = "String", FieldType = FixFieldType.String }],
            Fields =
            [
                new FixFieldDefinition { Tag = 54, Name = "Side", FieldType = FixFieldType.String },
                new FixFieldDefinition { Tag = 9001, Name = "VenueExisting", FieldType = FixFieldType.String }
            ],
            Components = [new FixComponentDefinition { Id = 100, Name = "Core" }]
        };
        FixDictionary overlay = new FixDictionary
        {
            Identity = new FixDictionaryIdentity { Name = "Venue", Source = "venue.xml" },
            Fields =
            [
                new FixFieldDefinition { Tag = 100, Name = "TooLow", FieldType = FixFieldType.String },
                new FixFieldDefinition { Tag = 9002, Name = "Side", FieldType = FixFieldType.String },
                new FixFieldDefinition { Tag = 9001, Name = "VenueReplacement", FieldType = FixFieldType.String },
                new FixFieldDefinition { Tag = 9003, Name = "VenueAdded", FieldType = FixFieldType.String },
                new FixFieldDefinition { Tag = 9999, Name = "Missing", IsRemoved = true }
            ],
            Components = [new FixComponentDefinition { Id = 101, Name = "Core" }]
        };

        DictionaryLoadResult result = new FixDictionaryComposer().ComposeDictionary(baseDictionary, overlay, new FixDictionaryOptions());

        Assert.That(result.Dictionary, Is.Not.Null);
        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.Dictionary!.Fields.ConvertAll(field => field.Tag), Is.EqualTo(new[] { 54, 9001, 9003 }));
            Assert.That(result.Dictionary.Fields.Single(field => field.Tag == 9001).Name, Is.EqualTo("VenueReplacement"));
            Assert.That(result.Dictionary.Components.ConvertAll(component => component.Id), Is.EqualTo(new[] { 100 }));
            Assert.That(result.Diagnostics.Exists(diagnostic => diagnostic.Code == "invalid-venue-extension-tag" && diagnostic.Tag == 100 && diagnostic.Source == "venue.xml"), Is.True);
            Assert.That(result.Diagnostics.Exists(diagnostic => diagnostic.Code == "field-name-collision" && diagnostic.Tag == 9002), Is.True);
            Assert.That(result.Diagnostics.Exists(diagnostic => diagnostic.Code == "overlay-remove-missing-field" && diagnostic.Severity == ValidationSeverity.Warning), Is.True);
            Assert.That(result.Diagnostics.Exists(diagnostic => diagnostic.Code == "component-name-collision" && diagnostic.Severity == ValidationSeverity.Error), Is.True);
        });
    }

    [Test]
    public void OfficialExtensionPackMayAddStandardRangeFields()
    {
        FixDictionary baseDictionary = new FixDictionary
        {
            Identity = new FixDictionaryIdentity { Name = "FIX50SP2" },
            Datatypes = [new FixDatatypeDefinition { Name = "String", FieldType = FixFieldType.String }]
        };
        FixDictionary overlay = new FixDictionary
        {
            Identity = new FixDictionaryIdentity
            {
                Name = "FIX50SP2_EP1",
                HasExtensionPack = true,
                ExtensionPack = 1,
                Source = "ep1.xml"
            },
            Fields = [new FixFieldDefinition { Tag = 100, Name = "OfficialAddition", FieldType = FixFieldType.String }]
        };

        DictionaryLoadResult first = new FixDictionaryComposer().ComposeDictionary(baseDictionary, overlay, new FixDictionaryOptions());
        DictionaryLoadResult second = new FixDictionaryComposer().ComposeDictionary(baseDictionary, overlay, new FixDictionaryOptions());

        Assert.Multiple(() =>
        {
            Assert.That(first.Success, Is.True, string.Join(",", first.Diagnostics.ConvertAll(diagnostic => diagnostic.Code)));
            Assert.That(first.Diagnostics, Is.Empty);
            Assert.That(first.Dictionary!.Fields.Single().Tag, Is.EqualTo(100));
            Assert.That(first.Dictionary.Fields.Single().Source, Is.EqualTo("ep1.xml"));
            Assert.That(first.Dictionary.Identity.HasExtensionPack, Is.True);
            Assert.That(first.Dictionary.Identity.ExtensionPack, Is.EqualTo(1));
            Assert.That(second.Dictionary!.Fields.ConvertAll(field => field.Tag), Is.EqualTo(first.Dictionary.Fields.ConvertAll(field => field.Tag)));
            Assert.That(second.Diagnostics.ConvertAll(diagnostic => diagnostic.Code), Is.EqualTo(first.Diagnostics.ConvertAll(diagnostic => diagnostic.Code)));
        });
    }

    [Test]
    public void RemovedReferencedDefinitionsAndLimitsRemainStructuredFailures()
    {
        FixDictionary baseDictionary = new FixDictionary
        {
            Identity = new FixDictionaryIdentity { Name = "Base" },
            Datatypes = [new FixDatatypeDefinition { Name = "String", FieldType = FixFieldType.String }],
            Fields = [new FixFieldDefinition { Tag = 54, Name = "Side", FieldType = FixFieldType.String }],
            Messages =
            [
                new FixMessageDefinition
                {
                    Id = 1,
                    Name = "Order",
                    MsgType = "D",
                    Members = [Member("Side", FixMemberKind.Field, 54, FixPresence.Required)]
                }
            ]
        };
        FixDictionary overlay = new FixDictionary
        {
            Identity = new FixDictionaryIdentity { Name = "Overlay" },
            Fields = [new FixFieldDefinition { Tag = 54, Name = "Side", IsRemoved = true }]
        };

        DictionaryLoadResult unresolved = new FixDictionaryComposer().ComposeDictionary(baseDictionary, overlay, new FixDictionaryOptions());
        DictionaryLoadResult limited = new FixDictionaryComposer().ComposeDictionary(baseDictionary, new FixDictionary(), new FixDictionaryOptions { MaxDefinitions = 1 });

        Assert.Multiple(() =>
        {
            Assert.That(unresolved.Success, Is.False);
            Assert.That(unresolved.Diagnostics.Exists(diagnostic => diagnostic.Code == "unresolved-member-reference" && diagnostic.Severity == ValidationSeverity.Error), Is.True);
            Assert.That(limited.Success, Is.False);
            Assert.That(limited.Diagnostics.Exists(diagnostic => diagnostic.Code == "definition-limit-exceeded" && diagnostic.Severity == ValidationSeverity.Fatal), Is.True);
        });
    }

    [Test]
    public void DiffMethodReturnsDetachedIdentitiesForEquivalentDictionaries()
    {
        FixDictionary before = new FixDictionary { Identity = new FixDictionaryIdentity { Name = "before", Source = "before.xml" } };
        FixDictionary after = new FixDictionary { Identity = new FixDictionaryIdentity { Name = "before", Source = "after.xml" } };

        DictionaryDiff diff = new FixDictionaryComposer().DiffDictionaries(before, after);

        Assert.Multiple(() =>
        {
            Assert.That(diff.BeforeIdentity.Name, Is.EqualTo("before"));
            Assert.That(diff.AfterIdentity.Name, Is.EqualTo("before"));
            Assert.That(diff.BeforeIdentity, Is.Not.SameAs(before.Identity));
            Assert.That(diff.AfterIdentity, Is.Not.SameAs(after.Identity));
            Assert.That(diff.Changes, Is.Empty);
            Assert.That(diff.Diagnostics, Is.Empty);
        });
    }

    [Test]
    public void CrossCategoryConflictsAreRejectedInDeterministicOrder()
    {
        FixDictionary baseDictionary = new FixDictionary
        {
            Identity = new FixDictionaryIdentity { Name = "Base", Source = "base.xml" },
            CodeSets =
            [
                new FixCodeSetDefinition
                {
                    Id = 54,
                    Name = "SideCodeSet",
                    FieldType = FixFieldType.Char,
                    Codes = [new FixCodeDefinition { Name = "Buy", Value = "1" }]
                }
            ],
            Fields =
            [
                new FixFieldDefinition { Tag = 54, Name = "Side", FieldType = FixFieldType.Char, CodeSetName = "SideCodeSet" },
                new FixFieldDefinition { Tag = 448, Name = "PartyID", FieldType = FixFieldType.String },
                new FixFieldDefinition { Tag = 453, Name = "NoPartyIDs", FieldType = FixFieldType.NumInGroup }
            ],
            Components = [new FixComponentDefinition { Id = 100, Name = "OrderCore" }],
            Groups = [new FixGroupDefinition { Id = 200, Name = "Parties", CountTag = 453, DelimiterTag = 448 }],
            Messages = [new FixMessageDefinition { Id = 1, Name = "Order", MsgType = "D", Scenario = "base" }]
        };
        FixDictionary overlay = new FixDictionary
        {
            Identity = new FixDictionaryIdentity { Name = "Venue", Source = "venue.xml" },
            CodeSets =
            [
                new FixCodeSetDefinition { Id = 54, Name = "OtherCodeSet", Source = "codes-id.xml" },
                new FixCodeSetDefinition
                {
                    Id = 54,
                    Name = "SideCodeSet",
                    FieldType = FixFieldType.Char,
                    Codes = [new FixCodeDefinition { Name = "Buy", Value = "2", Source = "codes-name.xml" }]
                }
            ],
            Fields = [new FixFieldDefinition { Tag = 9001, Name = "Side", FieldType = FixFieldType.String, Source = "fields.xml" }],
            Components = [new FixComponentDefinition { Id = 101, Name = "OrderCore", Source = "components.xml" }],
            Groups = [new FixGroupDefinition { Id = 201, Name = "Parties", CountTag = 453, DelimiterTag = 448, Source = "groups.xml" }],
            Messages = [new FixMessageDefinition { Id = 2, Name = "Order", MsgType = "E", Scenario = "base", Source = "messages.xml" }]
        };
        FixDictionaryOptions options = new FixDictionaryOptions();

        DictionaryLoadResult first = new FixDictionaryComposer().ComposeDictionary(baseDictionary, overlay, options);
        DictionaryLoadResult second = new FixDictionaryComposer().ComposeDictionary(baseDictionary, overlay, options);

        Assert.That(first.Dictionary, Is.Not.Null);
        Assert.That(second.Dictionary, Is.Not.Null);
        Assert.Multiple(() =>
        {
            Assert.That(first.Success, Is.False);
            Assert.That(first.Diagnostics.ConvertAll(diagnostic => diagnostic.Code + ":" + diagnostic.Source), Is.EqualTo(new[]
            {
                "code-set-id-collision:codes-id.xml",
                "code-name-collision:codes-name.xml",
                "field-name-collision:fields.xml",
                "component-name-collision:components.xml",
                "group-name-collision:groups.xml",
                "message-name-collision:messages.xml"
            }));
            Assert.That(second.Diagnostics.ConvertAll(diagnostic => diagnostic.Code + ":" + diagnostic.Source), Is.EqualTo(first.Diagnostics.ConvertAll(diagnostic => diagnostic.Code + ":" + diagnostic.Source)));
            Assert.That(first.Dictionary!.CodeSets.Single().Codes.ConvertAll(code => code.Value), Is.EqualTo(new[] { "1" }));
            Assert.That(first.Dictionary.Fields.ConvertAll(field => field.Tag), Is.EqualTo(new[] { 54, 448, 453 }));
            Assert.That(first.Dictionary.Components.ConvertAll(component => component.Id), Is.EqualTo(new[] { 100 }));
            Assert.That(first.Dictionary.Groups.ConvertAll(group => group.Id), Is.EqualTo(new[] { 200 }));
            Assert.That(first.Dictionary.Messages.ConvertAll(message => message.MsgType), Is.EqualTo(new[] { "D" }));
            Assert.That(second.Dictionary!.CodeSets.Single().Codes.ConvertAll(code => code.Value), Is.EqualTo(first.Dictionary.CodeSets.Single().Codes.ConvertAll(code => code.Value)));
        });
    }

    [Test]
    public void MissingDeletionTargetsProduceStableWarningsWithoutMutatingTheBase()
    {
        FixDictionary baseDictionary = new FixDictionary
        {
            Identity = new FixDictionaryIdentity { Name = "Base", Source = "base.xml" },
            Datatypes = [new FixDatatypeDefinition { Name = "String", FieldType = FixFieldType.String }],
            CodeSets =
            [
                new FixCodeSetDefinition
                {
                    Id = 54,
                    Name = "SideCodeSet",
                    FieldType = FixFieldType.Char,
                    Codes = [new FixCodeDefinition { Name = "Buy", Value = "1" }]
                }
            ],
            Fields = [new FixFieldDefinition { Tag = 11, Name = "ClOrdID", FieldType = FixFieldType.String }],
            Header = [Member("ClOrdID", FixMemberKind.Field, 11, FixPresence.Required)],
            Components =
            [
                new FixComponentDefinition
                {
                    Id = 100,
                    Name = "OrderCore",
                    Members = [Member("ClOrdID", FixMemberKind.Field, 11, FixPresence.Required)]
                }
            ]
        };
        FixDictionary overlay = new FixDictionary
        {
            Identity = new FixDictionaryIdentity { Name = "Venue", Source = "venue.xml" },
            Datatypes = [new FixDatatypeDefinition { Name = "MissingDatatype", IsRemoved = true }],
            CodeSets =
            [
                new FixCodeSetDefinition
                {
                    Id = 54,
                    Name = "SideCodeSet",
                    FieldType = FixFieldType.Char,
                    Codes = [new FixCodeDefinition { Name = "MissingCode", Value = "9", IsRemoved = true }]
                },
                new FixCodeSetDefinition { Name = "MissingCodeSet", IsRemoved = true }
            ],
            Fields = [new FixFieldDefinition { Tag = 9001, Name = "MissingField", IsRemoved = true }],
            Header = [new FixMemberDefinition { Name = "MissingHeader", Kind = FixMemberKind.Field, ReferenceId = 9001, IsRemoved = true }],
            Components =
            [
                new FixComponentDefinition
                {
                    Id = 100,
                    Name = "OrderCore",
                    Members = [new FixMemberDefinition { Name = "MissingMember", Kind = FixMemberKind.Field, ReferenceId = 9001, IsRemoved = true }]
                },
                new FixComponentDefinition { Id = 999, Name = "MissingComponent", IsRemoved = true }
            ],
            Groups = [new FixGroupDefinition { Id = 999, Name = "MissingGroup", IsRemoved = true }],
            Messages = [new FixMessageDefinition { Name = "MissingMessage", MsgType = "Z", IsRemoved = true }],
            Scenarios = [new FixScenarioDefinition { Name = "MissingScenario", IsRemoved = true }]
        };

        DictionaryLoadResult result = new FixDictionaryComposer().ComposeDictionary(baseDictionary, overlay, new FixDictionaryOptions());

        Assert.That(result.Dictionary, Is.Not.Null);
        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True, string.Join(",", result.Diagnostics.ConvertAll(diagnostic => diagnostic.Code)));
            Assert.That(result.Diagnostics.ConvertAll(diagnostic => diagnostic.Code), Is.EqualTo(new[]
            {
                "overlay-remove-missing-datatype",
                "overlay-remove-missing-code",
                "overlay-remove-missing-code-set",
                "overlay-remove-missing-field",
                "overlay-remove-missing-member",
                "overlay-remove-missing-component",
                "overlay-remove-missing-group",
                "overlay-remove-missing-message",
                "overlay-remove-missing-scenario",
                "overlay-remove-missing-member"
            }));
            Assert.That(result.Diagnostics, Is.All.Matches<DictionaryDiagnostic>(diagnostic => diagnostic.Severity == ValidationSeverity.Warning && diagnostic.Source == "venue.xml"));
            Assert.That(result.Dictionary!.Datatypes.Single().Name, Is.EqualTo("String"));
            Assert.That(result.Dictionary.CodeSets.Single().Codes.Single().Value, Is.EqualTo("1"));
            Assert.That(result.Dictionary.Fields.Single().Tag, Is.EqualTo(11));
            Assert.That(result.Dictionary.Components.Single().Members.Single().ReferenceId, Is.EqualTo(11));
            Assert.That(result.Dictionary.Header.Single().ReferenceId, Is.EqualTo(11));
            Assert.That(baseDictionary.CodeSets.Single().Codes.Single().Value, Is.EqualTo("1"));
            Assert.That(baseDictionary.Components.Single().Members.Single().ReferenceId, Is.EqualTo(11));
        });
    }

    private static FixDictionary CreateBaseDictionary()
    {
        string origin = "base.xml";
        return new FixDictionary
        {
            Identity = new FixDictionaryIdentity
            {
                Name = "FIX44",
                BeginString = "FIX.4.4",
                ApplicationVersion = "FIX.4.4",
                Source = origin
            },
            Datatypes =
            [
                new FixDatatypeDefinition { Name = "String", FieldType = FixFieldType.String, Source = origin },
                new FixDatatypeDefinition { Name = "Legacy", FieldType = FixFieldType.String, Source = origin }
            ],
            CodeSets =
            [
                new FixCodeSetDefinition
                {
                    Id = 54,
                    Name = "SideCodeSet",
                    FieldType = FixFieldType.Char,
                    Source = origin,
                    Codes =
                    [
                        new FixCodeDefinition { Id = 1, Name = "Buy", Value = "1", Source = origin },
                        new FixCodeDefinition { Id = 2, Name = "Sell", Value = "2", Source = origin },
                        new FixCodeDefinition { Id = 4, Name = "Cross", Value = "4", Source = origin }
                    ]
                },
                new FixCodeSetDefinition { Id = 500, Name = "LegacyCodeSet", FieldType = FixFieldType.String, Source = origin }
            ],
            Fields =
            [
                new FixFieldDefinition { Tag = 8, Name = "BeginString", FieldType = FixFieldType.String, Source = origin },
                new FixFieldDefinition { Tag = 9, Name = "BodyLength", FieldType = FixFieldType.Length, Source = origin },
                new FixFieldDefinition { Tag = 10, Name = "CheckSum", FieldType = FixFieldType.String, Source = origin },
                new FixFieldDefinition { Tag = 11, Name = "ClOrdID", FieldType = FixFieldType.String, Description = "Base order identifier", Source = origin },
                new FixFieldDefinition { Tag = 35, Name = "MsgType", FieldType = FixFieldType.String, Source = origin },
                new FixFieldDefinition { Tag = 54, Name = "Side", FieldType = FixFieldType.Char, CodeSetName = "SideCodeSet", Source = origin },
                new FixFieldDefinition { Tag = 55, Name = "Symbol", FieldType = FixFieldType.String, Source = origin },
                new FixFieldDefinition { Tag = 448, Name = "PartyID", FieldType = FixFieldType.String, Source = origin },
                new FixFieldDefinition { Tag = 453, Name = "NoPartyIDs", FieldType = FixFieldType.NumInGroup, Source = origin }
            ],
            Header =
            [
                Member("BeginString", FixMemberKind.Field, 8, FixPresence.Required),
                Member("BodyLength", FixMemberKind.Field, 9, FixPresence.Required),
                Member("MsgType", FixMemberKind.Field, 35, FixPresence.Required)
            ],
            Trailer = [Member("CheckSum", FixMemberKind.Field, 10, FixPresence.Required)],
            Components =
            [
                new FixComponentDefinition
                {
                    Id = 100,
                    Name = "OrderCore",
                    Source = origin,
                    Members =
                    [
                        Member("ClOrdID", FixMemberKind.Field, 11, FixPresence.Required),
                        Member("Symbol", FixMemberKind.Field, 55, FixPresence.Required)
                    ]
                },
                new FixComponentDefinition { Id = 101, Name = "LegacyComponent", Source = origin }
            ],
            Groups =
            [
                new FixGroupDefinition
                {
                    Id = 200,
                    Name = "Parties",
                    CountTag = 453,
                    DelimiterTag = 448,
                    Source = origin,
                    Members = [Member("PartyID", FixMemberKind.Field, 448, FixPresence.Required)]
                },
                new FixGroupDefinition { Id = 201, Name = "LegacyGroup", CountTag = 453, DelimiterTag = 448, Source = origin }
            ],
            Messages =
            [
                new FixMessageDefinition
                {
                    Id = 1,
                    Name = "NewOrderSingle",
                    MsgType = "D",
                    Scenario = "base",
                    Description = "Base order",
                    Source = origin,
                    Members =
                    [
                        Member("OrderCore", FixMemberKind.Component, 100, FixPresence.Required),
                        Member("Parties", FixMemberKind.Group, 200, FixPresence.Optional)
                    ]
                },
                new FixMessageDefinition { Id = 2, Name = "Cancel", MsgType = "F", Scenario = "base", Source = origin }
            ],
            Scenarios =
            [
                new FixScenarioDefinition { Name = "base", Source = origin },
                new FixScenarioDefinition { Name = "legacy", ParentName = "base", Source = origin }
            ],
            Diagnostics =
            [
                new DictionaryDiagnostic { Severity = ValidationSeverity.Information, Code = "base-note", Source = origin }
            ]
        };
    }

    private static FixDictionary CreateOverlayDictionary()
    {
        return new FixDictionary
        {
            Identity = new FixDictionaryIdentity
            {
                Name = "VenueFIX44",
                CustomVersion = "venue-1",
                Source = "venue.xml"
            },
            Datatypes =
            [
                new FixDatatypeDefinition { Name = "Legacy", IsRemoved = true },
                new FixDatatypeDefinition { Name = "VenueText", BaseTypeName = "String" }
            ],
            CodeSets =
            [
                new FixCodeSetDefinition
                {
                    Id = 54,
                    Name = "SideCodeSet",
                    FieldType = FixFieldType.Char,
                    Codes =
                    [
                        new FixCodeDefinition { Id = 10, Name = "Bid", Value = "1" },
                        new FixCodeDefinition { Name = "Sell", Value = "2", IsRemoved = true },
                        new FixCodeDefinition { Id = 30, Name = "Auction", Value = "3" }
                    ]
                },
                new FixCodeSetDefinition { Name = "LegacyCodeSet", IsRemoved = true }
            ],
            Fields =
            [
                new FixFieldDefinition { Tag = 11, Name = "ClOrdID", FieldType = FixFieldType.String, Description = "Venue order identifier", Source = "venue-fields.xml" },
                new FixFieldDefinition { Tag = 55, Name = "Symbol", IsRemoved = true },
                new FixFieldDefinition { Tag = 9001, Name = "VenueID", FieldType = FixFieldType.String }
            ],
            Header = [Member("MsgType", FixMemberKind.Field, 35, FixPresence.Optional)],
            Components =
            [
                new FixComponentDefinition
                {
                    Id = 100,
                    Name = "OrderCore",
                    Members =
                    [
                        Member("ClOrdID", FixMemberKind.Field, 11, FixPresence.Required),
                        new FixMemberDefinition { Name = "Symbol", Kind = FixMemberKind.Field, ReferenceId = 55, IsRemoved = true },
                        Member("VenueID", FixMemberKind.Field, 9001, FixPresence.Optional)
                    ]
                },
                new FixComponentDefinition { Id = 101, Name = "LegacyComponent", IsRemoved = true }
            ],
            Groups =
            [
                new FixGroupDefinition
                {
                    Id = 200,
                    Name = "Parties",
                    CountTag = 453,
                    DelimiterTag = 448,
                    Members = [Member("VenueID", FixMemberKind.Field, 9001, FixPresence.Optional)]
                },
                new FixGroupDefinition { Id = 201, Name = "LegacyGroup", IsRemoved = true }
            ],
            Messages =
            [
                new FixMessageDefinition
                {
                    Id = 1,
                    Name = "NewOrderSingle",
                    MsgType = "D",
                    Scenario = "base",
                    Description = "Venue order",
                    Members = [Member("Side", FixMemberKind.Field, 54, FixPresence.Required)]
                },
                new FixMessageDefinition { Name = "Cancel", MsgType = "F", Scenario = "base", IsRemoved = true }
            ],
            Scenarios =
            [
                new FixScenarioDefinition { Name = "legacy", IsRemoved = true },
                new FixScenarioDefinition { Name = "auction", ParentName = "base", Rules = ["exists(54)"] }
            ],
            Diagnostics =
            [
                new DictionaryDiagnostic { Severity = ValidationSeverity.Warning, Code = "overlay-note", Source = "venue.xml" }
            ]
        };
    }

    private static FixMemberDefinition Member(string name, FixMemberKind kind, int referenceId, FixPresence presence)
    {
        return new FixMemberDefinition
        {
            Name = name,
            Kind = kind,
            ReferenceId = referenceId,
            Presence = presence
        };
    }
}
