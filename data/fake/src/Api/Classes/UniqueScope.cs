using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace fake;
/// <summary>
/// Produces values whose string keys have not previously been accepted by a named scope.
/// </summary>
/// <remarks>
/// Every call receives the configured retry budget. String values use their full value as
/// the key. Object values use caller-selected string parts encoded with length prefixes,
/// so part boundaries, empty strings, and null strings remain distinct.
/// </remarks>
public class UniqueScope : IUniqueScope
{
    string ScopeName;
    int RetryLimit;
    int MaximumCount;
    int MaximumOutputSize;
    int AttemptsRemaining;
    HashSet<string> Keys;
    object Sync;
    internal UniqueScope(string scopeName, int retryLimit, int maximumCount, int maximumOutputSize)
    {
        ScopeName = scopeName;
        RetryLimit = retryLimit;
        MaximumCount = maximumCount;
        MaximumOutputSize = maximumOutputSize;
        AttemptsRemaining = retryLimit;
        Keys = new HashSet<string>(StringComparer.Ordinal);
        Sync = new object ();
    }

    /// <summary>
    /// Generates a string whose complete value is unique within this scope.
    /// </summary>
    /// <param name="generate">The callback invoked until it returns an unused value or the retry budget is exhausted.</param>
    /// <returns>The first generated string not previously accepted by this scope.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when <c>generate</c> is null.</exception>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when an encoded key exceeds the configured output limit.</exception>
    /// <exception cref="FakeException">Thrown when no unique key is produced within the retry budget.</exception>
    public string Next(Func<string> generate)
    {
        Guard.NotNull(generate, "generate");
        lock (Sync)
        {
            AttemptsRemaining = RetryLimit;
            int attempt = 0;
            while (attempt < RetryLimit)
            {
                attempt = attempt + 1;
                AttemptsRemaining = RetryLimit - attempt;
                string value = generate();
                string key = EncodeSimple(value);
                if (Keys.Add(key))
                {
                    return value;
                }
            }

            throw Exhausted();
        }
    }

    /// <summary>
    /// Generates an object whose selected string parts form a unique composite key.
    /// </summary>
    /// <param name="generate">The callback invoked until it returns an object with an unused key or the retry budget is exhausted.</param>
    /// <param name="compositeKey">The callback that returns the ordered string parts identifying a generated object.</param>
    /// <returns>The first generated object whose composite key was not previously accepted by this scope.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when a callback is null.</exception>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when a key has too many parts or exceeds the configured output limit.</exception>
    /// <exception cref="FakeException">Thrown when the key callback returns null or no unique key is produced within the retry budget.</exception>
    public object NextFromGenerateAndCompositeKey(Func<object> generate, Func<object, string[]> compositeKey)
    {
        Guard.NotNull(generate, "generate");
        Guard.NotNull(compositeKey, "compositeKey");
        lock (Sync)
        {
            AttemptsRemaining = RetryLimit;
            int attempt = 0;
            while (attempt < RetryLimit)
            {
                attempt = attempt + 1;
                AttemptsRemaining = RetryLimit - attempt;
                object value = generate();
                string[] parts = compositeKey(value);
                if (parts == null)
                {
                    throw FakeException.Create($"Unique scope '{ScopeName}' composite key callback returned null.");
                }

                Guard.Count(parts.Length, MaximumCount, "compositeKey");
                string key = EncodeComposite(parts);
                if (Keys.Add(key))
                {
                    return value;
                }
            }

            throw Exhausted();
        }
    }

    /// <summary>
    /// Clears all accepted keys and restores the full retry budget.
    /// </summary>
    public void Reset()
    {
        lock (Sync)
        {
            Keys.Clear();
            AttemptsRemaining = RetryLimit;
        }
    }

    /// <summary>
    /// Returns the number of keys currently accepted by this scope.
    /// </summary>
    /// <returns>The accepted-key count.</returns>
    public int Count()
    {
        lock (Sync)
        {
            return Keys.Count;
        }
    }

    /// <summary>
    /// Returns the unused attempts from the most recent operation's retry budget.
    /// </summary>
    /// <remarks>A new operation and <see cref="Reset"/> both restore the full configured budget before attempting generation.</remarks>
    /// <returns>The number of attempts not used by the most recent operation.</returns>
    public int RemainingAttempts()
    {
        lock (Sync)
        {
            return AttemptsRemaining;
        }
    }

    internal bool AcceptObject(object? value, int attemptsRemaining)
    {
        lock (Sync)
        {
            AttemptsRemaining = attemptsRemaining;
            return Keys.Add(EncodeObject(value));
        }
    }

    internal bool AcceptComposite(string[] parts, int attemptsRemaining)
    {
        if (parts == null)
            throw FakeException.Create("Unique scope '" + ScopeName + "' composite key callback returned null.");
        Guard.Count(parts.Length, MaximumCount, "compositeKey");
        lock (Sync)
        {
            AttemptsRemaining = attemptsRemaining;
            return Keys.Add(EncodeComposite(parts));
        }
    }

    string EncodeSimple(string? value)
    {
        StringBuilder builder = new StringBuilder();
        builder.Append("S");
        AppendPart(builder, value);
        Guard.OutputSize(builder.Length, MaximumOutputSize, "generate");
        return builder.ToString();
    }

    string EncodeComposite(string[] parts)
    {
        StringBuilder builder = new StringBuilder();
        builder.Append("C");
        builder.Append(parts.Length.ToString(CultureInfo.InvariantCulture));
        builder.Append(":");
        int index = 0;
        while (index < parts.Length)
        {
            AppendPart(builder, parts[index]);
            index = index + 1;
        }

        Guard.OutputSize(builder.Length, MaximumOutputSize, "compositeKey");
        return builder.ToString();
    }

    string EncodeObject(object? value)
    {
        if (value == null)
            return EncodeSimple(null);
        if (value.GetType() == String.Empty.GetType())
            return EncodeSimple((string)value);
        StringBuilder builder = new StringBuilder();
        builder.Append("O");
        AppendPart(builder, value.GetType().FullName);
        AppendPart(builder, Convert.ToString(value, CultureInfo.InvariantCulture));
        Guard.OutputSize(builder.Length, MaximumOutputSize, "generate");
        return builder.ToString();
    }

    static void AppendPart(StringBuilder builder, string? value)
    {
        if (value == null)
        {
            builder.Append("-1:");
            return;
        }

        builder.Append(value.Length.ToString(CultureInfo.InvariantCulture));
        builder.Append(":");
        builder.Append(value);
    }

    FakeException Exhausted()
    {
        return FakeException.Create($"Unique scope '{ScopeName}' exhausted its retry limit after {RetryLimit} attempts with {Keys.Count} accepted keys.");
    }
}
