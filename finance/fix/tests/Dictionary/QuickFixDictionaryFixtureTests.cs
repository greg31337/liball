using System.Text;
using NUnit.Framework;

namespace fix.Tests;

public class QuickFixDictionaryFixtureTests
{
    [TestCase("FIX40.xml", 138, 27, true)]
    [TestCase("FIX41.xml", 206, 28, true)]
    [TestCase("FIX42.xml", 405, 46, true)]
    [TestCase("FIX43.xml", 635, 68, true)]
    [TestCase("FIX44.xml", 912, 93, true)]
    [TestCase("FIXT11.xml", 71, 8, true)]
    [TestCase("FIX50.xml", 1090, 93, false)]
    [TestCase("FIX50SP1.xml", 1373, 105, false)]
    [TestCase("FIX50SP2.xml", 6028, 156, false)]
    public void OfficialFixtureImportsAndRoundTripsRepresentableGrammar(string fileName, int expectedFields, int expectedMessages, bool expectedEnvelope)
    {
        string path = Path.Combine(RepositoryRoot(), "specs", "fix", "quickfix-dictionaries", fileName);

        AssertFixtureRoundTrip(path, expectedFields, expectedMessages, expectedEnvelope);
    }

    [TestCase("Venue44.xml", 14, 1, true)]
    [TestCase("VenueFixt11.xml", 9, 1, true)]
    public void CustomDialectFixtureImportsAndRoundTripsRepresentableGrammar(string fileName, int expectedFields, int expectedMessages, bool expectedEnvelope)
    {
        string path = Path.Combine(RepositoryRoot(), "tests", "Fixtures", "QuickFix", fileName);

        AssertFixtureRoundTrip(path, expectedFields, expectedMessages, expectedEnvelope);
    }

    [Test]
    public void FixtureMatricesCoverEveryCheckedXmlInput()
    {
        string officialDirectory = Path.Combine(RepositoryRoot(), "specs", "fix", "quickfix-dictionaries");
        string customDirectory = Path.Combine(RepositoryRoot(), "tests", "Fixtures", "QuickFix");

        Assert.Multiple(() =>
        {
            Assert.That(Directory.GetFiles(officialDirectory, "*.xml").Select(Path.GetFileName).Order().ToArray(), Is.EqualTo(new[]
            {
                "FIX40.xml",
                "FIX41.xml",
                "FIX42.xml",
                "FIX43.xml",
                "FIX44.xml",
                "FIX50.xml",
                "FIX50SP1.xml",
                "FIX50SP2.xml",
                "FIXT11.xml"
            }));
            Assert.That(Directory.GetFiles(customDirectory, "*.xml").Select(Path.GetFileName).Order().ToArray(), Is.EqualTo(new[]
            {
                "Venue44.xml",
                "VenueFixt11.xml"
            }));
        });
    }

    private static void AssertFixtureRoundTrip(string path, int expectedFields, int expectedMessages, bool expectedEnvelope)
    {
        FixDictionaryOptions options = new FixDictionaryOptions { AllowDuplicateDefinitions = true, PreserveSourceOrder = true };
        DictionaryLoadResult imported;
        using (FileStream source = File.OpenRead(path))
        {
            imported = new QuickFixDictionaryLoader().LoadDictionary(source, options);
        }
        Assert.That(imported.Success, Is.True, Path.GetFileName(path) + ":" + string.Join(",", imported.Diagnostics.Select(diagnostic => diagnostic.Code).Distinct()));
        Assert.That(imported.Dictionary, Is.Not.Null);
        FixDictionary before = imported.Dictionary!;
        string beforeProjection = QuickFixProjection(before);

        using MemoryStream destination = new MemoryStream();
        DictionaryLoadResult written = new QuickFixDictionaryWriter().WriteDictionary(destination, before, options);
        byte[] xml = destination.ToArray();
        DictionaryLoadResult reloaded;
        using (MemoryStream source = new MemoryStream(xml))
        {
            reloaded = new QuickFixDictionaryLoader().LoadDictionary(source, options);
        }
        Assert.That(reloaded.Dictionary, Is.Not.Null);
        FixDictionary after = reloaded.Dictionary!;

        Assert.Multiple(() =>
        {
            Assert.That(before.Fields, Has.Count.EqualTo(expectedFields));
            Assert.That(before.Messages, Has.Count.EqualTo(expectedMessages));
            Assert.That(before.Header.Count != 0, Is.EqualTo(expectedEnvelope));
            Assert.That(before.Trailer.Count != 0, Is.EqualTo(expectedEnvelope));
            Assert.That(imported.Diagnostics, Is.All.Matches<DictionaryDiagnostic>(diagnostic => diagnostic.Severity == ValidationSeverity.Warning));
            Assert.That(written.Success, Is.True, string.Join(",", written.Diagnostics.Select(diagnostic => diagnostic.Code).Distinct()));
            Assert.That(written.Dictionary, Is.SameAs(before));
            Assert.That(xml, Is.Not.Empty);
            Assert.That(Encoding.UTF8.GetString(xml), Does.StartWith("<fix type="));
            Assert.That(reloaded.Success, Is.True, string.Join(",", reloaded.Diagnostics.Select(diagnostic => diagnostic.Code).Distinct()));
            Assert.That(after.Fields, Has.Count.EqualTo(before.Fields.Count));
            Assert.That(after.Messages, Has.Count.EqualTo(before.Messages.Count));
            Assert.That(after.Header, Has.Count.EqualTo(before.Header.Count));
            Assert.That(after.Trailer, Has.Count.EqualTo(before.Trailer.Count));
            Assert.That(QuickFixProjection(after), Is.EqualTo(beforeProjection));
        });
    }

    private static string QuickFixProjection(FixDictionary dictionary)
    {
        StringBuilder builder = new StringBuilder();
        builder.Append("I|").Append(dictionary.Identity.Name).Append('|').Append(dictionary.Identity.BeginString).Append('|').Append(dictionary.Identity.ApplicationVersion).Append('\n');
        foreach (FixFieldDefinition field in dictionary.Fields.OrderBy(field => field.Tag))
        {
            builder.Append("F|").Append(field.Tag).Append('|').Append(field.Name).Append('|').Append(field.FieldType).Append('|');
            FixCodeSetDefinition? codeSet = dictionary.CodeSets.Find(candidate => candidate.Name == field.CodeSetName);
            if (codeSet != null)
            {
                foreach (FixCodeDefinition code in codeSet.Codes)
                {
                    builder.Append(code.Value).Append('=').Append(code.Name).Append(',');
                }
            }
            builder.Append('|').Append(field.HasLengthTag).Append(':').Append(field.LengthTag).Append('|').Append(field.HasDataTag).Append(':').Append(field.DataTag).Append('\n');
        }
        builder.Append("H|").Append(MemberListProjection(dictionary.Header, dictionary)).Append('\n');
        builder.Append("T|").Append(MemberListProjection(dictionary.Trailer, dictionary)).Append('\n');
        foreach (FixComponentDefinition component in dictionary.Components.OrderBy(component => component.Name, StringComparer.Ordinal))
        {
            builder.Append("C|").Append(component.Name).Append('|').Append(MemberListProjection(component.Members, dictionary)).Append('\n');
        }
        foreach (FixGroupDefinition group in dictionary.Groups.OrderBy(group => GroupProjection(group, dictionary), StringComparer.Ordinal))
        {
            builder.Append("G|").Append(GroupProjection(group, dictionary)).Append('\n');
        }
        foreach (FixMessageDefinition message in dictionary.Messages.OrderBy(message => message.MsgType, StringComparer.Ordinal))
        {
            builder.Append("M|").Append(message.MsgType).Append('|').Append(message.Name).Append('|').Append(message.Category).Append('|').Append(MemberListProjection(message.Members, dictionary)).Append('\n');
        }
        return builder.ToString();
    }

    private static string MemberListProjection(List<FixMemberDefinition> members, FixDictionary dictionary)
    {
        StringBuilder builder = new StringBuilder();
        foreach (FixMemberDefinition member in members)
        {
            builder.Append(member.Kind).Append(':').Append(MemberReferenceProjection(member, dictionary)).Append(':').Append(member.Presence).Append(';');
        }
        return builder.ToString();
    }

    private static string MemberReferenceProjection(FixMemberDefinition member, FixDictionary dictionary)
    {
        if (member.Kind == FixMemberKind.Field)
        {
            return member.ReferenceId.ToString(System.Globalization.CultureInfo.InvariantCulture);
        }
        if (member.Kind == FixMemberKind.Component)
        {
            FixComponentDefinition? component = dictionary.Components.Find(candidate => candidate.Id == member.ReferenceId);
            return component == null ? "missing-component" : component.Name;
        }
        if (member.Kind == FixMemberKind.Group)
        {
            FixGroupDefinition? group = dictionary.Groups.Find(candidate => candidate.Id == member.ReferenceId);
            return group == null ? "missing-group" : group.CountTag.ToString(System.Globalization.CultureInfo.InvariantCulture);
        }
        return "unknown";
    }

    private static string GroupProjection(FixGroupDefinition group, FixDictionary dictionary)
    {
        return group.CountTag.ToString(System.Globalization.CultureInfo.InvariantCulture) + "|" + group.DelimiterTag.ToString(System.Globalization.CultureInfo.InvariantCulture) + "|" + MemberListProjection(group.Members, dictionary);
    }

    private static string RepositoryRoot()
    {
        return Path.GetFullPath(Path.Combine(TestContext.CurrentContext.TestDirectory, "../../../.."));
    }
}
