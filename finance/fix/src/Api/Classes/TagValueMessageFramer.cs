using System;

namespace fix;

/// <summary>Locates complete TagValue messages in caller-owned byte spans.</summary>
public class TagValueMessageFramer : IMessageFramer
{
    /// <summary>Creates a TagValue message framer.</summary>
    public TagValueMessageFramer()
    {
    }

    /// <summary>Reads one frame without retaining the caller's span.</summary>
    /// <param name="source">The source bytes.</param>
    /// <param name="options">The framing options and resource limits.</param>
    /// <returns>The explicit framing outcome.</returns>
    public FrameResult ReadFrame(ReadOnlySpan<byte> source, FrameOptions options)
    {
        if (!options.RecoverFromJunk)
        {
            return TagValueFrameScanner.ScanFrame(source, options);
        }
        return RecoverFrame(source, options);
    }

    internal static FrameResult RecoverFrame(ReadOnlySpan<byte> source, FrameOptions options)
    {
        if (options.MaxFrameSize <= 0)
        {
            return TagValueFrameScanner.ScanFrame(source, options);
        }
        FrameResult partial = new FrameResult();
        bool hasPartial = false;
        FrameResult limited = new FrameResult();
        bool hasLimited = false;
        int trailingBegin = -1;

        for (int offset = 0; offset < source.Length; offset++)
        {
            if (source[offset] != 56)
            {
                continue;
            }
            if (offset + 1 >= source.Length)
            {
                trailingBegin = offset;
                continue;
            }
            if (source[offset + 1] != 61)
            {
                continue;
            }

            FrameResult candidate = TagValueFrameScanner.ScanFrame(source.Slice(offset), options);
            if (candidate.Status == FrameStatus.Complete)
            {
                return AdjustRecoveredResult(candidate, offset, true);
            }
            if (candidate.Status == FrameStatus.Incomplete && !hasPartial)
            {
                partial = AdjustRecoveredResult(candidate, offset, false);
                hasPartial = true;
            }
            else if (candidate.Status == FrameStatus.LimitExceeded && !hasLimited)
            {
                limited = AdjustRecoveredResult(candidate, offset, false);
                hasLimited = true;
            }
        }

        if (hasPartial)
        {
            return partial;
        }
        if (trailingBegin >= 0)
        {
            FrameResult trailing = new FrameResult();
            trailing.Status = FrameStatus.Incomplete;
            trailing.FrameOffset = trailingBegin;
            trailing.Consumed = trailingBegin;
            trailing.Required = trailingBegin + 2;
            trailing.ErrorOffset = source.Length;
            trailing.ErrorCode = "incomplete";
            trailing.ErrorMessage = "More bytes are required to determine the complete FIX frame.";
            return trailing;
        }
        if (hasLimited)
        {
            return limited;
        }

        FrameResult noFrame = new FrameResult();
        noFrame.Status = FrameStatus.Incomplete;
        noFrame.FrameOffset = source.Length;
        noFrame.Consumed = source.Length;
        if (source.Length == int.MaxValue)
        {
            noFrame.Status = FrameStatus.LimitExceeded;
            noFrame.Required = int.MaxValue;
            noFrame.ErrorOffset = source.Length;
            noFrame.ErrorCode = "limit-exceeded";
            noFrame.ErrorMessage = "The FIX frame could not be recovered within the supported size range.";
            return noFrame;
        }
        noFrame.Required = source.Length + 1;
        noFrame.ErrorOffset = source.Length;
        noFrame.ErrorCode = "incomplete";
        noFrame.ErrorMessage = "More bytes are required to locate a FIX frame boundary.";
        return noFrame;
    }

    internal static FrameResult AdjustRecoveredResult(FrameResult result, int offset, bool complete)
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
            result.ErrorMessage = "The recovered FIX frame exceeds the supported size range.";
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
