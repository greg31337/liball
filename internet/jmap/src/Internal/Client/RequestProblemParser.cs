using System;
using System.Text.Json;

namespace jmap;

internal class RequestProblemParser
{
    internal JmapJsonReader Reader = new JmapJsonReader(JmapProtocolLimits.SuggestedMinimumMaxSizeRequest, 64);
    internal ProtocolJsonCodec Protocol;
    internal ValueValidator Values = new ValueValidator();

    internal RequestProblemParser()
    {
        Protocol = new ProtocolJsonCodec(Reader);
    }

    internal JmapRequestException ParseProblemResponse(int httpStatus, string? contentType, byte[]? responseBody)
    {
        if (httpStatus < 100 || httpStatus > 599)
        {
            throw new ArgumentOutOfRangeException("httpStatus");
        }

        if (!IsProblemMediaType(contentType) || responseBody == null || responseBody.Length == 0)
        {
            return new JmapRequestException(
                "The JMAP request failed with HTTP status " + httpStatus + ".",
                httpStatus,
                null,
                null,
                default(JsonElement),
                responseBody,
                IsRetryableStatus(httpStatus));
        }

        JsonElement data = Reader.ReadUtf8Memory(responseBody);
        JmapProblem problem = Protocol.DecodeProblem(data);
        ValidateProblem(problem, data);
        string message = ProblemMessage(problem, httpStatus);
        return new JmapRequestException(
            message,
            httpStatus,
            problem.Type,
            problem,
            data.Clone(),
            responseBody,
            IsRetryableStatus(httpStatus));
    }

    internal bool IsProblemMediaType(string? contentType)
    {
        if (contentType == null)
        {
            return false;
        }

        int separator = contentType.IndexOf(';');
        string mediaType = separator < 0 ? contentType.Trim() : contentType.Substring(0, separator).Trim();
        return string.Equals(mediaType, JmapMediaTypes.ProblemJson, StringComparison.OrdinalIgnoreCase);
    }

    internal void ValidateProblem(JmapProblem problem, JsonElement data)
    {
        if (problem.HasStatus && (problem.Status < 100 || problem.Status > 599))
        {
            throw ProtocolFailure("The problem status is not an HTTP status code.", "status", data);
        }

        if (problem.Kind == JmapProblemKind.Limit &&
            (problem.Limit == null || problem.Limit.Length == 0 || !Values.IsValidUnicode(problem.Limit)))
        {
            throw ProtocolFailure("A limit problem must identify the applied limit.", "limit", data);
        }
    }

    internal string ProblemMessage(JmapProblem problem, int httpStatus)
    {
        if (problem.Detail != null && problem.Detail.Length != 0)
        {
            return problem.Detail;
        }

        if (problem.Title != null && problem.Title.Length != 0)
        {
            return problem.Title;
        }

        return "The JMAP request failed with HTTP status " + httpStatus + ".";
    }

    internal bool IsRetryableStatus(int httpStatus)
    {
        return httpStatus == 408 || httpStatus == 429 || httpStatus >= 500;
    }

    internal JmapProtocolException ProtocolFailure(string message, string path, JsonElement data)
    {
        JsonElement context = data.ValueKind == JsonValueKind.Undefined ? default(JsonElement) : data.Clone();
        return new JmapProtocolException(message, path, context);
    }
}
