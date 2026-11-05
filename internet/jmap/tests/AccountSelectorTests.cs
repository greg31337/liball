namespace jmap.Tests;

public class AccountSelectorTests
{
    [Test]
    public void ExplicitThenConfiguredThenPrimaryAccountSelectionUsesStablePrecedence()
    {
        AccountSelector selector = new AccountSelector();
        JmapSession session = Session();
        AddAccount(session, "explicit", false, true, false);
        AddAccount(session, "configured", false, true, false);
        AddAccount(session, "primary", false, true, false);
        session.PrimaryAccounts[JmapCapabilities.Mail] = "primary";
        JmapClientOptions options = new JmapClientOptions
        {
            DefaultAccountId = "configured"
        };

        string explicitResult = selector.SelectAccountId(
            session, options, "explicit", JmapCapabilities.Mail, JmapMethods.EmailGet, false);
        string configuredResult = selector.SelectAccountId(
            session, options, null, JmapCapabilities.Mail, JmapMethods.EmailGet, false);
        options.DefaultAccountId = null;
        string primaryResult = selector.SelectAccountId(
            session, options, null, null, JmapMethods.EmailGet, false);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(explicitResult, Is.EqualTo("explicit"));
            Assert.That(configuredResult, Is.EqualTo("configured"));
            Assert.That(primaryResult, Is.EqualTo("primary"));
        }
    }

    [Test]
    public void PrimaryAccountLookupIsCapabilitySpecificAndReturnsNullWhenUnmapped()
    {
        AccountSelector selector = new AccountSelector();
        JmapSession session = Session();
        AddAccount(session, "mail-account", false, true, false);
        AddAccount(session, "submission-account", false, false, true);
        session.PrimaryAccounts[JmapCapabilities.Mail] = "mail-account";
        session.PrimaryAccounts[JmapCapabilities.Submission] = "submission-account";

        string? mail = selector.FindPrimaryAccountId(session, JmapCapabilities.Mail);
        string? submission = selector.FindPrimaryAccountId(session, JmapCapabilities.Submission);
        string? vacation = selector.FindPrimaryAccountId(session, JmapCapabilities.VacationResponse);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(mail, Is.EqualTo("mail-account"));
            Assert.That(submission, Is.EqualTo("submission-account"));
            Assert.That(vacation, Is.Null);
        }
    }

    [Test]
    public void SelectionRejectsMissingAndUnsupportedAccountsWithOperationContext()
    {
        AccountSelector selector = new AccountSelector();
        JmapSession session = Session();
        AddAccount(session, "mail-only", false, true, false);
        JmapClientOptions options = new JmapClientOptions();

        JmapCapabilityException missing = Assert.Throws<JmapCapabilityException>(() =>
            selector.SelectAccountId(session, options, "missing", JmapCapabilities.Mail,
                JmapMethods.EmailGet, false))!;
        JmapCapabilityException unsupported = Assert.Throws<JmapCapabilityException>(() =>
            selector.SelectAccountId(session, options, "mail-only", JmapCapabilities.Submission,
                JmapMethods.EmailSubmissionGet, false))!;
        JmapCapabilityException noPrimary = Assert.Throws<JmapCapabilityException>(() =>
            selector.SelectAccountId(session, options, null, JmapCapabilities.Mail,
                JmapMethods.EmailGet, false))!;

        using (Assert.EnterMultipleScope())
        {
            Assert.That(missing.Capability, Is.EqualTo(JmapCapabilities.Mail));
            Assert.That(missing.MethodName, Is.EqualTo(JmapMethods.EmailGet));
            Assert.That(missing.AccountId, Is.EqualTo("missing"));
            Assert.That(unsupported.Capability, Is.EqualTo(JmapCapabilities.Submission));
            Assert.That(unsupported.MethodName, Is.EqualTo(JmapMethods.EmailSubmissionGet));
            Assert.That(unsupported.AccountId, Is.EqualTo("mail-only"));
            Assert.That(noPrimary.Capability, Is.EqualTo(JmapCapabilities.Mail));
            Assert.That(noPrimary.AccountId, Is.Null);
        }
    }

    [Test]
    public void WritableSelectionRejectsReadOnlyAccountWhileReadSelectionAllowsIt()
    {
        AccountSelector selector = new AccountSelector();
        JmapSession session = Session();
        AddAccount(session, "archive", true, true, false);
        JmapClientOptions options = new JmapClientOptions();

        string readable = selector.SelectAccountId(
            session, options, "archive", JmapCapabilities.Mail, JmapMethods.EmailGet, false);
        JmapCapabilityException readOnly = Assert.Throws<JmapCapabilityException>(() =>
            selector.SelectAccountId(session, options, "archive", JmapCapabilities.Mail,
                JmapMethods.EmailSet, true))!;

        using (Assert.EnterMultipleScope())
        {
            Assert.That(readable, Is.EqualTo("archive"));
            Assert.That(readOnly.Capability, Is.EqualTo(JmapCapabilities.Mail));
            Assert.That(readOnly.MethodName, Is.EqualTo(JmapMethods.EmailSet));
            Assert.That(readOnly.AccountId, Is.EqualTo("archive"));
            Assert.That(readOnly.Message, Does.Contain("read-only"));
        }
    }

    [Test]
    public void ExplicitAccountOverridesAnInvalidUnusedConfiguredDefault()
    {
        AccountSelector selector = new AccountSelector();
        JmapSession session = Session();
        AddAccount(session, "explicit", false, true, false);
        JmapClientOptions options = new JmapClientOptions
        {
            DefaultAccountId = "invalid/account"
        };

        string selected = selector.SelectAccountId(
            session, options, "explicit", JmapCapabilities.Mail, JmapMethods.EmailGet, false);
        JmapValidationException invalidDefault = Assert.Throws<JmapValidationException>(() =>
            selector.SelectAccountId(session, options, null, JmapCapabilities.Mail,
                JmapMethods.EmailGet, false))!;

        using (Assert.EnterMultipleScope())
        {
            Assert.That(selected, Is.EqualTo("explicit"));
            Assert.That(invalidDefault.InvalidProperties, Is.EqualTo(new[] { "defaultAccountId" }));
        }
    }

    [Test]
    public void MalformedPrimaryMappingsAndCapabilityObjectsAreProtocolFailures()
    {
        AccountSelector selector = new AccountSelector();
        JmapSession missingAccount = Session();
        missingAccount.PrimaryAccounts[JmapCapabilities.Mail] = "missing";

        JmapSession unsupportedPrimary = Session();
        AddAccount(unsupportedPrimary, "submission-only", false, false, true);
        unsupportedPrimary.PrimaryAccounts[JmapCapabilities.Mail] = "submission-only";

        JmapSession malformedCapability = Session();
        JmapAccount account = new JmapAccount();
        account.AccountCapabilities[JmapCapabilities.Mail] = new JmapCapability();
        malformedCapability.Accounts["broken"] = account;

        using (Assert.EnterMultipleScope())
        {
            Assert.That(() => selector.FindPrimaryAccountId(missingAccount, JmapCapabilities.Mail),
                Throws.TypeOf<JmapProtocolException>());
            Assert.That(() => selector.FindPrimaryAccountId(unsupportedPrimary, JmapCapabilities.Mail),
                Throws.TypeOf<JmapProtocolException>());
            Assert.That(() => selector.SelectAccountId(malformedCapability, new JmapClientOptions(),
                "broken", JmapCapabilities.Mail, JmapMethods.EmailGet, false),
                Throws.TypeOf<JmapProtocolException>());
        }
    }

    [Test]
    public void SelectionValidatesIdentifiersCapabilitiesMethodsAndRequiredInputs()
    {
        AccountSelector selector = new AccountSelector();
        JmapSession session = Session();
        AddAccount(session, "account-1", false, true, false);
        JmapClientOptions options = new JmapClientOptions();

        JmapValidationException account = Assert.Throws<JmapValidationException>(() =>
            selector.SelectAccountId(session, options, "bad/account", JmapCapabilities.Mail,
                JmapMethods.EmailGet, false))!;
        JmapValidationException capability = Assert.Throws<JmapValidationException>(() =>
            selector.SelectAccountId(session, options, "account-1", "mail",
                JmapMethods.EmailGet, false))!;
        JmapValidationException method = Assert.Throws<JmapValidationException>(() =>
            selector.SelectAccountId(session, options, "account-1", JmapCapabilities.Mail,
                "", false))!;
        JmapValidationException missingSession = Assert.Throws<JmapValidationException>(() =>
            selector.SelectAccountId(null, options, "account-1", JmapCapabilities.Mail,
                JmapMethods.EmailGet, false))!;

        using (Assert.EnterMultipleScope())
        {
            Assert.That(account.InvalidProperties, Is.EqualTo(new[] { "accountId" }));
            Assert.That(capability.InvalidProperties, Is.EqualTo(new[] { "capability" }));
            Assert.That(method.InvalidProperties, Is.EqualTo(new[] { "methodName" }));
            Assert.That(missingSession.InvalidProperties, Is.EqualTo(new[] { "session" }));
        }
    }

    private static JmapSession Session()
    {
        JmapSession session = new JmapSession();
        session.Capabilities[JmapCapabilities.Core] = new JmapCapability
        {
            Core = new JmapCoreCapabilities()
        };
        session.Capabilities[JmapCapabilities.Mail] = new JmapCapability();
        session.Capabilities[JmapCapabilities.Submission] = new JmapCapability();
        session.Capabilities[JmapCapabilities.VacationResponse] = new JmapCapability();
        return session;
    }

    private static void AddAccount(
        JmapSession session,
        string accountId,
        bool readOnly,
        bool supportsMail,
        bool supportsSubmission)
    {
        JmapAccount account = new JmapAccount();
        account.IsReadOnly = readOnly;
        if (supportsMail)
        {
            account.AccountCapabilities[JmapCapabilities.Mail] = new JmapCapability
            {
                Mail = new JmapMailCapabilities()
            };
        }

        if (supportsSubmission)
        {
            account.AccountCapabilities[JmapCapabilities.Submission] = new JmapCapability
            {
                Submission = new JmapSubmissionCapabilities()
            };
        }

        session.Accounts[accountId] = account;
    }
}
