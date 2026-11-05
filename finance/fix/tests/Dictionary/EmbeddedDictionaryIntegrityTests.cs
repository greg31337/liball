using NUnit.Framework;

namespace fix.Tests;

public class EmbeddedDictionaryIntegrityTests
{
    [TestCase("FIX40", "FIX.4.0", "2", 7, 39, 138, 0, 4, 27, "D", "NewOrderSingle", 136, 137)]
    [TestCase("FIX41", "FIX.4.1", "3", 10, 53, 206, 0, 7, 28, "D", "NewOrderSingle", 199, 104)]
    [TestCase("FIX42", "FIX.4.2", "4", 20, 104, 405, 0, 17, 46, "D", "NewOrderSingle", 199, 104)]
    [TestCase("FIX43", "FIX.4.3", "5", 23, 159, 635, 10, 32, 68, "D", "NewOrderSingle", 453, 448)]
    [TestCase("FIX44", "FIX.4.4", "6", 23, 245, 912, 104, 59, 93, "D", "NewOrderSingle", 453, 448)]
    [TestCase("FIXT11", "FIXT.1.1", "", 8, 9, 71, 2, 1, 8, "A", "Logon", 627, 628)]
    [TestCase("FIX50", "", "7", 26, 290, 1090, 121, 69, 93, "D", "NewOrderSingle", 453, 448)]
    [TestCase("FIX50SP1", "", "8", 27, 327, 1373, 163, 99, 105, "D", "NewOrderSingle", 453, 448)]
    [TestCase("FIX50SP2", "", "9", 32, 668, 6028, 725, 507, 156, "D", "NewOrderSingle", 453, 448)]
    [TestCase("FIX.Latest", "", "10", 38, 694, 6203, 165, 580, 181, "D", "NewOrderSingle", 453, 448)]
    public void EveryEmbeddedVersionHasACompleteResolvableGraph(
        string name,
        string beginString,
        string applicationVersion,
        int expectedDatatypes,
        int expectedCodeSets,
        int expectedFields,
        int expectedComponents,
        int expectedGroups,
        int expectedMessages,
        string representativeMsgType,
        string representativeMessageName,
        int representativeCountTag,
        int representativeDelimiterTag)
    {
        FixDictionary dictionary = Resolve(name);
        DictionaryIndex index = DictionaryNormalizer.NormalizeDictionary(dictionary, dictionary.Options);
        List<string> unresolved = ReferenceFailures(dictionary, index);
        FixMessageDefinition? message = dictionary.Messages.Find(candidate => candidate.MsgType == representativeMsgType);
        FixGroupDefinition? group = dictionary.Groups.Find(candidate => candidate.CountTag == representativeCountTag);

        Assert.Multiple(() =>
        {
            Assert.That(dictionary.Identity.Name, Is.EqualTo(name));
            Assert.That(dictionary.Identity.BeginString, Is.EqualTo(beginString));
            Assert.That(dictionary.Identity.ApplicationVersion, Is.EqualTo(applicationVersion));
            Assert.That(dictionary.Datatypes, Has.Count.EqualTo(expectedDatatypes));
            Assert.That(dictionary.CodeSets, Has.Count.EqualTo(expectedCodeSets));
            Assert.That(dictionary.Fields, Has.Count.EqualTo(expectedFields));
            Assert.That(dictionary.Components, Has.Count.EqualTo(expectedComponents));
            Assert.That(dictionary.Groups, Has.Count.EqualTo(expectedGroups));
            Assert.That(dictionary.Messages, Has.Count.EqualTo(expectedMessages));
            Assert.That(index.Success, Is.True, string.Join(",", index.Diagnostics.ConvertAll(diagnostic => diagnostic.Code + ":" + diagnostic.DefinitionName)));
            Assert.That(unresolved, Is.Empty, string.Join(Environment.NewLine, unresolved));
            Assert.That(message, Is.Not.Null);
            Assert.That(message!.Name, Is.EqualTo(representativeMessageName));
            Assert.That(message.Members, Is.Not.Empty);
            Assert.That(group, Is.Not.Null);
            Assert.That(group!.DelimiterTag, Is.EqualTo(representativeDelimiterTag));
            Assert.That(group.Members, Is.Not.Empty);
            Assert.That(group.Members[0].ReferenceId, Is.EqualTo(representativeDelimiterTag));
        });
    }

    [Test]
    public void ClassicDictionariesContainBothSessionAndApplicationLayers()
    {
        string[] names = ["FIX40", "FIX41", "FIX42", "FIX43", "FIX44"];

        foreach (string name in names)
        {
            FixDictionary dictionary = Resolve(name);

            Assert.Multiple(() =>
            {
                Assert.That(dictionary.Identity.BeginString, Does.StartWith("FIX.4."), name);
                Assert.That(dictionary.Identity.ApplicationVersion, Is.Not.Empty, name);
                Assert.That(dictionary.Header.ConvertAll(member => member.ReferenceId), Does.Contain(FixTags.BeginString), name);
                Assert.That(dictionary.Header.ConvertAll(member => member.ReferenceId), Does.Contain(FixTags.MsgType), name);
                Assert.That(dictionary.Trailer.ConvertAll(member => member.ReferenceId), Does.Contain(FixTags.CheckSum), name);
                Assert.That(dictionary.Messages.Exists(message => message.MsgType == FixAdminMessageTypes.Logon && message.Category == "admin"), Is.True, name);
                Assert.That(dictionary.Messages.Exists(message => message.MsgType == "D" && message.Category == "app"), Is.True, name);
            });
        }
    }

    [Test]
    public void FixtTransportAndFixFiveApplicationDictionariesRemainSeparateLayers()
    {
        FixDictionary transport = Resolve("FIXT11");
        string[] applicationNames = ["FIX50", "FIX50SP1", "FIX50SP2"];

        Assert.Multiple(() =>
        {
            Assert.That(transport.Identity.BeginString, Is.EqualTo(FixBeginStrings.Fixt11));
            Assert.That(transport.Identity.ApplicationVersion, Is.Empty);
            Assert.That(transport.Header.ConvertAll(member => member.ReferenceId), Does.Contain(FixTags.BeginString));
            Assert.That(transport.Trailer.ConvertAll(member => member.ReferenceId), Does.Contain(FixTags.CheckSum));
            Assert.That(transport.Messages, Is.All.Matches<FixMessageDefinition>(message => message.Category == "admin"));
            Assert.That(transport.Messages.Exists(message => message.MsgType == FixAdminMessageTypes.Logon), Is.True);
            Assert.That(transport.Messages.Exists(message => message.MsgType == "D"), Is.False);
        });

        foreach (string name in applicationNames)
        {
            FixDictionary application = Resolve(name);

            Assert.Multiple(() =>
            {
                Assert.That(application.Identity.BeginString, Is.Empty, name);
                Assert.That(application.Identity.ApplicationVersion, Is.Not.Empty, name);
                Assert.That(application.Header, Is.Empty, name);
                Assert.That(application.Trailer, Is.Empty, name);
                Assert.That(application.Messages.Exists(message => message.MsgType == FixAdminMessageTypes.Logon), Is.False, name);
                Assert.That(application.Messages.Exists(message => message.MsgType == "D" && message.Category == "app"), Is.True, name);
            });
        }
    }

    [Test]
    public void LatestApplicationIdentityRetainsTheCompleteEp309Vocabulary()
    {
        FixDictionary transport = Resolve("FIXT11");
        FixDictionary application = Resolve("FIX.Latest");
        FixFieldDefinition? securityId = application.Fields.Find(field => field.Tag == 48);

        Assert.Multiple(() =>
        {
            Assert.That(transport.Identity.BeginString, Is.EqualTo(FixBeginStrings.Fixt11));
            Assert.That(transport.Identity.ApplicationVersion, Is.Empty);
            Assert.That(application.Identity.BeginString, Is.Empty);
            Assert.That(application.Identity.ApplicationVersion, Is.EqualTo(FixApplicationVersions.FixLatest));
            Assert.That(application.Identity.HasExtensionPack, Is.True);
            Assert.That(application.Identity.ExtensionPack, Is.EqualTo(309));
            Assert.That(application.Messages, Has.Count.EqualTo(181));
            Assert.That(application.Messages.Exists(message => message.MsgType == "D" && message.Name == "NewOrderSingle"), Is.True);
            Assert.That(application.Groups.Exists(group => group.CountTag == 453 && group.DelimiterTag == 448), Is.True);
            Assert.That(securityId, Is.Not.Null);
            Assert.That(securityId!.HasDiscriminatorTag, Is.True);
            Assert.That(securityId.DiscriminatorTag, Is.EqualTo(22));
        });
    }

    private static FixDictionary Resolve(string name)
    {
        DictionaryLoadResult result = new EmbeddedDictionaryProvider(null).ResolveDictionary(new FixDictionaryIdentity { Name = name });

        Assert.That(result.Success, Is.True, name + ":" + string.Join(",", result.Diagnostics.ConvertAll(diagnostic => diagnostic.Code)));
        Assert.That(result.Dictionary, Is.Not.Null, name);
        return result.Dictionary!;
    }

    private static List<string> ReferenceFailures(FixDictionary dictionary, DictionaryIndex index)
    {
        List<string> failures = [];
        for (int diagnosticIndex = 0; diagnosticIndex < index.Diagnostics.Count; diagnosticIndex++)
        {
            DictionaryDiagnostic diagnostic = index.Diagnostics[diagnosticIndex];
            if (diagnostic.Code.StartsWith("unresolved-", StringComparison.Ordinal) || diagnostic.Code == "invalid-length-data-pair")
            {
                failures.Add(diagnostic.Code + ":" + diagnostic.DefinitionName);
            }
        }

        HashSet<string> datatypeNames = dictionary.Datatypes.ConvertAll(datatype => datatype.Name).ToHashSet(StringComparer.OrdinalIgnoreCase);
        HashSet<string> codeSetNames = dictionary.CodeSets.ConvertAll(codeSet => codeSet.Name).ToHashSet(StringComparer.OrdinalIgnoreCase);
        for (int fieldIndex = 0; fieldIndex < dictionary.Fields.Count; fieldIndex++)
        {
            FixFieldDefinition field = dictionary.Fields[fieldIndex];
            if (field.DatatypeName.Length != 0 && DictionaryNormalizer.ResolveBuiltinFieldType(field.DatatypeName) == FixFieldType.Unknown && !datatypeNames.Contains(field.DatatypeName))
            {
                failures.Add("unresolved-datatype:" + field.Tag + ":" + field.DatatypeName);
            }
            if (field.CodeSetName.Length != 0 && !codeSetNames.Contains(field.CodeSetName))
            {
                failures.Add("unresolved-code-set:" + field.Tag + ":" + field.CodeSetName);
            }
        }
        return failures;
    }
}
