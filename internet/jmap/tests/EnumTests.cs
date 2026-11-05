namespace jmap.Tests;

public class EnumTests
{
    [TestCase(JmapErrorCodes.UnknownCapabilityProblemType, JmapProblemKind.UnknownCapability)]
    [TestCase(JmapErrorCodes.NotJsonProblemType, JmapProblemKind.NotJson)]
    [TestCase(JmapErrorCodes.NotRequestProblemType, JmapProblemKind.NotRequest)]
    [TestCase(JmapErrorCodes.LimitProblemType, JmapProblemKind.Limit)]
    public void ProblemKindsMapFromRfcProblemTypeUris(string token, JmapProblemKind expected)
    {
        Assert.That(new JmapJsonCodec().ClassifyProblemType(token), Is.EqualTo(expected));
    }

    [TestCase(JmapErrorCodes.ServerUnavailable, JmapMethodErrorKind.ServerUnavailable)]
    [TestCase(JmapErrorCodes.ServerFail, JmapMethodErrorKind.ServerFail)]
    [TestCase(JmapErrorCodes.ServerPartialFail, JmapMethodErrorKind.ServerPartialFail)]
    [TestCase(JmapErrorCodes.UnknownMethod, JmapMethodErrorKind.UnknownMethod)]
    [TestCase(JmapErrorCodes.InvalidArguments, JmapMethodErrorKind.InvalidArguments)]
    [TestCase(JmapErrorCodes.InvalidResultReference, JmapMethodErrorKind.InvalidResultReference)]
    [TestCase(JmapErrorCodes.Forbidden, JmapMethodErrorKind.Forbidden)]
    [TestCase(JmapErrorCodes.AccountNotFound, JmapMethodErrorKind.AccountNotFound)]
    [TestCase(JmapErrorCodes.AccountNotSupportedByMethod, JmapMethodErrorKind.AccountNotSupportedByMethod)]
    [TestCase(JmapErrorCodes.AccountReadOnly, JmapMethodErrorKind.AccountReadOnly)]
    [TestCase(JmapErrorCodes.RequestTooLarge, JmapMethodErrorKind.RequestTooLarge)]
    [TestCase(JmapErrorCodes.CannotCalculateChanges, JmapMethodErrorKind.CannotCalculateChanges)]
    [TestCase(JmapErrorCodes.StateMismatch, JmapMethodErrorKind.StateMismatch)]
    [TestCase(JmapErrorCodes.AnchorNotFound, JmapMethodErrorKind.AnchorNotFound)]
    [TestCase(JmapErrorCodes.UnsupportedFilter, JmapMethodErrorKind.UnsupportedFilter)]
    [TestCase(JmapErrorCodes.UnsupportedSort, JmapMethodErrorKind.UnsupportedSort)]
    [TestCase(JmapErrorCodes.TooManyChanges, JmapMethodErrorKind.TooManyChanges)]
    [TestCase(JmapErrorCodes.FromAccountNotFound, JmapMethodErrorKind.FromAccountNotFound)]
    [TestCase(JmapErrorCodes.FromAccountNotSupportedByMethod, JmapMethodErrorKind.FromAccountNotSupportedByMethod)]
    public void MethodErrorKindsMapFromRfcErrorTokens(string token, JmapMethodErrorKind expected)
    {
        Assert.That(new JmapJsonCodec().ClassifyMethodErrorType(token), Is.EqualTo(expected));
    }

    [TestCase(JmapErrorCodes.Forbidden, JmapSetErrorKind.Forbidden)]
    [TestCase(JmapErrorCodes.OverQuota, JmapSetErrorKind.OverQuota)]
    [TestCase(JmapErrorCodes.TooLarge, JmapSetErrorKind.TooLarge)]
    [TestCase(JmapErrorCodes.RateLimit, JmapSetErrorKind.RateLimit)]
    [TestCase(JmapErrorCodes.NotFound, JmapSetErrorKind.NotFound)]
    [TestCase(JmapErrorCodes.InvalidPatch, JmapSetErrorKind.InvalidPatch)]
    [TestCase(JmapErrorCodes.WillDestroy, JmapSetErrorKind.WillDestroy)]
    [TestCase(JmapErrorCodes.InvalidProperties, JmapSetErrorKind.InvalidProperties)]
    [TestCase(JmapErrorCodes.Singleton, JmapSetErrorKind.Singleton)]
    [TestCase(JmapErrorCodes.AlreadyExists, JmapSetErrorKind.AlreadyExists)]
    [TestCase(JmapErrorCodes.MailboxHasChild, JmapSetErrorKind.MailboxHasChild)]
    [TestCase(JmapErrorCodes.MailboxHasEmail, JmapSetErrorKind.MailboxHasEmail)]
    [TestCase(JmapErrorCodes.BlobNotFound, JmapSetErrorKind.BlobNotFound)]
    [TestCase(JmapErrorCodes.TooManyKeywords, JmapSetErrorKind.TooManyKeywords)]
    [TestCase(JmapErrorCodes.TooManyMailboxes, JmapSetErrorKind.TooManyMailboxes)]
    [TestCase(JmapErrorCodes.ForbiddenFrom, JmapSetErrorKind.ForbiddenFrom)]
    [TestCase(JmapErrorCodes.InvalidEmail, JmapSetErrorKind.InvalidEmail)]
    [TestCase(JmapErrorCodes.TooManyRecipients, JmapSetErrorKind.TooManyRecipients)]
    [TestCase(JmapErrorCodes.NoRecipients, JmapSetErrorKind.NoRecipients)]
    [TestCase(JmapErrorCodes.InvalidRecipients, JmapSetErrorKind.InvalidRecipients)]
    [TestCase(JmapErrorCodes.ForbiddenMailFrom, JmapSetErrorKind.ForbiddenMailFrom)]
    [TestCase(JmapErrorCodes.ForbiddenToSend, JmapSetErrorKind.ForbiddenToSend)]
    [TestCase(JmapErrorCodes.CannotUnsend, JmapSetErrorKind.CannotUnsend)]
    public void SetErrorKindsMapFromRfcErrorTokens(string token, JmapSetErrorKind expected)
    {
        Assert.That(new JmapJsonCodec().ClassifySetErrorType(token), Is.EqualTo(expected));
    }

    [TestCase("AND", JmapFilterOperator.And)]
    [TestCase("OR", JmapFilterOperator.Or)]
    [TestCase("NOT", JmapFilterOperator.Not)]
    public void FilterOperatorsMapFromRfcTokens(string token, JmapFilterOperator expected)
    {
        Assert.That(new JmapJsonCodec().ClassifyFilterOperator(token), Is.EqualTo(expected));
    }

    [TestCase(JmapProperties.HeaderAsRawSuffix, JmapHeaderForm.Raw)]
    [TestCase(JmapProperties.HeaderAsTextSuffix, JmapHeaderForm.Text)]
    [TestCase(JmapProperties.HeaderAsAddressesSuffix, JmapHeaderForm.Addresses)]
    [TestCase(JmapProperties.HeaderAsGroupedAddressesSuffix, JmapHeaderForm.GroupedAddresses)]
    [TestCase(JmapProperties.HeaderAsMessageIdsSuffix, JmapHeaderForm.MessageIds)]
    [TestCase(JmapProperties.HeaderAsDateSuffix, JmapHeaderForm.Date)]
    [TestCase(JmapProperties.HeaderAsUrlsSuffix, JmapHeaderForm.Urls)]
    public void HeaderFormsMapFromRfcPropertySuffixes(string token, JmapHeaderForm expected)
    {
        Assert.That(new JmapJsonCodec().ClassifyHeaderForm(token), Is.EqualTo(expected));
    }

    [TestCase("state", JmapEventKind.State)]
    [TestCase("ping", JmapEventKind.Ping)]
    public void EventKindsMapFromRfcEventNames(string token, JmapEventKind expected)
    {
        Assert.That(new JmapJsonCodec().ClassifyEventName(token), Is.EqualTo(expected));
    }

    [TestCase("pending", JmapSubmissionUndoStatus.Pending)]
    [TestCase("final", JmapSubmissionUndoStatus.Final)]
    [TestCase("canceled", JmapSubmissionUndoStatus.Canceled)]
    public void SubmissionUndoStatusesMapFromRfcTokens(
        string token,
        JmapSubmissionUndoStatus expected)
    {
        Assert.That(new JmapJsonCodec().ClassifySubmissionUndoStatus(token), Is.EqualTo(expected));
    }

    [Test]
    public void ExtensibleEnumsPreserveUnknownClassificationAndWireTokensAreCaseSensitive()
    {
        IJmapCodec codec = new JmapJsonCodec();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(codec.ClassifyProblemType("https://vendor.example/problem"),
                Is.EqualTo(JmapProblemKind.Unknown));
            Assert.That(codec.ClassifyMethodErrorType("vendorMethodError"),
                Is.EqualTo(JmapMethodErrorKind.Unknown));
            Assert.That(codec.ClassifySetErrorType("vendorSetError"),
                Is.EqualTo(JmapSetErrorKind.Unknown));
            Assert.That(codec.ClassifyEventName("vendor-event"),
                Is.EqualTo(JmapEventKind.Unknown));
            Assert.That(codec.ClassifyProblemType("urn:ietf:params:jmap:error:notjson"),
                Is.EqualTo(JmapProblemKind.Unknown));
            Assert.That(codec.ClassifyMethodErrorType("ServerFail"),
                Is.EqualTo(JmapMethodErrorKind.Unknown));
            Assert.That(codec.ClassifySetErrorType("NotFound"),
                Is.EqualTo(JmapSetErrorKind.Unknown));
            Assert.That(codec.ClassifyEventName("State"),
                Is.EqualTo(JmapEventKind.Unknown));
        }
    }

    [Test]
    public void ClosedEnumsRejectUnknownAndCaseChangedWireTokens()
    {
        IJmapCodec codec = new JmapJsonCodec();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(() => codec.ClassifyFilterOperator("XOR"),
                Throws.TypeOf<JmapProtocolException>());
            Assert.That(() => codec.ClassifyFilterOperator("and"),
                Throws.TypeOf<JmapProtocolException>());
            Assert.That(() => codec.ClassifyHeaderForm(":asFuture"),
                Throws.TypeOf<JmapValidationException>());
            Assert.That(() => codec.ClassifyHeaderForm(":astext"),
                Throws.TypeOf<JmapValidationException>());
            Assert.That(() => codec.ClassifySubmissionUndoStatus("recalled"),
                Throws.TypeOf<JmapProtocolException>());
            Assert.That(() => codec.ClassifySubmissionUndoStatus("Pending"),
                Throws.TypeOf<JmapProtocolException>());
        }
    }

    [Test]
    public void ProblemKindsHaveStableInt32Values()
    {
        AssertEnum(
            typeof(JmapProblemKind),
            "Unknown,UnknownCapability,NotJson,NotRequest,Limit",
            new[] { 0, 1, 2, 3, 4 });
    }

    [Test]
    public void MethodErrorKindsHaveStableInt32Values()
    {
        AssertEnum(
            typeof(JmapMethodErrorKind),
            "Unknown,ServerUnavailable,ServerFail,ServerPartialFail,UnknownMethod,InvalidArguments,InvalidResultReference,Forbidden,AccountNotFound,AccountNotSupportedByMethod,AccountReadOnly,RequestTooLarge,CannotCalculateChanges,StateMismatch,AnchorNotFound,UnsupportedFilter,UnsupportedSort,TooManyChanges,FromAccountNotFound,FromAccountNotSupportedByMethod",
            new[] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19 });
    }

    [Test]
    public void SetErrorKindsHaveStableInt32Values()
    {
        AssertEnum(
            typeof(JmapSetErrorKind),
            "Unknown,Forbidden,OverQuota,TooLarge,RateLimit,NotFound,InvalidPatch,WillDestroy,InvalidProperties,Singleton,AlreadyExists,MailboxHasChild,MailboxHasEmail,BlobNotFound,TooManyKeywords,TooManyMailboxes,ForbiddenFrom,InvalidEmail,TooManyRecipients,NoRecipients,InvalidRecipients,ForbiddenMailFrom,ForbiddenToSend,CannotUnsend",
            new[] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20, 21, 22, 23 });
    }

    [Test]
    public void FilterOperatorsHaveStableInt32Values()
    {
        AssertEnum(
            typeof(JmapFilterOperator),
            "And,Or,Not",
            new[] { 0, 1, 2 });
    }

    [Test]
    public void HeaderFormsHaveStableInt32Values()
    {
        AssertEnum(
            typeof(JmapHeaderForm),
            "Raw,Text,Addresses,GroupedAddresses,MessageIds,Date,Urls",
            new[] { 0, 1, 2, 3, 4, 5, 6 });
    }

    [Test]
    public void EventKindsHaveStableInt32Values()
    {
        AssertEnum(
            typeof(JmapEventKind),
            "Unknown,State,Ping,Comment",
            new[] { 0, 1, 2, 3 });
    }

    [Test]
    public void SubmissionUndoStatusesHaveStableInt32Values()
    {
        AssertEnum(
            typeof(JmapSubmissionUndoStatus),
            "Pending,Final,Canceled",
            new[] { 0, 1, 2 });
    }

    [Test]
    public void CodecContractUsesEveryPublicProtocolEnum()
    {
        Type[] returnTypes = typeof(IJmapCodec)
            .GetMethods()
            .Select(method => method.ReturnType)
            .ToArray();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(returnTypes, Does.Contain(typeof(JmapProblemKind)));
            Assert.That(returnTypes, Does.Contain(typeof(JmapMethodErrorKind)));
            Assert.That(returnTypes, Does.Contain(typeof(JmapSetErrorKind)));
            Assert.That(returnTypes, Does.Contain(typeof(JmapFilterOperator)));
            Assert.That(returnTypes, Does.Contain(typeof(JmapHeaderForm)));
            Assert.That(returnTypes, Does.Contain(typeof(JmapEventKind)));
            Assert.That(returnTypes, Does.Contain(typeof(JmapSubmissionUndoStatus)));
        }
    }

    private static void AssertEnum(Type type, string commaSeparatedNames, int[] expectedValues)
    {
        string[] expectedNames = commaSeparatedNames.Split(',');
        int[] actualValues = Enum
            .GetValues(type)
            .Cast<object>()
            .Select(value => Convert.ToInt32(value))
            .ToArray();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(Enum.GetUnderlyingType(type), Is.EqualTo(typeof(int)));
            Assert.That(Enum.GetNames(type), Is.EqualTo(expectedNames));
            Assert.That(actualValues, Is.EqualTo(expectedValues));
        }
    }
}
