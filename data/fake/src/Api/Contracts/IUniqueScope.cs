using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace fake;
/// <summary>Defines the public operations exposed by <see cref="UniqueScope"/>.</summary>
public interface IUniqueScope
{
    /// <summary>
    /// Generates a string whose complete value is unique within this scope.
    /// </summary>
    /// <param name="generate">The callback invoked until it returns an unused value or the retry budget is exhausted.</param>
    /// <returns>The first generated string not previously accepted by this scope.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when <c>generate</c> is null.</exception>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when an encoded key exceeds the configured output limit.</exception>
    /// <exception cref="FakeException">Thrown when no unique key is produced within the retry budget.</exception>
    string Next(Func<string> generate);
    /// <summary>
    /// Generates an object whose selected string parts form a unique composite key.
    /// </summary>
    /// <param name="generate">The callback invoked until it returns an object with an unused key or the retry budget is exhausted.</param>
    /// <param name="compositeKey">The callback that returns the ordered string parts identifying a generated object.</param>
    /// <returns>The first generated object whose composite key was not previously accepted by this scope.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when a callback is null.</exception>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when a key has too many parts or exceeds the configured output limit.</exception>
    /// <exception cref="FakeException">Thrown when the key callback returns null or no unique key is produced within the retry budget.</exception>
    object NextFromGenerateAndCompositeKey(Func<object> generate, Func<object, string[]> compositeKey);
    /// <summary>
    /// Clears all accepted keys and restores the full retry budget.
    /// </summary>
    void Reset();
    /// <summary>
    /// Returns the number of keys currently accepted by this scope.
    /// </summary>
    /// <returns>The accepted-key count.</returns>
    int Count();
    /// <summary>
    /// Returns the unused attempts from the most recent operation's retry budget.
    /// </summary>
    /// <remarks>A new operation and <see cref="Reset"/> both restore the full configured budget before attempting generation.</remarks>
    /// <returns>The number of attempts not used by the most recent operation.</returns>
    int RemainingAttempts();
}
