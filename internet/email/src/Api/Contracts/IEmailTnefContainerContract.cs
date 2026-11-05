using System;
using System.Collections.Generic;

namespace email;
/// <summary>
/// Defines the public operations implemented by <see cref = "EmailTnefContainer"/>.
/// </summary>
public interface IEmailTnefContainerContract
{
    /// <summary>
    /// Gets the header.
    /// </summary>
    /// <returns>The current header.</returns>
    EmailTnefHeader GetHeader();
    /// <summary>
    /// Gets the attribute count.
    /// </summary>
    /// <returns>The current attribute count.</returns>
    int GetAttributeCount();
    /// <summary>
    /// Gets the attribute at a zero-based index.
    /// </summary>
    /// <param name = "index">The zero-based index of the requested item.</param>
    /// <returns>The requested attribute.</returns>
    /// <exception cref = "ArgumentOutOfRangeException">Thrown when a numeric, index, or enum argument is outside the supported range.</exception>
    EmailTnefAttribute GetAttributeAt(int index);
    /// <summary>
    /// Gets the attributes.
    /// </summary>
    /// <remarks>
    /// Collection and array results should be treated as snapshots unless the owning type explicitly documents shared mutable state.
    /// </remarks>
    /// <returns>The current attributes.</returns>
    List<EmailTnefAttribute> GetAttributes();
    /// <summary>
    /// Gets the attachment count.
    /// </summary>
    /// <returns>The current attachment count.</returns>
    int GetAttachmentCount();
    /// <summary>
    /// Gets the source bytes.
    /// </summary>
    /// <remarks>
    /// Collection and array results should be treated as snapshots unless the owning type explicitly documents shared mutable state.
    /// </remarks>
    /// <returns>The current source bytes.</returns>
    byte[] GetSourceBytes();
    /// <summary>
    /// Gets the source byte count.
    /// </summary>
    /// <returns>The current source byte count.</returns>
    long GetSourceByteCount();
    /// <summary>
    /// Determines whether the version is present or available.
    /// </summary>
    /// <returns><c>true</c> when the version is available; otherwise, <c>false</c>.</returns>
    bool HasVersion();
    /// <summary>
    /// Gets the version.
    /// </summary>
    /// <returns>The current version.</returns>
    uint GetVersion();
    /// <summary>
    /// Determines whether the code page is present or available.
    /// </summary>
    /// <returns><c>true</c> when the code page is available; otherwise, <c>false</c>.</returns>
    bool HasCodePage();
    /// <summary>
    /// Gets the code page.
    /// </summary>
    /// <returns>The current code page.</returns>
    int GetCodePage();
    /// <summary>
    /// Determines whether the sub code page is present or available.
    /// </summary>
    /// <returns><c>true</c> when the sub code page is available; otherwise, <c>false</c>.</returns>
    bool HasSubCodePage();
    /// <summary>
    /// Gets the sub code page.
    /// </summary>
    /// <returns>The current sub code page.</returns>
    int GetSubCodePage();
}
