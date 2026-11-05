using NUnit.Framework;

namespace fix.Tests;

public class FixDictionaryDiffTests
{
    [Test]
    public void DiffReportsDefinitionAndCodeChangesInDeterministicOrder()
    {
        FixDictionary before = new FixDictionary
        {
            Identity = new FixDictionaryIdentity { Name = "FIX44" },
            Datatypes =
            [
                new FixDatatypeDefinition { Name = "Text", FieldType = FixFieldType.String, Description = "old" },
                new FixDatatypeDefinition { Name = "Removed", FieldType = FixFieldType.Int }
            ],
            CodeSets =
            [
                new FixCodeSetDefinition
                {
                    Id = 1,
                    Name = "SideCodeSet",
                    FieldType = FixFieldType.Char,
                    Description = "old",
                    Codes =
                    [
                        new FixCodeDefinition { Id = 1, Value = "1", Name = "Buy" },
                        new FixCodeDefinition { Id = 2, Value = "2", Name = "Sell" }
                    ]
                }
            ],
            Fields =
            [
                new FixFieldDefinition { Tag = 54, Name = "Side", FieldType = FixFieldType.Char, CodeSetName = "SideCodeSet" },
                new FixFieldDefinition { Tag = 55, Name = "Symbol", FieldType = FixFieldType.String }
            ]
        };
        FixDictionary after = new FixDictionary
        {
            Identity = new FixDictionaryIdentity { Name = "FIX44" },
            Datatypes =
            [
                new FixDatatypeDefinition { Name = "Text", FieldType = FixFieldType.String, BaseTypeName = "String", Description = "new" },
                new FixDatatypeDefinition { Name = "Added", FieldType = FixFieldType.Boolean }
            ],
            CodeSets =
            [
                new FixCodeSetDefinition
                {
                    Id = 1,
                    Name = "SideCodeSet",
                    FieldType = FixFieldType.Char,
                    Description = "new",
                    Codes =
                    [
                        new FixCodeDefinition { Id = 1, Value = "1", Name = "Bid" },
                        new FixCodeDefinition { Id = 3, Value = "3", Name = "Cross" }
                    ]
                }
            ],
            Fields =
            [
                new FixFieldDefinition { Tag = 54, Name = "OrderSide", FieldType = FixFieldType.Char, CodeSetName = "SideCodeSet" },
                new FixFieldDefinition { Tag = 9001, Name = "VenueField", FieldType = FixFieldType.String }
            ]
        };

        DictionaryDiff first = new FixDictionaryComposer().DiffDictionaries(before, after);
        DictionaryDiff second = new FixDictionaryComposer().DiffDictionaries(before, after);

        Assert.Multiple(() =>
        {
            Assert.That(first.Changes.ConvertAll(change => change.Kind + ":" + change.Path), Is.EqualTo(new[]
            {
                "replace:datatypes/Text",
                "remove:datatypes/Removed",
                "add:datatypes/Added",
                "replace:code-sets/SideCodeSet",
                "replace:code-sets/SideCodeSet/codes/1",
                "remove:code-sets/SideCodeSet/codes/2",
                "add:code-sets/SideCodeSet/codes/3",
                "replace:fields/54",
                "remove:fields/55",
                "add:fields/9001"
            }));
            Assert.That(second.Changes.ConvertAll(change => change.Kind + ":" + change.Path + ":" + change.Before + ":" + change.After), Is.EqualTo(first.Changes.ConvertAll(change => change.Kind + ":" + change.Path + ":" + change.Before + ":" + change.After)));
            Assert.That(FindChange(first, "datatypes/Text").IsBreaking, Is.True);
            Assert.That(FindChange(first, "code-sets/SideCodeSet/codes/2").IsBreaking, Is.True);
            Assert.That(FindChange(first, "code-sets/SideCodeSet/codes/3").IsBreaking, Is.False);
            Assert.That(FindChange(first, "fields/54").IsBreaking, Is.False);
            Assert.That(FindChange(first, "fields/55").IsBreaking, Is.True);
            Assert.That(FindChange(first, "fields/9001").After, Does.Contain("name=VenueField"));
            Assert.That(first.Diagnostics, Is.Empty);
        });
    }

    [Test]
    public void DiffReportsRequirednessAndNestedMemberChanges()
    {
        FixDictionary before = CreateNestedDictionary();
        FixDictionary after = CreateNestedDictionary();
        after.Header[0].Presence = FixPresence.Required;
        after.Components[0].Members[0].Presence = FixPresence.Required;
        after.Components[0].Members[0].Position = 2;
        after.Components[0].Members[0].PresenceRule = "when trading";
        after.Components[0].Members.RemoveAt(1);
        after.Components[0].Members.Add(Member("Venue", FixMemberKind.Field, 9001, FixPresence.Optional, 3));
        after.Groups[0].DelimiterTag = 447;
        after.Groups[0].Members[0].Presence = FixPresence.Forbidden;
        after.Messages[0].Name = "NewOrderSingleUpdated";
        after.Messages[0].Members[0].Presence = FixPresence.Optional;
        after.Messages[0].Members[1].Presence = FixPresence.Conditional;
        after.Messages[0].Members.Add(Member("Venue", FixMemberKind.Field, 9001, FixPresence.Optional, 2));
        after.Scenarios[0].Members[0].Presence = FixPresence.Required;
        after.Scenarios[0].Rules[0] = "new-rule";

        DictionaryDiff diff = new FixDictionaryComposer().DiffDictionaries(before, after);

        Assert.Multiple(() =>
        {
            Assert.That(FindChange(diff, "header/members/35/presence").IsBreaking, Is.True);
            Assert.That(FindChange(diff, "components/100/members/11/presence").Before, Is.EqualTo("optional"));
            Assert.That(FindChange(diff, "components/100/members/11/presence").After, Is.EqualTo("required"));
            Assert.That(FindChange(diff, "components/100/members/11/definition").IsBreaking, Is.True);
            Assert.That(FindChange(diff, "components/100/members/55").Kind, Is.EqualTo("remove"));
            Assert.That(FindChange(diff, "components/100/members/9001").IsBreaking, Is.False);
            Assert.That(FindChange(diff, "groups/200").IsBreaking, Is.True);
            Assert.That(FindChange(diff, "groups/200/members/448/presence").IsBreaking, Is.True);
            Assert.That(FindChange(diff, "messages/D").IsBreaking, Is.False);
            Assert.That(FindChange(diff, "messages/D/members/component-100/presence").IsBreaking, Is.False);
            Assert.That(FindChange(diff, "messages/D/members/group-200/presence").IsBreaking, Is.True);
            Assert.That(FindChange(diff, "messages/D/members/9001").Kind, Is.EqualTo("add"));
            Assert.That(FindChange(diff, "scenarios/base/members/54/presence").IsBreaking, Is.True);
            Assert.That(FindChange(diff, "scenarios/base/rules").Before, Is.EqualTo("old-rule"));
            Assert.That(FindChange(diff, "scenarios/base/rules").After, Is.EqualTo("new-rule"));
        });
    }

    [Test]
    public void DiffReportsMessageGroupAndScenarioAdditionsAndRemovals()
    {
        FixDictionary before = new FixDictionary
        {
            Groups = [new FixGroupDefinition { Id = 1, Name = "OldGroup", CountTag = 100, DelimiterTag = 101 }],
            Messages = [new FixMessageDefinition { Id = 1, Name = "OldMessage", MsgType = "A/B", Scenario = "old/scenario" }],
            Scenarios = [new FixScenarioDefinition { Name = "old/scenario" }]
        };
        FixDictionary after = new FixDictionary
        {
            Groups = [new FixGroupDefinition { Id = 2, Name = "NewGroup", CountTag = 200, DelimiterTag = 201 }],
            Messages = [new FixMessageDefinition { Id = 2, Name = "NewMessage", MsgType = "C/D", Scenario = "new/scenario" }],
            Scenarios = [new FixScenarioDefinition { Name = "new/scenario" }]
        };

        DictionaryDiff diff = new FixDictionaryComposer().DiffDictionaries(before, after);

        Assert.Multiple(() =>
        {
            Assert.That(diff.Changes.ConvertAll(change => change.Kind + ":" + change.Path), Is.EqualTo(new[]
            {
                "remove:groups/1",
                "add:groups/2",
                "remove:messages/A~1B/scenarios/old~1scenario",
                "add:messages/C~1D/scenarios/new~1scenario",
                "remove:scenarios/old~1scenario",
                "add:scenarios/new~1scenario"
            }));
            Assert.That(diff.Changes[0].IsBreaking, Is.True);
            Assert.That(diff.Changes[1].IsBreaking, Is.False);
            Assert.That(diff.Changes[2].IsBreaking, Is.True);
            Assert.That(diff.Changes[3].IsBreaking, Is.False);
            Assert.That(diff.Changes[4].IsBreaking, Is.True);
            Assert.That(diff.Changes[5].IsBreaking, Is.False);
        });
    }

    [Test]
    public void DiffReportsIdentityChangesButIgnoresProvenanceOnlyChanges()
    {
        FixDictionary before = new FixDictionary
        {
            Identity = new FixDictionaryIdentity { Name = "Base", BeginString = "FIX.4.2", Source = "before.xml" },
            Fields = [new FixFieldDefinition { Tag = 54, Name = "Side", FieldType = FixFieldType.Char, Source = "before.xml" }]
        };
        FixDictionary after = new FixDictionary
        {
            Identity = new FixDictionaryIdentity { Name = "Next", BeginString = "FIX.4.4", Source = "after.xml" },
            Fields = [new FixFieldDefinition { Tag = 54, Name = "Side", FieldType = FixFieldType.Char, Source = "after.xml" }]
        };

        DictionaryDiff diff = new FixDictionaryComposer().DiffDictionaries(before, after);

        Assert.Multiple(() =>
        {
            Assert.That(diff.Changes.ConvertAll(change => change.Path), Is.EqualTo(new[] { "identity/name", "identity/begin-string" }));
            Assert.That(diff.Changes[0].IsBreaking, Is.False);
            Assert.That(diff.Changes[1].IsBreaking, Is.True);
            Assert.That(diff.BeforeIdentity, Is.Not.SameAs(before.Identity));
            Assert.That(diff.AfterIdentity, Is.Not.SameAs(after.Identity));
            Assert.That(diff.BeforeIdentity.Source, Is.EqualTo("before.xml"));
            Assert.That(diff.AfterIdentity.Source, Is.EqualTo("after.xml"));
        });
    }

    [Test]
    public void ComposedVenueDiffIsSemanticAndDeterministicAcrossOverridesAndDeletions()
    {
        FixDictionary before = new FixDictionary
        {
            Identity = new FixDictionaryIdentity { Name = "FIX44", BeginString = FixBeginStrings.Fix44, Source = "base.xml" },
            Datatypes = [new FixDatatypeDefinition { Name = "String", FieldType = FixFieldType.String, Source = "base.xml" }],
            Fields =
            [
                new FixFieldDefinition { Tag = 11, Name = "ClOrdID", FieldType = FixFieldType.String, Description = "base", Source = "base.xml" },
                new FixFieldDefinition { Tag = 55, Name = "Symbol", FieldType = FixFieldType.String, Source = "base.xml" }
            ]
        };
        FixDictionary overlay = new FixDictionary
        {
            Identity = new FixDictionaryIdentity { CustomVersion = "venue-1", Source = "venue.xml" },
            Fields =
            [
                new FixFieldDefinition { Tag = 11, Name = "ClOrdID", FieldType = FixFieldType.String, Description = "venue", Source = "override.xml" },
                new FixFieldDefinition { Tag = 55, Name = "Symbol", IsRemoved = true },
                new FixFieldDefinition { Tag = 9001, Name = "VenueOrderID", FieldType = FixFieldType.String }
            ]
        };

        DictionaryLoadResult composed = new FixDictionaryComposer().ComposeDictionary(before, overlay, new FixDictionaryOptions());
        DictionaryDiff first = new FixDictionaryComposer().DiffDictionaries(before, composed.Dictionary!);
        DictionaryDiff second = new FixDictionaryComposer().DiffDictionaries(before, composed.Dictionary!);

        Assert.Multiple(() =>
        {
            Assert.That(composed.Success, Is.True, string.Join(",", composed.Diagnostics.ConvertAll(diagnostic => diagnostic.Code)));
            Assert.That(composed.Dictionary!.Fields.Single(field => field.Tag == 11).Source, Is.EqualTo("override.xml"));
            Assert.That(composed.Dictionary.Fields.Single(field => field.Tag == 9001).Source, Is.EqualTo("venue.xml"));
            Assert.That(first.Changes.ConvertAll(change => change.Kind + ":" + change.Path), Is.EqualTo(new[]
            {
                "replace:identity/custom-version",
                "replace:fields/11",
                "remove:fields/55",
                "add:fields/9001"
            }));
            Assert.That(FindChange(first, "fields/11").IsBreaking, Is.False);
            Assert.That(FindChange(first, "fields/55").IsBreaking, Is.True);
            Assert.That(FindChange(first, "fields/9001").IsBreaking, Is.False);
            Assert.That(second.Changes.ConvertAll(change => change.Kind + ":" + change.Path + ":" + change.Before + ":" + change.After), Is.EqualTo(first.Changes.ConvertAll(change => change.Kind + ":" + change.Path + ":" + change.Before + ":" + change.After)));
            Assert.That(first.Changes, Is.All.Matches<DictionaryChange>(change => !change.Before.Contains("base.xml", StringComparison.Ordinal) && !change.After.Contains("venue.xml", StringComparison.Ordinal) && !change.After.Contains("override.xml", StringComparison.Ordinal)));
        });
    }

    private static FixDictionary CreateNestedDictionary()
    {
        return new FixDictionary
        {
            Header = [Member("MsgType", FixMemberKind.Field, 35, FixPresence.Optional, 0)],
            Components =
            [
                new FixComponentDefinition
                {
                    Id = 100,
                    Name = "OrderCore",
                    Members =
                    [
                        Member("ClOrdID", FixMemberKind.Field, 11, FixPresence.Optional, 0),
                        Member("Symbol", FixMemberKind.Field, 55, FixPresence.Required, 1)
                    ]
                }
            ],
            Groups =
            [
                new FixGroupDefinition
                {
                    Id = 200,
                    Name = "Parties",
                    CountTag = 453,
                    DelimiterTag = 448,
                    Members = [Member("PartyID", FixMemberKind.Field, 448, FixPresence.Optional, 0)]
                }
            ],
            Messages =
            [
                new FixMessageDefinition
                {
                    Id = 1,
                    Name = "NewOrderSingle",
                    MsgType = "D",
                    Members =
                    [
                        Member("OrderCore", FixMemberKind.Component, 100, FixPresence.Required, 0),
                        Member("Parties", FixMemberKind.Group, 200, FixPresence.Optional, 1)
                    ]
                }
            ],
            Scenarios =
            [
                new FixScenarioDefinition
                {
                    Name = "base",
                    Members = [Member("Side", FixMemberKind.Field, 54, FixPresence.Optional, 0)],
                    Rules = ["old-rule"]
                }
            ]
        };
    }

    private static FixMemberDefinition Member(string name, FixMemberKind kind, int referenceId, FixPresence presence, int position)
    {
        return new FixMemberDefinition
        {
            Name = name,
            Kind = kind,
            ReferenceId = referenceId,
            Presence = presence,
            Position = position
        };
    }

    private static DictionaryChange FindChange(DictionaryDiff diff, string path)
    {
        return diff.Changes.Single(change => change.Path == path);
    }
}
