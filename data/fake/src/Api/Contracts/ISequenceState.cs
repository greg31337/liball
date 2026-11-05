using System;
using System.Numerics;

namespace fake;
/// <summary>Defines the public operations exposed by <see cref="SequenceState"/>.</summary>
public interface ISequenceState
{
    /// <summary>
    /// Returns the next value and advances the current position.
    /// </summary>
    /// <returns>The value at the current sequence position.</returns>
    /// <exception cref="FakeException">Thrown when a finite sequence is exhausted or an unbounded value overflows.</exception>
    long Next();
    /// <summary>
    /// Returns consecutive values and advances the current position.
    /// </summary>
    /// <remarks>The operation is atomic: failure does not advance the current position.</remarks>
    /// <param name="count">The number of values to return.</param>
    /// <returns>A new array of consecutive sequence values.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when <c>count</c> is negative or exceeds the configured count limit.</exception>
    /// <exception cref="FakeException">Thrown when a finite sequence is exhausted or an unbounded value overflows.</exception>
    long[] Repeat(int count);
    /// <summary>
    /// Fills an existing array with consecutive values and advances the current position.
    /// </summary>
    /// <remarks>The operation is atomic: failure leaves the destination and current position unchanged.</remarks>
    /// <param name="destination">The array to fill.</param>
    /// <exception cref="System.ArgumentNullException">Thrown when <c>destination</c> is null.</exception>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when the destination exceeds the configured count limit.</exception>
    /// <exception cref="FakeException">Thrown when a finite sequence is exhausted or an unbounded value overflows.</exception>
    void Fill(long[] destination);
    /// <summary>
    /// Returns a deterministic zero-based page without changing the current position.
    /// </summary>
    /// <param name="pageIndex">The zero-based page index.</param>
    /// <param name="pageSize">The number of values in the page.</param>
    /// <returns>A new array containing the requested page.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when an argument is negative or <c>pageSize</c> exceeds the configured count limit.</exception>
    /// <exception cref="FakeException">Thrown when a finite page is beyond exhaustion or an unbounded value overflows.</exception>
    long[] Page(long pageIndex, int pageSize);
    /// <summary>
    /// Returns the sequence to its initial position.
    /// </summary>
    void Reset();
}
