using System.Text.Json;

namespace jmap.Tests;

public class MethodContextTests
{
    [Test]
    public void AccountChecksDistinguishTargetSourceCapabilityAndReadOnlyFailures()
    {
        MethodContext context = new MethodContext();
        MethodRegistry registry = new MethodRegistry(new JmapServerServices());
        RegisteredJmapMethod get = registry.FindMethod(JmapMethods.EmailGet)!;
        RegisteredJmapMethod set = registry.FindMethod(JmapMethods.EmailSet)!;
        RegisteredJmapMethod copy = registry.FindMethod(JmapMethods.EmailCopy)!;
        JmapSession missing = new JmapSession();
        JmapSession unsupported = SessionWithAccount("target", false, false);
        JmapSession readOnly = SessionWithAccount("target", true, true);
        JmapSession writable = SessionWithAccount("target", true, false);

        JmapMethodError? notFound = context.ValidateBeforeDispatch(
            Invocation(JmapMethods.EmailGet, "{\"accountId\":\"missing\"}"), get, missing);
        JmapMethodError? unsupportedError = context.ValidateBeforeDispatch(
            Invocation(JmapMethods.EmailGet, "{\"accountId\":\"target\"}"), get,
            unsupported);
        JmapMethodError? readOnlyError = context.ValidateBeforeDispatch(
            Invocation(JmapMethods.EmailSet, "{\"accountId\":\"target\"}"), set,
            readOnly);
        JmapMethodError? readAllowed = context.ValidateBeforeDispatch(
            Invocation(JmapMethods.EmailGet, "{\"accountId\":\"target\"}"), get,
            readOnly);
        JmapMethodError? sourceMissing = context.ValidateBeforeDispatch(
            Invocation(JmapMethods.EmailCopy,
                "{\"fromAccountId\":\"source\",\"accountId\":\"target\"}"),
            copy, writable);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(notFound!.Kind, Is.EqualTo(JmapMethodErrorKind.AccountNotFound));
            Assert.That(unsupportedError!.Kind,
                Is.EqualTo(JmapMethodErrorKind.AccountNotSupportedByMethod));
            Assert.That(readOnlyError!.Kind, Is.EqualTo(JmapMethodErrorKind.AccountReadOnly));
            Assert.That(readAllowed, Is.Null);
            Assert.That(sourceMissing!.Kind,
                Is.EqualTo(JmapMethodErrorKind.FromAccountNotFound));
        }
    }

    [Test]
    public void MalformedTypedAccountCapabilityIsAProtocolFailure()
    {
        MethodContext context = new MethodContext();
        MethodRegistry registry = new MethodRegistry(new JmapServerServices());
        RegisteredJmapMethod method = registry.FindMethod(JmapMethods.EmailGet)!;
        JmapSession session = new JmapSession();
        JmapAccount account = new JmapAccount();
        account.AccountCapabilities.Add(JmapCapabilities.Mail, new JmapCapability());
        session.Accounts.Add("account", account);

        Assert.That(() => context.ValidateBeforeDispatch(
            Invocation(JmapMethods.EmailGet, "{\"accountId\":\"account\"}"),
            method, session), Throws.TypeOf<JmapProtocolException>());
    }

    [Test]
    public void SuccessEnvelopeMustMatchAccountStateAndImplicitResponseRules()
    {
        MethodContext context = new MethodContext();
        MethodRegistry registry = new MethodRegistry(new JmapServerServices());
        RegisteredJmapMethod method = registry.FindMethod(JmapMethods.EmailSet)!;
        JmapInvocation invocation = Invocation(JmapMethods.EmailSet,
            "{\"accountId\":\"account\",\"ifInState\":\"s0\"}");
        MethodDispatchResult valid = Result(JmapMethods.EmailSet,
            "{\"accountId\":\"account\",\"oldState\":\"s0\",\"newState\":\"s1\"}");
        MethodDispatchResult wrongAccount = Result(JmapMethods.EmailSet,
            "{\"accountId\":\"other\",\"oldState\":\"s0\",\"newState\":\"s1\"}");
        MethodDispatchResult wrongState = Result(JmapMethods.EmailSet,
            "{\"accountId\":\"account\",\"oldState\":\"other\",\"newState\":\"s1\"}");
        MethodDispatchResult unexpectedImplicit = Result(JmapMethods.EmailSet,
            "{\"accountId\":\"account\",\"oldState\":\"s0\",\"newState\":\"s1\"}");
        unexpectedImplicit.Responses.Add(Response(
            JmapMethods.CoreEcho, "{}", "call"));

        Assert.That(() => context.ValidateAfterDispatch(invocation, method, valid),
            Throws.Nothing);
        Assert.That(() => context.ValidateAfterDispatch(invocation, method, wrongAccount),
            Throws.TypeOf<JmapProtocolException>());
        Assert.That(() => context.ValidateAfterDispatch(invocation, method, wrongState),
            Throws.TypeOf<JmapProtocolException>());
        Assert.That(() => context.ValidateAfterDispatch(
            invocation, method, unexpectedImplicit), Throws.TypeOf<JmapProtocolException>());
    }

    [Test]
    public void MailboxRightEvaluationMapsEveryRightAndFailsClosed()
    {
        MethodContext context = new MethodContext();
        MailboxRights rights = new MailboxRights
        {
            MayReadItems = true,
            MayAddItems = true,
            MayRemoveItems = true,
            MaySetSeen = true,
            MaySetKeywords = true,
            MayCreateChild = true,
            MayRename = true,
            MayDelete = true,
            MaySubmit = true
        };
        Mailbox mailbox = new Mailbox { Id = "mailbox", MyRights = rights };

        using (Assert.EnterMultipleScope())
        {
            Assert.That(context.HasMailboxRight(mailbox, MailboxRightKind.ReadItems), Is.True);
            Assert.That(context.HasMailboxRight(mailbox, MailboxRightKind.AddItems), Is.True);
            Assert.That(context.HasMailboxRight(mailbox, MailboxRightKind.RemoveItems), Is.True);
            Assert.That(context.HasMailboxRight(mailbox, MailboxRightKind.SetSeen), Is.True);
            Assert.That(context.HasMailboxRight(mailbox, MailboxRightKind.SetKeywords), Is.True);
            Assert.That(context.HasMailboxRight(mailbox, MailboxRightKind.CreateChild), Is.True);
            Assert.That(context.HasMailboxRight(mailbox, MailboxRightKind.Rename), Is.True);
            Assert.That(context.HasMailboxRight(mailbox, MailboxRightKind.Delete), Is.True);
            Assert.That(context.HasMailboxRight(mailbox, MailboxRightKind.Submit), Is.True);
            Assert.That(context.HasMailboxRight(null, MailboxRightKind.ReadItems), Is.False);
            Assert.That(context.HasMailboxRight(new Mailbox(), MailboxRightKind.AddItems), Is.False);
        }

        rights.MayDelete = false;
        Assert.That(context.HasMailboxRight(mailbox, MailboxRightKind.Delete), Is.False);
    }

    private static JmapSession SessionWithAccount(
        string accountId,
        bool supportsMail,
        bool readOnly)
    {
        JmapSession session = new JmapSession();
        JmapAccount account = new JmapAccount();
        account.IsReadOnly = readOnly;
        if (supportsMail)
        {
            JmapCapability capability = new JmapCapability();
            capability.Mail = new JmapMailCapabilities();
            account.AccountCapabilities.Add(JmapCapabilities.Mail, capability);
        }

        session.Accounts.Add(accountId, account);
        return session;
    }

    private static MethodDispatchResult Result(string name, string arguments)
    {
        MethodDispatchResult result = new MethodDispatchResult();
        result.Responses.Add(Response(name, arguments, "call"));
        return result;
    }

    private static JmapInvocation Invocation(string name, string arguments)
    {
        return Response(name, arguments, "call");
    }

    private static JmapInvocation Response(
        string name,
        string arguments,
        string callId)
    {
        JmapInvocation invocation = new JmapInvocation();
        invocation.Name = name;
        invocation.Arguments = Json(arguments);
        invocation.CallId = callId;
        return invocation;
    }

    private static JsonElement Json(string value)
    {
        return JsonDocument.Parse(value).RootElement.Clone();
    }
}
