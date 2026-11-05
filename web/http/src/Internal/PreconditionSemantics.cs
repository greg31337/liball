using System;
using System.Collections.Generic;

namespace http;

internal static class PreconditionSemantics
{
    internal static HttpPreconditionDecision EvaluatePreconditions(
        HttpRequest request,
        HttpPreconditionContext context)
    {
        ValidateContext(request, context);
        if (!MethodUsesPreconditions(context.Method))
        {
            return CreateProceed(context, null, false);
        }

        bool hasIfMatch = HasField(request.Headers, HttpHeaderNames.IfMatch);
        if (!context.IsCacheRecipient && hasIfMatch)
        {
            HttpPreconditionDecision ifMatch = EvaluateIfMatch(request, context);
            if (ifMatch.Outcome != HttpPreconditionOutcome.Proceed)
            {
                return ifMatch;
            }
        }

        if (!context.IsCacheRecipient && !hasIfMatch &&
            HasField(request.Headers, HttpHeaderNames.IfUnmodifiedSince))
        {
            HttpPreconditionDecision ifUnmodifiedSince =
                EvaluateIfUnmodifiedSince(request, context);
            if (ifUnmodifiedSince.Outcome != HttpPreconditionOutcome.Proceed)
            {
                return ifUnmodifiedSince;
            }
        }

        bool hasIfNoneMatch = HasField(request.Headers, HttpHeaderNames.IfNoneMatch);
        if (hasIfNoneMatch)
        {
            HttpPreconditionDecision ifNoneMatch = EvaluateIfNoneMatch(request, context);
            if (ifNoneMatch.Outcome != HttpPreconditionOutcome.Proceed)
            {
                return ifNoneMatch;
            }
        }

        if (!hasIfNoneMatch && IsGetOrHead(context.Method) &&
            HasField(request.Headers, HttpHeaderNames.IfModifiedSince))
        {
            HttpPreconditionDecision ifModifiedSince =
                EvaluateIfModifiedSince(request, context);
            if (ifModifiedSince.Outcome != HttpPreconditionOutcome.Proceed)
            {
                return ifModifiedSince;
            }
        }

        if (RangeSemantics.AppliesToRequestMethod(context.Method) &&
            HasField(request.Headers, HttpHeaderNames.Range))
        {
            HttpPreconditionDecision ifRange = EvaluateIfRange(request, context);
            if (ifRange.Outcome == HttpPreconditionOutcome.IgnoreRange)
            {
                return ifRange;
            }

            return CreateProceed(context, null, ifRange.ApplyRange);
        }

        return CreateProceed(context, null, false);
    }

    internal static HttpPreconditionDecision EvaluateIfMatch(
        HttpRequest request,
        HttpPreconditionContext context)
    {
        ValidateContext(request, context);
        PreconditionFieldValue condition = PreconditionParser.ParseEntityTagCondition(
            request.Headers,
            HttpHeaderNames.IfMatch);
        if (!condition.IsPresent)
        {
            return CreateProceed(context, null, false);
        }

        if (!condition.IsValid)
        {
            return CreateRejected(context, HttpHeaderNames.IfMatch);
        }

        bool matches = condition.IsWildcard ?
            context.RepresentationExists : MatchesStrongTagList(condition.EntityTags, context);
        if (!matches)
        {
            return CreatePreconditionFailed(context, HttpHeaderNames.IfMatch);
        }

        return CreateProceed(context, HttpHeaderNames.IfMatch, false);
    }

    internal static HttpPreconditionDecision EvaluateIfNoneMatch(
        HttpRequest request,
        HttpPreconditionContext context)
    {
        ValidateContext(request, context);
        PreconditionFieldValue condition = PreconditionParser.ParseEntityTagCondition(
            request.Headers,
            HttpHeaderNames.IfNoneMatch);
        if (!condition.IsPresent)
        {
            return CreateProceed(context, null, false);
        }

        if (!condition.IsValid)
        {
            return CreateRejected(context, HttpHeaderNames.IfNoneMatch);
        }

        bool matches = condition.IsWildcard ?
            context.RepresentationExists : MatchesWeakTagList(condition.EntityTags, context);
        if (!matches)
        {
            return CreateProceed(context, HttpHeaderNames.IfNoneMatch, false);
        }

        if (IsGetOrHead(context.Method))
        {
            return CreateNotModified(context, HttpHeaderNames.IfNoneMatch);
        }

        return CreatePreconditionFailed(context, HttpHeaderNames.IfNoneMatch);
    }

    internal static HttpPreconditionDecision EvaluateIfModifiedSince(
        HttpRequest request,
        HttpPreconditionContext context)
    {
        ValidateContext(request, context);
        PreconditionFieldValue condition = PreconditionParser.ParseDateCondition(
            request.Headers,
            HttpHeaderNames.IfModifiedSince);
        if (!condition.IsPresent || !condition.IsValid ||
            !IsGetOrHead(context.Method) || !context.RepresentationExists ||
            !context.HasLastModified)
        {
            return CreateProceed(context, null, false);
        }

        if (HttpDateCodec.CompareAtSecondPrecision(
            context.LastModified,
            condition.Date) <= 0)
        {
            return CreateNotModified(context, HttpHeaderNames.IfModifiedSince);
        }

        return CreateProceed(context, HttpHeaderNames.IfModifiedSince, false);
    }

    internal static HttpPreconditionDecision EvaluateIfUnmodifiedSince(
        HttpRequest request,
        HttpPreconditionContext context)
    {
        ValidateContext(request, context);
        PreconditionFieldValue condition = PreconditionParser.ParseDateCondition(
            request.Headers,
            HttpHeaderNames.IfUnmodifiedSince);
        if (!condition.IsPresent || !condition.IsValid ||
            !context.RepresentationExists || !context.HasLastModified)
        {
            return CreateProceed(context, null, false);
        }

        if (HttpDateCodec.CompareAtSecondPrecision(
            context.LastModified,
            condition.Date) > 0)
        {
            return CreatePreconditionFailed(
                context,
                HttpHeaderNames.IfUnmodifiedSince);
        }

        return CreateProceed(context, HttpHeaderNames.IfUnmodifiedSince, false);
    }

    internal static HttpPreconditionDecision EvaluateIfRange(
        HttpRequest request,
        HttpPreconditionContext context)
    {
        ValidateContext(request, context);
        PreconditionFieldValue condition = PreconditionParser.ParseIfRangeCondition(
            request.Headers);
        bool hasRange = HeaderStore.FindMatchingFields(
            request.Headers,
            HttpHeaderNames.Range).Count != 0;
        if (!RangeSemantics.AppliesToRequestMethod(context.Method) || !hasRange)
        {
            return CreateProceed(context, null, false);
        }

        if (!condition.IsPresent)
        {
            return CreateProceed(context, null, true);
        }

        bool matches = false;
        if (condition.IsValid && condition.IsEntityTag && !condition.EntityTag.IsWeak &&
            context.RepresentationExists && context.EntityTag != null)
        {
            matches = EntityTagComparer.CompareStrong(condition.EntityTag, context.EntityTag);
        }
        else if (condition.IsValid && condition.IsDate)
        {
            matches = MatchesStrongDate(condition.Date, context);
        }

        if (!matches)
        {
            return CreateIgnoreRange(context);
        }

        return CreateProceed(context, HttpHeaderNames.IfRange, true);
    }

    static bool MatchesStrongTagList(
        List<HttpEntityTag> tags,
        HttpPreconditionContext context)
    {
        if (!context.RepresentationExists || context.EntityTag == null)
        {
            return false;
        }

        int index = 0;
        while (index < tags.Count)
        {
            if (EntityTagComparer.CompareStrong(tags[index], context.EntityTag))
            {
                return true;
            }

            index = index + 1;
        }

        return false;
    }

    static bool MatchesWeakTagList(
        List<HttpEntityTag> tags,
        HttpPreconditionContext context)
    {
        if (!context.RepresentationExists || context.EntityTag == null)
        {
            return false;
        }

        int index = 0;
        while (index < tags.Count)
        {
            if (EntityTagComparer.CompareWeak(tags[index], context.EntityTag))
            {
                return true;
            }

            index = index + 1;
        }

        return false;
    }

    static bool MatchesStrongDate(
        DateTimeOffset validator,
        HttpPreconditionContext context)
    {
        if (!context.RepresentationExists || !context.HasLastModified ||
            !context.HasDate || !HttpDateCodec.EqualsAtSecondPrecision(
            validator,
            context.LastModified))
        {
            return false;
        }

        return HttpDateCodec.CompareAtSecondPrecision(
            context.Date,
            context.LastModified) > 0;
    }

    static bool IsGetOrHead(string method)
    {
        return method == HttpMethods.Get || method == HttpMethods.Head;
    }

    static bool MethodUsesPreconditions(string method)
    {
        return method != HttpMethods.Connect && method != HttpMethods.Options &&
            method != HttpMethods.Trace;
    }

    static bool HasField(HttpHeaders headers, string name)
    {
        return HeaderStore.FindMatchingFields(headers, name).Count != 0;
    }

    static void ValidateContext(HttpRequest request, HttpPreconditionContext context)
    {
        if (request == null || request.Headers == null ||
            request.Headers.Fields == null || context == null ||
            !HeaderStore.IsValidName(context.Method))
        {
            throw new HttpProtocolException(
                "The precondition evaluation context is incomplete.",
                "invalid-precondition-context");
        }

        int index = 0;
        while (index < request.Headers.Fields.Count)
        {
            HttpHeaderField field = request.Headers.Fields[index];
            if (field == null || !HeaderStore.IsValidName(field.Name) ||
                !HeaderStore.IsValidValue(field.Value))
            {
                throw new HttpProtocolException(
                    "The precondition evaluation context is malformed.",
                    "invalid-precondition-context");
            }

            index = index + 1;
        }
    }

    static HttpPreconditionDecision CreateProceed(
        HttpPreconditionContext context,
        string? condition,
        bool applyRange)
    {
        HttpPreconditionDecision result = new HttpPreconditionDecision();
        result.Outcome = HttpPreconditionOutcome.Proceed;
        result.Context = context;
        result.DecidingCondition = condition;
        result.ApplyRange = applyRange;
        return result;
    }

    static HttpPreconditionDecision CreateNotModified(
        HttpPreconditionContext context,
        string condition)
    {
        HttpPreconditionDecision result = new HttpPreconditionDecision();
        result.Outcome = HttpPreconditionOutcome.NotModified;
        result.Context = context;
        result.DecidingCondition = condition;
        result.HasStatusCode = true;
        result.StatusCode = HttpStatusCodes.NotModified;
        return result;
    }

    static HttpPreconditionDecision CreatePreconditionFailed(
        HttpPreconditionContext context,
        string condition)
    {
        HttpPreconditionDecision result = new HttpPreconditionDecision();
        result.Outcome = HttpPreconditionOutcome.PreconditionFailed;
        result.Context = context;
        result.DecidingCondition = condition;
        result.HasStatusCode = true;
        result.StatusCode = HttpStatusCodes.PreconditionFailed;
        return result;
    }

    static HttpPreconditionDecision CreateRejected(
        HttpPreconditionContext context,
        string condition)
    {
        HttpPreconditionDecision result = new HttpPreconditionDecision();
        result.Outcome = HttpPreconditionOutcome.Rejected;
        result.Context = context;
        result.DecidingCondition = condition;
        return result;
    }

    static HttpPreconditionDecision CreateIgnoreRange(HttpPreconditionContext context)
    {
        HttpPreconditionDecision result = new HttpPreconditionDecision();
        result.Outcome = HttpPreconditionOutcome.IgnoreRange;
        result.Context = context;
        result.DecidingCondition = HttpHeaderNames.IfRange;
        return result;
    }
}
