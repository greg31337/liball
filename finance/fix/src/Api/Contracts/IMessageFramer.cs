using System;

namespace fix;

/// <summary>Locates one complete FIX frame in caller-owned bytes.</summary>
public interface IMessageFramer
{
    /// <summary>Reads one frame without retaining the caller's span.</summary>
    /// <param name="source">The source bytes.</param>
    /// <param name="options">The framing options and resource limits.</param>
    /// <returns>The explicit framing outcome.</returns>
    FrameResult ReadFrame(ReadOnlySpan<byte> source, FrameOptions options);
}
