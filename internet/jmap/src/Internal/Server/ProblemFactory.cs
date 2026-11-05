using System;
using System.IO;

namespace jmap;

internal class ProblemFactory
{
    internal const int RequestErrorStatus = 400;
    internal JmapJsonReader Reader = new JmapJsonReader(JmapProtocolLimits.SuggestedMinimumMaxSizeRequest, 64);
    internal ProtocolJsonCodec Protocol;
    internal ValueValidator Values = new ValueValidator();

    internal ProblemFactory()
    {
        Protocol = new ProtocolJsonCodec(Reader);
    }

    internal JmapEndpointResponse CreateProblemResponse(JmapProblemKind kind, string? detail, string? limit)
    {
        JmapProblem problem = CreateProblemDetails(kind, detail, limit);
        return EncodeProblemResponse(problem);
    }

    internal JmapEndpointResponse CreateHttpProblemResponse(
        int status,
        string title,
        string? detail,
        string? type,
        string? limit)
    {
        if (status < 400 || status > 599)
        {
            throw new ArgumentOutOfRangeException("status");
        }

        if (title.Length == 0 || !Values.IsValidUnicode(title) ||
            (detail != null && !Values.IsValidUnicode(detail)) ||
            (type != null && !Values.IsValidUnicode(type)) ||
            (limit != null && !Values.IsValidUnicode(limit)))
        {
            throw new ArgumentException("HTTP problem metadata is invalid.");
        }

        JmapProblem problem = new JmapProblem();
        problem.Type = type;
        problem.Title = title;
        problem.Status = status;
        problem.HasStatus = true;
        problem.Detail = detail;
        problem.Limit = limit;
        return EncodeProblemResponse(problem);
    }

    internal JmapEndpointResponse EncodeProblemResponse(JmapProblem problem)
    {
        using MemoryStream stream = new MemoryStream();
        JmapJsonWriter writer = new JmapJsonWriter(stream);
        Protocol.EncodeProblem(problem, writer);
        writer.DisposeWriter();
        byte[] body = stream.ToArray();

        JmapEndpointResponse response = new JmapEndpointResponse();
        response.StatusCode = problem.Status;
        response.ContentType = JmapMediaTypes.ProblemJson;
        response.BodyBytes = body;
        response.ContentLength = body.Length;
        response.HasContentLength = true;
        return response;
    }

    internal JmapEndpointResponse CreateValidationResponse(RequestValidationResult? validation)
    {
        if (validation == null)
        {
            throw new ArgumentNullException("validation");
        }

        if (validation.IsValid)
        {
            throw new ArgumentException("A valid request does not have a problem response.", "validation");
        }

        return CreateProblemResponse(validation.ProblemKind, validation.Detail, validation.Limit);
    }

    internal JmapProblem CreateProblemDetails(JmapProblemKind kind, string? detail, string? limit)
    {
        if (detail != null && !Values.IsValidUnicode(detail))
        {
            throw new ArgumentException("Problem detail must be valid Unicode.", "detail");
        }

        if (kind == JmapProblemKind.Limit)
        {
            if (limit == null || limit.Length == 0 || !Values.IsValidUnicode(limit))
            {
                throw new ArgumentException("A limit problem requires the applied limit name.", "limit");
            }
        }
        else if (limit != null)
        {
            throw new ArgumentException("Only a limit problem may contain an applied limit name.", "limit");
        }

        JmapProblem problem = new JmapProblem();
        problem.Type = ProblemType(kind);
        problem.Kind = kind;
        problem.Title = ProblemTitle(kind);
        problem.Status = RequestErrorStatus;
        problem.HasStatus = true;
        problem.Detail = detail;
        problem.Limit = limit;
        return problem;
    }

    internal string ProblemType(JmapProblemKind kind)
    {
        switch (kind)
        {
            case JmapProblemKind.UnknownCapability:
                return JmapErrorCodes.UnknownCapabilityProblemType;
            case JmapProblemKind.NotJson:
                return JmapErrorCodes.NotJsonProblemType;
            case JmapProblemKind.NotRequest:
                return JmapErrorCodes.NotRequestProblemType;
            case JmapProblemKind.Limit:
                return JmapErrorCodes.LimitProblemType;
            default:
                throw new ArgumentOutOfRangeException("kind");
        }
    }

    internal string ProblemTitle(JmapProblemKind kind)
    {
        switch (kind)
        {
            case JmapProblemKind.UnknownCapability:
                return "Unknown Capability";
            case JmapProblemKind.NotJson:
                return "Not JSON";
            case JmapProblemKind.NotRequest:
                return "Not a JMAP Request";
            case JmapProblemKind.Limit:
                return "Request Limit Exceeded";
            default:
                throw new ArgumentOutOfRangeException("kind");
        }
    }
}
