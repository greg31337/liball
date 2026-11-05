using System.Text;

namespace http.Tests;

public class HttpPreconditionEvaluatorTests
{
    const string EarlierDate = "Sat, 29 Oct 1994 19:43:30 GMT";
    const string CurrentDate = "Sat, 29 Oct 1994 19:43:31 GMT";
    const string LaterDate = "Sat, 29 Oct 1994 19:43:32 GMT";

    [Test]
    public void PublicServiceProvidesStrongAndWeakEntityTagComparison()
    {
        IHttpPreconditionEvaluator service = new HttpPreconditionEvaluator();
        HttpEntityTag strong = Tag("current", false);
        HttpEntityTag sameWeak = Tag("current", true);

        Assert.Multiple(() =>
        {
            Assert.That(service.CompareStrongTag(strong, Tag("current", false)),
                Is.True);
            Assert.That(service.CompareStrongTag(strong, sameWeak), Is.False);
            Assert.That(service.CompareWeakTag(strong, sameWeak), Is.True);
            Assert.That(service.CompareWeakTag(strong, Tag("other", false)), Is.False);
        });
    }

    [Test]
    public void OriginEvaluationUsesProtocolOrderAndSkipsSupersededDateCondition()
    {
        IHttpPreconditionEvaluator service = new HttpPreconditionEvaluator();
        HttpPreconditionContext context = Context(HttpMethods.Get);
        HttpRequest request = NewRequest();
        AddField(request, HttpHeaderNames.IfMatch, "\"current\"");
        AddField(request, HttpHeaderNames.IfUnmodifiedSince, EarlierDate);
        AddField(request, HttpHeaderNames.IfNoneMatch, "W/\"current\"");
        HttpPreconditionDecision result = service.EvaluatePreconditions(request, context);

        HttpRequest failedRequest = NewRequest();
        AddField(failedRequest, HttpHeaderNames.IfMatch, "\"other\"");
        AddField(failedRequest, HttpHeaderNames.IfNoneMatch, "\"other\"");
        HttpPreconditionDecision failed = service.EvaluatePreconditions(
            failedRequest,
            context);

        Assert.Multiple(() =>
        {
            Assert.That(result.Outcome, Is.EqualTo(HttpPreconditionOutcome.NotModified));
            Assert.That(result.StatusCode, Is.EqualTo(HttpStatusCodes.NotModified));
            Assert.That(result.DecidingCondition,
                Is.EqualTo(HttpHeaderNames.IfNoneMatch));
            Assert.That(failed.Outcome,
                Is.EqualTo(HttpPreconditionOutcome.PreconditionFailed));
            Assert.That(failed.StatusCode,
                Is.EqualTo(HttpStatusCodes.PreconditionFailed));
            Assert.That(failed.DecidingCondition, Is.EqualTo(HttpHeaderNames.IfMatch));
        });
    }

    [Test]
    public void IfUnmodifiedSinceAppliesOnlyAtAnOriginWithoutIfMatch()
    {
        IHttpPreconditionEvaluator service = new HttpPreconditionEvaluator();
        HttpRequest request = NewRequest();
        AddField(request, HttpHeaderNames.IfMatch, "malformed");
        AddField(request, HttpHeaderNames.IfUnmodifiedSince, EarlierDate);
        AddField(request, HttpHeaderNames.IfNoneMatch, "\"other\"");

        HttpPreconditionContext origin = Context(HttpMethods.Put);
        HttpRequest originRequest = NewRequest();
        AddField(originRequest, HttpHeaderNames.IfUnmodifiedSince, EarlierDate);
        HttpPreconditionDecision originResult = service.EvaluatePreconditions(
            originRequest,
            origin);

        HttpPreconditionContext cache = Context(HttpMethods.Put);
        cache.IsCacheRecipient = true;
        HttpPreconditionDecision cacheResult = service.EvaluatePreconditions(
            request,
            cache);

        Assert.Multiple(() =>
        {
            Assert.That(originResult.Outcome,
                Is.EqualTo(HttpPreconditionOutcome.PreconditionFailed));
            Assert.That(originResult.DecidingCondition,
                Is.EqualTo(HttpHeaderNames.IfUnmodifiedSince));
            Assert.That(cacheResult.Outcome, Is.EqualTo(HttpPreconditionOutcome.Proceed));
            Assert.That(cacheResult.DecidingCondition, Is.Null);
        });
    }

    [Test]
    public void IfNoneMatchUsesMethodSpecificResultAndSuppressesIfModifiedSince()
    {
        IHttpPreconditionEvaluator service = new HttpPreconditionEvaluator();
        HttpRequest matching = RequestWithField(
            HttpHeaderNames.IfNoneMatch,
            "W/\"current\"");
        HttpPreconditionDecision getResult = service.EvaluatePreconditions(
            matching,
            Context(HttpMethods.Get));
        HttpPreconditionDecision putResult = service.EvaluatePreconditions(
            matching,
            Context(HttpMethods.Put));

        HttpRequest nonmatching = RequestWithField(
            HttpHeaderNames.IfNoneMatch,
            "\"other\"");
        AddField(nonmatching, HttpHeaderNames.IfModifiedSince, LaterDate);
        HttpPreconditionDecision suppressed = service.EvaluatePreconditions(
            nonmatching,
            Context(HttpMethods.Get));

        Assert.Multiple(() =>
        {
            Assert.That(getResult.Outcome,
                Is.EqualTo(HttpPreconditionOutcome.NotModified));
            Assert.That(getResult.StatusCode, Is.EqualTo(HttpStatusCodes.NotModified));
            Assert.That(putResult.Outcome,
                Is.EqualTo(HttpPreconditionOutcome.PreconditionFailed));
            Assert.That(putResult.StatusCode,
                Is.EqualTo(HttpStatusCodes.PreconditionFailed));
            Assert.That(suppressed.Outcome, Is.EqualTo(HttpPreconditionOutcome.Proceed));
            Assert.That(suppressed.DecidingCondition, Is.Null);
        });
    }

    [Test]
    public void IfModifiedSinceAppliesOnlyToGetAndHead()
    {
        IHttpPreconditionEvaluator service = new HttpPreconditionEvaluator();
        HttpRequest request = RequestWithField(
            HttpHeaderNames.IfModifiedSince,
            CurrentDate);

        HttpPreconditionDecision getResult = service.EvaluatePreconditions(
            request,
            Context(HttpMethods.Get));
        HttpPreconditionDecision headResult = service.EvaluatePreconditions(
            request,
            Context(HttpMethods.Head));
        HttpPreconditionDecision postResult = service.EvaluatePreconditions(
            request,
            Context(HttpMethods.Post));

        Assert.Multiple(() =>
        {
            Assert.That(getResult.Outcome,
                Is.EqualTo(HttpPreconditionOutcome.NotModified));
            Assert.That(headResult.Outcome,
                Is.EqualTo(HttpPreconditionOutcome.NotModified));
            Assert.That(postResult.Outcome, Is.EqualTo(HttpPreconditionOutcome.Proceed));
        });
    }

    [Test]
    public void MethodsWithoutRepresentationSelectionIgnoreAllPreconditions()
    {
        IHttpPreconditionEvaluator service = new HttpPreconditionEvaluator();
        string[] methods = new string[]
        {
            HttpMethods.Connect,
            HttpMethods.Options,
            HttpMethods.Trace
        };
        HttpRequest request = NewRequest();
        AddField(request, HttpHeaderNames.IfMatch, "malformed");
        AddField(request, HttpHeaderNames.IfNoneMatch, "\"current\"");
        AddField(request, HttpHeaderNames.Range, "bytes=0-9");
        AddField(request, HttpHeaderNames.IfRange, "\"other\"");

        int index = 0;
        while (index < methods.Length)
        {
            HttpPreconditionDecision result = service.EvaluatePreconditions(
                request,
                Context(methods[index]));
            Assert.Multiple(() =>
            {
                Assert.That(result.Outcome,
                    Is.EqualTo(HttpPreconditionOutcome.Proceed));
                Assert.That(result.DecidingCondition, Is.Null);
                Assert.That(result.ApplyRange, Is.False);
            });
            index = index + 1;
        }
    }

    [Test]
    public void RangeProcessingOccursLastAndOnlyForGet()
    {
        IHttpPreconditionEvaluator service = new HttpPreconditionEvaluator();
        HttpRequest matching = RangeRequest("\"current\"");
        HttpRequest mismatching = RangeRequest("\"other\"");
        HttpRequest rangeOnly = RequestWithField(
            HttpHeaderNames.Range,
            "bytes=0-9");

        HttpPreconditionDecision apply = service.EvaluatePreconditions(
            matching,
            Context(HttpMethods.Get));
        HttpPreconditionDecision ignore = service.EvaluatePreconditions(
            mismatching,
            Context(HttpMethods.Get));
        HttpPreconditionDecision unconditional = service.EvaluatePreconditions(
            rangeOnly,
            Context(HttpMethods.Get));
        HttpPreconditionDecision head = service.EvaluatePreconditions(
            mismatching,
            Context(HttpMethods.Head));

        AddField(mismatching, HttpHeaderNames.IfNoneMatch, "\"current\"");
        HttpPreconditionDecision earlier = service.EvaluatePreconditions(
            mismatching,
            Context(HttpMethods.Get));

        Assert.Multiple(() =>
        {
            Assert.That(apply.Outcome, Is.EqualTo(HttpPreconditionOutcome.Proceed));
            Assert.That(apply.ApplyRange, Is.True);
            Assert.That(apply.DecidingCondition, Is.Null);
            Assert.That(ignore.Outcome,
                Is.EqualTo(HttpPreconditionOutcome.IgnoreRange));
            Assert.That(ignore.ApplyRange, Is.False);
            Assert.That(unconditional.ApplyRange, Is.True);
            Assert.That(head.Outcome, Is.EqualTo(HttpPreconditionOutcome.Proceed));
            Assert.That(head.ApplyRange, Is.False);
            Assert.That(earlier.Outcome,
                Is.EqualTo(HttpPreconditionOutcome.NotModified));
            Assert.That(earlier.ApplyRange, Is.False);
        });
    }

    [Test]
    public void PublicIfRangeOperationRetainsIndividualEvaluation()
    {
        IHttpPreconditionEvaluator service = new HttpPreconditionEvaluator();
        HttpPreconditionDecision result = service.EvaluateIfRange(
            RangeRequest("\"other\""),
            Context(HttpMethods.Get));

        Assert.That(result.Outcome, Is.EqualTo(HttpPreconditionOutcome.IgnoreRange));
    }

    [Test]
    public void EntityTagComparisonCoversStrengthOpaqueBytesAndInvalidTags()
    {
        IHttpPreconditionEvaluator service = new HttpPreconditionEvaluator();
        HttpEntityTag strong = Tag("CaseSensitive", false);
        HttpEntityTag weak = Tag("CaseSensitive", true);
        HttpEntityTag wildcard = Tag("CaseSensitive", false);
        wildcard.IsWildcard = true;
        HttpEntityTag invalid = Tag("bad tag", false);

        Assert.Multiple(() =>
        {
            Assert.That(service.CompareStrongTag(strong, Tag("CaseSensitive", false)), Is.True);
            Assert.That(service.CompareStrongTag(strong, weak), Is.False);
            Assert.That(service.CompareWeakTag(strong, weak), Is.True);
            Assert.That(service.CompareWeakTag(strong, Tag("casesensitive", false)), Is.False);
            Assert.That(service.CompareWeakTag(Tag("", false), Tag("", true)), Is.True);
            Assert.That(service.CompareStrongTag(strong, wildcard), Is.False);
            Assert.That(service.CompareWeakTag(invalid, invalid), Is.False);
            Assert.That(service.CompareWeakTag(null!, strong), Is.False);
        });
    }

    [Test]
    public void IfMatchUsesStrongListsAndWildcardExistence()
    {
        IHttpPreconditionEvaluator service = new HttpPreconditionEvaluator();
        HttpPreconditionContext existing = Context(HttpMethods.Put);
        HttpPreconditionContext missing = Context(HttpMethods.Put);
        missing.RepresentationExists = false;
        missing.EntityTag = null;

        HttpPreconditionDecision list = service.EvaluatePreconditions(
            RequestWithField(HttpHeaderNames.IfMatch, "\"other\", \"current\""), existing);
        HttpPreconditionDecision weak = service.EvaluatePreconditions(
            RequestWithField(HttpHeaderNames.IfMatch, "W/\"current\""), existing);
        HttpPreconditionDecision existingWildcard = service.EvaluatePreconditions(
            RequestWithField(HttpHeaderNames.IfMatch, "*"), existing);
        HttpPreconditionDecision missingWildcard = service.EvaluatePreconditions(
            RequestWithField(HttpHeaderNames.IfMatch, "*"), missing);

        Assert.Multiple(() =>
        {
            Assert.That(list.Outcome, Is.EqualTo(HttpPreconditionOutcome.Proceed));
            Assert.That(weak.Outcome, Is.EqualTo(HttpPreconditionOutcome.PreconditionFailed));
            Assert.That(weak.HasStatusCode, Is.True);
            Assert.That(weak.StatusCode, Is.EqualTo(HttpStatusCodes.PreconditionFailed));
            Assert.That(existingWildcard.Outcome, Is.EqualTo(HttpPreconditionOutcome.Proceed));
            Assert.That(missingWildcard.Outcome,
                Is.EqualTo(HttpPreconditionOutcome.PreconditionFailed));
            Assert.That(missingWildcard.DecidingCondition, Is.EqualTo(HttpHeaderNames.IfMatch));
        });
    }

    [Test]
    public void IfNoneMatchUsesWeakWildcardAndMethodSpecificOutcomes()
    {
        IHttpPreconditionEvaluator service = new HttpPreconditionEvaluator();
        HttpRequest weak = RequestWithField(HttpHeaderNames.IfNoneMatch, "W/\"current\"");
        HttpRequest wildcard = RequestWithField(HttpHeaderNames.IfNoneMatch, "*");
        HttpPreconditionContext missing = Context(HttpMethods.Put);
        missing.RepresentationExists = false;
        missing.EntityTag = null;

        HttpPreconditionDecision get = service.EvaluatePreconditions(weak, Context(HttpMethods.Get));
        HttpPreconditionDecision head = service.EvaluatePreconditions(wildcard, Context(HttpMethods.Head));
        HttpPreconditionDecision put = service.EvaluatePreconditions(wildcard, Context(HttpMethods.Put));
        HttpPreconditionDecision create = service.EvaluatePreconditions(wildcard, missing);

        Assert.Multiple(() =>
        {
            Assert.That(get.Outcome, Is.EqualTo(HttpPreconditionOutcome.NotModified));
            Assert.That(get.StatusCode, Is.EqualTo(HttpStatusCodes.NotModified));
            Assert.That(head.Outcome, Is.EqualTo(HttpPreconditionOutcome.NotModified));
            Assert.That(put.Outcome, Is.EqualTo(HttpPreconditionOutcome.PreconditionFailed));
            Assert.That(put.StatusCode, Is.EqualTo(HttpStatusCodes.PreconditionFailed));
            Assert.That(create.Outcome, Is.EqualTo(HttpPreconditionOutcome.Proceed));
        });
    }

    [Test]
    public void DateConditionsUseSecondPrecisionBoundaries()
    {
        IHttpPreconditionEvaluator service = new HttpPreconditionEvaluator();
        HttpPreconditionContext get = Context(HttpMethods.Get);
        get.LastModified = System.DateTimeOffset.Parse("1994-10-29T19:43:31.900+00:00");
        HttpPreconditionContext put = Context(HttpMethods.Put);
        put.LastModified = get.LastModified;

        HttpPreconditionDecision modifiedEarlier = service.EvaluatePreconditions(
            RequestWithField(HttpHeaderNames.IfModifiedSince, EarlierDate), get);
        HttpPreconditionDecision modifiedEqual = service.EvaluatePreconditions(
            RequestWithField(HttpHeaderNames.IfModifiedSince, CurrentDate), get);
        HttpPreconditionDecision modifiedLater = service.EvaluatePreconditions(
            RequestWithField(HttpHeaderNames.IfModifiedSince, LaterDate), get);
        HttpPreconditionDecision unmodifiedEarlier = service.EvaluatePreconditions(
            RequestWithField(HttpHeaderNames.IfUnmodifiedSince, EarlierDate), put);
        HttpPreconditionDecision unmodifiedEqual = service.EvaluatePreconditions(
            RequestWithField(HttpHeaderNames.IfUnmodifiedSince, CurrentDate), put);
        HttpPreconditionDecision unmodifiedLater = service.EvaluatePreconditions(
            RequestWithField(HttpHeaderNames.IfUnmodifiedSince, LaterDate), put);

        Assert.Multiple(() =>
        {
            Assert.That(modifiedEarlier.Outcome, Is.EqualTo(HttpPreconditionOutcome.Proceed));
            Assert.That(modifiedEqual.Outcome, Is.EqualTo(HttpPreconditionOutcome.NotModified));
            Assert.That(modifiedLater.Outcome, Is.EqualTo(HttpPreconditionOutcome.NotModified));
            Assert.That(unmodifiedEarlier.Outcome,
                Is.EqualTo(HttpPreconditionOutcome.PreconditionFailed));
            Assert.That(unmodifiedEqual.Outcome, Is.EqualTo(HttpPreconditionOutcome.Proceed));
            Assert.That(unmodifiedLater.Outcome, Is.EqualTo(HttpPreconditionOutcome.Proceed));
        });
    }

    [Test]
    public void InvalidRepeatedAndUnavailableDatesAreIgnored()
    {
        IHttpPreconditionEvaluator service = new HttpPreconditionEvaluator();
        HttpRequest repeated = RequestWithField(HttpHeaderNames.IfModifiedSince, CurrentDate);
        AddField(repeated, HttpHeaderNames.IfModifiedSince, LaterDate);
        HttpPreconditionContext unavailable = Context(HttpMethods.Get);
        unavailable.HasLastModified = false;
        HttpPreconditionContext missing = Context(HttpMethods.Get);
        missing.RepresentationExists = false;

        HttpPreconditionDecision invalid = service.EvaluatePreconditions(
            RequestWithField(HttpHeaderNames.IfModifiedSince, "not-a-date"),
            Context(HttpMethods.Get));
        HttpPreconditionDecision duplicate = service.EvaluatePreconditions(
            repeated, Context(HttpMethods.Get));
        HttpPreconditionDecision noDate = service.EvaluatePreconditions(
            RequestWithField(HttpHeaderNames.IfModifiedSince, LaterDate), unavailable);
        HttpPreconditionDecision noRepresentation = service.EvaluatePreconditions(
            RequestWithField(HttpHeaderNames.IfModifiedSince, LaterDate), missing);

        Assert.Multiple(() =>
        {
            Assert.That(invalid.Outcome, Is.EqualTo(HttpPreconditionOutcome.Proceed));
            Assert.That(duplicate.Outcome, Is.EqualTo(HttpPreconditionOutcome.Proceed));
            Assert.That(noDate.Outcome, Is.EqualTo(HttpPreconditionOutcome.Proceed));
            Assert.That(noRepresentation.Outcome, Is.EqualTo(HttpPreconditionOutcome.Proceed));
            Assert.That(invalid.DecidingCondition, Is.Null);
            Assert.That(duplicate.DecidingCondition, Is.Null);
        });
    }

    [Test]
    public void MalformedTagConditionsRejectAtTheirPrecedenceStep()
    {
        IHttpPreconditionEvaluator service = new HttpPreconditionEvaluator();
        HttpRequest malformedMatch = NewRequest();
        AddField(malformedMatch, HttpHeaderNames.IfMatch, "malformed");
        AddField(malformedMatch, HttpHeaderNames.IfNoneMatch, "\"current\"");
        HttpRequest malformedNone = NewRequest();
        AddField(malformedNone, HttpHeaderNames.IfNoneMatch, "*, \"current\"");
        AddField(malformedNone, HttpHeaderNames.IfModifiedSince, CurrentDate);

        HttpPreconditionDecision match = service.EvaluatePreconditions(
            malformedMatch, Context(HttpMethods.Get));
        HttpPreconditionDecision none = service.EvaluatePreconditions(
            malformedNone, Context(HttpMethods.Get));

        Assert.Multiple(() =>
        {
            Assert.That(match.Outcome, Is.EqualTo(HttpPreconditionOutcome.Rejected));
            Assert.That(match.DecidingCondition, Is.EqualTo(HttpHeaderNames.IfMatch));
            Assert.That(match.HasStatusCode, Is.False);
            Assert.That(none.Outcome, Is.EqualTo(HttpPreconditionOutcome.Rejected));
            Assert.That(none.DecidingCondition, Is.EqualTo(HttpHeaderNames.IfNoneMatch));
        });
    }

    [Test]
    public void SuccessfulEarlierConditionsContinueInProtocolOrder()
    {
        IHttpPreconditionEvaluator service = new HttpPreconditionEvaluator();
        HttpRequest origin = NewRequest();
        AddField(origin, HttpHeaderNames.IfUnmodifiedSince, LaterDate);
        AddField(origin, HttpHeaderNames.IfNoneMatch, "W/\"current\"");
        HttpPreconditionDecision originResult = service.EvaluatePreconditions(
            origin, Context(HttpMethods.Get));

        HttpRequest cached = NewRequest();
        AddField(cached, HttpHeaderNames.IfMatch, "malformed");
        AddField(cached, HttpHeaderNames.IfUnmodifiedSince, EarlierDate);
        AddField(cached, HttpHeaderNames.IfNoneMatch, "W/\"current\"");
        HttpPreconditionContext cache = Context(HttpMethods.Get);
        cache.IsCacheRecipient = true;
        HttpPreconditionDecision cacheResult = service.EvaluatePreconditions(cached, cache);

        Assert.Multiple(() =>
        {
            Assert.That(originResult.Outcome, Is.EqualTo(HttpPreconditionOutcome.NotModified));
            Assert.That(originResult.DecidingCondition, Is.EqualTo(HttpHeaderNames.IfNoneMatch));
            Assert.That(cacheResult.Outcome, Is.EqualTo(HttpPreconditionOutcome.NotModified));
            Assert.That(cacheResult.DecidingCondition, Is.EqualTo(HttpHeaderNames.IfNoneMatch));
        });
    }

    [Test]
    public void IfRangeEntityTagsRequireStrongExactCurrentValidator()
    {
        IHttpPreconditionEvaluator service = new HttpPreconditionEvaluator();
        HttpPreconditionContext strong = Context(HttpMethods.Get);
        HttpPreconditionContext weakCurrent = Context(HttpMethods.Get);
        weakCurrent.EntityTag = Tag("current", true);
        HttpPreconditionContext missing = Context(HttpMethods.Get);
        missing.EntityTag = null;

        HttpPreconditionDecision match = service.EvaluateIfRange(
            RangeRequest("\"current\""), strong);
        HttpPreconditionDecision mismatch = service.EvaluateIfRange(
            RangeRequest("\"other\""), strong);
        HttpPreconditionDecision weakRequest = service.EvaluateIfRange(
            RangeRequest("W/\"current\""), strong);
        HttpPreconditionDecision weakRepresentation = service.EvaluateIfRange(
            RangeRequest("\"current\""), weakCurrent);
        HttpPreconditionDecision unavailable = service.EvaluateIfRange(
            RangeRequest("\"current\""), missing);

        Assert.Multiple(() =>
        {
            Assert.That(match.Outcome, Is.EqualTo(HttpPreconditionOutcome.Proceed));
            Assert.That(match.ApplyRange, Is.True);
            Assert.That(match.DecidingCondition, Is.EqualTo(HttpHeaderNames.IfRange));
            Assert.That(mismatch.Outcome, Is.EqualTo(HttpPreconditionOutcome.IgnoreRange));
            Assert.That(weakRequest.Outcome, Is.EqualTo(HttpPreconditionOutcome.IgnoreRange));
            Assert.That(weakRepresentation.Outcome, Is.EqualTo(HttpPreconditionOutcome.IgnoreRange));
            Assert.That(unavailable.Outcome, Is.EqualTo(HttpPreconditionOutcome.IgnoreRange));
        });
    }

    [Test]
    public void IfRangeDatesRequireExactLastModifiedAndStrongDateEvidence()
    {
        IHttpPreconditionEvaluator service = new HttpPreconditionEvaluator();
        HttpPreconditionContext strong = Context(HttpMethods.Get);
        strong.HasDate = true;
        strong.Date = System.DateTimeOffset.Parse("1994-10-29T19:43:32+00:00");
        HttpPreconditionContext weak = Context(HttpMethods.Get);
        weak.HasDate = true;
        weak.Date = weak.LastModified;
        HttpPreconditionContext noDate = Context(HttpMethods.Get);

        HttpPreconditionDecision exact = service.EvaluateIfRange(
            RangeRequest(CurrentDate), strong);
        HttpPreconditionDecision older = service.EvaluateIfRange(
            RangeRequest(EarlierDate), strong);
        HttpPreconditionDecision weakEvidence = service.EvaluateIfRange(
            RangeRequest(CurrentDate), weak);
        HttpPreconditionDecision missingEvidence = service.EvaluateIfRange(
            RangeRequest(CurrentDate), noDate);

        Assert.Multiple(() =>
        {
            Assert.That(exact.Outcome, Is.EqualTo(HttpPreconditionOutcome.Proceed));
            Assert.That(exact.ApplyRange, Is.True);
            Assert.That(older.Outcome, Is.EqualTo(HttpPreconditionOutcome.IgnoreRange));
            Assert.That(weakEvidence.Outcome, Is.EqualTo(HttpPreconditionOutcome.IgnoreRange));
            Assert.That(missingEvidence.Outcome, Is.EqualTo(HttpPreconditionOutcome.IgnoreRange));
        });
    }

    [Test]
    public void IfRangeHandlesAbsenceMalformedDuplicatesNoRangeAndNonGetMethods()
    {
        IHttpPreconditionEvaluator service = new HttpPreconditionEvaluator();
        HttpRequest rangeOnly = RequestWithField(HttpHeaderNames.Range, "bytes=0-9");
        HttpRequest duplicate = RangeRequest("\"current\"");
        AddField(duplicate, HttpHeaderNames.IfRange, "\"current\"");
        HttpRequest noRange = RequestWithField(HttpHeaderNames.IfRange, "\"current\"");

        HttpPreconditionDecision absent = service.EvaluateIfRange(
            rangeOnly, Context(HttpMethods.Get));
        HttpPreconditionDecision malformed = service.EvaluateIfRange(
            RangeRequest("invalid"), Context(HttpMethods.Get));
        HttpPreconditionDecision repeated = service.EvaluateIfRange(
            duplicate, Context(HttpMethods.Get));
        HttpPreconditionDecision ignored = service.EvaluateIfRange(
            noRange, Context(HttpMethods.Get));
        HttpPreconditionDecision head = service.EvaluateIfRange(
            RangeRequest("\"current\""), Context(HttpMethods.Head));
        HttpPreconditionDecision put = service.EvaluateIfRange(
            RangeRequest("\"current\""), Context(HttpMethods.Put));

        Assert.Multiple(() =>
        {
            Assert.That(absent.ApplyRange, Is.True);
            Assert.That(malformed.Outcome, Is.EqualTo(HttpPreconditionOutcome.IgnoreRange));
            Assert.That(repeated.Outcome, Is.EqualTo(HttpPreconditionOutcome.IgnoreRange));
            Assert.That(ignored.Outcome, Is.EqualTo(HttpPreconditionOutcome.Proceed));
            Assert.That(ignored.ApplyRange, Is.False);
            Assert.That(head.ApplyRange, Is.False);
            Assert.That(put.ApplyRange, Is.False);
        });
    }

    [Test]
    public void PublicOperationsRejectIncompleteOrMalformedContexts()
    {
        IHttpPreconditionEvaluator service = new HttpPreconditionEvaluator();
        HttpPreconditionContext missingMethod = new HttpPreconditionContext();
        HttpRequest missingHeaders = NewRequest();
        missingHeaders.Headers = null!;
        HttpRequest missingFields = NewRequest();
        missingFields.Headers.Fields = null!;
        HttpRequest malformedField = NewRequest();
        AddField(malformedField, "Bad Field", "value");

        Assert.Multiple(() =>
        {
            Assert.Throws<HttpProtocolException>(() => service.EvaluatePreconditions(
                null!, Context(HttpMethods.Get)));
            Assert.Throws<HttpProtocolException>(() => service.EvaluatePreconditions(
                NewRequest(), null!));
            Assert.Throws<HttpProtocolException>(() => service.EvaluatePreconditions(
                NewRequest(), missingMethod));
            Assert.Throws<HttpProtocolException>(() => service.EvaluatePreconditions(
                missingHeaders, Context(HttpMethods.Get)));
            Assert.Throws<HttpProtocolException>(() => service.EvaluateIfRange(
                missingFields, Context(HttpMethods.Get)));
            Assert.Throws<HttpProtocolException>(() => service.EvaluatePreconditions(
                malformedField, Context(HttpMethods.Get)));
        });
    }

    static HttpPreconditionContext Context(string method)
    {
        HttpPreconditionContext context = new HttpPreconditionContext();
        context.Method = method;
        context.RepresentationExists = true;
        context.EntityTag = Tag("current", false);
        context.HasLastModified = true;
        context.LastModified = System.DateTimeOffset.Parse(
            "1994-10-29T19:43:31+00:00");
        return context;
    }

    static HttpEntityTag Tag(string opaqueTag, bool weak)
    {
        HttpEntityTag tag = new HttpEntityTag();
        tag.OpaqueTag = Encoding.ASCII.GetBytes(opaqueTag);
        tag.IsWeak = weak;
        return tag;
    }

    static HttpRequest RangeRequest(string ifRange)
    {
        HttpRequest request = RequestWithField(HttpHeaderNames.Range, "bytes=0-9");
        AddField(request, HttpHeaderNames.IfRange, ifRange);
        return request;
    }

    static HttpRequest RequestWithField(string name, string value)
    {
        HttpRequest request = NewRequest();
        AddField(request, name, value);
        return request;
    }

    static HttpRequest NewRequest()
    {
        return new HttpRequest();
    }

    static void AddField(HttpRequest request, string name, string value)
    {
        HttpHeaderField field = new HttpHeaderField();
        field.Name = name;
        field.Value = Encoding.ASCII.GetBytes(value);
        request.Headers.Fields.Add(field);
    }
}
