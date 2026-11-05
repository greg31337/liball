using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;

namespace token;

/// <summary>
/// Represents registered and private JWT claims as an ordered JSON object.
/// </summary>
public class JwtClaims
{
    /// <summary>Contains the claims in their current order.</summary>
    public JsonObject Values;

    /// <summary>Initializes an empty JWT claim set.</summary>
    public JwtClaims()
    {
        Values = JsonObject.CreateEmpty();
    }

    /// <summary>Creates a JWT claim set from an existing JSON object.</summary>
    /// <param name="values">The claim values to copy.</param>
    /// <returns>A claim set containing an independent copy of the values.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <c>values</c> is null.</exception>
    internal static JwtClaims Create(JsonObject values)
    {
        if (values == null)
        {
            throw new ArgumentNullException("values");
        }

        JwtClaims result = new JwtClaims();
        result.Values = new JsonObject(values.Members);
        return result;
    }

    /// <summary>Reads a strict JWT claim set with a maximum JSON depth of 64.</summary>
    /// <param name="json">The JWT claim-set JSON text.</param>
    /// <returns>A result containing the parsed claims or stable failure details.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <c>json</c> is null.</exception>
    internal static JwtClaimsResult Read(string json)
    {
        return Read(json, 64);
    }

    /// <summary>Reads a strict JWT claim set with an explicit JSON nesting limit.</summary>
    /// <param name="json">The JWT claim-set JSON text.</param>
    /// <param name="maximumDepth">The maximum number of nested objects and arrays.</param>
    /// <returns>A result containing the parsed claims or stable failure details.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <c>json</c> is null.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <c>maximumDepth</c> is negative.</exception>
    internal static JwtClaimsResult Read(string json, int maximumDepth)
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

    /// <summary>Reads strict UTF-8 JWT claim-set bytes with a maximum JSON depth of 64.</summary>
    /// <param name="utf8">The JWT claim-set JSON bytes.</param>
    /// <returns>A result containing the parsed claims or stable failure details.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <c>utf8</c> is null.</exception>
    internal static JwtClaimsResult Read(byte[] utf8)
    {
        return Read(utf8, 64);
    }

    /// <summary>Reads strict UTF-8 JWT claim-set bytes with an explicit JSON nesting limit.</summary>
    /// <param name="utf8">The JWT claim-set JSON bytes.</param>
    /// <param name="maximumDepth">The maximum number of nested objects and arrays.</param>
    /// <returns>A result containing the parsed claims or stable failure details.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <c>utf8</c> is null.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <c>maximumDepth</c> is negative.</exception>
    internal static JwtClaimsResult Read(byte[] utf8, int maximumDepth)
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

    /// <summary>Reads a JWT claim set while enforcing token policy size and depth limits.</summary>
    /// <param name="json">The JWT claim-set JSON text.</param>
    /// <param name="policy">The limits to enforce.</param>
    /// <returns>A result containing the parsed claims or stable failure details.</returns>
    /// <exception cref="ArgumentNullException">Thrown when an argument is null.</exception>
    internal static JwtClaimsResult Read(string json, TokenPolicy policy)
    {
        if (json == null)
        {
            throw new ArgumentNullException("json");
        }

        if (policy == null)
        {
            throw new ArgumentNullException("policy");
        }

        TokenError? policyError = ValidatePolicy(policy);
        if (policyError != null)
        {
            return Failure(policyError);
        }

        TokenBytesResult encoded = Utf8Codec.Encode(json);
        if (!encoded.Success)
        {
            return Failure(encoded.Error);
        }

        if ((encoded.Bytes ?? throw new InvalidOperationException("A required value is null.")).Length > policy.MaximumTokenSizeInBytes)
        {
            return Failure(
                "token_too_large",
                "The JWT claim set exceeds the configured token size limit.");
        }

        return Read(json, policy.MaximumJsonDepth);
    }

    /// <summary>Reads UTF-8 JWT claim-set bytes while enforcing token policy size and depth limits.</summary>
    /// <param name="utf8">The JWT claim-set JSON bytes.</param>
    /// <param name="policy">The limits to enforce.</param>
    /// <returns>A result containing the parsed claims or stable failure details.</returns>
    /// <exception cref="ArgumentNullException">Thrown when an argument is null.</exception>
    internal static JwtClaimsResult Read(byte[] utf8, TokenPolicy policy)
    {
        if (utf8 == null)
        {
            throw new ArgumentNullException("utf8");
        }

        if (policy == null)
        {
            throw new ArgumentNullException("policy");
        }

        TokenError? policyError = ValidatePolicy(policy);
        if (policyError != null)
        {
            return Failure(policyError);
        }

        if (utf8.Length > policy.MaximumTokenSizeInBytes)
        {
            return Failure(
                "token_too_large",
                "The JWT claim set exceeds the configured token size limit.");
        }

        return Read(utf8, policy.MaximumJsonDepth);
    }

    /// <summary>Creates a validated JWT claim set from a JSON object model.</summary>
    /// <param name="values">The claim values to validate and copy.</param>
    /// <returns>A result containing the claims or stable failure details.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <c>values</c> is null.</exception>
    internal static JwtClaimsResult FromObject(JsonObject values)
    {
        if (values == null)
        {
            throw new ArgumentNullException("values");
        }

        TokenError? error = Validate(values);
        if (error != null)
        {
            return Failure(error);
        }

        JwtClaimsResult result = new JwtClaimsResult();
        result.Claims = JwtClaims.Create(values);
        result.Success = true;
        return result;
    }

    /// <summary>Finds a claim by its case-sensitive name.</summary>
    /// <param name="name">The registered or private claim name.</param>
    /// <returns>The matching JSON value, or <c>null</c> when the claim is absent.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <c>name</c> is null.</exception>
    internal JsonValue? Get(string name)
    {
        if (name == null)
        {
            throw new ArgumentNullException("name");
        }

        if (Values == null)
        {
            return null;
        }

        return Values.Get(name);
    }

    /// <summary>Adds or replaces a claim using ordinal name comparison.</summary>
    /// <param name="name">The registered or private claim name.</param>
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

        if (Values == null)
        {
            Values = JsonObject.CreateEmpty();
        }

        int index = 0;
        while (index < Values.Members.Length)
        {
            JsonMember member = Values.Members[index];
            if (member != null
                && string.Equals(member.Name, name, StringComparison.Ordinal))
            {
                member.Value = value;
                return;
            }

            index += 1;
        }

        JsonMember[] expanded = new JsonMember[Values.Members.Length + 1];
        Values.Members.AsSpan().CopyTo(expanded);
        expanded[expanded.Length - 1] = new JsonMember(name, value);
        Values.Members = expanded;
    }

    /// <summary>Removes a claim using ordinal name comparison.</summary>
    /// <param name="name">The registered or private claim name.</param>
    /// <returns><c>true</c> when a claim was removed; otherwise, <c>false</c>.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <c>name</c> is null.</exception>
    internal bool Remove(string name)
    {
        if (name == null)
        {
            throw new ArgumentNullException("name");
        }

        if (Values == null)
        {
            return false;
        }

        int removed = -1;
        int index = 0;
        while (index < Values.Members.Length)
        {
            JsonMember member = Values.Members[index];
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

        JsonMember[] reduced = new JsonMember[Values.Members.Length - 1];
        index = 0;
        int destination = 0;
        while (index < Values.Members.Length)
        {
            if (index != removed)
            {
                reduced[destination] = Values.Members[index];
                destination += 1;
            }

            index += 1;
        }

        Values.Members = reduced;
        return true;
    }

    /// <summary>Writes the claims as compact JSON while preserving claim order.</summary>
    /// <returns>A result containing compact JSON or stable failure details.</returns>
    internal TokenTextResult Write()
    {
        return Write(this, 64);
    }

    /// <summary>Writes the claims as compact UTF-8 JSON while preserving claim order.</summary>
    /// <returns>A result containing compact JSON bytes or stable failure details.</returns>
    internal TokenBytesResult WriteUtf8()
    {
        return WriteUtf8(this, 64);
    }

    /// <summary>Writes the claims as compact JSON while enforcing token policy limits.</summary>
    /// <param name="policy">The limits to enforce.</param>
    /// <returns>A result containing compact JSON or stable failure details.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <c>policy</c> is null.</exception>
    internal TokenTextResult WriteWithPolicy(TokenPolicy policy)
    {
        if (policy == null)
        {
            throw new ArgumentNullException("policy");
        }

        TokenError? policyError = ValidatePolicy(policy);
        if (policyError != null)
        {
            return TextFailure(policyError);
        }

        TokenTextResult result = Write(this, policy.MaximumJsonDepth);
        if (!result.Success)
        {
            return result;
        }

        TokenBytesResult encoded = Utf8Codec.Encode((result.Text ?? throw new InvalidOperationException("A required value is null.")));
        if (!encoded.Success)
        {
            return TextFailure(encoded.Error);
        }

        if ((encoded.Bytes ?? throw new InvalidOperationException("A required value is null.")).Length > policy.MaximumTokenSizeInBytes)
        {
            return TextFailure(
                "token_too_large",
                "The JWT claim set exceeds the configured token size limit.");
        }

        return result;
    }

    /// <summary>Writes the claims as compact UTF-8 JSON while enforcing token policy limits.</summary>
    /// <param name="policy">The limits to enforce.</param>
    /// <returns>A result containing compact JSON bytes or stable failure details.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <c>policy</c> is null.</exception>
    internal TokenBytesResult WriteUtf8WithPolicy(TokenPolicy policy)
    {
        TokenTextResult text = WriteWithPolicy(policy);
        if (!text.Success)
        {
            return BytesFailure(text.Error);
        }

        return Utf8Codec.Encode((text.Text ?? throw new InvalidOperationException("A required value is null.")));
    }

    /// <summary>Writes a claim set as compact JSON while preserving claim order.</summary>
    /// <param name="claims">The claims to write.</param>
    /// <returns>A result containing compact JSON or stable failure details.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <c>claims</c> is null.</exception>
    internal static TokenTextResult Write(JwtClaims claims)
    {
        return Write(claims, 64);
    }

    /// <summary>Writes a claim set as compact UTF-8 JSON while preserving claim order.</summary>
    /// <param name="claims">The claims to write.</param>
    /// <returns>A result containing compact JSON bytes or stable failure details.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <c>claims</c> is null.</exception>
    internal static TokenBytesResult WriteUtf8(JwtClaims claims)
    {
        return WriteUtf8(claims, 64);
    }

    internal static TokenTextResult Write(JwtClaims claims, int maximumDepth)
    {
        if (claims == null)
        {
            throw new ArgumentNullException("claims");
        }

        if (claims.Values == null)
        {
            return TextFailure(
                "invalid_claim_set",
                "The JWT claim-set object is missing.");
        }

        TokenError? error = Validate(claims.Values);
        if (error != null)
        {
            return TextFailure(error);
        }

        return JwtClaimsWriter.Write(claims.Values, maximumDepth);
    }

    internal static TokenBytesResult WriteUtf8(JwtClaims claims, int maximumDepth)
    {
        TokenTextResult text = Write(claims, maximumDepth);
        if (!text.Success)
        {
            return BytesFailure(text.Error);
        }

        return Utf8Codec.Encode((text.Text ?? throw new InvalidOperationException("A required value is null.")));
    }

    internal static TokenError? Validate(JsonObject values)
    {
        if (values.Members == null)
        {
            return new TokenError(
                "invalid_claim_set",
                "The JWT claim member array is missing.",
                -1);
        }

        HashSet<string> names = new HashSet<string>(StringComparer.Ordinal);
        int index = 0;
        while (index < values.Members.Length)
        {
            JsonMember member = values.Members[index];
            if (member == null || member.Name == null || member.Value == null)
            {
                return new TokenError(
                    "invalid_claim_set",
                    "A JWT claim member is incomplete.",
                    index);
            }

            if (!names.Add(member.Name))
            {
                return new TokenError(
                    "duplicate_json_member",
                    "The JWT claim set contains a duplicate claim name.",
                    index);
            }

            if (!HasRegisteredType(member.Name, member.Value))
            {
                return new TokenError(
                    "invalid_claim_type",
                    "A registered JWT claim has an invalid JSON value type.",
                    index);
            }

            index += 1;
        }

        return null;
    }

    internal static bool HasRegisteredType(string name, JsonValue value)
    {
        if (string.Equals(name, JwtClaimNames.Audience, StringComparison.Ordinal))
        {
            return IsString(value) || IsStringArray(value);
        }

        if (string.Equals(name, JwtClaimNames.ExpirationTime, StringComparison.Ordinal)
            || string.Equals(name, JwtClaimNames.NotBefore, StringComparison.Ordinal)
            || string.Equals(name, JwtClaimNames.IssuedAt, StringComparison.Ordinal)
            || string.Equals(name, JwtClaimNames.TimeOfEvent, StringComparison.Ordinal)
            || string.Equals(name, JwtClaimNames.AuthenticationTime, StringComparison.Ordinal))
        {
            return value.Kind == TokenJsonValueKind.Number
                && value.NumberText != null;
        }

        if (string.Equals(
            name,
            JwtClaimNames.AuthenticationMethodsReferences,
            StringComparison.Ordinal))
        {
            return IsStringArray(value);
        }

        if (string.Equals(name, JwtClaimNames.Confirmation, StringComparison.Ordinal))
        {
            return value.Kind == TokenJsonValueKind.Object
                && value.ObjectValue != null;
        }

        if (string.Equals(name, JwtClaimNames.SecurityEvents, StringComparison.Ordinal))
        {
            return value.Kind == TokenJsonValueKind.Object
                && value.ObjectValue != null;
        }

        if (IsRegisteredStringClaim(name))
        {
            return IsString(value);
        }

        return true;
    }

    internal static bool IsRegisteredStringClaim(string name)
    {
        return string.Equals(name, JwtClaimNames.Issuer, StringComparison.Ordinal)
            || string.Equals(name, JwtClaimNames.Subject, StringComparison.Ordinal)
            || string.Equals(name, JwtClaimNames.JwtId, StringComparison.Ordinal)
            || string.Equals(name, JwtClaimNames.AuthorizedParty, StringComparison.Ordinal)
            || string.Equals(name, JwtClaimNames.Nonce, StringComparison.Ordinal)
            || string.Equals(name, JwtClaimNames.AccessTokenHash, StringComparison.Ordinal)
            || string.Equals(name, JwtClaimNames.CodeHash, StringComparison.Ordinal)
            || string.Equals(
                name,
                JwtClaimNames.AuthenticationContextClassReference,
                StringComparison.Ordinal)
            || string.Equals(name, JwtClaimNames.Scope, StringComparison.Ordinal)
            || string.Equals(name, JwtClaimNames.ClientId, StringComparison.Ordinal)
            || string.Equals(name, JwtClaimNames.TransactionId, StringComparison.Ordinal);
    }

    internal static bool IsString(JsonValue value)
    {
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
            if (item == null || !IsString(item))
            {
                return false;
            }

            index += 1;
        }

        return true;
    }

    internal static TokenError? ValidatePolicy(TokenPolicy policy)
    {
        if (policy.MaximumTokenSizeInBytes <= 0
            || policy.MaximumJsonDepth <= 0)
        {
            return new TokenError(
                "invalid_policy",
                "The JWT claim-set policy is invalid.",
                -1);
        }

        return null;
    }

    internal static JwtClaimsResult Failure(TokenError? error)
    {
        JwtClaimsResult result = new JwtClaimsResult();
        result.Error = error;
        return result;
    }

    internal static JwtClaimsResult Failure(string code, string message)
    {
        return Failure(new TokenError(code, message, -1));
    }

    internal static TokenTextResult TextFailure(TokenError? error)
    {
        TokenTextResult result = new TokenTextResult();
        result.Error = error;
        return result;
    }

    internal static TokenTextResult TextFailure(string code, string message)
    {
        return TextFailure(new TokenError(code, message, -1));
    }

    internal static TokenBytesResult BytesFailure(TokenError? error)
    {
        TokenBytesResult result = new TokenBytesResult();
        result.Error = error;
        return result;
    }
}
