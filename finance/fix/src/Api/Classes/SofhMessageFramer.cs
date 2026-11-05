using System;

namespace fix;

/// <summary>Locates complete messages framed by a Simple Open Framing Header.</summary>
public class SofhMessageFramer : IMessageFramer
{
    /// <summary>Creates a Simple Open Framing Header message framer.</summary>
    public SofhMessageFramer()
    {
    }

    /// <summary>Reads one SOFH frame without retaining the caller's span.</summary>
    /// <param name="source">The source bytes.</param>
    /// <param name="options">The framing options and resource limits.</param>
    /// <returns>The explicit framing outcome.</returns>
    public FrameResult ReadFrame(ReadOnlySpan<byte> source, FrameOptions options)
    {
        if (!options.RecoverFromJunk)
        {
            return SofhFrameScanner.ScanFrame(source, options);
        }
        return RecoverSofhFrame(source, options);
    }

    internal static FrameResult RecoverSofhFrame(ReadOnlySpan<byte> source, FrameOptions options)
    {
        if (options.MaxFrameSize < FixDefaults.SofhHeaderSize)
        {
            return SofhFrameScanner.ScanFrame(source, options);
        }
        FrameResult partial = new FrameResult();
        bool hasPartial = false;
        FrameResult limited = new FrameResult();
        bool hasLimited = false;
        int lastHeaderOffset = source.Length - FixDefaults.SofhHeaderSize;
        for (int offset = 0; offset <= lastHeaderOffset; offset++)
        {
            FrameResult candidate = SofhFrameScanner.ScanFrame(source.Slice(offset), options);
            if (candidate.Status == FrameStatus.Complete)
            {
                return AdjustRecoveredSofh(candidate, offset, true);
            }
            if (candidate.Status == FrameStatus.Incomplete && !hasPartial)
            {
                partial = AdjustRecoveredSofh(candidate, offset, false);
                hasPartial = true;
            }
            else if (candidate.Status == FrameStatus.LimitExceeded && candidate.EncodingType != 0 && !hasLimited)
            {
                limited = AdjustRecoveredSofh(candidate, offset, false);
                hasLimited = true;
            }
        }
        if (hasPartial)
        {
            return partial;
        }
        if (hasLimited)
        {
            return limited;
        }

        FrameResult trailing = new FrameResult();
        trailing.IsSofh = true;
        trailing.Status = FrameStatus.Incomplete;
        int retained = source.Length;
        if (retained > FixDefaults.SofhHeaderSize - 1)
        {
            retained = FixDefaults.SofhHeaderSize - 1;
        }
        trailing.FrameOffset = source.Length - retained;
        trailing.Consumed = trailing.FrameOffset;
        if (source.Length == int.MaxValue)
        {
            trailing.Status = FrameStatus.LimitExceeded;
            trailing.Required = int.MaxValue;
            trailing.ErrorOffset = source.Length;
            trailing.ErrorCode = "limit-exceeded";
            trailing.ErrorMessage = "The SOFH frame could not be recovered within the supported size range.";
            return trailing;
        }
        trailing.Required = source.Length + 1;
        if (source.Length < FixDefaults.SofhHeaderSize)
        {
            trailing.Required = FixDefaults.SofhHeaderSize;
        }
        trailing.ErrorOffset = source.Length;
        trailing.ErrorCode = "incomplete";
        trailing.ErrorMessage = "More bytes are required to locate a SOFH frame boundary.";
        return trailing;
    }

    internal static FrameResult AdjustRecoveredSofh(FrameResult result, int offset, bool complete)
    {
        result.FrameOffset = offset;
        long consumed = (long)offset + result.Consumed;
        long required = (long)offset + result.Required;
        if (consumed > int.MaxValue || required > int.MaxValue)
        {
            result.Status = FrameStatus.LimitExceeded;
            result.Consumed = offset;
            result.Required = int.MaxValue;
            result.ErrorOffset = offset;
            result.ErrorCode = "limit-exceeded";
            result.ErrorMessage = "The recovered SOFH frame exceeds the supported size range.";
            return result;
        }
        result.Consumed = complete ? (int)consumed : offset;
        result.Required = (int)required;
        if (result.ErrorOffset >= 0)
        {
            result.ErrorOffset += offset;
        }
        if (complete && offset > 0)
        {
            result.Status = FrameStatus.Recovered;
        }
        return result;
    }
}
