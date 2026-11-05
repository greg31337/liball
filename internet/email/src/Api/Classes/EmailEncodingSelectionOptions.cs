using System;

namespace email;
/// <summary>
/// Controls how encoding selection operations behave.
/// </summary>
/// <remarks>
/// The constructor establishes a complete default policy. Setters reject unsupported values before the options are used by an operation.
/// </remarks>
public class EmailEncodingSelectionOptions : IEmailEncodingSelectionOptionsContract
{
    string mediaType;
    EmailTransferEncodingConstraint constraint;
    int maximumLineBytes;
    int minimumTextPercentage;
    /// <summary>
    /// Creates a new encoding selection options object.
    /// </summary>
    public EmailEncodingSelectionOptions()
    {
        mediaType = "application/octet-stream";
        constraint = EmailTransferEncodingConstraint.SevenBit;
        maximumLineBytes = EmailTransferEncodingConstants.DefaultMaximumLineBytes;
        minimumTextPercentage = 80;
    }

    /// <summary>
    /// Gets the media type.
    /// </summary>
    /// <returns>The current media type.</returns>
    public string GetMediaType()
    {
        return mediaType;
    }

    /// <summary>
    /// Sets the media type used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    /// <exception cref = "ArgumentException">Thrown when an argument is empty, malformed, or inconsistent with the other inputs.</exception>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    public void SetMediaType(string value)
    {
        if (value == null)
        {
            throw new ArgumentNullException("value");
        }

        if (value.Trim().Length == 0)
        {
            throw new ArgumentException("The media type cannot be empty.", "value");
        }

        mediaType = value;
    }

    /// <summary>
    /// Gets the constraint.
    /// </summary>
    /// <returns>The current constraint.</returns>
    public EmailTransferEncodingConstraint GetConstraint()
    {
        return constraint;
    }

    /// <summary>
    /// Sets the constraint used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    /// <exception cref = "ArgumentOutOfRangeException">Thrown when a numeric, index, or enum argument is outside the supported range.</exception>
    public void SetConstraint(EmailTransferEncodingConstraint value)
    {
        if (value < EmailTransferEncodingConstraint.SevenBit || value > EmailTransferEncodingConstraint.Binary)
        {
            throw new ArgumentOutOfRangeException("value");
        }

        constraint = value;
    }

    /// <summary>
    /// Gets the maximum line bytes.
    /// </summary>
    /// <returns>The current maximum line bytes.</returns>
    public int GetMaximumLineBytes()
    {
        return maximumLineBytes;
    }

    /// <summary>
    /// Sets the maximum line bytes used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    /// <exception cref = "ArgumentOutOfRangeException">Thrown when a numeric, index, or enum argument is outside the supported range.</exception>
    public void SetMaximumLineBytes(int value)
    {
        if (value < 1)
        {
            throw new ArgumentOutOfRangeException("value");
        }

        maximumLineBytes = value;
    }

    /// <summary>
    /// Gets the minimum text percentage.
    /// </summary>
    /// <returns>The current minimum text percentage.</returns>
    public int GetMinimumTextPercentage()
    {
        return minimumTextPercentage;
    }

    /// <summary>
    /// Sets the minimum text percentage used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    /// <exception cref = "ArgumentOutOfRangeException">Thrown when a numeric, index, or enum argument is outside the supported range.</exception>
    public void SetMinimumTextPercentage(int value)
    {
        if (value < 0 || value > 100)
        {
            throw new ArgumentOutOfRangeException("value");
        }

        minimumTextPercentage = value;
    }

    /// <summary>
    /// Creates an independent copy of this instance and its option state.
    /// </summary>
    /// <returns>A separate copy with the same current settings and values.</returns>
    public EmailEncodingSelectionOptions Clone()
    {
        EmailEncodingSelectionOptions copy = new EmailEncodingSelectionOptions();
        copy.SetMediaType(mediaType);
        copy.SetConstraint(constraint);
        copy.SetMaximumLineBytes(maximumLineBytes);
        copy.SetMinimumTextPercentage(minimumTextPercentage);
        return copy;
    }
}
