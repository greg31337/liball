using System;
using System.Text;

namespace token;

/// <summary>
/// Contains and serializes an ordered JSON Web Key Set.
/// </summary>
public class JsonWebKeySet
{
    /// <summary>The keys in source or output order.</summary>
    public TokenKey[] Keys;

    /// <summary>Initializes a key set with an independent copy of a key-reference array.</summary>
    /// <param name="keys">The keys to include in order.</param>
    /// <exception cref="ArgumentNullException">Thrown when <c>keys</c> is null.</exception>
    public JsonWebKeySet(TokenKey[] keys)
    {
        if (keys == null)
        {
            throw new ArgumentNullException("keys");
        }

        Keys = keys.AsSpan().ToArray();
    }

    /// <summary>Creates an empty key set.</summary>
    /// <returns>An empty JSON Web Key Set.</returns>
    internal static JsonWebKeySet CreateEmpty()
    {
        return new JsonWebKeySet(System.Array.Empty<TokenKey>());
    }

    /// <summary>Reads a strict JSON Web Key Set object.</summary>
    /// <param name="json">The JSON object text.</param>
    /// <returns>A result containing the parsed key set or stable failure details.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <c>json</c> is null.</exception>
    internal static JwksResult Read(string json)
    {
        if (json == null)
        {
            throw new ArgumentNullException("json");
        }

        JsonObjectResult parsed = JsonObject.Read(json);
        if (!parsed.Success)
        {
            return Failure(parsed.Error);
        }

        return ReadObject(parsed.Object);
    }

    /// <summary>Reads strict UTF-8 JSON Web Key Set bytes.</summary>
    /// <param name="utf8">The UTF-8 JSON object bytes.</param>
    /// <returns>A result containing the parsed key set or stable failure details.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <c>utf8</c> is null.</exception>
    internal static JwksResult Read(byte[] utf8)
    {
        if (utf8 == null)
        {
            throw new ArgumentNullException("utf8");
        }

        JsonObjectResult parsed = JsonObject.Read(utf8);
        if (!parsed.Success)
        {
            return Failure(parsed.Error);
        }

        return ReadObject(parsed.Object);
    }

    /// <summary>Writes this key set as compact deterministic JSON.</summary>
    /// <returns>A result containing the JSON Web Key Set or stable failure details.</returns>
    internal TokenTextResult Write()
    {
        return Write(Keys);
    }

    /// <summary>Selects the only key that satisfies every active query criterion.</summary>
    /// <remarks>
    /// Selection fails when no key matches or when multiple keys match. The returned key is
    /// the same instance stored by this set and ownership is not transferred.
    /// </remarks>
    /// <param name="query">The conjunctive key-selection criteria.</param>
    /// <returns>A result containing the unique matching key or stable failure details.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <c>query</c> is null.</exception>
    internal TokenKeyResult Select(TokenKeyQuery query)
    {
        return FindKey(query);
    }

    /// <summary>Selects the only key that satisfies every active query criterion.</summary>
    /// <param name="query">The conjunctive key-selection criteria.</param>
    /// <returns>A result containing the unique matching key or stable failure details.</returns>
    internal TokenKeyResult FindKey(TokenKeyQuery query)
    {
        if (query == null)
        {
            throw new ArgumentNullException("query");
        }

        TokenKey? matched = null;
        if (Keys != null)
        {
            int index = 0;
            while (index < Keys.Length)
            {
                if (query.Matches(Keys[index]))
                {
                    if (matched != null)
                    {
                        return KeyFailure(
                            "ambiguous_jwks_key",
                            "More than one JWK Set key matches the query.");
                    }

                    matched = Keys[index];
                }

                index++;
            }
        }

        if (matched == null)
        {
            return KeyFailure(
                "jwks_key_not_found",
                "No JWK Set key matches the query.");
        }

        TokenKeyResult result = new TokenKeyResult();
        result.Success = true;
        result.Key = matched;
        return result;
    }

    /// <summary>Selects a unique key using unverified token metadata.</summary>
    /// <remarks>
    /// Resolution matches available key identifiers and rejects keys whose declared
    /// algorithm conflicts with the requested algorithm. The returned key is the same
    /// instance stored by this set and ownership is not transferred.
    /// </remarks>
    /// <param name="context">The unverified token metadata and requested operation.</param>
    /// <returns>A result containing the unique matching key or stable failure details.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <c>context</c> is null.</exception>
    internal TokenKeyResult Resolve(TokenKeyResolutionContext context)
    {
        if (context == null)
        {
            throw new ArgumentNullException("context");
        }

        TokenKey? matched = null;
        if (Keys != null)
        {
            int index = 0;
            while (index < Keys.Length)
            {
                if (MatchesContext(Keys[index], context))
                {
                    if (matched != null)
                    {
                        return KeyFailure(
                            "ambiguous_jwks_key",
                            "More than one JWK Set key matches the token metadata.");
                    }

                    matched = Keys[index];
                }

                index++;
            }
        }

        if (matched == null)
        {
            return KeyFailure(
                "jwks_key_not_found",
                "No JWK Set key matches the token metadata.");
        }

        TokenKeyResult result = new TokenKeyResult();
        result.Success = true;
        result.Key = matched;
        return result;
    }

    /// <summary>Writes keys as a compact deterministic JSON Web Key Set.</summary>
    /// <remarks>The input order is preserved and caller-owned keys are not modified or released.</remarks>
    /// <param name="keys">The keys to serialize in order.</param>
    /// <returns>A result containing the JSON Web Key Set or stable failure details.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <c>keys</c> is null.</exception>
    internal static TokenTextResult Write(TokenKey[] keys)
    {
        if (keys == null)
        {
            throw new ArgumentNullException("keys");
        }

        StringBuilder builder = new StringBuilder();
        builder.Append("{\"keys\":[");
        int index = 0;
        while (index < keys.Length)
        {
            if (keys[index] == null)
            {
                return TextFailure(
                    "invalid_jwks_key",
                    "The JWK Set contains a null key.");
            }

            TokenTextResult written = JsonWebKey.Write(keys[index]);
            if (!written.Success)
            {
                return TextFailure(written.Error);
            }

            if (index > 0)
            {
                builder.Append(',');
            }

            builder.Append(written.Text);
            index++;
        }

        builder.Append("]}");
        TokenTextResult result = new TokenTextResult();
        result.Success = true;
        result.Text = builder.ToString();
        return result;
    }

    /// <summary>Releases owned native and secret material from every key in this set.</summary>
    internal void ReleaseOwnedMaterial()
    {
        if (Keys == null)
        {
            return;
        }

        int index = 0;
        while (index < Keys.Length)
        {
            if (Keys[index] != null)
            {
                Keys[index].ReleaseOwnedMaterial();
            }

            index++;
        }
    }

    internal static JwksResult ReadObject(JsonObject? value)
    {
        JsonValue? keysValue = (value ?? throw new InvalidOperationException("A required value is null.")).Get("keys");
        if (keysValue == null)
        {
            return Failure(
                "missing_jwks_keys",
                "The JWK Set keys member is required.");
        }

        if (keysValue.Kind != TokenJsonValueKind.Array)
        {
            return Failure(
                "invalid_jwks_keys",
                "The JWK Set keys member must be an array.");
        }

        TokenKey[] keys = new TokenKey[(keysValue.ArrayValue ?? throw new InvalidOperationException("A required value is null.")).Length];
        int index = 0;
        while (index < keys.Length)
        {
            JsonValue item = keysValue.ArrayValue[index];
            if (item.Kind != TokenJsonValueKind.Object)
            {
                Release(keys, index);
                return Failure(
                    "invalid_jwks_key",
                    "Every JWK Set keys item must be an object.");
            }

            JwkResult parsed = JsonWebKey.ReadObject(item.ObjectValue);
            if (!parsed.Success)
            {
                Release(keys, index);
                return Failure(parsed.Error);
            }

            keys[index] = (parsed.Key ?? throw new InvalidOperationException("A required value is null."));
            index++;
        }

        JwksResult result = new JwksResult();
        result.Success = true;
        result.Set = new JsonWebKeySet(keys);
        return result;
    }

    internal static bool MatchesContext(
        TokenKey key,
        TokenKeyResolutionContext context)
    {
        if (key == null)
        {
            return false;
        }

        if (context.KeyId != null
            && !string.Equals(context.KeyId, key.KeyId, StringComparison.Ordinal))
        {
            return false;
        }

        if (context.KeyIdBytes != null
            && (key.KeyIdBytes == null
                || !ConstantTime.AreEqual(context.KeyIdBytes, key.KeyIdBytes)))
        {
            return false;
        }

        if (context.Algorithm != null
            && !MatchesJoseAlgorithm(key, context.Algorithm))
        {
            return false;
        }

        if (context.HasCoseAlgorithm
            && !MatchesCoseAlgorithm(key, context.CoseAlgorithm))
        {
            return false;
        }

        return true;
    }

    internal static bool MatchesJoseAlgorithm(TokenKey key, string algorithm)
    {
        if (key.Algorithm != null
            && !string.Equals(key.Algorithm, algorithm, StringComparison.Ordinal))
        {
            return false;
        }

        if (key.HasCoseAlgorithm)
        {
            int[] labels = JoseCoseAlgorithmMap.GetCoseLabels(algorithm);
            return RegistryLookup.Contains(labels, key.CoseAlgorithm);
        }

        return true;
    }

    internal static bool MatchesCoseAlgorithm(TokenKey key, int algorithm)
    {
        if (key.HasCoseAlgorithm && key.CoseAlgorithm != algorithm)
        {
            return false;
        }

        if (key.Algorithm != null)
        {
            AlgorithmMappingResult mapped = JoseCoseAlgorithmMap.FromCose(
                algorithm);
            return mapped.Mapped
                && string.Equals(
                    key.Algorithm,
                    mapped.JoseName,
                    StringComparison.Ordinal);
        }

        return true;
    }

    internal static void Release(TokenKey[] keys, int count)
    {
        int index = 0;
        while (index < count)
        {
            if (keys[index] != null)
            {
                keys[index].ReleaseOwnedMaterial();
            }

            index++;
        }
    }

    internal static JwksResult Failure(string code, string message)
    {
        return Failure(new TokenError(code, message, -1));
    }

    internal static JwksResult Failure(TokenError? error)
    {
        JwksResult result = new JwksResult();
        result.Error = error;
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

    internal static TokenKeyResult KeyFailure(string code, string message)
    {
        TokenKeyResult result = new TokenKeyResult();
        result.Error = new TokenError(code, message, -1);
        return result;
    }
}
