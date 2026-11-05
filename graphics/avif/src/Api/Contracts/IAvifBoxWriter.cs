using System;

namespace avif;

/// <summary>Defines the public instance operations provided by <see cref="AvifBoxWriter"/>.</summary>
public interface IAvifBoxWriter
{
    /// <summary>Returns the outcome of the most recent writer operation.</summary>
    /// <returns>The stable result code.</returns>
    AvifResultCode GetCode();

    /// <summary>Begins a box with a four-character type.</summary>
    /// <param name="type">The four-character box type.</param>
    /// <returns>The operation result.</returns>
    AvifResultCode BeginBox(string type);

    /// <summary>Begins a FullBox with version and 24-bit flags.</summary>
    /// <param name="type">The four-character box type.</param>
    /// <param name="version">The version from zero through 255.</param>
    /// <param name="flags">The flags from zero through 16777215.</param>
    /// <returns>The operation result.</returns>
    AvifResultCode BeginFullBox(string type, int version, int flags);

    /// <summary>Writes bytes into the current box.</summary>
    /// <param name="source">The bytes to copy.</param>
    /// <returns>The operation result.</returns>
    AvifResultCode Write(ReadOnlySpan<byte> source);

    /// <summary>Ends the innermost open box and patches its declared size.</summary>
    /// <returns>The operation result.</returns>
    AvifResultCode EndBox();

    /// <summary>Finalizes the box stream and returns an independent byte snapshot.</summary>
    /// <returns>The encoded result.</returns>
    AvifEncodeResult Finish();

    /// <summary>Releases retained output and construction state.</summary>
    /// <returns><c>Success</c>; repeated calls are harmless.</returns>
    AvifResultCode Close();
}
