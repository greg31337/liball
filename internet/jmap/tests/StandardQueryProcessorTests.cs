using System.Text.Json;

namespace jmap.Tests;

public class StandardQueryProcessorTests
{
    [Test]
    public void QueryPreservesStableBackendOrderAndReturnsRequestedMetadata()
    {
        StandardQueryProcessor processor = Processor(10, 4);
        JmapQueryRequest request = Request();
        request.HasPosition = true;
        request.Position = 1;
        request.HasLimit = true;
        request.Limit = 2;
        request.HasCalculateTotal = true;
        request.CalculateTotal = true;
        StandardQueryData data = Data("query-7", "z", "a", "m", "b");
        data.CanCalculateChanges = true;

        StandardQueryResult result = processor.ProcessQuery(request, data);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(result.IsSuccess, Is.True);
            Assert.That(result.Error, Is.Null);
            Assert.That(result.Response, Is.Not.Null);
            Assert.That(result.Response!.AccountId, Is.EqualTo("account-1"));
            Assert.That(result.Response.QueryState, Is.EqualTo("query-7"));
            Assert.That(result.Response.CanCalculateChanges, Is.True);
            Assert.That(result.Response.Position, Is.EqualTo(1));
            Assert.That(result.Response.Ids, Is.EqualTo(new[] { "a", "m" }));
            Assert.That(result.Response.HasTotal, Is.True);
            Assert.That(result.Response.Total, Is.EqualTo(4));
            Assert.That(result.Response.HasLimit, Is.False);
        }
    }

    [Test]
    public void NegativePositionsAreRelativeToTheEndAndClampAtZero()
    {
        StandardQueryProcessor processor = Processor(10, 4);
        StandardQueryData data = Data("state", "a", "b", "c", "d", "e");
        JmapQueryRequest relative = Request();
        relative.HasPosition = true;
        relative.Position = -2;
        JmapQueryRequest clamped = Request();
        clamped.HasPosition = true;
        clamped.Position = -20;
        JmapQueryRequest beyondEnd = Request();
        beyondEnd.HasPosition = true;
        beyondEnd.Position = 20;

        StandardQueryResult relativeResult = processor.ProcessQuery(relative, data);
        StandardQueryResult clampedResult = processor.ProcessQuery(clamped, data);
        StandardQueryResult beyondResult = processor.ProcessQuery(beyondEnd, data);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(relativeResult.Response!.Position, Is.EqualTo(3));
            Assert.That(relativeResult.Response.Ids, Is.EqualTo(new[] { "d", "e" }));
            Assert.That(clampedResult.Response!.Position, Is.Zero);
            Assert.That(clampedResult.Response.Ids, Is.EqualTo(new[] { "a", "b", "c", "d", "e" }));
            Assert.That(beyondResult.Response!.Position, Is.EqualTo(20));
            Assert.That(beyondResult.Response.Ids, Is.Empty);
        }
    }

    [Test]
    public void AnchorAndOffsetOverridePositionAndMissingAnchorReturnsStandardError()
    {
        StandardQueryProcessor processor = Processor(10, 4);
        StandardQueryData data = Data("state", "a", "b", "c", "d");
        JmapQueryRequest anchored = Request();
        anchored.HasPosition = true;
        anchored.Position = 3;
        anchored.Anchor = "c";
        anchored.HasAnchorOffset = true;
        anchored.AnchorOffset = -1;
        anchored.HasLimit = true;
        anchored.Limit = 2;
        JmapQueryRequest missing = Request();
        missing.Anchor = "absent";
        JmapQueryRequest clamped = Request();
        clamped.Anchor = "b";
        clamped.HasAnchorOffset = true;
        clamped.AnchorOffset = -20;

        StandardQueryResult anchoredResult = processor.ProcessQuery(anchored, data);
        StandardQueryResult missingResult = processor.ProcessQuery(missing, data);
        StandardQueryResult clampedResult = processor.ProcessQuery(clamped, data);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(anchoredResult.Response!.Position, Is.EqualTo(1));
            Assert.That(anchoredResult.Response.Ids, Is.EqualTo(new[] { "b", "c" }));
            AssertError(missingResult, JmapErrorCodes.AnchorNotFound, JmapMethodErrorKind.AnchorNotFound);
            Assert.That(clampedResult.Response!.Position, Is.Zero);
            Assert.That(clampedResult.Response.Ids, Is.EqualTo(new[] { "a", "b", "c", "d" }));
        }
    }

    [Test]
    public void ServerLimitIsReportedWhenDefaultedNullOrClamped()
    {
        StandardQueryProcessor processor = Processor(2, 4);
        StandardQueryData data = Data("state", "a", "b", "c");
        JmapQueryRequest omitted = Request();
        JmapQueryRequest nullLimit = Request();
        nullLimit.HasLimit = true;
        nullLimit.IsLimitNull = true;
        JmapQueryRequest oversized = Request();
        oversized.HasLimit = true;
        oversized.Limit = 20;
        JmapQueryRequest zero = Request();
        zero.HasLimit = true;
        zero.Limit = 0;

        StandardQueryResult omittedResult = processor.ProcessQuery(omitted, data);
        StandardQueryResult nullResult = processor.ProcessQuery(nullLimit, data);
        StandardQueryResult oversizedResult = processor.ProcessQuery(oversized, data);
        StandardQueryResult zeroResult = processor.ProcessQuery(zero, data);

        using (Assert.EnterMultipleScope())
        {
            AssertLimit(omittedResult, 2, new[] { "a", "b" });
            AssertLimit(nullResult, 2, new[] { "a", "b" });
            AssertLimit(oversizedResult, 2, new[] { "a", "b" });
            Assert.That(zeroResult.Response!.Ids, Is.Empty);
            Assert.That(zeroResult.Response.HasLimit, Is.False);
        }
    }

    [Test]
    public void TotalIsOmittedUnlessCalculationWasRequested()
    {
        JmapQueryRequest request = Request();
        request.CalculateTotal = true;

        StandardQueryResult result = Processor(10, 4).ProcessQuery(request, Data("state", "a", "b"));

        using (Assert.EnterMultipleScope())
        {
            Assert.That(result.Response!.HasTotal, Is.False);
            Assert.That(result.Response.Total, Is.Zero);
        }
    }

    [Test]
    public void RecursiveSupportedFilterIsAccepted()
    {
        JmapFilterNode leaf = Condition("name", JsonSerializer.SerializeToElement("Ada"));
        JmapFilterNode not = Operator(JmapFilterOperator.Not, leaf);
        JmapFilterNode root = Operator(JmapFilterOperator.And, not, Condition("role", JsonSerializer.SerializeToElement("inbox")));
        JmapQueryRequest request = Request();
        request.Filter = root;

        StandardQueryResult result = Processor(10, 3).ProcessQuery(request, Data("state", "a"));

        Assert.That(result.IsSuccess, Is.True);
    }

    [Test]
    public void UnsupportedFilterPropertyAndDepthReturnUnsupportedFilterWhileMalformedStructureIsInvalid()
    {
        StandardQueryProcessor processor = Processor(10, 2);
        JmapQueryRequest propertyRequest = Request();
        propertyRequest.Filter = Condition("unknown", JsonSerializer.SerializeToElement(true));
        JmapQueryRequest structureRequest = Request();
        structureRequest.Filter = Condition("name", JsonSerializer.SerializeToElement("Ada"));
        structureRequest.Filter.Conditions = new List<JmapFilterNode>();
        JmapQueryRequest depthRequest = Request();
        depthRequest.Filter = Operator(JmapFilterOperator.And, Operator(JmapFilterOperator.Or, Condition("name", JsonSerializer.SerializeToElement("Ada"))));

        StandardQueryResult propertyResult = processor.ProcessQuery(propertyRequest, Data("state"));
        StandardQueryResult structureResult = processor.ProcessQuery(structureRequest, Data("state"));
        StandardQueryResult depthResult = processor.ProcessQuery(depthRequest, Data("state"));

        using (Assert.EnterMultipleScope())
        {
            AssertError(propertyResult, JmapErrorCodes.UnsupportedFilter, JmapMethodErrorKind.UnsupportedFilter);
            AssertError(structureResult, JmapErrorCodes.InvalidArguments, JmapMethodErrorKind.InvalidArguments);
            AssertError(depthResult, JmapErrorCodes.UnsupportedFilter, JmapMethodErrorKind.UnsupportedFilter);
        }
    }

    [Test]
    public void ComparatorPropertiesExtensionsAndStringCollationsAreValidated()
    {
        StandardQueryProcessor processor = Processor(10, 4);
        JmapQueryRequest supported = Request();
        JmapComparator supportedComparator = Comparator("name", "i;unicode-casemap");
        supportedComparator.UnknownProperties.Add("keyword", JsonSerializer.SerializeToElement("$seen"));
        supported.Sort = new List<JmapComparator> { supportedComparator };
        JmapQueryRequest ignoredCollation = Request();
        ignoredCollation.Sort = new List<JmapComparator> { Comparator("size", "vendor-unknown") };
        JmapQueryRequest badCollation = Request();
        badCollation.Sort = new List<JmapComparator> { Comparator("name", "vendor-unknown") };
        JmapQueryRequest badProperty = Request();
        badProperty.Sort = new List<JmapComparator> { Comparator("createdAt", null) };
        JmapQueryRequest badExtension = Request();
        JmapComparator extensionComparator = Comparator("name", null);
        extensionComparator.UnknownProperties.Add("vendorFlag", JsonSerializer.SerializeToElement(true));
        badExtension.Sort = new List<JmapComparator> { extensionComparator };

        StandardQueryResult supportedResult = processor.ProcessQuery(supported, Data("state"));
        StandardQueryResult ignoredResult = processor.ProcessQuery(ignoredCollation, Data("state"));
        StandardQueryResult collationResult = processor.ProcessQuery(badCollation, Data("state"));
        StandardQueryResult propertyResult = processor.ProcessQuery(badProperty, Data("state"));
        StandardQueryResult extensionResult = processor.ProcessQuery(badExtension, Data("state"));

        using (Assert.EnterMultipleScope())
        {
            Assert.That(supportedResult.IsSuccess, Is.True);
            Assert.That(ignoredResult.IsSuccess, Is.True);
            AssertError(collationResult, JmapErrorCodes.UnsupportedSort, JmapMethodErrorKind.UnsupportedSort);
            AssertError(propertyResult, JmapErrorCodes.UnsupportedSort, JmapMethodErrorKind.UnsupportedSort);
            AssertError(extensionResult, JmapErrorCodes.UnsupportedSort, JmapMethodErrorKind.UnsupportedSort);
        }
    }

    [Test]
    public void InvalidCommonArgumentsReturnInvalidArguments()
    {
        StandardQueryProcessor processor = Processor(10, 4);
        StandardQueryData data = Data("state");
        JmapQueryRequest missingAccount = Request();
        missingAccount.AccountId = null;
        JmapQueryRequest badPosition = Request();
        badPosition.HasPosition = true;
        badPosition.Position = JmapProtocolLimits.MaximumInt + 1;
        JmapQueryRequest badOffset = Request();
        badOffset.HasAnchorOffset = true;
        badOffset.AnchorOffset = JmapProtocolLimits.MinimumInt - 1;
        JmapQueryRequest badLimit = Request();
        badLimit.HasLimit = true;
        badLimit.Limit = -1;
        JmapQueryRequest badFlags = Request();
        badFlags.IsLimitNull = true;

        StandardQueryResult nullResult = processor.ProcessQuery(null, data);
        StandardQueryResult accountResult = processor.ProcessQuery(missingAccount, data);
        StandardQueryResult positionResult = processor.ProcessQuery(badPosition, data);
        StandardQueryResult offsetResult = processor.ProcessQuery(badOffset, data);
        StandardQueryResult limitResult = processor.ProcessQuery(badLimit, data);
        StandardQueryResult flagsResult = processor.ProcessQuery(badFlags, data);

        using (Assert.EnterMultipleScope())
        {
            AssertError(nullResult, JmapErrorCodes.InvalidArguments, JmapMethodErrorKind.InvalidArguments);
            AssertError(accountResult, JmapErrorCodes.InvalidArguments, JmapMethodErrorKind.InvalidArguments);
            AssertError(positionResult, JmapErrorCodes.InvalidArguments, JmapMethodErrorKind.InvalidArguments);
            AssertError(offsetResult, JmapErrorCodes.InvalidArguments, JmapMethodErrorKind.InvalidArguments);
            AssertError(limitResult, JmapErrorCodes.InvalidArguments, JmapMethodErrorKind.InvalidArguments);
            AssertError(flagsResult, JmapErrorCodes.InvalidArguments, JmapMethodErrorKind.InvalidArguments);
        }
    }

    [Test]
    public void InvalidBackendQueryMetadataIsAProtocolFailure()
    {
        StandardQueryProcessor processor = Processor(10, 4);
        StandardQueryData missingState = Data(null, "a");
        StandardQueryData invalidId = Data("state", "bad/id");
        StandardQueryData duplicateId = Data("state", "a", "a");

        JmapProtocolException stateError = Assert.Throws<JmapProtocolException>(() => processor.ProcessQuery(Request(), missingState))!;
        JmapProtocolException idError = Assert.Throws<JmapProtocolException>(() => processor.ProcessQuery(Request(), invalidId))!;
        JmapProtocolException duplicateError = Assert.Throws<JmapProtocolException>(() => processor.ProcessQuery(Request(), duplicateId))!;

        using (Assert.EnterMultipleScope())
        {
            Assert.That(stateError.Path, Is.EqualTo("queryState"));
            Assert.That(idError.Path, Is.EqualTo("ids/0"));
            Assert.That(duplicateError.Path, Is.EqualTo("ids/1"));
        }
    }

    private static StandardQueryProcessor Processor(long maximumResults, int maximumFilterDepth)
    {
        return new StandardQueryProcessor(
            maximumResults,
            maximumFilterDepth,
            new List<string> { "name", "role" },
            new List<string> { "name", "size" },
            new List<string> { "name" },
            new List<string> { "i;unicode-casemap" },
            new List<string> { "keyword" });
    }

    private static JmapQueryRequest Request()
    {
        JmapQueryRequest request = new JmapQueryRequest();
        request.AccountId = "account-1";
        return request;
    }

    private static StandardQueryData Data(string? queryState, params string[] ids)
    {
        StandardQueryData data = new StandardQueryData();
        data.QueryState = queryState;
        data.OrderedIds.AddRange(ids);
        return data;
    }

    private static JmapFilterNode Condition(string property, JsonElement value)
    {
        JmapFilterNode filter = new JmapFilterNode();
        filter.FilterProperties.Add(property, value);
        return filter;
    }

    private static JmapFilterNode Operator(JmapFilterOperator value, params JmapFilterNode[] conditions)
    {
        JmapFilterNode filter = new JmapFilterNode();
        filter.HasOperator = true;
        filter.Operator = value;
        filter.Conditions = new List<JmapFilterNode>(conditions);
        return filter;
    }

    private static JmapComparator Comparator(string property, string? collation)
    {
        JmapComparator comparator = new JmapComparator();
        comparator.Property = property;
        comparator.Collation = collation;
        return comparator;
    }

    private static void AssertLimit(StandardQueryResult result, long expectedLimit, string[] expectedIds)
    {
        using (Assert.EnterMultipleScope())
        {
            Assert.That(result.IsSuccess, Is.True);
            Assert.That(result.Response!.HasLimit, Is.True);
            Assert.That(result.Response.Limit, Is.EqualTo(expectedLimit));
            Assert.That(result.Response.Ids, Is.EqualTo(expectedIds));
        }
    }

    private static void AssertError(StandardQueryResult result, string type, JmapMethodErrorKind kind)
    {
        using (Assert.EnterMultipleScope())
        {
            Assert.That(result.IsSuccess, Is.False);
            Assert.That(result.Response, Is.Null);
            Assert.That(result.Error, Is.Not.Null);
            Assert.That(result.Error!.Type, Is.EqualTo(type));
            Assert.That(result.Error.Kind, Is.EqualTo(kind));
        }
    }
}
