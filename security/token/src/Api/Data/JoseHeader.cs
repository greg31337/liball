using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;

namespace token;

/// <summary>
/// Represents registered and private JOSE header parameters as an ordered JSON object.
/// </summary>
public class JoseHeader
{
    /// <summary>Contains the header parameters in their current order.</summary>
    public JsonObject Parameters;

    /// <summary>Initializes an empty JOSE header.</summary>
    public JoseHeader()
    {
        Parameters = JsonObject.CreateEmpty();
    }

    /// <summary>Creates a JOSE header from an existing JSON object.</summary>
    /// <param name="parameters">The header parameters to copy.</param>
    /// <returns>A header containing an independent copy of the parameters.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <c>parameters</c> is null.</exception>
    internal static JoseHeader Create(JsonObject parameters)
    {
        if (parameters == null)
        {
            throw new ArgumentNullException("parameters");
        }

        JoseHeader result = new JoseHeader();
        result.Parameters = new JsonObject(parameters.Members);
        return result;
    }

    /// <summary>Reads a strict JOSE header JSON object with a maximum depth of 64.</summary>
    /// <param name="json">The JOSE header JSON text.</param>
    /// <returns>A result containing the parsed header or stable failure details.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <c>json</c> is null.</exception>
    internal static JoseHeaderResult Read(string json)
    {
        return Read(json, 64);
    }

    /// <summary>Reads a strict JOSE header JSON object with an explicit nesting limit.</summary>
    /// <param name="json">The JOSE header JSON text.</param>
    /// <param name="maximumDepth">The maximum number of nested objects and arrays.</param>
    /// <returns>A result containing the parsed header or stable failure details.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <c>json</c> is null.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <c>maximumDepth</c> is negative.</exception>
    internal static JoseHeaderResult Read(string json, int maximumDepth)
    {
        if (json == null)
        {
            throw new ArgumentNullException("json");
        }

        JsonObjectResult parsed = JsonObject.Read(json, maximumDepth);
        if (!parsed.Success)
        {
            return Failure(parsed.Error);
        }

        return FromObject((parsed.Object ?? throw new InvalidOperationException("A required value is null.")));
    }

    /// <summary>Reads strict UTF-8 JOSE header bytes with a maximum depth of 64.</summary>
    /// <param name="utf8">The JOSE header JSON bytes.</param>
    /// <returns>A result containing the parsed header or stable failure details.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <c>utf8</c> is null.</exception>
    internal static JoseHeaderResult Read(byte[] utf8)
    {
        return Read(utf8, 64);
    }

    /// <summary>Reads strict UTF-8 JOSE header bytes with an explicit nesting limit.</summary>
    /// <param name="utf8">The JOSE header JSON bytes.</param>
    /// <param name="maximumDepth">The maximum number of nested objects and arrays.</param>
    /// <returns>A result containing the parsed header or stable failure details.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <c>utf8</c> is null.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <c>maximumDepth</c> is negative.</exception>
    internal static JoseHeaderResult Read(byte[] utf8, int maximumDepth)
    {
        if (utf8 == null)
        {
            throw new ArgumentNullException("utf8");
        }

        JsonObjectResult parsed = JsonObject.Read(utf8, maximumDepth);
        if (!parsed.Success)
        {
            return Failure(parsed.Error);
        }

        return FromObject((parsed.Object ?? throw new InvalidOperationException("A required value is null.")));
    }

    /// <summary>Creates a validated JOSE header from a JSON object model.</summary>
    /// <param name="parameters">The header parameters to validate and copy.</param>
    /// <returns>A result containing the header or stable failure details.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <c>parameters</c> is null.</exception>
    internal static JoseHeaderResult FromObject(JsonObject parameters)
    {
        if (parameters == null)
        {
            throw new ArgumentNullException("parameters");
        }

        TokenResult validation = Validate(parameters);
        if (!validation.Success)
        {
            return Failure(validation.Error);
        }

        JoseHeaderResult result = new JoseHeaderResult();
        result.Header = JoseHeader.Create(parameters);
        result.Success = true;
        return result;
    }

    /// <summary>Finds a header parameter by its case-sensitive name.</summary>
    /// <param name="name">The registered or private parameter name.</param>
    /// <returns>The matching JSON value, or <c>null</c> when the parameter is absent.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <c>name</c> is null.</exception>
    internal JsonValue? Get(string name)
    {
        if (name == null)
        {
            throw new ArgumentNullException("name");
        }

        if (Parameters == null)
        {
            return null;
        }

        return Parameters.Get(name);
    }

    /// <summary>Adds or replaces a header parameter using ordinal name comparison.</summary>
    /// <param name="name">The registered or private parameter name.</param>
    /// <param name="value">The JSON value to store.</param>
    /// <exception cref="ArgumentNullException">Thrown when an argument is null.</exception>
    internal void Set(string name, JsonValue value)
    {
        if (name == null)
        {
            throw new ArgumentNullException("name");
        }

        if (value == null)
        {
            throw new ArgumentNullException("value");
        }

        if (Parameters == null)
        {
            Parameters = JsonObject.CreateEmpty();
        }

        int index = 0;
        while (index < Parameters.Members.Length)
        {
            JsonMember member = Parameters.Members[index];
            if (member != null
                && string.Equals(member.Name, name, StringComparison.Ordinal))
            {
                member.Value = value;
                return;
            }

            index += 1;
        }

        JsonMember[] expanded = new JsonMember[Parameters.Members.Length + 1];
        Parameters.Members.AsSpan().CopyTo(expanded);
        expanded[expanded.Length - 1] = new JsonMember(name, value);
        Parameters.Members = expanded;
    }

    /// <summary>Removes a header parameter using ordinal name comparison.</summary>
    /// <param name="name">The registered or private parameter name.</param>
    /// <returns><c>true</c> when a parameter was removed; otherwise, <c>false</c>.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <c>name</c> is null.</exception>
    internal bool Remove(string name)
    {
        if (name == null)
        {
            throw new ArgumentNullException("name");
        }

        if (Parameters == null)
        {
            return false;
        }

        int removed = -1;
        int index = 0;
        while (index < Parameters.Members.Length)
        {
            JsonMember member = Parameters.Members[index];
            if (member != null
                && string.Equals(member.Name, name, StringComparison.Ordinal))
            {
                removed = index;
                break;
            }

            index += 1;
        }

        if (removed < 0)
        {
            return false;
        }

        JsonMember[] reduced = new JsonMember[Parameters.Members.Length - 1];
        index = 0;
        int destination = 0;
        while (index < Parameters.Members.Length)
        {
            if (index != removed)
            {
                reduced[destination] = Parameters.Members[index];
                destination += 1;
            }

            index += 1;
        }

        Parameters.Members = reduced;
        return true;
    }

    /// <summary>Writes the header as compact JSON while preserving parameter order.</summary>
    /// <returns>A result containing compact JSON or stable failure details.</returns>
    internal TokenTextResult Write()
    {
        return Write(this);
    }

    /// <summary>Writes the header as compact UTF-8 JSON while preserving parameter order.</summary>
    /// <returns>A result containing compact JSON bytes or stable failure details.</returns>
    internal TokenBytesResult WriteUtf8()
    {
        return WriteUtf8(this);
    }

    /// <summary>Writes a header as compact JSON while preserving parameter order.</summary>
    /// <param name="header">The header to write.</param>
    /// <returns>A result containing compact JSON or stable failure details.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <c>header</c> is null.</exception>
    internal static TokenTextResult Write(JoseHeader header)
    {
        if (header == null)
        {
            throw new ArgumentNullException("header");
        }

        if (header.Parameters == null)
        {
            return TextFailure(
                "invalid_header",
                "The JOSE header parameter object is missing.");
        }

        TokenResult validation = Validate(header.Parameters);
        if (!validation.Success)
        {
            return TextFailure(validation.Error);
        }

        return JoseHeaderWriter.Write(header.Parameters);
    }

    /// <summary>Writes a header as compact UTF-8 JSON while preserving parameter order.</summary>
    /// <param name="header">The header to write.</param>
    /// <returns>A result containing compact JSON bytes or stable failure details.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <c>header</c> is null.</exception>
    internal static TokenBytesResult WriteUtf8(JoseHeader header)
    {
        TokenTextResult text = Write(header);
        if (!text.Success)
        {
            TokenBytesResult failure = new TokenBytesResult();
            failure.Error = text.Error;
            return failure;
        }

        return Utf8Codec.Encode((text.Text ?? throw new InvalidOperationException("A required value is null.")));
    }

    internal static TokenResult Validate(JsonObject parameters)
    {
        if (parameters.Members == null)
        {
            return ValidationFailure(
                "invalid_header",
                "The JOSE header member array is missing.");
        }

        HashSet<string> names = new HashSet<string>(StringComparer.Ordinal);
        int index = 0;
        while (index < parameters.Members.Length)
        {
            JsonMember member = parameters.Members[index];
            if (member == null || member.Name == null || member.Value == null)
            {
                return ValidationFailure(
                    "invalid_header",
                    "A JOSE header member is incomplete.",
                    index);
            }

            if (!names.Add(member.Name))
            {
                return ValidationFailure(
                    "duplicate_json_member",
                    "The JOSE header contains a duplicate parameter name.",
                    index);
            }

            if (JoseHeaderNamesOperations.IsRegistered(member.Name)
                && !HasRegisteredType(member.Name, member.Value))
            {
                return ValidationFailure(
                    "invalid_header_parameter_type",
                    "A registered JOSE header parameter has an invalid JSON value type.",
                    index);
            }

            index += 1;
        }

        TokenResult result = new TokenResult();
        result.Success = true;
        return result;
    }

    internal static bool HasRegisteredType(string name, JsonValue value)
    {
        if (string.Equals(name, JoseHeaderNames.Jwk, StringComparison.Ordinal)
            || string.Equals(name, JoseHeaderNames.EphemeralPublicKey, StringComparison.Ordinal))
        {
            return value.Kind == TokenJsonValueKind.Object
                && value.ObjectValue != null;
        }

        if (string.Equals(name, JoseHeaderNames.Critical, StringComparison.Ordinal)
            || string.Equals(
                name,
                JoseHeaderNames.X509CertificateChain,
                StringComparison.Ordinal)
            || string.Equals(
                name,
                JoseHeaderNames.SignatureValidationToken,
                StringComparison.Ordinal)
            || string.Equals(name, JoseHeaderNames.TrustChain, StringComparison.Ordinal)
            || string.Equals(name, JoseHeaderNames.PeerTrustChain, StringComparison.Ordinal))
        {
            return IsStringArray(value);
        }

        if (string.Equals(name, JoseHeaderNames.Audience, StringComparison.Ordinal))
        {
            return value.Kind == TokenJsonValueKind.String
                && value.StringValue != null
                || IsStringArray(value);
        }

        if (string.Equals(
            name,
            JoseHeaderNames.Base64UrlEncodePayload,
            StringComparison.Ordinal))
        {
            return value.Kind == TokenJsonValueKind.Boolean;
        }

        if (string.Equals(name, JoseHeaderNames.Pbes2Count, StringComparison.Ordinal))
        {
            return value.Kind == TokenJsonValueKind.Number
                && value.NumberText != null;
        }

        return value.Kind == TokenJsonValueKind.String
            && value.StringValue != null;
    }

    internal static bool IsStringArray(JsonValue value)
    {
        if (value.Kind != TokenJsonValueKind.Array || value.ArrayValue == null)
        {
            return false;
        }

        int index = 0;
        while (index < value.ArrayValue.Length)
        {
            JsonValue item = value.ArrayValue[index];
            if (item == null || item.Kind != TokenJsonValueKind.String
                || item.StringValue == null)
            {
                return false;
            }

            index += 1;
        }

        return true;
    }

    internal static JoseHeaderResult Failure(TokenError? error)
    {
        JoseHeaderResult result = new JoseHeaderResult();
        result.Error = error;
        return result;
    }

    internal static TokenResult ValidationFailure(string code, string message)
    {
        return ValidationFailure(code, message, -1);
    }

    internal static TokenResult ValidationFailure(string code, string message, int position)
    {
        TokenResult result = new TokenResult();
        result.Error = new TokenError(code, message, position);
        return result;
    }

    internal static TokenTextResult TextFailure(string code, string message)
    {
        return TextFailure(new TokenError(code, message, -1));
    }

    internal static TokenTextResult TextFailure(TokenError? error)
    {
        TokenTextResult result = new TokenTextResult();
        result.Error = error;
        return result;
    }
}
