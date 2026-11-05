using System.Text;
using fix;

namespace fix.Tests;

[TestFixture]
public class QuickFixDictionaryWriterTests
{
    [Test]
    public void WriterUsesCanonicalOrderAndRoundTripsRepresentableDefinitions()
    {
        FixDictionary dictionary = CreateRoundTripDictionary();
        FixDictionaryOptions options = new FixDictionaryOptions { PreserveSourceOrder = false };

        (DictionaryLoadResult firstResult, byte[] firstBytes) = Write(dictionary, options);
        (DictionaryLoadResult secondResult, byte[] secondBytes) = Write(dictionary, options);
        string xml = Encoding.UTF8.GetString(firstBytes);
        DictionaryLoadResult loaded = Load(firstBytes);

        Assert.Multiple(() =>
        {
            Assert.That(firstResult.Success, Is.True, string.Join(",", firstResult.Diagnostics.ConvertAll(diagnostic => diagnostic.Code)));
            Assert.That(firstResult.Diagnostics, Is.Empty);
            Assert.That(secondResult.Success, Is.True);
            Assert.That(secondBytes, Is.EqualTo(firstBytes));
            Assert.That(xml, Does.StartWith("<fix type=\"FIX\" major=\"4\" minor=\"4\" servicepack=\"0\">"));
            Assert.That(xml.IndexOf("<header>", StringComparison.Ordinal), Is.LessThan(xml.IndexOf("<messages>", StringComparison.Ordinal)));
            Assert.That(xml.IndexOf("<messages>", StringComparison.Ordinal), Is.LessThan(xml.IndexOf("<trailer>", StringComparison.Ordinal)));
            Assert.That(xml.IndexOf("<trailer>", StringComparison.Ordinal), Is.LessThan(xml.IndexOf("<components>", StringComparison.Ordinal)));
            Assert.That(xml.IndexOf("<components>", StringComparison.Ordinal), Is.LessThan(xml.IndexOf("<fields>", StringComparison.Ordinal)));
            Assert.That(xml.IndexOf("name=\"FirstMessage\"", StringComparison.Ordinal), Is.LessThan(xml.IndexOf("name=\"SecondMessage\"", StringComparison.Ordinal)));
            Assert.That(xml.IndexOf("number=\"8\"", StringComparison.Ordinal), Is.LessThan(xml.IndexOf("number=\"9\"", StringComparison.Ordinal)));
            Assert.That(xml.IndexOf("number=\"9\"", StringComparison.Ordinal), Is.LessThan(xml.IndexOf("number=\"10\"", StringComparison.Ordinal)));
            Assert.That(xml.IndexOf("enum=\"1\"", StringComparison.Ordinal), Is.LessThan(xml.IndexOf("enum=\"2\"", StringComparison.Ordinal)));
            Assert.That(xml, Does.Contain("<group name=\"NoPartyIDs\" required=\"N\">"));
            Assert.That(xml, Does.Contain("<group name=\"NoPartySubIDs\" required=\"N\">"));
            Assert.That(loaded.Success, Is.True, string.Join(",", loaded.Diagnostics.ConvertAll(diagnostic => diagnostic.Code)));
            Assert.That(loaded.Diagnostics, Is.Empty);
        });

        FixDictionary roundTripped = loaded.Dictionary!;
        Assert.Multiple(() =>
        {
            Assert.That(roundTripped.Fields.ConvertAll(field => field.Tag), Is.Ordered);
            Assert.That(roundTripped.Messages.ConvertAll(message => message.MsgType), Is.EqualTo(new[] { "U1", "U2" }));
            Assert.That(roundTripped.Components.Single().Name, Is.EqualTo("OrderCore"));
            Assert.That(roundTripped.CodeSets.Single().Codes.ConvertAll(code => code.Value), Is.EqualTo(new[] { "1", "2" }));
            Assert.That(roundTripped.Groups.ConvertAll(group => group.CountTag), Is.EqualTo(new[] { 453, 802 }));
            Assert.That(roundTripped.Groups[0].Members.ConvertAll(member => member.Kind), Is.EqualTo(new[] { FixMemberKind.Field, FixMemberKind.Group }));
            Assert.That(roundTripped.Header.ConvertAll(member => member.ReferenceId), Is.EqualTo(new[] { 8, 9, 35 }));
            Assert.That(roundTripped.Trailer.Single().ReferenceId, Is.EqualTo(10));
            Assert.That(roundTripped.Fields.Exists(field => field.Tag == 9001), Is.True);
        });
    }

    [Test]
    public void WriterPreservesDefinitionAndMemberOrderWhenRequested()
    {
        FixDictionary dictionary = new FixDictionary
        {
            Identity = new FixDictionaryIdentity
            {
                Name = "FIX44",
                BeginString = "FIX.4.4",
                ApplicationVersion = "FIX.4.4"
            },
            Fields =
            [
                new FixFieldDefinition { Tag = 35, Name = "MsgType", FieldType = FixFieldType.String },
                new FixFieldDefinition { Tag = 8, Name = "BeginString", FieldType = FixFieldType.String }
            ],
            Header =
            [
                Member("MsgType", FixMemberKind.Field, 35, 1, FixPresence.Required),
                Member("BeginString", FixMemberKind.Field, 8, 0, FixPresence.Required)
            ],
            Messages =
            [
                new FixMessageDefinition { Id = 2, Name = "Zulu", MsgType = "Z", Category = "app" },
                new FixMessageDefinition { Id = 1, Name = "Alpha", MsgType = "A", Category = "app" }
            ]
        };

        (DictionaryLoadResult result, byte[] bytes) = Write(dictionary, new FixDictionaryOptions { PreserveSourceOrder = true });
        string xml = Encoding.UTF8.GetString(bytes);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.Diagnostics, Is.Empty);
            Assert.That(xml.IndexOf("name=\"Zulu\"", StringComparison.Ordinal), Is.LessThan(xml.IndexOf("name=\"Alpha\"", StringComparison.Ordinal)));
            Assert.That(xml.IndexOf("number=\"35\"", StringComparison.Ordinal), Is.LessThan(xml.IndexOf("number=\"8\"", StringComparison.Ordinal)));
            Assert.That(xml.IndexOf("<field name=\"MsgType\"", StringComparison.Ordinal), Is.LessThan(xml.IndexOf("<field name=\"BeginString\"", StringComparison.Ordinal)));
        });
    }

    [Test]
    public void WriterMapsApplicationVersionIdentifierWhenBeginStringIsAbsent()
    {
        FixDictionary dictionary = new FixDictionary
        {
            Identity = new FixDictionaryIdentity
            {
                Name = "FIX50SP2",
                ApplicationVersion = FixApplicationVersions.Fix50Sp2
            }
        };

        (DictionaryLoadResult result, byte[] bytes) = Write(dictionary, new FixDictionaryOptions());
        string xml = Encoding.UTF8.GetString(bytes);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.Diagnostics, Is.Empty);
            Assert.That(xml, Does.StartWith("<fix type=\"FIX\" major=\"5\" minor=\"0\" servicepack=\"2\">"));
        });
    }

    [Test]
    public void WriterReportsOrderedLossDiagnosticsAndWritesRepresentableSubset()
    {
        FixDictionary dictionary = CreateLossyDictionary();

        (DictionaryLoadResult result, byte[] bytes) = Write(dictionary, new FixDictionaryOptions { PreserveSourceOrder = false });
        string xml = Encoding.UTF8.GetString(bytes);
        List<string> codes = result.Diagnostics.ConvertAll(diagnostic => diagnostic.Code);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True, string.Join(",", codes));
            Assert.That(result.Dictionary, Is.SameAs(dictionary));
            Assert.That(dictionary.Diagnostics, Is.Empty);
            Assert.That(result.Diagnostics, Is.All.Matches<DictionaryDiagnostic>(diagnostic => diagnostic.Severity == ValidationSeverity.Warning));
            Assert.That(codes.GetRange(0, 7), Is.EqualTo(new[]
            {
                "quickfix-loss-dictionary-name",
                "quickfix-loss-application-version",
                "quickfix-loss-extension-pack",
                "quickfix-loss-custom-version",
                "quickfix-loss-dictionary-scenario",
                "quickfix-loss-datatype-metadata",
                "quickfix-loss-scenario"
            }));
            Assert.That(codes, Does.Contain("quickfix-loss-presence"));
            Assert.That(codes, Does.Contain("quickfix-loss-presence-rule"));
            Assert.That(codes, Does.Contain("quickfix-loss-message-scenario"));
            Assert.That(codes, Does.Contain("quickfix-loss-component-metadata"));
            Assert.That(codes, Does.Contain("quickfix-loss-field-datatype"));
            Assert.That(codes, Does.Contain("quickfix-loss-code-set-name"));
            Assert.That(codes, Does.Contain("quickfix-loss-code-set-description"));
            Assert.That(codes, Does.Contain("quickfix-loss-code-description"));
            Assert.That(codes, Does.Contain("quickfix-loss-unreferenced-group"));
            Assert.That(xml, Does.StartWith("<fix type=\"FIXT\" major=\"1\" minor=\"1\" servicepack=\"0\">"));
            Assert.That(xml, Does.Contain("<field number=\"44\" name=\"PriceField\" type=\"PRICE\">"));
            Assert.That(xml, Does.Contain("<value enum=\"A\" description=\"Active\" />"));
        });
    }

    [Test]
    public void WriterReturnsStructuredFailuresForInvalidOutputInputs()
    {
        FixDictionary invalid = new FixDictionary
        {
            Identity = new FixDictionaryIdentity { Name = "Invalid" },
            Header =
            [
                Member("Missing", FixMemberKind.Field, 999, 0, FixPresence.Required),
                Member("MissingGroup", FixMemberKind.Group, 1, 1, FixPresence.Optional)
            ],
            Groups =
            [
                new FixGroupDefinition { Id = 1, Name = "MissingGroup", CountTag = 777, DelimiterTag = 778 }
            ]
        };
        (DictionaryLoadResult invalidResult, byte[] invalidBytes) = Write(invalid, new FixDictionaryOptions());
        using MemoryStream readOnly = new MemoryStream(Array.Empty<byte>(), false);
        DictionaryLoadResult unwritable = new QuickFixDictionaryWriter().WriteDictionary(readOnly, invalid, new FixDictionaryOptions());
        FixDictionary limitedDictionary = CreateRoundTripDictionary();
        (DictionaryLoadResult limited, byte[] limitedBytes) = Write(limitedDictionary, new FixDictionaryOptions { MaxDefinitions = 1 });

        Assert.Multiple(() =>
        {
            Assert.That(invalidResult.Success, Is.False);
            Assert.That(invalidBytes, Is.Not.Empty);
            Assert.That(invalidResult.Diagnostics.Exists(diagnostic => diagnostic.Code == "invalid-dictionary-version" && diagnostic.Severity == ValidationSeverity.Error), Is.True);
            Assert.That(invalidResult.Diagnostics.Exists(diagnostic => diagnostic.Code == "unresolved-field-reference" && diagnostic.Severity == ValidationSeverity.Error), Is.True);
            Assert.That(invalidResult.Diagnostics.Exists(diagnostic => diagnostic.Code == "unresolved-group-count-field" && diagnostic.Severity == ValidationSeverity.Error), Is.True);
            Assert.That(unwritable.Success, Is.False);
            Assert.That(unwritable.Diagnostics.ConvertAll(diagnostic => diagnostic.Code), Is.EqualTo(new[] { "unwritable-destination" }));
            Assert.That(limited.Success, Is.False);
            Assert.That(limitedBytes, Is.Not.Empty);
            Assert.That(limited.Diagnostics.Exists(diagnostic => diagnostic.Code == "definition-limit-exceeded" && diagnostic.Severity == ValidationSeverity.Fatal), Is.True);
        });
    }

    private static FixDictionary CreateRoundTripDictionary()
    {
        return new FixDictionary
        {
            Identity = new FixDictionaryIdentity
            {
                Name = "FIX44",
                BeginString = "FIX.4.4",
                ApplicationVersion = "FIX.4.4"
            },
            CodeSets =
            [
                new FixCodeSetDefinition
                {
                    Id = 54,
                    Name = "SideCodeSet",
                    FieldType = FixFieldType.Char,
                    Codes =
                    [
                        new FixCodeDefinition { Id = 2, Name = "SELL", Value = "2", SortOrder = 2 },
                        new FixCodeDefinition { Id = 1, Name = "BUY", Value = "1", SortOrder = 1 }
                    ]
                }
            ],
            Fields =
            [
                new FixFieldDefinition { Tag = 523, Name = "PartySubID", FieldType = FixFieldType.String },
                new FixFieldDefinition { Tag = 10, Name = "CheckSum", FieldType = FixFieldType.String },
                new FixFieldDefinition { Tag = 35, Name = "MsgType", FieldType = FixFieldType.String },
                new FixFieldDefinition { Tag = 453, Name = "NoPartyIDs", FieldType = FixFieldType.NumInGroup },
                new FixFieldDefinition { Tag = 448, Name = "PartyID", FieldType = FixFieldType.String },
                new FixFieldDefinition { Tag = 802, Name = "NoPartySubIDs", FieldType = FixFieldType.NumInGroup },
                new FixFieldDefinition { Tag = 54, Name = "Side", FieldType = FixFieldType.Char, CodeSetName = "SideCodeSet" },
                new FixFieldDefinition { Tag = 11, Name = "ClOrdID", FieldType = FixFieldType.String },
                new FixFieldDefinition { Tag = 9, Name = "BodyLength", FieldType = FixFieldType.Length },
                new FixFieldDefinition { Tag = 8, Name = "BeginString", FieldType = FixFieldType.String },
                new FixFieldDefinition { Tag = 9001, Name = "VenueID", FieldType = FixFieldType.String }
            ],
            Header =
            [
                Member("MsgType", FixMemberKind.Field, 35, 2, FixPresence.Required),
                Member("BeginString", FixMemberKind.Field, 8, 0, FixPresence.Required),
                Member("BodyLength", FixMemberKind.Field, 9, 1, FixPresence.Required)
            ],
            Trailer = [Member("CheckSum", FixMemberKind.Field, 10, 0, FixPresence.Required)],
            Components =
            [
                new FixComponentDefinition
                {
                    Id = 100,
                    Name = "OrderCore",
                    Members =
                    [
                        Member("VenueID", FixMemberKind.Field, 9001, 1, FixPresence.Optional),
                        Member("ClOrdID", FixMemberKind.Field, 11, 0, FixPresence.Required)
                    ]
                }
            ],
            Groups =
            [
                new FixGroupDefinition
                {
                    Id = 200,
                    Name = "NoPartyIDs",
                    CountTag = 453,
                    DelimiterTag = 448,
                    Members =
                    [
                        Member("NoPartySubIDs", FixMemberKind.Group, 201, 1, FixPresence.Optional),
                        Member("PartyID", FixMemberKind.Field, 448, 0, FixPresence.Required)
                    ]
                },
                new FixGroupDefinition
                {
                    Id = 201,
                    Name = "NoPartySubIDs",
                    CountTag = 802,
                    DelimiterTag = 523,
                    Members = [Member("PartySubID", FixMemberKind.Field, 523, 0, FixPresence.Required)]
                }
            ],
            Messages =
            [
                new FixMessageDefinition { Id = 2, Name = "SecondMessage", MsgType = "U2", Category = "app" },
                new FixMessageDefinition
                {
                    Id = 1,
                    Name = "FirstMessage",
                    MsgType = "U1",
                    Category = "app",
                    Members =
                    [
                        Member("NoPartyIDs", FixMemberKind.Group, 200, 2, FixPresence.Optional),
                        Member("Side", FixMemberKind.Field, 54, 1, FixPresence.Required),
                        Member("OrderCore", FixMemberKind.Component, 100, 0, FixPresence.Required)
                    ]
                }
            ]
        };
    }

    private static FixDictionary CreateLossyDictionary()
    {
        FixMemberDefinition conditional = Member("PriceField", FixMemberKind.Field, 44, 0, FixPresence.Conditional);
        conditional.Scenario = "auction";
        conditional.PresenceRule = "exists(55)";
        return new FixDictionary
        {
            Identity = new FixDictionaryIdentity
            {
                Name = "VenueTransport",
                BeginString = "FIXT.1.1",
                ApplicationVersion = "FIX.5.0SP2",
                HasExtensionPack = true,
                ExtensionPack = 309,
                CustomVersion = "venue-7",
                Scenario = "auction"
            },
            Datatypes =
            [
                new FixDatatypeDefinition
                {
                    Name = "VenuePrice",
                    BaseTypeName = "PRICE",
                    FieldType = FixFieldType.Price,
                    Description = "Venue price alias"
                }
            ],
            CodeSets =
            [
                new FixCodeSetDefinition
                {
                    Id = 44,
                    Name = "VenuePriceCodeSet",
                    FieldType = FixFieldType.Price,
                    Description = "Venue states",
                    Codes =
                    [
                        new FixCodeDefinition { Id = 1, Name = "Active", Value = "A", Description = "Currently active" }
                    ]
                }
            ],
            Fields =
            [
                new FixFieldDefinition
                {
                    Tag = 44,
                    Name = "PriceField",
                    Abbreviation = "Px",
                    DatatypeName = "VenuePrice",
                    FieldType = FixFieldType.Price,
                    CodeSetName = "VenuePriceCodeSet",
                    Description = "A venue price"
                },
                new FixFieldDefinition { Tag = 555, Name = "NoLegs", FieldType = FixFieldType.NumInGroup },
                new FixFieldDefinition { Tag = 600, Name = "LegSymbol", FieldType = FixFieldType.String }
            ],
            Header = [conditional],
            Components =
            [
                new FixComponentDefinition
                {
                    Id = 10,
                    Name = "VenueComponent",
                    Abbreviation = "VComp",
                    Description = "Venue metadata",
                    Members = [Member("PriceField", FixMemberKind.Field, 44, 0, FixPresence.Optional)]
                }
            ],
            Groups =
            [
                new FixGroupDefinition
                {
                    Id = 20,
                    Name = "NoLegs",
                    CountTag = 555,
                    DelimiterTag = 600,
                    Description = "Leg entries",
                    Members = [Member("LegSymbol", FixMemberKind.Field, 600, 0, FixPresence.Required)]
                }
            ],
            Messages =
            [
                new FixMessageDefinition
                {
                    Id = 30,
                    Name = "VenueMessage",
                    MsgType = "U1",
                    Category = "app",
                    Scenario = "auction",
                    Description = "Venue message",
                    Members = [Member("VenueComponent", FixMemberKind.Component, 10, 0, FixPresence.Required)]
                }
            ],
            Scenarios =
            [
                new FixScenarioDefinition
                {
                    Name = "auction",
                    ParentName = "base",
                    Description = "Auction behavior",
                    Rules = ["exists(55)"]
                }
            ]
        };
    }

    private static FixMemberDefinition Member(string name, FixMemberKind kind, int referenceId, int position, FixPresence presence)
    {
        return new FixMemberDefinition
        {
            Name = name,
            Kind = kind,
            ReferenceId = referenceId,
            Position = position,
            Presence = presence
        };
    }

    private static (DictionaryLoadResult Result, byte[] Bytes) Write(FixDictionary dictionary, FixDictionaryOptions options)
    {
        using MemoryStream destination = new MemoryStream();
        DictionaryLoadResult result = new QuickFixDictionaryWriter().WriteDictionary(destination, dictionary, options);
        return (result, destination.ToArray());
    }

    private static DictionaryLoadResult Load(byte[] bytes)
    {
        using MemoryStream source = new MemoryStream(bytes);
        return new QuickFixDictionaryLoader().LoadDictionary(source, new FixDictionaryOptions());
    }
}
