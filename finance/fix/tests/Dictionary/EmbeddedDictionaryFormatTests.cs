using System.IO.Compression;
using System.Text;
using NUnit.Framework;

namespace fix.Tests;

public class EmbeddedDictionaryFormatTests
{
    [Test]
    public void ResourceRoundTripPreservesCanonicalGraphAndProvenance()
    {
        FixDictionary expected = CreateDictionary();
        using MemoryStream stream = new MemoryStream();

        EmbeddedDictionaryFormat.WriteResource(stream, expected);
        stream.Position = 0;
        FixDictionary actual = EmbeddedDictionaryFormat.ReadResource(stream);

        DictionaryDiff diff = new FixDictionaryComposer().DiffDictionaries(expected, actual);
        Assert.Multiple(() =>
        {
            Assert.That(diff.Changes, Is.Empty);
            Assert.That(actual.Options.MaxDefinitions, Is.EqualTo(1234));
            Assert.That(actual.Options.UserDefinedTagMinimum, Is.EqualTo(7000));
            Assert.That(actual.Options.AllowDuplicateDefinitions, Is.True);
            Assert.That(actual.Options.AllowUnknownDatatypes, Is.True);
            Assert.That(actual.Options.RequireResolvedReferences, Is.False);
            Assert.That(actual.Options.NormalizeNames, Is.False);
            Assert.That(actual.Options.PreserveSourceOrder, Is.False);
            Assert.That(actual.Identity.Source, Is.EqualTo("spec.xml"));
            Assert.That(actual.Datatypes[0].Source, Is.EqualTo("spec.xml"));
            Assert.That(actual.CodeSets[0].Codes[0].Source, Is.EqualTo("spec.xml"));
            Assert.That(actual.Fields[0].Source, Is.EqualTo("spec.xml"));
            Assert.That(actual.Fields[1].HasDiscriminatorTag, Is.True);
            Assert.That(actual.Fields[1].DiscriminatorTag, Is.EqualTo(95));
            Assert.That(actual.Header[0].Source, Is.EqualTo("spec.xml"));
            Assert.That(actual.Header[0].RulePresence, Is.EqualTo(FixPresence.Forbidden));
            Assert.That(actual.Components[0].Members[0].Source, Is.EqualTo("spec.xml"));
            Assert.That(actual.Groups[0].Members[0].Source, Is.EqualTo("spec.xml"));
            Assert.That(actual.Messages[0].Members[0].Source, Is.EqualTo("spec.xml"));
            Assert.That(actual.Scenarios[0].Rules, Is.EqualTo(new[] { "x=1", "y=2" }));
            Assert.That(actual.Scenarios[0].IsRemoved, Is.True);
        });
    }

    [Test]
    public void ResourceEncodingIsByteForByteDeterministic()
    {
        FixDictionary dictionary = CreateDictionary();
        using MemoryStream first = new MemoryStream();
        using MemoryStream second = new MemoryStream();

        EmbeddedDictionaryFormat.WriteResource(first, dictionary);
        EmbeddedDictionaryFormat.WriteResource(second, dictionary);

        Assert.That(second.ToArray(), Is.EqualTo(first.ToArray()));
    }

    [Test]
    public void ResourceReaderRejectsUnsupportedFormatAndInvalidCounts()
    {
        using MemoryStream invalidFormat = new MemoryStream();
        using (GZipStream compression = new GZipStream(invalidFormat, CompressionLevel.SmallestSize, true))
        using (BinaryWriter writer = new BinaryWriter(compression, Encoding.UTF8, true))
        {
            writer.Write("FIXDICT");
            writer.Write(2);
        }
        invalidFormat.Position = 0;

        using MemoryStream invalidCount = new MemoryStream();
        using BinaryWriter countWriter = new BinaryWriter(invalidCount, Encoding.UTF8, true);
        countWriter.Write(-1);
        countWriter.Flush();
        invalidCount.Position = 0;
        using BinaryReader countReader = new BinaryReader(invalidCount, Encoding.UTF8, true);

        Assert.Multiple(() =>
        {
            Assert.That(() => EmbeddedDictionaryFormat.ReadResource(invalidFormat), Throws.TypeOf<FixDictionaryException>());
            Assert.That(() => EmbeddedDictionaryFormat.ReadDefinitionCount(countReader), Throws.TypeOf<FixDictionaryException>());
        });
    }

    private static FixDictionary CreateDictionary()
    {
        string source = "spec.xml";
        return new FixDictionary
        {
            Identity = new FixDictionaryIdentity
            {
                Name = "FIX.Latest",
                ApplicationVersion = FixApplicationVersions.FixLatest,
                HasExtensionPack = true,
                ExtensionPack = 309,
                CustomVersion = "venue",
                Scenario = "base",
                Source = source
            },
            Options = new FixDictionaryOptions
            {
                MaxDefinitions = 1234,
                UserDefinedTagMinimum = 7000,
                AllowDuplicateDefinitions = true,
                AllowUnknownDatatypes = true,
                RequireResolvedReferences = false,
                NormalizeNames = false,
                PreserveSourceOrder = false
            },
            Datatypes =
            [
                new FixDatatypeDefinition { Name = "VenueText", FieldType = FixFieldType.String, BaseTypeName = "String", Description = "text", Source = source }
            ],
            CodeSets =
            [
                new FixCodeSetDefinition
                {
                    Id = 500,
                    Name = "SideCodeSet",
                    FieldType = FixFieldType.Char,
                    Description = "sides",
                    Source = source,
                    Codes = [new FixCodeDefinition { Id = 1, Name = "Buy", Value = "1", SortOrder = 2, Description = "buy", Source = source }]
                }
            ],
            Fields =
            [
                new FixFieldDefinition
                {
                    Tag = 95,
                    Name = "RawDataLength",
                    Abbreviation = "RawLen",
                    DatatypeName = "Length",
                    FieldType = FixFieldType.Length,
                    HasDataTag = true,
                    DataTag = 96,
                    Description = "length",
                    Source = source
                },
                new FixFieldDefinition
                {
                    Tag = 96,
                    Name = "RawData",
                    DatatypeName = "data",
                    FieldType = FixFieldType.Data,
                    CodeSetName = "SideCodeSet",
                    HasDiscriminatorTag = true,
                    DiscriminatorTag = 95,
                    HasLengthTag = true,
                    LengthTag = 95,
                    Source = source
                }
            ],
            Header = [Member("RawDataLength", FixMemberKind.Field, 95, 0, FixPresence.Required, source)],
            Trailer = [Member("RawData", FixMemberKind.Field, 96, 0, FixPresence.Optional, source)],
            Components =
            [
                new FixComponentDefinition
                {
                    Id = 100,
                    Name = "Core",
                    Abbreviation = "Cr",
                    Description = "core",
                    Source = source,
                    Members = [Member("RawDataLength", FixMemberKind.Field, 95, 0, FixPresence.Required, source)]
                }
            ],
            Groups =
            [
                new FixGroupDefinition
                {
                    Id = 200,
                    Name = "DataGroup",
                    CountTag = 1000,
                    DelimiterTag = 95,
                    Description = "group",
                    Source = source,
                    Members = [Member("RawDataLength", FixMemberKind.Field, 95, 0, FixPresence.Required, source)]
                }
            ],
            Messages =
            [
                new FixMessageDefinition
                {
                    Id = 1,
                    Name = "DataMessage",
                    MsgType = "U1",
                    Category = "venue",
                    Scenario = "base",
                    Description = "message",
                    Source = source,
                    Members = [Member("Core", FixMemberKind.Component, 100, 0, FixPresence.Conditional, source)]
                }
            ],
            Scenarios =
            [
                new FixScenarioDefinition
                {
                    Name = "base",
                    ParentName = "root",
                    Description = "scenario",
                    Source = source,
                    IsRemoved = true,
                    Members = [Member("DataGroup", FixMemberKind.Group, 200, 0, FixPresence.Forbidden, source)],
                    Rules = ["x=1", "y=2"]
                }
            ]
        };
    }

    private static FixMemberDefinition Member(string name, FixMemberKind kind, int referenceId, int position, FixPresence presence, string source)
    {
        return new FixMemberDefinition
        {
            Name = name,
            Kind = kind,
            ReferenceId = referenceId,
            Position = position,
            Presence = presence,
            Scenario = "base",
            PresenceRule = "when x",
            RulePresence = FixPresence.Forbidden,
            Source = source
        };
    }
}
