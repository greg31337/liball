using System.Text;

namespace http.Tests;

public class PreconditionSemanticsTests
{
    const string EarlierDate = "Sat, 29 Oct 1994 19:43:30 GMT";
    const string CurrentDate = "Sat, 29 Oct 1994 19:43:31 GMT";
    const string LaterDate = "Sat, 29 Oct 1994 19:43:32 GMT";

    [Test]
    public void EntityTagConditionsParseCombinedLinesAndStandaloneWildcard()
    {
        HttpHeaders listHeaders = new HttpHeaders();
        AddField(listHeaders, HttpHeaderNames.IfMatch, "\"one\"");
        AddField(listHeaders, HttpHeaderNames.IfMatch, "W/\"two\"");
        HttpHeaders wildcardHeaders = new HttpHeaders();
        AddField(wildcardHeaders, HttpHeaderNames.IfNoneMatch, "*");

        PreconditionFieldValue list = PreconditionParser.ParseEntityTagCondition(
            listHeaders,
            HttpHeaderNames.IfMatch);
        PreconditionFieldValue wildcard = PreconditionParser.ParseEntityTagCondition(
            wildcardHeaders,
            HttpHeaderNames.IfNoneMatch);

        Assert.Multiple(() =>
        {
            Assert.That(list.IsPresent, Is.True);
            Assert.That(list.IsValid, Is.True);
            Assert.That(list.EntityTags, Has.Count.EqualTo(2));
            Assert.That(list.EntityTags[1].IsWeak, Is.True);
            Assert.That(wildcard.IsValid, Is.True);
            Assert.That(wildcard.IsWildcard, Is.True);
        });
    }

    [Test]
    public void EntityTagConditionRejectsMalformedWildcardMixture()
    {
        HttpHeaders headers = new HttpHeaders();
        AddField(headers, HttpHeaderNames.IfMatch, "*, \"tag\"");

        PreconditionFieldValue result = PreconditionParser.ParseEntityTagCondition(
            headers,
            HttpHeaderNames.IfMatch);

        Assert.Multiple(() =>
        {
            Assert.That(result.IsPresent, Is.True);
            Assert.That(result.IsValid, Is.False);
        });
    }

    [Test]
    public void DateConditionsParseOneDateAndIgnoreInvalidOrRepeatedValues()
    {
        HttpHeaders validHeaders = new HttpHeaders();
        AddField(validHeaders, HttpHeaderNames.IfModifiedSince, "  " + CurrentDate + "\t");
        HttpHeaders invalidHeaders = new HttpHeaders();
        AddField(invalidHeaders, HttpHeaderNames.IfModifiedSince, "not-a-date");
        HttpHeaders repeatedHeaders = new HttpHeaders();
        AddField(repeatedHeaders, HttpHeaderNames.IfModifiedSince, CurrentDate);
        AddField(repeatedHeaders, HttpHeaderNames.IfModifiedSince, LaterDate);

        PreconditionFieldValue valid = PreconditionParser.ParseDateCondition(
            validHeaders,
            HttpHeaderNames.IfModifiedSince);
        PreconditionFieldValue invalid = PreconditionParser.ParseDateCondition(
            invalidHeaders,
            HttpHeaderNames.IfModifiedSince);
        PreconditionFieldValue repeated = PreconditionParser.ParseDateCondition(
            repeatedHeaders,
            HttpHeaderNames.IfModifiedSince);

        Assert.Multiple(() =>
        {
            Assert.That(valid.IsValid, Is.True);
            Assert.That(valid.IsDate, Is.True);
            Assert.That(valid.Date, Is.EqualTo(DateTimeOffset.Parse(
                "1994-10-29T19:43:31+00:00")));
            Assert.That(invalid.IsPresent, Is.True);
            Assert.That(invalid.IsValid, Is.False);
            Assert.That(repeated.IsValid, Is.False);
        });
    }

    [Test]
    public void IfRangeParserDistinguishesEntityTagsDatesAndInvalidValues()
    {
        PreconditionFieldValue entityTag = PreconditionParser.ParseIfRangeCondition(
            Headers(HttpHeaderNames.IfRange, "\"tag\""));
        PreconditionFieldValue date = PreconditionParser.ParseIfRangeCondition(
            Headers(HttpHeaderNames.IfRange, CurrentDate));
        PreconditionFieldValue invalid = PreconditionParser.ParseIfRangeCondition(
            Headers(HttpHeaderNames.IfRange, "invalid"));

        Assert.Multiple(() =>
        {
            Assert.That(entityTag.IsEntityTag, Is.True);
            Assert.That(entityTag.IsDate, Is.False);
            Assert.That(date.IsDate, Is.True);
            Assert.That(date.IsEntityTag, Is.False);
            Assert.That(invalid.IsPresent, Is.True);
            Assert.That(invalid.IsValid, Is.False);
        });
    }

    [Test]
    public void IfMatchUsesStrongComparisonAndWildcardExistence()
    {
        HttpPreconditionContext existing = Context(HttpMethods.Put, true, false);
        existing.EntityTag = Tag("current", false);
        HttpPreconditionDecision strong = PreconditionSemantics.EvaluateIfMatch(
            Request(HttpHeaderNames.IfMatch, "\"other\", \"current\""),
            existing);
        HttpPreconditionDecision weak = PreconditionSemantics.EvaluateIfMatch(
            Request(HttpHeaderNames.IfMatch, "W/\"current\""),
            existing);
        HttpPreconditionDecision wildcard = PreconditionSemantics.EvaluateIfMatch(
            Request(HttpHeaderNames.IfMatch, "*"),
            existing);
        HttpPreconditionContext missing = Context(HttpMethods.Put, false, false);
        HttpPreconditionDecision missingWildcard = PreconditionSemantics.EvaluateIfMatch(
            Request(HttpHeaderNames.IfMatch, "*"),
            missing);

        Assert.Multiple(() =>
        {
            Assert.That(strong.Outcome, Is.EqualTo(HttpPreconditionOutcome.Proceed));
            Assert.That(weak.Outcome,
                Is.EqualTo(HttpPreconditionOutcome.PreconditionFailed));
            Assert.That(weak.StatusCode, Is.EqualTo(HttpStatusCodes.PreconditionFailed));
            Assert.That(wildcard.Outcome, Is.EqualTo(HttpPreconditionOutcome.Proceed));
            Assert.That(missingWildcard.Outcome,
                Is.EqualTo(HttpPreconditionOutcome.PreconditionFailed));
        });
    }

    [Test]
    public void MalformedIfMatchAndIfNoneMatchAreRejected()
    {
        HttpPreconditionContext context = Context(HttpMethods.Get, true, false);
        context.EntityTag = Tag("current", false);

        HttpPreconditionDecision match = PreconditionSemantics.EvaluateIfMatch(
            Request(HttpHeaderNames.IfMatch, "bad"),
            context);
        HttpPreconditionDecision none = PreconditionSemantics.EvaluateIfNoneMatch(
            Request(HttpHeaderNames.IfNoneMatch, "*, \"tag\""),
            context);

        Assert.Multiple(() =>
        {
            Assert.That(match.Outcome, Is.EqualTo(HttpPreconditionOutcome.Rejected));
            Assert.That(match.DecidingCondition, Is.EqualTo(HttpHeaderNames.IfMatch));
            Assert.That(none.Outcome, Is.EqualTo(HttpPreconditionOutcome.Rejected));
            Assert.That(none.DecidingCondition, Is.EqualTo(HttpHeaderNames.IfNoneMatch));
        });
    }

    [Test]
    public void IfNoneMatchUsesWeakComparisonAndMethodSpecificFailure()
    {
        HttpPreconditionContext get = Context(HttpMethods.Get, true, false);
        get.EntityTag = Tag("current", false);
        HttpPreconditionContext put = Context(HttpMethods.Put, true, false);
        put.EntityTag = Tag("current", true);
        HttpPreconditionDecision getResult = PreconditionSemantics.EvaluateIfNoneMatch(
            Request(HttpHeaderNames.IfNoneMatch, "W/\"current\""),
            get);
        HttpPreconditionDecision putResult = PreconditionSemantics.EvaluateIfNoneMatch(
            Request(HttpHeaderNames.IfNoneMatch, "\"current\""),
            put);
        HttpPreconditionDecision missing = PreconditionSemantics.EvaluateIfNoneMatch(
            Request(HttpHeaderNames.IfNoneMatch, "*"),
            Context(HttpMethods.Put, false, false));

        Assert.Multiple(() =>
        {
            Assert.That(getResult.Outcome, Is.EqualTo(HttpPreconditionOutcome.NotModified));
            Assert.That(getResult.StatusCode, Is.EqualTo(HttpStatusCodes.NotModified));
            Assert.That(putResult.Outcome,
                Is.EqualTo(HttpPreconditionOutcome.PreconditionFailed));
            Assert.That(missing.Outcome, Is.EqualTo(HttpPreconditionOutcome.Proceed));
        });
    }

    [Test]
    public void IfModifiedSinceUsesEarlierOrEqualSecondPrecision()
    {
        HttpPreconditionContext context = Context(HttpMethods.Get, true, true);
        HttpPreconditionDecision earlier = PreconditionSemantics.EvaluateIfModifiedSince(
            Request(HttpHeaderNames.IfModifiedSince, EarlierDate),
            context);
        HttpPreconditionDecision equal = PreconditionSemantics.EvaluateIfModifiedSince(
            Request(HttpHeaderNames.IfModifiedSince, CurrentDate),
            context);
        HttpPreconditionDecision later = PreconditionSemantics.EvaluateIfModifiedSince(
            Request(HttpHeaderNames.IfModifiedSince, LaterDate),
            context);

        Assert.Multiple(() =>
        {
            Assert.That(earlier.Outcome, Is.EqualTo(HttpPreconditionOutcome.Proceed));
            Assert.That(equal.Outcome, Is.EqualTo(HttpPreconditionOutcome.NotModified));
            Assert.That(later.Outcome, Is.EqualTo(HttpPreconditionOutcome.NotModified));
        });
    }

    [Test]
    public void IfModifiedSinceIgnoresInvalidWrongMethodAndUnavailableDate()
    {
        HttpPreconditionDecision invalid = PreconditionSemantics.EvaluateIfModifiedSince(
            Request(HttpHeaderNames.IfModifiedSince, "invalid"),
            Context(HttpMethods.Get, true, true));
        HttpPreconditionDecision post = PreconditionSemantics.EvaluateIfModifiedSince(
            Request(HttpHeaderNames.IfModifiedSince, CurrentDate),
            Context(HttpMethods.Post, true, true));
        HttpPreconditionDecision unavailable = PreconditionSemantics.EvaluateIfModifiedSince(
            Request(HttpHeaderNames.IfModifiedSince, CurrentDate),
            Context(HttpMethods.Get, true, false));

        Assert.Multiple(() =>
        {
            Assert.That(invalid.Outcome, Is.EqualTo(HttpPreconditionOutcome.Proceed));
            Assert.That(invalid.DecidingCondition, Is.Null);
            Assert.That(post.DecidingCondition, Is.Null);
            Assert.That(unavailable.DecidingCondition, Is.Null);
        });
    }

    [Test]
    public void IfUnmodifiedSinceUsesEarlierOrEqualRule()
    {
        HttpPreconditionContext context = Context(HttpMethods.Put, true, true);
        HttpPreconditionDecision earlier = PreconditionSemantics.EvaluateIfUnmodifiedSince(
            Request(HttpHeaderNames.IfUnmodifiedSince, EarlierDate),
            context);
        HttpPreconditionDecision equal = PreconditionSemantics.EvaluateIfUnmodifiedSince(
            Request(HttpHeaderNames.IfUnmodifiedSince, CurrentDate),
            context);
        HttpPreconditionDecision later = PreconditionSemantics.EvaluateIfUnmodifiedSince(
            Request(HttpHeaderNames.IfUnmodifiedSince, LaterDate),
            context);

        Assert.Multiple(() =>
        {
            Assert.That(earlier.Outcome,
                Is.EqualTo(HttpPreconditionOutcome.PreconditionFailed));
            Assert.That(earlier.StatusCode, Is.EqualTo(HttpStatusCodes.PreconditionFailed));
            Assert.That(equal.Outcome, Is.EqualTo(HttpPreconditionOutcome.Proceed));
            Assert.That(later.Outcome, Is.EqualTo(HttpPreconditionOutcome.Proceed));
        });
    }

    [Test]
    public void IfUnmodifiedSinceIgnoresInvalidRepeatedAndUnavailableDates()
    {
        HttpRequest repeated = Request(HttpHeaderNames.IfUnmodifiedSince, CurrentDate);
        AddField(repeated.Headers, HttpHeaderNames.IfUnmodifiedSince, LaterDate);
        HttpPreconditionDecision invalid = PreconditionSemantics.EvaluateIfUnmodifiedSince(
            Request(HttpHeaderNames.IfUnmodifiedSince, "invalid"),
            Context(HttpMethods.Put, true, true));
        HttpPreconditionDecision repeatedResult =
            PreconditionSemantics.EvaluateIfUnmodifiedSince(
                repeated,
                Context(HttpMethods.Put, true, true));
        HttpPreconditionDecision unavailable =
            PreconditionSemantics.EvaluateIfUnmodifiedSince(
                Request(HttpHeaderNames.IfUnmodifiedSince, EarlierDate),
                Context(HttpMethods.Put, true, false));

        Assert.Multiple(() =>
        {
            Assert.That(invalid.DecidingCondition, Is.Null);
            Assert.That(repeatedResult.DecidingCondition, Is.Null);
            Assert.That(unavailable.DecidingCondition, Is.Null);
        });
    }

    [Test]
    public void IfRangeStrongEntityTagControlsRangeApplication()
    {
        HttpPreconditionContext context = Context(HttpMethods.Get, true, false);
        context.EntityTag = Tag("current", false);
        HttpPreconditionDecision match = PreconditionSemantics.EvaluateIfRange(
            RangeRequest("\"current\""),
            context);
        HttpPreconditionDecision mismatch = PreconditionSemantics.EvaluateIfRange(
            RangeRequest("\"other\""),
            context);
        HttpPreconditionDecision weak = PreconditionSemantics.EvaluateIfRange(
            RangeRequest("W/\"current\""),
            context);

        Assert.Multiple(() =>
        {
            Assert.That(match.Outcome, Is.EqualTo(HttpPreconditionOutcome.Proceed));
            Assert.That(match.ApplyRange, Is.True);
            Assert.That(mismatch.Outcome, Is.EqualTo(HttpPreconditionOutcome.IgnoreRange));
            Assert.That(mismatch.ApplyRange, Is.False);
            Assert.That(weak.Outcome, Is.EqualTo(HttpPreconditionOutcome.IgnoreRange));
        });
    }

    [Test]
    public void IfRangeDateMustExactlyMatchAndBeStrong()
    {
        HttpPreconditionContext strong = Context(HttpMethods.Get, true, true);
        strong.HasDate = true;
        strong.Date = DateTimeOffset.Parse("1994-10-29T19:43:32+00:00");
        HttpPreconditionContext weak = Context(HttpMethods.Get, true, true);
        weak.HasDate = true;
        weak.Date = weak.LastModified;

        HttpPreconditionDecision accepted = PreconditionSemantics.EvaluateIfRange(
            RangeRequest(CurrentDate),
            strong);
        HttpPreconditionDecision stale = PreconditionSemantics.EvaluateIfRange(
            RangeRequest(EarlierDate),
            strong);
        HttpPreconditionDecision weakDate = PreconditionSemantics.EvaluateIfRange(
            RangeRequest(CurrentDate),
            weak);

        Assert.Multiple(() =>
        {
            Assert.That(accepted.ApplyRange, Is.True);
            Assert.That(stale.Outcome, Is.EqualTo(HttpPreconditionOutcome.IgnoreRange));
            Assert.That(weakDate.Outcome, Is.EqualTo(HttpPreconditionOutcome.IgnoreRange));
        });
    }

    [Test]
    public void IfRangeIsIgnoredWithoutRangeAndAbsentValidatorAllowsRange()
    {
        HttpPreconditionContext context = Context(HttpMethods.Get, true, false);
        context.EntityTag = Tag("current", false);
        HttpPreconditionDecision noRange = PreconditionSemantics.EvaluateIfRange(
            Request(HttpHeaderNames.IfRange, "\"other\""),
            context);
        HttpRequest rangeOnly = new HttpRequest();
        AddField(rangeOnly.Headers, HttpHeaderNames.Range, "bytes=0-9");
        HttpPreconditionDecision noValidator = PreconditionSemantics.EvaluateIfRange(
            rangeOnly,
            context);
        HttpPreconditionDecision malformed = PreconditionSemantics.EvaluateIfRange(
            RangeRequest("invalid"),
            context);

        Assert.Multiple(() =>
        {
            Assert.That(noRange.Outcome, Is.EqualTo(HttpPreconditionOutcome.Proceed));
            Assert.That(noRange.ApplyRange, Is.False);
            Assert.That(noValidator.ApplyRange, Is.True);
            Assert.That(malformed.Outcome, Is.EqualTo(HttpPreconditionOutcome.IgnoreRange));
        });
    }

    static HttpPreconditionContext Context(
        string method,
        bool exists,
        bool hasLastModified)
    {
        HttpPreconditionContext context = new HttpPreconditionContext();
        context.Method = method;
        context.RepresentationExists = exists;
        context.HasLastModified = hasLastModified;
        context.LastModified = DateTimeOffset.Parse("1994-10-29T19:43:31+00:00");
        return context;
    }

    static HttpEntityTag Tag(string value, bool weak)
    {
        HttpEntityTag tag = new HttpEntityTag();
        tag.OpaqueTag = Encoding.ASCII.GetBytes(value);
        tag.IsWeak = weak;
        return tag;
    }

    static HttpRequest RangeRequest(string ifRange)
    {
        HttpRequest request = Request(HttpHeaderNames.IfRange, ifRange);
        AddField(request.Headers, HttpHeaderNames.Range, "bytes=0-9");
        return request;
    }

    static HttpRequest Request(string name, string value)
    {
        HttpRequest request = new HttpRequest();
        AddField(request.Headers, name, value);
        return request;
    }

    static HttpHeaders Headers(string name, string value)
    {
        HttpHeaders headers = new HttpHeaders();
        AddField(headers, name, value);
        return headers;
    }

    static void AddField(HttpHeaders headers, string name, string value)
    {
        HttpHeaderField field = new HttpHeaderField();
        field.Name = name;
        field.Value = Encoding.ASCII.GetBytes(value);
        headers.Fields.Add(field);
    }
}
