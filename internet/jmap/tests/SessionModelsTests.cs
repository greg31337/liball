using System.Text.Json;

namespace jmap.Tests;

public class SessionModelsTests
{
    [Test]
    public void NewSessionsHaveIndependentEmptyCollections()
    {
        JmapSession first = new JmapSession();
        JmapSession second = new JmapSession();

        first.PrimaryAccounts.Add(JmapCapabilities.Mail, "account-1");

        using (Assert.EnterMultipleScope())
        {
            Assert.That(first.Capabilities, Is.Empty);
            Assert.That(first.Accounts, Is.Empty);
            Assert.That(first.PrimaryAccounts, Has.Count.EqualTo(1));
            Assert.That(first.UnknownProperties, Is.Empty);
            Assert.That(second.PrimaryAccounts, Is.Empty);
            Assert.That(second.PrimaryAccounts, Is.Not.SameAs(first.PrimaryAccounts));
            Assert.That(first.Username, Is.Null);
            Assert.That(first.ApiUrl, Is.Null);
            Assert.That(first.DownloadUrl, Is.Null);
            Assert.That(first.UploadUrl, Is.Null);
            Assert.That(first.EventSourceUrl, Is.Null);
            Assert.That(first.State, Is.Null);
        }
    }

    [Test]
    public void SessionAssociatesAccountsAndTypedCapabilities()
    {
        JmapCoreCapabilities core = new JmapCoreCapabilities();
        core.MaxSizeUpload = 50000000;
        core.MaxConcurrentUpload = 4;
        core.MaxSizeRequest = 10000000;
        core.MaxConcurrentRequests = 4;
        core.MaxCallsInRequest = 16;
        core.MaxObjectsInGet = 500;
        core.MaxObjectsInSet = 500;
        core.CollationAlgorithms.Add("i;unicode-casemap");

        JmapMailCapabilities mail = new JmapMailCapabilities();
        JmapCapability coreCapability = new JmapCapability();
        coreCapability.Core = core;
        JmapCapability mailCapability = new JmapCapability();
        mailCapability.Mail = mail;

        JmapAccount account = new JmapAccount();
        account.Name = "Personal";
        account.IsPersonal = true;
        account.AccountCapabilities.Add(JmapCapabilities.Mail, mailCapability);

        JmapSession session = new JmapSession();
        session.Capabilities.Add(JmapCapabilities.Core, coreCapability);
        session.Accounts.Add("account-1", account);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(session.Capabilities[JmapCapabilities.Core].Core, Is.SameAs(core));
            Assert.That(session.Accounts["account-1"], Is.SameAs(account));
            Assert.That(account.AccountCapabilities[JmapCapabilities.Mail].Mail, Is.SameAs(mail));
            Assert.That(core.MaxSizeUpload, Is.EqualTo(50000000));
            Assert.That(core.MaxObjectsInSet, Is.EqualTo(500));
            Assert.That(core.CollationAlgorithms, Is.EqualTo(new[] { "i;unicode-casemap" }));
            Assert.That(core.UnknownProperties, Is.Empty);
            Assert.That(account.UnknownProperties, Is.Empty);
        }
    }

    [Test]
    public void MailCapabilityDistinguishesMissingNullAndNumericLimits()
    {
        JmapMailCapabilities missing = new JmapMailCapabilities();

        JmapMailCapabilities explicitNull = new JmapMailCapabilities();
        explicitNull.HasMaxMailboxesPerEmail = true;
        explicitNull.IsMaxMailboxesPerEmailNull = true;
        explicitNull.HasMaxMailboxDepth = true;
        explicitNull.IsMaxMailboxDepthNull = true;

        JmapMailCapabilities numeric = new JmapMailCapabilities();
        numeric.HasMaxMailboxesPerEmail = true;
        numeric.MaxMailboxesPerEmail = 25;
        numeric.HasMaxMailboxDepth = true;
        numeric.MaxMailboxDepth = 10;
        numeric.MaxSizeMailboxName = 255;
        numeric.MaxSizeAttachmentsPerEmail = 50000000;
        numeric.MayCreateTopLevelMailbox = true;
        numeric.EmailQuerySortOptions.Add(JmapProperties.ReceivedAt);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(missing.HasMaxMailboxesPerEmail, Is.False);
            Assert.That(missing.IsMaxMailboxesPerEmailNull, Is.False);
            Assert.That(missing.HasMaxMailboxDepth, Is.False);
            Assert.That(explicitNull.HasMaxMailboxesPerEmail, Is.True);
            Assert.That(explicitNull.IsMaxMailboxesPerEmailNull, Is.True);
            Assert.That(explicitNull.HasMaxMailboxDepth, Is.True);
            Assert.That(explicitNull.IsMaxMailboxDepthNull, Is.True);
            Assert.That(numeric.MaxMailboxesPerEmail, Is.EqualTo(25));
            Assert.That(numeric.IsMaxMailboxesPerEmailNull, Is.False);
            Assert.That(numeric.MaxMailboxDepth, Is.EqualTo(10));
            Assert.That(numeric.IsMaxMailboxDepthNull, Is.False);
            Assert.That(numeric.MaxSizeMailboxName, Is.EqualTo(255));
            Assert.That(numeric.MaxSizeAttachmentsPerEmail, Is.EqualTo(50000000));
            Assert.That(numeric.MayCreateTopLevelMailbox, Is.True);
            Assert.That(numeric.EmailQuerySortOptions, Is.EqualTo(new[] { JmapProperties.ReceivedAt }));
        }
    }

    [Test]
    public void SubmissionAndVacationCapabilitiesPreserveExtensionData()
    {
        JmapSubmissionCapabilities submission = new JmapSubmissionCapabilities();
        submission.MaxDelayedSend = 3600;
        submission.SubmissionExtensions.Add("FUTURERELEASE", new List<string> { "T", "+" });

        JmapVacationResponseCapabilities vacation = new JmapVacationResponseCapabilities();
        vacation.UnknownProperties.Add("vendorFlag", Json("true"));

        JmapCapability submissionCapability = new JmapCapability();
        submissionCapability.Submission = submission;
        JmapCapability vacationCapability = new JmapCapability();
        vacationCapability.VacationResponse = vacation;
        JmapCapability vendorCapability = new JmapCapability();
        vendorCapability.UnknownProperties.Add("maxWidgets", Json("42"));

        using (Assert.EnterMultipleScope())
        {
            Assert.That(submissionCapability.Submission, Is.SameAs(submission));
            Assert.That(submission.MaxDelayedSend, Is.EqualTo(3600));
            Assert.That(submission.SubmissionExtensions["FUTURERELEASE"], Is.EqualTo(new[] { "T", "+" }));
            Assert.That(submission.UnknownProperties, Is.Empty);
            Assert.That(vacationCapability.VacationResponse, Is.SameAs(vacation));
            Assert.That(vacation.UnknownProperties["vendorFlag"].GetBoolean(), Is.True);
            Assert.That(vendorCapability.UnknownProperties["maxWidgets"].GetInt32(), Is.EqualTo(42));
        }
    }

    [Test]
    public void SessionAndAccountPreserveUnknownProperties()
    {
        JmapSession session = new JmapSession();
        session.UnknownProperties.Add("vendorSession", Json("{\"enabled\":true}"));

        JmapAccount account = new JmapAccount();
        account.UnknownProperties.Add("vendorAccount", Json("[1,2,3]"));

        using (Assert.EnterMultipleScope())
        {
            Assert.That(session.UnknownProperties["vendorSession"].GetProperty("enabled").GetBoolean(), Is.True);
            Assert.That(account.UnknownProperties["vendorAccount"].GetArrayLength(), Is.EqualTo(3));
        }
    }

    [Test]
    public void ClientOptionsHaveSafeUsefulDefaultsAndRemainMutable()
    {
        JmapClientOptions options = new JmapClientOptions();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(options.DiscoveryOptions, Is.Not.Null);
            Assert.That(options.DiscoveryOptions.MaximumRedirects, Is.EqualTo(5));
            Assert.That(options.DiscoveryOptions.DnsTimeoutMilliseconds, Is.EqualTo(10000));
            Assert.That(options.DiscoveryOptions.AllowInsecureHttp, Is.False);
            Assert.That(options.DiscoveryOptions.SessionUrl, Is.Null);
            Assert.That(options.DiscoveryOptions.Username, Is.Null);
            Assert.That(options.ServiceLocator, Is.Null);
            Assert.That(options.DefaultAccountId, Is.Null);
            Assert.That(options.DefaultAccountCapability, Is.EqualTo(JmapCapabilities.Mail));
            Assert.That(options.RefreshSessionOnStateChange, Is.True);
        }

        options.DiscoveryOptions.SessionUrl = "https://example.test/.well-known/jmap";
        options.DiscoveryOptions.Username = "user@example.test";
        options.DefaultAccountId = "account-1";
        options.RefreshSessionOnStateChange = false;

        using (Assert.EnterMultipleScope())
        {
            Assert.That(options.DiscoveryOptions.SessionUrl, Is.EqualTo("https://example.test/.well-known/jmap"));
            Assert.That(options.DiscoveryOptions.Username, Is.EqualTo("user@example.test"));
            Assert.That(options.DefaultAccountId, Is.EqualTo("account-1"));
            Assert.That(options.RefreshSessionOnStateChange, Is.False);
        }
    }

    private static JsonElement Json(string value)
    {
        using JsonDocument document = JsonDocument.Parse(value);
        return document.RootElement.Clone();
    }
}
