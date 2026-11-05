using System.Text;
using NUnit.Framework;

namespace fix.Tests;

public class FixtDictionarySelectionIntegrationTests
{
    [Test]
    public void LogonDefaultSelectsResolvableTransportAndApplicationDictionaries()
    {
        SessionSettings settings = Settings("FIX50SP2", FixApplicationVersions.Fix50Sp2);
        FixMessage logon = FixtMessage(FixAdminMessageTypes.Logon);
        logon.BodyFields.Add(Field(FixTags.DefaultApplVerID, FixApplicationVersions.Fix50Sp1));

        DictionarySelectionResult selection = Select(logon, settings);
        EmbeddedDictionaryProvider provider = new EmbeddedDictionaryProvider(null);
        DictionaryLoadResult transport = provider.ResolveDictionary(selection.TransportIdentity);
        DictionaryLoadResult application = provider.ResolveDictionary(selection.ApplicationIdentity);

        Assert.Multiple(() =>
        {
            Assert.That(selection.Success, Is.True);
            Assert.That(selection.ApplicationIdentity.Name, Is.Empty);
            Assert.That(selection.ApplicationIdentity.ApplicationVersion, Is.EqualTo(FixApplicationVersions.Fix50Sp1));
            Assert.That(transport.Dictionary!.Identity.Name, Is.EqualTo("FIXT11"));
            Assert.That(application.Success, Is.True, string.Join(",", application.Diagnostics.ConvertAll(diagnostic => diagnostic.Code)));
            Assert.That(application.Dictionary!.Identity.Name, Is.EqualTo("FIX50SP1"));
            Assert.That(settings.ApplicationDictionary.Name, Is.EqualTo("FIX50SP2"));
            Assert.That(settings.ApplicationDictionary.ApplicationVersion, Is.EqualTo(FixApplicationVersions.Fix50Sp2));
        });
    }

    [Test]
    public void PerMessageApplicationVersionOverridesLogonDefaultWithoutMutatingSessionState()
    {
        SessionSettings settings = Settings("FIX50", FixApplicationVersions.Fix50);
        FixMessage logon = FixtMessage(FixAdminMessageTypes.Logon);
        logon.BodyFields.Add(Field(FixTags.DefaultApplVerID, FixApplicationVersions.Fix50));
        FixMessage order = FixtMessage("D");
        order.HeaderFields.Add(Field(FixTags.ApplVerID, FixApplicationVersions.Fix50Sp2));

        DictionarySelectionResult logonSelection = Select(logon, settings);
        DictionarySelectionResult orderSelection = Select(order, settings);
        EmbeddedDictionaryProvider provider = new EmbeddedDictionaryProvider(null);
        DictionaryLoadResult logonApplication = provider.ResolveDictionary(logonSelection.ApplicationIdentity);
        DictionaryLoadResult orderApplication = provider.ResolveDictionary(orderSelection.ApplicationIdentity);

        Assert.Multiple(() =>
        {
            Assert.That(logonApplication.Dictionary!.Identity.Name, Is.EqualTo("FIX50"));
            Assert.That(orderSelection.ApplicationIdentity.Name, Is.Empty);
            Assert.That(orderSelection.ApplicationIdentity.ApplicationVersion, Is.EqualTo(FixApplicationVersions.Fix50Sp2));
            Assert.That(orderApplication.Success, Is.True, string.Join(",", orderApplication.Diagnostics.ConvertAll(diagnostic => diagnostic.Code)));
            Assert.That(orderApplication.Dictionary!.Identity.Name, Is.EqualTo("FIX50SP2"));
            Assert.That(settings.ApplicationDictionary.Name, Is.EqualTo("FIX50"));
            Assert.That(settings.ApplicationDictionary.ApplicationVersion, Is.EqualTo(FixApplicationVersions.Fix50));
        });
    }

    [Test]
    public void CustomVersionSelectionRoutesTheQualifiedIdentityToRuntimeResolution()
    {
        SessionSettings settings = Settings("FIX50SP2", FixApplicationVersions.Fix50Sp2);
        FixMessage message = FixtMessage("D");
        message.HeaderFields.Add(Field(FixTags.ApplVerID, FixApplicationVersions.Fix50Sp2));
        message.HeaderFields.Add(Field(FixTags.ApplExtID, "309"));
        message.HeaderFields.Add(Field(FixTags.CstmApplVerID, "VENUE-ALPHA"));
        RecordingDictionaryProvider runtime = new RecordingDictionaryProvider();

        DictionarySelectionResult selection = Select(message, settings);
        DictionaryLoadResult resolved = new EmbeddedDictionaryProvider(runtime).ResolveDictionary(selection.ApplicationIdentity);

        Assert.Multiple(() =>
        {
            Assert.That(selection.Success, Is.True);
            Assert.That(selection.ApplicationIdentity.Name, Is.Empty);
            Assert.That(selection.ApplicationIdentity.ApplicationVersion, Is.EqualTo(FixApplicationVersions.Fix50Sp2));
            Assert.That(selection.ApplicationIdentity.HasExtensionPack, Is.True);
            Assert.That(selection.ApplicationIdentity.ExtensionPack, Is.EqualTo(309));
            Assert.That(selection.ApplicationIdentity.CustomVersion, Is.EqualTo("VENUE-ALPHA"));
            Assert.That(runtime.CallCount, Is.EqualTo(1));
            Assert.That(runtime.LastIdentity, Is.SameAs(selection.ApplicationIdentity));
            Assert.That(resolved.Success, Is.True);
            Assert.That(resolved.Dictionary!.Identity.Name, Is.EqualTo("runtime"));
        });
    }

    [Test]
    public void UnknownSelectedApplicationReturnsAStableMissingDictionaryResult()
    {
        SessionSettings settings = Settings("FIX50", FixApplicationVersions.Fix50);
        settings.ApplicationDictionary.Source = "session-alpha";
        FixMessage message = FixtMessage("D");
        message.HeaderFields.Add(Field(FixTags.ApplVerID, "99"));

        DictionarySelectionResult selection = Select(message, settings);
        EmbeddedDictionaryProvider provider = new EmbeddedDictionaryProvider(null);
        DictionaryLoadResult transport = provider.ResolveDictionary(selection.TransportIdentity);
        DictionaryLoadResult application = provider.ResolveDictionary(selection.ApplicationIdentity);

        Assert.Multiple(() =>
        {
            Assert.That(selection.Success, Is.True);
            Assert.That(selection.ApplicationIdentity.Name, Is.Empty);
            Assert.That(selection.ApplicationIdentity.ApplicationVersion, Is.EqualTo("99"));
            Assert.That(transport.Success, Is.True);
            Assert.That(transport.Dictionary!.Identity.Name, Is.EqualTo("FIXT11"));
            Assert.That(application.Success, Is.False);
            Assert.That(application.Dictionary, Is.Null);
            Assert.That(application.Diagnostics.ConvertAll(diagnostic => diagnostic.Code), Is.EqualTo(new[] { "dictionary-not-found" }));
            Assert.That(application.Diagnostics[0].Source, Is.EqualTo("session-alpha"));
        });
    }

    [Test]
    public void SimultaneousSessionsResolveIndependentApplicationVersions()
    {
        SessionSettings firstSettings = Settings("FIX50", FixApplicationVersions.Fix50);
        SessionSettings secondSettings = Settings("FIX50SP2", FixApplicationVersions.Fix50Sp2);
        string[] resolvedNames = new string[64];
        EmbeddedDictionaryProvider provider = new EmbeddedDictionaryProvider(null);

        Parallel.For(0, resolvedNames.Length, index =>
        {
            SessionSettings settings = index % 2 == 0 ? firstSettings : secondSettings;
            DictionarySelectionResult selection = Select(FixtMessage("D"), settings);
            DictionaryLoadResult resolved = provider.ResolveDictionary(selection.ApplicationIdentity);
            resolvedNames[index] = resolved.Dictionary!.Identity.Name;
        });

        Assert.Multiple(() =>
        {
            Assert.That(resolvedNames.Where((name, index) => index % 2 == 0), Is.All.EqualTo("FIX50"));
            Assert.That(resolvedNames.Where((name, index) => index % 2 != 0), Is.All.EqualTo("FIX50SP2"));
            Assert.That(firstSettings.ApplicationDictionary.ApplicationVersion, Is.EqualTo(FixApplicationVersions.Fix50));
            Assert.That(secondSettings.ApplicationDictionary.ApplicationVersion, Is.EqualTo(FixApplicationVersions.Fix50Sp2));
        });
    }

    private static DictionarySelectionResult Select(FixMessage message, SessionSettings settings)
    {
        return DictionaryVersionSelector.SelectDictionaryIdentities(message, settings.TransportDictionary, settings.ApplicationDictionary, settings.DefaultApplicationVersion);
    }

    private static SessionSettings Settings(string applicationName, string applicationVersion)
    {
        return new SessionSettings
        {
            TransportDictionary = new FixDictionaryIdentity { Name = "FIXT11", BeginString = FixBeginStrings.Fixt11 },
            ApplicationDictionary = new FixDictionaryIdentity { Name = applicationName, ApplicationVersion = applicationVersion },
            DefaultApplicationVersion = applicationVersion
        };
    }

    private static FixMessage FixtMessage(string msgType)
    {
        return new FixMessage
        {
            BeginString = FixBeginStrings.Fixt11,
            MsgType = msgType,
            HeaderFields = [Field(FixTags.BeginString, FixBeginStrings.Fixt11)]
        };
    }

    private static FixField Field(int tag, string value)
    {
        return new FixField { Tag = tag, Value = Encoding.ASCII.GetBytes(value) };
    }
}
