using System.Text;
using NUnit.Framework;

namespace fix.Tests;

public class DictionaryVersionSelectorTests
{
    [Test]
    public void ClassicFixUsesBeginStringForBothDictionaryIdentities()
    {
        FixMessage message = new FixMessage
        {
            BeginString = FixBeginStrings.Fix44,
            HeaderFields =
            [
                Field(FixTags.BeginString, FixBeginStrings.Fix44),
                Field(FixTags.ApplVerID, FixApplicationVersions.Fix50Sp2),
                Field(FixTags.ApplVerID, FixApplicationVersions.Fix50)
            ]
        };
        FixDictionaryIdentity transport = new FixDictionaryIdentity
        {
            Name = "configured-transport",
            BeginString = FixBeginStrings.Fixt11,
            ApplicationVersion = "transport-stale",
            HasExtensionPack = true,
            ExtensionPack = 1,
            CustomVersion = "transport-custom",
            Scenario = "session",
            Source = "transport.xml"
        };
        FixDictionaryIdentity application = new FixDictionaryIdentity
        {
            Name = "configured-application",
            ApplicationVersion = FixApplicationVersions.Fix50Sp2,
            HasExtensionPack = true,
            ExtensionPack = 100,
            CustomVersion = "venue",
            Source = "application.xml"
        };

        DictionarySelectionResult result = DictionaryVersionSelector.SelectDictionaryIdentities(message, transport, application, FixApplicationVersions.Fix50);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.Diagnostics, Is.Empty);
            Assert.That(result.TransportIdentity.BeginString, Is.EqualTo(FixBeginStrings.Fix44));
            Assert.That(result.TransportIdentity.ApplicationVersion, Is.Empty);
            Assert.That(result.TransportIdentity.HasExtensionPack, Is.False);
            Assert.That(result.TransportIdentity.Scenario, Is.EqualTo("session"));
            Assert.That(result.ApplicationIdentity.BeginString, Is.EqualTo(FixBeginStrings.Fix44));
            Assert.That(result.ApplicationIdentity.ApplicationVersion, Is.Empty);
            Assert.That(result.ApplicationIdentity.HasExtensionPack, Is.False);
            Assert.That(result.ApplicationIdentity.CustomVersion, Is.Empty);
            Assert.That(result.TransportIdentity.Source, Is.EqualTo("transport.xml"));
            Assert.That(result.ApplicationIdentity.Source, Is.EqualTo("application.xml"));
            Assert.That(result.TransportIdentity, Is.Not.SameAs(transport));
            Assert.That(result.ApplicationIdentity, Is.Not.SameAs(application));
            Assert.That(transport.BeginString, Is.EqualTo(FixBeginStrings.Fixt11));
            Assert.That(application.ApplicationVersion, Is.EqualTo(FixApplicationVersions.Fix50Sp2));
        });
    }

    [Test]
    public void FixtMessageApplicationVersionAndQualifiersOverrideDefaults()
    {
        FixMessage message = new FixMessage
        {
            BeginString = FixBeginStrings.Fixt11,
            HeaderFields =
            [
                Field(FixTags.BeginString, FixBeginStrings.Fixt11),
                Field(FixTags.ApplVerID, FixApplicationVersions.Fix50Sp2),
                Field(FixTags.ApplExtID, "309"),
                Field(FixTags.CstmApplVerID, "VENUE-2")
            ],
            BodyFields = [Field(FixTags.DefaultApplVerID, FixApplicationVersions.Fix50)]
        };
        FixDictionaryIdentity configuredApplication = ApplicationIdentity(FixApplicationVersions.Fix50Sp1, 100, "VENUE-1");

        DictionarySelectionResult result = DictionaryVersionSelector.SelectDictionaryIdentities(message, TransportIdentity(), configuredApplication, FixApplicationVersions.Fix44);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.TransportIdentity.BeginString, Is.EqualTo(FixBeginStrings.Fixt11));
            Assert.That(result.TransportIdentity.ApplicationVersion, Is.Empty);
            Assert.That(result.ApplicationIdentity.BeginString, Is.Empty);
            Assert.That(result.ApplicationIdentity.ApplicationVersion, Is.EqualTo(FixApplicationVersions.Fix50Sp2));
            Assert.That(result.ApplicationIdentity.HasExtensionPack, Is.True);
            Assert.That(result.ApplicationIdentity.ExtensionPack, Is.EqualTo(309));
            Assert.That(result.ApplicationIdentity.CustomVersion, Is.EqualTo("VENUE-2"));
            Assert.That(result.ApplicationIdentity.Name, Is.Empty);
            Assert.That(result.ApplicationIdentity.Source, Is.EqualTo("application.xml"));
            Assert.That(configuredApplication.ApplicationVersion, Is.EqualTo(FixApplicationVersions.Fix50Sp1));
            Assert.That(configuredApplication.ExtensionPack, Is.EqualTo(100));
            Assert.That(configuredApplication.CustomVersion, Is.EqualTo("VENUE-1"));
        });
    }

    [Test]
    public void FixtFallbackPrecedenceRetainsOnlyApplicableConfiguredQualifiers()
    {
        FixDictionaryIdentity configuredApplication = ApplicationIdentity(FixApplicationVersions.Fix50Sp2, 100, "VENUE-1");
        FixMessage negotiated = FixtMessage();
        negotiated.BodyFields.Add(Field(FixTags.DefaultApplVerID, FixApplicationVersions.Fix50));
        FixMessage configuredDefault = FixtMessage();
        FixMessage configuredIdentity = FixtMessage();
        FixMessage qualifiedDefault = FixtMessage();
        qualifiedDefault.HeaderFields.Add(Field(FixTags.ApplExtID, "309"));
        qualifiedDefault.HeaderFields.Add(Field(FixTags.CstmApplVerID, "VENUE-2"));

        DictionarySelectionResult fromNegotiation = DictionaryVersionSelector.SelectDictionaryIdentities(negotiated, TransportIdentity(), configuredApplication, FixApplicationVersions.Fix50Sp1);
        DictionarySelectionResult fromConfiguredDefault = DictionaryVersionSelector.SelectDictionaryIdentities(configuredDefault, TransportIdentity(), configuredApplication, FixApplicationVersions.Fix50Sp1);
        DictionarySelectionResult fromConfiguredIdentity = DictionaryVersionSelector.SelectDictionaryIdentities(configuredIdentity, TransportIdentity(), configuredApplication, "");
        DictionarySelectionResult withQualifiers = DictionaryVersionSelector.SelectDictionaryIdentities(qualifiedDefault, TransportIdentity(), configuredApplication, "");

        Assert.Multiple(() =>
        {
            Assert.That(fromNegotiation.ApplicationIdentity.ApplicationVersion, Is.EqualTo(FixApplicationVersions.Fix50));
            Assert.That(fromNegotiation.ApplicationIdentity.HasExtensionPack, Is.False);
            Assert.That(fromNegotiation.ApplicationIdentity.CustomVersion, Is.Empty);
            Assert.That(fromConfiguredDefault.ApplicationIdentity.ApplicationVersion, Is.EqualTo(FixApplicationVersions.Fix50Sp1));
            Assert.That(fromConfiguredDefault.ApplicationIdentity.HasExtensionPack, Is.False);
            Assert.That(fromConfiguredDefault.ApplicationIdentity.CustomVersion, Is.Empty);
            Assert.That(fromConfiguredIdentity.ApplicationIdentity.ApplicationVersion, Is.EqualTo(FixApplicationVersions.Fix50Sp2));
            Assert.That(fromConfiguredIdentity.ApplicationIdentity.HasExtensionPack, Is.True);
            Assert.That(fromConfiguredIdentity.ApplicationIdentity.ExtensionPack, Is.EqualTo(100));
            Assert.That(fromConfiguredIdentity.ApplicationIdentity.CustomVersion, Is.EqualTo("VENUE-1"));
            Assert.That(withQualifiers.ApplicationIdentity.ApplicationVersion, Is.EqualTo(FixApplicationVersions.Fix50Sp2));
            Assert.That(withQualifiers.ApplicationIdentity.ExtensionPack, Is.EqualTo(309));
            Assert.That(withQualifiers.ApplicationIdentity.CustomVersion, Is.EqualTo("VENUE-2"));
            Assert.That(fromNegotiation.Success && fromConfiguredDefault.Success && fromConfiguredIdentity.Success && withQualifiers.Success, Is.True);
        });
    }

    [Test]
    public void SelectionReportsDuplicatesInvalidExtensionAndEmptyValuesDeterministically()
    {
        FixMessage message = FixtMessage();
        message.HeaderFields.Add(Field(FixTags.ApplVerID, FixApplicationVersions.Fix50));
        message.HeaderFields.Add(Field(FixTags.ApplVerID, FixApplicationVersions.Fix50Sp2));
        message.HeaderFields.Add(Field(FixTags.ApplExtID, "30x"));
        message.HeaderFields.Add(Field(FixTags.CstmApplVerID, ""));

        DictionarySelectionResult result = DictionaryVersionSelector.SelectDictionaryIdentities(message, TransportIdentity(), new FixDictionaryIdentity(), "");

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.ApplicationIdentity.ApplicationVersion, Is.EqualTo(FixApplicationVersions.Fix50));
            Assert.That(result.ApplicationIdentity.HasExtensionPack, Is.False);
            Assert.That(result.Diagnostics.ConvertAll(diagnostic => diagnostic.Code + ":" + diagnostic.Tag), Is.EqualTo(new[]
            {
                "duplicate-dictionary-version-field:1128",
                "invalid-application-extension:1156",
                "empty-dictionary-version-field:1129"
            }));
        });
    }

    [Test]
    public void SelectionReportsMissingAndConflictingRequiredVersions()
    {
        DictionarySelectionResult missingBegin = DictionaryVersionSelector.SelectDictionaryIdentities(new FixMessage(), new FixDictionaryIdentity(), new FixDictionaryIdentity(), "");
        DictionarySelectionResult missingApplication = DictionaryVersionSelector.SelectDictionaryIdentities(FixtMessage(), TransportIdentity(), new FixDictionaryIdentity(), "");
        FixMessage conflicting = FixtMessage();
        conflicting.HeaderFields[0] = Field(FixTags.BeginString, FixBeginStrings.Fix44);
        DictionarySelectionResult conflictingBegin = DictionaryVersionSelector.SelectDictionaryIdentities(conflicting, TransportIdentity(), ApplicationIdentity(FixApplicationVersions.Fix50, 0, ""), "");

        Assert.Multiple(() =>
        {
            Assert.That(missingBegin.Success, Is.False);
            Assert.That(missingBegin.Diagnostics.ConvertAll(diagnostic => diagnostic.Code), Is.EqualTo(new[] { "missing-begin-string" }));
            Assert.That(missingApplication.Success, Is.False);
            Assert.That(missingApplication.Diagnostics.ConvertAll(diagnostic => diagnostic.Code), Is.EqualTo(new[] { "missing-application-version" }));
            Assert.That(conflictingBegin.Success, Is.False);
            Assert.That(conflictingBegin.TransportIdentity.BeginString, Is.EqualTo(FixBeginStrings.Fixt11));
            Assert.That(conflictingBegin.ApplicationIdentity.ApplicationVersion, Is.EqualTo(FixApplicationVersions.Fix50));
            Assert.That(conflictingBegin.Diagnostics.ConvertAll(diagnostic => diagnostic.Code), Is.EqualTo(new[] { "conflicting-begin-string" }));
        });
    }

    [TestCase("1", 1)]
    [TestCase("309", 309)]
    [TestCase("2147483647", int.MaxValue)]
    [TestCase("", -1)]
    [TestCase("0", 0)]
    [TestCase("-1", -1)]
    [TestCase("2147483648", -1)]
    public void ExtensionPackParsingIsStrictAndOverflowSafe(string text, int expected)
    {
        Assert.That(DictionaryVersionSelector.ParseExtensionPack(Encoding.ASCII.GetBytes(text)), Is.EqualTo(expected));
    }

    private static FixMessage FixtMessage()
    {
        return new FixMessage
        {
            BeginString = FixBeginStrings.Fixt11,
            HeaderFields = [Field(FixTags.BeginString, FixBeginStrings.Fixt11)]
        };
    }

    private static FixDictionaryIdentity TransportIdentity()
    {
        return new FixDictionaryIdentity
        {
            Name = "transport",
            BeginString = FixBeginStrings.Fixt11,
            Source = "transport.xml"
        };
    }

    private static FixDictionaryIdentity ApplicationIdentity(string version, int extensionPack, string customVersion)
    {
        return new FixDictionaryIdentity
        {
            Name = "application",
            ApplicationVersion = version,
            HasExtensionPack = extensionPack > 0,
            ExtensionPack = extensionPack,
            CustomVersion = customVersion,
            Source = "application.xml"
        };
    }

    private static FixField Field(int tag, string value)
    {
        return new FixField { Tag = tag, Value = Encoding.ASCII.GetBytes(value) };
    }
}
