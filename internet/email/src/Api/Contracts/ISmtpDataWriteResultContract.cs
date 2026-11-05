using System;

namespace email;
/// <summary>
/// Defines the public operations implemented by <see cref = "SmtpDataWriteResult"/>.
/// </summary>
public interface ISmtpDataWriteResultContract
{
    /// <summary>
    /// Gets the source byte count.
    /// </summary>
    /// <returns>The current source byte count.</returns>
    long GetSourceByteCount();
    /// <summary>
    /// Gets the data byte count.
    /// </summary>
    /// <returns>The current data byte count.</returns>
    long GetDataByteCount();
    /// <summary>
    /// Gets the output byte count.
    /// </summary>
    /// <returns>The current output byte count.</returns>
    long GetOutputByteCount();
    /// <summary>
    /// Gets the dot stuffed line count.
    /// </summary>
    /// <returns>The current dot stuffed line count.</returns>
    long GetDotStuffedLineCount();
    /// <summary>
    /// Gets the normalized line ending count.
    /// </summary>
    /// <returns>The current normalized line ending count.</returns>
    long GetNormalizedLineEndingCount();
}
