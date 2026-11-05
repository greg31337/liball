using System;
using System.Collections.Generic;

namespace config;

/// <summary>Masks values whose configuration paths contain secret-like tokens.</summary>
/// <remarks>Matching is case-insensitive and checks for a token anywhere in the full path. The defaults include password, secret, token, key, and connectionstring.</remarks>
public class ConfigRedactor : IConfigRedactor
{
    internal List<string> Tokens;
    /// <summary>Contains the text substituted for a value at a matching path.</summary>
    internal string Replacement;

    /// <summary>Creates a redactor with common secret tokens and <c>[redacted]</c> replacement text.</summary>
    public ConfigRedactor()
    {
        Tokens = new List<string>();
        Replacement = "[redacted]";
        AddToken("password");
        AddToken("secret");
        AddToken("token");
        AddToken("key");
        AddToken("connectionstring");
    }

    /// <summary>Adds a case-insensitive substring used to recognize secret paths.</summary>
    /// <param name="token">The token to add. Empty tokens are ignored.</param>
    public void AddToken(string token)
    {
        if (token.Length == 0)
        {
            return;
        }

        Tokens.Add(token.ToLowerInvariant());
    }

    /// <summary>Gets the number of registered secret tokens.</summary>
    /// <returns>The token count, including defaults.</returns>
    public int TokenCount()
    {
        return Tokens.Count;
    }

    /// <summary>Sets the text substituted for values at matching paths.</summary>
    /// <param name="replacement">The replacement text.</param>
    /// <returns>This redactor for fluent calls.</returns>
    public ConfigRedactor SetReplacement(string replacement)
    {
        Replacement = replacement;
        return this;
    }

    /// <summary>Gets the text substituted for values at matching paths.</summary>
    /// <returns>The replacement text.</returns>
    public string ReplacementText()
    {
        return Replacement;
    }

    /// <summary>Checks whether a path contains any registered token.</summary>
    /// <param name="path">The full configuration path.</param>
    /// <returns>True when the path should be masked.</returns>
    public bool IsSecretPath(string path)
    {
        string lowerPath = path.ToLowerInvariant();

        int index = 0;
        while (index < Tokens.Count)
        {
            if (lowerPath.Contains(Tokens[index]))
            {
                return true;
            }

            index = index + 1;
        }

        return false;
    }

    /// <summary>Returns replacement text for a secret path and the original value otherwise.</summary>
    /// <param name="path">The full configuration path.</param>
    /// <param name="value">The text that may need masking.</param>
    /// <returns>The replacement or original text.</returns>
    public string RedactValue(string path, string value)
    {
        if (IsSecretPath(path))
        {
            return Replacement;
        }

        return value;
    }

    internal string RedactDiagnosticMessage(string path, string message)
    {
        return RedactValue(path, message);
    }
}
