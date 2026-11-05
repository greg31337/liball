using System;
using System.Globalization;

namespace fake;

internal static class HttpValueGenerator
{
    static readonly string[] MethodValues = new string[]
    {
        "CONNECT", "DELETE", "GET", "HEAD", "OPTIONS",
        "PATCH", "POST", "PUT", "TRACE"
    };

    static readonly string[] HeaderValues = new string[]
    {
        "Accept", "Accept-Encoding", "Authorization", "Cache-Control",
        "Connection", "Content-Encoding", "Content-Length", "Content-Type",
        "Cookie", "Date", "ETag", "Expect", "Host", "If-Match",
        "If-Modified-Since", "If-None-Match", "If-Range",
        "If-Unmodified-Since", "Last-Modified", "Location", "Origin",
        "Range", "Referer", "Retry-After", "Server", "Set-Cookie",
        "Transfer-Encoding", "Upgrade", "User-Agent", "Vary",
        "WWW-Authenticate"
    };

    static readonly int[] StatusCodes = new int[]
    {
        100, 101, 102, 103,
        200, 201, 202, 203, 204, 205, 206, 207, 208, 226,
        300, 301, 302, 303, 304, 305, 307, 308,
        400, 401, 402, 403, 404, 405, 406, 407, 408, 409, 410,
        411, 412, 413, 414, 415, 416, 417, 418, 421, 422, 423,
        424, 425, 426, 428, 429, 431, 451,
        500, 501, 502, 503, 504, 505, 506, 507, 508, 510, 511
    };

    static readonly string[] ReasonPhrases = new string[]
    {
        "Continue", "Switching Protocols", "Processing", "Early Hints",
        "OK", "Created", "Accepted", "Non-Authoritative Information",
        "No Content", "Reset Content", "Partial Content", "Multi-Status",
        "Already Reported", "IM Used",
        "Multiple Choices", "Moved Permanently", "Found", "See Other",
        "Not Modified", "Use Proxy", "Temporary Redirect", "Permanent Redirect",
        "Bad Request", "Unauthorized", "Payment Required", "Forbidden",
        "Not Found", "Method Not Allowed", "Not Acceptable",
        "Proxy Authentication Required", "Request Timeout", "Conflict", "Gone",
        "Length Required", "Precondition Failed", "Content Too Large",
        "URI Too Long", "Unsupported Media Type", "Range Not Satisfiable",
        "Expectation Failed", "Unused", "Misdirected Request",
        "Unprocessable Content", "Locked", "Failed Dependency", "Too Early",
        "Upgrade Required", "Precondition Required", "Too Many Requests",
        "Request Header Fields Too Large", "Unavailable For Legal Reasons",
        "Internal Server Error", "Not Implemented", "Bad Gateway",
        "Service Unavailable", "Gateway Timeout", "HTTP Version Not Supported",
        "Variant Also Negotiates", "Insufficient Storage", "Loop Detected",
        "Not Extended", "Network Authentication Required"
    };

    internal static string Method(FakeRandom random, int maximumOutputSize)
    {
        string result = MethodValues[random.IntFromMaximumExclusive(MethodValues.Length)];
        Guard.OutputSize(result.Length, maximumOutputSize, "httpMethod");
        return result;
    }

    internal static string[] Methods(int maximumCount, int maximumOutputSize)
    {
        return CopyStrings(
            MethodValues,
            maximumCount,
            maximumOutputSize,
            "httpMethods");
    }

    internal static string Header(FakeRandom random, int maximumOutputSize)
    {
        string result = HeaderValues[random.IntFromMaximumExclusive(HeaderValues.Length)];
        Guard.OutputSize(result.Length, maximumOutputSize, "httpHeader");
        return result;
    }

    internal static string[] Headers(int maximumCount, int maximumOutputSize)
    {
        return CopyStrings(
            HeaderValues,
            maximumCount,
            maximumOutputSize,
            "httpHeaders");
    }

    internal static HttpHeaderRecord GeneratedHeaderRecord(
        GenerationContext context,
        FakeRandom random)
    {
        string name = Header(
            random,
            context.CapturedOptions.MaximumOutputSize);
        return KnownHeaderRecord(context, name);
    }

    internal static HttpHeaderRecord KnownHeaderRecord(
        GenerationContext context,
        string name)
    {
        string canonical = CanonicalHeader(name);
        string date = context.ReferenceTime()
            .ToUniversalTime()
            .ToString("R", CultureInfo.InvariantCulture);
        string value;
        switch (canonical)
        {
            case "Accept":
                value = "application/json";
                break;
            case "Accept-Encoding":
                value = "gzip";
                break;
            case "Authorization":
                value = "Bearer synthetic-token";
                break;
            case "Cache-Control":
                value = "no-store";
                break;
            case "Connection":
                value = "close";
                break;
            case "Content-Encoding":
                value = "identity";
                break;
            case "Content-Length":
                value = "0";
                break;
            case "Content-Type":
                value = "application/json";
                break;
            case "Cookie":
                value = "session=synthetic";
                break;
            case "Date":
            case "If-Modified-Since":
            case "If-Unmodified-Since":
            case "Last-Modified":
                value = date;
                break;
            case "ETag":
            case "If-Match":
            case "If-None-Match":
            case "If-Range":
                value = "\"synthetic\"";
                break;
            case "Expect":
                value = "100-continue";
                break;
            case "Host":
                value = "example.com";
                break;
            case "Location":
            case "Referer":
                value = "https://example.com/fixture";
                break;
            case "Origin":
                value = "https://example.com";
                break;
            case "Range":
                value = "bytes=0-99";
                break;
            case "Retry-After":
                value = "120";
                break;
            case "Server":
                value = "fake/1.0";
                break;
            case "Set-Cookie":
                value = "session=synthetic; Secure; HttpOnly; SameSite=Strict";
                break;
            case "Transfer-Encoding":
                value = "chunked";
                break;
            case "Upgrade":
                value = "websocket";
                break;
            case "User-Agent":
                value = "fake/1.0";
                break;
            case "Vary":
                value = "Accept";
                break;
            case "WWW-Authenticate":
                value = "Bearer realm=\"synthetic\"";
                break;
            default:
                throw FakeException.Create(
                    "A supported HTTP field name has no synthetic value fixture.");
        }
        return ExplicitHeaderRecord(
            canonical,
            value,
            context.CapturedOptions.MaximumOutputSize);
    }

    internal static HttpHeaderRecord ExplicitHeaderRecord(
        string name,
        string value,
        int maximumOutputSize)
    {
        ValidateHeaderName(name, maximumOutputSize);
        ValidateHeaderValue(value, maximumOutputSize);
        string text = name + ": " + value;
        Guard.OutputSize(text.Length, maximumOutputSize, "httpHeaderRecord");
        HttpHeaderRecord result = new HttpHeaderRecord();
        result.Name = name;
        result.Value = value;
        result.Text = text;
        result.IsSynthetic = true;
        result.RepresentsLiveCredential = false;
        return result;
    }

    internal static int Status(FakeRandom random)
    {
        return StatusCodes[random.IntFromMaximumExclusive(StatusCodes.Length)];
    }

    internal static HttpStatusRecord StatusRecord(
        FakeRandom random,
        int maximumOutputSize)
    {
        int index = random.IntFromMaximumExclusive(StatusCodes.Length);
        return Record(index, maximumOutputSize);
    }

    internal static HttpStatusRecord StatusRecord(
        int code,
        int maximumOutputSize)
    {
        int index = FindStatus(code);
        return Record(index, maximumOutputSize);
    }

    internal static string ReasonPhrase(int code, int maximumOutputSize)
    {
        int index = FindStatus(code);
        string result = ReasonPhrases[index];
        Guard.OutputSize(result.Length, maximumOutputSize, "statusCode");
        return result;
    }

    static HttpStatusRecord Record(int index, int maximumOutputSize)
    {
        int code = StatusCodes[index];
        string phrase = ReasonPhrases[index];
        string text = code.ToString(CultureInfo.InvariantCulture) + " " + phrase;
        Guard.OutputSize(phrase.Length, maximumOutputSize, "httpStatus");
        Guard.OutputSize(text.Length, maximumOutputSize, "httpStatus");
        HttpStatusRecord result = new HttpStatusRecord();
        result.Code = code;
        result.ReasonPhrase = phrase;
        result.StatusClass = code / 100;
        result.IsError = code >= 400;
        result.Text = text;
        return result;
    }

    static int FindStatus(int code)
    {
        int index = 0;
        while (index < StatusCodes.Length)
        {
            if (StatusCodes[index] == code)
            {
                return index;
            }
            index = index + 1;
        }
        throw new ArgumentOutOfRangeException(
            "statusCode",
            code,
            "statusCode is not in the supported HTTP status set.");
    }

    static string CanonicalHeader(string name)
    {
        ValidateHeaderName(name, int.MaxValue);
        int index = 0;
        while (index < HeaderValues.Length)
        {
            if (string.Equals(
                HeaderValues[index],
                name,
                StringComparison.OrdinalIgnoreCase))
            {
                return HeaderValues[index];
            }
            index++;
        }
        throw new ArgumentException(
            "name is not in the supported HTTP field-name set.",
            "name");
    }

    static void ValidateHeaderName(string name, int maximumOutputSize)
    {
        name = Guard.NotNullOrWhiteSpace(name, "name");
        UnicodeText.Validate(name, "name");
        int index = 0;
        while (index < name.Length)
        {
            char value = name[index];
            bool valid = value >= '0' && value <= '9';
            valid = valid || value >= 'A' && value <= 'Z';
            valid = valid || value >= 'a' && value <= 'z';
            valid = valid || value == '!' || value == '#' ||
                value == '$' || value == '%' || value == '&' ||
                value == '\'' || value == '*' || value == '+' ||
                value == '-' || value == '.' || value == '^' ||
                value == '_' || value == '`' || value == '|' ||
                value == '~';
            if (!valid)
            {
                throw new ArgumentException(
                    "name must be a nonempty ASCII HTTP field-name token.",
                    "name");
            }
            index++;
        }
        Guard.OutputSize(name.Length, maximumOutputSize, "name");
    }

    static void ValidateHeaderValue(string value, int maximumOutputSize)
    {
        Guard.NotNull(value, "value");
        UnicodeText.Validate(value, "value");
        int index = 0;
        while (index < value.Length)
        {
            char character = value[index];
            if (character != '\t' &&
                (character < ' ' || character > '~'))
            {
                throw new ArgumentException(
                    "value may contain only visible ASCII characters and horizontal tabs.",
                    "value");
            }
            index++;
        }
        Guard.OutputSize(value.Length, maximumOutputSize, "value");
    }

    static string[] CopyStrings(
        string[] values,
        int maximumCount,
        int maximumOutputSize,
        string parameterName)
    {
        Guard.Count(values.Length, maximumCount, parameterName);
        string[] result = new string[values.Length];
        Array.Copy(values, result, values.Length);
        int total = 0;
        int index = 0;
        while (index < result.Length)
        {
            total = total + result[index].Length;
            index = index + 1;
        }
        Guard.OutputSize(total, maximumOutputSize, parameterName);
        return result;
    }
}
