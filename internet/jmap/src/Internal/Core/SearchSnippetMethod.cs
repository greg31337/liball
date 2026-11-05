using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace jmap;

internal class SearchSnippetMethod
{
    internal ValueValidator Values = new ValueValidator();
    internal JmapJsonReader Reader = new JmapJsonReader(JmapProtocolLimits.SuggestedMinimumMaxSizeRequest, 64);
    internal EmailQueryJsonCodec Codec;
    internal ProtocolJsonCodec Protocol;
    internal EmailFilterEvaluator Filters = new EmailFilterEvaluator();
    internal EmailTextMatcher Text = new EmailTextMatcher();
    internal EmailMethod Emails = new EmailMethod();
    internal ResponseMapper Responses = new ResponseMapper();
    internal ErrorFactory Errors = new ErrorFactory(null);

    internal SearchSnippetMethod()
    {
        Codec = new EmailQueryJsonCodec(Reader);
        Protocol = new ProtocolJsonCodec(Reader);
    }

    internal Task<JmapInvocation> ProcessInvocationAsync(
        JmapInvocation invocation,
        JmapSession session,
        ISearchSnippetService snippetService,
        JmapRequestContext context,
        JmapServerOptions options,
        CancellationToken cancellationToken)
    {
        return Task.Run(
            () => ProcessInvocation(invocation, session, snippetService, context, options, cancellationToken),
            cancellationToken);
    }

    internal JmapInvocation ProcessInvocation(
        JmapInvocation invocation,
        JmapSession session,
        ISearchSnippetService snippetService,
        JmapRequestContext context,
        JmapServerOptions options,
        CancellationToken cancellationToken)
    {
        ValidateInvocation(invocation);
        string callId = invocation.CallId == null ? "" : invocation.CallId;
        if (!IsRequestShape(invocation.Arguments, 64))
        {
            return ErrorInvocation(JmapErrorCodes.InvalidArguments, JmapMethodErrorKind.InvalidArguments,
                "The SearchSnippet/get arguments are invalid.", callId);
        }

        SearchSnippetGetRequest request = Codec.DecodeSearchSnippetGetRequest(invocation.Arguments);
        ValidateDispatchDependencies(session, snippetService, context, options);
        JmapMethodError? requestError = ValidateRequest(request, options.MaxObjectsInGet, options.MaxJsonDepth);
        if (requestError != null)
        {
            return ErrorInvocation(requestError.Type, requestError.Kind, requestError.Description, callId);
        }

        JmapMethodError? accountError = Emails.ValidateMailAccount(session, request.AccountId);
        if (accountError != null)
        {
            return ErrorInvocation(accountError.Type, accountError.Kind, accountError.Description, callId);
        }

        cancellationToken.ThrowIfCancellationRequested();
        SearchSnippetGetResponse response = snippetService
            .GetStoredSearchSnippetsAsync(request, context, cancellationToken).GetAwaiter().GetResult();
        ValidateResponse(response, request);
        return SuccessInvocation(response, callId);
    }

    internal JmapInvocation CreateInvocation(SearchSnippetGetRequest request, string callId)
    {
        JmapMethodError? error = ValidateRequest(request, int.MaxValue, 64);
        if (error != null || !IsValidCallId(callId))
        {
            throw ValidationFailure("Valid SearchSnippet/get arguments and a call identifier are required.",
                "request");
        }

        using MemoryStream stream = new MemoryStream();
        JmapJsonWriter writer = new JmapJsonWriter(stream);
        Codec.EncodeSearchSnippetGetRequest(request, writer);
        writer.DisposeWriter();
        return InvocationFromBytes(JmapMethods.SearchSnippetGet, callId, stream.ToArray());
    }

    internal SearchSnippetGetResponse MapResponse(
        JmapResponse response,
        string callId,
        SearchSnippetGetRequest request)
    {
        JmapMethodError? error = ValidateRequest(request, int.MaxValue, 64);
        if (error != null)
        {
            throw ValidationFailure("Valid original SearchSnippet/get arguments are required.", "request");
        }

        JmapCallResult result = Responses.FindTypedResult(response, callId, JmapMethods.SearchSnippetGet);
        JsonElement arguments = Responses.RequireSuccessfulArguments(result, JmapMethods.SearchSnippetGet);
        SearchSnippetGetResponse decoded = Codec.DecodeSearchSnippetGetResponse(arguments);
        ValidateResponse(decoded, request);
        return decoded;
    }

    internal SearchSnippet CreateSnippet(Email email, EmailFilter? filter)
    {
        if (email == null || !Values.IsId(email.Id))
        {
            throw ValidationFailure("A stored Email with a valid id is required.", "email");
        }

        JmapMethodError? filterError = Filters.ValidateFilter(filter, 64);
        if (filterError != null)
        {
            throw ValidationFailure("A valid supported Email filter is required.", "filter");
        }

        List<string> subjectQueries = new List<string>();
        List<string> bodyQueries = new List<string>();
        CollectQueries(filter, true, subjectQueries, bodyQueries);
        EmailTextMatchResult subjectMatch = MatchQueries(email.Subject, subjectQueries);
        EmailTextMatchResult bodyMatch = MatchQueries(Text.BuildEmailBodyText(email), bodyQueries);
        SearchSnippet result = new SearchSnippet();
        result.EmailId = email.Id;
        if (subjectMatch.Ranges.Count != 0)
        {
            result.Subject = BuildHighlightedWindow(subjectMatch.SearchText, subjectMatch.Ranges, 0,
                subjectMatch.SearchText.Length);
        }

        if (bodyMatch.Ranges.Count != 0)
        {
            result.Preview = BuildPreview(bodyMatch);
        }

        return result;
    }

    internal void CollectQueries(
        EmailFilter? filter,
        bool positive,
        List<string> subjectQueries,
        List<string> bodyQueries)
    {
        if (filter == null)
        {
            return;
        }

        if (filter.HasOperator)
        {
            if (filter.Conditions == null)
            {
                return;
            }

            bool childPositive = filter.Operator == JmapFilterOperator.Not ? !positive : positive;
            foreach (EmailFilter child in filter.Conditions)
            {
                CollectQueries(child, childPositive, subjectQueries, bodyQueries);
            }

            return;
        }

        if (!positive)
        {
            return;
        }

        AddQuery(filter.Text, subjectQueries);
        AddQuery(filter.Text, bodyQueries);
        AddQuery(filter.Subject, subjectQueries);
        AddQuery(filter.Body, bodyQueries);
        if (filter.Header != null && filter.Header.Count == 2 &&
            string.Equals(filter.Header[0], "Subject", StringComparison.OrdinalIgnoreCase))
        {
            AddQuery(filter.Header[1], subjectQueries);
        }
    }

    internal void AddQuery(string? value, List<string> queries)
    {
        if (value != null && value.Length != 0 && !queries.Contains(value))
        {
            queries.Add(value);
        }
    }

    internal EmailTextMatchResult MatchQueries(string? value, List<string> queries)
    {
        EmailTextMatchResult result = new EmailTextMatchResult();
        result.SearchText = Text.PrepareText(value);
        foreach (string query in queries)
        {
            List<EmailSearchTerm> terms = Text.ParseTerms(query);
            foreach (EmailSearchTerm term in terms)
            {
                Text.AddTermRanges(result.SearchText, term.Text, result.Ranges);
            }
        }

        Text.SortAndMergeRanges(result.Ranges);
        result.IsMatch = result.Ranges.Count != 0;
        return result;
    }

    internal string? BuildPreview(EmailTextMatchResult match)
    {
        if (match.Ranges.Count == 0)
        {
            return null;
        }

        EmailTextRange primary = match.Ranges[0];
        int primaryEnd = primary.Start + primary.Length;
        int start = primary.Start - 96;
        if (start < 0)
        {
            start = 0;
        }

        int end = primaryEnd + 96;
        if (end > match.SearchText.Length)
        {
            end = match.SearchText.Length;
        }

        start = SafeWindowStart(match.SearchText, start);
        end = SafeWindowEnd(match.SearchText, end);
        string value = BuildHighlightedWindow(match.SearchText, match.Ranges, start, end);
        while (Encoding.UTF8.GetByteCount(value) > JmapProtocolLimits.MaximumSearchSnippetPreviewSize &&
            (start < primary.Start || end > primaryEnd))
        {
            int left = primary.Start - start;
            int right = end - primaryEnd;
            if (left >= right && start < primary.Start)
            {
                start = NextTextIndex(match.SearchText, start);
            }
            else if (end > primaryEnd)
            {
                end = PreviousTextIndex(match.SearchText, end);
            }
            else
            {
                start = NextTextIndex(match.SearchText, start);
            }

            value = BuildHighlightedWindow(match.SearchText, match.Ranges, start, end);
        }

        if (Encoding.UTF8.GetByteCount(value) > JmapProtocolLimits.MaximumSearchSnippetPreviewSize)
        {
            return null;
        }

        return value;
    }

    internal string BuildHighlightedWindow(
        string value,
        List<EmailTextRange> ranges,
        int start,
        int end)
    {
        StringBuilder result = new StringBuilder();
        if (start > 0)
        {
            result.Append("...");
        }

        int index = start;
        foreach (EmailTextRange range in ranges)
        {
            int rangeEnd = range.Start + range.Length;
            if (range.Start < start || rangeEnd > end || range.Start < index)
            {
                continue;
            }

            AppendEscapedText(result, value, index, range.Start);
            result.Append("<mark>");
            AppendEscapedText(result, value, range.Start, rangeEnd);
            result.Append("</mark>");
            index = rangeEnd;
        }

        AppendEscapedText(result, value, index, end);
        if (end < value.Length)
        {
            result.Append("...");
        }

        return result.ToString();
    }

    internal void AppendEscapedText(StringBuilder destination, string value, int start, int end)
    {
        int index = start;
        while (index < end)
        {
            char character = value[index];
            if (character == '&')
            {
                destination.Append("&amp;");
            }
            else if (character == '<')
            {
                destination.Append("&lt;");
            }
            else if (character == '>')
            {
                destination.Append("&gt;");
            }
            else
            {
                destination.Append(character);
            }

            index = index + 1;
        }
    }

    internal int SafeWindowStart(string value, int index)
    {
        if (index > 0 && index < value.Length && char.IsLowSurrogate(value[index]) &&
            char.IsHighSurrogate(value[index - 1]))
        {
            return index - 1;
        }

        return index;
    }

    internal int SafeWindowEnd(string value, int index)
    {
        if (index > 0 && index < value.Length && char.IsHighSurrogate(value[index - 1]) &&
            char.IsLowSurrogate(value[index]))
        {
            return index + 1;
        }

        return index;
    }

    internal int NextTextIndex(string value, int index)
    {
        if (index + 1 < value.Length && char.IsHighSurrogate(value[index]) &&
            char.IsLowSurrogate(value[index + 1]))
        {
            return index + 2;
        }

        return index + 1;
    }

    internal int PreviousTextIndex(string value, int index)
    {
        if (index > 1 && char.IsLowSurrogate(value[index - 1]) && char.IsHighSurrogate(value[index - 2]))
        {
            return index - 2;
        }

        return index - 1;
    }

    internal JmapMethodError? ValidateRequest(
        SearchSnippetGetRequest? request,
        int maximumObjectsInGet,
        int maximumFilterDepth)
    {
        if (request == null || !Values.IsId(request.AccountId) || request.EmailIds == null ||
            maximumObjectsInGet < 0 || maximumFilterDepth <= 0)
        {
            return MethodError(JmapErrorCodes.InvalidArguments, JmapMethodErrorKind.InvalidArguments,
                "A valid accountId, emailIds array, and server limits are required.");
        }

        if (request.EmailIds.Count > maximumObjectsInGet)
        {
            return MethodError(JmapErrorCodes.RequestTooLarge, JmapMethodErrorKind.RequestTooLarge,
                "The SearchSnippet/get request contains too many Email identifiers.");
        }

        foreach (string emailId in request.EmailIds)
        {
            if (!Values.IsId(emailId))
            {
                return MethodError(JmapErrorCodes.InvalidArguments, JmapMethodErrorKind.InvalidArguments,
                    "The emailIds argument contains an invalid Email identifier.");
            }
        }

        return Filters.ValidateFilter(request.Filter, maximumFilterDepth);
    }

    internal void ValidateResponse(SearchSnippetGetResponse? response, SearchSnippetGetRequest request)
    {
        if (response == null || response.AccountId != request.AccountId || response.List == null)
        {
            throw ProtocolFailure("The SearchSnippet/get response has invalid account or list metadata.",
                "methodResponses/1", default(JsonElement));
        }

        HashSet<string> expected = new HashSet<string>(request.EmailIds, StringComparer.Ordinal);
        HashSet<string> seen = new HashSet<string>(StringComparer.Ordinal);
        int index = 0;
        foreach (SearchSnippet snippet in response.List)
        {
            if (snippet == null || !Values.IsId(snippet.EmailId) || snippet.EmailId == null ||
                !expected.Contains(snippet.EmailId) ||
                !seen.Add(snippet.EmailId) || !HasSafeHighlightedMarkup(snippet.Subject) ||
                !HasSafeHighlightedMarkup(snippet.Preview) || snippet.Preview != null &&
                Encoding.UTF8.GetByteCount(snippet.Preview) > JmapProtocolLimits.MaximumSearchSnippetPreviewSize)
            {
                throw ProtocolFailure("The SearchSnippet/get result list contains an invalid snippet.",
                    JmapProperties.List + "/" + index, default(JsonElement));
            }

            index = index + 1;
        }

        if (response.NotFound != null)
        {
            foreach (string emailId in response.NotFound)
            {
                if (!Values.IsId(emailId) || !expected.Contains(emailId) || !seen.Add(emailId))
                {
                    throw ProtocolFailure("The SearchSnippet/get notFound partition is invalid.",
                        JmapProperties.NotFound, default(JsonElement));
                }
            }
        }

        if (seen.Count != expected.Count || response.NotFound == null && response.List.Count != expected.Count ||
            response.NotFound != null && response.NotFound.Count == 0)
        {
            throw ProtocolFailure("The SearchSnippet/get response does not exactly partition requested Emails.",
                "methodResponses/1", default(JsonElement));
        }
    }

    internal bool HasSafeHighlightedMarkup(string? value)
    {
        if (value == null)
        {
            return true;
        }

        if (!Values.IsValidUnicode(value))
        {
            return false;
        }

        int markDepth = 0;
        bool hasMark = false;
        bool markedContent = false;
        int index = 0;
        while (index < value.Length)
        {
            if (StartsAt(value, "<mark>", index))
            {
                if (markDepth != 0)
                {
                    return false;
                }

                markDepth = 1;
                hasMark = true;
                markedContent = false;
                index = index + 6;
            }
            else if (StartsAt(value, "</mark>", index))
            {
                if (markDepth != 1 || !markedContent)
                {
                    return false;
                }

                markDepth = 0;
                index = index + 7;
            }
            else if (value[index] == '<' || value[index] == '>')
            {
                return false;
            }
            else if (value[index] == '&')
            {
                int semicolon = value.IndexOf(';', index + 1);
                if (semicolon < 0 || semicolon - index > 32)
                {
                    return false;
                }

                string entity = value.Substring(index, semicolon - index + 1);
                if (WebUtility.HtmlDecode(entity) == entity)
                {
                    return false;
                }

                if (markDepth == 1)
                {
                    markedContent = true;
                }

                index = semicolon + 1;
            }
            else
            {
                if (markDepth == 1)
                {
                    markedContent = true;
                }

                index = index + 1;
            }
        }

        return markDepth == 0 && hasMark;
    }

    internal bool StartsAt(string value, string expected, int start)
    {
        return start >= 0 && start + expected.Length <= value.Length &&
            string.Equals(value.Substring(start, expected.Length), expected, StringComparison.Ordinal);
    }

    internal bool IsRequestShape(JsonElement arguments, int maximumDepth)
    {
        JsonElement accountId = Codec.Values.GetMember(arguments, JmapProperties.AccountId);
        JsonElement filter = Codec.Values.GetMember(arguments, JmapProperties.Filter);
        JsonElement emailIds = Codec.Values.GetMember(arguments, JmapProperties.EmailIds);
        return accountId.ValueKind == JsonValueKind.String &&
            Emails.IsOptionalEmailFilterShape(filter, 1, maximumDepth) && IsStringArray(emailIds);
    }

    internal bool IsStringArray(JsonElement value)
    {
        if (value.ValueKind != JsonValueKind.Array)
        {
            return false;
        }

        foreach (JsonElement item in value.EnumerateArray())
        {
            if (item.ValueKind != JsonValueKind.String)
            {
                return false;
            }
        }

        return true;
    }

    internal void ValidateDispatchDependencies(
        JmapSession session,
        ISearchSnippetService snippetService,
        JmapRequestContext context,
        JmapServerOptions options)
    {
        if (session == null || snippetService == null || context == null || options == null ||
            session.Accounts == null || session.Capabilities == null)
        {
            throw ProtocolFailure("SearchSnippet dispatch dependencies are required.", "searchSnippetService",
                default(JsonElement));
        }
    }

    internal void ValidateInvocation(JmapInvocation? invocation)
    {
        if (invocation == null)
        {
            throw ValidationFailure("A SearchSnippet invocation is required.", "methodCalls/0");
        }

        if (invocation.Name != JmapMethods.SearchSnippetGet)
        {
            throw ValidationFailure("The invocation has the wrong SearchSnippet method name.",
                "methodCalls/0/0");
        }

        if (invocation.Arguments.ValueKind != JsonValueKind.Object)
        {
            throw ValidationFailure("SearchSnippet arguments must be a JSON object.", "methodCalls/0/1");
        }

        if (!IsValidCallId(invocation.CallId))
        {
            throw ValidationFailure("A usable SearchSnippet call identifier is required.", "methodCalls/0/2");
        }
    }

    internal JmapInvocation SuccessInvocation(SearchSnippetGetResponse response, string callId)
    {
        using MemoryStream stream = new MemoryStream();
        JmapJsonWriter writer = new JmapJsonWriter(stream);
        Codec.EncodeSearchSnippetGetResponse(response, writer);
        writer.DisposeWriter();
        return InvocationFromBytes(JmapMethods.SearchSnippetGet, callId, stream.ToArray());
    }

    internal JmapInvocation ErrorInvocation(
        string? type,
        JmapMethodErrorKind kind,
        string? description,
        string callId)
    {
        JmapMethodError error = Errors.CreateMethodError(type, description, false);
        error.Kind = kind;
        using MemoryStream stream = new MemoryStream();
        JmapJsonWriter writer = new JmapJsonWriter(stream);
        Protocol.EncodeMethodError(error, writer);
        writer.DisposeWriter();
        return InvocationFromBytes(ResponseMapper.ErrorResponseName, callId, stream.ToArray());
    }

    internal JmapInvocation InvocationFromBytes(string name, string callId, byte[] bytes)
    {
        JmapInvocation invocation = new JmapInvocation();
        invocation.Name = name;
        invocation.CallId = callId;
        invocation.Arguments = Reader.ReadUtf8Memory(bytes);
        return invocation;
    }

    internal JmapMethodError MethodError(string type, JmapMethodErrorKind kind, string description)
    {
        JmapMethodError error = new JmapMethodError();
        error.Type = type;
        error.Kind = kind;
        error.Description = description;
        return error;
    }

    internal bool IsValidCallId(string? callId)
    {
        return callId != null && callId.Length != 0 && Values.IsValidUnicode(callId);
    }

    internal JmapValidationException ValidationFailure(string message, string path)
    {
        return new JmapValidationException(message, new List<string> { path }, default(JsonElement));
    }

    internal JmapProtocolException ProtocolFailure(string message, string path, JsonElement data)
    {
        JsonElement context = data.ValueKind == JsonValueKind.Undefined ? default(JsonElement) : data.Clone();
        return new JmapProtocolException(message, path, context);
    }
}
