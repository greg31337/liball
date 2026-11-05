using System;

namespace email;
/// <summary>
/// Defines the public operations implemented by <see cref = "EmailPartialMessageMetadata"/>.
/// </summary>
public interface IEmailPartialMessageMetadataContract
{
    /// <summary>
    /// Gets the parameters.
    /// </summary>
    /// <returns>The current parameters.</returns>
    EmailParameterList GetParameters();
    /// <summary>
    /// Determines whether the fragment ID is present or available.
    /// </summary>
    /// <returns><c>true</c> when the fragment ID is available; otherwise, <c>false</c>.</returns>
    bool HasFragmentId();
    /// <summary>
    /// Gets the fragment ID.
    /// </summary>
    /// <returns>The current fragment ID.</returns>
    string? GetFragmentId();
    /// <summary>
    /// Determines whether the fragment number is present or available.
    /// </summary>
    /// <returns><c>true</c> when the fragment number is available; otherwise, <c>false</c>.</returns>
    bool HasFragmentNumber();
    /// <summary>
    /// Gets the fragment number.
    /// </summary>
    /// <returns>The current fragment number.</returns>
    long GetFragmentNumber();
    /// <summary>
    /// Determines whether the total fragments is present or available.
    /// </summary>
    /// <returns><c>true</c> when the total fragments is available; otherwise, <c>false</c>.</returns>
    bool HasTotalFragments();
    /// <summary>
    /// Gets the total fragments.
    /// </summary>
    /// <returns>The current total fragments.</returns>
    long GetTotalFragments();
    /// <summary>
    /// Determines whether the required metadata is present or available.
    /// </summary>
    /// <returns><c>true</c> when the required metadata is available; otherwise, <c>false</c>.</returns>
    bool HasRequiredMetadata();
    /// <summary>
    /// Determines whether the current value or state is final fragment.
    /// </summary>
    /// <returns><c>true</c> when the value or state is final fragment; otherwise, <c>false</c>.</returns>
    bool IsFinalFragment();
    /// <summary>
    /// Determines whether the current value or state is sequence consistent.
    /// </summary>
    /// <returns><c>true</c> when the value or state is sequence consistent; otherwise, <c>false</c>.</returns>
    bool IsSequenceConsistent();
}
