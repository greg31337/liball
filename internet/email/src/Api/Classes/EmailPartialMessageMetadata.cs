using System;

namespace email;
/// <summary>
/// Represents a partial message metadata.
/// </summary>
public class EmailPartialMessageMetadata : IEmailPartialMessageMetadataContract
{
    EmailParameterList parameters;
    string? fragmentId;
    long fragmentNumber;
    bool fragmentNumberPresent;
    long totalFragments;
    bool totalFragmentsPresent;
    /// <summary>
    /// Creates a new partial message metadata.
    /// </summary>
    /// <param name = "parameters">The parameters used by the operation.</param>
    /// <param name = "fragmentId">The fragment ID used by the operation.</param>
    /// <param name = "fragmentNumber">The fragment number used by the operation.</param>
    /// <param name = "fragmentNumberPresent">Whether to enable the fragment number present behavior.</param>
    /// <param name = "totalFragments">The total fragments used by the operation.</param>
    /// <param name = "totalFragmentsPresent">Whether to enable the total fragments present behavior.</param>
    /// <exception cref = "ArgumentException">Thrown when an argument is empty, malformed, or inconsistent with the other inputs.</exception>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    /// <exception cref = "ArgumentOutOfRangeException">Thrown when a numeric, index, or enum argument is outside the supported range.</exception>
    public EmailPartialMessageMetadata(EmailParameterList parameters, string? fragmentId, long fragmentNumber, bool fragmentNumberPresent, long totalFragments, bool totalFragmentsPresent)
    {
        if (parameters == null)
        {
            throw new ArgumentNullException("parameters");
        }

        if (fragmentId != null && fragmentId.Length == 0)
        {
            throw new ArgumentException("A partial-message fragment identifier cannot be empty.", "fragmentId");
        }

        if (fragmentNumberPresent && fragmentNumber < 1)
        {
            throw new ArgumentOutOfRangeException("fragmentNumber");
        }

        if (totalFragmentsPresent && totalFragments < 1)
        {
            throw new ArgumentOutOfRangeException("totalFragments");
        }

        this.parameters = parameters;
        this.fragmentId = fragmentId;
        this.fragmentNumber = fragmentNumber;
        this.fragmentNumberPresent = fragmentNumberPresent;
        this.totalFragments = totalFragments;
        this.totalFragmentsPresent = totalFragmentsPresent;
    }

    /// <summary>
    /// Gets the parameters.
    /// </summary>
    /// <returns>The current parameters.</returns>
    public EmailParameterList GetParameters()
    {
        return parameters;
    }

    /// <summary>
    /// Determines whether the fragment ID is present or available.
    /// </summary>
    /// <returns><c>true</c> when the fragment ID is available; otherwise, <c>false</c>.</returns>
    public bool HasFragmentId()
    {
        return fragmentId != null;
    }

    /// <summary>
    /// Gets the fragment ID.
    /// </summary>
    /// <returns>The current fragment ID.</returns>
    public string? GetFragmentId()
    {
        return fragmentId;
    }

    /// <summary>
    /// Determines whether the fragment number is present or available.
    /// </summary>
    /// <returns><c>true</c> when the fragment number is available; otherwise, <c>false</c>.</returns>
    public bool HasFragmentNumber()
    {
        return fragmentNumberPresent;
    }

    /// <summary>
    /// Gets the fragment number.
    /// </summary>
    /// <returns>The current fragment number.</returns>
    public long GetFragmentNumber()
    {
        return fragmentNumber;
    }

    /// <summary>
    /// Determines whether the total fragments is present or available.
    /// </summary>
    /// <returns><c>true</c> when the total fragments is available; otherwise, <c>false</c>.</returns>
    public bool HasTotalFragments()
    {
        return totalFragmentsPresent;
    }

    /// <summary>
    /// Gets the total fragments.
    /// </summary>
    /// <returns>The current total fragments.</returns>
    public long GetTotalFragments()
    {
        return totalFragments;
    }

    /// <summary>
    /// Determines whether the required metadata is present or available.
    /// </summary>
    /// <returns><c>true</c> when the required metadata is available; otherwise, <c>false</c>.</returns>
    public bool HasRequiredMetadata()
    {
        return fragmentId != null && fragmentNumberPresent;
    }

    /// <summary>
    /// Determines whether the current value or state is final fragment.
    /// </summary>
    /// <returns><c>true</c> when the value or state is final fragment; otherwise, <c>false</c>.</returns>
    public bool IsFinalFragment()
    {
        return fragmentNumberPresent && totalFragmentsPresent && fragmentNumber == totalFragments;
    }

    /// <summary>
    /// Determines whether the current value or state is sequence consistent.
    /// </summary>
    /// <returns><c>true</c> when the value or state is sequence consistent; otherwise, <c>false</c>.</returns>
    public bool IsSequenceConsistent()
    {
        return !fragmentNumberPresent || !totalFragmentsPresent || fragmentNumber <= totalFragments;
    }
}
